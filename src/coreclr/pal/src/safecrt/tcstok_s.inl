// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*tcstok_s.inl - general implementation of _tcstok_s
*

*
*Purpose:
*       This file contains the general algorithm for wcstok_s and its variants.
*
****/

_FUNC_PROLOGUE
_CHAR * __cdecl _FUNC_NAME(_CHAR *_String, const _CHAR *_Control, _CHAR **_Context)
{
    _CHAR *token;
    const _CHAR *ctl;

    /* validation section */
    _VALIDATE_POINTER_ERROR_RETURN(_Context, EINVAL, NULL);
    _VALIDATE_POINTER_ERROR_RETURN(_Control, EINVAL, NULL);
    _VALIDATE_CONDITION_ERROR_RETURN(_String != NULL || *_Context != NULL, EINVAL, NULL);

    /* If string==NULL, continue with previous string */
    if (!_String)
    {
        _String = *_Context;
    }

    /* Find beginning of token (skip over leading delimiters). Note that
    * there is no token iff this loop sets string to point to the terminal null. */
    for ( ; *_String != 0 ; _String++)
    {
        for (ctl = _Control; *ctl != 0 && *ctl != *_String; ctl++)
            ;
        if (*ctl == 0)
        {
            break;
        }
    }

    token = _String;

    /* Find the end of the token. If it is not the end of the string,
    * put a null there. */
    for ( ; *_String != 0 ; _String++)
    {
        for (ctl = _Control; *ctl != 0 && *ctl != *_String; ctl++)
            ;
        if (*ctl != 0)
        {
            *_String++ = 0;
            break;
        }
    }

    /* Update the context */
    *_Context = _String;

    /* Determine if a token has been found. */
    if (token == _String)
    {
        return NULL;
    }
    else
    {
        return token;
    }
}
