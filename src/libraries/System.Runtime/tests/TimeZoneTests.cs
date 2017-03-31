// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public static partial class TimeZoneTests
    {
        [Fact]
        public static void TestBasicTimeZoneProperties()
        {
#pragma warning disable 0618
            var currentZone = TimeZone.CurrentTimeZone;
#pragma warning restore 0618
            Assert.Equal(TimeZoneInfo.Local.StandardName, currentZone.StandardName);
            Assert.Equal(TimeZoneInfo.Local.DaylightName, currentZone.DaylightName);

            int year = DateTime.Today.Year;

            DaylightTime dt = currentZone.GetDaylightChanges(year);

            TimeZoneInfo.AdjustmentRule currentRule = null;
            if (TimeZoneInfo.Local.SupportsDaylightSavingTime)
            {
                foreach (var rule in TimeZoneInfo.Local.GetAdjustmentRules())
                {
                    if (rule.DateStart.Year <= year && rule.DateEnd.Year >= year && rule.DaylightDelta != TimeSpan.Zero)
                    {
                        currentRule = rule;
                        break;
                    }
                }
            }

            if (currentRule != null)
            {
                DateTime startTransition = TransitionTimeToDateTime(year, currentRule.DaylightTransitionStart);
                DateTime endTransition = TransitionTimeToDateTime(year, currentRule.DaylightTransitionEnd);

                Assert.Equal(startTransition, dt.Start);
                Assert.Equal(endTransition, dt.End);
                Assert.Equal(currentRule.DaylightDelta, dt.Delta);
            }
            else
            {
                Assert.Equal(DateTime.MinValue, dt.Start);
                Assert.Equal(DateTime.MinValue, dt.End);
                Assert.Equal(TimeSpan.Zero, dt.Delta);
            }
        }

        [Fact]
        public static void TestOffsetsAndDaylight()
        {
#pragma warning disable 0618
            var currentZone = TimeZone.CurrentTimeZone;
#pragma warning restore 0618
            DateTime now = DateTime.Now;

            Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(now), currentZone.GetUtcOffset(now));
            Assert.Equal(TimeZoneInfo.Local.IsDaylightSavingTime(now), currentZone.IsDaylightSavingTime(now));
#pragma warning disable 0618
            Assert.Equal(TimeZoneInfo.Local.IsDaylightSavingTime(now), TimeZone.IsDaylightSavingTime(now, currentZone.GetDaylightChanges(now.Year)));
#pragma warning restore 0618
        }

        [Fact]
        public static void TestRoundTripping()
        {
#pragma warning disable 0618
            var currentZone = TimeZone.CurrentTimeZone;
#pragma warning restore 0618
            DateTime now = DateTime.Now;

            if (!TimeZoneInfo.Local.IsAmbiguousTime(now))
            {
                var utcTime = currentZone.ToUniversalTime(now);
                var localTime = currentZone.ToLocalTime(utcTime);
                Assert.Equal(now, localTime);
            }
        }

        private static DateTime TransitionTimeToDateTime(Int32 year, TimeZoneInfo.TransitionTime transitionTime)
        {
            DateTime value;
            DateTime timeOfDay = transitionTime.TimeOfDay;

            if (transitionTime.IsFixedDateRule)
            {
                Int32 day = DateTime.DaysInMonth(year, transitionTime.Month);
                value = new DateTime(year, transitionTime.Month, (day < transitionTime.Day) ? day : transitionTime.Day, 
                            timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
            }
            else
            {
                if (transitionTime.Week <= 4)
                {
                    value = new DateTime(year, transitionTime.Month, 1, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = (int)transitionTime.DayOfWeek - dayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }
                    delta += 7 * (transitionTime.Week - 1);

                    if (delta > 0)
                    {
                        value = value.AddDays(delta);
                    }
                }
                else
                {
                    Int32 daysInMonth = DateTime.DaysInMonth(year, transitionTime.Month);
                    value = new DateTime(year, transitionTime.Month, daysInMonth, timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

                    int dayOfWeek = (int) value.DayOfWeek;
                    int delta = dayOfWeek - (int)transitionTime.DayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }

                    if (delta > 0) {
                        value = value.AddDays(-delta);
                    }
                }
            }
            return value;
        }
    }
}