/*
 * Output and debugging functions
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *
 * (C) 2006 Novell, Inc.
 * Copyright 2011 Xamarin Inc.
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
#include <stdlib.h>
#include <glib.h>

/* The current fatal levels, error is always fatal */
static GLogLevelFlags fatal = G_LOG_LEVEL_ERROR;
static GLogFunc default_log_func;
static gpointer default_log_func_user_data;
static GPrintFunc stdout_handler, stderr_handler;

static void default_stdout_handler (const gchar *string);
static void default_stderr_handler (const gchar *string);

static GAbortFunc internal_abort_func;

void
g_assertion_disable_global (GAbortFunc abort_func)
{
	internal_abort_func = abort_func;
}

void
g_assert_abort (void)
{
	if (internal_abort_func)
		internal_abort_func ();
	else
		abort ();
}

gint
g_printv (const gchar *format, va_list args)
{
	char *msg;
	int ret;

	ret = g_vasprintf (&msg, format, args);
	if (ret < 0)
		return -1;

	if (!stdout_handler)
		stdout_handler = default_stdout_handler;

	stdout_handler (msg);
	g_free (msg);

	return ret;
}

void
g_print (const gchar *format, ...)
{
	va_list args;
	va_start (args, format);
	g_printv (format, args);
	va_end (args);
}

gint
g_printf (gchar const *format, ...)
{
	va_list args;
	gint ret;

	va_start (args, format);
	ret = g_printv (format, args);
	va_end (args);

	return ret;
}

