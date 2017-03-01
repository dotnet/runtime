// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;

using TlsCipherSuite = Interop.AppleCrypto.TlsCipherSuite;

namespace System.Net.Security
{
    internal partial class SslConnectionInfo
    {
        private const ExchangeAlgorithmType EcDheAlgorithm = (ExchangeAlgorithmType)44550;

        public SslConnectionInfo(SafeSslHandle sslContext)
        {
            SslProtocols protocol;
            TlsCipherSuite cipherSuite;

            int osStatus = Interop.AppleCrypto.SslGetProtocolVersion(sslContext, out protocol);

            if (osStatus != 0)
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);

            osStatus = Interop.AppleCrypto.SslGetCipherSuite(sslContext, out cipherSuite);

            if (osStatus != 0)
                throw Interop.AppleCrypto.CreateExceptionForOSStatus(osStatus);

            Protocol = (int)protocol;

            MapCipherSuite(cipherSuite);
        }

        private void MapCipherSuite(TlsCipherSuite cipherSuite)
        {
            TlsMapping mapping;

            if (!s_tlsLookup.TryGetValue(cipherSuite, out mapping))
            {
                Debug.Fail($"No mapping found for cipherSuite {cipherSuite}");
            }

            KeyExchangeAlg = (int)mapping.KeyExchangeAlgorithm;
            KeyExchKeySize = 0;
            DataCipherAlg = (int)mapping.CipherAlgorithm;
            DataKeySize = mapping.CipherAlgorithmStrength;
            DataHashAlg = (int)mapping.HashAlgorithm;
            DataHashKeySize = (int)mapping.HashAlgorithmStrength;
        }

        private struct TlsMapping
        {
            internal ExchangeAlgorithmType KeyExchangeAlgorithm;
            // The Key Exchange size isn't part of the CipherSuite
            internal CipherAlgorithmType CipherAlgorithm;
            internal int CipherAlgorithmStrength;
            internal HashAlgorithmType HashAlgorithm;
            internal int HashAlgorithmStrength;

            internal static TlsMapping EcDhe(CipherAlgorithmType cipher, HashAlgorithmType hash)
            {
                int cipherSize;
                int hashSize;

                switch (cipher)
                {
                    case CipherAlgorithmType.Aes128:
                        cipherSize = 128;
                        break;
                    case CipherAlgorithmType.Aes192:
                        cipherSize = 192;
                        break;
                    case CipherAlgorithmType.Aes256:
                        cipherSize = 256;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cipher));
                }

                switch (hash)
                {
                    case HashAlgorithmType.Sha1:
                        hashSize = 160;
                        break;
                    case HashAlgorithmType.Sha256:
                        hashSize = 256;
                        break;
                    case HashAlgorithmType.Sha384:
                        hashSize = 384;
                        break;
                    case HashAlgorithmType.Sha512:
                        hashSize = 512;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(hash));
                }

                return new TlsMapping
                {
                    KeyExchangeAlgorithm = EcDheAlgorithm,
                    CipherAlgorithm = cipher,
                    CipherAlgorithmStrength = cipherSize,
                    HashAlgorithm = hash,
                    HashAlgorithmStrength = hashSize,
                };
            }
        }

        private static readonly Dictionary<TlsCipherSuite, TlsMapping> s_tlsLookup = new Dictionary<TlsCipherSuite, TlsMapping>
        {
            {
                TlsCipherSuite.TLS_NULL_WITH_NULL_NULL,
                new TlsMapping()
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
                TlsMapping.EcDhe(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
                TlsMapping.EcDhe(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.EcDhe(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                TlsMapping.EcDhe(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.EcDhe(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },
        };
    }
}
