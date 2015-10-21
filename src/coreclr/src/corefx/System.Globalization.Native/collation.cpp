//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.
//

#include <assert.h>
#include <stdint.h>
#include <unicode/uchar.h>
#include <unicode/ucol.h>
#include <unicode/usearch.h>
#include <unicode/utf16.h>

const int32_t CompareOptionsIgnoreCase = 1;
// const int32_t CompareOptionsIgnoreNonSpace = 2;
// const int32_t CompareOptionsIgnoreSymbols = 4;
// const int32_t CompareOptionsIgnoreKanaType = 8;
// const int32_t CompareOptionsIgnoreWidth = 0x10;
// const int32_t CompareOptionsStringSort = 0x20000000;

/*
 * To collator returned by this function is owned by the callee and must be
 *closed when this method returns
 * with a U_SUCCESS UErrorCode.
 *
 * On error, the return value is undefined.
 */
UCollator* GetCollatorForLocaleAndOptions(const char* lpLocaleName, int32_t options, UErrorCode* pErr)
{
    UCollator* pColl = nullptr;

    pColl = ucol_open(lpLocaleName, pErr);

    if ((options & CompareOptionsIgnoreCase) == CompareOptionsIgnoreCase)
    {
        ucol_setAttribute(pColl, UCOL_STRENGTH, UCOL_SECONDARY, pErr);
    }

    return pColl;
}

/*
Function:
CompareString
*/
extern "C" int32_t CompareString(const char* lpLocaleName,
                                 const UChar* lpStr1,
                                 int32_t cwStr1Length,
                                 const UChar* lpStr2,
                                 int32_t cwStr2Length,
                                 int32_t options)
{
    static_assert(UCOL_EQUAL == 0, "managed side requires 0 for equal strings");
    static_assert(UCOL_LESS < 0, "managed side requires less than zero for a < b");
    static_assert(UCOL_GREATER > 0, "managed side requires greater than zero for a > b");

    UCollationResult result = UCOL_EQUAL;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        result = ucol_strcoll(pColl, lpStr1, cwStr1Length, lpStr2, cwStr2Length);
        ucol_close(pColl);
    }

    return result;
}

/*
Function:
IndexOf
*/
extern "C" int32_t
IndexOf(const char* lpLocaleName, const UChar* lpTarget, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    static_assert(USEARCH_DONE == -1, "managed side requires -1 for not found");

    int32_t result = USEARCH_DONE;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, -1, lpSource, cwSourceLength, pColl, nullptr, &err);

        if (U_SUCCESS(err))
        {
            result = usearch_first(pSearch, &err);
            usearch_close(pSearch);
        }

        ucol_close(pColl);
    }

    return result;
}

/*
Function:
LastIndexOf
*/
extern "C" int32_t LastIndexOf(
    const char* lpLocaleName, const UChar* lpTarget, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    static_assert(USEARCH_DONE == -1, "managed side requires -1 for not found");

    int32_t result = USEARCH_DONE;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, -1, lpSource, cwSourceLength, pColl, nullptr, &err);

        if (U_SUCCESS(err))
        {
            result = usearch_last(pSearch, &err);
            usearch_close(pSearch);
        }

        ucol_close(pColl);
    }

    return result;
}

/*
Static Function:
AreEqualOrdinalIgnoreCase
*/
static bool AreEqualOrdinalIgnoreCase(UChar32 one, UChar32 two)
{
    // Return whether the two characters are identical or would be identical if they were upper-cased.

    if (one == two)
    {
        return true;
    }

    if (one == 0x0131 || two == 0x0131)
    {
        // On Windows with InvariantCulture, the LATIN SMALL LETTER DOTLESS I (U+0131)
        // capitalizes to itself, whereas with ICU it capitalizes to LATIN CAPITAL LETTER I (U+0049).
        // We special case it to match the Windows invariant behavior.
        return false;
    }

    return u_toupper(one) == u_toupper(two);
}

/*
Function:
IndexOfOrdinalIgnoreCase
*/
extern "C" int32_t
IndexOfOrdinalIgnoreCase(
    const UChar* lpTarget, int32_t cwTargetLength, 
    const UChar* lpSource, int32_t cwSourceLength, 
    int32_t findLast)
{
    int32_t result = -1;

    int32_t endIndex = cwSourceLength - cwTargetLength;
    assert(endIndex >= 0);

    int32_t i = 0;
    while (i <= endIndex)
    {
        int32_t srcIdx = i, trgIdx = 0;
        const UChar *src = lpSource, *trg = lpTarget;
        UChar32 srcCodepoint, trgCodepoint;

        bool match = true;
        while (trgIdx < cwTargetLength)
        {
            U16_NEXT(src, srcIdx, cwSourceLength, srcCodepoint);
            U16_NEXT(trg, trgIdx, cwTargetLength, trgCodepoint);
            if (!AreEqualOrdinalIgnoreCase(srcCodepoint, trgCodepoint))
            {
                match = false; 
                break;
            }
        }

        if (match) 
        {
            result = i;
            if (!findLast)
            {
                break;
            }
        }

        U16_FWD_1(lpSource, i, cwSourceLength);
    }

    return result;
}

