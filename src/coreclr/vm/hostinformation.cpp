// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "hostinformation.h"

namespace
{
    host_runtime_contract s_hostContract = {};
}

void HostInformation::SetContract(_In_ host_runtime_contract* hostContract)
{
    _ASSERTE(s_hostContract.size == 0 && hostContract != nullptr);

    // Copy the contract values
    s_hostContract = *hostContract;
}

bool HostInformation::GetProperty(_In_z_ const char* name, SString& value)
{
    if (s_hostContract.get_runtime_property == nullptr)
        return false;

    size_t len = MAX_PATH + 1;
    char* dest = value.OpenUTF8Buffer(static_cast<COUNT_T>(len));
    size_t lenActual = s_hostContract.get_runtime_property(name, dest, len, s_hostContract.context);
    value.CloseBuffer();

    // Doesn't exist or failed to get property
    if (lenActual == (size_t)-1 || lenActual == 0)
        return false;

    if (lenActual <= len)
        return true;

    // Buffer was not large enough
    len = lenActual;
    dest = value.OpenUTF8Buffer(static_cast<COUNT_T>(len));
    lenActual = s_hostContract.get_runtime_property(name, dest, len, s_hostContract.context);
    value.CloseBuffer();

    return lenActual > 0 && lenActual <= len;
}

bool HostInformation::HasExternalProbe()
{
    size_t requiredSize = offsetof(host_runtime_contract, external_assembly_probe) + sizeof(s_hostContract.external_assembly_probe);
    return s_hostContract.size >= requiredSize && s_hostContract.external_assembly_probe != nullptr;
}

bool HostInformation::ExternalAssemblyProbe(_In_ const SString& path, _Out_ void** data, _Out_ int64_t* size)
{
    if (!HasExternalProbe())
        return false;

    StackSString utf8Path;
    utf8Path.SetAndConvertToUTF8(path.GetUnicode());
    return s_hostContract.external_assembly_probe(utf8Path.GetUTF8(), data, size);
}
