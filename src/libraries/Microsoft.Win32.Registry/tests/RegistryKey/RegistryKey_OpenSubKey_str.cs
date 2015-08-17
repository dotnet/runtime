// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.OpenSubKey(name: null));

            // Should throw if subkey name greater than 255 chars
            Assert.Throws<ArgumentException>(() => _testRegistryKey.OpenSubKey(new string('a', 256)));

            // OpenSubKey should be read only by default
            const string name = "FooBar";
            _testRegistryKey.SetValue(name, 42);
            using (var rk = Registry.CurrentUser.OpenSubKey(_testRegistryKeyName))
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
                _testRegistryKey.Dispose();
                _testRegistryKey.OpenSubKey(_testRegistryKeyName);
            });
        }

        [Fact]
        public void OpenSubKeyTest()
        {
            _testRegistryKey.CreateSubKey(_testRegistryKeyName);
            Assert.NotNull(_testRegistryKey.OpenSubKey(_testRegistryKeyName));
            Assert.Equal(expected: 1, actual: _testRegistryKey.SubKeyCount);

            _testRegistryKey.DeleteSubKey(_testRegistryKeyName);
            Assert.Null(_testRegistryKey.OpenSubKey(_testRegistryKeyName));
            Assert.Equal(expected: 0, actual: _testRegistryKey.SubKeyCount);
        }

        [Fact]
        public void OpenSubKeyTest2()
        {
            string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();
            foreach (var subKeyName in subKeyNames)
            {
                _testRegistryKey.CreateSubKey(subKeyName);
            }
            
            Assert.Equal(subKeyNames.Length, _testRegistryKey.SubKeyCount);
            Assert.Equal(subKeyNames, _testRegistryKey.GetSubKeyNames());
        }
    }
}
