// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "gcenv.h"
#include "gcheaputilities.h"
#include "gceventstatus.h"

void InitializeGCEventLock()
{
}

HRESULT InitializeDefaultGC();

HRESULT InitializeGCSelector()
{
    return InitializeDefaultGC();
}

void GCHeapUtilities::RecordEventStateChange(bool isPublicProvider, GCEventKeyword keywords, GCEventLevel level)
{
    GCEventStatus::Set(isPublicProvider ? GCEventProvider_Default : GCEventProvider_Private, keywords, level);
}
