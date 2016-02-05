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
#include "pal/malloc.hpp"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/misc.h"

#include <errno.h>
/* <stdarg.h> needs to be included after "palinternal.h" to avoid name
   collision for va_start and va_end */
#include <stdarg.h>
#include <time.h>
#include <limits.h>
#if HAVE_CRT_EXTERNS_H
#include <crt_externs.h>
#endif  // HAVE_CRT_EXTERNS_H
#if defined(_AMD64_) || defined(_x86_)
#include <xmmintrin.h>
#endif // defined(_AMD64_) || defined(_x86_)
#if defined(_DEBUG)
#include <assert.h>
#endif //defined(_DEBUG)

SET_DEFAULT_DEBUG_CHANNEL(CRT);

char **palEnvironment = NULL;

CRITICAL_SECTION gcsEnvironment;

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

Function : _putenv.
    
See MSDN for more details.

Note:   The BSD implementation can cause
        memory leaks. See man pages for more details.
--*/
int
__cdecl 
_putenv( const char * envstring )
{
    int ret = -1;

    PERF_ENTRY(_putenv);
    ENTRY( "_putenv( %p (%s) )\n", envstring ? envstring : "NULL", envstring ? envstring : "NULL") ;
    
    if (!envstring)
    {
        ERROR( "_putenv() called with NULL envstring!\n");
        goto EXIT;
    }

    ret = MiscPutenv(envstring, TRUE) ? 0 : -1;

EXIT:    
    LOGEXIT( "_putenv returning %d\n", ret);
    PERF_EXIT(_putenv);
    return ret;
}

/*++

Function : PAL_getenv
    
See MSDN for more details.
--*/
char * __cdecl PAL_getenv(const char *varname)
{
    char *retval;

    PERF_ENTRY(getenv);
    ENTRY("getenv (%p (%s))\n", varname ? varname : "NULL", varname ? varname : "NULL");
    
    if (strcmp(varname, "") == 0)
    {
        ERROR("getenv called with a empty variable name\n");
        LOGEXIT("getenv returning NULL\n");
        PERF_EXIT(getenv);
        return(NULL);
    }
    retval = MiscGetenv(varname);

    LOGEXIT("getenv returning %p\n", retval);
    PERF_EXIT(getenv);
    return(retval);
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

/*++
Function:
  MiscGetEnvArray

Get a reference to the process's environ array into palEnvironment

NOTE: This function MUST be called while holding the gcsEnvironment 
      critical section (except if the caller is the initialization 
      routine)
--*/
void
MiscGetEnvArray(void)
{
#if HAVE__NSGETENVIRON
    palEnvironment = *(_NSGetEnviron());
#else   // HAVE__NSGETENVIRON
    extern char **environ;
    palEnvironment = environ;
#endif  // HAVE__NSGETENVIRON
}

/*++
Function:
  MiscSetEnvArray

Make sure the process's environ array is in sync with palEnvironment variable

NOTE: This function MUST be called while holding the gcsEnvironment 
      critical section (except if the caller is the initialization 
      routine)
--*/
void
MiscSetEnvArray(void)
{
#if HAVE__NSGETENVIRON
    *(_NSGetEnviron()) = palEnvironment;
#else   // HAVE__NSGETENVIRON
    extern char **environ;
    environ = palEnvironment;
#endif  // HAVE__NSGETENVIRON
}

/*++
Function:
  MiscInitialize

Initialization function called from PAL_Initialize.
Allocates the TLS Index. On systems that use extern variables for
time zone information, this also initializes those variables.

Note: This is called before debug channels are initialized, so it
      cannot use debug tracing calls.
--*/
BOOL
MiscInitialize(void)
{
    InternalInitializeCriticalSection(&gcsEnvironment);
    MiscGetEnvArray();

    return TRUE;
}

/*++
Function:
  MiscCleanup

Termination function called from PAL_Terminate to delete the
TLS Keys created in MiscInitialize
--*/
void MiscCleanup(void)
{
     TRACE("Cleaning Misc...\n");
     InternalDeleteCriticalSection(&gcsEnvironment);
}

/*++
Function:
  MiscGetenv

Gets an environment variable's value from environ. The returned buffer
must not be modified or freed.
--*/
char *MiscGetenv(const char *name)
{
    int i, length;
    char *equals;
    char *pRet = NULL;
    CPalThread * pthrCurrent = InternalGetCurrentThread();

    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);

    length = strlen(name);
    for(i = 0; palEnvironment[i] != NULL; i++)
    {
        if (memcmp(palEnvironment[i], name, length) == 0)
        {
            equals = palEnvironment[i] + length;
            if (*equals == '\0')
            {
                pRet = (char *) "";
                goto done;
            } 
            else if (*equals == '=') 
            {
                pRet = equals + 1;
                goto done;
            }
        }
    }

done:
    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    return pRet;
}


