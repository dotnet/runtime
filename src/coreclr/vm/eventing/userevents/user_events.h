// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __USEREVENTS_H__
#define __USEREVENTS_H__

#include <common.h>
#include <configuration.h>

namespace
{
	bool s_userEventsEnabled = false;
}

inline void InitUserEvents()
{
    bool isEnabled = Configuration::GetKnobBooleanValue(W("System.Diagnostics.Tracing.UserEvents"), false); 
    if (!isEnabled)
    {
        isEnabled = CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EnableUserEvents) != 0;
    }

    s_userEventsEnabled = isEnabled;
}

inline bool IsUserEventsEnabled()
{
    return s_userEventsEnabled;
}

#endif // __USEREVENTS_H__