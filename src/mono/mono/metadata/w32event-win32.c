/**
 * \file
 * Runtime support for managed Event on Win32
 *
 * Author:
 *	Ludovic Henry (luhenry@microsoft.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "w32event.h"

#include <windows.h>
#include <winbase.h>
#include <mono/metadata/handle.h>
#include <mono/utils/mono-error-internals.h>
#include "icall-decl.h"

void
mono_w32event_init (void)
{
}

gpointer
mono_w32event_create (gboolean manual, gboolean initial)
{
	return CreateEvent (NULL, manual, initial, NULL);
}

gboolean
mono_w32event_close (gpointer handle)
{
	return CloseHandle (handle);
}

void
mono_w32event_set (gpointer handle)
{
	SetEvent (handle);
}
