// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.AccessControl;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str_rkpc_rr : RegistryKeyOpenSubKeyTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.OpenSubKey(name: null, permissionCheck: RegistryKeyPermissionCheck.ReadSubTree, rights: RegistryRights.ReadKey));

            // Should throw if subkey name greater than 255 chars
            AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.OpenSubKey(new string('a', 256), RegistryKeyPermissionCheck.Default, rights: RegistryRights.FullControl));

            // Should throw when opened with default permission check and write rights
            const string name = "FooBar";
            TestRegistryKey.SetValue(name, 42);
            TestRegistryKey.CreateSubKey(name).Dispose();
            using (var rk = Registry.CurrentUser.OpenSubKey(name: TestRegistryKeyName, permissionCheck: RegistryKeyPermissionCheck.Default, rights: RegistryRights.WriteKey))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.CreateSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.SetValue(name, "String"));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKeyTree(name));
            }

            // Should throw when opened with read permission check and read rights
            using (var rk = Registry.CurrentUser.OpenSubKey(name: TestRegistryKeyName, permissionCheck: RegistryKeyPermissionCheck.ReadSubTree, rights: RegistryRights.ReadKey))
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
                TestRegistryKey.OpenSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.Default, rights: RegistryRights.FullControl);
            });
        }

        [Fact]
        public void PermissionCheckShouldTakePrecedenceOverRegistryRightsTest()
        {
            const string name = "FooBar";
            // Should throw when opened with write permission check and read rights
            using (var rk = Registry.CurrentUser.OpenSubKey(name: TestRegistryKeyName, permissionCheck: RegistryKeyPermissionCheck.ReadSubTree, rights: RegistryRights.WriteKey))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.CreateSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.SetValue(name, "String"));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKeyTree(name));
            }
        }

        [Fact]
        public void OpenSubKeyTest()
        {
            // [] Vanilla; open a subkey in read/write mode and write to it
            const string valueName = "FooBar";
            const string expectedValue = "BLAH";

            using (var rk = TestRegistryKey.OpenSubKey("", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.SetValue | RegistryRights.QueryValues))
            {
                rk.SetValue(valueName, expectedValue);
                Assert.Equal(expectedValue, rk.GetValue(valueName));
            }

            using (var rk = TestRegistryKey.OpenSubKey("", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.CreateSubKey))
            {
                rk.CreateSubKey(valueName).Dispose();
                using RegistryKey subkey = rk.OpenSubKey(valueName);
                Assert.NotNull(subkey);
            }
        }

        private const RegistryKeyPermissionCheck DefaultPermissionCheck = RegistryKeyPermissionCheck.Default;
        private const RegistryKeyPermissionCheck WritablePermissionCheck = RegistryKeyPermissionCheck.ReadWriteSubTree;
        private const RegistryKeyPermissionCheck NonWritablePermissionCheck = RegistryKeyPermissionCheck.ReadSubTree;
        private const RegistryRights WritableRegistryRights = RegistryRights.WriteKey;
        private const RegistryRights NonWritableRegistryRights = RegistryRights.ReadKey;

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_DefaultPermissionCheck_WritableRegistryRights_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, DefaultPermissionCheck, WritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_DefaultPermissionCheck_NonWritableRegistryRights_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, DefaultPermissionCheck, NonWritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_WritablePermissionCheck_WritableRegistryRights_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, WritablePermissionCheck, WritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_WritablePermissionCheck_NonWritableRegistryRights_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, WritablePermissionCheck, NonWritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritablePermissionCheck_WritableRegistryRights_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, NonWritablePermissionCheck, WritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritablePermissionCheck_NonWritableRegistryRights_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName, NonWritablePermissionCheck, NonWritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_DefaultPermissionCheck_WritableRegistryRights_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, DefaultPermissionCheck, WritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_DefaultPermissionCheck_NonWritableRegistryRights_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, DefaultPermissionCheck, NonWritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_WritablePermissionCheck_WritableRegistryRights_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, WritablePermissionCheck, WritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_WritablePermissionCheck_NonWritableRegistryRights_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, WritablePermissionCheck, NonWritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritablePermissionCheck_WritableRegistryRights_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, NonWritablePermissionCheck, WritableRegistryRights));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritablePermissionCheck_NonWritableRegistryRights_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, NonWritablePermissionCheck, NonWritableRegistryRights));
    }
}
