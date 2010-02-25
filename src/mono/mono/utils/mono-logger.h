#ifndef __MONO_LOGGER_H__
#define __MONO_LOGGER_H__

#include <mono/utils/mono-publib.h>
MONO_BEGIN_DECLS

void 
mono_trace_set_level_string (const char *value);

void 
mono_trace_set_mask_string (const char *value);

MONO_END_DECLS

#endif /* __MONO_LOGGER_H__ */
