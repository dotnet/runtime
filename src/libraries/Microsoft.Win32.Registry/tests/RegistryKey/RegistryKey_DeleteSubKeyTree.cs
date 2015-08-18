// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKeyTree : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string name = "Test";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKeyTree(null, throwOnMissingSubKey: true));
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKeyTree(null, throwOnMissingSubKey: false));

            // Should throw if target subkey is system subkey and name is empty
            Assert.Throws<ArgumentException>(() => Registry.CurrentUser.DeleteSubKeyTree(string.Empty, throwOnMissingSubKey: false));

            // Should throw because subkey doesn't exists
            Assert.Throws<ArgumentException>(() => TestRegistryKey.DeleteSubKeyTree(name, throwOnMissingSubKey: true));

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
                rk.CreateSubKey("a");
                rk.CreateSubKey("b");
                TestRegistryKey.DeleteSubKeyTree(subKeyExists, false);
            }
            //throwOnMissing is false with subkey present
            using (var rk = TestRegistryKey.CreateSubKey(subKeyExists2))
            {
                rk.CreateSubKey("a");
                rk.CreateSubKey("b");
                TestRegistryKey.DeleteSubKeyTree(subKeyExists2, true);
            }
        }
    }
}
