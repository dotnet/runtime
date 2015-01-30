//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test6.c
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               GenerateConsoleCtrlEvent
**
** Purpose:
**
** Test to ensure proper operation of the SetConsoleCtrlHandler()
** API by trying to remove a non-existent handler.
** 

**
**===========================================================================*/
#include <palsuite.h>


static BOOL g_bFlag1 = FALSE;
static BOOL g_bFlag2 = FALSE;


/* first handler function */
static BOOL PALAPI CtrlHandler1( DWORD CtrlType ) 
{ 
    if( CtrlType == CTRL_C_EVENT )
    {
        g_bFlag1 = TRUE;
        return TRUE;
    }

    return FALSE;
}


/* second handler function */
static BOOL PALAPI CtrlHandler2( DWORD CtrlType ) 
{ 
    if( CtrlType == CTRL_C_EVENT )
    {
        g_bFlag2 = TRUE;
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
    if( ! SetConsoleCtrlHandler( CtrlHandler1, TRUE ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                "CtrlHandler1\n",
                GetLastError() );
        Fail( "Test failed\n" );
    }
    
    /* test that the right control handler functions are set */
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
    if( g_bFlag2 )
    {
        Trace( "ERROR:CtrlHandler2() was inexplicably called\n" );
        ret = FAIL;
        goto done;
    }
    
    if( ! g_bFlag1 )
    {
        Trace( "ERROR:CtrlHandler1() was not called but should have been\n" );
        ret = FAIL;
        goto done;
    }
    
    /* reset our flags */
    g_bFlag1 = FALSE;

    
    /* try to unset CtrlHandler2, which isn't set in the first place */
    if( SetConsoleCtrlHandler( CtrlHandler2, FALSE ) )
    {
        ret = FAIL;
        Trace( "ERROR:SetConsoleCtrlHandler() succeeded trying to "
                "remove CtrlHandler2, which isn't set\n" );
        goto done;
    }


    /* make sure that the existing control handler functions are still set */
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
    if( g_bFlag2 )
    {
        Trace( "ERROR:CtrlHandler2() was inexplicably called\n" );
        ret = FAIL;
        goto done;
    }
    
    if( ! g_bFlag1 )
    {
        Trace( "ERROR:CtrlHandler1() was not called but should have been\n" );
        ret = FAIL;
        goto done;
    }
    
    
    
done:
    /* unset any handlers that were set */
    if( ! SetConsoleCtrlHandler( CtrlHandler1, FALSE ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to "
                "remove CtrlHandler1\n",
                GetLastError() );
        Fail( "Test failed\n" );
    }
    
    
    /* PAL termination */
    PAL_TerminateEx(ret);

    
    /* return our result */
    return ret;
}

