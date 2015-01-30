//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test5.c
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               GenerateConsoleCtrlEvent
**
** Purpose:
**
** Test to ensure proper operation of the SetConsoleCtrlHandler()
** API by using it to chain multiple handler functions together.
** 

**
**===========================================================================*/
#include <palsuite.h>


/* helper test structure */
struct handlerData
{
    PHANDLER_ROUTINE func;      /* handler routine */
    BOOL             set;       /* whether this handler was set             */
    BOOL             stop;      /* whether handling should stop with this   */
    BOOL             result;    /* flag tracking whether handler was called */
};


/* we'll work with three handler functions */
#define NUM_HANDLERS 3

/* handler function prototypes */
static BOOL PALAPI CtrlHandler1( DWORD CtrlType );
static BOOL PALAPI CtrlHandler2( DWORD CtrlType );
static BOOL PALAPI CtrlHandler3( DWORD CtrlType );


/* array of control handler data */
static struct handlerData g_handlers[] = 
{
    { CtrlHandler1, FALSE, TRUE, FALSE },
    { CtrlHandler2, FALSE, FALSE, FALSE },
    { CtrlHandler3, FALSE, FALSE, FALSE }
};


/* global flag to track whether the handlers are called in the wrong order */
static BOOL g_bWrongOrder = FALSE;


/* first handler function */
static BOOL PALAPI CtrlHandler1( DWORD CtrlType ) 
{ 
    if( CtrlType == CTRL_C_EVENT )
    {
        /* set our handler flag to true */
        g_handlers[0].result = TRUE;
        
        /* check for right handler order */
        if( (!g_handlers[1].result) || (!g_handlers[2].result) )
        {
            g_bWrongOrder = TRUE;
        }
        return g_handlers[0].stop;
    }

    return FALSE;
}


/* second handler function */
static BOOL PALAPI CtrlHandler2( DWORD CtrlType ) 
{ 
    if( CtrlType == CTRL_C_EVENT )
    {
        /* set our handler flag to true */
        g_handlers[1].result = TRUE;
        
        /* check for right handler order */
        if( g_handlers[0].result || (!g_handlers[2].result) )
        {
            g_bWrongOrder = TRUE;
        }
        return g_handlers[1].stop;
    }

    return FALSE;
}


/* third handler function */
static BOOL PALAPI CtrlHandler3( DWORD CtrlType ) 
{ 
    if( CtrlType == CTRL_C_EVENT )
    {
        /* set our handler flag to true */
        g_handlers[2].result = TRUE;
        
        /* check for right handler order */
        if( g_handlers[0].result || g_handlers[1].result )
        {
            g_bWrongOrder = TRUE;
        }
        return g_handlers[2].stop;
    }

    return FALSE;
}






/* main entry point function */
int __cdecl main( int argc, char **argv ) 

{
    /* local variables */
    int     i;
    int     finalHandler;
    BOOL    ret = PASS;


    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }


    /* chain together three simple control handlers */
    for( i=0; i<NUM_HANDLERS; i++ )
    {
        if( SetConsoleCtrlHandler( g_handlers[i].func, TRUE ) )
        {
            g_handlers[i].set = TRUE;
        }
        else
        {
            ret = FAIL;
            Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                    "handler #%d\n",
                    GetLastError(),
                    (i+1) );
            goto done;
        }
    }
    

    /* first test -- verify that all three handlers are called, in the */
    /* correct sequence (CtrlHandler3, CtrlHandler2, CtrlHandler1)     */
    if( ! GenerateConsoleCtrlEvent( CTRL_C_EVENT, 0 ) )
    {
        Trace( "ERROR:%lu:GenerateConsoleCtrlEvent() failed\n",
                GetLastError() );
        ret = FAIL;
        goto done;
    }

    /* give the handlers a chance to execute */    
    Sleep( 2000 );
    
    
    /* check the results and reset all the handler-called flags */
    for( i=0; i<NUM_HANDLERS; i++ )
    {
        if( ! g_handlers[i].result )
        {
            Trace( "FAIL:Handler #%d was not called\n", (i+1) );
            ret = FAIL;
        }
        else
        {
            g_handlers[i].result = FALSE;
        }
    }
    
    if( g_bWrongOrder )
    {
        Trace( "FAIL:Handlers were called in the wrong order\n" );
        ret = FAIL;
    }
    
    /* we're done if we got an error result */
    if( ! ret )
    {
        goto done;
    }

    
    /* same test, only this time we want to stop at the second handler */
    finalHandler = 1;
    g_handlers[ finalHandler ].stop = TRUE;
    if( ! GenerateConsoleCtrlEvent( CTRL_C_EVENT, 0 ) )
    {
        Trace( "ERROR:%lu:GenerateConsoleCtrlEvent() failed\n",
                GetLastError() );
        ret = FAIL;
        goto done;
    }
    
    /* give the handlers a chance to execute */    
    Sleep( 2000 );
    
    
    /* check the results */
    for( i=0; i<NUM_HANDLERS; i++ )
    {
        if( i < finalHandler )
        {
            /* should not have been called */
            if( g_handlers[i].result )
            {
                Trace( "FAIL:Handler #%d was called but shouldn't "
                        "have been\n",
                        (i+1) );
                ret = FAIL;
            }
        }
        else
        {
            /* should have been called */
            if( ! g_handlers[i].result )
            {
                Trace( "FAIL:Handler #%d was not called\n", (i+1) );
                ret = FAIL;
            }
        }
    }
    
    
    
done:
    /* unset any handlers that were set */
    for( i=0; i<NUM_HANDLERS; i++ )
    {
        if( g_handlers[i].set )
        {
            if( ! SetConsoleCtrlHandler( g_handlers[i].func, FALSE ) )
            {
                ret = FAIL;
                Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                        "handler #%d\n",
                        GetLastError(),
                        (i+1) );
            }
        }
    }
    
    
    /* PAL termination */
    PAL_TerminateEx(ret);

    
    /* return our result */
    return ret;
}
