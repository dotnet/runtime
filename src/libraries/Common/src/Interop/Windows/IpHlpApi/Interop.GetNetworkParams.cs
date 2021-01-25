// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        [DllImport(Interop.Libraries.IpHlpApi, ExactSpelling = true)]
        internal static extern uint GetNetworkParams(IntPtr pFixedInfo, ref uint pOutBufLen);
    }
}
