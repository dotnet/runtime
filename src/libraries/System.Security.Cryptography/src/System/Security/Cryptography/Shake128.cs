// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    public sealed partial class Shake128 : IDisposable
    {
        private readonly LiteXof _hashProvider;
        private bool _disposed;

        public Shake128()
        {
            CheckPlatformSupport();
            _hashProvider = CreateHashProvider();
        }

        public static bool IsSupported { get; } = GetIsSupported();

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

        private static void CheckPlatformSupport()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException();
            }
        }

        private void CheckDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

        private static partial LiteXof CreateHashProvider();
        private static partial bool GetIsSupported();
    }
}
