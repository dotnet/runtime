//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//----------------------------------------------------------
// SPMIUtil.cpp - General utility functions
//----------------------------------------------------------

#include "standardpch.h"
#include "logging.h"
#include "spmiutil.h"

bool breakOnDebugBreakorAV = false;

void DebugBreakorAV(int val)
{
    if (IsDebuggerPresent())
    {
        if (val == 0)
            __debugbreak();
        if (breakOnDebugBreakorAV)
            __debugbreak();
    }

    int exception_code = EXCEPTIONCODE_DebugBreakorAV + val;
    // assert((EXCEPTIONCODE_DebugBreakorAV <= exception_code) && (exception_code < EXCEPTIONCODE_DebugBreakorAV_MAX))
    LogException(exception_code, "DebugBreak or AV Exception %d", val);
}

char* GetEnvironmentVariableWithDefaultA(const char* envVarName, const char* defaultValue)
{
    char* retString = nullptr;

    // Figure out how much space we need to allocate
    DWORD dwRetVal = ::GetEnvironmentVariableA(envVarName, nullptr, 0);
    if (dwRetVal != 0)
    {
        retString = new char[dwRetVal];
        dwRetVal  = ::GetEnvironmentVariableA(envVarName, retString, dwRetVal);
    }
    else
    {
        if (defaultValue != nullptr)
        {
            dwRetVal  = (DWORD)strlen(defaultValue) + 1; // add one for null terminator
            retString = new char[dwRetVal];
            memcpy_s(retString, dwRetVal, defaultValue, dwRetVal);
        }
    }

    return retString;
}

WCHAR* GetEnvironmentVariableWithDefaultW(const WCHAR* envVarName, const WCHAR* defaultValue)
{
    WCHAR* retString = nullptr;

    // Figure out how much space we need to allocate
    DWORD dwRetVal = ::GetEnvironmentVariableW(envVarName, nullptr, 0);
    if (dwRetVal != 0)
    {
        retString = new WCHAR[dwRetVal];
        dwRetVal  = ::GetEnvironmentVariableW(envVarName, retString, dwRetVal);
    }
    else
    {
        if (defaultValue != nullptr)
        {
            dwRetVal  = (DWORD)wcslen(defaultValue) + 1; // add one for null terminator
            retString = new WCHAR[dwRetVal];
            memcpy_s(retString, dwRetVal * sizeof(WCHAR), defaultValue, dwRetVal * sizeof(WCHAR));
        }
    }

    return retString;
}

#ifdef FEATURE_PAL
// For some reason, the PAL doesn't have GetCommandLineA(). So write it.
LPSTR GetCommandLineA()
{
    LPSTR  pCmdLine  = nullptr;
    LPWSTR pwCmdLine = GetCommandLineW();

    if (pwCmdLine != nullptr)
    {
        // Convert to ASCII

        int n = WideCharToMultiByte(CP_ACP, 0, pwCmdLine, -1, nullptr, 0, nullptr, nullptr);
        if (n == 0)
        {
            LogError("MultiByteToWideChar failed %d", GetLastError());
            return nullptr;
        }

        pCmdLine = new char[n];

        int n2 = WideCharToMultiByte(CP_ACP, 0, pwCmdLine, -1, pCmdLine, n, nullptr, nullptr);
        if ((n2 == 0) || (n2 != n))
        {
            LogError("MultiByteToWideChar failed %d", GetLastError());
            return nullptr;
        }
    }

    return pCmdLine;
}
#endif // FEATURE_PAL
