//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*tmakepath_s.inl - general implementation of _tmakepath_s
*

*
*Purpose:
*       This file contains the general algorithm for _makepath_s and its variants.
*
*******************************************************************************/

_FUNC_PROLOGUE
errno_t __cdecl _FUNC_NAME(__out_ecount_z(_SIZE) _CHAR *_DEST, __in_opt size_t _SIZE, __in_z_opt const _CHAR *_Drive, __in_z_opt const _CHAR *_Dir, __in_z_opt const _CHAR *_Filename, __in_z_opt const _CHAR *_Ext)
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

#if _MBS_SUPPORT
        p = _MBSDEC(_Dir, p);
#else  /* _MBS_SUPPORT */
        p = p - 1;
#endif  /* _MBS_SUPPORT */
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
