// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;

#pragma warning disable CA1513

namespace System.Security.Cryptography
{
#if !NET7_0_OR_GREATER && NET
    [UnsupportedOSPlatform("browser")]
#endif
    internal sealed partial class SP800108HmacCounterKdfImplementationManaged : SP800108HmacCounterKdfImplementationBase
    {
        private byte[] _key;
        private int _keyReferenceCount = 1;
        private int _disposed;
        private readonly HashAlgorithmName _hashAlgorithm;

        internal override void DeriveBytes(ReadOnlySpan<byte> label, ReadOnlySpan<byte> context, Span<byte> destination)
        {
            byte[] key = IncrementAndAcquireKey();

            try
            {
                DeriveBytesOneShot(key, _hashAlgorithm, label, context, destination);
            }
            finally
            {
                ReleaseKey();
            }
        }

        internal override void DeriveBytes(ReadOnlySpan<char> label, ReadOnlySpan<char> context, Span<byte> destination)
        {
            byte[] key = IncrementAndAcquireKey();

            try
            {
                DeriveBytesOneShot(key, _hashAlgorithm, label, context, destination);
            }
            finally
            {
                ReleaseKey();
            }
        }

        internal override void DeriveBytes(byte[] label, byte[] context, Span<byte> destination)
        {
            byte[] key = IncrementAndAcquireKey();

            try
            {
                DeriveBytesOneShot(key, _hashAlgorithm, label, context, destination);
            }
            finally
            {
                ReleaseKey();
            }
        }

        public override void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                ReleaseKey();
            }
        }

        private byte[] IncrementAndAcquireKey()
        {
            while (true)
            {
                int current = Volatile.Read(ref _keyReferenceCount);

                if (current == 0)
                {
                    throw new ObjectDisposedException(nameof(SP800108HmacCounterKdfImplementationManaged));
                }

                Debug.Assert(current > 0);
                int incrementedCount = checked(current + 1);

                if (Interlocked.CompareExchange(ref _keyReferenceCount, incrementedCount, current) == current)
                {
                    return _key;
                }
            }
        }

        public void ReleaseKey()
        {
            int newReferenceCount = Interlocked.Decrement(ref _keyReferenceCount);
            Debug.Assert(newReferenceCount >= 0, newReferenceCount.ToString());

            if (newReferenceCount == 0)
            {
                ZeroKey();
            }
        }

        private void ZeroKey()
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null!;
        }
    }
}
