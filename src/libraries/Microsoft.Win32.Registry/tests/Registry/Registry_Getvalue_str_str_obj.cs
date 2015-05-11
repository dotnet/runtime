// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class Registry_GetValue_str_str_obj : TestSubKey
    {
        private const string TestKey = "CM1001_TEST";

        public Registry_GetValue_str_str_obj()
            : base(TestKey)
        {
        }

        [Fact]
        public static void NegativeTests()
        {
            // Should throw if passed keyName is null
            Assert.Throws<ArgumentNullException>(
                () => Registry.GetValue(keyName: null,valueName: null, defaultValue: null));

            // Passing a string which does NOT start with one of the valid base key names, that should throw ArgumentException.
            Assert.Throws<ArgumentException>(() => Registry.GetValue("HHHH_MMMM", null, null));
        }

        [Fact]
        public void ShouldReturnNull()
        {
            // Passing null object as default object to return shouldn't throw
            Assert.Null(Registry.GetValue(_testRegistryKey.Name, "xzy", defaultValue: null));

            // If the key does not exists, then the method should return null all time
            Assert.Null(Registry.GetValue(_testRegistryKey.Name + "\\XYZ", null, -1));
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData("TestValueTypes")]
        public void TestGetValueWithValueTypes(string valueName, object testValue)
        {
            _testRegistryKey.SetValue(valueName, testValue);
            Assert.Equal(testValue.ToString(), Registry.GetValue(_testRegistryKey.Name, valueName, null).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestRegistryKeys
        {
            get
            {
                return new[]
                {
                    new object[] { Registry.CurrentUser,     "Test1" },
                    new object[] { Registry.LocalMachine,    "Test2" },
                    new object[] { Registry.ClassesRoot,     "Test3" },
                    new object[] { Registry.Users,           "Test4" },
                    new object[] { Registry.PerformanceData, "Test5" },
                    new object[] { Registry.CurrentConfig,   "Test6" },
                };
            }
        }

        [Theory]
        [MemberData("TestRegistryKeys")]
        public static void GetValueFromDifferentKeys(RegistryKey key, string valueName)
        {
            const int expectedValue = 11;
            const int defaultValue = 42;
            try
            {
                key.SetValue(valueName, expectedValue);
                try
                {
                    Assert.Equal(expectedValue, (int)Registry.GetValue(key.Name, valueName, defaultValue));
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
        [MemberData("TestRegistryKeys")]
        public static void GetDefaultValueFromDifferentKeys(RegistryKey key, string valueName)
        {
            valueName = null;
            try
            {
                if (key.IsDefaultValueSet())
                {
                    Registry.GetValue(key.Name, valueName, null);
                }
                else
                {
                    Assert.Equal(TestData.DefaultValue, Registry.GetValue(key.Name, valueName, TestData.DefaultValue));
                }
            }
            catch (IOException) { }
        }
    }
}
