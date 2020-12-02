/**
 * \file
 * Security internal calls
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/image-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/security.h>
#include <mono/utils/strenc.h>
#include <mono/utils/w32subset.h>
#include "reflection-internals.h"
#include "icall-decl.h"

#ifndef ENABLE_NETCORE

#ifndef HOST_WIN32
#ifdef HAVE_GRP_H
#include <grp.h>
#endif
#ifdef HAVE_PWD_H
#include <pwd.h>
#endif
#include <string.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>
#include "icall-decl.h"

/* Disclaimers */

#if defined(__GNUC__)

#ifdef HAVE_GRP_H
#ifndef HAVE_GETGRGID_R
	#warning Non-thread safe getgrgid being used!
#endif
#ifndef HAVE_GETGRNAM_R
	#warning Non-thread safe getgrnam being used!
#endif
#endif

#ifdef HAVE_PWD_H
#ifndef HAVE_GETPWNAM_R
	#warning Non-thread safe getpwnam being used!
#endif
#ifndef HAVE_GETPWUID_R
	#warning Non-thread safe getpwuid being used!
#endif
#endif
#endif /* defined(__GNUC__) */
#endif /* !HOST_WIN32 */

/* internal functions - reuse driven */

/* ask a server to translate a SID into a textual representation */
#ifndef HOST_WIN32
#define MONO_SYSCONF_DEFAULT_SIZE	((size_t) 1024)

/*
 * Ensure we always get a valid (usable) value from sysconf.
 * In case of error, we return the default value.
 */
static size_t mono_sysconf (int name)
{
#ifdef HAVE_SYSCONF
	size_t size = (size_t) sysconf (name);
	/* default value */
	return (size == -1) ? MONO_SYSCONF_DEFAULT_SIZE : size;
#else
	return MONO_SYSCONF_DEFAULT_SIZE;
#endif
}

static gchar*
GetTokenName (uid_t uid)
{
	gchar *uname = NULL;

#ifdef HAVE_PWD_H

#ifdef HAVE_GETPWUID_R
	struct passwd pwd;
	size_t fbufsize;
	gchar *fbuf;
	gint32 retval;
#endif
	struct passwd *p = NULL;
	gboolean result;

#ifdef HAVE_GETPWUID_R
#ifdef _SC_GETPW_R_SIZE_MAX
 	fbufsize = mono_sysconf (_SC_GETPW_R_SIZE_MAX);
#else
	fbufsize = MONO_SYSCONF_DEFAULT_SIZE;
#endif
	fbuf = (gchar *)g_malloc0 (fbufsize);
	retval = getpwuid_r (uid, &pwd, fbuf, fbufsize, &p);
	result = ((retval == 0) && (p == &pwd));
#else
	/* default to non thread-safe but posix compliant function */
	p = getpwuid (uid);
	result = (p != NULL);
#endif

	if (result) {
		uname = g_strdup (p->pw_name);
	}

#ifdef HAVE_GETPWUID_R
	g_free (fbuf);
#endif

#endif /* HAVE_PWD_H */

	return uname;
}

#ifdef HAVE_GRP_H

static gboolean
IsMemberInList (uid_t user, struct group *g) 
{
	gboolean result = FALSE;
	gchar *utf8_username = GetTokenName (user);

	if (!utf8_username)
		return FALSE;

	if (g) {
		gchar **users = g->gr_mem;

		while (*users) {
			gchar *u = *(users);
			if (strcmp (utf8_username, u) == 0) {
				result = TRUE;
				break;
			}
			users++;
		}
	}		

	g_free (utf8_username);
	return result;
}

#endif /* HAVE_GRP_H */

static gboolean
IsDefaultGroup (uid_t user, gid_t group)
{
	gboolean result = FALSE;

#ifdef HAVE_PWD_H

#ifdef HAVE_GETPWUID_R
	struct passwd pwd;
	size_t fbufsize;
	gchar *fbuf;
	gint32 retval;
#endif
	struct passwd *p = NULL;

#ifdef HAVE_GETPWUID_R
#ifdef _SC_GETPW_R_SIZE_MAX
 	fbufsize = mono_sysconf (_SC_GETPW_R_SIZE_MAX);
#else
	fbufsize = MONO_SYSCONF_DEFAULT_SIZE;
#endif

	fbuf = (gchar *)g_malloc0 (fbufsize);
	retval = getpwuid_r (user, &pwd, fbuf, fbufsize, &p);
	result = ((retval == 0) && (p == &pwd));
#else
	/* default to non thread-safe but posix compliant function */
	p = getpwuid (user);
	result = (p != NULL);
#endif

	if (result) {
		result = (p->pw_gid == group);
	}

#ifdef HAVE_GETPWUID_R
	g_free (fbuf);
#endif

#endif /* HAVE_PWD_H */

	return result;
}

