#ifndef __MONO_LOGGER_INTERNAL_H__
#define __MONO_LOGGER_INTERNAL_H__

#include <glib.h>
#include "mono-logger.h"

G_BEGIN_DECLS

typedef enum {
	MONO_TRACE_ASSEMBLY		= (1<<0),
	MONO_TRACE_TYPE			= (1<<1),
	MONO_TRACE_DLLIMPORT		= (1<<2),
	MONO_TRACE_GC			= (1<<3),
        MONO_TRACE_CONFIG		= (1<<4),
	MONO_TRACE_AOT			= (1<<5),
	MONO_TRACE_SECURITY		= (1<<6),
	MONO_TRACE_ALL			= MONO_TRACE_ASSEMBLY |
					  MONO_TRACE_TYPE |
					  MONO_TRACE_DLLIMPORT |
					  MONO_TRACE_GC |
					  MONO_TRACE_CONFIG |
					  MONO_TRACE_AOT |
					  MONO_TRACE_SECURITY
} MonoTraceMask;

void 
mono_trace_cleanup (void) MONO_INTERNAL;

void 
mono_trace (GLogLevelFlags level, MonoTraceMask mask, const char *format, ...) MONO_INTERNAL;

void 
mono_tracev (GLogLevelFlags level, MonoTraceMask mask, const char *format, va_list args) MONO_INTERNAL;

void 
mono_trace_set_level (GLogLevelFlags level) MONO_INTERNAL;

void 
mono_trace_set_mask (MonoTraceMask mask) MONO_INTERNAL;

void 
mono_trace_push (GLogLevelFlags level, MonoTraceMask mask) MONO_INTERNAL;

void 
mono_trace_pop (void) MONO_INTERNAL;

gboolean
mono_trace_is_traced (GLogLevelFlags level, MonoTraceMask mask) MONO_INTERNAL;

#ifdef G_HAVE_ISO_VARARGS
#define mono_trace_error(...)	mono_trace(G_LOG_LEVEL_ERROR, \
											__VA_ARGS__)
#define mono_trace_warning(...) mono_trace(G_LOG_LEVEL_WARNING, \
											__VA_ARGS__)
#define mono_trace_message(...) mono_trace(G_LOG_LEVEL_MESSAGE, \
											__VA_ARGS__)
#elif defined(G_HAVE_GNUC_VARARGS)
#define mono_trace_error(format...)	mono_trace(G_LOG_LEVEL_ERROR, \
											format)
#define mono_trace_warning(format...) mono_trace(G_LOG_LEVEL_WARNING, \
											format)
#define mono_trace_message(format...) mono_trace(G_LOG_LEVEL_MESSAGE, \
											format)
#else /* no varargs macros */
static void
mono_trace_error(MonoTraceMask mask, const char *format, ...)
{
	va_list args;
	va_start (args, format);
	mono_tracev(G_LOG_LEVEL_ERROR, mask, format, args);
	va_end (args);
}

static void
mono_trace_warning(MonoTraceMask mask, const char *format, ...)
{
	va_list args;
	va_start (args, format);
	mono_tracev(G_LOG_LEVEL_WARNING, mask, format, args);
	va_end (args);
}

static void
mono_trace_message(MonoTraceMask mask, const char *format, ...)
{
	va_list args;
	va_start (args, format);
	mono_tracev(G_LOG_LEVEL_MESSAGE, mask, format, args);
	va_end (args);
}

#endif /* !__GNUC__ */

G_END_DECLS

#endif /* __MONO_LOGGER_INTERNAL_H__ */
