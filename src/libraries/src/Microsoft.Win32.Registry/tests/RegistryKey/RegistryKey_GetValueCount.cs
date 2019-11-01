// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValueCount : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                return TestRegistryKey.ValueCount;
            });
        }

        [Fact]
        public void TestValueCount()
        {
            Assert.Equal(expected: 0, actual: TestRegistryKey.ValueCount);

            TestRegistryKey.SetValue(TestRegistryKeyName, 5);
            Assert.Equal(expected: 1, actual: TestRegistryKey.ValueCount);

            TestRegistryKey.DeleteValue(TestRegistryKeyName);
            Assert.Equal(expected: 0, actual: TestRegistryKey.ValueCount);
        }

        [Fact]
        public void TestValueCount2()
        {
            int expectedCount = 0;
            foreach (var testCase in TestData.TestValueTypes)
            {
                TestRegistryKey.SetValue(testCase[0].ToString(), testCase[1]);
                ++expectedCount;
            }

            Assert.Equal(expectedCount, TestRegistryKey.ValueCount);
        }
    }
}
