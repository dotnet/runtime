// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class Registry_GetValue_str_str_obj : RegistryTestsBase
    {
        [Fact]
        public static void NullKeyName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("keyName", null, () => Registry.GetValue(null, null, null));
        }

        public static readonly object[][] ArgumentExceptionTestData =
        {
            new object[] { string.Empty }, // Not a valid base key name.
            new object[] { "HHHH_MMMM" }, // Not a valid base key name.
            new object[] { "HHHH_CURRENT_USER" }, // Not a valid base key name.
            new object[] { "HKEY_CURRENT_USER_FOOBAR" }, // Starts with one of the valid base key names but isn't valid.
            new object[] { new string('a', 10) }, // String of length 10 that isn't HKEY_USERS.
            new object[] { new string('a', 17) }, // String of length 17 that isn't HKEY_CURRENT_USER or HKEY_CLASSES_ROOT.
            new object[] { new string('a', 18) }, // String of length 18 that isn't HKEY_LOCAL_MACHINE.
            new object[] { new string('a', 19) }, // String of length 19 that isn't HKEY_CURRENT_CONFIG.
            new object[] { new string('a', 21) } // String of length 21 that isn't HKEY_PERFORMANCE_DATA.
        };

        [Theory]
        [MemberData(nameof(ArgumentExceptionTestData))]
        public static void InvalidKeyName_ThrowsArgumentException(string keyName)
        {
            AssertExtensions.Throws<ArgumentException>("keyName", null, () => Registry.GetValue(keyName, null, null));
        }

        [Fact]
        public void ShouldReturnNull()
        {
            // Passing null object as default object to return shouldn't throw
            Assert.Null(Registry.GetValue(TestRegistryKey.Name, "xzy", defaultValue: null));

            // If the key does not exists, then the method should return null all time
            Assert.Null(Registry.GetValue(TestRegistryKey.Name + "\\XYZ", null, -1));
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData(nameof(TestValueTypes))]
        public void TestGetValueWithValueTypes(string valueName, object testValue)
        {
            TestRegistryKey.SetValue(valueName, testValue);
            Assert.Equal(testValue.ToString(), Registry.GetValue(TestRegistryKey.Name, valueName, null).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestRegistryKeys
        {
            get
            {
                return new[]
                {
                    new object[] { Registry.CurrentUser,     "Test1", true },
                    new object[] { Registry.LocalMachine,    "Test2", true },
                    new object[] { Registry.ClassesRoot,     "Test3", true },
                    new object[] { Registry.Users,           "Test4", true },
                    new object[] { Registry.PerformanceData, "Test5", true },
                    new object[] { Registry.CurrentConfig,   "Test6", true },

                    new object[] { Registry.CurrentUser,     "Test7", false },
                    new object[] { Registry.LocalMachine,    "Test8", false },
                    new object[] { Registry.ClassesRoot,     "Test9", false },
                    new object[] { Registry.Users,           "Test10", false },
                    new object[] { Registry.PerformanceData, "Test11", false },
                    new object[] { Registry.CurrentConfig,   "Test12", false },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TestRegistryKeys))]
        public static void GetValueFromDifferentKeys(RegistryKey key, string valueName, bool useSeparator)
        {
            const int expectedValue = 11;
            const int defaultValue = 42;
            try
            {
                key.SetValue(valueName, expectedValue);
                try
                {
                    // keyName should be case insensitive so we mix casing
                    string keyName = MixUpperAndLowerCase(key.Name) + (useSeparator ? "\\" : "");
                    Assert.Equal(expectedValue, (int)Registry.GetValue(keyName, valueName, defaultValue));
                }
                finally
                {
                    key.DeleteValue(valueName);
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }

        [Theory]
        [MemberData(nameof(TestRegistryKeys))]
        public static void GetDefaultValueFromDifferentKeys(RegistryKey key, string valueName, bool useSeparator)
        {
            // We ignore valueName because we test against default value
            valueName = null;
            try
            {
                // keyName should be case insensitive so we mix casing
                string keyName = MixUpperAndLowerCase(key.Name) + (useSeparator ? "\\" : "");
                if (key.IsSystemKeyDefaultValueSet())
                {
                    Registry.GetValue(keyName, valueName, null);
                }
                else
                {
                    Assert.Equal(TestData.DefaultValue, Registry.GetValue(keyName, valueName, TestData.DefaultValue));
                }
            }
            catch (IOException) { }
        }

        private static string MixUpperAndLowerCase(string str)
        {
            var builder = new System.Text.StringBuilder(str);

            for (int i = 0; i < builder.Length; ++i)
            {
                if (i % 2 == 0)
                {
                    builder[i] = char.ToLowerInvariant(builder[i]);
                }
                else
                {
                    builder[i] = char.ToUpperInvariant(builder[i]);
                }
            }

            return builder.ToString();
        }
    }
}
