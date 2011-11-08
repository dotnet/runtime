/*
 * profiler.c: Profiler interface for Mono
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com).
 */

#include "config.h"
#include "mono/metadata/profiler-private.h"
#include "mono/metadata/assembly.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/mono-debug.h"
#include "mono/metadata/debug-mono-symfile.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internal.h"
#include "mono/io-layer/io-layer.h"
#include "mono/utils/mono-dl.h"
#include <string.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_BACKTRACE_SYMBOLS
#include <execinfo.h>
#endif

typedef struct _ProfilerDesc ProfilerDesc;
struct _ProfilerDesc {
	ProfilerDesc *next;
	MonoProfiler *profiler;
	MonoProfileFlags events;

	MonoProfileAppDomainFunc   domain_start_load;
	MonoProfileAppDomainResult domain_end_load;
	MonoProfileAppDomainFunc   domain_start_unload;
	MonoProfileAppDomainFunc   domain_end_unload;

	MonoProfileAssemblyFunc   assembly_start_load;
	MonoProfileAssemblyResult assembly_end_load;
	MonoProfileAssemblyFunc   assembly_start_unload;
	MonoProfileAssemblyFunc   assembly_end_unload;

	MonoProfileModuleFunc   module_start_load;
	MonoProfileModuleResult module_end_load;
	MonoProfileModuleFunc   module_start_unload;
	MonoProfileModuleFunc   module_end_unload;

	MonoProfileClassFunc   class_start_load;
	MonoProfileClassResult class_end_load;
	MonoProfileClassFunc   class_start_unload;
	MonoProfileClassFunc   class_end_unload;

	MonoProfileMethodFunc   jit_start;
	MonoProfileMethodResult jit_end;
	MonoProfileJitResult    jit_end2;
	MonoProfileMethodFunc   method_free;
	MonoProfileMethodFunc   method_start_invoke;
	MonoProfileMethodFunc   method_end_invoke;
	MonoProfileMethodResult man_unman_transition;
	MonoProfileAllocFunc    allocation_cb;
	MonoProfileMonitorFunc  monitor_event_cb;
	MonoProfileStatFunc     statistical_cb;
	MonoProfileStatCallChainFunc statistical_call_chain_cb;
	int                     statistical_call_chain_depth;
	MonoProfilerCallChainStrategy  statistical_call_chain_strategy;
	MonoProfileMethodFunc   method_enter;
	MonoProfileMethodFunc   method_leave;

	MonoProfileExceptionFunc	exception_throw_cb;
	MonoProfileMethodFunc exception_method_leave_cb;
	MonoProfileExceptionClauseFunc exception_clause_cb;

	MonoProfileIomapFunc iomap_cb;

	MonoProfileThreadFunc   thread_start;
	MonoProfileThreadFunc   thread_end;
	MonoProfileThreadNameFunc   thread_name;

	MonoProfileCoverageFilterFunc coverage_filter_cb;

	MonoProfileFunc shutdown_callback;

	MonoProfileGCFunc        gc_event;
	MonoProfileGCResizeFunc  gc_heap_resize;
	MonoProfileGCMoveFunc    gc_moves;
	MonoProfileGCHandleFunc  gc_handle;
	MonoProfileGCRootFunc    gc_roots;

	MonoProfileFunc          runtime_initialized_event;

	MonoProfilerCodeChunkNew code_chunk_new;
	MonoProfilerCodeChunkDestroy code_chunk_destroy;
	MonoProfilerCodeBufferNew code_buffer_new;
};

static ProfilerDesc *prof_list = NULL;

#define mono_profiler_coverage_lock() EnterCriticalSection (&profiler_coverage_mutex)
#define mono_profiler_coverage_unlock() LeaveCriticalSection (&profiler_coverage_mutex)
static CRITICAL_SECTION profiler_coverage_mutex;

/* this is directly accessible to other mono libs.
 * It is the ORed value of all the profiler's events.
 */
MonoProfileFlags mono_profiler_events;

