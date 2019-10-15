// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

#include <stdint.h>

#include "pal_icushim.h"
#include "pal_idna.h"

const uint32_t AllowUnassigned = 0x1;
const uint32_t UseStd3AsciiRules = 0x2;

uint32_t GetOptions(uint32_t flags)
{
    // Using Nontransitional to Unicode and Check ContextJ to match the current behavior of .NET on Windows
    uint32_t options = UIDNA_NONTRANSITIONAL_TO_UNICODE | UIDNA_CHECK_CONTEXTJ;

    if ((flags & AllowUnassigned) == AllowUnassigned)
    {
        options |= UIDNA_ALLOW_UNASSIGNED;
    }

    if ((flags & UseStd3AsciiRules) == UseStd3AsciiRules)
    {
        options |= UIDNA_USE_STD3_RULES;
    }

    return options;
}

/*
Function:
ToASCII

Used by System.Globalization.IdnMapping.GetAsciiCore to convert an Unicode
domain name to ASCII

Return values:
0: internal error during conversion.
>0: the length of the converted string (not including the null terminator).
*/
int32_t GlobalizationNative_ToAscii(
    uint32_t flags, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    UErrorCode err = U_ZERO_ERROR;
    UIDNAInfo info = UIDNA_INFO_INITIALIZER;

    UIDNA* pIdna = uidna_openUTS46(GetOptions(flags), &err);

    int32_t asciiStrLen = uidna_nameToASCII(pIdna, lpSrc, cwSrcLength, lpDst, cwDstLength, &info, &err);

    // To have a consistent behavior with Windows, we mask out the error when having 2 hyphens in the third and fourth place.
    info.errors &= ~UIDNA_ERROR_HYPHEN_3_4;

    uidna_close(pIdna);

    return ((U_SUCCESS(err) || (err == U_BUFFER_OVERFLOW_ERROR)) && (info.errors == 0)) ? asciiStrLen : 0;
}

/*
Function:
ToUnicode

Used by System.Globalization.IdnMapping.GetUnicodeCore to convert an ASCII name
to Unicode

Return values:
0: internal error during conversion.
>0: the length of the converted string (not including the null terminator).
*/
int32_t GlobalizationNative_ToUnicode(
    int32_t flags, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    UErrorCode err = U_ZERO_ERROR;
    UIDNAInfo info = UIDNA_INFO_INITIALIZER;

    UIDNA* pIdna = uidna_openUTS46(GetOptions(flags), &err);

    int32_t unicodeStrLen = uidna_nameToUnicode(pIdna, lpSrc, cwSrcLength, lpDst, cwDstLength, &info, &err);

    uidna_close(pIdna);

    return ((U_SUCCESS(err) || (err == U_BUFFER_OVERFLOW_ERROR)) && (info.errors == 0)) ? unicodeStrLen : 0;
}
