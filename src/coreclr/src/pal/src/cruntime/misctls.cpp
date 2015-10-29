//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    cruntime/misctls.ccpp

Abstract:

    Implementation of C runtime functions that don't fit anywhere else
    and depend on per-thread data



--*/

#include "pal/thread.hpp"
#include "pal/palinternal.h"

extern "C"
{
#include "pal/dbgmsg.h"
#include "pal/misc.h"
}

#include <errno.h>
/* <stdarg.h> needs to be included after "palinternal.h" to avoid name
   collision for va_start and va_end */
#include <stdarg.h>
#include <time.h>
#if HAVE_CRT_EXTERNS_H
#include <crt_externs.h>
#endif  // HAVE_CRT_EXTERNS_H

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*++
Function:

    localtime

See MSDN for more details.
--*/

struct PAL_tm *
__cdecl
PAL_localtime(const PAL_time_t *clock)
{
    CPalThread *pThread = NULL;
    struct tm tmpResult;
    struct PAL_tm *result = NULL;

    PERF_ENTRY(localtime);
    ENTRY( "localtime( clock=%p )\n",clock );

    /* Get the per-thread buffer from the thread structure. */
    pThread = InternalGetCurrentThread();

    result = &pThread->crtInfo.localtimeBuffer;

    localtime_r(reinterpret_cast<const time_t*>(clock), &tmpResult);

    // Copy the result into the Windows struct.
    result->tm_sec = tmpResult.tm_sec;
    result->tm_min = tmpResult.tm_min;
    result->tm_hour = tmpResult.tm_hour;
    result->tm_mday = tmpResult.tm_mday;
    result->tm_mon  = tmpResult.tm_mon;
    result->tm_year = tmpResult.tm_year;
    result->tm_wday = tmpResult.tm_wday;
    result->tm_yday = tmpResult.tm_yday;
    result->tm_isdst = tmpResult.tm_isdst;

    LOGEXIT( "localtime returned %p\n", result );
    PERF_EXIT(localtime);

    return result;
}

/*++
Function:

    ctime

    There appears to be a difference between the FreeBSD and windows
    implementations.  FreeBSD gives Wed Dec 31 18:59:59 1969 for a
    -1 param, and Windows returns NULL

See MSDN for more details.
--*/
char *
__cdecl
PAL_ctime( const PAL_time_t *clock )
{
    CPalThread *pThread = NULL;
    char * retval = NULL;

    PERF_ENTRY(ctime);
    ENTRY( "ctime( clock=%p )\n",clock );
    if(*clock < 0)
    {
        /*If the input param is less than zero the value
         *returned is less than the Unix epoch
         *1st of January 1970*/
        WARN("The input param is less than zero");
        goto done;
    }

    /* Get the per-thread buffer from the thread structure. */
    pThread = InternalGetCurrentThread();

    retval = pThread->crtInfo.ctimeBuffer;

    ctime_r(reinterpret_cast<const time_t*>(clock),retval);

done:

    LOGEXIT( "ctime() returning %p (%s)\n",retval,retval);
    PERF_EXIT(ctime);

    return retval;
}

