// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ZeroFreeCoTaskMemUnicodeTests
    {
        [Fact]
        public void ZeroFreeCoTaskMemUnicode_ValidPointer_Success()
        {
            using (SecureString secureString = ToSecureString("hello"))
            {
                IntPtr ptr = Marshal.SecureStringToCoTaskMemUnicode(secureString);
                Marshal.ZeroFreeCoTaskMemUnicode(ptr);
            }
        }

        [Fact]
        public void ZeroFreeCoTaskMemUnicode_Zero_Nop()
        {
            Marshal.ZeroFreeCoTaskMemUnicode(IntPtr.Zero);
        }

        private static SecureString ToSecureString(string data)
        {
            var str = new SecureString();
            foreach (char c in data)
            {
                str.AppendChar(c);
            }
            str.MakeReadOnly();
            return str;
        }
    }
}
