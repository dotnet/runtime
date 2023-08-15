// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "pal_errors.h"
#include "pal_icushim_internal.h"
#include "pal_normalization.h"
#import <Foundation/Foundation.h>

static NSString* GetNormalizedStringForForm(NormalizationForm normalizationForm, NSString* sourceString)
{
    switch (normalizationForm)
    {
        case FormC:
            return sourceString.precomposedStringWithCanonicalMapping;
        case FormD:
            return sourceString.decomposedStringWithCanonicalMapping;
        case FormKC:
            return sourceString.precomposedStringWithCompatibilityMapping;
        case FormKD:
            return sourceString.decomposedStringWithCompatibilityMapping;
        default:
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
int32_t GlobalizationNative_IsNormalizedNative(NormalizationForm normalizationForm, const uint16_t* lpStr, int32_t cwStrLength)
{
    NSString *sourceString = [NSString stringWithCharacters: lpStr length: cwStrLength];
    NSString *normalizedString = GetNormalizedStringForForm(normalizationForm, sourceString);

    return normalizedString == NULL ? -1 : [sourceString isEqualToString: normalizedString];
}

/**
 * Append a code point to a string, overwriting 1 or 2 code units.
 * The offset points to the current end of the string contents
 * and is advanced (post-increment).
 * "Safe" macro, checks for a valid code point.
 * Converts code points outside of Basic Multilingual Plane into
 * corresponding surrogate pairs if sufficient space in the string.
 * High surrogate range: 0xD800 - 0xDBFF 
 * Low surrogate range: 0xDC00 - 0xDFFF
 * If the code point is not valid or a trail surrogate does not fit,
 * then isError is set to true.
 *
 * @param buffer const uint16_t * string buffer
 * @param offset string offset, must be offset<capacity
 * @param capacity size of the string buffer
 * @param codePoint code point to append
 * @param isError output bool set to true if an error occurs, otherwise not modified
 */
#define Append(buffer, offset, capacity, codePoint, isError) { \
    if ((offset) >= (capacity)) /* insufficiently sized destination buffer */ { \
        (isError) = InsufficientBuffer; \
    } else if ((uint32_t)(codePoint) > 0x10ffff) /* invalid code point */  { \
        (isError) = InvalidCodePoint; \
    } else if ((uint32_t)(codePoint) <= 0xffff) { \
        (buffer)[(offset)++] = (uint16_t)(codePoint); \
    } else { \
        (buffer)[(offset)++] = (uint16_t)(((codePoint) >> 10) + 0xd7c0); \
        (buffer)[(offset)++] = (uint16_t)(((codePoint)&0x3ff) | 0xdc00); \
    } \
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
int32_t GlobalizationNative_NormalizeStringNative(NormalizationForm normalizationForm, const uint16_t* lpSource, int32_t cwSourceLength, uint16_t* lpDst, int32_t cwDstLength)
{
    NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
    NSString *normalizedString = GetNormalizedStringForForm(normalizationForm, sourceString);
    int32_t index = 0;
    int32_t dstIdx = 0, isError = 0;
    uint16_t dstCodepoint;
    while (index < normalizedString.length && dstIdx < cwDstLength)
    {
        dstCodepoint = [normalizedString characterAtIndex: index];
        Append(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
        index++;
    }

    return isError || normalizedString == NULL ? 0 : [normalizedString length];
}
