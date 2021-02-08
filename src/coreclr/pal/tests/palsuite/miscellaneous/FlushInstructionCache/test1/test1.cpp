// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test1.c
**
** Purpose: Tests that FlushInstructionCache returns the correct value for a 
**          number of different inputs.
**
**
** Note :
** For this function, what constitutes "invalid parameters" will depend entirely
** on the platform; because of this we can't simply test values on Windows and 
** then ask for the same results everywhere. Because of this, this test can 
** ensure that the function succeeds for some "obviously" valid values, but 
** can't make sure that it fails for invalid values.
**
**=========================================================*/

#include <palsuite.h>

void DoTest(void *Buffer, int Size, int Expected)
{
    int ret;
    
    SetLastError(0);
    ret = FlushInstructionCache(GetCurrentProcess(), Buffer, Size);
    if (!ret && Expected)
    {
        Fail("Expected FlushInstructionCache to return non-zero, got zero!\n"
            "region: %p, size: %d, GetLastError: %d\n", Buffer, Size, 
            GetLastError());
    }
    else if (ret && !Expected)
    {
        Fail("Expected FlushInstructionCache to return zero, got non-zero!\n"
            "region: %p, size: %d, GetLastError: %d\n", Buffer, Size, 
            GetLastError());
    }

    if (!Expected && ERROR_NOACCESS != GetLastError())
    {
        Fail("FlushInstructionCache failed to set the last error to "
            "ERROR_NOACCESS!\n");
    }

}

PALTEST(miscellaneous_FlushInstructionCache_test1_paltest_flushinstructioncache_test1, "miscellaneous/FlushInstructionCache/test1/paltest_flushinstructioncache_test1")
{
    char ValidPtr[256];

    if(PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    /* with valid pointer, zero-size and valid size must succeed */
    DoTest(ValidPtr, 0, 1);
    DoTest(ValidPtr, 42, 1);

    PAL_Terminate();
    return PASS;
}
