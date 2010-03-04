/*
 * Output and debugging functions
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *
 * (C) 2006 Novell, Inc.
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
#include <stdio.h>
#include <stdlib.h>
#include <glib.h>

/* The current fatal levels, error is always fatal */
static GLogLevelFlags fatal = G_LOG_LEVEL_ERROR;

void
g_print (const gchar *format, ...)
{
	va_list args;

	va_start (args, format);
	vprintf (format, args);
	va_end (args);
}

GLogLevelFlags
g_log_set_always_fatal (GLogLevelFlags fatal_mask)
{
	GLogLevelFlags old_fatal = fatal;

	fatal |= fatal_mask;
	
	return old_fatal;
}

GLogLevelFlags
g_log_set_fatal_mask (const gchar *log_domain, GLogLevelFlags fatal_mask)
{
	/*
	 * Mono does not use a G_LOG_DOMAIN currently, so we just assume things are fatal
	 * if we decide to set G_LOG_DOMAIN (we probably should) we should implement
	 * this.
	 */
	return fatal_mask;
}

void
g_logv (const gchar *log_domain, GLogLevelFlags log_level, const gchar *format, va_list args)
{
	char *msg;
	
	vasprintf (&msg, format, args);
	printf ("%s%s%s",
		log_domain != NULL ? log_domain : "",
		log_domain != NULL ? ": " : "",
		msg);
	free (msg);
	if (log_level & fatal){
		fflush (stdout);
		fflush (stderr);
		abort ();
	}
}

void
g_log (const gchar *log_domain, GLogLevelFlags log_level, const gchar *format, ...)
{
	va_list args;

	va_start (args, format);
	g_logv (log_domain, log_level, format, args);
	va_end (args);
}

void
g_assertion_message (const gchar *format, ...)
{
	va_list args;

	va_start (args, format);
	g_logv (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR, format, args);
	va_end (args);
}

