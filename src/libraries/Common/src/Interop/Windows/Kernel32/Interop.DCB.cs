// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal static class DCBFlags
        {
            // Since C# does not provide access to bitfields and the native DCB structure contains
            // a very necessary one, these are the positional offsets (from the right) of areas
            // of the 32-bit integer used in SerialStream's SetDcbFlag() and GetDcbFlag() methods.
            internal const int FBINARY = 0;
            internal const int FPARITY = 1;
            internal const int FOUTXCTSFLOW = 2;
            internal const int FOUTXDSRFLOW = 3;
            internal const int FDTRCONTROL = 4;
            internal const int FDSRSENSITIVITY = 6;
            internal const int FOUTX = 8;
            internal const int FINX = 9;
            internal const int FERRORCHAR = 10;
            internal const int FNULL = 11;
            internal const int FRTSCONTROL = 12;
            internal const int FABORTONOERROR = 14;
            internal const int FDUMMY2 = 15;
        }

        internal static class DCBDTRFlowControl
        {
            internal const int DTR_CONTROL_DISABLE = 0x00;
            internal const int DTR_CONTROL_ENABLE = 0x01;
        }

        internal static class DCBRTSFlowControl
        {
            internal const int RTS_CONTROL_DISABLE = 0x00;
            internal const int RTS_CONTROL_ENABLE = 0x01;
            internal const int RTS_CONTROL_HANDSHAKE = 0x02;
            internal const int RTS_CONTROL_TOGGLE = 0x03;
        }

        internal static class DCBStopBits
        {
            internal const byte ONESTOPBIT = 0;
            internal const byte ONE5STOPBITS = 1;
            internal const byte TWOSTOPBITS = 2;
        }

        // Declaration for C# representation of Win32 Device Control Block (DCB)
        // structure.  Note that all flag properties are encapsulated in the Flags field here,
        // and accessed/set through SerialStream's GetDcbFlag(...) and SetDcbFlag(...) methods.
        //
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa363214.aspx
        internal struct DCB
        {
            internal const byte EOFCHAR = 26;

            internal const byte DEFAULTXONCHAR = (byte)17;
            internal const byte DEFAULTXOFFCHAR = (byte)19;

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