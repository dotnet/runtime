// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_OpenSubKey_str : RegistryKeyOpenSubKeyTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.OpenSubKey(name: null));

            // Should throw if subkey name greater than 255 chars
            AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.OpenSubKey(new string('a', 256)));

            // OpenSubKey should be read only by default
            const string name = "FooBar";
            TestRegistryKey.SetValue(name, 42);
            using (var rk = Registry.CurrentUser.OpenSubKey(TestRegistryKeyName))
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
                TestRegistryKey.Dispose();
                TestRegistryKey.OpenSubKey(TestRegistryKeyName);
            });
        }

        [Fact]
        public void OpenSubKeyTest()
        {
            using RegistryKey created = TestRegistryKey.CreateSubKey(TestRegistryKeyName);
            using RegistryKey subkey = TestRegistryKey.OpenSubKey(TestRegistryKeyName);
            Assert.NotNull(subkey);
            Assert.Equal(expected: 1, actual: TestRegistryKey.SubKeyCount);

            TestRegistryKey.DeleteSubKey(TestRegistryKeyName);
            using RegistryKey subkey2 = TestRegistryKey.OpenSubKey(TestRegistryKeyName);
            Assert.Null(subkey2);
            Assert.Equal(expected: 0, actual: TestRegistryKey.SubKeyCount);
        }

        [Fact]
        public void OpenSubKeyTest2()
        {
            string[] subKeyNames = Enumerable.Range(1, 9).Select(x => "BLAH_" + x.ToString()).ToArray();
            foreach (var subKeyName in subKeyNames)
            {
                TestRegistryKey.CreateSubKey(subKeyName).Dispose();
            }

            Assert.Equal(subKeyNames.Length, TestRegistryKey.SubKeyCount);
            Assert.Equal(subKeyNames, TestRegistryKey.GetSubKeyNames());
        }

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_KeyExists_OpensWithFixedUpName(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyExists_OpensWithFixedUpName(expected, () => TestRegistryKey.OpenSubKey(subKeyName));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void OpenSubKey_KeyDoesNotExist_ReturnsNull(string expected, string subKeyName) =>
            Verify_OpenSubKey_KeyDoesNotExist_ReturnsNull(expected, () => TestRegistryKey.OpenSubKey(subKeyName));
    }
}
