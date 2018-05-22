// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file contains two ICryptoTransforms: ToBase64Transform and FromBase64Transform
// they may be attached to a CryptoStream in either read or write mode

using System.Text;

namespace System.Security.Cryptography
{
    public enum FromBase64TransformMode
    {
        IgnoreWhiteSpaces = 0,
        DoNotIgnoreWhiteSpaces = 1,
    }

    public class ToBase64Transform : ICryptoTransform
    {
        // converting to Base64 takes 3 bytes input and generates 4 bytes output
        public int InputBlockSize => 3;
        public int OutputBlockSize => 4;
        public bool CanTransformMultipleBlocks => false;
        public virtual bool CanReuseTransform => true;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            // For now, only convert 3 bytes to 4
            byte[] tempBytes = ConvertToBase64(inputBuffer, inputOffset, 3);

            Buffer.BlockCopy(tempBytes, 0, outputBuffer, outputOffset, tempBytes.Length);
            return tempBytes.Length;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            // Convert.ToBase64CharArray already does padding, so all we have to check is that
            // the inputCount wasn't 0
            if (inputCount == 0)
            {
                return Array.Empty<byte>();
            }

            // Again, for now only a block at a time
            return ConvertToBase64(inputBuffer, inputOffset, inputCount);
        }

        private byte[] ConvertToBase64(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            char[] temp = new char[4];
            Convert.ToBase64CharArray(inputBuffer, inputOffset, inputCount, temp, 0);
            byte[] tempBytes = Encoding.ASCII.GetBytes(temp);
            if (tempBytes.Length != 4)
                throw new CryptographicException(SR.Cryptography_SSE_InvalidDataSize);

            return tempBytes;
        }

        private static void ValidateTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null) throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (inputCount < 0 || (inputCount > inputBuffer.Length)) throw new ArgumentException(SR.Argument_InvalidValue);
            if ((inputBuffer.Length - inputCount) < inputOffset) throw new ArgumentException(SR.Argument_InvalidOffLen);
        }

        // Must implement IDisposable, but in this case there's nothing to do.

        public void Dispose()
        {
            Clear();
        }

        public void Clear()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) { }

        ~ToBase64Transform()
        {
            // A finalizer is not necessary here, however since we shipped a finalizer that called
            // Dispose(false) in desktop v2.0, we need to keep it in case any existing code had subclassed
            // this transform and expects to have a base class finalizer call its dispose method.
            Dispose(false);
        }
    }

    public class FromBase64Transform : ICryptoTransform
    {
        private byte[] _inputBuffer = new byte[4];
        private int _inputIndex;
        private FromBase64TransformMode _whitespaces;

        public FromBase64Transform() : this(FromBase64TransformMode.IgnoreWhiteSpaces) { }
        public FromBase64Transform(FromBase64TransformMode whitespaces)
        {
            _whitespaces = whitespaces;
            _inputIndex = 0;
        }

        // Converting from Base64 generates 3 bytes output from each 4 bytes input block
        public int InputBlockSize => 1;
        public int OutputBlockSize => 3;
        public bool CanTransformMultipleBlocks => false;
        public virtual bool CanReuseTransform => true;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
            if (_inputBuffer == null) throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);

            int effectiveCount;
            byte[] temp = GetTempBuffer(inputBuffer, inputOffset, inputCount, out effectiveCount);

            if (effectiveCount + _inputIndex < 4)
            {
                Buffer.BlockCopy(temp, 0, _inputBuffer, _inputIndex, effectiveCount);
                _inputIndex += effectiveCount;
                return 0;
            }

            byte[] result = ConvertFromBase64(temp, effectiveCount);

            Buffer.BlockCopy(result, 0, outputBuffer, outputOffset, result.Length);

            return result.Length;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
            if (_inputBuffer == null) throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);

            int effectiveCount;
            byte[] temp = GetTempBuffer(inputBuffer, inputOffset, inputCount, out effectiveCount);

            if (effectiveCount + _inputIndex < 4)
            {
                Reset();
                return Array.Empty<byte>();
            }

            byte[] result = ConvertFromBase64(temp, effectiveCount);

            // reinitialize the transform
            Reset();

            return result;
        }

        private byte[] GetTempBuffer(byte[] inputBuffer, int inputOffset, int inputCount, out int effectiveCount)
        {
            byte[] temp;

            if (_whitespaces == FromBase64TransformMode.IgnoreWhiteSpaces)
            {
                temp = DiscardWhiteSpaces(inputBuffer, inputOffset, inputCount);
                effectiveCount = temp.Length;
            }
            else
            {
                temp = new byte[inputCount];
                Buffer.BlockCopy(inputBuffer, inputOffset, temp, 0, inputCount);
                effectiveCount = inputCount;
            }

            return temp;
        }

        private byte[] ConvertFromBase64(byte[] temp, int effectiveCount)
        {
            // Get the number of 4 bytes blocks to transform
            int numBlocks = (effectiveCount + _inputIndex) / 4;

            byte[] transformBuffer = new byte[_inputIndex + effectiveCount];
            Buffer.BlockCopy(_inputBuffer, 0, transformBuffer, 0, _inputIndex);
            Buffer.BlockCopy(temp, 0, transformBuffer, _inputIndex, effectiveCount);

            _inputIndex = (effectiveCount + _inputIndex) % 4;
            Buffer.BlockCopy(temp, effectiveCount - _inputIndex, _inputBuffer, 0, _inputIndex);

            char[] tempChar = Encoding.ASCII.GetChars(transformBuffer, 0, 4 * numBlocks);
            byte[] tempBytes = Convert.FromBase64CharArray(tempChar, 0, 4 * numBlocks);
            return tempBytes;
        }

        private byte[] DiscardWhiteSpaces(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            int i, iCount = 0;
            for (i = 0; i < inputCount; i++)
            {
                if (char.IsWhiteSpace((char)inputBuffer[inputOffset + i])) iCount++;
            }

            // If there's nothing to do, leave early
            if (iCount == 0 && inputOffset == 0 &&
                inputCount == inputBuffer.Length)
            {
                return inputBuffer;
            }

            byte[] rgbOut = new byte[inputCount - iCount];
            iCount = 0;
            for (i = 0; i < inputCount; i++)
            {
                if (!char.IsWhiteSpace((char)inputBuffer[inputOffset + i]))
                {
                    rgbOut[iCount++] = inputBuffer[inputOffset + i];
                }
            }

            return rgbOut;
        }

        private static void ValidateTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null) throw new ArgumentNullException(nameof(inputBuffer));
            if (inputOffset < 0) throw new ArgumentOutOfRangeException(nameof(inputOffset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (inputCount < 0 || (inputCount > inputBuffer.Length)) throw new ArgumentException(SR.Argument_InvalidValue);
            if ((inputBuffer.Length - inputCount) < inputOffset) throw new ArgumentException(SR.Argument_InvalidOffLen);
        }

        // must implement IDisposable, which in this case means clearing the input buffer

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Reset the state of the transform so it can be used again
        private void Reset()
        {
            _inputIndex = 0;
        }

        public void Clear()
        {
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            // we always want to clear the input buffer
            if (disposing)
            {
                if (_inputBuffer != null)
                    Array.Clear(_inputBuffer, 0, _inputBuffer.Length);
                _inputBuffer = null;
                _inputIndex = 0;
            }
        }

        ~FromBase64Transform()
        {
            Dispose(false);
        }
    }
}
