// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValue_str_obj : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.GetValue(null, TestData.DefaultValue);
            });
        }

        [Fact]
        public void GetDefaultValueTest()
        {
            if (!_testRegistryKey.IsDefaultValueSet())
            {
                Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(null, TestData.DefaultValue));
                Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(string.Empty, TestData.DefaultValue));
            }

            Assert.True(_testRegistryKey.SetDefaultValue(TestData.DefaultValue));
            Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(null, null));
            Assert.Equal(TestData.DefaultValue, _testRegistryKey.GetValue(string.Empty, null));
        }

        [Fact]
        public void ShouldAcceptNullAsDefaultValue()
        {
            Assert.Null(_testRegistryKey.GetValue("tt", defaultValue: null));
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData("TestValueTypes")]
        public void TestGetValueWithValueTypes(string valueName, object testValue)
        {
            _testRegistryKey.SetValue(valueName, testValue);
            Assert.Equal(testValue.ToString(), _testRegistryKey.GetValue(valueName, null).ToString());
            _testRegistryKey.DeleteValue(valueName);
        }
    }
}
