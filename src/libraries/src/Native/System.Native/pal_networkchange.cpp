// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"

#include "pal_errno.h"
#include "pal_networkchange.h"
#include "pal_types.h"
#include "pal_utilities.h"

#include <errno.h>
#include <linux/rtnetlink.h>
#include <net/if.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <sys/uio.h>
#include <unistd.h>

#pragma clang diagnostic ignored "-Wcast-align" // NLMSG_* macros trigger this

extern "C" Error SystemNative_CreateNetworkChangeListenerSocket(int32_t* retSocket)
{
    sockaddr_nl sa = {};
    sa.nl_family = AF_NETLINK;
    sa.nl_groups = RTMGRP_LINK | RTMGRP_IPV4_IFADDR;
    int32_t sock = socket(AF_NETLINK, SOCK_RAW, NETLINK_ROUTE);
    if (sock == -1)
    {
        *retSocket = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }
    if (bind(sock, reinterpret_cast<sockaddr*>(&sa), sizeof(sa)) != 0)
    {
        *retSocket = -1;
        return SystemNative_ConvertErrorPlatformToPal(errno);
    }

    *retSocket = sock;
    return PAL_SUCCESS;
}

extern "C" Error SystemNative_CloseNetworkChangeListenerSocket(int32_t socket)
{
    int err = close(socket);
    return err == 0 || CheckInterrupted(err) ? PAL_SUCCESS : SystemNative_ConvertErrorPlatformToPal(errno);
}

extern "C" NetworkChangeKind SystemNative_ReadSingleEvent(int32_t sock)
{
    char buffer[4096];
    iovec iov = {buffer, sizeof(buffer)};
    sockaddr_nl sanl;
    msghdr msg = { .msg_name = reinterpret_cast<void*>(&sanl), .msg_namelen = sizeof(sockaddr_nl), .msg_iov = &iov, .msg_iovlen = 1 };
    ssize_t len;
    while (CheckInterrupted(len = recvmsg(sock, &msg, 0)));
    if (len == -1)
    {
        // Probably means the socket has been closed.
        // If so, the managed side will ignore the return value.
        return NetworkChangeKind::None;
    }

    nlmsghdr* hdr = reinterpret_cast<nlmsghdr*>(buffer);
    // This channel should only send a single message at a time.
    // This means there should be no multi-part messages (NLM_F_MULTI).
    assert((hdr->nlmsg_flags & NLM_F_MULTI) == 0);
    switch (hdr->nlmsg_type)
    {
        case NLMSG_DONE:
            return NetworkChangeKind::None;
        case NLMSG_ERROR:
            return NetworkChangeKind::None;
        case RTM_NEWADDR:
            return NetworkChangeKind::AddressAdded;
        case RTM_DELADDR:
            return NetworkChangeKind::AddressRemoved;
        case RTM_NEWLINK:
            return ReadNewLinkMessage(hdr);
        case RTM_DELLINK:
            return NetworkChangeKind::LinkRemoved;
        default:
            return NetworkChangeKind::None;
    }
}

NetworkChangeKind ReadNewLinkMessage(nlmsghdr* hdr)
{
    assert(hdr != nullptr);
    ifinfomsg* ifimsg;
    ifimsg = reinterpret_cast<ifinfomsg*>(NLMSG_DATA(hdr));
    if (ifimsg->ifi_family == AF_INET)
    {
        if ((ifimsg->ifi_flags & IFF_UP) != 0)
        {
            return NetworkChangeKind::LinkAdded;
        }
    }

    return NetworkChangeKind::None;
}
