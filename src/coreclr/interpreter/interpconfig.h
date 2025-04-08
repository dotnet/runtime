// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef _INTERPCONFIG_H_
#define _INTERPCONFIG_H_

class ICorJitHost;

class InterpConfigValues
{
private:
    bool m_isInitialized;

#define RELEASE_CONFIG_STRING(name, key)    const char* m_##name;

#include "interpconfigvalues.h"

public:

#define RELEASE_CONFIG_STRING(name, key)    \
    inline const char* name() const         \
    {                                       \
        return m_##name;                    \
    }

#include "interpconfigvalues.h"

public:
    InterpConfigValues()
    {
    }

    inline bool IsInitialized() const
    {
        return m_isInitialized != 0;
    }

    void Initialize(ICorJitHost* host);
    void Destroy(ICorJitHost* host);
};

extern InterpConfigValues InterpConfig;

#endif
