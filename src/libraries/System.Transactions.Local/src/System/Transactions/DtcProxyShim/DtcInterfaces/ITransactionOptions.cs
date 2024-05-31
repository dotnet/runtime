// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace System.Transactions.DtcProxyShim.DtcInterfaces;

// https://learn.microsoft.com/previous-versions/windows/desktop/ms686489(v=vs.85)
[GeneratedComInterface, Guid("3A6AD9E0-23B9-11cf-AD60-00AA00A74CCD"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface ITransactionOptions
{
    void SetOptions(Xactopt pOptions);

    void GetOptions();
}
