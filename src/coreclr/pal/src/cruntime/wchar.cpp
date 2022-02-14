// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++

Module Name:

    wchar.c

Abstract:

    Implementation of wide char string functions.

--*/

#include "pal/palinternal.h"
#include "pal/cruntime.h"
#include "pal/dbgmsg.h"

#include "pal/thread.hpp"
#include "pal/threadsusp.hpp"

#if HAVE_CONFIG_H
#include "config.h"
#endif

#include <wctype.h>
#include <errno.h>
#include <algorithm>

SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*--
Function:
  _wtoi

See MSDN doc
--*/
int
__cdecl
_wtoi(
    const wchar_16 *string)
{
    int len;
    int ret;
    char *tempStr;

    PERF_ENTRY(_wtoi);
    ENTRY("_wtoi (string=%p)\n", string);

    len = WideCharToMultiByte(CP_ACP, 0, string, -1, 0, 0, 0, 0);
    if (!len)
    {
        ASSERT("WideCharToMultiByte failed.  Error is %d\n",
              GetLastError());
        return -1;
    }
    tempStr = (char *) PAL_malloc(len);
    if (!tempStr)
    {
        ERROR("PAL_malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        return -1;
    }
    len = WideCharToMultiByte(CP_ACP, 0, string, -1, tempStr, len, 0, 0);
    if (!len)
    {
        ASSERT("WideCharToMultiByte failed.  Error is %d\n",
              GetLastError());
        PAL_free(tempStr);
        return -1;
    }
    ret = atoi(tempStr);

    PAL_free(tempStr);
    LOGEXIT("_wtoi returns int %d\n", ret);
    PERF_EXIT(_wtoi);
    return ret;
}


/*++
Function:
  _wcsnicmp

Compare characters of two strings without regard to case

Return Value

The return value indicates the relationship between the substrings as follows.

Return Value

Description

< 0        string1 substring less than string2 substring
  0        string1 substring identical to string2 substring
> 0        string1 substring greater than string2 substring

Parameters

string1, string2        Null-terminated strings to compare
count                   Number of characters to compare

Remarks

The _strnicmp function lexicographically compares, at most, the first
count characters of string1 and string2. The comparison is performed
without regard to case; _strnicmp is a case-insensitive version of
strncmp. The comparison ends if a terminating null character is
reached in either string before count characters are compared. If the
strings are equal when a terminating null character is reached in
either string before count characters are compared, the shorter string
is lesser.

--*/
int
__cdecl
_wcsnicmp(
          const wchar_16 *string1,
          const wchar_16 *string2,
          size_t count)
{
    size_t i;
    int diff = 0;

    PERF_ENTRY(_wcsnicmp);
    ENTRY("_wcsnicmp (string1=%p (%S), string2=%p (%S), count=%lu)\n",
          string1?string1:W16_NULLSTRING,
          string1?string1:W16_NULLSTRING, string2?string2:W16_NULLSTRING, string2?string2:W16_NULLSTRING,
         (unsigned long) count);

    for (i = 0; i < count; i++)
    {
        diff = towlower(string1[i]) - towlower(string2[i]);
        if (diff != 0 || 0 == string1[i] || 0 == string2[i])
        {
            break;
        }
    }
    LOGEXIT("_wcsnicmp returning int %d\n", diff);
    PERF_EXIT(_wcsnicmp);
    return diff;
}

/*++
Function:
  _wcsicmp

Compare characters of two strings without regard to case

Return Value

The return value indicates the relationship between the substrings as follows.

Return Value

Description

< 0        string1 substring less than string2 substring
  0        string1 substring identical to string2 substring
> 0        string1 substring greater than string2 substring

Parameters

string1, string2        Null-terminated strings to compare

--*/
int
__cdecl
_wcsicmp(
          const wchar_16 *string1,
          const wchar_16 *string2)
{
    int ret;

    PERF_ENTRY(_wcsicmp);
    ENTRY("_wcsicmp (string1=%p (%S), string2=%p (%S))\n",
          string1?string1:W16_NULLSTRING,
          string1?string1:W16_NULLSTRING, string2?string2:W16_NULLSTRING, string2?string2:W16_NULLSTRING);

    ret = _wcsnicmp(string1, string2, 0x7fffffff);

    LOGEXIT("_wcsnicmp returns int %d\n", ret);
    PERF_EXIT(_wcsicmp);
    return ret;
}

/*++
Function:
  PAL_wcstoul

Convert string to an unsigned long-integer value.

Return Value

wcstoul returns the converted value, if any, or UINT32_MAX on
overflow. It returns 0 if no conversion can be performed. errno is
set to ERANGE if overflow or underflow occurs.

Parameters

nptr    Null-terminated string to convert
endptr  Pointer to character that stops scan
base    Number base to use

Remarks

wcstoul stops reading the string nptr at the first character it cannot
recognize as part of a number. This may be the terminating null
character, or it may be the first numeric character greater than or
equal to base. The LC_NUMERIC category setting of the current locale
determines recognition of the radix character in nptr; for more
information, see setlocale. If endptr is not NULL, a pointer to the
character that stopped the scan is stored at the location pointed to
by endptr. If no conversion can be performed (no valid digits were
found or an invalid base was specified), the value of nptr is stored
at the location pointed to by endptr.

Notes :
    MSDN states that only space and tab are accepted as leading whitespace, but
    tests indicate that other whitespace characters (newline, carriage return,
    etc) are also accepted. This matches the behavior on Unix systems.

    For wcstol and wcstoul, we need to check if the value to be returned
    is outside the 32 bit range. If so, the returned value needs to be set
    as appropriate, according to the MSDN pages for wcstol and wcstoul,
    and in all instances errno must be set to ERANGE (The one exception
    is converting a string representing a negative value to unsigned long).
    Note that on 64 bit Windows, long's are still 32 bit. Thus, to match
    Windows behavior, we must return long's in the 32 bit range.
--*/

/* The use of ULONG is by design, to ensure that a 32 bit value is always
returned from this function. If "unsigned long" is used instead of ULONG,
then a 64 bit value could be returned on 64 bit platforms like HP-UX, thus
breaking Windows behavior .*/
ULONG
__cdecl
PAL_wcstoul(
        const wchar_16 *nptr,
        wchar_16 **endptr,
        int base)
{
    char *s_nptr = 0;
    char *s_endptr = 0;
    unsigned long res;
    int size;
    DWORD dwLastError = 0;

    PERF_ENTRY(wcstoul);
    ENTRY("wcstoul (nptr=%p (%S), endptr=%p, base=%d)\n", nptr?nptr:W16_NULLSTRING, nptr?nptr:W16_NULLSTRING,
          endptr, base);

    size = WideCharToMultiByte(CP_ACP, 0, nptr, -1, NULL, 0, NULL, NULL);
    if (!size)
    {
        dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failed.  Error is %d\n", dwLastError);
        SetLastError(ERROR_INVALID_PARAMETER);
        res = 0;
        goto PAL_wcstoulExit;
    }
    s_nptr = (char *)PAL_malloc(size);
    if (!s_nptr)
    {
        ERROR("PAL_malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        res = 0;
        goto PAL_wcstoulExit;
    }
    size = WideCharToMultiByte(CP_ACP, 0, nptr, -1, s_nptr, size, NULL, NULL);
    if (!size)
    {
        dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failed.  Error is %d\n", dwLastError);
        SetLastError(ERROR_INVALID_PARAMETER);
        res = 0;
        goto PAL_wcstoulExit;
    }

    res = strtoul(s_nptr, &s_endptr, base);

#ifdef HOST_64BIT
    if (res > UINT32_MAX)
    {
        wchar_16 wc = *nptr;
        while (iswspace(wc))
        {
            wc = *nptr++;
        }
        /* If the string represents a positive number that is greater than
           _UI32_MAX, set errno to ERANGE. Otherwise, don't set errno
           to match Windows behavior. */
        if (wc != '-')
        {
            res = UINT32_MAX;
            errno = ERANGE;
        }
    }
#endif

    /* only ASCII characters will be accepted by strtol, and those always get
       mapped to single-byte characters, so the first rejected character will
       have the same index in the multibyte and widechar strings */
    if( endptr )
    {
        size = s_endptr - s_nptr;
        *endptr = (wchar_16 *)&nptr[size];
    }

PAL_wcstoulExit:
    PAL_free(s_nptr);
    LOGEXIT("wcstoul returning unsigned long %lu\n", res);
    PERF_EXIT(wcstoul);

    /* When returning unsigned long res from this function, it will be
    implicitly cast to ULONG. This handles situations where a string that
    represents a negative number is passed in to wcstoul. The Windows
    behavior is analogous to taking the binary equivalent of the negative
    value and treating it as a positive number. Returning a ULONG from
    this function, as opposed to native unsigned long, allows us to match
    this behavior. The explicit case to ULONG below is used to silence any
    potential warnings due to the implicit casting.  */
    return (ULONG)res;
}

ULONGLONG
__cdecl
PAL__wcstoui64(
        const wchar_16 *nptr,
        wchar_16 **endptr,
        int base)
{
    char *s_nptr = 0;
    char *s_endptr = 0;
    unsigned long long res;
    int size;
    DWORD dwLastError = 0;

    PERF_ENTRY(wcstoul);
    ENTRY("_wcstoui64 (nptr=%p (%S), endptr=%p, base=%d)\n", nptr?nptr:W16_NULLSTRING, nptr?nptr:W16_NULLSTRING,
          endptr, base);

    size = WideCharToMultiByte(CP_ACP, 0, nptr, -1, NULL, 0, NULL, NULL);
    if (!size)
    {
        dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failed.  Error is %d\n", dwLastError);
        SetLastError(ERROR_INVALID_PARAMETER);
        res = 0;
        goto PAL__wcstoui64Exit;
    }
    s_nptr = (char *)PAL_malloc(size);
    if (!s_nptr)
    {
        ERROR("PAL_malloc failed\n");
        SetLastError(ERROR_NOT_ENOUGH_MEMORY);
        res = 0;
        goto PAL__wcstoui64Exit;
    }
    size = WideCharToMultiByte(CP_ACP, 0, nptr, -1, s_nptr, size, NULL, NULL);
    if (!size)
    {
        dwLastError = GetLastError();
        ASSERT("WideCharToMultiByte failed.  Error is %d\n", dwLastError);
        SetLastError(ERROR_INVALID_PARAMETER);
        res = 0;
        goto PAL__wcstoui64Exit;
    }

    res = strtoull(s_nptr, &s_endptr, base);

    /* only ASCII characters will be accepted by strtoull, and those always get
       mapped to single-byte characters, so the first rejected character will
       have the same index in the multibyte and widechar strings */
    if( endptr )
    {
        size = s_endptr - s_nptr;
        *endptr = (wchar_16 *)&nptr[size];
    }

PAL__wcstoui64Exit:
    PAL_free(s_nptr);
    LOGEXIT("_wcstoui64 returning unsigned long long %llu\n", res);
    PERF_EXIT(_wcstoui64);

    return res;
}

WCHAR * __cdecl PAL_wcsncat(WCHAR *, const WCHAR *, size_t);

/*++
Function:
  PAL_wcscat

See MSDN or the man page for mcscat.

--*/
wchar_16 *
__cdecl
PAL_wcscat(
        wchar_16 *strDestination,
        const wchar_16 *strSource)
{
    wchar_16 *ret;
    PERF_ENTRY(wcscat);
    ENTRY("wcscat (strDestination=%p (%S), strSource=%p (%S))\n",
          strDestination?strDestination:W16_NULLSTRING,
          strDestination?strDestination:W16_NULLSTRING, strSource?strSource:W16_NULLSTRING, strSource?strSource:W16_NULLSTRING);

    ret = PAL_wcsncat( strDestination, strSource, PAL_wcslen( strSource ) );

    LOGEXIT("wcscat returnng wchar_t %p (%S)\n", ret, ret);
    PERF_EXIT(wcscat);
    return ret;
}


/*++
Function:
  PAL_wcscpy

See MSDN or the man page for mcscpy.

--*/
wchar_16 *
__cdecl
PAL_wcscpy(
        wchar_16 *strDestination,
        const wchar_16 *strSource)
{
    wchar_16 *start = strDestination;

    PERF_ENTRY(wcscpy);
    ENTRY("wcscpy (strDestination=%p, strSource=%p (%S))\n",
          strDestination, strSource ? strSource:W16_NULLSTRING, strSource ? strSource:W16_NULLSTRING);

    if (strDestination == NULL)
    {
        ERROR("invalid strDestination argument\n");
        LOGEXIT("wcscpy returning wchar_t NULL\n");
        PERF_EXIT(wcscpy);
        return NULL;
    }

    if (strSource == NULL)
    {
        ERROR("invalid strSource argument\n");
        LOGEXIT("wcscpy returning wchar_t NULL\n");
        PERF_EXIT(wcscpy);
        return NULL;
    }

    /* copy source string to destination string */
    while(*strSource)
    {
        *strDestination++ = *strSource++;
    }

    /* add terminating null */
    *strDestination = '\0';

    LOGEXIT("wcscpy returning wchar_t %p (%S)\n", start, start);
    PERF_EXIT(wcscpy);
    return start;
}


/*++
Function:
  PAL_wcslen

See MSDN or the man page for wcslen.

--*/
size_t
__cdecl
PAL_wcslen(
        const wchar_16 *string)
{
    size_t nChar = 0;

    PERF_ENTRY(wcslen);
    ENTRY("wcslen (string=%p (%S))\n", string?string:W16_NULLSTRING, string?string:W16_NULLSTRING);

    if ( !string )
    {
        LOGEXIT("wcslen returning size_t %u\n", 0);
        PERF_EXIT(wcslen);
        return 0;
    }
    while (*string++)
    {
        nChar++;
    }

    LOGEXIT("wcslen returning size_t %u\n", nChar);
    PERF_EXIT(wcslen);
    return nChar;
}


/*++
Function:
  PAL_wcsncmp

See MSDN or the man page for wcsncmp.
--*/
int
__cdecl
PAL_wcsncmp(
        const wchar_16 *string1,
        const wchar_16 *string2,
        size_t count)
{
    size_t i;
    int diff = 0;

    PERF_ENTRY(wcsncmp);
    ENTRY("wcsncmp (string1=%p (%S), string2=%p (%S) count=%lu)\n",
          string1?string1:W16_NULLSTRING,
          string1?string1:W16_NULLSTRING, string2?string2:W16_NULLSTRING, string2?string2:W16_NULLSTRING,
          (unsigned long) count);

    for (i = 0; i < count; i++)
    {
        diff = string1[i] - string2[i];
        if (diff != 0)
        {
            break;
        }

        /* stop if we reach the end of the string */
        if(string1[i]==0)
        {
            break;
        }
    }
    LOGEXIT("wcsncmp returning int %d\n", diff);
    PERF_EXIT(wcsncmp);
    return diff;
}

/*++
Function:
  PAL_wcscmp

See MSDN or the man page for wcscmp.
--*/
int
__cdecl
PAL_wcscmp(
        const wchar_16 *string1,
        const wchar_16 *string2)
{
    int ret;

    PERF_ENTRY(wcscmp);
    ENTRY("wcscmp (string1=%p (%S), string2=%p (%S))\n",
          string1?string1:W16_NULLSTRING,
          string1?string1:W16_NULLSTRING, string2?string2:W16_NULLSTRING, string2?string2:W16_NULLSTRING);

    ret = PAL_wcsncmp(string1, string2, 0x7fffffff);

    LOGEXIT("wcscmp returns int %d\n", ret);
    PERF_EXIT(wcscmp);
    return ret;
}

/*++
Function:
  PAL_wcschr

See MSDN or man page for wcschr.

--*/
wchar_16 _WConst_return *
__cdecl
PAL_wcschr(
        const wchar_16 * string,
        wchar_16 c)
{
    PERF_ENTRY(wcschr);
    ENTRY("wcschr (string=%p (%S), c=%C)\n", string?string:W16_NULLSTRING, string?string:W16_NULLSTRING, c);

    while (*string)
    {
        if (*string == c)
        {
            LOGEXIT("wcschr returning wchar_t %p (%S)\n", string, string);
            PERF_EXIT(wcschr);
            return (wchar_16 *) string;
        }
        string++;
    }

    // Check if the comparand was \000
    if (*string == c)
        return (wchar_16 *) string;

    LOGEXIT("wcschr returning wchar_t NULL\n");
    PERF_EXIT(wcschr);
    return NULL;
}


/*++
Function:
  PAL_wcsrchr

See MSDN or man page for wcsrchr.

--*/
wchar_16 _WConst_return *
__cdecl
PAL_wcsrchr(
        const wchar_16 * string,
        wchar_16 c)
{
    wchar_16 *last = NULL;

    PERF_ENTRY(wcsrchr);
    ENTRY("wcsrchr (string=%p (%S), c=%C)\n", string?string:W16_NULLSTRING, string?string:W16_NULLSTRING, c);

    while (*string)
    {
        if (*string == c)
        {
            last = (wchar_16 *) string;
        }
        string++;
    }

    LOGEXIT("wcsrchr returning wchar_t %p (%S)\n", last?last:W16_NULLSTRING, last?last:W16_NULLSTRING);
    PERF_EXIT(wcsrchr);
    return (wchar_16 *)last;
}


/*++
Function:
  PAL_wcspbrk

See MSDN or man page for wcspbrk.
--*/
const wchar_16 *
__cdecl
PAL_wcspbrk(
        const wchar_16 *string,
        const wchar_16 *strCharSet)
{
    PERF_ENTRY(wcspbrk);
    ENTRY("wcspbrk (string=%p (%S), strCharSet=%p (%S))\n",
          string?string:W16_NULLSTRING,
          string?string:W16_NULLSTRING, strCharSet?strCharSet:W16_NULLSTRING, strCharSet?strCharSet:W16_NULLSTRING);

    while (*string)
    {
        if (PAL_wcschr(strCharSet, *string) != NULL)
        {
            LOGEXIT("wcspbrk returning wchar_t %p (%S)\n", string, string);
            PERF_EXIT(wcspbrk);
            return (wchar_16 *) string;
        }

        string++;
    }

    LOGEXIT("wcspbrk returning wchar_t NULL\n");
    PERF_EXIT(wcspbrk);
    return NULL;
}


/*++
Function:
  PAL_wcsstr

See MSDN or man page for wcsstr.
--*/
const wchar_16 *
__cdecl
PAL_wcsstr(
        const wchar_16 *string,
        const wchar_16 *strCharSet)
{
    wchar_16 *ret = NULL;
    int i;

    PERF_ENTRY(wcsstr);
    ENTRY("wcsstr (string=%p (%S), strCharSet=%p (%S))\n",
      string?string:W16_NULLSTRING,
      string?string:W16_NULLSTRING, strCharSet?strCharSet:W16_NULLSTRING, strCharSet?strCharSet:W16_NULLSTRING);

    if (string == NULL)
    {
        ret = NULL;
        goto leave;
    }

    if (strCharSet == NULL)
    {
        ret = NULL;
        goto leave;
    }

    if (*strCharSet == 0)
    {
        ret = (wchar_16 *)string;
        goto leave;
    }

    while (*string != 0)
    {
        i = 0;
        while (1)
        {
            if (*(strCharSet + i) == 0)
            {
                ret = (wchar_16 *) string;
                goto leave;
            }
            else if (*(string + i) == 0)
            {
                ret = NULL;
                goto leave;
            }
            else if (*(string + i) != *(strCharSet + i))
            {
                break;
            }

            i++;
        }
        string++;
    }

 leave:
    LOGEXIT("wcsstr returning wchar_t %p (%S)\n", ret?ret:W16_NULLSTRING, ret?ret:W16_NULLSTRING);
    PERF_EXIT(wcsstr);
    return ret;
}

/*++
Function :

    PAL_wcsncpy

see msdn doc.
--*/
wchar_16 *
__cdecl
PAL_wcsncpy( wchar_16 * strDest, const wchar_16 *strSource, size_t count )
{
    UINT length = sizeof( wchar_16 ) * count;
    PERF_ENTRY(wcsncpy);
    ENTRY("wcsncpy( strDest:%p, strSource:%p (%S), count:%lu)\n",
          strDest, strSource, strSource, (unsigned long) count);

    memset( strDest, 0, length );
    length = std::min( count, PAL_wcslen( strSource ) ) * sizeof( wchar_16 );
    memcpy( strDest, strSource, length );

    LOGEXIT("wcsncpy returning (wchar_16*): %p\n", strDest);
    PERF_EXIT(wcsncpy);
    return strDest;
}

/*++
Function :

    wcsncat

see msdn doc.
--*/
wchar_16 *
__cdecl
PAL_wcsncat( wchar_16 * strDest, const wchar_16 *strSource, size_t count )
{
    wchar_16 *start = strDest;
    UINT LoopCount = 0;
    UINT StrSourceLength = 0;

    PERF_ENTRY(wcsncat);
    ENTRY( "wcsncat (strDestination=%p (%S), strSource=%p (%S), count=%lu )\n",
            strDest ? strDest : W16_NULLSTRING,
            strDest ? strDest : W16_NULLSTRING,
            strSource ? strSource : W16_NULLSTRING,
            strSource ? strSource : W16_NULLSTRING, (unsigned long) count);

    if ( strDest == NULL )
    {
        ERROR("invalid strDest argument\n");
        LOGEXIT("wcsncat returning wchar_t NULL\n");
        PERF_EXIT(wcsncat);
        return NULL;
    }

    if ( strSource == NULL )
    {
        ERROR("invalid strSource argument\n");
        LOGEXIT("wcsncat returning wchar_t NULL\n");
        PERF_EXIT(wcsncat);
        return NULL;
    }

    /* find end of source string */
    while ( *strDest )
    {
        strDest++;
    }

    StrSourceLength = PAL_wcslen( strSource );
    if ( StrSourceLength < count )
    {
        count = StrSourceLength;
    }

    /* concatenate new string */
    while( *strSource && LoopCount < count )
    {
      *strDest++ = *strSource++;
      LoopCount++;
    }

    /* add terminating null */
    *strDest = '\0';

    LOGEXIT("wcsncat returning wchar_t %p (%S)\n", start, start);
    PERF_EXIT(wcsncat);
    return start;
}

static BOOL MISC_CRT_WCSTOD_IsValidCharacter( WCHAR c )
{
    if ( c == '+' || c == '-' || c == '.' || ( c >= '0' && c <= '9' ) ||
         c == 'e' || c == 'E' || c == 'd' || c == 'D' )
    {
        return TRUE;
    }
    return FALSE;
}

/*++
Function :

    wcstod

    There is a slight difference between the Windows version of wcstod
    and the BSD versio of wcstod.

    Under Windows the string "  -1b  " returns -1.000000 stop char = 'b'
    Under BSD the same string returns 0.000000 stop ' '

see msdn doc.
--*/
double
__cdecl
PAL_wcstod( const wchar_16 * nptr, wchar_16 **endptr )
{
    double RetVal = 0.0;
    LPSTR  lpStringRep = NULL;
    LPWSTR lpStartOfExpression = (LPWSTR)nptr;
    LPWSTR lpEndOfExpression = NULL;
    UINT Length = 0;

    PERF_ENTRY(wcstod);
    ENTRY( "wcstod( %p (%S), %p (%S) )\n", nptr, nptr, endptr , endptr );

    if ( !nptr )
    {
        ERROR( "nptr is invalid.\n" );
        LOGEXIT( "wcstod returning 0.0\n" );
        PERF_EXIT(wcstod);
        return 0.0;
    }

    /* Eat white space. */
    while ( iswspace( *lpStartOfExpression ) )
    {
        lpStartOfExpression++;
    }

    /* Get the end of the expression. */
    lpEndOfExpression = lpStartOfExpression;
    while ( *lpEndOfExpression )
    {
        if ( !MISC_CRT_WCSTOD_IsValidCharacter( *lpEndOfExpression ) )
        {
            break;
        }
        lpEndOfExpression++;
    }

    if ( lpEndOfExpression != lpStartOfExpression )
    {
        Length = lpEndOfExpression - lpStartOfExpression;
        lpStringRep = (LPSTR)PAL_malloc( Length + 1);

        if ( lpStringRep )
        {
            if ( WideCharToMultiByte( CP_ACP, 0, lpStartOfExpression, Length,
                                      lpStringRep, Length + 1 ,
                                      NULL, 0 ) != 0 )
            {
                LPSTR ScanStop = NULL;
                lpStringRep[Length]= 0;
                RetVal = strtod( lpStringRep, &ScanStop );

                /* See if strtod failed. */
                if ( RetVal == 0.0 && ScanStop == lpStringRep )
                {
                    ASSERT( "An error occurred in the conversion.\n" );
                    lpEndOfExpression = (LPWSTR)nptr;
                }
            }
            else
            {
                ASSERT( "Wide char to multibyte conversion failed.\n" );
                lpEndOfExpression = (LPWSTR)nptr;
            }
        }
        else
        {
            ERROR( "Not enough memory.\n" );
            lpEndOfExpression = (LPWSTR)nptr;
        }
    }
    else
    {
        ERROR( "Malformed expression.\n" );
        lpEndOfExpression = (LPWSTR)nptr;
    }

    /* Set the stop scan character. */
    if ( endptr != NULL )
    {
        *endptr = lpEndOfExpression;
    }

    PAL_free( lpStringRep );
    LOGEXIT( "wcstod returning %f.\n", RetVal );
    PERF_EXIT(wcstod);
    return RetVal;
}
