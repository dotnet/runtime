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

Preforms upper or lower casing of a string into a new buffer, preforming special casing for turkish, if needed.
*/
extern "C" void ChangeCase(const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength, int32_t bToUpper, int32_t bTurkishCasing)
{
    int32_t srcIdx = 0;
    int32_t dstIdx = 0;

    UBool isError = FALSE;

    while (srcIdx < cwSrcLength)
    {
        UChar32 srcCodepoint;
        UChar32 dstCodepoint;

        // Decode the next one or two UTF-16 code units into a codepoint and update srcIdx to point to the next UTF-16 code unit to decode.
        U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);

        if (bToUpper)
        {
            if (!bTurkishCasing)
            {
                dstCodepoint = u_toupper(srcCodepoint);
            }
            else
            {
                // In turkish casing, LATIN SMALL LETTER I (U+0069) upper cases to LATIN CAPITAL LETTER I WITH DOT ABOVE (U+0130).
                dstCodepoint = ((srcCodepoint == (UChar32)0x0069) ? (UChar32)0x0130 : u_toupper(srcCodepoint));
            }
        }
        else
        {
            if (!bTurkishCasing)
            {
                dstCodepoint = u_tolower(srcCodepoint);
            }
            else
            {
                // In turkish casing, LATIN CAPITAL LETTER I (U+0049) lower cases to LATIN SMALL LETTER DOTLESS I (U+0131).
                dstCodepoint = ((srcCodepoint == (UChar32)0x0049) ? (UChar32)0x0131 : u_tolower(srcCodepoint));
            }
        }

        // Write dstCodepoint into lpDst at offset dstIdx and update dstIdx.
        U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);

        // Ensure that we wrote the data and the source codepoint when encoded in UTF16 is the same
        // number of code units as the cased codepoint.
        assert(isError == FALSE && srcIdx == dstIdx);
    }
}

