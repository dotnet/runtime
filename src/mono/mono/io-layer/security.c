/*
 * security.c:  Security
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * (C) 2004 Novell (http://www.novell.com)
 */

#include <config.h>
#include <mono/io-layer/io-layer.h>

#include <pwd.h>
#include <string.h>
#include <sys/types.h>
#include <unistd.h>


gboolean
GetUserName (gchar *buffer, gint32 *size) 
{
#ifdef HAVE_GETPWUID_R
	struct passwd *pbuf;
	size_t fbufsize;
	gchar *fbuf;
#endif
	struct passwd *p;
	uid_t uid;

	if (!size) {
		SetLastError (ERROR_INVALID_PARAMETER);
		return FALSE;
	}

	uid = getuid ();
#ifdef HAVE_GETPWUID_R
#ifdef _SC_GETPW_R_SIZE_MAX
	fbufsize = (size_t) sysconf (_SC_GETPW_R_SIZE_MAX);
#else
	fbufsize = (size_t) 1024;
#endif
	
	fbuf = g_malloc0 (fbufsize);
	pbuf = g_new0 (struct passwd, 1);
	getpwuid_r (uid, pbuf, fbuf, fbufsize, &p);
#else
	p = getpwuid (uid);
#endif
	if (p) {
		gint32 sz = strlen (p->pw_name);
		if (buffer) {
			if (sz > *size)
				sz = *size;
			strncpy (buffer, p->pw_name, sz);
		}
		*size = sz;
		return TRUE;
	}

#ifdef HAVE_GETPWUID_R
	g_free (pbuf);
	g_free (fbuf);
#endif
	*size = 0;
	SetLastError (ERROR_INVALID_HANDLE);
	return FALSE;
}

