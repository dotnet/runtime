/**
 * \file
 * Android-specific interface to the logger
 *
 * This module contains the Android logcat logger interface
 *
 * Author:
 *    Marek Habersack <grendel@twistedcode.net>
 *
 */
#include <config.h>

#if defined (HOST_ANDROID)

#include <android/log.h>
#include "mono-logger-internals.h"

/**
 * mono_log_open_logcat:
 * \param path Unused
 * \param userData Unused
 * Open access to Android logcat (no-op)
 */   
void
mono_log_open_logcat (const char *path, void *userData)
{
	/* No-op on Android */
}

/**
 * mono_log_write_logcat:
 * \param domain Identifier string
 * \param level Logging level flags
 * \param format \c printf format string
 * \param vargs Variable argument list
 * Write data to Android logcat.
 */
void
mono_log_write_logcat (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message)
{
	android_LogPriority apriority;

	switch (level & G_LOG_LEVEL_MASK)
	{
		case G_LOG_LEVEL_ERROR:
			apriority = ANDROID_LOG_FATAL;
			break;

		case G_LOG_LEVEL_CRITICAL:
			apriority = ANDROID_LOG_ERROR;
			break;

		case G_LOG_LEVEL_WARNING:
			apriority = ANDROID_LOG_WARN;
			break;

		case G_LOG_LEVEL_MESSAGE:
			apriority = ANDROID_LOG_INFO;
			break;

		case G_LOG_LEVEL_INFO:
			apriority = ANDROID_LOG_DEBUG;
			break;

		case G_LOG_LEVEL_DEBUG:
			apriority = ANDROID_LOG_VERBOSE;
			break;

		default:
			apriority = ANDROID_LOG_UNKNOWN;
			break;
	}

	__android_log_write (apriority, log_domain, message);
	if (apriority == ANDROID_LOG_FATAL)
		g_assert_abort ();
}

/**
 * mono_log_close_logcat
 *
 * 	Close access to Android logcat (no-op)
 */
void
mono_log_close_logcat ()
{
	/* No-op on Android */
}

#else

#include <mono/utils/mono-compiler.h>

MONO_EMPTY_SOURCE_FILE (mono_log_android);

#endif
