// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                TestRegistryKey.Dispose();
                TestRegistryKey.GetValue(null, TestData.DefaultValue);
            });
        }

        [Fact]
        public void GetDefaultValueTest()
        {
            if (!TestRegistryKey.IsDefaultValueSet())
            {
                Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null, TestData.DefaultValue));
                Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(string.Empty, TestData.DefaultValue));
            }

            Assert.True(TestRegistryKey.SetDefaultValue(TestData.DefaultValue));
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(null, null));
            Assert.Equal(TestData.DefaultValue, TestRegistryKey.GetValue(string.Empty, null));
        }

        [Fact]
        public void ShouldAcceptNullAsDefaultValue()
        {
            Assert.Null(TestRegistryKey.GetValue("tt", defaultValue: null));
        }

        public static IEnumerable<object[]> TestValueTypes { get { return TestData.TestValueTypes; } }

        [Theory]
        [MemberData(nameof(TestValueTypes))]
        public void TestGetValueWithValueTypes(string valueName, object testValue)
        {
            TestRegistryKey.SetValue(valueName, testValue);
            Assert.Equal(testValue.ToString(), TestRegistryKey.GetValue(valueName, null).ToString());
            TestRegistryKey.DeleteValue(valueName);
        }
    }
}
