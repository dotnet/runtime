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
        private static readonly bool s_isWindows8OrGreater = OperatingSystem.IsWindowsVersionAtLeast(6, 2);

        private static partial SP800108HmacCounterKdfImplementationBase CreateImplementation(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm)
        {
            if (s_isWindows8OrGreater)
            {
                return new SP800108HmacCounterKdfImplementationCng(key, hashAlgorithm);
            }
            else
            {
                return new SP800108HmacCounterKdfImplementationManaged(key, hashAlgorithm);
            }
        }

        private static partial byte[] DeriveBytesCore(
            byte[] key,
            HashAlgorithmName hashAlgorithm,
            byte[] label,
            byte[] context,
            int derivedKeyLengthInBytes)
        {
            byte[] result = new byte[derivedKeyLengthInBytes];

            if (s_isWindows8OrGreater)
            {
                SP800108HmacCounterKdfImplementationCng.DeriveBytesOneShot(key, hashAlgorithm, label, context, result);
            }
            else
            {
                SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, result);
            }

            return result;
        }

        private static partial void DeriveBytesCore(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            if (s_isWindows8OrGreater)
            {
                SP800108HmacCounterKdfImplementationCng.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
            }
            else
            {
                SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
            }
        }

        private static partial void DeriveBytesCore(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<char> label,
            ReadOnlySpan<char> context,
            Span<byte> destination)
        {
            if (s_isWindows8OrGreater)
            {
                SP800108HmacCounterKdfImplementationCng.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
            }
            else
            {
                SP800108HmacCounterKdfImplementationManaged.DeriveBytesOneShot(key, hashAlgorithm, label, context, destination);
            }
        }
    }
}
