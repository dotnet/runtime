// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    public abstract class HashAlgorithm : IDisposable, ICryptoTransform
    {
        private bool _disposed;
        protected int HashSizeValue;
        protected internal byte[]? HashValue;
        protected int State;

        protected HashAlgorithm() { }

        [Obsolete(Obsoletions.DefaultCryptoAlgorithmsMessage, DiagnosticId = Obsoletions.DefaultCryptoAlgorithmsDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static HashAlgorithm Create() =>
            CryptoConfigForwarder.CreateDefaultHashAlgorithm();

        [RequiresUnreferencedCode(CryptoConfigForwarder.CreateFromNameUnreferencedCodeMessage)]
        public static HashAlgorithm? Create(string hashName) =>
            (HashAlgorithm?)CryptoConfigForwarder.CreateFromName(hashName);

        public virtual int HashSize => HashSizeValue;

        public virtual byte[]? Hash
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(null);
                if (State != 0)
                    throw new CryptographicUnexpectedOperationException(SR.Cryptography_HashNotYetFinalized);

                return (byte[]?)HashValue?.Clone();
            }
        }

        public byte[] ComputeHash(byte[] buffer)
        {
            if (_disposed)
                throw new ObjectDisposedException(null);
            ArgumentNullException.ThrowIfNull(buffer);

            HashCore(buffer, 0, buffer.Length);
            return CaptureHashCodeAndReinitialize();
        }

        public bool TryComputeHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }

            if (destination.Length < HashSizeValue / 8)
            {
                bytesWritten = 0;
                return false;
            }

            HashCore(source);
            if (!TryHashFinal(destination, out bytesWritten))
            {
                // The only reason for failure should be that the destination isn't long enough,
                // but we checked the size earlier.
                throw new InvalidOperationException(SR.InvalidOperation_IncorrectImplementation);
            }
            HashValue = null;

            Initialize();
            return true;
        }

        public byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0 || (count > buffer.Length))
                throw new ArgumentException(SR.Argument_InvalidValue);
            if ((buffer.Length - count) < offset)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            if (_disposed)
                throw new ObjectDisposedException(null);

            HashCore(buffer, offset, count);
            return CaptureHashCodeAndReinitialize();
        }

        public byte[] ComputeHash(Stream inputStream)
        {
            if (_disposed)
                throw new ObjectDisposedException(null);

            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            int bytesRead;
            int clearLimit = 0;

            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (bytesRead > clearLimit)
                {
                    clearLimit = bytesRead;
                }

                HashCore(buffer, 0, bytesRead);
            }

            CryptographicOperations.ZeroMemory(buffer.AsSpan(0, clearLimit));
            ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
            return CaptureHashCodeAndReinitialize();
        }

        public Task<byte[]> ComputeHashAsync(
            Stream inputStream,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(inputStream);

            if (_disposed)
                throw new ObjectDisposedException(null);

            return ComputeHashAsyncCore(inputStream, cancellationToken);
        }

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

        private byte[] CaptureHashCodeAndReinitialize()
        {
            HashValue = HashFinal();

            // Clone the hash value prior to invoking Initialize in case the user-defined Initialize
            // manipulates the array.
            byte[] tmp = (byte[])HashValue.Clone();
            Initialize();
            return tmp;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Clear()
        {
            (this as IDisposable).Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Although we don't have any resources to dispose at this level,
                // we need to continue to throw ObjectDisposedExceptions from CalculateHash
                // for compatibility with the .NET Framework.
                _disposed = true;
            }
            return;
        }

        // ICryptoTransform methods

        // We assume any HashAlgorithm can take input a byte at a time
        public virtual int InputBlockSize => 1;
        public virtual int OutputBlockSize => 1;
        public virtual bool CanTransformMultipleBlocks => true;
        public virtual bool CanReuseTransform => true;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[]? outputBuffer, int outputOffset)
        {
            ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            // Change the State value
            State = 1;

            HashCore(inputBuffer, inputOffset, inputCount);
            if ((outputBuffer != null) && ((inputBuffer != outputBuffer) || (inputOffset != outputOffset)))
            {
                // We let BlockCopy do the destination array validation
                Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
            }
            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            HashCore(inputBuffer, inputOffset, inputCount);
            HashValue = CaptureHashCodeAndReinitialize();
            byte[] outputBytes;
            if (inputCount != 0)
            {
                outputBytes = new byte[inputCount];
                Buffer.BlockCopy(inputBuffer, inputOffset, outputBytes, 0, inputCount);
            }
            else
            {
                outputBytes = Array.Empty<byte>();
            }

            // Reset the State value
            State = 0;

            return outputBytes;
        }

        private void ValidateTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ArgumentNullException.ThrowIfNull(inputBuffer);

            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException(nameof(inputOffset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (inputCount < 0 || inputCount > inputBuffer.Length)
                throw new ArgumentException(SR.Argument_InvalidValue);
            if ((inputBuffer.Length - inputCount) < inputOffset)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            if (_disposed)
                throw new ObjectDisposedException(null);
        }

        protected abstract void HashCore(byte[] array, int ibStart, int cbSize);
        protected abstract byte[] HashFinal();
        public abstract void Initialize();

        protected virtual void HashCore(ReadOnlySpan<byte> source)
        {
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(source.Length);
            source.CopyTo(array);
            HashCore(array, 0, source.Length);
            Array.Clear(array, 0, source.Length);
            ArrayPool<byte>.Shared.Return(array);
        }

        protected virtual bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            int hashSizeInBytes = HashSizeValue / 8;

            if (destination.Length >= hashSizeInBytes)
            {
                byte[] final = HashFinal();
                if (final.Length == hashSizeInBytes)
                {
                    new ReadOnlySpan<byte>(final).CopyTo(destination);
                    bytesWritten = final.Length;
                    return true;
                }

                throw new InvalidOperationException(SR.InvalidOperation_IncorrectImplementation);
            }

            bytesWritten = 0;
            return false;
        }
    }
}