#ifdef HAVE_GRP_H

static gboolean
IsMemberOf (gid_t user, struct group *g) 
{
	if (!g)
		return FALSE;

	/* is it the user default group */
	if (IsDefaultGroup (user, g->gr_gid))
		return TRUE;

	/* is the user in the group list */
	return IsMemberInList (user, g);
}

#endif /* HAVE_GRP_H */

#endif /* !HOST_WIN32 */

/* ICALLS */

/* System.Security.Principal.WindowsIdentity */

#ifndef HOST_WIN32
gpointer
mono_security_principal_windows_identity_get_current_token (MonoError *error)
{
	return GINT_TO_POINTER (geteuid ());
}

gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (MonoError *error)
{
	return mono_security_principal_windows_identity_get_current_token (error);
}

static gint32
internal_get_token_name (gpointer token, gunichar2 ** uniname)
{
	gint32 size = 0;

	gchar *uname = GetTokenName ((uid_t) GPOINTER_TO_INT (token));

	if (uname) {
		size = strlen (uname);
		*uniname = g_utf8_to_utf16 (uname, size, NULL, NULL, NULL);
		g_free (uname);
	}

	return size;
}

MonoStringHandle
ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName (gpointer token, MonoError *error)
{
	MonoStringHandle result;
	gunichar2 *uniname = NULL;
	gint32 size = 0;

	error_init (error);

	size = internal_get_token_name (token, &uniname);

	if (size > 0) {
		result = mono_string_new_utf16_handle (mono_domain_get (), uniname, size, error);
	}
	else
		result = mono_string_new_handle (mono_domain_get (), "", error);

	if (uniname)
		g_free (uniname);

	return result;
}
#endif  /* !HOST_WIN32 */

#ifndef HOST_WIN32
gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetUserToken (MonoStringHandle username, MonoError *error)
{
	gpointer token = (gpointer)-2;

	error_init (error);
#if defined (HAVE_PWD_H) && !defined (HOST_WASM)

#ifdef HAVE_GETPWNAM_R
	struct passwd pwd;
	size_t fbufsize;
	gchar *fbuf;
	gint32 retval;
#endif
	struct passwd *p;
	gchar *utf8_name;
	gboolean result;

	utf8_name = mono_string_handle_to_utf8 (username, error);
	return_val_if_nok (error, NULL);

#ifdef HAVE_GETPWNAM_R
#ifdef _SC_GETPW_R_SIZE_MAX
 	fbufsize = mono_sysconf (_SC_GETPW_R_SIZE_MAX);
#else
	fbufsize = MONO_SYSCONF_DEFAULT_SIZE;
#endif

	fbuf = (gchar *)g_malloc0 (fbufsize);
	retval = getpwnam_r (utf8_name, &pwd, fbuf, fbufsize, &p);
	result = ((retval == 0) && (p == &pwd));
#else
	/* default to non thread-safe but posix compliant function */
	p = getpwnam (utf8_name);
	result = (p != NULL);
#endif

	if (result) {
		token = GINT_TO_POINTER (p->pw_uid);
	}

#ifdef HAVE_GETPWNAM_R
	g_free (fbuf);
#endif
	g_free (utf8_name);

#endif /* HAVE_PWD_H */

	return token;
}
#endif /* HOST_WIN32 */

/* http://www.dotnet247.com/247reference/msgs/39/195403.aspx
// internal static string[] WindowsIdentity._GetRoles (IntPtr token)
*/

#ifndef HOST_WIN32
MonoArrayHandle
ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token, MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();

	/* POSIX-compliant systems should use IsMemberOfGroupId or IsMemberOfGroupName */
	g_warning ("WindowsIdentity._GetRoles should never be called on POSIX");

	return mono_array_new_handle (domain, mono_get_string_class (), 0, error);
}
#endif /* !HOST_WIN32 */

/* System.Security.Principal.WindowsImpersonationContext */

#ifndef HOST_WIN32
MonoBoolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken (gpointer token, MonoError *error)
{
	return TRUE;
}
#endif /* !HOST_WIN32 */

#ifndef HOST_WIN32
gpointer
ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token, MonoError *error)
{
	return token;
}
#endif /* !HOST_WIN32 */