/**
 * mono_profiler_install:
 * @prof: a MonoProfiler structure pointer, or a pointer to a derived structure.
 * @callback: the function to invoke at shutdown
 *
 * Use mono_profiler_install to activate profiling in the Mono runtime.
 * Typically developers of new profilers will create a new structure whose
 * first field is a MonoProfiler and put any extra information that they need
 * to access from the various profiling callbacks there.
 *
 */
void
mono_profiler_install (MonoProfiler *prof, MonoProfileFunc callback)
{
	ProfilerDesc *desc = g_new0 (ProfilerDesc, 1);
	if (!prof_list)
		InitializeCriticalSection (&profiler_coverage_mutex);
	desc->profiler = prof;
	desc->shutdown_callback = callback;
	desc->next = prof_list;
	prof_list = desc;
}

/**
 * mono_profiler_set_events:
 * @events: an ORed set of values made up of MONO_PROFILER_ flags
 *
 * The events descriped in the @events argument is a set of flags
 * that represent which profiling events must be triggered.  For
 * example if you have registered a set of methods for tracking
 * JIT compilation start and end with mono_profiler_install_jit_compile,
 * you will want to pass the MONO_PROFILE_JIT_COMPILATION flag to
 * this routine.
 *
 * You can call mono_profile_set_events more than once and you can
 * do this at runtime to modify which methods are invoked.
 */
void
mono_profiler_set_events (MonoProfileFlags events)
{
	ProfilerDesc *prof;
	MonoProfileFlags value = 0;
	if (prof_list)
		prof_list->events = events;
	for (prof = prof_list; prof; prof = prof->next)
		value |= prof->events;
	mono_profiler_events = value;
}

/**
 * mono_profiler_get_events:
 *
 * Returns a list of active events that will be intercepted. 
 */
MonoProfileFlags
mono_profiler_get_events (void)
{
	return mono_profiler_events;
}

/**
 * mono_profiler_install_enter_leave:
 * @enter: the routine to be called on each method entry
 * @fleave: the routine to be called each time a method returns
 *
 * Use this routine to install routines that will be called everytime
 * a method enters and leaves.   The routines will receive as an argument
 * the MonoMethod representing the method that is entering or leaving.
 */
void
mono_profiler_install_enter_leave (MonoProfileMethodFunc enter, MonoProfileMethodFunc fleave)
{
	if (!prof_list)
		return;
	prof_list->method_enter = enter;
	prof_list->method_leave = fleave;
}

/**
 * mono_profiler_install_jit_compile:
 * @start: the routine to be called when the JIT process starts.
 * @end: the routine to be called when the JIT process ends.
 *
 * Use this routine to install routines that will be called when JIT 
 * compilation of a method starts and completes.
 */
void 
mono_profiler_install_jit_compile (MonoProfileMethodFunc start, MonoProfileMethodResult end)
{
	if (!prof_list)
		return;
	prof_list->jit_start = start;
	prof_list->jit_end = end;
}

void 
mono_profiler_install_jit_end (MonoProfileJitResult end)
{
	if (!prof_list)
		return;
	prof_list->jit_end2 = end;
}

void 
mono_profiler_install_method_free (MonoProfileMethodFunc callback)
{
	if (!prof_list)
		return;
	prof_list->method_free = callback;
}

void
mono_profiler_install_method_invoke (MonoProfileMethodFunc start, MonoProfileMethodFunc end)
{
	if (!prof_list)
		return;
	prof_list->method_start_invoke = start;
	prof_list->method_end_invoke = end;
}

void 
mono_profiler_install_thread (MonoProfileThreadFunc start, MonoProfileThreadFunc end)
{
	if (!prof_list)
		return;
	prof_list->thread_start = start;
	prof_list->thread_end = end;
}

void 
mono_profiler_install_thread_name (MonoProfileThreadNameFunc thread_name_cb)
{
	if (!prof_list)
		return;
	prof_list->thread_name = thread_name_cb;
}

void 
mono_profiler_install_transition (MonoProfileMethodResult callback)
{
	if (!prof_list)
		return;
	prof_list->man_unman_transition = callback;
}

void 
mono_profiler_install_allocation (MonoProfileAllocFunc callback)
{
	if (!prof_list)
		return;
	prof_list->allocation_cb = callback;
}

