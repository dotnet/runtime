/**
 * \file
 * Windows security support.
 *
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <glib.h>

#if defined(HOST_WIN32)
#include <winsock2.h>
#include <windows.h>
#include "mono/metadata/mono-security-windows-internals.h"

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
#include <aclapi.h>
#include <accctrl.h>
#endif

#ifndef PROTECTED_DACL_SECURITY_INFORMATION
#define PROTECTED_DACL_SECURITY_INFORMATION	0x80000000L
#endif

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static gunichar2*
GetSidName (gunichar2 *server, PSID sid, gint32 *size)
{
	gunichar2 *uniname = NULL;
	DWORD cchName = 0;
	DWORD cchDomain = 0;
	SID_NAME_USE peUse; /* out */

	LookupAccountSid (server, sid, NULL, &cchName, NULL,
		&cchDomain, &peUse);

	if ((cchName > 0) && (cchDomain > 0)) {
		gunichar2 *user = g_malloc0 ((cchName + 1) * 2);
		gunichar2 *domain = g_malloc0 ((cchDomain + 1) * 2);

		LookupAccountSid (server, sid, user, &cchName, domain,
			&cchDomain, &peUse);

		if (cchName > 0) {
			if (cchDomain > 0) {
				/* domain/machine name included (+ sepearator) */
				*size = cchName + cchDomain + 1;
				uniname = g_malloc0 ((*size + 1) * 2);
				memcpy (uniname, domain, cchDomain * 2);
				*(uniname + cchDomain) = '\\';
				memcpy (uniname + cchDomain + 1, user, cchName * 2);
				g_free (user);
			}
			else {
				/* no domain / machine */
				*size = cchName;
				uniname = user;
			}
		}
		else {
			/* nothing -> return NULL */
			g_free (user);
		}

		g_free (domain);
	}

	return uniname;
}

gpointer
mono_security_principal_windows_identity_get_current_token (void)
{
	gpointer token = NULL;

	/* Note: This isn't a copy of the Token - we must not close it!!!
	 * http://www.develop.com/kbrown/book/html/whatis_windowsprincipal.html
	 */

	/* thread may be impersonating somebody */
	if (OpenThreadToken (GetCurrentThread (), MAXIMUM_ALLOWED, 1, &token) == 0) {
		/* if not take the process identity */
		OpenProcessToken (GetCurrentProcess (), MAXIMUM_ALLOWED, &token);
	}

	return token;
}

gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (MonoError *error)
{
	error_init (error);
	return mono_security_principal_windows_identity_get_current_token ();
}

