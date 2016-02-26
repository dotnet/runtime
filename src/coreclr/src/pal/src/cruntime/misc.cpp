// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    cruntime/misc.cpp

Abstract:

    Implementation of C runtime functions that don't fit anywhere else.



--*/

#include "pal/thread.hpp"
#include "pal/threadsusp.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/misc.h"

#include <errno.h>
/* <stdarg.h> needs to be included after "palinternal.h" to avoid name
   collision for va_start and va_end */
#include <stdarg.h>
#include <time.h>
#include <limits.h>

#if defined(_AMD64_) || defined(_x86_)
#include <xmmintrin.h>
#endif // defined(_AMD64_) || defined(_x86_)
#if defined(_DEBUG)
#include <assert.h>
#endif //defined(_DEBUG)

SET_DEFAULT_DEBUG_CHANNEL(CRT);

using namespace CorUnix;

/*++
Function:
  _gcvt_s

See MSDN doc.
--*/
char * 
__cdecl 
_gcvt_s( char * buffer, int iSize, double value, int digits )
{
    PERF_ENTRY(_gcvt);
    ENTRY( "_gcvt( value:%f digits=%d, buffer=%p )\n", value, digits, buffer );    

    if ( !buffer )
    {
        ERROR( "buffer was an invalid pointer.\n" );
    }

    switch ( digits )
    {
    case 7 :
        /* Fall through */
    case 8 :
        /* Fall through */
    case 15 :
        /* Fall through */
    case 17 :
        
        sprintf_s( buffer, iSize, "%.*g", digits, value );
        break;
    
    default :
        ASSERT( "Only the digits 7, 8, 15, and 17 are valid.\n" );
        *buffer = '\0';
    }
    
    LOGEXIT( "_gcvt returns %p (%s)\n", buffer , buffer );
    PERF_EXIT(_gcvt);
    return buffer;
}
    

/*++
Function :

    __iscsym

See MSDN for more details.
--*/
int
__cdecl 
__iscsym( int c )
{
    PERF_ENTRY(__iscsym);
    ENTRY( "__iscsym( c=%d )\n", c );

    if ( isalnum( c ) || c == '_'  )
    {
        LOGEXIT( "__iscsym returning 1\n" );
        PERF_EXIT(__iscsym);
        return 1;
    }

    LOGEXIT( "__iscsym returning 0\n" );
    PERF_EXIT(__iscsym);
    return 0;
}


/*++

Function :

    PAL_errno
    
    Returns the address of the errno.

--*/
int * __cdecl PAL_errno( int caller )
{
    int *retval;
    PERF_ENTRY(errno);
    ENTRY( "PAL_errno( void )\n" );
    retval = (INT*)(&errno);
    LOGEXIT("PAL_errno returns %p\n",retval);
    PERF_EXIT(errno);
    return retval;
}

/*++
Function:

    mktime

See MSDN for more details.
--*/

PAL_time_t 
__cdecl
PAL_mktime(struct PAL_tm *tm)
{
    time_t result;
    struct tm tmpTm;

    PERF_ENTRY(mktime);
    ENTRY( "mktime( tm=%p )\n",tm );

    /*copy the value of Windows struct into BSD struct*/
    tmpTm.tm_sec = tm->tm_sec;
    tmpTm.tm_min = tm->tm_min;
    tmpTm.tm_hour = tm->tm_hour;
    tmpTm.tm_mday = tm->tm_mday;
    tmpTm.tm_mon  = tm->tm_mon;
    tmpTm.tm_year = tm->tm_year;
    tmpTm.tm_wday = tm->tm_wday;
    tmpTm.tm_yday = tm->tm_yday;
    tmpTm.tm_isdst = tm->tm_isdst;

    result = mktime(&tmpTm);

    LOGEXIT( "mktime returned %#lx\n",result );
    PERF_EXIT(mktime);
    return result;
}

