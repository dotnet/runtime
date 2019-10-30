//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "standardpch.h"
#include "runtimedetails.h"
#include "spmiutil.h"
#include "jithost.h"

JitHost* g_ourJitHost;

JitHost::JitHost(ICorJitHost* wrappedHost, MethodContext* methodContext) : wrappedHost(wrappedHost), mc(methodContext)
{
}

void JitHost::setMethodContext(MethodContext* methodContext)
{
    this->mc = methodContext;
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
    mc->cr->AddCall("getIntConfigValue");
    int result = wrappedHost->getIntConfigValue(key, defaultValue);

    // The JIT eagerly asks about every config value. If we store all these
    // queries, it takes almost half the MC file space. So only store the
    // non-default answers.
    if (result != defaultValue)
    {
        mc->recGetIntConfigValue(key, defaultValue, result);
    }
    return result;
}

const WCHAR* JitHost::getStringConfigValue(const WCHAR* key)
{
    mc->cr->AddCall("getStringConfigValue");
    const WCHAR* result = wrappedHost->getStringConfigValue(key);

    // Don't store null returns, which is the default
    if (result != nullptr)
    {
        mc->recGetStringConfigValue(key, result);
    }
    return result;
}

void JitHost::freeStringConfigValue(const WCHAR* value)
{
    mc->cr->AddCall("freeStringConfigValue");
    wrappedHost->freeStringConfigValue(value);
}
