// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Transactions.DtcProxyShim;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms687604(v=vs.85)
[GeneratedComInterface, Guid(Guids.IID_ITransactionDispenser), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionDispenser
{
    void GetOptionsObject([MarshalAs(UnmanagedType.Interface)] out ITransactionOptions ppOptions);

    void BeginTransaction(
        IntPtr punkOuter,
        OletxTransactionIsolationLevel isoLevel,
        OletxTransactionIsoFlags isoFlags,
        [MarshalAs(UnmanagedType.Interface)] ITransactionOptions pOptions,
        [MarshalAs(UnmanagedType.Interface)] out ITransaction ppTransaction);
}
