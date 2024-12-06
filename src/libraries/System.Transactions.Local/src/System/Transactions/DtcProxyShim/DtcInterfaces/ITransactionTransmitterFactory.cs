// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms679232(v=vs.85)
[GeneratedComInterface, Guid("59313E00-B36C-11cf-A539-00AA006887C3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionTransmitterFactory
{
    void Create([MarshalAs(UnmanagedType.Interface)] out ITransactionTransmitter pTxTransmitter);
}
