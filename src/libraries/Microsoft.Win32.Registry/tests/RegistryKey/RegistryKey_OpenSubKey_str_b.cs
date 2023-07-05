// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str_b : RegistryKeyOpenSubKeyTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.OpenSubKey(name: null, writable: false));

            // Should throw if subkey name greater than 255 chars
            AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.OpenSubKey(new string('a', 256), true));

            // OpenSubKey should be read only
            const string name = "FooBar";
            TestRegistryKey.SetValue(name, 42);
            TestRegistryKey.CreateSubKey(name).Dispose();
            using (var rk = Registry.CurrentUser.OpenSubKey(name: TestRegistryKeyName, writable: false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.CreateSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.SetValue(name, "String"));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKeyTree(name));
            }

            // Should throw if RegistryKey closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.OpenSubKey(TestRegistryKeyName, true);
            });
        }

        [Fact]
        public void OpenSubKeyTest()
        {
            // [] Should have write rights when true is passed
            const int testValue = 32;
            using (var rk = TestRegistryKey.OpenSubKey("", true))
            {
                rk.CreateSubKey(TestRegistryKeyName).Dispose();
                rk.SetValue(TestRegistryKeyName, testValue);

                using RegistryKey subkey = rk.OpenSubKey(TestRegistryKeyName);
                Assert.NotNull(subkey);
                Assert.Equal(testValue, (int)rk.GetValue(TestRegistryKeyName));
            }
        }

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_Writable_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, writable: true));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritable_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, writable: false));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_Writable_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, writable: true));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritable_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, writable: false));
    }
}
