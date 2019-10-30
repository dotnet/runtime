// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include <windows.h>
#include <stdio.h> 
#include <wchar.h>
#include <assert.h>
#include "processdescriptor.h"

ProcessDescriptor ProcessDescriptor::FromCurrentProcess()
{
    return FromPid(GetCurrentProcessId());
}
