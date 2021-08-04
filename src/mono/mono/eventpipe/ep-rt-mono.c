#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-rt-config.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-rt.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-event.h>

#include <eglib/gmodule.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-rand.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/profiler-private.h>
#include <mono/mini/mini-runtime.h>
#include <mono/sgen/sgen-conf.h>
#include <mono/sgen/sgen-tagged-pointer.h>
#include "mono/utils/mono-logger-internals.h"
#include <runtime_version.h>
#include <clretwallmain.h>

// EventPipe rt init state.
gboolean _ep_rt_mono_initialized;

// EventPipe TLS key.
MonoNativeTlsKey _ep_rt_mono_thread_holder_tls_id;

// Random byte provider.
gpointer _ep_rt_mono_rand_provider;

// EventPipe global config lock.
ep_rt_spin_lock_handle_t _ep_rt_mono_config_lock = {0};

// OS cmd line.
mono_lazy_init_t _ep_rt_mono_os_cmd_line_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
char *_ep_rt_mono_os_cmd_line = NULL;

// Managed cmd line.
mono_lazy_init_t _ep_rt_mono_managed_cmd_line_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
char *_ep_rt_mono_managed_cmd_line = NULL;


// Sample profiler.
static GArray * _ep_rt_mono_sampled_thread_callstacks = NULL;
static uint32_t _ep_rt_mono_max_sampled_thread_count = 32;

// Mono profilers.
static MonoProfilerHandle _ep_rt_default_profiler = NULL;
static MonoProfilerHandle _ep_rt_dotnet_runtime_profiler_provider = NULL;
static MonoProfilerHandle _ep_rt_dotnet_mono_profiler_provider = NULL;
static MonoCallSpec _ep_rt_dotnet_mono_profiler_provider_callspec = {0};

// Phantom JIT compile method.
MonoMethod *_ep_rt_mono_runtime_helper_compile_method = NULL;
MonoJitInfo *_ep_rt_mono_runtime_helper_compile_method_jitinfo = NULL;

// Monitor.Enter methods.
MonoMethod *_ep_rt_mono_monitor_enter_method = NULL;
MonoJitInfo *_ep_rt_mono_monitor_enter_method_jitinfo = NULL;

MonoMethod *_ep_rt_mono_monitor_enter_v4_method = NULL;
MonoJitInfo *_ep_rt_mono_monitor_enter_v4_method_jitinfo = NULL;

// Rundown types.
typedef
bool
(*ep_rt_mono_fire_method_rundown_events_func)(
	const uint64_t method_id,
	const uint64_t module_id,
	const uint64_t method_start_address,
	const uint32_t method_size,
	const uint32_t method_token,
	const uint32_t method_flags,
	const ep_char8_t *method_namespace,
	const ep_char8_t *method_name,
	const ep_char8_t *method_signature,
	const uint16_t count_of_map_entries,
	const uint32_t *il_offsets,
	const uint32_t *native_offsets,
	bool aot_method,
	bool verbose,
	void *user_data);

typedef
bool
(*ep_rt_mono_fire_assembly_rundown_events_func)(
	const uint64_t domain_id,
	const uint64_t assembly_id,
	const uint32_t assembly_flags,
	const uint32_t binding_id,
	const ep_char8_t *assembly_name,
	const uint64_t module_id,
	const uint32_t module_flags,
	const uint32_t reserved_flags,
	const ep_char8_t *module_il_path,
	const ep_char8_t *module_native_path,
	const uint8_t *managed_pdb_signature,
	const uint32_t managed_pdb_age,
	const ep_char8_t *managed_pdb_build_path,
	const uint8_t *native_pdb_signature,
	const uint32_t native_pdb_age,
	const ep_char8_t *native_pdb_build_path,
	void *user_data);

typedef
bool
(*ep_rt_mono_fire_domain_rundown_events_func)(
	const uint64_t domain_id,
	const uint32_t domain_flags,
	const ep_char8_t *domain_name,
	const uint32_t domain_index,
	void *user_data);

typedef struct _EventPipeFireMethodEventsData{
	MonoDomain *domain;
	uint8_t *buffer;
	size_t buffer_size;
	ep_rt_mono_fire_method_rundown_events_func method_events_func;
} EventPipeFireMethodEventsData;

typedef struct _EventPipeStackWalkData {
	EventPipeStackContents *stack_contents;
	bool top_frame;
	bool async_frame;
	bool safe_point_frame;
	bool runtime_invoke_frame;
} EventPipeStackWalkData;

typedef struct _EventPipeSampleProfileStackWalkData {
	EventPipeStackWalkData stack_walk_data;
	EventPipeStackContents stack_contents;
	uint64_t thread_id;
	uintptr_t thread_ip;
	uint32_t payload_data;
} EventPipeSampleProfileStackWalkData;

// Rundown flags.
#define RUNTIME_SKU_CORECLR 0x2
#define METHOD_FLAGS_DYNAMIC_METHOD 0x1
#define METHOD_FLAGS_GENERIC_METHOD 0x2
#define METHOD_FLAGS_SHARED_GENERIC_METHOD 0x4
#define METHOD_FLAGS_JITTED_METHOD 0x8
#define METHOD_FLAGS_JITTED_HELPER_METHOD 0x10
#define METHOD_FLAGS_EXTENT_HOT_SECTION 0x00000000
#define METHOD_FLAGS_EXTENT_COLD_SECTION 0x10000000

#define MODULE_FLAGS_NATIVE_MODULE 0x2
#define MODULE_FLAGS_DYNAMIC_MODULE 0x4
#define MODULE_FLAGS_MANIFEST_MODULE 0x8

#define ASSEMBLY_FLAGS_DYNAMIC_ASSEMBLY 0x2
#define ASSEMBLY_FLAGS_NATIVE_ASSEMBLY 0x4
#define ASSEMBLY_FLAGS_COLLECTIBLE_ASSEMBLY 0x8

#define DOMAIN_FLAGS_DEFAULT_DOMAIN 0x1
#define DOMAIN_FLAGS_EXECUTABLE_DOMAIN 0x2

// Event data types.
struct _ModuleEventData {
	uint8_t signature [EP_GUID_SIZE];
	uint64_t domain_id;
	uint64_t module_id;
	uint64_t assembly_id;
	const char *module_il_path;
	const char *module_il_pdb_path;
	const char *module_native_path;
	const char *module_native_pdb_path;
	uint32_t module_il_pdb_age;
	uint32_t module_native_pdb_age;
	uint32_t reserved_flags;
	uint32_t module_flags;
};

typedef struct _ModuleEventData ModuleEventData;

struct _AssemblyEventData {
	uint64_t domain_id;
	uint64_t assembly_id;
	uint64_t binding_id;
	char *assembly_name;
	uint32_t assembly_flags;
};

typedef struct _AssemblyEventData AssemblyEventData;

// Event flags.
#define THREAD_FLAGS_GC_SPECIAL 0x00000001
#define THREAD_FLAGS_FINALIZER 0x00000002
#define THREAD_FLAGS_THREADPOOL_WORKER 0x00000004

#define EXCEPTION_THROWN_FLAGS_HAS_INNER 0x1
#define EXCEPTION_THROWN_FLAGS_IS_NESTED 0x2
#define EXCEPTION_THROWN_FLAGS_IS_RETHROWN 0x4
#define EXCEPTION_THROWN_FLAGS_IS_CSE 0x8
#define EXCEPTION_THROWN_FLAGS_IS_CLS_COMPLIANT 0x10

// Provider keyword flags.
#define GC_KEYWORD 0x1
#define GC_HANDLE_KEYWORD 0x2
#define LOADER_KEYWORD 0x8
#define JIT_KEYWORD 0x10
#define APP_DOMAIN_RESOURCE_MANAGEMENT_KEYWORD 0x800
#define CONTENTION_KEYWORD 0x4000
#define EXCEPTION_KEYWORD 0x8000
#define THREADING_KEYWORD 0x10000
#define GC_ALLOCATION_KEYWORD 0x200000
#define GC_MOVES_KEYWORD 0x400000
#define GC_ROOT_KEYWORD 0x800000
#define GC_FINALIZATION_KEYWORD 0x1000000
#define GC_RESIZE_KEYWORD 0x2000000
#define METHOD_TRACING_KEYWORD 0x20000000
#define TYPE_DIAGNOSTIC_KEYWORD 0x8000000000
#define TYPE_LOADING_KEYWORD 0x8000000000
#define MONITOR_KEYWORD 0x10000000000
#define METHOD_INSTRUMENTATION_KEYWORD 0x40000000000

// GC provider types.

typedef struct _GCObjectAddressData {
	MonoObject *object;
	void *address;
} GCObjectAddressData;

typedef struct _GCAddressObjectData {
	void *address;
	MonoObject *object;
} GCAddressObjectData;

/*
 * Forward declares of all static functions.
 */

static
bool
fire_method_rundown_events_func (
	const uint64_t method_id,
	const uint64_t module_id,
	const uint64_t method_start_address,
	const uint32_t method_size,
	const uint32_t method_token,
	const uint32_t method_flags,
	const ep_char8_t *method_namespace,
	const ep_char8_t *method_name,
	const ep_char8_t *method_signature,
	const uint16_t count_of_map_entries,
	const uint32_t *il_offsets,
	const uint32_t *native_offsets,
	bool aot_method,
	bool verbose,
	void *user_data);

static
bool
fire_assembly_rundown_events_func (
	const uint64_t domain_id,
	const uint64_t assembly_id,
	const uint32_t assembly_flags,
	const uint32_t binding_id,
	const ep_char8_t *assembly_name,
	const uint64_t module_id,
	const uint32_t module_flags,
	const uint32_t reserved_flags,
	const ep_char8_t *module_il_path,
	const ep_char8_t *module_native_path,
	const uint8_t *managed_pdb_signature,
	const uint32_t managed_pdb_age,
	const ep_char8_t *managed_pdb_build_path,
	const uint8_t *native_pdb_signature,
	const uint32_t native_pdb_age,
	const ep_char8_t *native_pdb_build_path,
	void *user_data);

static
bool
fire_domain_rundown_events_func (
	const uint64_t domain_id,
	const uint32_t domain_flags,
	const ep_char8_t *domain_name,
	const uint32_t domain_index,
	void *user_data);

static
void
eventpipe_fire_method_events (
	MonoJitInfo *ji,
	MonoMethod *method,
	EventPipeFireMethodEventsData *events_data);

static
void
eventpipe_fire_method_events_func (
	MonoJitInfo *ji,
	void *user_data);

static
void
eventpipe_fire_assembly_events (
	MonoDomain *domain,
	MonoAssembly *assembly,
	ep_rt_mono_fire_assembly_rundown_events_func assembly_events_func);

static
gboolean
eventpipe_execute_rundown (
	ep_rt_mono_fire_domain_rundown_events_func domain_events_func,
	ep_rt_mono_fire_assembly_rundown_events_func assembly_events_func,
	ep_rt_mono_fire_method_rundown_events_func methods_events_func);

static
gboolean
eventpipe_walk_managed_stack_for_thread (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	EventPipeStackWalkData *stack_walk_data);

static
gboolean
eventpipe_walk_managed_stack_for_thread_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data);

static
gboolean
eventpipe_sample_profiler_walk_managed_stack_for_thread_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data);

static
void
profiler_eventpipe_thread_exited (
	MonoProfiler *prof,
	uintptr_t tid);

static
bool
parse_mono_profiler_options (const ep_char8_t *option);

static
bool
get_module_event_data (
	MonoImage *image,
	ModuleEventData *module_data);

static
bool
get_assembly_event_data (
	MonoAssembly *assembly,
	AssemblyEventData *assembly_data);

static
uint32_t
get_type_start_id (MonoType *type);

static
gboolean
get_exception_ip_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data);

static
void
runtime_profiler_jit_begin (
	MonoProfiler *prof,
	MonoMethod *method);

static
void
runtime_profiler_jit_failed (
	MonoProfiler *prof,
	MonoMethod *method);

static
void
runtime_profiler_jit_done (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoJitInfo *ji);

static
void
runtime_profiler_image_loaded (
	MonoProfiler *prof,
	MonoImage *image);

static
void
runtime_profiler_image_unloaded (
	MonoProfiler *prof,
	MonoImage *image);

static
void
runtime_profiler_assembly_loaded (
	MonoProfiler *prof,
	MonoAssembly *assembly);

static
void
runtime_profiler_assembly_unloaded (
	MonoProfiler *prof,
	MonoAssembly *assembly);

static
void
runtime_profiler_thread_started (
	MonoProfiler *prof,
	uintptr_t tid);

static
void
runtime_profiler_thread_stopped (
	MonoProfiler *prof,
	uintptr_t tid);

static
void
runtime_profiler_class_loading (
	MonoProfiler *prof,
	MonoClass *klass);

static
void
runtime_profiler_class_failed (
	MonoProfiler *prof,
	MonoClass *klass);

static
void
runtime_profiler_class_loaded (
	MonoProfiler *prof,
	MonoClass *klass);

static
void
runtime_profiler_exception_throw (
	MonoProfiler *prof,
	MonoObject *exception);

static
void
runtime_profiler_exception_clause (
	MonoProfiler *prof,
	MonoMethod *method,
	uint32_t clause_num,
	MonoExceptionEnum clause_type,
	MonoObject *exc);

static
void
runtime_profiler_monitor_contention (
	MonoProfiler *prof,
	MonoObject *obj);

static
void
runtime_profiler_monitor_acquired (
	MonoProfiler *prof,
	MonoObject *obj);

static
void
runtime_profiler_monitor_failed (
	MonoProfiler *prof,
	MonoObject *obj);

static
void
runtime_profiler_jit_code_buffer (
	MonoProfiler *prof,
	const mono_byte *buffer,
	uint64_t size,
	MonoProfilerCodeBufferType type,
	const void *data);

static
void
mono_profiler_app_domain_loading (
	MonoProfiler *prof,
	MonoDomain *domain);

static
void
mono_profiler_app_domain_loaded (
	MonoProfiler *prof,
	MonoDomain *domain);

static
void
mono_profiler_app_domain_unloading (
	MonoProfiler *prof,
	MonoDomain *domain);

static
void
mono_profiler_app_domain_unloaded (
	MonoProfiler *prof,
	MonoDomain *domain);

static
void
mono_profiler_app_domain_name (
	MonoProfiler *prof,
	MonoDomain *domain,
	const char *name);

static
void
mono_profiler_get_generic_types (
	MonoGenericInst *generic_instance,
	uint32_t *generic_type_count,
	uint8_t **generic_types);

static
void
mono_profiler_get_jit_data (
	MonoMethod *method,
	uint64_t *method_id,
	uint64_t *module_id,
	uint32_t *method_token,
	uint32_t *method_generic_type_count,
	uint8_t **method_generic_types);

static
void
mono_profiler_jit_begin (
	MonoProfiler *prof,
	MonoMethod *method);

static
void
mono_profiler_jit_failed (
	MonoProfiler *prof,
	MonoMethod *method);

static
void
mono_profiler_jit_done (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoJitInfo *ji);

static
void
mono_profiler_jit_chunk_created (
	MonoProfiler *prof,
	const mono_byte *chunk,
	uintptr_t size);

static
void
mono_profiler_jit_chunk_destroyed (
	MonoProfiler *prof,
	const mono_byte *chunk);

static
void
mono_profiler_jit_code_buffer (
	MonoProfiler *prof,
	const mono_byte *buffer,
	uint64_t size,
	MonoProfilerCodeBufferType type,
	const void *data);

static
void
mono_profiler_get_class_data (
	MonoClass *klass,
	uint64_t *class_id,
	uint64_t *module_id,
	ep_char8_t **class_name,
	uint32_t *class_generic_type_count,
	uint8_t **class_generic_types);

static
void
mono_profiler_class_loading (
	MonoProfiler *prof,
	MonoClass *klass);

static
void
mono_profiler_class_failed (
	MonoProfiler *prof,
	MonoClass *klass);

static
void
mono_profiler_class_loaded (
	MonoProfiler *prof,
	MonoClass *klass);

static
void
mono_profiler_vtable_loading (
	MonoProfiler *prof,
	MonoVTable *vtable);

static
void
mono_profiler_vtable_failed (
	MonoProfiler *prof,
	MonoVTable *vtable);

static
void
mono_profiler_vtable_loaded (
	MonoProfiler *prof,
	MonoVTable *vtable);

static
void
mono_profiler_module_loading (
	MonoProfiler *prof,
	MonoImage *image);

static
void
mono_profiler_module_failed (
	MonoProfiler *prof,
	MonoImage *image);

static
void
mono_profiler_module_loaded (
	MonoProfiler *prof,
	MonoImage *image);

static
void
mono_profiler_module_unloading (
	MonoProfiler *prof,
	MonoImage *image);

static
void
mono_profiler_module_unloaded (
	MonoProfiler *prof,
	MonoImage *image);

static
void
mono_profiler_assembly_loading (
	MonoProfiler *prof,
	MonoAssembly *assembly);

static
void
mono_profiler_assembly_loaded (
	MonoProfiler *prof,
	MonoAssembly *assembly);

static
void
mono_profiler_assembly_unloading (
	MonoProfiler *prof,
	MonoAssembly *assembly);

static
void
mono_profiler_assembly_unloaded (
	MonoProfiler *prof,
	MonoAssembly *assembly);

static
void
mono_profiler_method_enter (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoProfilerCallContext *context);

static
void
mono_profiler_method_leave (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoProfilerCallContext *context);

static
void
mono_profiler_method_tail_call (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoMethod *target_method);

static
void
mono_profiler_method_exception_leave (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoObject *exc);

static
void
mono_profiler_method_free (
	MonoProfiler *prof,
	MonoMethod *method);

static
void
mono_profiler_method_begin_invoke (
	MonoProfiler *prof,
	MonoMethod *method);

static
void
mono_profiler_method_end_invoke (
	MonoProfiler *prof,
	MonoMethod *method);

static
MonoProfilerCallInstrumentationFlags
mono_profiler_method_instrumentation (
	MonoProfiler *prof,
	MonoMethod *method);

static
void
mono_profiler_exception_throw (
	MonoProfiler *prof,
	MonoObject *exc);

