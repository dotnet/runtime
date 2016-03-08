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

typedef void (*MonoPrintCallback) (const char *string, mono_bool is_stdout);

typedef void (*MonoLoggerOpen) (const char *, void *);
typedef void (*MonoLoggerWrite) (const char *, GLogLevelFlags, const char *, ...);
typedef void (*MonoLoggerClose) (void);

typedef struct _MonoLogCallback_ {
	MonoLoggerOpen 	opener;		/* Routine to open logging */
	MonoLoggerWrite	writer;		/* Routine to write log data */
	MonoLoggerClose closer; 	/* Routine to close logging */
} MonoLogCallback;

MONO_API void
mono_trace_set_log_handler (MonoLogCallback *callback, const char *dest, void *user_data);

MONO_API void
mono_trace_set_print_handler (MonoPrintCallback callback);

MONO_API void
mono_trace_set_printerr_handler (MonoPrintCallback callback);

MONO_API void
mono_log_open_syslog(const char *, void *);

MONO_API void
mono_log_write_syslog(const char *, GLogLevelFlags, const char *, va_list);

MONO_API void
mono_log_close_syslog(void);

MONO_API void
mono_log_open_logfile(const char *, void *);

MONO_API void
mono_log_write_logfile(const char *, GLogLevelFlags, const char *, va_list);

MONO_API void
mono_log_close_logfile(void);

MONO_END_DECLS

#endif /* __MONO_LOGGER_H__ */