void
mono_profiler_install_monitor  (MonoProfileMonitorFunc callback)
{
	if (!prof_list)
		return;
	prof_list->monitor_event_cb = callback;
}

void 
mono_profiler_install_statistical (MonoProfileStatFunc callback)
{
	if (!prof_list)
		return;
	prof_list->statistical_cb = callback;
}

void 
mono_profiler_install_statistical_call_chain (MonoProfileStatCallChainFunc callback, int call_chain_depth, MonoProfilerCallChainStrategy call_chain_strategy) {
	if (!prof_list)
		return;
	if (call_chain_depth > MONO_PROFILER_MAX_STAT_CALL_CHAIN_DEPTH) {
		call_chain_depth = MONO_PROFILER_MAX_STAT_CALL_CHAIN_DEPTH;
	}
	if ((call_chain_strategy >= MONO_PROFILER_CALL_CHAIN_INVALID) || (call_chain_strategy < MONO_PROFILER_CALL_CHAIN_NONE)) {
		call_chain_strategy = MONO_PROFILER_CALL_CHAIN_NONE;
	}
	prof_list->statistical_call_chain_cb = callback;
	prof_list->statistical_call_chain_depth = call_chain_depth;
	prof_list->statistical_call_chain_strategy = call_chain_strategy;
}

int
mono_profiler_stat_get_call_chain_depth (void) {
	if (prof_list && prof_list->statistical_call_chain_cb != NULL) {
		return prof_list->statistical_call_chain_depth;
	} else {
		return 0;
	}
}

MonoProfilerCallChainStrategy
mono_profiler_stat_get_call_chain_strategy (void) {
	if (prof_list && prof_list->statistical_call_chain_cb != NULL) {
		return prof_list->statistical_call_chain_strategy;
	} else {
		return MONO_PROFILER_CALL_CHAIN_NONE;
	}
}

void mono_profiler_install_exception (MonoProfileExceptionFunc throw_callback, MonoProfileMethodFunc exc_method_leave, MonoProfileExceptionClauseFunc clause_callback)
{
	if (!prof_list)
		return;
	prof_list->exception_throw_cb = throw_callback;
	prof_list->exception_method_leave_cb = exc_method_leave;
	prof_list->exception_clause_cb = clause_callback;
}

void 
mono_profiler_install_coverage_filter (MonoProfileCoverageFilterFunc callback)
{
	if (!prof_list)
		return;
	prof_list->coverage_filter_cb = callback;
}

void 
mono_profiler_install_appdomain   (MonoProfileAppDomainFunc start_load, MonoProfileAppDomainResult end_load,
                                   MonoProfileAppDomainFunc start_unload, MonoProfileAppDomainFunc end_unload)

{
	if (!prof_list)
		return;
	prof_list->domain_start_load = start_load;
	prof_list->domain_end_load = end_load;
	prof_list->domain_start_unload = start_unload;
	prof_list->domain_end_unload = end_unload;
}

void 
mono_profiler_install_assembly    (MonoProfileAssemblyFunc start_load, MonoProfileAssemblyResult end_load,
                                   MonoProfileAssemblyFunc start_unload, MonoProfileAssemblyFunc end_unload)
{
	if (!prof_list)
		return;
	prof_list->assembly_start_load = start_load;
	prof_list->assembly_end_load = end_load;
	prof_list->assembly_start_unload = start_unload;
	prof_list->assembly_end_unload = end_unload;
}

void 
mono_profiler_install_module      (MonoProfileModuleFunc start_load, MonoProfileModuleResult end_load,
                                   MonoProfileModuleFunc start_unload, MonoProfileModuleFunc end_unload)
{
	if (!prof_list)
		return;
	prof_list->module_start_load = start_load;
	prof_list->module_end_load = end_load;
	prof_list->module_start_unload = start_unload;
	prof_list->module_end_unload = end_unload;
}

