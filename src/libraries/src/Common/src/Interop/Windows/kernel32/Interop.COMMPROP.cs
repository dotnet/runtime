// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        // Declaration for C# representation of Win32 COMMPROP
        // structure associated with a file handle to a serial communications resource.
        // Currently the only fields used are dwMaxTxQueue, dwMaxRxQueue, and dwMaxBaud
        // to ensure that users provide appropriate settings to the SerialStream constructor.
        //
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363189.aspx
        internal struct COMMPROP
        {
            public ushort  wPacketLength;
            public ushort  wPacketVersion;
            public int dwServiceMask;
            public int dwReserved1;
            public int dwMaxTxQueue;
            public int dwMaxRxQueue;
            public int dwMaxBaud;
            public int dwProvSubType;
            public int dwProvCapabilities;
            public int dwSettableParams;
            public int dwSettableBaud;
            public ushort wSettableData;
            public ushort  wSettableStopParity;
            public int dwCurrentTxQueue;
            public int dwCurrentRxQueue;
            public int dwProvSpec1;
            public int dwProvSpec2;
            public char wcProvChar;
        }

    }
}