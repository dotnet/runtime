// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_CreateSubKey_str_rkpc : RegistryKeyCreateSubKeyTestsBase
    {
        [Fact]
        public void CreateWriteableSubkeyAndWrite()
        {
            // [] Vanilla; create a new subkey in read/write mode and write to it
            const string testValueName = "TestValue";
            const string testStringValueName = "TestString";
            const string testStringValue = "Hello World!\u2020\u00FE";
            const int testValue = 42;

            using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName, writable: true))
            {
                Assert.NotNull(rk);

                rk.SetValue(testValueName, testValue);
                Assert.Equal(1, rk.ValueCount);

                rk.SetValue(testStringValueName, testStringValue);
                Assert.Equal(2, rk.ValueCount);

                Assert.Equal(testValue, rk.GetValue(testValueName));
                Assert.Equal(testStringValue, rk.GetValue(testStringValueName).ToString());
            }
        }

        [Fact]
        public void CreateReadWritePermissionCheckSubKeyAndWrite()
        {
            // [] Vanilla; create a new subkey in read/write mode and write to it
            const string testValueName = "TestValue";
            const string testStringValueName = "TestString";
            const string testStringValue = "Hello World!\u2020\u00FE";
            const int testValue = 42;

            using (var rk = TestRegistryKey.CreateSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                Assert.NotNull(rk);

                rk.SetValue(testValueName, testValue);
                Assert.Equal(1, rk.ValueCount);

                rk.SetValue(testStringValueName, testStringValue);
                Assert.Equal(2, rk.ValueCount);

                Assert.Equal(testValue, rk.GetValue(testValueName));
                Assert.Equal(testStringValue, rk.GetValue(testStringValueName).ToString());
            }
        }

        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.CreateSubKey(null, true));
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.CreateSubKey(null, RegistryKeyPermissionCheck.ReadWriteSubTree));
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.CreateSubKey(null, RegistryKeyPermissionCheck.ReadWriteSubTree, new RegistrySecurity()));
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.CreateSubKey(null, RegistryKeyPermissionCheck.ReadWriteSubTree, new RegistryOptions()));
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.CreateSubKey(null, RegistryKeyPermissionCheck.ReadWriteSubTree, new RegistryOptions(), new RegistrySecurity()));

            // Should throw if passed option is invalid
            AssertExtensions.Throws<ArgumentException>("options", () => TestRegistryKey.CreateSubKey(TestRegistryKeyName, true, options: (RegistryOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => TestRegistryKey.CreateSubKey(TestRegistryKeyName, true, options: (RegistryOptions)3));
            AssertExtensions.Throws<ArgumentException>("options", () => TestRegistryKey.CreateSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, (RegistryOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => TestRegistryKey.CreateSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, (RegistryOptions)3));
            AssertExtensions.Throws<ArgumentException>("options", () => TestRegistryKey.CreateSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, (RegistryOptions)(-1), new RegistrySecurity()));
            AssertExtensions.Throws<ArgumentException>("options", () => TestRegistryKey.CreateSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree, (RegistryOptions)3, new RegistrySecurity()));

            // Should throw if key length above 255 characters
            const int maxValueNameLength = 255;
            AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.CreateSubKey(new string('a', maxValueNameLength + 1)));

            // Should throw if RegistryKey is readonly
            const string name = "FooBar";
            TestRegistryKey.SetValue(name, 42);
            using (var rk = Registry.CurrentUser.CreateSubKey(TestRegistryKeyName, writable: false, options: RegistryOptions.None))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.CreateSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.SetValue(name, "String"));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteValue(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKey(name));
                Assert.Throws<UnauthorizedAccessException>(() => rk.DeleteSubKeyTree(name));
            }

            using (var rk = Registry.CurrentUser.CreateSubKey(TestRegistryKeyName, RegistryKeyPermissionCheck.ReadSubTree, RegistryOptions.None))
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
                TestRegistryKey.CreateSubKey(TestRegistryKeyName, true);
            });
        }

        [Fact]
        public void CreateWritableSubkeyWithEmptyName()
        {
            // [] Let the name of the created subkey be empty
            string expectedName = TestRegistryKey.Name + @"\";
            using (var rk = TestRegistryKey.CreateSubKey(string.Empty, true))
            {
                Assert.NotNull(rk);
                Assert.Equal(expectedName, rk.Name);
            }
        }

        [Fact]
        public void CreateReadWritePermissionCheckSubkeyWithEmptyName()
        {
            // [] Let the name of the created subkey be empty
            string expectedName = TestRegistryKey.Name + @"\";
            using (var rk = TestRegistryKey.CreateSubKey(string.Empty, RegistryKeyPermissionCheck.ReadWriteSubTree))
            {
                Assert.NotNull(rk);
                Assert.Equal(expectedName, rk.Name);
            }
        }

        [Theory]
        [InlineData(false, RegistryOptions.Volatile)]
        [InlineData(true, RegistryOptions.Volatile)]
        [InlineData(false, RegistryOptions.None)]
        [InlineData(true, RegistryOptions.None)]
        public void RegistryOptionsTestsValid(bool alreadyExists, RegistryOptions options)
        {
            string subkey = "TEST_" + options.ToString();

            if (alreadyExists)
            {
                TestRegistryKey.CreateSubKey(subkey, true, options).Dispose();
            }

            using RegistryKey created = TestRegistryKey.CreateSubKey(subkey, true, options);
            Assert.NotNull(created);
        }

        [Theory]
        [InlineData(false, RegistryOptions.Volatile)]
        [InlineData(true, RegistryOptions.Volatile)]
        [InlineData(false, RegistryOptions.None)]
        [InlineData(true, RegistryOptions.None)]
        public void ReadWritePermissionCheckWithRegistryOptionsTestsValid(bool alreadyExists, RegistryOptions options)
        {
            string subkey = "TEST_" + options.ToString();

            if (alreadyExists)
            {
                TestRegistryKey.CreateSubKey(subkey, RegistryKeyPermissionCheck.ReadWriteSubTree, options).Dispose();
            }

            using RegistryKey created = TestRegistryKey.CreateSubKey(subkey, RegistryKeyPermissionCheck.ReadWriteSubTree, options);
            Assert.NotNull(created);
        }

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_Writable_KeyExists_OpensKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, writable: true));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_ReadWritePermissionCheck_KeyExists_OpensKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_NonWritable_KeyExists_OpensKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, writable: false));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_DefaultPermissionCheck_KeyExists_OpensKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, RegistryKeyPermissionCheck.Default));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_ReadPermissionCheck_KeyExists_OpensKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyExists_OpensKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_Writable_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, writable: true));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_ReadWritePermissionCheck_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, RegistryKeyPermissionCheck.ReadWriteSubTree));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_NonWritable_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, writable: false));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_DefaultPermissionCheck_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, RegistryKeyPermissionCheck.Default));

        [Theory]
        [MemberData(nameof(TestRegistrySubKeyNames))]
        public void CreateSubKey_ReadPermissionCheck_KeyDoesNotExist_CreatesKeyWithFixedUpName(string expected, string subKeyName) =>
            Verify_CreateSubKey_KeyDoesNotExist_CreatesKeyWithFixedUpName(expected, () => TestRegistryKey.CreateSubKey(subKeyName, RegistryKeyPermissionCheck.ReadSubTree));
    }
}
