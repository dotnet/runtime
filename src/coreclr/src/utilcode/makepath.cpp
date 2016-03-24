// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/***
*makepath.c - create path name from components
*

*
*Purpose:
*       To provide support for creation of full path names from components
*
*******************************************************************************/
#include "stdafx.h"
#include "winwrap.h"
#include "utilcode.h"
#include "ex.h"

#ifndef FEATURE_CORECLR
/***
*void _makepath() - build path name from components
*
*Purpose:
*       create a path name from its individual components
*
*Entry:
*       WCHAR *path  - pointer to buffer for constructed path
*       WCHAR *drive - pointer to drive component, may or may not contain
*                     trailing ':'
*       WCHAR *dir   - pointer to subdirectory component, may or may not include
*                     leading and/or trailing '/' or '\' characters
*       WCHAR *fname - pointer to file base name component
*       WCHAR *ext   - pointer to extension component, may or may not contain
*                     a leading '.'.
*
*Exit:
*       path - pointer to constructed path name
*
*Exceptions:
*
*******************************************************************************/

void MakePath (
        __out_ecount (MAX_LONGPATH) WCHAR *path,
        __in LPCWSTR drive,
        __in LPCWSTR dir,
        __in LPCWSTR fname,
        __in LPCWSTR ext
        )
{
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
            FORBID_FAULT;
        }
        CONTRACTL_END

        const WCHAR *p;
        DWORD count = 0;

        /* we assume that the arguments are in the following form (although we
         * do not diagnose invalid arguments or illegal filenames (such as
         * names longer than 8.3 or with illegal characters in them)
         *
         *  drive:
         *      A           ; or
         *      A:
         *  dir:
         *      \top\next\last\     ; or
         *      /top/next/last/     ; or
         *      either of the above forms with either/both the leading
         *      and trailing / or \ removed.  Mixed use of '/' and '\' is
         *      also tolerated
         *  fname:
         *      any valid file name
         *  ext:
         *      any valid extension (none if empty or null )
         */

        /* copy drive */

        if (drive && *drive) {
                *path++ = *drive;
                *path++ = _T(':');
                count += 2;
        }

        /* copy dir */

        if ((p = dir)) {
                while (*p) {
                        *path++ = *p++;
                        count++;

                        if (count == MAX_LONGPATH) {
                            --path;
                            *path = _T('\0');
                            return;
                        }
                }

#ifdef _MBCS
                if (*(p=_mbsdec(dir,p)) != _T('/') && *p != _T('\\')) {
#else  /* _MBCS */
                // suppress warning for the following line; this is safe but would require significant code
                // delta for prefast to understand.
#ifdef _PREFAST_
                #pragma warning( suppress: 26001 ) 
#endif
                if (*(p-1) != _T('/') && *(p-1) != _T('\\')) {
#endif  /* _MBCS */
                        *path++ = _T('\\');
                        count++;

                        if (count == MAX_LONGPATH) {
                            --path;
                            *path = _T('\0');
                            return;
                        }
                }
        }

        /* copy fname */

        if ((p = fname)) {
                while (*p) {
                        *path++ = *p++;
                        count++;

                        if (count == MAX_LONGPATH) {
                            --path;
                            *path = _T('\0');
                            return;
                        }
                }
        }

        /* copy ext, including 0-terminator - check to see if a '.' needs
         * to be inserted.
         */

        if ((p = ext)) {
                if (*p && *p != _T('.')) {
                        *path++ = _T('.');
                        count++;

                        if (count == MAX_LONGPATH) {
                            --path;
                            *path = _T('\0');
                            return;
                        }
                }

                while ((*path++ = *p++)) {
                    count++;

                    if (count == MAX_LONGPATH) {
                        --path;
                        *path = _T('\0');
                        return;
                    }
                }
        }
        else {
                /* better add the 0-terminator */
                *path = _T('\0');
        }
}
#endif // !FEATURE_CORECLR

/***
*void Makepath() - build path name from components
*
*Purpose:
*       create a path name from its individual components
*
*Entry:
*       CQuickWSTR &szPath - Buffer for constructed path
*       WCHAR *drive - pointer to drive component, may or may not contain
*                     trailing ':'
*       WCHAR *dir   - pointer to subdirectory component, may or may not include
*                     leading and/or trailing '/' or '\' characters
*       WCHAR *fname - pointer to file base name component
*       WCHAR *ext   - pointer to extension component, may or may not contain
*                     a leading '.'.
*
*Exit:
*       path - pointer to constructed path name
*
*Exceptions:
*
*******************************************************************************/

