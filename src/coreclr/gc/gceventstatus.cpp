// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gceventstatus.h"

Volatile<GCEventLevel> GCEventStatus::enabledLevels[2] = {Volatile<GCEventLevel>(GCEventLevel_None), Volatile<GCEventLevel>(GCEventLevel_None)};
Volatile<GCEventKeyword> GCEventStatus::enabledKeywords[2] = {Volatile<GCEventKeyword>(GCEventKeyword_None), Volatile<GCEventKeyword>(GCEventKeyword_None)};