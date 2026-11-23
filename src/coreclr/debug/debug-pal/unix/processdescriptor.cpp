// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>
#include <unistd.h>
#include <minipal/thread.h>
#include <minipal/transportname.h>
#include "processdescriptor.h"

ProcessDescriptor ProcessDescriptor::FromCurrentProcess()
{
    return Create(minipal_get_current_process_id(), nullptr);
}

void GetTransportPipeName(
    char *name,
    DWORD id,
    const char *applicationGroupId,
    const char *suffix)
{
    minipal_get_transport_name(
        name,
        MAX_DEBUGGER_TRANSPORT_PIPE_NAME_LENGTH,
        "clr-debug-pipe",
        id,
        applicationGroupId,
        suffix);
}
