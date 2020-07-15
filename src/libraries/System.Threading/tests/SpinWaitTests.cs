// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Threading.Tests
{
    public static class SpinWaitTests
    {
        [Fact]
        public static void RunSpinWaitTests()
        {
            SpinWait spinner = new SpinWait();

            spinner.SpinOnce();
            Assert.Equal(1, spinner.Count);
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

        [Fact]
        public static void SpinOnce_Sleep1Threshold()
        {
            SpinWait spinner = new SpinWait();

            AssertExtensions.Throws<ArgumentOutOfRangeException>("sleep1Threshold", () => spinner.SpinOnce(-2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sleep1Threshold", () => spinner.SpinOnce(int.MinValue));
            Assert.Equal(0, spinner.Count);

            spinner.SpinOnce(sleep1Threshold: -1);
            Assert.Equal(1, spinner.Count);
            spinner.SpinOnce(sleep1Threshold: 0);
            Assert.Equal(2, spinner.Count);
            spinner.SpinOnce(sleep1Threshold: 1);
            Assert.Equal(3, spinner.Count);
            spinner.SpinOnce(sleep1Threshold: int.MaxValue);
            Assert.Equal(4, spinner.Count);
            int i = 5;
            for (; i < 10; ++i)
            {
                spinner.SpinOnce(sleep1Threshold: -1);
                Assert.Equal(i, spinner.Count);
            }
            for (; i < 20; ++i)
            {
                spinner.SpinOnce(sleep1Threshold: 15);
                Assert.Equal(i, spinner.Count);
            }
        }
    }
}
