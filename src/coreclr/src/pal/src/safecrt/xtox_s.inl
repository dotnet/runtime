// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/***
*xtoa.c - convert integers/longs to ASCII string
*

*
*Purpose:
*       The module has code to convert integers/longs to ASCII strings.  See
*
*******************************************************************************/

#ifdef _UNICODE
#define xtox_s     xtow_s
#define _itox_s    _itow_s
#define _ltox_s    _ltow_s
#define _ultox_s   _ultow_s
#define _i64tox_s  _i64tow_s
#define xtox       xtow
#define _ltox      _ltow
#define _ultox     _ultow
#else  /* _UNICODE */
#define xtox_s     xtoa_s
#define _itox_s    _itoa_s
#define _ltox_s    _ltoa_s
#define _ultox_s   _ultoa_s
#define _i64tox_s  _i64toa_s
#define xtox       xtoa
#define _ltox      _ltoa
#define _ultox     _ultoa
#endif  /* _UNICODE */

/***
*char *_itoa_s, *_ltoa_s, *_ultoa_s(val, buf, sizeInTChars, radix) - convert binary int to ASCII
*       string
*
*Purpose:
*       Converts an int to a character string.
*
*Entry:
*       val - number to be converted (int, long or unsigned long)
*       char *buf - ptr to buffer to place result
*       size_t sizeInTChars - size of the destination buffer
*       int radix - base to convert into
*
*Exit:
*       Fills in space pointed to by buf with string result.
*       Returns the errno_t: err != 0 means that something went wrong, and
*       an empty string (buf[0] = 0) is returned.
*
*Exceptions:
*           Input parameters and buffer length are validated.
*       Refer to the validation section of the function.
*
*******************************************************************************/

/* helper routine that does the main job. */
#ifdef _SECURE_ITOA
static errno_t __stdcall xtox_s
        (
        unsigned long val,
        TCHAR *buf,
        size_t sizeInTChars,
        unsigned radix,
        int is_neg
        )
#else  /* _SECURE_ITOA */
static void __stdcall xtox
        (
        unsigned long val,
        TCHAR *buf,
        unsigned radix,
        int is_neg
        )
#endif  /* _SECURE_ITOA */
{
        TCHAR *p;                /* pointer to traverse string */
        TCHAR *firstdig;         /* pointer to first digit */
        TCHAR temp;              /* temp char */
        unsigned digval;         /* value of digit */
#ifdef _SECURE_ITOA
        size_t length;           /* current length of the string */

        /* validation section */
        _VALIDATE_RETURN_ERRCODE(buf != NULL, EINVAL);
        _VALIDATE_RETURN_ERRCODE(sizeInTChars > 0, EINVAL);
        _RESET_STRING(buf, sizeInTChars);
        _VALIDATE_RETURN_ERRCODE(sizeInTChars > (size_t)(is_neg ? 2 : 1), ERANGE);
        _VALIDATE_RETURN_ERRCODE(2 <= radix && radix <= 36, EINVAL);
        length = 0;

#endif  /* _SECURE_ITOA */
        p = buf;

        if (is_neg) {
            /* negative, so output '-' and negate */
            *p++ = _T('-');
#ifdef _SECURE_ITOA
            length++;
#endif  /* _SECURE_ITOA */
            val = (unsigned long)(-(long)val);
        }

        firstdig = p;           /* save pointer to first digit */

        do {
            digval = (unsigned) (val % radix);
            val /= radix;       /* get next digit */

            /* convert to ascii and store */
            if (digval > 9)
                *p++ = (TCHAR) (digval - 10 + _T('a'));  /* a letter */
            else
                *p++ = (TCHAR) (digval + _T('0'));       /* a digit */
#ifndef _SECURE_ITOA
        } while (val > 0);
#else  /* _SECURE_ITOA */
            length++;
        } while (val > 0 && length < sizeInTChars);

        /* Check for buffer overrun */
        if (length >= sizeInTChars)
        {
            buf[0] = '\0';
            _VALIDATE_RETURN_ERRCODE(length < sizeInTChars, ERANGE);
        }
