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
void* mono_perfcounter_get_impl (MonoString* category, MonoString* counter, MonoString* instance,
		int *type, MonoBoolean *custom);

ICALL_EXPORT
MonoBoolean mono_perfcounter_get_sample (void *impl, MonoBoolean only_value, MonoCounterSample *sample);

ICALL_EXPORT
gint64 mono_perfcounter_update_value    (void *impl, MonoBoolean do_incr, gint64 value);

ICALL_EXPORT
void   mono_perfcounter_free_data       (void *impl);

/* Category icalls */
ICALL_EXPORT
MonoBoolean mono_perfcounter_category_del    (MonoString *name);

ICALL_EXPORT
MonoString* mono_perfcounter_category_help   (MonoString *category);

ICALL_EXPORT
MonoBoolean mono_perfcounter_category_exists (MonoString *counter, MonoString *category);

ICALL_EXPORT
MonoBoolean mono_perfcounter_create          (MonoString *category, MonoString *help, int type, MonoArray *items);

ICALL_EXPORT
MonoBoolean mono_perfcounter_instance_exists (MonoString *instance, MonoString *category);

ICALL_EXPORT
MonoArray*  mono_perfcounter_category_names  (void);

ICALL_EXPORT
MonoArray*  mono_perfcounter_counter_names   (MonoString *category);

ICALL_EXPORT
MonoArray*  mono_perfcounter_instance_names  (MonoString *category);

typedef gboolean (*PerfCounterEnumCallback) (char *category_name, char *name, unsigned char type, gint64 value, gpointer user_data);
MONO_API void mono_perfcounter_foreach (PerfCounterEnumCallback cb, gpointer user_data);

#endif /* __MONO_PERFCOUNTERS_H__ */

