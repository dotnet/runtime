// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        [LibraryImport(Libraries.Crypt32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CertGetCertificateChain(
            IntPtr hChainEngine,
            SafeCertContextHandle pCertContext,
            FILETIME* pTime,
            SafeCertStoreHandle hStore,
            ref CERT_CHAIN_PARA pChainPara,
            CertChainFlags dwFlags,
            IntPtr pvReserved,
            out SafeX509ChainHandle ppChainContext);

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CERT_CHAIN_PARA
        {
            public int cbSize;
            public CERT_USAGE_MATCH RequestedUsage;
            public CERT_USAGE_MATCH RequestedIssuancePolicy;
            public int dwUrlRetrievalTimeout;
            public int fCheckRevocationFreshnessTime;
            public int dwRevocationFreshnessTime;
            public FILETIME* pftCacheResync;
            public int pStrongSignPara;
            public int dwStrongSignFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CERT_USAGE_MATCH
        {
            public CertUsageMatchType dwType;
            public CTL_USAGE Usage;
        }

        internal enum CertUsageMatchType : int
        {
            USAGE_MATCH_TYPE_AND = 0x00000000,
            USAGE_MATCH_TYPE_OR = 0x00000001,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CTL_USAGE
        {
            public int cUsageIdentifier;
            public IntPtr rgpszUsageIdentifier;
        }
    }
}
