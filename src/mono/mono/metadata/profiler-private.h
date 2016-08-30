
#ifndef __MONO_PROFILER_PRIVATE_H__
#define __MONO_PROFILER_PRIVATE_H__

#include <mono/metadata/profiler.h>
#include "mono/utils/mono-compiler.h"
#include <glib.h>

extern MonoProfileFlags mono_profiler_events;

enum {
	MONO_PROFILE_START_LOAD,
	MONO_PROFILE_END_LOAD,
	MONO_PROFILE_START_UNLOAD,
	MONO_PROFILE_END_UNLOAD
};

typedef struct {
	int entries;
	struct {
                guchar* cil_code;
                int count;
        } data [1];
} MonoProfileCoverageInfo;

void mono_profiler_shutdown        (void);

void mono_profiler_method_enter    (MonoMethod *method);
void mono_profiler_method_leave    (MonoMethod *method);
void mono_profiler_method_jit      (MonoMethod *method);
void mono_profiler_method_end_jit  (MonoMethod *method, MonoJitInfo* jinfo, int result);
void mono_profiler_method_free     (MonoMethod *method);
void mono_profiler_method_start_invoke (MonoMethod *method);
void mono_profiler_method_end_invoke   (MonoMethod *method);

void mono_profiler_code_transition (MonoMethod *method, int result);
void mono_profiler_allocation      (MonoObject *obj);
void mono_profiler_monitor_event   (MonoObject *obj, MonoProfilerMonitorEvent event);
void mono_profiler_stat_hit        (guchar *ip, void *context);
void mono_profiler_stat_call_chain (int call_chain_depth, guchar **ips, void *context);
int  mono_profiler_stat_get_call_chain_depth (void);
MonoProfilerCallChainStrategy  mono_profiler_stat_get_call_chain_strategy (void);
void mono_profiler_thread_start    (gsize tid);
void mono_profiler_thread_end      (gsize tid);
void mono_profiler_thread_name     (gsize tid, const char *name);

void mono_profiler_exception_thrown         (MonoObject *exception);
void mono_profiler_exception_method_leave   (MonoMethod *method);
void mono_profiler_exception_clause_handler (MonoMethod *method, int clause_type, int clause_num);

void mono_profiler_assembly_event  (MonoAssembly *assembly, int code);
void mono_profiler_assembly_loaded (MonoAssembly *assembly, int result);

void mono_profiler_module_event  (MonoImage *image, int code);
void mono_profiler_module_loaded (MonoImage *image, int result);

void mono_profiler_class_event  (MonoClass *klass, int code);
void mono_profiler_class_loaded (MonoClass *klass, int result);

void mono_profiler_appdomain_event  (MonoDomain *domain, int code);
void mono_profiler_appdomain_loaded (MonoDomain *domain, int result);
void mono_profiler_appdomain_name   (MonoDomain *domain, const char *name);

void mono_profiler_context_loaded (MonoAppContext *context);
void mono_profiler_context_unloaded (MonoAppContext *context);

void mono_profiler_iomap (char *report, const char *pathname, const char *new_pathname);

MonoProfileCoverageInfo* mono_profiler_coverage_alloc (MonoMethod *method, int entries);
void                     mono_profiler_coverage_free  (MonoMethod *method);

void mono_profiler_gc_event       (MonoGCEvent e, int generation);
void mono_profiler_gc_heap_resize (gint64 new_size);
void mono_profiler_gc_moves       (void **objects, int num);
void mono_profiler_gc_handle      (int op, int type, uintptr_t handle, MonoObject *obj);
void mono_profiler_gc_roots       (int num, void **objects, int *root_types, uintptr_t *extra_info);

void mono_profiler_gc_finalize_begin (void);
void mono_profiler_gc_finalize_object_begin (MonoObject *obj);
void mono_profiler_gc_finalize_object_end (MonoObject *obj);
void mono_profiler_gc_finalize_end (void);

void mono_profiler_code_chunk_new (gpointer chunk, int size);
void mono_profiler_code_chunk_destroy (gpointer chunk);
void mono_profiler_code_buffer_new (gpointer buffer, int size, MonoProfilerCodeBufferType type, gconstpointer data);

void mono_profiler_runtime_initialized (void);

int64_t mono_profiler_get_sampling_rate (void);
MonoProfileSamplingMode mono_profiler_get_sampling_mode (void);

#endif /* __MONO_PROFILER_PRIVATE_H__ */

