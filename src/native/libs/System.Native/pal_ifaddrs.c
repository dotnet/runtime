// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "pal_ifaddrs.h"
#include "pal_safecrt.h"

#include <errno.h>
#include <limits.h>
#include <stdarg.h>
#include <string.h>
#include <unistd.h>

#include <linux/netlink.h>
#include <linux/rtnetlink.h>

#include <android/log.h>
#define LOG(level, ...) __android_log_print(level, "DOTNET_NETLINK", ## __VA_ARGS__)
#define LOG_INFO(...) LOG(ANDROID_LOG_INFO, ## __VA_ARGS__)
#define LOG_WARN(...) LOG(ANDROID_LOG_WARN, ## __VA_ARGS__)
#ifdef DEBUG
#define LOG_DEBUG(...) LOG(ANDROID_LOG_DEBUG, ## __VA_ARGS__)
#else
#define LOG_DEBUG(...) do {} while (0)
#endif

/* Maximum interface address label size, should be more than enough */
#define MAX_IFA_LABEL_SIZE 1024

/* This is the message we send to the kernel */
struct netlink_request {
    struct nlmsghdr header;
    struct rtgenmsg message;
};

struct netlink_session {
    int sock_fd;
    int seq;
    struct sockaddr_nl them; /* kernel end */
    struct sockaddr_nl us; /* our end */
    struct msghdr message_header; /* for use with sendmsg */
    struct iovec payload_vector; /* Used to send netlink_request */
};

struct sockaddr_ll_extended {
    unsigned short int sll_family;
    unsigned short int sll_protocol;
    int sll_ifindex;
    unsigned short int sll_hatype;
    unsigned char sll_pkttype;
    unsigned char sll_halen;
    unsigned char sll_addr[24];
};

static void free_single_ifaddrs(struct ifaddrs **ifap)
{
    struct ifaddrs *ifa = ifap ? *ifap : NULL;
    if (!ifa)
        return;

    if (ifa->ifa_name)
        free(ifa->ifa_name);

    if (ifa->ifa_addr)
        free(ifa->ifa_addr);

    if (ifa->ifa_netmask)
        free(ifa->ifa_netmask);

    if (ifa->ifa_broadaddr)
        free(ifa->ifa_broadaddr);

    if (ifa->ifa_data)
        free(ifa->ifa_data);

    free(ifa);
    *ifap = NULL;
}

static int open_netlink_session(struct netlink_session *session)
{
    abort_if_invalid_pointer_argument(session);

    memset(session, 0, sizeof(*session));
    session->sock_fd = socket(AF_NETLINK, SOCK_RAW, NETLINK_ROUTE);
    if (session->sock_fd == -1) {
        LOG_WARN("Failed to create a netlink socket. %s\n", strerror(errno));
        return -1;
    }

    /* Fill out addresses */
    session->us.nl_family = AF_NETLINK;

    /* We have previously used `getpid()` here but it turns out that WebView/Chromium does the same
       and there can only be one session with the same PID. Setting it to 0 will cause the kernel to
       assign some PID that's unique and valid instead.
       See: https://bugzilla.xamarin.com/show_bug.cgi?id=41860
    */
    session->us.nl_pid = 0;
    session->us.nl_groups = 0;

    session->them.nl_family = AF_NETLINK;

    if (bind (session->sock_fd, (struct sockaddr *)&session->us, sizeof(session->us)) < 0) {
        LOG_WARN("Failed to bind to the netlink socket. %s\n", strerror(errno));
        return -1;
    }

    return 0;
}

static int send_netlink_dump_request(struct netlink_session *session, int type)
{
    struct netlink_request request;

    memset(&request, 0, sizeof(request));
    request.header.nlmsg_len = NLMSG_LENGTH(sizeof(struct rtgenmsg));
    /* Flags (from netlink.h):
       NLM_F_REQUEST - it's a request message
       NLM_F_DUMP - gives us the root of the link tree and returns all links matching our requested
       AF, which in our case means all of them (AF_PACKET)
    */
    request.header.nlmsg_flags = NLM_F_REQUEST | NLM_F_ROOT | NLM_F_MATCH;
    request.header.nlmsg_seq = (uint32_t)++session->seq;
    request.header.nlmsg_pid = session->us.nl_pid;
    request.header.nlmsg_type = (uint16_t)type;

    /* AF_PACKET means we want to see everything */
    request.message.rtgen_family = AF_PACKET;

    memset(&session->payload_vector, 0, sizeof(session->payload_vector));
    session->payload_vector.iov_len = request.header.nlmsg_len;
    session->payload_vector.iov_base = &request;

    memset(&session->message_header, 0, sizeof(session->message_header));
    session->message_header.msg_namelen = sizeof(session->them);
    session->message_header.msg_name = &session->them;
    session->message_header.msg_iovlen = 1;
    session->message_header.msg_iov = &session->payload_vector;

    if (sendmsg(session->sock_fd, (const struct msghdr*)&session->message_header, 0) < 0) {
        LOG_WARN("Failed to send netlink message. %s\n", strerror(errno));
        return -1;
    }

    return 0;
}

