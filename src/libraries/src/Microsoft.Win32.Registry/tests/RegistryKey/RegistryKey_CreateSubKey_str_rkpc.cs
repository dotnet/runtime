// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_CreateSubKey_str_rkpc : TestSubKey
    {
        private const string TestKey = "REG_TEST_2";

        public RegistryKey_CreateSubKey_str_rkpc()
            : base(TestKey)
        {
        }

        [Fact]
        public void CreateWriteableSubkeyAndWrite()
        {
            // [] Vanilla; create a new subkey in read/write mode and write to it
            const string testValueName = "TestValue";
            const string testStringValueName = "TestString";
            const string testStringValue = "Hello World!†þ";
            const int testValue = 42;

            using (var rk = _testRegistryKey.CreateSubKey(TestKey, writable: true))
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
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.CreateSubKey(null, true));

            // Should throw if passed option is invalid
            Assert.Throws<ArgumentException>(() => _testRegistryKey.CreateSubKey(TestKey, true, options: (RegistryOptions)(-1)));
            Assert.Throws<ArgumentException>(() => _testRegistryKey.CreateSubKey(TestKey, true, options: (RegistryOptions)3));

            // Should throw if key length above 255 characters
            const int maxValueNameLength = 255;
            Assert.Throws<ArgumentException>(() => _testRegistryKey.CreateSubKey(new string('a', maxValueNameLength + 1)));

            //According to msdn documetation max nesting level exceeds is 510 but actual is 508
            const int maxNestedLevel = 508;
            string exceedsNestedSubkeyName = string.Join(@"\", Enumerable.Repeat("a", maxNestedLevel));
            Assert.Throws<IOException>(() => _testRegistryKey.CreateSubKey(exceedsNestedSubkeyName, true));

            // Should throw if RegistryKey is readonly
            const string name = "FooBar";
            _testRegistryKey.SetValue(name, 42);
            using (var rk = Registry.CurrentUser.CreateSubKey(TestKey, writable: false, options: RegistryOptions.None))
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
                _testRegistryKey.CreateSubKey(TestKey, true);
            });
        }

        [Fact]
        public void CreateSubkeyWithEmptyName()
        {
            // [] Let the name of the created subkey be empty
            string expectedName = _testRegistryKey.Name + @"\";
            var rk = _testRegistryKey.CreateSubKey(string.Empty, true);
            Assert.NotNull(rk);
            Assert.Equal(expectedName, rk.Name);
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
                _testRegistryKey.CreateSubKey(subkey);
            }

            Assert.NotNull(_testRegistryKey.CreateSubKey(subkey));
        }
    }
}
