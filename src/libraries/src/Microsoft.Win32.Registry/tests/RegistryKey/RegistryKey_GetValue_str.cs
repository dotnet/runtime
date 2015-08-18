// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValue_str : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.GetValue(null);
            });
        }

        [Fact]
        public void GetDefaultValueTest()
        {
            Assert.True(TestRegistryKey.SetDefaultValue(TestData.DefaultValue));
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null));
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(string.Empty));
        }

        [Fact]
        public void RegistryKeyGetValueMultiStringDoesNotDiscardZeroLengthStrings()
        {
            const string valueName = "Test";
            string[] expected = { "", "Hello", "", "World", "" };

            TestRegistryKey.SetValue(valueName, expected);
            Assert.Equal(expected, (string[])TestRegistryKey.GetValue(valueName));
            TestRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData("TestValueTypes")]
        public void TestGetValueWithValueTypes(string valueName, object testValue)
        {
            TestRegistryKey.SetValue(valueName, testValue);
            Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }

        [Fact]
        public void GetStringTest()
        {
            const string valueName = "Test";
            const string expected = "Here is a little test string";

            TestRegistryKey.SetValue(valueName, expected);
            Assert.Equal(expected, TestRegistryKey.GetValue(valueName).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }

        [Fact]
        public void GetByteArrayTest()
        {
            const string valueName = "UBArr";
            byte[] expected = { 1, 2, 3 };

            TestRegistryKey.SetValue(valueName, expected);
            Assert.Equal(expected, (byte[])TestRegistryKey.GetValue(valueName));
            TestRegistryKey.DeleteValue(valueName);
        }

        [Fact]
        public void GetStringArrayTest()
        {
            const string valueName = "StringArr";
            string[] expected =
            {
                "This is a public",
                "broadcast intend to test",
                "lot of things. one of which"
            };

            TestRegistryKey.SetValue(valueName, expected);
            Assert.Equal(expected, (string[])TestRegistryKey.GetValue(valueName));
            TestRegistryKey.DeleteValue(valueName);
        }
    }
}