static
void
mono_profiler_exception_clause (
	MonoProfiler *prof,
	MonoMethod *method,
	uint32_t clause_num,
	MonoExceptionEnum clause_type,
	MonoObject *exc);

static
void
mono_profiler_gc_event (
	MonoProfiler *prof,
	MonoProfilerGCEvent gc_event,
	uint32_t generation,
	mono_bool serial);

static
void
mono_profiler_gc_allocation (
	MonoProfiler *prof,
	MonoObject *object);

static
void
mono_profiler_gc_moves (
	MonoProfiler *prof,
	MonoObject *const* objects,
	uint64_t count);

static
void
mono_profiler_gc_resize (
	MonoProfiler *prof,
	uintptr_t size);

static
void
mono_profiler_gc_handle_created (
	MonoProfiler *prof,
	uint32_t handle,
	MonoGCHandleType type,
	MonoObject * object);

static
void
mono_profiler_gc_handle_deleted (
	MonoProfiler *prof,
	uint32_t handle,
	MonoGCHandleType type);

static
void
mono_profiler_gc_finalizing (MonoProfiler *prof);

static
void
mono_profiler_gc_finalized (MonoProfiler *prof);

static
void
mono_profiler_gc_root_register (
	MonoProfiler *prof,
	const mono_byte *start,
	uintptr_t size,
	MonoGCRootSource source,
	const void * key,
	const char * name);

static
void
mono_profiler_gc_root_unregister (
	MonoProfiler *prof,
	const mono_byte *start);

static
void
mono_profiler_gc_roots (
	MonoProfiler *prof,
	uint64_t count,
	const mono_byte *const * addresses,
	MonoObject *const * objects);

static
void
mono_profiler_monitor_contention (
	MonoProfiler *prof,
	MonoObject *object);

static
void
mono_profiler_monitor_failed (
	MonoProfiler *prof,
	MonoObject *object);

static
void
mono_profiler_monitor_acquired (
	MonoProfiler *prof,
	MonoObject *object);

static
void
mono_profiler_thread_started (
	MonoProfiler *prof,
	uintptr_t tid);

static
void
mono_profiler_thread_stopping (
	MonoProfiler *prof,
	uintptr_t tid);

static
void
mono_profiler_thread_stopped (
	MonoProfiler *prof,
	uintptr_t tid);

static
void
mono_profiler_thread_exited (
	MonoProfiler *prof,
	uintptr_t tid);

static
void
mono_profiler_thread_name (
	MonoProfiler *prof,
	uintptr_t tid,
	const char *name);

/*
 * Forward declares of all private functions (accessed using extern in ep-rt-mono.h).
 */

void
ep_rt_mono_component_init (void);

void
ep_rt_mono_init (void);

void
ep_rt_mono_init_finish (void);

void
ep_rt_mono_fini (void);

bool
ep_rt_mono_rand_try_get_bytes (
	uint8_t *buffer,
	size_t buffer_size);

EventPipeThread *
ep_rt_mono_thread_get_or_create (void);

void *
ep_rt_mono_thread_attach (bool background_thread);

void *
ep_rt_mono_thread_attach_2 (bool background_thread, EventPipeThreadType thread_type);

void
ep_rt_mono_thread_detach (void);

void
ep_rt_mono_thread_exited (void);

int64_t
ep_rt_mono_perf_counter_query (void);

int64_t
ep_rt_mono_perf_frequency_query (void);

void
ep_rt_mono_system_time_get (EventPipeSystemTime *system_time);

int64_t
ep_rt_mono_system_timestamp_get (void);

void
ep_rt_mono_os_environment_get_utf16 (ep_rt_env_array_utf16_t *env_array);

void
ep_rt_mono_init_providers_and_events (void);

void
ep_rt_mono_provider_config_init (EventPipeProviderConfiguration *provider_config);

bool
ep_rt_mono_providers_validate_all_disabled (void);

void
ep_rt_mono_fini_providers_and_events (void);

bool
ep_rt_mono_sample_profiler_write_sampling_event_for_threads (
	ep_rt_thread_handle_t sampling_thread,
	EventPipeEvent *sampling_event);

bool
ep_rt_mono_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents);

bool
ep_rt_mono_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len);

bool
ep_rt_mono_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len);

void
ep_rt_mono_execute_rundown (ep_rt_execution_checkpoint_array_t *execution_checkpoints);

static
inline
bool
profiler_callback_is_enabled (uint64_t enabled_keywords, uint64_t keyword)
{
	return (enabled_keywords & keyword) == keyword;
}

static
inline
uint16_t
clr_instance_get_id (void)
{
	// Mono runtime id.
	return 9;
}

static
bool
fire_method_rundown_events_func (
	const uint64_t method_id,
	const uint64_t module_id,
	const uint64_t method_start_address,
	const uint32_t method_size,
	const uint32_t method_token,
	const uint32_t method_flags,
	const ep_char8_t *method_namespace,
	const ep_char8_t *method_name,
	const ep_char8_t *method_signature,
	const uint16_t count_of_map_entries,
	const uint32_t *il_offsets,
	const uint32_t *native_offsets,
	bool aot_method,
	bool verbose,
	void *user_data)
{
	FireEtwMethodDCEndILToNativeMap (
		method_id,
		0,
		0,
		count_of_map_entries,
		il_offsets,
		native_offsets,
		clr_instance_get_id (),
		NULL,
		NULL);

	if (verbose) {
		FireEtwMethodDCEndVerbose_V1 (
			method_id,
			module_id,
			method_start_address,
			method_size,
			method_token,
			method_flags | METHOD_FLAGS_EXTENT_HOT_SECTION,
			method_namespace,
			method_name,
			method_signature,
			clr_instance_get_id (),
			NULL,
			NULL);

		if (aot_method)
			FireEtwMethodDCEndVerbose_V1 (
				method_id,
				module_id,
				method_start_address,
				method_size,
				method_token,
				method_flags | METHOD_FLAGS_EXTENT_COLD_SECTION,
				method_namespace,
				method_name,
				method_signature,
				clr_instance_get_id (),
				NULL,
				NULL);
	} else {
		FireEtwMethodDCEnd_V1 (
			method_id,
			module_id,
			method_start_address,
			method_size,
			method_token,
			method_flags | METHOD_FLAGS_EXTENT_HOT_SECTION,
			clr_instance_get_id (),
			NULL,
			NULL);

		if (aot_method)
			FireEtwMethodDCEnd_V1 (
				method_id,
				module_id,
				method_start_address,
				method_size,
				method_token,
				method_flags | METHOD_FLAGS_EXTENT_COLD_SECTION,
				clr_instance_get_id (),
				NULL,
				NULL);
	}

	return true;
}

static
bool
fire_assembly_rundown_events_func (
	const uint64_t domain_id,
	const uint64_t assembly_id,
	const uint32_t assembly_flags,
	const uint32_t binding_id,
	const ep_char8_t *assembly_name,
	const uint64_t module_id,
	const uint32_t module_flags,
	const uint32_t reserved_flags,
	const ep_char8_t *module_il_path,
	const ep_char8_t *module_native_path,
	const uint8_t *managed_pdb_signature,
	const uint32_t managed_pdb_age,
	const ep_char8_t *managed_pdb_build_path,
	const uint8_t *native_pdb_signature,
	const uint32_t native_pdb_age,
	const ep_char8_t *native_pdb_build_path,
	void *user_data)
{
	FireEtwModuleDCEnd_V2 (
		module_id,
		assembly_id,
		module_flags,
		reserved_flags,
		module_il_path,
		module_native_path,
		clr_instance_get_id (),
		managed_pdb_signature,
		managed_pdb_age,
		managed_pdb_build_path,
		native_pdb_signature,
		native_pdb_age,
		native_pdb_build_path,
		NULL,
		NULL);

	FireEtwDomainModuleDCEnd_V1 (
		module_id,
		assembly_id,
		domain_id,
		module_flags,
		reserved_flags,
		module_il_path,
		module_native_path,
		clr_instance_get_id (),
		NULL,
		NULL);

	FireEtwAssemblyDCEnd_V1 (
		assembly_id,
		domain_id,
		binding_id,
		assembly_flags,
		assembly_name,
		clr_instance_get_id (),
		NULL,
		NULL);

	return true;
}

static
bool
fire_domain_rundown_events_func (
	const uint64_t domain_id,
	const uint32_t domain_flags,
	const ep_char8_t *domain_name,
	const uint32_t domain_index,
	void *user_data)
{
	return FireEtwAppDomainDCEnd_V1 (
		domain_id,
		domain_flags,
		domain_name,
		domain_index,
		clr_instance_get_id (),
		NULL,
		NULL);
}

static
void
eventpipe_fire_method_events (
	MonoJitInfo *ji,
	MonoMethod *method,
	EventPipeFireMethodEventsData *events_data)
{
	EP_ASSERT (ji != NULL);
	EP_ASSERT (events_data->domain != NULL);
	EP_ASSERT (events_data->method_events_func != NULL);

	uint64_t method_id = 0;
	uint64_t module_id = 0;
	uint64_t method_code_start = (uint64_t)ji->code_start;
	uint32_t method_code_size = (uint32_t)ji->code_size;
	uint32_t method_token = 0;
	uint32_t method_flags = 0;
	uint8_t kind = MONO_CLASS_DEF;
	char *method_namespace = NULL;
	const char *method_name = NULL;
	char *method_signature = NULL;
	bool verbose = (MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context.Level >= (uint8_t)EP_EVENT_LEVEL_VERBOSE);

	//TODO: Optimize string formatting into functions accepting GString to reduce heap alloc.

	if (method) {
		method_id = (uint64_t)method;
		method_token = method->token;

		if (mono_jit_info_get_generic_sharing_context (ji))
			method_flags |= METHOD_FLAGS_SHARED_GENERIC_METHOD;

		if (method->dynamic)
			method_flags |= METHOD_FLAGS_DYNAMIC_METHOD;

		if (!ji->from_aot && !ji->from_llvm) {
			method_flags |= METHOD_FLAGS_JITTED_METHOD;
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				method_flags |= METHOD_FLAGS_JITTED_HELPER_METHOD;
		}

		if (method->is_generic || method->is_inflated)
			method_flags |= METHOD_FLAGS_GENERIC_METHOD;

		if (method->klass) {
			module_id = (uint64_t)m_class_get_image (method->klass);
			kind = m_class_get_class_kind (method->klass);
			if (kind == MONO_CLASS_GTD || kind == MONO_CLASS_GINST)
				method_flags |= METHOD_FLAGS_GENERIC_METHOD;
		}

		if (verbose) {
			method_name = method->name;
			method_signature = mono_signature_full_name (method->signature);
			if (method->klass)
				method_namespace = mono_type_get_name_full (m_class_get_byval_arg (method->klass), MONO_TYPE_NAME_FORMAT_IL);
		}

	}

	uint16_t offset_entries = 0;
	uint32_t *il_offsets = NULL;
	uint32_t *native_offsets = NULL;

	MonoDebugMethodJitInfo *debug_info = method ? mono_debug_find_method (method, events_data->domain) : NULL;
	if (debug_info) {
		offset_entries = debug_info->num_line_numbers;
		if (offset_entries != 0) {
			size_t needed_size = (offset_entries * sizeof (uint32_t) * 2);
			if (!events_data->buffer || needed_size > events_data->buffer_size) {
				g_free (events_data->buffer);
				events_data->buffer_size = (size_t)(needed_size * 1.5);
				events_data->buffer = g_new (uint8_t, events_data->buffer_size);
			}

			if (events_data->buffer) {
				il_offsets = (uint32_t*)events_data->buffer;
				native_offsets = il_offsets + offset_entries;

				for (int offset_count = 0; offset_count < offset_entries; ++offset_count) {
					il_offsets [offset_count] = debug_info->line_numbers [offset_count].il_offset;
					native_offsets [offset_count] = debug_info->line_numbers [offset_count].native_offset;
				}
			}
		}

		mono_debug_free_method_jit_info (debug_info);
	}

	if (events_data->buffer && !il_offsets && !native_offsets) {
		// No IL offset -> Native offset mapping available. Put all code on IL offset 0.
		EP_ASSERT (events_data->buffer_size >= sizeof (uint32_t) * 2);
		offset_entries = 1;
		il_offsets = (uint32_t*)events_data->buffer;
		native_offsets = il_offsets + offset_entries;
		il_offsets [0] = 0;
		native_offsets [0] = (uint32_t)ji->code_size;
	}

	events_data->method_events_func (
		method_id,
		module_id,
		method_code_start,
		method_code_size,
		method_token,
		method_flags,
		(ep_char8_t *)method_namespace,
		(ep_char8_t *)method_name,
		(ep_char8_t *)method_signature,
		offset_entries,
		il_offsets,
		native_offsets,
		(ji->from_aot || ji->from_llvm),
		verbose,
		NULL);

	g_free (method_namespace);
	g_free (method_signature);
}

static
inline
bool
include_method (MonoMethod *method)
{
	if (!method) {
		return false;
	} else if (!m_method_is_wrapper (method)) {
		return true;
	} else {
		WrapperInfo *wrapper = mono_marshal_get_wrapper_info (method);
		return (wrapper && wrapper->subtype == WRAPPER_SUBTYPE_PINVOKE) ? true : false;
	}
}

static
void
eventpipe_fire_method_events_func (
	MonoJitInfo *ji,
	void  *user_data)
{
	EventPipeFireMethodEventsData *events_data = (EventPipeFireMethodEventsData *)user_data;
	EP_ASSERT (events_data != NULL);

	if (ji && !ji->is_trampoline && !ji->async) {
		MonoMethod *method = jinfo_get_method (ji);
		if (include_method (method))
			eventpipe_fire_method_events (ji, method, events_data);
	}
}

static
void
eventpipe_fire_assembly_events (
	MonoDomain *domain,
	MonoAssembly *assembly,
	ep_rt_mono_fire_assembly_rundown_events_func assembly_events_func)
{
	EP_ASSERT (domain != NULL);
	EP_ASSERT (assembly != NULL);
	EP_ASSERT (assembly_events_func != NULL);

	uint64_t domain_id = (uint64_t)domain;
	uint64_t module_id = (uint64_t)assembly->image;
	uint64_t assembly_id = (uint64_t)assembly;

	// TODO: Extract all module IL/Native paths and pdb metadata when available.
	const char *module_il_path = "";
	const char *module_il_pdb_path = "";
	const char *module_native_path = "";
	const char *module_native_pdb_path = "";
	uint8_t signature [EP_GUID_SIZE] = { 0 };
	uint32_t module_il_pdb_age = 0;
	uint32_t module_native_pdb_age = 0;

	uint32_t reserved_flags = 0;
	uint64_t binding_id = 0;

	// Native methods are part of JIT table and already emitted.
	// TODO: FireEtwMethodDCEndVerbose_V1_or_V2 for all native methods in module as well?

	// Netcore has a 1:1 between assemblies and modules, so its always a manifest module.
	uint32_t module_flags = MODULE_FLAGS_MANIFEST_MODULE;
	if (assembly->image) {
		if (assembly->image->dynamic)
			module_flags |= MODULE_FLAGS_DYNAMIC_MODULE;
		if (assembly->image->aot_module)
			module_flags |= MODULE_FLAGS_NATIVE_MODULE;

		module_il_path = assembly->image->filename ? assembly->image->filename : "";
	}

	uint32_t assembly_flags = 0;
	if (assembly->dynamic)
		assembly_flags |= ASSEMBLY_FLAGS_DYNAMIC_ASSEMBLY;

	if (assembly->image && assembly->image->aot_module) {
		assembly_flags |= ASSEMBLY_FLAGS_NATIVE_ASSEMBLY;
	}

	char *assembly_name = mono_stringify_assembly_name (&assembly->aname);

	assembly_events_func (
		domain_id,
		assembly_id,
		assembly_flags,
		binding_id,
		(const ep_char8_t*)assembly_name,
		module_id,
		module_flags,
		reserved_flags,
		(const ep_char8_t *)module_il_path,
		(const ep_char8_t *)module_native_path,
		signature,
		module_il_pdb_age,
		(const ep_char8_t *)module_il_pdb_path,
		signature,
		module_native_pdb_age,
		(const ep_char8_t *)module_native_pdb_path,
		NULL);

	g_free (assembly_name);
}

static
gboolean
eventpipe_execute_rundown (
	ep_rt_mono_fire_domain_rundown_events_func domain_events_func,
	ep_rt_mono_fire_assembly_rundown_events_func assembly_events_func,
	ep_rt_mono_fire_method_rundown_events_func method_events_func)
{
	EP_ASSERT (domain_events_func != NULL);
	EP_ASSERT (assembly_events_func != NULL);
	EP_ASSERT (method_events_func != NULL);

	// Under netcore we only have root domain.
	MonoDomain *root_domain = mono_get_root_domain ();
	if (root_domain) {
		uint64_t domain_id = (uint64_t)root_domain;

		// Emit all functions in use (JIT, AOT and Interpreter).
		EventPipeFireMethodEventsData events_data;
		events_data.domain = root_domain;
		events_data.buffer_size = 1024 * sizeof(uint32_t);
		events_data.buffer = g_new (uint8_t, events_data.buffer_size);
		events_data.method_events_func = method_events_func;

		// All called JIT/AOT methods should be included in jit info table.
		mono_jit_info_table_foreach_internal (eventpipe_fire_method_events_func, &events_data);

		// All called interpreted methods should be included in interpreter jit info table.
		if (mono_get_runtime_callbacks ()->is_interpreter_enabled())
			mono_get_runtime_callbacks ()->interp_jit_info_foreach (eventpipe_fire_method_events_func, &events_data);

		// Phantom methods injected in callstacks representing runtime functions.
		if (_ep_rt_mono_runtime_helper_compile_method_jitinfo && _ep_rt_mono_runtime_helper_compile_method)
			eventpipe_fire_method_events (_ep_rt_mono_runtime_helper_compile_method_jitinfo, _ep_rt_mono_runtime_helper_compile_method, &events_data);
		if (_ep_rt_mono_monitor_enter_method_jitinfo && _ep_rt_mono_monitor_enter_method)
			eventpipe_fire_method_events (_ep_rt_mono_monitor_enter_method_jitinfo, _ep_rt_mono_monitor_enter_method, &events_data);
		if (_ep_rt_mono_monitor_enter_v4_method_jitinfo && _ep_rt_mono_monitor_enter_v4_method)
			eventpipe_fire_method_events (_ep_rt_mono_monitor_enter_v4_method_jitinfo, _ep_rt_mono_monitor_enter_v4_method, &events_data);

		g_free (events_data.buffer);

		// Iterate all assemblies in domain.
		GPtrArray *assemblies = mono_alc_get_all_loaded_assemblies ();
		if (assemblies) {
			for (int i = 0; i < assemblies->len; ++i) {
				MonoAssembly *assembly = (MonoAssembly *)g_ptr_array_index (assemblies, i);
				if (assembly)
					eventpipe_fire_assembly_events (root_domain, assembly, assembly_events_func);
			}
			g_ptr_array_free (assemblies, TRUE);
		}

		uint32_t domain_flags = DOMAIN_FLAGS_DEFAULT_DOMAIN | DOMAIN_FLAGS_EXECUTABLE_DOMAIN;
		const char *domain_name = root_domain->friendly_name ? root_domain->friendly_name : "";
		uint32_t domain_index = 1;

		domain_events_func (
			domain_id,
			domain_flags,
			(const ep_char8_t *)domain_name,
			domain_index,
			NULL);
	}

	return TRUE;
}

