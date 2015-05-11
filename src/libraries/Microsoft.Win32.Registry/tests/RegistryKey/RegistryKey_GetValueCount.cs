// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValueCount : TestSubKey
    {
        private const string TestKey = "REG_TEST_10";

        public RegistryKey_GetValueCount()
            : base(TestKey)
        {
        }

        [Fact]
        public void NegativeTests()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                return _testRegistryKey.ValueCount;
            });
        }

        [Fact]
        public void TestValueCount()
        {
            Assert.Equal(expected: 0, actual: _testRegistryKey.ValueCount);

            _testRegistryKey.SetValue(TestKey, 5);
            Assert.Equal(expected: 1, actual: _testRegistryKey.ValueCount);

            _testRegistryKey.DeleteValue(TestKey);
            Assert.Equal(expected: 0, actual: _testRegistryKey.ValueCount);
        }

        [Fact]
        public void TestValueCount2()
        {
            int expectedCount = 0;
            foreach (var testCase in TestData.TestValueTypes)
            {
                _testRegistryKey.SetValue(testCase[0].ToString(), testCase[1]);
                ++expectedCount;
            }

            Assert.Equal(expectedCount, _testRegistryKey.ValueCount);
        }
    }
}
