// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteValue_Str_Bln : TestSubKey
    {
        private const string TestKey = "REG_TEST_6";

        public RegistryKey_DeleteValue_Str_Bln()
            : base(TestKey)
        {
        }

        [Fact]
        public void NegativeTests()
        {
            const string valueName = "TestValue";

            Assert.Throws<ArgumentException>(() => _testRegistryKey.DeleteValue(null, true));

            // Should NOT throw because value doesn't exists
            _testRegistryKey.DeleteValue(valueName, throwOnMissingValue: false);

            // Should throw because value doesn't exists
            Assert.Throws<ArgumentException>(() => _testRegistryKey.DeleteValue(valueName, throwOnMissingValue: true));

            _testRegistryKey.SetValue(valueName, 42);

            // Should throw because RegistryKey is readonly
            using (var rk = Registry.CurrentUser.OpenSubKey(TestKey, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(valueName, true));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.DeleteValue(valueName, true);
            });
        }

        [Fact]
        public void DeleteValueTest()
        {
            // [] Vanilla case, deleting a value
            const string valueName = TestKey;
            Assert.Equal(expected: 0, actual: _testRegistryKey.ValueCount);
            _testRegistryKey.SetValue(valueName, 5);
            Assert.Equal(expected: 1, actual: _testRegistryKey.ValueCount);
            _testRegistryKey.DeleteValue(valueName, throwOnMissingValue: true);
            Assert.Equal(expected: 0, actual: _testRegistryKey.ValueCount);
        }

        [Fact]
        public void Test04()
        {
            // [] Vanilla case , add a  bunch different objects and then Delete them
            object[][] testCases = TestData.TestValueTypes.ToArray();
            foreach (var testCase in testCases)
            {
                _testRegistryKey.SetValue(testCase[0].ToString(), testCase[1]);
            }

            Assert.Equal(expected: testCases.Length, actual: _testRegistryKey.ValueCount);

            foreach (var testCase in testCases)
            {
                _testRegistryKey.DeleteValue(testCase[0].ToString(), throwOnMissingValue: true);
            }

            Assert.Equal(expected: 0, actual: _testRegistryKey.ValueCount);
        }
    }
}
