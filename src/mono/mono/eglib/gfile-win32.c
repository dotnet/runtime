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
#include <windows.h>
#include <glib.h>
#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/types.h>
#include <direct.h>

#ifdef G_OS_WIN32
#include <io.h>
#define open _open
#ifndef S_ISREG
#define S_ISREG(x) ((x &  _S_IFMT) == _S_IFREG)
#endif
#ifndef S_ISDIR
#define S_ISDIR(x) ((x &  _S_IFMT) == _S_IFDIR)
#endif
#endif

int mkstemp (char *tmp_template)
{
	int fd;
	gunichar2* utf16_template;

	utf16_template  = u8to16 (tmp_template);

	fd = -1;
	utf16_template = _wmktemp( utf16_template);
	if (utf16_template && *utf16_template) {
		/* FIXME: _O_TEMPORARY causes file to disappear on close causing a test to fail */
		fd = _wopen( utf16_template, _O_BINARY | _O_CREAT /*| _O_TEMPORARY*/ | _O_RDWR | _O_EXCL, _S_IREAD | _S_IWRITE);
	}

	/* FIXME: this will crash if utf16_template == NULL */
	sprintf (tmp_template + strlen (tmp_template) - 6, "%S", utf16_template + wcslen (utf16_template) - 6);

	g_free (utf16_template);
	return fd;
}

gchar *
g_mkdtemp (char *tmp_template)
{
	gunichar2* utf16_template;

	utf16_template  = u8to16 (tmp_template);

	utf16_template = _wmktemp(utf16_template);
	if (utf16_template && *utf16_template) {
		if (_wmkdir (utf16_template) == 0){
			char *ret = u16to8 (utf16_template);
			g_free (utf16_template);
			return ret;
		}
	}

	g_free (utf16_template);
	return NULL;
}

gboolean
g_file_test (const gchar *filename, GFileTest test)
{
	gunichar2* utf16_filename = NULL;
	DWORD attr;
	
	if (filename == NULL || test == 0)
		return FALSE;

	utf16_filename = u8to16 (filename);
	attr = GetFileAttributesW (utf16_filename);
	g_free (utf16_filename);
	
	if (attr == INVALID_FILE_ATTRIBUTES)
		return FALSE;

	if ((test & G_FILE_TEST_EXISTS) != 0) {
		return TRUE;
	}

	if ((test & G_FILE_TEST_IS_EXECUTABLE) != 0) {
		/* Testing executable permission on Windows is hard, and this is unused, treat as EXISTS for now. */
		return TRUE;
	}

	if ((test & G_FILE_TEST_IS_REGULAR) != 0) {
		if (attr & (FILE_ATTRIBUTE_DEVICE|FILE_ATTRIBUTE_DIRECTORY))
			return FALSE;
		return TRUE;
	}

	if ((test & G_FILE_TEST_IS_DIR) != 0) {
		if (attr & FILE_ATTRIBUTE_DIRECTORY)
			return TRUE;
	}

	/* make this last in case it is OR'd with something else */
	if ((test & G_FILE_TEST_IS_SYMLINK) != 0) {
		return FALSE;
	}

	return FALSE;
}
