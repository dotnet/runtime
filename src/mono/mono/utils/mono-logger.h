#ifndef __MONO_LOGGER_H__
#define __MONO_LOGGER_H__

#include <mono/utils/mono-publib.h>
MONO_BEGIN_DECLS

MONO_API void 
mono_trace_set_level_string (const char *value);

MONO_API void 
mono_trace_set_mask_string (const char *value);

typedef void (*MonoLogCallback) (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data);
typedef void (*MonoPrintCallback) (const char *string, mono_bool is_stdout);

MONO_API void
mono_trace_set_log_handler (MonoLogCallback callback, void *user_data);

MONO_API void
mono_trace_set_print_handler (MonoPrintCallback callback);

MONO_API void
mono_trace_set_printerr_handler (MonoPrintCallback callback);


MONO_END_DECLS

#endif /* __MONO_LOGGER_H__ */
