// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms686073(v=vs.85)
[ComImport, Guid("0D563181-DEFB-11CE-AED1-00AA0051E2C4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IResourceManagerSink
{
    void TMDown();
}
