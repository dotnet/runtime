﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms684377(v=vs.85)
[ComImport, Guid("02656950-2152-11d0-944C-00A0C905416E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionCloner
{
    void Commit(
        [MarshalAs(UnmanagedType.Bool)] bool fRetainingt,
        [MarshalAs(UnmanagedType.U4)] OletxXacttc grfTC,
        uint grfRM);

    void Abort(
        IntPtr reason,
        [MarshalAs(UnmanagedType.Bool)] bool retaining,
        [MarshalAs(UnmanagedType.Bool)] bool async);

    void GetTransactionInfo(out OletxXactTransInfo xactInfo);

    void CloneWithCommitDisabled([MarshalAs(UnmanagedType.Interface)] out ITransaction ppITransaction);
}
