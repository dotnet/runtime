// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _HOSTINFORMATION_H_
#define _HOSTINFORMATION_H_

#include <corehost/host_runtime_contract.h>
#include "simplefilenamemap.h"
#include "sstring.h"

class HostInformation
{
public:
    static HostInformation& Instance()
    {
        static HostInformation instance;
        return instance;
    }

    ~HostInformation();

    void SetContract(_In_ host_runtime_contract* hostContract);
    bool GetProperty(_In_z_ const char* name, SString& value);
    void GetEntryAssemblyName(/*Out*/ SString& entryAssemblyName);

    SimpleNameToFileNameMap* GetHostAssemblyNames();
    void ResolveHostAssemblyPath(const SString& simpleName, /*Out*/ SString& resolvedPath);

private:
    HostInformation() {} // Private constructor to prevent instantiation
    HostInformation(const HostInformation&) = delete; // Delete copy constructor
    HostInformation& operator=(const HostInformation&) = delete; // Delete assignment operator

    LPCSTR ConvertToUTF8(LPCWSTR utf16String);
    LPCWSTR ConvertToUnicode(const char* utf8String);

private:
    SimpleNameToFileNameMap* m_simpleFileNameMap;
};

#endif // _HOSTINFORMATION_H_
