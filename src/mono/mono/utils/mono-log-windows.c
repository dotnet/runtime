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
#include "mono-logger.h"

static FILE *logFile = NULL;
static void *logUserData = NULL;
static char *logFileName = L".//mono.log";

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
 * mono_log_open_logfile
 * 	
 *	Open the logfile. If the path is not specified default to stdout. If the
 *	open fails issue a warning and use stdout as the log file destination.
 *
 * 	@ident - Identifier: ignored
 * 	@userData - Not used
 */
void
mono_log_open_syslog(const char *ident, void *userData)
{
	logFile = fopen(logFileName, "w");
	if (logFile == NULL) {
		g_warning("opening of log file %s failed with %s",
			  strerror(errno));
	}
	logUserData = userData;
}

/**
 * mono_log_write_logfile
 * 	
 * 	Write data to the log file.
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
	struct tm tod;
	char logTime[80],
	      logMessage[512];
	pid_t pid;
	int iLog = 0;
	size_t nLog;

	if (logFile == NULL)
		mono_log_open_logfile(NULL, NULL);

	time(&t);
	localtime(&t, &tod);
	pid = _getpid();
	strftime(logTime, sizeof(logTime), "%F %T", &tod);
	iLog = snprintf(logMessage, sizeof(logMessage), "%s level[%c] mono[%d]: ",
			logTime,mapLogFileLevel(level),pid);
	nLog = sizeof(logMessage) - iLog - 2;
	iLog = vsnprintf(logMessage+iLog, nLog, format, args);
	logMessage[iLog++] = '\n';
	logMessage[iLog++] = 0;
	fputs(logMessage, logFile);
	fflush(logFile);

	if (level == G_LOG_FLAG_FATAL)
		abort();
}

/**
 * mono_log_close_logfile
 *
 * 	Close the log file
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
