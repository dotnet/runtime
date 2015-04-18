// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_CreateSubKey_str : TestSubKey
    {
        private const string TestKey = "REG_TEST_1";

        public RegistryKey_CreateSubKey_str()
            : base(TestKey)
        {
        }

        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.CreateSubKey(null));

            // Should throw if key length above 255 characters
            const int maxValueNameLength = 255;
            Assert.Throws<ArgumentException>(() => _testRegistryKey.CreateSubKey(new string('a', maxValueNameLength + 1)));

            //According to msdn documetation max nesting level exceeds is 510 but actual is 508
            const int maxNestedLevel = 508;
            string exceedsNestedSubkeyName = string.Join(@"\", Enumerable.Repeat("a", maxNestedLevel));
            Assert.Throws<IOException>(() => _testRegistryKey.CreateSubKey(exceedsNestedSubkeyName));

            // Should throw if RegistryKey is readonly
            const string name = "FooBar";
            _testRegistryKey.SetValue(name, 42);
            using (var rk = Registry.CurrentUser.CreateSubKey(TestKey, writable: false))
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
                _testRegistryKey.CreateSubKey(TestKey);
            });
        }

        [Fact]
        public void CreateSubkeyWithEmptyName()
        {
            string expectedName = _testRegistryKey.Name + @"\";
            var rk = _testRegistryKey.CreateSubKey(string.Empty);
            Assert.NotNull(rk);
            Assert.Equal(expectedName, rk.Name);
        }

        [Fact]
        public void CreateSubKeyAndCheckThatItExists()
        {
            _testRegistryKey.CreateSubKey(TestKey);
            Assert.NotNull(_testRegistryKey.OpenSubKey(TestKey));
            Assert.Equal(expected: 1, actual:_testRegistryKey.SubKeyCount);
        }

        [Fact]
        public void CreateSubKeyShouldOpenExisting()
        {
            // CreateSubKey should open subkey if it already exists
            Assert.NotNull(_testRegistryKey.CreateSubKey(TestKey));
            Assert.NotNull(_testRegistryKey.OpenSubKey(TestKey));
            Assert.NotNull(_testRegistryKey.CreateSubKey(TestKey));
        }

        [Theory]
        [InlineData("Dyalog APL/W 10.0")]
        [InlineData(@"a\b\c\d\e\f\g\h")]
        [InlineData(@"a\b\c\/d\//e\f\g\h\//\\")]
        public void CreateSubKeyWithName(string subkeyName)
        {
            _testRegistryKey.CreateSubKey(subkeyName);
            Assert.NotNull(_testRegistryKey.OpenSubKey(subkeyName));
        }

        [Fact]
        public void DeepTest()
        {
            //[] how deep can we go with this

            string subkeyName = string.Empty;

            // Changed the number of times we repeat str1 from 100 to 30 in response to the Windows OS
            //There is a restriction of 255 characters for the keyname even if it is multikeys. Not worth to pursue as a bug
            // reduced further to allow for WoW64 changes to the string.
            for (int i = 0; i < 25 && subkeyName.Length < 230; i++)
                subkeyName = subkeyName + i.ToString() + @"\";
            
            _testRegistryKey.CreateSubKey(subkeyName);
            Assert.NotNull(_testRegistryKey.OpenSubKey(subkeyName));

            //However, we are interested in ensuring that there are no buffer overflow issues with a deeply nested keys
            RegistryKey rk = _testRegistryKey;
            for (int i = 0; i < 3; i++)
            {
                rk = rk.OpenSubKey(subkeyName, true);
                Assert.NotNull(rk);
                rk.CreateSubKey(subkeyName);
            }
        }
    }
}
