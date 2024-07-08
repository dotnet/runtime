// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms678930(v=vs.85)
[GeneratedComInterface, Guid("5433376B-414D-11d3-B206-00C04FC2F3EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionVoterNotifyAsync2
{
    void Committed(
        [MarshalAs(UnmanagedType.Bool)] bool fRetaining,
        IntPtr pNewUOW,
        uint hresult);

    void Aborted(
        IntPtr pboidReason,
        [MarshalAs(UnmanagedType.Bool)] bool fRetaining,
        IntPtr pNewUOW,
        uint hresult);

    void HeuristicDecision(
        OletxTransactionHeuristic dwDecision,
        IntPtr pboidReason,
        uint hresult);

    void Indoubt();

    void VoteRequest();
}
