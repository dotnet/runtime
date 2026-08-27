// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_CHAIN_ENGINE_CONFIG
        {
            public int cbSize;
            public IntPtr hRestrictedRoot;
            public IntPtr hRestrictedTrust;
            public IntPtr hRestrictedOther;
            public int cAdditionalStore;
            public IntPtr rghAdditionalStore;
            public ChainEngineConfigFlags dwFlags;
            public int dwUrlRetrievalTimeout;
            public int MaximumCachedCertificates;
            public int CycleDetectionModulus;
            public IntPtr hExclusiveRoot;
            public IntPtr hExclusiveTrustedPeople;
        }
    }
}