inline
static
bool
in_safe_point_frame (EventPipeStackContents *stack_content, WrapperInfo *wrapper)
{
	EP_ASSERT (stack_content != NULL);

	// If top of stack is a managed->native icall wrapper for one of the below subtypes, we are at a safe point frame.
	if (wrapper && ep_stack_contents_get_length (stack_content) == 0 && wrapper->subtype == WRAPPER_SUBTYPE_ICALL_WRAPPER &&
			(wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_threads_state_poll ||
			wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_threads_enter_gc_safe_region_unbalanced ||
			wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_threads_exit_gc_safe_region_unbalanced ||
			wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_threads_enter_gc_unsafe_region_unbalanced ||
			wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_threads_exit_gc_unsafe_region_unbalanced))
		return true;

	return false;
}

inline
static
bool
in_runtime_invoke_frame (EventPipeStackContents *stack_content, WrapperInfo *wrapper)
{
	EP_ASSERT (stack_content != NULL);

	// If top of stack is a managed->native runtime invoke wrapper, we are at a managed frame.
	if (wrapper && ep_stack_contents_get_length (stack_content) == 0 &&
			(wrapper->subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_NORMAL ||
			wrapper->subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_DIRECT ||
			wrapper->subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_DYNAMIC ||
			wrapper->subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL))
		return true;

	return false;
}

inline
static
bool
in_monitor_enter_frame (WrapperInfo *wrapper)
{
	if (wrapper && wrapper->subtype == WRAPPER_SUBTYPE_ICALL_WRAPPER &&
			(wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_monitor_enter_fast ||
			wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_monitor_enter_internal))
		return true;

	return false;
}

inline
static
bool
in_monitor_enter_v4_frame (WrapperInfo *wrapper)
{
	if (wrapper && wrapper->subtype == WRAPPER_SUBTYPE_ICALL_WRAPPER &&
			(wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_monitor_enter_v4_fast ||
			wrapper->d.icall.jit_icall_id == MONO_JIT_ICALL_mono_monitor_enter_v4_internal))
		return true;

	return false;
}

static
gboolean
eventpipe_walk_managed_stack_for_thread (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	EventPipeStackWalkData *stack_walk_data)
{
	EP_ASSERT (frame != NULL);
	EP_ASSERT (stack_walk_data != NULL);

	switch (frame->type) {
	case FRAME_TYPE_DEBUGGER_INVOKE:
	case FRAME_TYPE_MANAGED_TO_NATIVE:
	case FRAME_TYPE_TRAMPOLINE:
	case FRAME_TYPE_INTERP_TO_MANAGED:
	case FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX:
	case FRAME_TYPE_INTERP_ENTRY:
		stack_walk_data->top_frame = false;
		return FALSE;
	case FRAME_TYPE_JIT_ENTRY:
		// Frame in JIT compiler at top of callstack, add phantom frame representing call into JIT compiler.
		// Makes it possible to detect stacks waiting on JIT compiler.
		if (_ep_rt_mono_runtime_helper_compile_method && stack_walk_data->top_frame)
			ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)_ep_rt_mono_runtime_helper_compile_method), _ep_rt_mono_runtime_helper_compile_method);
		stack_walk_data->top_frame = false;
		return FALSE;
	case FRAME_TYPE_MANAGED:
	case FRAME_TYPE_INTERP:
		if (frame->ji) {
			stack_walk_data->async_frame |= frame->ji->async;
			MonoMethod *method = frame->ji->async ? NULL : frame->actual_method;
			if (method && m_method_is_wrapper (method)) {
				WrapperInfo *wrapper = mono_marshal_get_wrapper_info (method);
				if (in_safe_point_frame (stack_walk_data->stack_contents, wrapper)) {
					stack_walk_data->safe_point_frame = true;
				}else if (in_runtime_invoke_frame (stack_walk_data->stack_contents, wrapper)) {
					stack_walk_data->runtime_invoke_frame = true;
				} else if (_ep_rt_mono_monitor_enter_method && in_monitor_enter_frame (wrapper)) {
					ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)_ep_rt_mono_monitor_enter_method), _ep_rt_mono_monitor_enter_method);
				} else if (_ep_rt_mono_monitor_enter_v4_method && in_monitor_enter_v4_frame (wrapper)) {
					ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)_ep_rt_mono_monitor_enter_v4_method), _ep_rt_mono_monitor_enter_v4_method);
				} else if (wrapper && wrapper->subtype == WRAPPER_SUBTYPE_PINVOKE) {
					ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)frame->ji->code_start + frame->native_offset), method);
				}
			} else if (method && !m_method_is_wrapper (method)) {
				ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)frame->ji->code_start + frame->native_offset), method);
			} else if (!method && frame->ji->async && !frame->ji->is_trampoline) {
				ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)frame->ji->code_start), method);
			}
		}
		stack_walk_data->top_frame = false;
		return ep_stack_contents_get_length (stack_walk_data->stack_contents) >= EP_MAX_STACK_DEPTH;
	default:
		EP_UNREACHABLE ("eventpipe_walk_managed_stack_for_thread");
		return FALSE;
	}
}

static
gboolean
eventpipe_walk_managed_stack_for_thread_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data)
{
	return eventpipe_walk_managed_stack_for_thread (frame, ctx, (EventPipeStackWalkData *)data);
}

static
gboolean
eventpipe_sample_profiler_walk_managed_stack_for_thread_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data)
{
	EP_ASSERT (frame != NULL);
	EP_ASSERT (data != NULL);

	EventPipeSampleProfileStackWalkData *sample_data = (EventPipeSampleProfileStackWalkData *)data;

	if (sample_data->payload_data == EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR) {
		switch (frame->type) {
		case FRAME_TYPE_MANAGED:
			sample_data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED;
			break;
		case FRAME_TYPE_MANAGED_TO_NATIVE:
		case FRAME_TYPE_TRAMPOLINE:
			sample_data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL;
			break;
		case FRAME_TYPE_JIT_ENTRY:
			sample_data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL;
			break;
		case FRAME_TYPE_INTERP:
			sample_data->payload_data = frame->managed ? EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED : EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL;
			break;
		case FRAME_TYPE_INTERP_TO_MANAGED:
		case FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX:
			break;
		default:
			sample_data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED;
		}
	}

	return eventpipe_walk_managed_stack_for_thread (frame, ctx, &sample_data->stack_walk_data);
}

static
void
profiler_eventpipe_thread_exited (
	MonoProfiler *prof,
	uintptr_t tid)
{
	void ep_rt_mono_thread_exited (void);
	ep_rt_mono_thread_exited ();
}

static bool
parse_mono_profiler_options (const ep_char8_t *option)
{
	do {
		if (!*option)
			return false;

		if (!strncmp (option, "alloc", 5)) {
			mono_profiler_enable_allocations ();
			option += 5;
		} else if (!strncmp (option, "exception", 9)) {
			mono_profiler_enable_clauses ();
			option += 9;
		/*} else if (!strncmp (option, "sample", 6)) {
			mono_profiler_enable_sampling (_ep_rt_dotnet_mono_profiler_provider);
			option += 6;*/
		} else {
			return false;
		}

		if (*option == ',')
			option++;
	} while (*option);

	return true;
}

void
ep_rt_mono_component_init (void)
{
	_ep_rt_default_profiler = mono_profiler_create (NULL);
	_ep_rt_dotnet_runtime_profiler_provider = mono_profiler_create (NULL);
	_ep_rt_dotnet_mono_profiler_provider = mono_profiler_create (NULL);

	char *diag_env = g_getenv("MONO_DIAGNOSTICS");
	if (diag_env) {
		int diag_argc = 1;
		char **diag_argv = g_new (char *, 1);
		if (diag_argv) {
			diag_argv [0] = NULL;
			if (!mono_parse_options_from (diag_env, &diag_argc, &diag_argv)) {
				for (int i = 0; i < diag_argc; ++i) {
					if (diag_argv [i]) {
						if (strncmp (diag_argv [i], "--diagnostic-mono-profiler=", 27) == 0) {
							if (!parse_mono_profiler_options (diag_argv [i] + 27))
								mono_trace (G_LOG_LEVEL_ERROR, MONO_TRACE_DIAGNOSTICS, "Failed parsing MONO_DIAGNOSTICS environment variable option: %s", diag_argv [i]);
						} else if (strncmp (diag_argv [i], "--diagnostic-mono-profiler-callspec=", 36) == 0) {
							char *errstr = NULL;
							if (!mono_callspec_parse (diag_argv [i] + 36, &_ep_rt_dotnet_mono_profiler_provider_callspec, &errstr)) {
								mono_trace (G_LOG_LEVEL_ERROR, MONO_TRACE_DIAGNOSTICS, "Failed parsing '%s': %s", diag_argv [i], errstr);
								g_free (errstr);
								mono_callspec_cleanup (&_ep_rt_dotnet_mono_profiler_provider_callspec);
							} else {
								mono_profiler_set_call_instrumentation_filter_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_instrumentation);
							}
						} else if (strncmp (diag_argv [i], "--diagnostic-ports=", 19) == 0) {
							char *diag_ports_env = g_getenv("DOTNET_DiagnosticPorts");
							if (diag_ports_env)
								mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_DIAGNOSTICS, "DOTNET_DiagnosticPorts environment variable already set, ignoring --diagnostic-ports used in MONO_DIAGNOSTICS environment variable");
							else
								g_setenv ("DOTNET_DiagnosticPorts", diag_argv [i] + 19, TRUE);
							g_free (diag_ports_env);

						} else {
							mono_trace (G_LOG_LEVEL_ERROR, MONO_TRACE_DIAGNOSTICS, "Failed parsing MONO_DIAGNOSTICS environment variable, unknown option: %s", diag_argv [i]);
						}

						g_free (diag_argv [i]);
						diag_argv [i] = NULL;
					}
				}

				g_free (diag_argv);
			} else {
				mono_trace (G_LOG_LEVEL_ERROR, MONO_TRACE_DIAGNOSTICS, "Failed parsing MONO_DIAGNOSTICS environment variable");
			}
		}
	}
	g_free (diag_env);
}

void
ep_rt_mono_init (void)
{
	mono_native_tls_alloc (&_ep_rt_mono_thread_holder_tls_id, NULL);

	mono_100ns_ticks ();
	mono_rand_open ();
	_ep_rt_mono_rand_provider = mono_rand_init (NULL, 0);

	_ep_rt_mono_initialized = TRUE;

	EP_ASSERT (_ep_rt_default_profiler != NULL);
	EP_ASSERT (_ep_rt_dotnet_runtime_profiler_provider != NULL);
	EP_ASSERT (_ep_rt_dotnet_mono_profiler_provider != NULL);

	mono_profiler_set_thread_stopped_callback (_ep_rt_default_profiler, profiler_eventpipe_thread_exited);

	MonoMethodSignature *method_signature = mono_metadata_signature_alloc (mono_get_corlib (), 1);
	if (method_signature) {
		method_signature->params[0] = m_class_get_byval_arg (mono_get_object_class());
		method_signature->ret = m_class_get_byval_arg (mono_get_void_class());

		ERROR_DECL (error);
		MonoClass *runtime_helpers = mono_class_from_name_checked (mono_get_corlib (), "System.Runtime.CompilerServices", "RuntimeHelpers", error);
		if (is_ok (error) && runtime_helpers) {
			MonoMethodBuilder *method_builder = mono_mb_new (runtime_helpers, "CompileMethod", MONO_WRAPPER_RUNTIME_INVOKE);
			if (method_builder) {
				_ep_rt_mono_runtime_helper_compile_method = mono_mb_create_method (method_builder, method_signature, 1);
				mono_mb_free (method_builder);
			}
		}
		mono_error_cleanup (error);
		mono_metadata_free_method_signature (method_signature);

		if (_ep_rt_mono_runtime_helper_compile_method) {
			_ep_rt_mono_runtime_helper_compile_method_jitinfo = (MonoJitInfo *)g_new0 (MonoJitInfo, 1);
			if (_ep_rt_mono_runtime_helper_compile_method) {
				_ep_rt_mono_runtime_helper_compile_method_jitinfo->code_start = MINI_FTNPTR_TO_ADDR (_ep_rt_mono_runtime_helper_compile_method);
				_ep_rt_mono_runtime_helper_compile_method_jitinfo->code_size = 20;
				_ep_rt_mono_runtime_helper_compile_method_jitinfo->d.method = _ep_rt_mono_runtime_helper_compile_method;
			}
		}
	}

	{
		ERROR_DECL (error);
		MonoMethodDesc *desc = NULL;
		MonoClass *monitor = mono_class_from_name_checked (mono_get_corlib (), "System.Threading", "Monitor", error);
		if (is_ok (error) && monitor) {
			desc = mono_method_desc_new ("Monitor:Enter(object,bool&)", FALSE);
			if (desc) {
				_ep_rt_mono_monitor_enter_v4_method = mono_method_desc_search_in_class (desc, monitor);
				mono_method_desc_free (desc);

				if (_ep_rt_mono_monitor_enter_v4_method) {
					_ep_rt_mono_monitor_enter_v4_method_jitinfo = (MonoJitInfo *)g_new0 (MonoJitInfo, 1);
					if (_ep_rt_mono_monitor_enter_v4_method_jitinfo) {
						_ep_rt_mono_monitor_enter_v4_method_jitinfo->code_start = MINI_FTNPTR_TO_ADDR (_ep_rt_mono_monitor_enter_v4_method);
						_ep_rt_mono_monitor_enter_v4_method_jitinfo->code_size = 20;
						_ep_rt_mono_monitor_enter_v4_method_jitinfo->d.method = _ep_rt_mono_monitor_enter_v4_method;
					}
				}
			}

			desc = mono_method_desc_new ("Monitor:Enter(object)", FALSE);
			if (desc) {
				_ep_rt_mono_monitor_enter_method = mono_method_desc_search_in_class (desc, monitor);
				mono_method_desc_free (desc);

				if (_ep_rt_mono_monitor_enter_method ) {
					_ep_rt_mono_monitor_enter_method_jitinfo = (MonoJitInfo *)g_new0 (MonoJitInfo, 1);
					if (_ep_rt_mono_monitor_enter_method_jitinfo) {
						_ep_rt_mono_monitor_enter_method_jitinfo->code_start = MINI_FTNPTR_TO_ADDR (_ep_rt_mono_monitor_enter_method);
						_ep_rt_mono_monitor_enter_method_jitinfo->code_size = 20;
						_ep_rt_mono_monitor_enter_method_jitinfo->d.method = _ep_rt_mono_monitor_enter_method;
					}
				}
			}
		}
		mono_error_cleanup (error);
	}
}

void
ep_rt_mono_init_finish (void)
{
	if (mono_runtime_get_no_exec ())
		return;

	// Managed init of diagnostics classes, like registration of RuntimeEventSource (if available).
	ERROR_DECL (error);

	MonoClass *runtime_event_source = mono_class_from_name_checked (mono_get_corlib (), "System.Diagnostics.Tracing", "RuntimeEventSource", error);
	if (is_ok (error) && runtime_event_source) {
		MonoMethod *init = mono_class_get_method_from_name_checked (runtime_event_source, "Initialize", -1, 0, error);
		if (is_ok (error) && init) {
			mono_runtime_try_invoke_handle (init, NULL_HANDLE, NULL, error);
		}
	}

	mono_error_cleanup (error);
}

void
ep_rt_mono_fini (void)
{
	if (_ep_rt_mono_sampled_thread_callstacks)
		g_array_free (_ep_rt_mono_sampled_thread_callstacks, TRUE);

	if (_ep_rt_mono_initialized)
		mono_rand_close (_ep_rt_mono_rand_provider);

	g_free (_ep_rt_mono_runtime_helper_compile_method_jitinfo);
	_ep_rt_mono_runtime_helper_compile_method_jitinfo = NULL;

	mono_free_method (_ep_rt_mono_runtime_helper_compile_method);
	_ep_rt_mono_runtime_helper_compile_method = NULL;

	g_free (_ep_rt_mono_monitor_enter_method_jitinfo);
	_ep_rt_mono_monitor_enter_method_jitinfo = NULL;
	_ep_rt_mono_monitor_enter_method = NULL;

	g_free (_ep_rt_mono_monitor_enter_v4_method_jitinfo);
	_ep_rt_mono_monitor_enter_v4_method_jitinfo = NULL;
	_ep_rt_mono_monitor_enter_v4_method = NULL;

	if (_ep_rt_dotnet_mono_profiler_provider_callspec.enabled) {
		mono_profiler_set_call_instrumentation_filter_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
		mono_callspec_cleanup (&_ep_rt_dotnet_mono_profiler_provider_callspec);
	}

	_ep_rt_mono_sampled_thread_callstacks = NULL;
	_ep_rt_mono_rand_provider = NULL;
	_ep_rt_mono_initialized = FALSE;
}

