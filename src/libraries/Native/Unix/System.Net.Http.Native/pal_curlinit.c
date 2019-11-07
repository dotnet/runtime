// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_curlinit.h"

#include <pthread.h>
#include <stdbool.h>
#include <curl/curl.h>

int32_t HttpNative_EnsureCurlIsInitialized(void)
{
    static pthread_mutex_t lock = PTHREAD_MUTEX_INITIALIZER;
    static bool initializationAttempted = false;
    static int32_t errorCode = -1;

    pthread_mutex_lock(&lock);
    {
        if (!initializationAttempted)
        {
            errorCode = (int32_t)(curl_global_init(CURL_GLOBAL_ALL));
            initializationAttempted = true;
        }
    }
    pthread_mutex_unlock(&lock);

    return errorCode;
}
