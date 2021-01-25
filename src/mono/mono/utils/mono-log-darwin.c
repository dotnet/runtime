/**
 * \file
 * Darwin-specific interface to the logger
 *
 */
#include <config.h>

#if (defined(HOST_WATCHOS) && (__WATCH_OS_VERSION_MIN_REQUIRED >= __WATCHOS_3_0)) || defined(HOST_MACCAT)
/* emitted by clang:
 *   > /Users/lewurm/work/mono-watch4/mono/utils/mono-log-darwin.c:35:2: error: 'asl_log' is \
 *   > deprecated: first deprecated in watchOS 3.0 - os_log(3) has replaced \
 *   > asl(3) [-Werror,-Wdeprecated-declarations]
 */

/* untested stuff: */
#include <os/log.h>
#include "mono-logger-internals.h"
void
mono_log_open_asl (const char *path, void *userData)
{
}

void
mono_log_write_asl (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message)
{
	switch (level & G_LOG_LEVEL_MASK)
	{
		case G_LOG_LEVEL_MESSAGE:
			os_log (OS_LOG_DEFAULT, "%s%s%s\n",
				log_domain != NULL ? log_domain : "",
				log_domain != NULL ? ": " : "",
				message);
			break;
		case G_LOG_LEVEL_INFO:
			os_log_info (OS_LOG_DEFAULT, "%s%s%s\n",
				log_domain != NULL ? log_domain : "",
				log_domain != NULL ? ": " : "",
				message);
			break;
		case G_LOG_LEVEL_DEBUG:
			os_log_debug (OS_LOG_DEFAULT, "%s%s%s\n",
				log_domain != NULL ? log_domain : "",
				log_domain != NULL ? ": " : "",
				message);
			break;
		case G_LOG_LEVEL_ERROR:
		case G_LOG_LEVEL_WARNING:
			os_log_error (OS_LOG_DEFAULT, "%s%s%s\n",
				log_domain != NULL ? log_domain : "",
				log_domain != NULL ? ": " : "",
				message);
		case G_LOG_LEVEL_CRITICAL:
		default:
			os_log_fault (OS_LOG_DEFAULT, "%s%s%s\n",
				log_domain != NULL ? log_domain : "",
				log_domain != NULL ? ": " : "",
				message);
			break;
	}

	if (level & G_LOG_LEVEL_ERROR)
		abort();
}

void
mono_log_close_asl ()
{
}

#elif defined(HOST_IOS)

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
		g_assert_abort ();
}

void
mono_log_close_asl ()
{
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_log_darwin);

#endif
