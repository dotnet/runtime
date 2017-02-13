/**
 * \file
 * Windows-specific networking implementations
 *
 * Author:
 *	Alexander Köplinger (alex.koeplinger@outlook.com)
 */

#include <mono/utils/networking.h>

#if defined(HOST_WIN32)

void *
mono_get_local_interfaces (int family, int *interface_count)
{
	*interface_count = 0;
	return NULL;
}

void
mono_networking_init (void)
{
	WSADATA wsadata;
	int err;

	err = WSAStartup (2 /* 2.0 */, &wsadata);
	if(err)
		g_error ("%s: Couldn't initialise networking", __func__);
}

void
mono_networking_shutdown (void)
{
	WSACleanup ();
}

#endif /* defined(HOST_WIN32) */
