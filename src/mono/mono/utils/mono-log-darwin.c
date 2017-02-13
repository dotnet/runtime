/**
 * \file
 * Darwin-specific interface to the logger
 *
 */
#include <config.h>

#if defined(HOST_IOS)

#include <asl.h>
#include "mono-logger-internals.h"
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
mono_log_open_asl (const char *path, void *userData)
{
}

void
mono_log_write_asl (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message)
{
	asl_log (NULL, NULL, to_asl_priority (level), "%s%s%s\n",
		log_domain != NULL ? log_domain : "",
		log_domain != NULL ? ": " : "",
		message);

	if (level & G_LOG_LEVEL_ERROR)
		abort();
}

void
mono_log_close_asl ()
{
}
#endif
