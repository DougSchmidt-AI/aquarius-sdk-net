﻿using System;
using System.Collections.Generic;
using System.Linq;
using Aquarius.Samples.Client;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Ploeh.AutoFixture;
using ServiceStack;

namespace Aquarius.UnitTests.Samples.Client
{
    [TestFixture]
    public class LazyGetTests
    {
        private IServiceClient _mockServiceClient;
        private IFixture _fixture;
        private ISamplesClient _client;

        [Route("/things", HttpMethods.Get)]
        public class GetThings : IReturn<ThingResults>, IPaginatedRequest
        {
            public string Cursor { get; set; }
        }

        public class ThingResults: IPaginatedResponse<Thing>
        {
            public int TotalCount { get; set; }
            public string Cursor { get; set; }
            public List<Thing> DomainObjects { get; set; }
        }

        public class Thing
        {
            public string Name { get; set; }
        }


        [SetUp]
        public void ForEachTest()
        {
            _fixture = new Fixture();
            _mockServiceClient = Substitute.For<IServiceClient>();

            SetupMockClient();

            _client = SamplesClient.CreateTestClient(_mockServiceClient);
        }

        private void SetupMockClient()
        {
            _mockServiceClient
                .Get(Arg.Any<SamplesClient.GetStatus>())
                .Returns(new SamplesClient.StatusResponse {ReleaseName = "0.0"});
        }

        [Test]
        public void LazyGet_AllResultsInFirstResponse_Succeeds()
        {
            _mockServiceClient
                .Get(Arg.Any<GetThings>())
                .Returns(
                    x => CreateResults(2, 2));

            var items = EvaluateLazyResults();

            AssertResults(items, 1, 2);
        }

        private ThingResults CreateResults(int resultCount, int totalCount)
        {
            var results = new ThingResults
            {
                Cursor = _fixture.Create<string>(),
                DomainObjects = new List<Thing>(),
                TotalCount = totalCount
            };

            if (resultCount > 0)
            {
                results.DomainObjects = _fixture.CreateMany<Thing>(resultCount).ToList();
            }
            else
            {
                results.Cursor = null;
            }

            return results;
        }

        private List<Thing> EvaluateLazyResults()
        {
            return _client.LazyGet<Thing, GetThings, ThingResults>(new GetThings()).DomainObjects.ToList();
        }

        private void AssertResults(List<Thing> items, int expectedRequestCount, int expectedTotalCount)
        {
            items.Count.ShouldBeEquivalentTo(expectedTotalCount, "Total count of items should match");

            _mockServiceClient
                .Received(expectedRequestCount)
                .Get(Arg.Any<GetThings>());
        }

        [Test]
        public void LazyGet_SecondResponseFails_Throws()
        {
            _mockServiceClient
                .Get(Arg.Any<GetThings>())
                .Returns(
                    x => CreateResults(1, 2),
                    x => { throw new ArgumentException();});

            ((Action) (() => EvaluateLazyResults())).ShouldThrow<ArgumentException>();

        }
        // public void LazyGet_AllResultsInFirstResponse_Succeeds()
    }
}