#endif  /* _SECURE_ITOA */
        /* We now have the digit of the number in the buffer, but in reverse
           order.  Thus we reverse them now. */

        *p-- = _T('\0');            /* terminate string; p points to last digit */

        do {
            temp = *p;
            *p = *firstdig;
            *firstdig = temp;   /* swap *p and *firstdig */
            --p;
            ++firstdig;         /* advance to next two digits */
        } while (firstdig < p); /* repeat until halfway */
#ifdef _SECURE_ITOA
        return 0;
#endif  /* _SECURE_ITOA */
}

/* Actual functions just call conversion helper with neg flag set correctly,
   and return pointer to buffer. */

#ifdef _SECURE_ITOA
DLLEXPORT errno_t __cdecl _itox_s (
        int val,
        TCHAR *buf,
        size_t sizeInTChars,
        int radix
        )
{
        errno_t e = 0;

        if (radix == 10 && val < 0)
            e = xtox_s((unsigned long)val, buf, sizeInTChars, radix, 1);
        else
            e = xtox_s((unsigned long)(unsigned int)val, buf, sizeInTChars, radix, 0);

        return e;
}

errno_t __cdecl _ltox_s (
        long val,
        TCHAR *buf,
        size_t sizeInTChars,
        int radix
        )
{
        return xtox_s((unsigned long)val, buf, sizeInTChars, radix, (radix == 10 && val < 0));
}

errno_t __cdecl _ultox_s (
        unsigned long val,
        TCHAR *buf,
        size_t sizeInTChars,
        int radix
        )
{
        return xtox_s(val, buf, sizeInTChars, radix, 0);
}

#else  /* _SECURE_ITOA */

/***
*char *_itoa, *_ltoa, *_ultoa(val, buf, radix) - convert binary int to ASCII
*       string
*
*Purpose:
*       Converts an int to a character string.
*
*Entry:
*       val - number to be converted (int, long or unsigned long)
*       int radix - base to convert into
*       char *buf - ptr to buffer to place result
*
*Exit:
*       fills in space pointed to by buf with string result
*       returns a pointer to this buffer
*
*Exceptions:
*           Input parameters are validated. The buffer is assumed to be big enough to
*       contain the string. Refer to the validation section of the function.
*
*******************************************************************************/

/* Actual functions just call conversion helper with neg flag set correctly,
   and return pointer to buffer. */

TCHAR * __cdecl _ltox (
        long val,
        TCHAR *buf,
        int radix
        )
{
        xtox((unsigned long)val, buf, radix, (radix == 10 && val < 0));
        return buf;
}

TCHAR * __cdecl _ultox (
        unsigned long val,
        TCHAR *buf,
        int radix
        )
{
        xtox(val, buf, radix, 0);
        return buf;
}

#endif  /* _SECURE_ITOA */

#ifndef _NO_INT64

/***
*char *_i64toa_s(val, buf, sizeInTChars, radix) - convert binary int to ASCII
*       string
*
*Purpose:
*       Converts an int64 to a character string.
*
*Entry:
*       val - number to be converted
*       char *buf - ptr to buffer to place result
*       size_t sizeInTChars - size of the destination buffer
*       int radix - base to convert into
*
*Exit:
*       Fills in space pointed to by buf with string result.
*       Returns the errno_t: err != 0 means that something went wrong, and
*       an empty string (buf[0] = 0) is returned.
*
*Exceptions:
*       Input parameters and buffer length are validated.
*       Refer to the validation section of the function.
*
*******************************************************************************/

#ifdef _SECURE_ITOA
static errno_t __fastcall x64tox_s
        (/* stdcall is faster and smaller... Might as well use it for the helper. */
        unsigned __int64 val,
        TCHAR *buf,
        size_t sizeInTChars,
        unsigned radix,
        int is_neg
        )
#else  /* _SECURE_ITOA */
static void __fastcall x64tox
        (/* stdcall is faster and smaller... Might as well use it for the helper. */
        unsigned __int64 val,
        TCHAR *buf,
        unsigned radix,
        int is_neg
        )
