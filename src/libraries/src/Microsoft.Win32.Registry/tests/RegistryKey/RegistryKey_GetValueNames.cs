// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValueNames : RegistryTestsBase
    {
        [Fact]
        public void ShoudThrowIfDisposed()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.GetValueNames();
            });
        }

        [Fact]
        public void ShouldThrowIfRegistryKeyDeleted()
        {
            Registry.CurrentUser.DeleteSubKeyTree(_testRegistryKeyName);
            Assert.Throws<IOException>(() => _testRegistryKey.GetValueNames());
        }

        [Fact]
        public void TestGetValueNames()
        {
            // [] Add several values and get the values then check the names
            Assert.Equal(expected: 0, actual: _testRegistryKey.GetValueNames().Length);

            string[] expected = { _testRegistryKeyName };
            foreach (string valueName in expected)
            {
                _testRegistryKey.SetValue(valueName, 5);
            }

            Assert.Equal(expected, _testRegistryKey.GetValueNames());
            
            _testRegistryKey.DeleteValue(_testRegistryKeyName);
            Assert.Equal(expected: 0, actual: _testRegistryKey.GetValueNames().Length);
        }

        [Fact]
        public void TestGetValueNames2()
        {
            foreach (var testCase in TestData.TestValueTypes)
            {
                _testRegistryKey.SetValue(testCase[0].ToString(), testCase[1]);
            }

            string[] expected = TestData.TestValueTypes.Select(x => x[0].ToString()).ToArray();
            Assert.Equal(expected, _testRegistryKey.GetValueNames());
        }

        [Fact]
        public static void TestGetValueNamesForPerformanceData()
        {
            var rk = Registry.PerformanceData;
            int iNumValue = rk.GetValueNames().Length;
        }
    }
}