static int append_ifaddr(struct ifaddrs *addr, struct ifaddrs **ifaddrs_head, struct ifaddrs **last_ifaddr)
{
    abort_if_invalid_pointer_argument(addr);
    abort_if_invalid_pointer_argument(ifaddrs_head);
    abort_if_invalid_pointer_argument(last_ifaddr);

    if (addr->ifa_name == NULL) {
        LOG_WARN("ifa_name is NULL -- skipping");
        return 0; // skip this addr
    }

    if (!*ifaddrs_head) {
        *ifaddrs_head = *last_ifaddr = addr;
        if (!*ifaddrs_head)
            return -1;
    } else if (!*last_ifaddr) {
        struct ifaddrs *last = *ifaddrs_head;

        while (last->ifa_next)
            last = last->ifa_next;
        *last_ifaddr = last;
    }

    addr->ifa_next = NULL;
    if (addr == *last_ifaddr)
        return 0;

    (*last_ifaddr)->ifa_next = addr;
    *last_ifaddr = addr;

    return 0;
}

static int fill_sa_address(struct sockaddr **sa, struct ifaddrmsg *net_address, void *rta_data, size_t rta_payload_length)
{
    abort_if_invalid_pointer_argument(sa);
    abort_if_invalid_pointer_argument(net_address);
    abort_if_invalid_pointer_argument(rta_data);

    switch (net_address->ifa_family) {
        case AF_INET: {
            struct sockaddr_in *sa4;
            if (rta_payload_length != 4) /* IPv4 address length */ {
                LOG_WARN("Unexpected IPv4 address payload length %zu", rta_payload_length);
                return -1;
            }
            sa4 = (struct sockaddr_in*)calloc(1, sizeof(*sa4));
            if (sa4 == NULL)
                return -1;

            sa4->sin_family = AF_INET;
            memcpy(&sa4->sin_addr, rta_data, rta_payload_length);
            *sa = (struct sockaddr*)sa4;
            break;
        }

        case AF_INET6: {
            struct sockaddr_in6 *sa6;
            if (rta_payload_length != 16) /* IPv6 address length */ {
                LOG_WARN("Unexpected IPv6 address payload length %zu", rta_payload_length);
                return -1;
            }
            sa6 = (struct sockaddr_in6*)calloc(1, sizeof(*sa6));
            if (sa6 == NULL)
                return -1;

            sa6->sin6_family = AF_INET6;
            memcpy(&sa6->sin6_addr, rta_data, rta_payload_length);
            if (IN6_IS_ADDR_LINKLOCAL(&sa6->sin6_addr) || IN6_IS_ADDR_MC_LINKLOCAL (&sa6->sin6_addr))
                sa6->sin6_scope_id = net_address->ifa_index;
            *sa = (struct sockaddr*)sa6;
            break;
        }

        default: {
            struct sockaddr *sagen;
            if (rta_payload_length > sizeof(sagen->sa_data)) {
                LOG_WARN("Unexpected RTA payload length %zu (wanted at most %zu)", rta_payload_length, sizeof(sagen->sa_data));
                return -1;
            }

            *sa = sagen = (struct sockaddr*)calloc(1, sizeof(*sagen));
            if (!sagen)
                return -1;

            sagen->sa_family = net_address->ifa_family;
            memcpy(&sagen->sa_data, rta_data, rta_payload_length);
            break;
        }
    }

    return 0;
}

