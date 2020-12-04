// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: test.c
**
** Purpose: Test for CGroup
**
**
**  Steps to run this test on ubuntu:
**  1. sudo apt-get install cgroup-bin
**  2. sudo vi /etc/default/grub
**     Add cgroup_enable=memory swapaccount=1 to GRUB_CMDLINE_LINUX_DEFAULT
**  3. sudo update-grub
**  4. reboot
**  5. sudo cgcreate -g cpu,memory:/myGroup -a <username>:<username> -t <username>:<username>
**  6. echo 4M > /sys/fs/cgroup/memory/mygroup/memory.limit_in_bytes
**  7. echo 4M > /sys/fs/cgroup/memory/mygroup/memory.memsw.limit_in_bytes
**  8. cgexe -g memory:/mygroup --sticky <application>
**=========================================================*/

#include <palsuite.h>

PALTEST(miscellaneous_CGroup_test1_paltest_cgroup_test1, "miscellaneous/CGroup/test1/paltest_cgroup_test1")
{

    /*
     * Initialize the PAL and return FAILURE if this fails
     */

    if(0 != (PAL_Initialize(argc, argv)))
    {
      return FAIL;
    }

    size_t mem_limit = PAL_GetRestrictedPhysicalMemoryLimit();

    FILE* file = fopen("/sys/fs/cgroup/memory/mygroup/memory.limit_in_bytes", "r");
    if(file != NULL)
    {
        if(mem_limit != 4194304)
            Fail("Memory limit obtained from PAL_GetRestrictedPhysicalMemory is not 4MB\n");
        fclose(file); 
    }

    PAL_Terminate();
    return PASS;
}



