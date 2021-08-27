// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_environment.h"

#include <Foundation/Foundation.h>
#include <CoreFoundation/CoreFoundation.h>
#include <objc/runtime.h>
#include <objc/message.h>

char* SystemNative_GetEnv(const char* variable)
{
    return getenv(variable);
}

static char *empty_key_value_pair = "=";

static void get_environ_helper(const void *key, const void *value, void *context)
{
    char ***temp_environ_ptr = (char***)context;
    const char *utf8_key = [(NSString *)key UTF8String];
    const char *utf8_value = [(NSString *)value UTF8String];
    int utf8_key_length = strlen(utf8_key);
    int utf8_value_length = strlen(utf8_value);
    char *key_value_pair;

    key_value_pair = malloc(utf8_key_length + utf8_value_length + 2);
    if (key_value_pair != NULL)
    {
        strcpy(key_value_pair, utf8_key);
        key_value_pair[utf8_key_length] = '=';
        strcpy(key_value_pair + utf8_key_length + 1, utf8_value);
    }
    else
    {
        // In case of failed allocation add pointer to preallocated entry. This is
        // ignored on the managed side and skipped over in SystemNative_FreeEnviron.
        key_value_pair = empty_key_value_pair;
    }

    **temp_environ_ptr = key_value_pair;
    (*temp_environ_ptr)++;
}

char** SystemNative_GetEnviron()
{
    char **temp_environ;
    char **temp_environ_ptr;

    CFDictionaryRef environment = (CFDictionaryRef)[[NSProcessInfo processInfo] environment];
    int count = CFDictionaryGetCount(environment);
    temp_environ = (char **)malloc((count + 1) * sizeof(char *));
    if (temp_environ != NULL)
    {
        temp_environ_ptr = temp_environ;
        CFDictionaryApplyFunction(environment, get_environ_helper, &temp_environ_ptr);
        *temp_environ_ptr = NULL;
    }

    return temp_environ;
}

void SystemNative_FreeEnviron(char** environ)
{
    if (environ != NULL)
    {
        for (char** environ_ptr = environ; *environ_ptr != NULL; environ_ptr++)
        {
            if (*environ_ptr != empty_key_value_pair)
            {
                free(*environ_ptr);
            }
        }

        free(environ);
    }
}
