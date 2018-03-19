// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ============================================================
//
// FusionHelpers.cpp
//
// Implements various helper functions
//
// ============================================================

#include "fusionhelpers.hpp"

#include "shlwapi.h"

#define IS_UPPER_A_TO_Z(x) (((x) >= L'A') && ((x) <= L'Z'))
#define IS_LOWER_A_TO_Z(x) (((x) >= L'a') && ((x) <= L'z'))
#define IS_0_TO_9(x) (((x) >= L'0') && ((x) <= L'9'))
#define CAN_SIMPLE_UPCASE(x) (((x)&~0x7f) == 0)
#define SIMPLE_UPCASE(x) (IS_LOWER_A_TO_Z(x) ? ((x) - L'a' + L'A') : (x))
#define CAN_SIMPLE_LOWERCASE(x) (((x)&~0x7f) == 0)
#define SIMPLE_LOWERCASE(x) (IS_UPPER_A_TO_Z(x) ? ((x) - L'A' + L'a') : (x))

// ---------------------------------------------------------------------------
// Private Helpers
// ---------------------------------------------------------------------------
namespace
{
    WCHAR FusionMapChar(WCHAR wc)
    {
        WCHAR                     wTmp;

#ifndef FEATURE_PAL

#ifdef FEATURE_USE_LCID
        int iRet = WszLCMapString(g_lcid, LCMAP_UPPERCASE, &wc, 1, &wTmp, 1);
#else
        int iRet = LCMapStringEx(g_lcid, LCMAP_UPPERCASE, &wc, 1, &wTmp, 1, NULL, NULL, 0);
#endif
        if (!iRet) {
            _ASSERTE(!"LCMapString failed!");
            iRet = GetLastError();
            wTmp = wc;
        }
#else // !FEATURE_PAL
        // For PAL, no locale specific processing is done
        wTmp = toupper(wc);
#endif // !FEATURE_PAL

        return wTmp;
    }
};

// ---------------------------------------------------------------------------
// FusionCompareStringN
// ---------------------------------------------------------------------------
// if nChar < 0, compare the whole string
int FusionCompareStringN(LPCWSTR pwz1, LPCWSTR pwz2, int nChar, BOOL bCaseSensitive)
{
    int                               iRet = 0;
    int                               nCount = 0;
    WCHAR                             ch1;
    WCHAR                             ch2;
    _ASSERTE(pwz1 && pwz2);

    // same point always return equal.
    if (pwz1 == pwz2) {
        return 0;
    }

    // Case sensitive comparison 
    if (bCaseSensitive) {
        if (nChar >= 0)
            return wcsncmp(pwz1, pwz2, nChar);
        else
            return wcscmp(pwz1, pwz2);
    }

    for (;;) {
        ch1 = *pwz1++;
        ch2 = *pwz2++;

        if (ch1 == L'\0' || ch2 == L'\0') {
            break;
        }
        
        // We use OS mapping table 
        ch1 = (CAN_SIMPLE_UPCASE(ch1)) ? (SIMPLE_UPCASE(ch1)) : (FusionMapChar(ch1));
        ch2 = (CAN_SIMPLE_UPCASE(ch2)) ? (SIMPLE_UPCASE(ch2)) : (FusionMapChar(ch2));
        nCount++;

        if (ch1 != ch2 || (nChar >= 0 && nCount >= nChar)) {
            break;
        }
    }

    if (ch1 > ch2) {
        iRet = 1;
    }
    else if (ch1 < ch2) {
        iRet = -1;
    }

    return iRet; 
}

// ---------------------------------------------------------------------------
// FusionCompareStringI
// ---------------------------------------------------------------------------
int FusionCompareStringI(LPCWSTR pwz1, LPCWSTR pwz2)
{
    return FusionCompareStringN(pwz1, pwz2, -1, FALSE);
}
