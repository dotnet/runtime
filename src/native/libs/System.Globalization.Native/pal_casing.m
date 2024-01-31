// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_casing_hg.h"
#include "pal_hybrid.h"
#include "pal_errors.h"

#import <Foundation/Foundation.h>

#if !__has_feature(objc_arc)
#error This file relies on ARC for memory management, but ARC is not enabled.
#endif

/**
 * Is this code unit a lead surrogate (U+d800..U+dbff)?
 * @param c 16-bit code unit
 * @return true or false
 */
#define IS_LEAD(c) (((c)&0xfffffc00) == 0xd800)

/**
 * Is this code unit a trail surrogate (U+dc00..U+dfff)?
 * @param c 16-bit code unit
 * @return true or false
 */
#define IS_TRAIL(c) (((c)&0xfffffc00) == 0xdc00)

/**
 * Get a code point index from a string at a code point boundary offset,
 * and advance the offset to the next code point boundary.
 * (Post-incrementing forward iteration.)
 * "Safe" macro, handles unpaired surrogates and checks for string boundaries.
 *
 * The length can be negative for a NUL-terminated string.
 *
 * The offset may point to the lead surrogate unit
 * for a supplementary code point, in which case for casing will be read
 * the following trail surrogate as well.
 * If the offset points to a trail surrogate or
 * to a single, unpaired lead surrogate, then for casing will be read that unpaired surrogate.
 *
 * @param s const uint16_t* string
 * @param i output string offset, must be i<length
 * @param length string length
 */
#define NEXTOFFSET(s, i, length) { \
    uint16_t c = (s)[(i)++]; \
    if (IS_LEAD(c)) { \
        uint16_t __c2; \
        if ((i) != (length) && IS_TRAIL(__c2 = (s)[(i)])) { \
            ++(i); \
        } \
    } \
}

/*
Function:
ChangeCaseNative

Performs upper or lower casing of a string into a new buffer, taking into account the specified locale.
Two things we are considering here:
1. Prohibiting code point expansions. Some characters code points expand when uppercased or lowercased, which may lead to an insufficient destination buffer.
   Instead, we prohibit these expansions and iterate through the string character by character opting for the original character if it would have been expanded.
2. Properly handling surrogate pairs. Characters can be comprised of more than one code point
   (i.e. surrogate pairs like \uD801\uDC37). All code points for a character are needed to properly change case
Returns 0 for success, non-zero on failure see ErrorCodes.
*/
int32_t GlobalizationNative_ChangeCaseNative(const uint16_t* localeName, int32_t lNameLength,
                                             const uint16_t* lpSrc, int32_t cwSrcLength, uint16_t* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    @autoreleasepool
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

        int32_t srcIdx = 0, dstIdx = 0, isError = 0;
        uint16_t dstCodepoint;
        while (srcIdx < cwSrcLength)
        {
            int32_t startIndex = srcIdx;
            NEXTOFFSET(lpSrc, srcIdx, cwSrcLength);
            int32_t srcLength = srcIdx - startIndex;
            NSString *src = [NSString stringWithCharacters: lpSrc + startIndex length: srcLength];
            NSString *dst = bToUpper ? [src uppercaseStringWithLocale:currentLocale] : [src lowercaseStringWithLocale:currentLocale];
            int32_t index = 0;
            // iterate over all code points of a surrogate pair character
            while (index < srcLength)
            {
                // the dst.length > srcLength is to prevent code point expansions
                dstCodepoint = dst.length > srcLength ? [src characterAtIndex: index] : [dst characterAtIndex: index];
                Append(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                index++;
            }
            if (isError)
                return isError;
        }
        return Success;
    }
}

/*
Function:
ChangeCaseInvariantNative

Performs upper or lower casing of a string into a new buffer.
Two things we are considering here:
1. Prohibiting code point expansions. Some characters code points expand when uppercased or lowercased, which may lead to an insufficient destination buffer.
   Instead, we prohibit these expansions and iterate through the string character by character opting for the original character if it would have been expanded.
2. Properly handling surrogate pairs. Characters can be comprised of more than one code point
   (i.e. surrogate pairs like \uD801\uDC37). All code points for a character are needed to properly change case
Returns 0 for success, non-zero on failure see ErrorCodes.
*/
int32_t GlobalizationNative_ChangeCaseInvariantNative(const uint16_t* lpSrc, int32_t cwSrcLength, uint16_t* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    @autoreleasepool
    {
        int32_t srcIdx = 0, dstIdx = 0, isError = 0;
        uint16_t dstCodepoint;
        while (srcIdx < cwSrcLength)
        {
            int32_t startIndex = srcIdx;
            NEXTOFFSET(lpSrc, srcIdx, cwSrcLength);
            int32_t srcLength = srcIdx - startIndex;
            NSString *src = [NSString stringWithCharacters: lpSrc + startIndex length: srcLength];
            NSString *dst = bToUpper ? src.uppercaseString : src.lowercaseString;
            int32_t index = 0;
            // iterate over all code points of a surrogate pair character
            while (index < srcLength)
            {
                // the dst.length > srcLength is to prevent code point expansions
                dstCodepoint = dst.length > srcLength ? [src characterAtIndex: index] : [dst characterAtIndex: index];
                Append(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                index++;
            }
            if (isError)
                return isError;
        }
        return Success;
    }
}

