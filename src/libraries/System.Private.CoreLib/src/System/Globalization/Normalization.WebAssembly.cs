// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Globalization
{
    internal static partial class Normalization
    {
        private static unsafe bool JsIsNormalized(string strInput, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            ValidateArguments(strInput, normalizationForm);

            int ret = Interop.JsGlobalization.IsNormalized(normalizationForm, strInput, out int exception, out object ex_result);
            if (exception != 0)
                throw new Exception((string)ex_result);

            return ret == 1;
        }

        private static unsafe string JsNormalize(string strInput, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            ValidateArguments(strInput, normalizationForm);

            char[]? toReturn = null;
            try
            {
                const int StackallocThreshold = 512;

                Span<char> buffer = strInput.Length <= StackallocThreshold
                    ? stackalloc char[StackallocThreshold]
                    : (toReturn = ArrayPool<char>.Shared.Rent(strInput.Length));

                for (int attempt = 0; attempt < 2; attempt++)
                {
                    int realLen;
                    fixed (char* pDest = &MemoryMarshal.GetReference(buffer))
                    {
                        realLen = Interop.JsGlobalization.NormalizeString(normalizationForm, strInput, pDest, buffer.Length, out int exception, out object ex_result);
                        if (exception != 0)
                            throw new Exception((string)ex_result);
                    }

                    if (realLen <= buffer.Length)
                    {
                        ReadOnlySpan<char> result = buffer.Slice(0, realLen);
                        return result.SequenceEqual(strInput)
                            ? strInput
                            : new string(result);
                    }

                    Debug.Assert(realLen > StackallocThreshold);

                    if (attempt == 0)
                    {
                        if (toReturn != null)
                        {
                            // Clear toReturn first to ensure we don't return the same buffer twice
                            char[] temp = toReturn;
                            toReturn = null;
                            ArrayPool<char>.Shared.Return(temp);
                        }

                        buffer = toReturn = ArrayPool<char>.Shared.Rent(realLen);
                    }
                }

                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(strInput));
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
