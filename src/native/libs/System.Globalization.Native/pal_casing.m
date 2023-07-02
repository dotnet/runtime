// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_icushim_internal.h"
#include "pal_casing.h"
#include "pal_errors.h"

#import <Foundation/Foundation.h>

#if defined(TARGET_OSX) || defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)


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
ChangeCaseNative

Performs upper or lower casing of a string into a new buffer, taking into account the specified locale.
Returns 0 for success, non-zero on failure see ErrorCodes.
*/
int32_t GlobalizationNative_ChangeCaseNative(const uint16_t* localeName, int32_t lNameLength,
                                             const uint16_t* lpSrc, int32_t cwSrcLength, uint16_t* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    NSLocale *currentLocale;
    if(localeName == NULL || lNameLength == 0)
    {
        currentLocale = [NSLocale systemLocale];
    }
    else
    {
        NSString *locName = [NSString stringWithCharacters: localeName length: lNameLength];
        currentLocale = [NSLocale localeWithLocaleIdentifier:locName];
    }
    NSString *source = [NSString stringWithCharacters: lpSrc length: cwSrcLength];
    NSString *result = bToUpper ? [source uppercaseStringWithLocale:currentLocale] : [source lowercaseStringWithLocale:currentLocale];

    int32_t srcIdx = 0, dstIdx = 0, isError = 0;
    uint16_t dstCodepoint;
    while (srcIdx < result.length)
    {
        dstCodepoint = [result characterAtIndex:srcIdx++];
        Append(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
        if (isError)
            return isError;
    }
    return Success;
}

/*
Function:
ChangeCaseInvariantNative

Performs upper or lower casing of a string into a new buffer.
Returns 0 for success, non-zero on failure see ErrorCodes.
*/
int32_t GlobalizationNative_ChangeCaseInvariantNative(const uint16_t* lpSrc, int32_t cwSrcLength, uint16_t* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    NSString *source = [NSString stringWithCharacters: lpSrc length: cwSrcLength];
    NSString *result = bToUpper ? source.uppercaseString : source.lowercaseString;

    int32_t srcIdx = 0, dstIdx = 0, isError = 0;
    uint16_t dstCodepoint;
    while (srcIdx < result.length)
    {
        dstCodepoint = [result characterAtIndex:srcIdx++];
        Append(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
        if (isError)
            return isError;
    }
    return Success;
}

#endif
