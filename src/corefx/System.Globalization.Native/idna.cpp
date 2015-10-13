//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.
//

#include <stdint.h>
#include <unicode/uidna.h>

const uint32_t AllowUnassigned = 0x1;
const uint32_t UseStd3AsciiRules = 0x2;

uint32_t GetOptions(uint32_t flags)
{
    uint32_t options = UIDNA_DEFAULT;

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
extern "C" int32_t ToAscii(uint32_t flags, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    UErrorCode err = U_ZERO_ERROR;
    UIDNAInfo info = UIDNA_INFO_INITIALIZER;

    UIDNA* pIdna = uidna_openUTS46(GetOptions(flags), &err);

    int32_t asciiStrLen = uidna_nameToASCII(pIdna, lpSrc, cwSrcLength, lpDst, cwDstLength, &info, &err);

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
extern "C" int32_t ToUnicode(int32_t flags, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    UErrorCode err = U_ZERO_ERROR;
    UIDNAInfo info = UIDNA_INFO_INITIALIZER;

    UIDNA* pIdna = uidna_openUTS46(GetOptions(flags), &err);

    int32_t unicodeStrLen = uidna_nameToUnicode(pIdna, lpSrc, cwSrcLength, lpDst, cwDstLength, &info, &err);

    uidna_close(pIdna);

    return ((U_SUCCESS(err) || (err == U_BUFFER_OVERFLOW_ERROR)) && (info.errors == 0)) ? unicodeStrLen : 0;
}
