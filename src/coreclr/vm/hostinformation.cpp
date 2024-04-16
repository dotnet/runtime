// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "hostinformation.h"

namespace
{
    host_runtime_contract* s_hostContract = nullptr;
}

HostInformation::~HostInformation()
{
    if (m_simpleFileNameMap != nullptr)
    {
        delete m_simpleFileNameMap;
    }
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

void HostInformation::GetEntryAssemblyName(SString& entryAssemblyName)
{
    if ((s_hostContract == nullptr) || (s_hostContract->entry_assembly == nullptr))
    {
        entryAssemblyName.Set(W(""));
        return;
    }   

    entryAssemblyName.SetUTF8(s_hostContract->entry_assembly);
    entryAssemblyName.Normalize();
}

SimpleNameToFileNameMap* HostInformation::GetHostAssemblyNames()
{
    if (m_simpleFileNameMap != nullptr)
        return m_simpleFileNameMap;

    m_simpleFileNameMap = new SimpleNameToFileNameMap();

    if (s_hostContract == nullptr || s_hostContract->get_assemblies == nullptr)
        return m_simpleFileNameMap;

    char** assemblies;
    uint32_t assemblyCount = 0;
    assemblies = s_hostContract->get_assemblies(assemblyCount, s_hostContract->context);

    if (assemblies == nullptr)
        return m_simpleFileNameMap;

    for (uint32_t i = 0; i < assemblyCount; i++)
    {
        SimpleNameToFileNameMapEntry mapEntry;
        SString assemblyName;

        assemblyName.SetUTF8(assemblies[i]);
        assemblyName.Normalize();

        LPWSTR wszSimpleName = new WCHAR[assemblyName.GetCount() + 1];
        if (wszSimpleName == nullptr)
        {
            continue;
        }
        wcscpy_s(wszSimpleName, assemblyName.GetCount() + 1, assemblyName.GetUnicode());

        mapEntry.m_wszSimpleName = wszSimpleName;
        mapEntry.m_wszILFileName = nullptr;

        m_simpleFileNameMap->AddOrReplace(mapEntry);
    }

    s_hostContract->destroy_assemblies(assemblies, assemblyCount);

    return m_simpleFileNameMap;
}

void HostInformation::ResolveHostAssemblyPath(const SString& simpleName, SString& resolvedPath)
{
    if (s_hostContract == nullptr || s_hostContract->resolve_assembly_to_path == nullptr)
    {
        resolvedPath.Set(simpleName);
        return;
    }

    SString ssUtf8SimpleName(simpleName);
    simpleName.ConvertToUTF8(ssUtf8SimpleName);

    LPCSTR assemblyPath = s_hostContract->resolve_assembly_to_path(ssUtf8SimpleName.GetUTF8(), s_hostContract->context);

    if (assemblyPath == nullptr)
    {
        resolvedPath.Set(simpleName);
    }
    else
    {
        resolvedPath.SetUTF8(assemblyPath);
        resolvedPath.Normalize();
    }
}

LPCSTR HostInformation::ConvertToUTF8(LPCWSTR utf16String)
{
    int length = WideCharToMultiByte(CP_ACP, 0, utf16String, -1, NULL, 0, NULL, NULL);

    if (length <= 0)
        return "";

    char* ret = new char[length];

    if (ret == nullptr)
    {
        return "";
    }

    length = WideCharToMultiByte(CP_ACP, 0, utf16String, -1, ret, length, NULL, NULL);

    return (length > 0) ? ret : "";
}

LPCWSTR HostInformation::ConvertToUnicode(const char* utf8String)
{
    int length = MultiByteToWideChar(CP_UTF8, 0, utf8String, -1, NULL, 0);

    if (length <= 0)
        return L"";

    LPWSTR ret = new WCHAR[length];

    if (ret == nullptr)
    {
        return L"";
    }

    length = MultiByteToWideChar(CP_UTF8, 0, utf8String, -1, ret, length);

    return (length > 0) ? ret : L"";
}
