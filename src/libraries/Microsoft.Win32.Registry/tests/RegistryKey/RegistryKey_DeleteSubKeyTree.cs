// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKeyTree : RegistryKeyDeleteSubKeyTreeTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string name = "Test";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKeyTree(null, throwOnMissingSubKey: true));
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKeyTree(null, throwOnMissingSubKey: false));

            // Should throw if target subkey is system subkey and name is empty
            AssertExtensions.Throws<ArgumentException>(null, () => Registry.CurrentUser.DeleteSubKeyTree(string.Empty, throwOnMissingSubKey: false));

            // Should throw because subkey doesn't exists
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.DeleteSubKeyTree(name, throwOnMissingSubKey: true));

            // Should throw because RegistryKey is readonly
            using (var rk = TestRegistryKey.OpenSubKey(string.Empty, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKeyTree(name, throwOnMissingSubKey: false));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.DeleteSubKeyTree(name, throwOnMissingSubKey: true);
            });
        }

        [Fact]
        public void SubkeyMissingTest()
        {
            //Should NOT throw when throwOnMissing is false with subkey missing
            const string name = "Test";
            TestRegistryKey.DeleteSubKeyTree(name, throwOnMissingSubKey: false);
        }

        [Fact]
        public void SubkeyExistsTests()
        {
            const string subKeyExists = "SubkeyExists";
            const string subKeyExists2 = "SubkeyExists2";

            //throwOnMissing is true with subkey present
            using (var rk = TestRegistryKey.CreateSubKey(subKeyExists))
            {
                using RegistryKey a = rk.CreateSubKey("a");
                using RegistryKey b = rk.CreateSubKey("b");
                TestRegistryKey.DeleteSubKeyTree(subKeyExists, false);
            }
            //throwOnMissing is false with subkey present
            using (var rk = TestRegistryKey.CreateSubKey(subKeyExists2))
            {
                using RegistryKey a = rk.CreateSubKey("a");
                using RegistryKey b = rk.CreateSubKey("b");
                TestRegistryKey.DeleteSubKeyTree(subKeyExists2, true);
            }
        }

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKeyTree_ThrowOnMissing_KeyExists_KeyDeleted(string expected, string subKeyName) =>
            Verify_DeleteSubKeyTree_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: true));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKeyTree_DoNotThrow_KeyExists_KeyDeleted(string expected, string subKeyName) =>
            Verify_DeleteSubKeyTree_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKeyTree_ThrowOnMissing_KeyDoesNotExists_Throws(string expected, string subKeyName) =>
            Verify_DeleteSubKeyTree_KeyDoesNotExists_Throws(expected, () => TestRegistryKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: true));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKeyTree_DoNotThrow_KeyDoesNotExists_DoesNotThrow(string expected, string subKeyName) =>
            Verify_DeleteSubKeyTree_KeyDoesNotExists_DoesNotThrow(expected, () => TestRegistryKey.DeleteSubKeyTree(subKeyName, throwOnMissingSubKey: false));
    }
}
