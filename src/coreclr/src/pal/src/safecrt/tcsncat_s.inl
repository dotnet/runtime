//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/***
*tcsncat_s.inl - general implementation of _tcscpy_s
*

*
*Purpose:
*       This file contains the general algorithm for strncat_s and its variants.
*
****/

_FUNC_PROLOGUE
errno_t __cdecl _FUNC_NAME(_CHAR *_DEST, size_t _SIZE, const _CHAR *_SRC, size_t _COUNT)
{
    _CHAR *p;
    size_t available;

    if (_COUNT == 0 && _DEST == NULL && _SIZE == 0)
    {
        /* this case is allowed; nothing to do */
        _RETURN_NO_ERROR;
    }

    /* validation section */
    _VALIDATE_STRING(_DEST, _SIZE);
    if (_COUNT != 0)
    {
        _VALIDATE_POINTER_RESET_STRING(_SRC, _DEST, _SIZE);
    }

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

    if (_COUNT == _TRUNCATE)
    {
        while ((*p++ = *_SRC++) != 0 && --available > 0)
        {
        }
    }
    else
    {
        _ASSERT_EXPR((!_CrtGetCheckCount() || _COUNT < available), "Buffer is too small");

        while (_COUNT > 0 && (*p++ = *_SRC++) != 0 && --available > 0)
        {
            _COUNT--;
        }
        if (_COUNT == 0)
        {
            *p = 0;
        }
    }

    if (available == 0)
    {
        if (_COUNT == _TRUNCATE)
        {
            _DEST[_SIZE - 1] = 0;
            _RETURN_TRUNCATE;
        }
        _RESET_STRING(_DEST, _SIZE);
        _RETURN_BUFFER_TOO_SMALL(_DEST, _SIZE);
    }
    _FILL_STRING(_DEST, _SIZE, _SIZE - available + 1);
    _RETURN_NO_ERROR;
}

