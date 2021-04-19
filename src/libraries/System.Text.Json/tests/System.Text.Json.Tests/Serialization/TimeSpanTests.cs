// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.Json.Serialization.Tests
{
    public static class TimeSpanTests
    {
        [Theory]
        [InlineData(@"""PT-1S""")]
        [InlineData(@"""PT-1M""")]
        [InlineData(@"""PT-1H""")]
        [InlineData(@"""P-1D""")]
        [InlineData(@"""P-1M""")]
        [InlineData(@"""P-1Y""")]
        [InlineData(@"""T1S""")]
        [InlineData(@"""T1M""")]
        [InlineData(@"""T1H""")]
        [InlineData(@"""1D""")]
        [InlineData(@"""1M""")]
        [InlineData(@"""1Y""")]
        [InlineData(@"""P1Y2M3D4H5M6""")]
        [InlineData(@"""P1Y2M3DT4H5M6""")]
        [InlineData(@"""P1Y2M3DT4H5""")]
        [InlineData(@"""P1Y2M3DT4""")]
        [InlineData(@"""P1Y2M3""")]
        [InlineData(@"""P1Y2""")]
        [InlineData(@"""P1""")]
        public static void ReadInvalid(string json) =>
            Assert.Throws<FormatException>(() => JsonSerializer.Deserialize<TimeSpan>(json));

        [Theory]
        [MemberData(nameof(ReadCases))]
        public static void Read(string json, TimeSpan expected) =>
            Assert.Equal(expected, JsonSerializer.Deserialize<TimeSpan>(json));

        [Theory]
        [MemberData(nameof(WriteCases))]
        public static void Write(string expected, TimeSpan value) =>
            Assert.Equal(expected, JsonSerializer.Serialize(value));

        public static IEnumerable<object[]> ReadCases() =>
            TestCases().Concat(
                new []
                {
                    new object[] { @"""PT1.5S""", Time(0, 0, 1, 5_000_000) },
                    new object[] { @"""PT1.5M""", Time(0, 1, 30) },
                    new object[] { @"""PT1.5H""", Time(1, 30, 0) },
                    new object[] { @"""P1.5D""", Date(0, 0, 1) + Time(12, 0, 0) },
                    new object[] { @"""P1.5M""", Date(0, 1, 15) },
                    new object[] { @"""P1.5Y""", Date(1, 6, 0) },
                    new object[] { @"""PT1,5S""", Time(0, 0, 1, 5_000_000) },
                    new object[] { @"""PT1,5M""", Time(0, 1, 30) },
                    new object[] { @"""PT1,5H""", Time(1, 30, 0) },
                    new object[] { @"""P1,5D""", Date(0, 0, 1) + Time(12, 0, 0) },
                    new object[] { @"""P1,5M""", Date(0, 1, 15) },
                    new object[] { @"""P1,5Y""", Date(1, 6, 0) },
                });

        public static IEnumerable<object[]> WriteCases() =>
            TestCases().Concat(
                new []
                {
                    new object[] { @"""PT1.5S""", Time(0, 0, 1, 5_000_000) },
                    new object[] { @"""PT1M30S""", Time(0, 1, 30) },
                    new object[] { @"""PT1H30M""", Time(1, 30, 0) },
                    new object[] { @"""P1DT12H""", Date(0, 0, 1) + Time(12, 0, 0) },
                    new object[] { @"""P1M15D""", Date(0, 1, 15) },
                    new object[] { @"""P1Y6M""", Date(1, 6, 0) },
                });
            
        private static IEnumerable<object[]> TestCases()
        {
            yield return new object[] { @"""PT0S""", TimeSpan.Zero };
            yield return new object[] { @"""P29247Y1M14DT2H48M5.4775807S""", TimeSpan.MaxValue };
            yield return new object[] { @"""-P29247Y1M14DT2H48M5.4775808S""", TimeSpan.MinValue };

            yield return new object[] { @"""PT1S""", Time(0, 0, 1) };
            yield return new object[] { @"""PT1M""", Time(0, 1, 0) };
            yield return new object[] { @"""PT1H""", Time(1, 0, 0) };
            yield return new object[] { @"""P1D""", Date(0, 0, 1) };
            yield return new object[] { @"""P1M""", Date(0, 1, 0) };
            yield return new object[] { @"""P1Y""", Date(1, 0, 0) };

            yield return new object[] { @"""PT6S""", Time(0, 0, 6) };
            yield return new object[] { @"""PT5M6S""", Time(0, 5, 6) };
            yield return new object[] { @"""PT4H5M6S""", Time(4, 5, 6) };
            yield return new object[] { @"""P3D""", Date(0, 0, 3) };
            yield return new object[] { @"""P2M3D""", Date(0, 2, 3) };
            yield return new object[] { @"""P1Y2M3D""", Date(1, 2, 3) };
            yield return new object[] { @"""P3DT4H5M6S""", Date(0, 0, 3) + Time(4, 5, 6) };
            yield return new object[] { @"""P2M3DT4H5M6S""", Date(0, 2, 3) + Time(4, 5, 6) };
            yield return new object[] { @"""P1Y2M3DT4H5M6S""", Date(1, 2, 3) + Time(4, 5, 6) };
            yield return new object[] { @"""P1Y2M3DT4H5M""", Date(1, 2, 3) + Time(4, 5, 0) };
            yield return new object[] { @"""P1Y2M3DT4H""", Date(1, 2, 3) + Time(4, 0, 0) };
            yield return new object[] { @"""P1Y2M3D""", Date(1, 2, 3) };
            yield return new object[] { @"""P1Y2M""", Date(1, 2, 0) };
            yield return new object[] { @"""P1Y""", Date(1, 0, 0) };
            
            yield return new object[] { @"""PT4H6S""", Time(4, 0, 6) };
            yield return new object[] { @"""P1Y3D""", Date(1, 0, 3) };
        }

        static TimeSpan Date(int years, int months, int days) =>
            new TimeSpan(
                years * TimeSpan.TicksPerDay * 365 +
                months * TimeSpan.TicksPerDay * 30 +
                days * TimeSpan.TicksPerDay);
    
        static TimeSpan Time(int hours, int minutes, int seconds, int nanoseconds = 0) =>
            new TimeSpan(
                hours * TimeSpan.TicksPerHour +
                minutes * TimeSpan.TicksPerMinute +
                seconds * TimeSpan.TicksPerSecond +
                nanoseconds);

    }
}
