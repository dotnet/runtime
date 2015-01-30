//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test7.c
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
        return FALSE;
    }

    return FALSE;
}





/* main entry point function */
int __cdecl main( int argc, char **argv ) 

{
    /* local variables */
    BOOL    ret = PASS;
    BOOL    bSetHandler1 = FALSE;


    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }


    /* set the console control handler functions */
    if( SetConsoleCtrlHandler( CtrlHandler1, TRUE ) )
    {
        bSetHandler1 = TRUE;
    }
    else
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                "CtrlHandler1\n",
                GetLastError() );
        Fail( "Test failed\n" );
    }
    
    if( ! SetConsoleCtrlHandler( CtrlHandler2, TRUE ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to add "
                "CtrlHandler2\n",
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
    if( ! g_bFlag1 )
    {
        Trace( "ERROR:CtrlHandler1() was not called but should have been\n" );
        ret = FAIL;
        goto done;
    }
    if( ! g_bFlag2 )
    {
        Trace( "ERROR:CtrlHandler2() was not called but should have been\n" );
        ret = FAIL;
        goto done;
    }
    
    
    /* reset our flags */
    g_bFlag1 = FALSE;
    g_bFlag2 = FALSE;

    
    /* try to unset CtrlHandler2 */
    if( ! SetConsoleCtrlHandler( CtrlHandler2, FALSE ) )
    {
        ret = FAIL;
        Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to "
                "remove CtrlHandler2\n",
                GetLastError() );
        goto done;
    }


    /* make sure that CtrlHandler1 is set and CtrlHandler2 isn't */
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
        Trace( "ERROR:CtrlHandler2() was called after it was unset\n" );
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
    /* unset CtrlHandler1 if it was set */
    if( bSetHandler1 )
    {
        if( ! SetConsoleCtrlHandler( CtrlHandler1, FALSE ) )
        {
            ret = FAIL;
            Trace( "ERROR:%lu:SetConsoleCtrlHandler() failed to "
                    "remove CtrlHandler1\n",
                    GetLastError() );
            Fail( "Test failed\n" );
        }
    }
    
    
    /* PAL termination */
    PAL_TerminateEx(ret);

    
    /* return our result */
    return ret;
}

