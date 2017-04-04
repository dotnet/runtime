// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                TestRegistryKey.Dispose();
                TestRegistryKey.GetValueNames();
            });
        }

        [Fact]
        public void ShouldThrowIfRegistryKeyDeleted()
        {
            Registry.CurrentUser.DeleteSubKeyTree(TestRegistryKeyName);
            Assert.Throws<IOException>(() => TestRegistryKey.GetValueNames());
        }

        [Fact]
        public void TestGetValueNames()
        {
            // [] Add several values and get the values then check the names
            Assert.Equal(expected: 0, actual: TestRegistryKey.GetValueNames().Length);

            string[] expected = { TestRegistryKeyName };
            foreach (string valueName in expected)
            {
                TestRegistryKey.SetValue(valueName, 5);
            }

            Assert.Equal(expected, TestRegistryKey.GetValueNames());
            
            TestRegistryKey.DeleteValue(TestRegistryKeyName);
            Assert.Equal(expected: 0, actual: TestRegistryKey.GetValueNames().Length);
        }

        [Fact]
        public void TestGetValueNames2()
        {
            foreach (var testCase in TestData.TestValueTypes)
            {
                TestRegistryKey.SetValue(testCase[0].ToString(), testCase[1]);
            }

            string[] expected = TestData.TestValueTypes.Select(x => x[0].ToString()).ToArray();
            Assert.Equal(expected, TestRegistryKey.GetValueNames());
        }

        [Fact]
        public static void TestGetValueNamesForPerformanceData()
        {
            var rk = Registry.PerformanceData;
            string[] names = rk.GetValueNames();
            Assert.Equal(new string[] { "Global", "Costly" }, names);
        }
    }
}
