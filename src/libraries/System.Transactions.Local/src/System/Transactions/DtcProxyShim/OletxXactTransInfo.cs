// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim;

[ComVisible(false)]
[StructLayout(LayoutKind.Sequential)]
internal struct OletxXactTransInfo
{
    internal Guid Uow;
    internal OletxTransactionIsolationLevel IsoLevel;
    internal OletxTransactionIsoFlags IsoFlags;
    internal int GrfTCSupported;
    internal int GrfRMSupported;
    internal int GrfTCSupportedRetaining;
    internal int GrfRMSupportedRetaining;
}
