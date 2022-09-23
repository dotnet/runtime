// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms687604(v=vs.85)
[ComImport, Guid(Guids.IID_ITransactionDispenser), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionDispenser
{
    void GetOptionsObject([MarshalAs(UnmanagedType.Interface)] out ITransactionOptions ppOptions);

    void BeginTransaction(
        IntPtr punkOuter,
        [MarshalAs(UnmanagedType.I4)] OletxTransactionIsolationLevel isoLevel,
        [MarshalAs(UnmanagedType.I4)] OletxTransactionIsoFlags isoFlags,
        [MarshalAs(UnmanagedType.Interface)] ITransactionOptions pOptions,
        [MarshalAs(UnmanagedType.Interface)] out ITransaction ppTransaction);
}
