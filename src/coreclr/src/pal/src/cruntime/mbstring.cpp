// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    mbstring.c

Abstract:

    Implementation of the multi-byte string functions in the C runtime library that
    are Windows specific.

Implementation Notes:

    Assuming it is not possible to change multi-byte code page using
    the PAL (_setmbcp does not seem to be required), these functions
    should have a trivial implementation (treat as single-byte). If it
    is possible, then support for multi-byte code pages will have to
    be implemented before these functions can behave correctly for
    multi-byte strings.



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"

#include <algorithm>

SET_DEFAULT_DEBUG_CHANNEL(CRT);


/*++
Function:
  _mbsinc

Return Value

Returns a pointer to the character that immediately follows string.

Parameter

string  Character pointer

Remarks

The _mbsinc function returns a pointer to the first byte of the
multibyte character that immediately follows string.

--*/
unsigned char *
__cdecl
_mbsinc(
        const unsigned char *string)
{
    unsigned char *ret;

    PERF_ENTRY(_mbsinc);
    ENTRY("_mbsinc (string=%p)\n", string);

    if (string == NULL)
    {
        ret = NULL;
    }
    else
    {
        ret = (unsigned char *) string;
        if (IsDBCSLeadByteEx(CP_ACP, *ret))
        {
            ++ret;
        }
        ++ret;
    }

    LOGEXIT("_mbsinc returning unsigned char* %p (%s)\n", ret, ret);
    PERF_EXIT(_mbsinc);
    return ret;
}


/*++
Function:
  _mbsninc

Return Value

Returns a pointer to string after string has been incremented by count
characters, or NULL if the supplied pointer is NULL. If count is
greater than or equal to the number of characters in string, the
result is undefined.

Parameters

string  Source string
count   Number of characters to increment string pointer

Remarks

The _mbsninc function increments string by count multibyte
characters. _mbsninc recognizes multibyte-character sequences
according to the multibyte code page currently in use.

--*/
unsigned char *
__cdecl
_mbsninc(
         const unsigned char *string, size_t count)
{
    unsigned char *ret;
    CPINFO cpinfo;

    PERF_ENTRY(_mbsninc);
    ENTRY("_mbsninc (string=%p, count=%lu)\n", string, count);
    if (string == NULL)
    {
        ret = NULL;
    }
    else
    {
        ret = (unsigned char *) string;
        if (GetCPInfo(CP_ACP, &cpinfo) && cpinfo.MaxCharSize == 1)
        {
            ret += std::min(count, strlen((const char*)string));
        }
        else
        {
            while (count-- && (*ret != 0))
            {
                if (IsDBCSLeadByteEx(CP_ACP, *ret))
                {
                    ++ret;
                }
                ++ret;
            }
        }
    }
    LOGEXIT("_mbsninc returning unsigned char* %p (%s)\n", ret, ret);
    PERF_EXIT(_mbsninc);
    return ret;
}

/*++
Function:
  _mbsdec

Return Value

_mbsdec returns a pointer to the character that immediately precedes
current; _mbsdec returns NULL if the value of start is greater than or
equal to that of current.

Parameters

start    Pointer to first byte of any multibyte character in the source
         string; start must precede current in the source string

current  Pointer to first byte of any multibyte character in the source
         string; current must follow start in the source string

--*/
unsigned char *
__cdecl
_mbsdec(
        const unsigned char *start,
        const unsigned char *current)
{
    unsigned char *ret;
    unsigned char *strPtr;
    CPINFO cpinfo;

    PERF_ENTRY(_mbsdec);
    ENTRY("_mbsdec (start=%p, current=%p)\n", start, current);

    if (current <= start)
    {
        ret = NULL;
    }
    else if (GetCPInfo(CP_ACP, &cpinfo) && cpinfo.MaxCharSize == 1)
    {
        ret = (unsigned char *) current - 1;
    }
    else
    {
        ret = strPtr = (unsigned char *) start;
        while (strPtr < current)
        {
            ret = strPtr;
            if (IsDBCSLeadByteEx(CP_ACP, *strPtr))
            {
                ++strPtr;
            }
            ++strPtr;
        }
    }
    LOGEXIT("_mbsdec returning unsigned int %p (%s)\n", ret, ret);
    PERF_EXIT(_mbsdec);
    return ret;
}
