// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=====================================================================
**
** Source:  test3.c
**
** Purpose: Tests how QueueUserAPC handles an invalid thread.
**
**
**===================================================================*/

#include <palsuite.h>

PALTEST(threading_QueueUserAPC_test3_paltest_queueuserapc_test3, "threading/QueueUserAPC/test3/paltest_queueuserapc_test3")
{    
    int ret;

    if (0 != (PAL_Initialize(argc, argv)))
    {
        return FAIL;
    }

    ret = QueueUserAPC(NULL, NULL, 0);
    if (ret != 0)
    {
        Fail("QueueUserAPC passed with an invalid thread!\n");
    }

    PAL_Terminate();
    return PASS;
}
