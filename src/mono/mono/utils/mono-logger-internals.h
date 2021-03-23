/**
 * \file
 */

#ifndef __MONO_LOGGER_INTERNAL_H__
#define __MONO_LOGGER_INTERNAL_H__

#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include "mono-logger.h"

typedef enum {
	MONO_TRACE_ASSEMBLY           = 1 << 0,
	MONO_TRACE_TYPE               = 1 << 1,
	MONO_TRACE_DLLIMPORT          = 1 << 2,
	MONO_TRACE_GC                 = 1 << 3,
	MONO_TRACE_CONFIG             = 1 << 4,
	MONO_TRACE_AOT                = 1 << 5,
	MONO_TRACE_SECURITY           = 1 << 6,
	MONO_TRACE_THREADPOOL         = 1 << 7,
	MONO_TRACE_IO_SELECTOR        = 1 << 8,
	MONO_TRACE_IO_LAYER_PROCESS   = 1 << 9,
	MONO_TRACE_IO_LAYER_SOCKET    = 1 << 10,
	MONO_TRACE_IO_LAYER_FILE      = 1 << 11,
	MONO_TRACE_IO_LAYER_EVENT     = 1 << 12,
	MONO_TRACE_IO_LAYER_SEMAPHORE = 1 << 13,
	MONO_TRACE_IO_LAYER_MUTEX     = 1 << 14,
	MONO_TRACE_IO_LAYER_HANDLE    = 1 << 15,
	MONO_TRACE_TAILCALL           = 1 << 16,
	MONO_TRACE_PROFILER           = 1 << 17,
	MONO_TRACE_TIERED             = 1 << 18,
	MONO_TRACE_QCALL              = 1 << 19,
	MONO_TRACE_METADATA_UPDATE    = 1 << 20,
} MonoTraceMask;

MONO_BEGIN_DECLS
MONO_API_DATA GLogLevelFlags mono_internal_current_level;
MONO_API_DATA MonoTraceMask mono_internal_current_mask;
MONO_END_DECLS

MONO_API void
mono_trace_init (void);

void 
mono_trace_cleanup (void);

MONO_API void
mono_tracev_inner (GLogLevelFlags level, MonoTraceMask mask, const char *format, va_list args);

void 
mono_trace_set_level (GLogLevelFlags level);

void 
mono_trace_set_mask (MonoTraceMask mask);

void 
mono_trace_push (GLogLevelFlags level, MonoTraceMask mask);

void 
mono_trace_pop (void);

gboolean
mono_trace_is_traced (GLogLevelFlags level, MonoTraceMask mask);

#define MONO_TRACE_IS_TRACED(level, mask) \
	G_UNLIKELY ((level) <= mono_internal_current_level && ((mask) & mono_internal_current_mask))

G_GNUC_UNUSED static void
mono_tracev (GLogLevelFlags level, MonoTraceMask mask, const char *format, va_list args)
{
	if (MONO_TRACE_IS_TRACED (level, mask))
		mono_tracev_inner (level, mask, format, args);
}

/**
 * mono_trace:
 *
 *	@level: Verbose level of the specified message
 *	@mask: Type of the specified message
 *
 * Traces a new message, depending on the current logging level
 * and trace mask.
 */
G_GNUC_UNUSED MONO_ATTR_FORMAT_PRINTF(3,4) static void
mono_trace (GLogLevelFlags level, MonoTraceMask mask, const char *format, ...)
{
	if (MONO_TRACE_IS_TRACED (level, mask)) {
		va_list args;
		va_start (args, format);
		mono_tracev_inner (level, mask, format, args);
		va_end (args);
	}
}

// __VA_ARGS__ is never empty, so a comma before it is always correct.
#define mono_trace_error(...)	(mono_trace (G_LOG_LEVEL_ERROR, __VA_ARGS__))
#define mono_trace_warning(...) (mono_trace (G_LOG_LEVEL_WARNING, __VA_ARGS__))
#define mono_trace_message(...) (mono_trace (G_LOG_LEVEL_MESSAGE, __VA_ARGS__))

