// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Winsock
    {
        internal const string GetAddrInfoExCancelFunctionName = "GetAddrInfoExCancel";

        internal unsafe delegate void LPLOOKUPSERVICE_COMPLETION_ROUTINE([In] int dwError, [In] int dwBytes, [In] NativeOverlapped* lpOverlapped);

        [DllImport(Libraries.Ws2_32, ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern unsafe int GetAddrInfoExW(
            [In] string pName,
            [In] string pServiceName,
            [In] int dwNamespace,
            [In] IntPtr lpNspId,
            [In] ref AddressInfoEx pHints,
            [Out] out AddressInfoEx* ppResult,
            [In] IntPtr timeout,
            [In] ref NativeOverlapped lpOverlapped,
            [In] LPLOOKUPSERVICE_COMPLETION_ROUTINE lpCompletionRoutine,
            [Out] out IntPtr lpNameHandle
        );

        [DllImport(Libraries.Ws2_32, ExactSpelling = true)]
        internal static extern unsafe void FreeAddrInfoExW([In] AddressInfoEx* pAddrInfo);
    }
}
