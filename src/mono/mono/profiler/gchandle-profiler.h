#include <glib.h>
#include <pthread.h>

#include <mono/io-layer/mono-mutex.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/debug-helpers.h>

struct _MonoProfiler {
	const char *type_name; // Stacktraces are stored only for elements of this type
	GPtrArray *gchandles;
	GHashTable *jitted_methods;
	GPtrArray *stacktraces;
	mono_mutex_t mutex;
};

MonoProfiler* gchandle_profiler_new ();

// Functions which dump output to the terminal
void gchandle_profiler_dump_jitted_methods (MonoProfiler *prof);
void gchandle_profiler_dump_gchandles (MonoProfiler *prof);
void gchandle_profiler_dump_gchandle_traces (MonoProfiler *prof);

// The profiler hooks which are passed to/used by mono
void mono_profiler_startup (const char *desc);
void gchandle_profiler_method_jitted (MonoProfiler *prof, MonoMethod *method, MonoJitInfo* jinfo, int result);
void gchandle_profiler_track_gchandle (MonoProfiler *prof, int op, int type, uintptr_t handle, MonoObject *obj);
void gchandle_profiler_shutdown (MonoProfiler *prof);

// Helper functions needed by the profiler
void accumulate_gchandles_by_type (gpointer data, gpointer user_data);
void add_hashtable_keys_to_ptr_array (gpointer key, gpointer value, gpointer user_data);
const char *class_name_from_gchandle (gint32 gchandle);
void dump_jitted_methods (gpointer key, gpointer value, gpointer user_data);
gint gchandle_instances_comparer (gconstpointer base1, gconstpointer base2, gpointer user_data);
char *get_stack_trace ();
gboolean stack_walk_fn (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data);