static int fill_ll_address(struct sockaddr_ll_extended **sa, struct ifinfomsg *net_interface, void *rta_data, size_t rta_payload_length)
{
    abort_if_invalid_pointer_argument(sa);
    abort_if_invalid_pointer_argument(net_interface);

    /* Always allocate, do not free - caller may reuse the same variable */
    *sa = (struct sockaddr_ll_extended*)calloc(1, sizeof(**sa));
    if (!*sa)
        return -1;

    (*sa)->sll_family = AF_PACKET; /* Always for physical links */

    /* The assert can only fail for Iniband links, which are quite unlikely to be found
     * in any mobile devices
     */
    LOG_DEBUG("rta_payload_length == %zu; sizeof sll_addr == %zu; hw type == 0x%X\n", rta_payload_length, sizeof((*sa)->sll_addr), net_interface->ifi_type);
    if ((size_t)(rta_payload_length) > sizeof((*sa)->sll_addr)) {
        LOG_INFO("Address is too long to place in sockaddr_ll (%zu > %zu)", rta_payload_length, sizeof((*sa)->sll_addr));
        free(*sa);
        *sa = NULL;
        return -1;
    }

    if (rta_payload_length > UCHAR_MAX) {
        LOG_INFO("Payload length too big to fit in the address structure");
        free(*sa);
        *sa = NULL;
        return -1;
    }

    (*sa)->sll_ifindex = net_interface->ifi_index;
    (*sa)->sll_hatype = net_interface->ifi_type;
    (*sa)->sll_halen = (unsigned char)(rta_payload_length);
    memcpy((*sa)->sll_addr, rta_data, rta_payload_length);

    return 0;
}


static struct ifaddrs *get_link_info(struct nlmsghdr *message)
{
    ssize_t length;
    struct rtattr *attribute;
    struct ifinfomsg *net_interface;
    struct ifaddrs *ifa = NULL;
    struct sockaddr_ll_extended *sa = NULL;

    abort_if_invalid_pointer_argument(message);
    net_interface = (struct ifinfomsg*)(NLMSG_DATA(message));
    length = (ssize_t)(message->nlmsg_len - NLMSG_LENGTH(sizeof(*net_interface)));
    if (length <= 0) {
        goto error;
    }

    ifa = (struct ifaddrs*)calloc(1, sizeof(*ifa));
    if (!ifa) {
        goto error;
    }

    ifa->ifa_flags = net_interface->ifi_flags;
    attribute = IFLA_RTA(net_interface);
    while (RTA_OK(attribute, length)) {
        switch (attribute->rta_type) {
            case IFLA_IFNAME:
                ifa->ifa_name = strdup((const char*)(RTA_DATA(attribute)));
                if (!ifa->ifa_name) {
                    goto error;
                }
                LOG_DEBUG("   interface name (payload length: %zu; string length: %zu)\n", RTA_PAYLOAD(attribute), strlen(ifa->ifa_name));
                LOG_DEBUG("     %s\n", ifa->ifa_name);
                break;

            case IFLA_BROADCAST:
                LOG_DEBUG("   interface broadcast (%zu bytes)\n", RTA_PAYLOAD(attribute));
                if (fill_ll_address(&sa, net_interface, RTA_DATA(attribute), RTA_PAYLOAD(attribute)) < 0) {
                    goto error;
                }
                ifa->ifa_broadaddr = (struct sockaddr*)sa;
                break;

            case IFLA_ADDRESS:
                LOG_DEBUG("   interface address (%zu bytes)\n", RTA_PAYLOAD(attribute));
                if (fill_ll_address(&sa, net_interface, RTA_DATA(attribute), RTA_PAYLOAD(attribute)) < 0) {
                    goto error;
                }
                ifa->ifa_addr = (struct sockaddr*)sa;
                break;

            default:
                break;
        }

        attribute = RTA_NEXT (attribute, length);
    }

    LOG_DEBUG("link flags: 0x%X", ifa->ifa_flags);
    return ifa;

  error:
    if (sa)
        free(sa);
    free_single_ifaddrs(&ifa);

    return NULL;
}

static struct ifaddrs *find_interface_by_index(int index, struct ifaddrs **ifaddrs_head)
{
    struct ifaddrs *cur;
    if (!ifaddrs_head || !*ifaddrs_head)
        return NULL;

    /* Normally expensive, but with the small amount of links in the chain we'll deal with it's not
     * worth the extra housekeeping and memory overhead
     */
    cur = *ifaddrs_head;
    while (cur) {
        if (cur->ifa_addr && cur->ifa_addr->sa_family == AF_PACKET && ((struct sockaddr_ll_extended*)cur->ifa_addr)->sll_ifindex == index)
            return cur;
        if (cur == cur->ifa_next)
            break;
        cur = cur->ifa_next;
    }

    return NULL;
}

