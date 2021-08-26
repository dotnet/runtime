// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"
#include "pal_environment.h"

#include <Foundation/Foundation.h>
#include <CoreFoundation/CoreFoundation.h>
#include <objc/runtime.h>
#include <objc/message.h>

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

    **temp_environ_ptr = key_value_pair;
    (*temp_environ_ptr)++;
}

char** GetEnvironApple()
{
    static char **environ;

    // NOTE: This function is not thread-safe and it leaks one array per process. This is
    // intentional behavior and the managed code is expected to take additional guards
    // around this call.
    if (environ == NULL)
    {
        int environ_size = 1;
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
            environ = temp_environ;
        }
    }

    return environ;
}