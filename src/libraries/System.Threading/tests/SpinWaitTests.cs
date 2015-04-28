// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Threading;

namespace Test
{
    public static class SpinWaitTests
    {
        //
        // SpinWait
        //
        [Fact]
        public static void RunSpinWaitTests()
        {
            SpinWait spinner = new SpinWait();

            spinner.SpinOnce();
            Assert.Equal(spinner.Count, 1);
        }

        [Fact]
        public static void RunSpinWaitTests_Negative()
        {
            //test SpinUntil
            Assert.Throws<ArgumentNullException>(
               () => SpinWait.SpinUntil(null));
            // Failure Case:  SpinUntil didn't throw ANE when null condition  passed
            Assert.Throws<ArgumentOutOfRangeException>(
               () => SpinWait.SpinUntil(() => true, TimeSpan.MaxValue));
            // Failure Case:  SpinUntil didn't throw AORE when milliseconds > int.Max passed
            Assert.Throws<ArgumentOutOfRangeException>(
               () => SpinWait.SpinUntil(() => true, -2));
            // Failure Case:  SpinUntil didn't throw AORE when milliseconds < -1 passed

            Assert.False(SpinWait.SpinUntil(() => false, TimeSpan.FromMilliseconds(100)),
               "RunSpinWaitTests:  SpinUntil returned true when the condition i always false!");
            Assert.True(SpinWait.SpinUntil(() => true, 0),
               "RunSpinWaitTests:  SpinUntil returned false when the condition i always true!");
        }
    }
}
