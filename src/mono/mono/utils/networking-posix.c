/**
 * \file
 * Modern posix networking code
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#include <config.h>
#include <glib.h>

#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif
#ifdef HAVE_SYS_IOCTL_H
#include <sys/ioctl.h>
#endif
#ifdef HAVE_NET_IF_H
#include <net/if.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_GETIFADDRS
#include <ifaddrs.h>
#endif

#include <mono/utils/networking.h>
#include <mono/utils/mono-threads-coop.h>

static void*
get_address_from_sockaddr (struct sockaddr *sa)
{
	switch (sa->sa_family) {
	case AF_INET:
		return &((struct sockaddr_in*)sa)->sin_addr;
	case AF_INET6:
		return &((struct sockaddr_in6*)sa)->sin6_addr;
	}
	return NULL;
}

#ifdef HAVE_GETADDRINFO

int
mono_get_address_info (const char *hostname, int port, int flags, MonoAddressInfo **result)
{
	char service_name [16];
	struct addrinfo hints, *res = NULL, *info;
	MonoAddressEntry *cur = NULL, *prev = NULL;
	MonoAddressInfo *addr_info;
	int ret;

	memset (&hints, 0, sizeof (struct addrinfo));
	*result = NULL;

	hints.ai_family = PF_UNSPEC;
	if (flags & MONO_HINT_IPV4)
		hints.ai_family = PF_INET;
	else if (flags & MONO_HINT_IPV6)
		hints.ai_family = PF_INET6;

	hints.ai_socktype = SOCK_STREAM;

	if (flags & MONO_HINT_CANONICAL_NAME)
		hints.ai_flags = AI_CANONNAME;

/* Some ancient libc don't define AI_ADDRCONFIG */
#ifdef AI_ADDRCONFIG
	if (flags & MONO_HINT_CONFIGURED_ONLY)
		hints.ai_flags = AI_ADDRCONFIG;
#endif
	sprintf (service_name, "%d", port);

	MONO_ENTER_GC_SAFE;
	ret = getaddrinfo (hostname, service_name, &hints, &info);
	MONO_EXIT_GC_SAFE;

	if (ret)
		return 1; /* FIXME propagate the error */

	res = info;
	*result = addr_info = g_new0 (MonoAddressInfo, 1);

	while (res) {
		cur = g_new0 (MonoAddressEntry, 1);
		cur->family = res->ai_family;
		cur->socktype = res->ai_socktype;
		cur->protocol = res->ai_protocol;
		if (cur->family == PF_INET) {
			cur->address_len = sizeof (struct in_addr);
			cur->address.v4 = ((struct sockaddr_in*)res->ai_addr)->sin_addr;
		} else if (cur->family == PF_INET6) {
			cur->address_len = sizeof (struct in6_addr);
			cur->address.v6 = ((struct sockaddr_in6*)res->ai_addr)->sin6_addr;
		} else {
			g_warning ("Cannot handle address family %d", cur->family);
			res = res->ai_next;
			g_free (cur);
			continue;
		}

		if (res->ai_canonname)
			cur->canonical_name = g_strdup (res->ai_canonname);

		if (prev)
			prev->next = cur;
		else
			addr_info->entries = cur;
			
		prev = cur;
		res = res->ai_next;
	}

	freeaddrinfo (info);
	return 0;
}

#endif

#ifdef HAVE_GETPROTOBYNAME

static int
fetch_protocol (const char *proto_name, int *cache, int *proto, int default_val)
{
	if (!*cache) {
		struct protoent *pent;

		pent = getprotobyname (proto_name);
		*proto = pent ? pent->p_proto : default_val;
		*cache = 1;
	}
	return *proto;
}

int
mono_networking_get_tcp_protocol (void)
{
	static int cache, proto;
	return fetch_protocol ("tcp", &cache, &proto, 6); //6 is SOL_TCP on linux
}

int
mono_networking_get_ip_protocol (void)
{
	static int cache, proto;
	return fetch_protocol ("ip", &cache, &proto, 0); //0 is SOL_IP on linux
}

int
mono_networking_get_ipv6_protocol (void)
{
	static int cache, proto;
	return fetch_protocol ("ipv6", &cache, &proto, 41); //41 is SOL_IPV6 on linux
}

#endif

#if defined (HAVE_SIOCGIFCONF)

#define IFCONF_BUFF_SIZE 1024
#ifndef _SIZEOF_ADDR_IFREQ
#define _SIZEOF_ADDR_IFREQ(ifr) (sizeof (struct ifreq))
#endif

#define FOREACH_IFR(IFR, IFC) \
	for (IFR = (IFC).ifc_req;	\
	ifr < (struct ifreq*)((char*)(IFC).ifc_req + (IFC).ifc_len); \
	ifr = (struct ifreq*)((char*)(IFR) + _SIZEOF_ADDR_IFREQ (*(IFR))))

