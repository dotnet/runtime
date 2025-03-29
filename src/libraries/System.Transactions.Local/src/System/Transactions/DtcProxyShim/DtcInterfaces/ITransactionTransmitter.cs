// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms682296(v=vs.85)
[GeneratedComInterface, Guid("59313E01-B36C-11cf-A539-00AA006887C3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionTransmitter
{
    void Set([MarshalAs(UnmanagedType.Interface)] ITransaction transaction);

    void GetPropagationTokenSize(out uint pcbToken);

    void MarshalPropagationToken(
        uint cbToken,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbToken,
        out uint pcbUsed);

    void UnmarshalReturnToken(
        uint cbReturnToken,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbToken);

    void Reset();
}
