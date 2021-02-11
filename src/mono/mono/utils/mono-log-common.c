/**
 * \file
 * Platform-independent interface to the logger
 *
 * This module contains the POSIX syslog logger interface
 *
 * Author:
 *    Neale Ferguson <neale@sinenomine.net>
 *
 */
#include <config.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>
#include <errno.h>
#include <time.h>
#ifndef HOST_WIN32
#include <sys/time.h>
#else
#include <process.h>
#endif
#include "mono-logger-internals.h"
#include "mono-proclib.h"
#include "mono-time.h"

static FILE *logFile = NULL;
static void *logUserData = NULL;

/**
 * mapSyslogLevel:
 * 	
 * 	@level - GLogLevelFlags value
 * 	@returns The equivalent character identifier
 */
static char
mapLogFileLevel (GLogLevelFlags level)
{
	if (level & G_LOG_LEVEL_ERROR)
		return ('E');
	if (level & G_LOG_LEVEL_CRITICAL)
		return ('C');
	if (level & G_LOG_LEVEL_WARNING)
		return ('W');
	if (level & G_LOG_LEVEL_MESSAGE)
		return ('N');
	if (level & G_LOG_LEVEL_INFO)
		return ('I');
	if (level & G_LOG_LEVEL_DEBUG)
		return ('D');
	return ('I');
}

/**
 * mono_log_open_logfile:
 * \param path Path for log file
 * \param userData Not used
 * Open the logfile. If the path is not specified default to stdout. If the
 * open fails issue a warning and use stdout as the log file destination.
 */
void
mono_log_open_logfile(const char *path, void *userData)
{
	if (path == NULL) {
		logFile = stdout;
	} else {
#ifndef HOST_WIN32
		logFile = fopen(path, "w");
#else
		gunichar2 *wPath = g_utf8_to_utf16(path, -1, 0, 0, 0);
		if (wPath != NULL) {
			logFile = _wfopen((wchar_t *) wPath, L"w");
			g_free (wPath);
		}
#endif
		if (logFile == NULL) {
			g_warning("opening of log file %s failed with %s - defaulting to stdout", 
				  path, strerror(errno));
			logFile = stdout;
		}
	}
	logUserData = userData;
}

/**
 * mono_log_write_logfile:
 * \param domain Identifier string
 * \param level Logging level flags
 * \param format \c printf format string
 * \param vargs Variable argument list
 * Write data to the log file.
 */
void
mono_log_write_logfile (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message)
{
	time_t t;

	if (logFile == NULL)
		logFile = stdout;

	if (hdr) {
		pid_t pid;
		char logTime [80];

#ifdef HAVE_LOCALTIME_R
		struct tm tod;
		time(&t);
		localtime_r(&t, &tod);
		strftime(logTime, sizeof(logTime), MONO_STRFTIME_F " " MONO_STRFTIME_T, &tod);
#else
		struct tm *tod;
		time(&t);
		tod = localtime(&t);
		strftime(logTime, sizeof(logTime), MONO_STRFTIME_F " " MONO_STRFTIME_T, tod);
#endif

		pid = mono_process_current_pid ();

		fprintf (logFile, "%s level[%c] mono[%d]: %s\n", logTime, mapLogFileLevel (level), pid, message);
	} else {
		fprintf (logFile, "%s%s%s\n",
			log_domain != NULL ? log_domain : "",
			log_domain != NULL ? ": " : "",
			message);
	}

	fflush(logFile);

	if (level & G_LOG_LEVEL_ERROR)
		g_assert_abort ();
}

/**
 * mono_log_close_logfile:
 * Close the log file
 */
void
mono_log_close_logfile()
{
	if (logFile) {
		if (logFile != stdout)
			fclose(logFile);
		logFile = NULL;
	}
}
