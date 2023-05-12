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
        public bool CanTransformMultipleBlocks => true;
        public virtual bool CanReuseTransform => true;

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            int inputBlocks = Math.DivRem(inputCount, InputBlockSize, out int inputRemainder);

            if (inputBlocks == 0)
                ThrowHelper.ThrowArgumentOutOfRange(ThrowHelper.ExceptionArgument.inputCount);

            if (outputBuffer == null)
                ThrowHelper.ThrowArgumentNull(ThrowHelper.ExceptionArgument.outputBuffer);

            if (inputRemainder != 0)
                ThrowHelper.ThrowArgumentOutOfRange(ThrowHelper.ExceptionArgument.inputCount);

            int requiredOutputLength = checked(inputBlocks * OutputBlockSize);
            if (requiredOutputLength > outputBuffer.Length - outputOffset)
                ThrowHelper.ThrowArgumentOutOfRange(ThrowHelper.ExceptionArgument.outputBuffer);

            Span<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);
            Span<byte> output = outputBuffer.AsSpan(outputOffset, requiredOutputLength);

            OperationStatus status = Base64.EncodeToUtf8(input, output, out int consumed, out int written, isFinalBlock: false);

            Debug.Assert(status == OperationStatus.Done);
            Debug.Assert(consumed == input.Length);
            Debug.Assert(written == output.Length);

            return written;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            // inputCount <= InputBlockSize is allowed
            ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);

            // Convert.ToBase64CharArray already does padding, so all we have to check is that the inputCount wasn't 0
            if (inputCount == 0)
                return Array.Empty<byte>();

            Span<byte> input = inputBuffer.AsSpan(inputOffset, inputCount);

            int inputBlocks = Math.DivRem(inputCount, InputBlockSize, out int inputRemainder);
            int outputBlocks = inputBlocks + (inputRemainder != 0 ? 1 : 0);

            byte[] output = new byte[outputBlocks * OutputBlockSize];

            OperationStatus status = Base64.EncodeToUtf8(input, output, out int consumed, out int written, isFinalBlock: true);

            Debug.Assert(written == output.Length);
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
        /// <summary>Characters considered whitespace.</summary>
        /// <remarks>
        /// We assume ASCII encoded data. If there is any non-ASCII char, it is invalid
        /// Base64 and will be caught during decoding.
        /// SPACE        32
        /// TAB           9
        /// LF           10
        /// VTAB         11
        /// FORM FEED    12
        /// CR           13
        /// </remarks>
        private static readonly SearchValues<byte> s_whiteSpace = SearchValues.Create(" \t\n\v\f\r"u8);
        private readonly FromBase64TransformMode _whitespaces;
        private byte[] _inputBuffer = new byte[4];
        private int _inputIndex;

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
            ObjectDisposedException.ThrowIf(_inputBuffer == null, typeof(FromBase64Transform));

            if (outputBuffer == null)
                ThrowHelper.ThrowArgumentNull(ThrowHelper.ExceptionArgument.outputBuffer);

            ReadOnlySpan<byte> inputBufferSpan = inputBuffer.AsSpan(inputOffset, inputCount);
            int bytesToTransform = _inputIndex + inputBufferSpan.Length;

            byte[]? transformBufferArray = null;
            Span<byte> transformBuffer = stackalloc byte[StackAllocSize];
            if (bytesToTransform > StackAllocSize)
            {
                transformBuffer = transformBufferArray = CryptoPool.Rent(inputCount);
            }

            transformBuffer = AppendInputBuffers(inputBufferSpan, transformBuffer);
            // update bytesToTransform since it can be less if some whitespace was discarded.
            bytesToTransform = transformBuffer.Length;

            // Too little data to decode: save data to _inputBuffer, so it can be transformed later
            if (bytesToTransform < InputBlockSize)
            {
                transformBuffer.CopyTo(_inputBuffer);

                _inputIndex = bytesToTransform;

                ReturnToCryptoPool(transformBufferArray, transformBuffer.Length);

                return 0;
            }

            ConvertFromBase64(transformBuffer, outputBuffer.AsSpan(outputOffset), out _, out int written);

            ReturnToCryptoPool(transformBufferArray, transformBuffer.Length);

            return written;
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            // inputCount != InputBlockSize is allowed
            ThrowHelper.ValidateTransformBlock(inputBuffer, inputOffset, inputCount);
            ObjectDisposedException.ThrowIf(_inputBuffer == null, typeof(FromBase64Transform));

            if (inputCount == 0)
            {
                return Array.Empty<byte>();
            }

            ReadOnlySpan<byte> inputBufferSpan = inputBuffer.AsSpan(inputOffset, inputCount);
            int bytesToTransform = _inputIndex + inputBufferSpan.Length;

            // The common case is inputCount <= Base64InputBlockSize
            byte[]? transformBufferArray = null;
            Span<byte> transformBuffer = stackalloc byte[StackAllocSize];

            if (bytesToTransform > StackAllocSize)
            {
                transformBuffer = transformBufferArray = CryptoPool.Rent(inputCount);
            }

            transformBuffer = AppendInputBuffers(inputBufferSpan, transformBuffer);
            // update bytesToTransform since it can be less if some whitespace was discarded.
            bytesToTransform = transformBuffer.Length;

            // Too little data to decode
            if (bytesToTransform < InputBlockSize)
            {
                // reinitialize the transform
                Reset();

                ReturnToCryptoPool(transformBufferArray, transformBuffer.Length);

                return Array.Empty<byte>();
            }

            int outputSize = GetOutputSize(bytesToTransform, transformBuffer);
            byte[] output = new byte[outputSize];

            ConvertFromBase64(transformBuffer, output, out int consumed, out int written);
            Debug.Assert(written == outputSize);

            ReturnToCryptoPool(transformBufferArray, transformBuffer.Length);

            // reinitialize the transform
            Reset();

            return output;
        }

        private Span<byte> AppendInputBuffers(ReadOnlySpan<byte> inputBuffer, Span<byte> transformBuffer)
        {
            int index = _inputIndex;
            _inputBuffer.AsSpan(0, index).CopyTo(transformBuffer);

            if (_whitespaces == FromBase64TransformMode.DoNotIgnoreWhiteSpaces)
            {
                if (inputBuffer.IndexOfAny(s_whiteSpace) >= 0)
                {
                    ThrowHelper.ThrowBase64FormatException();
                }
            }
            else
            {
                int whitespaceIndex;
                while ((whitespaceIndex = inputBuffer.IndexOfAny(s_whiteSpace)) >= 0)
                {
                    inputBuffer.Slice(0, whitespaceIndex).CopyTo(transformBuffer.Slice(index));
                    index += whitespaceIndex;
                    inputBuffer = inputBuffer.Slice(whitespaceIndex);

                    do
                    {
                        inputBuffer = inputBuffer.Slice(1);
                    }
                    while (!inputBuffer.IsEmpty && s_whiteSpace.Contains(inputBuffer[0]));
                }
            }

            inputBuffer.CopyTo(transformBuffer.Slice(index));
            return transformBuffer.Slice(0, index + inputBuffer.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetOutputSize(int bytesToTransform, Span<byte> tmpBuffer)
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

        private void ConvertFromBase64(Span<byte> transformBuffer, Span<byte> outputBuffer, out int consumed, out int written)
        {
            int bytesToTransform = transformBuffer.Length;
            Debug.Assert(bytesToTransform >= 4);

            // Save data that won't be transformed to _inputBuffer, so it can be transformed later
            _inputIndex = bytesToTransform & 3;     // bit hack for % 4
            bytesToTransform -= _inputIndex;        // only transform up to the next multiple of 4
            Debug.Assert(_inputIndex < _inputBuffer.Length);
            transformBuffer.Slice(transformBuffer.Length - _inputIndex).CopyTo(_inputBuffer);

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
        }

        private static void ReturnToCryptoPool(byte[]? array, int clearSize)
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

        [DoesNotReturn]
        public static void ThrowArgumentNull(ExceptionArgument argument) => throw new ArgumentNullException(argument.ToString());
        [DoesNotReturn]
        public static void ThrowArgumentOutOfRange(ExceptionArgument argument) => throw new ArgumentOutOfRangeException(argument.ToString(), SR.ArgumentOutOfRange_NeedNonNegNum);
        [DoesNotReturn]
        public static void ThrowInvalidOffLen() => throw new ArgumentException(SR.Argument_InvalidOffLen);
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
