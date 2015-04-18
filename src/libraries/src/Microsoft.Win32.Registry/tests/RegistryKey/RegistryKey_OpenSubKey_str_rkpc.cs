// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.AccessControl;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str_rkpc : TestSubKey
    {
        private const string TestKey = "REG_TEST_12";

        public RegistryKey_OpenSubKey_str_rkpc()
            : base(TestKey)
        {
        }

        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.OpenSubKey(name: null, rights: RegistryRights.ReadKey));

            // Should throw if subkey name greater than 255 chars
            Assert.Throws<ArgumentException>(() => _testRegistryKey.OpenSubKey(new string('a', 256), RegistryRights.FullControl));

            // OpenSubKey should be read only
            const string name = "FooBar";
            _testRegistryKey.SetValue(name, 42);
            _testRegistryKey.CreateSubKey(name);
            using (var rk = Registry.CurrentUser.OpenSubKey(name: TestKey, rights: RegistryRights.ReadKey))
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
                _testRegistryKey.OpenSubKey(TestKey, RegistryRights.Delete);
            });
        }

        [Fact]
        public void OpenSubKeyTest()
        {
            // [] Vanilla; open a subkey in read/write mode and write to it
            const string valueName = "FooBar";
            const string expectedValue = "BLAH";
            using (var rk = _testRegistryKey.OpenSubKey("", RegistryRights.SetValue | RegistryRights.QueryValues))
            {
                rk.SetValue(valueName, expectedValue);
                Assert.Equal(expectedValue, rk.GetValue(valueName));
            }

            using (var rk = _testRegistryKey.OpenSubKey("", RegistryRights.CreateSubKey))
            {
                rk.CreateSubKey(valueName);
                Assert.NotNull(rk.OpenSubKey(valueName));
            }
        }
    }
}
