// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_DeleteSubKeyTree_str : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            const string name = "Test";

            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.DeleteSubKeyTree(null));

            // Should throw if target subkey is system subkey and name is empty
            Assert.Throws<ArgumentException>(() => Registry.CurrentUser.DeleteSubKeyTree(string.Empty));

            // Should throw because subkey doesn't exists
            Assert.Throws<ArgumentException>(() => _testRegistryKey.DeleteSubKeyTree(name));

            // Should throw because RegistryKey is readonly
            using (var rk = _testRegistryKey.OpenSubKey(string.Empty, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKeyTree(name));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.DeleteSubKeyTree(name);
            });
        }

        [Fact]
        public void SelfDeleteTest()
        {
            using (var rk = _testRegistryKey.CreateSubKey(_testRegistryKeyName))
            {
                rk.CreateSubKey(_testRegistryKeyName);
                rk.DeleteSubKeyTree("");
            }

            Assert.Null(_testRegistryKey.OpenSubKey(_testRegistryKeyName));
        }

        [Fact]
        public void DeleteSubKeyTreeTest()
        {
            // Creating new SubKey and deleting it
            _testRegistryKey.CreateSubKey(_testRegistryKeyName);
            Assert.NotNull(_testRegistryKey.OpenSubKey(_testRegistryKeyName));

            _testRegistryKey.DeleteSubKeyTree(_testRegistryKeyName);
            Assert.Null(_testRegistryKey.OpenSubKey(_testRegistryKeyName));
        }

        [Fact]
        public void DeleteSubKeyTreeTest2()
        {
            // [] Add in multiple subkeys and then delete the root key
            string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();

            using (var rk = _testRegistryKey.CreateSubKey(_testRegistryKeyName))
            {
                foreach (var subKeyName in subKeyNames)
                {
                    var rk2 = rk.CreateSubKey(subKeyName);
                    Assert.NotNull(rk2);
                    Assert.NotNull(rk2.CreateSubKey("Test"));
                }

                Assert.Equal(subKeyNames, rk.GetSubKeyNames());
            }

            _testRegistryKey.DeleteSubKeyTree(_testRegistryKeyName);
            Assert.Null(_testRegistryKey.OpenSubKey(_testRegistryKeyName));
        }
    }
}
