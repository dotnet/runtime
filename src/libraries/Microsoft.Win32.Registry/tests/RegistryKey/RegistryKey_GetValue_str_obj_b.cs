// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValue_str_obj_b : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            Assert.Throws<ArgumentException>(() => _testRegistryKey.GetValue(null, null, (RegistryValueOptions)(-1)));
            Assert.Throws<ArgumentException>(() => _testRegistryKey.GetValue(null, null, (RegistryValueOptions)2));

            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.GetValue(null, TestData.DefaultValue, RegistryValueOptions.None);
            });
        }

        [Fact]
        public void GetDefaultValue()
        {
            if (!_testRegistryKey.IsDefaultValueSet())
            {
                Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(null, TestData.DefaultValue, RegistryValueOptions.DoNotExpandEnvironmentNames));
                Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(string.Empty, TestData.DefaultValue, RegistryValueOptions.DoNotExpandEnvironmentNames));
            }

            Assert.True(_testRegistryKey.SetDefaultValue(TestData.DefaultValue));
            Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(null, null, RegistryValueOptions.DoNotExpandEnvironmentNames));
            Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(string.Empty, null, RegistryValueOptions.DoNotExpandEnvironmentNames));
        }

        [Fact]
        public void ShouldAcceptNullAsDefaultValue()
        {
            Assert.Null(_testRegistryKey.GetValue("tt", defaultValue: null, options: RegistryValueOptions.DoNotExpandEnvironmentNames));
        }

        [Fact]
        public void GetStringValue()
        {
            // [] Pass name=Existing key, default value = null 
            const string valueName = "MyTestKey";
            const string expected = "This is a test string";

            _testRegistryKey.SetValue(valueName, expected, RegistryValueKind.ExpandString);
            Assert.Equal(expected, _testRegistryKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestExpandableStrings { get { return TestData.TestExpandableStrings; } }

        [Theory]
        [MemberData("TestExpandableStrings")]
        public void GetExpandableStringValueWithNoneOption(string testValue, string expectedValue)
        {
            // [] Make sure NoExpand = false works with some valid values.
            const string valueName = "MyTestKey";
            _testRegistryKey.SetValue(valueName, testValue, RegistryValueKind.ExpandString);
            Assert.Equal(expectedValue, _testRegistryKey.GetValue(valueName, null, RegistryValueOptions.None).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [InlineData("RegistryKey_GetValue_str_obj_b_MyEnv")]
        [InlineData("RegistryKey_GetValue_str_obj_b_PathPath")]
        [InlineData("RegistryKey_GetValue_str_obj_b_Name")]
        [InlineData("RegistryKey_GetValue_str_obj_b_blah")]
        [InlineData("RegistryKey_GetValue_str_obj_b_TestKEyyyyyyyyyyyyyy")]
        public void GetValueWithNewlyCreatedEnvironmentVarables(string varName)
        {
            const string valueName = "MyTestKey";
            string expectedValue = "%" + varName + "%" + @"\subdirectory\myfile.txt";
            Helpers.SetEnvironmentVariable(varName, @"C:\UsedToBeCurrentDirectoryButAnythingWorks");
            _testRegistryKey.SetValue(valueName, expectedValue, RegistryValueKind.ExpandString);
            Assert.Equal(expectedValue, _testRegistryKey.GetValue(valueName, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames));
            _testRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData("TestValueTypes")]
        public void GetValueWithValueTypes(string valueName, object testValue)
        {
            _testRegistryKey.SetValue(valueName, testValue, RegistryValueKind.ExpandString);
            Assert.Equal(testValue.ToString(), _testRegistryKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [MemberData("TestExpandableStrings")]
        public void GetExpandableStringValueWithDoNotExpandOption(string testValue, string expectedValue)
        {
            const string valueName = "MyTestKey";
            _testRegistryKey.SetValue(valueName, testValue, RegistryValueKind.ExpandString);
            Assert.Equal(testValue, _testRegistryKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestEnvironment { get { return TestData.TestEnvironment; } }

        [Theory]
        [MemberData("TestEnvironment")]
        public void GetValueWithEnvironmentVariable(string valueName, string envVariableName, string expectedVariableValue)
        {
            _testRegistryKey.SetValue(valueName, expectedVariableValue, RegistryValueKind.ExpandString);
            Assert.Equal(expectedVariableValue, _testRegistryKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }
    }
}
