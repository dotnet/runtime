// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    wchartls.c

Abstract:

    Implementation of wide char string functions that depend on per-thread data



--*/

#include "pal/palinternal.h"
#include "pal/thread.hpp"
#include "pal/dbgmsg.h"

using namespace CorUnix;


SET_DEFAULT_DEBUG_CHANNEL(CRT);

/*++
Function:
   PAL_wcstok

Finds the next token in a wide character string.

Return value:

A pointer to the next token found in strToken.  Returns NULL when no more 
tokens are found.  Each call modifies strToken by substituting a NULL 
character for each delimiter that is encountered.

Parameters:
strToken        String containing token(s)
strDelimit      Set of delimiter characters

--*/
WCHAR *
__cdecl
PAL_wcstok(WCHAR *strToken, const WCHAR *strDelimit)
{
    CPalThread *pThread = NULL;
    WCHAR *retval = NULL;
    WCHAR *delim_ptr;
    WCHAR *next_context;     /* string to save in TLS for future calls */

    PERF_ENTRY(wcstok);
    ENTRY("PAL_wcstok (strToken=%p (%S), strDelimit=%p (%S))\n",
          strToken?strToken:W16_NULLSTRING,
          strToken?strToken:W16_NULLSTRING, 
          strDelimit?strDelimit:W16_NULLSTRING, 
          strDelimit?strDelimit:W16_NULLSTRING);

    /* Get the per-thread buffer from the thread structure. */
    pThread = InternalGetCurrentThread();
    
    if(NULL == strDelimit)
    {
        ERROR("delimiter string is NULL\n");
        goto done;
    }

    /* get token string from TLS if none is provided */
    if(NULL == strToken)
    {
        TRACE("wcstok() called with NULL string, using previous string\n");
        strToken = pThread->crtInfo.wcstokContext;
        if(NULL == strToken)
        {            
            ERROR("wcstok called with NULL string without a previous call\n");
            goto done;
        }
    }
    
    /* first, skip all leading delimiters */
    while ((*strToken != '\0') && (PAL_wcschr(strDelimit,*strToken)))
    {
        strToken++;
    }

    /* if there were only delimiters, there's no string */
    if('\0' == strToken[0])
    {
        TRACE("end of string already reached, returning NULL\n");
        goto done;
    }

    /* we're now at the beginning of the token; look for the first delimiter */
    delim_ptr = PAL_wcspbrk(strToken,strDelimit);
    if(NULL == delim_ptr)
    {
        TRACE("no delimiters found, this is the last token\n");
        /* place the next context at the end of the string, so that subsequent 
           calls will return NULL */
        next_context = strToken+PAL_wcslen(strToken);
        retval = strToken;
    }
    else
    {
        /* null-terminate current token */
        *delim_ptr=0;

        /* place the next context right after the delimiter */
        next_context = delim_ptr+1;
        retval = strToken;
        
        TRACE("found delimiter; next token will be %p\n",next_context);
    }

    pThread->crtInfo.wcstokContext = next_context;

done:
    LOGEXIT("PAL_wcstok() returns %p (%S)\n", retval?retval:W16_NULLSTRING, retval?retval:W16_NULLSTRING);
    PERF_EXIT(wcstok);
    return(retval);
}

