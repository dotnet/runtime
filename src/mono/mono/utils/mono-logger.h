#ifndef __MONO_LOGGER_H__
#define __MONO_LOGGER_H__

#include <glib.h>

typedef enum {
	MONO_TRACE_ASSEMBLY		= (1<<0),
	MONO_TRACE_TYPE			= (1<<1),
	MONO_TRACE_DLLIMPORT	= (1<<2),
	MONO_TRACE_GC			= (1<<3),

	MONO_TRACE_ALL			=   MONO_TRACE_ASSEMBLY |
								MONO_TRACE_TYPE |
								MONO_TRACE_DLLIMPORT |
								MONO_TRACE_GC,
} MonoTraceMask;

void 
mono_trace_cleanup (void);

void 
mono_trace (GLogLevelFlags level, MonoTraceMask mask, const char *format, ...);

void 
mono_tracev (GLogLevelFlags level, MonoTraceMask mask, const char *format, va_list args);

void 
mono_trace_set_level (GLogLevelFlags level);

void 
mono_trace_set_level_string (const char *value);

void 
mono_trace_set_mask (MonoTraceMask mask);

void 
mono_trace_set_mask_string (char *value);

void 
mono_trace_push (GLogLevelFlags level, MonoTraceMask mask);

void 
mono_trace_pop (void);

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

#endif /* __MONO_LOGGER_H__ */