gint32
mono_security_win_get_token_name (gpointer token, gunichar2 ** uniname)
{
	gint32 size = 0;

	GetTokenInformation (token, TokenUser, NULL, size, (PDWORD)&size);
	if (size > 0) {
		TOKEN_USER *tu = g_malloc0 (size);
		if (GetTokenInformation (token, TokenUser, tu, size, (PDWORD)&size)) {
			*uniname = GetSidName (NULL, tu->User.Sid, &size);
		}
		g_free (tu);
	}

	return size;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

MonoStringHandle
ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName (gpointer token, MonoError *error)
{
	MonoStringHandle result;
	gunichar2 *uniname = NULL;
	gint32 size = 0;

	error_init (error);

	size = mono_security_win_get_token_name (token, &uniname);

	if (size > 0) {
		result = mono_string_new_utf16_handle (mono_domain_get (), uniname, size, error);
	}
	else
		result = mono_string_new_handle (mono_domain_get (), "", error);

	if (uniname)
		g_free (uniname);

	return result;
}

gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetUserToken (MonoStringHandle username, MonoError *error)
{
	error_init (error);
	gpointer token = NULL;

	/* TODO: MS has something like this working in Windows 2003 (client and
	 * server) but works only for domain accounts (so it's quite limiting).
	 * http://www.develop.com/kbrown/book/html/howto_logonuser.html
	 */
	g_warning ("Unsupported on Win32 (anyway requires W2K3 minimum)");
	return token;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
MonoArray*
ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token)
{
	MonoError error;
	MonoArray *array = NULL;
	MonoDomain *domain = mono_domain_get ();

	gint32 size = 0;

	GetTokenInformation (token, TokenGroups, NULL, size, (PDWORD)&size);
	if (size > 0) {
		TOKEN_GROUPS *tg = g_malloc0 (size);
		if (GetTokenInformation (token, TokenGroups, tg, size, (PDWORD)&size)) {
			int i=0;
			int num = tg->GroupCount;

			array = mono_array_new_checked (domain, mono_get_string_class (), num, &error);
			if (mono_error_set_pending_exception (&error)) {
				g_free (tg);
				return NULL;
			}

			for (i=0; i < num; i++) {
				gint32 size = 0;
				gunichar2 *uniname = GetSidName (NULL, tg->Groups [i].Sid, &size);

				if (uniname) {
					MonoString *str = mono_string_new_utf16_checked (domain, uniname, size, &error);
					if (!is_ok (&error)) {
						g_free (uniname);
						g_free (tg);
						mono_error_set_pending_exception (&error);
						return NULL;
					}
					mono_array_setref (array, i, str);
					g_free (uniname);
				}
			}
		}
		g_free (tg);
	}

	if (!array) {
		/* return empty array of string, i.e. string [0] */
		array = mono_array_new_checked (domain, mono_get_string_class (), 0, &error);
		mono_error_set_pending_exception (&error);
	}
	return array;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken (gpointer token)
{
	gboolean result = TRUE;
	result = (CloseHandle (token) != 0);
	return result;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
gpointer
ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token)
{
	gpointer dupe = NULL;

	if (DuplicateToken (token, SecurityImpersonation, &dupe) == 0) {
		dupe = NULL;
	}
	return dupe;
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

gboolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupId (gpointer user, gpointer group)
{
	gboolean result = FALSE;

	/* The convertion from an ID to a string is done in managed code for Windows */
	g_warning ("IsMemberOfGroupId should never be called on Win32");
	return result;
}

gboolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName (gpointer user, MonoString *group)
{
	gboolean result = FALSE;

	/* Windows version use a cache built using WindowsIdentity._GetRoles */
	g_warning ("IsMemberOfGroupName should never be called on Win32");
	return result;
}

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
static PSID
GetAdministratorsSid (void)
{
	SID_IDENTIFIER_AUTHORITY admins = { SECURITY_NT_AUTHORITY };
	PSID pSid = NULL;
	if (!AllocateAndInitializeSid (&admins, 2, SECURITY_BUILTIN_DOMAIN_RID,
		DOMAIN_ALIAS_RID_ADMINS, 0, 0, 0, 0, 0, 0, &pSid))
		return NULL;
	/* Note: this SID must be freed with FreeSid () */
	return pSid;
}

static PSID
GetEveryoneSid (void)
{
	SID_IDENTIFIER_AUTHORITY everyone = { SECURITY_WORLD_SID_AUTHORITY };
	PSID pSid = NULL;
	if (!AllocateAndInitializeSid (&everyone, 1, SECURITY_WORLD_RID, 0, 0, 0, 0, 0, 0, 0, &pSid))
		return NULL;
	/* Note: this SID must be freed with FreeSid () */
	return pSid;
}

static PSID
GetCurrentUserSid (void)
{
	PSID sid = NULL;
	guint32 size = 0;
	gpointer token = mono_security_principal_windows_identity_get_current_token ();

	GetTokenInformation (token, TokenUser, NULL, size, (PDWORD)&size);
	if (size > 0) {
		TOKEN_USER *tu = g_malloc0 (size);
		if (GetTokenInformation (token, TokenUser, tu, size, (PDWORD)&size)) {
			DWORD length = GetLengthSid (tu->User.Sid);
			sid = (PSID) g_malloc0 (length);
			if (!CopySid (length, sid, tu->User.Sid)) {
				g_free (sid);
				sid = NULL;
			}
		}
		g_free (tu);
	}
	/* Note: this SID must be freed with g_free () */
	return sid;
}

static ACCESS_MASK
GetRightsFromSid (PSID sid, PACL acl)
{
	ACCESS_MASK rights = 0;
	TRUSTEE trustee;

	BuildTrusteeWithSidW (&trustee, sid);
	if (GetEffectiveRightsFromAcl (acl, &trustee, &rights) != ERROR_SUCCESS)
		return 0;

	return rights;
}

gboolean
mono_security_win_is_machine_protected (gunichar2 *path)
{
	gboolean success = FALSE;
	PACL pDACL = NULL;
	PSECURITY_DESCRIPTOR pSD = NULL;
	PSID pEveryoneSid = NULL;

	DWORD dwRes = GetNamedSecurityInfoW (path, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION, NULL, NULL, &pDACL, NULL, &pSD);
	if (dwRes != ERROR_SUCCESS)
		return FALSE;

	/* We check that Everyone is still limited to READ-ONLY -
	but not if new entries have been added by an Administrator */

	pEveryoneSid = GetEveryoneSid ();
	if (pEveryoneSid) {
		ACCESS_MASK rights = GetRightsFromSid (pEveryoneSid, pDACL);
		/* http://msdn.microsoft.com/library/en-us/security/security/generic_access_rights.asp?frame=true */
		success = (rights == (READ_CONTROL | SYNCHRONIZE | FILE_READ_DATA | FILE_READ_EA | FILE_READ_ATTRIBUTES));
		FreeSid (pEveryoneSid);
	}
	/* Note: we don't need to check our own access -
	we'll know soon enough when reading the file */

	if (pSD)
		LocalFree (pSD);

	return success;
}

gboolean
mono_security_win_is_user_protected (gunichar2 *path)
{
	gboolean success = FALSE;
	PACL pDACL = NULL;
	PSID pEveryoneSid = NULL;
	PSECURITY_DESCRIPTOR pSecurityDescriptor = NULL;

	DWORD dwRes = GetNamedSecurityInfoW (path, SE_FILE_OBJECT,
		DACL_SECURITY_INFORMATION, NULL, NULL, &pDACL, NULL, &pSecurityDescriptor);
	if (dwRes != ERROR_SUCCESS)
		return FALSE;

	/* We check that our original entries in the ACL are in place -
	but not if new entries have been added by the user */

	/* Everyone should be denied */
	pEveryoneSid = GetEveryoneSid ();
	if (pEveryoneSid) {
		ACCESS_MASK rights = GetRightsFromSid (pEveryoneSid, pDACL);
		success = (rights == 0);
		FreeSid (pEveryoneSid);
	}
	/* Note: we don't need to check our own access -
	we'll know soon enough when reading the file */

	if (pSecurityDescriptor)
		LocalFree (pSecurityDescriptor);

	return success;
}

gboolean
mono_security_win_protect_machine (gunichar2 *path)
{
	PSID pEveryoneSid = GetEveryoneSid ();
	PSID pAdminsSid = GetAdministratorsSid ();
	DWORD retval = -1;

	if (pEveryoneSid && pAdminsSid) {
		PACL pDACL = NULL;
		EXPLICIT_ACCESS ea [2];
		ZeroMemory (&ea, 2 * sizeof (EXPLICIT_ACCESS));

		/* grant all access to the BUILTIN\Administrators group */
		BuildTrusteeWithSidW (&ea [0].Trustee, pAdminsSid);
		ea [0].grfAccessPermissions = GENERIC_ALL;
		ea [0].grfAccessMode = SET_ACCESS;
		ea [0].grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
		ea [0].Trustee.TrusteeForm = TRUSTEE_IS_SID;
		ea [0].Trustee.TrusteeType = TRUSTEE_IS_WELL_KNOWN_GROUP;

		/* read-only access everyone */
		BuildTrusteeWithSidW (&ea [1].Trustee, pEveryoneSid);
		ea [1].grfAccessPermissions = GENERIC_READ;
		ea [1].grfAccessMode = SET_ACCESS;
		ea [1].grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
		ea [1].Trustee.TrusteeForm = TRUSTEE_IS_SID;
		ea [1].Trustee.TrusteeType = TRUSTEE_IS_WELL_KNOWN_GROUP;

		retval = SetEntriesInAcl (2, ea, NULL, &pDACL);
		if (retval == ERROR_SUCCESS) {
			/* with PROTECTED_DACL_SECURITY_INFORMATION we */
			/* remove any existing ACL (like inherited ones) */
			retval = SetNamedSecurityInfo (path, SE_FILE_OBJECT,
				DACL_SECURITY_INFORMATION | PROTECTED_DACL_SECURITY_INFORMATION,
				NULL, NULL, pDACL, NULL);
		}
		if (pDACL)
			LocalFree (pDACL);
	}

	if (pEveryoneSid)
		FreeSid (pEveryoneSid);
	if (pAdminsSid)
		FreeSid (pAdminsSid);
	return (retval == ERROR_SUCCESS);
}

gboolean
mono_security_win_protect_user (gunichar2 *path)
{
	DWORD retval = -1;

	PSID pCurrentSid = GetCurrentUserSid ();
	if (pCurrentSid) {
		PACL pDACL = NULL;
		EXPLICIT_ACCESS ea;
		ZeroMemory (&ea, sizeof (EXPLICIT_ACCESS));

		/* grant exclusive access to the current user */
		BuildTrusteeWithSidW (&ea.Trustee, pCurrentSid);
		ea.grfAccessPermissions = GENERIC_ALL;
		ea.grfAccessMode = SET_ACCESS;
		ea.grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
		ea.Trustee.TrusteeForm = TRUSTEE_IS_SID;
		ea.Trustee.TrusteeType = TRUSTEE_IS_USER;

		retval = SetEntriesInAcl (1, &ea, NULL, &pDACL);
		if (retval == ERROR_SUCCESS) {
			/* with PROTECTED_DACL_SECURITY_INFORMATION we
			   remove any existing ACL (like inherited ones) */
			retval = SetNamedSecurityInfo (path, SE_FILE_OBJECT,
				DACL_SECURITY_INFORMATION | PROTECTED_DACL_SECURITY_INFORMATION,
				NULL, NULL, pDACL, NULL);
		}

		if (pDACL)
			LocalFree (pDACL);
		g_free (pCurrentSid); /* g_malloc0 */
	}

	return (retval == ERROR_SUCCESS);
}
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure (MonoString *root)
{
	gint32 flags;

	/* ACL are nice... unless you have FAT or other uncivilized filesystem */
	if (!GetVolumeInformation (mono_string_chars (root), NULL, 0, NULL, NULL, (LPDWORD)&flags, NULL, 0))
		return FALSE;
	return ((flags & FS_PERSISTENT_ACLS) == FS_PERSISTENT_ACLS);
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected (MonoString *path)
{
	gboolean ret = FALSE;

	/* no one, but the owner, should have write access to the directory */
	ret = mono_security_win_is_machine_protected (mono_string_chars (path));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected (MonoString *path)
{
	gboolean ret = FALSE;

	/* no one, but the user, should have access to the directory */
	ret = mono_security_win_is_user_protected (mono_string_chars (path));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine (MonoString *path)
{
	gboolean ret = FALSE;

	/* read/write to owner, read to everyone else */
	ret = mono_security_win_protect_machine (mono_string_chars (path));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser (MonoString *path)
{
	gboolean ret = FALSE;

	/* read/write to user, no access to everyone else */
	ret = mono_security_win_protect_user (mono_string_chars (path));
	return (MonoBoolean)ret;
}
#endif /* HOST_WIN32 */
