// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms686084(v=vs.85)
[ComImport, Guid("5433376A-414D-11d3-B206-00C04FC2F3EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionVoterFactory2
{
    void Create(
        [MarshalAs(UnmanagedType.Interface)] ITransaction pITransaction,
        [MarshalAs(UnmanagedType.Interface)] ITransactionVoterNotifyAsync2 pVoterNotify,
        [MarshalAs(UnmanagedType.Interface)] out ITransactionVoterBallotAsync2 ppVoterBallot);
}
