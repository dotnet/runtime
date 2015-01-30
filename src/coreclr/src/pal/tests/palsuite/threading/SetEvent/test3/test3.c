//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*=============================================================================
**
** Source: test3.c
**
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               CreateEvent
**               CloseHandle
**
** Purpose:
**
** Test to ensure proper operation of the SetEvent()
** API by calling it on an event handle that's been
** closed. We expect it to return an appropriate error
** result.
** 

**
**===========================================================================*/
#include <palsuite.h>



int __cdecl main( int argc, char **argv ) 

{
    /* local variables */
    HANDLE                  hEvent = NULL;
    LPSECURITY_ATTRIBUTES   lpEventAttributes = NULL;
    BOOL                    bManualReset = TRUE; 
    BOOL                    bInitialState = FALSE;
    LPCTSTR                 lpName = "WooBaby";


    /* PAL initialization */
    if( (PAL_Initialize(argc, argv)) != 0 )
    {
        return( FAIL );
    }


    /* create an event which we can use with SetEvent */
    hEvent = CreateEvent(   lpEventAttributes, 
                            bManualReset,
                            bInitialState,
                            lpName );

    if( hEvent == INVALID_HANDLE_VALUE )
    {
        /* ERROR */
        Fail( "ERROR:%lu:CreateEvent() call failed\n", GetLastError() );
    }

    /* close the event handle */
    if( ! CloseHandle( hEvent ) )
    {
        Fail( "ERROR:%lu:CloseHandle() call failed\n", GetLastError() );
    }

    /* try to set the event */
    if( SetEvent( hEvent ) )
    {
        /* ERROR */
        Fail( "FAIL:SetEvent() call succeeded on a closed event handle\n" );
    }
    
    /* verify the result of GetLastError() */
    if( GetLastError() != ERROR_INVALID_HANDLE )
    {
        /* ERROR */
        Fail( "FAIL:SetEvent() call failed on a closed event handle"
                "but returned an unexpected error result %lu\n" );
    }
    


    /* PAL termination */
    PAL_Terminate();

    /* return success */
    return PASS;
}