bool
ep_rt_mono_rand_try_get_bytes (
	uint8_t *buffer,
	size_t buffer_size)
{
	EP_ASSERT (_ep_rt_mono_rand_provider != NULL);

	ERROR_DECL (error);
	return mono_rand_try_get_bytes (&_ep_rt_mono_rand_provider, (guchar *)buffer, (gssize)buffer_size, error);
}

EventPipeThread *
ep_rt_mono_thread_get_or_create (void)
{
	EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (_ep_rt_mono_thread_holder_tls_id);
	if (!thread_holder) {
		thread_holder = thread_holder_alloc_func ();
		mono_native_tls_set_value (_ep_rt_mono_thread_holder_tls_id, thread_holder);
	}
	return ep_thread_holder_get_thread (thread_holder);
}

void *
ep_rt_mono_thread_attach (bool background_thread)
{
	MonoThread *thread = NULL;

	// NOTE, under netcore, only root domain exists.
	if (!mono_thread_current ()) {
		thread = mono_thread_internal_attach (mono_get_root_domain ());
		if (background_thread && thread) {
			mono_thread_set_state (thread, ThreadState_Background);
			mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NO_SAMPLE);
		}
	}

	return thread;
}

void *
ep_rt_mono_thread_attach_2 (bool background_thread, EventPipeThreadType thread_type)
{
	void *result = ep_rt_mono_thread_attach (background_thread);
	if (result && thread_type == EP_THREAD_TYPE_SAMPLING) {
		// Increase sampling thread priority, accepting failures.
#ifdef HOST_WIN32
		SetThreadPriority (GetCurrentThread (), THREAD_PRIORITY_HIGHEST);
#elif _POSIX_PRIORITY_SCHEDULING
		int policy;
		int priority;
		struct sched_param param;
		int schedparam_result = pthread_getschedparam (pthread_self (), &policy, &param);
		if (schedparam_result == 0) {
			// Attempt to switch the thread to real time scheduling. This will not
			// necessarily work on all OSs; for example, most Linux systems will give
			// us EPERM here unless configured to allow this.
			priority = param.sched_priority;
			param.sched_priority = sched_get_priority_max (SCHED_RR);
			if (param.sched_priority != -1) {
				schedparam_result = pthread_setschedparam (pthread_self (), SCHED_RR, &param);
				if (schedparam_result != 0) {
					// Fallback, attempt to increase to max priority using current policy.
					param.sched_priority = sched_get_priority_max (policy);
					if (param.sched_priority != -1 && param.sched_priority != priority)
						pthread_setschedparam (pthread_self (), policy, &param);
				}
			}
		}
#endif
	}

	return result;
}

void
ep_rt_mono_thread_detach (void)
{
	MonoThread *current_thread = mono_thread_current ();
	if (current_thread)
		mono_thread_internal_detach (current_thread);
}

void
ep_rt_mono_thread_exited (void)
{
	if (_ep_rt_mono_initialized) {
		EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (_ep_rt_mono_thread_holder_tls_id);
		if (thread_holder)
			thread_holder_free_func (thread_holder);
		mono_native_tls_set_value (_ep_rt_mono_thread_holder_tls_id, NULL);
	}
}

#ifdef HOST_WIN32
int64_t
ep_rt_mono_perf_counter_query (void)
{
	LARGE_INTEGER value;
	if (QueryPerformanceCounter (&value))
		return (int64_t)value.QuadPart;
	else
		return 0;
}

int64_t
ep_rt_mono_perf_frequency_query (void)
{
	LARGE_INTEGER value;
	if (QueryPerformanceFrequency (&value))
		return (int64_t)value.QuadPart;
	else
		return 0;
}

void
ep_rt_mono_system_time_get (EventPipeSystemTime *system_time)
{
	SYSTEMTIME value;
	GetSystemTime (&value);

	EP_ASSERT (system_time != NULL);
	ep_system_time_set (
		system_time,
		value.wYear,
		value.wMonth,
		value.wDayOfWeek,
		value.wDay,
		value.wHour,
		value.wMinute,
		value.wSecond,
		value.wMilliseconds);
}

int64_t
ep_rt_mono_system_timestamp_get (void)
{
	FILETIME value;
	GetSystemTimeAsFileTime (&value);
	return (int64_t)((((uint64_t)value.dwHighDateTime) << 32) | (uint64_t)value.dwLowDateTime);
}
#else
#include <sys/types.h>
#include <sys/stat.h>
#include <utime.h>
#include <time.h>

#if HAVE_SYS_TIME_H
#include <sys/time.h>
#endif // HAVE_SYS_TIME_H

#if HAVE_MACH_ABSOLUTE_TIME
#include <mach/mach_time.h>
static mono_lazy_init_t _ep_rt_mono_time_base_info_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
static mach_timebase_info_data_t _ep_rt_mono_time_base_info = {0};
#endif

#ifdef HAVE_LOCALTIME_R
#define HAVE_GMTIME_R 1
#endif

static const int64_t SECS_BETWEEN_1601_AND_1970_EPOCHS = 11644473600LL;
static const int64_t SECS_TO_100NS = 10000000;
static const int64_t SECS_TO_NS = 1000000000;
static const int64_t MSECS_TO_MIS = 1000;

/* clock_gettime () is found by configure on Apple builds, but its only present from ios 10, macos 10.12, tvos 10 and watchos 3 */
#if defined (HAVE_CLOCK_MONOTONIC) && (defined(TARGET_IOS) || defined(TARGET_OSX) || defined(TARGET_WATCHOS) || defined(TARGET_TVOS))
#undef HAVE_CLOCK_MONOTONIC
#endif

#ifndef HAVE_CLOCK_MONOTONIC
static const int64_t MISECS_TO_NS = 1000;
#endif

static
void
time_base_info_lazy_init (void);

static
int64_t
system_time_to_int64 (
	time_t sec,
	long nsec);

#if HAVE_MACH_ABSOLUTE_TIME
static
void
time_base_info_lazy_init (void)
{
	kern_return_t result = mach_timebase_info (&_ep_rt_mono_time_base_info);
	if (result != KERN_SUCCESS)
		memset (&_ep_rt_mono_time_base_info, 0, sizeof (_ep_rt_mono_time_base_info));
}
#endif

int64_t
ep_rt_mono_perf_counter_query (void)
{
#if HAVE_MACH_ABSOLUTE_TIME
	return (int64_t)mach_absolute_time ();
#elif HAVE_CLOCK_MONOTONIC
	struct timespec ts;
	int result = clock_gettime (CLOCK_MONOTONIC, &ts);
	if (result == 0)
		return ((int64_t)(ts.tv_sec) * (int64_t)(SECS_TO_NS)) + (int64_t)(ts.tv_nsec);
#else
	#error "ep_rt_mono_perf_counter_get requires either mach_absolute_time () or clock_gettime (CLOCK_MONOTONIC) to be supported."
#endif
	return 0;
}

int64_t
ep_rt_mono_perf_frequency_query (void)
{
#if HAVE_MACH_ABSOLUTE_TIME
	// (numer / denom) gives you the nanoseconds per tick, so the below code
	// computes the number of ticks per second. We explicitly do the multiplication
	// first in order to help minimize the error that is produced by integer division.
	mono_lazy_initialize (&_ep_rt_mono_time_base_info_init, time_base_info_lazy_init);
	if (_ep_rt_mono_time_base_info.denom == 0 || _ep_rt_mono_time_base_info.numer == 0)
		return 0;
	return ((int64_t)(SECS_TO_NS) * (int64_t)(_ep_rt_mono_time_base_info.denom)) / (int64_t)(_ep_rt_mono_time_base_info.numer);
#elif HAVE_CLOCK_MONOTONIC
	// clock_gettime () returns a result in terms of nanoseconds rather than a count. This
	// means that we need to either always scale the result by the actual resolution (to
	// get a count) or we need to say the resolution is in terms of nanoseconds. We prefer
	// the latter since it allows the highest throughput and should minimize error propagated
	// to the user.
	return (int64_t)(SECS_TO_NS);
#else
	#error "ep_rt_mono_perf_frequency_query requires either mach_absolute_time () or clock_gettime (CLOCK_MONOTONIC) to be supported."
#endif
	return 0;
}

void
ep_rt_mono_system_time_get (EventPipeSystemTime *system_time)
{
	time_t tt;
#if HAVE_GMTIME_R
	struct tm ut;
#endif /* HAVE_GMTIME_R */
	struct tm *ut_ptr;
	struct timeval time_val;
	int timeofday_retval;

	EP_ASSERT (system_time != NULL);

	tt = time (NULL);

	/* We can't get millisecond resolution from time (), so we get it from gettimeofday () */
	timeofday_retval = gettimeofday (&time_val, NULL);

#if HAVE_GMTIME_R
	ut_ptr = &ut;
	if (gmtime_r (&tt, ut_ptr) == NULL)
#else /* HAVE_GMTIME_R */
	if ((ut_ptr = gmtime (&tt)) == NULL)
#endif /* HAVE_GMTIME_R */
		EP_UNREACHABLE ();

	uint16_t milliseconds = 0;
	if (timeofday_retval != -1) {
		int old_seconds;
		int new_seconds;

		milliseconds = time_val.tv_usec / MSECS_TO_MIS;

		old_seconds = ut_ptr->tm_sec;
		new_seconds = time_val.tv_sec % 60;

		/* just in case we reached the next second in the interval between time () and gettimeofday () */
		if (old_seconds != new_seconds)
			milliseconds = 999;
	}

	ep_system_time_set (
		system_time,
		1900 + ut_ptr->tm_year,
		ut_ptr->tm_mon + 1,
		ut_ptr->tm_wday,
		ut_ptr->tm_mday,
		ut_ptr->tm_hour,
		ut_ptr->tm_min,
		ut_ptr->tm_sec,
		milliseconds);
}

static
inline
int64_t
system_time_to_int64 (
	time_t sec,
	long nsec)
{
	return ((int64_t)sec + SECS_BETWEEN_1601_AND_1970_EPOCHS) * SECS_TO_100NS + (nsec / 100);
}

int64_t
ep_rt_mono_system_timestamp_get (void)
{
#if HAVE_CLOCK_MONOTONIC
	struct timespec time;
	if (clock_gettime (CLOCK_REALTIME, &time) == 0)
		return system_time_to_int64 (time.tv_sec, time.tv_nsec);
#else
	struct timeval time;
	if (gettimeofday (&time, NULL) == 0)
		return system_time_to_int64 (time.tv_sec, time.tv_usec * MISECS_TO_NS);
#endif
	else
		return system_time_to_int64 (0, 0);
}
#endif

#ifndef HOST_WIN32
#if defined(__APPLE__)
#if defined (TARGET_OSX)
G_BEGIN_DECLS
gchar ***_NSGetEnviron(void);
G_END_DECLS
#define environ (*_NSGetEnviron())
#else
static char *_ep_rt_mono_environ[1] = { NULL };
#define environ _ep_rt_mono_environ
#endif /* defined (TARGET_OSX) */
#else
G_BEGIN_DECLS
extern char **environ;
G_END_DECLS
#endif /* defined (__APPLE__) */
#endif /* !defined (HOST_WIN32) */

void
ep_rt_mono_os_environment_get_utf16 (ep_rt_env_array_utf16_t *env_array)
{
	EP_ASSERT (env_array != NULL);
#ifdef HOST_WIN32
	LPWSTR envs = GetEnvironmentStringsW ();
	if (envs) {
		LPWSTR next = envs;
		while (*next) {
			ep_rt_env_array_utf16_append (env_array, ep_rt_utf16_string_dup (next));
			next += ep_rt_utf16_string_len (next) + 1;
		}
		FreeEnvironmentStringsW (envs);
	}
#else
	gchar **next = NULL;
	for (next = environ; *next != NULL; ++next)
		ep_rt_env_array_utf16_append (env_array, ep_rt_utf8_to_utf16_string (*next, -1));
#endif
}

void
ep_rt_mono_init_providers_and_events (void)
{
	extern void InitProvidersAndEvents (void);
	InitProvidersAndEvents ();
}

void
ep_rt_mono_provider_config_init (EventPipeProviderConfiguration *provider_config)
{
	if (!ep_rt_utf8_string_compare (ep_config_get_rundown_provider_name_utf8 (), ep_provider_config_get_provider_name (provider_config))) {
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context.Level = ep_provider_config_get_logging_level (provider_config);
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context.EnabledKeywordsBitmask = ep_provider_config_get_keywords (provider_config);
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context.IsEnabled = true;
	}
}

bool
ep_rt_mono_providers_validate_all_disabled (void)
{
	return (!MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context.IsEnabled &&
		!MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context.IsEnabled &&
		!MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context.IsEnabled &&
		!MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_EVENTPIPE_Context.IsEnabled);
}

void
ep_rt_mono_fini_providers_and_events (void)
{
	// dotnet/runtime: issue 12775: EventPipe shutdown race conditions
	// Deallocating providers/events here might cause AV if a WriteEvent
	// was to occur. Thus, we are not doing this cleanup.
}

bool
ep_rt_mono_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents)
{
	EP_ASSERT (thread != NULL && stack_contents != NULL);

	EventPipeStackWalkData stack_walk_data;
	stack_walk_data.stack_contents = stack_contents;
	stack_walk_data.top_frame = true;
	stack_walk_data.async_frame = false;
	stack_walk_data.safe_point_frame = false;
	stack_walk_data.runtime_invoke_frame = false;

	if (thread == ep_rt_thread_get_handle () && mono_get_eh_callbacks ()->mono_walk_stack_with_ctx)
		mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (eventpipe_walk_managed_stack_for_thread_func, NULL, MONO_UNWIND_SIGNAL_SAFE, &stack_walk_data);
	else if (mono_get_eh_callbacks ()->mono_walk_stack_with_state)
		mono_get_eh_callbacks ()->mono_walk_stack_with_state (eventpipe_walk_managed_stack_for_thread_func, mono_thread_info_get_suspend_state (thread), MONO_UNWIND_SIGNAL_SAFE, &stack_walk_data);

	return true;
}

bool
ep_rt_mono_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	EP_ASSERT (method != NULL);
	EP_ASSERT (name != NULL);

	MonoClass *method_class = mono_method_get_class (method);
	MonoImage *method_image = method_class ? mono_class_get_image (method_class) : NULL;
	const ep_char8_t *assembly_name = method_image ? mono_image_get_name (method_image) : NULL;

	if (!assembly_name)
		return false;

	g_strlcpy (name, assembly_name, name_len);
	return true;
}

bool
ep_rt_mono_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	EP_ASSERT (method != NULL);
	EP_ASSERT (name != NULL);

	char *full_method_name = mono_method_get_name_full (method, TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL);
	if (!full_method_name)
		return false;

	g_strlcpy (name, full_method_name, name_len);

	g_free (full_method_name);
	return true;
}

bool
ep_rt_mono_sample_profiler_write_sampling_event_for_threads (
	ep_rt_thread_handle_t sampling_thread,
	EventPipeEvent *sampling_event)
{
	// Follows CoreClr implementation of sample profiler. Generic invasive/expensive way to do CPU sample profiling relying on STW and stackwalks.
	// TODO: Investigate alternatives on platforms supporting Signals/SuspendThread (see Mono profiler) or CPU PMU's (see ETW/perf_event_open).

	// Sample profiler only runs on one thread, no need to synchorinize.
	if (!_ep_rt_mono_sampled_thread_callstacks)
		_ep_rt_mono_sampled_thread_callstacks = g_array_sized_new (FALSE, FALSE, sizeof (EventPipeSampleProfileStackWalkData), _ep_rt_mono_max_sampled_thread_count);

	// Make sure there is room based on previous max number of sampled threads.
	// NOTE, there is a chance there are more threads than max, if that's the case we will
	// miss those threads in this sample, but will be included in next when max has been adjusted.
	g_array_set_size (_ep_rt_mono_sampled_thread_callstacks, _ep_rt_mono_max_sampled_thread_count);

	uint32_t filtered_thread_count = 0;
	uint32_t sampled_thread_count = 0;

	mono_stop_world (MONO_THREAD_INFO_FLAGS_NO_GC);

	gboolean async_context = mono_thread_info_is_async_context ();
	mono_thread_info_set_is_async_context (TRUE);

	// Record all info needed in sample events while runtime is suspended, must be async safe.
	FOREACH_THREAD_SAFE_EXCLUDE (thread_info, MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE) {
		if (!mono_thread_info_is_running (thread_info)) {
			MonoThreadUnwindState *thread_state = mono_thread_info_get_suspend_state (thread_info);
			if (thread_state->valid) {
				if (sampled_thread_count < _ep_rt_mono_max_sampled_thread_count) {
					EventPipeSampleProfileStackWalkData *data = &g_array_index (_ep_rt_mono_sampled_thread_callstacks, EventPipeSampleProfileStackWalkData, sampled_thread_count);
					data->thread_id = ep_rt_thread_id_t_to_uint64_t (mono_thread_info_get_tid (thread_info));
					data->thread_ip = (uintptr_t)MONO_CONTEXT_GET_IP (&thread_state->ctx);
					data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR;
					data->stack_walk_data.stack_contents = &data->stack_contents;
					data->stack_walk_data.top_frame = true;
					data->stack_walk_data.async_frame = false;
					data->stack_walk_data.safe_point_frame = false;
					data->stack_walk_data.runtime_invoke_frame = false;
					ep_stack_contents_reset (&data->stack_contents);
					mono_get_eh_callbacks ()->mono_walk_stack_with_state (eventpipe_sample_profiler_walk_managed_stack_for_thread_func, thread_state, MONO_UNWIND_SIGNAL_SAFE, data);
					if (data->payload_data == EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL && (data->stack_walk_data.safe_point_frame || data->stack_walk_data.runtime_invoke_frame)) {
						// If classified as external code (managed->native frame on top of stack), but have a safe point or runtime invoke frame
						// as second, re-classify current callstack to be executing managed code.
						data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED;
					}

					sampled_thread_count++;
				}
			}
		}
		filtered_thread_count++;
	} FOREACH_THREAD_SAFE_END

	mono_thread_info_set_is_async_context (async_context);
	mono_restart_world (MONO_THREAD_INFO_FLAGS_NO_GC);

	// Fire sample event for threads. Must be done after runtime is resumed since it's not async safe.
	// Since we can't keep thread info around after runtime as been suspended, use an empty
	// adapter instance and only set recorded tid as parameter inside adapter.
	THREAD_INFO_TYPE adapter = { { 0 } };
	for (uint32_t i = 0; i < sampled_thread_count; ++i) {
		EventPipeSampleProfileStackWalkData *data = &g_array_index (_ep_rt_mono_sampled_thread_callstacks, EventPipeSampleProfileStackWalkData, i);
		if (data->payload_data != EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR && ep_stack_contents_get_length(&data->stack_contents) > 0) {
			// Check if we have an async frame, if so we will need to make sure all frames are registered in regular jit info table.
			// TODO: An async frame can contain wrapper methods (no way to check during stackwalk), we could skip writing profile event
			// for this specific stackwalk or we could cleanup stack_frames before writing profile event.
			if (data->stack_walk_data.async_frame) {
				for (int i = 0; i < data->stack_contents.next_available_frame; ++i)
					mono_jit_info_table_find_internal ((gpointer)data->stack_contents.stack_frames [i], TRUE, FALSE);
			}
			mono_thread_info_set_tid (&adapter, ep_rt_uint64_t_to_thread_id_t (data->thread_id));
			ep_write_sample_profile_event (sampling_thread, sampling_event, &adapter, &data->stack_contents, (uint8_t *)&data->payload_data, sizeof (data->payload_data));
		}
	}

	// Current thread count will be our next maximum sampled threads.
	_ep_rt_mono_max_sampled_thread_count = filtered_thread_count;

	return true;
}

