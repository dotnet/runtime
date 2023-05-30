// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _HOSTINFORMATION_H_
#define _HOSTINFORMATION_H_

#include <corehost/host_runtime_contract.h>

class HostInformation
{
public:
    static void SetContract(_In_ host_runtime_contract* hostContract);
    static bool GetProperty(_In_z_ const char* name, SString& value);
};

#endif // _HOSTINFORMATION_H_
