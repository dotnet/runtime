// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/***
*splitpath.c - break down path name into components
*

*
*Purpose:
*       To provide support for accessing the individual components of an
*       arbitrary path name
*
*******************************************************************************/
#include "stdafx.h"
#include "winwrap.h"
#include "utilcode.h"
#include "sstring.h"


/***
*SplitPath() - split a path name into its individual components
*
*Purpose:
*       to split a path name into its individual components
*
*Entry:
*       path  - pointer to path name to be parsed
*       drive - pointer to buffer for drive component, if any
*       dir   - pointer to buffer for subdirectory component, if any
*       fname - pointer to buffer for file base name component, if any
*       ext   - pointer to buffer for file name extension component, if any
*
*Exit:
*       drive - pointer to drive string.  Includes ':' if a drive was given.
*       dir   - pointer to subdirectory string.  Includes leading and trailing
*           '/' or '\', if any.
*       fname - pointer to file base name
*       ext   - pointer to file extension, if any.  Includes leading '.'.
*
*Exceptions:
*
*******************************************************************************/

void SplitPath(
        const WCHAR *path,
        __inout_z __inout_ecount_opt(driveSizeInWords) WCHAR *drive, int driveSizeInWords,
        __inout_z __inout_ecount_opt(dirSizeInWords) WCHAR *dir, int dirSizeInWords,
        __inout_z __inout_ecount_opt(fnameSizeInWords) WCHAR *fname, size_t fnameSizeInWords,
        __inout_z __inout_ecount_opt(extSizeInWords) WCHAR *ext, size_t extSizeInWords)
{
    WRAPPER_NO_CONTRACT;

    LPCWSTR _wszDrive, _wszDir, _wszFileName, _wszExt;
    size_t _cchDrive, _cchDir, _cchFileName, _cchExt;

    SplitPathInterior(path,
                      &_wszDrive, &_cchDrive,
                      &_wszDir, &_cchDir,
                      &_wszFileName, &_cchFileName,
                      &_wszExt, &_cchExt);

    if (drive && _wszDrive)
        wcsncpy_s(drive, driveSizeInWords, _wszDrive, min(_cchDrive, _MAX_DRIVE));

    if (dir && _wszDir)
        wcsncpy_s(dir, dirSizeInWords, _wszDir, min(_cchDir, _MAX_DIR));

    if (fname && _wszFileName)
        wcsncpy_s(fname, fnameSizeInWords, _wszFileName, min(_cchFileName, _MAX_FNAME));

    if (ext && _wszExt)
        wcsncpy_s(ext, extSizeInWords, _wszExt, min(_cchExt, _MAX_EXT));
}

