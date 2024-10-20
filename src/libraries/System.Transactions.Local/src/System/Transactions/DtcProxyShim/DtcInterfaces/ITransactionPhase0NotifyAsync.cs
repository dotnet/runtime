// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms686106(v=vs.85)
[GeneratedComInterface, Guid("EF081809-0C76-11d2-87A6-00C04F990F34"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionPhase0NotifyAsync
{
    void Phase0Request([MarshalAs(UnmanagedType.Bool)] bool fAbortHint);

    void EnlistCompleted(int status);
}
