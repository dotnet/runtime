//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full
// license information.
//

#include <assert.h>
#include <stdint.h>
#include <map>
#include <unicode/uchar.h>
#include <unicode/ucol.h>
#include <unicode/usearch.h>
#include <unicode/utf16.h>

const int32_t CompareOptionsIgnoreCase = 1;
const int32_t CompareOptionsIgnoreNonSpace = 2;
const int32_t CompareOptionsIgnoreSymbols = 4;
// const int32_t CompareOptionsIgnoreKanaType = 8;
// const int32_t CompareOptionsIgnoreWidth = 0x10;
// const int32_t CompareOptionsStringSort = 0x20000000;

typedef std::map<int32_t, UCollator*> TCollatorMap;
typedef std::pair<int32_t, UCollator*> TCollatorMapPair;

/*
 * For increased performance, we cache the UCollator objects for a locale and
 * share them across threads. This is safe (and supported in ICU) if we ensure
 * multiple threads are only ever dealing with const UCollators.
 */
typedef struct _sort_handle
{
    UCollator* regular;
    TCollatorMap collatorsPerOption;

    _sort_handle() : regular(nullptr)
    {
    }

} SortHandle;

/*
 * The collator returned by this function is owned by the callee and must be
 * closed when this method returns with a U_SUCCESS UErrorCode.
 *
 * On error, the return value is undefined.
 */
UCollator* CloneCollatorWithOptions(const UCollator* pCollator, int32_t options, UErrorCode* pErr)
{
    UColAttributeValue strength = UCOL_DEFAULT;

    bool isIgnoreCase = (options & CompareOptionsIgnoreCase) == CompareOptionsIgnoreCase;
    bool isIgnoreNonSpace = (options & CompareOptionsIgnoreNonSpace) == CompareOptionsIgnoreNonSpace;
    bool isIgnoreSymbols = (options & CompareOptionsIgnoreSymbols) == CompareOptionsIgnoreSymbols;

    if (isIgnoreCase)
    {
        strength = UCOL_SECONDARY;
    }

    if (isIgnoreNonSpace)
    {
        strength = UCOL_PRIMARY;
    }

    UCollator* pClonedCollator = ucol_safeClone(pCollator, nullptr, nullptr, pErr);

    if (isIgnoreSymbols)
    {
        ucol_setAttribute(pClonedCollator, UCOL_ALTERNATE_HANDLING, UCOL_SHIFTED, pErr);
    }

    if (strength != UCOL_DEFAULT)
    {
        ucol_setAttribute(pClonedCollator, UCOL_STRENGTH, strength, pErr);

        // casing differs at the tertiary level.
        // if strength is less than tertiary, but we are not ignoring case, then we need to flip CASE_LEVEL On
        if (strength < UCOL_TERTIARY && !isIgnoreCase)
        {
            ucol_setAttribute(pClonedCollator, UCOL_CASE_LEVEL, UCOL_ON, pErr);
        }
    }

    return pClonedCollator;
}

extern "C" SortHandle* GetSortHandle(const char* lpLocaleName)
{
    SortHandle* pSortHandle = new SortHandle();

    UErrorCode err = U_ZERO_ERROR;

    pSortHandle->regular = ucol_open(lpLocaleName, &err);

    if (U_FAILURE(err))
    {
        if (pSortHandle->regular != nullptr)
              ucol_close(pSortHandle->regular);

        delete pSortHandle;
        pSortHandle = nullptr;
    }

    return pSortHandle;
}

extern "C" void CloseSortHandle(SortHandle* pSortHandle)
{
    ucol_close(pSortHandle->regular);
    pSortHandle->regular = nullptr;

    TCollatorMap::iterator it;
    for (it = pSortHandle->collatorsPerOption.begin(); it != pSortHandle->collatorsPerOption.end(); it++)
    {
        ucol_close(it->second);
    }

    delete pSortHandle;
}

