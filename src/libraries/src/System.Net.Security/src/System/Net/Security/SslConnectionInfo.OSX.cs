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
        private const int LookupMapCount = 133;

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa375549(v=vs.85).aspx
        // CALG_DH_SF
        private const ExchangeAlgorithmType DiffieHellmanStatic = (ExchangeAlgorithmType)0xAA01;
        // CALG_ECDH
        private const ExchangeAlgorithmType EcDhAlgorithm = (ExchangeAlgorithmType)0xAA05;
        // CALG_ECDH_EPHEM
        private const ExchangeAlgorithmType EcDheAlgorithm = (ExchangeAlgorithmType)0xAA06;

#if DEBUG
        static SslConnectionInfo()
        {
            Debug.Assert(
                s_tlsLookup.Count == LookupMapCount,
                $"Lookup dictionary was of size {s_tlsLookup.Count} instead of {LookupMapCount}");

            Array enumValues = Enum.GetValues(typeof(TlsCipherSuite));

            foreach (TlsCipherSuite val in enumValues)
            {
                Debug.Assert(s_tlsLookup.ContainsKey(val), $"No mapping found for {val} ({(int)val})");
            }
        }
#endif

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

            internal static TlsMapping DhEphem(CipherAlgorithmType cipher, HashAlgorithmType hash) =>
                Build(ExchangeAlgorithmType.DiffieHellman, cipher, GetCipherSize(cipher), hash);

            internal static TlsMapping DhEphem(CipherAlgorithmType cipher, int cipherSize, HashAlgorithmType hash) =>
                Build(ExchangeAlgorithmType.DiffieHellman, cipher, cipherSize, hash);

            internal static TlsMapping DhStatic(CipherAlgorithmType cipher, HashAlgorithmType hash) =>
                Build(DiffieHellmanStatic, cipher, GetCipherSize(cipher), hash);

            internal static TlsMapping DhStatic(CipherAlgorithmType cipher, int cipherSize, HashAlgorithmType hash) =>
                Build(DiffieHellmanStatic, cipher, cipherSize, hash);

            internal static TlsMapping EcDhEphem(CipherAlgorithmType cipher, HashAlgorithmType hash) =>
                Build(EcDheAlgorithm, cipher, GetCipherSize(cipher), hash);

            internal static TlsMapping EcDhEphem(CipherAlgorithmType cipher, int cipherSize, HashAlgorithmType hash) =>
                Build(EcDheAlgorithm, cipher, cipherSize, hash);

            internal static TlsMapping EcDhStatic(CipherAlgorithmType cipher, HashAlgorithmType hash) =>
                Build(EcDhAlgorithm, cipher, GetCipherSize(cipher), hash);

            internal static TlsMapping EcDhStatic(CipherAlgorithmType cipher, int cipherSize, HashAlgorithmType hash) =>
                Build(EcDhAlgorithm, cipher, cipherSize, hash);

            internal static TlsMapping NoExchange(CipherAlgorithmType cipher, HashAlgorithmType hash) =>
                Build(ExchangeAlgorithmType.None, cipher, GetCipherSize(cipher), hash);

            internal static TlsMapping NoExchange(CipherAlgorithmType cipher, int cipherSize, HashAlgorithmType hash) =>
                Build(ExchangeAlgorithmType.None, cipher, cipherSize, hash);

            internal static TlsMapping Rsa(CipherAlgorithmType cipher, HashAlgorithmType hash) =>
                Build(ExchangeAlgorithmType.RsaKeyX, cipher, GetCipherSize(cipher), hash);

            internal static TlsMapping Rsa(CipherAlgorithmType cipher, int cipherSize, HashAlgorithmType hash) =>
                Build(ExchangeAlgorithmType.RsaKeyX, cipher, cipherSize, hash);

            private static TlsMapping Build(
                ExchangeAlgorithmType exchange,
                CipherAlgorithmType cipher,
                int cipherSize,
                HashAlgorithmType hash)
            {
                int hashSize = GetHashSize(hash);

                return new TlsMapping
                {
                    KeyExchangeAlgorithm = exchange,
                    CipherAlgorithm = cipher,
                    CipherAlgorithmStrength = cipherSize,
                    HashAlgorithm = hash,
                    HashAlgorithmStrength = hashSize,
                };
            }

            private static int GetHashSize(HashAlgorithmType hash)
            {
                switch (hash)
                {
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

            private static int GetCipherSize(CipherAlgorithmType cipher)
            {
                switch (cipher)
                {
                    case CipherAlgorithmType.None:
                    case CipherAlgorithmType.Null:
                        return 0;
                    case CipherAlgorithmType.Rc2:
                        Debug.Fail($"RC2 should always include the keysize");
                        return 0;
                    case CipherAlgorithmType.Des:
                        return 56;
                    case CipherAlgorithmType.Rc4:
                        Debug.Fail($"RC4 should always include the keysize");
                        return 0;
                    case CipherAlgorithmType.TripleDes:
                        return 168;
                    case CipherAlgorithmType.Aes128:
                        return 128;
                    case CipherAlgorithmType.Aes192:
                        return 192;
                    case CipherAlgorithmType.Aes256:
                        return 256;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(cipher));
                }
            }
        }

        private static readonly Dictionary<TlsCipherSuite, TlsMapping> s_tlsLookup =
            new Dictionary<TlsCipherSuite, TlsMapping>(LookupMapCount)
        {
            {
                TlsCipherSuite.TLS_NULL_WITH_NULL_NULL,
                new TlsMapping()
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_NULL_MD5,
                TlsMapping.Rsa(CipherAlgorithmType.None, HashAlgorithmType.Md5)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_NULL_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.None, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_RSA_EXPORT_WITH_RC4_40_MD5,
                TlsMapping.Rsa(CipherAlgorithmType.Rc4, 40, HashAlgorithmType.Md5)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5,
                TlsMapping.Rsa(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Md5)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_RSA_EXPORT_WITH_RC2_CBC_40_MD5,
                TlsMapping.Rsa(CipherAlgorithmType.Rc2, 40, HashAlgorithmType.Md5)
            },

            {
                TlsCipherSuite.SSL_RSA_EXPORT_WITH_DES40_CBC_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.Des, 40, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_RSA_WITH_DES_CBC_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.Des, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DH_DSS_EXPORT_WITH_DES40_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Des, 40, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DH_DSS_WITH_DES_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Des, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DH_RSA_EXPORT_WITH_DES40_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Des, 40, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DH_RSA_WITH_DES_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Des, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Des, 40, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DHE_DSS_WITH_DES_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Des, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Des, 40, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DHE_RSA_WITH_DES_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Des, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DH_anon_EXPORT_WITH_RC4_40_MD5,
                TlsMapping.DhStatic(CipherAlgorithmType.Rc4, 40, HashAlgorithmType.Md5)
            },

            {
                TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5,
                TlsMapping.DhStatic(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Md5)
            },

            {
                TlsCipherSuite.SSL_DH_anon_EXPORT_WITH_DES40_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Des, 40, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.SSL_DH_anon_WITH_DES_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Des, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_NULL_SHA,
                TlsMapping.NoExchange(CipherAlgorithmType.None, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.None, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.None, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256,
                TlsMapping.Rsa(CipherAlgorithmType.None, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
                TlsMapping.Rsa(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256,
                TlsMapping.Rsa(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA,
                TlsMapping.NoExchange(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.NoExchange(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA,
                TlsMapping.NoExchange(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA,
                TlsMapping.NoExchange(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA,
                TlsMapping.Rsa(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
                TlsMapping.Rsa(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.Rsa(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384,
                TlsMapping.DhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256,
                TlsMapping.NoExchange(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384,
                TlsMapping.NoExchange(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256,
                TlsMapping.Rsa(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384,
                TlsMapping.Rsa(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256,
                TlsMapping.NoExchange(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384,
                TlsMapping.NoExchange(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256,
                TlsMapping.NoExchange(CipherAlgorithmType.None, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384,
                TlsMapping.NoExchange(CipherAlgorithmType.None, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384,
                TlsMapping.DhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256,
                TlsMapping.DhEphem(CipherAlgorithmType.None, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384,
                TlsMapping.DhEphem(CipherAlgorithmType.None, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256,
                TlsMapping.Rsa(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384,
                TlsMapping.Rsa(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256,
                TlsMapping.Rsa(CipherAlgorithmType.None, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384,
                TlsMapping.Rsa(CipherAlgorithmType.None, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.None, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA,
                TlsMapping.EcDhEphem(CipherAlgorithmType.None, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.EcDhEphem(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.None, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.None, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Rc4, 128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.TripleDes, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha1)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },
            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.EcDhEphem(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },

            {
                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes128, HashAlgorithmType.Sha256)
            },

            {
                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384,
                TlsMapping.EcDhStatic(CipherAlgorithmType.Aes256, HashAlgorithmType.Sha384)
            },
        };
    }
}