void
ep_rt_mono_execute_rundown (ep_rt_execution_checkpoint_array_t *execution_checkpoints)
{
	ep_char8_t runtime_module_path [256];
	const uint8_t object_guid [EP_GUID_SIZE] = { 0 };
	const uint16_t runtime_product_qfe_version = 0;
	const uint32_t startup_flags = 0;
	const uint8_t startup_mode = 0;
	const ep_char8_t *command_line = "";

	if (!g_module_address ((void *)mono_init, runtime_module_path, sizeof (runtime_module_path), NULL, NULL, 0, NULL))
		runtime_module_path [0] = '\0';

	FireEtwRuntimeInformationDCStart (
		clr_instance_get_id (),
		RUNTIME_SKU_CORECLR,
		RuntimeProductMajorVersion,
		RuntimeProductMinorVersion,
		RuntimeProductPatchVersion,
		runtime_product_qfe_version,
		RuntimeFileMajorVersion,
		RuntimeFileMajorVersion,
		RuntimeFileBuildVersion,
		RuntimeFileRevisionVersion,
		startup_mode,
		startup_flags,
		command_line,
		object_guid,
		runtime_module_path,
		NULL,
		NULL);

	if (execution_checkpoints) {
		ep_rt_execution_checkpoint_array_iterator_t execution_checkpoints_iterator = ep_rt_execution_checkpoint_array_iterator_begin (execution_checkpoints);
		while (!ep_rt_execution_checkpoint_array_iterator_end (execution_checkpoints, &execution_checkpoints_iterator)) {
			EventPipeExecutionCheckpoint *checkpoint = ep_rt_execution_checkpoint_array_iterator_value (&execution_checkpoints_iterator);
			FireEtwExecutionCheckpointDCEnd (
				clr_instance_get_id (),
				checkpoint->name,
				checkpoint->timestamp,
				NULL,
				NULL);
			ep_rt_execution_checkpoint_array_iterator_next (&execution_checkpoints_iterator);
		}
	}

	FireEtwDCEndInit_V1 (
		clr_instance_get_id (),
		NULL,
		NULL);

	eventpipe_execute_rundown (
		fire_domain_rundown_events_func,
		fire_assembly_rundown_events_func,
		fire_method_rundown_events_func);

	FireEtwDCEndComplete_V1 (
		clr_instance_get_id (),
		NULL,
		NULL);
}

bool
ep_rt_mono_write_event_ee_startup_start (void)
{
	return FireEtwEEStartupStart_V1 (
		clr_instance_get_id (),
		NULL,
		NULL);
}

bool
ep_rt_mono_write_event_jit_start (MonoMethod *method)
{
	if (!EventEnabledMethodJittingStarted_V1 ())
		return true;

	//TODO: Optimize string formatting into functions accepting GString to reduce heap alloc.
	if (method) {
		uint64_t method_id = 0;
		uint64_t module_id = 0;
		uint32_t code_size = 0;
		uint32_t method_token = 0;
		char *method_namespace = NULL;
		const char *method_name = NULL;
		char *method_signature = NULL;

		//TODO: SendMethodDetailsEvent

		method_id = (uint64_t)method;

		if (!method->dynamic)
			method_token = method->token;

		if (!mono_method_has_no_body (method)) {
			ERROR_DECL (error);
			MonoMethodHeader *header = mono_method_get_header_internal (method, error);
			if (header)
				code_size = header->code_size;
		}

		method_name = method->name;
		method_signature = mono_signature_full_name (method->signature);

		if (method->klass) {
			module_id = (uint64_t)m_class_get_image (method->klass);
			method_namespace = mono_type_get_name_full (m_class_get_byval_arg (method->klass), MONO_TYPE_NAME_FORMAT_IL);
		}

		FireEtwMethodJittingStarted_V1 (
			method_id,
			module_id,
			method_token,
			code_size,
			method_namespace,
			method_name,
			method_signature,
			clr_instance_get_id (),
			NULL,
			NULL);

		g_free (method_namespace);
		g_free (method_signature);
	}

	return true;
}

bool
ep_rt_mono_write_event_method_il_to_native_map (
	MonoMethod *method,
	MonoJitInfo *ji)
{
	if (!EventEnabledMethodILToNativeMap ())
		return true;

	if (method) {
		// Under netcore we only have root domain.
		MonoDomain *root_domain = mono_get_root_domain ();

		uint64_t method_id = (uint64_t)method;
		uint32_t fixed_buffer [64];
		uint8_t *buffer = NULL;

		uint16_t offset_entries = 0;
		uint32_t *il_offsets = NULL;
		uint32_t *native_offsets = NULL;

		MonoDebugMethodJitInfo *debug_info = method ? mono_debug_find_method (method, root_domain) : NULL;
		if (debug_info) {
			if (offset_entries != 0) {
				offset_entries = debug_info->num_line_numbers;
				size_t needed_size = (offset_entries * sizeof (uint32_t) * 2);
				if (needed_size > sizeof (fixed_buffer)) {
					buffer = g_new (uint8_t, needed_size);
					il_offsets = (uint32_t*)buffer;
				} else {
					il_offsets = fixed_buffer;
				}
				if (il_offsets) {
					native_offsets = il_offsets + offset_entries;
					for (int offset_count = 0; offset_count < offset_entries; ++offset_count) {
						il_offsets [offset_count] = debug_info->line_numbers [offset_count].il_offset;
						native_offsets [offset_count] = debug_info->line_numbers [offset_count].native_offset;
					}
				}
			}

			mono_debug_free_method_jit_info (debug_info);
		}

		if (!il_offsets && !native_offsets) {
			// No IL offset -> Native offset mapping available. Put all code on IL offset 0.
			EP_ASSERT (sizeof (fixed_buffer) >= sizeof (uint32_t) * 2);
			offset_entries = 1;
			il_offsets = fixed_buffer;
			native_offsets = il_offsets + offset_entries;
			il_offsets [0] = 0;
			native_offsets [0] = ji ? (uint32_t)ji->code_size : 0;
		}

		FireEtwMethodILToNativeMap (
			method_id,
			0,
			0,
			offset_entries,
			il_offsets,
			native_offsets,
			clr_instance_get_id (),
			NULL,
			NULL);

		g_free (buffer);
	}

	return true;
}

bool
ep_rt_mono_write_event_method_load (
	MonoMethod *method,
	MonoJitInfo *ji)
{
	if (!EventEnabledMethodLoad_V1 () && !EventEnabledMethodLoadVerbose_V1 ())
		return true;

	//TODO: Optimize string formatting into functions accepting GString to reduce heap alloc.
	if (method) {
		uint64_t method_id = 0;
		uint64_t module_id = 0;
		uint64_t method_code_start = ji ? (uint64_t)ji->code_start : 0;
		uint32_t method_code_size = ji ? (uint32_t)ji->code_size : 0;
		uint32_t method_token = 0;
		uint32_t method_flags = 0;
		uint8_t kind = MONO_CLASS_DEF;
		char *method_namespace = NULL;
		const char *method_name = NULL;
		char *method_signature = NULL;
		bool verbose = (MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context.Level >= (uint8_t)EP_EVENT_LEVEL_VERBOSE);

		method_id = (uint64_t)method;

		if (!method->dynamic)
			method_token = method->token;

		if (ji && mono_jit_info_get_generic_sharing_context (ji)) {
			method_flags |= METHOD_FLAGS_SHARED_GENERIC_METHOD;
			verbose = true;
		}

		if (method->dynamic) {
			method_flags |= METHOD_FLAGS_DYNAMIC_METHOD;
			verbose = true;
		}

		if (ji && !ji->from_aot && !ji->from_llvm) {
			method_flags |= METHOD_FLAGS_JITTED_METHOD;
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				method_flags |= METHOD_FLAGS_JITTED_HELPER_METHOD;
		}

		if (method->is_generic || method->is_inflated) {
			method_flags |= METHOD_FLAGS_GENERIC_METHOD;
			verbose = true;
		}

		if (method->klass) {
			module_id = (uint64_t)m_class_get_image (method->klass);
			kind = m_class_get_class_kind (method->klass);
			if (kind == MONO_CLASS_GTD || kind == MONO_CLASS_GINST)
				method_flags |= METHOD_FLAGS_GENERIC_METHOD;
		}

		//TODO: SendMethodDetailsEvent

		if (verbose) {
			method_name = method->name;
			method_signature = mono_signature_full_name (method->signature);

			if (method->klass)
				method_namespace = mono_type_get_name_full (m_class_get_byval_arg (method->klass), MONO_TYPE_NAME_FORMAT_IL);

			FireEtwMethodLoadVerbose_V1 (
				method_id,
				module_id,
				method_code_start,
				method_code_size,
				method_token,
				method_flags | METHOD_FLAGS_EXTENT_HOT_SECTION,
				method_namespace,
				method_name,
				method_signature,
				clr_instance_get_id (),
				NULL,
				NULL);

			if (ji && (ji->from_aot || ji->from_llvm))
				FireEtwMethodLoadVerbose_V1 (
					method_id,
					module_id,
					method_code_start,
					method_code_size,
					method_token,
					method_flags | METHOD_FLAGS_EXTENT_COLD_SECTION,
					method_namespace,
					method_name,
					method_signature,
					clr_instance_get_id (),
					NULL,
					NULL);
		} else {
			FireEtwMethodLoad_V1 (
				method_id,
				module_id,
				method_code_start,
				method_code_size,
				method_token,
				method_flags | METHOD_FLAGS_EXTENT_HOT_SECTION,
				clr_instance_get_id (),
				NULL,
				NULL);

			if (ji && (ji->from_aot || ji->from_llvm))
				FireEtwMethodLoad_V1 (
					method_id,
					module_id,
					method_code_start,
					method_code_size,
					method_token,
					method_flags | METHOD_FLAGS_EXTENT_COLD_SECTION,
					clr_instance_get_id (),
					NULL,
					NULL);
		}

		g_free (method_namespace);
		g_free (method_signature);
	}

	return true;
}

static
bool
get_module_event_data (
	MonoImage *image,
	ModuleEventData *module_data)
{
	if (image && module_data) {
		memset (module_data->signature, 0, EP_GUID_SIZE);

		// Under netcore we only have root domain.
		MonoDomain *root_domain = mono_get_root_domain ();

		module_data->domain_id = (uint64_t)root_domain;
		module_data->module_id = (uint64_t)image;
		module_data->assembly_id = (uint64_t)image->assembly;

		// TODO: Extract all module IL/Native paths and pdb metadata when available.
		module_data->module_il_path = "";
		module_data->module_il_pdb_path = "";
		module_data->module_native_path = "";
		module_data->module_native_pdb_path = "";

		module_data->module_il_pdb_age = 0;
		module_data->module_native_pdb_age = 0;

		module_data->reserved_flags = 0;

		// Netcore has a 1:1 between assemblies and modules, so its always a manifest module.
		module_data->module_flags = MODULE_FLAGS_MANIFEST_MODULE;
		if (image->dynamic)
			module_data->module_flags |= MODULE_FLAGS_DYNAMIC_MODULE;
		if (image->aot_module)
			module_data->module_flags |= MODULE_FLAGS_NATIVE_MODULE;

		module_data->module_il_path = image->filename ? image->filename : "";
	}

	return true;
}

bool
ep_rt_mono_write_event_module_load (MonoImage *image)
{
	if (!EventEnabledModuleLoad_V2 () && !EventEnabledDomainModuleLoad_V1 ())
		return true;

	if (image) {
		ModuleEventData module_data;
		if (get_module_event_data (image, &module_data)) {
			FireEtwModuleLoad_V2 (
				module_data.module_id,
				module_data.assembly_id,
				module_data.module_flags,
				module_data.reserved_flags,
				module_data.module_il_path,
				module_data.module_native_path,
				clr_instance_get_id (),
				module_data.signature,
				module_data.module_il_pdb_age,
				module_data.module_il_pdb_path,
				module_data.signature,
				module_data.module_native_pdb_age,
				module_data.module_native_pdb_path,
				NULL,
				NULL);

			FireEtwDomainModuleLoad_V1 (
				module_data.module_id,
				module_data.assembly_id,
				module_data.domain_id,
				module_data.module_flags,
				module_data.reserved_flags,
				module_data.module_il_path,
				module_data.module_native_path,
				clr_instance_get_id (),
				NULL,
				NULL);
		}
	}

	return true;
}

bool
ep_rt_mono_write_event_module_unload (MonoImage *image)
{
	if (!EventEnabledModuleUnload_V2())
		return true;

	if (image) {
		ModuleEventData module_data;
		if (get_module_event_data (image, &module_data)) {
			FireEtwModuleUnload_V2 (
				module_data.module_id,
				module_data.assembly_id,
				module_data.module_flags,
				module_data.reserved_flags,
				module_data.module_il_path,
				module_data.module_native_path,
				clr_instance_get_id (),
				module_data.signature,
				module_data.module_il_pdb_age,
				module_data.module_il_pdb_path,
				module_data.signature,
				module_data.module_native_pdb_age,
				module_data.module_native_pdb_path,
				NULL,
				NULL);
		}
	}

	return true;
}

static
bool
get_assembly_event_data (
	MonoAssembly *assembly,
	AssemblyEventData *assembly_data)
{
	if (assembly && assembly_data) {
		// Under netcore we only have root domain.
		MonoDomain *root_domain = mono_get_root_domain ();

		assembly_data->domain_id = (uint64_t)root_domain;
		assembly_data->assembly_id = (uint64_t)assembly;
		assembly_data->binding_id = 0;

		assembly_data->assembly_flags = 0;
		if (assembly->dynamic)
			assembly_data->assembly_flags |= ASSEMBLY_FLAGS_DYNAMIC_ASSEMBLY;

		if (assembly->image && assembly->image->aot_module)
			assembly_data->assembly_flags |= ASSEMBLY_FLAGS_NATIVE_ASSEMBLY;

		assembly_data->assembly_name = mono_stringify_assembly_name (&assembly->aname);
	}

	return true;
}

bool
ep_rt_mono_write_event_assembly_load (MonoAssembly *assembly)
{
	if (!EventEnabledAssemblyLoad_V1 ())
		return true;

	if (assembly) {
		AssemblyEventData assembly_data;
		if (get_assembly_event_data (assembly, &assembly_data)) {
			FireEtwAssemblyLoad_V1 (
				assembly_data.assembly_id,
				assembly_data.domain_id,
				assembly_data.binding_id,
				assembly_data.assembly_flags,
				assembly_data.assembly_name,
				clr_instance_get_id (),
				NULL,
				NULL);

			g_free (assembly_data.assembly_name);
		}
	}

	return true;
}

bool
ep_rt_mono_write_event_assembly_unload (MonoAssembly *assembly)
{
	if (!EventEnabledAssemblyUnload_V1 ())
		return true;

	if (assembly) {
		AssemblyEventData assembly_data;
		if (get_assembly_event_data (assembly, &assembly_data)) {
			FireEtwAssemblyUnload_V1 (
				assembly_data.assembly_id,
				assembly_data.domain_id,
				assembly_data.binding_id,
				assembly_data.assembly_flags,
				assembly_data.assembly_name,
				clr_instance_get_id (),
				NULL,
				NULL);

			g_free (assembly_data.assembly_name);
		}
	}

	return true;
}

bool
ep_rt_mono_write_event_thread_created (ep_rt_thread_id_t tid)
{
	if (!EventEnabledThreadCreated ())
		return true;

	uint64_t managed_thread = 0;
	uint64_t native_thread_id = ep_rt_thread_id_t_to_uint64_t (tid);
	uint64_t managed_thread_id = 0;
	uint32_t flags = 0;

	MonoThread *thread = mono_thread_current ();
	if (thread && mono_thread_info_get_tid (thread->thread_info) == tid) {
		managed_thread_id = (uint64_t)mono_thread_get_managed_id (thread);
		managed_thread = (uint64_t)thread;

		switch (mono_thread_info_get_flags (thread->thread_info)) {
		case MONO_THREAD_INFO_FLAGS_NO_GC:
		case MONO_THREAD_INFO_FLAGS_NO_SAMPLE:
			flags |= THREAD_FLAGS_GC_SPECIAL;
		}

		if (mono_gc_is_finalizer_thread (thread))
			flags |= THREAD_FLAGS_FINALIZER;

		if (thread->threadpool_thread)
			flags |= THREAD_FLAGS_THREADPOOL_WORKER;
	}

	FireEtwThreadCreated (
		managed_thread,
		(uint64_t)mono_get_root_domain (),
		flags,
		managed_thread_id,
		native_thread_id,
		clr_instance_get_id (),
		NULL,
		NULL);

	return true;
}

