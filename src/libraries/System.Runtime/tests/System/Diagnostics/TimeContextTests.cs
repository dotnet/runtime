// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

using TestClock = System.Tests.TimeClockTests.TestClock;

namespace System.Tests
{
    public class TimeContextTests
    {
        private readonly ITestOutputHelper _output;
        private readonly TimeClock _testClock = new TestClock();
        private static readonly TimeZoneInfo s_testTimeZone = GetTestTimeZone();

        public TimeContextTests(ITestOutputHelper output)
        {
            _output = output;
        }

        #region Examples

        [Fact]
        public void LeapYearBusinessLogicTest1()
        {
            // This test demonstrates testing code that is sensitive to leap years.
            // Testing actual application code like this might expose leap day bugs before they become an issue.

            DateTime GetOneYearFromToday_Incorrect()
            {
                // This shows an incorrect way to get a date that is one year from today.
                // It will throw an exception when "today" is a leap day (Februrary 29th).
                return new DateTime(DateTime.Today.Year + 1, DateTime.Today.Month, DateTime.Today.Day);
            }

            DateTime GetOneYearFromToday_Correct()
            {
                // This shows the correct way to get a date that is one year from today.
                // When run on a leap day (Februrary 29th), it will return February 28th of the following year.
                return DateTime.Today.AddYears(1);
            }

            // When the current year is not a leap year, either method will pass the test on the last day of February.
            var clock1 = new VirtualClock(new DateTime(2021, 2, DateTime.DaysInMonth(2021, 2)));
            TimeContext.Run(clock1, () =>
            {
                var result1a = GetOneYearFromToday_Correct();
                Assert.Equal(new DateTime(2022, 2, 28), result1a);

                var result1b = GetOneYearFromToday_Incorrect();
                Assert.Equal(new DateTime(2022, 2, 28), result1b);
            });

            // When the current year is a leap year, only the correct method will pass the test on the last day of February.
            var clock2 = new VirtualClock(new DateTime(2024, 2, DateTime.DaysInMonth(2024, 2)));
            TimeContext.Run(clock2, () =>
            {
                var result2a = GetOneYearFromToday_Correct();
                Assert.Equal(new DateTime(2025, 2, 28), result2a);

                // In an application's unit testing, we'd expect the bad code to fail the test.
                // However here we're expecting it to throw, highlighting the value of testing such code.
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var result2b = GetOneYearFromToday_Incorrect();
                });
            });
        }

        [Fact]
        public void LeapYearBusinessLogicTest2()
        {
            // This test demonstrates testing code that is sensitive to leap years.
            // Testing actual application code like this might expose leap day bugs before they become an issue.

            DateTime GetBirthdayThisYear_Incorrect(DateTime dateOfBirth)
            {
                // This shows an incorrect way to get a birthday for the current year.
                // It will throw an exception for February 29th birthdays when the current year is not a leap year.
                return new DateTime(DateTime.Now.Year, dateOfBirth.Month, dateOfBirth.Day);
            }

            DateTime GetBirthdayThisYear_Correct(DateTime dateOfBirth)
            {
                // This is the correct way to get a birthday for the current year.
                // It will treat February 29th birthdays as February 28th when the current year is not a leap year.
                return dateOfBirth.AddYears(DateTime.Now.Year - dateOfBirth.Year);
            }

            var dateOfBirth = new DateTime(2000, 2, 29);

            // When the current year is a leap year, either method will pass the test.
            var clock1 = new VirtualClock(new DateTime(2020, 1, 1));
            TimeContext.Run(clock1, () =>
            {
                var result1a = GetBirthdayThisYear_Correct(dateOfBirth);
                Assert.Equal(new DateTime(2020, 2, 29), result1a);

                var result1b = GetBirthdayThisYear_Incorrect(dateOfBirth);
                Assert.Equal(new DateTime(2020, 2, 29), result1b);
            });

            // When the current year is not a leap year, only the correct method will pass the test.
            var clock2 = new VirtualClock(new DateTime(2021, 1, 1));
            TimeContext.Run(clock2, () =>
            {
                var result2a = GetBirthdayThisYear_Correct(dateOfBirth);
                Assert.Equal(new DateTime(2021, 2, 28), result2a);

                // In an application's unit testing, we'd expect the bad code to fail the test.
                // However here we're expecting it to throw, highlighting the value of testing such code.
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    var result2b = GetBirthdayThisYear_Incorrect(dateOfBirth);
                });
            });
        }

        // The following two test classes are used in the LeapYearBusinessLogicTest3 below it.

        private class TestClassWithIncorrectInternalArray
        {
            // This shows an incorrect way to define an array with values for each day of the current year.
            private int[] _values = new int[365];

            public void AssignTodaysValue(int value)
            {
                // This will work correctly on most days, but it will throw an exeception when used
                // on the last day of a leap year (Day 366).
                _values[DateTime.Today.DayOfYear - 1] = value;
            }
        }

        private class TestClassWithCorrectInternalArray
        {
            // This shows one way to correctly define an array with values for each day of the current year.
            private int[] _values = new int[DateTime.IsLeapYear(DateTime.Today.Year) ? 366 : 365];

            public void AssignTodaysValue(int value)
            {
                // This will work correctly for every day, including Day 366.
                _values[DateTime.Today.DayOfYear - 1] = value;
            }
        }

        [Fact]
        public void LeapYearBusinessLogicTest3()
        {
            // This test demonstrates testing code that is sensitive to leap years.
            // Testing actual application code like this might expose leap day bugs before they become an issue.

            // When the current year is not a leap year, either method will pass the test.
            var clock1 = new VirtualClock(new DateTime(2021, 12, 31));
            TimeContext.Run(clock1, () =>
            {
                var test1 = new TestClassWithCorrectInternalArray();
                test1.AssignTodaysValue(12345);

                var test2 = new TestClassWithIncorrectInternalArray();
                test2.AssignTodaysValue(12345);
            });

            // When the current year is a leap year, only the correct method will pass the test.
            var clock2 = new VirtualClock(new DateTime(2024, 12, 31));
            TimeContext.Run(clock2, () =>
            {
                var test1 = new TestClassWithCorrectInternalArray();
                test1.AssignTodaysValue(12345);

                // In an application's unit testing, we'd expect the bad code to fail the test.
                // However here we're expecting it to throw, highlighting the value of testing such code.
                Assert.Throws<IndexOutOfRangeException>(() =>
                {
                    var test2 = new TestClassWithIncorrectInternalArray();
                    test2.AssignTodaysValue(12345);
                });
            });
        }

        [Fact]
        public void DaylightSavingTime_SpringForwardTest()
        {
            // This test demonstrates advancing the clock by whole hours on the day of a DST "spring-forward" transition.
            // The time context ensures that the values returned from DateTime.Now are controled by the provided VirtualClock.
            // Testing actual application code like this might expose any bugs related to the start of daylight saving time.

            var dt = new DateTime(2020, 3, 8, 0, 0, 0);
            var advancement = TimeSpan.FromHours(1);
            var clock = new VirtualClock(dt, advancement);

            var stdOffset = s_testTimeZone.GetUtcOffset(new DateTime(2020, 1, 1));
            var dstOffset = s_testTimeZone.GetUtcOffset(new DateTime(2020, 7, 1));

            TimeContext.Run(clock, s_testTimeZone, () =>
            {
                var firstValue = DateTimeOffset.Now;
                var secondValue = DateTimeOffset.Now;
                var thirdValue = DateTimeOffset.Now;

                // In the test time zone, DST started on 2020-03-08, when the local time advanced from 1:59:59 to 3:00:00.
                // Thus, the 2:00 hour is skipped.  Since we are advancing by whole hours, we should not see it in
                // consecutive calls to DateTime.Now.
                Assert.Equal(new DateTimeOffset(2020, 3, 8, 0, 0, 0, stdOffset), firstValue);
                Assert.Equal(new DateTimeOffset(2020, 3, 8, 1, 0, 0, stdOffset), secondValue);
                Assert.Equal(new DateTimeOffset(2020, 3, 8, 3, 0, 0, dstOffset), thirdValue);

                // Test the offsets separately also.
                Assert.Equal(stdOffset, firstValue.Offset);
                Assert.Equal(stdOffset, secondValue.Offset);
                Assert.Equal(dstOffset, thirdValue.Offset);
            });
        }

        [Fact]
        public void DaylightSavingTime_FallBackTest()
        {
            // This test demonstrates advancing the clock by whole hours on the day of a DST "fall-back" transition.
            // The time context ensures that the values returned from DateTime.Now are controled by the provided VirtualClock.
            // Testing actual application code like this might expose any bugs related to the end of daylight saving time.

            var dt = new DateTime(2020, 11, 1, 0, 0, 0);
            var advancement = TimeSpan.FromHours(1);
            var clock = new VirtualClock(dt, advancement);

            var stdOffset = s_testTimeZone.GetUtcOffset(new DateTime(2020, 1, 1));
            var dstOffset = s_testTimeZone.GetUtcOffset(new DateTime(2020, 7, 1));

            TimeContext.Run(clock, s_testTimeZone, () =>
            {
                var firstValue = DateTimeOffset.Now;
                var secondValue = DateTimeOffset.Now;
                var thirdValue = DateTimeOffset.Now;
                var fourthValue = DateTimeOffset.Now;

                // In the test time zone, DST ended on 2020-11-01, when the local time advanced from 1:59:59 to 1:00:00.
                // The 1:00 hour is repeated.  Since we are advancing by whole hours, we should see it twice in
                // consecutive calls to DateTime.Now.
                Assert.Equal(new DateTimeOffset(2020, 11, 1, 0, 0, 0, dstOffset), firstValue);
                Assert.Equal(new DateTimeOffset(2020, 11, 1, 1, 0, 0, dstOffset), secondValue);
                Assert.Equal(new DateTimeOffset(2020, 11, 1, 1, 0, 0, stdOffset), thirdValue);
                Assert.Equal(new DateTimeOffset(2020, 11, 1, 2, 0, 0, stdOffset), fourthValue);

                // Test the offsets separately also.
                Assert.Equal(dstOffset, firstValue.Offset);
                Assert.Equal(dstOffset, secondValue.Offset);
                Assert.Equal(stdOffset, thirdValue.Offset);
                Assert.Equal(stdOffset, fourthValue.Offset);
            });
        }

        #endregion

        #region Default Values Tests

        [Fact]
        public void CurrentClock_IsActualSystemClockByDefault()
        {
            Assert.True(TimeContext.ActualSystemClockIsActive);
            Assert.IsType<ActualSystemClock>(TimeContext.Current.Clock);
        }

        [Fact]
        public void CurrentLocalTimeZone_IsActualSystemLocalTimeZoneByDefault()
        {
            // Unlike the clock test, we cannot tell if the value from ActualSystemLocalTimeZone
            // is indeed the actual system local time zone by testing the value or its type.
            // We'll just emit it to the test output for easy inspection if needed.
            TimeZoneInfo tz = TimeContext.ActualSystemLocalTimeZone;
            _output.WriteLine($"The local system time zone is: [{ tz.Id }] { tz.DisplayName }.");

            // However, we can test if the ActualSystemLocalTimeZoneIsActive property is working.
            // Internally this property compares the accessor function rather than the time zone value itself.
            Assert.True(TimeContext.ActualSystemLocalTimeZoneIsActive);
        }

        #endregion

        #region Multi-threading/task Tests

        [Fact]
        public void CanUseTwoDifferentClockInTwoDifferentThreads()
        {
            var dt1 = new DateTime(2000, 1, 1);
            var dt2 = new DateTime(2020, 12, 31);

            var clock1 = new VirtualClock(dt1);
            var clock2 = new VirtualClock(dt2);

            var t1 = new Thread(() =>
            {
                TimeContext.Run(clock1, () =>
                {
                    var now = DateTime.Now;
                    Assert.Equal(dt1, now);
                });
            });

            var t2 = new Thread(() =>
            {
                TimeContext.Run(clock2, () =>
                {
                    var now = DateTime.Now;
                    Assert.Equal(dt2, now);
                });
            });

            t1.Start();
            t2.Start();

            t1.Join();
            t2.Join();
        }

        [Fact]
        public async Task CanUseTwoDifferentClockInTwoDifferentTasks()
        {
            var dt1 = new DateTime(2000, 1, 1);
            var dt2 = new DateTime(2020, 12, 31);

            var clock1 = new VirtualClock(dt1);
            var clock2 = new VirtualClock(dt2);

            var t1 = new Task(async () =>
            {
                await TimeContext.RunAsync(clock1, async () =>
                {
                    await Task.Yield();

                    var now = DateTime.Now;
                    Assert.Equal(dt1, now);
                });
            });

            var t2 = new Task(async () =>
            {
                await TimeContext.RunAsync(clock2, async () =>
                {
                    await Task.Yield();

                    var now = DateTime.Now;
                    Assert.Equal(dt2, now);
                });
            });

            t1.Start();
            t2.Start();

            await Task.WhenAll(t1, t2);
        }
        #endregion

        #region Clock Changing Tests

        [Fact]
        public void CurrentClock_CanBeChangedForAnOperation()
        {
            TimeContext.Run(_testClock, () =>
            {
                Assert.IsType<TestClock>(TimeContext.Current.Clock);
                Assert.False(TimeContext.ActualSystemClockIsActive);
            });
        }

        [Fact]
        public void CurrentClock_IsRestoredAfterAnOperation()
        {
            TimeContext.Run(_testClock, () => { });

            Assert.IsType<ActualSystemClock>(TimeContext.Current.Clock);
            Assert.True(TimeContext.ActualSystemClockIsActive);
        }

        [Fact]
        public void CurrentClock_CanBeChangedForAnOperationWithResult()
        {
            int result = TimeContext.Run(_testClock, () =>
            {
                Assert.IsType<TestClock>(TimeContext.Current.Clock);
                Assert.False(TimeContext.ActualSystemClockIsActive);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public void CurrentClock_IsRestoredAfterAnOperationWithResult()
        {
            int result = TimeContext.Run(_testClock, () => 1);

            Assert.IsType<ActualSystemClock>(TimeContext.Current.Clock);
            Assert.True(TimeContext.ActualSystemClockIsActive);
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task CurrentClock_CanBeChangedForAnAsyncOperation()
        {
            await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                Assert.IsType<TestClock>(TimeContext.Current.Clock);
                Assert.False(TimeContext.ActualSystemClockIsActive);
            });
        }

        [Fact]
        public async Task CurrentClock_IsRestoredAfterAnAsyncOperation()
        {
            await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();
            });

            Assert.IsType<ActualSystemClock>(TimeContext.Current.Clock);
            Assert.True(TimeContext.ActualSystemClockIsActive);
        }

        [Fact]
        public async Task CurrentClock_CanBeChangedForAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                Assert.IsType<TestClock>(TimeContext.Current.Clock);
                Assert.False(TimeContext.ActualSystemClockIsActive);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task CurrentClock_IsRestoredAfterAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                return 1;
            });

            Assert.IsType<ActualSystemClock>(TimeContext.Current.Clock);
            Assert.True(TimeContext.ActualSystemClockIsActive);
            Assert.Equal(1, result);
        }

        [Fact]
        public void DateTimeOffset_UtcNow_UsesTestClockDuringAnOperation()
        {
            TimeContext.Run(_testClock, () =>
            {
                DateTimeOffset actual = DateTimeOffset.UtcNow;
                DateTimeOffset expected = TestClock.Value;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void DateTimeOffset_Now_UsesTestClockDuringAnOperation()
        {
            TimeContext.Run(_testClock, () =>
            {
                DateTimeOffset actual = DateTimeOffset.Now;
                DateTimeOffset expected = TestClock.Value;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void DateTime_UtcNow_UsesTestClockDuringAnOperation()
        {
            TimeContext.Run(_testClock, () =>
            {
                DateTime actual = DateTime.UtcNow;
                DateTime expected = TestClock.Value.UtcDateTime;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void DateTime_Now_UsesTestClockDuringAnOperation()
        {
            TimeContext.Run(_testClock, () =>
            {
                DateTime actual = DateTime.Now;
                DateTime expected = TestClock.Value.LocalDateTime;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void DateTime_Today_UsesTestClockDuringAnOperation()
        {
            TimeContext.Run(_testClock, () =>
            {
                DateTime actual = DateTime.Today;
                DateTime expected = TestClock.Value.LocalDateTime.Date;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void DateTimeOffset_UtcNow_UsesTestClockDuringAnOperationWithResult()
        {
            int result = TimeContext.Run(_testClock, () =>
            {
                DateTimeOffset actual = DateTimeOffset.UtcNow;
                DateTimeOffset expected = TestClock.Value;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public void DateTimeOffset_Now_UsesTestClockDuringAnOperationWithResult()
        {
            int result = TimeContext.Run(_testClock, () =>
            {
                DateTimeOffset actual = DateTimeOffset.Now;
                DateTimeOffset expected = TestClock.Value;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public void DateTime_UtcNow_UsesTestClockDuringAnOperationWithResult()
        {
            int result = TimeContext.Run(_testClock, () =>
            {
                DateTime actual = DateTime.UtcNow;
                DateTime expected = TestClock.Value.UtcDateTime;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public void DateTime_Now_UsesTestClockDuringAnOperationWithResult()
        {
            int result = TimeContext.Run(_testClock, () =>
            {
                DateTime actual = DateTime.Now;
                DateTime expected = TestClock.Value.LocalDateTime;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public void DateTime_Today_UsesTestClockDuringAnOperationWithResult()
        {
            int result = TimeContext.Run(_testClock, () =>
            {
                DateTime actual = DateTime.Today;
                DateTime expected = TestClock.Value.LocalDateTime.Date;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DateTimeOffset_UtcNow_UsesTestClockDuringAnAsyncOperation()
        {
            await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTimeOffset actual = DateTimeOffset.UtcNow;
                DateTimeOffset expected = TestClock.Value;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public async Task DateTimeOffset_Now_UsesTestClockDuringAnAsyncOperation()
        {
            await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTimeOffset actual = DateTimeOffset.Now;
                DateTimeOffset expected = TestClock.Value;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public async Task DateTime_UtcNow_UsesTestClockDuringAnAsyncOperation()
        {
            await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.UtcNow;
                DateTime expected = TestClock.Value.UtcDateTime;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public async Task DateTime_Now_UsesTestClockDuringAnAsyncOperation()
        {
            await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.Now;
                DateTime expected = TestClock.Value.LocalDateTime;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public async Task DateTime_Today_UsesTestClockDuringAnAsyncOperation()
        {
            await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.Today;
                DateTime expected = TestClock.Value.LocalDateTime.Date;
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public async Task DateTimeOffset_UtcNow_UsesTestClockDuringAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTimeOffset actual = DateTimeOffset.UtcNow;
                DateTimeOffset expected = TestClock.Value;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DateTimeOffset_Now_UsesTestClockDuringAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTimeOffset actual = DateTimeOffset.Now;
                DateTimeOffset expected = TestClock.Value;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DateTime_UtcNow_UsesTestClockDuringAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.UtcNow;
                DateTime expected = TestClock.Value.UtcDateTime;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DateTime_Now_UsesTestClockDuringAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.Now;
                DateTime expected = TestClock.Value.LocalDateTime;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DateTime_Today_UsesTestClockDuringAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(_testClock, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.Today;
                DateTime expected = TestClock.Value.LocalDateTime.Date;
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        #endregion

        #region Local Time Zone Changing Tests

        [Fact]
        public void CurrentLocalTimeZone_CanBeChangedForAnOperation()
        {
            var originalTimeZone = TimeContext.Current.LocalTimeZone;

            TimeContext.Run(s_testTimeZone, () =>
            {
                var localTimeZone = TimeContext.Current.LocalTimeZone;
                Assert.Equal(s_testTimeZone, localTimeZone);
                Assert.NotEqual(originalTimeZone, localTimeZone);
            });
        }

        [Fact]
        public void CurrentLocalTimeZone_IsRestoredAfterAnOperation()
        {
            var originalTimeZone = TimeContext.Current.LocalTimeZone;

            TimeContext.Run(s_testTimeZone, () => { });

            var localTimeZone = TimeContext.Current.LocalTimeZone;
            Assert.Equal(originalTimeZone, localTimeZone);
            Assert.NotEqual(s_testTimeZone, localTimeZone);
        }

        [Fact]
        public void CurrentLocalTimeZone_CanBeChangedForAnOperationWithResult()
        {
            var originalTimeZone = TimeContext.Current.LocalTimeZone;

            int result = TimeContext.Run(s_testTimeZone, () =>
            {
                var localTimeZone = TimeContext.Current.LocalTimeZone;
                Assert.Equal(s_testTimeZone, localTimeZone);
                Assert.NotEqual(originalTimeZone, localTimeZone);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public void CurrentLocalTimeZone_IsRestoredAfterAnOperationWithResult()
        {
            var originalTimeZone = TimeContext.Current.LocalTimeZone;

            int result = TimeContext.Run(s_testTimeZone, () => 1);

            var localTimeZone = TimeContext.Current.LocalTimeZone;
            Assert.Equal(originalTimeZone, localTimeZone);
            Assert.NotEqual(s_testTimeZone, localTimeZone);
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task CurrentLocalTimeZone_CanBeChangedForAnAsyncOperation()
        {
            var originalTimeZone = TimeContext.Current.LocalTimeZone;

            await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                var localTimeZone = TimeContext.Current.LocalTimeZone;
                Assert.Equal(s_testTimeZone, localTimeZone);
                Assert.NotEqual(originalTimeZone, localTimeZone);
            });
        }

        [Fact]
        public async Task CurrentLocalTimeZone_IsRestoredAfterAnAsyncOperation()
        {
            var originalTimeZone = TimeContext.Current.LocalTimeZone;

            await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();
            });

            var localTimeZone = TimeContext.Current.LocalTimeZone;
            Assert.Equal(originalTimeZone, localTimeZone);
            Assert.NotEqual(s_testTimeZone, localTimeZone);
        }

        [Fact]
        public async Task CurrentLocalTimeZone_CanBeChangedForAnAsyncOperationWithResult()
        {
            var originalTimeZone = TimeContext.Current.LocalTimeZone;

            int result = await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                var localTimeZone = TimeContext.Current.LocalTimeZone;
                Assert.Equal(s_testTimeZone, localTimeZone);
                Assert.NotEqual(originalTimeZone, localTimeZone);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task CurrentLocalTimeZone_IsRestoredAfterAnAsyncOperationWithResult()
        {
            var originalTimeZone = TimeContext.Current.LocalTimeZone;

            int result = await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                return 1;
            });

            var localTimeZone = TimeContext.Current.LocalTimeZone;
            Assert.Equal(originalTimeZone, localTimeZone);
            Assert.NotEqual(s_testTimeZone, localTimeZone);
            Assert.Equal(1, result);
        }

        [Fact]
        public void DateTimeOffset_Now_UsesTestTimeZoneDuringAnOperation()
        {
            TimeContext.Run(s_testTimeZone, () =>
            {
                DateTimeOffset actual = DateTimeOffset.Now;
                DateTimeOffset expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected.Offset, actual.Offset);
            });
        }

        [Fact]
        public void DateTime_Now_UsesTestTimeZoneDuringAnOperation()
        {
            TimeContext.Run(s_testTimeZone, () =>
            {
                DateTime actual = DateTime.Now;
                DateTime expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void DateTime_Today_UsesTestTimeZoneDuringAnOperation()
        {
            TimeContext.Run(s_testTimeZone, () =>
            {
                DateTime actual = DateTime.Today;
                DateTime expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public void DateTimeOffset_Now_UsesTestTimeZoneDuringAnOperationWithResult()
        {
            int result = TimeContext.Run(s_testTimeZone, () =>
            {
                DateTimeOffset actual = DateTimeOffset.Now;
                DateTimeOffset expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected.Offset, actual.Offset);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public void DateTime_Now_UsesTestTimeZoneDuringAnOperationWithResult()
        {
            int result = TimeContext.Run(s_testTimeZone, () =>
            {
                DateTime actual = DateTime.Now;
                DateTime expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public void DateTime_Today_UsesTestTimeZoneDuringAnOperationWithResult()
        {
            int result = TimeContext.Run(s_testTimeZone, () =>
            {
                DateTime actual = DateTime.Today;
                DateTime expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DateTimeOffset_Now_UsesTestTimeZoneDuringAnAsyncOperation()
        {
            await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                DateTimeOffset actual = DateTimeOffset.Now;
                DateTimeOffset expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected.Offset, actual.Offset);
            });
        }

        [Fact]
        public async Task DateTime_Now_UsesTestTimeZoneDuringAnAsyncOperation()
        {
            await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.Now;
                DateTime expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public async Task DateTime_Today_UsesTestTimeZoneDuringAnAsyncOperation()
        {
            await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.Today;
                DateTime expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected, actual);
            });
        }

        [Fact]
        public async Task DateTimeOffset_Now_UsesTestTimeZoneDuringAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                DateTimeOffset actual = DateTimeOffset.Now;
                DateTimeOffset expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected.Offset, actual.Offset);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DateTime_Now_UsesTestTimeZoneDuringAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.Now;
                DateTime expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        [Fact]
        public async Task DateTime_Today_UsesTestTimeZoneDuringAnAsyncOperationWithResult()
        {
            int result = await TimeContext.RunAsync(s_testTimeZone, async () =>
            {
                await Task.Yield();

                DateTime actual = DateTime.Today;
                DateTime expected = TimeZoneInfo.ConvertTime(actual, s_testTimeZone);
                Assert.Equal(expected, actual);
                return 1;
            });

            Assert.Equal(1, result);
        }

        #endregion

        #region Helper functions

        private static TimeZoneInfo GetTestTimeZone()
        {
            // Choose a test time zone that is not the actual system time zone.
            // We'll use USA time zones that have the same DST rules so we can test some transitions.
            bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string id1 = windows ? "Eastern Standard Time" : "America/New_York";
            string id2 = windows ? "Central Standard Time" : "America/Chicago";
            string tzid = TimeContext.ActualSystemLocalTimeZone.Id.Equals(id1, StringComparison.OrdinalIgnoreCase) ? id2 : id1;
            return TimeZoneInfo.FindSystemTimeZoneById(tzid);
        }

        #endregion
    }
}
