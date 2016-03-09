/*
 * mono-log-posix.c: POSIX interface to the logger
 *
 * This module contains the POSIX syslog logger routines
 *
 * Author:
 *    Neale Ferguson <neale@sinenomine.net>
 *
 */
#include <config.h>

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#if defined(_POSIX_VERSION) 

#include <stdlib.h>
#include <stdio.h>
#include <ctype.h>
#include <string.h>
#include <glib.h>
#include <syslog.h>
#include <stdarg.h>
#include <errno.h>
#include <time.h>
#include <sys/time.h>
#include "mono-logger.h"

static void *logUserData = NULL;

/**
 * mapSyslogLevel:
 * 	
 * 	@level - GLogLevelFlags value
 * 	@returns The equivalent syslog priority value
 */
static __inline__ int
mapSyslogLevel(GLogLevelFlags level) 
{
	if (level & G_LOG_LEVEL_ERROR)
		return (LOG_ERR);
	if (level & G_LOG_LEVEL_CRITICAL)
		return (LOG_CRIT);
	if (level & G_LOG_LEVEL_WARNING)
		return (LOG_WARNING);
	if (level & G_LOG_LEVEL_MESSAGE)
		return (LOG_NOTICE);
	if (level & G_LOG_LEVEL_INFO)
		return (LOG_INFO);
	if (level & G_LOG_LEVEL_DEBUG)
		return (LOG_DEBUG);
	return (LOG_INFO);
}

/**
 * mono_log_open_logfile
 * 	
 *	Open the syslog interface specifying that we want our PID recorded 
 *	and that we're using the LOG_USER facility.
 *
 * 	@ident - Identifier: ignored
 * 	@userData - Not used
 */
void
mono_log_open_syslog(const char *ident, void *userData)
{
	openlog("mono", LOG_PID, LOG_USER);
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
	vsyslog(mapSyslogLevel(level), format, args);

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
	closelog();
}
#endif