bool
ep_rt_mono_write_event_thread_terminated (ep_rt_thread_id_t tid)
{
	if (!EventEnabledThreadTerminated ())
		return true;

	uint64_t managed_thread = 0;
	MonoThread *thread = mono_thread_current ();
	if (thread && mono_thread_info_get_tid (thread->thread_info) == tid)
		managed_thread = (uint64_t)thread;

	FireEtwThreadTerminated (
		managed_thread,
		(uint64_t)mono_get_root_domain (),
		clr_instance_get_id (),
		NULL,
		NULL);

	return true;
}

static
uint32_t
get_type_start_id (MonoType *type)
{
	uint32_t start_id = (uint32_t)(uintptr_t)type;

	start_id = (((start_id * 215497) >> 16) ^ ((start_id * 1823231) + start_id));

	// Mix in highest bits on 64-bit systems only
	if (sizeof (type) > 4)
		start_id = start_id ^ (((uint64_t)type >> 31) >> 1);

	return start_id;
}

bool
ep_rt_mono_write_event_type_load_start (MonoType *type)
{
	if (!EventEnabledTypeLoadStart ())
		return true;

	FireEtwTypeLoadStart (
		get_type_start_id (type),
		clr_instance_get_id (),
		NULL,
		NULL);

	return true;
}

bool
ep_rt_mono_write_event_type_load_stop (MonoType *type)
{
	if (!EventEnabledTypeLoadStop ())
		return true;

	char *type_name = NULL;
	if (type)
		type_name = mono_type_get_name_full (type, MONO_TYPE_NAME_FORMAT_IL);

	FireEtwTypeLoadStop (
		get_type_start_id (type),
		clr_instance_get_id (),
		6 /* CLASS_LOADED */,
		(uint64_t)type,
		type_name,
		NULL,
		NULL);

	g_free (type_name);

	return true;
}

static
gboolean
get_exception_ip_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data)
{
	*(uintptr_t *)data = (uintptr_t)MONO_CONTEXT_GET_IP (ctx);
	return TRUE;
}

bool
ep_rt_mono_write_event_exception_thrown (MonoObject *obj)
{
	if (!EventEnabledExceptionThrown_V1 ())
		return true;

	if (obj) {
		ERROR_DECL (error);
		char *type_name = NULL;
		char *exception_message = NULL;
		uint16_t flags = 0;
		uint32_t hresult = 0;
		uintptr_t ip = 0;

		if (mono_object_isinst_checked ((MonoObject *) obj, mono_get_exception_class (), error)) {
			MonoException *exception = (MonoException *)obj;
			flags |= EXCEPTION_THROWN_FLAGS_IS_CLS_COMPLIANT;
			if (exception->inner_ex)
				flags |= EXCEPTION_THROWN_FLAGS_HAS_INNER;
			exception_message = ep_rt_utf16_to_utf8_string (mono_string_chars_internal (exception->message), mono_string_length_internal (exception->message));
			hresult = exception->hresult;
		}

		if (mono_get_eh_callbacks ()->mono_walk_stack_with_ctx)
			mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (get_exception_ip_func, NULL, MONO_UNWIND_SIGNAL_SAFE, (void *)&ip);

		type_name = mono_type_get_name_full (m_class_get_byval_arg (mono_object_class (obj)), MONO_TYPE_NAME_FORMAT_IL);

		FireEtwExceptionThrown_V1 (
			type_name,
			exception_message,
			(void *)&ip,
			hresult,
			flags,
			clr_instance_get_id (),
			NULL,
			NULL);

		if (!mono_component_profiler_clauses_enabled ()) {
			FireEtwExceptionThrownStop (
				NULL,
				NULL);
		}

		g_free (exception_message);
		g_free (type_name);

		mono_error_cleanup (error);
	}

	return true;
}

bool
ep_rt_mono_write_event_exception_clause (
	MonoMethod *method,
	uint32_t clause_num,
	MonoExceptionEnum clause_type,
	MonoObject *obj)
{
	if (!mono_component_profiler_clauses_enabled ())
		return true;

	if ((clause_type == MONO_EXCEPTION_CLAUSE_FAULT || clause_type == MONO_EXCEPTION_CLAUSE_NONE) && (!EventEnabledExceptionCatchStart() || !EventEnabledExceptionCatchStop()))
		return true;

	if (clause_type == MONO_EXCEPTION_CLAUSE_FILTER && (!EventEnabledExceptionFilterStart() || !EventEnabledExceptionFilterStop()))
		return true;

	if (clause_type == MONO_EXCEPTION_CLAUSE_FINALLY && (!EventEnabledExceptionFinallyStart() || !EventEnabledExceptionFinallyStop()))
		return true;

	uintptr_t ip = 0; //TODO: Have profiler pass along IP of handler block.
	uint64_t method_id = (uint64_t)method;
	char *method_name = NULL;

	method_name = mono_method_get_name_full (method, TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL);

	if ((clause_type == MONO_EXCEPTION_CLAUSE_FAULT || clause_type == MONO_EXCEPTION_CLAUSE_NONE)) {
		FireEtwExceptionCatchStart (
			(uint64_t)ip,
			method_id,
			(const ep_char8_t *)method_name,
			clr_instance_get_id (),
			NULL,
			NULL);

		FireEtwExceptionCatchStop (
			NULL,
			NULL);

		FireEtwExceptionThrownStop (
			NULL,
			NULL);
	}

	if (clause_type == MONO_EXCEPTION_CLAUSE_FILTER) {
		FireEtwExceptionFilterStart (
			(uint64_t)ip,
			method_id,
			(const ep_char8_t *)method_name,
			clr_instance_get_id (),
			NULL,
			NULL);

		FireEtwExceptionFilterStop (
			NULL,
			NULL);
	}

	if (clause_type == MONO_EXCEPTION_CLAUSE_FINALLY) {
		FireEtwExceptionFinallyStart (
			(uint64_t)ip,
			method_id,
			(const ep_char8_t *)method_name,
			clr_instance_get_id (),
			NULL,
			NULL);

		FireEtwExceptionFinallyStop (
			NULL,
			NULL);
	}

	g_free (method_name);
	return true;
}

bool
ep_rt_mono_write_event_monitor_contention_start (MonoObject *obj)
{
	if (!EventEnabledContentionStart_V1 ())
		return true;

	FireEtwContentionStart_V1 (
		0 /* ManagedContention */,
		clr_instance_get_id (),
		NULL,
		NULL);

	return true;
}

bool
ep_rt_mono_write_event_monitor_contention_stop (MonoObject *obj)
{
	if (!EventEnabledContentionStop ())
		return true;

	FireEtwContentionStop (
		0 /* ManagedContention */,
		clr_instance_get_id (),
		NULL,
		NULL);

	return true;
}

bool
ep_rt_mono_write_event_method_jit_memory_allocated_for_code (
	const uint8_t *buffer,
	uint64_t size,
	MonoProfilerCodeBufferType type,
	const void *data)
{
	if (!EventEnabledMethodJitMemoryAllocatedForCode ())
		return true;

	if (type != MONO_PROFILER_CODE_BUFFER_METHOD)
		return true;

	uint64_t method_id = 0;
	uint64_t module_id = 0;

	if (data) {
		MonoMethod *method;
		method = (MonoMethod *)data;
		method_id = (uint64_t)method;
		if (method->klass)
			module_id = (uint64_t)(uint64_t)m_class_get_image (method->klass);
	}

	FireEtwMethodJitMemoryAllocatedForCode (
		method_id,
		module_id,
		size,
		0,
		size,
		0 /* CORJIT_ALLOCMEM_DEFAULT_CODE_ALIGN */,
		clr_instance_get_id (),
		NULL,
		NULL);

	return true;
}

bool
ep_rt_write_event_threadpool_worker_thread_start (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolWorkerThreadStart (
		active_thread_count,
		retired_worker_thread_count,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_threadpool_worker_thread_stop (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolWorkerThreadStop (
		active_thread_count,
		retired_worker_thread_count,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_threadpool_worker_thread_wait (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolWorkerThreadWait (
		active_thread_count,
		retired_worker_thread_count,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_threadpool_worker_thread_adjustment_sample (
	double throughput,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolWorkerThreadAdjustmentSample (
		throughput,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_threadpool_worker_thread_adjustment_adjustment (
	double average_throughput,
	uint32_t networker_thread_count,
	/*NativeRuntimeEventSource.ThreadAdjustmentReasonMap*/ int32_t reason,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolWorkerThreadAdjustmentAdjustment (
		average_throughput,
		networker_thread_count,
		reason,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_threadpool_worker_thread_adjustment_stats (
	double duration,
	double throughput,
	double threadpool_worker_thread_wait,
	double throughput_wave,
	double throughput_error_estimate,
	double average_throughput_error_estimate,
	double throughput_ratio,
	double confidence,
	double new_control_setting,
	uint16_t new_thread_wave_magnitude,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolWorkerThreadAdjustmentStats (
		duration,
		throughput,
		threadpool_worker_thread_wait,
		throughput_wave,
		throughput_error_estimate,
		average_throughput_error_estimate,
		throughput_ratio,
		confidence,
		new_control_setting,
		new_thread_wave_magnitude,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_threadpool_io_enqueue (
	intptr_t native_overlapped,
	intptr_t overlapped,
	bool multi_dequeues,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolIOEnqueue (
		(const void *)native_overlapped,
		(const void *)overlapped,
		multi_dequeues,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_threadpool_io_dequeue (
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolIODequeue (
		(const void *)native_overlapped,
		(const void *)overlapped,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_threadpool_working_thread_count (
	uint16_t count,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolWorkingThreadCount (
		count,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

static
void
runtime_profiler_jit_begin (
	MonoProfiler *prof,
	MonoMethod *method)
{
	ep_rt_mono_write_event_jit_start (method);
}

static
void
runtime_profiler_jit_failed (
	MonoProfiler *prof,
	MonoMethod *method)
{
	//TODO: CoreCLR doesn't have this case, so no failure event currently exists.
}

static
void
runtime_profiler_jit_done (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoJitInfo *ji)
{
	ep_rt_mono_write_event_method_load (method, ji);
	ep_rt_mono_write_event_method_il_to_native_map (method, ji);
}

static
void
runtime_profiler_image_loaded (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (image && image->heap_pdb.size == 0)
		ep_rt_mono_write_event_module_load (image);
}

static
void
runtime_profiler_image_unloaded (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (image && image->heap_pdb.size == 0)
		ep_rt_mono_write_event_module_unload (image);
}

static
void
runtime_profiler_assembly_loaded (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	ep_rt_mono_write_event_assembly_load (assembly);
}

static
void
runtime_profiler_assembly_unloaded (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	ep_rt_mono_write_event_assembly_unload (assembly);
}

static
void
runtime_profiler_thread_started (
	MonoProfiler *prof,
	uintptr_t tid)
{
	ep_rt_mono_write_event_thread_created (ep_rt_uint64_t_to_thread_id_t (tid));
}

static
void
runtime_profiler_thread_stopped (
	MonoProfiler *prof,
	uintptr_t tid)
{
	ep_rt_mono_write_event_thread_terminated (ep_rt_uint64_t_to_thread_id_t (tid));
}

static
void
runtime_profiler_class_loading (
	MonoProfiler *prof,
	MonoClass *klass)
{
	ep_rt_mono_write_event_type_load_start (m_class_get_byval_arg (klass));
}

static
void
runtime_profiler_class_failed (
	MonoProfiler *prof,
	MonoClass *klass)
{
	ep_rt_mono_write_event_type_load_stop (m_class_get_byval_arg (klass));
}

static
void
runtime_profiler_class_loaded (
	MonoProfiler *prof,
	MonoClass *klass)
{
	ep_rt_mono_write_event_type_load_stop (m_class_get_byval_arg (klass));
}

static
void
runtime_profiler_exception_throw (
	MonoProfiler *prof,
	MonoObject *exc)
{
	ep_rt_mono_write_event_exception_thrown (exc);
}

static
void
runtime_profiler_exception_clause (
	MonoProfiler *prof,
	MonoMethod *method,
	uint32_t clause_num,
	MonoExceptionEnum clause_type,
	MonoObject *exc)
{
	ep_rt_mono_write_event_exception_clause (method, clause_num, clause_type, exc);
}

static
void
runtime_profiler_monitor_contention (
	MonoProfiler *prof,
	MonoObject *obj)
{
	ep_rt_mono_write_event_monitor_contention_start (obj);
}

static
void
runtime_profiler_monitor_acquired (
	MonoProfiler *prof,
	MonoObject *obj)
{
	ep_rt_mono_write_event_monitor_contention_stop (obj);
}

static
void
runtime_profiler_monitor_failed (
	MonoProfiler *prof,
	MonoObject *obj)
{
	ep_rt_mono_write_event_monitor_contention_stop (obj);
}

static
void
runtime_profiler_jit_code_buffer (
	MonoProfiler *prof,
	const mono_byte *buffer,
	uint64_t size,
	MonoProfilerCodeBufferType type,
	const void *data)
{
	ep_rt_mono_write_event_method_jit_memory_allocated_for_code ((const uint8_t *)buffer, size, type, data);
}

void
EventPipeEtwCallbackDotNETRuntime (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	ep_rt_config_requires_lock_not_held ();

	EP_ASSERT(is_enabled == 0 || is_enabled == 1) ;
	EP_ASSERT (_ep_rt_dotnet_runtime_profiler_provider != NULL);

	match_any_keywords = (is_enabled == 1) ? match_any_keywords : 0;

	EP_LOCK_ENTER (section1)
		uint64_t enabled_keywords = MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context.EnabledKeywordsBitmask;

		if (profiler_callback_is_enabled(match_any_keywords, JIT_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, JIT_KEYWORD)) {
				mono_profiler_set_jit_begin_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_jit_begin);
				mono_profiler_set_jit_failed_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_jit_failed);
				mono_profiler_set_jit_done_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_jit_done);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, JIT_KEYWORD)) {
				mono_profiler_set_jit_begin_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_jit_failed_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_jit_done_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, LOADER_KEYWORD)) {
			if (!profiler_callback_is_enabled(enabled_keywords, LOADER_KEYWORD)) {
				mono_profiler_set_image_loaded_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_image_loaded);
				mono_profiler_set_image_unloaded_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_image_unloaded);
				mono_profiler_set_assembly_loaded_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_assembly_loaded);
				mono_profiler_set_assembly_unloaded_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_assembly_unloaded);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, LOADER_KEYWORD)) {
				mono_profiler_set_image_loaded_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_image_unloaded_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_assembly_loaded_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_assembly_unloaded_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, APP_DOMAIN_RESOURCE_MANAGEMENT_KEYWORD | THREADING_KEYWORD)) {
			if (!profiler_callback_is_enabled(enabled_keywords, APP_DOMAIN_RESOURCE_MANAGEMENT_KEYWORD | THREADING_KEYWORD)) {
				mono_profiler_set_thread_started_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_thread_started);
				mono_profiler_set_thread_stopped_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_thread_stopped);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, APP_DOMAIN_RESOURCE_MANAGEMENT_KEYWORD | THREADING_KEYWORD)) {
				mono_profiler_set_thread_started_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_thread_stopped_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, TYPE_DIAGNOSTIC_KEYWORD)) {
			if (!profiler_callback_is_enabled(enabled_keywords, TYPE_DIAGNOSTIC_KEYWORD)) {
				mono_profiler_set_class_loading_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_class_loading);
				mono_profiler_set_class_failed_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_class_failed);
				mono_profiler_set_class_loaded_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_class_loaded);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, TYPE_DIAGNOSTIC_KEYWORD)) {
				mono_profiler_set_class_loading_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_class_failed_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_class_loaded_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, EXCEPTION_KEYWORD)) {
			if (!profiler_callback_is_enabled(enabled_keywords, EXCEPTION_KEYWORD)) {
				mono_profiler_set_exception_throw_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_exception_throw);
				mono_profiler_set_exception_clause_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_exception_clause);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, EXCEPTION_KEYWORD)) {
				mono_profiler_set_exception_throw_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_exception_clause_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, CONTENTION_KEYWORD)) {
			if (!profiler_callback_is_enabled(enabled_keywords, CONTENTION_KEYWORD)) {
				mono_profiler_set_monitor_contention_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_monitor_contention);
				mono_profiler_set_monitor_acquired_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_monitor_acquired);
				mono_profiler_set_monitor_failed_callback (_ep_rt_dotnet_runtime_profiler_provider, runtime_profiler_monitor_failed);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, CONTENTION_KEYWORD)) {
				mono_profiler_set_monitor_contention_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_monitor_acquired_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
				mono_profiler_set_monitor_failed_callback (_ep_rt_dotnet_runtime_profiler_provider, NULL);
			}
		}

		MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context.Level = level;
		MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context.EnabledKeywordsBitmask = match_any_keywords;
		MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context.IsEnabled = (is_enabled == 1 ? true : false);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
EventPipeEtwCallbackDotNETRuntimeRundown (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context.Level = level;
	MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context.EnabledKeywordsBitmask = match_any_keywords;
	MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context.IsEnabled = (is_enabled == 1 ? true : false);
}

void
EventPipeEtwCallbackDotNETRuntimePrivate (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context.Level = level;
	MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context.EnabledKeywordsBitmask = match_any_keywords;
	MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context.IsEnabled = (is_enabled == 1 ? true : false);
}

void
EventPipeEtwCallbackDotNETRuntimeStress (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_EVENTPIPE_Context.Level = level;
	MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_EVENTPIPE_Context.EnabledKeywordsBitmask = match_any_keywords;
	MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_EVENTPIPE_Context.IsEnabled = (is_enabled == 1 ? true : false);
}

static
void
mono_profiler_app_domain_loading (
	MonoProfiler *prof,
	MonoDomain *domain)
{
	if (!EventEnabledMonoProfilerAppDomainLoading ())
		return;

	uint64_t domain_id = (uint64_t)domain;
	FireEtwMonoProfilerAppDomainLoading (
		domain_id,
		NULL,
		NULL);
}