static char *get_interface_name_by_index(int index, struct ifaddrs **ifaddrs_head)
{
    struct ifaddrs *iface = find_interface_by_index(index, ifaddrs_head);
    if (!iface || !iface->ifa_name)
        return NULL;

    return iface->ifa_name;
}

static int get_interface_flags_by_index(int index, struct ifaddrs **ifaddrs_head)
{
    struct ifaddrs *iface = find_interface_by_index(index, ifaddrs_head);
    if (!iface)
        return 0;

    return (int)(iface->ifa_flags);
}

static int calculate_address_netmask(struct ifaddrs *ifa, struct ifaddrmsg *net_address)
{
    if (ifa->ifa_addr && ifa->ifa_addr->sa_family != AF_UNSPEC && ifa->ifa_addr->sa_family != AF_PACKET) {
        uint32_t prefix_length = 0;
        uint32_t data_length = 0;
        unsigned char *netmask_data = NULL;

        switch (ifa->ifa_addr->sa_family) {
            case AF_INET: {
                struct sockaddr_in *sa = (struct sockaddr_in*)calloc(1, sizeof(struct sockaddr_in));
                if (!sa)
                    return -1;

                ifa->ifa_netmask = (struct sockaddr*)sa;
                prefix_length = net_address->ifa_prefixlen;
                if (prefix_length > 32)
                    prefix_length = 32;
                data_length = sizeof(sa->sin_addr);
                netmask_data = (unsigned char*)&sa->sin_addr;
                break;
            }

            case AF_INET6: {
                struct sockaddr_in6 *sa = (struct sockaddr_in6*)calloc(1, sizeof(struct sockaddr_in6));
                if (!sa)
                    return -1;

                ifa->ifa_netmask = (struct sockaddr*)sa;
                prefix_length = net_address->ifa_prefixlen;
                if (prefix_length > 128)
                    prefix_length = 128;
                data_length = sizeof(sa->sin6_addr);
                netmask_data = (unsigned char*)&sa->sin6_addr;
                break;
            }
        }

        if (ifa->ifa_netmask && netmask_data) {
            /* Fill the first X bytes with 255 */
            uint32_t prefix_bytes = prefix_length / 8;
            uint32_t postfix_bytes;

            if (prefix_bytes > data_length) {
                errno = EINVAL;
                return -1;
            }
            postfix_bytes = data_length - prefix_bytes;
            memset(netmask_data, 0xFF, prefix_bytes);
            if (postfix_bytes > 0)
                memset(netmask_data + prefix_bytes + 1, 0x00, postfix_bytes);
            LOG_DEBUG("   calculating netmask, prefix length is %u bits (%u bytes), data length is %u bytes\n", prefix_length, prefix_bytes, data_length);
            if (prefix_bytes + 2 < data_length)
                /* Set the rest of the mask bits in the byte following the last 0xFF value */
                netmask_data[prefix_bytes + 1] = (unsigned char)(0xff << (8 - (prefix_length % 8)));
        }
    }

    return 0;
}


static struct ifaddrs *get_link_address(struct nlmsghdr *message, struct ifaddrs **ifaddrs_head)
{
    ssize_t length = 0;
    struct rtattr *attribute;
    struct ifaddrmsg *net_address;
    struct ifaddrs *ifa = NULL;
    struct sockaddr **sa;
    size_t payload_size;

    abort_if_invalid_pointer_argument(message);
    net_address = (struct ifaddrmsg*)(NLMSG_DATA(message));
    length = (ssize_t)(IFA_PAYLOAD(message));
    LOG_DEBUG("   address data length: %zu", length);
    if (length <= 0) {
        goto error;
    }

    ifa = (struct ifaddrs*)(calloc(1, sizeof(*ifa)));
    if (!ifa) {
        goto error;
    }

    // values < 0 are never returned, the cast is safe
    ifa->ifa_flags = (unsigned int)(get_interface_flags_by_index((int)(net_address->ifa_index), ifaddrs_head));

