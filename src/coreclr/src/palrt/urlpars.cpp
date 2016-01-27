// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//
// ===========================================================================
// File: urlpars.cpp
//
// URL APIs ported from shlwapi (especially for Fusion)
// ===========================================================================

#include "common.h"

#define SLASH       W('/')
#define WHACK       W('\\')

#define UPF_SCHEME_OPAQUE           0x00000001  //  should not be treated as hierarchical
#define UPF_SCHEME_INTERNET         0x00000002
#define UPF_SCHEME_NOHISTORY        0x00000004
#define UPF_SCHEME_CONVERT          0x00000008  //  treat slashes and whacks as equiv
#define UPF_SCHEME_DONTCORRECT      0x00000010  //  Don't try to autocorrect to this scheme

PRIVATE CONST WORD isSafe[96] =

/*   Bit 0       alphadigit     -- 'a' to 'z', '0' to '9', 'A' to 'Z'
**   Bit 1       Hex            -- '0' to '9', 'a' to 'f', 'A' to 'F'
**   Bit 2       valid scheme   -- alphadigit | "-" | "." | "+"
**   Bit 3       mark           -- "%" | "$"| "-" | "_" | "." | "!" | "~" | "*" | "'" | "(" | ")" | ","
*/
/*   0  1  2  3  4  5  6  7  8  9  A  B  C  D  E  F */
    {0, 8, 0, 0, 8, 8, 0, 8, 8, 8, 8, 12, 8,12,12, 0,    /* 2x   !"#$%&'()*+,-./  */
     3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 8, 8, 0, 8, 0, 0,    /* 3x  0123456789:;<=>?  */
     8, 3, 3, 3, 3, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1,    /* 4x  @ABCDEFGHIJKLMNO  */
     1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 8,    /* 5X  PQRSTUVWXYZ[\]^_  */
     0, 3, 3, 3, 3, 3, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1,    /* 6x  `abcdefghijklmno  */
     1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 8, 0};   /* 7X  pqrstuvwxyz{|}~  DEL */

PRIVATE inline BOOL IsSafe(WCHAR ch, WORD mask)
{
    if(((ch > 31 ) && (ch < 128) && (isSafe[ch - 32] & mask)))
        return TRUE;

    return FALSE;
}

PRIVATE inline BOOL IsAsciiCharW(WCHAR ch)
{
    return (!(ch >> 8) && ((CHAR) ch));
}

BOOL IsValidSchemeCharW(WCHAR ch)
{
    if(IsAsciiCharW(ch))
        return IsSafe( (CHAR) ch, 5);
    return FALSE;
}



WCHAR const c_szHttpScheme[]           = W("http");
WCHAR const c_szFileScheme[]           = W("file");
WCHAR const c_szFTPScheme[]            = W("ftp");
WCHAR const c_szHttpsScheme[]          = W("https");

const struct
{
    LPCWSTR pszScheme;
    URL_SCHEME eScheme;
    DWORD cchScheme;
    DWORD dwFlags;
} g_mpUrlSchemeTypes[] =
    {
    // Because we use a linear search, sort this in the order of
    // most common usage.
    { c_szHttpScheme,   URL_SCHEME_HTTP,      SIZECHARS(c_szHttpScheme) - 1,     UPF_SCHEME_INTERNET|UPF_SCHEME_CONVERT},
    { c_szFileScheme,   URL_SCHEME_FILE,      SIZECHARS(c_szFileScheme) - 1,     UPF_SCHEME_CONVERT},
    { c_szFTPScheme,    URL_SCHEME_FTP,       SIZECHARS(c_szFTPScheme) - 1,      UPF_SCHEME_INTERNET|UPF_SCHEME_CONVERT},
    { c_szHttpsScheme,  URL_SCHEME_HTTPS,     SIZECHARS(c_szHttpsScheme) -1,     UPF_SCHEME_INTERNET|UPF_SCHEME_CONVERT|UPF_SCHEME_DONTCORRECT},
    };


/*----------------------------------------------------------
Purpose: Return the scheme ordinal type (URL_SCHEME_*) based on the
         URL string.


Returns: URL_SCHEME_ ordinal
Cond:    --
*/

PRIVATE inline BOOL IsSameSchemeW(LPCWSTR pszLocal, LPCWSTR pszGlobal, DWORD cch)
{
    ASSERT(pszLocal);
    ASSERT(pszGlobal);
    ASSERT(cch);

    return !StrCmpNIW(pszLocal, pszGlobal, cch);
}



PRIVATE URL_SCHEME
SchemeTypeFromStringW(
   LPCWSTR psz,
   DWORD cch)
{
   DWORD i;

   // psz is a counted string (by cch), not a null-terminated string,
   // so use IS_VALID_READ_BUFFER instead of IS_VALID_STRING_PTRW.
   ASSERT(IS_VALID_READ_BUFFER(psz, WCHAR, cch));
   ASSERT(cch);

   // We use a linear search.  A binary search wouldn't pay off
   // because the list isn't big enough, and we can sort the list
   // according to the most popular protocol schemes and pay off
   // bigger.

   for (i = 0; i < ARRAYSIZE(g_mpUrlSchemeTypes); i++)
   {
       if(cch == g_mpUrlSchemeTypes[i].cchScheme &&
           IsSameSchemeW(psz, g_mpUrlSchemeTypes[i].pszScheme, cch))
            return g_mpUrlSchemeTypes[i].eScheme;
   }

   return URL_SCHEME_UNKNOWN;
}

