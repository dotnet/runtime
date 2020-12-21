// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    string.cpp

Abstract:

    Implementation of the string functions in the C runtime library that are Windows specific.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/cruntime.h"

#include <string.h>
#include <ctype.h>
#include <pthread.h>
#include <errno.h>
#include <limits.h>
#include <unistd.h>


SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*++
Function:
  _strnicmp

compare at most count characters from two strings, ignoring case

The strnicmp() function compares, with case insensitivity, at most count
characters from s1 to s2. All uppercase characters from s1 and s2 are
mapped to lowercase for the purposes of doing the comparison.

Returns:

Value Meaning

< 0   s1 is less than s2
0     s1 is equal to s2
> 0   s1 is greater than s2

--*/
int
__cdecl
_strnicmp( const char *s1, const char *s2, size_t count )
{
    int ret;

    PERF_ENTRY(_strnicmp);
    ENTRY("_strnicmp (s1=%p (%s), s2=%p (%s), count=%d)\n", s1?s1:"NULL", s1?s1:"NULL", s2?s2:"NULL", s2?s2:"NULL", count);

    ret = strncasecmp(s1, s2, count );

    LOGEXIT("_strnicmp returning int %d\n", ret);
    PERF_EXIT(_strnicmp);
    return ret;
}

/*++
Function:
  _stricmp

compare two strings, ignoring case

The stricmp() function compares, with case insensitivity, the string
pointed to by s1 to the string pointed to by s2. All uppercase
characters from s1 and s2 are mapped to lowercase for the purposes of
doing the comparison.

Returns:

Value Meaning

< 0   s1 is less than s2
0     s1 is equal to s2
> 0   s1 is greater than s2

--*/
int
__cdecl
_stricmp(
         const char *s1,
         const char *s2)
{
    int ret;

    PERF_ENTRY(_stricmp);
    ENTRY("_stricmp (s1=%p (%s), s2=%p (%s))\n", s1?s1:"NULL", s1?s1:"NULL", s2?s2:"NULL", s2?s2:"NULL");

    ret = strcasecmp(s1, s2);

    LOGEXIT("_stricmp returning int %d\n", ret);
    PERF_EXIT(_stricmp);
    return ret;
}


/*++
Function:
  PAL_strtoul

Convert string to an unsigned long-integer value.

Return Value

strtoul returns the converted value, if any, or UINT32_MAX on
overflow. It returns 0 if no conversion can be performed. errno is
set to ERANGE if overflow or underflow occurs.

Parameters

szNumber  Null-terminated string to convert to a ULONG
pszEnd          Pointer to character that stops scan
nBase           Number base to use

Remarks

strtoul stops reading the string szNumber at the first character it cannot
recognize as part of a number. This may be the terminating null
character, or it may be the first numeric character greater than or
equal to base. The LC_NUMERIC category setting of the current locale
determines recognition of the radix character in szNumber; for more
information, see setlocale. If pszEnd is not NULL, a pointer to the
character that stopped the scan is stored at the location pointed to
by pszEnd. If no conversion can be performed (no valid digits were
found or an invalid base was specified), the value of szNumber is stored
at the location pointed to by pszEnd.

Notes :
    MSDN states that only space and tab are accepted as leading whitespace, but
    tests indicate that other whitespace characters (newline, carriage return,
    etc) are also accepted. This matches the behavior on Unix systems.

    For strtoul, we need to check if the value to be returned
    is outside the 32 bit range. If so, the returned value needs to be set
    as appropriate, according to the MSDN pages and in all instances errno
    must be set to ERANGE (The one exception is converting a string
    representing a negative value to unsigned long).
    Note that on 64 bit Windows, long's are still 32 bit. Thus, to match
    Windows behavior, we must return long's in the 32 bit range.
    --*/

