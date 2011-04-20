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
#include <string.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>

static gpointer error_quark = "FileError";

gpointer
g_file_error_quark (void)
{
	return error_quark;
}

GFileError
g_file_error_from_errno (gint err_no)
{
	switch (err_no) {
	case EEXIST:
		return G_FILE_ERROR_EXIST;
	case EISDIR:
		return G_FILE_ERROR_ISDIR;
	case EACCES:
		return G_FILE_ERROR_ACCES;
	case ENAMETOOLONG:
		return G_FILE_ERROR_NAMETOOLONG;
	case ENOENT:
		return G_FILE_ERROR_NOENT;
	case ENOTDIR:
		return G_FILE_ERROR_NOTDIR;
	case ENXIO:
		return G_FILE_ERROR_NXIO;
	case ENODEV:
		return G_FILE_ERROR_NODEV;
	case EROFS:
		return G_FILE_ERROR_ROFS;
#ifdef ETXTBSY
	case ETXTBSY:
		return G_FILE_ERROR_TXTBSY;
#endif
	case EFAULT:
		return G_FILE_ERROR_FAULT;
#ifdef ELOOP
	case ELOOP:
		return G_FILE_ERROR_LOOP;
#endif
	case ENOSPC:
		return G_FILE_ERROR_NOSPC;
	case ENOMEM:
		return G_FILE_ERROR_NOMEM;
	case EMFILE:
		return G_FILE_ERROR_MFILE;
	case ENFILE:
		return G_FILE_ERROR_NFILE;
	case EBADF:
		return G_FILE_ERROR_BADF;
	case EINVAL:
		return G_FILE_ERROR_INVAL;
	case EPIPE:
		return G_FILE_ERROR_PIPE;
	case EAGAIN:
		return G_FILE_ERROR_AGAIN;
	case EINTR:
		return G_FILE_ERROR_INTR;
	case EIO:
		return G_FILE_ERROR_IO;
	case EPERM:
		return G_FILE_ERROR_PERM;
	case ENOSYS:
		return G_FILE_ERROR_NOSYS;
	default:
		return G_FILE_ERROR_FAILED;
	}
}

#ifdef G_OS_WIN32
#define TMP_FILE_FORMAT "%.*s%s.tmp"
#else
#define TMP_FILE_FORMAT "%.*s.%s~"
#endif

gboolean
g_file_set_contents (const gchar *filename, const gchar *contents, gssize length, GError **err)
{
	const char *name;
	char *path;
	FILE *fp;
	
	if (!(name = strrchr (filename, G_DIR_SEPARATOR)))
		name = filename;
	else
		name++;
	
	path = g_strdup_printf (TMP_FILE_FORMAT, name - filename, filename, name);
	if (!(fp = fopen (path, "wb"))) {
		g_set_error (err, G_FILE_ERROR, g_file_error_from_errno (errno), "%s", g_strerror (errno));
		g_free (path);
		return FALSE;
	}
	
	if (length < 0)
		length = strlen (contents);
	
	if (fwrite (contents, 1, length, fp) < length) {
		g_set_error (err, G_FILE_ERROR, g_file_error_from_errno (ferror (fp)), "%s", g_strerror (ferror (fp)));
		g_unlink (path);
		g_free (path);
		fclose (fp);
		
		return FALSE;
	}
	
	fclose (fp);
	
	if (g_rename (path, filename) != 0) {
		g_set_error (err, G_FILE_ERROR, g_file_error_from_errno (errno), "%s", g_strerror (errno));
		g_unlink (path);
		g_free (path);
		return FALSE;
	}
	
	g_free (path);
	
	return TRUE;
}
