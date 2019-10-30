// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class FreeCoTaskMemTests
    {
        [Fact]
        public void FreeCoTaskMem_ValidPointer_Success()
        {
            IntPtr mem = Marshal.AllocCoTaskMem(10);
            Marshal.FreeCoTaskMem(mem);
        }

        [Fact]
        public void FreeCoTaskMem_Zero_Nop()
        {
            Marshal.FreeCoTaskMem(IntPtr.Zero);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void FreeCoTaskMem_Win32Atom_Nop()
        {
            // Windows Marshal has specific checks that do not free
            // anything if the ptr is less than 64K.
            Marshal.FreeCoTaskMem((IntPtr)1);
        }
    }
}
