// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"
#include <linux/netlink.h>

enum class NetworkChangeKind : int32_t
{
    None = -1,
    AddressAdded = 0,
    AddressRemoved = 1,
    LinkAdded = 2,
    LinkRemoved = 3
};

typedef void (*NetworkChangeEvent)(NetworkChangeKind notificationKind);

extern "C" NetworkChangeKind SystemNative_ReadSingleEvent(int sock);
NetworkChangeKind ReadNewLinkMessage(nlmsghdr* hdr);
