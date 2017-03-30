// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Ports
{
    public enum SerialPinChange
    {
        CtsChanged = NativeMethods.EV_CTS,
        DsrChanged = NativeMethods.EV_DSR,
        CDChanged = NativeMethods.EV_RLSD,
        Ring = NativeMethods.EV_RING,
        Break = NativeMethods.EV_BREAK
    }
}
