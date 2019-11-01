// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteValue_Str_Bln : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string valueName = "TestValue";

            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.DeleteValue(null, true));

            // Should NOT throw because value doesn't exists
            TestRegistryKey.DeleteValue(valueName, throwOnMissingValue: false);

            // Should throw because value doesn't exists
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.DeleteValue(valueName, throwOnMissingValue: true));

            TestRegistryKey.SetValue(valueName, 42);

            // Should throw because RegistryKey is readonly
            using (var rk = Registry.CurrentUser.OpenSubKey(TestRegistryKeyName, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(valueName, true));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.DeleteValue(valueName, true);
            });
        }

        [Fact]
        public void DeleteValueTest()
        {
            // [] Vanilla case, deleting a value
            Assert.Equal(expected: 0, actual: TestRegistryKey.ValueCount);
            TestRegistryKey.SetValue(TestRegistryKeyName, 5);
            Assert.Equal(expected: 1, actual: TestRegistryKey.ValueCount);
            TestRegistryKey.DeleteValue(TestRegistryKeyName, throwOnMissingValue: true);
            Assert.Equal(expected: 0, actual: TestRegistryKey.ValueCount);
        }

        [Fact]
        public void Test04()
        {
            // [] Vanilla case , add a  bunch different objects and then Delete them
            object[][] testCases = TestData.TestValueTypes.ToArray();
            foreach (var testCase in testCases)
            {
                TestRegistryKey.SetValue(testCase[0].ToString(), testCase[1]);
            }

            Assert.Equal(expected: testCases.Length, actual: TestRegistryKey.ValueCount);

            foreach (var testCase in testCases)
            {
                TestRegistryKey.DeleteValue(testCase[0].ToString(), throwOnMissingValue: true);
            }

            Assert.Equal(expected: 0, actual: TestRegistryKey.ValueCount);
        }
    }
}
