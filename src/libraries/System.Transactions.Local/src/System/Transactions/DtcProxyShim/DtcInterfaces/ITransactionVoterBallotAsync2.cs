// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms680565(v=vs.85)
[GeneratedComInterface, Guid("5433376C-414D-11d3-B206-00C04FC2F3EF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionVoterBallotAsync2
{
    void VoteRequestDone(int hr, IntPtr pboidReason);
}
