/**
 * \file
 * Darwin-specific interface to the logger
 *
 */
#include <config.h>

#if defined(HOST_IOS) || defined(HOST_TVOS) || defined(HOST_WATCHOS) || defined(HOST_MACCAT)
#include <os/log.h>
#include "mono-logger-internals.h"

void
mono_log_open_os_log (const char *path, void *userData)
{
}

static int
to_os_log_priority (GLogLevelFlags log_level)
{
	switch (log_level & G_LOG_LEVEL_MASK)
	{
		case G_LOG_LEVEL_ERROR:     return OS_LOG_TYPE_ERROR;
		case G_LOG_LEVEL_CRITICAL:  return OS_LOG_TYPE_ERROR;
		case G_LOG_LEVEL_WARNING:   return OS_LOG_TYPE_DEFAULT;
		case G_LOG_LEVEL_MESSAGE:   return OS_LOG_TYPE_DEFAULT;
		case G_LOG_LEVEL_INFO:      return OS_LOG_TYPE_DEFAULT;
		case G_LOG_LEVEL_DEBUG:     return OS_LOG_TYPE_DEFAULT;
	}
	return OS_LOG_TYPE_ERROR;
}

static const char *
to_log_level_name (GLogLevelFlags log_level)
{
	switch (log_level & G_LOG_LEVEL_MASK)
	{
		case G_LOG_LEVEL_ERROR:     return "error";
		case G_LOG_LEVEL_CRITICAL:  return "critical";
		case G_LOG_LEVEL_WARNING:   return "warning";
		case G_LOG_LEVEL_MESSAGE:   return "message";
		case G_LOG_LEVEL_INFO:      return "info";
		case G_LOG_LEVEL_DEBUG:     return "debug";
	}
	return "unknown";
}

// keep in sync with g_log_default_handler
void
mono_log_write_os_log (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message)
{
	os_log_with_type (OS_LOG_DEFAULT, to_os_log_priority (level), "%{public}s%{public}s%{public}s: %{public}s",
		log_domain != NULL ? log_domain : "",
		log_domain != NULL ? ": " : "",
		to_log_level_name(level),
		message);

	if (level & G_LOG_LEVEL_ERROR)
		abort();
}

void
mono_log_close_os_log ()
{
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_log_darwin);

#endif
