// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms686771(v=vs.85)
[GeneratedComInterface, Guid("E1CF9B53-8745-11ce-A9BA-00AA006C3706"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionExportFactory
{
    void GetRemoteClassId(out Guid pclsid);

    void Create(
        uint cbWhereabouts,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbWhereabouts,
        [MarshalAs(UnmanagedType.Interface)] out ITransactionExport ppExport);
}