static
void
mono_profiler_app_domain_loaded (
	MonoProfiler *prof,
	MonoDomain *domain)
{
	if (!EventEnabledMonoProfilerAppDomainLoaded ())
		return;

	uint64_t domain_id = (uint64_t)domain;
	FireEtwMonoProfilerAppDomainLoaded (
		domain_id,
		NULL,
		NULL);
}

static
void
mono_profiler_app_domain_unloading (
	MonoProfiler *prof,
	MonoDomain *domain)
{
	if (!EventEnabledMonoProfilerAppDomainUnloading ())
		return;

	uint64_t domain_id = (uint64_t)domain;
	FireEtwMonoProfilerAppDomainUnloading (
		domain_id,
		NULL,
		NULL);
}

static
void
mono_profiler_app_domain_unloaded (
	MonoProfiler *prof,
	MonoDomain *domain)
{
	if (!EventEnabledMonoProfilerAppDomainUnloaded ())
		return;

	uint64_t domain_id = (uint64_t)domain;
	FireEtwMonoProfilerAppDomainUnloaded (
		domain_id,
		NULL,
		NULL);
}

static
void
mono_profiler_app_domain_name (
	MonoProfiler *prof,
	MonoDomain *domain,
	const char *name)
{
	if (!EventEnabledMonoProfilerAppDomainName ())
		return;

	uint64_t domain_id = (uint64_t)domain;
	FireEtwMonoProfilerAppDomainName (
		domain_id,
		(const ep_char8_t *)(name ? name : ""),
		NULL,
		NULL);
}

static
void
mono_profiler_get_generic_types (
	MonoGenericInst *generic_instance,
	uint32_t *generic_type_count,
	uint8_t **generic_types)
{
	if (generic_instance) {
		uint8_t *buffer = g_malloc (generic_instance->type_argc * (sizeof (uint8_t) + sizeof (uint64_t)));
		if (buffer) {
			*generic_types = buffer;
			*generic_type_count = generic_instance->type_argc;
			for (uint32_t i = 0; i < generic_instance->type_argc; ++i) {
				uint8_t type = generic_instance->type_argv [i]->type;
				memcpy (buffer, &type, sizeof (type));
				buffer += sizeof (type);

				uint64_t class_id = (uint64_t)mono_class_from_mono_type_internal (generic_instance->type_argv [i]);
				memcpy (buffer, &class_id, sizeof (class_id));
				buffer += sizeof (class_id);
			}
		}
	}
}

static
void
mono_profiler_get_jit_data (
	MonoMethod *method,
	uint64_t *method_id,
	uint64_t *module_id,
	uint32_t *method_token,
	uint32_t *method_generic_type_count,
	uint8_t **method_generic_types)
{
	*method_id = (uint64_t)method;
	*module_id = 0;
	*method_token = 0;

	if (method) {
		*method_token = method->token;
		if (method->klass)
			*module_id = (uint64_t)m_class_get_image (method->klass);

		if (method_generic_type_count && method_generic_types) {
			if (method->is_inflated) {
				MonoGenericContext *context = mono_method_get_context (method);
				MonoGenericInst *method_instance = (context && context->method_inst) ? context->method_inst : NULL;
				mono_profiler_get_generic_types (method_instance, method_generic_type_count, method_generic_types);
			}
		}
	}
}

static
void
mono_profiler_jit_begin (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerJitBegin())
		return;

	uint64_t method_id;
	uint64_t module_id;
	uint32_t method_token;

	mono_profiler_get_jit_data (method, &method_id, &module_id, &method_token, NULL, NULL);

	FireEtwMonoProfilerJitBegin (
		method_id,
		module_id,
		method_token,
		NULL,
		NULL);
}

static
void
mono_profiler_jit_failed (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerJitFailed())
		return;

	uint64_t method_id;
	uint64_t module_id;
	uint32_t method_token;

	mono_profiler_get_jit_data (method, &method_id, &module_id, &method_token, NULL, NULL);

	FireEtwMonoProfilerJitFailed (
		method_id,
		module_id,
		method_token,
		NULL,
		NULL);
}

static
void
mono_profiler_jit_done (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoJitInfo *ji)
{
	if (!EventEnabledMonoProfilerJitDone () && !EventEnabledMonoProfilerJitDone_V1 () && !EventEnabledMonoProfilerJitDoneVerbose ())
		return;

	bool verbose = (MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_EVENTPIPE_Context.Level >= (uint8_t)EP_EVENT_LEVEL_VERBOSE);

	uint64_t method_id;
	uint64_t module_id;
	uint32_t method_token;

	uint32_t method_generic_type_count = 0;
	uint8_t *method_generic_types = NULL;

	mono_profiler_get_jit_data (method, &method_id, &module_id, &method_token, &method_generic_type_count, &method_generic_types);

	FireEtwMonoProfilerJitDone_V1 (
		method_id,
		module_id,
		method_token,
		method_generic_type_count,
		sizeof (uint8_t) + sizeof (uint64_t),
		method_generic_types,
		NULL,
		NULL);

	g_free (method_generic_types);

	if (verbose) {
		//TODO: Optimize string formatting into functions accepting GString to reduce heap alloc.
		char *method_namespace = NULL;
		const char *method_name = method->name;
		char *method_signature = mono_signature_full_name (method->signature);
		if (method->klass)
			method_namespace = mono_type_get_name_full (m_class_get_byval_arg (method->klass), MONO_TYPE_NAME_FORMAT_IL);

		FireEtwMonoProfilerJitDoneVerbose (
			method_id,
			(const ep_char8_t *)method_namespace,
			(const ep_char8_t *)method_name,
			(const ep_char8_t *)method_signature,
			NULL,
			NULL);

		g_free (method_namespace);
		g_free (method_signature);
	}
}

static
void
mono_profiler_jit_chunk_created (
	MonoProfiler *prof,
	const mono_byte *chunk,
	uintptr_t size)
{
	if (!EventEnabledMonoProfilerJitChunkCreated())
		return;

	FireEtwMonoProfilerJitChunkCreated (
		chunk,
		(uint64_t)size,
		NULL,
		NULL);
}

static
void
mono_profiler_jit_chunk_destroyed (
	MonoProfiler *prof,
	const mono_byte *chunk)
{
	if (!EventEnabledMonoProfilerJitChunkDestroyed())
		return;

	FireEtwMonoProfilerJitChunkDestroyed (
		chunk,
		NULL,
		NULL);
}

static
void
mono_profiler_jit_code_buffer (
	MonoProfiler *prof,
	const mono_byte *buffer,
	uint64_t size,
	MonoProfilerCodeBufferType type,
	const void *data)
{
	if (!EventEnabledMonoProfilerJitCodeBuffer())
		return;

	FireEtwMonoProfilerJitCodeBuffer (
		buffer,
		size,
		(uint8_t)type,
		NULL,
		NULL);
}

static
void
mono_profiler_get_class_data (
	MonoClass *klass,
	uint64_t *class_id,
	uint64_t *module_id,
	ep_char8_t **class_name,
	uint32_t *class_generic_type_count,
	uint8_t **class_generic_types)
{
	*class_id = (uint64_t)klass;
	*module_id = 0;

	if (klass)
		*module_id = (uint64_t)m_class_get_image (klass);

	if (klass && class_name)
		*class_name = (ep_char8_t *)mono_type_get_name_full (m_class_get_byval_arg (klass), MONO_TYPE_NAME_FORMAT_IL);
	else if (class_name)
		*class_name = NULL;

	if (class_generic_type_count && class_generic_types) {
		if (mono_class_is_ginst (klass)) {
			MonoGenericContext *context = mono_class_get_context (klass);
			MonoGenericInst *class_instance = (context && context->class_inst) ? context->class_inst : NULL;
			mono_profiler_get_generic_types (class_instance, class_generic_type_count, class_generic_types);
		}
	}
}

static
void
mono_profiler_class_loading (
	MonoProfiler *prof,
	MonoClass *klass)
{
	if (!EventEnabledMonoProfilerClassLoading())
		return;

	uint64_t class_id;
	uint64_t module_id;
	
	mono_profiler_get_class_data (klass, &class_id, &module_id, NULL, NULL, NULL);

	FireEtwMonoProfilerClassLoading (
		class_id,
		module_id,
		NULL,
		NULL);
}

static
void
mono_profiler_class_failed (
	MonoProfiler *prof,
	MonoClass *klass)
{
	if (!EventEnabledMonoProfilerClassFailed())
		return;

	uint64_t class_id;
	uint64_t module_id;

	mono_profiler_get_class_data (klass, &class_id, &module_id, NULL, NULL, NULL);

	FireEtwMonoProfilerClassFailed (
		class_id,
		module_id,
		NULL,
		NULL);
}

static
void
mono_profiler_class_loaded (
	MonoProfiler *prof,
	MonoClass *klass)
{
	if (!EventEnabledMonoProfilerClassLoaded () && !EventEnabledMonoProfilerClassLoaded_V1 ())
		return;

	uint64_t class_id;
	uint64_t module_id;
	ep_char8_t *class_name;

	uint32_t class_generic_type_count = 0;
	uint8_t *class_generic_types = NULL;

	mono_profiler_get_class_data (klass, &class_id, &module_id, &class_name, &class_generic_type_count, &class_generic_types);

	FireEtwMonoProfilerClassLoaded_V1 (
		class_id,
		module_id,
		class_name ? class_name : "",
		class_generic_type_count,
		sizeof (uint8_t) + sizeof (uint64_t),
		class_generic_types,
		NULL,
		NULL);

	g_free (class_name);
	g_free (class_generic_types);
}

static
inline
void
get_vtable_data (
	MonoVTable *vtable,
	uint64_t *vtable_id,
	uint64_t *class_id,
	uint64_t *domain_id)
{
	*vtable_id = (uint64_t)vtable;
	*class_id = 0;
	*domain_id = 0;

	if (vtable) {
		*class_id = (uint64_t)mono_vtable_class_internal (vtable);
		*domain_id = (uint64_t)mono_vtable_domain_internal (vtable);
	}
}

static
void
mono_profiler_vtable_loading (
	MonoProfiler *prof,
	MonoVTable *vtable)
{
	if (!EventEnabledMonoProfilerVTableLoading())
		return;

	uint64_t vtable_id;
	uint64_t class_id;
	uint64_t domain_id;

	get_vtable_data (vtable, &vtable_id, &class_id, &domain_id);

	FireEtwMonoProfilerVTableLoading (
		vtable_id,
		class_id,
		domain_id,
		NULL,
		NULL);
}

static
void
mono_profiler_vtable_failed (
	MonoProfiler *prof,
	MonoVTable *vtable)
{
	if (!EventEnabledMonoProfilerVTableFailed())
		return;

	uint64_t vtable_id;
	uint64_t class_id;
	uint64_t domain_id;

	get_vtable_data (vtable, &vtable_id, &class_id, &domain_id);
		
	FireEtwMonoProfilerVTableFailed (
		vtable_id,
		class_id,
		domain_id,
		NULL,
		NULL);
}

static
void
mono_profiler_vtable_loaded (
	MonoProfiler *prof,
	MonoVTable *vtable)
{
	if (!EventEnabledMonoProfilerVTableLoaded())
		return;

	uint64_t vtable_id;
	uint64_t class_id;
	uint64_t domain_id;

	get_vtable_data (vtable, &vtable_id, &class_id, &domain_id);
		
	FireEtwMonoProfilerVTableLoaded (
		vtable_id,
		class_id,
		domain_id,
		NULL,
		NULL);
}

static
void
mono_profiler_module_loading (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleLoading ())
		return;
	
	FireEtwMonoProfilerModuleLoading (
		(uint64_t)image,
		NULL,
		NULL);
}

static
void
mono_profiler_module_failed (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleFailed ())
		return;

	FireEtwMonoProfilerModuleFailed (
		(uint64_t)image,
		NULL,
		NULL);
}

static
void
mono_profiler_module_loaded (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleLoaded ())
		return;
	
	uint64_t module_id = (uint64_t)image;
	const ep_char8_t *module_path = NULL;
	const ep_char8_t *module_guid = NULL;

	if (image) {
		ModuleEventData module_data;
		if (get_module_event_data (image, &module_data))
			module_path = (const ep_char8_t *)module_data.module_il_path;
		module_guid = (const ep_char8_t *)mono_image_get_guid (image);
	}

	FireEtwMonoProfilerModuleLoaded (
		module_id,
		module_path ? module_path : "",
		module_guid ? module_guid : "",
		NULL,
		NULL);
}

static
void
mono_profiler_module_unloading (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleUnloading ())
		return;

	FireEtwMonoProfilerModuleUnloading (
		(uint64_t)image,
		NULL,
		NULL);
}

static
void
mono_profiler_module_unloaded (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleUnloaded ())
		return;
	
	uint64_t module_id = (uint64_t)image;
	const ep_char8_t *module_path = NULL;
	const ep_char8_t *module_guid = NULL;

	if (image) {
		ModuleEventData module_data;
		if (get_module_event_data (image, &module_data))
			module_path = (const ep_char8_t *)module_data.module_il_path;
		module_guid = (const ep_char8_t *)mono_image_get_guid (image);
	}

	FireEtwMonoProfilerModuleUnloaded (
		module_id,
		module_path ? module_path : "",
		module_guid ? module_guid : "",
		NULL,
		NULL);
}

static
inline
void
get_assembly_data (
	MonoAssembly *assembly,
	uint64_t *assembly_id,
	uint64_t *module_id,
	ep_char8_t **assembly_name)
{
	*assembly_id = (uint64_t)assembly;
	*module_id = 0;

	if (assembly)
		*module_id = (uint64_t)mono_assembly_get_image_internal (assembly);

	if (assembly && assembly_name)
		*assembly_name = (ep_char8_t *)mono_stringify_assembly_name (&assembly->aname);
	else if (assembly_name)
		*assembly_name = NULL;
}

static
void
mono_profiler_assembly_loading (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	if (!EventEnabledMonoProfilerAssemblyLoading ())
		return;
	
	uint64_t assembly_id;
	uint64_t module_id;

	get_assembly_data (assembly, &assembly_id, &module_id, NULL);

	FireEtwMonoProfilerAssemblyLoading (
		assembly_id,
		module_id,
		NULL,
		NULL);
}

static
void
mono_profiler_assembly_loaded (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	if (!EventEnabledMonoProfilerAssemblyLoaded ())
		return;

	uint64_t assembly_id;
	uint64_t module_id;
	ep_char8_t *assembly_name;

	get_assembly_data (assembly, &assembly_id, &module_id, &assembly_name);

	FireEtwMonoProfilerAssemblyLoaded (
		assembly_id,
		module_id,
		assembly_name ? assembly_name : "",
		NULL,
		NULL);

	g_free (assembly_name);
}

static
void
mono_profiler_assembly_unloading (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	if (!EventEnabledMonoProfilerAssemblyUnloading ())
		return;
	
	uint64_t assembly_id;
	uint64_t module_id;

	get_assembly_data (assembly, &assembly_id, &module_id, NULL);

	FireEtwMonoProfilerAssemblyUnloading (
		assembly_id,
		module_id,
		NULL,
		NULL);
}

static
void
mono_profiler_assembly_unloaded (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	if (!EventEnabledMonoProfilerAssemblyUnloaded ())
		return;

	uint64_t assembly_id;
	uint64_t module_id;
	ep_char8_t *assembly_name;

	get_assembly_data (assembly, &assembly_id, &module_id, &assembly_name);

	FireEtwMonoProfilerAssemblyUnloaded (
		assembly_id,
		module_id,
		assembly_name ? assembly_name : "",
		NULL,
		NULL);

	g_free (assembly_name);
}

static
void
mono_profiler_method_enter (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoProfilerCallContext *context)
{
	if (!EventEnabledMonoProfilerMethodEnter ())
		return;

	FireEtwMonoProfilerMethodEnter (
		(uint64_t)method,
		NULL,
		NULL);
}

static
void
mono_profiler_method_leave (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoProfilerCallContext *context)
{
	if (!EventEnabledMonoProfilerMethodLeave ())
		return;

	FireEtwMonoProfilerMethodLeave (
		(uint64_t)method,
		NULL,
		NULL);
}

static
void
mono_profiler_method_tail_call (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoMethod *target_method)
{
	if (!EventEnabledMonoProfilerMethodTailCall ())
		return;

	FireEtwMonoProfilerMethodTailCall (
		(uint64_t)method,
		NULL,
		NULL);
}

static
void
mono_profiler_method_exception_leave (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoObject *exc)
{
	if (!EventEnabledMonoProfilerMethodExceptionLeave ())
		return;

	FireEtwMonoProfilerMethodExceptionLeave (
		(uint64_t)method,
		NULL,
		NULL);
}

static
void
mono_profiler_method_free (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerMethodFree ())
		return;

	FireEtwMonoProfilerMethodFree (
		(uint64_t)method,
		NULL,
		NULL);
}

static
void
mono_profiler_method_begin_invoke (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerMethodBeginInvoke ())
		return;

	FireEtwMonoProfilerMethodBeginInvoke (
		(uint64_t)method,
		NULL,
		NULL);
}

static
void
mono_profiler_method_end_invoke (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerMethodEndInvoke ())
		return;

	FireEtwMonoProfilerMethodEndInvoke (
		(uint64_t)method,
		NULL,
		NULL);
}

static
MonoProfilerCallInstrumentationFlags
mono_profiler_method_instrumentation (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (_ep_rt_dotnet_mono_profiler_provider_callspec.len > 0 && !mono_callspec_eval (method, &_ep_rt_dotnet_mono_profiler_provider_callspec))
		return MONO_PROFILER_CALL_INSTRUMENTATION_NONE;

	return MONO_PROFILER_CALL_INSTRUMENTATION_ENTER |
			MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE |
			MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL |
			MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE;
}

static
void
mono_profiler_exception_throw (
	MonoProfiler *prof,
	MonoObject *exc)
{
	if (!EventEnabledMonoProfilerExceptionThrow ())
		return;

	uint64_t type_id = 0;

	if (exc && mono_object_class(exc))
		type_id = (uint64_t)m_class_get_byval_arg (mono_object_class(exc));

	FireEtwMonoProfilerExceptionThrow (
		type_id,
		SGEN_POINTER_UNTAG_ALL (exc),
		NULL,
		NULL);
}

