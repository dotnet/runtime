// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class Registry_SetValue_str_str_obj_valueKind : RegistryTestsBase
    {
        public static IEnumerable<object[]> TestObjects { get { return TestData.TestObjects; } }

        [Theory]
        [MemberData(nameof(TestObjects))]
        public void SetValueWithUnknownValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            string valueName = "Testing_" + testIndex.ToString();

            Registry.SetValue(TestRegistryKey.Name, valueName, testValue, RegistryValueKind.Unknown);
            Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName).ToString());
            Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
            TestRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [MemberData(nameof(TestObjects))]
        public void SetValueWithStringValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            string valueName = "Testing_" + testIndex.ToString();
            expectedValueKind = RegistryValueKind.String;

            Registry.SetValue(TestRegistryKey.Name, valueName, testValue, expectedValueKind);
            Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName).ToString());
            Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
            TestRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [MemberData(nameof(TestObjects))]
        public void SetValueWithExpandStringValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            string valueName = "Testing_" + testIndex.ToString();
            expectedValueKind = RegistryValueKind.ExpandString;

            Registry.SetValue(TestRegistryKey.Name, valueName, testValue, expectedValueKind);
            Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName).ToString());
            Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
            TestRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [MemberData(nameof(TestObjects))]
        public void SetValueWithMultiStringValeKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            try
            {
                string valueName = "Testing_" + testIndex.ToString();
                expectedValueKind = RegistryValueKind.MultiString;

                Registry.SetValue(TestRegistryKey.Name, valueName, testValue, expectedValueKind);
                Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
                TestRegistryKey.DeleteValue(valueName);
            }
            catch (ArgumentException)
            {
                Assert.IsNotType<string[]>(testValue);
            }
        }

        [Theory]
        [MemberData(nameof(TestObjects))]
        public void SetValueWithBinaryValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            try
            {
                string valueName = "Testing_" + testIndex.ToString();
                expectedValueKind = RegistryValueKind.Binary;

                Registry.SetValue(TestRegistryKey.Name, valueName, testValue, expectedValueKind);
                Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
                TestRegistryKey.DeleteValue(valueName);
            }
            catch (ArgumentException)
            {
                Assert.IsNotType<byte[]>(testValue);
            }
        }

        [Theory]
        [MemberData(nameof(TestObjects))]
        public void SetValueWithDWordValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            try
            {
                string valueName = "Testing_" + testIndex.ToString();
                expectedValueKind = RegistryValueKind.DWord;

                Registry.SetValue(TestRegistryKey.Name, valueName, testValue, expectedValueKind);
                Assert.Equal(Convert.ToInt32(testValue).ToString(), TestRegistryKey.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
                Assert.True(testIndex <= 15);
                TestRegistryKey.DeleteValue(valueName);
            }
            catch (ArgumentException ioe)
            {
                Assert.False(testIndex <= 15, ioe.ToString());
            }
        }

        [Theory]
        [MemberData(nameof(TestObjects))]
        public void SetValueWithQWordValueKind(int testIndex, object testValue, RegistryValueKind expectedValueKind)
        {
            try
            {
                string valueName = "Testing_" + testIndex.ToString();
                expectedValueKind = RegistryValueKind.QWord;

                Registry.SetValue(TestRegistryKey.Name, valueName, testValue, expectedValueKind);
                Assert.Equal(Convert.ToInt64(testValue).ToString(), TestRegistryKey.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
                Assert.True(testIndex <= 25);
                TestRegistryKey.DeleteValue(valueName);
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

            Assert.True(TestRegistryKey.GetValue(valueName) == null, "Registry key already exists");
            Registry.SetValue(TestRegistryKey.Name, valueName, expectedValue1, expectedValueKind1);
            Assert.True(TestRegistryKey.GetValue(valueName) != null, "Registry key doesn't exists");
            Assert.Equal(expectedValue1, (int)TestRegistryKey.GetValue(valueName));
            Assert.Equal(expectedValueKind1, TestRegistryKey.GetValueKind(valueName));

            Registry.SetValue(TestRegistryKey.Name, valueName, expectedValue2, expectedValueKind2);
            Assert.Equal(expectedValue2, (long)TestRegistryKey.GetValue(valueName));
            Assert.Equal(expectedValueKind2, TestRegistryKey.GetValueKind(valueName));
        }

        public static IEnumerable<object[]> TestValueNames { get { return TestData.TestValueNames; } }

        [Theory]
        [MemberData(nameof(TestValueNames))]
        public void SetValueWithNameTest(string valueName)
        {
            const long expectedValue = long.MaxValue;
            const RegistryValueKind expectedValueKind = RegistryValueKind.QWord;

            Registry.SetValue(TestRegistryKey.Name, valueName, expectedValue, expectedValueKind);
            Assert.Equal(expectedValue, (long)TestRegistryKey.GetValue(valueName));
            Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
        }

        [Fact]
        public void NegativeTests()
        {
            // Should throw if key length above 255 characters but prior to V4 the limit is 16383
            const int maxValueNameLength = 16383;
            var valueName = new string('a', maxValueNameLength + 1);
            AssertExtensions.Throws<ArgumentException>("name", null, () => Registry.SetValue(TestRegistryKey.Name, valueName, ulong.MaxValue, RegistryValueKind.String));

            valueName = "FooBar";
            // Should throw if passed value is null
            Assert.Throws<ArgumentNullException>(() => Registry.SetValue(TestRegistryKey.Name, valueName, null, RegistryValueKind.QWord));

            // Should throw because valueKind is equal to -2 which is not an acceptable value
            AssertExtensions.Throws<ArgumentException>("valueKind", () => Registry.SetValue(TestRegistryKey.Name, valueName, int.MinValue, (RegistryValueKind)(-2)));

            // Should throw because passed array contains null
            string[] strArr = { "one", "two", null, "three" };
            AssertExtensions.Throws<ArgumentException>(null, () => Registry.SetValue(TestRegistryKey.Name, valueName, strArr, RegistryValueKind.MultiString));

            // Should throw because passed array has wrong type
            AssertExtensions.Throws<ArgumentException>(null, () => Registry.SetValue(TestRegistryKey.Name, valueName, new[] { new object() }, RegistryValueKind.MultiString));

            // Should throw because passed array has wrong type
            object[] objTemp = { "my string", "your string", "Any once string" };
            AssertExtensions.Throws<ArgumentException>(null, () => Registry.SetValue(TestRegistryKey.Name, valueName, objTemp, RegistryValueKind.Unknown));
        }
    }
}
