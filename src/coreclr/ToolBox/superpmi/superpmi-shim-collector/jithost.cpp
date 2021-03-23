// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "standardpch.h"
#include "runtimedetails.h"
#include "spmiutil.h"
#include "jithost.h"

JitHost* g_ourJitHost;

// RecordVariable: return `true` if the given COMPlus variable `key` should be recorded
// in the method context.
bool RecordVariable(const WCHAR* key)
{
    // Special-case: we don't want to store some COMPlus variables during
    // collections that we don't want to see on replay:
    //   COMPlus_JitName -- used to get the VM to load the SuperPMI collection shim
    //                      without requiring the shim to overwrite the original JIT.
    //                      This JIT doesn't care about this on SuperPMI replay, but
    //                      we don't need to waste the space in the MC file storing it.
    //   COMPlus_AltJitName -- if collecting with an altjit, this is set. The JIT doesn't
    //                      use this on replay, but it doesn't need to be stored.
    //   COMPlus_EnableExtraSuperPmiQueries -- used to force the JIT to ask additional
    //                      questions during SuperPMI collection. We don't want to store
    //                      this variable because we don't want to replay using it.

    if ((_wcsicmp(key, W("JitName")) == 0) ||
        (_wcsicmp(key, W("AltJitName")) == 0) ||
        (_wcsicmp(key, W("EnableExtraSuperPmiQueries")) == 0))
    {
        return false;
    }

    // By default, we record everything.
    return true;
}

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
    if (RecordVariable(key) && (result != defaultValue))
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
    if (RecordVariable(key) && (result != nullptr))
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