static
void
mono_profiler_exception_clause (
	MonoProfiler *prof,
	MonoMethod *method,
	uint32_t clause_num,
	MonoExceptionEnum clause_type,
	MonoObject *exc)
{
	if (!EventEnabledMonoProfilerExceptionClause ())
		return;

	uint64_t type_id = 0;

	if (exc && mono_object_class(exc))
		type_id = (uint64_t)m_class_get_byval_arg (mono_object_class(exc));

	FireEtwMonoProfilerExceptionClause (
		(uint8_t)clause_type,
		clause_num,
		(uint64_t)method,
		type_id,
		SGEN_POINTER_UNTAG_ALL (exc),
		NULL,
		NULL);
}

static
void
mono_profiler_gc_event (
	MonoProfiler *prof,
	MonoProfilerGCEvent gc_event,
	uint32_t generation,
	mono_bool serial)
{
	if (!EventEnabledMonoProfilerGCEvent ())
		return;

	// TODO: Needs to be async safe.
	/*FireEtwMonoProfilerGCEvent (
		(uint8_t)gc_event,
		generation,
		NULL,
		NULL);*/
}

static
void
mono_profiler_gc_allocation (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerGCAllocation ())
		return;

	uint64_t vtable_id = 0;
	uint64_t object_size = 0;

	if (object) {
		vtable_id = (uint64_t)mono_object_get_vtable_internal (object);
		object_size = (uint64_t)mono_object_get_size_internal (object);
		
		/* account for object alignment */
		object_size += 7;
		object_size &= ~7;
	}

	FireEtwMonoProfilerGCAllocation (
		vtable_id,
		SGEN_POINTER_UNTAG_ALL (object),
		object_size,
		NULL,
		NULL);
}

static
void
mono_profiler_gc_moves (
	MonoProfiler *prof,
	MonoObject *const* objects,
	uint64_t count)
{
	if (!EventEnabledMonoProfilerGCMoves ())
		return;

	// TODO: Needs to be async safe.
	/*uint64_t obj_count = count / 2;

	GCObjectAddressData data [32];
	uint64_t data_chunks = obj_count / G_N_ELEMENTS (data);
	uint64_t data_rest = obj_count % G_N_ELEMENTS (data);
	uint64_t current_obj = 0;

	for (int chunk = 0; chunk < data_chunks; chunk++) {
		for (int i = 0; i < G_N_ELEMENTS (data); i++) {
			data [i].object = SGEN_POINTER_UNTAG_ALL (objects [current_obj++]);
			data [i].address = objects [current_obj++];
		}

		FireEtwMonoProfilerGCMoves (
			G_N_ELEMENTS (data),
			sizeof (GCObjectAddressData),
			data,
			NULL,
			NULL);
	}

	if ((data_rest != 0)&& (data_rest % 2 == 0)) {
		for (int i = 0; i < data_rest; i++) {
			data [i].object = SGEN_POINTER_UNTAG_ALL (objects [current_obj++]);
			data [i].address = objects [current_obj++];
		}

		FireEtwMonoProfilerGCMoves (
			data_rest,
			sizeof (GCObjectAddressData),
			data,
			NULL,
			NULL);
	}*/
}

static
void
mono_profiler_gc_resize (
	MonoProfiler *prof,
	uintptr_t size)
{
	if (!EventEnabledMonoProfilerGCResize ())
		return;

	// TODO: Needs to be async safe.
	/*FireEtwMonoProfilerGCResize (
		(uint64_t)size,
		NULL,
		NULL);*/
}

static
void
mono_profiler_gc_handle_created (
	MonoProfiler *prof,
	uint32_t handle,
	MonoGCHandleType type,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerGCHandleCreated ())
		return;

	FireEtwMonoProfilerGCHandleCreated (
		handle,
		(uint8_t)type,
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);
}

static
void
mono_profiler_gc_handle_deleted (
	MonoProfiler *prof,
	uint32_t handle,
	MonoGCHandleType type)
{
	if (!EventEnabledMonoProfilerGCHandleDeleted ())
		return;

	FireEtwMonoProfilerGCHandleDeleted (
		handle,
		(uint8_t)type,
		NULL,
		NULL);
}

static
void
mono_profiler_gc_finalizing (MonoProfiler *prof)
{
	if (!EventEnabledMonoProfilerGCFinalizing ())
		return;

	FireEtwMonoProfilerGCFinalizing (
		NULL,
		NULL);
}

static
void
mono_profiler_gc_finalized (MonoProfiler *prof)
{
	if (!EventEnabledMonoProfilerGCFinalized ())
		return;

	FireEtwMonoProfilerGCFinalized (
		NULL,
		NULL);
}

static
void
mono_profiler_gc_finalizing_object (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerGCFinalizingObject ())
		return;

	FireEtwMonoProfilerGCFinalizingObject (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);
}

static
void
mono_profiler_gc_finalized_object (
	MonoProfiler *prof,
	MonoObject * object)
{
	if (!EventEnabledMonoProfilerGCFinalizedObject ())
		return;

	FireEtwMonoProfilerGCFinalizedObject (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);
}

static
void
mono_profiler_gc_root_register (
	MonoProfiler *prof,
	const mono_byte *start,
	uintptr_t size,
	MonoGCRootSource source,
	const void * key,
	const char * name)
{
	if (!EventEnabledMonoProfilerGCRootRegister ())
		return;

	FireEtwMonoProfilerGCRootRegister (
		start,
		(uint64_t)size,
		(uint8_t) source,
		(uint64_t)key,
		(const ep_char8_t *)(name ? name : ""),
		NULL,
		NULL);
}

static
void
mono_profiler_gc_root_unregister (
	MonoProfiler *prof,
	const mono_byte *start)
{
	if (!EventEnabledMonoProfilerGCRootUnregister ())
		return;

	FireEtwMonoProfilerGCRootUnregister (
		start,
		NULL,
		NULL);
}

static
void
mono_profiler_gc_roots (
	MonoProfiler *prof,
	uint64_t count,
	const mono_byte *const * addresses,
	MonoObject *const * objects)
{
	if (!EventEnabledMonoProfilerGCRoots ())
		return;

	// TODO: Needs to be async safe.
	/*GCAddressObjectData data [32];
	uint64_t data_chunks = count / G_N_ELEMENTS (data);
	uint64_t data_rest = count % G_N_ELEMENTS (data);
	uint64_t current_obj = 0;

	for (int chunk = 0; chunk < data_chunks; chunk++) {
		for (int i = 0; i < G_N_ELEMENTS (data); i++) {
			data [i].address = addresses [current_obj];
			data [i].object = SGEN_POINTER_UNTAG_ALL (objects [current_obj]);
			current_obj++;
		}

		FireEtwMonoProfilerGCRoots (
			G_N_ELEMENTS (data),
			sizeof (GCAddressObjectData),
			data,
			NULL,
			NULL);
	}

	if (data_rest != 0) {
		for (int i = 0; i < data_rest; i++) {
			data [i].address = addresses [current_obj];
			data [i].object = SGEN_POINTER_UNTAG_ALL (objects [current_obj]);
			current_obj++;
		}

		FireEtwMonoProfilerGCRoots (
			data_rest,
			sizeof (GCAddressObjectData),
			data,
			NULL,
			NULL);
	}*/
}

static
void
mono_profiler_monitor_contention (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerMonitorContention ())
		return;

	FireEtwMonoProfilerMonitorContention (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);
}

static
void
mono_profiler_monitor_failed (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerMonitorFailed ())
		return;

	FireEtwMonoProfilerMonitorFailed (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);
}

static
void
mono_profiler_monitor_acquired (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerMonitorAcquired ())
		return;

	FireEtwMonoProfilerMonitorAcquired (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);
}

static
void
mono_profiler_thread_started (
	MonoProfiler *prof,
	uintptr_t tid)
{
	if (!EventEnabledMonoProfilerThreadStarted ())
		return;

	FireEtwMonoProfilerThreadStarted (
		(uint64_t)tid,
		NULL,
		NULL);
}

static
void
mono_profiler_thread_stopping (
	MonoProfiler *prof,
	uintptr_t tid)
{
	if (!EventEnabledMonoProfilerThreadStopping ())
		return;

	FireEtwMonoProfilerThreadStopping (
		(uint64_t)tid,
		NULL,
		NULL);
}

static
void
mono_profiler_thread_stopped (
	MonoProfiler *prof,
	uintptr_t tid)
{
	if (!EventEnabledMonoProfilerThreadStopped ())
		return;

	FireEtwMonoProfilerThreadStopped (
		(uint64_t)tid,
		NULL,
		NULL);
}

static
void
mono_profiler_thread_exited (
	MonoProfiler *prof,
	uintptr_t tid)
{
	if (!EventEnabledMonoProfilerThreadExited ())
		return;

	FireEtwMonoProfilerThreadExited (
		(uint64_t)tid,
		NULL,
		NULL);
}

static
void
mono_profiler_thread_name (
	MonoProfiler *prof,
	uintptr_t tid,
	const char *name)
{
	if (!EventEnabledMonoProfilerThreadName ())
		return;

	FireEtwMonoProfilerThreadName (
		(uint64_t)tid,
		(ep_char8_t *)(name ? name : ""),
		NULL,
		NULL);
}

void
EventPipeEtwCallbackDotNETRuntimeMonoProfiler (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	ep_rt_config_requires_lock_not_held ();

	EP_ASSERT(is_enabled == 0 || is_enabled == 1) ;
	EP_ASSERT (_ep_rt_dotnet_mono_profiler_provider != NULL);

	match_any_keywords = (is_enabled == 1) ? match_any_keywords : 0;

	EP_LOCK_ENTER (section1)
		uint64_t enabled_keywords = MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_EVENTPIPE_Context.EnabledKeywordsBitmask;

		if (profiler_callback_is_enabled(match_any_keywords, LOADER_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, LOADER_KEYWORD)) {
				mono_profiler_set_domain_loading_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_app_domain_loading);
				mono_profiler_set_domain_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_app_domain_loaded);
				mono_profiler_set_domain_unloading_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_app_domain_unloading);
				mono_profiler_set_domain_unloaded_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_app_domain_unloaded);
				mono_profiler_set_domain_name_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_app_domain_name);
				mono_profiler_set_image_loading_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_module_loading);
				mono_profiler_set_image_failed_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_module_failed);
				mono_profiler_set_image_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_module_loaded);
				mono_profiler_set_image_unloading_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_module_unloading);
				mono_profiler_set_image_unloaded_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_module_unloaded);
				mono_profiler_set_assembly_loading_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_assembly_loading);
				mono_profiler_set_assembly_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_assembly_loaded);
				mono_profiler_set_assembly_unloading_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_assembly_unloading);
				mono_profiler_set_assembly_unloaded_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_assembly_unloaded);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, LOADER_KEYWORD)) {
				mono_profiler_set_domain_loading_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_domain_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_domain_unloading_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_domain_unloaded_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_domain_name_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_image_loading_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_image_failed_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_image_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_image_unloading_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_image_unloaded_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_assembly_loading_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_assembly_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_assembly_unloading_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_assembly_unloaded_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, JIT_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, JIT_KEYWORD)) {
				mono_profiler_set_jit_begin_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_jit_begin);
				mono_profiler_set_jit_failed_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_jit_failed);
				mono_profiler_set_jit_done_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_jit_done);
				mono_profiler_set_jit_chunk_created_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_jit_chunk_created);
				mono_profiler_set_jit_chunk_destroyed_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_jit_chunk_destroyed);
				mono_profiler_set_jit_code_buffer_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_jit_code_buffer);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, JIT_KEYWORD)) {
				mono_profiler_set_jit_begin_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_jit_failed_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_jit_done_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_jit_chunk_created_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_jit_chunk_destroyed_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_jit_code_buffer_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, TYPE_LOADING_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, TYPE_LOADING_KEYWORD)) {
				mono_profiler_set_class_loading_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_class_loading);
				mono_profiler_set_class_failed_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_class_failed);
				mono_profiler_set_class_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_class_loaded);
				mono_profiler_set_vtable_loading_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_vtable_loading);
				mono_profiler_set_vtable_failed_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_vtable_failed);
				mono_profiler_set_vtable_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_vtable_loaded);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, TYPE_LOADING_KEYWORD)) {
				mono_profiler_set_class_loading_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_class_failed_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_class_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_vtable_loading_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_vtable_failed_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_vtable_loaded_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, METHOD_TRACING_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, METHOD_TRACING_KEYWORD)) {
				mono_profiler_set_method_enter_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_enter);
				mono_profiler_set_method_leave_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_leave);
				mono_profiler_set_method_tail_call_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_tail_call);
				mono_profiler_set_method_exception_leave_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_exception_leave);
				mono_profiler_set_method_free_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_free);
				mono_profiler_set_method_begin_invoke_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_begin_invoke);
				mono_profiler_set_method_end_invoke_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_end_invoke);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, METHOD_TRACING_KEYWORD)) {
				mono_profiler_set_method_enter_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_method_leave_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_method_tail_call_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_method_exception_leave_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_method_free_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_method_begin_invoke_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_method_end_invoke_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, EXCEPTION_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, EXCEPTION_KEYWORD)) {
				mono_profiler_set_exception_throw_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_exception_throw);
				mono_profiler_set_exception_clause_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_exception_clause);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, EXCEPTION_KEYWORD)) {
				mono_profiler_set_exception_throw_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_exception_clause_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, GC_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD)) {
				mono_profiler_set_gc_event_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_event);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD)) {
				mono_profiler_set_gc_event_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, GC_KEYWORD | GC_ALLOCATION_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_ALLOCATION_KEYWORD)) {
				mono_profiler_set_gc_allocation_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_allocation);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_ALLOCATION_KEYWORD)) {
				mono_profiler_set_gc_allocation_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, GC_KEYWORD | GC_MOVES_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_MOVES_KEYWORD)) {
				mono_profiler_set_gc_moves_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_moves);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_MOVES_KEYWORD)) {
				mono_profiler_set_gc_moves_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, GC_KEYWORD | GC_RESIZE_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_RESIZE_KEYWORD)) {
				mono_profiler_set_gc_resize_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_resize);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_RESIZE_KEYWORD)) {
				mono_profiler_set_gc_resize_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, GC_KEYWORD | GC_HANDLE_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_HANDLE_KEYWORD)) {
				mono_profiler_set_gc_handle_created_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_handle_created);
				mono_profiler_set_gc_handle_deleted_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_handle_deleted);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_HANDLE_KEYWORD)) {
				mono_profiler_set_gc_handle_created_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_gc_handle_deleted_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, GC_KEYWORD | GC_FINALIZATION_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_FINALIZATION_KEYWORD)) {
				mono_profiler_set_gc_finalizing_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_finalizing);
				mono_profiler_set_gc_finalized_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_finalized);
				mono_profiler_set_gc_finalizing_object_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_finalizing_object);
				mono_profiler_set_gc_finalized_object_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_finalized_object);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_FINALIZATION_KEYWORD)) {
				mono_profiler_set_gc_finalizing_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_gc_finalized_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_gc_finalizing_object_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_gc_finalized_object_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, GC_KEYWORD | GC_ROOT_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_ROOT_KEYWORD)) {
				mono_profiler_set_gc_root_register_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_root_register);
				mono_profiler_set_gc_root_unregister_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_root_unregister);
				mono_profiler_set_gc_roots_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_gc_roots);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, GC_KEYWORD | GC_ROOT_KEYWORD)) {
				mono_profiler_set_gc_root_register_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_gc_root_unregister_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_gc_roots_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, MONITOR_KEYWORD | CONTENTION_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, MONITOR_KEYWORD | CONTENTION_KEYWORD)) {
				mono_profiler_set_monitor_contention_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_monitor_contention);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, MONITOR_KEYWORD | CONTENTION_KEYWORD)) {
				mono_profiler_set_monitor_contention_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, MONITOR_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, MONITOR_KEYWORD)) {
				mono_profiler_set_monitor_failed_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_monitor_failed);
				mono_profiler_set_monitor_acquired_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_monitor_acquired);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, MONITOR_KEYWORD)) {
				mono_profiler_set_monitor_failed_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_monitor_acquired_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (profiler_callback_is_enabled(match_any_keywords, THREADING_KEYWORD)) {
			if (!profiler_callback_is_enabled (enabled_keywords, THREADING_KEYWORD)) {
				mono_profiler_set_thread_started_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_thread_started);
				mono_profiler_set_thread_stopping_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_thread_stopping);
				mono_profiler_set_thread_stopped_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_thread_stopped);
				mono_profiler_set_thread_exited_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_thread_exited);
				mono_profiler_set_thread_name_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_thread_name);
			}
		} else {
			if (profiler_callback_is_enabled (enabled_keywords, THREADING_KEYWORD)) {
				mono_profiler_set_thread_started_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_thread_stopping_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_thread_stopped_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_thread_exited_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				mono_profiler_set_thread_name_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
			}
		}

		if (!_ep_rt_dotnet_mono_profiler_provider_callspec.enabled) {
			if (profiler_callback_is_enabled(match_any_keywords, METHOD_INSTRUMENTATION_KEYWORD)) {
				if (!profiler_callback_is_enabled (enabled_keywords, METHOD_INSTRUMENTATION_KEYWORD)) {
					mono_profiler_set_call_instrumentation_filter_callback (_ep_rt_dotnet_mono_profiler_provider, mono_profiler_method_instrumentation);
				}
			} else {
				if (profiler_callback_is_enabled (enabled_keywords, METHOD_INSTRUMENTATION_KEYWORD)) {
					mono_profiler_set_call_instrumentation_filter_callback (_ep_rt_dotnet_mono_profiler_provider, NULL);
				}
			}
		}

		MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_EVENTPIPE_Context.Level = level;
		MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_EVENTPIPE_Context.EnabledKeywordsBitmask = match_any_keywords;
		MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_EVENTPIPE_Context.IsEnabled = (is_enabled == 1 ? true : false);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

#endif /* ENABLE_PERFTRACING */

MONO_EMPTY_SOURCE_FILE(eventpipe_rt_mono);
