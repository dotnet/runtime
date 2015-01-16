/*
 * networking-windows.c: Windows-specific networking implementations
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

#endif /* defined(HOST_WIN32) */
