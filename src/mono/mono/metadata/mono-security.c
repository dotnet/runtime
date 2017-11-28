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

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/security.h>
#include <mono/utils/strenc.h>

#ifndef HOST_WIN32
#include <config.h>
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

/* Disclaimers */

#if defined(__GNUC__)

#ifndef HAVE_GETGRGID_R
	#warning Non-thread safe getgrgid being used!
#endif
#ifndef HAVE_GETGRNAM_R
	#warning Non-thread safe getgrnam being used!
#endif
#ifndef HAVE_GETPWNAM_R
	#warning Non-thread safe getpwnam being used!
#endif
#ifndef HAVE_GETPWUID_R
	#warning Non-thread safe getpwuid being used!
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
	size_t size = (size_t) sysconf (name);
	/* default value */
	return (size == -1) ? MONO_SYSCONF_DEFAULT_SIZE : size;
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
mono_security_principal_windows_identity_get_current_token ()
{
	return GINT_TO_POINTER (geteuid ());
}

gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (MonoError *error)
{
	error_init (error);
	return mono_security_principal_windows_identity_get_current_token ();
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
MonoArray*
ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token)
{
	MonoError error;
	MonoArray *array = NULL;
	MonoDomain *domain = mono_domain_get ();

	/* POSIX-compliant systems should use IsMemberOfGroupId or IsMemberOfGroupName */
	g_warning ("WindowsIdentity._GetRoles should never be called on POSIX");

	if (!array) {
		/* return empty array of string, i.e. string [0] */
		array = mono_array_new_checked (domain, mono_get_string_class (), 0, &error);
		mono_error_set_pending_exception (&error);
	}
	return array;
}
#endif /* !HOST_WIN32 */

/* System.Security.Principal.WindowsImpersonationContext */

#ifndef HOST_WIN32
gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken (gpointer token)
{
	return TRUE;
}
#endif /* !HOST_WIN32 */

#ifndef HOST_WIN32
gpointer
ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token)
{
	return token;
}
#endif /* !HOST_WIN32 */

#if G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken (gpointer token)
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

gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf (void)
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
#endif /* G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

/* System.Security.Principal.WindowsPrincipal */

#ifndef HOST_WIN32
gboolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupId (gpointer user, gpointer group)
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

	if (result) {
		result = IsMemberOf ((uid_t) GPOINTER_TO_INT (user), g);
	}

#ifdef HAVE_GETGRGID_R
	g_free (fbuf);
#endif

#endif /* HAVE_GRP_H */

	return result;
}

gboolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName (gpointer user, MonoString *group)
{
	gboolean result = FALSE;

#ifdef HAVE_GRP_H

	gchar *utf8_groupname;

	utf8_groupname = mono_unicode_to_external (mono_string_chars (group));
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

		if (result) {
			result = IsMemberOf ((uid_t) GPOINTER_TO_INT (user), g);
		}

#ifdef HAVE_GETGRNAM_R
		g_free (fbuf);
#endif
		g_free (utf8_groupname);
	}

#endif /* HAVE_GRP_H */

	return result;
}
#endif /* !HOST_WIN32 */

/* Mono.Security.Cryptography IO related internal calls */

#ifndef HOST_WIN32
static gboolean
IsProtected (MonoString *path, gint32 protection) 
{
	gboolean result = FALSE;
	gchar *utf8_name = mono_unicode_to_external (mono_string_chars (path));
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
Protect (MonoString *path, gint32 file_mode, gint32 add_dir_mode)
{
	gboolean result = FALSE;
	gchar *utf8_name = mono_unicode_to_external (mono_string_chars (path));
	if (utf8_name) {
		struct stat st;
		if (stat (utf8_name, &st) == 0) {
			int mode = file_mode;
			if (st.st_mode & S_IFDIR)
				mode |= add_dir_mode;
			result = (chmod (utf8_name, mode) == 0);
		}
		g_free (utf8_name);
	}
	return result;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure (MonoString *root)
{
	/* we assume some kind of security is applicable outside Windows */
	return TRUE;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected (MonoString *path)
{
	gboolean ret = FALSE;

	/* no one, but the owner, should have write access to the directory */
	ret = IsProtected (path, (S_IWGRP | S_IWOTH));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected (MonoString *path)
{
	gboolean ret = FALSE;

	/* no one, but the user, should have access to the directory */
	ret = IsProtected (path, (S_IRGRP | S_IWGRP | S_IXGRP | S_IROTH | S_IWOTH | S_IXOTH));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine (MonoString *path)
{
	gboolean ret = FALSE;

	/* read/write to owner, read to everyone else */
	ret = Protect (path, (S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH), (S_IXUSR | S_IXGRP | S_IXOTH));
	return (MonoBoolean)ret;
}

MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser (MonoString *path)
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

static MonoImage *system_security_assembly = NULL;

void
ves_icall_System_Security_SecureString_DecryptInternal (MonoArray *data, MonoObject *scope)
{
	MonoError error;
	invoke_protected_memory_method (data, scope, FALSE, &error);
	mono_error_set_pending_exception (&error);
}
void
ves_icall_System_Security_SecureString_EncryptInternal (MonoArray* data, MonoObject *scope)
{
	MonoError error;
	invoke_protected_memory_method (data, scope, TRUE, &error);
	mono_error_set_pending_exception (&error);
}

void invoke_protected_memory_method (MonoArray *data, MonoObject *scope, gboolean encrypt, MonoError *error)
{
	MonoClass *klass;
	MonoMethod *method;
	void *params [2];

	error_init (error);
	
	if (system_security_assembly == NULL) {
		system_security_assembly = mono_image_loaded ("System.Security");
		if (!system_security_assembly) {
			MonoAssembly *sa = mono_assembly_open_predicate ("System.Security.dll", FALSE, FALSE, NULL, NULL, NULL);
			if (!sa)
				g_assert_not_reached ();
			system_security_assembly = mono_assembly_get_image (sa);
		}
	}

	klass = mono_class_load_from_name (system_security_assembly,
								  "System.Security.Cryptography", "ProtectedMemory");
	method = mono_class_get_method_from_name (klass, encrypt ? "Protect" : "Unprotect", 2);
	params [0] = data;
	params [1] = scope; /* MemoryProtectionScope.SameProcess */

	mono_runtime_invoke_checked (method, NULL, params, error);
}
