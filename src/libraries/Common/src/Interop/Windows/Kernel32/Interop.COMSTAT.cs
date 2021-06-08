// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // Declaration for C# representation of Win32 COMSTAT structure associated with
        // a file handle to a serial communications resource.  SerialStream's
        // InBufferBytes and OutBufferBytes directly expose cbInQue and cbOutQue to reading, respectively.
        //
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363200.aspx
        internal struct COMSTAT
        {
            public uint Flags;
            public uint cbInQue;
            public uint cbOutQue;
        }
    }
}
