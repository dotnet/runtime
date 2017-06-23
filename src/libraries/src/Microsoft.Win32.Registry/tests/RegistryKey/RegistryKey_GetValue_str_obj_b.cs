// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            AssertExtensions.Throws<ArgumentException>("options", () => TestRegistryKey.GetValue(null, null, (RegistryValueOptions)(-1)));
            AssertExtensions.Throws<ArgumentException>("options", () => TestRegistryKey.GetValue(null, null, (RegistryValueOptions)2));

            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.GetValue(null, TestData.DefaultValue, RegistryValueOptions.None);
            });
        }

        [Fact]
        public void GetDefaultValue()
        {
            if (!TestRegistryKey.IsDefaultValueSet())
            {
                Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null, TestData.DefaultValue, RegistryValueOptions.DoNotExpandEnvironmentNames));
                Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(string.Empty, TestData.DefaultValue, RegistryValueOptions.DoNotExpandEnvironmentNames));
            }

            Assert.True(TestRegistryKey.SetDefaultValue(TestData.DefaultValue));
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null, null, RegistryValueOptions.DoNotExpandEnvironmentNames));
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(string.Empty, null, RegistryValueOptions.DoNotExpandEnvironmentNames));
        }

        [Fact]
        public void ShouldAcceptNullAsDefaultValue()
        {
            Assert.Null(TestRegistryKey.GetValue("tt", defaultValue: null, options: RegistryValueOptions.DoNotExpandEnvironmentNames));
        }

        [Fact]
        public void GetStringValue()
        {
            // [] Pass name=Existing key, default value = null 
            const string valueName = "MyTestKey";
            const string expected = "This is a test string";

            TestRegistryKey.SetValue(valueName, expected, RegistryValueKind.ExpandString);
            Assert.Equal(expected, TestRegistryKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestExpandableStrings { get { return TestData.TestExpandableStrings; } }

        [Theory]
        [MemberData(nameof(TestExpandableStrings))]
        public void GetExpandableStringValueWithNoneOption(string testValue, string expectedValue)
        {
            // [] Make sure NoExpand = false works with some valid values.
            const string valueName = "MyTestKey";
            TestRegistryKey.SetValue(valueName, testValue, RegistryValueKind.ExpandString);
            Assert.Equal(expectedValue, TestRegistryKey.GetValue(valueName, null, RegistryValueOptions.None).ToString());
            TestRegistryKey.DeleteValue(valueName);
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
            TestRegistryKey.SetValue(valueName, expectedValue, RegistryValueKind.ExpandString);
            Assert.Equal(expectedValue, TestRegistryKey.GetValue(valueName, string.Empty, RegistryValueOptions.DoNotExpandEnvironmentNames));
            TestRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData(nameof(TestValueTypes))]
        public void GetValueWithValueTypes(string valueName, object testValue)
        {
            TestRegistryKey.SetValue(valueName, testValue, RegistryValueKind.ExpandString);
            Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }

        [Theory]
        [MemberData(nameof(TestExpandableStrings))]
        public void GetExpandableStringValueWithDoNotExpandOption(string testValue, string expectedValue)
        {
            const string valueName = "MyTestKey";
            TestRegistryKey.SetValue(valueName, testValue, RegistryValueKind.ExpandString);
            Assert.Equal(testValue, TestRegistryKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }

        public static IEnumerable<object[]> TestEnvironment { get { return TestData.TestEnvironment; } }

        [Theory]
        [MemberData(nameof(TestEnvironment))]
        public void GetValueWithEnvironmentVariable(string valueName, string envVariableName, string expectedVariableValue)
        {
            TestRegistryKey.SetValue(valueName, expectedVariableValue, RegistryValueKind.ExpandString);
            Assert.Equal(expectedVariableValue, TestRegistryKey.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }
    }
}