#if defined (HOST_ANDROID) || (defined (TARGET_IOS) && defined (TARGET_IOS))

#define mono_gc_printf(gc_log_file, format, ...) g_log ("mono-gc", G_LOG_LEVEL_MESSAGE, format, ##__VA_ARGS__)
#define mono_runtime_printf(format, ...) g_log ("mono-rt", G_LOG_LEVEL_MESSAGE, format "\n", ##__VA_ARGS__)
#define mono_runtime_printf_err(format, ...) g_log ("mono-rt", G_LOG_LEVEL_CRITICAL, format "\n", ##__VA_ARGS__)
#define mono_profiler_printf(format, ...) g_log ("mono-prof", G_LOG_LEVEL_MESSAGE, format "\n", ##__VA_ARGS__)
#define mono_profiler_printf_err(format, ...) g_log ("mono-prof", G_LOG_LEVEL_CRITICAL, format "\n", ##__VA_ARGS__)
#define mono_runtime_stdout_fflush() do { } while (0)

#else

#define mono_gc_printf(gc_log_file, format, ...) do {	\
	fprintf (gc_log_file, format, ##__VA_ARGS__);	\
	fflush (gc_log_file);	\
} while (0)

#define mono_runtime_printf(format, ...) fprintf (stdout, format "\n", ##__VA_ARGS__)
#define mono_runtime_printf_err(format, ...) fprintf (stderr, format "\n", ##__VA_ARGS__)
#define mono_profiler_printf(format, ...) fprintf (stdout, format "\n", ##__VA_ARGS__)
#define mono_profiler_printf_err(format, ...) fprintf (stderr, format "\n", ##__VA_ARGS__)
#define mono_runtime_stdout_fflush() do { fflush (stdout); } while (0)

#endif

/* Internal logging API */
typedef void (*MonoLoggerOpen) (const char *, void *);
typedef void (*MonoLoggerWrite) (const char *, GLogLevelFlags, mono_bool, const char *);
typedef void (*MonoLoggerClose) (void);

typedef struct {
	MonoLoggerOpen 	opener;		/* Routine to open logging */
	MonoLoggerWrite	writer;		/* Routine to write log data */
	MonoLoggerClose closer; 	/* Routine to close logging */
	char		*dest;		/* Log destination */
	void		*user_data;	/* User data from legacy handler */
	mono_bool       header;		/* Whether we want pid/time/date in log message */
} MonoLogCallParm;

void mono_trace_set_log_handler_internal (MonoLogCallParm *callback, void *user_data);
void mono_trace_set_logdest_string (const char *value);
void mono_trace_set_logheader_string (const char *value);

void mono_log_open_syslog (const char *, void *);
void mono_log_write_syslog (const char *, GLogLevelFlags, mono_bool, const char *);
void mono_log_close_syslog (void);

void mono_log_open_logfile (const char *, void *);
void mono_log_write_logfile (const char *, GLogLevelFlags, mono_bool, const char *);
void mono_log_close_logfile (void);

#if HOST_ANDROID
void mono_log_open_logcat (const char *path, void *userData);
void mono_log_write_logcat (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message);
void mono_log_close_logcat (void);
#endif

#if defined(HOST_IOS)
void mono_log_open_asl (const char *path, void *userData);
void mono_log_write_asl (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message);
void mono_log_close_asl (void);
#endif

void mono_log_open_recorder (const char *path, void *userData);
void mono_log_write_recorder (const char *log_domain, GLogLevelFlags level, mono_bool hdr, const char *message);
void mono_log_close_recorder (void);
void mono_log_dump_recorder (void);

void mono_dump_mem (gconstpointer d, int len);

#endif /* __MONO_LOGGER_INTERNAL_H__ */
