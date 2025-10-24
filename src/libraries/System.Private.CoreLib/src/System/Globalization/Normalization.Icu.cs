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
        private static unsafe bool IcuIsNormalized(ReadOnlySpan<char> source, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(!source.IsEmpty);
#pragma warning disable CA1416 // FormKC and FormKD are unsupported on browser, ValidateArguments is throwing PlatformNotSupportedException in that case so suppressing the warning here
            Debug.Assert(normalizationForm is NormalizationForm.FormC or NormalizationForm.FormD or NormalizationForm.FormKC or NormalizationForm.FormKD);
#pragma warning restore CA1416

            ValidateArguments(source, normalizationForm, nameof(source));

            int ret;
            fixed (char* pInput = source)
            {
#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
                if (GlobalizationMode.Hybrid)
                {
                    ret = Interop.Globalization.IsNormalizedNative(normalizationForm, pInput, source.Length);
                }
                else
#endif
                {
                    ret = Interop.Globalization.IsNormalized(normalizationForm, pInput, source.Length);
                }
            }

            if (ret == -1)
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(source));
            }

            return ret == 1;
        }

        private static unsafe string IcuNormalize(string strInput, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(normalizationForm == NormalizationForm.FormC || normalizationForm == NormalizationForm.FormD || normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD);

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
#endif
                        {
                            realLen = Interop.Globalization.NormalizeString(normalizationForm, pInput, strInput.Length, pDest, buffer.Length);
                        }
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

        private static unsafe bool IcuTryNormalize(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten, NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(normalizationForm == NormalizationForm.FormC || normalizationForm == NormalizationForm.FormD || normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD);

            if (destination.IsEmpty)
            {
                charsWritten = 0;
                return false;
            }

            ValidateArguments(source, normalizationForm, nameof(source));

            int realLen;
            fixed (char* pInput = source)
            fixed (char* pDest = destination)
            {
#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
                if (GlobalizationMode.Hybrid)
                {
                    realLen = Interop.Globalization.NormalizeStringNative(normalizationForm, pInput, source.Length, pDest, destination.Length);
                }
                else
#endif
                {
                    realLen = Interop.Globalization.NormalizeString(normalizationForm, pInput, source.Length, pDest, destination.Length);
                }
            }

            if (realLen < 0)
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(source));
            }

            if (realLen <= destination.Length)
            {
                charsWritten = realLen;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static unsafe int IcuGetNormalizedLength(ReadOnlySpan<char> source, NormalizationForm normalizationForm)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(!GlobalizationMode.UseNls);
            Debug.Assert(!source.IsEmpty);
            Debug.Assert(normalizationForm == NormalizationForm.FormC || normalizationForm == NormalizationForm.FormD || normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD);

            ValidateArguments(source, normalizationForm, nameof(source));

            int realLen;
            fixed (char* pInput = source)
            {
#if TARGET_MACCATALYST || TARGET_IOS || TARGET_TVOS
                if (GlobalizationMode.Hybrid)
                {
                    realLen = Interop.Globalization.NormalizeStringNative(normalizationForm, pInput, source.Length, null, 0);
                }
                else
#endif
                {
                    realLen = Interop.Globalization.NormalizeString(normalizationForm, pInput, source.Length, null, 0);
                }
            }

            if (realLen < 0)
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, nameof(source));
            }

            return realLen;
        }

        private static void ValidateArguments(ReadOnlySpan<char> strInput, NormalizationForm normalizationForm, string paramName = "strInput")
        {
            if ((OperatingSystem.IsBrowser() || OperatingSystem.IsWasi()) && (normalizationForm == NormalizationForm.FormKC || normalizationForm == NormalizationForm.FormKD))
            {
                // Browser's ICU doesn't contain data needed for FormKC and FormKD
                throw new PlatformNotSupportedException(SR.Argument_UnsupportedNormalizationFormInBrowser);
            }

            if (HasInvalidUnicodeSequence(strInput))
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, paramName);
            }
        }

        /// <summary>
        /// ICU does not signal an error during normalization if the input string has invalid unicode,
        /// unlike Windows (which uses the ERROR_NO_UNICODE_TRANSLATION error value to signal an error).
        ///
        /// We walk the string ourselves looking for these bad sequences so we can continue to throw
        /// ArgumentException in these cases.
        /// </summary>
        private static bool HasInvalidUnicodeSequence(ReadOnlySpan<char> s)
        {
            const char Noncharacter = '\uFFFE';

            int i = s.IndexOfAnyInRange(CharUnicodeInfo.HIGH_SURROGATE_START, Noncharacter);

            for (; (uint)i < (uint)s.Length; i++)
            {
                char c = s[i];

                if (c < CharUnicodeInfo.HIGH_SURROGATE_START)
                {
                    continue;
                }

                if (c == Noncharacter)
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
                    if ((uint)(i + 1) >= (uint)s.Length || !char.IsLowSurrogate(s[i + 1]))
                    {
                        // A high surrogate at the end of the string or a high surrogate
                        // not followed by a low surrogate
                        return true;
                    }

                    i++; // consume the low surrogate.
                }
            }

            return false;
        }
    }
}
