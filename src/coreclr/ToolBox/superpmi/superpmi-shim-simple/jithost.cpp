// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "runtimedetails.h"
#include "spmiutil.h"
#include "jithost.h"

JitHost* g_ourJitHost;

JitHost::JitHost(ICorJitHost* wrappedHost) : wrappedHost(wrappedHost)
{
}

void* JitHost::allocateMemory(size_t size)
{
    return wrappedHost->allocateMemory(size);
}

void JitHost::freeMemory(void* block)
{
    return wrappedHost->freeMemory(block);
}

int JitHost::getIntConfigValue(const WCHAR* key, int defaultValue)
{
    return wrappedHost->getIntConfigValue(key, defaultValue);
}

const WCHAR* JitHost::getStringConfigValue(const WCHAR* key)
{
    return wrappedHost->getStringConfigValue(key);
}

void JitHost::freeStringConfigValue(const WCHAR* value)
{
    wrappedHost->freeStringConfigValue(value);
}