/* The use of ULONG is by design, to ensure that a 32 bit value is always
returned from this function. If "unsigned long" is used instead of ULONG,
then a 64 bit value could be returned on 64 bit platforms like HP-UX, thus
breaking Windows behavior. */
ULONG
__cdecl
PAL_strtoul(const char *szNumber, char **pszEnd, int nBase)
{
    unsigned long ulResult;

    PERF_ENTRY(strtoul);
    ENTRY("strtoul (szNumber=%p (%s), pszEnd=%p, nBase=%d)\n",
        szNumber?szNumber:"NULL",
        szNumber?szNumber:"NULL",
        pszEnd,
        nBase);

    ulResult = strtoul(szNumber, pszEnd, nBase);

#ifdef HOST_64BIT
    if (ulResult > UINT32_MAX)
    {
        char ch = *szNumber;
        while (isspace(ch))
        {
	    ch = *szNumber++;
        }
        /* If the string represents a positive number that is greater than
            _UI32_MAX, set errno to ERANGE. Otherwise, don't set errno
            to match Windows behavior. */
        if (ch != '-')
        {
            ulResult = UINT32_MAX;
            errno = ERANGE;
        }
    }
#endif

    LOGEXIT("strtoul returning unsigned long %lu\n", ulResult);
    PERF_EXIT(wcstoul);

    /* When returning unsigned long res from this function, it will be
        implicitly cast to ULONG. This handles situations where a string that
        represents a negative number is passed in to strtoul. The Windows
        behavior is analogous to taking the binary equivalent of the negative
        value and treating it as a positive number. Returning a ULONG from
        this function, as opposed to native unsigned long, allows us to match
        this behavior. The explicit cast to ULONG below is used to silence any
        potential warnings due to the implicit casting.  */
    return (ULONG)ulResult;

}

/*++
Function:
  PAL_strtoull

Convert string to an unsigned long long-integer value.

Return Value

strtoull returns the converted value, if any, or UINT64_MAX on
overflow. It returns 0 if no conversion can be performed. errno is
set to ERANGE if overflow or underflow occurs.

Parameters

szNumber  Null-terminated string to convert to a ULONGLONG
pszEnd          Pointer to character that stops scan
nBase           Number base to use

Remarks

strtoull stops reading the string szNumber at the first character it cannot
recognize as part of a number. This may be the terminating null
character, or it may be the first numeric character greater than or
equal to base. The LC_NUMERIC category setting of the current locale
determines recognition of the radix character in szNumber; for more
information, see setlocale. If pszEnd is not NULL, a pointer to the
character that stopped the scan is stored at the location pointed to
by pszEnd. If no conversion can be performed (no valid digits were
found or an invalid base was specified), the value of szNumber is stored
at the location pointed to by pszEnd.

Notes :
    MSDN states that only space and tab are accepted as leading whitespace, but
    tests indicate that other whitespace characters (newline, carriage return,
    etc) are also accepted. This matches the behavior on Unix systems.

    For strtoull, we need to check if the value to be returned
    is outside the 64 bit range. If so, the returned value needs to be set
    as appropriate, according to the MSDN pages and in all instances errno
    must be set to ERANGE (The one exception is converting a string
    representing a negative value to unsigned long).
    --*/

/* The use of ULONGLONG is by design, to ensure that a 64 bit value is always
returned from this function. */
ULONGLONG
__cdecl
PAL_strtoull(const char *szNumber, char **pszEnd, int nBase)
{
    unsigned long long ullResult;

    PERF_ENTRY(strtoull);
    ENTRY("strtoull (szNumber=%p (%s), pszEnd=%p, nBase=%d)\n",
        szNumber?szNumber:"NULL",
        szNumber?szNumber:"NULL",
        pszEnd,
        nBase);

    ullResult = strtoull(szNumber, pszEnd, nBase);

    if (ullResult > UINT64_MAX)
    {
        char ch = *szNumber;
        while (isspace(ch))
        {
            ch = *szNumber++;
        }
        /* If the string represents a positive number that is greater than
           UINT64_MAX, set errno to ERANGE. Otherwise, don't set errno
           to match Windows behavior. */
        if (ch != '-')
        {
            ullResult = UINT64_MAX;
            errno = ERANGE;
        }
    }

    LOGEXIT("strtoull returning unsigned long long %llu\n", ullResult);
    PERF_EXIT(strtoull);

    /* When returning unsigned long long res from this function, it will be
        implicitly cast to ULONGLONG. This handles situations where a string that
        represents a negative number is passed in to strtoull. The Windows
        behavior is analogous to taking the binary equivalent of the negative
        value and treating it as a positive number. Returning a ULONGLONG from
        this function, as opposed to native unsigned long long, allows us to match
        this behavior. The explicit cast to ULONGLONG below is used to silence any
        potential warnings due to the implicit casting.  */
    return (ULONGLONG)ullResult;
}
