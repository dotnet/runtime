/*
 * security.c:  Security internal calls
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/security.h>
#include <mono/io-layer/io-layer.h>
#include <mono/utils/strenc.h>

#ifndef PLATFORM_WIN32

#include <config.h>
#include <grp.h>
#include <pwd.h>
#include <string.h>
#include <sys/types.h>
#include <unistd.h>

/* Disclaimers */

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

#endif /* not PLATFORM_WIN32 */


/* internal functions - reuse driven */

#ifdef PLATFORM_WIN32

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
			// nothing -> return NULL
			g_free (user);
		}

		g_free (domain);
	}

	return uniname;
}


#else /* not PLATFORM_WIN32 */


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
 	fbufsize = (size_t) sysconf (_SC_GETPW_R_SIZE_MAX);
#else
	fbufsize = (size_t) 1024;
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
 	fbufsize = (size_t) sysconf (_SC_GETPW_R_SIZE_MAX);
#else
	fbufsize = (size_t) 1024;
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


/* System.Environment */


MonoString*
ves_icall_System_Environment_get_UserName (void)
{
	MONO_ARCH_SAVE_REGS;

	/* using glib is more portable */
	return mono_string_new (mono_domain_get (), g_get_user_name ());
}


/* System.Security.Principal.WindowsIdentity */


gpointer
ves_icall_System_Security_Principal_WindowsIdentity_GetCurrentToken (void)
{
	gpointer token = NULL;

	MONO_ARCH_SAVE_REGS;

#ifdef PLATFORM_WIN32
	/* Note: This isn't a copy of the Token - we must not close it!!!
	 * http://www.develop.com/kbrown/book/html/whatis_windowsprincipal.html
	 */

	/* thread may be impersonating somebody */
	if (OpenThreadToken (GetCurrentThread (), TOKEN_QUERY, 1, &token) == 0) {
		/* if not take the process identity */
		OpenProcessToken (GetCurrentProcess (), TOKEN_QUERY, &token);
	}
#else
	token = (gpointer) geteuid ();
#endif
	return token;
}


MonoString*
ves_icall_System_Security_Principal_WindowsIdentity_GetTokenName (gpointer token)
{
	MonoString *result = NULL;
	gunichar2 *uniname = NULL;
	gint32 size = 0;

#ifdef PLATFORM_WIN32
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
	gchar *uname = GetTokenName ((uid_t) token);

	MONO_ARCH_SAVE_REGS;

	if (uname) {
		size = strlen (uname);
		uniname = g_utf8_to_utf16 (uname, size, NULL, NULL, NULL);
		g_free (uname);
	}
#endif /* PLATFORM_WIN32 */

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
#ifdef PLATFORM_WIN32
	gpointer token = NULL;

	MONO_ARCH_SAVE_REGS;

	/* TODO: MS has something like this working in Windows 2003 (client and
	 * server) but works only for domain accounts (so it's quite limiting).
	 * http://www.develop.com/kbrown/book/html/howto_logonuser.html
	 */
	g_warning ("Unsupported on Win32 (anyway requires W2K3 minimum)");

#else /* PLATFORM_WIN32*/

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
 	fbufsize = (size_t) sysconf (_SC_GETPW_R_SIZE_MAX);
#else
	fbufsize = (size_t) 1024;
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
		token = (gpointer) p->pw_uid;
	}

#ifdef HAVE_GETPWNAM_R
	g_free (fbuf);
#endif
	g_free (utf8_name);
#endif
	return token;
}


// http://www.dotnet247.com/247reference/msgs/39/195403.aspx
// internal static string[] WindowsIdentity._GetRoles (IntPtr token)
MonoArray*
ves_icall_System_Security_Principal_WindowsIdentity_GetRoles (gpointer token) 
{
	MonoArray *array = NULL;
	MonoDomain *domain = mono_domain_get (); 
#ifdef PLATFORM_WIN32
	gint32 size = 0;

	MONO_ARCH_SAVE_REGS;

	GetTokenInformation (token, TokenGroups, NULL, size, (PDWORD)&size);
	if (size > 0) {
		TOKEN_GROUPS *tg = g_malloc0 (size);
		if (GetTokenInformation (token, TokenGroups, tg, size, (PDWORD)&size)) {
			int i=0;
			int num = tg->GroupCount;

			array = mono_array_new (domain, mono_defaults.string_class, num);

			for (i=0; i < num; i++) {
				gint32 size = 0;
				gunichar2 *uniname = GetSidName (NULL, tg->Groups [i].Sid, &size);

				if (uniname) {
					MonoString *str = mono_string_new_utf16 (domain, uniname, size);
					mono_array_set (array, MonoString *, i, str);
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
		array = mono_array_new (domain, mono_defaults.string_class, 0);
	}
	return array;
}


/* System.Security.Principal.WindowsImpersonationContext */


gboolean
ves_icall_System_Security_Principal_WindowsImpersonationContext_CloseToken (gpointer token)
{
	gboolean result = TRUE;

	MONO_ARCH_SAVE_REGS;

#ifdef PLATFORM_WIN32
	result = (CloseHandle (token) != 0);
#endif
	return result;
}


gpointer
ves_icall_System_Security_Principal_WindowsImpersonationContext_DuplicateToken (gpointer token)
{
	gpointer dupe = NULL;

	MONO_ARCH_SAVE_REGS;

#ifdef PLATFORM_WIN32
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

#ifdef PLATFORM_WIN32
	MONO_ARCH_SAVE_REGS;

	/* The convertion from an ID to a string is done in managed code for Windows */
	g_warning ("IsMemberOfGroupId should never be called on Win32");

#else /* PLATFORM_WIN32 */

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
 	fbufsize = (size_t) sysconf (_SC_GETGR_R_SIZE_MAX);
#else
	fbufsize = (size_t) 1024;
#endif
	fbuf = g_malloc0 (fbufsize);
	retval = getgrgid_r ((gid_t) group, &grp, fbuf, fbufsize, &g);
	result = ((retval == 0) && (g == &grp));
#else
	/* default to non thread-safe but posix compliant function */
	g = getgrgid ((gid_t) group);
	result = (g != NULL);
#endif

	if (result) {
		result = IsMemberOf ((uid_t) user, g);
	}

#ifdef HAVE_GETGRGID_R
	g_free (fbuf);
#endif

#endif /* PLATFORM_WIN32 */

	return result;
}


gboolean
ves_icall_System_Security_Principal_WindowsPrincipal_IsMemberOfGroupName (gpointer user, MonoString *group)
{
	gboolean result = FALSE;

#ifdef PLATFORM_WIN32

	MONO_ARCH_SAVE_REGS;

	/* Windows version use a cache built using WindowsIdentity._GetRoles */
	g_warning ("IsMemberOfGroupName should never be called on Win32");

#else /* PLATFORM_WIN32 */
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
	 	size_t fbufsize = (size_t) sysconf (_SC_GETGR_R_SIZE_MAX);
#else
		size_t fbufsize = (size_t) 1024;
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
			result = IsMemberOf ((uid_t) user, g);
		}

#ifdef HAVE_GETGRNAM_R
		g_free (fbuf);
#endif
		g_free (utf8_groupname);
	}
#endif /* PLATFORM_WIN32 */

	return result;
}
