﻿using System;
using System.Runtime.Serialization;
using Aquarius.Samples.Client;
using Aquarius.Samples.Client.ServiceModel;
using Aquarius.TimeSeries.Client;
using FluentAssertions;
using NodaTime;
using NUnit.Framework;
using ServiceStack;

namespace Aquarius.UnitTests.Samples.Client
{
    [TestFixture]
    public class JsonSerializationTests
    {
        [TestFixtureSetUp]
        public void BeforeAnyTests()
        {
            ServiceStackConfig.ConfigureServiceStack();
        }

        private const string Iso8601AprilFools2014 = "2014-04-01T00:00:00Z";
        private const string Iso8601AprilFools2015 = "2015-04-01T00:00:00Z";

        private static readonly Instant AprilFools2014 = Instant.FromDateTimeUtc(new DateTime(2014, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        private static readonly Instant AprilFools2015 = Instant.FromDateTimeUtc(new DateTime(2015, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        public readonly object[][] ValidTimeRanges =
        {
            new object[]
            {
                "Empty",
                "{}",
                new TimeRange()
            },
            new object[]
            {
                "Only startTime",
                "{\"startTime\":\"" + Iso8601AprilFools2014 + "\"}",
                new TimeRange{StartTime = AprilFools2014}
            },
            new object[]
            {
                "Only endTime",
                "{\"endTime\":\"" + Iso8601AprilFools2014 + "\"}",
                new TimeRange{EndTime = AprilFools2014}
            },
            new object[]
            {
                "A year of fools",
                "{\"startTime\":\"" + Iso8601AprilFools2014 + "\",\"endTime\":\"" + Iso8601AprilFools2015 + "\"}",
                new TimeRange{StartTime = AprilFools2014, EndTime = AprilFools2015}
            },
        };

        [TestCaseSource(nameof(ValidTimeRanges))]
        public void TimeRange_Valid_ShouldParseFromJsonAndRoundtrip(string reason, string jsonText, TimeRange timeRange)
        {
            var actualTimeRange = jsonText.FromJson<TimeRange>();
            actualTimeRange.ShouldBeEquivalentTo(timeRange, $"From JSON: {reason}");

            var actualJsonText = actualTimeRange.ToJson();
            var actualRoundTrip = actualJsonText.FromJson<TimeRange>();

            actualRoundTrip.ShouldBeEquivalentTo(timeRange, $"Round trip: {reason}");
        }

        [Test]
        public void TimeRange_FromInvalidJson_Throws()
        {
            var invalidJsonWithIdenticalStartAndEndTimes = "{\"startTime\":\"" + Iso8601AprilFools2014 +
                                                           "\",\"endTime\":\"" + Iso8601AprilFools2014 + "\"}";

            Action action = () => invalidJsonWithIdenticalStartAndEndTimes.FromJson<TimeRange>();

            action.ShouldThrow<SerializationException>();
        }

        public class DtoWithTimestamps
        {
            public Timestamp StartTime { get; set; }
            public Timestamp EndTime { get; set; }
        }

        [TestCaseSource(nameof(ValidTimeRanges))]
        public void Timestamp_FromValidJson(string reason, string jsonText, TimeRange timeRange)
        {
            var actual = jsonText.FromJson<DtoWithTimestamps>();
            var expected = new DtoWithTimestamps {StartTime = timeRange.StartTime, EndTime = timeRange.EndTime};
            actual.ShouldBeEquivalentTo(expected, $"From JSON: {reason}");

            var actualJsonText = actual.ToJson();
            var actualRoundTrip = actualJsonText.FromJson<DtoWithTimestamps>();

            actualRoundTrip.ShouldBeEquivalentTo(actual, $"Round trip: {reason}");
        }
    }
}
