// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

/*
// rips the last part of the path off including the backslash
//      C:\foo      -> C:\
//      C:\foo\bar  -> C:\foo
//      C:\foo\     -> C:\foo
//      \\x\y\x     -> \\x\y
//      \\x\y       -> \\x
//      \\x         -> \\ (Just the double slash!)
//      \foo        -> \  (Just the slash!)
//
// in/out:
//      pFile   fully qualified path name
// returns:
//      TRUE    we stripped something
//      FALSE   didn't strip anything (root directory case)
//
*/
STDAPI_(BOOL) PathRemoveFileSpecW(LPWSTR pFile)
{
    RIPMSG(pFile && IS_VALID_STRING_PTR(pFile, -1), "PathRemoveFileSpec: caller passed bad pFile");

    if (pFile)
    {
        LPWSTR pT;
        LPWSTR pT2 = pFile;

        for (pT = pT2; *pT2; pT2++)
        {
            if (IsPathSeparator(*pT2))
            {
                pT = pT2;             // last "\" found, (we will strip here)
            }
            else if (*pT2 == W(':'))     // skip ":\" so we don't
            {
                if (IsPathSeparator(pT2[1]))    // strip the "\" from "C:\"
                {
                    pT2++;
                }
                pT = pT2 + 1;
            }
        }

        if (*pT == 0)
        {
            // didn't strip anything
            return FALSE;
        }
        else if (((pT == pFile) && IsPathSeparator(*pT)) ||                     //  is it the "\foo" case?
                 ((pT == pFile+1) && (*pT == CH_WHACK && *pFile == CH_WHACK)))  //  or the "\\bar" case?
        {
            // Is it just a '\'?
            if (*(pT+1) != W('\0'))
            {
                // Nope.
                *(pT+1) = W('\0');
                return TRUE;        // stripped something
            }
            else
            {
                // Yep.
                return FALSE;
            }
        }
        else
        {
            *pT = 0;
            return TRUE;    // stripped something
        }
    }
    return  FALSE;
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
// its begining.
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
#ifndef PLATFORM_UNIX        
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
#ifndef PLATFORM_UNIX            
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

// Modifies:
//      pszRoot
//
// Returns:
//      TRUE if a drive root was found
//      FALSE otherwise
//
STDAPI_(BOOL) PathStripToRootW(LPWSTR pszRoot)
{
    RIPMSG(pszRoot && IS_VALID_STRING_PTR(pszRoot, -1), "PathStripToRoot: caller passed bad pszRoot");

    if (pszRoot)
    {
        while (!PathIsRootW(pszRoot))
        {
            if (!PathRemoveFileSpecW(pszRoot))
            {
                // If we didn't strip anything off,
                // must be current drive
                return FALSE;
            }
        }
        return TRUE;
    }
    return FALSE;
}



/*----------------------------------------------------------
Purpose: Concatenate lpszDir and lpszFile into a properly formed
         path and canonicalize any relative path pieces.

         lpszDest and lpszFile can be the same buffer
         lpszDest and lpszDir can be the same buffer

Returns: pointer to lpszDest
*/
STDAPI_(LPWSTR) PathCombineW(LPWSTR lpszDest, LPCWSTR lpszDir, LPCWSTR lpszFile)
{
#ifdef DEBUG
    RIPMSG(lpszDest && IS_VALID_WRITE_BUFFER(lpszDest, TCHAR, MAX_LONGPATH), "PathCombine: caller passed bad lpszDest");
    RIPMSG(!lpszDir || IS_VALID_STRING_PTR(lpszDir, -1), "PathCombine: caller passed bad lpszDir");
    RIPMSG(!lpszFile || IS_VALID_STRING_PTR(lpszFile, -1), "PathCombine: caller passed bad lpszFile");
    RIPMSG(lpszDir || lpszFile, "PathCombine: caller neglected to pass lpszDir or lpszFile");
#endif // DEBUG


    if (lpszDest)
    {
        TCHAR szTemp[MAX_LONGPATH];
        LPWSTR pszT;

        *szTemp = W('\0');

        if (lpszDir && *lpszDir)
        {
            if (!lpszFile || *lpszFile==W('\0'))
            {
                // lpszFile is empty
                StringCchCopyNW(szTemp, ARRAYSIZE(szTemp), lpszDir, ARRAYSIZE(szTemp));
            }
            else if (PathIsRelativeW(lpszFile))
            {
                StringCchCopyNW(szTemp, ARRAYSIZE(szTemp), lpszDir, ARRAYSIZE(szTemp));
                pszT = PathAddBackslashW(szTemp);
                if (pszT)
                {
                    size_t iRemaining = ARRAYSIZE(szTemp) - (pszT - szTemp);

                    if (wcslen(lpszFile) < iRemaining)
                    {
                        StringCchCopyNW(pszT, iRemaining, lpszFile, iRemaining);
                    }
                    else
                    {
                        *szTemp = W('\0');
                    }
                }
                else
                {
                    *szTemp = W('\0');
                }
            }
            else if (IsPathSeparator(*lpszFile) && !PathIsUNCW(lpszFile))
            {
                StringCchCopyNW(szTemp, ARRAYSIZE(szTemp), lpszDir, ARRAYSIZE(szTemp));
                // FEATURE: Note that we do not check that an actual root is returned;
                // it is assumed that we are given valid parameters
                PathStripToRootW(szTemp);

                pszT = PathAddBackslashW(szTemp);
                if (pszT)
                {
                    // Skip the backslash when copying
                    // Note: We don't support strings longer than 4GB, but that's
                    // okay because we already fail at MAX_PATH
                    int iRemaining = (int)(ARRAYSIZE(szTemp) - (pszT - szTemp));
                    StringCchCopyNW(pszT, iRemaining, lpszFile+1, iRemaining);
                }
                else
                {
                    *szTemp = W('\0');
                }
            }
            else
            {
                // already fully qualified file part
                StringCchCopyNW(szTemp, ARRAYSIZE(szTemp), lpszFile, ARRAYSIZE(szTemp));
            }
        }
        else if (lpszFile && *lpszFile)
        {
            // no dir just use file.
            StringCchCopyNW(szTemp, ARRAYSIZE(szTemp), lpszFile, ARRAYSIZE(szTemp));
        }

        //
        // if szTemp has something in it we succeeded.  Also if szTemp is empty and
        // the input strings are empty we succeed and PathCanonicalize() will
        // return "\"
        // 
        if (*szTemp || ((lpszDir || lpszFile) && !((lpszDir && *lpszDir) || (lpszFile && *lpszFile))))
        {
            PathCanonicalizeW(lpszDest, szTemp); // this deals with .. and . stuff
                                                // returns "\" on empty szTemp
        }
        else
        {
            *lpszDest = W('\0');   // set output buffer to empty string.
            lpszDest  = NULL;         // return failure.
        }
    }

    return lpszDest;
}

// add a backslash to a qualified path
//
// in:
//  lpszPath    path (A:, C:\foo, etc)
//
// out:
//  lpszPath    A:\, C:\foo\    ;
//
// returns:
//  pointer to the NULL that terminates the path
//
STDAPI_(LPWSTR) PathAddBackslashW(LPWSTR lpszPath)
{
    LPWSTR lpszRet = NULL;

    RIPMSG(lpszPath && IS_VALID_STRING_PTR(lpszPath, -1), "PathAddBackslash: caller passed bad lpszPath");

    if (lpszPath)
    {
        size_t ichPath = wcslen(lpszPath);
        LPWSTR lpszEnd = lpszPath + ichPath;

        if (ichPath)
        {

            // Get the end of the source directory
            switch(*(lpszEnd-1))
            {
                case CH_SLASH:
                case CH_WHACK:
                    break;

                default:
                    // try to keep us from tromping over MAX_PATH in size.
                    // if we find these cases, return NULL.  Note: We need to
                    // check those places that call us to handle their GP fault
                    // if they try to use the NULL!
                    if (ichPath >= (MAX_PATH - 2)) // -2 because ichPath doesn't include NULL, and we're adding a CH_WHACK.
                    {
                        return(NULL);
                    }

                    *lpszEnd++ = CH_WHACK;
                    *lpszEnd = W('\0');
            }
        }

        lpszRet = lpszEnd;
    }

    return lpszRet;
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





//---------------------------------------------------------------------------
// Return TRUE if the path isn't absoulte.
//
// TRUE
//      "foo.exe"
//      ".\foo.exe"
//      "..\boo\foo.exe"
//
// FALSE
//      "\foo"
//      "c:bar"     <- be careful
//      "c:\bar"
//      "\\foo\bar"
//
STDAPI_(BOOL) PathIsRelativeW(LPCWSTR lpszPath)
{
    RIPMSG(lpszPath && IS_VALID_STRING_PTR(lpszPath, -1), "PathIsRelative: caller passed bad lpszPath");

    if (!lpszPath || *lpszPath == 0)
    {
        // The NULL path is assumed relative
        return TRUE;
    }

    if (IsPathSeparator(lpszPath[0]))
    {
        // Does it begin with a slash ?
        return FALSE;
    }
    else if (lpszPath[1] == W(':'))
    {
        // Does it begin with a drive and a colon ?
        return FALSE;
    }
    else
    {
        // Probably relative.
        return TRUE;
    }
}

// find the next slash or null terminator
LPWSTR StrSlash(LPCWSTR psz)
{
    for (; *psz && !IsPathSeparator(*psz); psz++);

    // Cast to a non-const string to mimic the behavior
    // of wcschr/StrChr and strchr.
    return (LPWSTR) psz;
}



