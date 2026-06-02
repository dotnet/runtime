// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal interface IHashStatic
    {
        internal static abstract int HashSizeInBytes { get; }
        internal static abstract string HashAlgorithmName { get; }
        internal static abstract bool IsSupported { get; }
    }

    // This class acts as a single implementation of the hash classes that the public APIs defer to.
    // The public APIs call these methods directly, so they need to behave as if they were public,
    // including parameter validation and async behavior.
    internal static class HashStatic<THash> where THash : IHashStatic
    {
        internal static byte[] HashData(byte[] source)
        {
            ArgumentNullException.ThrowIfNull(source);

            return HashData(new ReadOnlySpan<byte>(source));
        }

        internal static byte[] HashData(ReadOnlySpan<byte> source)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(THash.HashSizeInBytes);

            int written = HashData(source, buffer.AsSpan());
            Debug.Assert(written == buffer.Length);

            return buffer;
        }

        internal static int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!TryHashData(source, destination, out int bytesWritten))
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            return bytesWritten;
        }

        internal static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            CheckPlatformSupport();

            if (destination.Length < THash.HashSizeInBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.HashData(THash.HashAlgorithmName, source, destination);
            Debug.Assert(bytesWritten == THash.HashSizeInBytes);

            return true;
        }

        internal static int HashData(Stream source, Span<byte> destination)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destination.Length < THash.HashSizeInBytes)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckPlatformSupport();
            return LiteHashProvider.HashStream(THash.HashAlgorithmName, source, destination);
        }

        public static byte[] HashData(Stream source)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckPlatformSupport();
            return LiteHashProvider.HashStream(THash.HashAlgorithmName, THash.HashSizeInBytes, source);
        }

        internal static ValueTask<byte[]> HashDataAsync(Stream source, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckPlatformSupport();
            return LiteHashProvider.HashStreamAsync(THash.HashAlgorithmName, source, cancellationToken);
        }

        internal static ValueTask<int> HashDataAsync(
            Stream source,
            Memory<byte> destination,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(source);

            if (destination.Length < THash.HashSizeInBytes)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            if (!source.CanRead)
                throw new ArgumentException(SR.Argument_StreamNotReadable, nameof(source));

            CheckPlatformSupport();
            return LiteHashProvider.HashStreamAsync(
                THash.HashAlgorithmName,
                source,
                destination,
                cancellationToken);
        }

        internal static void CheckPlatformSupport()
        {
            if (!THash.IsSupported)
                throw new PlatformNotSupportedException();
        }
    }
}
