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
#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>

#ifdef G_OS_WIN32
#include <io.h>
#define open _open
#define S_ISREG(x) ((x &  _S_IFMT) == _S_IFREG)
#define S_ISDIR(x) ((x &  _S_IFMT) == _S_IFDIR)
#endif

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

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

#ifndef O_LARGEFILE
#define OPEN_FLAGS (O_RDONLY)
#else
#define OPEN_FLAGS (O_RDONLY | O_LARGEFILE)
#endif
gboolean
g_file_get_contents (const gchar *filename, gchar **contents, gsize *length, GError **error)
{
	gchar *str;
	int fd;
	struct stat st;
	long offset;
	int nread;

	g_return_val_if_fail (filename != NULL, FALSE);
	g_return_val_if_fail (contents != NULL, FALSE);
	g_return_val_if_fail (error == NULL || *error == NULL, FALSE);

	*contents = NULL;
	if (length)
		*length = 0;

	fd = open (filename, OPEN_FLAGS);
	if (fd == -1) {
		if (error != NULL) {
			int err = errno;
			*error = g_error_new (G_LOG_DOMAIN, g_file_error_from_errno (err), "Error opening file");
		}
		return FALSE;
	}

	if (fstat (fd, &st) != 0) {
		if (error != NULL) {
			int err = errno;
			*error = g_error_new (G_LOG_DOMAIN, g_file_error_from_errno (err), "Error in fstat()");
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
	str [st.st_size] = '\0';
	if (length) {
		*length = st.st_size;
	}
	*contents = str;
	return TRUE;
}

#ifdef _MSC_VER
int mkstemp (char *tmp_template)
{
	int fd;
	gunichar2* utf16_template;

	utf16_template  = u8to16 (tmp_template);

	fd = -1;
	utf16_template = _wmktemp( utf16_template);
	if (utf16_template && *utf16_template) {
		/* FIXME: _O_TEMPORARY causes file to disappear on close causing a test to fail */
		fd = _wopen( utf16_template, _O_BINARY | _O_CREAT /*| _O_TEMPORARY*/ | _O_EXCL, _S_IREAD | _S_IWRITE);
	}

	sprintf (tmp_template + strlen (tmp_template) - 6, "%S", utf16_template + wcslen (utf16_template) - 6);

	g_free (utf16_template);
	return fd;
}
#endif

gint
g_file_open_tmp (const gchar *tmpl, gchar **name_used, GError **error)
{
	const static gchar *default_tmpl = ".XXXXXX";
	gchar *t;
	gint fd;
	size_t len;

	g_return_val_if_fail (error == NULL || *error == NULL, -1);

	if (tmpl == NULL)
		tmpl = default_tmpl;

	if (strchr (tmpl, G_DIR_SEPARATOR) != NULL) {
		if (error) {
			*error = g_error_new (G_LOG_DOMAIN, 24, "Template should not have any " G_DIR_SEPARATOR_S);
		}
		return -1;
	}

	len = strlen (tmpl);
	if (len < 6 || strcmp (tmpl + len - 6, "XXXXXX")) {
		if (error) {
			*error = g_error_new (G_LOG_DOMAIN, 24, "Template should end with XXXXXX");
		}
		return -1;
	}

	t = g_build_filename (g_get_tmp_dir (), tmpl, NULL);

	fd = mkstemp (t);

	if (fd == -1) {
		if (error) {
			int err = errno;
			*error = g_error_new (G_LOG_DOMAIN, g_file_error_from_errno (err), "Error in mkstemp()");
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

#ifdef _MSC_VER
#pragma warning(disable:4701)
#endif

gboolean
g_file_test (const gchar *filename, GFileTest test)
{
#ifdef G_OS_WIN32
	struct _stat64 stat;
	int ret = 0;
	gunichar2* utf16_filename = NULL;

	if (filename == NULL || test == 0)
		return FALSE;

	utf16_filename = u8to16 (filename);
	ret = _wstati64 (utf16_filename, &stat);
	g_free (utf16_filename);

	if ((test & G_FILE_TEST_EXISTS) != 0) {
		if (ret == 0)
			return TRUE;
	}

	if (ret != 0)
		return FALSE;

	if ((test & G_FILE_TEST_IS_EXECUTABLE) != 0) {
		if (stat.st_mode & _S_IEXEC)
			return TRUE;
	}

	if ((test & G_FILE_TEST_IS_REGULAR) != 0) {
		if (stat.st_mode & _S_IFREG)
			return TRUE;
	}

	if ((test & G_FILE_TEST_IS_DIR) != 0) {
		if (stat.st_mode & _S_IFDIR)
			return TRUE;
	}

	/* make this last in case it is OR'd with something else */
	if ((test & G_FILE_TEST_IS_SYMLINK) != 0) {
		return FALSE;
	}

	return FALSE;
#else
	struct stat st;
	gboolean have_stat;

	if (filename == NULL || test == 0)
		return FALSE;

	have_stat = FALSE;

	if ((test & G_FILE_TEST_EXISTS) != 0) {
		if (access (filename, F_OK) == 0)
			return TRUE;
	}

	if ((test & G_FILE_TEST_IS_EXECUTABLE) != 0) {
		if (access (filename, X_OK) == 0)
			return TRUE;
	}
	if ((test & G_FILE_TEST_IS_SYMLINK) != 0) {
		have_stat = (lstat (filename, &st) == 0);
		if (have_stat && S_ISLNK (st.st_mode))
			return TRUE;
	}

	if ((test & G_FILE_TEST_IS_REGULAR) != 0) {
		if (!have_stat)
			have_stat = (stat (filename, &st) == 0);
		if (have_stat && S_ISREG (st.st_mode))
			return TRUE;
	}
	if ((test & G_FILE_TEST_IS_DIR) != 0) {
		if (!have_stat)
			have_stat = (stat (filename, &st) == 0);
		if (have_stat && S_ISDIR (st.st_mode))
			return TRUE;
	}
	return FALSE;
#endif
}


