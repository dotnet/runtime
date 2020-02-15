// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <assert.h>
#include <stdint.h>

#include "pal_casing.h"
#include "pal_icushim.h"

// Workaround for warnings produced by U16_NEXT and U16_APPEND macro expansions
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wsign-conversion"


// Performs simple case folding of a code point, but forbids a non-ASCII code point
// from folding to an ASCII code point. If this occurs the API will return the original
// code point value.
static UChar32 CaseFoldCodePoint(UChar32 codePoint)
{
    UChar32 codePointFolded = u_foldCase(codePoint, U_FOLD_CASE_DEFAULT);

    // Subtracting 0x80 from the code point value will cause ASCII code points to become negative
    // and non-ASCII code points to become non-negative. Since these code paths are expected to
    // be called when we have a mix of ASCII and non-ASCII chars, this allows the branch condition
    // to almost always evaluate to false.

    if ((codePoint - 0x80) ^ (codePointFolded - 0x80) < 0)
    {
        codePointFolded = codePoint;
    }

    return codePointFolded;
}

/*
Function:
ChangeCase

Performs simple case mapping (upper or lower) of a string into a new buffer.
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

    UBool isError = FALSE;
    int32_t srcIdx = 0, dstIdx = 0;
    UChar32 srcCodepoint, dstCodepoint;

    if (bToUpper)
    {
        while (srcIdx < cwSrcLength)
        {
            U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            dstCodepoint = u_toupper(srcCodepoint);
            U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            assert(isError == FALSE && srcIdx == dstIdx);
        }
    }
    else
    {
        while (srcIdx < cwSrcLength)
        {
            U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
            dstCodepoint = u_tolower(srcCodepoint);
            U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
            assert(isError == FALSE && srcIdx == dstIdx);
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

    UBool isError = FALSE;
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
            assert(isError == FALSE && srcIdx == dstIdx);
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
            assert(isError == FALSE && srcIdx == dstIdx);
        }
    }
}

/*
Function:
SimpleCaseFold

Performs simple case folding of a string into a new buffer.
Non-ASCII code points are not mapped to ASCII code points.
*/
void GlobalizationNative_SimpleCaseFold(
    const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    UBool isError = FALSE;
    int32_t srcIdx = 0, dstIdx = 0;
    UChar32 srcCodepoint, dstCodepoint;

    while (srcIdx < cwSrcLength)
    {
        U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
        dstCodepoint = CaseFoldCodePoint(srcCodepoint);
        U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
        assert(isError == FALSE && srcIdx == dstIdx);
    }
}

#pragma clang diagnostic pop