void
mono_profiler_install_class       (MonoProfileClassFunc start_load, MonoProfileClassResult end_load,
                                   MonoProfileClassFunc start_unload, MonoProfileClassFunc end_unload)
{
	if (!prof_list)
		return;
	prof_list->class_start_load = start_load;
	prof_list->class_end_load = end_load;
	prof_list->class_start_unload = start_unload;
	prof_list->class_end_unload = end_unload;
}

void
mono_profiler_method_enter (MonoMethod *method)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_ENTER_LEAVE) && prof->method_enter)
			prof->method_enter (prof->profiler, method);
	}
}

void
mono_profiler_method_leave (MonoMethod *method)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_ENTER_LEAVE) && prof->method_leave)
			prof->method_leave (prof->profiler, method);
	}
}

void 
mono_profiler_method_jit (MonoMethod *method)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_JIT_COMPILATION) && prof->jit_start)
			prof->jit_start (prof->profiler, method);
	}
}

void 
mono_profiler_method_end_jit (MonoMethod *method, MonoJitInfo* jinfo, int result)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_JIT_COMPILATION)) {
			if (prof->jit_end)
				prof->jit_end (prof->profiler, method, result);
			if (prof->jit_end2)
				prof->jit_end2 (prof->profiler, method, jinfo, result);
		}
	}
}

void 
mono_profiler_method_free (MonoMethod *method)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_METHOD_EVENTS) && prof->method_free)
			prof->method_free (prof->profiler, method);
	}
}

void
mono_profiler_method_start_invoke (MonoMethod *method)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_METHOD_EVENTS) && prof->method_start_invoke)
			prof->method_start_invoke (prof->profiler, method);
	}
}

void
mono_profiler_method_end_invoke (MonoMethod *method)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_METHOD_EVENTS) && prof->method_end_invoke)
			prof->method_end_invoke (prof->profiler, method);
	}
}

void 
mono_profiler_code_transition (MonoMethod *method, int result)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_TRANSITIONS) && prof->man_unman_transition)
			prof->man_unman_transition (prof->profiler, method, result);
	}
}

void 
mono_profiler_allocation (MonoObject *obj, MonoClass *klass)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_ALLOCATIONS) && prof->allocation_cb)
			prof->allocation_cb (prof->profiler, obj, klass);
	}
}

void
mono_profiler_monitor_event      (MonoObject *obj, MonoProfilerMonitorEvent event) {
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_MONITOR_EVENTS) && prof->monitor_event_cb)
			prof->monitor_event_cb (prof->profiler, obj, event);
	}
}

void
mono_profiler_stat_hit (guchar *ip, void *context)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_STATISTICAL) && prof->statistical_cb)
			prof->statistical_cb (prof->profiler, ip, context);
	}
}

void
mono_profiler_stat_call_chain (int call_chain_depth, guchar **ips, void *context)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_STATISTICAL) && prof->statistical_call_chain_cb)
			prof->statistical_call_chain_cb (prof->profiler, call_chain_depth, ips, context);
	}
}

void
mono_profiler_exception_thrown (MonoObject *exception)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_EXCEPTIONS) && prof->exception_throw_cb)
			prof->exception_throw_cb (prof->profiler, exception);
	}
}

void
mono_profiler_exception_method_leave (MonoMethod *method)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_EXCEPTIONS) && prof->exception_method_leave_cb)
			prof->exception_method_leave_cb (prof->profiler, method);
	}
}

void
mono_profiler_exception_clause_handler (MonoMethod *method, int clause_type, int clause_num)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_EXCEPTIONS) && prof->exception_clause_cb)
			prof->exception_clause_cb (prof->profiler, method, clause_type, clause_num);
	}
}

void
mono_profiler_thread_start (gsize tid)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_THREADS) && prof->thread_start)
			prof->thread_start (prof->profiler, tid);
	}
}

void 
mono_profiler_thread_end (gsize tid)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_THREADS) && prof->thread_end)
			prof->thread_end (prof->profiler, tid);
	}
}

void
mono_profiler_thread_name (gsize tid, const char *name)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_THREADS) && prof->thread_name)
			prof->thread_name (prof->profiler, tid, name);
	}
}

