// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms678823(v=vs.85)
[GeneratedComInterface, Guid("69E971F0-23CE-11cf-AD60-00AA00A74CCD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionResourceAsync
{
    void PrepareRequest(
        [MarshalAs(UnmanagedType.Bool)] bool fRetaining,
        OletxXactRm grfRM,
        [MarshalAs(UnmanagedType.Bool)] bool fWantMoniker,
        [MarshalAs(UnmanagedType.Bool)] bool fSinglePhase);

    void CommitRequest(OletxXactRm grfRM, IntPtr pNewUOW);

    void AbortRequest(
        IntPtr pboidReason,
        [MarshalAs(UnmanagedType.Bool)] bool fRetaining,
        IntPtr pNewUOW);

    void TMDown();
}
