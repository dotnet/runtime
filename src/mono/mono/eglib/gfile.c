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

static gpointer error_quark = (gpointer)"FileError";

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

FILE *
g_fopen (const char *path, const char *mode)
{
	FILE *fp;

	if (!path)
		return NULL;

#ifndef HOST_WIN32
	fp = fopen (path, mode);
#else
	gunichar2 *wPath = g_utf8_to_utf16 (path, -1, 0, 0, 0);
	gunichar2 *wMode = g_utf8_to_utf16 (mode, -1, 0, 0, 0);

	if (!wPath || !wMode)
		return NULL;

	fp = _wfopen ((wchar_t *) wPath, (wchar_t *) wMode);
	g_free (wPath);
	g_free (wMode);
#endif

	return fp;
}
