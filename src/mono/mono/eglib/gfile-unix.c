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
#include <errno.h>
#include <fcntl.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

gboolean
g_file_test (const gchar *filename, GFileTest test)
{
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
#if !defined(__PASE__)
		if (access (filename, X_OK) == 0)
			return TRUE;
#else
		/*
		 * PASE always returns true for X_OK; contrary to how AIX
		 * behaves (but *does* correspond to how it's documented!).
		 * This behaviour is also consistent with the ILE, so it's
		 * probably just an upcall returning the same results. As
		 * such, workaround it.
		 */
		if (!have_stat)
			have_stat = (stat (filename, &st) == 0);
		/* Hairy parens, but just manually try all permission bits */
		if (have_stat && (
			((st.st_mode & S_IXOTH)
				|| ((st.st_mode & S_IXUSR) && (st.st_uid == getuid()))
				|| ((st.st_mode & S_IXGRP) && (st.st_gid == getgid())))))
			return TRUE;
#endif
	}
#ifdef HAVE_LSTAT
	if ((test & G_FILE_TEST_IS_SYMLINK) != 0) {
		have_stat = (lstat (filename, &st) == 0);
		if (have_stat && S_ISLNK (st.st_mode))
			return TRUE;
	}
#endif

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
}

gchar *
g_mkdtemp (char *temp)
{
/*
 * On systems without mkdtemp, use a reimplemented version
 * adapted from the Win32 version of this file. AIX is an
 * exception because i before version 7.2 lacks mkdtemp in
 * libc, and GCC can "fix" system headers so that it isn't
 * present without redefining it.
 */
#if defined(HAVE_MKDTEMP) && !defined(_AIX)
	return mkdtemp (g_strdup (temp));
#else
	temp = mktemp (g_strdup (temp));
	/* 0700 is the mode specified in specs */
	if (temp && *temp && mkdir (temp, 0700) == 0)
		return temp;

	g_free (temp);
	return NULL;
#endif
}
