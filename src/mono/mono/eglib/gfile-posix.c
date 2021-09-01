/*
 * File utility functions.
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
#include <config.h>
#include <glib.h>
#include <stdio.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/stat.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <fcntl.h>
#include <errno.h>

#ifdef _MSC_VER
#include <direct.h>
#endif
#ifdef G_OS_WIN32
int mkstemp (char *tmp_template);
#endif

#ifndef O_LARGEFILE
#define OPEN_FLAGS (O_RDONLY)
#else
#define OPEN_FLAGS (O_RDONLY | O_LARGEFILE)
#endif
gboolean
g_file_get_contents (const gchar *filename, gchar **contents, gsize *length, GError **gerror)
{
	gchar *str;
	int fd;
	struct stat st;
	long offset;
	int nread;

	g_return_val_if_fail (filename != NULL, FALSE);
	g_return_val_if_fail (contents != NULL, FALSE);
	g_return_val_if_fail (gerror == NULL || *gerror == NULL, FALSE);

	*contents = NULL;
	if (length)
		*length = 0;

	fd = open (filename, OPEN_FLAGS);
	if (fd == -1) {
		if (gerror != NULL) {
			int err = errno;
			*gerror = g_error_new (G_LOG_DOMAIN, g_file_error_from_errno (err), "Error opening file");
		}
		return FALSE;
	}

	if (fstat (fd, &st) != 0) {
		if (gerror != NULL) {
			int err = errno;
			*gerror = g_error_new (G_LOG_DOMAIN, g_file_error_from_errno (err), "Error in fstat()");
		}
		close (fd);
		return FALSE;
	}

	str = g_malloc (st.st_size + 1);
	offset = 0;
	do {
		nread = read (fd, str + offset, st.st_size - offset);
		if (nread > 0) {
			offset += nread;
		}
	} while ((nread > 0 && offset < st.st_size) || (nread == -1 && errno == EINTR));

	close (fd);

#ifdef G_OS_WIN32
	// Windows defaults to O_TEXT for opened files, meaning that st_size can be larger than
	// what's actually read into str due to new line conversion.
	g_assert (offset <= st.st_size);
	str [offset] = '\0';
	if (length)
		*length = offset;
#else
	str [st.st_size] = '\0';
	if (length) {
		*length = st.st_size;
	}
#endif

	*contents = str;
	return TRUE;
}

gint
g_file_open_tmp (const gchar *tmpl, gchar **name_used, GError **gerror)
{
	const static gchar *default_tmpl = ".XXXXXX";
	gchar *t;
	gint fd;
	size_t len;

	g_return_val_if_fail (gerror == NULL || *gerror == NULL, -1);

	if (tmpl == NULL)
		tmpl = default_tmpl;

	if (strchr (tmpl, G_DIR_SEPARATOR) != NULL) {
		if (gerror) {
			*gerror = g_error_new (G_LOG_DOMAIN, 24, "Template should not have any " G_DIR_SEPARATOR_S);
		}
		return -1;
	}

	len = strlen (tmpl);
	if (len < 6 || strcmp (tmpl + len - 6, "XXXXXX")) {
		if (gerror) {
			*gerror = g_error_new (G_LOG_DOMAIN, 24, "Template should end with XXXXXX");
		}
		return -1;
	}

	t = g_build_filename (g_get_tmp_dir (), tmpl, (const char*)NULL);

	fd = mkstemp (t);

	if (fd == -1) {
		if (gerror) {
			int err = errno;
			*gerror = g_error_new (G_LOG_DOMAIN, g_file_error_from_errno (err), "Error in mkstemp()");
		}
		g_free (t);
		return -1;
	}

	if (name_used) {
		*name_used = t;
	} else {
		g_free (t);
	}
	return fd;
}
