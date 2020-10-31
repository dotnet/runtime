// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source : test.c
**
** Purpose: Test for GlobalMemoryStatusEx() function
**
**
**=========================================================*/

#include <palsuite.h>

PALTEST(miscellaneous_GlobalMemoryStatusEx_test1_paltest_globalmemorystatusex_test1, "miscellaneous/GlobalMemoryStatusEx/test1/paltest_globalmemorystatusex_test1")
{
  
    MEMORYSTATUSEX memoryStatus;

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    if (!GlobalMemoryStatusEx(&memoryStatus))
    {
        Fail("ERROR: GlobalMemoryStatusEx failed.");      
    }

    printf("GlobalMemoryStatusEx:\n");
    printf("    ullTotalPhys: %llu\n", memoryStatus.ullTotalPhys);
    printf("    ullAvailPhys: %llu\n", memoryStatus.ullAvailPhys);
    printf("    ullTotalVirtual: %llu\n", memoryStatus.ullTotalVirtual);
    printf("    ullAvailVirtual: %llu\n", memoryStatus.ullAvailVirtual);
    printf("    ullTotalPageFile: %llu\n", memoryStatus.ullTotalPageFile);
    printf("    ullAvailPageFile: %llu\n", memoryStatus.ullAvailPageFile);
    printf("    ullAvailExtendedVirtual: %llu\n", memoryStatus.ullAvailExtendedVirtual);
    printf("    dwMemoryLoad: %u\n", memoryStatus.dwMemoryLoad);

    if (memoryStatus.ullTotalPhys == 0 ||
        memoryStatus.ullAvailPhys == 0 ||
        memoryStatus.ullTotalVirtual == 0 ||
        memoryStatus.ullAvailVirtual == 0
        )
    {
        Fail("ERROR: GlobalMemoryStatusEx succeeded, but returned zero physical of virtual memory sizes.");      
    }

    PAL_Terminate();
    return PASS;
}
