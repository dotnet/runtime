// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;

namespace System.Security.Cryptography
{
    public abstract class HashAlgorithm : IDisposable, ICryptoTransform
    {
        private bool _disposed;
        protected int HashSizeValue;
        protected internal byte[] HashValue;
        protected int State = 0;

        protected HashAlgorithm() { }

        public static HashAlgorithm Create() =>
            CryptoConfigForwarder.CreateDefaultHashAlgorithm();

        public static HashAlgorithm Create(string hashName) =>
            (HashAlgorithm)CryptoConfigForwarder.CreateFromName(hashName);

        public virtual int HashSize => HashSizeValue;

        public virtual byte[] Hash
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(null);
                if (State != 0)
                    throw new CryptographicUnexpectedOperationException(SR.Cryptography_HashNotYetFinalized);

                return (byte[])HashValue?.Clone();
            }
        }

        public byte[] ComputeHash(byte[] buffer)
        {
            if (_disposed)
                throw new ObjectDisposedException(null);
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            HashCore(buffer, 0, buffer.Length);
            return CaptureHashCodeAndReinitialize();
        }

        public bool TryComputeHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(null);
            }

            if (destination.Length < HashSizeValue/8)
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
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
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

            // Default the buffer size to 4K.
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                int bytesRead;
                while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    HashCore(buffer, 0, bytesRead);
                }

                return CaptureHashCodeAndReinitialize();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
                ArrayPool<byte>.Shared.Return(buffer);
            }
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
                // for compatibility with the desktop framework.
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

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
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
            if (inputBuffer == null)
                throw new ArgumentNullException(nameof(inputBuffer));
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
            byte[] array = ArrayPool<byte>.Shared.Rent(source.Length);
            try
            {
                source.CopyTo(array);
                HashCore(array, 0, source.Length);
            }
            finally
            {
                Array.Clear(array, 0, source.Length);
                ArrayPool<byte>.Shared.Return(array);
            }
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
