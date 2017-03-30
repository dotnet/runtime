// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Ports
{
    public enum Parity
    {
        None = NativeMethods.NOPARITY,
        Odd = NativeMethods.ODDPARITY,
        Even = NativeMethods.EVENPARITY,
        Mark = NativeMethods.MARKPARITY,
        Space = NativeMethods.SPACEPARITY
    };
}

