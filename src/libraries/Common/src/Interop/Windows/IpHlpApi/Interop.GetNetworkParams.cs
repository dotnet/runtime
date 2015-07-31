// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        [DllImport(Interop.Libraries.IpHlpApi, ExactSpelling = true)]
        internal extern static uint GetNetworkParams(SafeLocalAllocHandle pFixedInfo, ref uint pOutBufLen);
    }
}
