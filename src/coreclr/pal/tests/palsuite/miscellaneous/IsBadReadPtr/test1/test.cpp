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
#include <sys/mman.h>

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
           "Can't properly exec test case without this.");
    }


    /* This should be readable, and return 0 */
    ResultValue = IsBadReadPtr(TestingPointer,MEMORY_AMOUNT);
    if(ResultValue != 0)
    {
        free(TestingPointer);

        Fail("ERROR: The function returned %d instead of 0, when pointing "
             "at readable memory.",ResultValue);
    }

    /* If we pass 0, the result should be 0 as well */
    ResultValue = IsBadReadPtr(TestingPointer,0);
    if(ResultValue != 0)
    {
        free(TestingPointer);

        Fail("ERROR: The function returned %d instead of 0, when the "
             "function was passed a range of 0 bytes.",ResultValue);
    }
    free(TestingPointer); /* we are done with this */

    /* create a readable address */
    TestingPointer = mmap(NULL, 80, PROT_READ, MAP_ANON | MAP_PRIVATE, -1, 0);

    if (TestingPointer == MAP_FAILED )
    {
        Fail("ERROR: call to mmap failed");
    }

    ResultValue = IsBadReadPtr(TestingPointer,16);
    if(ResultValue != 0)
    {
        munmap(TestingPointer, 80);

        Fail("ERROR: The function returned %d instead of 1 when checking "
            "on unreadable memory.",ResultValue);
    }

    munmap(TestingPointer, 80);

    /* create an unreadable address */
    TestingPointer = mmap(NULL, 80, PROT_NONE, MAP_ANON | MAP_PRIVATE, -1, 0);

    if (TestingPointer == MAP_FAILED )
    {
        Fail("ERROR: call to mmap failed");
    }

    ResultValue = IsBadReadPtr(TestingPointer,16);

    if(ResultValue == 0)
    {
        munmap(TestingPointer, 80);

        Fail("ERROR: The function returned %d instead of 1 when checking "
             "on unreadable memory.",ResultValue);
    }

    munmap(TestingPointer, 80);


    /* This should be unreadable and return 1 */
    ResultValue = IsBadReadPtr(NULL,16);
    if(ResultValue != 1)
    {
        Fail("ERROR: The function returned %d instead of 1 when checking "
             "to see if NULL was readable.",ResultValue);
    }

    PAL_Terminate();
    return PASS;
}
