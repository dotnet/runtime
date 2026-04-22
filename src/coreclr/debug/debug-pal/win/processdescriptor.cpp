// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <windows.h>
#include <wchar.h>
#include "processdescriptor.h"

ProcessDescriptor ProcessDescriptor::FromCurrentProcess()
{
    return FromPid(GetCurrentProcessId());
}
