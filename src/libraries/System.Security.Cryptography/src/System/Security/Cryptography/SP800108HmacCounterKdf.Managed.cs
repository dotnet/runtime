// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace System.Security.Cryptography
{
    public sealed partial class SP800108HmacCounterKdf : IDisposable
    {
        private static partial SP800108HmacCounterKdfImplementationBase CreateImplementation(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm)
        {
            return new SP800108HmacCounterKdfImplementationManaged(key, hashAlgorithm);
        }

        private static partial byte[] DeriveBytesCore(
            byte[] key,
            HashAlgorithmName hashAlgorithm,
            byte[] label,
            byte[] context,
            int derivedKeyLengthInBytes)
        {
            byte[] result = new byte[derivedKeyLengthInBytes];

            SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, result);

            return result;
        }

        private static partial void DeriveBytesCore(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
        }

        private static partial void DeriveBytesCore(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<char> label,
            ReadOnlySpan<char> context,
            Span<byte> destination)
        {
            SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
        }
    }
}
