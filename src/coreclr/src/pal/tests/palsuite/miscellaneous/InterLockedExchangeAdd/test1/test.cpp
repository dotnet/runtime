// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Source : test.c
**
** Purpose: InterlockedExchangeAdd() function
**
**
**=========================================================*/

/*
** The InterlockedExchangeAdd function performs an atomic addition of Value 
** to the value pointed to by Addend. 
** The result is stored in the address specified by Addend. 
** The initial value of the variable pointed to by Addend is returned as the function value.
*/
#include <palsuite.h>

int __cdecl main(int argc, char *argv[]) 
{
    
    LONG TheReturn;
  
    LONG *ptrValue = NULL;
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }



#if defined(BIT64)
    ptrValue = (LONG *) malloc(sizeof(LONG));

    if(ptrValue == NULL)
    {
		Fail("Error:%d:Malloc failed for ptrValue\n", GetLastError()); 
	}

	*ptrValue = (LONG) 0;

    TheReturn = InterlockedExchangeAdd( ptrValue, (LONG) 5);
    

    /* Added, it should be 5  now */
    if(*ptrValue != 5) 
    {
        
      Trace("ERROR: After an add operation, the value should be 5, "
             "but it is really %d.", *ptrValue);
      free(ptrValue);
      Fail("");
    }
  
    /* Check to make sure the function returns the original value (5 in this case) */
    if(TheReturn != 0) 
    {
        Trace("ERROR: The function should have returned the new value of %d "
             "but instead returned %d.", *ptrValue, TheReturn);    
        free(ptrValue);
		Fail("");
    }

    TheReturn = InterlockedExchangeAdd( ptrValue, (LONG) 1);


    /* Added twice, it should be 6  now */
    if(*ptrValue != 6) 
    {
        
      Trace("ERROR: After having two add operations, the value should be 6, "
             "but it is really %d.", *ptrValue);
      free(ptrValue);
      Fail("");
    }
  
    /* Check to make sure the function returns the original value (5 in this case) */
    if(TheReturn != 5) 
    {
        Trace("ERROR: The function should have returned the new value of %d "
             "but instead returned %d.", *ptrValue, TheReturn);    
        free(ptrValue);
		Fail("");
    }
    
    free(ptrValue);
#endif
    PAL_Terminate();
    return PASS; 
} 





