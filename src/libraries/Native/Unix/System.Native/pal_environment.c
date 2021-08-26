// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_environment.h"

#include <stdlib.h>
#include <string.h>

char* SystemNative_GetEnv(const char* variable)
{
    return getenv(variable);
}

char** SystemNative_GetEnviron()
{
    extern char **environ;
    return environ;
}
