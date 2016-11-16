// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Win32.RegistryTests
{
    public class RegistryKey_GetValue_CorruptData : RegistryTestsBase
    {
        [Fact]
        public void ReadRegMultiSzLackingFinalNullTerminatorCorrectly()
        {
            // We have to intentionally write a REG_MULTI_SZ value
            // lacking a final null terminator, like "str1\0str2\0str3\0"
            // (there should be 2 \0s at the end) to the registry.
            // Since we can't do this via the RegistryKey APIs,
            // do it manually via P/Invoke calls.

            const string TestValueName = "CorruptData";
            string corrupt = "str1\0str2\0str3\0";

            SafeRegistryHandle handle = TestRegistryKey.Handle;
            int ret = Interop.Advapi32.RegSetValueEx(handle, TestValueName, 0,
                RegistryValueKind.MultiString, corrupt, corrupt.Length * 2);
            Assert.Equal(0, ret);
            try
            {
                object o = TestRegistryKey.GetValue(TestValueName);
                Assert.IsType<string[]>(o);

                var strings = (string[])o;
                string[] expected = { "str1", "str2", "str3" };
                Assert.Equal(expected, strings);

            }
            finally
            {
                TestRegistryKey.DeleteValue(TestValueName);
            }
        }

        [Theory]
        [InlineData(RegistryValueKind.String, new byte[] { 6, 5, 6 })]
        [InlineData(RegistryValueKind.ExpandString, new byte[] { 6, 5, 6 })]
        [InlineData(RegistryValueKind.MultiString, new byte[] { 6, 5, 6, 0, 0 })]
        public void RegSzOddByteLength(RegistryValueKind kind, byte[] contents)
        {
            const string TestValueName = "CorruptData2";

            SafeRegistryHandle handle = TestRegistryKey.Handle;
            int ret = Interop.Advapi32.RegSetValueEx(handle, TestValueName, 0,
                kind, contents, contents.Length);
            Assert.Equal(0, ret);
            try
            {
                object o = TestRegistryKey.GetValue(TestValueName);

                string s;
                if (kind == RegistryValueKind.MultiString)
                {
                    Assert.IsType<string[]>(o);
                    var strings = (string[])o;
                    Assert.Equal(1, strings.Length);
                    s = strings[0];
                }
                else
                {
                    Assert.IsType<string>(o);
                    s = (string)o;
                }

                Assert.Equal(2, s.Length);
                Assert.Equal(0x506, s[0]);
                Assert.Equal(0x6, s[1]);
            }
            finally
            {
                TestRegistryKey.DeleteValue(TestValueName);
            }
        }
    }
}
