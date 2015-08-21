// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.Net.Sockets
{
#if DEBUG
    internal sealed class SafeFreeAddrInfo : DebugSafeHandle
    {
#else
    internal sealed class SafeFreeAddrInfo : SafeHandleZeroOrMinusOneIsInvalid {
#endif
        private SafeFreeAddrInfo() : base(true) { }

        internal static int GetAddrInfo(string nodename, string servicename, ref AddressInfo hints, out SafeFreeAddrInfo outAddrInfo)
        {
            return Interop.Winsock.GetAddrInfoW(nodename, servicename, ref hints, out outAddrInfo);
        }

        override protected bool ReleaseHandle()
        {
            Interop.Winsock.freeaddrinfo(handle);
            return true;
        }
    }
}