void
g_printerr (const gchar *format, ...)
{
	char *msg;
	va_list args;

	va_start (args, format);
	if (g_vasprintf (&msg, format, args) < 0) {
		va_end (args);
		return;
	}
	va_end (args);

	if (!stderr_handler)
		stderr_handler = default_stderr_handler;

	stderr_handler (msg);
	g_free (msg);
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

#define g_logstr monoeg_g_logstr
#define g_logv_nofree monoeg_g_logv_nofree

static void
g_logstr (const gchar *log_domain, GLogLevelFlags log_level, const gchar *msg)
{
	if (!default_log_func)
		default_log_func = g_log_default_handler;
	
	default_log_func (log_domain, log_level, msg, default_log_func_user_data);
}

static gchar*
g_logv_nofree (const gchar *log_domain, GLogLevelFlags log_level, const gchar *format, va_list args)
{
	char *msg;

	if (internal_abort_func) {
		g_async_safe_vprintf (format, args);
		return NULL;
	} else if (g_vasprintf (&msg, format, args) < 0) {
		return NULL;
	}

	g_logstr (log_domain, log_level, msg);
	return msg;
}

void
g_logv (const gchar *log_domain, GLogLevelFlags log_level, const gchar *format, va_list args)
{
	g_free (g_logv_nofree (log_domain, log_level, format, args));
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
g_log_disabled (const gchar *log_domain, GLogLevelFlags log_level, const char *file, int line)
{
	g_log (log_domain, log_level, "%s:%d <disabled>", file, line);
}

static char *failure_assertion = NULL;

const char *
g_get_assertion_message (void)
{
	return failure_assertion;
}

void
g_assertion_message (const gchar *format, ...)
{
	va_list args;

	va_start (args, format);

	failure_assertion = g_logv_nofree (G_LOG_DOMAIN, G_LOG_LEVEL_ERROR, format, args);

	va_end (args);
	exit (0);
}

// Emscriptem emulates varargs, and fails to stack pack multiple outgoing varargs areas,
// so this function serves to remove varargs in its caller and conserve stack.
void
mono_assertion_message_disabled (const char *file, int line)
{
	mono_assertion_message (file, line, "<disabled>");
}

// Emscriptem emulates varargs, and fails to stack pack multiple outgoing varargs areas,
// so this function serves to remove varargs in its caller and conserve stack.
void
mono_assertion_message (const char *file, int line, const char *condition)
{
	g_assertion_message ("* Assertion at %s:%d, condition `%s' not met\n", file, line, condition);
}

// Emscriptem emulates varargs, and fails to stack pack multiple outgoing varargs areas,
// so this function serves to remove varargs in its caller and conserve stack.
void
mono_assertion_message_unreachable (const char *file, int line)
{
	g_assertion_message ("* Assertion: should not be reached at %s:%d\n", file, line);
}

#if HOST_ANDROID
#include <android/log.h>

static android_LogPriority
to_android_priority (GLogLevelFlags log_level)
{
	switch (log_level & G_LOG_LEVEL_MASK)
	{
		case G_LOG_LEVEL_ERROR:     return ANDROID_LOG_FATAL;
		case G_LOG_LEVEL_CRITICAL:  return ANDROID_LOG_ERROR;
		case G_LOG_LEVEL_WARNING:   return ANDROID_LOG_WARN;
		case G_LOG_LEVEL_MESSAGE:   return ANDROID_LOG_INFO;
		case G_LOG_LEVEL_INFO:      return ANDROID_LOG_DEBUG;
		case G_LOG_LEVEL_DEBUG:     return ANDROID_LOG_VERBOSE;
	}
	return ANDROID_LOG_UNKNOWN;
}

#define LOG_MESSAGE_MAX_LEN 4096

static void
android_log_line (gint log_priority, const gchar *log_domain, const gchar *log_message, gint log_len)
{
	gchar log_buf [LOG_MESSAGE_MAX_LEN];

	g_assert (log_len <= LOG_MESSAGE_MAX_LEN - 1);

	/* If line is longer than LOG_MESSAGE_MAX_LEN - 1, then we simply cut it out. This is consistent with the previous behavior. */
	strncpy (log_buf, log_message, log_len);
	log_buf [log_len] = '\0';

	__android_log_write (log_priority, log_domain, log_buf);
}

static void
android_log (gint log_priority, const gchar *log_domain, const gchar *log_message)
{
	gint log_message_len, log_message_p_len;
	const gchar *log_message_p;

	log_message_len = strlen (log_message);
	if (log_message_len <= LOG_MESSAGE_MAX_LEN) {
		__android_log_write (log_priority, log_domain, log_message);
		return;
	}

	for (log_message_p = log_message; log_message_p < log_message + log_message_len;) {
		const gchar *p = strstr (log_message_p, "\n");
		if (p == NULL) {
			/* There is no more "\n". */
			android_log_line (log_priority, log_domain, log_message_p, LOG_MESSAGE_MAX_LEN - 1);
			break;
		}

		log_message_p_len = p - log_message_p;
		if (log_message_p_len > LOG_MESSAGE_MAX_LEN - 1)
			log_message_p_len = LOG_MESSAGE_MAX_LEN - 1;

		android_log_line (log_priority, log_domain, log_message_p, log_message_p_len);

		/* Set `log_message_p` to the character right after "\n" */
		log_message_p = p + 1;
	}
}

void
g_log_default_handler (const gchar *log_domain, GLogLevelFlags log_level, const gchar *message, gpointer unused_data)
{
	android_log (to_android_priority (log_level), log_domain, message);
	if (log_level & fatal)
		g_assert_abort ();
}

static void
default_stdout_handler (const gchar *message)
{
	/* TODO: provide a proper app name */
	android_log (ANDROID_LOG_ERROR, "mono", message);
}

static void
default_stderr_handler (const gchar *message)
{
	/* TODO: provide a proper app name */
	android_log (ANDROID_LOG_ERROR, "mono", message);
}


#elif defined(HOST_IOS)
#include <asl.h>

static int
to_asl_priority (GLogLevelFlags log_level)
{
	switch (log_level & G_LOG_LEVEL_MASK)
	{
		case G_LOG_LEVEL_ERROR:     return ASL_LEVEL_CRIT;
		case G_LOG_LEVEL_CRITICAL:  return ASL_LEVEL_ERR;
		case G_LOG_LEVEL_WARNING:   return ASL_LEVEL_WARNING;
		case G_LOG_LEVEL_MESSAGE:   return ASL_LEVEL_NOTICE;
		case G_LOG_LEVEL_INFO:      return ASL_LEVEL_INFO;
		case G_LOG_LEVEL_DEBUG:     return ASL_LEVEL_DEBUG;
	}
	return ASL_LEVEL_ERR;
}

void
g_log_default_handler (const gchar *log_domain, GLogLevelFlags log_level, const gchar *message, gpointer unused_data)
{
	asl_log (NULL, NULL, to_asl_priority (log_level), "%s", message);
	if (log_level & fatal)
		g_assert_abort ();
}

static void
default_stdout_handler (const gchar *message)
{
	asl_log (NULL, NULL, ASL_LEVEL_WARNING, "%s", message);
}

static void
default_stderr_handler (const gchar *message)
{
	asl_log (NULL, NULL, ASL_LEVEL_WARNING, "%s", message);
}

#else

void
g_log_default_handler (const gchar *log_domain, GLogLevelFlags log_level, const gchar *message, gpointer unused_data)
{
	FILE *target = stdout;

	fprintf (target, "%s%s%s\n",
		log_domain != NULL ? log_domain : "",
		log_domain != NULL ? ": " : "",
		message);

	if (log_level & fatal) {
		fflush (stdout);
		fflush (stderr);
		g_assert_abort ();
	}
}

static void
default_stdout_handler (const gchar *string)
{
	fprintf (stdout, "%s", string);
}

static void
default_stderr_handler (const gchar *string)
{
	fprintf (stderr, "%s", string);
}

#endif

GLogFunc
g_log_set_default_handler (GLogFunc log_func, gpointer user_data)
{
	GLogFunc old = default_log_func;
	default_log_func = log_func;
	default_log_func_user_data = user_data;
	return old;
}

GPrintFunc
g_set_print_handler (GPrintFunc func)
{
	GPrintFunc old = stdout_handler;
	stdout_handler = func;
	return old;
}

GPrintFunc
g_set_printerr_handler (GPrintFunc func)
{
	GPrintFunc old = stderr_handler;
	stderr_handler = func;
	return old;
}

