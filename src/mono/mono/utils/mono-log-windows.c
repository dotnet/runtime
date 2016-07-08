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
#include "mono-logger.h"

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
mono_log_write_syslog(const char *domain, GLogLevelFlags level, mono_bool hdr, const char *format, va_list args)
{
	time_t t;
	struct tm *tod;
	char logTime[80],
	      logMessage[512];
	pid_t pid;
	int iLog = 0;
	size_t nLog;

	if (logFile == NULL)
		mono_log_open_syslog(NULL, NULL);

	time(&t);
	tod = localtime(&t);
	pid = _getpid();
	strftime(logTime, sizeof(logTime), "%Y-%m-%d %H:%M:%S", tod);
	iLog = sprintf(logMessage, "%s level[%c] mono[%d]: ",
		       logTime,mapLogFileLevel(level),pid);
	nLog = sizeof(logMessage) - iLog - 2;
	vsnprintf(logMessage+iLog, nLog, format, args);
	iLog = strlen(logMessage);
	logMessage[iLog++] = '\n';
	logMessage[iLog++] = 0;
	fputs(logMessage, logFile);
	fflush(logFile);

	if (level == G_LOG_FLAG_FATAL)
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
