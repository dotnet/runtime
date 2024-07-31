// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms686073(v=vs.85)
[GeneratedComInterface, Guid("0D563181-DEFB-11CE-AED1-00AA0051E2C4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IResourceManagerSink
{
    void TMDown();
}
