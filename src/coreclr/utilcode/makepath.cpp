// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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
        _Out_ CQuickWSTR &szPath,
        _In_ LPCWSTR drive,
        _In_ LPCWSTR dir,
        _In_ LPCWSTR fname,
        _In_ LPCWSTR ext
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

                // suppress warning for the following line; this is safe but would require significant code
                // delta for prefast to understand.
#ifdef _PREFAST_
                #pragma warning( suppress: 26001 )
#endif
                if (*(p-1) != _T('/') && *(p-1) != _T('\\')) {
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


// Returns the directory for clr module. So, if path was for "C:\Dir1\Dir2\Filename.DLL",
// then this would return "C:\Dir1\Dir2\" (note the trailing backslash).HRESULT GetClrModuleDirectory(SString& wszPath)
HRESULT GetClrModuleDirectory(SString& wszPath)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        CANNOT_TAKE_LOCK;
    }
    CONTRACTL_END;

    DWORD dwRet = GetClrModulePathName(wszPath);

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
