// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//
// ===========================================================================
// File: path.cpp
//
// Path APIs ported from shlwapi (especially for Fusion)
// ===========================================================================

#include "common.h"
#include "strsafe.h"


#define CH_SLASH W('/')
#define CH_WHACK W('\\')

//
// Inline function to check for a double-backslash at the
// beginning of a string
//

static __inline BOOL DBL_BSLASH(LPCWSTR psz)
{
    return (psz[0] == W('\\') && psz[1] == W('\\'));
}

//
// Inline function to check for a path separator character.
//

static __inline BOOL IsPathSeparator(WCHAR ch)
{
    return (ch == CH_SLASH || ch == CH_WHACK);
}

__inline BOOL ChrCmpW_inline(WCHAR w1, WCHAR wMatch)
{
    return(!(w1 == wMatch));
}

STDAPI_(LPWSTR) StrRChrW(LPCWSTR lpStart, LPCWSTR lpEnd, WCHAR wMatch)
{
    LPCWSTR lpFound = NULL;

    RIPMSG(lpStart && IS_VALID_STRING_PTRW(lpStart, -1), "StrRChrW: caller passed bad lpStart");
    RIPMSG(!lpEnd || lpEnd <= lpStart + wcslen(lpStart), "StrRChrW: caller passed bad lpEnd");
    // don't need to check for NULL lpStart

    if (!lpEnd)
        lpEnd = lpStart + wcslen(lpStart);

    for ( ; lpStart < lpEnd; lpStart++)
    {
        if (!ChrCmpW_inline(*lpStart, wMatch))
            lpFound = lpStart;
    }
    return ((LPWSTR)lpFound);
}


// check if a path is a root
//
// returns:
//  TRUE
//      "\" "X:\" "\\" "\\foo" "\\foo\bar"
//
//  FALSE for others including "\\foo\bar\" (!)
//
STDAPI_(BOOL) PathIsRootW(LPCWSTR pPath)
{
    RIPMSG(pPath && IS_VALID_STRING_PTR(pPath, -1), "PathIsRoot: caller passed bad pPath");

    if (!pPath || !*pPath)
    {
        return FALSE;
    }

    if (!lstrcmpiW(pPath + 1, W(":\\")))
    {
        return TRUE;    // "X:\" case
    }

    if (IsPathSeparator(*pPath) && (*(pPath + 1) == 0))
    {
        return TRUE;    // "/" or "\" case
    }

    if (DBL_BSLASH(pPath))      // smells like UNC name
    {
        LPCWSTR p;
        int cBackslashes = 0;

        for (p = pPath + 2; *p; p++)
        {
            if (*p == W('\\'))
            {
                //
                //  return FALSE for "\\server\share\dir"
                //  so just check if there is more than one slash
                //
                //  "\\server\" without a share name causes
                //  problems for WNet APIs.  we should return
                //  FALSE for this as well
                //
                if ((++cBackslashes > 1) || !*(p+1))
                    return FALSE;
            }
        }
        // end of string with only 1 more backslash
        // must be a bare UNC, which looks like a root dir
        return TRUE;
    }
    return FALSE;
}

//
// Return a pointer to the end of the next path component in the string.
// ie return a pointer to the next backslash or terminating NULL.
//
LPCWSTR GetPCEnd(LPCWSTR lpszStart)
{
    LPCWSTR lpszEnd;
    LPCWSTR lpszSlash;

    lpszEnd = StrChr(lpszStart, CH_WHACK);
    lpszSlash = StrChr(lpszStart, CH_SLASH);
    if ((lpszSlash && lpszSlash < lpszEnd) ||
        !lpszEnd)
    {
        lpszEnd = lpszSlash;
    }
    if (!lpszEnd)
    {
        lpszEnd = lpszStart + wcslen(lpszStart);
    }

    return lpszEnd;
}

//
// Given a pointer to the end of a path component, return a pointer to
// its beginning.
// ie return a pointer to the previous backslash (or start of the string).
//
LPCWSTR PCStart(LPCWSTR lpszStart, LPCWSTR lpszEnd)
{
    LPCWSTR lpszBegin = StrRChrW(lpszStart, lpszEnd, CH_WHACK);
    LPCWSTR lpszSlash = StrRChrW(lpszStart, lpszEnd, CH_SLASH);
    if (lpszSlash > lpszBegin)
    {
        lpszBegin = lpszSlash;
    }
    if (!lpszBegin)
    {
        lpszBegin = lpszStart;
    }
    return lpszBegin;
}

