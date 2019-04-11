// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Ports
{
    public enum SerialPinChange
    {
        CtsChanged = 0x08,
        DsrChanged = 0x10,
        CDChanged = 0x20,
        Ring = 0x100,
        Break = 0x40
    }
}
