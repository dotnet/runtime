// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <stdint.h>

#include "pal_icushim_internal.h"
#include "pal_idna.h"

#if defined(TARGET_WINDOWS)
// Windows icu headers doesn't define this member as it is marked as deprecated as of ICU 55.
enum {
    UIDNA_ALLOW_UNASSIGNED=1
};
#endif

static const uint32_t AllowUnassigned = 0x1;
static const uint32_t UseStd3AsciiRules = 0x2;

static uint32_t GetOptions(uint32_t flags, uint32_t useToAsciiFlags)
{
    uint32_t options = UIDNA_CHECK_CONTEXTJ;

    if ((flags & AllowUnassigned) == AllowUnassigned)
    {
        options |= UIDNA_ALLOW_UNASSIGNED;
    }

    if ((flags & UseStd3AsciiRules) == UseStd3AsciiRules)
    {
        options |= UIDNA_USE_STD3_RULES;
    }

    if (useToAsciiFlags)
    {
        options |=  UIDNA_NONTRANSITIONAL_TO_ASCII;
    }
    else
    {
        options |=  UIDNA_NONTRANSITIONAL_TO_UNICODE;
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

    UIDNA* pIdna = uidna_openUTS46(GetOptions(flags, /* useToAsciiFlags */ 1), &err);

    int32_t asciiStrLen = uidna_nameToASCII(pIdna, lpSrc, cwSrcLength, lpDst, cwDstLength, &info, &err);

    // To have a consistent behavior with Windows, we mask out the error when having 2 hyphens in the third and fourth place.
    info.errors &= (uint32_t)~UIDNA_ERROR_HYPHEN_3_4;

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
    uint32_t flags, const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    UErrorCode err = U_ZERO_ERROR;
    UIDNAInfo info = UIDNA_INFO_INITIALIZER;

    UIDNA* pIdna = uidna_openUTS46(GetOptions(flags, /* useToAsciiFlags */ 0), &err);

    int32_t unicodeStrLen = uidna_nameToUnicode(pIdna, lpSrc, cwSrcLength, lpDst, cwDstLength, &info, &err);

    uidna_close(pIdna);

    return ((U_SUCCESS(err) || (err == U_BUFFER_OVERFLOW_ERROR)) && (info.errors == 0)) ? unicodeStrLen : 0;
}
