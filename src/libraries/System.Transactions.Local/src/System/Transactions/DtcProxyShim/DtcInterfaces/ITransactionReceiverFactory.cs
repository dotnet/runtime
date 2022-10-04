// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms683577(v=vs.85)
[ComImport, Guid("59313E02-B36C-11cf-A539-00AA006887C3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionReceiverFactory
{
    void Create([MarshalAs(UnmanagedType.Interface)] out ITransactionReceiver pTxReceiver);
}
