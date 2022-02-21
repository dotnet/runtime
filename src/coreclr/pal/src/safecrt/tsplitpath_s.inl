// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*tsplitpath_s.inl - general implementation of _tsplitpath_s
*

*
*Purpose:
*       This file contains the general algorithm for _splitpath_s and its variants.
*
*******************************************************************************/

_FUNC_PROLOGUE
errno_t __cdecl _FUNC_NAME(
    _In_z_ const _CHAR *_Path,
    _Out_writes_opt_z_(_DriveSize) _CHAR *_Drive, _In_ size_t _DriveSize,
    _Out_writes_opt_z_(_DirSize) _CHAR *_Dir, _In_ size_t _DirSize,
    _Out_writes_opt_z_(_FilenameSize) _CHAR *_Filename, _In_ size_t _FilenameSize,
    _Out_writes_opt_z_(_ExtSize) _CHAR *_Ext, _In_ size_t _ExtSize
)
{
    const _CHAR *tmp;
    const _CHAR *last_slash;
    const _CHAR *dot;
    int drive_set = 0;
    size_t length = 0;
    int bEinval = 0;

    /* validation section */
    if (_Path == NULL)
    {
        goto error_einval;
    }
    if ((_Drive == NULL && _DriveSize != 0) || (_Drive != NULL && _DriveSize == 0))
    {
        goto error_einval;
    }
    if ((_Dir == NULL && _DirSize != 0) || (_Dir != NULL && _DirSize == 0))
    {
        goto error_einval;
    }
    if ((_Filename == NULL && _FilenameSize != 0) || (_Filename != NULL && _FilenameSize == 0))
    {
        goto error_einval;
    }
    if ((_Ext == NULL && _ExtSize != 0) || (_Ext != NULL && _ExtSize == 0))
    {
        goto error_einval;
    }

    /* check if _Path begins with the longpath prefix */
    if (_Path[0] == _T('\\') && _Path[1] == _T('\\') && _Path[2] == _T('?') && _Path[3] == _T('\\'))
    {
        _Path += 4;
    }

    /* extract drive letter and ':', if any */
    if (!drive_set)
    {
// The CorUnix PAL is never built on Windows and thus, the code below
// for the drive check is not required.
#if 0
        size_t skip = _MAX_DRIVE - 2;
        tmp = _Path;
        while (skip > 0 && *tmp != 0)
        {
            skip--;
            tmp++;
        }
        if (*tmp == _T(':'))
        {
            if (_Drive != NULL)
            {
                if (_DriveSize < _MAX_DRIVE)
                {
                    goto error_erange;
                }
                _TCSNCPY_S(_Drive, _DriveSize, _Path, _MAX_DRIVE - 1);
            }
            _Path = tmp + 1;
        }
        else
#endif
        {
            if (_Drive != NULL)
            {
                _RESET_STRING(_Drive, _DriveSize);
            }
        }
    }

    /* extract path string, if any. _Path now points to the first character
     * of the path, if any, or the filename or extension, if no path was
     * specified.  Scan ahead for the last occurrence, if any, of a '/' or
     * '\' path separator character.  If none is found, there is no path.
     * We will also note the last '.' character found, if any, to aid in
     * handling the extension.
     */
    last_slash = NULL;
    dot = NULL;
    tmp = _Path;
    for (; *tmp != 0; ++tmp)
    {
        if (*tmp == _T('/') || *tmp == _T('\\'))
        {
            /* point to one beyond for later copy */
            last_slash = tmp + 1;
        }
        else if (*tmp == _T('.'))
        {
            dot = tmp;
        }
    }

    if (last_slash != NULL)
    {
        /* found a path - copy up through last_slash or max characters
         * allowed, whichever is smaller
         */
        if (_Dir != NULL) {
            length = (size_t)(last_slash - _Path);
            if (_DirSize <= length)
            {
                goto error_erange;
            }
            _TCSNCPY_S(_Dir, _DirSize, _Path, length);

            // Normalize the path seperator
            size_t iIndex;
            for(iIndex = 0; iIndex < length; iIndex++)
            {
                if (_Dir[iIndex] == _T('\\'))
                {
                    _Dir[iIndex] = _T('/');
                }
            }
        }
        _Path = last_slash;
    }
    else
    {
        /* there is no path */
        if (_Dir != NULL)
        {
            _RESET_STRING(_Dir, _DirSize);
        }
    }

    /* extract file name and extension, if any.  Path now points to the
     * first character of the file name, if any, or the extension if no
     * file name was given.  Dot points to the '.' beginning the extension,
     * if any.
     */
    if (dot != NULL && (dot >= _Path))
    {
		/* found the marker for an extension - copy the file name up to the '.' */
        if (_Filename)
        {
            length = (size_t)(dot - _Path);
            if (length == 0)
            {
                // At this time, dot will be equal to _Path if string is something like "/."
                // since _path was set to last_slash, which in turn, was set to "tmp +1"
                // where "tmp" is the location where "/" was found. See code above for
                // clarification.
                //
                // For such cases, return the "." in filename buffer.
                //
                // Thus, if the length is zero, we know its a string like "/." and thus, we
                // set length to 1 to get the "." in filename buffer.
                length = 1;
            }

            if (_FilenameSize <= length)
            {
                goto error_erange;
            }
            _TCSNCPY_S(_Filename, _FilenameSize, _Path, length);
        }

        /* now we can get the extension - remember that tmp still points
         * to the terminating NULL character of path.
         */
        if (_Ext)
        {
            // At this time, _Path is pointing to the character after the last slash found.
            // (See comments and code above for clarification).
            //
            // Returns extension as empty string for strings like "/.".
            if (dot > _Path)
            {
                length = (size_t)(tmp - dot);
                if (_ExtSize <= length)
                {
                     goto error_erange;
                }

                /* Since dot pointed to the ".", make sure we actually have an extension
                like ".cmd" and not just ".", OR

                Confirm that its a string like "/.." - for this, return the
                second "." in the extension part.

                However, for strings like "/myfile.", return empty string
                in extension buffer.
                */
                int fIsDir = (*(dot-1) == _T('.'))?1:0;
                if (length > 1 || (length == 1 && fIsDir == 1))
                    _TCSNCPY_S(_Ext, _ExtSize, dot, length);
                else
                    _RESET_STRING(_Ext, _ExtSize);
            }
            else
                _RESET_STRING(_Ext, _ExtSize);
        }
    }
    else
    {
        /* found no extension, give empty extension and copy rest of
         * string into fname.
         */
        if (_Filename)
        {
            length = (size_t)(tmp - _Path);
            if (_FilenameSize <= length)
            {
                goto error_erange;
            }
            _TCSNCPY_S(_Filename, _FilenameSize, _Path, length);
        }
        if (_Ext)
        {
            _RESET_STRING(_Ext, _ExtSize);
        }
    }

    _RETURN_NO_ERROR;

error_einval:
    bEinval = 1;

error_erange:
    if (_Drive != NULL && _DriveSize > 0)
    {
        _RESET_STRING(_Drive, _DriveSize);
    }
    if (_Dir != NULL && _DirSize > 0)
    {
        _RESET_STRING(_Dir, _DirSize);
    }
    if (_Filename != NULL && _FilenameSize > 0)
    {
        _RESET_STRING(_Filename, _FilenameSize);
    }
    if (_Ext != NULL && _ExtSize > 0)
    {
        _RESET_STRING(_Ext, _ExtSize);
    }

    _VALIDATE_POINTER(_Path);
    if (bEinval)
    {
        _RETURN_EINVAL;
    }
    return (errno = ERANGE);
}
