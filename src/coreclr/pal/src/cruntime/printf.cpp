// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    printf.c

Abstract:

    Implementation of the printf family functions.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/cruntime.h"
#include "pal/thread.hpp"
#include "pal/threadsusp.hpp"
#include "pal/printfcpp.hpp"

/* <stdarg.h> needs to be included after "palinternal.h" to avoid name
   collision for va_start and va_end */
#include <stdarg.h>
#include <errno.h>

SET_DEFAULT_DEBUG_CHANNEL(CRT);

#if SSCANF_SUPPORT_ll
const static char *scanf_longlongfmt = "ll";
#else
const static char *scanf_longlongfmt = "q";
#endif

#if SSCANF_CANNOT_HANDLE_MISSING_EXPONENT
static int SscanfFloatCheckExponent(LPCSTR buff, LPCSTR floatFmt,
                                      void * voidPtr, int * pn);
#endif // SSCANF_CANNOT_HANDLE_MISSING_EXPONENT

/*******************************************************************************
Function:
  PAL_printf_arg_remover

Parameters:
  ap
    - pointer to the va_list from which to remove arguments
  Width
    - the width of the current format operation
  Precision
    - the precision of the current format option
  Type
    - the type of the argument for the current format option
  Prefix
    - the prefix for the current format option
*******************************************************************************/
void PAL_printf_arg_remover(va_list *ap, INT Width, INT Precision, INT Type, INT Prefix)
{
    /* remove arg and precision if needed */
    if (PRECISION_STAR == Precision ||
        PRECISION_INVALID == Precision)
    {
        (void)va_arg(*ap, int);
    }
    if (WIDTH_STAR == Width ||
        WIDTH_INVALID == Width)
    {
        (void)va_arg(*ap, int);
    }
    if (Type == PFF_TYPE_FLOAT)
    {
        (void)va_arg(*ap, double);
    }
    else if (Type == PFF_TYPE_INT && Prefix == PFF_PREFIX_LONGLONG)
    {
        (void)va_arg(*ap, INT64);
    }
    else if (Type == PFF_TYPE_INT || Type == PFF_TYPE_CHAR)
    {
        (void)va_arg(*ap, int);
    }
    else
    {
        (void)va_arg(*ap, void *);
    }
}

/*++
Function:
  PAL_printf

See MSDN doc.
--*/
int
__cdecl
PAL_printf(
      const char *format,
      ...)
{
    LONG Length;
    va_list ap;

    PERF_ENTRY(printf);
    ENTRY("PAL_printf (format=%p (%s))\n", format, format);

    va_start(ap, format);
    Length = PAL_vprintf(format, ap);
    va_end(ap);

    LOGEXIT("PAL_printf returns int %d\n", Length);
    PERF_EXIT(printf);
    return Length;
}

/*++
Function:
  PAL_fprintf

See MSDN doc.
--*/
int
__cdecl
PAL_fprintf(PAL_FILE *stream,const char *format,...)
{
    LONG Length = 0;
    va_list ap;

    PERF_ENTRY(fprintf);
    ENTRY("PAL_fprintf(stream=%p,format=%p (%s))\n",stream, format, format);

    va_start(ap, format);
    Length = PAL_vfprintf( stream, format, ap);
    va_end(ap);

    LOGEXIT("PAL_fprintf returns int %d\n", Length);
    PERF_EXIT(fprintf);
    return Length;
}

/*++
Function:
  PAL_vprintf

See MSDN doc.
--*/
int
__cdecl
PAL_vprintf(
      const char *format,
      va_list ap)
{
    LONG Length;

    PERF_ENTRY(vprintf);
    ENTRY("PAL_vprintf (format=%p (%s))\n", format, format);

    Length = PAL_vfprintf( PAL_get_stdout(PAL_get_caller), format, ap);

    LOGEXIT("PAL_vprintf returns int %d\n", Length);
    PERF_EXIT(vprintf);
    return Length;
}

#if SSCANF_CANNOT_HANDLE_MISSING_EXPONENT
/*++
Function:
  SscanfFloatCheckExponent

  Parameters:
  buff:     pointer to the buffer to be parsed; the target float must be at
            the beginning of the buffer, except for any number of leading
            spaces
  floatFmt: must be "%e%n" (or "%f%n" or "%g%n")
  voidptr:  optional pointer to output variable (which should be a float)
  pn:       pointer to an int to receive the number of bytes parsed.

  Notes:
  On some platforms (specifically AIX) sscanf fails to parse a float from
  a string such as 12.34e (while it succeeds for e.g. 12.34a). Sscanf
  initially interprets the 'e' as the keyword for the beginning of a
  10-exponent of a floating point in scientific notation (as in 12.34e5),
  but then it fails to parse the actual exponent. At this point sscanf should
  be able to fall back on the narrower pattern, and parse the floating point
  in common decimal notation (i.e. 12.34). However AIX's sscanf fails to do
  so and it does not parse any number.
  This function checks the given string for a such case and removes
  the 'e' before parsing the float.

--*/

static int SscanfFloatCheckExponent(LPCSTR buff, LPCSTR floatFmt,
                                      void * voidPtr, int * pn)
{
    int ret = 0;
    int digits = 0;
    int points = 0;
    LPCSTR pos = buff;

    /* skip initial spaces */
    while (*pos && isspace(*pos))
        pos++;

    /* go to the end of a float, if there is one */
    while (*pos)
    {
        if (isdigit(*pos))
            digits++;
        else if (*pos == '.')
        {
            if (++points > 1)
                break;
        }
        else
            break;

        pos++;
    }

    /* check if it is something like 12.34e and the trailing 'e' is not
       the suffix of a valid exponent of 10, such as 12.34e+5 */
    if ( digits > 0 && *pos && tolower(*pos) == 'e' &&
         !( *(pos+1) &&
            ( isdigit(*(pos+1)) ||
              ( (*(pos+1) == '+' || *(pos+1) == '-') && isdigit(*(pos+2)) )
                )
             )
        )
    {
        CHAR * pLocBuf = (CHAR *)PAL_malloc((pos-buff+1)*sizeof(CHAR));
        if (pLocBuf)
        {
            memcpy(pLocBuf, buff, (pos-buff)*sizeof(CHAR));
            pLocBuf[pos-buff] = 0;
            if (voidPtr)
                ret = sscanf_s(pLocBuf, floatFmt, voidPtr, pn);
            else
                ret = sscanf_s(pLocBuf, floatFmt, pn);
            PAL_free (pLocBuf);
        }
    }
    return ret;
}
#endif // SSCANF_CANNOT_HANDLE_MISSING_EXPONENT
