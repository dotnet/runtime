// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_CreateSubKey_str_rkpc : RegistryTestsBase
    {
        [Fact]
        public void CreateWriteableSubkeyAndWrite()
        {
            // [] Vanilla; create a new subkey in read/write mode and write to it
            const string testValueName = "TestValue";
            const string testStringValueName = "TestString";
            const string testStringValue = "Hello World!†þ";
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
        public void NegativeTests()
        {
            // Should throw if passed subkey name is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.CreateSubKey(null, true));

            // Should throw if passed option is invalid
            Assert.Throws<ArgumentException>(() => TestRegistryKey.CreateSubKey(TestRegistryKeyName, true, options: (RegistryOptions)(-1)));
            Assert.Throws<ArgumentException>(() => TestRegistryKey.CreateSubKey(TestRegistryKeyName, true, options: (RegistryOptions)3));

            // Should throw if key length above 255 characters
            const int maxValueNameLength = 255;
            Assert.Throws<ArgumentException>(() => TestRegistryKey.CreateSubKey(new string('a', maxValueNameLength + 1)));

            //According to msdn documetation max nesting level exceeds is 510 but actual is 508
            const int maxNestedLevel = 508;
            string exceedsNestedSubkeyName = string.Join(@"\", Enumerable.Repeat("a", maxNestedLevel));
            Assert.Throws<IOException>(() => TestRegistryKey.CreateSubKey(exceedsNestedSubkeyName, true));

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

            // Should throw if RegistryKey closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.CreateSubKey(TestRegistryKeyName, true);
            });
        }

        [Fact]
        public void CreateSubkeyWithEmptyName()
        {
            // [] Let the name of the created subkey be empty
            string expectedName = TestRegistryKey.Name + @"\";
            var rk = TestRegistryKey.CreateSubKey(string.Empty, true);
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
                TestRegistryKey.CreateSubKey(subkey);
            }

            Assert.NotNull(TestRegistryKey.CreateSubKey(subkey));
        }
    }
}