#if HAVE_API_SUPPORT_WIN32_SECURITY
MonoBoolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken (gpointer token, MonoError *error)
{
#ifdef HOST_WIN32
	return (ImpersonateLoggedOnUser (token) != 0);
#else
	uid_t itoken = (uid_t) GPOINTER_TO_INT (token);
#ifdef HAVE_SETRESUID
	if (setresuid (-1, itoken, getuid ()) < 0)
		return FALSE;
#endif
	return geteuid () == itoken;
#endif
}

MonoBoolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf (MonoError *error)
{
#ifdef HOST_WIN32
	return (RevertToSelf () != 0);
#else
#ifdef HAVE_GETRESUID
	uid_t ruid, euid;
#endif
	uid_t suid = -1;

#ifdef HAVE_GETRESUID
	if (getresuid (&ruid, &euid, &suid) < 0)
		return FALSE;
#endif
#ifdef HAVE_SETRESUID
	if (setresuid (-1, suid, -1) < 0)
		return FALSE;
#else
	return TRUE;
#endif
	return geteuid () == suid;
#endif
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SECURITY
MonoBoolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken (gpointer token, MonoError *error)
{
	g_unsupported_api ("ImpersonateLoggedOnUser");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "ImpersonateLoggedOnUser");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}

MonoBoolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf (MonoError *error)
{
	g_unsupported_api ("RevertToSelf");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "RevertToSelf");
	SetLastError (ERROR_NOT_SUPPORTED);
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_SECURITY */

/* System.Security.Principal.WindowsPrincipal */

#ifndef HOST_WIN32
MonoBoolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupId (gpointer user, gpointer group, MonoError *error)
{
	gboolean result = FALSE;

#ifdef HAVE_GRP_H

#ifdef HAVE_GETGRGID_R
	struct group grp;
	size_t fbufsize;
	gchar *fbuf;
	gint32 retval;
#endif
	struct group *g = NULL;

#ifdef HAVE_GETGRGID_R
#ifdef _SC_GETGR_R_SIZE_MAX
 	fbufsize = mono_sysconf (_SC_GETGR_R_SIZE_MAX);
#else
	fbufsize = MONO_SYSCONF_DEFAULT_SIZE;
#endif
	fbuf = (gchar *)g_malloc0 (fbufsize);
	retval = getgrgid_r ((gid_t) GPOINTER_TO_INT (group), &grp, fbuf, fbufsize, &g);
	result = ((retval == 0) && (g == &grp));
#else
	/* default to non thread-safe but posix compliant function */
	g = getgrgid ((gid_t) GPOINTER_TO_INT (group));
	result = (g != NULL);
#endif

	if (result)
		result = IsMemberOf ((uid_t) GPOINTER_TO_INT (user), g);

#ifdef HAVE_GETGRGID_R
	g_free (fbuf);
#endif

#endif /* HAVE_GRP_H */

	return result;
}

MonoBoolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName (gpointer user, const gchar *utf8_groupname, MonoError *error)
{
	gboolean result = FALSE;

#ifdef HAVE_GRP_H

	if (utf8_groupname) {
		struct group *g = NULL;
#ifdef HAVE_GETGRNAM_R
		struct group grp;
		gchar *fbuf;
		gint32 retval;
#ifdef _SC_GETGR_R_SIZE_MAX
	 	size_t fbufsize = mono_sysconf (_SC_GETGR_R_SIZE_MAX);
#else
		size_t fbufsize = MONO_SYSCONF_DEFAULT_SIZE;
#endif
		fbuf = (gchar *)g_malloc0 (fbufsize);
		retval = getgrnam_r (utf8_groupname, &grp, fbuf, fbufsize, &g);
		result = ((retval == 0) && (g == &grp));
#else
		/* default to non thread-safe but posix compliant function */
		g = getgrnam (utf8_groupname);
		result = (g != NULL);
#endif
		if (result)
			result = IsMemberOf ((uid_t) GPOINTER_TO_INT (user), g);

#ifdef HAVE_GETGRNAM_R
		g_free (fbuf);
#endif
	}

#endif /* HAVE_GRP_H */

	return result;
}
#endif /* !HOST_WIN32 */

/* Mono.Security.Cryptography IO related internal calls */

#ifndef HOST_WIN32
static gboolean
IsProtected (const gunichar2 *path, gint32 protection)
{
	gboolean result = FALSE;
	gchar *utf8_name = mono_unicode_to_external (path);
	if (utf8_name) {
		struct stat st;
		if (stat (utf8_name, &st) == 0) {
			result = (((st.st_mode & 0777) & protection) == 0);
		}
		g_free (utf8_name);
	}
	return result;
}


