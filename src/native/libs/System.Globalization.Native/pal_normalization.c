// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdbool.h>
#include <stdint.h>

#include "pal_icushim_internal.h"
#include "pal_common.h"
#include "pal_normalization.h"

static const UNormalizer2* GetNormalizerForForm(NormalizationForm normalizationForm, UErrorCode* pErrorCode)
{
    switch (normalizationForm)
    {
        case FormC:
            return unorm2_getNFCInstance(pErrorCode);
        case FormD:
            return unorm2_getNFDInstance(pErrorCode);
        case FormKC:
            return unorm2_getNFKCInstance(pErrorCode);
        case FormKD:
            return unorm2_getNFKDInstance(pErrorCode);
        default:
            *pErrorCode = U_ILLEGAL_ARGUMENT_ERROR;
            return NULL;
    }
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
int32_t GlobalizationNative_IsNormalized(
    NormalizationForm normalizationForm, const UChar* lpStr, int32_t cwStrLength)
{
    UErrorCode err = U_ZERO_ERROR;
    const UNormalizer2* pNormalizer = GetNormalizerForForm(normalizationForm, &err);
    UBool isNormalized = unorm2_isNormalized(pNormalizer, lpStr, cwStrLength, &err);

    if (U_SUCCESS(err))
    {
        return isNormalized ? 1 : 0;
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
int32_t GlobalizationNative_NormalizeString(
    NormalizationForm normalizationForm, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    UErrorCode err = U_ZERO_ERROR;
    const UNormalizer2* pNormalizer = GetNormalizerForForm(normalizationForm, &err);
    int32_t normalizedLen = unorm2_normalize(pNormalizer, lpSrc, cwSrcLength, lpDst, cwDstLength, &err);

    return (U_SUCCESS(err) || (err == U_BUFFER_OVERFLOW_ERROR)) ? normalizedLen : 0;
}
