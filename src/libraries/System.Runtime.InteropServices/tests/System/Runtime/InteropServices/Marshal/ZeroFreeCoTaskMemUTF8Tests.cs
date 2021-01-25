// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class ZeroFreeCoTaskMemUTF8Tests
    {
        [Fact]
        public void ZeroFreeCoTaskMemUTF8_ValidPointer_Success()
        {
            IntPtr ptr = Marshal.StringToCoTaskMemUTF8("hello");
            Marshal.ZeroFreeCoTaskMemUTF8(ptr);
        }
    }
}
