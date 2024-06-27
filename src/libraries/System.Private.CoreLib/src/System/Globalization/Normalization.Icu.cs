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
        private static unsafe bool IcuIsNormalized(string strInput, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);

            ValidateArguments(strInput, normalizationForm);

            int ret;
            fixed (char* pInput = strInput)
            {
#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
                if (GlobalizationMode.Hybrid)
                {
                    ret = Interop.Globalization.IsNormalizedNative(normalizationForm, pInput, strInput.Length);
                }
                else
#else
                {
                    ret = Interop.Globalization.IsNormalized(normalizationForm, pInput, strInput.Length);
                }
#endif
            }

            if (ret == -1)
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(strInput));
            }

            return ret == 1;
        }

        private static unsafe string IcuNormalize(string strInput, NormalizationForm normalizationForm)
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
                    fixed (char* pInput = strInput)
                    fixed (char* pDest = &MemoryMarshal.GetReference(buffer))
                    {
#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
                    if (GlobalizationMode.Hybrid)
                    {
                        realLen = Interop.Globalization.NormalizeStringNative(normalizationForm, pInput, strInput.Length, pDest, buffer.Length);
                    }
                    else
#else
                    {
                        realLen = Interop.Globalization.NormalizeString(normalizationForm, pInput, strInput.Length, pDest, buffer.Length);
                    }
#endif
                    }

                    if (realLen == -1)
                    {
                        throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(strInput));
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

        private static void ValidateArguments(string strInput, NormalizationForm normalizationForm)
        {
            Debug.Assert(strInput != null);

            if (OperatingSystem.IsBrowser() && (normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD))
            {
                // Browser's ICU doesn't contain data needed for FormKC and FormKD
                throw new PlatformNotSupportedException();
            }

            if (normalizationForm != NormalizationForm.FormC && normalizationForm != NormalizationForm.FormD &&
                normalizationForm != NormalizationForm.FormKC && normalizationForm != NormalizationForm.FormKD)
            {
                throw new ArgumentException(SR.Argument_InvalidNormalizationForm, nameof(normalizationForm));
            }

            if (HasInvalidUnicodeSequence(strInput))
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(strInput));
            }
        }

        /// <summary>
        /// ICU does not signal an error during normalization if the input string has invalid unicode,
        /// unlike Windows (which uses the ERROR_NO_UNICODE_TRANSLATION error value to signal an error).
        ///
        /// We walk the string ourselves looking for these bad sequences so we can continue to throw
        /// ArgumentException in these cases.
        /// </summary>
        private static bool HasInvalidUnicodeSequence(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];

                if (c < '\ud800')
                {
                    continue;
                }

                if (c == '\uFFFE')
                {
                    return true;
                }

                // If we see low surrogate before a high one, the string is invalid.
                if (char.IsLowSurrogate(c))
                {
                    return true;
                }

                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 >= s.Length || !char.IsLowSurrogate(s[i + 1]))
                    {
                        // A high surrogate at the end of the string or a high surrogate
                        // not followed by a low surrogate
                        return true;
                    }
                    else
                    {
                        i++; // consume the low surrogate.
                        continue;
                    }
                }
            }

            return false;
        }
    }
}
