/**
 * \file
 * Portable networking functions
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

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

