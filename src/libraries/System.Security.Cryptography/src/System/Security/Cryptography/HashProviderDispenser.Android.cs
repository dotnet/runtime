// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        internal static bool KmacSupported(string algorithmId)
        {
            _ = algorithmId;
            return false;
        }

        internal static partial class OneShotHashProvider
        {
            public static int KmacData(
                string algorithmId,
                ReadOnlySpan<byte> key,
                ReadOnlySpan<byte> source,
                Span<byte> destination,
                ReadOnlySpan<byte> customizationString,
                bool xof)
            {
                _ = algorithmId;
                _ = key;
                _ = customizationString;
                _ = source;
                _ = destination;
                _ = xof;
                Debug.Fail("Platform should have checked if KMAC was available first.");
                throw new UnreachableException();
            }

            internal static unsafe void HashDataXof(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                _ = hashAlgorithmId;
                _ = source;
                _ = destination;
                Debug.Fail("Platform should have checked if SHAKE was available first.");
                throw new UnreachableException();
            }
        }
    }
}
