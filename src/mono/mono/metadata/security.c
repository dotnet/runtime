/*
 * security.c:  Security internal calls
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <mono/metadata/assembly.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/image.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/security.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/strenc.h>

#ifdef HOST_WIN32

#include <aclapi.h>
#include <accctrl.h>

#ifndef PROTECTED_DACL_SECURITY_INFORMATION
#define PROTECTED_DACL_SECURITY_INFORMATION	0x80000000L
#endif

#else

#include <config.h>
#include <grp.h>
#include <pwd.h>
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

#endif /* not HOST_WIN32 */


/* internal functions - reuse driven */

#ifdef HOST_WIN32

/* ask a server to translate a SID into a textual representation */
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


#else /* not HOST_WIN32 */

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
	fbuf = g_malloc0 (fbufsize);
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

	return uname;
}


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


static gboolean
IsDefaultGroup (uid_t user, gid_t group)
{
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

	fbuf = g_malloc0 (fbufsize);
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

	return result;
}


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

#endif


/* ICALLS */


/* System.Security.Principal.WindowsIdentity */


gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (void)
{
	gpointer token = NULL;

	MONO_ARCH_SAVE_REGS;

#ifdef HOST_WIN32
	/* Note: This isn't a copy of the Token - we must not close it!!!
	 * http://www.develop.com/kbrown/book/html/whatis_windowsprincipal.html
	 */

	/* thread may be impersonating somebody */
	if (OpenThreadToken (GetCurrentThread (), TOKEN_QUERY, 1, &token) == 0) {
		/* if not take the process identity */
		OpenProcessToken (GetCurrentProcess (), TOKEN_QUERY, &token);
	}
#else
	token = GINT_TO_POINTER (geteuid ());
#endif
	return token;
}


MonoString*
ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName (gpointer token)
{
	MonoString *result = NULL;
	gunichar2 *uniname = NULL;
	gint32 size = 0;

#ifdef HOST_WIN32
	MONO_ARCH_SAVE_REGS;

	GetTokenInformation (token, TokenUser, NULL, size, (PDWORD)&size);
	if (size > 0) {
		TOKEN_USER *tu = g_malloc0 (size);
		if (GetTokenInformation (token, TokenUser, tu, size, (PDWORD)&size)) {
			uniname = GetSidName (NULL, tu->User.Sid, &size);
		}
		g_free (tu);
	}
#else 
	gchar *uname = GetTokenName ((uid_t) GPOINTER_TO_INT (token));

	MONO_ARCH_SAVE_REGS;

	if (uname) {
		size = strlen (uname);
		uniname = g_utf8_to_utf16 (uname, size, NULL, NULL, NULL);
		g_free (uname);
	}
#endif /* HOST_WIN32 */

	if (size > 0) {
		result = mono_string_new_utf16 (mono_domain_get (), uniname, size);
	}
	else
		result = mono_string_new (mono_domain_get (), "");

	if (uniname)
		g_free (uniname);

	return result;
}


gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetUserToken (MonoString *username)
{
#ifdef HOST_WIN32
	gpointer token = NULL;

	MONO_ARCH_SAVE_REGS;

	/* TODO: MS has something like this working in Windows 2003 (client and
	 * server) but works only for domain accounts (so it's quite limiting).
	 * http://www.develop.com/kbrown/book/html/howto_logonuser.html
	 */
	g_warning ("Unsupported on Win32 (anyway requires W2K3 minimum)");

#else /* HOST_WIN32*/

#ifdef HAVE_GETPWNAM_R
	struct passwd pwd;
	size_t fbufsize;
	gchar *fbuf;
	gint32 retval;
#endif
	gpointer token = (gpointer) -2;
	struct passwd *p;
	gchar *utf8_name;
	gboolean result;

	MONO_ARCH_SAVE_REGS;

	utf8_name = mono_unicode_to_external (mono_string_chars (username));

#ifdef HAVE_GETPWNAM_R
#ifdef _SC_GETPW_R_SIZE_MAX
 	fbufsize = mono_sysconf (_SC_GETPW_R_SIZE_MAX);
#else
	fbufsize = MONO_SYSCONF_DEFAULT_SIZE;
#endif

	fbuf = g_malloc0 (fbufsize);
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
#endif
	return token;
}


