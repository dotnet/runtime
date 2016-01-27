// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValueKind_str : RegistryTestsBase
    {
        [Fact]
        public void NegativeTests()
        {
            // Registry Key does not exist
            Assert.Throws<IOException>(() => TestRegistryKey.GetValueKind("DoesNotExist"));

            // RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                TestRegistryKey.Dispose();
                TestRegistryKey.GetValueKind("FooBar");
            });

        }

        [Fact]
        public void GetValueKindForDefaultvalue()
        {
            const RegistryValueKind expectedValueKind = RegistryValueKind.QWord;
            TestRegistryKey.SetValue(null, 42, expectedValueKind);

            Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(null));
            Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(string.Empty));
        }

        [Fact]
        public void GetValueKindFromReadonlyRegistryKey()
        {
            // [] RegistryKey is readonly
            const string valueName = null;
            const long expectedValue = long.MaxValue;
            const RegistryValueKind expectedValueKind = RegistryValueKind.QWord;
            TestRegistryKey.SetValue(valueName, expectedValue, expectedValueKind);

            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(TestRegistryKeyName, false))
            {
                Assert.Equal(expectedValue.ToString(), rk.GetValue(valueName).ToString());
                Assert.Equal(expectedValueKind, rk.GetValueKind(valueName));
            }
        }

        [Fact]
        public void ValueKindNoneTests()
        {
            const string valueName = "NoneKind";
            const RegistryValueKind expectedValueKind = RegistryValueKind.None;
            
            TestRegistryKey.SetValue(valueName, new byte[] { 23, 32 }, RegistryValueKind.None);
            Assert.Equal(expectedValueKind, TestRegistryKey.GetValueKind(valueName));
        }
    }
}
