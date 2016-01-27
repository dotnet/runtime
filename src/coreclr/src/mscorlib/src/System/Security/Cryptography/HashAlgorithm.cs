// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System.IO;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class HashAlgorithm : IDisposable, ICryptoTransform {
        protected int HashSizeValue;
        protected internal byte[] HashValue;
        protected int State = 0;

        private bool m_bDisposed = false;

        protected HashAlgorithm() {}

        //
        // public properties
        //

        public virtual int HashSize {
            get { return HashSizeValue; }
        }

        public virtual byte[] Hash {
            get {
                if (m_bDisposed) 
                    throw new ObjectDisposedException(null);
                if (State != 0)
                    throw new CryptographicUnexpectedOperationException(Environment.GetResourceString("Cryptography_HashNotYetFinalized"));
                return (byte[]) HashValue.Clone();
            }
        }

        //
        // public methods
        //

        static public HashAlgorithm Create() {
            return Create("System.Security.Cryptography.HashAlgorithm");
        }

        static public HashAlgorithm Create(String hashName) {
            return (HashAlgorithm) CryptoConfig.CreateFromName(hashName);
        }

        public byte[] ComputeHash(Stream inputStream) {
            if (m_bDisposed) 
                throw new ObjectDisposedException(null);

            // Default the buffer size to 4K.
            byte[] buffer = new byte[4096];
            int bytesRead;
            do {
                bytesRead = inputStream.Read(buffer, 0, 4096);
                if (bytesRead > 0) {
                    HashCore(buffer, 0, bytesRead);
                }
            } while (bytesRead > 0);

            HashValue = HashFinal();
            byte[] Tmp = (byte[]) HashValue.Clone();
            Initialize();
            return(Tmp);
        }

        public byte[] ComputeHash(byte[] buffer) {
            if (m_bDisposed) 
                throw new ObjectDisposedException(null);

            // Do some validation
            if (buffer == null) throw new ArgumentNullException("buffer");

            HashCore(buffer, 0, buffer.Length);
            HashValue = HashFinal();
            byte[] Tmp = (byte[]) HashValue.Clone();
            Initialize();
            return(Tmp);
        }

        public byte[] ComputeHash(byte[] buffer, int offset, int count) {
            // Do some validation
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0 || (count > buffer.Length))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((buffer.Length - count) < offset)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (m_bDisposed)
                throw new ObjectDisposedException(null);

            HashCore(buffer, offset, count);
            HashValue = HashFinal();
            byte[] Tmp = (byte[]) HashValue.Clone();
            Initialize();
            return(Tmp);
        }

        // ICryptoTransform methods

        // we assume any HashAlgorithm can take input a byte at a time
        public virtual int InputBlockSize { 
            get { return(1); }
        }

        public virtual int OutputBlockSize {
            get { return(1); }
        }

        public virtual bool CanTransformMultipleBlocks { 
            get { return(true); }
        }

        public virtual bool CanReuseTransform { 
            get { return(true); }
        }

        // We implement TransformBlock and TransformFinalBlock here
        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) {
            // Do some validation, we let BlockCopy do the destination array validation
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (inputCount < 0 || (inputCount > inputBuffer.Length))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (m_bDisposed)
                throw new ObjectDisposedException(null);

            // Change the State value
            State = 1;
            HashCore(inputBuffer, inputOffset, inputCount);
            if ((outputBuffer != null) && ((inputBuffer != outputBuffer) || (inputOffset != outputOffset)))
                Buffer.BlockCopy(inputBuffer, inputOffset, outputBuffer, outputOffset, inputCount);
            return inputCount;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) {
            // Do some validation
            if (inputBuffer == null)
                throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0)
                throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (inputCount < 0 || (inputCount > inputBuffer.Length))
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset)
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (m_bDisposed)
                throw new ObjectDisposedException(null);

            HashCore(inputBuffer, inputOffset, inputCount);
            HashValue = HashFinal();
            byte[] outputBytes;
            if (inputCount != 0)
            {
                outputBytes = new byte[inputCount];
                Buffer.InternalBlockCopy(inputBuffer, inputOffset, outputBytes, 0, inputCount);
            }
            else
            {
                outputBytes = EmptyArray<Byte>.Value;
            }
            // reset the State value
            State = 0;
            return outputBytes;
        }

        // IDisposable methods

        // To keep mscorlib compatibility with Orcas, CoreCLR's HashAlgorithm has an explicit IDisposable
        // implementation. Post-Orcas the desktop has an implicit IDispoable implementation.
#if FEATURE_CORECLR
        void IDisposable.Dispose()
        {
            Dispose();
        }
#endif // FEATURE_CORECLR

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Clear() {
            (this as IDisposable).Dispose();
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (HashValue != null)
                    Array.Clear(HashValue, 0, HashValue.Length);
                HashValue = null;
                m_bDisposed = true;
            }
        }

        //
        // abstract public methods
        //

        public abstract void Initialize();

        protected abstract void HashCore(byte[] array, int ibStart, int cbSize);

        protected abstract byte[] HashFinal();
    }
}
