// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.AccessControl;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str_rr : RegistryKeyOpenSubKeyTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.OpenSubKey(name: null, rights: RegistryRights.ReadKey));

            // Should throw if subkey name greater than 255 chars
            AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.OpenSubKey(new string('a', 256), RegistryRights.FullControl));

            // OpenSubKey should be read only
            const string name = "FooBar";
            TestRegistryKey.SetValue(name, 42);
            TestRegistryKey.CreateSubKey(name);
            using (var rk = Registry.CurrentUser.OpenSubKey(name: TestRegistryKeyName, rights: RegistryRights.ReadKey))
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
                TestRegistryKey.OpenSubKey(TestRegistryKeyName, RegistryRights.Delete);
            });
        }

        [Fact]
        public void OpenSubKeyTest()
        {
            // [] Vanilla; open a subkey in read/write mode and write to it
            const string valueName = "FooBar";
            const string expectedValue = "BLAH";
            using (var rk = TestRegistryKey.OpenSubKey("", RegistryRights.SetValue | RegistryRights.QueryValues))
            {
                rk.SetValue(valueName, expectedValue);
                Assert.Equal(expectedValue, rk.GetValue(valueName));
            }

            using (var rk = TestRegistryKey.OpenSubKey("", RegistryRights.CreateSubKey))
            {
                rk.CreateSubKey(valueName);
                Assert.NotNull(rk.OpenSubKey(valueName));
            }
        }

        private const RegistryRights Writable = RegistryRights.ReadKey | RegistryRights.WriteKey;
        private const RegistryRights NonWritable = RegistryRights.ReadKey;

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
        public void OpenSubKey_Writable_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, Writable));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_NonWritable_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName, NonWritable));
    }
}
