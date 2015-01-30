//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test8.c
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               GenerateConsoleCtrlEvent
**
** Purpose:
**
** Test to ensure proper operation of the SetConsoleCtrlHandler()
** API by attempting to add and remove the same handler multiple
** times.
** 

**
**===========================================================================*/
#include <palsuite.h>



/* the number of times to set the console control handler function */
#define HANDLER_SET_COUNT 5



/* the number of times the console control handler function's been called */
static int g_count = 0;




/* to avoid having the default control handler abort our process */
static BOOL PALAPI FinalCtrlHandler( DWORD CtrlType )
{
    return (CtrlType == CTRL_C_EVENT);
}


/* test handler function */
static BOOL PALAPI CtrlHandler1( DWORD CtrlType ) 
{ 
    if( CtrlType == CTRL_C_EVENT )
    {
        ++g_count;
    }

    return FALSE;
}





/* main entry point function */
int __cdecl main( int argc, char **argv ) 

{
    /* local variables */
    int     i;
    BOOL    ret = PASS;
    BOOL    bSetFinalHandler = FALSE;
    int     nSetCount = 0;


    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }


    /* set our final console control handler function */
    if( SetConsoleCtrlHandler( FinalCtrlHandler, TRUE ) )
    {
        bSetFinalHandler = TRUE;
    }
    else
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                "FinalCtrlHandler\n",
                GetLastError() );
        Fail( "Test failed\n" );
    }
    
    /* try to set our test handler multiple times */
    for( i=0; i<HANDLER_SET_COUNT; i++ )
    {
        if( SetConsoleCtrlHandler( CtrlHandler1, TRUE ) )
        {
            nSetCount++;
        }
        else
        {
            ret = FAIL;
            Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                    "CtrlHandler1 at attempt #%d\n",
                    GetLastError(),
                    i );
            goto done;
        }
    }

    /* loop here -- generate an event and verify that our handler */
    /* was called the correct number of times, then unset it one  */
    /* time and repeat until it's completely unset.               */
    for( ; nSetCount>0; nSetCount-- )
    {
        /* test that the control handler functions are set */
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
        if( g_count != nSetCount )
        {
            Trace( "ERROR:CtrlHandler1() was not called %d times, "
                    "expected %d\n",
                    g_count,
                    nSetCount );
            ret = FAIL;
            goto done;
        }
        
        /* unset the control handler one time */
        if( ! SetConsoleCtrlHandler( CtrlHandler1, FALSE ) )
        {
            ret = FAIL;
            Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to "
                    "remove CtrlHandler instance #%d\n",
                    GetLastError(),
                    nSetCount );
            Fail( "Test failed\n" );
        }
        
        /* reset our counter */
        g_count = 0;
    }
    
    
    
done:
    /* unset any lingering instances of our test control handler */
    for( ; nSetCount>0; nSetCount-- )
    {
        /* unset the control handler one time */
        if( ! SetConsoleCtrlHandler( CtrlHandler1, FALSE ) )
        {
            ret = FAIL;
            Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to "
                    "remove CtrlHandler instance #%d\n",
                    GetLastError(),
                    nSetCount );
            Fail( "Test failed\n" );
        }
    }
    
    
    /* unset our final control handler if it was set */
    if( bSetFinalHandler )
    {
        if( ! SetConsoleCtrlHandler( FinalCtrlHandler, FALSE ) )
        {
            ret = FAIL;
            Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to "
                    "remove FinalCtrlHandler\n",
                    GetLastError() );
            Fail( "Test failed\n" );
        }
    }
    
    
    /* PAL termination */
    PAL_TerminateEx(ret);

    
    /* return our result */
    return ret;
}