/* http://www.dotnet247.com/247reference/msgs/39/195403.aspx
// internal static string[] WindowsIdentity._GetRoles (IntPtr token)
*/
MonoArray*
ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token) 
{
	MonoArray *array = NULL;
	MonoDomain *domain = mono_domain_get (); 
#ifdef HOST_WIN32
	gint32 size = 0;

	MONO_ARCH_SAVE_REGS;

	GetTokenInformation (token, TokenGroups, NULL, size, (PDWORD)&size);
	if (size > 0) {
		TOKEN_GROUPS *tg = g_malloc0 (size);
		if (GetTokenInformation (token, TokenGroups, tg, size, (PDWORD)&size)) {
			int i=0;
			int num = tg->GroupCount;

			array = mono_array_new (domain, mono_get_string_class (), num);

			for (i=0; i < num; i++) {
				gint32 size = 0;
				gunichar2 *uniname = GetSidName (NULL, tg->Groups [i].Sid, &size);

				if (uniname) {
					MonoString *str = mono_string_new_utf16 (domain, uniname, size);
					mono_array_setref (array, i, str);
					g_free (uniname);
				}
			}
		}
		g_free (tg);
	}
#else
	/* POSIX-compliant systems should use IsMemberOfGroupId or IsMemberOfGroupName */
	g_warning ("WindowsIdentity._GetRoles should never be called on POSIX");
#endif
	if (!array) {
		/* return empty array of string, i.e. string [0] */
		array = mono_array_new (domain, mono_get_string_class (), 0);
	}
	return array;
}


/* System.Security.Principal.WindowsImpersonationContext */


gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken (gpointer token)
{
	gboolean result = TRUE;

	MONO_ARCH_SAVE_REGS;

#ifdef HOST_WIN32
	result = (CloseHandle (token) != 0);
#endif
	return result;
}


gpointer
ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token)
{
	gpointer dupe = NULL;

	MONO_ARCH_SAVE_REGS;

#ifdef HOST_WIN32
	if (DuplicateToken (token, SecurityImpersonation, &dupe) == 0) {
		dupe = NULL;
	}
#else
	dupe = token;
#endif
	return dupe;
}


gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_SetCurrentToken (gpointer token)
{
	MONO_ARCH_SAVE_REGS;

	/* Posix version implemented in /mono/mono/io-layer/security.c */
	return (ImpersonateLoggedOnUser (token) != 0);
}


gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_RevertToSelf (void)
{
	MONO_ARCH_SAVE_REGS;

	/* Posix version implemented in /mono/mono/io-layer/security.c */
	return (RevertToSelf () != 0);
}


/* System.Security.Principal.WindowsPrincipal */

gboolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupId (gpointer user, gpointer group)
{
	gboolean result = FALSE;

#ifdef HOST_WIN32
	MONO_ARCH_SAVE_REGS;

	/* The convertion from an ID to a string is done in managed code for Windows */
	g_warning ("IsMemberOfGroupId should never be called on Win32");

#else /* HOST_WIN32 */

#ifdef HAVE_GETGRGID_R
	struct group grp;
	size_t fbufsize;
	gchar *fbuf;
	gint32 retval;
#endif
	struct group *g = NULL;

	MONO_ARCH_SAVE_REGS;

#ifdef HAVE_GETGRGID_R
#ifdef _SC_GETGR_R_SIZE_MAX
 	fbufsize = mono_sysconf (_SC_GETGR_R_SIZE_MAX);
#else
	fbufsize = MONO_SYSCONF_DEFAULT_SIZE;
#endif
	fbuf = g_malloc0 (fbufsize);
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

#endif /* HOST_WIN32 */

	return result;
}


gboolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName (gpointer user, MonoString *group)
{
	gboolean result = FALSE;

#ifdef HOST_WIN32

	MONO_ARCH_SAVE_REGS;

	/* Windows version use a cache built using WindowsIdentity._GetRoles */
	g_warning ("IsMemberOfGroupName should never be called on Win32");

#else /* HOST_WIN32 */
	gchar *utf8_groupname;

	MONO_ARCH_SAVE_REGS;

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
		fbuf = g_malloc0 (fbufsize);
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
#endif /* HOST_WIN32 */

	return result;
}


/* Mono.Security.Cryptography IO related internal calls */

#ifdef HOST_WIN32

