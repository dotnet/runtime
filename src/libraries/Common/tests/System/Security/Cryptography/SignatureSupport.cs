// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static class SignatureSupport
    {
        private static int _supportsRsaSha1Signatures = 0;

        internal static bool CanProduceSha1Signature(AsymmetricAlgorithm algorithm)
        {
#if NETFRAMEWORK
    return true;
#else
            try
            {
                // We expect all non-Linux platforms to support SHA1 signatures, currently.
                if (!OperatingSystem.IsLinux())
                {
                    return true;
                }

                switch (algorithm)
                {
                    case ECDsa ecdsa:
                        try
                        {
                            ecdsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA1);
                            return true;
                        }
                        catch (CryptographicException)
                        {
                            return false;
                        }
                    case RSA rsa:
                        try
                        {
                            rsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                            return true;
                        }
                        catch (CryptographicException)
                        {
                            return false;
                        }
                    default:
                        throw new NotSupportedException($"Algorithm type {algorithm.GetType()} is not supported.");
                }
            }
            finally
            {
                algorithm.Dispose();
            }
#endif
        }

        public static bool SupportsRsaSha1Signatures
        {
            get
            {
                if (_supportsRsaSha1Signatures == 0)
                {
                    bool supported = CanProduceSha1Signature(new RSACryptoServiceProvider());
                    _supportsRsaSha1Signatures = supported ? 1 : -1;
                }
                return _supportsRsaSha1Signatures == 1;
            }
        }  
    }
}
