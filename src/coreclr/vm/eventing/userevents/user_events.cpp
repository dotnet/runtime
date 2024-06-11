// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <common.h>
#include <configuration.h>
#include "user_events.h"

namespace
{
	bool s_userEventsEnabled = false;
}

void InitDotNETRuntime();
bool DotNETRuntimeEnabledByKeyword(uint8_t level, uint64_t keyword);

void InitDotNETRuntimePrivate();
bool DotNETRuntimePrivateEnabledByKeyword(uint8_t level, uint64_t keyword);

void InitDotNETRuntimeRundown();
bool DotNETRuntimeRundownEnabledByKeyword(uint8_t level, uint64_t keyword);

void InitDotNETRuntimeStress();
bool DotNETRuntimeStressEnabledByKeyword(uint8_t level, uint64_t keyword);

void InitUserEvents()
{
    bool isEnabled = Configuration::GetKnobBooleanValue(W("System.Diagnostics.Tracing.UserEvents"), false); 
    if (!isEnabled)
    {
        isEnabled = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EnableUserEvents) != 0;
    }

    s_userEventsEnabled = isEnabled;

    if (s_userEventsEnabled)
    {
    	InitDotNETRuntime();
    	InitDotNETRuntimePrivate();
    	InitDotNETRuntimeRundown();
		InitDotNETRuntimeStress();
    }
}

bool IsUserEventsEnabled()
{
    return s_userEventsEnabled;
}

bool IsUserEventsEnabledByKeyword(LPCWSTR name, uint8_t level, uint64_t keyword)
{
    if (u16_strcmp(W("Microsoft-Windows-DotNETRuntime"), name) == 0)
    {
        return DotNETRuntimeEnabledByKeyword(level, keyword);
    }
    else if (u16_strcmp(W("Microsoft-Windows-DotNETRuntimePrivate"), name) == 0)
    {
        return DotNETRuntimePrivateEnabledByKeyword(level, keyword);
    }
    else if (u16_strcmp(W("Microsoft-Windows-DotNETRuntimeRundown"), name) == 0)
    {
        return DotNETRuntimeRundownEnabledByKeyword(level, keyword);
    }
    else if (u16_strcmp(W("Microsoft-Windows-DotNETRuntimeStress"), name) == 0)
    {
        return DotNETRuntimeStressEnabledByKeyword(level, keyword);
    }

    return false;
}