static PSID
GetAdministratorsSid (void) 
{
	SID_IDENTIFIER_AUTHORITY admins = SECURITY_NT_AUTHORITY;
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
	SID_IDENTIFIER_AUTHORITY everyone = SECURITY_WORLD_SID_AUTHORITY;
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
	gpointer token = ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken ();

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


static gboolean 
IsMachineProtected (gunichar2 *path)
{
	gboolean success = FALSE;
	PACL pDACL = NULL;
	PSID pEveryoneSid = NULL;

	DWORD dwRes = GetNamedSecurityInfoW (path, SE_FILE_OBJECT, DACL_SECURITY_INFORMATION, NULL, NULL, &pDACL, NULL, NULL);
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

	if (pDACL)
		LocalFree (pDACL);

	return success;
}


static gboolean 
IsUserProtected (gunichar2 *path)
{
	gboolean success = FALSE;
	PACL pDACL = NULL;
	PSID pEveryoneSid = NULL;

	DWORD dwRes = GetNamedSecurityInfoW (path, SE_FILE_OBJECT, 
		DACL_SECURITY_INFORMATION, NULL, NULL, &pDACL, NULL, NULL);
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

	if (pDACL)
		LocalFree (pDACL);

	return success;
}


static gboolean 
ProtectMachine (gunichar2 *path)
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


static gboolean 
ProtectUser (gunichar2 *path)
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

#else

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

#endif /* not HOST_WIN32 */


MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_CanSecure (MonoString *root)
{
#if HOST_WIN32
	gint32 flags;

	MONO_ARCH_SAVE_REGS;

	/* ACL are nice... unless you have FAT or other uncivilized filesystem */
	if (!GetVolumeInformation (mono_string_chars (root), NULL, 0, NULL, NULL, (LPDWORD)&flags, NULL, 0))
		return FALSE;
	return ((flags & FS_PERSISTENT_ACLS) == FS_PERSISTENT_ACLS);
#else
	MONO_ARCH_SAVE_REGS;
	/* we assume some kind of security is applicable outside Windows */
	return TRUE;
#endif
}


MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsMachineProtected (MonoString *path)
{
	gboolean ret = FALSE;

	MONO_ARCH_SAVE_REGS;

	/* no one, but the owner, should have write access to the directory */
#ifdef HOST_WIN32
	ret = IsMachineProtected (mono_string_chars (path));
#else
	ret = IsProtected (path, (S_IWGRP | S_IWOTH));
#endif
	return ret;
}


MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_IsUserProtected (MonoString *path)
{
	gboolean ret = FALSE;

	MONO_ARCH_SAVE_REGS;

	/* no one, but the user, should have access to the directory */
#ifdef HOST_WIN32
	ret = IsUserProtected (mono_string_chars (path));
#else
	ret = IsProtected (path, (S_IRGRP | S_IWGRP | S_IXGRP | S_IROTH | S_IWOTH | S_IXOTH));
#endif
	return ret;
}


MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectMachine (MonoString *path)
{
	gboolean ret = FALSE;

	MONO_ARCH_SAVE_REGS;

	/* read/write to owner, read to everyone else */
#ifdef HOST_WIN32
	ret = ProtectMachine (mono_string_chars (path));
#else
	ret = Protect (path, (S_IRUSR | S_IWUSR | S_IRGRP | S_IROTH), (S_IXUSR | S_IXGRP | S_IXOTH));
#endif
	return ret;
}


MonoBoolean
ves_icall_Mono_Security_Cryptography_KeyPairPersistence_ProtectUser (MonoString *path)
{
	gboolean ret = FALSE;
	
	MONO_ARCH_SAVE_REGS;

	/* read/write to user, no access to everyone else */
#ifdef HOST_WIN32
	ret = ProtectUser (mono_string_chars (path));
#else
	ret = Protect (path, (S_IRUSR | S_IWUSR), S_IXUSR);
#endif
	return ret;
}


/*
 * Returns TRUE if there is "something" where the Authenticode signature is 
 * normally located. Returns FALSE is data directory is empty.
 *
 * Note: Neither the structure nor the signature is verified by this function.
 */
MonoBoolean
ves_icall_System_Security_Policy_Evidence_IsAuthenticodePresent (MonoReflectionAssembly *refass)
{
	if (refass && refass->assembly && refass->assembly->image) {
		return mono_image_has_authenticode_entry (refass->assembly->image);
	}
	return FALSE;
}


/* System.Security.SecureString related internal calls */

static MonoImage *system_security_assembly = NULL;

void
ves_icall_System_Security_SecureString_DecryptInternal (MonoArray *data, MonoObject *scope)
{
	invoke_protected_memory_method (data, scope, FALSE);
}
void
ves_icall_System_Security_SecureString_EncryptInternal (MonoArray* data, MonoObject *scope)
{
	invoke_protected_memory_method (data, scope, TRUE);
}

void invoke_protected_memory_method (MonoArray *data, MonoObject *scope, gboolean encrypt)
{
	MonoClass *klass;
	MonoMethod *method;
	void *params [2];

	MONO_ARCH_SAVE_REGS;

	if (system_security_assembly == NULL) {
		system_security_assembly = mono_image_loaded ("System.Security");
		if (!system_security_assembly) {
			MonoAssembly *sa = mono_assembly_open ("System.Security.dll", NULL);
			if (!sa)
				g_assert_not_reached ();
			system_security_assembly = mono_assembly_get_image (sa);
		}
	}

	klass = mono_class_from_name (system_security_assembly,
								  "System.Security.Cryptography", "ProtectedMemory");
	method = mono_class_get_method_from_name (klass, encrypt ? "Protect" : "Unprotect", 2);
	params [0] = data;
	params [1] = scope; /* MemoryProtectionScope.SameProcess */
	mono_runtime_invoke (method, NULL, params, NULL);
}
