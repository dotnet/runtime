/*
 * security.c:  Security internal calls
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */

#include <mono/metadata/appdomain.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/security.h>
#include <mono/io-layer/io-layer.h>

#ifndef PLATFORM_WIN32
#include <unistd.h>
#include <sys/utsname.h>
#endif

MonoString*
ves_icall_System_Environment_get_UserName (void)
{
	MonoString *result = NULL;
	gint32 length = 0;

	MONO_ARCH_SAVE_REGS;

	if (GetUserName (NULL, &length)) {
		gchar *username = g_malloc (length + 1);
		if (GetUserName (username, &length)) {
			username [length] = 0;
			result = mono_string_new (mono_domain_get (), username);
		}
		g_free (username);
	}

	if (!result) {
		// should never happen - display warning
		g_warning ("couldn't retrieve the username");
		result = mono_string_new (mono_domain_get (), "");
	}

	return result;
}

/* note: we better manipulate the string in managed code (easier and safer) */
MonoString*
ves_icall_System_Environment_GetOSVersionString (void)
{
#ifdef PLATFORM_WIN32
	OSVERSIONINFO verinfo;

	MONO_ARCH_SAVE_REGS;

	verinfo.dwOSVersionInfoSize = sizeof (OSVERSIONINFO);
	if (GetVersionEx (&info)) {
		char version [64];
		// maximum string length is 35 bytes
		// 3 x 10 bytes per number, 1 byte for 0, 3 x 1 byte for dots, 1 for NULL
		sprintf (version, "%d.%d.%d.0", 
			info.dwMajorVersion,
			info.dwMinorVersion,
			info.dwBuildNumber);
		return mono_string_new (mono_domain_get (), version);
	}
#else
	struct utsname name;

	MONO_ARCH_SAVE_REGS;

	if (uname (&name) >= 0) {
		return mono_string_new (mono_domain_get (), name.release);
	}
#endif
	return mono_string_new (mono_domain_get (), "0.0.0.0");
}