void 
mono_profiler_assembly_event  (MonoAssembly *assembly, int code)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (!(prof->events & MONO_PROFILE_ASSEMBLY_EVENTS))
			continue;

		switch (code) {
		case MONO_PROFILE_START_LOAD:
			if (prof->assembly_start_load)
				prof->assembly_start_load (prof->profiler, assembly);
			break;
		case MONO_PROFILE_START_UNLOAD:
			if (prof->assembly_start_unload)
				prof->assembly_start_unload (prof->profiler, assembly);
			break;
		case MONO_PROFILE_END_UNLOAD:
			if (prof->assembly_end_unload)
				prof->assembly_end_unload (prof->profiler, assembly);
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

void 
mono_profiler_assembly_loaded (MonoAssembly *assembly, int result)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_ASSEMBLY_EVENTS) && prof->assembly_end_load)
			prof->assembly_end_load (prof->profiler, assembly, result);
	}
}

void mono_profiler_iomap (char *report, const char *pathname, const char *new_pathname)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_IOMAP_EVENTS) && prof->iomap_cb)
			prof->iomap_cb (prof->profiler, report, pathname, new_pathname);
	}
}

void 
mono_profiler_module_event  (MonoImage *module, int code)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (!(prof->events & MONO_PROFILE_MODULE_EVENTS))
			continue;

		switch (code) {
		case MONO_PROFILE_START_LOAD:
			if (prof->module_start_load)
				prof->module_start_load (prof->profiler, module);
			break;
		case MONO_PROFILE_START_UNLOAD:
			if (prof->module_start_unload)
				prof->module_start_unload (prof->profiler, module);
			break;
		case MONO_PROFILE_END_UNLOAD:
			if (prof->module_end_unload)
				prof->module_end_unload (prof->profiler, module);
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

void 
mono_profiler_module_loaded (MonoImage *module, int result)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_MODULE_EVENTS) && prof->module_end_load)
			prof->module_end_load (prof->profiler, module, result);
	}
}

void 
mono_profiler_class_event  (MonoClass *klass, int code)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (!(prof->events & MONO_PROFILE_CLASS_EVENTS))
			continue;

		switch (code) {
		case MONO_PROFILE_START_LOAD:
			if (prof->class_start_load)
				prof->class_start_load (prof->profiler, klass);
			break;
		case MONO_PROFILE_START_UNLOAD:
			if (prof->class_start_unload)
				prof->class_start_unload (prof->profiler, klass);
			break;
		case MONO_PROFILE_END_UNLOAD:
			if (prof->class_end_unload)
				prof->class_end_unload (prof->profiler, klass);
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

void 
mono_profiler_class_loaded (MonoClass *klass, int result)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_CLASS_EVENTS) && prof->class_end_load)
			prof->class_end_load (prof->profiler, klass, result);
	}
}

void 
mono_profiler_appdomain_event  (MonoDomain *domain, int code)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (!(prof->events & MONO_PROFILE_APPDOMAIN_EVENTS))
			continue;

		switch (code) {
		case MONO_PROFILE_START_LOAD:
			if (prof->domain_start_load)
				prof->domain_start_load (prof->profiler, domain);
			break;
		case MONO_PROFILE_START_UNLOAD:
			if (prof->domain_start_unload)
				prof->domain_start_unload (prof->profiler, domain);
			break;
		case MONO_PROFILE_END_UNLOAD:
			if (prof->domain_end_unload)
				prof->domain_end_unload (prof->profiler, domain);
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

void 
mono_profiler_appdomain_loaded (MonoDomain *domain, int result)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_APPDOMAIN_EVENTS) && prof->domain_end_load)
			prof->domain_end_load (prof->profiler, domain, result);
	}
}

void 
mono_profiler_shutdown (void)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (prof->shutdown_callback)
			prof->shutdown_callback (prof->profiler);
	}

	mono_profiler_set_events (0);
}

void
mono_profiler_gc_heap_resize (gint64 new_size)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_GC) && prof->gc_heap_resize)
			prof->gc_heap_resize (prof->profiler, new_size);
	}
}

void
mono_profiler_gc_event (MonoGCEvent event, int generation)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_GC) && prof->gc_event)
			prof->gc_event (prof->profiler, event, generation);
	}
}