#endif  /* _SECURE_ITOA */
{
        TCHAR *p;                /* pointer to traverse string */
        TCHAR *firstdig;         /* pointer to first digit */
        TCHAR temp;              /* temp char */
        unsigned digval;         /* value of digit */
#ifdef _SECURE_ITOA
        size_t length;           /* current length of the string */

        /* validation section */
        _VALIDATE_RETURN_ERRCODE(buf != NULL, EINVAL);
        _VALIDATE_RETURN_ERRCODE(sizeInTChars > 0, EINVAL);
        _RESET_STRING(buf, sizeInTChars);
        _VALIDATE_RETURN_ERRCODE(sizeInTChars > (size_t)(is_neg ? 2 : 1), ERANGE);
        _VALIDATE_RETURN_ERRCODE(2 <= radix && radix <= 36, EINVAL);
        length = 0;
#endif  /* _SECURE_ITOA */
        p = buf;

        if ( is_neg )
        {
            *p++ = _T('-');         /* negative, so output '-' and negate */
#ifdef _SECURE_ITOA
            length++;
#endif  /* _SECURE_ITOA */
            val = (unsigned __int64)(-(__int64)val);
        }

        firstdig = p;           /* save pointer to first digit */

        do {
            digval = (unsigned) (val % radix);
            val /= radix;       /* get next digit */

            /* convert to ascii and store */
            if (digval > 9)
                *p++ = (TCHAR) (digval - 10 + _T('a'));  /* a letter */
            else
                *p++ = (TCHAR) (digval + _T('0'));       /* a digit */

#ifndef _SECURE_ITOA
        } while (val > 0);
#else  /* _SECURE_ITOA */
            length++;
        } while (val > 0 && length < sizeInTChars);

        /* Check for buffer overrun */
        if (length >= sizeInTChars)
        {
            buf[0] = '\0';
            _VALIDATE_RETURN_ERRCODE(length < sizeInTChars, ERANGE);
        }
#endif  /* _SECURE_ITOA */
        /* We now have the digit of the number in the buffer, but in reverse
           order.  Thus we reverse them now. */

        *p-- = _T('\0');            /* terminate string; p points to last digit */

        do {
            temp = *p;
            *p = *firstdig;
            *firstdig = temp;   /* swap *p and *firstdig */
            --p;
            ++firstdig;         /* advance to next two digits */
        } while (firstdig < p); /* repeat until halfway */

#ifdef _SECURE_ITOA
        return 0;
#endif  /* _SECURE_ITOA */
}

#ifdef _SECURE_ITOA

/* Actual functions just call conversion helper with neg flag set correctly,
   and return pointer to buffer. */

DLLEXPORT errno_t __cdecl _i64tox_s (
        long long val,
        TCHAR *buf,
        size_t sizeInTChars,
        int radix
        )
{
        return x64tox_s((unsigned __int64)val, buf, sizeInTChars, radix, (radix == 10 && val < 0));
}

errno_t __cdecl _ui64tox_s (
        unsigned long long val,
        TCHAR *buf,
        size_t sizeInTChars,
        int radix
        )
{
        return x64tox_s(val, buf, sizeInTChars, radix, 0);
}

#else  /* _SECURE_ITOA */

/***
*char *_i64toa(val, buf, radix) - convert binary int to ASCII
*       string
*
*Purpose:
*       Converts an int64 to a character string.
*
*Entry:
*       val - number to be converted
*       int radix - base to convert into
*       char *buf - ptr to buffer to place result
*
*Exit:
*       fills in space pointed to by buf with string result
*       returns a pointer to this buffer
*
*Exceptions:
*           Input parameters are validated. The buffer is assumed to be big enough to
*       contain the string. Refer to the validation section of the function.
*
*******************************************************************************/

/* Actual functions just call conversion helper with neg flag set correctly,
   and return pointer to buffer. */

TCHAR * __cdecl _ui64tox (
        unsigned __int64 val,
        TCHAR *buf,
        int radix
        )
{
        x64tox(val, buf, radix, 0);
        return buf;
}

#endif  /* _SECURE_ITOA */

#endif  /* _NO_INT64 */
