/*
 * Directory utility functions.
 *
 * Author:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com)
 *
 * (C) 2006 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#include "config.h"
#include <glib.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include "../utils/mono-errno.h"
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <dirent.h>

struct _GDir {
	DIR *dir;
#ifndef HAVE_REWINDDIR
	char *path;
#endif
};

GDir *
g_dir_open (const gchar *path, guint flags, GError **gerror)
{
	GDir *dir;

	g_return_val_if_fail (path != NULL, NULL);
	g_return_val_if_fail (gerror == NULL || *gerror == NULL, NULL);

	(void) flags; /* this is not used */
	dir = g_new (GDir, 1);
	dir->dir = opendir (path);
	if (dir->dir == NULL) {
		if (gerror) {
			gint err = errno;
			*gerror = g_error_new (G_LOG_DOMAIN, g_file_error_from_errno (err), strerror (err));
		}
		g_free (dir);
		return NULL;
	}
#ifndef HAVE_REWINDDIR
	dir->path = g_strdup (path);
#endif
	return dir;
}

const gchar *
g_dir_read_name (GDir *dir)
{
	struct dirent *entry;

	g_return_val_if_fail (dir != NULL && dir->dir != NULL, NULL);
	do {
		entry = readdir (dir->dir);
		if (entry == NULL)
			return NULL;
	} while ((strcmp (entry->d_name, ".") == 0) || (strcmp (entry->d_name, "..") == 0));

	return entry->d_name;
}

void
g_dir_rewind (GDir *dir)
{
	g_return_if_fail (dir != NULL && dir->dir != NULL);
#ifndef HAVE_REWINDDIR
	closedir (dir->dir);
	dir->dir = opendir (dir->path);
#else
	rewinddir (dir->dir);
#endif
}

void
g_dir_close (GDir *dir)
{
	g_return_if_fail (dir != NULL && dir->dir != 0);
	closedir (dir->dir);
#ifndef HAVE_REWINDDIR
	g_free (dir->path);
#endif
	dir->dir = NULL;
	g_free (dir);
}

int
g_mkdir_with_parents (const gchar *pathname, int mode)
{
	char *path, *d;
	int rv;
	
	if (!pathname || *pathname == '\0') {
		mono_set_errno (EINVAL);
		return -1;
	}
	
	d = path = g_strdup (pathname);
	if (*d == '/')
		d++;
	
	while (TRUE) {
		if (*d == '/' || *d == '\0') {
		  char orig = *d;
		  *d = '\0';
		  rv = mkdir (path, mode);
		  if (rv == -1 && errno != EEXIST) {
		  	g_free (path);
			return -1;
		  }

		  *d++ = orig;
		  while (orig == '/' && *d == '/')
		  	d++;
		  if (orig == '\0')
		  	break;
		} else {
			d++;
		}
	}
	
	g_free (path);
	
	return 0;
}
