// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class FreeHGlobalTests
    {
        [Fact]
        public void FreeHGlobal_ValidPointer_Success()
        {
            IntPtr mem = Marshal.AllocHGlobal(10);
            Marshal.FreeHGlobal(mem);
        }

        [Fact]
        public void FreeHGlobal_Zero_Nop()
        {
            Marshal.FreeHGlobal(IntPtr.Zero);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FreeHGlobal_Win32Atom_Nop()
        {
            // Windows Marshal has specific checks that do not free
            // anything if the ptr is less than 64K.
            Marshal.FreeHGlobal((IntPtr)1);
        }
    }
}