void MakePath (
        __out CQuickWSTR &szPath,
        __in LPCWSTR drive,
        __in LPCWSTR dir,
        __in LPCWSTR fname,
        __in LPCWSTR ext
        )
{
        CONTRACTL
        {
            NOTHROW;
            GC_NOTRIGGER;
        }
        CONTRACTL_END

        SIZE_T maxCount = 4      // Possible separators between components, plus null terminator
            + (drive != nullptr ? 2 : 0)
            + (dir != nullptr ? wcslen(dir) : 0)
            + (fname != nullptr ? wcslen(fname) : 0)
            + (ext != nullptr ? wcslen(ext) : 0);
        LPWSTR path = szPath.AllocNoThrow(maxCount);

        const WCHAR *p;
        DWORD count = 0;

        /* we assume that the arguments are in the following form (although we
         * do not diagnose invalid arguments or illegal filenames (such as
         * names longer than 8.3 or with illegal characters in them)
         *
         *  drive:
         *      A           ; or
         *      A:
         *  dir:
         *      \top\next\last\     ; or
         *      /top/next/last/     ; or
         *      either of the above forms with either/both the leading
         *      and trailing / or \ removed.  Mixed use of '/' and '\' is
         *      also tolerated
         *  fname:
         *      any valid file name
         *  ext:
         *      any valid extension (none if empty or null )
         */

        /* copy drive */

        if (drive && *drive) {
                *path++ = *drive;
                *path++ = _T(':');
                count += 2;
        }

        /* copy dir */

        if ((p = dir)) {
                while (*p) {
                        *path++ = *p++;
                        count++;

                        _ASSERTE(count < maxCount);
                }

#ifdef _MBCS
                if (*(p=_mbsdec(dir,p)) != _T('/') && *p != _T('\\')) {
#else  /* _MBCS */
                // suppress warning for the following line; this is safe but would require significant code
                // delta for prefast to understand.
#ifdef _PREFAST_
                #pragma warning( suppress: 26001 ) 
#endif
                if (*(p-1) != _T('/') && *(p-1) != _T('\\')) {
#endif  /* _MBCS */
                        *path++ = _T('\\');
                        count++;

                        _ASSERTE(count < maxCount);
                }
        }

        /* copy fname */

        if ((p = fname)) {
                while (*p) {
                        *path++ = *p++;
                        count++;

                        _ASSERTE(count < maxCount);
                }
        }

        /* copy ext, including 0-terminator - check to see if a '.' needs
         * to be inserted.
         */

        if ((p = ext)) {
                if (*p && *p != _T('.')) {
                        *path++ = _T('.');
                        count++;

                        _ASSERTE(count < maxCount);
                }

                while ((*path++ = *p++)) {
                    count++;

                    _ASSERTE(count < maxCount);
                }
        }
        else {
                /* better add the 0-terminator */
                *path = _T('\0');
        }

        szPath.Shrink(count + 1);
}

#if !defined(FEATURE_CORECLR)
static LPCWSTR g_wszProcessExePath = NULL;

HRESULT GetProcessExePath(LPCWSTR *pwszProcessExePath)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CONSISTENCY_CHECK(CheckPointer(pwszProcessExePath));
    }
    CONTRACTL_END;

    HRESULT hr = S_OK;

    if (g_wszProcessExePath == NULL)
    {
        DWORD cchProcName = 0;
        NewArrayHolder<WCHAR> wszProcName;
        EX_TRY
        {
            PathString wszProcNameString;
            cchProcName = WszGetModuleFileName(NULL, wszProcNameString);
            if (cchProcName == 0)
            {
                hr = HRESULT_FROM_GetLastError();
            }
            else
            {
                wszProcName = wszProcNameString.GetCopyOfUnicodeString();
            }
        }
        EX_CATCH_HRESULT(hr);

        if (FAILED(hr))
        {
            return hr;
        }
        
        if (InterlockedCompareExchangeT(&g_wszProcessExePath, const_cast<LPCWSTR>(wszProcName.GetValue()), NULL) == NULL)
        {
            wszProcName.SuppressRelease();
        }
    }
    _ASSERTE(g_wszProcessExePath != NULL);
    _ASSERTE(SUCCEEDED(hr));

    *pwszProcessExePath = g_wszProcessExePath;
    return hr;
}
#endif

// Returns the directory for HMODULE. So, if HMODULE was for "C:\Dir1\Dir2\Filename.DLL",
// then this would return "C:\Dir1\Dir2\" (note the trailing backslash).
HRESULT GetHModuleDirectory(
    __in                          HMODULE   hMod,
    SString&                                 wszPath)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    DWORD dwRet = WszGetModuleFileName(hMod, wszPath);
   
     if (dwRet == 0)
    {   // Some other error.
        return HRESULT_FROM_GetLastError();
    }

     CopySystemDirectory(wszPath, wszPath);
         

    return S_OK;
}

//
// Returns path name from a file name. 
// Example: For input "C:\Windows\System.dll" returns "C:\Windows\".
// Warning: The input file name string might be destroyed.
// 
// Arguments:
//    pPathString - [in] SString with file  name
//                
//    pBuffer    - [out] SString .
// 
// Return Value:
//    S_OK - Output buffer contains path name.
//    other errors - If Sstring throws.
//
HRESULT CopySystemDirectory(const SString& pPathString,
                            SString& pbuffer)
{
    HRESULT hr = S_OK;
    EX_TRY
    {
        pbuffer.Set(pPathString);
        SString::Iterator iter = pbuffer.End();
        if (pbuffer.FindBack(iter,DIRECTORY_SEPARATOR_CHAR_W))
        {
            iter++;
            pbuffer.Truncate(iter);
        }
        else
        {
            hr = E_UNEXPECTED;
        }
    }
    EX_CATCH_HRESULT(hr);

    return hr;
}
