/**
 * \file
 * UWP security support for Mono.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
*/
#include <config.h>
#include <glib.h>
#include "mono/utils/mono-compiler.h"

#if G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT)
#include <windows.h>
#include "mono/metadata/mono-security-windows-internals.h"

gpointer
mono_security_principal_windows_identity_get_current_token ()
{
	g_unsupported_api ("OpenThreadToken, OpenProcessToken");

	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}

gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (MonoError *error)
{
	error_init (error);

	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "OpenThreadToken, OpenProcessToken");
	return mono_security_principal_windows_identity_get_current_token ();
}

MonoArray*
ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("GetTokenInformation");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetTokenInformation");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return NULL;
}

gpointer
ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("DuplicateToken");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "DuplicateToken");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return NULL;
}

gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken (gpointer token)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("ImpersonateLoggedOnUser");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "ImpersonateLoggedOnUser");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf (void)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("RevertToSelf");

	mono_error_set_not_supported(&mono_error, G_UNSUPPORTED_API, "RevertToSelf");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

gint32
mono_security_win_get_token_name (gpointer token, gunichar2 ** uniname)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("GetTokenInformation");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetTokenInformation");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return 0;
}

gboolean
mono_security_win_is_machine_protected (gunichar2 *path)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("GetNamedSecurityInfo, LocalFree");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetNamedSecurityInfo, LocalFree");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

gboolean
mono_security_win_is_user_protected (gunichar2 *path)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("GetNamedSecurityInfo, LocalFree");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "GetNamedSecurityInfo, LocalFree");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

gboolean
mono_security_win_protect_machine (gunichar2 *path)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("BuildTrusteeWithSid, SetEntriesInAcl, SetNamedSecurityInfo, LocalFree, FreeSid");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "BuildTrusteeWithSid, SetEntriesInAcl, SetNamedSecurityInfo, LocalFree, FreeSid");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}

gboolean
mono_security_win_protect_user (gunichar2 *path)
{
	MonoError mono_error;
	error_init (&mono_error);

	g_unsupported_api ("BuildTrusteeWithSid, SetEntriesInAcl, SetNamedSecurityInfo, LocalFree");

	mono_error_set_not_supported (&mono_error, G_UNSUPPORTED_API, "BuildTrusteeWithSid, SetEntriesInAcl, SetNamedSecurityInfo, LocalFree");
	mono_error_set_pending_exception (&mono_error);

	SetLastError (ERROR_NOT_SUPPORTED);

	return FALSE;
}
#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (mono_security_windows_uwp);
#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