/**
Function:

    _ecvt

See MSDN for more information.

NOTES:
    There is a difference between PAL _ecvt and Win32 _ecvt.

    If Window's _ecvt receives a double 0.000000000000000000005, and count 50
    the result is "49999999999999998000000000000000000000000000000000"

    Under BSD the same call will result in :
    49999999999999998021734900744965462766153934333829

    The difference is due to the difference between BSD and Win32 sprintf.

--*/
char * __cdecl
_ecvt( double value, int count, int * dec, int * sign )
{
    CONST CHAR * FORMAT_STRING = "%.348e";
    CHAR TempBuffer[ ECVT_MAX_BUFFER_SIZE ];
    CPalThread *pThread = NULL;
    LPSTR lpReturnBuffer = NULL;
    LPSTR lpStartOfReturnBuffer = NULL;
    LPSTR lpTempBuffer = NULL;
    LPSTR lpEndOfTempBuffer = NULL;
    INT nTempBufferLength = 0;
    CHAR ExponentBuffer[ 6 ];
    INT nExponentValue = 0;
    INT LoopIndex = 0;

    PERF_ENTRY(_ecvt);
    ENTRY( "_ecvt( value=%.30g, count=%d, dec=%p, sign=%p )\n",
           value, count, dec, sign );

    /* Get the per-thread buffer from the thread structure. */
    pThread = InternalGetCurrentThread();

    lpStartOfReturnBuffer = lpReturnBuffer = pThread->crtInfo.ECVTBuffer;

    /* Sanity checks */
    if ( !dec || !sign )
    {
        ERROR( "dec and sign have to be valid pointers.\n" );
        *lpReturnBuffer = '\0';
        goto done;
    }
    else
    {
        *dec = *sign = 0;
    }

    if ( value < 0.0 )
    {
        *sign = 1;
    }

    if ( count > ECVT_MAX_COUNT_SIZE )
    {
        count = ECVT_MAX_COUNT_SIZE;
    }

    /* Get the string to work with. */
    sprintf_s( TempBuffer, sizeof(TempBuffer), FORMAT_STRING, value );

    /* Check to see if value was a valid number. */
    if ( strcmp( "NaN", TempBuffer ) == 0 || strcmp( "-NaN", TempBuffer ) == 0 )
    {
        TRACE( "value was not a number!\n" );
        if (strcpy_s( lpStartOfReturnBuffer, ECVT_MAX_BUFFER_SIZE, "1#QNAN0" ) != SAFECRT_SUCCESS)
        {
            ERROR( "strcpy_s failed!\n" );
            *lpStartOfReturnBuffer = '\0';
            goto done;
        }

        *dec = 1;
        goto done;
    }

    /* Check to see if it is infinite. */
    if ( strcmp( "Inf", TempBuffer ) == 0 || strcmp( "-Inf", TempBuffer ) == 0 )
    {
        TRACE( "value is infinite!\n" );
        if (strcpy_s( lpStartOfReturnBuffer, ECVT_MAX_BUFFER_SIZE, "1#INF00" ) != SAFECRT_SUCCESS)
        {
            ERROR( "strcpy_s failed!\n" );
            *lpStartOfReturnBuffer = '\0';
            goto done;
        }

        *dec = 1;
        if ( *TempBuffer == '-' )
        {
            *sign = 1;
        }
        goto done;
    }

    nTempBufferLength = strlen( TempBuffer );
    lpEndOfTempBuffer = &(TempBuffer[ nTempBufferLength ]);

    /* Extract the exponent, and convert it to integer. */
    while ( *lpEndOfTempBuffer != 'e' && nTempBufferLength > 0 )
    {
        nTempBufferLength--;
        lpEndOfTempBuffer--;
    }
    
    ExponentBuffer[ 0 ] = '\0';
    if (strncat_s( ExponentBuffer, sizeof(ExponentBuffer), lpEndOfTempBuffer + 1, 5 ) != SAFECRT_SUCCESS)
    {
        ERROR( "strncat_s failed!\n" );
        *lpStartOfReturnBuffer = '\0';
        goto done;
    }

    nExponentValue = atoi( ExponentBuffer );

    /* End the string at the 'e' */
    *lpEndOfTempBuffer = '\0';
    nTempBufferLength--;

    /* Determine decimal location. */
    if ( nExponentValue == 0 )
    {
        *dec = 1;
    }
    else
    {
        *dec = nExponentValue + 1;
    }

    if ( value == 0.0 )
    {
        *dec = 0;
    }
    /* Copy the string from the temp buffer upto count characters, 
    removing the sign, and decimal as required. */
    lpTempBuffer = TempBuffer;
    *lpReturnBuffer = '0';
    lpReturnBuffer++;

    while ( LoopIndex < ECVT_MAX_COUNT_SIZE )
    {
        if ( isdigit(*lpTempBuffer) )
        {
            *lpReturnBuffer = *lpTempBuffer;
            LoopIndex++;
            lpReturnBuffer++;
        }
        lpTempBuffer++;

        if ( LoopIndex == count + 1 )
        {
            break;
        }
    }

    *lpReturnBuffer = '\0';

    /* Round if needed. If count is less then 0 
    then windows does not round for some reason.*/
    nTempBufferLength = strlen( lpStartOfReturnBuffer ) - 1;
    
    /* Add one for the preceeding zero. */
    lpReturnBuffer = ( lpStartOfReturnBuffer + 1 );

    if ( nTempBufferLength >= count && count >= 0 )
    {
        /* Determine whether I need to round up. */
        if ( *(lpReturnBuffer + count) >= '5' )
        {
            CHAR cNumberToBeRounded;
            if ( count != 0 )
            {
                cNumberToBeRounded = *(lpReturnBuffer + count - 1);
            }
            else
            {
                cNumberToBeRounded = *lpReturnBuffer;
            }
            
            if ( cNumberToBeRounded < '9' )
            {
                if ( count > 0 )
                {
                    /* Add one to the character. */
                    (*(lpReturnBuffer + count - 1))++;
                }
                else
                {
                    if ( cNumberToBeRounded >= '5' )
                    {
                        (*dec)++;
                    }
                }
            }
            else
            {
                LPSTR lpRounding = NULL;

                if ( count > 0 )
                {
                    lpRounding = lpReturnBuffer + count - 1;
                }
                else
                {
                    lpRounding = lpReturnBuffer + count;
                }

                while ( cNumberToBeRounded == '9' )
                {
                    cNumberToBeRounded = *lpRounding;
                    
                    if ( cNumberToBeRounded == '9' )
                    {
                        *lpRounding = '0';
                        lpRounding--;
                    }
                }
                
                if ( lpRounding == lpStartOfReturnBuffer )
                {
                    /* Overflow. number is a whole number now. */
                    *lpRounding = '1';
                    memset( ++lpRounding, '0', count);

                    /* The decimal has moved. */
                    (*dec)++;
                }
                else
                {
                    *lpRounding = ++cNumberToBeRounded;
                }
            }
        }
        else
        {
            /* Get rid of the preceding 0 */
            lpStartOfReturnBuffer++;
        }
    }

    if ( *lpStartOfReturnBuffer == '0' )
    {
        lpStartOfReturnBuffer++;
    }

    if ( count >= 0 )
    {
        *(lpStartOfReturnBuffer + count) = '\0';
    }
    else
    {
        *lpStartOfReturnBuffer = '\0';
    }

done:

    LOGEXIT( "_ecvt returning %p (%s)\n", lpStartOfReturnBuffer , lpStartOfReturnBuffer );
    PERF_EXIT(_ecvt);

    return lpStartOfReturnBuffer;
}