static gboolean
Protect (const gunichar2 *path, gint32 file_mode, gint32 add_dir_mode)
{
	gboolean result = FALSE;
	gchar *utf8_name = mono_unicode_to_external (path);
	if (utf8_name) {
		struct stat st;
		if (stat (utf8_name, &st) == 0) {
			int mode = file_mode;
			if (st.st_mode & S_IFDIR)
				mode |= add_dir_mode;
#ifdef HAVE_CHMOD
			result = (chmod (utf8_name, mode) == 0);
#else
			result = -1; // FIXME Huh? This must be TRUE or FALSE.
#endif
		}
		g_free (utf8_name);
	}
	return result;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure (const gunichar2 *path)
{
	/* we assume some kind of security is applicable outside Windows */
	return TRUE;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected (const gunichar2 *path)
{
	gboolean ret = FALSE;

	/* no one, but the owner, should have write access to the directory */
	ret = IsProtected (path, (S_IWGRP | S_IWOTH));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected (const gunichar2 *path)
{
	gboolean ret = FALSE;

	/* no one, but the user, should have access to the directory */
	ret = IsProtected (path, (S_IRGRP | S_IWGRP | S_IXGRP | S_IROTH | S_IWOTH | S_IXOTH));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine (const gunichar2 *path)
{
	gboolean ret = FALSE;

	/* read/write to owner, read to everyone else */
	ret = Protect (path, (S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH), (S_IXUSR | S_IXGRP | S_IXOTH));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser (const gunichar2 *path)
{
	gboolean ret = FALSE;
	
	/* read/write to user, no access to everyone else */
	ret = Protect (path, (S_IRUSR | S_IWUSR), S_IXUSR);
	return (MonoBoolean)ret;
}
#endif /* !HOST_WIN32 */

/*
 * Returns TRUE if there is "something" where the Authenticode signature is 
 * normally located. Returns FALSE is data directory is empty.
 *
 * Note: Neither the structure nor the signature is verified by this function.
 */
MonoBoolean
ves_icall_System_Security_Policy_Evidence_IsAuthenticodePresent (MonoReflectionAssemblyHandle refass, MonoError *error)
{
	error_init (error);
	if (MONO_HANDLE_IS_NULL (refass))
		return FALSE;
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (refass, assembly);
	if (assembly && assembly->image) {
		return (MonoBoolean)mono_image_has_authenticode_entry (assembly->image);
	}
	return FALSE;
}


/* System.Security.SecureString related internal calls */

static MonoImage *system_security_assembly;
static MonoMethod *mono_method_securestring_decrypt;
static MonoMethod *mono_method_securestring_encrypt;

static void
mono_invoke_protected_memory_method (MonoArrayHandle data, MonoObjectHandle scope,
	const char *method_name, MonoMethod **method, MonoError *error)
{
	if (!*method) {
		MonoDomain *domain = mono_domain_get ();
		MonoAssemblyLoadContext *alc = mono_domain_default_alc (domain);
		if (system_security_assembly == NULL) {
			system_security_assembly = mono_image_loaded_internal (alc, "System.Security", FALSE);
			if (!system_security_assembly) {
				MonoAssemblyOpenRequest req;
				mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, alc);
				MonoAssembly *sa = mono_assembly_request_open ("System.Security.dll", &req, NULL);
				g_assert (sa);
				system_security_assembly = mono_assembly_get_image_internal (sa);
			}
		}
		MonoClass *klass = mono_class_load_from_name (system_security_assembly,
									  "System.Security.Cryptography", "ProtectedMemory");
		*method = mono_class_get_method_from_name_checked (klass, method_name, 2, 0, error);
		mono_error_assert_ok (error);
		g_assert (*method);
	}
	void *params [ ] = {
		MONO_HANDLE_RAW (data),
		MONO_HANDLE_RAW (scope) // MemoryProtectionScope.SameProcess
	};
	mono_runtime_invoke_handle_void (*method, NULL_HANDLE, params, error);
}

void
ves_icall_System_Security_SecureString_DecryptInternal (MonoArrayHandle data, MonoObjectHandle scope, MonoError *error)
{
	mono_invoke_protected_memory_method (data, scope, "Unprotect", &mono_method_securestring_decrypt, error);
}
void
ves_icall_System_Security_SecureString_EncryptInternal (MonoArrayHandle data, MonoObjectHandle scope, MonoError *error)
{
	mono_invoke_protected_memory_method (data, scope, "Protect", &mono_method_securestring_encrypt, error);
}

#else

MONO_EMPTY_SOURCE_FILE (mono_security);

#endif /* ENABLE_NETCORE */
