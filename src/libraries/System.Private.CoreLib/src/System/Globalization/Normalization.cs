// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;

namespace System.Globalization
{
    internal static partial class Normalization
    {
        internal static bool IsNormalized(ReadOnlySpan<char> source, NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            CheckNormalizationForm(normalizationForm);

            // In Invariant mode we assume all characters are normalized because we don't support any linguistic operations on strings.
            // If it's ASCII && one of the 4 main forms, then it's already normalized.
            if (GlobalizationMode.Invariant || Ascii.IsValid(source))
            {
                return true;
            }

            return GlobalizationMode.UseNls ?
                NlsIsNormalized(source, normalizationForm) :
                IcuIsNormalized(source, normalizationForm);
        }

        internal static string Normalize(string strInput, NormalizationForm normalizationForm)
        {
            CheckNormalizationForm(normalizationForm);

            // In Invariant mode we assume all characters are normalized because we don't support any linguistic operations on strings.
            // If it's ASCII && one of the 4 main forms, then it's already normalized.
            if (GlobalizationMode.Invariant || Ascii.IsValid(strInput))
            {
                return strInput;
            }

            return GlobalizationMode.UseNls ?
                NlsNormalize(strInput, normalizationForm) :
                IcuNormalize(strInput, normalizationForm);
        }

        internal static bool TryNormalize(ReadOnlySpan<char> source, Span<char> destination, out int charsWritten, NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            CheckNormalizationForm(normalizationForm);

            if (source.Overlaps(destination))
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.InvalidOperation_SpanOverlappedOperation);
            }

            // In Invariant mode we assume all characters are normalized because we don't support any linguistic operations on strings.
            // If it's ASCII && one of the 4 main forms, then it's already normalized.
            if (GlobalizationMode.Invariant || Ascii.IsValid(source))
            {
                if (source.TryCopyTo(destination))
                {
                    charsWritten = source.Length;
                    return true;
                }

                charsWritten = 0;
                return false;
            }

            return GlobalizationMode.UseNls ?
                NlsTryNormalize(source, destination, out charsWritten, normalizationForm) :
                IcuTryNormalize(source, destination, out charsWritten, normalizationForm);
        }

        internal static int GetNormalizedLength(this ReadOnlySpan<char> source, NormalizationForm normalizationForm = NormalizationForm.FormC)
        {
            CheckNormalizationForm(normalizationForm);

            // In Invariant mode we assume all characters are normalized because we don't support any linguistic operations on strings.
            // If it's ASCII && one of the 4 main forms, then it's already normalized.
            if (GlobalizationMode.Invariant || Ascii.IsValid(source))
            {
                return source.Length;
            }

            return GlobalizationMode.UseNls ?
                NlsGetNormalizedLength(source, normalizationForm) :
                IcuGetNormalizedLength(source, normalizationForm);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckNormalizationForm(NormalizationForm normalizationForm)
        {
            if (normalizationForm != NormalizationForm.FormC &&
                normalizationForm != NormalizationForm.FormD &&
                normalizationForm != NormalizationForm.FormKC &&
                normalizationForm != NormalizationForm.FormKD)
            {
                throw new ArgumentException(SR.Argument_InvalidNormalizationForm, nameof(normalizationForm));
            }
        }
    }
}
