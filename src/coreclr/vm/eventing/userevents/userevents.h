// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __USEREVENTS_H__
#define __USEREVENTS_H__

#ifdef TARGET_UNIX
static bool s_userEventsEnabled = false;

inline void InitUserEvents()
{
	// TODO: What to call this runtimeconfig.json switch? S.D.T implies it supports EventSource
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

#else // TARGET_UNIX
inline void InitUserEvents()
{

}

inline bool IsUserEventsEnabled()
{
	return false;
}

#endif // TARGET_UNIX
#endif // __USEREVENTS_H__