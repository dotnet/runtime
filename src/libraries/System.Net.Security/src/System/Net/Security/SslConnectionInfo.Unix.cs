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

            ushort data = GetPackedData(cipherSuite);
            Debug.Assert(data != 0, $"No mapping found for cipherSuite {cipherSuite}");

            KeyExchangeAlg = (int)s_exchangeAlgorithmTypes[(data >> (16 - (4 * 1)) & 0xF)];
            DataCipherAlg = (int)s_cipherEnumValues[(data >> (16 - (4 * 2)) & 0xF)];
            DataKeySize = (int)s_cipherStrengthEnumValues[(data >> (16 - (4 * 3)) & 0xF)];
            DataHashAlg = (int)s_hashEnumValues[(data >> (16 - (4 * 4)) & 0xF)];
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
        }

        private static ReadOnlySpan<int> s_exchangeAlgorithmTypes =>
            new[] { (int)ExchangeAlgorithmType.None, (int)ExchangeAlgorithmType.RsaSign, (int)ExchangeAlgorithmType.RsaKeyX, (int)ExchangeAlgorithmType.DiffieHellman, };

        private static ReadOnlySpan<int> s_cipherEnumValues =>
            new[] { (int)CipherAlgorithmType.None, (int)CipherAlgorithmType.Null, (int)CipherAlgorithmType.Des, (int)CipherAlgorithmType.Rc2, (int)CipherAlgorithmType.TripleDes, (int)CipherAlgorithmType.Aes128, (int)CipherAlgorithmType.Aes192, (int)CipherAlgorithmType.Aes256, (int)CipherAlgorithmType.Aes, (int)CipherAlgorithmType.Rc4, };

        private static ReadOnlySpan<int> s_cipherStrengthEnumValues =>
            new[] { (int)CipherAlgorithmStrength.Zero, (int)CipherAlgorithmStrength.Forty, (int)CipherAlgorithmStrength.FiftySix, (int)CipherAlgorithmStrength.OneTwentyEight, (int)CipherAlgorithmStrength.OneSixtyEight, (int)CipherAlgorithmStrength.TwoFiftySix, };

        private static ReadOnlySpan<int> s_hashEnumValues =>
            new[] { (int)HashAlgorithmType.None, (int)HashAlgorithmType.Md5, (int)HashAlgorithmType.Sha1, (int)HashAlgorithmType.Sha256, (int)HashAlgorithmType.Sha384, (int)HashAlgorithmType.Sha512, };

        private static ushort GetPackedData(TlsCipherSuite cipherSuite)
        {
            switch (cipherSuite)
            {
                case TlsCipherSuite.TLS_NULL_WITH_NULL_NULL: return 0 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_NULL_MD5: return 2 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA: return 2 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5: return 2 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5: return 2 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA: return 2 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5: return 2 << (16 - (4 * 1)) | 3 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_IDEA_CBC_SHA: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA: return 2 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_DES_CBC_SHA: return 2 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 2 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA: return 2 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 2 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 2 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 2 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 2 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_RC4_40_MD5: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_DES_CBC_SHA: return 3 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 2 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_SHA: return 0 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 2 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_SHA: return 0 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_SHA: return 0 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_SHA: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_MD5: return 0 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 2 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_MD5: return 0 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_MD5: return 0 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_MD5: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA: return 0 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA: return 0 << (16 - (4 * 1)) | 3 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_SHA: return 0 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5: return 0 << (16 - (4 * 1)) | 2 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5: return 0 << (16 - (4 * 1)) | 3 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_MD5: return 0 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 1 << (16 - (4 * 3)) | 1 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA: return 0 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA: return 2 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA: return 2 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA: return 2 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256: return 2 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256: return 2 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256: return 2 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA: return 0 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA: return 0 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA: return 2 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA: return 2 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA: return 2 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA: return 2 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_SEED_CBC_SHA: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_SEED_CBC_SHA: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256: return 2 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384: return 2 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256: return 2 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384: return 2 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256: return 0 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384: return 0 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256: return 2 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384: return 2 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256: return 2 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384: return 2 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_AES_128_GCM_SHA256: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_AES_256_GCM_SHA384: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_AES_128_CCM_SHA256: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_AES_128_CCM_8_SHA256: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA: return 0 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA: return 0 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA: return 0 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA: return 3 << (16 - (4 * 1)) | 9 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA: return 3 << (16 - (4 * 1)) | 4 << (16 - (4 * 2)) | 4 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 2 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384: return 3 << (16 - (4 * 1)) | 1 << (16 - (4 * 2)) | 0 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_CBC_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_CBC_SHA384: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_GCM_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_GCM_SHA384: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_CBC_SHA256: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_CBC_SHA384: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_GCM_SHA256: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_GCM_SHA384: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 3 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 4 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM: return 2 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM: return 2 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM_8: return 2 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM_8: return 2 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM_8: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM_8: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_GCM_SHA256: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_GCM_SHA384: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_CCM_SHA256: return 0 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_CCM_SHA384: return 0 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_PSK_WITH_CHACHA20_POLY1305_SHA256: return 0 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256: return 3 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256: return 2 << (16 - (4 * 1)) | 0 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384: return 3 << (16 - (4 * 1)) | 7 << (16 - (4 * 2)) | 5 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256: return 3 << (16 - (4 * 1)) | 5 << (16 - (4 * 2)) | 3 << (16 - (4 * 3)) | 0 << (16 - (4 * 4));
                default: return 0;
            }
        }

        private enum CipherAlgorithmStrength
        {
            Zero = 0,
            Forty = 40,
            FiftySix = 56,
            OneTwentyEight = 128,
            OneSixtyEight = 168,
            TwoFiftySix = 256
        }
    }
}
