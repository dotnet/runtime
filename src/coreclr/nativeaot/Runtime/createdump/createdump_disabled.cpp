// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "createdump_sentinel.h"

bool g_createdumpLinked = false;

bool nativeaot_createdump(int, char*[], int*)
{
    return false;
}
