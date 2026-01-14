// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <string.h>
#include <sal.h>
#include "pinvokeoverride.h"

const void* callhelpers_pinvoke_override(const char* library_name, const char* entry_point_name);

void add_pinvoke_override()
{
    PInvokeOverride::SetPInvokeOverride(callhelpers_pinvoke_override, PInvokeOverride::Source::RuntimeConfiguration);
}
