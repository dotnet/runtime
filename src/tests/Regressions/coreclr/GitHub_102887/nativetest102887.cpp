// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

#include <dispatch/dispatch.h>
#include <dispatch/queue.h>

extern "C" DLL_EXPORT void StartDispatchQueueThread(void (*work)(void* args))
{
    dispatch_queue_global_t q = dispatch_get_global_queue(QOS_CLASS_UTILITY, 0);
    dispatch_async_f(q, NULL, work);
}
