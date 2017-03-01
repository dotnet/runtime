// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Ports
{
    public enum SerialError
    {
        TXFull = NativeMethods.CE_TXFULL,
        RXOver = NativeMethods.CE_RXOVER,
        Overrun = NativeMethods.CE_OVERRUN,
        RXParity = NativeMethods.CE_PARITY,
        Frame = NativeMethods.CE_FRAME,
    }
}
