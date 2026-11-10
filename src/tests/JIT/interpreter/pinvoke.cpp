// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdio.h"
#include <stdlib.h>
#include <platformdefines.h>

#define EXPORT_IT extern "C" DLL_EXPORT

EXPORT_IT void writeToStdout (const char *str)
{
    puts(str);
}

EXPORT_IT int sumTwoInts (int x, int y)
{
    return x + y;
}

EXPORT_IT double sumTwoDoubles (double x, double y)
{
    return x + y;
}
