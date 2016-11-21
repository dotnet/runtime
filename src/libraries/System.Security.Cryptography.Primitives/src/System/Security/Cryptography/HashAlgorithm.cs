// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Security.Cryptography
{
    public abstract class HashAlgorithm : IDisposable, ICryptoTransform
    {
        protected HashAlgorithm() { }

        public static HashAlgorithm Create()
        {
            return Create("System.Security.Cryptography.HashAlgorithm");
        }

        public static HashAlgorithm Create(string hashName)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual int HashSize
        {
            get
            {
                return 0;  // For desktop compatibility, return 0 as this property was always initialized by a subclass.
            }
        }

        public virtual byte[] Hash
        {
            get
            {
                if (_disposed)
                    throw new ObjectDisposedException(null);
                if (State != 0)
                    throw new CryptographicUnexpectedOperationException(SR.Cryptography_HashNotYetFinalized);

                return (byte[])HashValue.Clone();
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
            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                HashCore(buffer, 0, bytesRead);
            }
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
                // for compatibility with the desktop framework.
                _disposed = true;
            }
            return;
        }

        // ICryptoTransform methods

        // We assume any HashAlgorithm can take input a byte at a time
        public virtual int InputBlockSize
        {
            get { return (1); }
        }

        public virtual int OutputBlockSize
        {
            get { return (1); }
        }

        public virtual bool CanTransformMultipleBlocks
        {
            get { return true; }
        }

        public virtual bool CanReuseTransform
        {
            get { return true; }
        }

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

        private bool _disposed;
        protected internal byte[] HashValue;
        protected int State = 0;
    }
}
