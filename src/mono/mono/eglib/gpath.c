/*
 * Portable Utility Functions
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
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
#include <stdio.h>
#include <glib.h>
#include <errno.h>
#include <sys/stat.h>

#ifdef G_OS_WIN32
#include <direct.h>
#include <windows.h>
#endif

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifdef G_OS_WIN32

#ifndef MAX_PATH
#define MAX_PATH 260
#endif

/* Helper function to check if a Windows path needs the \\?\ prefix for long path support.
 * Returns TRUE if:
 * - The path is long enough to potentially hit MAX_PATH limit
 * - The path doesn't already have the \\?\ prefix
 * - The path is an absolute Windows path (e.g., C:\path), UNC path (e.g., \\server\share),
 *   or drive-relative path (e.g., \Windows\System32)
 */
static gboolean
g_path_needs_long_prefix (const gchar *path)
{
	if (!path || strlen(path) <= 2)
		return FALSE;
	
	/* Only add prefix for paths that are approaching or exceeding MAX_PATH */
	if (strlen(path) < MAX_PATH)
		return FALSE;
	
	if (strncmp(path, "\\\\?\\", 4) == 0)
		return FALSE;
	
	if (path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
		return TRUE;
	
	if (path[0] == '\\' && path[1] == '\\' && path[2] != '?')
		return TRUE;
	
	if (path[0] == '\\' && path[1] != '\\')
		return TRUE;
	
	return FALSE;
}

/* Helper function to convert a drive-relative path to an absolute path.
 * For example, \Windows\System32 becomes C:\Windows\System32.
 * Caller must free the result with g_free().
 */
static gchar *
g_path_drive_relative_to_absolute (const gchar *path)
{
	if (!path)
		return g_strdup(path);

	char current_dir[MAX_PATH];
	if (GetCurrentDirectoryA(MAX_PATH, current_dir) > 0 && current_dir[1] == ':') {
		gchar *result = g_malloc(strlen(path) + 3);
		result[0] = current_dir[0];
		result[1] = ':';
		strcpy(result + 2, path);
		return result;
	}

	return g_strdup(path);
}

/* Makes a path compatible with long path support by adding \\?\ prefix if needed. Caller must free the result. */
gchar *
g_path_make_long_compatible (const gchar *path)
{
	if (!path)
		return NULL;

	gchar *work_path;
	
	/* Drive-relative paths (e.g., \Windows\System32) need to be converted to absolute paths first */
	if (path[0] == '\\' && path[1] != '\\') {
		work_path = g_path_drive_relative_to_absolute(path);
	} else {
		work_path = g_strdup(path);
	}
	
	gchar *result;
	
	if (!g_path_needs_long_prefix(work_path)) {
		g_free(work_path);
		return g_strdup(path);
	}
	
	/* Handle UNC paths: \\server\share becomes \\?\UNC\server\share */
	if (work_path[0] == '\\' && work_path[1] == '\\') {
		result = g_malloc(strlen(work_path) + 7);
		strcpy(result, "\\\\?\\UNC\\");
		strcat(result, work_path + 2);
		g_free(work_path);
		return result;
	}
	
	/* Handle absolute paths: C:\path becomes \\?\C:\path */
	result = g_malloc(strlen(work_path) + 5);
	strcpy(result, "\\\\?\\");
	strcat(result, work_path);
	g_free(work_path);
	return result;
}
#endif

gchar *
g_build_path (const gchar *separator, const gchar *first_element, ...)
{
	const char *elem, *next, *endptr;
	gboolean trimmed;
	GString *path;
	va_list args;
	size_t slen;

	g_return_val_if_fail (separator != NULL, NULL);

	path = g_string_sized_new (48);
	slen = strlen (separator);

	va_start (args, first_element);
	for (elem = first_element; elem != NULL; elem = next) {
		/* trim any trailing separators from @elem */
		endptr = elem + strlen (elem);
		trimmed = FALSE;

		while (endptr >= elem + slen) {
			if (strncmp (endptr - slen, separator, slen) != 0)
				break;

			endptr -= slen;
			trimmed = TRUE;
		}

		/* append elem, not including any trailing separators */
		if (endptr > elem)
			g_string_append_len (path, elem, endptr - elem);

		/* get the next element */
		do {
			if (!(next = va_arg (args, char *)))
				break;

			/* remove leading separators */
			while (!strncmp (next, separator, slen))
				next += slen;
		} while (*next == '\0');

		if (next || trimmed)
			g_string_append_len (path, separator, slen);
	}
	va_end (args);

	return g_string_free (path, FALSE);
}

static gchar*
strrchr_separator (const gchar* filename)
{
#ifdef G_OS_WIN32
	char *p2;
#endif
	char *p;

	p = (char*)strrchr (filename, G_DIR_SEPARATOR);
#ifdef G_OS_WIN32
	p2 = (char*)strrchr (filename, '/');
	if (p2 > p)
		p = p2;
#endif

	return p;
}

gchar *
g_path_get_dirname (const gchar *filename)
{
	char *p, *r;
	size_t count;
	g_return_val_if_fail (filename != NULL, NULL);

	p = strrchr_separator (filename);
	if (p == NULL)
		return g_strdup (".");
	if (p == filename)
		return g_strdup ("/");
	count = p - filename;
	r = g_malloc (count + 1);
	strncpy (r, filename, count);
	r [count] = 0;

	return r;
}

gchar *
g_path_get_basename (const char *filename)
{
	char *r;
	g_return_val_if_fail (filename != NULL, NULL);

	/* Empty filename -> . */
	if (!*filename)
		return g_strdup (".");

	/* No separator -> filename */
	r = strrchr_separator (filename);
	if (r == NULL)
		return g_strdup (filename);

	/* Trailing slash, remove component */
	if (r [1] == 0){
		char *copy = g_strdup (filename);
		copy [r-filename] = 0;
		r = strrchr_separator (copy);

		if (r == NULL){
			g_free (copy);
			return g_strdup ("/");
		}
		r = g_strdup (&r[1]);
		g_free (copy);
		return r;
	}

	return g_strdup (&r[1]);
}

#if !defined (HAVE_STRTOK_R)
// This is from BSD's strtok_r

char *
strtok_r(char *s, const char *delim, char **last)
{
	char *spanp;
	int c, sc;
	char *tok;

	if (s == NULL && (s = *last) == NULL)
		return NULL;

	/*
	 * Skip (span) leading delimiters (s += strspn(s, delim), sort of).
	 */
cont:
	c = *s++;
	for (spanp = (char *)delim; (sc = *spanp++) != 0; ){
		if (c == sc)
			goto cont;
	}

	if (c == 0){         /* no non-delimiter characters */
		*last = NULL;
		return NULL;
	}
	tok = s - 1;

	/*
	 * Scan token (scan for delimiters: s += strcspn(s, delim), sort of).
	 * Note that delim must have one NUL; we stop if we see that, too.
	 */
	while (true) {
		c = *s++;
		spanp = (char *)delim;
		do {
			if ((sc = *spanp++) == c) {
				if (c == 0)
					s = NULL;
				else {
					char *w = s - 1;
					*w = '\0';
				}
				*last = s;
				return tok;
			}
		}
		while (sc != 0);
	}
	/* NOTREACHED */
}
#endif

gboolean
g_ensure_directory_exists (const gchar *filename)
{
#ifdef G_OS_WIN32
	gchar *dir_utf8 = g_path_get_dirname (filename);
	gunichar2 *p;
	gunichar2 *dir_utf16 = NULL;
	int retval;

	if (!dir_utf8 || !dir_utf8 [0])
		return FALSE;

	dir_utf16 = g_utf8_to_utf16 (dir_utf8, (glong)strlen (dir_utf8), NULL, NULL, NULL);
	g_free (dir_utf8);

	if (!dir_utf16)
		return FALSE;

	p = dir_utf16;

	/* make life easy and only use one directory separator */
	while (*p != '\0')
	{
		if (*p == '/')
			*p = '\\';
		p++;
	}

	p = dir_utf16;

	/* get past C:\ )*/
	while (*p++ != '\\')
	{
	}

	while (1) {
		p = wcschr (p, '\\');
		if (p)
			*p = '\0';
		retval = _wmkdir (dir_utf16);
		if (retval != 0 && errno != EEXIST) {
			g_free (dir_utf16);
			return FALSE;
		}
		if (!p)
			break;
		*p++ = '\\';
	}

	g_free (dir_utf16);
	return TRUE;
#else
	char *p;
	gchar *dir = g_path_get_dirname (filename);
	int retval;
	struct stat sbuf;

	if (!dir || !dir [0]) {
		g_free (dir);
		return FALSE;
	}

	if (stat (dir, &sbuf) == 0 && S_ISDIR (sbuf.st_mode)) {
		g_free (dir);
		return TRUE;
	}

	p = dir;
	while (*p == '/')
		p++;

	while (1) {
		p = strchr (p, '/');
		if (p)
			*p = '\0';
		retval = mkdir (dir, 0777);
		if (retval != 0 && errno != EEXIST) {
			g_free (dir);
			return FALSE;
		}
		if (!p)
			break;
		*p++ = '/';
	}

	g_free (dir);
	return TRUE;
#endif
}

