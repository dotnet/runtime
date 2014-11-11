#ifndef __MONO_PERFCOUNTERS_H__
#define __MONO_PERFCOUNTERS_H__

#include <glib.h>
#include <mono/metadata/object.h>
#include <mono/utils/mono-compiler.h>

typedef struct _MonoCounterSample MonoCounterSample;

void* mono_perfcounter_get_impl (MonoString* category, MonoString* counter, MonoString* instance,
		MonoString* machine, int *type, MonoBoolean *custom) MONO_INTERNAL;

MonoBoolean mono_perfcounter_get_sample (void *impl, MonoBoolean only_value, MonoCounterSample *sample) MONO_INTERNAL;

gint64 mono_perfcounter_update_value    (void *impl, MonoBoolean do_incr, gint64 value) MONO_INTERNAL;
void   mono_perfcounter_free_data       (void *impl) MONO_INTERNAL;

/* Category icalls */
MonoBoolean mono_perfcounter_category_del    (MonoString *name) MONO_INTERNAL;
MonoString* mono_perfcounter_category_help   (MonoString *category, MonoString *machine) MONO_INTERNAL;
MonoBoolean mono_perfcounter_category_exists (MonoString *counter, MonoString *category, MonoString *machine) MONO_INTERNAL;
MonoBoolean mono_perfcounter_create          (MonoString *category, MonoString *help, int type, MonoArray *items) MONO_INTERNAL;
int         mono_perfcounter_instance_exists (MonoString *instance, MonoString *category, MonoString *machine) MONO_INTERNAL;
MonoArray*  mono_perfcounter_category_names  (MonoString *machine) MONO_INTERNAL;
MonoArray*  mono_perfcounter_counter_names   (MonoString *category, MonoString *machine) MONO_INTERNAL;
MonoArray*  mono_perfcounter_instance_names  (MonoString *category, MonoString *machine) MONO_INTERNAL;

typedef gboolean (*PerfCounterEnumCallback) (char *category_name, char *name, unsigned char type, gint64 value, gpointer user_data);
MONO_API void mono_perfcounter_foreach (PerfCounterEnumCallback cb, gpointer user_data);

#endif /* __MONO_PERFCOUNTERS_H__ */

