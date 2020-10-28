// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    public abstract partial class HashAlgorithm : IDisposable, ICryptoTransform
    {
        private async Task<byte[]> ComputeHashAsyncCore(
            Stream inputStream,
            CancellationToken cancellationToken)
        {
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] rented = ArrayPool<byte>.Shared.Rent(4096);
            Memory<byte> buffer = rented;
            int clearLimit = 0;
            int bytesRead;

            while ((bytesRead = await inputStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (bytesRead > clearLimit)
                {
                    clearLimit = bytesRead;
                }

                HashCore(rented, 0, bytesRead);
            }

            CryptographicOperations.ZeroMemory(rented.AsSpan(0, clearLimit));
            ArrayPool<byte>.Shared.Return(rented, clearArray: false);
            return CaptureHashCodeAndReinitialize();
        }
    }
}
