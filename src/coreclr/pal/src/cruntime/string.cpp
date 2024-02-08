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
