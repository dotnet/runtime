// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKey_str : RegistryKeyDeleteSubKeyTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string name = "Test";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKey(null));

            // Should throw because subkey doesn't exists
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.DeleteSubKey(name));

            // Should throw if subkey has child subkeys
            using (var rk = TestRegistryKey.CreateSubKey(name))
            {
                rk.CreateSubKey(name);
                Assert.Throws<InvalidOperationException>(() => TestRegistryKey.DeleteSubKey(name));
            }

            // Should throw because RegistryKey is readonly
            using (var rk = TestRegistryKey.OpenSubKey(string.Empty, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.DeleteSubKey(name);
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

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKey_KeyExists_KeyDeleted(string expected, string subkeyName) =>
            Verify_DeleteSubKey_KeyExists_KeyDeleted(expected, () => TestRegistryKey.DeleteSubKey(subkeyName));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void DeleteSubKey_KeyDoesNotExists_Throws(string expected, string subkeyName) =>
            Verify_DeleteSubKey_KeyDoesNotExists_Throws(expected, () => TestRegistryKey.DeleteSubKey(subkeyName));
    }
}
