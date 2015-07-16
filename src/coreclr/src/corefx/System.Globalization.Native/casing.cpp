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
ToUpperSimple
*/
extern "C" void ToUpperSimple(const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    int32_t srcIdx = 0;
    int32_t dstIdx = 0;

    UBool isError = FALSE;

    while (srcIdx < cwSrcLength)
    {
        UChar32 srcCodepoint;
        UChar32 dstCodepoint;

        U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
        dstCodepoint = u_toupper(srcCodepoint);

        U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);

        // Ensure that we wrote the data and the source codepoint when encoded in UTF16 is the same
        // number of code units as the cased codepoint.
        assert(isError == FALSE && srcIdx == dstIdx);
    }
}

/*
Function:
ToLowerSimple
*/
extern "C" void ToLowerSimple(const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    int32_t srcIdx = 0;
    int32_t dstIdx = 0;

    UBool isError = FALSE;

    while (srcIdx < cwSrcLength)
    {
        UChar32 srcCodepoint;
        UChar32 dstCodepoint;

        U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);
        dstCodepoint = u_tolower(srcCodepoint);

        U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);

        // Ensure that we wrote the data and the source codepoint when encoded in UTF16 is the same
        // number of code units as the cased codepoint.
        assert(isError == FALSE && srcIdx == dstIdx);
    }
}

/*
Function:
ToUpperSimpleTurkishAzeri
*/
extern "C" void ToUpperSimpleTurkishAzeri(const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    int32_t srcIdx = 0;
    int32_t dstIdx = 0;

    UBool isError = FALSE;

    while (srcIdx < cwSrcLength)
    {
        UChar32 srcCodepoint;
        UChar32 dstCodepoint;

        U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);

        dstCodepoint = ((srcCodepoint == (UChar32)0x0069) ? (UChar32)0x0130 : u_toupper(srcCodepoint));

        U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);

        // Ensure that we wrote the data and the source codepoint when encoded in UTF16 is the same
        // number of code units as the cased codepoint.
        assert(isError == FALSE && srcIdx == dstIdx);
    }
}

/*
Function:
ToLowerSimpleTurkishAzeri
*/
extern "C" void ToLowerSimpleTurkishAzeri(const UChar* lpSrc, int32_t cwSrcLength, UChar* lpDst, int32_t cwDstLength)
{
    int32_t srcIdx = 0;
    int32_t dstIdx = 0;

    UBool isError = FALSE;

    while (srcIdx < cwSrcLength)
    {
        UChar32 srcCodepoint;
        UChar32 dstCodepoint;

        U16_NEXT(lpSrc, srcIdx, cwSrcLength, srcCodepoint);

        dstCodepoint = ((srcCodepoint == (UChar32)0x0049) ? (UChar32)0x0131 : u_tolower(srcCodepoint));

        U16_APPEND(lpDst, dstIdx, cwDstLength, dstCodepoint, isError);

        // Ensure that we wrote the data and the source codepoint when encoded in UTF16 is the same
        // number of code units as the cased codepoint.
        assert(isError == FALSE && srcIdx == dstIdx);
    }
}

