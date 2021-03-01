// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <pthread.h>

#define NAMELEN 256
char* GetThreadName()
{
    char* threadName = (char*)malloc(sizeof(char) * NAMELEN);
    pthread_t thread = pthread_self();
    int rc = pthread_getname_np(thread, threadName, NAMELEN);
    if (rc != 0)
    {
        free(threadName);
        return NULL;
    }

    return threadName;
}