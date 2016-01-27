// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file contains two ICryptoTransforms: ToBase64Transform and FromBase64Transform
// they may be attached to a CryptoStream in either read or write mode

namespace System.Security.Cryptography {
    using System;
    using System.IO;
    using System.Text;
    using System.Diagnostics.Contracts;

    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum FromBase64TransformMode {
        IgnoreWhiteSpaces = 0,
        DoNotIgnoreWhiteSpaces = 1,
    }

[System.Runtime.InteropServices.ComVisible(true)]
    public class ToBase64Transform : ICryptoTransform {
        // converting to Base64 takes 3 bytes input and generates 4 bytes output
        public int InputBlockSize {
            get { return(3); }
        }

        public int OutputBlockSize {
            get { return(4); }
        }

        public bool CanTransformMultipleBlocks {
            get { return(false); }
        }

        public virtual bool CanReuseTransform { 
            get { return(true); }
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) {
            // Do some validation
            if (inputBuffer == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (inputCount < 0 || (inputCount > inputBuffer.Length)) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            // for now, only convert 3 bytes to 4
            char[] temp = new char[4];
            Convert.ToBase64CharArray(inputBuffer, inputOffset, 3, temp, 0);
            byte[] tempBytes = Encoding.ASCII.GetBytes(temp);
            if (tempBytes.Length != 4) throw new CryptographicException(Environment.GetResourceString( "Cryptography_SSE_InvalidDataSize" ));
            Buffer.BlockCopy(tempBytes, 0, outputBuffer, outputOffset, tempBytes.Length);
            return(tempBytes.Length);
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) { 
            // Do some validation
            if (inputBuffer == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (inputCount < 0 || (inputCount > inputBuffer.Length)) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            // Convert.ToBase64CharArray already does padding, so all we have to check is that
            // the inputCount wasn't 0

            // again, for now only a block at a time
            if (inputCount == 0) {
                return(EmptyArray<Byte>.Value);
            } else {
                char[] temp = new char[4];
                Convert.ToBase64CharArray(inputBuffer, inputOffset, inputCount, temp, 0);
                byte[] tempBytes = Encoding.ASCII.GetBytes(temp);
                return(tempBytes);
            }
        }

        // must implement IDisposable, but in this case there's nothing to do.

        public void Dispose() {
            Clear();
        }

        public void Clear() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
        }

        ~ToBase64Transform() {
            //
            // A finalizer is not necessary here, however since we shipped a finalizer that called
            // Dispose(false) in v2.0, we need to keep it around in case any existing code had subclassed
            // this transform and expects to have a base class finalizer call its dispose method
            // 

            Dispose(false);
        }
    }

[System.Runtime.InteropServices.ComVisible(true)]
    public class FromBase64Transform : ICryptoTransform {
        private byte[] _inputBuffer = new byte[4];
        private int    _inputIndex;
        
        private FromBase64TransformMode _whitespaces;

        // Constructors
        public FromBase64Transform() : this(FromBase64TransformMode.IgnoreWhiteSpaces) {}
        public FromBase64Transform(FromBase64TransformMode whitespaces) {
            _whitespaces = whitespaces;
            _inputIndex = 0;
        }
        
        // converting from Base64 generates 3 bytes output from each 4 bytes input block
        public int InputBlockSize {
            get { return(1); }
        }

        public int OutputBlockSize {
            get { return(3); }
        }

        public bool CanTransformMultipleBlocks {
            get { return(false); }
        }

        public virtual bool CanReuseTransform { 
            get { return(true); }
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) {
            // Do some validation
            if (inputBuffer == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (inputCount < 0 || (inputCount > inputBuffer.Length)) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (_inputBuffer == null)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));

            byte[] temp = new byte[inputCount];
            char[] tempChar;
            int effectiveCount;

            if (_whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces) {
                temp = DiscardWhiteSpaces(inputBuffer, inputOffset, inputCount);
                effectiveCount = temp.Length;
            } else {
                Buffer.InternalBlockCopy(inputBuffer, inputOffset, temp, 0, inputCount);
                effectiveCount = inputCount;
            }            

            if (effectiveCount + _inputIndex < 4) {
                Buffer.InternalBlockCopy(temp, 0, _inputBuffer, _inputIndex, effectiveCount);
                _inputIndex += effectiveCount;  
                return 0;  
            }
            
