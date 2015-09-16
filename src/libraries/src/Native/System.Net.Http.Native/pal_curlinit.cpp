// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_curlinit.h"

#include <pthread.h>
#include <curl/curl.h>

extern "C" int32_t EnsureCurlIsInitialized()
{
    static pthread_mutex_t lock = PTHREAD_MUTEX_INITIALIZER;
    static bool initializationAttempted = false;
    static int32_t errorCode = -1;

    pthread_mutex_lock(&lock);
    {
        if (!initializationAttempted)
        {
            errorCode = curl_global_init(CURL_GLOBAL_ALL);
            initializationAttempted = true;
        }
    }
    pthread_mutex_unlock(&lock);

    return errorCode;
}
