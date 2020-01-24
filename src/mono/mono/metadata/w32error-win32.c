/**
 * \file
 */

#include <windows.h>

#include "w32error.h"

guint32
mono_w32error_get_last (void)
{
	return GetLastError ();
}

void
mono_w32error_set_last (guint32 error)
{
	SetLastError (error);
}

guint32
mono_w32error_unix_to_win32 (guint32 error)
{
	g_assert_not_reached ();
}
