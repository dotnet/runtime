#ifndef __MONO_LOGGER_H__
#define __MONO_LOGGER_H__

#include <mono/utils/mono-publib.h>
MONO_BEGIN_DECLS

MONO_API void 
mono_trace_set_level_string (const char *value);

MONO_API void 
mono_trace_set_mask_string (const char *value);

MONO_API void 
mono_trace_set_logdest_string (const char *value);

MONO_API void 
mono_trace_set_logheader_string (const char *value);

typedef void (*MonoPrintCallback) (const char *string, mono_bool is_stdout);
typedef void (*MonoLogCallback) (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data);


typedef void (*MonoLoggerOpen) (const char *, void *);
typedef void (*MonoLoggerWrite) (const char *, GLogLevelFlags, mono_bool, const char *, va_list);
typedef void (*MonoLoggerClose) (void);

typedef struct _MonoLogCallParm_ {
	MonoLoggerOpen 	opener;		/* Routine to open logging */
	MonoLoggerWrite	writer;		/* Routine to write log data */
	MonoLoggerClose closer; 	/* Routine to close logging */
 	char		*dest;		/* Log destination */
	void		*user_data;	/* User data from legacy handler */
	mono_bool       header;		/* Whether we want pid/time/date in log message */
} MonoLogCallParm;

void
mono_trace_set_log_handler_internal (MonoLogCallParm *callback, void *user_data);

MONO_API void
mono_trace_set_log_handler (MonoLogCallback callback, void *user_data);

MONO_API void
mono_trace_set_print_handler (MonoPrintCallback callback);

MONO_API void
mono_trace_set_printerr_handler (MonoPrintCallback callback);

MONO_API void
mono_log_open_syslog(const char *, void *);

MONO_API void
mono_log_write_syslog(const char *, GLogLevelFlags, mono_bool, const char *, va_list);

MONO_API void
mono_log_close_syslog(void);

MONO_API void
mono_log_open_logfile(const char *, void *);

MONO_API void
mono_log_write_logfile(const char *, GLogLevelFlags, mono_bool, const char *, va_list);

MONO_API void
mono_log_close_logfile(void);

MONO_END_DECLS

#endif /* __MONO_LOGGER_H__ */
