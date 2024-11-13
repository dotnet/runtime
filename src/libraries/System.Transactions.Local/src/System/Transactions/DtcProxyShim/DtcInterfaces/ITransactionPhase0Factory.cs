// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms682238(v=vs.85)
[GeneratedComInterface, Guid("82DC88E0-A954-11d1-8F88-00600895E7D5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionPhase0Factory
{
    void Create(
        [MarshalAs(UnmanagedType.Interface)] ITransactionPhase0NotifyAsync pITransactionPhase0Notify,
        [MarshalAs(UnmanagedType.Interface)] out ITransactionPhase0EnlistmentAsync ppITransactionPhase0Enlistment);
}
