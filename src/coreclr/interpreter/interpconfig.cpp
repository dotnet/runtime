// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpreter.h"

InterpConfigValues InterpConfig;

void InterpConfigValues::Initialize(ICorJitHost* host)
{
    assert(!m_isInitialized);

#define RELEASE_CONFIG_STRING(name, key)    m_##name = host->getStringConfigValue(key);

#include "interpconfigvalues.h"

    m_isInitialized = true;
}

void InterpConfigValues::Destroy(ICorJitHost* host)
{
    if (!m_isInitialized)
        return;

#define RELEASE_CONFIG_STRING(name, key)    host->freeStringConfigValue(m_##name);

#include "interpconfigvalues.h"

    m_isInitialized = false;
}
