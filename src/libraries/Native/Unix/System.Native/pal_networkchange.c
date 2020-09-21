// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_config.h"

#include "pal_errno.h"
#include "pal_networkchange.h"
#include "pal_types.h"
#include "pal_utilities.h"

#include <errno.h>
#include <net/if.h>
#include <sys/socket.h>
#include <sys/types.h>
#include <sys/uio.h>
#include <unistd.h>
#if HAVE_LINUX_RTNETLINK_H
#include <linux/rtnetlink.h>
#elif HAVE_RT_MSGHDR
#include <net/route.h>
#else
#error System must have linux/rtnetlink.h or net/route.h.
#endif

#pragma clang diagnostic ignored "-Wcast-align" // NLMSG_* macros trigger this

Error SystemNative_CreateNetworkChangeListenerSocket(int32_t* retSocket)
{
#if HAVE_LINUX_RTNETLINK_H
    struct sockaddr_nl sa;
    memset(&sa, 0, sizeof(struct sockaddr_nl));

    sa.nl_family = AF_NETLINK;
    sa.nl_groups = RTMGRP_LINK | RTMGRP_IPV4_IFADDR | RTMGRP_IPV4_ROUTE | RTMGRP_IPV6_ROUTE;
    int32_t sock = socket(AF_NETLINK, SOCK_RAW, NETLINK_ROUTE);
#elif HAVE_RT_MSGHDR
    int32_t sock = socket(PF_ROUTE, SOCK_RAW, 0);
#endif
    if (sock == -1)
    {
        *retSocket = -1;
        return (Error)(SystemNative_ConvertErrorPlatformToPal(errno));
    }

#if HAVE_LINUX_RTNETLINK_H
    if (bind(sock, (struct sockaddr*)(&sa), sizeof(sa)) != 0)
    {
        *retSocket = -1;
        Error palError = (Error)(SystemNative_ConvertErrorPlatformToPal(errno));
        close(sock);
        return palError;
    }
#endif

    *retSocket = sock;
    return Error_SUCCESS;
}

Error SystemNative_CloseNetworkChangeListenerSocket(int32_t socket)
{
    int err = close(socket);
    return err == 0 || CheckInterrupted(err) ? Error_SUCCESS : (Error)(SystemNative_ConvertErrorPlatformToPal(errno));
}

#if HAVE_LINUX_RTNETLINK_H
static NetworkChangeKind ReadNewLinkMessage(struct nlmsghdr* hdr)
{
    assert(hdr != NULL);
    struct ifinfomsg* ifimsg;
    ifimsg = (struct ifinfomsg*)NLMSG_DATA(hdr);
    if (ifimsg->ifi_family == AF_INET)
    {
        if ((ifimsg->ifi_flags & IFF_UP) != 0)
        {
            return LinkAdded;
        }
    }

    return None;
}

void SystemNative_ReadEvents(int32_t sock, NetworkChangeEvent onNetworkChange)
{
    char buffer[4096];
    struct iovec iov = {buffer, sizeof(buffer)};
    struct sockaddr_nl sanl;
    struct msghdr msg = { .msg_name = (void*)(&sanl), .msg_namelen = sizeof(struct sockaddr_nl), .msg_iov = &iov, .msg_iovlen = 1 };
    ssize_t len;
    while (CheckInterrupted(len = recvmsg(sock, &msg, 0)));
    if (len == -1)
    {
        // Probably means the socket has been closed.
        return;
    }

    assert(len >= 0);
    for (struct nlmsghdr* hdr = (struct nlmsghdr*)buffer; NLMSG_OK(hdr, (size_t)len); NLMSG_NEXT(hdr, len))
    {
        switch (hdr->nlmsg_type)
        {
            case NLMSG_DONE:
                return; // End of a multi-part message; stop reading.
            case NLMSG_ERROR:
                return;
            case RTM_NEWADDR:
                onNetworkChange(sock, AddressAdded);
                break;
            case RTM_DELADDR:
                onNetworkChange(sock, AddressRemoved);
                break;
            case RTM_NEWLINK:
                onNetworkChange(sock, ReadNewLinkMessage(hdr));
                break;
            case RTM_DELLINK:
                onNetworkChange(sock, LinkRemoved);
                break;
            case RTM_NEWROUTE:
            case RTM_DELROUTE:
            {
                struct rtmsg* dataAsRtMsg = (struct rtmsg*)NLMSG_DATA(hdr);
                if (dataAsRtMsg->rtm_table == RT_TABLE_MAIN)
                {
                    onNetworkChange(sock, AvailabilityChanged);
                    return;
                }
                break;
            }
            default:
                break;
        }
    }
}
#elif HAVE_RT_MSGHDR
void SystemNative_ReadEvents(int32_t sock, NetworkChangeEvent onNetworkChange)
{
    char buffer[4096];
    ssize_t count = read(sock, buffer, sizeof(buffer));
    if (count < 0)
    {
        return;
    }

    struct rt_msghdr msghdr;
    for (char *ptr = buffer; (ptr + sizeof(struct rt_msghdr)) <= (buffer + count); ptr += msghdr.rtm_msglen)
    {
        memcpy(&msghdr, ptr, sizeof(msghdr));
        if (msghdr.rtm_version != RTM_VERSION)
        {
            // version mismatch
            return;
        }

        switch (msghdr.rtm_type)
        {
            case RTM_NEWADDR:
                onNetworkChange(sock, AddressAdded);
                break;
            case RTM_DELADDR:
                onNetworkChange(sock, AddressRemoved);
                break;
            case RTM_ADD:
            case RTM_DELETE:
            case RTM_REDIRECT:
                onNetworkChange(sock, AvailabilityChanged);
                return;
            default:
                break;
        }
    }
}
#endif
