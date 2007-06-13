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

#include <stdlib.h>
#include <glib.h>

#ifdef G_OS_WIN32
#include <windows.h>
#endif

const gchar *
g_getenv(const gchar *variable)
{
#ifdef G_OS_WIN32
	gunichar2 *var, *buffer;
	gchar* val = NULL;
	gint32 buffer_size = 1024;
	gint32 retval;
	var = u8to16(variable); 
	buffer = g_malloc(buffer_size*sizeof(gunichar2));
	retval = GetEnvironmentVariable (var, buffer, buffer_size);
	if (retval != 0) {
		if (retval > buffer_size) {
			g_free (buffer);
			buffer_size = retval;
			buffer = g_malloc(buffer_size*sizeof(gunichar2));
			retval = GetEnvironmentVariable (var, buffer, buffer_size);
		}
		val = u16to8 (buffer);
	}
	g_free(var);
	g_free(buffer);
	return val; 
#else
	return getenv(variable);
#endif
}

gboolean
g_setenv(const gchar *variable, const gchar *value, gboolean overwrite)
{
#ifdef G_OS_WIN32
	gunichar2 *var, *val;
	gboolean result;
	var = u8to16(variable); 
	val = u8to16(value);
	result = (SetEnvironmentVariable(var, val) != 0) ? TRUE : FALSE;
	g_free(var);
	g_free(val);
	return result;
#else
	return setenv(variable, value, overwrite) == 0;
#endif
}

void
g_unsetenv(const gchar *variable)
{
#ifdef G_OS_WIN32
	gunichar2 *var;
	var = u8to16(variable); 
	SetEnvironmentVariable(var, TEXT(""));
	g_free(var);
#else
	unsetenv(variable);
#endif
}

gchar*
g_win32_getlocale(void)
{
#ifdef G_OS_WIN32
	/* FIXME: Use GetThreadLocale
	 * and convert LCID to standard 
	 * string form, "en_US" */
	return strdup ("en_US");
#else
	return NULL;
#endif
}


