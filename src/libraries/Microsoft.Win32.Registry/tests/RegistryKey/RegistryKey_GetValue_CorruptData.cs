// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            int ret = Interop.mincore.RegSetValueEx(handle, TestValueName, 0,
                RegistryValueKind.MultiString, corrupt, corrupt.Length * 2);
            Assert.Equal(0, ret);

            object o = TestRegistryKey.GetValue(TestValueName);
            Assert.IsType<string[]>(o);

            var strings = (string[])o;
            string[] expected = { "str1", "str2", "str3" };
            Assert.Equal(expected, strings);

            TestRegistryKey.DeleteValue(TestValueName);
        }

        [Fact]
        public void RegSzOddByteLength()
        {
            const string TestValueName = "CorruptData2";
            byte[] contents = { 6, 5, 6 };

            SafeRegistryHandle handle = TestRegistryKey.Handle;
            int ret = Interop.mincore.RegSetValueEx(handle, TestValueName, 0,
                RegistryValueKind.String, contents, contents.Length);
            Assert.Equal(0, ret);

            object o = TestRegistryKey.GetValue(TestValueName);
            Assert.IsType<string>(o);

            string s = (string)o;
            Assert.Equal(s.Length, 2); // Math.Ceil(contents.Length / 2);

            Assert.Equal(0x506, s[0]);
            Assert.Equal(0x6, s[1]);

            TestRegistryKey.DeleteValue(TestValueName);
        }

        [Fact]
        public void RegExpandSzOddByteLength()
        {
            const string TestValueName = "CorruptData3";
            byte[] contents = { 6, 5, 6 };

            SafeRegistryHandle handle = TestRegistryKey.Handle;
            int ret = Interop.mincore.RegSetValueEx(handle, TestValueName, 0,
                RegistryValueKind.ExpandString, contents, contents.Length);
            Assert.Equal(0, ret);

            object o = TestRegistryKey.GetValue(TestValueName);
            Assert.IsType<string>(o);

            string s = (string)o;
            Assert.Equal(s.Length, 2); // Math.Ceil(contents.Length / 2);

            Assert.Equal(0x506, s[0]);
            Assert.Equal(0x6, s[1]);

            TestRegistryKey.DeleteValue(TestValueName);
        }

        [Fact]
        public void RegMultiSzOddByteLength()
        {
            const string TestValueName = "CorruptData4";
            byte[] contents = { 6, 5, 6, 0, 0 };

            SafeRegistryHandle handle = TestRegistryKey.Handle;
            int ret = Interop.mincore.RegSetValueEx(handle, TestValueName, 0,
                RegistryValueKind.MultiString, contents, contents.Length);
            Assert.Equal(0, ret);

            object o = TestRegistryKey.GetValue(TestValueName);
            Assert.IsType<string[]>(o);

            var strings = (string[])o;
            Assert.Equal(strings.Length, 1);

            string s = strings[0];
            Assert.Equal(s.Length, 2);

            Assert.Equal(0x506, s[0]);
            Assert.Equal(0x6, s[1]);

            TestRegistryKey.DeleteValue(TestValueName);
        }
    }
}
