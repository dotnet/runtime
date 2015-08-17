// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteValue_str : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string valueName = "TestValue";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentException>(() => _testRegistryKey.DeleteValue(null));

            // Should throw because value doesn't exists
            Assert.Throws<ArgumentException>(() => _testRegistryKey.DeleteValue(valueName));

            _testRegistryKey.SetValue(valueName, 42);

            // Should throw because RegistryKey is readonly
            using (var rk = Registry.CurrentUser.OpenSubKey(_testRegistryKeyName, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(valueName));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.DeleteValue(valueName);
            });
        }

        [Fact]
        public void DeleteValueTest()
        {
            // [] Vanilla case, deleting a value
            Assert.Equal(expected: 0, actual: _testRegistryKey.ValueCount);
            _testRegistryKey.SetValue(_testRegistryKeyName, 5);
            Assert.Equal(expected: 1, actual: _testRegistryKey.ValueCount);
            _testRegistryKey.DeleteValue(_testRegistryKeyName);
            Assert.Equal(expected: 0, actual: _testRegistryKey.ValueCount);
        }

        [Fact]
        public void DeleteValueTest2()
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
                _testRegistryKey.DeleteValue(testCase[0].ToString());
            }

            Assert.Equal(expected: 0, actual: _testRegistryKey.ValueCount);
        }
    }
}