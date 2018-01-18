// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.IO.Tests
{
    public abstract class BaseGetSetTimes<T> : FileSystemTest
    {
        public delegate void SetTime(T item, DateTime time);
        public delegate DateTime GetTime(T item);

        public abstract T GetExistingItem();
        public abstract T GetMissingItem();

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
                DateTime dt = new DateTime(2014, 12, 1, 12, 0, 0, function.Kind);
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

        [Fact]
        [ActiveIssue(26349, TestPlatforms.AnyUnix)]
        public void TimesIncludeMillisecondPart()
        {
            T item = GetExistingItem();
            Assert.All(TimeFunctions(), (function) =>
            {
                var msec = 0;
                for (int i = 0; i < 3; i++)
                {
                    msec = function.Getter(item).Millisecond;
                    if (msec != 0)
                        break;

                    item = GetExistingItem(); // try a new file/directory
                }

                Assert.NotEqual(0, msec);
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
