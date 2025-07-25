// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using NTSTATUS=Interop.BCrypt.NTSTATUS;

namespace System.Security.Cryptography
{
#if !NET
    internal sealed class Shake256 : IDisposable
    {
#if NETFRAMEWORK
        private volatile int _concurrentCount;
        private readonly SafeBCryptHashHandle _hashHandle;

        internal Shake256()
            : this(CreateHashHandle())
        {
        }

        internal Shake256(SafeBCryptHashHandle hashHandle)
        {
            Debug.Assert(hashHandle != null);
            _hashHandle = hashHandle;
        }

        internal void AppendData(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty)
            {
                return;
            }

            int count = Interlocked.Increment(ref _concurrentCount);

            try
            {
                if (count != 1)
                {
                    throw new CryptographicException(SR.Cryptography_ConcurrentUseNotSupported);
                }

                NTSTATUS ntStatus = Interop.BCrypt.BCryptHashData(_hashHandle, data, data.Length, dwFlags: 0);

                if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                {
                    throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentCount);
            }
        }

        internal unsafe void GetHashAndReset(Span<byte> destination)
        {
            int count = Interlocked.Increment(ref _concurrentCount);

            try
            {
                if (count != 1)
                {
                    throw new CryptographicException(SR.Cryptography_ConcurrentUseNotSupported);
                }

                fixed (byte* pDestination = &Helpers.GetNonNullPinnableReference(destination))
                {
                    NTSTATUS ntStatus = Interop.BCrypt.BCryptFinishHash(_hashHandle, pDestination, destination.Length, dwFlags: 0);

                    if (ntStatus != NTSTATUS.STATUS_SUCCESS)
                    {
                        throw Interop.BCrypt.CreateCryptographicException(ntStatus);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentCount);
            }
        }

        internal void GetCurrentHash(Span<byte> destination)
        {
            // Doesn't need a concurrency blocker, because clone has one, and this
            // doesn't expose the instance.
            using (Shake256 clone = Clone())
            {
                clone.GetHashAndReset(destination);
            }
        }

        internal Shake256 Clone()
        {
            int count = Interlocked.Increment(ref _concurrentCount);

            try
            {
                if (count != 1)
                {
                    throw new CryptographicException(SR.Cryptography_ConcurrentUseNotSupported);
                }

                SafeBCryptHashHandle clone = Interop.BCrypt.BCryptDuplicateHash(_hashHandle);
                return new Shake256(clone);
            }
            finally
            {
                Interlocked.Decrement(ref _concurrentCount);
            }
        }

        public void Dispose()
        {
            _hashHandle?.Dispose();
        }

        private static SafeBCryptHashHandle CreateHashHandle()
        {
            NTSTATUS ntStatus = Interop.BCrypt.BCryptCreateHash(
                (nuint)Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_CSHAKE256_ALG_HANDLE,
                out SafeBCryptHashHandle hashHandle,
                pbHashObject: IntPtr.Zero,
                cbHashObject: 0,
                secret: ReadOnlySpan<byte>.Empty,
                cbSecret: 0,
                Interop.BCrypt.BCryptCreateHashFlags.None);

            if (ntStatus != NTSTATUS.STATUS_SUCCESS)
            {
                hashHandle.Dispose();
                throw Interop.BCrypt.CreateCryptographicException(ntStatus);
            }

            return hashHandle;
        }

        private void Act(Action action)
        {
            if (Interlocked.Increment(ref _concurrentCount) == 1)
            {
                try
                {
                    action();
                }
                finally
                {
                    Interlocked.Decrement(ref _concurrentCount);
                }
            }
            else
            {
                throw new InvalidOperationException("Concurrent access is not allowed.");
            }
        }
#else // !NETFRAMEWORK
#pragma warning disable CA1822
#pragma warning disable IDE0060

        // The .NET Standard 2.0 and 2.1 builds just need the shape to exist.
        // Shake256 exists in .NET 8 and later.

        internal Shake256()
        {
            throw new PlatformNotSupportedException();
        }

        internal void AppendData(ReadOnlySpan<byte> data)
        {
        }

        internal void GetHashAndReset(Span<byte> destination)
        {
        }

        internal void GetCurrentHash(Span<byte> destination)
        {
        }

        internal Shake256 Clone()
        {
            return null!;
        }

        public void Dispose()
        {
        }
#pragma warning disable IDE0060
#pragma warning restore CA1822
#endif
    }
#elif NET8_0
    internal static class Shake256Extensions
    {
        internal static Shake256 Clone(this Shake256 shake256)
        {
            throw new PlatformNotSupportedException();
        }
    }
#endif
}
