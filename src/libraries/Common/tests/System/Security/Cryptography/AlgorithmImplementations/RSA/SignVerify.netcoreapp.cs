// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Rsa.Tests
{
    public sealed class SignVerify_Span : SignVerify
    {
        protected override byte[] SignData(RSA rsa, byte[] data, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            TryWithOutputArray(dest => rsa.TrySignData(data, dest, hashAlgorithm, padding, out int bytesWritten) ? (true, bytesWritten) : (false, 0));

        protected override byte[] SignHash(RSA rsa, byte[] hash, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            TryWithOutputArray(dest => rsa.TrySignHash(hash, dest, hashAlgorithm, padding, out int bytesWritten) ? (true, bytesWritten) : (false, 0));

        protected override bool VerifyData(RSA rsa, byte[] data, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            rsa.VerifyData((ReadOnlySpan<byte>)data, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);

        protected override bool VerifyHash(RSA rsa, byte[] hash, byte[] signature, HashAlgorithmName hashAlgorithm, RSASignaturePadding padding) =>
            rsa.VerifyHash((ReadOnlySpan<byte>)hash, (ReadOnlySpan<byte>)signature, hashAlgorithm, padding);

        private static byte[] TryWithOutputArray(Func<byte[], (bool,int)> func)
        {
            for (int length = 1; ; length = checked(length * 2))
            {
                var result = new byte[length];
                var (success, bytesWritten) = func(result);
                if (success)
                {
                    Array.Resize(ref result, bytesWritten);
                    return result;
                }
            }
        }
    }
}