/*
 Return value is a "Win32 BOOL" (1 = true, 0 = false)
 */
extern "C" int32_t StartsWith(
    const char* lpLocaleName, const UChar* lpTarget, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    int32_t result = FALSE;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, -1, lpSource, cwSourceLength, pColl, nullptr, &err);
        int32_t idx = USEARCH_DONE;

        if (U_SUCCESS(err))
        {
            idx = usearch_first(pSearch, &err);

            if (idx == 0)
            {
                result = TRUE;
            }
            else
            {
                UCollationElements* pCollElem = ucol_openElements(pColl, lpSource, idx, &err);

                if (U_SUCCESS(err))
                {
                    int32_t curCollElem = UCOL_NULLORDER;

                    result = TRUE;

                    while ((curCollElem = ucol_next(pCollElem, &err)) != UCOL_NULLORDER)
                    {
                        if (curCollElem != 0)
                        {
                            // Non ignorable collation element found between start of the
                            // string and the first match for lpTarget.
                            result = FALSE;
                            break;
                        }
                    }

                    if (U_FAILURE(err))
                    {
                        result = FALSE;
                    }

                    ucol_closeElements(pCollElem);
                }
            }

            usearch_close(pSearch);
        }

        ucol_close(pColl);
    }

    return result;
}

/*
 Return value is a "Win32 BOOL" (1 = true, 0 = false)
 */
extern "C" int32_t EndsWith(
    const char* lpLocaleName, const UChar* lpTarget, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    int32_t result = FALSE;
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, -1, lpSource, cwSourceLength, pColl, nullptr, &err);
        int32_t idx = USEARCH_DONE;

        if (U_SUCCESS(err))
        {
            idx = usearch_last(pSearch, &err);

            if (idx != USEARCH_DONE)
            {
                if ((idx + usearch_getMatchedLength(pSearch)) == cwSourceLength)
                {
                    result = TRUE;
                }

                // TODO (dotnet/corefx#3467): We should do something similar to what
                // StartsWith does where we can ignore
                // some collation elements at the end of te string if they are zero.
            }

            usearch_close(pSearch);
        }

        ucol_close(pColl);
    }

    return result;
}

extern "C" int32_t GetSortKey(const char* lpLocaleName,
                              const UChar* lpStr,
                              int32_t cwStrLength,
                              uint8_t* sortKey,
                              int32_t cbSortKeyLength,
                              int32_t options)
{
    UErrorCode err = U_ZERO_ERROR;
    UCollator* pColl = GetCollatorForLocaleAndOptions(lpLocaleName, options, &err);
    int32_t result = 0;

    if (U_SUCCESS(err))
    {
        result = ucol_getSortKey(pColl, lpStr, cwStrLength, sortKey, cbSortKeyLength);

        ucol_close(pColl);
    }

    return result;
}

extern "C" int32_t
CompareStringOrdinalIgnoreCase(const UChar* lpStr1, int32_t cwStr1Length, const UChar* lpStr2, int32_t cwStr2Length)
{
    assert(lpStr1 != nullptr);
    assert(cwStr1Length >= 0);
    assert(lpStr2 != nullptr);
    assert(cwStr2Length >= 0);

    int32_t str1Idx = 0;
    int32_t str2Idx = 0;

    while (str1Idx < cwStr1Length && str2Idx < cwStr2Length)
    {
        UChar32 str1Codepoint;
        UChar32 str2Codepoint;

        U16_NEXT(lpStr1, str1Idx, cwStr1Length, str1Codepoint);
        U16_NEXT(lpStr2, str2Idx, cwStr2Length, str2Codepoint);

        if (str1Codepoint != str2Codepoint && u_toupper(str1Codepoint) != u_toupper(str2Codepoint))
        {
            return str1Codepoint < str2Codepoint ? -1 : 1;
        }
    }

    if (cwStr1Length < cwStr2Length)
    {
        return -1;
    }

    if (cwStr2Length < cwStr1Length)
    {
        return 1;
    }

    return 0;
}