/*++
Function:
  MiscPutenv

Sets an environment variable's value by directly modifying palEnvironment.
Returns TRUE if the variable was set, or FALSE if PAL_malloc or realloc
failed or if the given string is malformed.
--*/
BOOL MiscPutenv(const char *string, BOOL deleteIfEmpty)
{
    const char *equals, *existingEquals;
    char *copy = NULL;
    int length;
    int i, j;
    bool fOwningCS = false;
    BOOL result = FALSE;
    CPalThread * pthrCurrent = InternalGetCurrentThread();
    
    equals = strchr(string, '=');
    if (equals == string || equals == NULL)
    {
        // "=foo" and "foo" have no meaning
        goto done;
    }
    if (equals[1] == '\0' && deleteIfEmpty)
    {
        // "foo=" removes foo from the environment in _putenv() on Windows.
        // The same string can result from a call to SetEnvironmentVariable()
        // with the empty string as the value, but in that case we want to
        // set the variable's value to "". deleteIfEmpty will be FALSE in
        // that case.
        length = strlen(string);
        copy = (char *) InternalMalloc(length);
        if (copy == NULL)
        {
            goto done;
        }
        memcpy(copy, string, length - 1);
        copy[length - 1] = '\0';    // Change '=' to '\0'
        MiscUnsetenv(copy);
        result = TRUE;
    }
    else
    {
        // See if we are replacing an item or adding one.
        
        // Make our copy up front, since we'll use it either way.
        copy = strdup(string);
        if (copy == NULL)
        {
            goto done;
        }
        
        length = equals - string;

        InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);
        fOwningCS = true;
        
        for(i = 0; palEnvironment[i] != NULL; i++)
        {
            existingEquals = strchr(palEnvironment[i], '=');
            if (existingEquals == NULL)
            {
                // The PAL screens out malformed strings, but
                // environ comes from the system, so it might
                // have strings without '='. We treat the entire
                // string as a name in that case.
                existingEquals = palEnvironment[i] + strlen(palEnvironment[i]);
            }
            if (existingEquals - palEnvironment[i] == length)
            {
                if (memcmp(string, palEnvironment[i], length) == 0)
                {
                    // Replace this one. Don't free the original,
                    // though, because there may be outstanding
                    // references to it that were acquired via
                    // getenv. This is an unavoidable memory leak.
                    palEnvironment[i] = copy;
                    
                    // Set 'copy' to NULL so it won't be freed
                    copy = NULL;
                    
                    result = TRUE;
                    break;
                }
            }
        }
        if (palEnvironment[i] == NULL)
        {            
            static BOOL sAllocatedEnviron = FALSE;
            // Add a new environment variable.
            // We'd like to realloc palEnvironment, but we can't do that the
            // first time through.
            char **newEnviron = NULL;
            
            if (sAllocatedEnviron) {
                if (NULL == (newEnviron = 
                        (char **)InternalRealloc(palEnvironment, (i + 2) * sizeof(char *))))
                {
                    goto done;
                }
            }
            else
            {
                // Allocate palEnvironment ourselves so we can realloc it later.
                newEnviron = (char **)InternalMalloc((i + 2) * sizeof(char *));
                if (newEnviron == NULL)
                {
                    goto done;
                }
                for(j = 0; palEnvironment[j] != NULL; j++)
                {
                    newEnviron[j] = palEnvironment[j];
                }
                sAllocatedEnviron = TRUE;
            }
            palEnvironment = newEnviron;
            MiscSetEnvArray();
            palEnvironment[i] = copy;
            palEnvironment[i + 1] = NULL;

            // Set 'copy' to NULL so it won't be freed
            copy = NULL;
            
            result = TRUE;
        }
    }
done:

    if (fOwningCS)
    {
        InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
    }
    if (NULL != copy)
    {
        InternalFree(copy);
    }
    return result;
}

/*++
Function:
  MiscUnsetenv

Removes a variable from the environment. Does nothing if the variable
does not exist in the environment.
--*/
void MiscUnsetenv(const char *name)
{
    const char *equals;
    int length;
    int i, j;
    CPalThread * pthrCurrent = InternalGetCurrentThread();
    
    length = strlen(name);

    InternalEnterCriticalSection(pthrCurrent, &gcsEnvironment);
    for(i = 0; palEnvironment[i] != NULL; i++)
    {
        equals = strchr(palEnvironment[i], '=');
        if (equals == NULL)
        {
            equals = palEnvironment[i] + strlen(palEnvironment[i]);
        }
        if (equals - palEnvironment[i] == length)
        {
            if (memcmp(name, palEnvironment[i], length) == 0)
            {
                // Remove this one. Don't free it, though, since
                // there might be oustanding references to it that
                // were acquired via getenv. This is an
                // unavoidable memory leak.
                for(j = i + 1; palEnvironment[j] != NULL; j++) { }
                // i is now the one we want to remove. j is the
                // last index in palEnvironment, which is NULL.

                // Shift palEnvironment down by the difference between i and j.
                memmove(palEnvironment + i, palEnvironment + i + 1, (j - i) * sizeof(char *));
            }
        }
    }
    InternalLeaveCriticalSection(pthrCurrent, &gcsEnvironment);
}

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
