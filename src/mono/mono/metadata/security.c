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
