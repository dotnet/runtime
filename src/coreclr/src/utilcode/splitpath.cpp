// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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
*_splitpath() - split a path name into its individual components
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
    __in      LPCWSTR wszPath,
    __out_opt LPCWSTR *pwszDrive,    __out_opt size_t *pcchDrive,
    __out_opt LPCWSTR *pwszDir,      __out_opt size_t *pcchDir,
    __out_opt LPCWSTR *pwszFileName, __out_opt size_t *pcchFileName,
    __out_opt LPCWSTR *pwszExt,      __out_opt size_t *pcchExt)
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

    if ((wcslen(wszPath) >= (_MAX_DRIVE - 2)) && (*(wszPath + _MAX_DRIVE - 2) == _T(':'))) {
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
     * specified.  Scan ahead for the last occurence, if any, of a '/' or
     * '\' path separator character.  If none is found, there is no path.
     * We will also note the last '.' character found, if any, to aid in
     * handling the extension.
     */

    for (last_slash = NULL, p = (WCHAR *)wszPath; *p; p++) {
#ifdef _MBCS
        if (_ISLEADBYTE (*p))
            p++;
        else {
#endif  /* _MBCS */
        if (*p == _T('/') || *p == _T('\\'))
            /* point to one beyond for later copy */
            last_slash = p + 1;
        else if (*p == _T('.'))
            dot = p;
#ifdef _MBCS
        }
#endif  /* _MBCS */
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
*_splitpath() - split a path name into its individual components
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

void    SplitPath(__in SString const &path,
                  __inout_opt SString *drive,
                  __inout_opt SString *dir,
                  __inout_opt SString *fname,
                  __inout_opt SString *ext)
{
    LPWSTR wzDrive = NULL;
    if (drive != NULL)
        wzDrive = drive->OpenUnicodeBuffer(_MAX_DRIVE);

    LPWSTR wzDir = NULL;
    if (dir != NULL)
        wzDir = dir->OpenUnicodeBuffer(_MAX_DIR);

    LPWSTR wzFname = NULL;
    if (fname != NULL)
        wzFname = fname->OpenUnicodeBuffer(_MAX_FNAME);

    LPWSTR wzExt = NULL;
    if (ext != NULL)
        wzExt = ext->OpenUnicodeBuffer(_MAX_EXT);

    SplitPath(path,
            wzDrive, _MAX_DRIVE,
            wzDir, _MAX_DIR,
            wzFname, _MAX_FNAME,
            wzExt, _MAX_EXT);

    if (drive != NULL)
        drive->CloseBuffer(static_cast<COUNT_T>(wcslen(wzDrive)));

    if (dir != NULL)
        dir->CloseBuffer(static_cast<COUNT_T>(wcslen(wzDir)));

    if (fname != NULL)
        fname->CloseBuffer(static_cast<COUNT_T>(wcslen(wzFname)));

    if (ext != NULL)
        ext->CloseBuffer(static_cast<COUNT_T>(wcslen(wzExt)));
}

