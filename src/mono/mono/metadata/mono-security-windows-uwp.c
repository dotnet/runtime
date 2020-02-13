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

static void
mono_security_win_not_supported (const char *functions, MonoError *error)
{
	g_unsupported_api (functions);
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, functions);
	SetLastError (ERROR_NOT_SUPPORTED);
}

gpointer
mono_security_principal_windows_identity_get_current_token (MonoError *error)
{
	// FIXME This is now supported by UWP.
	mono_security_win_not_supported ("OpenThreadToken, OpenProcessToken", error);
	return NULL;
}

gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (MonoError *error)
{
	return mono_security_principal_windows_identity_get_current_token (error);
}

MonoArrayHandle
ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token, MonoError *error)
{
	mono_security_win_not_supported ("GetTokenInformation", error);
	return NULL_HANDLE_ARRAY;
}

gpointer
ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token, MonoError *error)
{
	// FIXME This is now supported by UWP.
	mono_security_win_not_supported ("DuplicateToken", error);
	return NULL;
}

MonoBoolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken (gpointer token, MonoError *error)
{
	mono_security_win_not_supported ("ImpersonateLoggedOnUser", error);
	return FALSE;
}

MonoBoolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf (MonoError *error)
{
	mono_security_win_not_supported ("RevertToSelf", error);
	return FALSE;
}

gint32
mono_security_win_get_token_name (gpointer token, gunichar2 **uniname, MonoError *error)
{
	// FIXME This is now supported by UWP.
	mono_security_win_not_supported ("GetTokenInformation", error);
	return 0;
}

gboolean
mono_security_win_is_machine_protected (const gunichar2 *path, MonoError *error)
{
	// FIXME This is now supported by UWP.
	mono_security_win_not_supported ("GetNamedSecurityInfo, LocalFree", error);
	return FALSE;
}

gboolean
mono_security_win_is_user_protected (const gunichar2 *path, MonoError *error)
{
	// FIXME This is now supported by UWP.
	mono_security_win_not_supported ("GetNamedSecurityInfo, LocalFree", error);
	return FALSE;
}

gboolean
mono_security_win_protect_machine (const gunichar2 *path, MonoError *error)
{
	// FIXME This is now supported by UWP. Except BuildTrusteeWithSid?
	mono_security_win_not_supported ("BuildTrusteeWithSid, SetEntriesInAcl, SetNamedSecurityInfo, LocalFree, FreeSid", error);
	return FALSE;
}

gboolean
mono_security_win_protect_user (const gunichar2 *path, MonoError *error)
{
	// FIXME This is now supported by UWP. Except BuildTrusteeWithSid?
	mono_security_win_not_supported ("BuildTrusteeWithSid, SetEntriesInAcl, SetNamedSecurityInfo, LocalFree", error);
	return FALSE;
}

#else /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */

MONO_EMPTY_SOURCE_FILE (mono_security_windows_uwp);

#endif /* G_HAVE_API_SUPPORT(HAVE_UWP_WINAPI_SUPPORT) */
