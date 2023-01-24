// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str_rkpc : RegistryKeyOpenSubKeyTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.OpenSubKey(name: null, permissionCheck: RegistryKeyPermissionCheck.ReadSubTree));

            // Should throw if subkey name greater than 255 chars
            AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.OpenSubKey(new string('a', 256), RegistryKeyPermissionCheck.Default));

            // Should throw when opened with default permission check
            const string name = "FooBar";
            TestRegistryKey.SetValue(name, 42);
            TestRegistryKey.CreateSubKey(name).Dispose();
            using (var rk = Registry.CurrentUser.OpenSubKey(name: TestRegistryKeyName, permissionCheck: RegistryKeyPermissionCheck.Default))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.CreateSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.SetValue(name, "String"));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKeyTree(name));
            }

            // Should throw when opened with read permission check
            using (var rk = Registry.CurrentUser.OpenSubKey(name: TestRegistryKeyName, permissionCheck: RegistryKeyPermissionCheck.ReadSubTree))
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
                TestRegistryKey.OpenSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.Default);
            });
        }

        [Fact]
        public void OpenSubKeyTest()
        {
            // [] Vanilla; open a subkey in read/write mode and write to it
            const string valueName = "FooBar";
            const string expectedValue = "BLAH";

            using (var rk = TestRegistryKey.OpenSubKey("", RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                rk.SetValue(valueName, expectedValue);
                Assert.Equal(expectedValue, rk.GetValue(valueName));
                rk.CreateSubKey(valueName).Dispose();
                using RegistryKey subkey = rk.OpenSubKey(valueName);
                Assert.NotNull(subkey);
            }
        }

        private const RegistryKeyPermissionCheck DefaultPermissionCheck = RegistryKeyPermissionCheck.Default;
        private const RegistryKeyPermissionCheck Writable = RegistryKeyPermissionCheck.ReadWriteSubTree;
        private const RegistryKeyPermissionCheck NonWritable = RegistryKeyPermissionCheck.ReadSubTree;

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_DefaultPermissionCheck_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, DefaultPermissionCheck));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_Writable_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, Writable));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritable_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, NonWritable));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_DefaultPermissionCheck_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, DefaultPermissionCheck));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_Writable_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, Writable));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritable_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, NonWritable));
    }
}