void
mono_profiler_gc_moves (void **objects, int num)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_GC_MOVES) && prof->gc_moves)
			prof->gc_moves (prof->profiler, objects, num);
	}
}

void
mono_profiler_gc_handle (int op, int type, uintptr_t handle, MonoObject *obj)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_GC_ROOTS) && prof->gc_handle)
			prof->gc_handle (prof->profiler, op, type, handle, obj);
	}
}

void
mono_profiler_gc_roots (int num, void **objects, int *root_types, uintptr_t *extra_info)
{
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if ((prof->events & MONO_PROFILE_GC_ROOTS) && prof->gc_roots)
			prof->gc_roots (prof->profiler, num, objects, root_types, extra_info);
	}
}

void
mono_profiler_install_gc (MonoProfileGCFunc callback, MonoProfileGCResizeFunc heap_resize_callback)
{
	mono_gc_enable_events ();
	if (!prof_list)
		return;
	prof_list->gc_event = callback;
	prof_list->gc_heap_resize = heap_resize_callback;
}

/**
 * mono_profiler_install_gc_moves:
 * @callback: callback function
 *
 * Install the @callback function that the GC will call when moving objects.
 * The callback receives an array of pointers and the number of elements
 * in the array. Every even element in the array is the original object location
 * and the following odd element is the new location of the object in memory.
 * So the number of elements argument will always be a multiple of 2.
 * Since this callback happens during the GC, it is a restricted environment:
 * no locks can be taken and the object pointers can be inspected only once
 * the GC is finished (of course the original location pointers will not
 * point to valid objects anymore).
 */
void
mono_profiler_install_gc_moves (MonoProfileGCMoveFunc callback)
{
	if (!prof_list)
		return;
	prof_list->gc_moves = callback;
}

/**
 * mono_profiler_install_gc_roots:
 * @handle_callback: callback function
 * @roots_callback: callback function
 *
 * Install the @handle_callback function that the GC will call when GC
 * handles are created or destroyed.
 * The callback receives an operation, which is either #MONO_PROFILER_GC_HANDLE_CREATED
 * or #MONO_PROFILER_GC_HANDLE_DESTROYED, the handle type, the handle value and the
 * object pointer, if present.
 * Install the @roots_callback function that the GC will call when tracing
 * the roots for a collection.
 * The callback receives the number of elements and three arrays: an array
 * of objects, an array of root types and flags and an array of extra info.
 * The size of each array is given by the first argument.
 */
void
mono_profiler_install_gc_roots (MonoProfileGCHandleFunc handle_callback, MonoProfileGCRootFunc roots_callback)
{
	if (!prof_list)
		return;
	prof_list->gc_handle = handle_callback;
	prof_list->gc_roots = roots_callback;
}

void
mono_profiler_install_runtime_initialized (MonoProfileFunc runtime_initialized_callback)
{
	if (!prof_list)
		return;
	prof_list->runtime_initialized_event = runtime_initialized_callback;
}

void
mono_profiler_runtime_initialized (void) {
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (prof->runtime_initialized_event)
			prof->runtime_initialized_event (prof->profiler);
	}
}

void
mono_profiler_install_code_chunk_new (MonoProfilerCodeChunkNew callback) {
	if (!prof_list)
		return;
	prof_list->code_chunk_new = callback;
}
void
mono_profiler_code_chunk_new (gpointer chunk, int size) {
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (prof->code_chunk_new)
			prof->code_chunk_new (prof->profiler, chunk, size);
	}
}

void
mono_profiler_install_code_chunk_destroy (MonoProfilerCodeChunkDestroy callback) {
	if (!prof_list)
		return;
	prof_list->code_chunk_destroy = callback;
}
void
mono_profiler_code_chunk_destroy (gpointer chunk) {
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (prof->code_chunk_destroy)
			prof->code_chunk_destroy (prof->profiler, chunk);
	}
}

void
mono_profiler_install_code_buffer_new (MonoProfilerCodeBufferNew callback) {
	if (!prof_list)
		return;
	prof_list->code_buffer_new = callback;
}

