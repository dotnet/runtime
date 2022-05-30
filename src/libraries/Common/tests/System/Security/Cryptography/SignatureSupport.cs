// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    internal static class SignatureSupport
    {
        internal static bool CanProduceSha1Signature(AsymmetricAlgorithm algorithm)
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
                    finally
                    {
                        algorithm.Dispose();
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
                    finally
                    {
                        algorithm.Dispose();
                    }
                default:
                    throw new NotSupportedException($"Algorithm type {algorithm.GetType()} is not supported.");
            }
        }
    }
}