inline BOOL IsSeparator(const WCHAR *p)
{
    return (*p == SLASH || *p == WHACK );
}

PRIVATE inline BOOL IsUrlPrefixW(LPCWSTR psz)
{
    //
    // Optimized for this particular case. 
    //
    if (psz[0]==L'u' || psz[0]==L'U') {
        if (psz[1]==L'r' || psz[1]==L'R') {
            if (psz[2]==L'l' || psz[2]==L'L') {
                return TRUE;
            }
        }
    }
    return FALSE;
    // return !StrCmpNIW(psz, c_szURLPrefixW, c_cchURLPrefix);
}

//
//  FindSchemeW() around for Perf reasons for ParseURL()
//  Any changes in either FindScheme() needs to reflected in the other
//
LPCWSTR FindSchemeW(LPCWSTR psz, LPDWORD pcchScheme, BOOL fAllowSemicolon = FALSE)
{
    LPCWSTR pch;
    DWORD cch;

    ASSERT(pcchScheme);
    ASSERT(psz);

    *pcchScheme = 0;

    for (pch = psz, cch = 0; *pch; pch++, cch++)
    {

        if (*pch == L':' ||

            // Autocorrect permits a semicolon typo
            (fAllowSemicolon && *pch == L';'))
        {
            if (IsUrlPrefixW(psz))
            {
                psz = pch +1;

                //  set pcchScheme to skip past "URL:"
                *pcchScheme = cch + 1;

                //  reset cch for the scheme len
                cch = (DWORD) -1;
                continue;
            }
            else
            {
                //
                //  Scheme found if it is at least two characters
                if(cch > 1)
                {
                    *pcchScheme = cch;
                    return psz;
                }
                break;
            }
        }
        if(!IsValidSchemeCharW(*pch))
            break;
    }

    return NULL;
}

PRIVATE DWORD
CountSlashes(LPCWSTR *ppsz)
{
    DWORD cSlashes = 0;
    LPCWSTR pch = *ppsz;

    while (IsSeparator(pch))
    {
        *ppsz = pch;
        pch++;
        cSlashes++;
    }

    return cSlashes;
}

/*----------------------------------------------------------
Purpose: Parse the given path into the PARSEDURL structure.

  ******
  ******  This function must not do any extraneous
  ******  things.  It must be small and fast.
  ******

    Returns: NOERROR if a valid URL format
    URL_E_INVALID_SYNTAX if not

      Cond:    --
*/
STDMETHODIMP
ParseURLW(
          LPCWSTR pcszURL,
          PPARSEDURLW ppu)
{
    HRESULT hr = E_INVALIDARG;

    RIP(IS_VALID_STRING_PTRW(pcszURL, -1));
    RIP(IS_VALID_WRITE_PTR(ppu, PARSEDURLW));

    if (pcszURL && ppu && SIZEOF(*ppu) == ppu->cbSize)
    {
        DWORD cch;
        hr = URL_E_INVALID_SYNTAX;      // assume error

        ppu->pszProtocol = FindSchemeW(pcszURL, &cch);

        if(ppu->pszProtocol)
        {
            ppu->cchProtocol = cch;

            // Determine protocol scheme number
            ppu->nScheme = SchemeTypeFromStringW(ppu->pszProtocol, cch);

            ppu->pszSuffix = ppu->pszProtocol + cch + 1;

            //
            //  APPCOMPAT - Backwards compatibility.  
            //  ParseURL() believes in file: urls like "file://C:\foo\bar"
            //  and some pieces of code will use it to get the Dos Path.
            //  new code should always call PathCreateFromUrl() to
            //  get the dos path of a file: URL.
            //
            //  i am leaving this behavior in case some compat stuff is out there.
            //
            if (URL_SCHEME_FILE == ppu->nScheme &&
                '/' == ppu->pszSuffix[0] && '/' == ppu->pszSuffix[1])
            {
                // Yes; skip the "//"
                ppu->pszSuffix += 2;

#ifndef PLATFORM_UNIX
                // There might be a third slash.  Skip it.
                // IEUNIX - On UNIX, it's a root directory, so don't skip it!
                if ('/' == *ppu->pszSuffix)
                    ppu->pszSuffix++;
#endif
            }

            ppu->cchSuffix = lstrlenW(ppu->pszSuffix);

            hr = S_OK;
        }
    }


#ifdef DEBUG
    if (hr==S_OK)
    {
        WCHAR rgchDebugProtocol[MAX_PATH_FNAME];
        WCHAR rgchDebugSuffix[MAX_PATH_FNAME];

        // (+ 1) for null terminator.

        StrCpyNW(rgchDebugProtocol, ppu->pszProtocol,
            min(ppu->cchProtocol + 1, SIZECHARS(rgchDebugProtocol)));

        // (+ 1) for null terminator.

        StrCpyNW(rgchDebugSuffix, ppu->pszSuffix,
            min(ppu->cchSuffix + 1, SIZECHARS(rgchDebugSuffix)));

    }
#endif

    return(hr);
}

STDAPI_(BOOL) PathIsURLW(IN LPCWSTR pszPath)
{
    PARSEDURLW pu;

    if (!pszPath)
        return FALSE;

    RIPMSG(IS_VALID_STRING_PTR(pszPath, -1), "PathIsURL: caller passed bad pszPath");

    pu.cbSize = SIZEOF(pu);
    return SUCCEEDED(ParseURLW(pszPath, &pu));
}
