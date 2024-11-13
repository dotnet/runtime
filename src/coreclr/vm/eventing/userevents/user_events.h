// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __USEREVENTS_H__
#define __USEREVENTS_H__


void InitUserEvents();
bool IsUserEventsEnabled();
bool IsUserEventsEnabledByKeyword(UCHAR providerId, uint8_t level, uint64_t keyword);

#endif // __USEREVENTS_H__