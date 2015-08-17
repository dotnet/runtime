// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_SetValueKind_str_obj_valueKind : RegistryTestsBase
    {
        public static IEnumerable<object[]> TestObjects { get { return TestData.TestObjects; } }

        [Theory]
        [MemberData("TestObjects")]
        public void SetValueWithUnknownValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            string valueName = "Testing_" + testIndex.ToString();

            _testRegistryKey.SetValue(valueName, testValue, RegistryValueKind.Unknown);
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

            _testRegistryKey.SetValue(valueName, testValue, expectedValueKind);
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

            _testRegistryKey.SetValue(valueName, testValue, expectedValueKind);
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

                _testRegistryKey.SetValue(valueName, testValue, expectedValueKind);
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

                _testRegistryKey.SetValue(valueName, testValue, expectedValueKind);
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

                _testRegistryKey.SetValue(valueName, testValue, expectedValueKind);
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

                _testRegistryKey.SetValue(valueName, testValue, expectedValueKind);
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
            _testRegistryKey.SetValue(valueName, expectedValue1, expectedValueKind1);
            Assert.True(_testRegistryKey.GetValue(valueName) != null, "Registry key doesn't exists");
            Assert.Equal(expectedValue1, (int)_testRegistryKey.GetValue(valueName));
            Assert.Equal(expectedValueKind1, _testRegistryKey.GetValueKind(valueName));

            _testRegistryKey.SetValue(valueName, expectedValue2, expectedValueKind2);
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

            _testRegistryKey.SetValue(valueName, expectedValue, expectedValueKind);
            Assert.Equal(expectedValue, (long)_testRegistryKey.GetValue(valueName));
            Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
        }

        [Fact]
        public void NegativeTests()
        {
            // Should throw if key length above 255 characters but prior to V4 the limit is 16383
            const int maxValueNameLength = 16383;
            string tooLongValueName = new string('a', maxValueNameLength + 1);
            Assert.Throws<ArgumentException>(() => _testRegistryKey.SetValue(tooLongValueName, ulong.MaxValue, RegistryValueKind.String));

            const string valueName = "FooBar";
            // Should throw if passed value is null
            Assert.Throws<ArgumentNullException>(() => _testRegistryKey.SetValue(valueName, null, RegistryValueKind.QWord));

            // Should throw because valueKind is equal to -2 which is not an acceptable value
            Assert.Throws<ArgumentException>(() => _testRegistryKey.SetValue(valueName, int.MinValue, (RegistryValueKind)(-2)));

            // Should throw because passed array contains null
            string[] strArr = { "one", "two", null, "three" };
            Assert.Throws<ArgumentException>(() => _testRegistryKey.SetValue(valueName, strArr, RegistryValueKind.MultiString));

            // Should throw because passed array has wrong type
            Assert.Throws<ArgumentException>(() => _testRegistryKey.SetValue(valueName, new[] { new object() }, RegistryValueKind.MultiString));

            // Should throw because passed array has wrong type
            object[] objTemp = { "my string", "your string", "Any once string" };
            Assert.Throws<ArgumentException>(() => _testRegistryKey.SetValue(valueName, objTemp, RegistryValueKind.Unknown));

            // Should throw because RegistryKey is readonly
            using (var rk = Registry.CurrentUser.OpenSubKey(_testRegistryKeyName, false))
            {
                Assert.Throws<UnauthorizedAccessException>(() => rk.SetValue(valueName, int.MaxValue, RegistryValueKind.DWord));
            }

            // Should throw if RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.SetValue(valueName, int.MinValue, RegistryValueKind.DWord);
            });
        }
    }
}
