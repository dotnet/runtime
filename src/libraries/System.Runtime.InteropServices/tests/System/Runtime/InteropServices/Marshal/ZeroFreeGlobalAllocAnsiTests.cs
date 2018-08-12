// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ZeroFreeGlobalAllocAnsiTests
    {
        [Fact]
        public void ZeroFreeGlobalAllocAnsi_ValidPointer_Success()
        {
            using (SecureString secureString = ToSecureString("hello"))
            {
                IntPtr ptr = Marshal.SecureStringToGlobalAllocAnsi(secureString);
                Marshal.ZeroFreeGlobalAllocAnsi(ptr);
            }
        }

        [Fact]
        public void ZeroFreeGlobalAllocAnsi_Zero_Nop()
        {
            Marshal.ZeroFreeGlobalAllocAnsi(IntPtr.Zero);
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
