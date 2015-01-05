/*
 * networking.c: Portable networking functions
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#include <config.h>
#include <mono/utils/networking.h>
#include <glib.h>

void
mono_free_address_info (MonoAddressInfo *ai)
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


/* port in host order, address in network order */
void
mono_socket_address_init (MonoSocketAddress *sa, socklen_t *len, int family, const void *address, int port)
{
	memset (sa, 0, sizeof (MonoSocketAddress));
	if (family == AF_INET) {
		*len = sizeof (struct in_addr);

		sa->v4.sin_family = family;
		sa->v4.sin_addr = *(struct in_addr*)address;
		sa->v4.sin_port = htons (port);
#if HAVE_SOCKADDR_IN_SIN_LEN
		sa->v4.sin_len = sizeof (*len);
#endif
	} else if (family == AF_INET6) {
		*len = sizeof (struct in6_addr);

		sa->v6.sin6_family = family;
		sa->v6.sin6_addr = *(struct in6_addr*)address;
		sa->v6.sin6_port = htons (port);
#if HAVE_SOCKADDR_IN6_SIN_LEN
		sa->v6.sin6_len = sizeof (*len);
#endif
	} else {
		g_error ("Cannot handle address family %d", family);
	}
}
