// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal static partial class Normalization
    {
        internal static unsafe bool IsNormalized(string strInput, NormalizationForm normalizationForm)
        {
            if (GlobalizationMode.Invariant)
            {
                // In Invariant mode we assume all characters are normalized.
                // This is because we don't support any linguistic operation on the strings
                return true;
            }

            Debug.Assert(strInput != null);

            // The only way to know if IsNormalizedString failed is through checking the Win32 last error
            // IsNormalizedString pinvoke has SetLastError attribute property which will set the last error
            // to 0 (ERROR_SUCCESS) before executing the calls.
            Interop.BOOL result;
            fixed (char* pInput = strInput)
            {
                result = Interop.Normaliz.IsNormalizedString(normalizationForm, pInput, strInput.Length);
            }

            int lastError = Marshal.GetLastWin32Error();
            switch (lastError)
            {
                case Interop.Errors.ERROR_SUCCESS:
                    break;

                case Interop.Errors.ERROR_INVALID_PARAMETER:
                case Interop.Errors.ERROR_NO_UNICODE_TRANSLATION:
                    if (normalizationForm != NormalizationForm.FormC &&
                        normalizationForm != NormalizationForm.FormD &&
                        normalizationForm != NormalizationForm.FormKC &&
                        normalizationForm != NormalizationForm.FormKD)
                    {
                        throw new ArgumentException(SR.Argument_InvalidNormalizationForm, nameof(normalizationForm));
                    }

                    throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(strInput));

                case Interop.Errors.ERROR_NOT_ENOUGH_MEMORY:
                    throw new OutOfMemoryException();

                default:
                    throw new InvalidOperationException(SR.Format(SR.UnknownError_Num, lastError));
            }

            return result != Interop.BOOL.FALSE;
        }

        internal static unsafe string Normalize(string strInput, NormalizationForm normalizationForm)
        {
            if (GlobalizationMode.Invariant)
            {
                // In Invariant mode we assume all characters are normalized.
                // This is because we don't support any linguistic operation on the strings
                return strInput;
            }

            Debug.Assert(strInput != null);

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
                    int lastError = Marshal.GetLastWin32Error();

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
                            if (normalizationForm != NormalizationForm.FormC &&
                                normalizationForm != NormalizationForm.FormD &&
                                normalizationForm != NormalizationForm.FormKC &&
                                normalizationForm != NormalizationForm.FormKD)
                            {
                                throw new ArgumentException(SR.Argument_InvalidNormalizationForm, nameof(normalizationForm));
                            }

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
    }
}
