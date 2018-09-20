// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public abstract class BaseGetSetTimes<T> : FileSystemTest
    {
        protected const string HFS = "hfs";
        public delegate void SetTime(T item, DateTime time);
        public delegate DateTime GetTime(T item);
        // AppContainer restricts access to DriveFormat (::GetVolumeInformation)
        private static string driveFormat = PlatformDetection.IsInAppContainer ? string.Empty : new DriveInfo(Path.GetTempPath()).DriveFormat;

        protected static bool isHFS => driveFormat != null && driveFormat.Equals(HFS, StringComparison.InvariantCultureIgnoreCase);
        protected static bool isNotHFS => !isHFS;

        public abstract T GetExistingItem();
        public abstract T GetMissingItem();

        public abstract string GetItemPath(T item);

        public abstract IEnumerable<TimeFunction> TimeFunctions(bool requiresRoundtripping = false);

        public class TimeFunction : Tuple<SetTime, GetTime, DateTimeKind>
        {
            public TimeFunction(SetTime setter, GetTime getter, DateTimeKind kind)
                : base(item1: setter, item2: getter, item3: kind)
            {
            }

            public static TimeFunction Create(SetTime setter, GetTime getter, DateTimeKind kind)
                => new TimeFunction(setter, getter, kind);

            public SetTime Setter => Item1;
            public GetTime Getter => Item2;
            public DateTimeKind Kind => Item3;
        }

        [Fact]
        public void SettingUpdatesProperties()
        {
            T item = GetExistingItem();

            Assert.All(TimeFunctions(requiresRoundtripping: true), (function) =>
            {
                // Checking that milliseconds are not dropped after setter.
                DateTime dt = new DateTime(2014, 12, 1, 12, 3, 3, isHFS ? 0 : 321, function.Kind);
                function.Setter(item, dt);
                DateTime result = function.Getter(item);
                Assert.Equal(dt, result);
                Assert.Equal(dt.ToLocalTime(), result.ToLocalTime());

                // File and Directory UTC APIs treat a DateTimeKind.Unspecified as UTC whereas
                // ToUniversalTime treats it as local.
                if (function.Kind == DateTimeKind.Unspecified)
                {
                    Assert.Equal(dt, result.ToUniversalTime());
                }
                else
                {
                    Assert.Equal(dt.ToUniversalTime(), result.ToUniversalTime());
                }
            });
        }

        [Fact]
        public void CanGetAllTimesAfterCreation()
        {
            DateTime beforeTime = DateTime.UtcNow.AddSeconds(-3);
            T item = GetExistingItem();
            DateTime afterTime = DateTime.UtcNow.AddSeconds(3);
            ValidateSetTimes(item, beforeTime, afterTime);
        }

        [ConditionalFact(nameof(isNotHFS))] // OSX HFS driver format does not support millisec granularity
        public void TimesIncludeMillisecondPart()
        {
            T item = GetExistingItem();
            Assert.All(TimeFunctions(), (function) =>
            {
                var msec = 0;
                for (int i = 0; i < 5; i++)
                {
                    DateTime time = function.Getter(item);
                    msec = time.Millisecond;
                    if (msec != 0)
                        break;

                    // This case should only happen 1/1000 times, unless the OS/Filesystem does
                    // not support millisecond granularity.

                    // If it's 1/1000, or low granularity, this may help:
                    Thread.Sleep(1234);

                    // If it's the OS/Filesystem often returns 0 for the millisecond part, this may
                    // help prove it. This should only be written 1/1000 runs, unless the test is going to
                    // fail.
                    Console.WriteLine($"## TimesIncludeMillisecondPart got a file time of {time.ToString("o")} on {driveFormat}");

                    item = GetExistingItem(); // try a new file/directory
                }

                Assert.NotEqual(0, msec);
            });
        }

        [ConditionalFact(nameof(isHFS))]
        public void TimesIncludeMillisecondPart_HFS()
        {
            T item = GetExistingItem();
            // OSX HFS driver format does not support millisec granularity
            Assert.All(TimeFunctions(), (function) =>
            {
                DateTime time = function.Getter(item);
                Assert.Equal(0, time.Millisecond);
            });
        }

        protected void ValidateSetTimes(T item, DateTime beforeTime, DateTime afterTime)
        {
            Assert.All(TimeFunctions(), (function) =>
            {
                // We want to test all possible DateTimeKind conversions to ensure they function as expected
                if (function.Kind == DateTimeKind.Local)
                    Assert.InRange(function.Getter(item).Ticks, beforeTime.ToLocalTime().Ticks, afterTime.ToLocalTime().Ticks);
                else
                    Assert.InRange(function.Getter(item).Ticks, beforeTime.Ticks, afterTime.Ticks);
                Assert.InRange(function.Getter(item).ToLocalTime().Ticks, beforeTime.ToLocalTime().Ticks, afterTime.ToLocalTime().Ticks);
                Assert.InRange(function.Getter(item).ToUniversalTime().Ticks, beforeTime.Ticks, afterTime.Ticks);
            });
        }

        public void DoesntExist_ReturnsDefaultValues()
        {
            T item = GetMissingItem();

            Assert.All(TimeFunctions(), (function) =>
            {
                Assert.Equal(
                    function.Kind == DateTimeKind.Local
                        ? DateTime.FromFileTime(0).Ticks
                        : DateTime.FromFileTimeUtc(0).Ticks,
                    function.Getter(item).Ticks);
            });
        }
    }
}
