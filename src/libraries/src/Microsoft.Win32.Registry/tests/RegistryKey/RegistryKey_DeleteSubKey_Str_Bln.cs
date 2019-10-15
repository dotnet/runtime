// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKey_Str_Bln : RegistryKeyDeleteSubKeyTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string name = "Test";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKey(null, true));
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKey(null, false));

            // Should throw because subkey doesn't exists
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.DeleteSubKey(name, throwOnMissingSubKey: true));

            // Should throw if subkey has child subkeys
            using (var rk = TestRegistryKey.CreateSubKey(name))
            {
                rk.CreateSubKey(name);
                Assert.Throws<InvalidOperationException>(() => TestRegistryKey.DeleteSubKey(name, false));
            }

            // Should throw because RegistryKey is readonly
            using (var rk = TestRegistryKey.OpenSubKey(string.Empty, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name, false));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.DeleteSubKey(name, false);
            });
        }

        [Fact]
        public void DeleteSubKeyTest()
        {
            Assert.Equal(expected: 0, actual: TestRegistryKey.SubKeyCount);
            Assert.NotNull(TestRegistryKey.CreateSubKey(TestRegistryKeyName));
            Assert.Equal(expected: 1, actual: TestRegistryKey.SubKeyCount);

            TestRegistryKey.DeleteSubKey(TestRegistryKeyName);
            Assert.Null(TestRegistryKey.OpenSubKey(TestRegistryKeyName));
            Assert.Equal(expected: 0, actual: TestRegistryKey.SubKeyCount);
        }

        [Fact]
        public void DeleteSubKeyTest2()
        {
            string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();
            foreach (var subKeyName in subKeyNames)
            {
                TestRegistryKey.CreateSubKey(subKeyName);
            }

            Assert.Equal(subKeyNames, TestRegistryKey.GetSubKeyNames());
            foreach (var subKeyName in subKeyNames)
            {
                TestRegistryKey.DeleteSubKey(subKeyName);
                Assert.Null(TestRegistryKey.OpenSubKey(subKeyName));
            }
        }

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKey_KeyExists_ThrowOnMissing_KeyDeleted(string expected, string subkeyName) =>
            Verify_DeleteSubKey_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteSubKey(subkeyName, throwOnMissingSubKey: true));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKey_KeyExists_DoNotThrow_KeyDeleted(string expected, string subkeyName) =>
            Verify_DeleteSubKey_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteSubKey(subkeyName, throwOnMissingSubKey: false));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKey_KeyDoesNotExists_ThrowOnMissing_Throws(string expected, string subkeyName) =>
            Verify_DeleteSubKey_KeyDoesNotExists_Throws(expected, () => TestRegistryKey.DeleteSubKey(subkeyName, throwOnMissingSubKey: true));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKey_KeyDoesNotExists_DoNotThrow_DoesNotThrow(string expected, string subkeyName) =>
            Verify_DeleteSubKey_KeyDoesNotExists_DoesNotThrow(expected, () => TestRegistryKey.DeleteSubKey(subkeyName, throwOnMissingSubKey: false));
    }
}