            // Get the number of 4 bytes blocks to transform
            int numBlocks = (effectiveCount + _inputIndex) / 4;
            byte[] transformBuffer = new byte[_inputIndex + effectiveCount];
            Buffer.InternalBlockCopy(_inputBuffer, 0, transformBuffer, 0, _inputIndex);
            Buffer.InternalBlockCopy(temp, 0, transformBuffer, _inputIndex, effectiveCount);
            _inputIndex = (effectiveCount + _inputIndex) % 4; 
            Buffer.InternalBlockCopy(temp, effectiveCount - _inputIndex, _inputBuffer, 0, _inputIndex);

            tempChar = Encoding.ASCII.GetChars(transformBuffer, 0, 4*numBlocks);

            byte[] tempBytes = Convert.FromBase64CharArray(tempChar, 0, 4*numBlocks);
            Buffer.BlockCopy(tempBytes, 0, outputBuffer, outputOffset, tempBytes.Length);
            return(tempBytes.Length);
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) {
            // Do some validation
            if (inputBuffer == null) throw new ArgumentNullException("inputBuffer");
            if (inputOffset < 0) throw new ArgumentOutOfRangeException("inputOffset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (inputCount < 0 || (inputCount > inputBuffer.Length)) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidValue"));
            if ((inputBuffer.Length - inputCount) < inputOffset) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));
            Contract.EndContractBlock();

            if (_inputBuffer == null)
                throw new ObjectDisposedException(null, Environment.GetResourceString("ObjectDisposed_Generic"));

            byte[] temp = new byte[inputCount];
            char[] tempChar;
            int effectiveCount;

            if (_whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces) {
                temp = DiscardWhiteSpaces(inputBuffer, inputOffset, inputCount);
                effectiveCount = temp.Length;
            } else {
                Buffer.InternalBlockCopy(inputBuffer, inputOffset, temp, 0, inputCount);
                effectiveCount = inputCount;
            }            

            if (effectiveCount + _inputIndex < 4) {
                Reset();
                return (EmptyArray<Byte>.Value);  
            }
            
            // Get the number of 4 bytes blocks to transform
            int numBlocks = (effectiveCount + _inputIndex) / 4;
            byte[] transformBuffer = new byte[_inputIndex + effectiveCount];
            Buffer.InternalBlockCopy(_inputBuffer, 0, transformBuffer, 0, _inputIndex);
            Buffer.InternalBlockCopy(temp, 0, transformBuffer, _inputIndex, effectiveCount);
            _inputIndex = (effectiveCount + _inputIndex) % 4; 
            Buffer.InternalBlockCopy(temp, effectiveCount - _inputIndex, _inputBuffer, 0, _inputIndex);

            tempChar = Encoding.ASCII.GetChars(transformBuffer, 0, 4*numBlocks);

            byte[] tempBytes = Convert.FromBase64CharArray(tempChar, 0, 4*numBlocks);
            // reinitialize the transform
            Reset();
            return(tempBytes);
        }

        private byte[] DiscardWhiteSpaces(byte[] inputBuffer, int inputOffset, int inputCount) {
            int i, iCount = 0;
            for (i=0; i<inputCount; i++)
                if (Char.IsWhiteSpace((char)inputBuffer[inputOffset + i])) iCount++;
            byte[] rgbOut = new byte[inputCount - iCount];
            iCount = 0;
            for (i=0; i<inputCount; i++)
                if (!Char.IsWhiteSpace((char)inputBuffer[inputOffset + i])) {
                    rgbOut[iCount++] = inputBuffer[inputOffset + i];
                }
            return rgbOut;
        }

        // must implement IDisposable, which in this case means clearing the input buffer

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Reset the state of the transform so it can be used again
        private void Reset() {
            _inputIndex = 0;
        }

        public void Clear() {
            Dispose();
        }

        protected virtual void Dispose(bool disposing) {
            // we always want to clear the input buffer
            if (disposing) {
                if (_inputBuffer != null)
                    Array.Clear(_inputBuffer, 0, _inputBuffer.Length);
                _inputBuffer = null;
                _inputIndex = 0;
            }
        }

        ~FromBase64Transform() {
            //
            // A finalizer is not necessary here, however since we shipped a finalizer that called
            // Dispose(false) in v2.0, we need to keep it around in case any existing code had subclassed
            // this transform and expects to have a base class finalizer call its dispose method
            // 

            Dispose(false);
        }
    }
}
