// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms682783(v=vs.85)
[ComImport, Guid("0141fda4-8fc0-11ce-bd18-204c4f4f5020"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionImportWhereabouts
{
    internal void GetWhereaboutsSize(out uint pcbSize);

    internal void GetWhereabouts(
        uint cbWhereabouts,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] byte[] rgbWhereabouts,
        out uint pcbUsed);
}
