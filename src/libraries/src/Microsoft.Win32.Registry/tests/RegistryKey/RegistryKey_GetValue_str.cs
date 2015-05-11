// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValue_str : TestSubKey
    {
        private const string TestKey = "REG_TEST_9";

        public RegistryKey_GetValue_str()
            : base(TestKey)
        {
        }

        [Fact]
        public void NegativeTests()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.GetValue(null);
            });
        }

        [Fact]
        public void GetDefaultValueTest()
        {
            Assert.True(_testRegistryKey.SetDefaultValue(TestData.DefaultValue));
            Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(null));
            Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(string.Empty));
        }

        [Fact]
        public void RegistryKeyGetValueMultiStringDoesNotDiscardZeroLengthStrings()
        {
            const string valueName = "Test";
            string[] expected = { "", "Hello", "", "World", "" };

            _testRegistryKey.SetValue(valueName, expected);
            Assert.Equal(expected, (string[])_testRegistryKey.GetValue(valueName));
            _testRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData("TestValueTypes")]
        public void TestGetValueWithValueTypes(string valueName, object testValue)
        {
            _testRegistryKey.SetValue(valueName, testValue);
            Assert.Equal(testValue.ToString(), _testRegistryKey.GetValue(valueName).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }

        [Fact]
        public void GetStringTest()
        {
            const string valueName = "Test";
            const string expected = "Here is a little test string";

            _testRegistryKey.SetValue(valueName, expected);
            Assert.Equal(expected, _testRegistryKey.GetValue(valueName).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }

        [Fact]
        public void GetByteArrayTest()
        {
            const string valueName = "UBArr";
            byte[] expected = { 1, 2, 3 };

            _testRegistryKey.SetValue(valueName, expected);
            Assert.Equal(expected, (byte[])_testRegistryKey.GetValue(valueName));
            _testRegistryKey.DeleteValue(valueName);
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

            _testRegistryKey.SetValue(valueName, expected);
            Assert.Equal(expected, (string[])_testRegistryKey.GetValue(valueName));
            _testRegistryKey.DeleteValue(valueName);
        }
    }
}
