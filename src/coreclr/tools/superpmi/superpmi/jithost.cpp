// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "superpmi.h"
#include "jitinstance.h"
#include "icorjitinfo.h"
#include "jithost.h"

#include <clrconfignocache.h>

WCHAR* GetPrefixedEnvironmentVariable(const WCHAR* prefix, size_t prefixLen, const WCHAR* key, JitInstance& jitInstance)
{
    // Prepend prefix to the provided key
    size_t   keyLen       = u16_strlen(key);
    size_t   keyBufferLen = keyLen + prefixLen + 1;
    WCHAR* keyBuffer =
        reinterpret_cast<WCHAR*>(jitInstance.allocateArray(sizeof(WCHAR) * keyBufferLen));
    wcscpy_s(keyBuffer, keyBufferLen, prefix);
    wcscpy_s(&keyBuffer[prefixLen], keyLen + 1, key);

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

// Look for 'key' as an environment variable named DOTNET_<key> and fallback to COMPlus_<key>.
// The returned value is nullptr if it is not found, or a string if found. If not nullptr,
// the returned value must be freed with jitInstance.freeLongLivedArray(value).
char* GetConfigFromEnvironmentVariable(const char* key, JitInstance& jitInstance)
{
    int keyLen = MultiByteToWideChar(CP_UTF8, 0, key, -1, nullptr, 0);
    if (keyLen == 0)
    {
        return nullptr;
    }

    WCHAR* keyUtf8 = new WCHAR[keyLen];
    if (MultiByteToWideChar(CP_UTF8, 0, key, -1, keyUtf8, keyLen) == 0)
    {
        delete[] keyUtf8;
        return nullptr;
    }

    WCHAR* result = GetPrefixedEnvironmentVariable(DOTNET_PREFIX, LEN_OF_DOTNET_PREFIX, keyUtf8, jitInstance);
    if (result == nullptr)
    {
        result = GetPrefixedEnvironmentVariable(COMPLUS_PREFIX, LEN_OF_COMPLUS_PREFIX, keyUtf8, jitInstance);
    }

    delete[] keyUtf8;

    if (result == nullptr)
    {
        return nullptr;
    }

    int resultLen = WideCharToMultiByte(CP_UTF8, 0, result, -1, nullptr, 0, nullptr, nullptr);
    if (resultLen == 0)
    {
        jitInstance.freeLongLivedArray(result);
        return nullptr;
    }

    char* resultUtf8 = (char*)jitInstance.allocateLongLivedArray((size_t)resultLen);

    if (WideCharToMultiByte(CP_UTF8, 0, result, -1, resultUtf8, resultLen, nullptr, nullptr) == 0)
    {
        jitInstance.freeLongLivedArray(result);
        jitInstance.freeLongLivedArray(resultUtf8);
        return nullptr;
    }

    jitInstance.freeLongLivedArray(result);

    return resultUtf8;
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

bool JitHost::convertStringValueToInt(const char* key, const char* stringValue, int& result)
{
    if (stringValue == nullptr)
    {
        return false;
    }

    char*      endPtr;
    unsigned long longResult = strtoul(stringValue, &endPtr, 16);
    bool          succeeded  = (errno != ERANGE) && (endPtr != stringValue) && (longResult <= INT_MAX);
    if (!succeeded)
    {
        LogWarning("Can't convert int config value from string, key: %ws, string value: %ws\n", key, stringValue);
        return false;
    }

    result = static_cast<int>(longResult);
    return true;
}

int JitHost::getIntConfigValue(const char* key, int defaultValue)
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
        if (strcmp(key, "SuperPMIMethodContextNumber") == 0)
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
        char* envVar = GetConfigFromEnvironmentVariable(key, jitInstance);
        valueFound    = convertStringValueToInt(key, envVar, result);
        if (envVar != nullptr)
        {
            jitInstance.freeLongLivedArray(envVar);
        }
    }

    if (valueFound)
    {
        LogDebug("Environment variable %s=%d", key, result);
    }

    return valueFound ? result : defaultValue;
}

const char* JitHost::getStringConfigValue(const char* key)
{
    jitInstance.mc->cr->AddCall("getStringConfigValue");

    bool           needToDup = true;
    const char* result    = nullptr;

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
        result    = GetConfigFromEnvironmentVariable(key, jitInstance);
        needToDup = false;
    }

    if (result != nullptr && needToDup)
    {
        // Now we need to dup it, so you can call freeStringConfigValue() on what we return.
        size_t   resultLenInChars = strlen(result) + 1;
        char* dupResult = (char*)jitInstance.allocateLongLivedArray(sizeof(char) * resultLenInChars);
        strcpy_s(dupResult, resultLenInChars, result);
        result = dupResult;
    }

    if (result != nullptr)
    {
        LogDebug("Environment variable %s=%s", key, result);
    }
    return result;
}

void JitHost::freeStringConfigValue(const char* value)
{
    jitInstance.mc->cr->AddCall("freeStringConfigValue");
    jitInstance.freeLongLivedArray((void*)value);
}
