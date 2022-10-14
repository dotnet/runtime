// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms678954(v=vs.85)
[ComImport, Guid("0141fda5-8fc0-11ce-bd18-204c4f4f5020"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionExport
{
    void Export([MarshalAs(UnmanagedType.Interface)] ITransaction punkTransaction, out uint pcbTransactionCookie);

    void GetTransactionCookie(
        [MarshalAs(UnmanagedType.Interface)] ITransaction pITransaction,
        uint cbTransactionCookie,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] byte[] rgbTransactionCookie,
        out uint pcbUsed);
}
