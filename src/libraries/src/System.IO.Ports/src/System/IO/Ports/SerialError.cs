// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Ports
{
    public enum SerialError
    {
        TXFull = 0x100,
        RXOver = 0x01,
        Overrun = 0x02,
        RXParity = 0x04,
        Frame = 0x08,
    }
}
