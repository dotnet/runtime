// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Transactions;
using System.Transactions.DtcProxyShim.DtcInterfaces;

internal static partial class Interop
{
    internal static partial class Xolehlp
    {
        // https://learn.microsoft.com/previous-versions/windows/desktop/ms678898(v=vs.85)
        [LibraryImport(Libraries.Xolehlp, StringMarshalling = StringMarshalling.Utf16)]
        [RequiresUnreferencedCode(TransactionManager.DistributedTransactionTrimmingWarning)]
        internal static unsafe partial int DtcGetTransactionManagerExW(
            [MarshalAs(UnmanagedType.LPWStr)] string? pszHost,
            [MarshalAs(UnmanagedType.LPWStr)] string? pszTmName,
            in Guid riid,
            int grfOptions,
            void* pvConfigPararms,
            [MarshalAs(UnmanagedType.Interface)] out ITransactionDispenser ppvObject);
    }
}
