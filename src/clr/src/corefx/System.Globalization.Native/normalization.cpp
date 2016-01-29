// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <stdint.h>
#include <unicode/unorm2.h>

/*
 * These values should be kept in sync with System.Text.NormalizationForm
 */
enum class NormalizationForm : int32_t
{
    C = 0x1,
    D = 0x2,
    KC = 0x5,
    KD = 0x6
};

const UNormalizer2* GetNormalizerForForm(NormalizationForm normalizationForm, UErrorCode* pErrorCode)
{
    switch (normalizationForm)
    {
        case NormalizationForm::C:
            return unorm2_getNFCInstance(pErrorCode);
        case NormalizationForm::D:
            return unorm2_getNFDInstance(pErrorCode);
        case NormalizationForm::KC:
            return unorm2_getNFKCInstance(pErrorCode);
        case NormalizationForm::KD:
            return unorm2_getNFKDInstance(pErrorCode);
    }

    *pErrorCode = U_ILLEGAL_ARGUMENT_ERROR;
    return nullptr;
}

/*
Function:
IsNormalized

Used by System.StringNormalizationExtensions.IsNormalized to detect if a string
is in a certain
Unicode Normalization Form.

Return values:
0: lpStr is not normalized.
1: lpStr is normalized.
-1: internal error during normalization.
*/
extern "C" int32_t GlobalizationNative_IsNormalized(
    NormalizationForm normalizationForm, const UChar* lpStr, int32_t cwStrLength)
{
    UErrorCode err = U_ZERO_ERROR;
    const UNormalizer2* pNormalizer = GetNormalizerForForm(normalizationForm, &err);
    UBool isNormalized = unorm2_isNormalized(pNormalizer, lpStr, cwStrLength, &err);

    if (U_SUCCESS(err))
    {
        return isNormalized == TRUE ? 1 : 0;
    }
    else
    {
        return -1;
    }
}

/*
Function:
NormalizeString

Used by System.StringNormalizationExtensions.Normalize to normalize a string
into a certain
Unicode Normalization Form.

Return values:
0: internal error during normalization.
>0: the length of the normalized string (not counting the null terminator).
*/
extern "C" int32_t GlobalizationNative_NormalizeString(
    NormalizationForm normalizationForm, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    UErrorCode err = U_ZERO_ERROR;
    const UNormalizer2* pNormalizer = GetNormalizerForForm(normalizationForm, &err);
    int32_t normalizedLen = unorm2_normalize(pNormalizer, lpSrc, cwSrcLength, lpDst, cwDstLength, &err);

    return (U_SUCCESS(err) || (err == U_BUFFER_OVERFLOW_ERROR)) ? normalizedLen : 0;
}