const UCollator* GetCollatorFromSortHandle(SortHandle* pSortHandle, int32_t options, UErrorCode* pErr)
{
    UCollator* pCollator;
    if (options == 0)
    {
        pCollator = pSortHandle->regular;
    }
    else
    {
        TCollatorMap::iterator entry = pSortHandle->collatorsPerOption.find(options);
        if (entry == pSortHandle->collatorsPerOption.end())
        {
            pCollator = CloneCollatorWithOptions(pSortHandle->regular, options, pErr);
            pSortHandle->collatorsPerOption[options] = pCollator;
        }
        else
        {
            pCollator = entry->second;
        }
    }

    return pCollator;
}

/*
Function:
CompareString
*/
extern "C" int32_t CompareString(SortHandle* pSortHandle,
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
    const UCollator* pColl = GetCollatorFromSortHandle(pSortHandle, options, &err);

    if (U_SUCCESS(err))
    {
        result = ucol_strcoll(pColl, lpStr1, cwStr1Length, lpStr2, cwStr2Length);
    }

    return result;
}

/*
Function:
IndexOf
*/
extern "C" int32_t
IndexOf(SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    static_assert(USEARCH_DONE == -1, "managed side requires -1 for not found");

    int32_t result = USEARCH_DONE;
    UErrorCode err = U_ZERO_ERROR;
    const UCollator* pColl = GetCollatorFromSortHandle(pSortHandle, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, cwTargetLength, lpSource, cwSourceLength, pColl, nullptr, &err);

        if (U_SUCCESS(err))
        {
            result = usearch_first(pSearch, &err);
            usearch_close(pSearch);
        }
    }

    return result;
}

/*
Function:
LastIndexOf
*/
extern "C" int32_t LastIndexOf(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    static_assert(USEARCH_DONE == -1, "managed side requires -1 for not found");

    int32_t result = USEARCH_DONE;
    UErrorCode err = U_ZERO_ERROR;
    const UCollator* pColl = GetCollatorFromSortHandle(pSortHandle, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, cwTargetLength, lpSource, cwSourceLength, pColl, nullptr, &err);

        if (U_SUCCESS(err))
        {
            result = usearch_last(pSearch, &err);
            usearch_close(pSearch);
        }
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
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    int32_t result = FALSE;
    UErrorCode err = U_ZERO_ERROR;
    const UCollator* pColl = GetCollatorFromSortHandle(pSortHandle, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, cwTargetLength, lpSource, cwSourceLength, pColl, nullptr, &err);
        int32_t idx = USEARCH_DONE;

        if (U_SUCCESS(err))
        {
            idx = usearch_first(pSearch, &err);
            if (idx != USEARCH_DONE)
            {
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
            }

            usearch_close(pSearch);
        }
    }

    return result;
}

/*
 Return value is a "Win32 BOOL" (1 = true, 0 = false)
 */
extern "C" int32_t EndsWith(
    SortHandle* pSortHandle, const UChar* lpTarget, int32_t cwTargetLength, const UChar* lpSource, int32_t cwSourceLength, int32_t options)
{
    int32_t result = FALSE;
    UErrorCode err = U_ZERO_ERROR;
    const UCollator* pColl = GetCollatorFromSortHandle(pSortHandle, options, &err);

    if (U_SUCCESS(err))
    {
        UStringSearch* pSearch = usearch_openFromCollator(lpTarget, cwTargetLength, lpSource, cwSourceLength, pColl, nullptr, &err);
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
                // some collation elements at the end of the string if they are zero.
            }

            usearch_close(pSearch);
        }
    }

    return result;
}

extern "C" int32_t GetSortKey(SortHandle* pSortHandle,
                              const UChar* lpStr,
                              int32_t cwStrLength,
                              uint8_t* sortKey,
                              int32_t cbSortKeyLength,
                              int32_t options)
{
    UErrorCode err = U_ZERO_ERROR;
    const UCollator* pColl = GetCollatorFromSortHandle(pSortHandle, options, &err);
    int32_t result = 0;

    if (U_SUCCESS(err))
    {
        result = ucol_getSortKey(pColl, lpStr, cwStrLength, sortKey, cbSortKeyLength);
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
