// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*tmakepath_s.inl - general implementation of _tmakepath_s
*

*
*Purpose:
*       This file contains the general algorithm for _makepath_s and its variants.
*
*******************************************************************************/

_FUNC_PROLOGUE
errno_t __cdecl _FUNC_NAME(_Out_writes_z_(_SIZE) _CHAR *_DEST, _In_opt_ size_t _SIZE, _In_opt_z_ const _CHAR *_Drive, _In_opt_z_ const _CHAR *_Dir, _In_opt_z_ const _CHAR *_Filename, _In_opt_z_ const _CHAR *_Ext)
{
    size_t written;
    const _CHAR *p;
    _CHAR *d;

    /* validation section */
    _VALIDATE_STRING(_DEST, _SIZE);

    /* copy drive */
    written = 0;
    d = _DEST;
    if (_Drive != NULL && *_Drive != 0)
    {
        written += 2;
        if(written >= _SIZE)
        {
            goto error_return;
        }
        *d++ = *_Drive;
        *d++ = _T(':');
    }

    /* copy dir */
    p = _Dir;
    if (p != NULL && *p != 0)
    {
        do {
            if(++written >= _SIZE)
            {
                goto error_return;
            }
            *d++ = *p++;
        } while (*p != 0);

        p--;
        if (*p != _T('/') && *p != _T('\\'))
        {
            if(++written >= _SIZE)
            {
                goto error_return;
            }
            *d++ = _T('\\');
        }
    }

    /* copy fname */
    p = _Filename;
    if (p != NULL)
    {
        while (*p != 0)
        {
            if(++written >= _SIZE)
            {
                goto error_return;
            }
            *d++ = *p++;
        }
    }

    /* copy extension; check to see if a '.' needs to be inserted */
    p = _Ext;
    if (p != NULL)
    {
        if (*p != 0 && *p != _T('.'))
        {
            if(++written >= _SIZE)
            {
                goto error_return;
            }
            *d++ = _T('.');
        }
        while (*p != 0)
        {
            if(++written >= _SIZE)
            {
                goto error_return;
            }
            *d++ = *p++;
        }
    }

    if(++written > _SIZE)
    {
        goto error_return;
    }
    *d = 0;
    _FILL_STRING(_DEST, _SIZE, written);
    _RETURN_NO_ERROR;

error_return:
    _RESET_STRING(_DEST, _SIZE);
    _RETURN_BUFFER_TOO_SMALL(_DEST, _SIZE);

    /* should never happen, but compiler can't tell */
    return EINVAL;
}
