// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
** Source: CreateProcessW/test3/test3_child.cpp
**
** Purpose: Child process for CreateProcessW/test3. Launched by the parent
** via PATH resolution to verify that directories in the PATH are skipped
** when searching for an executable.
**
**============================================================*/

#define UNICODE
#include <palsuite.h>

PALTEST(threading_CreateProcessW_test3_paltest_createprocessw_test3_child, "threading/CreateProcessW/test3/paltest_createprocessw_test3_child")
{
    if (0 != PAL_Initialize(argc, argv))
    {
        return FAIL;
    }

    PAL_Terminate();
    return PASS;
}
