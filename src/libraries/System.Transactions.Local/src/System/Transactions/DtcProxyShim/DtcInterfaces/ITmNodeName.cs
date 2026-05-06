// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms687122(v=vs.85)
[GeneratedComInterface, Guid("30274F88-6EE4-474e-9B95-7807BC9EF8CF"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITmNodeName
{
    internal void GetNodeNameSize(out uint pcbNodeNameSize);

    internal void GetNodeName(uint cbNodeNameBufferSize, [MarshalAs(UnmanagedType.LPWStr)] out string pcbNodeSize);
}
