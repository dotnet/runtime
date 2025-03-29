// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed class SP800108HmacCounterKdfImplementationOpenSsl : SP800108HmacCounterKdfImplementationBase
    {
        private const int CharToBytesStackBufferSize = 256;

        private readonly HashAlgorithmName _hashAlgorithm;
        private readonly FixedMemoryKeyBox _keyBox;

        internal unsafe SP800108HmacCounterKdfImplementationOpenSsl(ReadOnlySpan<byte> key, HashAlgorithmName hashAlgorithm)
        {
            _hashAlgorithm = hashAlgorithm;
            _keyBox = new FixedMemoryKeyBox(key);
        }

        public override void Dispose()
        {
            _keyBox.Dispose();
        }

        internal override unsafe void DeriveBytes(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            Debug.Assert(Interop.Crypto.EvpKdfAlgs.Kbkdf is { IsInvalid: false });

            if (destination.IsEmpty)
            {
                return;
            }

            bool acquired = false;

            try
            {
                _keyBox.DangerousAddRef(ref acquired);
                Interop.Crypto.KbkdfHmacOneShot(
                    Interop.Crypto.EvpKdfAlgs.Kbkdf,
                    _keyBox.DangerousKeySpan,
                    _hashAlgorithm.Name!,
                    label,
                    context,
                    destination);
            }
            finally
            {
                if (acquired)
                {
                    _keyBox.DangerousRelease();
                }
            }
        }

        internal override void DeriveBytes(byte[] label, byte[] context, Span<byte> destination)
        {
            DeriveBytes(new ReadOnlySpan<byte>(label), new ReadOnlySpan<byte>(context), destination);
        }

        internal override void DeriveBytes(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
        {
            using (Utf8DataEncoding labelData = new Utf8DataEncoding(label, stackalloc byte[CharToBytesStackBufferSize]))
            using (Utf8DataEncoding contextData = new Utf8DataEncoding(context, stackalloc byte[CharToBytesStackBufferSize]))
            {
                DeriveBytes(labelData.Utf8Bytes, contextData.Utf8Bytes, destination);
            }
        }

        internal static void DeriveBytesOneShot(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<byte> label,
            ReadOnlySpan<byte> context,
            Span<byte> destination)
        {
            Debug.Assert(Interop.Crypto.EvpKdfAlgs.Kbkdf is { IsInvalid: false });

            if (destination.IsEmpty)
            {
                return;
            }

            Interop.Crypto.KbkdfHmacOneShot(
                Interop.Crypto.EvpKdfAlgs.Kbkdf,
                key,
                hashAlgorithm.Name!,
                label,
                context,
                destination);
        }

        internal static void DeriveBytesOneShot(
            ReadOnlySpan<byte> key,
            HashAlgorithmName hashAlgorithm,
            ReadOnlySpan<char> label,
            ReadOnlySpan<char> context,
            Span<byte> destination)
        {
            if (destination.Length == 0)
            {
                return;
            }

            using (Utf8DataEncoding labelData = new Utf8DataEncoding(label, stackalloc byte[CharToBytesStackBufferSize]))
            using (Utf8DataEncoding contextData = new Utf8DataEncoding(context, stackalloc byte[CharToBytesStackBufferSize]))
            {
                DeriveBytesOneShot(key, hashAlgorithm, labelData.Utf8Bytes, contextData.Utf8Bytes, destination);
            }
        }
    }
}
