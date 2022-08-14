// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms679195(v=vs.85)
[StructLayout(LayoutKind.Sequential)]
internal struct Xactopt
{
    internal Xactopt(uint ulTimeout, string szDescription)
        => (UlTimeout, SzDescription) = (ulTimeout, szDescription);

    public uint UlTimeout;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    public string SzDescription;
}