void
mono_profiler_install_iomap (MonoProfileIomapFunc callback)
{
	if (!prof_list)
		return;
	prof_list->iomap_cb = callback;
}

void
mono_profiler_code_buffer_new (gpointer buffer, int size, MonoProfilerCodeBufferType type, void *data) {
	ProfilerDesc *prof;
	for (prof = prof_list; prof; prof = prof->next) {
		if (prof->code_buffer_new)
			prof->code_buffer_new (prof->profiler, buffer, size, type, data);
	}
}

static GHashTable *coverage_hash = NULL;

MonoProfileCoverageInfo* 
mono_profiler_coverage_alloc (MonoMethod *method, int entries)
{
	MonoProfileCoverageInfo *res;
	int instrument = FALSE;
	ProfilerDesc *prof;

	for (prof = prof_list; prof; prof = prof->next) {
		/* note that we call the filter on all the profilers even if just
		 * a single one would be enough to instrument a method
		 */
		if (prof->coverage_filter_cb)
			if (prof->coverage_filter_cb (prof->profiler, method))
				instrument = TRUE;
	}
	if (!instrument)
		return NULL;

	mono_profiler_coverage_lock ();
	if (!coverage_hash)
		coverage_hash = g_hash_table_new (NULL, NULL);

	res = g_malloc0 (sizeof (MonoProfileCoverageInfo) + sizeof (void*) * 2 * entries);

	res->entries = entries;

	g_hash_table_insert (coverage_hash, method, res);
	mono_profiler_coverage_unlock ();

	return res;
}

/* safe only when the method antive code has been unloaded */
void
mono_profiler_coverage_free (MonoMethod *method)
{
	MonoProfileCoverageInfo* info;

	mono_profiler_coverage_lock ();
	if (!coverage_hash) {
		mono_profiler_coverage_unlock ();
		return;
	}

	info = g_hash_table_lookup (coverage_hash, method);
	if (info) {
		g_free (info);
		g_hash_table_remove (coverage_hash, method);
	}
	mono_profiler_coverage_unlock ();
}

/**
 * mono_profiler_coverage_get:
 * @prof: The profiler handle, installed with mono_profiler_install
 * @method: the method to gather information from.
 * @func: A routine that will be called back with the results
 *
 * If the MONO_PROFILER_INS_COVERAGE flag was active during JIT compilation
 * it is posisble to obtain coverage information about a give method.
 *
 * The function @func will be invoked repeatedly with instances of the
 * MonoProfileCoverageEntry structure.
 */
void 
mono_profiler_coverage_get (MonoProfiler *prof, MonoMethod *method, MonoProfileCoverageFunc func)
{
	MonoProfileCoverageInfo* info;
	int i, offset;
	guint32 code_size;
	const unsigned char *start, *end, *cil_code;
	MonoMethodHeader *header;
	MonoProfileCoverageEntry entry;
	MonoDebugMethodInfo *debug_minfo;

	mono_profiler_coverage_lock ();
	info = g_hash_table_lookup (coverage_hash, method);
	mono_profiler_coverage_unlock ();

	if (!info)
		return;

	header = mono_method_get_header (method);
	start = mono_method_header_get_code (header, &code_size, NULL);
	debug_minfo = mono_debug_lookup_method (method);

	end = start + code_size;
	for (i = 0; i < info->entries; ++i) {
		cil_code = info->data [i].cil_code;
		if (cil_code && cil_code >= start && cil_code < end) {
			char *fname = NULL;
			offset = cil_code - start;
			entry.iloffset = offset;
			entry.method = method;
			entry.counter = info->data [i].count;
			entry.line = entry.col = 1;
			entry.filename = NULL;
			if (debug_minfo) {
				MonoDebugSourceLocation *location;

				location = mono_debug_symfile_lookup_location (debug_minfo, offset);
				if (location) {
					entry.line = location->row;
					entry.col = location->column;
					entry.filename = fname = g_strdup (location->source_file);
					mono_debug_free_source_location (location);
				}
			}

			func (prof, &entry);
			g_free (fname);
		}
	}
	mono_metadata_free_mh (header);
}

