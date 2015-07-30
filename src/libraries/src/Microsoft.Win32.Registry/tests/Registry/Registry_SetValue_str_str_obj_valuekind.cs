// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class Registry_SetValue_str_str_obj_valueKind : TestSubKey
    {
        private const string TestKey = "CM3001_TEST";

        public Registry_SetValue_str_str_obj_valueKind()
            : base(TestKey)
        {
        }

        public static IEnumerable<object[]> TestObjects { get { return TestData.TestObjects; } }

        [Theory]
        [MemberData("TestObjects")]
        public void SetValueWithUnknownValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            string valueName = "Testing_" + testIndex.ToString();

            Registry.SetValue(_testRegistryKey.Name, valueName, testValue, RegistryValueKind.Unknown);
            Assert.Equal(testValue.ToString(), _testRegistryKey.GetValue(valueName).ToString());
            Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
            _testRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [MemberData("TestObjects")]
        public void SetValueWithStringValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            string valueName = "Testing_" + testIndex.ToString();
            expectedValueKind = RegistryValueKind.String;

            Registry.SetValue(_testRegistryKey.Name, valueName, testValue, expectedValueKind);
            Assert.Equal(testValue.ToString(), _testRegistryKey.GetValue(valueName).ToString());
            Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
            _testRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [MemberData("TestObjects")]
        public void SetValueWithExpandStringValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            string valueName = "Testing_" + testIndex.ToString();
            expectedValueKind = RegistryValueKind.ExpandString;

            Registry.SetValue(_testRegistryKey.Name, valueName, testValue, expectedValueKind);
            Assert.Equal(testValue.ToString(), _testRegistryKey.GetValue(valueName).ToString());
            Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
            _testRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [MemberData("TestObjects")]
        public void SetValueWithMultiStringValeKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            try
            {
                string valueName = "Testing_" + testIndex.ToString();
                expectedValueKind = RegistryValueKind.MultiString;

                Registry.SetValue(_testRegistryKey.Name, valueName, testValue, expectedValueKind);
                Assert.Equal(testValue.ToString(), _testRegistryKey.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
                _testRegistryKey.DeleteValue(valueName);
            }
            catch (ArgumentException)
            {
                Assert.IsNotType<string[]>(testValue);
            }
        }

        [Theory]
        [MemberData("TestObjects")]
        public void SetValueWithBinaryValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            try
            {
                string valueName = "Testing_" + testIndex.ToString();
                expectedValueKind = RegistryValueKind.Binary;

                Registry.SetValue(_testRegistryKey.Name, valueName, testValue, expectedValueKind);
                Assert.Equal(testValue.ToString(), _testRegistryKey.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
                _testRegistryKey.DeleteValue(valueName);
            }
            catch (ArgumentException)
            {
                Assert.IsNotType<byte[]>(testValue);
            }
        }

        [Theory]
        [MemberData("TestObjects")]
        public void SetValueWithDWordValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            try
            {
                string valueName = "Testing_" + testIndex.ToString();
                expectedValueKind = RegistryValueKind.DWord;

                Registry.SetValue(_testRegistryKey.Name, valueName, testValue, expectedValueKind);
                Assert.Equal(Convert.ToInt32(testValue).ToString(), _testRegistryKey.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
                Assert.True(testIndex <= 15);
                _testRegistryKey.DeleteValue(valueName);
            }
            catch (ArgumentException ioe)
            {
                Assert.False(testIndex <= 15, ioe.ToString());
            }
        }

        [Theory]
        [MemberData("TestObjects")]
        public void SetValueWithQWordValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            try
            {
                string valueName = "Testing_" + testIndex.ToString();
                expectedValueKind = RegistryValueKind.QWord;

                Registry.SetValue(_testRegistryKey.Name, valueName, testValue, expectedValueKind);
                Assert.Equal(Convert.ToInt64(testValue).ToString(), _testRegistryKey.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
                Assert.True(testIndex <= 25);
                _testRegistryKey.DeleteValue(valueName);
            }
            catch (ArgumentException ioe)
            {
                Assert.False(testIndex <= 25, ioe.ToString());
            }
        }

        [Fact]
        public void SetValueForNonExistingKey()
        {
            const string valueName = "FooBar";
            const int expectedValue1 = int.MaxValue;
            const long expectedValue2 = long.MinValue;
            const RegistryValueKind expectedValueKind1 = RegistryValueKind.DWord;
            const RegistryValueKind expectedValueKind2 = RegistryValueKind.QWord;

            Assert.True(_testRegistryKey.GetValue(valueName) == null, "Registry key already exists");
            Registry.SetValue(_testRegistryKey.Name, valueName, expectedValue1, expectedValueKind1);
            Assert.True(_testRegistryKey.GetValue(valueName) != null, "Registry key doesn't exists");
            Assert.Equal(expectedValue1, (int)_testRegistryKey.GetValue(valueName));
            Assert.Equal(expectedValueKind1, _testRegistryKey.GetValueKind(valueName));

            Registry.SetValue(_testRegistryKey.Name, valueName, expectedValue2, expectedValueKind2);
            Assert.Equal(expectedValue2, (long)_testRegistryKey.GetValue(valueName));
            Assert.Equal(expectedValueKind2, _testRegistryKey.GetValueKind(valueName));
        }

        public static IEnumerable<object[]> TestValueNames { get { return TestData.TestValueNames; } }

        [Theory]
        [MemberData("TestValueNames")]
        public void SetValueWithNameTest(string valueName)
        {
            const long expectedValue = long.MaxValue;
            const RegistryValueKind expectedValueKind = RegistryValueKind.QWord;

            Registry.SetValue(_testRegistryKey.Name, valueName, expectedValue, expectedValueKind);
            Assert.Equal(expectedValue, (long)_testRegistryKey.GetValue(valueName));
            Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
        }

        [Fact]
        public void NegativeTests()
        {
            // Should throw if key length above 255 characters but prior to V4 the limit is 16383
            const int maxValueNameLength = 16383;
            var valueName = new string('a', maxValueNameLength + 1);
            Assert.Throws<ArgumentException>(() => Registry.SetValue(_testRegistryKey.Name, valueName, ulong.MaxValue, RegistryValueKind.String));

            valueName = "FooBar";
            // Should throw if passed value is null
            Assert.Throws<ArgumentNullException>(() => Registry.SetValue(_testRegistryKey.Name, valueName, null, RegistryValueKind.QWord));

            // Should throw because valueKind is equal to -2 which is not an acceptable value
            Assert.Throws<ArgumentException>(() => Registry.SetValue(_testRegistryKey.Name, valueName, int.MinValue, (RegistryValueKind)(-2)));

            // Should throw because passed array contains null
            string[] strArr = { "one", "two", null, "three" };
            Assert.Throws<ArgumentException>(() => Registry.SetValue(_testRegistryKey.Name, valueName, strArr, RegistryValueKind.MultiString));

            // Should throw because passed array has wrong type
            Assert.Throws<ArgumentException>(() => Registry.SetValue(_testRegistryKey.Name, valueName, new[] { new object() }, RegistryValueKind.MultiString));

            // Should throw because passed array has wrong type
            object[] objTemp = { "my string", "your string", "Any once string" };
            Assert.Throws<ArgumentException>(() => Registry.SetValue(_testRegistryKey.Name, valueName, objTemp, RegistryValueKind.Unknown));
        }
    }
}