//*******************************************************************************
// A much more sensible version that just points to each section of the string.
//*******************************************************************************
void    SplitPathInterior(
    _In_      LPCWSTR wszPath,
    _Out_opt_ LPCWSTR *pwszDrive,    _Out_opt_ size_t *pcchDrive,
    _Out_opt_ LPCWSTR *pwszDir,      _Out_opt_ size_t *pcchDir,
    _Out_opt_ LPCWSTR *pwszFileName, _Out_opt_ size_t *pcchFileName,
    _Out_opt_ LPCWSTR *pwszExt,      _Out_opt_ size_t *pcchExt)
{
    LIMITED_METHOD_CONTRACT;

    // Arguments must come in valid pairs
    _ASSERTE(!!pwszDrive == !!pcchDrive);
    _ASSERTE(!!pwszDir == !!pcchDir);
    _ASSERTE(!!pwszFileName == !!pcchFileName);
    _ASSERTE(!!pwszExt == !!pcchExt);

    WCHAR *p;
    LPCWSTR last_slash = NULL, dot = NULL;

    /* we assume that the path argument has the following form, where any
     * or all of the components may be missing.
     *
     *  <drive><dir><fname><ext>
     *
     * and each of the components has the following expected form(s)
     *
     *  drive:
     *  0 to _MAX_DRIVE-1 characters, the last of which, if any, is a
     *  ':'
     *  dir:
     *  0 to _MAX_DIR-1 characters in the form of an absolute path
     *  (leading '/' or '\') or relative path, the last of which, if
     *  any, must be a '/' or '\'.  E.g -
     *  absolute path:
     *      \top\next\last\     ; or
     *      /top/next/last/
     *  relative path:
     *      top\next\last\  ; or
     *      top/next/last/
     *  Mixed use of '/' and '\' within a path is also tolerated
     *  fname:
     *  0 to _MAX_FNAME-1 characters not including the '.' character
     *  ext:
     *  0 to _MAX_EXT-1 characters where, if any, the first must be a
     *  '.'
     *
     */

    /* extract drive letter and :, if any */

    if ((wcslen(wszPath) > (_MAX_DRIVE - 2)) && (*(wszPath + _MAX_DRIVE - 2) == _T(':'))) {
        if (pwszDrive && pcchDrive) {
            *pwszDrive = wszPath;
            *pcchDrive = _MAX_DRIVE - 1;
        }
        wszPath += _MAX_DRIVE - 1;
    }
    else if (pwszDrive && pcchDrive) {
        *pwszDrive = NULL;
        *pcchDrive = 0;
    }

    /* extract path string, if any.  Path now points to the first character
     * of the path, if any, or the filename or extension, if no path was
     * specified.  Scan ahead for the last occurrence, if any, of a '/' or
     * '\' path separator character.  If none is found, there is no path.
     * We will also note the last '.' character found, if any, to aid in
     * handling the extension.
     */

    for (last_slash = NULL, p = (WCHAR *)wszPath; *p; p++) {
        if (*p == _T('/') || *p == _T('\\'))
            /* point to one beyond for later copy */
            last_slash = p + 1;
        else if (*p == _T('.'))
            dot = p;
    }

    if (last_slash) {
        /* found a path - copy up through last_slash or max. characters
         * allowed, whichever is smaller
         */

        if (pwszDir && pcchDir) {
            *pwszDir = wszPath;
            *pcchDir = last_slash - wszPath;
        }
        wszPath = last_slash;
    }
    else if (pwszDir && pcchDir) {
        *pwszDir = NULL;
        *pcchDir = 0;
    }

    /* extract file name and extension, if any.  Path now points to the
     * first character of the file name, if any, or the extension if no
     * file name was given.  Dot points to the '.' beginning the extension,
     * if any.
     */

    if (dot && (dot >= wszPath)) {
        /* found the marker for an extension - copy the file name up to
         * the '.'.
         */
        if (pwszFileName && pcchFileName) {
            *pwszFileName = wszPath;
            *pcchFileName = dot - wszPath;
        }
        /* now we can get the extension - remember that p still points
         * to the terminating nul character of path.
         */
        if (pwszExt && pcchExt) {
            *pwszExt = dot;
            *pcchExt = p - dot;
        }
    }
    else {
        /* found no extension, give empty extension and copy rest of
         * string into fname.
         */
        if (pwszFileName && pcchFileName) {
            *pwszFileName = wszPath;
            *pcchFileName = p - wszPath;
        }
        if (pwszExt && pcchExt) {
            *pwszExt = NULL;
            *pcchExt = 0;
        }
    }
}

/***
*SplitPath() - split a path name into its individual components
*
*Purpose:
*       to split a path name into its individual components
*
*Entry:
*       path  - SString representing the path name to be parsed
*       drive - Out SString for drive component
*       dir   - Out SString for subdirectory component
*       fname - Out SString for file base name component
*       ext   - Out SString for file name extension component
*
*Exit:
*       drive - Drive string.  Includes ':' if a drive was given.
*       dir   - Subdirectory string.  Includes leading and trailing
*           '/' or '\', if any.
*       fname - File base name
*       ext   - File extension, if any.  Includes leading '.'.
*
*Exceptions:
*
*******************************************************************************/

void    SplitPath(_In_ SString const &path,
                  __inout_opt SString *drive,
                  __inout_opt SString *dir,
                  __inout_opt SString *fname,
                  __inout_opt SString *ext)
{
    LPCWSTR wzDrive, wzDir, wzFname, wzExt;
    size_t cchDrive, cchDir, cchFname, cchExt;

    SplitPathInterior(path,
            &wzDrive, &cchDrive,
            &wzDir, &cchDir,
            &wzFname, &cchFname,
            &wzExt, &cchExt);

    if (drive != NULL)
        drive->Set(wzDrive, (COUNT_T)cchDrive);

    if (dir != NULL)
        dir->Set(wzDir, (COUNT_T)cchDir);

    if (fname != NULL)
        fname->Set(wzFname, (COUNT_T)cchFname);

    if (ext != NULL)
        ext->Set(wzExt, (COUNT_T)cchExt);
}

