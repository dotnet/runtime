// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_SetValue_str_obj : RegistryTestsBase
    {
        [Fact]
        public void Test01()
        {
            // [] Passing in null should throw ArgumentNullException
            //UPDATE: This sets the default value. We should move this test to a newly defined reg key so as not to screw up the system
            const string expected = "This is a test";
            TestRegistryKey.SetValue(null, expected);
            Assert.Equal(expected, TestRegistryKey.GetValue(null));
        }

        [Fact]
        public void NegativeTests()
        {
            // Should throw if passed value is null
            Assert.Throws<ArgumentNullException>(() => TestRegistryKey.SetValue("test", null));

            // Should throw if key length above 255 characters but prior to V4, the limit is 16383
            const int maxValueNameLength = 16383;
            AssertExtensions.Throws<ArgumentException>("name", null, () => TestRegistryKey.SetValue(new string('a', maxValueNameLength + 1), 5));

            // Should throw if passed value is array with uninitialized elements
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.SetValue("StringArr", value: new string[1]));

            // Should throw because only String[] (REG_MULTI_SZ) and byte[] (REG_BINARY) are supported.
            // RegistryKey.SetValue does not support arrays of type UInt32[].
            AssertExtensions.Throws<ArgumentException>(null, () => TestRegistryKey.SetValue("IntArray", value: new[] { 1, 2, 3 }));

            // Should throw if RegistryKey closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.SetValue("TestValue", 42);
            });
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData(nameof(TestValueTypes))]
        public void SetValueWithValueTypes(string valueName, object testValue)
        {
            TestRegistryKey.SetValue(valueName, testValue);
            Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }

        [Fact]
        public void SetValueWithInt32()
        {
            const string testValueName = "Int32";
            const int expected = -5;

            TestRegistryKey.SetValue(testValueName, expected);
            Assert.Equal(expected, (int)TestRegistryKey.GetValue(testValueName));
            TestRegistryKey.DeleteValue(testValueName);
        }

        [Fact]
        public void SetValueWithUInt64()
        {
            // This will be written as REG_SZ
            const string testValueName = "UInt64";
            const ulong expected = ulong.MaxValue;

            TestRegistryKey.SetValue(testValueName, expected);
            Assert.Equal(expected, Convert.ToUInt64(TestRegistryKey.GetValue(testValueName)));
            TestRegistryKey.DeleteValue(testValueName);
        }

        [Fact]
        public void SetValueWithByteArray()
        {
            // This will be written as  REG_BINARY
            const string testValueName = "UBArr";
            byte[] expected = { 1, 2, 3 };

            TestRegistryKey.SetValue(testValueName, expected);
            Assert.Equal(expected, (byte[])TestRegistryKey.GetValue(testValueName));
            TestRegistryKey.DeleteValue(testValueName);
        }

        [Fact]
        public void SetValueWithMultiString()
        {
            // This will be written as  REG_MULTI_SZ
            const string testValueName = "StringArr";
            string[] expected =
            {
                "This is a public",
                "broadcast intend to test",
                "lot of things. one of which"
            };

            TestRegistryKey.SetValue(testValueName, expected);
            Assert.Equal(expected, (string[])TestRegistryKey.GetValue(testValueName));
            TestRegistryKey.DeleteValue(testValueName);
        }

        public static IEnumerable<object[]> TestEnvironment { get { return TestData.TestEnvironment; } }

        [Theory]
        [MemberData(nameof(TestEnvironment))]
        public void SetValueWithEnvironmentVariable(string valueName, string envVariableName, string expectedVariableValue)
        {
            // ExpandEnvironmentStrings is converting "C:\Program Files (Arm)" to "C:\Program Files (x86)".
            if (envVariableName == "ProgramFiles" && PlatformDetection.IsArmProcess)
                return; // see https://github.com/dotnet/corefx/issues/28856

            string value = "%" + envVariableName + "%";
            TestRegistryKey.SetValue(valueName, value);

            string result = (string)TestRegistryKey.GetValue(valueName);
            //we don't expand for the user, REG_SZ_EXPAND not supported
            Assert.Equal(expectedVariableValue, Environment.ExpandEnvironmentVariables(result));
            TestRegistryKey.DeleteValue(valueName);
        }

        [Fact]
        public void SetValueWithEmptyString()
        {
            // Creating REG_SZ key with an empty string value does not add a null terminating char.
            const string testValueName = "test_122018";
            string expected = string.Empty;

            TestRegistryKey.SetValue(testValueName, expected);
            Assert.Equal(expected, (string)TestRegistryKey.GetValue(testValueName));
            TestRegistryKey.DeleteValue(testValueName);
        }
    }
}
