/*
 * security.c:  Security
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */

#include <mono/io-layer/io-layer.h>

#include <pwd.h>
#include <string.h>
#include <sys/types.h>
#include <unistd.h>


extern gboolean GetUserName (gchar *buffer, gint32 *size) 
{
	struct passwd *p;
	uid_t uid;

	if (!size) {
		SetLastError (ERROR_INVALID_PARAMETER);
		return 0;
	}

	uid = getuid ();
	p = getpwuid (uid);
	if (p) {
		gint32 sz = strlen (p->pw_name);
		if (buffer) {
			if (sz > *size)
				sz = *size;
			strncpy (buffer, p->pw_name, sz);
		}
		*size = sz;
		return 1;
	}

	// note: getpwuid return static data - no free here
	*size = 0;
	SetLastError (ERROR_INVALID_HANDLE);
	return 0;
}

