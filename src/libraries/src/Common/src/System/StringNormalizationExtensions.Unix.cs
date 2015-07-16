// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using System.Text;

namespace System
{
    static partial class StringNormalizationExtensions
    {
        [SecurityCritical]
        public static bool IsNormalized(this string value, NormalizationForm normalizationForm)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (normalizationForm != NormalizationForm.FormC && normalizationForm != NormalizationForm.FormD &&
                normalizationForm != NormalizationForm.FormKC && normalizationForm != NormalizationForm.FormKD)
            {
                throw new ArgumentException(SR.Argument_InvalidNormalizationForm, "normalizationForm");
            }

            if (HasInvalidUnicodeSequence(value))
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "value");
            }

            int ret = Interop.GlobalizationInterop.IsNormalized(normalizationForm, value, value.Length);

            if (ret == -1)
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "value");
            }

            return ret == 1;
        }

        [SecurityCritical]
        public static string Normalize(this string value, NormalizationForm normalizationForm)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            if (normalizationForm != NormalizationForm.FormC && normalizationForm != NormalizationForm.FormD &&
                normalizationForm != NormalizationForm.FormKC && normalizationForm != NormalizationForm.FormKD)
            {
                throw new ArgumentException(SR.Argument_InvalidNormalizationForm, "normalizationForm");
            }

            if (HasInvalidUnicodeSequence(value))
            {
                throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "value");
            }

            char[] buf = new char[value.Length];

            for (int attempts = 2; attempts > 0; attempts--)
            {
                int realLen = Interop.GlobalizationInterop.NormalizeString(normalizationForm, value, value.Length, buf, buf.Length);

                if (realLen == -1)
                {
                    throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "value");
                }

                if (realLen <= buf.Length)
                {
                    return new string(buf, 0, realLen);
                }

                buf = new char[realLen];
            }

            throw new ArgumentException(SR.Argument_InvalidCharSequenceNoIndex, "value");
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

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
                if (s[i] == '\uFFFE')
                {
                    return true;
                }

                if (char.IsLowSurrogate(s[i]))
                {
                    return true;
                }

                if (char.IsHighSurrogate(s[i]))
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
