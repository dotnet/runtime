// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Ports
{
    // TODO: These should be put in Common\Interop in classes like GenericOperations
    internal class NativeMethods
    {
        internal const int FILE_ATTRIBUTE_NORMAL = 0x00000080;
        internal const int FILE_FLAG_OVERLAPPED = 0x40000000;

        // The following are unique to the SerialPort/SerialStream classes
        internal const byte ONESTOPBIT = 0;
        internal const byte ONE5STOPBITS = 1;
        internal const byte TWOSTOPBITS = 2;

        internal const int DTR_CONTROL_DISABLE = 0x00;
        internal const int DTR_CONTROL_ENABLE = 0x01;
        internal const int DTR_CONTROL_HANDSHAKE = 0x02;

        internal const int RTS_CONTROL_DISABLE = 0x00;
        internal const int RTS_CONTROL_ENABLE = 0x01;
        internal const int RTS_CONTROL_HANDSHAKE = 0x02;
        internal const int RTS_CONTROL_TOGGLE = 0x03;

        internal const int MS_CTS_ON = 0x10;
        internal const int MS_DSR_ON = 0x20;
        internal const int MS_RING_ON = 0x40;
        internal const int MS_RLSD_ON = 0x80;

        internal const byte EOFCHAR = 26;

        // Since C# does not provide access to bitfields and the native DCB structure contains
        // a very necessary one, these are the positional offsets (from the right) of areas
        // of the 32-bit integer used in SerialStream's SetDcbFlag() and GetDcbFlag() methods.
        internal const int FBINARY = 0;
        internal const int FPARITY = 1;
        internal const int FOUTXCTSFLOW = 2;
        internal const int FOUTXDSRFLOW = 3;
        internal const int FDTRCONTROL = 4;
        internal const int FDSRSENSITIVITY = 6;
        internal const int FTXCONTINUEONXOFF = 7;
        internal const int FOUTX = 8;
        internal const int FINX = 9;
        internal const int FERRORCHAR = 10;
        internal const int FNULL = 11;
        internal const int FRTSCONTROL = 12;
        internal const int FABORTONOERROR = 14;
        internal const int FDUMMY2 = 15;

        internal const int PURGE_TXABORT = 0x0001;  // Kill the pending/current writes to the comm port.
        internal const int PURGE_RXABORT = 0x0002;  // Kill the pending/current reads to the comm port.
        internal const int PURGE_TXCLEAR = 0x0004;  // Kill the transmit queue if there.
        internal const int PURGE_RXCLEAR = 0x0008;  // Kill the typeahead buffer if there.

        internal const byte DEFAULTXONCHAR = (byte)17;
        internal const byte DEFAULTXOFFCHAR = (byte)19;

        internal const int SETRTS = 3;       // Set RTS high
        internal const int CLRRTS = 4;       // Set RTS low
        internal const int SETDTR = 5;       // Set DTR high
        internal const int CLRDTR = 6;

        internal const int EV_RXCHAR = 0x01;
        internal const int EV_RXFLAG = 0x02;
        internal const int EV_CTS = 0x08;
        internal const int EV_DSR = 0x10;
        internal const int EV_RLSD = 0x20;
        internal const int EV_BREAK = 0x40;
        internal const int EV_ERR = 0x80;
        internal const int EV_RING = 0x100;
        internal const int ALL_EVENTS = 0x1fb;  // don't use EV_TXEMPTY

        internal const int CE_RXOVER = 0x01;
        internal const int CE_OVERRUN = 0x02;
        internal const int CE_PARITY = 0x04;
        internal const int CE_FRAME = 0x08;
        internal const int CE_BREAK = 0x10;
        internal const int CE_TXFULL = 0x100;

        internal const int MAXDWORD = -1;   // note this is 0xfffffff, or UInt32.MaxValue, here used as an int

        internal const int NOPARITY = 0;
        internal const int ODDPARITY = 1;
        internal const int EVENPARITY = 2;
        internal const int MARKPARITY = 3;
        internal const int SPACEPARITY = 4;
    }
}
