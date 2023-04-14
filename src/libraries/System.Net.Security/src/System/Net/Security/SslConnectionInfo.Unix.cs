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

            ulong data = cipherSuite switch
            {
                TlsCipherSuite.TLS_NULL_WITH_NULL_NULL =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_NULL_MD5 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc2 << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_IDEA_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_DES_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)56 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)56 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)56 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)56 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)56 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_RC4_40_MD5 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_DES_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)56 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)56 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_MD5 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)56 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_MD5 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_WITH_RC4_128_MD5 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_MD5 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc2 << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Des << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc2 << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_MD5 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)40 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Md5  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_SEED_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_SEED_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_AES_128_CCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_AES_128_CCM_8_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Rc4 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.TripleDes << (64 - (16 * 2)) |
                    (ulong)168 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha1  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Null << (64 - (16 * 2)) |
                    (ulong)0 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha256  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.Sha384  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_CCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_CCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_PSK_WITH_CHACHA20_POLY1305_SHA256 =>
                    (ulong)ExchangeAlgorithmType.None << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256 =>
                    (ulong)ExchangeAlgorithmType.RsaKeyX << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.None << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes256 << (64 - (16 * 2)) |
                    (ulong)256 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256 =>
                    (ulong)ExchangeAlgorithmType.DiffieHellman << (64 - (16 * 1)) |
                    (ulong)CipherAlgorithmType.Aes128 << (64 - (16 * 2)) |
                    (ulong)128 << (64 - (16 * 3)) |
                    (ulong)HashAlgorithmType.None  << (64 - (16 * 4)),

                _ => 0
            };

            Debug.Assert(data != 0, $"No mapping found for cipherSuite {cipherSuite}");

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
    }
}
