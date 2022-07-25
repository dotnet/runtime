/*
 * gmisc.c: Misc functions with no place to go (right now)
 *
 * Author:
 *   Aaron Bockover (abockover@novell.com)
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
#include <stdlib.h>
#include <glib.h>
#include <windows.h>
#include <direct.h>
#include <io.h>
#include <assert.h>
#include "../utils/w32subset.h"

gboolean
g_hasenv (const gchar *variable)
{
	return g_getenv (variable) != NULL;
}

gchar *
g_getenv(const gchar *variable)
{
	gunichar2 *var, *buffer;
	gchar* val = NULL;
	gint32 buffer_size = 1024;
	gint32 retval;
	var = u8to16(variable);
	// FIXME This should loop in case another thread is growing the data.
	buffer = g_new (gunichar2, buffer_size);
	retval = GetEnvironmentVariableW (var, buffer, buffer_size);
	if (retval != 0) {
		if (retval > buffer_size) {
			g_free (buffer);
			buffer_size = retval;
			buffer = g_malloc(buffer_size*sizeof(gunichar2));
			retval = GetEnvironmentVariableW (var, buffer, buffer_size);
		}
		val = u16to8 (buffer);
	} else {
		if (GetLastError () != ERROR_ENVVAR_NOT_FOUND){
			val = g_malloc (1);
			*val = 0;
		}
	}
	g_free(var);
	g_free(buffer);
	return val;
}

gboolean
g_setenv(const gchar *variable, const gchar *value, gboolean overwrite)
{
	gunichar2 *var, *val;
	gboolean result;
	var = u8to16(variable);
	val = u8to16(value);
	result = (SetEnvironmentVariableW(var, val) != 0) ? TRUE : FALSE;
	g_free(var);
	g_free(val);
	return result;
}

gboolean
g_path_is_absolute (const char *filename)
{
	g_return_val_if_fail (filename != NULL, FALSE);

	if (filename[0] != '\0' && filename[1] != '\0') {
		if (filename[1] == ':' && filename[2] != '\0' &&
			(filename[2] == '\\' || filename[2] == '/'))
			return TRUE;
		/* UNC paths */
		else if (filename[0] == '\\' && filename[1] == '\\' &&
			filename[2] != '\0')
			return TRUE;
	}

	return FALSE;
}

static const char *tmp_dir;

const gchar *
g_get_tmp_dir (void)
{
	if (tmp_dir == NULL){
		if (tmp_dir == NULL){
			tmp_dir = g_getenv ("TMPDIR");
			if (tmp_dir == NULL){
				tmp_dir = g_getenv ("TMP");
				if (tmp_dir == NULL){
					tmp_dir = g_getenv ("TEMP");
					if (tmp_dir == NULL)
						tmp_dir = "C:\\temp";
				}
			}
		}
	}
	return tmp_dir;
}

gchar *
g_get_current_dir (void)
{
	gunichar2 *buffer = NULL;
	gchar* val = NULL;
	gint32 retval, buffer_size = MAX_PATH;

	buffer = g_new (gunichar2, buffer_size);
	retval = GetCurrentDirectoryW (buffer_size, buffer);

	if (retval != 0) {
		// the size might be larger than MAX_PATH
		// https://docs.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation?tabs=cmd
		if (retval > buffer_size) {
			buffer_size = retval;
			buffer = g_realloc (buffer, buffer_size*sizeof(gunichar2));
			retval = GetCurrentDirectoryW (buffer_size, buffer);
		}

		val = u16to8 (buffer);
	} else {
		if (GetLastError () != ERROR_ENVVAR_NOT_FOUND) {
			val = g_malloc (1);
			*val = 0;
		}
	}

	g_free (buffer);

	return val;
}
