// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include "pal_errors.h"
#include "pal_icushim_internal.h"
#include "pal_normalization.h"
#import <Foundation/Foundation.h>

#if !__has_feature(objc_arc)
#error This file relies on ARC for memory management, but ARC is not enabled.
#endif

#if defined(APPLE_HYBRID_GLOBALIZATION)
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
is in a certain Unicode Normalization Form.

Return values:
0: lpStr is not normalized.
1: lpStr is normalized.
-1: internal error during normalization.
*/
int32_t GlobalizationNative_IsNormalizedNative(NormalizationForm normalizationForm, const uint16_t* lpStr, int32_t cwStrLength)
{
    @autoreleasepool
    {
        NSString *sourceString = [NSString stringWithCharacters: lpStr length: cwStrLength];
        NSString *normalizedString = GetNormalizedStringForForm(normalizationForm, sourceString);

        return normalizedString == NULL ? -1 : [sourceString isEqualToString: normalizedString];
    }
}

/*
Function:
NormalizeString

Used by System.StringNormalizationExtensions.Normalize to normalize a string
into a certain Unicode Normalization Form.

Return values:
0: internal error during normalization.
>0: the length of the normalized string (not counting the null terminator).
*/
int32_t GlobalizationNative_NormalizeStringNative(NormalizationForm normalizationForm, const uint16_t* lpSource, int32_t cwSourceLength, uint16_t* lpDst, int32_t cwDstLength)
{
    @autoreleasepool
    {
        NSString *sourceString = [NSString stringWithCharacters: lpSource length: cwSourceLength];
        NSString *normalizedString = GetNormalizedStringForForm(normalizationForm, sourceString);

        if (normalizedString == NULL || normalizedString.length == 0)
        {
            return 0;
        }

        int32_t index = 0, dstIdx = 0, isError = 0;
        uint16_t dstCodepoint;
        while (index < normalizedString.length)
        {
            dstCodepoint = [normalizedString characterAtIndex: index];
            Append(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            index++;
        }

        return !isError ? [normalizedString length] : 0;
    }
}
#endif

