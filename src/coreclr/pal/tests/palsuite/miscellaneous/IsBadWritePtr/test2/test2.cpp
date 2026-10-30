// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test2.c
**
** Purpose:
** Create three consecutive pages, NOACCESS, READWRITE and
** NOACCESS. Check to ensure that the READWRITE page returns 0, to
** ensure that IsBadWritePtr isn't overflowing. Also check the other two
** pages to see that they return non-zero.
**
**
**=========================================================*/

#include <palsuite.h>
#include <minipal/ospagesize.h>
#include <sys/mman.h>

PALTEST(miscellaneous_IsBadWritePtr_test2_paltest_isbadwriteptr_test2, "miscellaneous/IsBadWritePtr/test2/paltest_isbadwriteptr_test2")
{
    BYTE* baseAddress;
    LPVOID PageOne;
    LPVOID PageTwo;
    LPVOID PageThree;
    size_t pageSize = minipal_getpagesize();

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    baseAddress = static_cast<BYTE*>(mmap(NULL, pageSize * 4, PROT_NONE, MAP_ANON | MAP_PRIVATE, -1, 0));
    if(baseAddress == MAP_FAILED)
    {
        Fail("ERROR: mmap failed to reserve the required memory.");
    }

    PageOne = baseAddress;
    PageTwo = baseAddress + pageSize;
    PageThree = baseAddress + (2 * pageSize);

    if(mprotect(PageTwo, pageSize, PROT_READ | PROT_WRITE) != 0)
    {
        munmap(baseAddress, pageSize * 4);
        Fail("ERROR: mprotect failed to make the middle page writable.");
    }

    if(IsBadWritePtr(PageOne,pageSize) == 0)
    {
        munmap(baseAddress, pageSize * 4);

        Fail("ERROR: Called IsBadWritePtr on a page which was set NOACCESS "
             "but the return value was 0, indicating that the memory is "
             "writable.");
    }

    if(IsBadWritePtr(PageTwo,pageSize) != 0)
    {
        munmap(baseAddress, pageSize * 4);

        Fail("ERROR: IsBadWritePtr didn't return 0 when called on a "
             "page which should have been writable.");
    }

    if(IsBadWritePtr(PageThree,pageSize) == 0)
    {
        munmap(baseAddress, pageSize * 4);

        Fail("ERROR: Called IsBadWritePtr on a page which was set NOACCESS "
             "but the return value was 0, indicating that the memory is "
             "writable.");
    }

    munmap(baseAddress, pageSize * 4);
    PAL_Terminate();
    return PASS;
}
