// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Authentication;

namespace System.Net
{
    internal class SslConnectionInfo
    {
        public readonly int Protocol;
        public readonly int DataCipherAlg;
        public readonly int DataKeySize = 0;
        public readonly int DataHashAlg;
        public readonly int DataHashKeySize = 0;
        public readonly int KeyExchangeAlg;
        public readonly int KeyExchKeySize = 0;

        internal SslConnectionInfo(Interop.libssl.SSL_CIPHER cipher)
        {
            Protocol = (int) MapProtocol(cipher.algorithm_ssl);
            DataCipherAlg = (int) MapCipherAlgorithmType(cipher.algorithm_enc);
            KeyExchangeAlg = (int) MapExchangeAlgorithmType(cipher.algorithm_mkey);
            DataHashAlg = (int) MapHashAlgorithmType(cipher.algorithm_mac);
            // TODO (Issue #3362) map key sizes
        }

        private static SslProtocols MapProtocol(long algorithm)
        {
            switch (algorithm)
            {
                case 1:
                    return SslProtocols.Ssl2;
                case 2:
                    return SslProtocols.Ssl3;
                case 3:
                    return SslProtocols.Tls11;
                case 4:
                    return SslProtocols.Tls12;
                default:
                    // TODO (Issue #3362) Figure out correct value for this
                    return SslProtocols.Tls;
            }
        }

        private static CipherAlgorithmType MapCipherAlgorithmType(long encryption)
        {
            switch (encryption)
            {
                case 1:
                    return CipherAlgorithmType.Des;
                case 2:
                    return CipherAlgorithmType.TripleDes;
                case 4:
                    return CipherAlgorithmType.Rc4;
                case 8:
                    return CipherAlgorithmType.Rc2;
                case 16:
                    return CipherAlgorithmType.None;
                case 32:
                    return CipherAlgorithmType.Null;
                case 64:
                    return CipherAlgorithmType.Aes128;
                case 128:
                    return CipherAlgorithmType.Aes256;
                default:
                    // TODO (Issue #3362) Handle other values
                    return CipherAlgorithmType.None;
            }
        }

        private static ExchangeAlgorithmType MapExchangeAlgorithmType(long mac)
        {
            switch (mac)
            {
                case 1:
                    return ExchangeAlgorithmType.RsaKeyX;
                case 2 | 32:
                    return ExchangeAlgorithmType.RsaSign;
                case 128:
                    return ExchangeAlgorithmType.DiffieHellman;
                default:
                    // TODO (Issue #3362) Handle other values
                    return ExchangeAlgorithmType.None;
            }
        }

        private static HashAlgorithmType MapHashAlgorithmType(long mac)
        {
            switch (mac)
            {
                case 1:
                    return HashAlgorithmType.Md5;
                case 2:
                    return HashAlgorithmType.Sha1;
                default:
                    // TODO (Issue #3362) Handle other values
                    return HashAlgorithmType.None;
            }
        }
    }
}
