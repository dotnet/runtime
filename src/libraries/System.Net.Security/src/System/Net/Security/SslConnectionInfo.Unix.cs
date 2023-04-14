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

            // set defaults to most common values to reduce the IL size
            KeyExchangeAlg = (int)ExchangeAlgorithmType.DiffieHellman;
            DataCipherAlg = (int)CipherAlgorithmType.Null;
            DataKeySize = 128;
            DataHashAlg = (int)HashAlgorithmType.None;
            DataHashKeySize = 0;

            switch (cipherSuite)
            {
                case TlsCipherSuite.TLS_NULL_WITH_NULL_NULL:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataKeySize = 0;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_NULL_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC4_40_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_RC4_128_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_RC4_128_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_EXPORT_WITH_RC2_CBC_40_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc2;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_IDEA_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_EXPORT_WITH_DES40_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_DES_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 56;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_3DES_EDE_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_EXPORT_WITH_DES40_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_DES_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 56;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_EXPORT_WITH_DES40_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_DES_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 56;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_EXPORT_WITH_DES40_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_DES_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 56;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_EXPORT_WITH_DES40_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_DES_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 56;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_RC4_40_MD5:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_RC4_128_MD5:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_DH_anon_EXPORT_WITH_DES40_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_DES_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 56;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 56;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_KRB5_WITH_DES_CBC_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 56;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_KRB5_WITH_3DES_EDE_CBC_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_KRB5_WITH_RC4_128_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_KRB5_WITH_IDEA_CBC_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc2;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_DES_CBC_40_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Des;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC2_CBC_40_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc2;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_KRB5_EXPORT_WITH_RC4_40_MD5:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataKeySize = 40;
                    DataHashAlg = (int)HashAlgorithmType.Md5;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_NULL_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_256_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_RC4_128_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_3DES_EDE_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_RC4_128_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_RC4_128_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_3DES_EDE_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_SEED_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_SEED_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_SEED_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_SEED_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_SEED_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_SEED_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_256_CBC_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_NULL_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA256:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_NULL_SHA384:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_AES_256_CBC_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_NULL_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_AES_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_AES_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_CHACHA20_POLY1305_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_AES_128_CCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_AES_128_CCM_8_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_NULL_SHA:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_RC4_128_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_NULL_SHA:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_RC4_128_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_NULL_SHA:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_RC4_128_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_NULL_SHA:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_RC4_128_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_anon_WITH_NULL_SHA:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_anon_WITH_RC4_128_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_anon_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDH_anon_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_WITH_3DES_EDE_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_3DES_EDE_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_3DES_EDE_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_128_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_128_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_128_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_WITH_AES_256_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_RSA_WITH_AES_256_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_SRP_SHA_DSS_WITH_AES_256_CBC_SHA:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_RC4_128_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Rc4;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_3DES_EDE_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.TripleDes;
                    DataKeySize = 168;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha1;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA256:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_NULL_SHA384:
                    DataKeySize = 0;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_CBC_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_ARIA_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_ARIA_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_CBC_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_CBC_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_ARIA_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_ARIA_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_ARIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_ARIA_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_ARIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_CAMELLIA_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DH_RSA_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DHE_DSS_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DH_DSS_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DH_anon_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_ECDH_ECDSA_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_ECDH_RSA_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_CAMELLIA_256_CBC_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_CAMELLIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_128_CBC_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_CAMELLIA_256_CBC_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_128_CBC_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataHashAlg = (int)HashAlgorithmType.Sha256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CAMELLIA_256_CBC_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    DataHashAlg = (int)HashAlgorithmType.Sha384;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_128_CCM_8:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_RSA_WITH_AES_256_CCM_8:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_128_CCM_8:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_AES_256_CCM_8:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_128_CCM:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_AES_256_CCM:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_128_CCM_8:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_AES_256_CCM_8:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_128_CCM_8:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_PSK_DHE_WITH_AES_256_CCM_8:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_256_CCM_8:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_GCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_GCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECCPWD_WITH_AES_128_CCM_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECCPWD_WITH_AES_256_CCM_SHA384:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_RSA_WITH_CHACHA20_POLY1305_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_PSK_WITH_CHACHA20_POLY1305_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_CHACHA20_POLY1305_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_DHE_PSK_WITH_CHACHA20_POLY1305_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_RSA_PSK_WITH_CHACHA20_POLY1305_SHA256:
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.RsaKeyX;
                    DataCipherAlg = (int)CipherAlgorithmType.None;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_GCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_256_GCM_SHA384:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes256;
                    DataKeySize = 256;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_8_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                case TlsCipherSuite.TLS_ECDHE_PSK_WITH_AES_128_CCM_SHA256:
                    DataCipherAlg = (int)CipherAlgorithmType.Aes128;
                    break;

                default:
                    Debug.Fail($"No mapping found for cipherSuite {cipherSuite}");
                    KeyExchangeAlg = (int)ExchangeAlgorithmType.None;
                    DataKeySize = 0;
                    break;
            }

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
