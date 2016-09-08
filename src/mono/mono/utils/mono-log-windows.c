/*
 * mono-log-windows.c: Simplistic simulation of a syslog logger for Windows
 *
 * This module contains the Windows syslog logger interface
 *
 * Author:
 *    Neale Ferguson <neale@sinenomine.net>
 *
 */
#include <config.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#ifdef HOST_WIN32

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>
#include <errno.h>
#include <time.h>
#include <process.h>
#include "mono-logger-internals.h"

static FILE *logFile = NULL;
static void *logUserData = NULL;
static wchar_t *logFileName = L".//mono.log";

/**
 * mapSyslogLevel:
 * 	
 * 	@level - GLogLevelFlags value
 * 	@returns The equivalent character identifier
 */
static inline char 
mapLogFileLevel(GLogLevelFlags level) 
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
 * mono_log_open_syslog
 * 	
 *	Open the syslog file. If the open fails issue a warning and 
 *	use stdout as the log file destination.
 *
 * 	@ident - Identifier: ignored
 * 	@userData - Not used
 */
void
mono_log_open_syslog(const char *ident, void *userData)
{
	logFile = _wfopen(logFileName, L"w");
	if (logFile == NULL) {
		g_warning("opening of log file %s failed with %s",
			  strerror(errno));
		logFile = stdout;
	}
	logUserData = userData;
}

/**
 * mono_log_write_syslog
 * 	
 * 	Write data to the syslog file.
 *
 * 	@domain - Identifier string
 * 	@level - Logging level flags
 * 	@format - Printf format string
 * 	@vargs - Variable argument list
 */
void
mono_log_write_syslog(const char *domain, GLogLevelFlags level, mono_bool hdr, const char *message)
{
	time_t t;
	pid_t pid;
	char logTime [80];

	if (logFile == NULL)
		logFile = stdout;

	struct tm *tod;
	time(&t);
	tod = localtime(&t);
	pid = _getpid();
	strftime(logTime, sizeof(logTime), "%F %T", tod);

	fprintf (logFile, "%s level[%c] mono[%d]: %s\n", logTime, mapLogFileLevel (level), pid, message);

	fflush(logFile);

	if (level & G_LOG_LEVEL_ERROR)
		abort();
}

/**
 * mono_log_close_syslog
 *
 * 	Close the syslog file
 */
void
mono_log_close_syslog()
{
	if (logFile) {
		fclose(logFile);
		logFile = NULL;
	}
}
#endif
