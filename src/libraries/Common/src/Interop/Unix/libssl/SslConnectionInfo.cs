// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Authentication;

namespace System.Net
{
    internal class SslConnectionInfo
    {
        //Windows constants for algorithms without matching enums in SslStream
        //Taken from wincrypt.h 
        internal const int SSL_SHA256 = 32780;
        internal const int SSL_SHA384 = 32781;
        internal const int SSL_ECDH = 43525;
        internal const int SSL_ECDSA = 41475;

        //Algorithm constants which are not present in wincrypt.h(not supported in Windows)
        //These constants values are not conflicting with existing constants in wincrypt.h
        internal const int SSL_IDEA = 229380;
        internal const int SSL_CAMELLIA128 = 229381;
        internal const int SSL_CAMELLIA256 = 229382;
        internal const int SSL_eGOST2814789CNT = 229383;
        internal const int SSL_SEED = 229384;

        internal const int SSL_kPSK = 229390;
        internal const int SSL_kGOST = 229391;
        internal const int SSL_kSRP = 229392;
        internal const int SSL_kKRB5 = 229393;

        internal const int SSL_GOST94 = 229410;
        internal const int SSL_GOST89 = 229411;
        internal const int SSL_AEAD = 229412;

        public readonly int Protocol;
        public readonly int DataCipherAlg;
        public readonly int DataKeySize = 0;
        public readonly int DataHashAlg;
        public readonly int DataHashKeySize = 0;
        public readonly int KeyExchangeAlg;
        public readonly int KeyExchKeySize = 0;

        internal SslConnectionInfo(Interop.libssl.SSL_CIPHER cipher, string protocol)
        {
            Protocol = (int) MapProtocolVersion(protocol);
            DataCipherAlg = (int) MapCipherAlgorithmType((Interop.libssl.CipherAlgorithm) cipher.algorithm_enc);
            KeyExchangeAlg = (int) MapExchangeAlgorithmType((Interop.libssl.KeyExchangeAlgorithm) cipher.algorithm_mkey);
            DataHashAlg = (int) MapHashAlgorithmType((Interop.libssl.DataHashAlgorithm) cipher.algorithm_mac);
            DataKeySize = cipher.alg_bits;
            // TODO (Issue #3362) map key sizes
        }

        private SslProtocols MapProtocolVersion(string protocolVersion)
        {
            switch (protocolVersion)
            {
                case "SSLv2":
                    return SslProtocols.Ssl2;
                case "SSLv3":
                    return SslProtocols.Ssl3;
                case "TLSv1":
                    return SslProtocols.Tls;
                case "TLSv1.1":
                    return SslProtocols.Tls11;
                case "TLSv1.2":
                    return SslProtocols.Tls12;
                default:
                    return SslProtocols.None;
            }
        }

        private static CipherAlgorithmType MapCipherAlgorithmType(Interop.libssl.CipherAlgorithm encryption)
        {
            switch (encryption)
            {
                case Interop.libssl.CipherAlgorithm.SSL_DES:
                    return CipherAlgorithmType.Des;

                case Interop.libssl.CipherAlgorithm.SSL_3DES:
                    return CipherAlgorithmType.TripleDes;

                case Interop.libssl.CipherAlgorithm.SSL_RC4:
                    return CipherAlgorithmType.Rc4;

                case Interop.libssl.CipherAlgorithm.SSL_RC2:
                    return CipherAlgorithmType.Rc2;

                case Interop.libssl.CipherAlgorithm.SSL_eNULL:
                    return CipherAlgorithmType.Null;

                case Interop.libssl.CipherAlgorithm.SSL_AES128:
                    return CipherAlgorithmType.Aes128;

                case Interop.libssl.CipherAlgorithm.SSL_AES256:
                    return CipherAlgorithmType.Aes256;

                case Interop.libssl.CipherAlgorithm.SSL_IDEA:
                    return (CipherAlgorithmType) SSL_IDEA;

                case Interop.libssl.CipherAlgorithm.SSL_CAMELLIA128:
                    return (CipherAlgorithmType) SSL_CAMELLIA128;

                case Interop.libssl.CipherAlgorithm.SSL_CAMELLIA256:
                    return (CipherAlgorithmType) SSL_CAMELLIA256;

                case Interop.libssl.CipherAlgorithm.SSL_eGOST2814789CNT:
                    return (CipherAlgorithmType) SSL_eGOST2814789CNT;

                case Interop.libssl.CipherAlgorithm.SSL_SEED:
                    return (CipherAlgorithmType) SSL_SEED;

                case Interop.libssl.CipherAlgorithm.SSL_AES128GCM:
                    return CipherAlgorithmType.Aes128;

                case Interop.libssl.CipherAlgorithm.SSL_AES256GCM:
                    return CipherAlgorithmType.Aes256;

                default:
                    return CipherAlgorithmType.None;
            }
        }

        private static ExchangeAlgorithmType MapExchangeAlgorithmType(Interop.libssl.KeyExchangeAlgorithm mkey)
        {
            switch (mkey)
            {
                case Interop.libssl.KeyExchangeAlgorithm.SSL_kRSA:
                    return ExchangeAlgorithmType.RsaKeyX;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kDHr:
                    return ExchangeAlgorithmType.DiffieHellman;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kDHd:
                    return ExchangeAlgorithmType.DiffieHellman;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kEDH:
                    return ExchangeAlgorithmType.DiffieHellman;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kKRB5:
                    return (ExchangeAlgorithmType) SSL_kKRB5;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kECDHr:
                    return (ExchangeAlgorithmType) SSL_ECDH;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kECDHe:
                    return (ExchangeAlgorithmType) SSL_ECDSA;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kEECDH:
                    return (ExchangeAlgorithmType) SSL_ECDSA;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kPSK:
                    return (ExchangeAlgorithmType) SSL_kPSK;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kGOST:
                    return (ExchangeAlgorithmType) SSL_kGOST;

                case Interop.libssl.KeyExchangeAlgorithm.SSL_kSRP:
                    return (ExchangeAlgorithmType) SSL_kSRP;

                default:
                    return ExchangeAlgorithmType.None;
            }
        }

        private static HashAlgorithmType MapHashAlgorithmType(Interop.libssl.DataHashAlgorithm mac)
        {
            switch (mac)
            {
                case Interop.libssl.DataHashAlgorithm.SSL_MD5:
                    return HashAlgorithmType.Md5;

                case Interop.libssl.DataHashAlgorithm.SSL_SHA1:
                    return HashAlgorithmType.Sha1;

                case Interop.libssl.DataHashAlgorithm.SSL_GOST94:
                    return (HashAlgorithmType) SSL_GOST94;

                case Interop.libssl.DataHashAlgorithm.SSL_GOST89MAC:
                    return (HashAlgorithmType) SSL_GOST89;

                case Interop.libssl.DataHashAlgorithm.SSL_SHA256:
                    return (HashAlgorithmType) SSL_SHA256;

                case Interop.libssl.DataHashAlgorithm.SSL_SHA384:
                    return (HashAlgorithmType) SSL_SHA384;

                case Interop.libssl.DataHashAlgorithm.SSL_AEAD:
                    return (HashAlgorithmType) SSL_AEAD;

                default:
                    return HashAlgorithmType.None;
            }
        }
    }
}
