// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file has been auto-generated. Do not edit by hand.
// Instead open Developer Command prompt and run: TextTransform FileName.tt
// Or set AllowTlsCipherSuiteGeneration=true and open VS and edit there directly

// This line is needed so that file compiles both as a T4 template and C# file

using System.Collections.Generic;
using System.Security.Authentication;

namespace System.Net.Security
{
    internal partial struct TlsCipherSuiteData
    {
        private const int LookupCount = 337;

        private static readonly Dictionary<TlsCipherSuite, TlsCipherSuiteData> s_tlsLookup =
            new Dictionary<TlsCipherSuite, TlsCipherSuiteData>(LookupCount)
            {
                {
                    TlsCipherSuite.TLS_NULL_WITH_NULL_NULL,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_NULL_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Rc2,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_IDEA_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_DES_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 56,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 56,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 56,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 56,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 56,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_RC4_40_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_DES_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 56,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 56,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 56,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_WITH_RC4_128_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Rc2,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Des,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Rc2,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_MD5,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 40,
                        MACAlgorithm = HashAlgorithmType.Md5,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_SEED_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_SEED_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_AES_128_CCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_AES_128_CCM_8_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Rc4,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.TripleDes,
                        CipherAlgorithmStrength = 168,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha1,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Null,
                        CipherAlgorithmStrength = 0,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.Sha256,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.Sha384,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_CCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_CCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_PSK_WITH_CHACHA20_POLY1305_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.None,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.RsaKeyX,
                        CipherAlgorithm = CipherAlgorithmType.None,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes256,
                        CipherAlgorithmStrength = 256,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
                {
                    TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256,
                    new TlsCipherSuiteData()
                    {
                        KeyExchangeAlgorithm = ExchangeAlgorithmType.DiffieHellman,
                        CipherAlgorithm = CipherAlgorithmType.Aes128,
                        CipherAlgorithmStrength = 128,
                        MACAlgorithm = HashAlgorithmType.None,
                    }
                },
            };
    }
}
