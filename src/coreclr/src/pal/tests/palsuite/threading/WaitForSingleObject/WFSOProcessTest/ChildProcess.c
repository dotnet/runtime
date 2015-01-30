//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

int __cdecl main(int argc, char **argv)
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








