/**
 * \file
 * Simplistic simulation of a syslog logger for Windows
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
#include "mono-proclib.h"
#include "mono-time.h"

static FILE *logFile = NULL;
static void *logUserData = NULL;
static const wchar_t *logFileName = L".//mono.log"; // FIXME double slash

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
 * mono_log_open_syslog:
 * \param ident Identifier: ignored
 * \param userData Not used
 * Open the syslog file. If the open fails issue a warning and 
 * use stdout as the log file destination.
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
 * \param domain Identifier string
 * \param level Logging level flags
 * \param format \c printf format string
 * \param vargs Variable argument list
 * Write data to the syslog file.
 */
void
mono_log_write_syslog(const char *domain, GLogLevelFlags level, mono_bool hdr, const char *message)
{
	time_t t;
	int pid;
	char logTime [80];

	if (logFile == NULL)
		logFile = stdout;

	struct tm *tod;
	time(&t);
	tod = localtime(&t);
	pid = mono_process_current_pid ();
	strftime(logTime, sizeof(logTime), MONO_STRFTIME_F " " MONO_STRFTIME_T, tod);

	fprintf (logFile, "%s level[%c] mono[%d]: %s\n", logTime, mapLogFileLevel (level), pid, message);

	fflush(logFile);

	if (level & G_LOG_LEVEL_ERROR)
		g_assert_abort ();
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

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_log_windows);

#endif
