// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "superpmi.h"
#include "jitinstance.h"
#include "icorjitinfo.h"
#include "jithost.h"

// Look for 'key' as an environment variable named COMPlus_<key>. The returned value
// is nullptr if it is not found, or a string if found. If not nullptr, the returned
// value must be freed with jitInstance.freeLongLivedArray(value).
WCHAR* GetCOMPlusVariable(const WCHAR* key, JitInstance& jitInstance)
{
    static const WCHAR Prefix[]  = W("COMPlus_");
    static const size_t  PrefixLen = (sizeof(Prefix) / sizeof(Prefix[0])) - 1;

    // Prepend "COMPlus_" to the provided key
    size_t   keyLen       = wcslen(key);
    size_t   keyBufferLen = keyLen + PrefixLen + 1;
    WCHAR* keyBuffer =
        reinterpret_cast<WCHAR*>(jitInstance.allocateArray(sizeof(WCHAR) * keyBufferLen));
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
    WCHAR* value       = reinterpret_cast<WCHAR*>(jitInstance.allocateLongLivedArray(sizeof(WCHAR) * valueLen));
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
    return jitInstance.allocateLongLivedArray(size);
}

void JitHost::freeMemory(void* block)
{
    jitInstance.freeLongLivedArray((void*)block);
}

bool JitHost::convertStringValueToInt(const WCHAR* key, const WCHAR* stringValue, int& result)
{
    if (stringValue == nullptr)
    {
        return false;
    }

    WCHAR*      endPtr;
    unsigned long longResult = wcstoul(stringValue, &endPtr, 16);
    bool          succeeded  = (errno != ERANGE) && (endPtr != stringValue) && (longResult <= INT_MAX);
    if (!succeeded)
    {
        LogWarning("Can't convert int config value from string, key: %ws, string value: %ws\n", key, stringValue);
        return false;
    }

    result = static_cast<int>(longResult);
    return true;
}

int JitHost::getIntConfigValue(const WCHAR* key, int defaultValue)
{
    jitInstance.mc->cr->AddCall("getIntConfigValue");

    // First check the force options, then the mc value. If value is not presented there, probe the JIT options, then
    // check
    // the special cases and then the  environment.

    int result = defaultValue;

    bool valueFound;

    valueFound = convertStringValueToInt(key, jitInstance.getForceOption(key), result);

    if (!valueFound)
    {
        // Right now we can't distinguish between the default value that was set explicitly and the default value
        // from the key that was not set. See comments in CLRConfig::GetConfigValue.
        result = jitInstance.mc->repGetIntConfigValue(key, defaultValue);
        if (result != defaultValue)
        {
            valueFound = true;
        }
    }

    if (!valueFound)
    {
        // Look for special case keys.
        if (wcscmp(key, W("SuperPMIMethodContextNumber")) == 0)
        {
            result     = jitInstance.mc->index;
            valueFound = true;
        }
    }

    if (!valueFound)
    {
        valueFound = convertStringValueToInt(key, jitInstance.getOption(key), result);
    }

    if (!valueFound)
    {
        WCHAR* complusVar = GetCOMPlusVariable(key, jitInstance);
        valueFound          = convertStringValueToInt(key, complusVar, result);
        if (complusVar != nullptr)
        {
            jitInstance.freeLongLivedArray(complusVar);
        }
    }

    if (valueFound)
    {
        LogDebug("Environment variable %ws=%d", key, result);
    }

    return valueFound ? result : defaultValue;
}

const WCHAR* JitHost::getStringConfigValue(const WCHAR* key)
{
    jitInstance.mc->cr->AddCall("getStringConfigValue");

    bool           needToDup = true;
    const WCHAR* result    = nullptr;

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
    }

    if (result == nullptr)
    {
        result    = GetCOMPlusVariable(key, jitInstance);
        needToDup = false;
    }

    if (result != nullptr && needToDup)
    {
        // Now we need to dup it, so you can call freeStringConfigValue() on what we return.
        size_t   resultLenInChars = wcslen(result) + 1;
        WCHAR* dupResult = (WCHAR*)jitInstance.allocateLongLivedArray(sizeof(WCHAR) * resultLenInChars);
        wcscpy_s(dupResult, resultLenInChars, result);
        result = dupResult;
    }

    if (result != nullptr)
    {
        LogDebug("Environment variable %ws=%ws", key, result);
    }
    return result;
}

void JitHost::freeStringConfigValue(const WCHAR* value)
{
    jitInstance.mc->cr->AddCall("freeStringConfigValue");
    jitInstance.freeLongLivedArray((void*)value);
}
