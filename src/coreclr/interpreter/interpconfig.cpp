// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "interpreter.h"

InterpConfigValues InterpConfig;

void InterpConfigValues::Initialize(ICorJitHost* host)
{
    assert(!m_isInitialized);

#define RELEASE_CONFIG_STRING(name, key)    m_##name = host->getStringConfigValue(key);
#define RELEASE_CONFIG_METHODSET(name, key)    do { const char *pConfigValue = host->getStringConfigValue(key); m_##name.initialize(pConfigValue); host->freeStringConfigValue(pConfigValue); } while (0);
#define RELEASE_CONFIG_INTEGER(name, key, defaultValue)   m_##name = host->getIntConfigValue(key, defaultValue);
#include "interpconfigvalues.h"

    m_isInitialized = true;
}

void InterpConfigValues::Destroy(ICorJitHost* host)
{
    if (!m_isInitialized)
        return;

#define RELEASE_CONFIG_STRING(name, key)    host->freeStringConfigValue(m_##name);
#define RELEASE_CONFIG_METHODSET(name, key) m_##name.destroy();
#define RELEASE_CONFIG_INTEGER(name, key, defaultValue)
#include "interpconfigvalues.h"

    m_isInitialized = false;
}
