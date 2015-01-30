//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test10.c
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               GenerateConsoleCtrlEvent
**
** Purpose:
**
** Test to ensure proper operation of the SetConsoleCtrlHandler()
** API by checking whether a console control handler function is
** actually removed by the API when it returns success for that
** operation.
** 

**
**===========================================================================*/
#include <palsuite.h>



/* global test value */
static BOOL g_bFlag = FALSE;



/* handler function */
static BOOL PALAPI CtrlHandler( DWORD CtrlType ) 
{ 
    if( CtrlType == CTRL_BREAK_EVENT )
    {
        g_bFlag = TRUE;
        return TRUE;
    }

    return FALSE;
}





/* main entry point function */
int __cdecl main( int argc, char **argv ) 

{
    /* local variables */
    BOOL    ret = PASS;


    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }


    /* set the console control handler function */
    if( ! SetConsoleCtrlHandler( CtrlHandler, TRUE ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                "CtrlHandler\n",
                GetLastError() );
        Fail( "Test failed\n" );
    }
    

    /* test that the right control handler functions are set */
    if( ! GenerateConsoleCtrlEvent( CTRL_BREAK_EVENT, 0 ) )
    {
        Trace( "ERROR:%lu:GenerateConsoleCtrlEvent() failed\n",
                GetLastError() );
        ret = FAIL;
        goto done;
    }

    /* give the handlers a chance to execute */    
    Sleep( 2000 );
    
    /* check the results */
    if( ! g_bFlag )
    {
        Trace( "ERROR:CtrlHandler() was not called but should have been\n" );
        ret = FAIL;
    }
    
    
    
done:
    /* unset the control handle that was set */
    if( ! SetConsoleCtrlHandler( CtrlHandler, FALSE ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to "
                "remove CtrlHandler\n",
                GetLastError() );
        Fail( "Test failed\n" );
    }
    
    
    /* PAL termination */
    PAL_TerminateEx(ret);

    
    /* return our result */
    return ret;
}

