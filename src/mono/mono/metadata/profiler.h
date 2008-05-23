#ifndef __MONO_PROFILER_H__
#define __MONO_PROFILER_H__

#include <mono/metadata/object.h>
#include <mono/metadata/appdomain.h>

G_BEGIN_DECLS

typedef enum {
	MONO_PROFILE_NONE = 0,
	MONO_PROFILE_APPDOMAIN_EVENTS = 1 << 0,
	MONO_PROFILE_ASSEMBLY_EVENTS  = 1 << 1,
	MONO_PROFILE_MODULE_EVENTS    = 1 << 2,
	MONO_PROFILE_CLASS_EVENTS     = 1 << 3,
	MONO_PROFILE_JIT_COMPILATION  = 1 << 4,
	MONO_PROFILE_INLINING         = 1 << 5,
	MONO_PROFILE_EXCEPTIONS       = 1 << 6,
	MONO_PROFILE_ALLOCATIONS      = 1 << 7,
	MONO_PROFILE_GC               = 1 << 8,
	MONO_PROFILE_THREADS          = 1 << 9,
	MONO_PROFILE_REMOTING         = 1 << 10,
	MONO_PROFILE_TRANSITIONS      = 1 << 11,
	MONO_PROFILE_ENTER_LEAVE      = 1 << 12,
	MONO_PROFILE_COVERAGE         = 1 << 13,
	MONO_PROFILE_INS_COVERAGE     = 1 << 14,
	MONO_PROFILE_STATISTICAL      = 1 << 15,
	MONO_PROFILE_METHOD_EVENTS    = 1 << 16
} MonoProfileFlags;

typedef enum {
	MONO_PROFILE_OK,
	MONO_PROFILE_FAILED
} MonoProfileResult;

typedef enum {
	MONO_GC_EVENT_START,
	MONO_GC_EVENT_MARK_START,
	MONO_GC_EVENT_MARK_END,
	MONO_GC_EVENT_RECLAIM_START,
	MONO_GC_EVENT_RECLAIM_END,
	MONO_GC_EVENT_END,
	MONO_GC_EVENT_PRE_STOP_WORLD,
	MONO_GC_EVENT_POST_STOP_WORLD,
	MONO_GC_EVENT_PRE_START_WORLD,
	MONO_GC_EVENT_POST_START_WORLD
} MonoGCEvent;

/* coverage info */
typedef struct {
	MonoMethod *method;
	int iloffset;
	int counter;
	const char *filename;
	int line;
	int col;
} MonoProfileCoverageEntry;

typedef struct _MonoProfiler MonoProfiler;


/*
 * Functions that the runtime will call on the profiler.
 */

typedef void (*MonoProfileFunc) (MonoProfiler *prof);

typedef void (*MonoProfileAppDomainFunc) (MonoProfiler *prof, MonoDomain   *domain);
typedef void (*MonoProfileMethodFunc)   (MonoProfiler *prof, MonoMethod   *method);
typedef void (*MonoProfileClassFunc)    (MonoProfiler *prof, MonoClass    *klass);
typedef void (*MonoProfileModuleFunc)   (MonoProfiler *prof, MonoImage    *module);
typedef void (*MonoProfileAssemblyFunc) (MonoProfiler *prof, MonoAssembly *assembly);

typedef void (*MonoProfileExceptionFunc) (MonoProfiler *prof, MonoObject *object);
typedef void (*MonoProfileExceptionClauseFunc) (MonoProfiler *prof, MonoMethod *method, int clause_type, int clause_num);

typedef void (*MonoProfileAppDomainResult)(MonoProfiler *prof, MonoDomain   *domain,   int result);
typedef void (*MonoProfileMethodResult)   (MonoProfiler *prof, MonoMethod   *method,   int result);
typedef void (*MonoProfileJitResult)      (MonoProfiler *prof, MonoMethod   *method,   MonoJitInfo* jinfo,   int result);
typedef void (*MonoProfileClassResult)    (MonoProfiler *prof, MonoClass    *klass,    int result);
typedef void (*MonoProfileModuleResult)   (MonoProfiler *prof, MonoImage    *module,   int result);
typedef void (*MonoProfileAssemblyResult) (MonoProfiler *prof, MonoAssembly *assembly, int result);

