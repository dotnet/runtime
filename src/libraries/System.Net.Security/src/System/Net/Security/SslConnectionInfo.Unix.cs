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

            Debug.Assert(s_cipherToIndex.IndexOf((int)cipherSuite) > -1, $"No mapping found for cipherSuite {cipherSuite}");
            ulong data = s_encodedNumber[s_cipherToIndex[(int)cipherSuite + 1]];

            KeyExchangeAlg = (int)(data >> (64 - (16 * 1)) & 0xFFFF);
            DataCipherAlg = (int)(data >> (64 - (16 * 2)) & 0xFFFF);
            DataKeySize = (int)(data >> (64 - (16 * 3)) & 0xFFFF);
            DataHashAlg = (int)(data >> (64 - (16 * 4)) & 0xFFFF);
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

        private static ReadOnlySpan<int> s_cipherToIndex => new[]
        {
            (int)TlsCipherSuite.TLS_NULL_WITH_NULL_NULL, 0,
            (int)TlsCipherSuite.TLS_RSA_WITH_NULL_MD5, 1,
            (int)TlsCipherSuite.TLS_RSA_WITH_NULL_SHA, 2,
            (int)TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5, 3,
            (int)TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5, 4,
            (int)TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA, 5,
            (int)TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5, 6,
            (int)TlsCipherSuite.TLS_RSA_WITH_IDEA_CBC_SHA, 7,
            (int)TlsCipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA, 8,
            (int)TlsCipherSuite.TLS_RSA_WITH_DES_CBC_SHA, 9,
            (int)TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA, 10,
            (int)TlsCipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA, 11,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA, 12,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA, 13,
            (int)TlsCipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA, 14,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA, 15,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA, 16,
            (int)TlsCipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA, 17,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA, 18,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA, 19,
            (int)TlsCipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA, 20,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA, 21,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA, 22,
            (int)TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_RC4_40_MD5, 23,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5, 24,
            (int)TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA, 25,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_DES_CBC_SHA, 26,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA, 27,
            (int)TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_SHA, 28,
            (int)TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_SHA, 29,
            (int)TlsCipherSuite.TLS_KRB5_WITH_RC4_128_SHA, 30,
            (int)TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_SHA, 31,
            (int)TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_MD5, 32,
            (int)TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_MD5, 33,
            (int)TlsCipherSuite.TLS_KRB5_WITH_RC4_128_MD5, 34,
            (int)TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_MD5, 35,
            (int)TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA, 36,
            (int)TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA, 37,
            (int)TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_SHA, 38,
            (int)TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5, 39,
            (int)TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5, 40,
            (int)TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_MD5, 41,
            (int)TlsCipherSuite.TLS_PSK_WITH_NULL_SHA, 42,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA, 43,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA, 44,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA, 45,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA, 46,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA, 47,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA, 48,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA, 49,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA, 50,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA, 51,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA, 52,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA, 53,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA, 54,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA, 55,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA, 56,
            (int)TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256, 57,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256, 58,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256, 59,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256, 60,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256, 61,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256, 62,
            (int)TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA, 63,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA, 64,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA, 65,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA, 66,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA, 67,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA, 68,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256, 69,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256, 70,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256, 71,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256, 72,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256, 73,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256, 74,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256, 75,
            (int)TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA, 76,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA, 77,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA, 78,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA, 79,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA, 80,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA, 81,
            (int)TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA, 82,
            (int)TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA, 83,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA, 84,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA, 85,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA, 86,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA, 87,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA, 88,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA, 89,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA, 90,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA, 91,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA, 92,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA, 93,
            (int)TlsCipherSuite.TLS_RSA_WITH_SEED_CBC_SHA, 94,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA, 95,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA, 96,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA, 97,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA, 98,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_SEED_CBC_SHA, 99,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256, 100,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384, 101,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256, 102,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384, 103,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256, 104,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384, 105,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256, 106,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384, 107,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256, 108,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384, 109,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256, 110,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384, 111,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256, 112,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384, 113,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256, 114,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384, 115,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256, 116,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384, 117,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256, 118,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384, 119,
            (int)TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256, 120,
            (int)TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384, 121,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256, 122,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384, 123,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256, 124,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384, 125,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256, 126,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384, 127,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256, 128,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384, 129,
            (int)TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256, 130,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256, 131,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256, 132,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256, 133,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256, 134,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256, 135,
            (int)TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256, 136,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256, 137,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256, 138,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256, 139,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256, 140,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256, 141,
            (int)TlsCipherSuite.TLS_AES_128_GCM_SHA256, 142,
            (int)TlsCipherSuite.TLS_AES_256_GCM_SHA384, 143,
            (int)TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256, 144,
            (int)TlsCipherSuite.TLS_AES_128_CCM_SHA256, 145,
            (int)TlsCipherSuite.TLS_AES_128_CCM_8_SHA256, 146,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA, 147,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA, 148,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA, 149,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA, 150,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA, 151,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA, 152,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA, 153,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA, 154,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA, 155,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA, 156,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA, 157,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA, 158,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA, 159,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA, 160,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA, 161,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA, 162,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA, 163,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA, 164,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA, 165,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA, 166,
            (int)TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA, 167,
            (int)TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA, 168,
            (int)TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA, 169,
            (int)TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA, 170,
            (int)TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA, 171,
            (int)TlsCipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA, 172,
            (int)TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA, 173,
            (int)TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA, 174,
            (int)TlsCipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA, 175,
            (int)TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA, 176,
            (int)TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA, 177,
            (int)TlsCipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA, 178,
            (int)TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA, 179,
            (int)TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA, 180,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256, 181,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384, 182,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256, 183,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384, 184,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256, 185,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384, 186,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256, 187,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384, 188,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256, 189,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384, 190,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256, 191,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384, 192,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256, 193,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384, 194,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256, 195,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384, 196,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA, 197,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA, 198,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA, 199,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA, 200,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256, 201,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384, 202,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA, 203,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256, 204,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384, 205,
            (int)TlsCipherSuite.TLS_RSA_WITH_ARIA_128_CBC_SHA256, 206,
            (int)TlsCipherSuite.TLS_RSA_WITH_ARIA_256_CBC_SHA384, 207,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256, 208,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384, 209,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256, 210,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384, 211,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256, 212,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384, 213,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256, 214,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384, 215,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_CBC_SHA256, 216,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_CBC_SHA384, 217,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256, 218,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384, 219,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256, 220,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384, 221,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256, 222,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384, 223,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256, 224,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384, 225,
            (int)TlsCipherSuite.TLS_RSA_WITH_ARIA_128_GCM_SHA256, 226,
            (int)TlsCipherSuite.TLS_RSA_WITH_ARIA_256_GCM_SHA384, 227,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256, 228,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384, 229,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256, 230,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384, 231,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256, 232,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384, 233,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256, 234,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384, 235,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_GCM_SHA256, 236,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_GCM_SHA384, 237,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256, 238,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384, 239,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256, 240,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384, 241,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256, 242,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384, 243,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256, 244,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384, 245,
            (int)TlsCipherSuite.TLS_PSK_WITH_ARIA_128_CBC_SHA256, 246,
            (int)TlsCipherSuite.TLS_PSK_WITH_ARIA_256_CBC_SHA384, 247,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256, 248,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384, 249,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256, 250,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384, 251,
            (int)TlsCipherSuite.TLS_PSK_WITH_ARIA_128_GCM_SHA256, 252,
            (int)TlsCipherSuite.TLS_PSK_WITH_ARIA_256_GCM_SHA384, 253,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256, 254,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384, 255,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256, 256,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384, 257,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256, 258,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384, 259,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256, 260,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384, 261,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256, 262,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384, 263,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256, 264,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384, 265,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256, 266,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384, 267,
            (int)TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256, 268,
            (int)TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384, 269,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256, 270,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384, 271,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256, 272,
            (int)TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384, 273,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256, 274,
            (int)TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384, 275,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256, 276,
            (int)TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384, 277,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256, 278,
            (int)TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384, 279,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256, 280,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384, 281,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256, 282,
            (int)TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384, 283,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256, 284,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384, 285,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256, 286,
            (int)TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384, 287,
            (int)TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256, 288,
            (int)TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384, 289,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256, 290,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384, 291,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256, 292,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384, 293,
            (int)TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256, 294,
            (int)TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384, 295,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256, 296,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384, 297,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256, 298,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384, 299,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256, 300,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384, 301,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM, 302,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM, 303,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM, 304,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM, 305,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM_8, 306,
            (int)TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM_8, 307,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8, 308,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8, 309,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM, 310,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM, 311,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM, 312,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM, 313,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM_8, 314,
            (int)TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM_8, 315,
            (int)TlsCipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8, 316,
            (int)TlsCipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8, 317,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM, 318,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM, 319,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8, 320,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8, 321,
            (int)TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_GCM_SHA256, 322,
            (int)TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_GCM_SHA384, 323,
            (int)TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_CCM_SHA256, 324,
            (int)TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_CCM_SHA384, 325,
            (int)TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256, 326,
            (int)TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256, 327,
            (int)TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256, 328,
            (int)TlsCipherSuite.TLS_PSK_WITH_CHACHA20_POLY1305_SHA256, 329,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256, 330,
            (int)TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256, 331,
            (int)TlsCipherSuite.TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256, 332,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256, 333,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384, 334,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256, 335,
            (int)TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256, 336,
        };

        private static ReadOnlySpan<ulong> s_encodedNumber => new[]
        {
            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc2 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)56 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)56 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)56 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)56 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)56 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)56 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)56 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)56 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc2 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc2 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)40 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)168 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)0 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)256 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

            /* KeyExchangeAlg */ (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
            /* DataCipherAlg */ (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
            /* DataKeySize */ (ulong)128 << (64 - (16 * 3)) |
            /* DataHashAlg */ (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

        };
    }
}
