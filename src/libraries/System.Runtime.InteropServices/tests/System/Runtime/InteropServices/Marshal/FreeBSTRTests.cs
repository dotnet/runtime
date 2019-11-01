// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class FreeBSTRTests
    {
        [Fact]
        public void FreeBSTR_ValidPointer_Success()
        {
            IntPtr ptr = Marshal.StringToBSTR("hello");
            Marshal.FreeBSTR(ptr);
        }

        [Fact]
        public void FreeBSTR_Zero_Nop()
        {
            Marshal.FreeBSTR(IntPtr.Zero);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FreeBSTR_Win32Atom_Nop()
        {
            // Windows Marshal has specific checks that do not free
            // anything if the ptr is less than 64K.
            Marshal.FreeBSTR((IntPtr)1);
        }
    }
}
