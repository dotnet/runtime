
#ifndef __MONO_PROFILER_PRIVATE_H__
#define __MONO_PROFILER_PRIVATE_H__

#include <mono/metadata/profiler.h>

extern MonoProfileFlags mono_profiler_events;

enum {
	MONO_PROFILE_START_LOAD,
	MONO_PROFILE_END_LOAD,
	MONO_PROFILE_START_UNLOAD,
	MONO_PROFILE_END_UNLOAD
};

void mono_profiler_shutdown        (void);

void mono_profiler_method_enter    (MonoMethod *method);
void mono_profiler_method_leave    (MonoMethod *method);
void mono_profiler_method_jit      (MonoMethod *method);
void mono_profiler_method_end_jit  (MonoMethod *method, int result);

void mono_profiler_code_transition (MonoMethod *method, int result);
void mono_profiler_thread_start    (guint32 tid);
void mono_profiler_thread_end      (guint32 tid);

void mono_profiler_assembly_event  (MonoAssembly *assembly, int code);
void mono_profiler_assembly_loaded (MonoAssembly *assembly, int result);

void mono_profiler_module_event  (MonoImage *image, int code);
void mono_profiler_module_loaded (MonoImage *image, int result);

void mono_profiler_class_event  (MonoClass *klass, int code);
void mono_profiler_class_loaded (MonoClass *klass, int result);

void mono_profiler_appdomain_event  (MonoDomain *domain, int code);
void mono_profiler_appdomain_loaded (MonoDomain *domain, int result);

#endif /* __MONO_PROFILER_PRIVATE_H__ */

