// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Threading.Tests
{
    public class TimerConstructorTests
    {
        public static IEnumerable<object[]> CallbacksForPeriodDueTimeOutOfRange()
        {
            yield return new object[] { null };
            yield return new object[] { new TimerCallback(_ => Assert.Fail("Callback should not have been invoked.")) };
        }

        [Theory]
        [MemberData(nameof(CallbacksForPeriodDueTimeOutOfRange))]
        public void Timer_Constructor_DueTimeOutOfRange_Throws(TimerCallback callback)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => new Timer(callback, null, -2, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => new Timer(callback, null, TimeSpan.FromMilliseconds(-2), new TimeSpan(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => new Timer(callback, null, -2L, 1L));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => new Timer(callback, null, TimeSpan.FromMilliseconds((long)0xFFFFFFFF), new TimeSpan(1)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("dueTime", () => new Timer(callback, null, 0xFFFFFFFFL, 1L));
        }

        [Theory]
        [MemberData(nameof(CallbacksForPeriodDueTimeOutOfRange))]
        public void Timer_Constructor_PeriodOutOfRange_Throws(TimerCallback callback)
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => new Timer(callback, null, 1, -2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => new Timer(callback, null, new TimeSpan(1), TimeSpan.FromMilliseconds(-2)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => new Timer(callback, null, 1L, -2L));

            AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => new Timer(callback, null, new TimeSpan(1), TimeSpan.FromMilliseconds(0xFFFFFFFF)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("period", () => new Timer(callback, null, 1L, 0xFFFFFFFFL));
        }


        [Fact]
        public void Timer_Constructor_NullCallback_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("callback", () => new Timer(null));
            AssertExtensions.Throws<ArgumentNullException>("callback", () => new Timer(null, new object(), 1, 1));
            AssertExtensions.Throws<ArgumentNullException>("callback", () => new Timer(null, new object(), 1L, 1L));
            AssertExtensions.Throws<ArgumentNullException>("callback", () => new Timer(null, new object(), (uint)1, (uint)1));
            AssertExtensions.Throws<ArgumentNullException>("callback", () => new Timer(null, new object(), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void Timer_AllConstructorsCanBeUsedSuccessfully()
        {
            const int Timeout = 10_000;
            new Timer(_ => { }, null, Timeout, Timeout).Dispose();
            new Timer(_ => { }, null, (long)Timeout, (long)Timeout).Dispose();
            new Timer(_ => { }, null, (uint)Timeout, (uint)Timeout).Dispose();
            new Timer(_ => { }, null, TimeSpan.FromMilliseconds(Timeout), TimeSpan.FromMilliseconds(Timeout)).Dispose();
        }
    }
}
