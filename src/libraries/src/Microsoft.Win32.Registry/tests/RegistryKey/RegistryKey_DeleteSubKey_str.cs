// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKey_str : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string name = "Test";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.DeleteSubKey(null));

            // Should throw because subkey doesn't exists
            Assert.Throws<ArgumentException>(() => TestRegistryKey.DeleteSubKey(name));

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
    }
}