void *
mono_get_local_interfaces (int family, int *interface_count)
{
	int fd;
	struct ifconf ifc;
	struct ifreq *ifr;
	int if_count = 0;
	gboolean ignore_loopback = FALSE;
	void *result = NULL;
	char *result_ptr;

	*interface_count = 0;

	if (!mono_address_size_for_family (family))
		return NULL;

	fd = socket (family, SOCK_STREAM, 0);
	if (fd == -1)
		return NULL;

	memset (&ifc, 0, sizeof (ifc));
	ifc.ifc_len = IFCONF_BUFF_SIZE;
	ifc.ifc_buf = (char *)g_malloc (IFCONF_BUFF_SIZE); /* We can't have such huge buffers on the stack. */
	if (ioctl (fd, SIOCGIFCONF, &ifc) < 0)
		goto done;

	FOREACH_IFR (ifr, ifc) {
		struct ifreq iflags;

		//only return addresses of the same type as @family
		if (ifr->ifr_addr.sa_family != family) {
			ifr->ifr_name [0] = '\0';
			continue;
		}

		strcpy (iflags.ifr_name, ifr->ifr_name);

		//ignore interfaces we can't get props for
		if (ioctl (fd, SIOCGIFFLAGS, &iflags) < 0) {
			ifr->ifr_name [0] = '\0';
			continue;
		}

		//ignore interfaces that are down
		if ((iflags.ifr_flags & IFF_UP) == 0) {
			ifr->ifr_name [0] = '\0';
			continue;
		}

		//If we have a non-loopback iface, don't return any loopback
		if ((iflags.ifr_flags & IFF_LOOPBACK) == 0) {
			ignore_loopback = TRUE;
			ifr->ifr_name [0] = 1;//1 means non-loopback
		} else {
			ifr->ifr_name [0] = 2; //2 means loopback
		}
		++if_count;
	}

	result = (char *)g_malloc (if_count * mono_address_size_for_family (family));
	result_ptr = (char *)result;
	FOREACH_IFR (ifr, ifc) {
		if (ifr->ifr_name [0] == '\0')
			continue;

		if (ignore_loopback && ifr->ifr_name [0] == 2) {
			--if_count;
			continue;
		}

		memcpy (result_ptr, get_address_from_sockaddr (&ifr->ifr_addr), mono_address_size_for_family (family));
		result_ptr += mono_address_size_for_family (family);
	}
	g_assert (result_ptr <= (char*)result + if_count * mono_address_size_for_family (family));

done:
	*interface_count = if_count;
	g_free (ifc.ifc_buf);
	close (fd);
	return result;
}

#elif defined(HAVE_GETIFADDRS)

void *
mono_get_local_interfaces (int family, int *interface_count)
{
	struct ifaddrs *ifap = NULL, *cur;
	int if_count = 0;
	gboolean ignore_loopback = FALSE;
	void *result;
	char *result_ptr;

	*interface_count = 0;

	if (!mono_address_size_for_family (family))
		return NULL;

	if (getifaddrs (&ifap))
		return NULL;

	for (cur = ifap; cur; cur = cur->ifa_next) {
		//ignore interfaces with no address assigned
		if (!cur->ifa_addr)
			continue;

		//ignore interfaces that don't belong to @family
		if (cur->ifa_addr->sa_family != family)
			continue;

		//ignore interfaces that are down
		if ((cur->ifa_flags & IFF_UP) == 0)
			continue;

		//If we have a non-loopback iface, don't return any loopback
		if ((cur->ifa_flags & IFF_LOOPBACK) == 0)
			ignore_loopback = TRUE;

		if_count++;
	}

	result_ptr = result = g_malloc (if_count * mono_address_size_for_family (family));
	for (cur = ifap; cur; cur = cur->ifa_next) {
		if (!cur->ifa_addr)
			continue;
		if (cur->ifa_addr->sa_family != family)
			continue;
		if ((cur->ifa_flags & IFF_UP) == 0)
			continue;

		//we decrement if_count because it did not on the previous loop.
		if (ignore_loopback && (cur->ifa_flags & IFF_LOOPBACK)) {
			--if_count;
			continue;
		}

		memcpy (result_ptr, get_address_from_sockaddr (cur->ifa_addr), mono_address_size_for_family (family));
		result_ptr += mono_address_size_for_family (family);
	}
	g_assert (result_ptr <= (char*)result + if_count * mono_address_size_for_family (family));

	freeifaddrs (ifap);
	*interface_count = if_count;
	return result;
}

#endif

#ifdef HAVE_GETNAMEINFO

gboolean
mono_networking_addr_to_str (MonoAddress *address, char *buffer, socklen_t buflen)
{
	MonoSocketAddress saddr;
	socklen_t len;
	mono_socket_address_init (&saddr, &len, address->family, &address->addr, 0);

	return getnameinfo (&saddr.addr, len, buffer, buflen, NULL, 0, NI_NUMERICHOST) == 0;
}

#elif HAVE_INET_NTOP

gboolean
mono_networking_addr_to_str (MonoAddress *address, char *buffer, socklen_t buflen)
{
	return inet_ntop (address->family, &address->addr, buffer, buflen) != NULL;
}

#endif

#ifndef _WIN32
// These are already defined in networking-windows.c for Windows
void
mono_networking_init (void)
{
	//nothing really
}

void
mono_networking_shutdown (void)
{
	//nothing really
}
#endif
