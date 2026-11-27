// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifdef TARGET_WINDOWS
#include <windows.h>
#else
#include <stdlib.h>
#endif

#include "ProcessEnvironment.h"

#ifndef TARGET_WINDOWS
#include "gcenv.base.h"
#endif

#include <eventpipe/ep-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-types.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-stack-contents.h>
#include <eventpipe/ep-rt.h>

#include "ds-rt-aot.h"

uint32_t
ds_rt_aot_set_environment_variable (const ep_char16_t *name, const ep_char16_t *value)
{
    uint32_t error = ProcessEnvironment_SetVariable((const CHAR16_T*)name, (const CHAR16_T*)value);
    return HRESULT_FROM_WIN32(error);
}

#endif /* ENABLE_PERFTRACING */
