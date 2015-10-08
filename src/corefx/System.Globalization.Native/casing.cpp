//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include <assert.h>
#include <stdint.h>
#include <unicode/uchar.h>
#include <unicode/utf16.h>

/*
Function:
ChangeCase

Performs upper or lower casing of a string into a new buffer, performing special casing for Turkish, if needed.
*/
extern "C" void ChangeCase(const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper, int32_t bTurkishCasing)
{
    int32_t srcIdx = 0;
    int32_t dstIdx = 0;

    UBool isError = FALSE;
    UChar32 srcCodepoint;
    UChar32 dstCodepoint;

    // Iterate through the string, decoding the next one or two UTF-16 code units into a codepoint 
    // and updating srcIdx to point to the next UTF-16 code unit to decode.  Then upper or lower
    // case it, write dstCodepoint into lpDst at offset dstIdx, and update dstIdx.
    // (The loop here has been manually cloned for each of the four cases, rather than having a 
    // single loop that internally branched based on bToUpper and bTurkishCasing, as the compiler 
    // wasn't doing that optimization, and it results in an ~15-20% perf improvement on longer strings.)

    if (bToUpper)
    {
        if (bTurkishCasing)
        {
            // bToUpper && bTurkishCasing
            while (srcIdx < cwSrcLength)
            {
                // In turkish casing, LATIN SMALL LETTER I (U+0069) upper cases to LATIN CAPITAL LETTER I WITH DOT ABOVE (U+0130).
                U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
                dstCodepoint = ((srcCodepoint == (UChar32)0x0069) ? (UChar32)0x0130 : u_toupper(srcCodepoint));
                U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                assert(isError == FALSE && srcIdx == dstIdx);
            }
        }
        else
        {
            // bToUpper && !bTurkishCasing
            while (srcIdx < cwSrcLength)
            {
                U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
                dstCodepoint = u_toupper(srcCodepoint);
                U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                assert(isError == FALSE && srcIdx == dstIdx);
            }
        }
    }
    else
    {
        if (bTurkishCasing)
        {
            // !bToUpper && bTurkishCasing
            while (srcIdx < cwSrcLength)
            {
                // In turkish casing, LATIN CAPITAL LETTER I (U+0049) lower cases to LATIN SMALL LETTER DOTLESS I (U+0131).
                U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
                dstCodepoint = ((srcCodepoint == (UChar32)0x0049) ? (UChar32)0x0131 : u_tolower(srcCodepoint));
                U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                assert(isError == FALSE && srcIdx == dstIdx);
            }
        }
        else
        {
            // !bToUpper && !bTurkishCasing
            while (srcIdx < cwSrcLength)
            {
                U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
                dstCodepoint = u_tolower(srcCodepoint);
                U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);
                assert(isError == FALSE && srcIdx == dstIdx);
            }
        }
    }
}
