// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Source: test2.c
**
** Purpose: Test to ensure GetProcessTimes works properly.
** 
** Dependencies: PAL_Initialize
**               PAL_Terminate
**               Fail
**               ZeroMemory
**               CompareFileTime
**               GetLastError
** 

**
**===========================================================================*/
#include <palsuite.h>


int __cdecl main( int argc, char **argv ) 

{
    int i, j, k;
    int *total;
    
    HANDLE hProcess;    
    FILETIME createTime;
    FILETIME exitTime;
    FILETIME kernelTime1;
    FILETIME userTime1;
    FILETIME kernelTime2;
    FILETIME userTime2;

    DWORD dwError;
    
    /* initialize the PAL */
    if( PAL_Initialize(argc, argv) != 0 )
    {
	    return( FAIL );
    }

    /* get our own process handle */
    hProcess = GetCurrentProcess();
    if( hProcess == NULL )
    {
        Fail(   "GetCurrentProcess() returned a NULL handle.\n" );
    }
    
    /* zero our time structures */
    ZeroMemory( &createTime, sizeof(createTime) );
    ZeroMemory( &exitTime, sizeof(exitTime) );
    ZeroMemory( &kernelTime1, sizeof(kernelTime1) );
    ZeroMemory( &userTime1, sizeof(userTime1) );
    ZeroMemory( &kernelTime2, sizeof(kernelTime2) );
    ZeroMemory( &userTime2, sizeof(userTime2) );

    /* check the process times for the child process */
    if( ! GetProcessTimes(  hProcess,
                            &createTime,
                            &exitTime,
                            &kernelTime1,
                            &userTime1 ) )
    {
        dwError = GetLastError();
        Fail( "GetProcessTimes() call failed with error code %d\n",
              dwError ); 
    }


    /* simulate some activity */
    for( i=0; i<1000; i++ )
    {
        for( j=0; j<1000; j++ )
        {
            /* do kernel work to increase system usage counters */
            total = (int*)malloc(1024 * 1024);

            *total = j * i;
            for( k=0; k<1000; k++ )
            {
                *total += k + i;
            }

            free(total);
        }
    }

    /* check the process times for the child process */
    if( ! GetProcessTimes(  hProcess,
                            &createTime,
                            &exitTime,
                            &kernelTime2,
                            &userTime2 ) )
    {
        dwError = GetLastError();
        Fail( "GetProcessTimes() call failed with error code %d\n",
              dwError ); 
    }


    /* very simple logical checking of the results */
    if( CompareFileTime( &kernelTime1, &kernelTime2 ) > 0 )
    {
        Fail( "Unexpected kernel time value reported.\n" );
    }
    
    if( CompareFileTime( &userTime1, &userTime2 ) > 0 )
    {
        Fail( "Unexpected user time value reported.\n" );
    }
        

    /* terminate the PAL */
    PAL_Terminate();
    
    /* return success */
    return PASS; 
}
