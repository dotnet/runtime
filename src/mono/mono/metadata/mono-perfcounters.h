#ifndef __MONO_PERFCOUNTERS_H__
#define __MONO_PERFCOUNTERS_H__

#include <glib.h>
#include <metadata/object.h>
#include <utils/mono-compiler.h>

typedef struct _MonoCounterSample MonoCounterSample;

void* mono_perfcounter_get_impl (MonoString* category, MonoString* counter, MonoString* instance,
		MonoString* machine, int *type, MonoBoolean *custom) MONO_INTERNAL;

MonoBoolean mono_perfcounter_get_sample (void *impl, MonoBoolean only_value, MonoCounterSample *sample) MONO_INTERNAL;

gint64 mono_perfcounter_update_value    (void *impl, MonoBoolean do_incr, gint64 value) MONO_INTERNAL;
void   mono_perfcounter_free_data       (void *impl) MONO_INTERNAL;

#endif /* __MONO_PERFCOUNTERS_H__ */

