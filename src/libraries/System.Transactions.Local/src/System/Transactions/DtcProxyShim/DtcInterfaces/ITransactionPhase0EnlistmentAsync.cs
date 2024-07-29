// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms685087(v=vs.85).</remarks
[GeneratedComInterface, Guid("82DC88E1-A954-11d1-8F88-00600895E7D5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionPhase0EnlistmentAsync
{
    void Enable();

    void WaitForEnlistment();

    void Phase0Done();

    void Unenlist();

    void GetTransaction([MarshalAs(UnmanagedType.Interface)] out ITransaction ppITransaction);
}
