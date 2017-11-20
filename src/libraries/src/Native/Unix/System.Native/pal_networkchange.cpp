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
    sa.nl_groups = RTMGRP_LINK | RTMGRP_IPV4_IFADDR | RTMGRP_IPV4_ROUTE | RTMGRP_IPV6_ROUTE;
    int32_t sock = socket(AF_NETLINK, SOCK_RAW, NETLINK_ROUTE);
    if (sock == -1)
    {
        *retSocket = -1;
        return static_cast<Error>(SystemNative_ConvertErrorPlatformToPal(errno));
    }
    if (bind(sock, reinterpret_cast<sockaddr*>(&sa), sizeof(sa)) != 0)
    {
        *retSocket = -1;
        return static_cast<Error>(SystemNative_ConvertErrorPlatformToPal(errno));
    }

    *retSocket = sock;
    return Error_SUCCESS;
}

extern "C" Error SystemNative_CloseNetworkChangeListenerSocket(int32_t socket)
{
    int err = close(socket);
    return err == 0 || CheckInterrupted(err) ? Error_SUCCESS : static_cast<Error>(SystemNative_ConvertErrorPlatformToPal(errno));
}

extern "C" void SystemNative_ReadEvents(int32_t sock, NetworkChangeEvent onNetworkChange)
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
        return;
    }

    for (nlmsghdr* hdr = reinterpret_cast<nlmsghdr*>(buffer); NLMSG_OK(hdr, UnsignedCast(len)); NLMSG_NEXT(hdr, len))
    {
        switch (hdr->nlmsg_type)
        {
            case NLMSG_DONE:
                return; // End of a multi-part message; stop reading.
            case NLMSG_ERROR:
                return;
            case RTM_NEWADDR:
                onNetworkChange(sock, NetworkChangeKind::AddressAdded);
                break;
            case RTM_DELADDR:
                onNetworkChange(sock, NetworkChangeKind::AddressRemoved);
                break;
            case RTM_NEWLINK:
                onNetworkChange(sock, ReadNewLinkMessage(hdr));
                break;
            case RTM_DELLINK:
                onNetworkChange(sock, NetworkChangeKind::LinkRemoved);
                break;
            case RTM_NEWROUTE:
            case RTM_DELROUTE:
            {
                rtmsg* dataAsRtMsg = reinterpret_cast<rtmsg*>(NLMSG_DATA(hdr));
                if (dataAsRtMsg->rtm_table == RT_TABLE_MAIN)
                {
                    onNetworkChange(sock, NetworkChangeKind::AvailabilityChanged);
                    return;
                }
                break;
            }
            default:
                break;
        }
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
