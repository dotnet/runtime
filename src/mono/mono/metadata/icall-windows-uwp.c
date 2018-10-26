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
#include <windows.h>
#include "mono/metadata/icall-windows-internals.h"
#include "mono/metadata/w32subset.h"

#if !HAVE_API_SUPPORT_WIN32_GET_COMPUTER_NAME
MonoStringHandle
mono_icall_get_machine_name (MonoError *error)
{
	g_unsupported_api ("GetComputerName");
	return mono_string_new_handle (mono_domain_get (), "mono", error);
}
#endif

#if !HAVE_API_SUPPORT_WIN32_SH_GET_FOLDER_PATH
MonoStringHandle
mono_icall_get_windows_folder_path (int folder, MonoError *error)
{
	error_init (error);
	g_unsupported_api ("SHGetFolderPath");
	return mono_string_new_handle (mono_domain_get (), "", error);
}
#endif

#if !HAVE_API_SUPPORT_WIN32_GET_LOGICAL_DRIVE_STRINGS
MonoArray *
mono_icall_get_logical_drives (void)
{
	ERROR_DECL_VALUE (mono_error);
	error_init (&mono_error);

	g_unsupported_api ("GetLogicalDriveStrings");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetLogicalDriveStrings");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return NULL;
}
#endif

#if !HAVE_API_SUPPORT_WIN32_SEND_MESSAGE_TIMEOUT
ICALL_EXPORT void
ves_icall_System_Environment_BroadcastSettingChange (MonoError *error)
{
	g_unsupported_api ("SendMessageTimeout");

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "SendMessageTimeout");

	SetLastError (ERROR_NOT_SUPPORTED);
}
#endif

#if !HAVE_API_SUPPORT_WIN32_GET_DRIVE_TYPE
guint32
mono_icall_drive_info_get_drive_type (MonoString *root_path_name)
{
	ERROR_DECL_VALUE (mono_error);
	error_init (&mono_error);

	g_unsupported_api ("GetDriveType");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetDriveType");
	mono_error_set_pending_exception (&mono_error);

	return DRIVE_UNKNOWN;
}
#endif

#if !HAVE_API_SUPPORT_WIN32_WAIT_FOR_INPUT_IDLE
gint32
mono_icall_wait_for_input_idle (gpointer handle, gint32 milliseconds)
{
	ERROR_DECL_VALUE (mono_error);
	error_init (&mono_error);

	g_unsupported_api ("WaitForInputIdle");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "WaitForInputIdle");
	mono_error_set_pending_exception (&mono_error);

	return WAIT_TIMEOUT;
}
#endif

MONO_EMPTY_SOURCE_FILE (icall_windows_uwp);
