// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file has been auto-generated. Do not edit by hand.
// Instead open Developer Command prompt and run: TextTransform FileName.tt
// Or set AllowTlsCipherSuiteGeneration=true and open VS and edit there directly

// This line is needed so that file compiles both as a T4 template and C# file

using System.Diagnostics;
using System.Security.Authentication;

namespace System.Net.Security
{
    internal partial struct SslConnectionInfo
    {
        private void MapCipherSuite(TlsCipherSuite cipherSuite)
        {
            TlsCipherSuite = cipherSuite;
            KeyExchKeySize = 0;

            int data1 = GetPackedData1(cipherSuite);
            Debug.Assert(data1 != 0, $"No mapping found for cipherSuite {cipherSuite}");

            int data2 = GetPackedData2(cipherSuite);
            Debug.Assert(data2 != 0, $"No mapping found for cipherSuite {cipherSuite}");

            KeyExchangeAlg = (data1 >> (32 - (16 * 1)) & 0xFFFF);
            DataCipherAlg = (data1 >> (32 - (16 * 2)) & 0xFFFF);
            DataKeySize = (data2 >> (32 - (16 * 3)) & 0xFFFF);
            DataHashAlg = (data2 >> (32 - (16 * 4)) & 0xFFFF);
            DataHashKeySize = GetHashSize((HashAlgorithmType)DataHashAlg);

            static int GetHashSize(HashAlgorithmType hash)
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

            static int GetPackedData1(TlsCipherSuite cipherSuite)
            {
                switch (cipherSuite)
                {
                    case TlsCipherSuite.TLS_NULL_WITH_NULL_NULL: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_NULL_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc2 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_IDEA_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_DES_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_RC4_40_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_DES_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc2 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Des << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc2 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_MD5: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_SEED_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_SEED_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_AES_128_CCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_AES_128_CCM_8_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Rc4 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.TripleDes << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Null << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_CCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_CCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_PSK_WITH_CHACHA20_POLY1305_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.None << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.RsaKeyX << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.None << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes256 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256: return
                        /* KeyExchangeAlg */ (int)ExchangeAlgorithmType.DiffieHellman << (32 - (16 * 1)) |
                        /* DataCipherAlg */ (int)CipherAlgorithmType.Aes128 << (32 - (16 * 2));

                    default: return 0;
                }
            }

            static int GetPackedData2(TlsCipherSuite cipherSuite)
            {
                switch (cipherSuite)
                {
                    case TlsCipherSuite.TLS_NULL_WITH_NULL_NULL: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_NULL_MD5: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_IDEA_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_DES_CBC_SHA: return
                        /* DataKeySize */ (int)56 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA: return
                        /* DataKeySize */ (int)56 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA: return
                        /* DataKeySize */ (int)56 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA: return
                        /* DataKeySize */ (int)56 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA: return
                        /* DataKeySize */ (int)56 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_RC4_40_MD5: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_DES_CBC_SHA: return
                        /* DataKeySize */ (int)56 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_SHA: return
                        /* DataKeySize */ (int)56 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_MD5: return
                        /* DataKeySize */ (int)56 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_MD5: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_MD5: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_MD5: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_SHA: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_MD5: return
                        /* DataKeySize */ (int)40 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Md5  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_SEED_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_SEED_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_AES_128_CCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_AES_128_CCM_8_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA: return
                        /* DataKeySize */ (int)168 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha1  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384: return
                        /* DataKeySize */ (int)0 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha256  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.Sha384  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM_8: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM_8: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM_8: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM_8: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_CCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_CCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_PSK_WITH_CHACHA20_POLY1305_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384: return
                        /* DataKeySize */ (int)256 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256: return
                        /* DataKeySize */ (int)128 << (32 - (16 * 3)) |
                        /* DataHashAlg */ (int)HashAlgorithmType.None  << (32 - (16 * 4));

                    default: return 0;
                }
            }
        }
    }
}
