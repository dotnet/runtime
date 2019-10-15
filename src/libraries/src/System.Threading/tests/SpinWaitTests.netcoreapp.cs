// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Threading.Tests
{
    public static partial class SpinWaitTests
    {
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
