// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal static partial class Normalization
    {
        private static unsafe bool NlsIsNormalized(ReadOnlySpan<char> source, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(normalizationForm == NormalizationForm.FormC || normalizationForm == NormalizationForm.FormD || normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD);

            Interop.BOOL result;
            fixed (char* pInput = source)
            {
                result = Interop.Normaliz.IsNormalizedString(normalizationForm, pInput, source.Length);
            }

            // The only way to know if IsNormalizedString failed is through checking the Win32 last error
            // IsNormalizedString pinvoke has SetLastError attribute property which will set the last error
            // to 0 (ERROR_SUCCESS) before executing the calls.
            CheckLastErrorAndThrowIfFailed(nameof(source));

            return result != Interop.BOOL.FALSE;
        }

        private static unsafe string NlsNormalize(string strInput, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert(strInput != null);
            Debug.Assert(normalizationForm == NormalizationForm.FormC || normalizationForm == NormalizationForm.FormD || normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD);

            if (strInput.Length == 0)
            {
                return string.Empty;
            }

            char[]? toReturn = null;
            try
            {
                const int StackallocThreshold = 512;

                Span<char> buffer = strInput.Length <= StackallocThreshold
                    ? stackalloc char[StackallocThreshold]
                    : (toReturn = ArrayPool<char>.Shared.Rent(strInput.Length));

                while (true)
                {
                    // we depend on Win32 last error when calling NormalizeString
                    // NormalizeString pinvoke has SetLastError attribute property which will set the last error
                    // to 0 (ERROR_SUCCESS) before executing the calls.
                    int realLength;
                    fixed (char* pInput = strInput)
                    fixed (char* pDest = &MemoryMarshal.GetReference(buffer))
                    {
                        realLength = Interop.Normaliz.NormalizeString(normalizationForm, pInput, strInput.Length, pDest, buffer.Length);
                    }
                    int lastError = Marshal.GetLastPInvokeError();

                    switch (lastError)
                    {
                        case Interop.Errors.ERROR_SUCCESS:
                            ReadOnlySpan<char> result = buffer.Slice(0, realLength);
                            return result.SequenceEqual(strInput)
                                ? strInput
                                : new string(result);

                        // Do appropriate stuff for the individual errors:
                        case Interop.Errors.ERROR_INSUFFICIENT_BUFFER:
                            realLength = Math.Abs(realLength);
                            Debug.Assert(realLength > buffer.Length, "Buffer overflow should have iLength > cBuffer.Length");
                            if (toReturn != null)
                            {
                                // Clear toReturn first to ensure we don't return the same buffer twice
                                char[] temp = toReturn;
                                toReturn = null;
                                ArrayPool<char>.Shared.Return(temp);
                            }
                            Debug.Assert(realLength > StackallocThreshold);
                            buffer = toReturn = ArrayPool<char>.Shared.Rent(realLength);
                            continue;

                        case Interop.Errors.ERROR_INVALID_PARAMETER:
                        case Interop.Errors.ERROR_NO_UNICODE_TRANSLATION:
                            // Illegal code point or order found.  Ie: FFFE or D800 D800, etc.
                            throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(strInput));

                        case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
                            throw new OutOfMemoryException();

                        default:
                            // We shouldn't get here...
                            throw new InvalidOperationException(SR.Format(SR.UnknownError_Num, lastError));
                    }
                }
            }
            finally
            {
                if (toReturn != null)
                {
                    ArrayPool<char>.Shared.Return(toReturn);
                }
            }
        }

        private static unsafe bool NlsTryNormalize(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten, NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(normalizationForm == NormalizationForm.FormC || normalizationForm == NormalizationForm.FormD || normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD);

            if (destination.IsEmpty)
            {
                charsWritten = 0;
                return false;
            }

            // we depend on Win32 last error when calling NormalizeString
            // NormalizeString pinvoke has SetLastError attribute property which will set the last error
            // to 0 (ERROR_SUCCESS) before executing the calls.

            int realLength;
            fixed (char* pInput = source)
            fixed (char* pDest = destination)
            {
                realLength = Interop.Normaliz.NormalizeString(normalizationForm, pInput, source.Length, pDest, destination.Length);
            }

            int lastError = Marshal.GetLastPInvokeError();
            switch (lastError)
            {
                case Interop.Errors.ERROR_SUCCESS:
                    charsWritten = realLength;
                    return true;

                // Do appropriate stuff for the individual errors:
                case Interop.Errors.ERROR_INSUFFICIENT_BUFFER:
                    charsWritten = 0;
                    return false;

                case Interop.Errors.ERROR_INVALID_PARAMETER:
                case Interop.Errors.ERROR_NO_UNICODE_TRANSLATION:
                    // Illegal code point or order found.  Ie: FFFE or D800 D800, etc.
                    throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(source));

                case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
                    throw new OutOfMemoryException();

                default:
                    // We shouldn't get here...
                    throw new InvalidOperationException(SR.Format(SR.UnknownError_Num, lastError));
            }
        }

        private static unsafe int NlsGetNormalizedLength(ReadOnlySpan<char> source, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(normalizationForm == NormalizationForm.FormC || normalizationForm == NormalizationForm.FormD || normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD);

            // we depend on Win32 last error when calling NormalizeString
            // NormalizeString pinvoke has SetLastError attribute property which will set the last error
            // to 0 (ERROR_SUCCESS) before executing the calls.

            int realLength;
            fixed (char* pInput = source)
            {
                realLength = Interop.Normaliz.NormalizeString(normalizationForm, pInput, source.Length, null, 0);
            }

            int lastError = Marshal.GetLastPInvokeError();
            switch (lastError)
            {
                case Interop.Errors.ERROR_SUCCESS:
                    return realLength;

                case Interop.Errors.ERROR_INVALID_PARAMETER:
                case Interop.Errors.ERROR_NO_UNICODE_TRANSLATION:
                    // Illegal code point or order found.  Ie: FFFE or D800 D800, etc.
                    throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(source));

                case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
                    throw new OutOfMemoryException();

                default:
                    // We shouldn't get here...
                    throw new InvalidOperationException(SR.Format(SR.UnknownError_Num, lastError));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckLastErrorAndThrowIfFailed(string inputName)
        {
            int lastError = Marshal.GetLastPInvokeError();
            switch (lastError)
            {
                case Interop.Errors.ERROR_SUCCESS:
                    break;

                case Interop.Errors.ERROR_INVALID_PARAMETER:
                case Interop.Errors.ERROR_NO_UNICODE_TRANSLATION:
                    throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, inputName);

                case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
                    throw new OutOfMemoryException();

                default:
                    throw new InvalidOperationException(SR.Format(SR.UnknownError_Num, lastError));
            }
        }
    }
}
