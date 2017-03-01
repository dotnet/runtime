// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        // Declaration for C# representation of Win32 Device Control Block (DCB)
        // structure.  Note that all flag properties are encapsulated in the Flags field here,
        // and accessed/set through SerialStream's GetDcbFlag(...) and SetDcbFlag(...) methods.
        //
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363214.aspx
        internal struct DCB
        {
            public uint DCBlength;
            public uint BaudRate;
            public uint Flags;
            public ushort wReserved;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public byte XonChar;
            public byte XoffChar;
            public byte ErrorChar;
            public byte EofChar;
            public byte EvtChar;
            public ushort wReserved1;
        }
    }
}