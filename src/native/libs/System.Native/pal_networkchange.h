// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"

typedef enum
{
    None = -1,
    AddressAdded = 0,
    AddressRemoved = 1,
    AvailabilityChanged = 2,
} NetworkChangeKind;

typedef void (*NetworkChangeEvent)(intptr_t sock, NetworkChangeKind notificationKind);

PALEXPORT Error SystemNative_ReadEvents(intptr_t sock, NetworkChangeEvent onNetworkChange);

PALEXPORT Error SystemNative_CreateNetworkChangeListenerSocket(intptr_t* retSocket);