    attribute = IFA_RTA(net_address);
    LOG_DEBUG("   reading attributes");
    while (RTA_OK(attribute, length)) {
        payload_size = RTA_PAYLOAD(attribute);
        LOG_DEBUG("     attribute payload_size == %zu\n", payload_size);
        sa = NULL;

        switch (attribute->rta_type) {
            case IFA_LABEL: {
                size_t room_for_trailing_null = 0;

                LOG_DEBUG("     attribute type: LABEL");
                if (payload_size > MAX_IFA_LABEL_SIZE) {
                    payload_size = MAX_IFA_LABEL_SIZE;
                    room_for_trailing_null = 1;
                }

                if (payload_size > 0) {
                    size_t alloc_size;
                    if (!multiply_s(payload_size, room_for_trailing_null, &alloc_size)) {
                        goto error;
                    }

                    ifa->ifa_name = (char*)malloc(alloc_size);
                    if (!ifa->ifa_name) {
                        goto error;
                    }

                    memcpy(ifa->ifa_name, RTA_DATA (attribute), payload_size);
                    if (room_for_trailing_null)
                        ifa->ifa_name[payload_size] = '\0';
                }
                break;
            }

            case IFA_LOCAL:
                LOG_DEBUG("     attribute type: LOCAL");
                if (ifa->ifa_addr) {
                    /* P2P protocol, set the dst/broadcast address union from the original address.
                     * Since ifa_addr is set it means IFA_ADDRESS occurred earlier and that address
                     * is indeed the P2P destination one.
                     */
                    ifa->ifa_dstaddr = ifa->ifa_addr;
                    ifa->ifa_addr = 0;
                }
                sa = &ifa->ifa_addr;
                break;

            case IFA_BROADCAST:
                LOG_DEBUG("     attribute type: BROADCAST");
                if (ifa->ifa_dstaddr) {
                    /* IFA_LOCAL happened earlier, undo its effect here */
                    free(ifa->ifa_dstaddr);
                    ifa->ifa_dstaddr = NULL;
                }
                sa = &ifa->ifa_broadaddr;
                break;

            case IFA_ADDRESS:
                LOG_DEBUG("     attribute type: ADDRESS");
                if (ifa->ifa_addr) {
                    /* Apparently IFA_LOCAL occurred earlier and we have a P2P connection
                     * here. IFA_LOCAL carries the destination address, move it there
                     */
                    ifa->ifa_dstaddr = ifa->ifa_addr;
                    ifa->ifa_addr = NULL;
                }
                sa = &ifa->ifa_addr;
                break;

            case IFA_UNSPEC:
                LOG_DEBUG("     attribute type: UNSPEC");
                break;

            case IFA_ANYCAST:
                LOG_DEBUG("     attribute type: ANYCAST");
                break;

            case IFA_CACHEINFO:
                LOG_DEBUG("     attribute type: CACHEINFO");
                break;

            case IFA_MULTICAST:
                LOG_DEBUG("     attribute type: MULTICAST");
                break;

            default:
                LOG_DEBUG("     attribute type: %u", attribute->rta_type);
                break;
        }

        if (sa) {
            if (fill_sa_address(sa, net_address, RTA_DATA(attribute), RTA_PAYLOAD(attribute)) < 0) {
                goto error;
            }
        }

        attribute = RTA_NEXT(attribute, length);
    }

    /* glibc stores the associated interface name in the address if IFA_LABEL never occurred */
    if (!ifa->ifa_name) {
        char *name = get_interface_name_by_index((int)(net_address->ifa_index), ifaddrs_head);
        LOG_DEBUG("   address has no name/label, getting one from interface\n");
        ifa->ifa_name = name ? strdup(name) : NULL;
    }
    LOG_DEBUG("   address label: %s\n", ifa->ifa_name);

    if (calculate_address_netmask(ifa, net_address) < 0) {
        goto error;
    }

    return ifa;

  error:
    {
        /* errno may be modified by free, or any other call inside the free_single_xamarin_ifaddrs
         * function. We don't care about errors in there since it is more important to know how we
         * failed to obtain the link address and not that we went OOM. Save and restore the value
         * after the resources are freed.
         */
        int errno_save = errno;
        free_single_ifaddrs (&ifa);
        errno = errno_save;
        return NULL;
    }

}

