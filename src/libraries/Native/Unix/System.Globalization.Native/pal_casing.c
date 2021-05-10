// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <assert.h>
#include <stdbool.h>
#include <stdint.h>

#include "pal_icushim_internal.h"
#include "pal_casing.h"

#ifdef __clang__
// Workaround for warnings produced by U16_NEXT and U16_APPEND macro expansions
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wsign-conversion"
#endif

/*
Function:
ChangeCase

Performs upper or lower casing of a string into a new buffer.
No special casing is performed beyond that provided by ICU.
*/
void GlobalizationNative_ChangeCase(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    // Iterate through the string, decoding the next one or two UTF-16 code units
    // into a codepoint and updating srcIdx to point to the next UTF-16 code unit
    // to decode.  Then upper or lower case it, write dstCodepoint into lpDst at
    // offset dstIdx, and update dstIdx.

    // (The loop here has been manually cloned for each of the four cases, rather
    // than having a single loop that internally branched based on bToUpper as the
    // compiler wasn't doing that optimization, and it results in an ~15-20% perf
    // improvement on longer strings.)

    UBool isError = false;
    (void)isError; // only used for assert
    int32_t srcIdx = 0, dstIdx = 0;
    UChar32 srcCodepoint, dstCodepoint;

    if (bToUpper)
    {
        while (srcIdx < cwSrcLength)
        {
            U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            dstCodepoint = u_toupper(srcCodepoint);
            U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            assert(isError == false && srcIdx == dstIdx);
        }
    }
    else
    {
        while (srcIdx < cwSrcLength)
        {
            U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            dstCodepoint = u_tolower(srcCodepoint);
            U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            assert(isError == false && srcIdx == dstIdx);
        }
    }
}

/*
Function:
ChangeCaseInvariant

Performs upper or lower casing of a string into a new buffer.
Special casing is performed to ensure that invariant casing
matches that of Windows in certain situations, e.g. Turkish i's.
*/
void GlobalizationNative_ChangeCaseInvariant(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    // See algorithmic comment in ChangeCase.

    UBool isError = false;
    (void)isError; // only used for assert
    int32_t srcIdx = 0, dstIdx = 0;
    UChar32 srcCodepoint, dstCodepoint;

    if (bToUpper)
    {
        while (srcIdx < cwSrcLength)
        {
            // On Windows with InvariantCulture, the LATIN SMALL LETTER DOTLESS I (U+0131)
            // capitalizes to itself, whereas with ICU it capitalizes to LATIN CAPITAL LETTER I (U+0049).
            // We special case it to match the Windows invariant behavior.
            U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            dstCodepoint = ((srcCodepoint == (UChar32)0x0131) ? (UChar32)0x0131 : u_toupper(srcCodepoint));
            U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            assert(isError == false && srcIdx == dstIdx);
        }
    }
    else
    {
        while (srcIdx < cwSrcLength)
        {
            // On Windows with InvariantCulture, the LATIN CAPITAL LETTER I WITH DOT ABOVE (U+0130)
            // lower cases to itself, whereas with ICU it lower cases to LATIN SMALL LETTER I (U+0069).
            // We special case it to match the Windows invariant behavior.
            U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            dstCodepoint = ((srcCodepoint == (UChar32)0x0130) ? (UChar32)0x0130 : u_tolower(srcCodepoint));
            U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            assert(isError == false && srcIdx == dstIdx);
        }
    }
}

/*
Function:
ChangeCaseTurkish

Performs upper or lower casing of a string into a new buffer, performing special
casing for Turkish.
*/
void GlobalizationNative_ChangeCaseTurkish(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper)
{
    // See algorithmic comment in ChangeCase.

    UBool isError = false;
    (void)isError; // only used for assert
    int32_t srcIdx = 0, dstIdx = 0;
    UChar32 srcCodepoint, dstCodepoint;

    if (bToUpper)
    {
        while (srcIdx < cwSrcLength)
        {
            // In turkish casing, LATIN SMALL LETTER I (U+0069) upper cases to LATIN
            // CAPITAL LETTER I WITH DOT ABOVE (U+0130).
            U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            dstCodepoint = ((srcCodepoint == (UChar32)0x0069) ? (UChar32)0x0130 : u_toupper(srcCodepoint));
            U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            assert(isError == false && srcIdx == dstIdx);
        }
    }
    else
    {
        while (srcIdx < cwSrcLength)
        {
            // In turkish casing, LATIN CAPITAL LETTER I (U+0049) lower cases to
            // LATIN SMALL LETTER DOTLESS I (U+0131).
            U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            dstCodepoint = ((srcCodepoint == (UChar32)0x0049) ? (UChar32)0x0131 : u_tolower(srcCodepoint));
            U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            assert(isError == false && srcIdx == dstIdx);
        }
    }
}

void GlobalizationNative_InitOrdinalCasingPage(int32_t pageNumber, UChar* pTarget)
{
    pageNumber <<= 8;
    for (int i = 0; i < 256; i++)
    {
        // Unfortunately, to ensure one-to-one simple mapping we have to call u_toupper on every character.
        // Using string casing ICU APIs cannot give such results even when using NULL locale to force root behavior.
        pTarget[i] = (UChar) u_toupper((UChar32)(pageNumber + i));
    }

    if (pageNumber == 0x0100)
    {
        // Disable Turkish I behavior on Ordinal operations
        pTarget[0x31] = (UChar)0x0131;  // Turkish lowercase i
        pTarget[0x7F] = (UChar)0x017F;  // // 017F;LATIN SMALL LETTER LONG S
    }
}

#ifdef __clang__
#pragma clang diagnostic pop
#endif
