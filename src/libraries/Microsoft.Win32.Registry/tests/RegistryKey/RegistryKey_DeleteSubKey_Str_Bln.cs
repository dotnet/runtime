// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKey_Str_Bln : TestSubKey
    {
        private const string TestKey = "REG_TEST_4";

        public RegistryKey_DeleteSubKey_Str_Bln()
            : base(TestKey)
        {
        }

        [Fact]
        public void NegativeTests()
        {
            const string name = "Test";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.DeleteSubKey(null, true));
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.DeleteSubKey(null, false));

            // Should throw because subkey doesn't exists
            Assert.Throws<ArgumentException>(() => _testRegistryKey.DeleteSubKey(name, throwOnMissingSubKey: true));

            // Should throw if subkey has child subkeys
            using (var rk = _testRegistryKey.CreateSubKey(name))
            {
                rk.CreateSubKey(name);
                Assert.Throws<InvalidOperationException>(() => _testRegistryKey.DeleteSubKey(name, false));
            }

            // Should throw because RegistryKey is readonly
            using (var rk = _testRegistryKey.OpenSubKey(string.Empty, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name, false));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.DeleteSubKey(name, false);
            });
        }

        [Fact]
        public void DeleteSubKeyTest()
        {
            Assert.Equal(expected: 0, actual: _testRegistryKey.SubKeyCount);
            Assert.NotNull(_testRegistryKey.CreateSubKey(TestKey));
            Assert.Equal(expected: 1, actual: _testRegistryKey.SubKeyCount);

            _testRegistryKey.DeleteSubKey(TestKey);
            Assert.Null(_testRegistryKey.OpenSubKey(TestKey));
            Assert.Equal(expected: 0, actual: _testRegistryKey.SubKeyCount);
        }

        [Fact]
        public void DeleteSubKeyTest2()
        {
            string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();
            foreach (var subKeyName in subKeyNames)
            {
                _testRegistryKey.CreateSubKey(subKeyName);
            }

            Assert.Equal(subKeyNames, _testRegistryKey.GetSubKeyNames());
            foreach (var subKeyName in subKeyNames)
            {
                _testRegistryKey.DeleteSubKey(subKeyName);
                Assert.Null(_testRegistryKey.OpenSubKey(subKeyName));
            }
        }
    }
}
