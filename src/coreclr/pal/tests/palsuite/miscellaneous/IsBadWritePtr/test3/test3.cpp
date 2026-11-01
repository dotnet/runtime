// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test3.c
**
** Purpose:
** Check that IsBadWritePtr returns non-zero on Read-only memory.
**
**
**=========================================================*/

#include <palsuite.h>
#include <minipal/ospagesize.h>
#include <sys/mman.h>

PALTEST(miscellaneous_IsBadWritePtr_test3_paltest_isbadwriteptr_test3, "miscellaneous/IsBadWritePtr/test3/paltest_isbadwriteptr_test3")
{
    LPVOID PageOne;
    size_t pageSize = minipal_getpagesize();

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    PageOne = mmap(NULL, pageSize, PROT_READ, MAP_ANON | MAP_PRIVATE, -1, 0);

    if(PageOne == MAP_FAILED)
    {
        Fail("ERROR: mmap failed to create the required read-only memory.");
    }

    if(IsBadWritePtr(PageOne,pageSize) == 0)
    {
        munmap(PageOne, pageSize);

        Fail("ERROR: IsBadWritePtr returned 0 when checking a section of "
             "read-only memory. It should be non-zero.");
    }

    munmap(PageOne, pageSize);
    PAL_Terminate();
    return PASS;
}
