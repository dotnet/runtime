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
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context.UserEventsProvider.id = 0;
        InitDotNETRuntimePrivate();
        MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context.UserEventsProvider.id = 1;
        InitDotNETRuntimeRundown();
        MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.UserEventsProvider.id = 2;
        InitDotNETRuntimeStress();
        MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_DOTNET_Context.UserEventsProvider.id = 3;
    }
}

bool IsUserEventsEnabled()
{
    return s_userEventsEnabled;
}

bool IsUserEventsEnabledByKeyword(UCHAR providerId, uint8_t level, uint64_t keyword)
{
    switch (providerId)
    {
        case 0:
        {
            return DotNETRuntimeEnabledByKeyword(level, keyword);
        }
        case 1:
        {
            return DotNETRuntimePrivateEnabledByKeyword(level, keyword);
        }
        case 2:
        {
            return DotNETRuntimeRundownEnabledByKeyword(level, keyword);
        }
        case 3:
        {
            return DotNETRuntimeStressEnabledByKeyword(level, keyword);
        }
        default:
        {
            _ASSERTE(!"Unknown provider id");
        }
    }

    return false;
}