/*++
Function:

   rand

   The difference between the FreeBSD and Windows implementations is the max
   of the return value. in FreeBSD, RAND_MAX is 0x7fffffff and in Windows
   it's 0x7fff.

See MSDN for more details.
--*/
int
__cdecl 
PAL_rand(void)
{
    int ret;
    PERF_ENTRY(rand);
    ENTRY("rand(void)\n");

    ret = (rand() % (PAL_RAND_MAX + 1));

    LOGEXIT("rand() returning %d\n", ret);
    PERF_EXIT(rand);
    return ret;
}


/*++
Function:

   time

See MSDN for more details.
--*/
PAL_time_t
__cdecl
PAL_time(PAL_time_t *tloc)
{
    time_t result;

    PERF_ENTRY(time);
    ENTRY( "time( tloc=%p )\n",tloc );

    result = time(tloc);

    LOGEXIT( "time returning %#lx\n",result );
    PERF_EXIT(time);
    return result;
}


PALIMPORT 
void __cdecl 
PAL_qsort(void *base, size_t nmemb, size_t size, 
          int (__cdecl *compar )(const void *, const void *))
{
    PERF_ENTRY(qsort);
    ENTRY("qsort(base=%p, nmemb=%lu, size=%lu, compar=%p\n",
          base,(unsigned long) nmemb,(unsigned long) size, compar);

/* reset ENTRY nesting level back to zero, qsort will invoke app-defined 
   callbacks and we want their entry traces... */
#if _ENABLE_DEBUG_MESSAGES_
{
    int old_level;
    old_level = DBG_change_entrylevel(0);
#endif /* _ENABLE_DEBUG_MESSAGES_ */

    qsort(base,nmemb,size,compar);

/* ...and set nesting level back to what it was */
#if _ENABLE_DEBUG_MESSAGES_
    DBG_change_entrylevel(old_level);
}
#endif /* _ENABLE_DEBUG_MESSAGES_ */

    LOGEXIT("qsort returns\n");
    PERF_EXIT(qsort);
}

PALIMPORT 
void * __cdecl 
PAL_bsearch(const void *key, const void *base, size_t nmemb, size_t size,
            int (__cdecl *compar)(const void *, const void *))
{
    void *retval;

    PERF_ENTRY(bsearch);
    ENTRY("bsearch(key=%p, base=%p, nmemb=%lu, size=%lu, compar=%p\n", 
          key, base, (unsigned long) nmemb, (unsigned long) size, compar);

/* reset ENTRY nesting level back to zero, bsearch will invoke app-defined 
   callbacks and we want their entry traces... */
#if _ENABLE_DEBUG_MESSAGES_
{
    int old_level;
    old_level = DBG_change_entrylevel(0);
#endif /* _ENABLE_DEBUG_MESSAGES_ */

    retval = bsearch(key,base,nmemb,size,compar);

/* ...and set nesting level back to what it was */
#if _ENABLE_DEBUG_MESSAGES_
    DBG_change_entrylevel(old_level);
}
#endif /* _ENABLE_DEBUG_MESSAGES_ */

    LOGEXIT("bsearch returns %p\n",retval);
    PERF_EXIT(bsearch);
    return retval;
}

#ifdef _AMD64_ 

PALIMPORT
unsigned int PAL__mm_getcsr(void)
{
    return _mm_getcsr();
}

PALIMPORT
void PAL__mm_setcsr(unsigned int i)
{
    _mm_setcsr(i);
}

#endif // _AMD64_ 

#if defined(_DEBUG)

/*++
Function:
PAL_memcpy

Overlapping buffer-safe version of memcpy.
See MSDN doc for memcpy
--*/
void *PAL_memcpy (void *dest, const void *src, size_t count)
{
    UINT_PTR x = (UINT_PTR)dest, y = (UINT_PTR)src;
    assert((x + count <= y) || (y + count <= x));
    
    void *ret;
    #undef memcpy
    ret = memcpy(dest, src, count);
    return ret;
}
#endif //DEBUG
