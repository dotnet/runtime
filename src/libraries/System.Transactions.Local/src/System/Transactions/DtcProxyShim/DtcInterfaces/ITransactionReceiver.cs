// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://docs.microsoft.com/previous-versions/windows/desktop/ms679193(v=vs.85)
[ComImport, Guid("59313E03-B36C-11cf-A539-00AA006887C3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITransactionReceiver
{
    void UnmarshalPropagationToken(
        uint cbToken,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] byte[] rgbToken,
        [MarshalAs(UnmanagedType.Interface)] out ITransaction ppTransaction);

    void GetReturnTokenSize(out uint pcbReturnToken);

    void MarshalReturnToken(
        uint cbReturnToken,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0), Out] out byte[] rgbReturnToken,
        out uint pcbUsed);

    void Reset();
}
