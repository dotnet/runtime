// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Computes the SHAKE128 hash for the input data.
    /// </summary>
    /// <remarks>
    /// This algorithm is specified by FIPS 202.
    /// </remarks>
    public sealed partial class Shake128 : IDisposable
    {
        // Some platforms have a mutable struct for LitXof, do not mark this field as readonly.
        private LiteXof _hashProvider;
        private bool _disposed;

        public Shake128()
        {
            CheckPlatformSupport();
            _hashProvider = LiteHashProvider.CreateXof(HashAlgorithmId);
        }

        public static bool IsSupported { get; } = HashProviderDispenser.HashSupported(HashAlgorithmId);

        public void AppendData(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            AppendData(new ReadOnlySpan<byte>(data));
        }

        public void AppendData(ReadOnlySpan<byte> data)
        {
            CheckDisposed();

            _hashProvider.Append(data);
        }

        public byte[] GetHashAndReset(int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckDisposed();

            byte[] output = new byte[outputLength];
            _hashProvider.Finalize(output);
            _hashProvider.Reset();
            return output;
        }

        public void GetHashAndReset(Span<byte> destination)
        {
            CheckDisposed();

            _hashProvider.Finalize(destination);
            _hashProvider.Reset();
        }

        public byte[] GetCurrentHash(int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckDisposed();

             byte[] output = new byte[outputLength];
            _hashProvider.Current(output);
            return output;
        }

        public void GetCurrentHash(Span<byte> destination)
        {
            CheckDisposed();
            _hashProvider.Current(destination);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _hashProvider.Dispose();
                _disposed = true;
            }
        }

        public static byte[] HashData(byte[] source, int outputLength)
        {
            ArgumentNullException.ThrowIfNull(source);

            return HashData(new ReadOnlySpan<byte>(source), outputLength);
        }

        public static byte[] HashData(ReadOnlySpan<byte> source, int outputLength)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(outputLength);
            CheckPlatformSupport();

            byte[] output = new byte[outputLength];
            HashDataCore(source, output);
            return output;
        }

        public static void HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            CheckPlatformSupport();
            HashDataCore(source, destination);
        }

        private static void HashDataCore(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            HashProviderDispenser.OneShotHashProvider.HashDataXof(HashAlgorithmId, source, destination);
        }

        private static void CheckPlatformSupport()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }
        }

        private void CheckDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
