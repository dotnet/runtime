/**
 * \file
 */

#ifndef __MONO_PERFCOUNTERS_H__
#define __MONO_PERFCOUNTERS_H__

#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/icalls.h>

typedef struct _MonoCounterSample MonoCounterSample;

ICALL_EXPORT
MonoBoolean mono_perfcounter_get_sample (void *impl, MonoBoolean only_value, MonoCounterSample *sample);

ICALL_EXPORT
gint64 mono_perfcounter_update_value    (void *impl, MonoBoolean do_incr, gint64 value);

ICALL_EXPORT
void   mono_perfcounter_free_data       (void *impl);

typedef gboolean (*PerfCounterEnumCallback) (char *category_name, char *name, unsigned char type, gint64 value, gpointer user_data);
MONO_API void mono_perfcounter_foreach (PerfCounterEnumCallback cb, gpointer user_data);

#endif /* __MONO_PERFCOUNTERS_H__ */

