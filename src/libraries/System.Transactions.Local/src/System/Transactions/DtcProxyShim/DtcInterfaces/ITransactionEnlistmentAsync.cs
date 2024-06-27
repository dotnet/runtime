// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms686429(v=vs.85)
[GeneratedComInterface, Guid("0fb15081-af41-11ce-bd2b-204c4f4f5020"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionEnlistmentAsync
{
    void PrepareRequestDone(int hr, IntPtr pmk, IntPtr pboidReason);

    void CommitRequestDone(int hr);

    void AbortRequestDone(int hr);
}