typedef void (*ProfilerInitializer) (const char*);
#define INITIALIZER_NAME "mono_profiler_startup"


static gboolean
load_profiler (MonoDl *pmodule, const char *desc, const char *symbol)
{
	char *err;
	ProfilerInitializer func;

	if (!pmodule)
		return FALSE;

	if ((err = mono_dl_symbol (pmodule, symbol, (gpointer *) &func))) {
		g_free (err);
		return FALSE;
	} else {
		func (desc);
	}
	return TRUE;
}

static gboolean
load_embedded_profiler (const char *desc, const char *name)
{
	char *err = NULL;
	char *symbol;
	MonoDl *pmodule = NULL;
	gboolean result;

	pmodule = mono_dl_open (NULL, MONO_DL_LAZY, &err);
	if (!pmodule) {
		g_warning ("Could not open main executable (%s)", err);
		g_free (err);
		return FALSE;
	}

	symbol = g_strdup_printf (INITIALIZER_NAME "_%s", name);
	result = load_profiler (pmodule, desc, symbol);
	g_free (symbol);

	return result;
}

static gboolean
load_profiler_from_directory (const char *directory, const char *libname, const char *desc)
{
	MonoDl *pmodule = NULL;
	char* path;
	char *err;
	void *iter;

	iter = NULL;
	err = NULL;
	while ((path = mono_dl_build_path (directory, libname, &iter))) {
		pmodule = mono_dl_open (path, MONO_DL_LAZY, &err);
		g_free (path);
		g_free (err);
		if (pmodule)
			return load_profiler (pmodule, desc, INITIALIZER_NAME);
	}
		
	return FALSE;
}

/**
 * mono_profiler_load:
 * @desc: arguments to configure the profiler
 *
 * Invoke this method to initialize the profiler.   This will drive the
 * loading of the internal ("default") or any external profilers.
 *
 * This routine is invoked by Mono's driver, but must be called manually
 * if you embed Mono into your application.
 */
void 
mono_profiler_load (const char *desc)
{
	char *cdesc = NULL;
	mono_gc_base_init ();

	if (!desc || (strcmp ("default", desc) == 0)) {
		desc = "log:report";
	}
	/* we keep command-line compat with the old version here */
	if (strncmp (desc, "default:", 8) == 0) {
		gchar **args, **ptr;
		GString *str = g_string_new ("log:report");
		args = g_strsplit (desc + 8, ",", -1);
		for (ptr = args; ptr && *ptr; ptr++) {
			const char *arg = *ptr;

			if (!strcmp (arg, "time"))
				g_string_append (str, ",calls");
			else if (!strcmp (arg, "alloc"))
				g_string_append (str, ",alloc");
			else if (!strcmp (arg, "stat"))
				g_string_append (str, ",sample");
			else if (!strcmp (arg, "jit"))
				continue; /* accept and do nothing */
			else if (strncmp (arg, "file=", 5) == 0) {
				g_string_append_printf (str, ",output=%s", arg + 5);
			} else {
				fprintf (stderr, "profiler : Unknown argument '%s'.\n", arg);
				return;
			}
		}
		desc = cdesc = g_string_free (str, FALSE);
	}
	{
		const char* col = strchr (desc, ':');
		char* libname;
		char *mname;
		gboolean res = FALSE;

		if (col != NULL) {
			mname = g_memdup (desc, col - desc + 1);
			mname [col - desc] = 0;
		} else {
			mname = g_strdup (desc);
		}
		if (!load_embedded_profiler (desc, mname)) {
			libname = g_strdup_printf ("mono-profiler-%s", mname);
			if (!load_profiler_from_directory (NULL, libname, desc)) {
				res = FALSE;
#if defined (MONO_ASSEMBLIES)
				res = load_profiler_from_directory (mono_assembly_getrootdir (), libname, desc);
#endif
				if (!res)
					g_warning ("The '%s' profiler wasn't found in the main executable nor could it be loaded from '%s'.", mname, libname);
			}
			g_free (libname);
		}
		g_free (mname);
	}
	g_free (cdesc);
}

