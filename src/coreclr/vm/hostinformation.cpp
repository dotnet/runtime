// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "hostinformation.h"

namespace
{
    host_runtime_contract* s_hostContract = nullptr;
}

void HostInformation::SetContract(_In_ host_runtime_contract* hostContract)
{
    _ASSERTE(s_hostContract == nullptr);
    s_hostContract = hostContract;
}

bool HostInformation::GetProperty(_In_z_ const char* name, SString& value)
{
    if (s_hostContract == nullptr || s_hostContract->get_runtime_property == nullptr)
        return false;

    size_t len = MAX_PATH + 1;
    char* dest = value.OpenUTF8Buffer(static_cast<COUNT_T>(len));
    size_t lenActual = s_hostContract->get_runtime_property(name, dest, len, s_hostContract->context);
    value.CloseBuffer();

    // Doesn't exist or failed to get property
    if (lenActual == (size_t)-1 || lenActual == 0)
        return false;

    if (lenActual <= len)
        return true;

    // Buffer was not large enough
    len = lenActual;
    dest = value.OpenUTF8Buffer(static_cast<COUNT_T>(len));
    lenActual = s_hostContract->get_runtime_property(name, dest, len, s_hostContract->context);
    value.CloseBuffer();

    return lenActual > 0 && lenActual <= len;
}
