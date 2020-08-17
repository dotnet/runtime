// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    return new NotImplementedHashProvider();
                case HashAlgorithmNames.SHA256:
                    return new NotImplementedHashProvider();
                case HashAlgorithmNames.SHA384:
                    return new NotImplementedHashProvider();
                case HashAlgorithmNames.SHA512:
                    return new NotImplementedHashProvider();
                case HashAlgorithmNames.MD5:
                    return new NotImplementedHashProvider();
            }
            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }

        public static unsafe HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            switch (hashAlgorithmId)
            {
                case HashAlgorithmNames.SHA1:
                    return new NotImplementedHashProvider();
                case HashAlgorithmNames.SHA256:
                    return new NotImplementedHashProvider();
                case HashAlgorithmNames.SHA384:
                    return new NotImplementedHashProvider();
                case HashAlgorithmNames.SHA512:
                    return new NotImplementedHashProvider();
                case HashAlgorithmNames.MD5:
                    return new NotImplementedHashProvider();
            }
            throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId));
        }

        public static class OneShotHashProvider
        {
            public static int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class NotImplementedHashProvider : HashProvider
        {
            public NotImplementedHashProvider()
            {
            }

            public override void AppendHashData(ReadOnlySpan<byte> data)
            {
                throw new NotImplementedException();
            }

            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                throw new NotImplementedException();
            }

            public override int GetCurrentHash(Span<byte> destination)
            {
                throw new NotImplementedException();
            }

            public override int HashSizeInBytes => throw new NotImplementedException();

            public override void Dispose(bool disposing)
            {
            }
        }
    }
}
