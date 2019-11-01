// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"
#include "gceventstatus.h"

Volatile<GCEventLevel> GCEventStatus::enabledLevels[2] = {GCEventLevel_None, GCEventLevel_None};
Volatile<GCEventKeyword> GCEventStatus::enabledKeywords[2] = {GCEventKeyword_None, GCEventKeyword_None};
