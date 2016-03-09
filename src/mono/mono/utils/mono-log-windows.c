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

#ifdef WIN32

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>
#include <errno.h>
#include <time.h>
#include <sys/time.h>
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
static __inline__ char 
mapLogFileLevel(GLogLevelFlags level) 
{
	if (level & G_LOG_LEVEL_ERROR)
		return ('E');
	if (level & G_LOG_LEVEL_CRIT)
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
mono_log_write_syslog(const char *domain, GLogLevelFlags level, const char *format, va_list args)
{
	time_t t;
	struct tm tod;
	char logTime[80];
	pid_t pid;

	if (logFile == NULL)
		mono_log_open_logfile(NULL, NULL);

	time(&t);
	localtime_r(&t, &tod);
	pid = getpid();
	strftime(logTime, sizeof(logTime), "%F %T", &tod);
	fprintf(logFile, "%s level[%c] mono[%d]: ",logTime,mapLogFileLevel(level),pid);
	vfprintf(logFile, format, args);
	fputc('\n', logFile);
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
