// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test.c
**
** Purpose: IsBadReadPtr() function
**
**
**=========================================================*/

#include <palsuite.h>

#define MEMORY_AMOUNT 16

PALTEST(miscellaneous_IsBadReadPtr_test1_paltest_isbadreadptr_test1, "miscellaneous/IsBadReadPtr/test1/paltest_isbadreadptr_test1")
{
    LPVOID TestingPointer = NULL;
    BOOL ResultValue = 0;
  
    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }
  
    TestingPointer = malloc(MEMORY_AMOUNT); 
    if ( TestingPointer == NULL )
    {
        Fail("ERROR: Failed to allocate memory for TestingPointer pointer. "
           "Can't properly exec test case without this.\n");
    }
 

    /* This should be readable, and return 0 */
    ResultValue = IsBadReadPtr(TestingPointer,MEMORY_AMOUNT);
    if(ResultValue != 0) 
    {
        free(TestingPointer);

        Fail("ERROR: The function returned %d instead of 0, when pointing "
             "at readable memory.\n",ResultValue);    
    }

    /* If we pass 0, the result should be 0 as well */
    ResultValue = IsBadReadPtr(TestingPointer,0);
    if(ResultValue != 0) 
    {
        free(TestingPointer);

        Fail("ERROR: The function returned %d instead of 0, when the "
             "function was passed a range of 0 bytes.\n",ResultValue);
    }
    free(TestingPointer); /* we are done with this */

    /* create a READABLE address */
    TestingPointer =  VirtualAlloc(
        NULL,               /* system selects address */
        80,                 /* size of allocation*/
        MEM_COMMIT,         /* commit */
        PAGE_READONLY);     /* protection = read only */

    if (TestingPointer == NULL )
    {
        Fail("ERROR: call to VirtualAlloc failed\n");
    }

    ResultValue = IsBadReadPtr(TestingPointer,16);
    if(ResultValue != 0)    /* if no access */
    {
        if(!VirtualFree(TestingPointer, 0, MEM_RELEASE))
        {
            Trace("ERROR: Call to VirtualFree failed with error"
                " code[ %u ]\n",GetLastError());  
        }

        Fail("ERROR: The function returned %d instead of 1 when checking "
            "on unreadable memory.\n",ResultValue);
    }

    if(!VirtualFree(TestingPointer,0, MEM_RELEASE))
    {
        Fail("ERROR: Call to VirtualFree failed with error"
            " code[ %u ]\n",GetLastError());  
    }

    /* create an unreadable address */
    TestingPointer =  VirtualAlloc(
        NULL,                 /* system selects address */
        80,                   /* size of allocation */
        MEM_COMMIT,           /* commit */
        PAGE_NOACCESS);       /* protection = no access */

    if (TestingPointer == NULL )
    {
        Fail("ERROR: call to VirtualAlloc failed\n");
    }

    ResultValue = IsBadReadPtr(TestingPointer,16);

    if(ResultValue == 0) /* if access */ 
    {
        if(!VirtualFree(TestingPointer, 0, MEM_RELEASE))
        {
            Trace("ERROR: Call to VirtualFree failed with error"
                " code[ %u ]\n",GetLastError());  
        }

        Fail("ERROR: The function returned %d instead of 1 when checking "
             "on unreadable memory.\n",ResultValue);
    }

    if(!VirtualFree(TestingPointer,0, MEM_RELEASE))
    {
        Fail("ERROR: Call to VirtualFree failed with error"
            " code[ %u ]\n",GetLastError());  
    }


    /* This should be unreadable and return 1 */
    ResultValue = IsBadReadPtr(NULL,16);
    if(ResultValue != 1) 
    {
        Fail("ERROR: The function returned %d instead of 1 when checking "
             "to see if NULL was readable.\n",ResultValue);
    }

    PAL_Terminate();
    return PASS;
}



