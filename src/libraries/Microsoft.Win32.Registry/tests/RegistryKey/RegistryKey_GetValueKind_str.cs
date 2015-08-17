// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Assert.Throws<IOException>(() => _testRegistryKey.GetValueKind("DoesNotExist"));

            // RegistryKey is closed
            Assert.Throws<ObjectDisposedException>(() =>
            {
                _testRegistryKey.Dispose();
                _testRegistryKey.GetValueKind("FooBar");
            });

        }

        [Fact]
        public void GetValueKindForDefaultvalue()
        {
            const RegistryValueKind expectedValueKind = RegistryValueKind.QWord;
            _testRegistryKey.SetValue(null, 42, expectedValueKind);

            Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(null));
            Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(string.Empty));
        }

        [Fact]
        public void GetValueKindFromReadonlyRegistryKey()
        {
            // [] RegistryKey is readonly
            const string valueName = null;
            const long expectedValue = long.MaxValue;
            const RegistryValueKind expectedValueKind = RegistryValueKind.QWord;
            _testRegistryKey.SetValue(valueName, expectedValue, expectedValueKind);

            using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(_testRegistryKeyName, false))
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
            
            _testRegistryKey.SetValue(valueName, new byte[] { 23, 32 }, RegistryValueKind.None);
            Assert.Equal(expectedValueKind, _testRegistryKey.GetValueKind(valueName));
        }
    }
}
