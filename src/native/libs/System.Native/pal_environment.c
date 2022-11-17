// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_environment.h"

#include <stdlib.h>
#include <string.h>
#if HAVE_NSGETENVIRON
#include <crt_externs.h>
#endif

char* SystemNative_GetEnv(const char* variable)
{
    return getenv(variable);
}

char** SystemNative_GetEnviron(void)
{
#if HAVE_NSGETENVIRON
    return *(_NSGetEnviron());
#else
    extern char **environ;
    return environ;
#endif
}

void SystemNative_FreeEnviron(char** environ)
{
    // no op
    (void)environ;
}
