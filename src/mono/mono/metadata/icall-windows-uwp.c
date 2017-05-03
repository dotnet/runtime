/**
 * \file
 * UWP icall support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
#include "mono/metadata/icall-windows-internals.h"

MonoStringHandle
mono_icall_get_machine_name (MonoError *error)
{
	g_unsupported_api ("GetComputerName");
	return mono_string_new_handle (mono_domain_get (), "mono", error);
}

MonoStringHandle
mono_icall_get_windows_folder_path (int folder, MonoError *error)
{
	error_init (error);
	g_unsupported_api ("SHGetFolderPath");
	return mono_string_new_handle (mono_domain_get (), "", error);
}

MonoArray *
mono_icall_get_logical_drives (void)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("GetLogicalDriveStrings");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetLogicalDriveStrings");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return NULL;
}

MonoBoolean
mono_icall_broadcast_setting_change (MonoError *error)
{
	error_init (error);

	g_unsupported_api ("SendMessageTimeout");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "SendMessageTimeout");

	SetLastError (ERROR_NOT_SUPPORTED);

	return is_ok (error);
}

guint32
mono_icall_drive_info_get_drive_type (MonoString *root_path_name)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("GetDriveType");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetDriveType");
	mono_error_set_pending_exception (&mono_error);

	return DRIVE_UNKNOWN;
}

gint32
mono_icall_wait_for_input_idle (gpointer handle, gint32 milliseconds)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("WaitForInputIdle");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "WaitForInputIdle");
	mono_error_set_pending_exception (&mono_error);

	return WAIT_TIMEOUT;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (icall_windows_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
