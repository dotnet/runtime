// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Authentication;

namespace System.Net.Security
{
    internal partial struct TlsCipherSuiteData
    {
        internal ExchangeAlgorithmType KeyExchangeAlgorithm;
        // The Key Exchange size isn't part of the CipherSuite
        internal CipherAlgorithmType CipherAlgorithm;
        internal int CipherAlgorithmStrength;
        internal HashAlgorithmType MACAlgorithm;
        internal int MACAlgorithmStrength => GetHashSize(MACAlgorithm);

#if DEBUG
        static TlsCipherSuiteData()
        {
            Debug.Assert(
                s_tlsLookup.Count == LookupCount,
                $"Lookup dictionary was of size {s_tlsLookup.Count} instead of {LookupCount}");

            foreach (TlsCipherSuite val in Enum.GetValues(typeof(TlsCipherSuite)))
            {
                Debug.Assert(s_tlsLookup.ContainsKey(val), $"No mapping found for {val} ({(int)val})");
            }
        }

        public override string ToString()
        {
            return $"Kx={KeyExchangeAlgorithm} Enc={CipherAlgorithm} [{CipherAlgorithmStrength}] Mac={MACAlgorithm} [{MACAlgorithmStrength}]";
        }
#endif

        public static TlsCipherSuiteData GetCipherSuiteData(TlsCipherSuite cipherSuite)
        {
            if (s_tlsLookup.TryGetValue(cipherSuite, out TlsCipherSuiteData mapping))
            {
                return mapping;
            }

            Debug.Fail($"No mapping found for cipherSuite {cipherSuite}");
            return default(TlsCipherSuiteData);
        }

        private static int GetHashSize(HashAlgorithmType hash)
        {
            switch (hash)
            {
                case HashAlgorithmType.None:
                     return 0;
                case HashAlgorithmType.Md5:
                    return 128;
                case HashAlgorithmType.Sha1:
                    return 160;
                case HashAlgorithmType.Sha256:
                    return 256;
                case HashAlgorithmType.Sha384:
                    return 384;
                case HashAlgorithmType.Sha512:
                    return 512;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hash));
            }
        }
    }
}
