/*
 * networking-posix.c: Modern posix networking code
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#include <config.h>
#include <mono/utils/networking.h>
#include <glib.h>

#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif

#ifdef HAVE_GETADDRINFO

int
mono_get_address_info (const char *hostname, int port, int flags, MonoAddressInfo **result)
{
	char service_name [16];
	struct addrinfo hints, *res = NULL, *info;
	MonoAddressEntry *cur = NULL, *prev = NULL;
	MonoAddressInfo *addr_info;

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
    if (getaddrinfo (hostname, service_name, &hints, &info))
		return 1; /* FIXME propagate the error */

	res = info;
	*result = addr_info = g_new0 (MonoAddressInfo, 1);

	while (res) {
		cur = g_new0 (MonoAddressEntry, 1);
		if (prev)
			prev->next = cur;
		else
			addr_info->entries = cur;

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
			g_error ("Cannot handle address family %d", cur->family);
		}

		if (res->ai_canonname)
			cur->canonical_name = g_strdup (res->ai_canonname);

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
