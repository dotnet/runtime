// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms686531(v=vs.85)
[GeneratedComInterface, Guid(Guids.IID_ITransaction), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransaction
{
    void Commit(
        [MarshalAs(UnmanagedType.Bool)] bool fRetaining,
        OletxXacttc grfTC,
        uint grfRM);

    void Abort(
        IntPtr reason,
        [MarshalAs(UnmanagedType.Bool)] bool retaining,
        [MarshalAs(UnmanagedType.Bool)] bool async);

    void GetTransactionInfo(out OletxXactTransInfo xactInfo);
}
