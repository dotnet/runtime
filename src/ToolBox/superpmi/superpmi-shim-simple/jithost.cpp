//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

int JitHost::getIntConfigValue(const wchar_t* key, int defaultValue)
{
    return wrappedHost->getIntConfigValue(key, defaultValue);
}

const wchar_t* JitHost::getStringConfigValue(const wchar_t* key)
{
    return wrappedHost->getStringConfigValue(key);
}

void JitHost::freeStringConfigValue(const wchar_t* value)
{
    wrappedHost->freeStringConfigValue(value);
}
