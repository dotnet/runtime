// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_environment.h"

#include <minipal/env.h>

char* SystemNative_GetEnv(const char* variable)
{
    return minipal_env_get(variable);
}

char** SystemNative_GetEnviron(void)
{
    return minipal_env_get_environ_copy();
}

void SystemNative_FreeEnviron(char** environ)
{
    minipal_env_free_environ(environ);
}