//
// Fix up a few special cases so that things roughly make sense.
//
void NearRootFixups(LPWSTR lpszPath, BOOL fUNC)
{
    // Check for empty path.
    if (lpszPath[0] == W('\0'))
    {
        // Fix up.
#ifndef TARGET_UNIX
        lpszPath[0] = CH_WHACK;
#else
        lpszPath[0] = CH_SLASH;
#endif
        lpszPath[1] = W('\0');
    }
    // Check for missing slash.
    if (lpszPath[1] == W(':') && lpszPath[2] == W('\0'))
    {
        // Fix up.
        lpszPath[2] = W('\\');
        lpszPath[3] = W('\0');
    }
    // Check for UNC root.
    if (fUNC && lpszPath[0] == W('\\') && lpszPath[1] == W('\0'))
    {
        // Fix up.
        //lpszPath[0] = W('\\'); // already checked in if guard
        lpszPath[1] = W('\\');
        lpszPath[2] = W('\0');
    }
}

/*----------------------------------------------------------
Purpose: Canonicalize a path.

Returns:
Cond:    --
*/
STDAPI_(BOOL) PathCanonicalizeW(LPWSTR lpszDst, LPCWSTR lpszSrc)
{
    LPCWSTR lpchSrc;
    LPCWSTR lpchPCEnd;      // Pointer to end of path component.
    LPWSTR lpchDst;
    BOOL fUNC;
    int cchPC;

    RIPMSG(lpszDst && IS_VALID_WRITE_BUFFER(lpszDst, WCHAR, MAX_PATH), "PathCanonicalize: caller passed bad lpszDst");
    RIPMSG(lpszSrc && IS_VALID_STRING_PTR(lpszSrc, -1), "PathCanonicalize: caller passed bad lpszSrc");
    RIPMSG(lpszDst != lpszSrc, "PathCanonicalize: caller passed the same buffer for lpszDst and lpszSrc");

    if (!lpszDst || !lpszSrc)
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        return FALSE;
    }

    *lpszDst = W('\0');

    fUNC = PathIsUNCW(lpszSrc);    // Check for UNCness.

    // Init.
    lpchSrc = lpszSrc;
    lpchDst = lpszDst;

    while (*lpchSrc)
    {
        lpchPCEnd = GetPCEnd(lpchSrc);
        cchPC = (int) (lpchPCEnd - lpchSrc)+1;

        if (cchPC == 1 && IsPathSeparator(*lpchSrc))   // Check for slashes.
        {
            // Just copy them.
#ifndef TARGET_UNIX
            *lpchDst = CH_WHACK;
#else
            *lpchDst = CH_SLASH;
#endif
            lpchDst++;
            lpchSrc++;
        }
        else if (cchPC == 2 && *lpchSrc == W('.'))  // Check for dots.
        {
            // Skip it...
            // Are we at the end?
            if (*(lpchSrc+1) == W('\0'))
            {
                lpchSrc++;

                // remove the last slash we copied (if we've copied one), but don't make a mal-formed root
                if ((lpchDst > lpszDst) && !PathIsRootW(lpszDst))
                    lpchDst--;
            }
            else
            {
                lpchSrc += 2;
            }
        }
        else if (cchPC == 3 && *lpchSrc == W('.') && *(lpchSrc + 1) == W('.')) // Check for dot dot.
        {
            // make sure we aren't already at the root
            if (!PathIsRootW(lpszDst))
            {
                // Go up... Remove the previous path component.
                lpchDst = (LPWSTR)PCStart(lpszDst, lpchDst - 1);
            }
            else
            {
                // When we can't back up, skip the trailing backslash
                // so we don't copy one again. (C:\..\FOO would otherwise
                // turn into C:\\FOO).
                if (IsPathSeparator(*(lpchSrc + 2)))
                {
                    lpchSrc++;
                }
            }

            // skip ".."
            lpchSrc += 2;
        }
        else                                                                        // Everything else
        {
            // Just copy it.
            int cchRemainingBuffer = MAX_PATH - (lpszDst - lpchDst);
            StringCchCopyNW(lpchDst, cchRemainingBuffer, lpchSrc, cchPC);
            lpchDst += cchPC - 1;
            lpchSrc += cchPC - 1;
        }

        // Keep everything nice and tidy.
        *lpchDst = W('\0');
    }

    // Check for weirdo root directory stuff.
    NearRootFixups(lpszDst, fUNC);

    return TRUE;
}

//---------------------------------------------------------------------------
// Returns TRUE if the given string is a UNC path.
//
// TRUE
//      "\\foo\bar"
//      "\\foo"         <- careful
//      "\\"
// FALSE
//      "\foo"
//      "foo"
//      "c:\foo"
//
//
STDAPI_(BOOL) PathIsUNCW(LPCWSTR pszPath)
{
    RIPMSG(pszPath && IS_VALID_STRING_PTR(pszPath, -1), "PathIsUNC: caller passed bad pszPath");

    if (pszPath)
    {
        return DBL_BSLASH(pszPath);
    }
    return FALSE;
}
