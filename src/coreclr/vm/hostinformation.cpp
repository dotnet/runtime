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

LPCWSTR HostInformation::GetEntryAssembly()
{
    if ((s_hostContract == nullptr) || (s_hostContract->entry_assembly == nullptr))
        return L"";

    LPCWSTR entryAssembly = ConvertToUnicode(s_hostContract->entry_assembly);

    size_t len = wcslen(entryAssembly) + 1;
    LPWSTR ret = new WCHAR[len];

    if (ret == nullptr)
    {
        return L"";
    }

    wcscpy_s(ret, len, entryAssembly);
    return ret;
}

SimpleNameToFileNameMap* HostInformation::GetHostAssemblyNames()
{
    if (m_simpleFileNameMap != nullptr)
        return m_simpleFileNameMap;

    m_simpleFileNameMap = new SimpleNameToFileNameMap();

    if (s_hostContract == nullptr || s_hostContract->get_assemblies == nullptr)
        return m_simpleFileNameMap;

    const host_runtime_assemblies* assemblies;
    assemblies = s_hostContract->get_assemblies(s_hostContract->context);

    if (assemblies == nullptr)
        return m_simpleFileNameMap;

    for (uint32_t i = 0; i < assemblies->assembly_count; i++)
    {
        LPCWSTR assemblyName = ConvertToUnicode(assemblies->assembly_names[i]);
        if (assemblyName != nullptr)
        {
            size_t len = wcslen(assemblyName) + 1;
            LPWSTR wszSimpleName = new WCHAR[len];

            if (wszSimpleName != nullptr)
            {
                wcscpy_s(wszSimpleName, len, assemblyName);

                SimpleNameToFileNameMapEntry mapEntry;
                mapEntry.m_wszSimpleName = wszSimpleName;
                mapEntry.m_wszILFileName = nullptr;

                m_simpleFileNameMap->AddOrReplace(mapEntry);
            }
        }
    }

    return m_simpleFileNameMap;
}

LPCWSTR HostInformation::ResolveHostAssemblyPath(LPCWSTR simpleName)
{
    LPCWSTR ret;

    if (s_hostContract == nullptr || s_hostContract->resolve_assembly_to_path == nullptr)
        return simpleName;

    LPCSTR utf8SimpleName = ConvertToUTF8(simpleName);

    if (utf8SimpleName == nullptr)
        return simpleName;

    LPCSTR assemblyPath = s_hostContract->resolve_assembly_to_path(utf8SimpleName, s_hostContract->context);

    if (assemblyPath == nullptr)
    {
        ret = simpleName;
    }
    else
    {
        ret = ConvertToUnicode(assemblyPath);
    }

    delete utf8SimpleName;

    return ret;
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
