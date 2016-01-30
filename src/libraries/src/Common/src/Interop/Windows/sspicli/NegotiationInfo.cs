// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
