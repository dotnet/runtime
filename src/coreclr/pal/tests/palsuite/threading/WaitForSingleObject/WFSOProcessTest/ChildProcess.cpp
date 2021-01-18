// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: ChildProcess.c 
**
** Purpose: Dummy Process which does some work on which the Main Test case waits
**			
** 

**
**=========================================================*/



#include <palsuite.h>

PALTEST(threading_WaitForSingleObject_WFSOProcessTest_paltest_waitforsingleobject_wfsoprocesstest_child, "threading/WaitForSingleObject/WFSOProcessTest/paltest_waitforsingleobject_wfsoprocesstest_child")
{

//Declare local variables
int i =0;

	

//Initialize PAL 
if(0 != (PAL_Initialize(argc, argv)))
    {
        return ( FAIL ); 
    }

//Do some work
for (i=0; i<100000; i++);

Trace("Counter Value was incremented to %d \n ",i); 

PAL_Terminate();
return ( PASS );

}








