// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump_sentinel.h"

bool g_createdumpLinked = false;

int nativeaot_createdump_main(int, char*[])
{
    return 1;
}