static int parse_netlink_reply(struct netlink_session *session, struct ifaddrs **ifaddrs_head, struct ifaddrs **last_ifaddr)
{
    struct msghdr netlink_reply;
    struct iovec reply_vector;
    struct nlmsghdr *current_message;
    struct ifaddrs *addr;
    int ret = -1;
    unsigned char *response = NULL;

    abort_if_invalid_pointer_argument(session);
    abort_if_invalid_pointer_argument(ifaddrs_head);
    abort_if_invalid_pointer_argument(last_ifaddr);

    size_t buf_size = (size_t)(getpagesize());
    LOG_DEBUG("receive buffer size == %zu", buf_size);

    size_t alloc_size;
    if (!multiply_s(sizeof(*response), buf_size, &alloc_size)) {
        goto cleanup;
    }

    response = (unsigned char*)malloc(alloc_size);
    ssize_t length = 0;
    if (!response) {
        goto cleanup;
    }

    while (1) {
        memset(response, 0, buf_size);
        memset(&reply_vector, 0, sizeof(reply_vector));
        reply_vector.iov_len = buf_size;
        reply_vector.iov_base = response;

        memset(&netlink_reply, 0, sizeof(netlink_reply));
        netlink_reply.msg_namelen = sizeof(&session->them);
        netlink_reply.msg_name = &session->them;
        netlink_reply.msg_iovlen = 1;
        netlink_reply.msg_iov = &reply_vector;

        length = recvmsg(session->sock_fd, &netlink_reply, 0);
        LOG_DEBUG("  length == %d\n", (int)length);

        if (length < 0) {
            LOG_DEBUG("Failed to receive reply from netlink. %s\n", strerror(errno));
            goto cleanup;
        }

#ifdef DEBUG
        LOG_DEBUG("response flags:");
        if (netlink_reply.msg_flags == 0)
            LOG_DEBUG("  [NONE]");
        else {
            if (netlink_reply.msg_flags & MSG_EOR)
                LOG_DEBUG("   MSG_EOR");
            if (netlink_reply.msg_flags & MSG_TRUNC)
                LOG_DEBUG("   MSG_TRUNC");
            if (netlink_reply.msg_flags & MSG_CTRUNC)
                LOG_DEBUG("   MSG_CTRUNC");
            if (netlink_reply.msg_flags & MSG_OOB)
                LOG_DEBUG("   MSG_OOB");
            if (netlink_reply.msg_flags & MSG_ERRQUEUE)
                LOG_DEBUG("   MSG_ERRQUEUE");
        }
#endif

        if (length == 0)
            break;

        for (current_message = (struct nlmsghdr*)response; current_message && NLMSG_OK(current_message, (size_t)length); current_message = NLMSG_NEXT(current_message, length)) {
            LOG_DEBUG("next message... (type: %u)\n", current_message->nlmsg_type);
            switch (current_message->nlmsg_type) {
                /* See rtnetlink.h */
                case RTM_NEWLINK:
                    LOG_DEBUG("  dumping link...\n");
                    addr = get_link_info(current_message);
                    if (!addr || append_ifaddr(addr, ifaddrs_head, last_ifaddr) < 0) {
                        ret = -1;
                        goto cleanup;
                    }
                    LOG_DEBUG("  done\n");
                    break;

                case RTM_NEWADDR:
                    LOG_DEBUG("  got an address\n");
                    addr = get_link_address(current_message, ifaddrs_head);
                    if (!addr || append_ifaddr(addr, ifaddrs_head, last_ifaddr) < 0) {
                        ret = -1;
                        goto cleanup;
                    }
                    break;

                case NLMSG_DONE:
                    LOG_DEBUG("  message done\n");
                    ret = 0;
                    goto cleanup;

                default:
                    LOG_DEBUG("  message type: %u", current_message->nlmsg_type);
                    break;
            }
        }
    }

  cleanup:
    if (response)
        free(response);
    return ret;
}

int _netlink_getifaddrs(struct ifaddrs **ifap)
{
    int ret = -1;

    *ifap = NULL;
    struct ifaddrs *ifaddrs_head = 0;
    struct ifaddrs *last_ifaddr = 0;
    struct netlink_session session;

    if (open_netlink_session(&session) < 0) {
        goto cleanup;
    }

    /* Request information about the specified link. In our case it will be all of them since we
        request the root of the link tree below
    */
    if ((send_netlink_dump_request(&session, RTM_GETLINK) < 0) ||
            (parse_netlink_reply(&session, &ifaddrs_head, &last_ifaddr) < 0) ||
            (send_netlink_dump_request(&session, RTM_GETADDR) < 0) ||
            (parse_netlink_reply(&session, &ifaddrs_head, &last_ifaddr) < 0)) {
        _netlink_freeifaddrs (ifaddrs_head);
        goto cleanup;
    }

    ret = 0;
    *ifap = ifaddrs_head;

cleanup:
    if (session.sock_fd >= 0) {
        close(session.sock_fd);
        session.sock_fd = -1;
    }

    return ret;
}

void _netlink_freeifaddrs(struct ifaddrs *ifa)
{
    struct ifaddrs *cur, *next;

    if (!ifa)
        return;

    cur = ifa;
    while (cur) {
        next = cur->ifa_next;
        free_single_ifaddrs (&cur);
        cur = next;
    }
}
