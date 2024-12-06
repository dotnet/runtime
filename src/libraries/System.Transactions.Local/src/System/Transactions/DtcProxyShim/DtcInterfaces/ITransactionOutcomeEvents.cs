// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// Keep this type as a ComImport type as it is used with IConnectionPointContainer (and as a result needs to use built-in COM).
// https://learn.microsoft.com/previous-versions/windows/desktop/ms686465(v=vs.85)
[ComImport, Guid("3A6AD9E2-23B9-11cf-AD60-00AA00A74CCD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionOutcomeEvents
{
    void Committed(
        [MarshalAs(UnmanagedType.Bool)] bool fRetaining,
        IntPtr pNewUOW,
        int hresult);

    void Aborted(
        IntPtr pboidReason,
        [MarshalAs(UnmanagedType.Bool)] bool fRetaining,
        IntPtr pNewUOW,
        int hresult);

    void HeuristicDecision(
        OletxTransactionHeuristic dwDecision,
        IntPtr pboidReason,
        int hresult);

    void Indoubt();
}
