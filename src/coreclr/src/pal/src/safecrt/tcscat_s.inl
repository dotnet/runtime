// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*tcscat_s.inl - general implementation of _tcscpy_s
*

*
*Purpose:
*       This file contains the general algorithm for strcat_s and its variants.
*
****/

_FUNC_PROLOGUE
errno_t __cdecl _FUNC_NAME(_CHAR *_DEST, size_t _SIZE, const _CHAR *_SRC)
{
    _CHAR *p;
    size_t available;

    /* validation section */
    _VALIDATE_STRING(_DEST, _SIZE);
    _VALIDATE_POINTER_RESET_STRING(_SRC, _DEST, _SIZE);

    p = _DEST;
    available = _SIZE;
    while (available > 0 && *p != 0)
    {
        p++;
        available--;
    }

    if (available == 0)
    {
        _RESET_STRING(_DEST, _SIZE);
        _RETURN_DEST_NOT_NULL_TERMINATED(_DEST, _SIZE);
    }

    while ((*p++ = *_SRC++) != 0 && --available > 0)
    {
    }

    if (available == 0)
    {
        _RESET_STRING(_DEST, _SIZE);
        _RETURN_BUFFER_TOO_SMALL(_DEST, _SIZE);
    }
    _FILL_STRING(_DEST, _SIZE, _SIZE - available + 1);
    _RETURN_NO_ERROR;
}

