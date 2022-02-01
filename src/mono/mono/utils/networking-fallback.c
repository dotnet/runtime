/**
 * \file
 * Fallback networking code that rely on old BSD apis or whatever else is available.
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#include <mono/utils/networking.h>
#include <glib.h>
#include <mono/utils/mono-compiler.h>

#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif

#if !defined (HAVE_GETADDRINFO) 

#if defined (HAVE_GETHOSTBYNAME) || defined (HAVE_GETHOSTBYNAME2)

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

int
mono_get_address_info (const char *hostname, int port, int flags, MonoAddressInfo **result)
{
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
		mono_free_address_info (addr_info);
		return 1;		
	}

	*result = addr_info;
	return 0;
}

#endif /* defined (HAVE_GETHOSTBYNAME) || defined (HAVE_GETHOSTBYNAME2) */
#else /* !defined (HAVE_GETADDRINFO) */

MONO_EMPTY_SOURCE_FILE (networking_fallback);
#endif /* !defined (HAVE_GETADDRINFO) */
