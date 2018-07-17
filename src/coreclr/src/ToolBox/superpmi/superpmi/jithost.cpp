//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "superpmi.h"
#include "jitinstance.h"
#include "icorjitinfo.h"
#include "jithost.h"

// Look for 'key' as an environment variable named COMPlus_<key>. The returned value
// is nullptr if it is not found, or a string if found. If not nullptr, the returned
// value must be freed with jitInstance.freeLongLivedArray(value).
wchar_t* GetCOMPlusVariable(const wchar_t* key, JitInstance& jitInstance)
{
    static const wchar_t Prefix[]  = W("COMPlus_");
    static const size_t  PrefixLen = (sizeof(Prefix) / sizeof(Prefix[0])) - 1;

    // Prepend "COMPlus_" to the provided key
    size_t   keyLen       = wcslen(key);
    size_t   keyBufferLen = keyLen + PrefixLen + 1;
    wchar_t* keyBuffer =
        reinterpret_cast<wchar_t*>(jitInstance.allocateArray(static_cast<ULONG>(sizeof(wchar_t) * keyBufferLen)));
    wcscpy_s(keyBuffer, keyBufferLen, Prefix);
    wcscpy_s(&keyBuffer[PrefixLen], keyLen + 1, key);

    // Look up the environment variable
    DWORD valueLen = GetEnvironmentVariableW(keyBuffer, nullptr, 0);
    if (valueLen == 0)
    {
        jitInstance.freeArray(keyBuffer);
        return nullptr;
    }

    // Note this value must live as long as the jit instance does.
    wchar_t* value       = reinterpret_cast<wchar_t*>(jitInstance.allocateLongLivedArray(sizeof(wchar_t) * valueLen));
    DWORD    newValueLen = GetEnvironmentVariableW(keyBuffer, value, valueLen);

    jitInstance.freeArray(keyBuffer);
    if (valueLen < newValueLen)
    {
        jitInstance.freeLongLivedArray(value);
        return nullptr;
    }

    return value;
}

JitHost::JitHost(JitInstance& jitInstance) : jitInstance(jitInstance)
{
}

void* JitHost::allocateMemory(size_t size)
{
    return InitIEEMemoryManager(&jitInstance)->ClrVirtualAlloc(nullptr, size, 0, 0);
}

void JitHost::freeMemory(void* block)
{
    InitIEEMemoryManager(&jitInstance)->ClrVirtualFree(block, 0, 0);
}

int JitHost::getIntConfigValue(const wchar_t* key, int defaultValue)
{
    jitInstance.mc->cr->AddCall("getIntConfigValue");

    int result = defaultValue;

    const wchar_t* forceValue = jitInstance.getForceOption(key);

    if (forceValue != nullptr)
    {
        wchar_t* endPtr;
        result         = static_cast<int>(wcstoul(forceValue, &endPtr, 16));
        bool succeeded = (errno != ERANGE) && (endPtr != forceValue);
        if (succeeded)
        {
            return result;
        }
    }

    result = jitInstance.mc->repGetIntConfigValue(key, defaultValue);

    if (result != defaultValue)
    {
        return result;
    }

    // Look for special case keys.
    if (wcscmp(key, W("SuperPMIMethodContextNumber")) == 0)
    {
        return jitInstance.mc->index;
    }

    // If the result is the default value, probe the JIT options and then the environment. If a value is found, parse
    // it as a hex integer.

    const wchar_t* value = jitInstance.getOption(key);

    bool succeeded;
    if (value != nullptr)
    {
        wchar_t* endPtr;
        result    = static_cast<int>(wcstoul(value, &endPtr, 16));
        succeeded = (errno != ERANGE) && (endPtr != value);
    }
    else
    {
        wchar_t* complus = GetCOMPlusVariable(key, jitInstance);
        if (complus == nullptr)
        {
            return defaultValue;
        }
        wchar_t* endPtr;
        result    = static_cast<int>(wcstoul(complus, &endPtr, 16));
        succeeded = (errno != ERANGE) && (endPtr != complus);
        jitInstance.freeLongLivedArray(complus);
    }

    return succeeded ? result : defaultValue;
}

const wchar_t* JitHost::getStringConfigValue(const wchar_t* key)
{
    jitInstance.mc->cr->AddCall("getStringConfigValue");

    const wchar_t* result = nullptr;

    // First check the force options, then mc value. If value is not presented there, probe the JIT options and then the
    // environment.

    result = jitInstance.getForceOption(key);

    if (result == nullptr)
    {
        result = jitInstance.mc->repGetStringConfigValue(key);
    }

    if (result == nullptr)
    {
        result = jitInstance.getOption(key);
        if (result == nullptr)
        {
            return GetCOMPlusVariable(key, jitInstance);
        }
    }

    // Now we need to dup it, so you can call freeStringConfigValue() on what we return.
    size_t   resultLenInChars = wcslen(result) + 1;
    wchar_t* dupResult = (wchar_t*)jitInstance.allocateLongLivedArray((ULONG)(sizeof(wchar_t) * resultLenInChars));
    wcscpy_s(dupResult, resultLenInChars, result);

    return dupResult;
}

void JitHost::freeStringConfigValue(const wchar_t* value)
{
    jitInstance.mc->cr->AddCall("freeStringConfigValue");
    jitInstance.freeLongLivedArray((void*)value);
}
