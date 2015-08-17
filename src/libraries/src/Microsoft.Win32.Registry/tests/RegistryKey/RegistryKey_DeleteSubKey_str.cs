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
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.DeleteSubKey(null));

            // Should throw because subkey doesn't exists
            Assert.Throws<ArgumentException>(() => _testRegistryKey.DeleteSubKey(name));

            // Should throw if subkey has child subkeys
            using (var rk = _testRegistryKey.CreateSubKey(name))
            {
                rk.CreateSubKey(name);
                Assert.Throws<InvalidOperationException>(() => _testRegistryKey.DeleteSubKey(name));
            }

            // Should throw because RegistryKey is readonly
            using (var rk = _testRegistryKey.OpenSubKey(string.Empty, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.DeleteSubKey(name);
            });
        }

        [Fact]
        public void DeleteSubKeyTest()
        {
            Assert.Equal(expected: 0, actual: _testRegistryKey.SubKeyCount);
            Assert.NotNull(_testRegistryKey.CreateSubKey(_testRegistryKeyName));
            Assert.Equal(expected: 1, actual: _testRegistryKey.SubKeyCount);

            _testRegistryKey.DeleteSubKey(_testRegistryKeyName);
            Assert.Null(_testRegistryKey.OpenSubKey(_testRegistryKeyName));
            Assert.Equal(expected: 0, actual: _testRegistryKey.SubKeyCount);
        }
    }
}
