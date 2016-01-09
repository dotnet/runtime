// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Net
{
    // SecPkgContext_NegotiationInfoW in sspi.h.
    [StructLayout(LayoutKind.Sequential)]
    internal struct NegotiationInfo
    {
        internal IntPtr PackageInfo;
        internal uint NegotiationState;
        internal static readonly int Size = Marshal.SizeOf<NegotiationInfo>();
        internal static readonly int NegotiationStateOffest = (int)Marshal.OffsetOf<NegotiationInfo>("NegotiationState");
    }
}
