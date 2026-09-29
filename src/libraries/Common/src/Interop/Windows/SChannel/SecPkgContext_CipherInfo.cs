// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Net
{
    // From Schannel.h
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct SecPkgContext_CipherInfo
    {
        private const int SZ_ALG_MAX_SIZE = 64;

        private readonly int dwVersion;
        private readonly int dwProtocol;
        public readonly int dwCipherSuite;
        private readonly int dwBaseCipherSuite;
        private AlgNameBuffer szCipherSuite;
        private AlgNameBuffer szCipher;
        private readonly int dwCipherLen;
        private readonly int dwCipherBlockLen; // in bytes
        private AlgNameBuffer szHash;
        private readonly int dwHashLen;
        private AlgNameBuffer szExchange;
        private readonly int dwMinExchangeLen;
        private readonly int dwMaxExchangeLen;
        private AlgNameBuffer szCertificate;
        private readonly int dwKeyType;

        [InlineArray(SZ_ALG_MAX_SIZE)]
        private struct AlgNameBuffer
        {
            private char _element0;
        }
    }
}
