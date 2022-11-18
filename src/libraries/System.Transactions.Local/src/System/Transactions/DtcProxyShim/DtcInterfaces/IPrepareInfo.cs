// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms686533(v=vs.85)
[ComImport, Guid("80c7bfd0-87ee-11ce-8081-0080c758527e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPrepareInfo
{
    void GetPrepareInfoSize(out uint pcbPrepInfo);

    void GetPrepareInfo([MarshalAs(UnmanagedType.LPArray), Out] byte[] pPrepInfo);
}
