/**
 * \file
 * Portable networking functions
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#include <mono/utils/mono-threads-coop.h>
#include <glib.h>

#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif
#ifdef HAVE_SYS_IOCTL_H
#include <sys/ioctl.h>
#endif
#ifdef HAVE_SYS_SOCKET_H
#include <sys/socket.h>
#endif
#ifdef HAVE_NET_IF_H
#include <net/if.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include "debugger-networking.h"


void
mono_debugger_networking_init (void)
{
#ifdef HOST_WIN32
	WSADATA wsadata;
	int err;

	err = WSAStartup (2 /* 2.0 */, &wsadata);
	if(err)
		g_error ("%s: Couldn't initialise networking", __func__);
#endif
}

void
mono_debugger_networking_shutdown (void)
{
#ifdef HOST_WIN32
	WSACleanup ();
#endif
}


/* port in host order, address in network order */
void
mono_debugger_socket_address_init (MonoSocketAddress *sa, socklen_t *len, int family, const void *address, int port)
{
	memset (sa, 0, sizeof (MonoSocketAddress));
	if (family == AF_INET) {
		*len = sizeof (struct sockaddr_in);

		sa->v4.sin_family = AF_INET;
		sa->v4.sin_addr = *(struct in_addr*)address;
		sa->v4.sin_port = htons (GINT_TO_UINT16 (port));
#if HAVE_SOCKADDR_IN_SIN_LEN
		sa->v4.sin_len = sizeof (*len);
#endif
#ifdef HAVE_STRUCT_SOCKADDR_IN6
	} else if (family == AF_INET6) {
		*len = sizeof (struct sockaddr_in6);

		sa->v6.sin6_family = AF_INET6;
		sa->v6.sin6_addr = *(struct in6_addr*)address;
		sa->v6.sin6_port = htons (GINT_TO_UINT16 (port));
#if HAVE_SOCKADDR_IN6_SIN_LEN
		sa->v6.sin6_len = sizeof (*len);
#endif
#endif
	} else {
		g_error ("Cannot handle address family %d", family);
	}
}


void
mono_debugger_free_address_info (MonoAddressInfo *ai)
{
	MonoAddressEntry *cur = ai->entries, *next;
	while (cur) {
		next = cur->next;
		g_free ((void*)cur->canonical_name);
		g_free (cur);
		cur = next;
	}
	g_strfreev (ai->aliases);
	g_free (ai);
}


#if !defined (HAVE_GETADDRINFO) && (defined (HAVE_GETHOSTBYNAME) || defined (HAVE_GETHOSTBYNAME2))
static void
add_hostent (MonoAddressInfo *info, int flags, struct hostent *h)
{
	MonoAddressEntry *cur, *prev = info->entries;
	int idx = 0;

	if (!h)
		return;

	if (!info->aliases)
		info->aliases = g_strdupv (h->h_aliases);

	while (h->h_addr_list [idx]) {
		cur = g_new0 (MonoAddressEntry, 1);
		if (prev)
			prev->next = cur;
		else
			info->entries = cur;

		if (flags & MONO_HINT_CANONICAL_NAME && h->h_name)
			cur->canonical_name = g_strdup (h->h_name);

		cur->family = h->h_addrtype;
		cur->socktype = SOCK_STREAM;
		cur->protocol = 0; /* Zero means the default stream protocol */
		cur->address_len = h->h_length;
		memcpy (&cur->address, h->h_addr_list [idx], h->h_length);

		prev = cur;
		++idx;
	}
}
#endif /* !defined (HAVE_GETADDRINFO) && (defined (HAVE_GETHOSTBYNAME) || defined (HAVE_GETHOSTBYNAME2)) */


int
mono_debugger_get_address_info (const char *hostname, int port, int flags, MonoAddressInfo **result)
{
#if defined (HAVE_GETADDRINFO) /* modern posix networking code */
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
	if (flags & MONO_HINT_NUMERIC_HOST)
		hints.ai_flags |= AI_NUMERICHOST;

/* Some ancient libc don't define AI_ADDRCONFIG */
#ifdef AI_ADDRCONFIG
	if (flags & MONO_HINT_CONFIGURED_ONLY)
		hints.ai_flags |= AI_ADDRCONFIG;
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
#ifdef HAVE_STRUCT_SOCKADDR_IN6
		} else if (cur->family == PF_INET6) {
			cur->address_len = sizeof (struct in6_addr);
			cur->address.v6 = ((struct sockaddr_in6*)res->ai_addr)->sin6_addr;
#endif
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

#elif defined (HAVE_GETHOSTBYNAME) || defined (HAVE_GETHOSTBYNAME2) /* fallback networking code that relies on old BSD apis or whatever else is available */
	MonoAddressInfo *addr_info;
	addr_info = g_new0 (MonoAddressInfo, 1);

#ifdef HAVE_GETHOSTBYNAME2
	if (flags & MONO_HINT_IPV6 || flags & MONO_HINT_UNSPECIFIED)
		add_hostent (addr_info, flags, gethostbyname2 (hostname, AF_INET6));
	if (flags & MONO_HINT_IPV4 || flags & MONO_HINT_UNSPECIFIED)
		add_hostent (addr_info, flags, gethostbyname2 (hostname, AF_INET));
#else
	add_hostent (addr_info, flags, gethostbyname (hostname))
#endif

	if (!addr_info->entries) {
		*result = NULL;
		mono_debugger_free_address_info (addr_info);
		return 1;
	}

	*result = addr_info;
	return 0;

#else
	g_error ("No networking implementation available");
	return 1;
#endif /* defined (HAVE_GETADDRINFO) */
}
