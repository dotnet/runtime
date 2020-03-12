/**
 * \file
 * Implements missing standard socket functions.
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */

#include <mono/utils/networking.h>
#include <mono/utils/mono-compiler.h>
#include <glib.h>

#ifdef HAVE_NETDB_H
#include <netdb.h>
#endif

//wasm does have inet_pton even though autoconf fails to find
#if !defined (HAVE_INET_PTON) && !defined (HOST_WASM)

int
inet_pton (int family, const char *address, void *inaddrp)
{
	if (family == AF_INET) {
#ifdef HAVE_INET_ATON
		struct in_addr inaddr;
		
		if (!inet_aton (address, &inaddr))
			return 0;
		
		memcpy (inaddrp, &inaddr, sizeof (struct in_addr));
		return 1;
#else
		/* assume the system has inet_addr(), if it doesn't
		   have that we're pretty much screwed... */
		guint32 inaddr;
		
		if (!strcmp (address, "255.255.255.255")) {
			/* special-case hack */
			inaddr = 0xffffffff;
		} else {
			inaddr = inet_addr (address);
#ifndef INADDR_NONE
#define INADDR_NONE ((in_addr_t) -1)
#endif
			if (inaddr == INADDR_NONE)
				return 0;
		}
		
		memcpy (inaddrp, &inaddr, sizeof (guint32));
		return 1;
#endif /* HAVE_INET_ATON */
	}
	
	return -1;
}

#else /* !HAVE_INET_PTON */

MONO_EMPTY_SOURCE_FILE (networking_missing);
#endif /* !HAVE_INET_PTON */
