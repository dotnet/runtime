// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms686533(v=vs.85)
[GeneratedComInterface, Guid("80c7bfd0-87ee-11ce-8081-0080c758527e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IPrepareInfo
{
    void GetPrepareInfoSize(out uint pcbPrepInfo);

    void GetPrepareInfo([MarshalAs(UnmanagedType.LPArray)] byte[] pPrepInfo);
}
