// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains two ICryptoTransforms: ToBase64Transform and FromBase64Transform
// they may be attached to a CryptoStream in either read or write mode

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
            // inputCount < InputBlockSize is not allowed
            ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount, InputBlockSize);

            if (outputBuffer == null)
                ThrowHelper.ThrowArgumentNull(ThrowHelper.ExceptionArgument.outputBuffer);

            // For now, only convert 3 bytes to 4
            Span<byte> input = inputBuffer.AsSpan(inputOffset, InputBlockSize);
            Span<byte> output = outputBuffer.AsSpan(outputOffset, OutputBlockSize);

            OperationStatus status = Base64.EncodeToUtf8(input, output, out int consumed, out int written, isFinalBlock: false);

            if (written != OutputBlockSize)
            {
                ThrowHelper.ThrowCryptographicException();
            }

            Debug.Assert(status == OperationStatus.Done);
            Debug.Assert(consumed == InputBlockSize);

            return written;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            // inputCount <= InputBlockSize is allowed
            ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            // Convert.ToBase64CharArray already does padding, so all we have to check is that
            // the inputCount wasn't 0
            if (inputCount == 0)
            {
                return Array.Empty<byte>();
            }
            else if (inputCount > InputBlockSize)
            {
                ThrowHelper.ThrowArgumentOutOfRange(ThrowHelper.ExceptionArgument.inputCount);
            }

            // Again, for now only a block at a time
            Span<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
            byte[] output = new byte[OutputBlockSize];

            OperationStatus status = Base64.EncodeToUtf8(input, output, out int consumed, out int written, isFinalBlock: true);

            if (written != OutputBlockSize)
            {
                ThrowHelper.ThrowCryptographicException();
            }

            Debug.Assert(status == OperationStatus.Done);
            Debug.Assert(consumed == inputCount);

            return output;
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
            // Dispose(false) in .NET Framework v2.0, we need to keep it in case any existing code had subclassed
            // this transform and expects to have a base class finalizer call its dispose method.
            Dispose(false);
        }
    }

    public class FromBase64Transform : ICryptoTransform
    {
        private byte[] _inputBuffer = new byte[4];
        private int _inputIndex;
        private readonly FromBase64TransformMode _whitespaces;

        public FromBase64Transform() : this(FromBase64TransformMode.IgnoreWhiteSpaces) { }
        public FromBase64Transform(FromBase64TransformMode whitespaces)
        {
            _whitespaces = whitespaces;
        }

        // A buffer with size 32 is stack allocated, to cover common cases and benefit from JIT's optimizations.
        private const int StackAllocSize = 32;

        // Converting from Base64 generates 3 bytes output from each 4 bytes input block
        public int InputBlockSize => 4;
        public int OutputBlockSize => 3;
        public bool CanTransformMultipleBlocks => true;
        public virtual bool CanReuseTransform => true;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            // inputCount != InputBlockSize is allowed
            ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            if (_inputBuffer == null)
                ThrowHelper.ThrowObjectDisposed();

            if (outputBuffer == null)
                ThrowHelper.ThrowArgumentNull(ThrowHelper.ExceptionArgument.outputBuffer);

            // The common case is inputCount = InputBlockSize
            byte[]? tmpBufferArray = null;
            Span<byte> tmpBuffer = stackalloc byte[StackAllocSize];
            if (inputCount > StackAllocSize)
            {
                tmpBuffer = tmpBufferArray = CryptoPool.Rent(inputCount);
            }

            tmpBuffer = GetTempBuffer(inputBuffer.AsSpan(inputOffset, inputCount), tmpBuffer);
            int bytesToTransform = _inputIndex + tmpBuffer.Length;

            // Too little data to decode: save data to _inputBuffer, so it can be transformed later
            if (bytesToTransform < InputBlockSize)
            {
                tmpBuffer.CopyTo(_inputBuffer.AsSpan(_inputIndex));

                _inputIndex = bytesToTransform;

                ReturnToCryptoPool(tmpBufferArray, tmpBuffer.Length);

                return 0;
            }

            ConvertFromBase64(tmpBuffer, outputBuffer.AsSpan(outputOffset), out _, out int written);

            ReturnToCryptoPool(tmpBufferArray, tmpBuffer.Length);

            return written;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            // inputCount != InputBlockSize is allowed
            ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            if (_inputBuffer == null)
            {
                ThrowHelper.ThrowObjectDisposed();
            }

            if (inputCount == 0)
            {
                return Array.Empty<byte>();
            }

            // The common case is inputCount <= Base64InputBlockSize
            byte[]? tmpBufferArray = null;
            Span<byte> tmpBuffer = stackalloc byte[StackAllocSize];
            if (inputCount > StackAllocSize)
            {
                tmpBuffer = tmpBufferArray = CryptoPool.Rent(inputCount);
            }

            tmpBuffer = GetTempBuffer(inputBuffer.AsSpan(inputOffset, inputCount), tmpBuffer);
            int bytesToTransform = _inputIndex + tmpBuffer.Length;

            // Too little data to decode
            if (bytesToTransform < InputBlockSize)
            {
                // reinitialize the transform
                Reset();

                ReturnToCryptoPool(tmpBufferArray, tmpBuffer.Length);

                return Array.Empty<byte>();
            }

            int outputSize = GetOutputSize(bytesToTransform, tmpBuffer);
            byte[] output = new byte[outputSize];

            ConvertFromBase64(tmpBuffer, output, out int consumed, out int written);
            Debug.Assert(written == outputSize);

            ReturnToCryptoPool(tmpBufferArray, tmpBuffer.Length);

            // reinitialize the transform
            Reset();

            return output;
        }

        private Span<byte> GetTempBuffer(Span<byte> inputBuffer, Span<byte> tmpBuffer)
        {
            if (_whitespaces == FromBase64TransformMode.DoNotIgnoreWhiteSpaces)
            {
                return inputBuffer;
            }

            return DiscardWhiteSpaces(inputBuffer, tmpBuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<byte> DiscardWhiteSpaces(Span<byte> inputBuffer, Span<byte> tmpBuffer)
        {
            int count = 0;

            for (int i = 0; i < inputBuffer.Length; i++)
            {
                if (!IsWhitespace(inputBuffer[i]))
                {
                    tmpBuffer[count++] = inputBuffer[i];
                }
            }

            return tmpBuffer.Slice(0, count);
        }

        private static bool IsWhitespace(byte value)
        {
            // We assume ASCII encoded data. If there is any non-ASCII char, it is invalid
            // Base64 and will be caught during decoding.

            // SPACE        32
            // TAB           9
            // LF           10
            // VTAB         11
            // FORM FEED    12
            // CR           13

            return value == 32 || ((uint)value - 9 <= (13 - 9));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetOutputSize(int bytesToTransform, Span<byte> tmpBuffer)
        {
            int outputSize = Base64.GetMaxDecodedFromUtf8Length(bytesToTransform);

            const byte padding = (byte)'=';
            int len = tmpBuffer.Length;

            // In Base64 there are maximum 2 padding chars

            if (tmpBuffer[len - 2] == padding)
            {
                outputSize--;
            }

            if (tmpBuffer[len - 1] == padding)
            {
                outputSize--;
            }

            return outputSize;
        }

        private void ConvertFromBase64(Span<byte> tmpBuffer, Span<byte> outputBuffer, out int consumed, out int written)
        {
            int bytesToTransform = _inputIndex + tmpBuffer.Length;
            Debug.Assert(bytesToTransform >= 4);

            byte[]? transformBufferArray = null;
            Span<byte> transformBuffer = stackalloc byte[StackAllocSize];
            if (bytesToTransform > StackAllocSize)
            {
                transformBuffer = transformBufferArray = CryptoPool.Rent(bytesToTransform);
            }

            // Copy _inputBuffer to transformBuffer and append tmpBuffer
            Debug.Assert(_inputIndex < _inputBuffer.Length);
            _inputBuffer.AsSpan(0, _inputIndex).CopyTo(transformBuffer);
            tmpBuffer.CopyTo(transformBuffer.Slice(_inputIndex));

            // Save data that won't be transformed to _inputBuffer, so it can be transformed later
            _inputIndex = bytesToTransform & 3;     // bit hack for % 4
            bytesToTransform -= _inputIndex;        // only transform up to the next multiple of 4
            Debug.Assert(_inputIndex < _inputBuffer.Length);
            tmpBuffer.Slice(tmpBuffer.Length - _inputIndex).CopyTo(_inputBuffer);

            transformBuffer = transformBuffer.Slice(0, bytesToTransform);
            OperationStatus status = Base64.DecodeFromUtf8(transformBuffer, outputBuffer, out consumed, out written);

            if (status == OperationStatus.Done)
            {
                Debug.Assert(consumed == bytesToTransform);
            }
            else
            {
                Debug.Assert(status == OperationStatus.InvalidData);
                ThrowHelper.ThrowBase64FormatException();
            }

            ReturnToCryptoPool(transformBufferArray, transformBuffer.Length);
        }

        private void ReturnToCryptoPool(byte[]? array, int clearSize)
        {
            if (array != null)
            {
                CryptoPool.Return(array, clearSize);
            }
        }

        public void Clear()
        {
            Dispose();
        }

        // Reset the state of the transform so it can be used again
        private void Reset()
        {
            _inputIndex = 0;
        }

        // must implement IDisposable, which in this case means clearing the input buffer

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // we always want to clear the input buffer
            if (disposing)
            {
                if (_inputBuffer != null)
                {
                    CryptographicOperations.ZeroMemory(_inputBuffer);
                    _inputBuffer = null!;
                }

                Reset();
            }
        }

        ~FromBase64Transform()
        {
            Dispose(false);
        }
    }

    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ValidateTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            if (inputBuffer == null)
                ThrowArgumentNull(ExceptionArgument.inputBuffer);

            if ((uint)inputCount > inputBuffer.Length)
                ThrowArgumentOutOfRange(ExceptionArgument.inputCount);

            if (inputOffset < 0)
                ThrowArgumentOutOfRange(ExceptionArgument.inputOffset);

            if ((inputBuffer.Length - inputCount) < inputOffset)
                ThrowInvalidOffLen();
        }

        public static void ValidateTransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, int inputBlockSize)
        {
            ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            if (inputCount < inputBlockSize)
                ThrowArgumentOutOfRange(ExceptionArgument.inputCount);
        }

        [DoesNotReturn]
        public static void ThrowArgumentNull(ExceptionArgument argument) => throw new ArgumentNullException(argument.ToString());
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRange(ExceptionArgument argument) => throw new ArgumentOutOfRangeException(argument.ToString(), SR.ArgumentOutOfRange_NeedNonNegNum);
        [DoesNotReturn]
        public static void ThrowInvalidOffLen() => throw new ArgumentException(SR.Argument_InvalidOffLen);
        [DoesNotReturn]
        public static void ThrowObjectDisposed() => throw new ObjectDisposedException(null, SR.ObjectDisposed_Generic);
        [DoesNotReturn]
        public static void ThrowCryptographicException() => throw new CryptographicException(SR.Cryptography_SSE_InvalidDataSize);
        [DoesNotReturn]
        public static void ThrowBase64FormatException() => throw new FormatException();

        public enum ExceptionArgument
        {
            inputBuffer,
            outputBuffer,
            inputOffset,
            inputCount
        }
    }
}
