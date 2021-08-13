// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "native-lib.h"
#include <stdio.h>

int print_line(int x)
{
    printf("print_line: %d\n", x);
    return 42 + x;
}
