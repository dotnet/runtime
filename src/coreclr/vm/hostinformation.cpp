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
    char* dest = value.OpenUTF8Buffer(static_cast<COUNT_T>(len) - 1); // OpenUTF8Buffer already includes a byte for the null terminator
    // get_runtime_property returns the length including a null terminator
    size_t lenActual = s_hostContract.get_runtime_property(name, dest, len, s_hostContract.context);

    // Doesn't exist or failed to get property
    if (lenActual == (size_t)-1 || lenActual == 0)
    {
        value.CloseBuffer(0);
        return false;
    }

    if (lenActual <= len)
    {
        value.CloseBuffer(static_cast<COUNT_T>(lenActual) - 1);
        return true;
    }

    value.CloseBuffer();

    // Buffer was not large enough
    len = lenActual;
    dest = value.OpenUTF8Buffer(static_cast<COUNT_T>(len) - 1); // OpenUTF8Buffer already includes a byte for the null terminator
    lenActual = s_hostContract.get_runtime_property(name, dest, len, s_hostContract.context);

    if (lenActual == (size_t)-1 || lenActual == 0 || lenActual > len)
    {
        value.CloseBuffer(0);
        return false;
    }

    value.CloseBuffer(static_cast<COUNT_T>(lenActual) - 1);
    return true;
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

bool HostInformation::GetNativeCodeData(_In_ const SString& assemblyPath, _In_z_ const char* ownerCompositeName, _Out_ void** header, _Out_ size_t* image_size, _Out_ void** image_base)
{
    _ASSERT(header != nullptr);
    _ASSERT(image_base != nullptr);
    _ASSERT(image_size != nullptr);

    size_t requiredSize = offsetof(host_runtime_contract, get_native_code_data) + sizeof(s_hostContract.get_native_code_data);
    if (s_hostContract.size < requiredSize || s_hostContract.get_native_code_data == nullptr)
        return false;

    StackSString utf8Path;
    utf8Path.SetAndConvertToUTF8(assemblyPath.GetUnicode());
    host_runtime_contract_native_code_context context
    {
        sizeof(host_runtime_contract_native_code_context),
        utf8Path.GetUTF8(),
        ownerCompositeName
    };
    host_runtime_contract_native_code_data data = { sizeof(host_runtime_contract_native_code_data) };
    if (!s_hostContract.get_native_code_data(&context, &data))
        return false;

    if (data.r2r_header_ptr == nullptr || data.image_size == 0 || data.image_base == nullptr)
        return false;

    _ASSERT(data.size >= offsetof(host_runtime_contract_native_code_data, image_base) + sizeof(data.image_base));
    *header = data.r2r_header_ptr;
    *image_size = data.image_size;
    *image_base = data.image_base;
    return true;
}