typedef void (*MonoProfileMethodInline)   (MonoProfiler *prof, MonoMethod   *parent, MonoMethod *child, int *ok);

typedef void (*MonoProfileThreadFunc)     (MonoProfiler *prof, gsize tid);
typedef void (*MonoProfileAllocFunc)      (MonoProfiler *prof, MonoObject *obj, MonoClass *klass);
typedef void (*MonoProfileStatFunc)       (MonoProfiler *prof, guchar *ip, void *context);
typedef void (*MonoProfileStatCallChainFunc) (MonoProfiler *prof, int call_chain_depth, guchar **ip, void *context);
typedef void (*MonoProfileGCFunc)         (MonoProfiler *prof, MonoGCEvent event, int generation);
typedef void (*MonoProfileGCResizeFunc)   (MonoProfiler *prof, gint64 new_size);

typedef gboolean (*MonoProfileCoverageFilterFunc)   (MonoProfiler *prof, MonoMethod *method);

typedef void (*MonoProfileCoverageFunc)   (MonoProfiler *prof, const MonoProfileCoverageEntry *entry);

/*
 * Function the profiler may call.
 */
void mono_profiler_install       (MonoProfiler *prof, MonoProfileFunc shutdown_callback);
void mono_profiler_set_events    (MonoProfileFlags events);

MonoProfileFlags mono_profiler_get_events (void);

void mono_profiler_install_appdomain   (MonoProfileAppDomainFunc start_load, MonoProfileAppDomainResult end_load,
                                        MonoProfileAppDomainFunc start_unload, MonoProfileAppDomainFunc end_unload);
void mono_profiler_install_assembly    (MonoProfileAssemblyFunc start_load, MonoProfileAssemblyResult end_load,
                                        MonoProfileAssemblyFunc start_unload, MonoProfileAssemblyFunc end_unload);
void mono_profiler_install_module      (MonoProfileModuleFunc start_load, MonoProfileModuleResult end_load,
                                        MonoProfileModuleFunc start_unload, MonoProfileModuleFunc end_unload);
void mono_profiler_install_class       (MonoProfileClassFunc start_load, MonoProfileClassResult end_load,
                                        MonoProfileClassFunc start_unload, MonoProfileClassFunc end_unload);

void mono_profiler_install_jit_compile (MonoProfileMethodFunc start, MonoProfileMethodResult end);
void mono_profiler_install_jit_end (MonoProfileJitResult end);
void mono_profiler_install_method_free (MonoProfileMethodFunc callback);
void mono_profiler_install_enter_leave (MonoProfileMethodFunc enter, MonoProfileMethodFunc fleave);
void mono_profiler_install_thread      (MonoProfileThreadFunc start, MonoProfileThreadFunc end);
void mono_profiler_install_transition  (MonoProfileMethodResult callback);
void mono_profiler_install_allocation  (MonoProfileAllocFunc callback);
void mono_profiler_install_statistical (MonoProfileStatFunc callback);
void mono_profiler_install_statistical_call_chain (MonoProfileStatCallChainFunc callback, int call_chain_depth);
void mono_profiler_install_exception   (MonoProfileExceptionFunc throw_callback, MonoProfileMethodFunc exc_method_leave, MonoProfileExceptionClauseFunc clause_callback);
void mono_profiler_install_coverage_filter (MonoProfileCoverageFilterFunc callback);
void mono_profiler_coverage_get  (MonoProfiler *prof, MonoMethod *method, MonoProfileCoverageFunc func);
void mono_profiler_install_gc    (MonoProfileGCFunc callback, MonoProfileGCResizeFunc heap_resize_callback);

void mono_profiler_load             (const char *desc);

G_END_DECLS

#endif /* __MONO_PROFILER_H__ */

