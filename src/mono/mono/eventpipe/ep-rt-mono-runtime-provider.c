#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-rt-config.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-rt.h>
#include <eventpipe/ep.h>

#include <eglib/gmodule.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/mini/mini-runtime.h>
#include <mono/sgen/sgen-conf.h>
#include <mono/sgen/sgen-tagged-pointer.h>
#include <clretwallmain.h>

extern EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context;
extern EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context;

#define RUNTIME_PROVIDER_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context
#define RUNTIME_RUNDOWN_PROVIDER_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context

#define NUM_NANOSECONDS_IN_1_MS 1000000

// Sample profiler.
static GArray * _sampled_thread_callstacks = NULL;
static uint32_t _max_sampled_thread_count = 32;

// Mono profilers.
extern MonoProfilerHandle _ep_rt_mono_default_profiler_provider;

// Phantom JIT compile method.
static MonoMethod *_runtime_helper_compile_method = NULL;
static MonoJitInfo *_runtime_helper_compile_method_jitinfo = NULL;

// Monitor.Enter methods.
static MonoMethod *_monitor_enter_method = NULL;
static MonoJitInfo *_monitor_enter_method_jitinfo = NULL;

static MonoMethod *_monitor_enter_v4_method = NULL;
static MonoJitInfo *_monitor_enter_v4_method_jitinfo = NULL;

// GC roots table.
static dn_umap_t _gc_roots_table = {0};

// Lock used for GC related activities.
static ep_rt_spin_lock_handle_t _gc_lock = {0};

// Rundown types.
typedef
bool
(*fire_method_rundown_events_func)(
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
(*fire_assembly_rundown_events_func)(
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
(*fire_domain_rundown_events_func)(
	const uint64_t domain_id,
	const uint32_t domain_flags,
	const ep_char8_t *domain_name,
	const uint32_t domain_index,
	void *user_data);

typedef struct _FireMethodEventsData {
	MonoDomain *domain;
	uint8_t *buffer;
	size_t buffer_size;
	fire_method_rundown_events_func method_events_func;
} FireMethodEventsData;

typedef struct _StackWalkData {
	EventPipeStackContents *stack_contents;
	bool top_frame;
	bool async_frame;
	bool safe_point_frame;
	bool runtime_invoke_frame;
} StackWalkData;

typedef struct _SampleProfileStackWalkData {
	StackWalkData stack_walk_data;
	EventPipeStackContents stack_contents;
	uint64_t thread_id;
	uintptr_t thread_ip;
	uint32_t payload_data;
} SampleProfileStackWalkData;

// Rundown flags.
#define RUNTIME_SKU_MONO 0x4
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
	uint8_t module_il_pdb_signature [EP_GUID_SIZE];
	uint8_t module_native_pdb_signature [EP_GUID_SIZE];
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

// BulkType types.

typedef enum {
	TYPE_FLAGS_DELEGATE = 0x1,
	TYPE_FLAGS_FINALIZABLE = 0x2,
	// unused = 0x4,
	TYPE_FLAGS_ARRAY = 0x8,

	TYPE_FLAGS_ARRAY_RANK_MASK = 0x3F00,
	TYPE_FLAGS_ARRAY_RANK_SHIFT = 8,
	TYPE_FLAGS_ARRAY_RANK_MAX = TYPE_FLAGS_ARRAY_RANK_MASK >> TYPE_FLAGS_ARRAY_RANK_SHIFT
} TypeFlags;

// This only contains the fixed-size data at the top of each struct in
// the bulk type event.  These fields must still match exactly the initial
// fields of the struct described in the manifest.
typedef struct _EventStructBulkTypeFixedSizedData {
	uint64_t type_id;
	uint64_t module_id;
	uint32_t type_name_id;
	uint32_t flags;
	uint8_t cor_element_type;
} EventStructBulkTypeFixedSizedData;

// Represents one instance of the Value struct inside a single BulkType event
typedef struct _BulkTypeValue {
	EventStructBulkTypeFixedSizedData fixed_sized_data;
	uint32_t type_parameters_count;
	MonoType **mono_type_parameters;
	const ep_char8_t *name;
} BulkTypeValue;

// ETW has a limitation of 64K for TOTAL event Size, however there is overhead associated with
// the event headers.   It is unclear exactly how much that is, but 1K should be sufficiently
// far away to avoid problems without sacrificing the perf of bulk processing.
#define MAX_EVENT_BYTE_COUNT (63 * 1024)

// The maximum event size, and the size of the buffer that we allocate to hold the event contents.
#define MAX_SIZE_OF_EVENT_BUFFER 65536

// Estimate of how many bytes we can squeeze in the event data for the value struct
// array. (Intentionally overestimate the size of the non-array parts to keep it safe.)
// This follows CoreCLR's kMaxBytesTypeValues.
#define MAX_TYPE_VALUES_BYTES (MAX_EVENT_BYTE_COUNT - 0x30)

// Estimate of how many type value elements we can put into the struct array, while
// staying under the ETW event size limit. Note that this is impossible to calculate
// perfectly, since each element of the struct array has variable size.
//
// In addition to the byte-size limit per event, Windows always forces on us a
// max-number-of-descriptors per event, which in the case of BulkType, will kick in
// far sooner. There's a max number of 128 descriptors allowed per event. 2 are used
// for Count + ClrInstanceID. Then 4 per batched value. (Might actually be 3 if there
// are no type parameters to log, but let's overestimate at 4 per value).
#define K_MAX_COUNT_TYPE_VALUES ((uint32_t)(128 - 2) / 4)

typedef enum {
	TYPE_LOG_BEHAVIOR_IF_FIRST_TIME,
	TYPE_LOG_BEHAVIOR_ALWAYS_LOG,
	TYPE_LOG_BEHAVIOR_ALWAYS_LOG_TOP_LEVEL
} TypeLogBehavior;

typedef struct _BulkTypeEventLogger {
	BulkTypeValue bulk_type_values [K_MAX_COUNT_TYPE_VALUES];
	uint8_t *bulk_type_event_buffer;
	uint32_t bulk_type_value_count;
	uint32_t bulk_type_value_byte_count;
	MonoMemPool *mem_pool;
	dn_umap_t *type_cache;
} BulkTypeEventLogger;

// ETW has a limit for maximum event size. Do not log overly large method type argument sets
static const uint32_t MAX_METHOD_TYPE_ARGUMENT_COUNT = 1024;

// GC roots type.
typedef struct _GCRootData {
	uintptr_t start;
	uintptr_t end;
	const void *key;
	char *name;
	MonoGCRootSource source;
} GCRootData;

// GC heap dump types.
typedef enum {
	BUFFERED_GC_EVENT_OBJECT_REF = 1,
	BUFFERED_GC_EVENT_ROOTS = 2,
} BufferedGCEventType;

typedef struct _BufferedGCEvent BufferedGCEvent;
struct _BufferedGCEvent {
	BufferedGCEventType type;
	uint32_t payload_size;
};

typedef struct _GCHeapDumpMemFileBuffer GCHeapDumpMemFileBuffer;
struct _GCHeapDumpMemFileBuffer {
	ep_char8_t *name;
	int fd;
	uint8_t *start;
	uint8_t *current;
	uint8_t *end;
};

typedef struct _GCHeapDumpBuffer GCHeapDumpBuffer;
struct _GCHeapDumpBuffer{
	void *context;
	bool (*reset_func)(void *context);
	uint8_t * (*alloc_func)(void *context ,size_t size);
	const uint8_t * (*get_next_buffer_func)(void *context, size_t *size);
};

typedef struct _GCHeapDumpBulkData GCHeapDumpBulkData;
struct _GCHeapDumpBulkData {
	uint8_t *data_start;
	uint8_t *data_current;
	uint8_t *data_end;
	uint32_t index;
	uint32_t count;
	uint32_t max_count;
};

typedef enum {
	GC_HEAP_DUMP_CONTEXT_STATE_INIT = 0,
	GC_HEAP_DUMP_CONTEXT_STATE_START = 1,
	GC_HEAP_DUMP_CONTEXT_STATE_DUMP = 2,
	GC_HEAP_DUMP_CONTEXT_STATE_END = 3
} GCHeapDumpContextState;

typedef struct _GCHeapDumpContext GCHeapDumpContext;
struct _GCHeapDumpContext {
	EVENTPIPE_TRACE_CONTEXT trace_context;
	GCHeapDumpBulkData bulk_nodes;
	GCHeapDumpBulkData bulk_edges;
	GCHeapDumpBulkData bulk_root_edges;
	GCHeapDumpBulkData bulk_root_cwt_elem_edges;
	GCHeapDumpBulkData bulk_root_static_vars;
	BulkTypeEventLogger *bulk_type_logger;
	GCHeapDumpBuffer *buffer;
	dn_vector_ptr_t *gc_roots;
	uint32_t gc_reason;
	uint32_t gc_type;
	uint32_t gc_count;
	uint32_t gc_depth;
	uint32_t retry_count;
	GCHeapDumpContextState state;
};

// Must match GCBulkNode layout in ClrEtwAll.man.
static const uint32_t BULK_NODE_EVENT_TYPE_SIZE =
	// Address
	sizeof (uintptr_t) +
	// Size
	sizeof (uint64_t) +
	// TypeID
	sizeof (uint64_t) +
	// EdgeCount
	sizeof (uint64_t);

// Must match GCBulkEdge layout in ClrEtwAll.man.
static const uint32_t BULK_EDGE_EVENT_TYPE_SIZE =
	// Value
	sizeof (uintptr_t) +
	// ReferencingFiledID
	sizeof (uint32_t);

// Must match GCBulkRootEdge layout in ClrEtwAll.man.
static const uint32_t BULK_ROOT_EDGE_EVENT_TYPE_SIZE =
	// RootedNodeAddresses
	sizeof (uintptr_t) +
	// GCRootKind
	sizeof (uint8_t) +
	// GCRootFlag
	sizeof (uint32_t) +
	// GCRootID
	sizeof (uintptr_t);

// Must match GCBulkRootConditionalWeakTableElementEdge layout in ClrEtwAll.man.
static const uint32_t BULK_ROOT_CWT_ELEM_EDGE_EVENT_TYPE_SIZE =
	// GCKeyNodeID
	sizeof (uintptr_t) +
	// GCValueNodeID
	sizeof (uintptr_t) +
	// GCRootID
	sizeof (uintptr_t);

// Must match GCBulkRootStaticVar layout in ClrEtwAll.man.
static const uint32_t BULK_ROOT_STATIC_VAR_EVENT_TYPE_SIZE =
	// GCRootID
	sizeof (uint64_t) +
	// ObjectID
	sizeof (uint64_t) +
	// TypeID
	sizeof (uint64_t) +
	// Flags
	sizeof (uint32_t) +
	//FieldName
	sizeof (ep_char16_t);

// GC heap dump flags.
#define GC_REASON_INDUCED 1
#define GC_TYPE_NGC 0
#define GC_ROOT_FLAGS_NONE 0
#define GC_ROOT_FLAGS_PINNING 1
#define GC_ROOT_FLAGS_WEAKREF 2
#define GC_ROOT_FLAGS_INTERIOR 4
#define GC_ROOT_FLAGS_REFCOUNTED 8
#define GC_ROOT_KIND_STACK 0
#define GC_ROOT_KIND_FINALIZER 1
#define GC_ROOT_KIND_HANDLE 2
#define GC_ROOT_KIND_OTHER 3

static volatile uint32_t _gc_heap_dump_requests = 0;
static volatile uint32_t _gc_heap_dump_count = 0;
static volatile uint64_t _gc_heap_dump_trigger_count = 0;

static dn_vector_t _gc_heap_dump_requests_data = {0};

static
uint64_t
get_typeid_for_type (MonoType *t);

static
void
bulk_type_log_type_and_parameters_if_necessary (
	BulkTypeEventLogger *type_logger,
	MonoType *mono_type,
	TypeLogBehavior log_behavior);


static
uint16_t
clr_instance_get_id (void)
{
	// Mono runtime id.
	return 9;
}

static
bool
is_keyword_and_level_enabled (
	const EVENTPIPE_TRACE_CONTEXT *context,
	uint8_t level,
	uint64_t keyword)
{
	if (context->IsEnabled && level <= context->Level)
		return (keyword == 0) || (keyword & context->EnabledKeywordsBitmask) != 0;
	return false;
}

static
bool
is_keword_enabled (uint64_t enabled_keywords, uint64_t keyword)
{
	return (enabled_keywords & keyword) == keyword;
}

static
bool
is_gc_heap_dump_enabled (GCHeapDumpContext *context)
{
	if (!context)
		return false;

	bool enabled = is_keyword_and_level_enabled (&context->trace_context, EP_EVENT_LEVEL_INFORMATIONAL, GC_HEAP_DUMP_KEYWORD);
	enabled &= context->gc_reason == GC_REASON_INDUCED;
	enabled &= context->gc_type == GC_TYPE_NGC;
	return enabled;
}

static
uint32_t
write_buffer_string_utf8_to_utf16_t (
	uint8_t **buf,
	const ep_char8_t *str,
	uint32_t len)
{
	uint32_t num_bytes_utf16_str = 0;
	if (str && len != 0) {
		glong len_utf16 = 0;
		ep_char16_t *str_utf16 = (ep_char16_t *)(g_utf8_to_utf16le ((const gchar *)str, (glong)len, NULL, &len_utf16, NULL));
		if (str_utf16 && len_utf16 != 0) {
			num_bytes_utf16_str = MIN (GLONG_TO_UINT32 (len_utf16), len) * sizeof (ep_char16_t);
			memcpy (*buf, str_utf16, num_bytes_utf16_str);
		}
		g_free (str_utf16);
	}

	(*buf) [num_bytes_utf16_str] = 0;
	num_bytes_utf16_str++;

	(*buf) [num_bytes_utf16_str] = 0;
	num_bytes_utf16_str++;

	*buf += num_bytes_utf16_str;
	return num_bytes_utf16_str;
}

static
bool
fire_method_rundown_events (
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
fire_assembly_rundown_events (
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
fire_domain_rundown_events (
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
fire_method_events (
	MonoJitInfo *ji,
	MonoMethod *method,
	FireMethodEventsData *events_data)
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
	bool verbose = (RUNTIME_RUNDOWN_PROVIDER_CONTEXT.Level >= (uint8_t)EP_EVENT_LEVEL_VERBOSE);

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
			method_signature = mono_signature_full_name (mono_method_signature_internal (method));
			if (method->klass)
				method_namespace = mono_type_get_name_full (m_class_get_byval_arg (method->klass), MONO_TYPE_NAME_FORMAT_IL);
		}

	}

	uint32_t offset_entries = 0;
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

				uint8_t *il_offsets_ptr = (uint8_t *)il_offsets;
				uint8_t *native_offsets_ptr = (uint8_t *)native_offsets;
				for (uint32_t offset_count = 0; offset_count < offset_entries; ++offset_count) {
					ep_write_buffer_uint32_t (&il_offsets_ptr, debug_info->line_numbers [offset_count].il_offset);
					ep_write_buffer_uint32_t (&native_offsets_ptr, debug_info->line_numbers [offset_count].native_offset);
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
		native_offsets [0] = ep_rt_val_uint32_t ((uint32_t)ji->code_size);
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
		GUINT32_TO_UINT16 (offset_entries),
		il_offsets,
		native_offsets,
		(ji->from_aot || ji->from_llvm),
		verbose,
		NULL);

	g_free (method_namespace);
	g_free (method_signature);
}

static
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
bool
get_module_event_data (
	MonoImage *image,
	ModuleEventData *module_data)
{
	if (module_data) {
		memset (module_data->module_il_pdb_signature, 0, EP_GUID_SIZE);
		memset (module_data->module_native_pdb_signature, 0, EP_GUID_SIZE);

		// Under netcore we only have root domain.
		MonoDomain *root_domain = mono_get_root_domain ();

		module_data->domain_id = (uint64_t)root_domain;
		module_data->module_id = (uint64_t)image;
		module_data->assembly_id = image ? (uint64_t)image->assembly : 0;

		// TODO: Extract all module native paths and pdb metadata when available.
		module_data->module_native_path = "";
		module_data->module_native_pdb_path = "";
		module_data->module_native_pdb_age = 0;

		module_data->reserved_flags = 0;

		// Netcore has a 1:1 between assemblies and modules, so its always a manifest module.
		module_data->module_flags = MODULE_FLAGS_MANIFEST_MODULE;
		if (image && image->dynamic)
			module_data->module_flags |= MODULE_FLAGS_DYNAMIC_MODULE;
		if (image && image->aot_module)
			module_data->module_flags |= MODULE_FLAGS_NATIVE_MODULE;

		module_data->module_il_path = NULL;
		if (image && image->filename) {
			/* if there's a filename, use it */
			module_data->module_il_path = image->filename;
		} else if (image && image->module_name) {
			/* otherwise, use the module name */
			module_data->module_il_path = image->module_name;
		}
		if (!module_data->module_il_path)
			module_data->module_il_path = "";

		module_data->module_il_pdb_path = "";
		module_data->module_il_pdb_age = 0;

		if (image && image->image_info) {
			MonoPEDirEntry *debug_dir_entry = (MonoPEDirEntry *)&image->image_info->cli_header.datadir.pe_debug;
			if (debug_dir_entry->size) {
				ImageDebugDirectory debug_dir;
				memset (&debug_dir, 0, sizeof (debug_dir));

				uint32_t offset = mono_cli_rva_image_map (image, debug_dir_entry->rva);
				for (uint32_t idx = 0; idx < debug_dir_entry->size / sizeof (ImageDebugDirectory); ++idx) {
					uint8_t *data = (uint8_t *) ((ImageDebugDirectory *) (image->raw_data + offset) + idx);
					debug_dir.major_version = read16 (data + 8);
					debug_dir.minor_version = read16 (data + 10);
					debug_dir.type = read32 (data + 12);
					debug_dir.pointer = read32 (data + 24);

					if (debug_dir.type == DEBUG_DIR_ENTRY_CODEVIEW && debug_dir.major_version == 0x100 && debug_dir.minor_version == 0x504d) {
						data  = (uint8_t *)(image->raw_data + debug_dir.pointer);
						int32_t signature = read32 (data);
						if (signature == 0x53445352) {
							memcpy (module_data->module_il_pdb_signature, data + 4, EP_GUID_SIZE);
							module_data->module_il_pdb_age = read32 (data + 20);
							module_data->module_il_pdb_path = (const char *)(data + 24);
							break;
						}
					}
				}
			}
		}
	}

	return true;
}

static
void
fire_method_events_callback (
	MonoJitInfo *ji,
	void  *user_data)
{
	FireMethodEventsData *events_data = (FireMethodEventsData *)user_data;
	EP_ASSERT (events_data != NULL);

	if (ji && !ji->is_trampoline && !ji->async) {
		MonoMethod *method = jinfo_get_method (ji);
		if (include_method (method))
			fire_method_events (ji, method, events_data);
	}
}

static
void
fire_assembly_events (
	MonoDomain *domain,
	MonoAssembly *assembly,
	fire_assembly_rundown_events_func assembly_events_func)
{
	EP_ASSERT (domain != NULL);
	EP_ASSERT (assembly != NULL);
	EP_ASSERT (assembly_events_func != NULL);

	// Native methods are part of JIT table and already emitted.
	// TODO: FireEtwMethodDCEndVerbose_V1_or_V2 for all native methods in module as well?

	uint32_t binding_id = 0;

	ModuleEventData module_data;
	memset (&module_data, 0, sizeof (module_data));

	get_module_event_data (assembly->image, &module_data);

	uint32_t assembly_flags = 0;
	if (assembly->dynamic)
		assembly_flags |= ASSEMBLY_FLAGS_DYNAMIC_ASSEMBLY;

	if (assembly->image && assembly->image->aot_module) {
		assembly_flags |= ASSEMBLY_FLAGS_NATIVE_ASSEMBLY;
	}

	char *assembly_name = mono_stringify_assembly_name (&assembly->aname);

	assembly_events_func (
		module_data.domain_id,
		module_data.assembly_id,
		assembly_flags,
		binding_id,
		(const ep_char8_t*)assembly_name,
		module_data.module_id,
		module_data.module_flags,
		module_data.reserved_flags,
		(const ep_char8_t *)module_data.module_il_path,
		(const ep_char8_t *)module_data.module_native_path,
		module_data.module_il_pdb_signature,
		module_data.module_il_pdb_age,
		(const ep_char8_t *)module_data.module_il_pdb_path,
		module_data.module_native_pdb_signature,
		module_data.module_native_pdb_age,
		(const ep_char8_t *)module_data.module_native_pdb_path,
		NULL);

	g_free (assembly_name);
}

static
gboolean
execute_rundown (
	fire_domain_rundown_events_func domain_events_func,
	fire_assembly_rundown_events_func assembly_events_func,
	fire_method_rundown_events_func method_events_func)
{
	EP_ASSERT (domain_events_func != NULL);
	EP_ASSERT (assembly_events_func != NULL);
	EP_ASSERT (method_events_func != NULL);

	// Under netcore we only have root domain.
	MonoDomain *root_domain = mono_get_root_domain ();
	if (root_domain) {
		uint64_t domain_id = (uint64_t)root_domain;

		// Emit all functions in use (JIT, AOT and Interpreter).
		FireMethodEventsData events_data;
		events_data.domain = root_domain;
		events_data.buffer_size = 1024 * sizeof(uint32_t);
		events_data.buffer = g_new (uint8_t, events_data.buffer_size);
		events_data.method_events_func = method_events_func;

		// All called JIT/AOT methods should be included in jit info table.
		mono_jit_info_table_foreach_internal (fire_method_events_callback, &events_data);

		// All called interpreted methods should be included in interpreter jit info table.
		if (mono_get_runtime_callbacks ()->is_interpreter_enabled())
			mono_get_runtime_callbacks ()->interp_jit_info_foreach (fire_method_events_callback, &events_data);

		// Phantom methods injected in callstacks representing runtime functions.
		if (_runtime_helper_compile_method_jitinfo && _runtime_helper_compile_method)
			fire_method_events (_runtime_helper_compile_method_jitinfo, _runtime_helper_compile_method, &events_data);
		if (_monitor_enter_method_jitinfo && _monitor_enter_method)
			fire_method_events (_monitor_enter_method_jitinfo, _monitor_enter_method, &events_data);
		if (_monitor_enter_v4_method_jitinfo && _monitor_enter_v4_method)
			fire_method_events (_monitor_enter_v4_method_jitinfo, _monitor_enter_v4_method, &events_data);

		g_free (events_data.buffer);

		// Iterate all assemblies in domain.
		GPtrArray *assemblies = mono_alc_get_all_loaded_assemblies ();
		if (assemblies) {
			for (uint32_t i = 0; i < assemblies->len; ++i) {
				MonoAssembly *assembly = (MonoAssembly *)g_ptr_array_index (assemblies, i);
				if (assembly)
					fire_assembly_events (root_domain, assembly, assembly_events_func);
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
walk_managed_stack_for_thread (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	StackWalkData *stack_walk_data)
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
		if (_runtime_helper_compile_method && stack_walk_data->top_frame)
			ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)_runtime_helper_compile_method), _runtime_helper_compile_method);
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
				} else if (_monitor_enter_method && in_monitor_enter_frame (wrapper)) {
					ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)_monitor_enter_method), _monitor_enter_method);
				} else if (_monitor_enter_v4_method && in_monitor_enter_v4_frame (wrapper)) {
					ep_stack_contents_append (stack_walk_data->stack_contents, (uintptr_t)((uint8_t*)_monitor_enter_v4_method), _monitor_enter_v4_method);
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
		EP_UNREACHABLE ("walk_managed_stack_for_thread");
		return FALSE;
	}
}

static
gboolean
walk_managed_stack_for_thread_callback (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data)
{
	return walk_managed_stack_for_thread (frame, ctx, (StackWalkData *)data);
}

static
gboolean
sample_profiler_walk_managed_stack_for_thread_callback (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data)
{
	EP_ASSERT (frame != NULL);
	EP_ASSERT (data != NULL);

	SampleProfileStackWalkData *sample_data = (SampleProfileStackWalkData *)data;

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

	return walk_managed_stack_for_thread (frame, ctx, &sample_data->stack_walk_data);
}

bool
ep_rt_mono_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents)
{
	EP_ASSERT (thread != NULL && stack_contents != NULL);

	StackWalkData stack_walk_data;
	stack_walk_data.stack_contents = stack_contents;
	stack_walk_data.top_frame = true;
	stack_walk_data.async_frame = false;
	stack_walk_data.safe_point_frame = false;
	stack_walk_data.runtime_invoke_frame = false;

	bool restore_async_context = FALSE;
	bool prevent_profiler_event_recursion = FALSE;
	EventPipeMonoThreadData *thread_data = ep_rt_mono_thread_data_get_or_create ();
	if (thread_data) {
		prevent_profiler_event_recursion = thread_data->prevent_profiler_event_recursion;
		if (prevent_profiler_event_recursion && !mono_thread_info_is_async_context ()) {
			// Running stackwalk in async context mode is currently the only way to prevent
			// unwinder to NOT load additional classes during stackwalk, making it signal unsafe and
			// potential triggering uncontrolled recursion in profiler class loading event.
			mono_thread_info_set_is_async_context (TRUE);
			restore_async_context = TRUE;
		}
		thread_data->prevent_profiler_event_recursion = TRUE;
	}

	if (thread == ep_rt_thread_get_handle () && mono_get_eh_callbacks ()->mono_walk_stack_with_ctx)
		mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (walk_managed_stack_for_thread_callback, NULL, MONO_UNWIND_SIGNAL_SAFE, &stack_walk_data);
	else if (mono_get_eh_callbacks ()->mono_walk_stack_with_state)
		mono_get_eh_callbacks ()->mono_walk_stack_with_state (walk_managed_stack_for_thread_callback, mono_thread_info_get_suspend_state (thread), MONO_UNWIND_SIGNAL_SAFE, &stack_walk_data);

	if (thread_data) {
		if (restore_async_context)
			mono_thread_info_set_is_async_context (FALSE);
		thread_data->prevent_profiler_event_recursion = prevent_profiler_event_recursion;
	}

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
	if (!_sampled_thread_callstacks)
		_sampled_thread_callstacks = g_array_sized_new (FALSE, FALSE, sizeof (SampleProfileStackWalkData), _max_sampled_thread_count);

	// Make sure there is room based on previous max number of sampled threads.
	// NOTE, there is a chance there are more threads than max, if that's the case we will
	// miss those threads in this sample, but will be included in next when max has been adjusted.
	g_array_set_size (_sampled_thread_callstacks, _max_sampled_thread_count);

	uint32_t filtered_thread_count = 0;
	uint32_t sampled_thread_count = 0;

	mono_stop_world (MONO_THREAD_INFO_FLAGS_NO_GC);

	bool restore_async_context = FALSE;
	if (!mono_thread_info_is_async_context ()) {
		mono_thread_info_set_is_async_context (TRUE);
		restore_async_context = TRUE;
	}

	// Record all info needed in sample events while runtime is suspended, must be async safe.
	FOREACH_THREAD_SAFE_EXCLUDE (thread_info, MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE) {
		if (!mono_thread_info_is_running (thread_info) && thread_info->jit_data) {
			MonoThreadUnwindState *thread_state = mono_thread_info_get_suspend_state (thread_info);
			if (thread_state->valid) {
				if (sampled_thread_count < _max_sampled_thread_count) {
					SampleProfileStackWalkData *data = &g_array_index (_sampled_thread_callstacks, SampleProfileStackWalkData, sampled_thread_count);
					data->thread_id = ep_rt_thread_id_t_to_uint64_t (mono_thread_info_get_tid (thread_info));
					data->thread_ip = (uintptr_t)MONO_CONTEXT_GET_IP (&thread_state->ctx);
					data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR;
					data->stack_walk_data.stack_contents = &data->stack_contents;
					data->stack_walk_data.top_frame = true;
					data->stack_walk_data.async_frame = false;
					data->stack_walk_data.safe_point_frame = false;
					data->stack_walk_data.runtime_invoke_frame = false;
					ep_stack_contents_reset (&data->stack_contents);
					mono_get_eh_callbacks ()->mono_walk_stack_with_state (sample_profiler_walk_managed_stack_for_thread_callback, thread_state, MONO_UNWIND_SIGNAL_SAFE, data);
					if (data->payload_data == EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL && (data->stack_walk_data.safe_point_frame || data->stack_walk_data.runtime_invoke_frame)) {
						// If classified as external code (managed->native frame on top of stack), but have a safe point or runtime invoke frame
						// as second, re-classify current callstack to be executing managed code.
						data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED;
					}
					if (data->stack_walk_data.top_frame && ep_stack_contents_get_length (&data->stack_contents) == 0) {
						// If no managed frames (including helper frames) are located on stack, mark sample as beginning in external code.
						// This can happen on attached embedding threads returning to native code between runtime invokes.
						// Make sure sample is still written into EventPipe for all attached threads even if they are currently not having
						// any managed frames on stack. Prevents some tools applying thread time heuristics to prolong duration of last sample
						// when embedding thread returns to native code. It also opens ability to visualize number of samples in unmanaged code
						// on attached threads when executing outside of runtime. If tooling is not interested in these sample events, they are easy
						// to identify and filter out.
						data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL;
					}

					sampled_thread_count++;
				}
			}
		}
		filtered_thread_count++;
	} FOREACH_THREAD_SAFE_END

	if (restore_async_context)
		mono_thread_info_set_is_async_context (FALSE);

	mono_restart_world (MONO_THREAD_INFO_FLAGS_NO_GC);

	// Fire sample event for threads. Must be done after runtime is resumed since it's not async safe.
	// Since we can't keep thread info around after runtime as been suspended, use an empty
	// adapter instance and only set recorded tid as parameter inside adapter.
	THREAD_INFO_TYPE adapter = { { 0 } };
	for (uint32_t thread_count = 0; thread_count < sampled_thread_count; ++thread_count) {
		SampleProfileStackWalkData *data = &g_array_index (_sampled_thread_callstacks, SampleProfileStackWalkData, thread_count);
		if ((data->stack_walk_data.top_frame && data->payload_data == EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL) || (data->payload_data != EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR && ep_stack_contents_get_length (&data->stack_contents) > 0)) {
			// Check if we have an async frame, if so we will need to make sure all frames are registered in regular jit info table.
			// TODO: An async frame can contain wrapper methods (no way to check during stackwalk), we could skip writing profile event
			// for this specific stackwalk or we could cleanup stack_frames before writing profile event.
			if (data->stack_walk_data.async_frame) {
				for (uint32_t frame_count = 0; frame_count < data->stack_contents.next_available_frame; ++frame_count)
					mono_jit_info_table_find_internal ((gpointer)data->stack_contents.stack_frames [frame_count], TRUE, FALSE);
			}
			mono_thread_info_set_tid (&adapter, ep_rt_uint64_t_to_thread_id_t (data->thread_id));
			uint32_t payload_data = ep_rt_val_uint32_t (data->payload_data);
			ep_write_sample_profile_event (sampling_thread, sampling_event, &adapter, &data->stack_contents, (uint8_t *)&payload_data, sizeof (payload_data));
		}
	}

	// Current thread count will be our next maximum sampled threads.
	_max_sampled_thread_count = filtered_thread_count;

	return true;
}

void
ep_rt_mono_execute_rundown (dn_vector_ptr_t *execution_checkpoints)
{
	ep_char8_t runtime_module_path [256];
	const uint8_t object_guid [EP_GUID_SIZE] = { 0 };
	const uint16_t runtime_product_qfe_version = 0;
	const uint8_t startup_flags = 0;
	const uint8_t startup_mode = 0;
	const ep_char8_t *command_line = "";

	if (!g_module_address ((void *)mono_init, runtime_module_path, sizeof (runtime_module_path), NULL, NULL, 0, NULL))
		runtime_module_path [0] = '\0';

	FireEtwRuntimeInformationDCStart (
		clr_instance_get_id (),
		RUNTIME_SKU_MONO,
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
		DN_VECTOR_PTR_FOREACH_BEGIN (EventPipeExecutionCheckpoint *, checkpoint, execution_checkpoints) {
			FireEtwExecutionCheckpointDCEnd (
				clr_instance_get_id (),
				checkpoint->name,
				checkpoint->timestamp,
				NULL,
				NULL);
		} DN_VECTOR_PTR_FOREACH_END;
	}

	FireEtwDCEndInit_V1 (
		clr_instance_get_id (),
		NULL,
		NULL);

	execute_rundown (
		fire_domain_rundown_events,
		fire_assembly_rundown_events,
		fire_method_rundown_events);

	FireEtwDCEndComplete_V1 (
		clr_instance_get_id (),
		NULL,
		NULL);
}

// Clear out BulkTypeValue before filling it out (array elements can get reused if there
// are enough types that we need to flush to multiple events).
static
void
bulk_type_value_clear (BulkTypeValue *bulk_type_value)
{
	memset (bulk_type_value, 0, sizeof(BulkTypeValue));
}

static
int
bulk_type_get_byte_count_in_event (BulkTypeValue *bulk_type_value)
{
	int name_len = bulk_type_value->name && bulk_type_value->name [0] != '\0'
		? GSIZE_TO_INT (strlen (bulk_type_value->name))
		: 0;

	// NOTE, must match manifest BulkType value type.
	return sizeof (bulk_type_value->fixed_sized_data.type_id) +
		sizeof (bulk_type_value->fixed_sized_data.module_id) +
		sizeof (bulk_type_value->fixed_sized_data.type_name_id) +
		sizeof (bulk_type_value->fixed_sized_data.flags) +
		sizeof (bulk_type_value->fixed_sized_data.cor_element_type) +
		(name_len + 1) * sizeof (ep_char16_t) +
		sizeof (bulk_type_value->type_parameters_count) +
		bulk_type_value->type_parameters_count * sizeof (uint64_t);
}

static
BulkTypeEventLogger*
bulk_type_event_logger_alloc (void)
{
	BulkTypeEventLogger *type_logger = g_malloc0 (sizeof (BulkTypeEventLogger));
	type_logger->bulk_type_event_buffer = g_malloc0 (sizeof (uint8_t) * MAX_SIZE_OF_EVENT_BUFFER);
	type_logger->mem_pool = mono_mempool_new ();

	dn_umap_custom_alloc_params_t params = {0, };
	params.value_dispose_func = g_free;
	type_logger->type_cache = dn_umap_custom_alloc (&params);

	return type_logger;
}

static
void
bulk_type_event_logger_free (BulkTypeEventLogger *type_logger)
{
	mono_mempool_destroy (type_logger->mem_pool);
	dn_umap_free (type_logger->type_cache);
	g_free (type_logger->bulk_type_event_buffer);
	g_free (type_logger);
}

//---------------------------------------------------------------------------------------
//
// fire_bulk_type_event fires an ETW event for all the types batched so far,
// it then resets the state to start batching new types at the beginning of the
// bulk_type_values array.
//
// This follows CoreCLR's BulkTypeEventLogger::FireBulkTypeEvent

static
void
bulk_type_fire_bulk_type_event (BulkTypeEventLogger *type_logger)
{
	if (type_logger->bulk_type_value_count == 0)
		return;

	uint16_t clr_instance_id = clr_instance_get_id ();

	uint32_t values_element_size = 0;

	uint8_t *ptr = type_logger->bulk_type_event_buffer;

	// NOTE, must match manifest BulkType value type.
	for (uint32_t type_value_index = 0; type_value_index < type_logger->bulk_type_value_count; type_value_index++) {
		BulkTypeValue *target = &type_logger->bulk_type_values [type_value_index];

		values_element_size += ep_write_buffer_uint64_t (&ptr, target->fixed_sized_data.type_id);
		values_element_size += ep_write_buffer_uint64_t (&ptr, target->fixed_sized_data.module_id);
		values_element_size += ep_write_buffer_uint32_t (&ptr, target->fixed_sized_data.type_name_id);
		values_element_size += ep_write_buffer_uint32_t (&ptr, target->fixed_sized_data.flags);
		values_element_size += ep_write_buffer_uint8_t (&ptr, target->fixed_sized_data.cor_element_type);

		uint32_t target_name_len = target->name && target->name [0] != '\0' ? GSIZE_TO_UINT32 (strlen (target->name)) : 0;
		values_element_size += write_buffer_string_utf8_to_utf16_t (&ptr, target->name, target_name_len);

		values_element_size += ep_write_buffer_uint32_t (&ptr, target->type_parameters_count);

		for (uint32_t i = 0; i < target->type_parameters_count; i++) {
			uint64_t type_parameter = get_typeid_for_type (target->mono_type_parameters [i]);
			values_element_size += ep_write_buffer_uint64_t (&ptr, type_parameter);
		}
	}

	FireEtwBulkType (
		type_logger->bulk_type_value_count,
		clr_instance_id,
		values_element_size,
		type_logger->bulk_type_event_buffer,
		NULL,
		NULL);

	memset (type_logger->bulk_type_event_buffer, 0, sizeof (uint8_t) * MAX_SIZE_OF_EVENT_BUFFER);
	type_logger->bulk_type_value_count = 0;
	type_logger->bulk_type_value_byte_count = 0;
}

//---------------------------------------------------------------------------------------
//
// get_typeid_for_type is responsible for obtaining the unique type identifier for a
// particular MonoType. MonoTypes are structs that are not unique pointers. There
// can be two different MonoTypes that both System.Thread or int32 or bool []. There
// is exactly one MonoClass * for any type, so we leverage the MonoClass a MonoType
// points to in order to obtain a unique type identifier in mono. With that unique
// MonoClass, its fields this_arg and _byval_arg are unique as well.
//
// Arguments:
//      * mono_type - MonoType to be logged
//
// Return Value:
//      type_id - Unique type identifier of mono_type

static
uint64_t
get_typeid_for_type (MonoType *t)
{
	if (m_type_is_byref (t))
		return (uint64_t)m_class_get_this_arg (mono_class_from_mono_type_internal (t));
	else
		return (uint64_t)m_class_get_byval_arg (mono_class_from_mono_type_internal (t));
}

static
uint64_t
get_typeid_for_class (MonoClass *c)
{
	return get_typeid_for_type (m_class_get_byval_arg (c));
}

//---------------------------------------------------------------------------------------
//
// bulk_type_log_single_type batches a single type into the bulk type array and flushes
// the array to ETW if it fills up. Most interaction with the type system (type analysis)
// is done here. This does not recursively batch up any parameter types (arrays or generics),
// but does add their unique identifiers to the mono_type_parameters array.
// ep_rt_mono_log_type_and_parameters is responsible for initiating any recursive calls to
// deal with type parameters.
//
// Arguments:
//	* type_logger - BulkTypeEventLogger instance
//      * mono_type - MonoType to be logged
//
// Return Value:
//      Index into array of where this type got batched. -1 if there was a failure.
//
// This follows CoreCLR's BulkTypeEventLogger::LogSingleType

static
int
bulk_type_log_single_type (
	BulkTypeEventLogger *type_logger,
	MonoType *mono_type)
{
	// If there's no room for another type, flush what we've got
	if (type_logger->bulk_type_value_count == K_MAX_COUNT_TYPE_VALUES)
		bulk_type_fire_bulk_type_event (type_logger);

	EP_ASSERT (type_logger->bulk_type_value_count < K_MAX_COUNT_TYPE_VALUES);

	BulkTypeValue *val = &type_logger->bulk_type_values [type_logger->bulk_type_value_count];
	bulk_type_value_clear (val);

	MonoClass *klass = mono_class_from_mono_type_internal (mono_type);
	MonoType *mono_underlying_type = mono_type_get_underlying_type (mono_type);

	// Initialize val fixed_sized_data
	val->fixed_sized_data.type_id = get_typeid_for_type (mono_type);
	val->fixed_sized_data.module_id = (uint64_t)m_class_get_image (klass);
	val->fixed_sized_data.type_name_id = m_class_get_type_token (klass) ? mono_metadata_make_token (MONO_TABLE_TYPEDEF, mono_metadata_token_index (m_class_get_type_token (klass))) : 0;
	if (mono_class_has_finalizer (klass))
		val->fixed_sized_data.flags |= TYPE_FLAGS_FINALIZABLE;
	if (m_class_is_delegate (klass))
		val->fixed_sized_data.flags |= TYPE_FLAGS_DELEGATE;
	val->fixed_sized_data.cor_element_type = (uint8_t)mono_underlying_type->type;

	// Sets val variable sized parameter type data, type_parameters_count, and mono_type_parameters associated
	// with arrays or generics to be recursively batched in the same ep_rt_mono_log_type_and_parameters call
	switch (mono_underlying_type->type) {
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	{
		MonoArrayType *mono_array_type = mono_type_get_array_type (mono_type);
		val->fixed_sized_data.flags |= TYPE_FLAGS_ARRAY;
		if (mono_underlying_type->type == MONO_TYPE_ARRAY) {
			// Only ranks less than TypeFlagsArrayRankMax are supported.
			// Fortunately TypeFlagsArrayRankMax should be greater than the
			// number of ranks the type loader will support
			uint32_t rank = mono_array_type->rank;
			if (rank < TYPE_FLAGS_ARRAY_RANK_MAX) {
				rank <<= 8;
				val->fixed_sized_data.flags |= rank;
			}
		}

		// mono arrays are always arrays of by value types
		val->mono_type_parameters = mono_mempool_alloc0 (type_logger->mem_pool, 1 * sizeof (MonoType*));
		*val->mono_type_parameters = m_class_get_byval_arg (mono_array_type->eklass);
		val->type_parameters_count++;
		break;
	}
	case MONO_TYPE_GENERICINST:
	{
		MonoGenericInst *class_inst = mono_type->data.generic_class->context.class_inst;
		val->type_parameters_count = class_inst->type_argc;
		val->mono_type_parameters = mono_mempool_alloc0 (type_logger->mem_pool, val->type_parameters_count * sizeof (MonoType*));
		memcpy (val->mono_type_parameters, class_inst->type_argv, val->type_parameters_count * sizeof (MonoType*));
		break;
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_PTR:
	case MONO_TYPE_BYREF:
	{
		if (mono_underlying_type == mono_type)
			break;
		val->mono_type_parameters = mono_mempool_alloc0 (type_logger->mem_pool, 1 * sizeof (MonoType*));
		*val->mono_type_parameters = mono_underlying_type;
		val->type_parameters_count++;
		break;
	}
	default:
		break;
	}

	val->name = "";
	bool log_type_name = is_keyword_and_level_enabled (&RUNTIME_PROVIDER_CONTEXT, EP_EVENT_LEVEL_INFORMATIONAL, GC_HEAP_AND_TYPE_NAMES_KEYWORD);
	if (type_logger->type_cache && log_type_name) {
		dn_umap_it_t result = dn_umap_find (type_logger->type_cache, GUINT64_TO_POINTER (val->fixed_sized_data.type_id));
		if (dn_umap_it_end (result) || !dn_umap_it_value (result)) {
			dn_umap_result_t insert = dn_umap_insert_or_assign (type_logger->type_cache, GUINT64_TO_POINTER (val->fixed_sized_data.type_id), mono_type_get_name_full (mono_type, MONO_TYPE_NAME_FORMAT_IL));
			if (insert.result)
				result = insert.it;
		}
		val->name = !dn_umap_it_end (result) ? (const ep_char8_t *)dn_umap_it_value (result) : "";
	}

	// Now that we know the full size of this type's data, see if it fits in our
	// batch or whether we need to flush
	int val_byte_count = bulk_type_get_byte_count_in_event (val);
	if (val_byte_count > MAX_TYPE_VALUES_BYTES) {
		// NOTE: If name is actively used, set it to NULL and relevant memory management to reduce byte count
		// This type is apparently so huge, it's too big to squeeze into an event, even
		// if it were the only type batched in the whole event.  Bail
		mono_trace (G_LOG_LEVEL_ERROR, MONO_TRACE_DIAGNOSTICS, "Failed to log single mono type %p with typeID %llu. Type is too large for the BulkType Event.\n", (gpointer)mono_type, (unsigned long long)val->fixed_sized_data.type_id);
		return -1;
	}

	if (type_logger->bulk_type_value_byte_count + val_byte_count > MAX_TYPE_VALUES_BYTES) {
		// Although this type fits into the array, its size is so big that the entire
		// array can't be logged via ETW. So flush the array, and start over by
		// calling ourselves--this refetches the type info and puts it at the
		// beginning of the array.  Since we know this type is small enough to be
		// batched into an event on its own, this recursive call will not try to
		// call itself again.
		g_assert (type_logger->bulk_type_value_byte_count + val_byte_count > MAX_TYPE_VALUES_BYTES);
		bulk_type_fire_bulk_type_event (type_logger);
		return bulk_type_log_single_type (type_logger, mono_type);
	}

	// The type fits into the batch, so update our state
	type_logger->bulk_type_value_count++;
	type_logger->bulk_type_value_byte_count += val_byte_count;
	return type_logger->bulk_type_value_count - 1;
}

//---------------------------------------------------------------------------------------
//
// High-level method to batch a type and (recursively) its type parameters, flushing to
// ETW as needed.  This is called by bulk_type_log_type_and_parameters_if_necessary.
//
// Arguments:
//	* type_logger - BulkTypeEventLogger instance
//      * mono_type - MonoType to be logged
//      log_behavior - Describe how type should be logged.
//
// This follows CoreCLR's BulkTypeEventLogger::LogTypeAndParameter

static
void
bulk_type_log_type_and_parameters (
	BulkTypeEventLogger *type_logger,
	MonoType *mono_type,
	TypeLogBehavior log_behavior)
{
	// Batch up this type.  This grabs useful info about the type, including any
	// type parameters it may have, and sticks it in bulk_type_values
	int bulk_type_value_index = bulk_type_log_single_type (type_logger, mono_type);
	if (bulk_type_value_index == -1) {
		// There was a failure trying to log the type, so don't bother with its type
		// parameters
		return;
	}

	// Look at the type info we just batched, so we can get the type parameters
	BulkTypeValue *val = &type_logger->bulk_type_values [bulk_type_value_index];

	// We're about to recursively call ourselves for the type parameters, so make a
	// local copy of their type handles first (else, as we log them we could flush
	// and clear out bulk_type_values, thus trashing val)
	uint32_t param_count = val->type_parameters_count;
	if (param_count == 0)
		return;

	MonoType **mono_type_parameters = mono_mempool_alloc0 (type_logger->mem_pool, param_count * sizeof (MonoType*));
	memcpy (mono_type_parameters, val->mono_type_parameters, sizeof (MonoType*) * param_count);

	if (log_behavior == TYPE_LOG_BEHAVIOR_ALWAYS_LOG_TOP_LEVEL)
		log_behavior = TYPE_LOG_BEHAVIOR_IF_FIRST_TIME;

	for (uint32_t i = 0; i < param_count; i++)
		bulk_type_log_type_and_parameters_if_necessary (type_logger, mono_type_parameters [i], log_behavior);
}

//---------------------------------------------------------------------------------------
//
// Outermost level of ETW-type-logging.  This method is used to log a unique type identifier
// (in this case a MonoType) and (recursively) its type parameters when present.
//
// Arguments:
//	* type_logger - BulkTypeEventLogger instance
//      * mono_type - MonoType to be logged
//      log_behavior - Describe how type should be logged.
//
// This follows CoreCLR's BulkTypeEventLogger::LogTypeAndParameters

static
void
bulk_type_log_type_and_parameters_if_necessary (
	BulkTypeEventLogger *type_logger,
	MonoType *mono_type,
	TypeLogBehavior log_behavior)
{
	if (!is_keyword_and_level_enabled (&RUNTIME_PROVIDER_CONTEXT, EP_EVENT_LEVEL_INFORMATIONAL, TYPE_KEYWORD))
		return;

	bool log_type = (log_behavior == TYPE_LOG_BEHAVIOR_ALWAYS_LOG || log_behavior == TYPE_LOG_BEHAVIOR_ALWAYS_LOG_TOP_LEVEL);

	if (!log_type && type_logger) {
		uint64_t type_id = get_typeid_for_type (mono_type);
		dn_umap_result_t result = dn_umap_insert (type_logger->type_cache, GUINT64_TO_POINTER (type_id), NULL);
		log_type = result.result;
	}

	if (!log_type)
		return;

	if (type_logger)
		bulk_type_log_type_and_parameters (type_logger, mono_type, log_behavior);
}

//---------------------------------------------------------------------------------------
//
// write_method_details_event is the method responsible for sending details of
// methods involved in events such as JitStart, Load/Unload, Rundown, R2R, and other
// eventpipe events. It calls ep_rt_mono_log_type_and_parameters_if_necessary to log
// unique types from the method type and available method instantiation parameter types
// that are ultimately emitted as a BulkType event in ep_rt_mono_fire_bulk_type_event.
// After appropriately logging type information, it sends method details outlined by
// the generated dotnetruntime.c and ClrEtwAll manifest.
//
// Arguments:
//      * method - a MonoMethod hit during an eventpipe event
//
// This follows CoreCLR's ETW::MethodLog::SendMethodDetailsEvent

static
void
write_event_method_details (MonoMethod *method)
{
	if (method->wrapper_type != MONO_WRAPPER_NONE || method->dynamic)
		return;

	MonoGenericContext *method_ctx = mono_method_get_context (method);

	MonoGenericInst *method_inst = NULL;
	if (method_ctx)
		method_inst = method_ctx->method_inst;

	if (method_inst && method_inst->type_argc > MAX_METHOD_TYPE_ARGUMENT_COUNT)
		return;

	BulkTypeEventLogger *type_logger = bulk_type_event_logger_alloc ();

	uint64_t method_type_id = 0;
	g_assert (mono_metadata_token_index (method->token) != 0);
	uint32_t method_token = mono_metadata_make_token (MONO_TABLE_METHOD, mono_metadata_token_index (method->token));
	uint64_t loader_module_id = 0;
	MonoClass *klass = method->klass;
	if (klass) {
		MonoType *method_mono_type = m_class_get_byval_arg (klass);
		method_type_id = get_typeid_for_class (klass);

		bulk_type_log_type_and_parameters_if_necessary (type_logger, method_mono_type, TYPE_LOG_BEHAVIOR_ALWAYS_LOG);

		loader_module_id = (uint64_t)mono_class_get_image (klass);
	}

	uint32_t method_inst_parameter_types_count = 0;
	if (method_inst)
		method_inst_parameter_types_count = method_inst->type_argc;

	uint64_t *method_inst_parameters_type_ids = mono_mempool_alloc0 (type_logger->mem_pool, method_inst_parameter_types_count * sizeof (uint64_t));
	uint8_t *buffer = (uint8_t *)method_inst_parameters_type_ids;
	for (uint32_t i = 0; i < method_inst_parameter_types_count; i++) {
		ep_write_buffer_uint64_t (&buffer, get_typeid_for_type (method_inst->type_argv [i]));

		bulk_type_log_type_and_parameters_if_necessary (type_logger, method_inst->type_argv [i], TYPE_LOG_BEHAVIOR_ALWAYS_LOG);
	}

	bulk_type_fire_bulk_type_event (type_logger);

	FireEtwMethodDetails (
		(uint64_t)method,
		method_type_id,
		method_token,
		method_inst_parameter_types_count,
		loader_module_id,
		(uint64_t*)method_inst_parameters_type_ids,
		NULL,
		NULL);

	bulk_type_event_logger_free (type_logger);
}

static
bool
write_event_jit_start (MonoMethod *method)
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

		write_event_method_details (method);

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
		method_signature = mono_signature_full_name (mono_method_signature_internal (method));

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

static
bool
write_event_method_il_to_native_map (
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

		uint32_t offset_entries = 0;
		uint32_t *il_offsets = NULL;
		uint32_t *native_offsets = NULL;

		MonoDebugMethodJitInfo *debug_info = method ? mono_debug_find_method (method, root_domain) : NULL;
		if (debug_info) {
			offset_entries = debug_info->num_line_numbers;
			if (offset_entries != 0) {
				size_t needed_size = (offset_entries * sizeof (uint32_t) * 2);
				if (needed_size > sizeof (fixed_buffer)) {
					buffer = g_new (uint8_t, needed_size);
					il_offsets = (uint32_t*)buffer;
				} else {
					il_offsets = fixed_buffer;
				}
				if (il_offsets) {
					native_offsets = il_offsets + offset_entries;
					uint8_t *il_offsets_ptr = (uint8_t *)il_offsets;
					uint8_t *native_offsets_ptr = (uint8_t *)native_offsets;
					for (uint32_t offset_count = 0; offset_count < offset_entries; ++offset_count) {
						ep_write_buffer_uint32_t (&il_offsets_ptr, debug_info->line_numbers [offset_count].il_offset);
						ep_write_buffer_uint32_t (&native_offsets_ptr, debug_info->line_numbers [offset_count].native_offset);
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
			native_offsets [0] = ji ? ep_rt_val_uint32_t ((uint32_t)ji->code_size) : 0;
		}

		FireEtwMethodILToNativeMap (
			method_id,
			0,
			0,
			GUINT32_TO_UINT16 (offset_entries),
			il_offsets,
			native_offsets,
			clr_instance_get_id (),
			NULL,
			NULL);

		g_free (buffer);
	}

	return true;
}

static
bool
write_event_method_load (
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
		bool verbose = (RUNTIME_PROVIDER_CONTEXT.Level >= (uint8_t)EP_EVENT_LEVEL_VERBOSE);

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

		write_event_method_details (method);

		if (verbose) {
			method_name = method->name;
			method_signature = mono_signature_full_name (mono_method_signature_internal (method));

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
write_event_module_load (MonoImage *image)
{
	if (!EventEnabledModuleLoad_V2 () && !EventEnabledDomainModuleLoad_V1 ())
		return true;

	if (image) {
		ModuleEventData module_data;
		memset (&module_data, 0, sizeof (module_data));
		if (get_module_event_data (image, &module_data)) {
			FireEtwModuleLoad_V2 (
				module_data.module_id,
				module_data.assembly_id,
				module_data.module_flags,
				module_data.reserved_flags,
				module_data.module_il_path,
				module_data.module_native_path,
				clr_instance_get_id (),
				module_data.module_il_pdb_signature,
				module_data.module_il_pdb_age,
				module_data.module_il_pdb_path,
				module_data.module_native_pdb_signature,
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

static
bool
write_event_module_unload (MonoImage *image)
{
	if (!EventEnabledModuleUnload_V2())
		return true;

	if (image) {
		ModuleEventData module_data;
		memset (&module_data, 0, sizeof (module_data));
		if (get_module_event_data (image, &module_data)) {
			FireEtwModuleUnload_V2 (
				module_data.module_id,
				module_data.assembly_id,
				module_data.module_flags,
				module_data.reserved_flags,
				module_data.module_il_path,
				module_data.module_native_path,
				clr_instance_get_id (),
				module_data.module_il_pdb_signature,
				module_data.module_il_pdb_age,
				module_data.module_il_pdb_path,
				module_data.module_native_pdb_signature,
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

static
bool
write_event_assembly_load (MonoAssembly *assembly)
{
	if (!EventEnabledAssemblyLoad_V1 ())
		return true;

	if (assembly) {
		AssemblyEventData assembly_data;
		memset (&assembly_data, 0, sizeof (assembly_data));
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

static
bool
write_event_assembly_unload (MonoAssembly *assembly)
{
	if (!EventEnabledAssemblyUnload_V1 ())
		return true;

	if (assembly) {
		AssemblyEventData assembly_data;
		memset (&assembly_data, 0, sizeof (assembly_data));
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

static
bool
write_event_thread_created (ep_rt_thread_id_t tid)
{
	if (!EventEnabledThreadCreated ())
		return true;

	uint64_t managed_thread = 0;
	uint32_t native_thread_id = MONO_NATIVE_THREAD_ID_TO_UINT (tid);
	uint32_t managed_thread_id = 0;
	uint32_t flags = 0;

	MonoThread *thread = mono_thread_current ();
	if (thread && mono_thread_info_get_tid (thread->thread_info) == tid) {
		managed_thread_id = mono_thread_get_managed_id (thread);
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

static
bool
write_event_thread_terminated (ep_rt_thread_id_t tid)
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

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
	// Mix in highest bits on 64-bit systems only
	if (sizeof (type) > 4)
		start_id = start_id ^ GUINT64_TO_UINT32 ((((uint64_t)type >> 31) >> 1));
MONO_RESTORE_WARNING

	return start_id;
}

static
bool
write_event_type_load_start (MonoType *type)
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

static
bool
write_event_type_load_stop (MonoType *type)
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

static
bool
write_event_exception_thrown (MonoObject *obj)
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
			if (exception->message)
				exception_message = ep_rt_utf16_to_utf8_string_n (mono_string_chars_internal (exception->message), mono_string_length_internal (exception->message));
			hresult = exception->hresult;
		}

		if (exception_message == NULL)
			exception_message = g_strdup ("");

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

static
bool
write_event_exception_clause (
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

static
bool
write_event_monitor_contention_start (MonoObject *obj)
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

static
bool
write_event_monitor_contention_stop (MonoObject *obj)
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

static
bool
write_event_method_jit_memory_allocated_for_code (
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
ep_rt_write_event_ee_startup_start (void)
{
	return FireEtwEEStartupStart_V1 (
		clr_instance_get_id (),
		NULL,
		NULL);
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
ep_rt_write_event_threadpool_min_max_threads (
	uint16_t min_worker_threads,
	uint16_t max_worker_threads,
	uint16_t min_io_completion_threads,
	uint16_t max_io_completion_threads,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolMinMaxThreads (
		min_worker_threads,
		max_worker_threads,
		min_io_completion_threads,
		max_io_completion_threads,
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

bool
ep_rt_write_event_threadpool_io_pack (
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id)
{
	return FireEtwThreadPoolIOPack (
		(const void *)native_overlapped,
		(const void *)overlapped,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_contention_lock_created (
	intptr_t lock_id,
	intptr_t associated_object_id,
	uint16_t clr_instance_id)
{
	return FireEtwContentionLockCreated (
		(const void *)lock_id,
		(const void *)associated_object_id,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_contention_start (
	uint8_t contention_flags,
	uint16_t clr_instance_id,
	intptr_t lock_id,
	intptr_t associated_object_id,
	uint64_t lock_owner_thread_id)
{
	return FireEtwContentionStart_V2 (
		contention_flags,
		clr_instance_id,
		(const void *)lock_id,
		(const void *)associated_object_id,
		lock_owner_thread_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_contention_stop (
	uint8_t contention_flags,
	uint16_t clr_instance_id,
	double duration_ns)
{
	return FireEtwContentionStop_V1 (
		contention_flags,
		clr_instance_id,
		duration_ns,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_wait_handle_wait_start (
	uint8_t wait_source,
	intptr_t associated_object_id,
	uint16_t clr_instance_id)
{
	return FireEtwWaitHandleWaitStart (
		wait_source,
		(const void *)associated_object_id,
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

bool
ep_rt_write_event_wait_handle_wait_stop (
	uint16_t clr_instance_id)
{
	return FireEtwWaitHandleWaitStop (
		clr_instance_id,
		NULL,
		NULL) == 0 ? true : false;
}

static
void
jit_begin_callback (
	MonoProfiler *prof,
	MonoMethod *method)
{
	write_event_jit_start (method);
}

static
void
jit_failed_callback (
	MonoProfiler *prof,
	MonoMethod *method)
{
	//TODO: CoreCLR doesn't have this case, so no failure event currently exists.
}

static
void
jit_done_callback (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoJitInfo *ji)
{
	write_event_method_load (method, ji);
	write_event_method_il_to_native_map (method, ji);
}

static
void
image_loaded_callback (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (image && image->heap_pdb.size == 0)
		write_event_module_load (image);
}

static
void
image_unloaded_callback (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (image && image->heap_pdb.size == 0)
		write_event_module_unload (image);
}

static
void
assembly_loaded_callback (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	write_event_assembly_load (assembly);
}

static
void
assembly_unloaded_callback (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	write_event_assembly_unload (assembly);
}

static
void
thread_started_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	write_event_thread_created (ep_rt_uint64_t_to_thread_id_t (tid));
}

static
void
thread_stopped_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	write_event_thread_terminated (ep_rt_uint64_t_to_thread_id_t (tid));
}

static
void
class_loading_callback (
	MonoProfiler *prof,
	MonoClass *klass)
{
	bool prevent_profiler_event_recursion = FALSE;
	EventPipeMonoThreadData *thread_data = ep_rt_mono_thread_data_get_or_create ();
	if (thread_data) {
		// Prevent additional class loading to happen recursively as part of fire TypeLoadStart event.
		// Additional class loading can happen as part of capturing callstack for TypeLoadStart event.
		prevent_profiler_event_recursion = thread_data->prevent_profiler_event_recursion;
		thread_data->prevent_profiler_event_recursion = TRUE;
	}

	write_event_type_load_start (m_class_get_byval_arg (klass));

	if (thread_data)
		thread_data->prevent_profiler_event_recursion = prevent_profiler_event_recursion;
}

static
void
class_failed_callback (
	MonoProfiler *prof,
	MonoClass *klass)
{
	write_event_type_load_stop (m_class_get_byval_arg (klass));
}

static
void
class_loaded_callback (
	MonoProfiler *prof,
	MonoClass *klass)
{
	write_event_type_load_stop (m_class_get_byval_arg (klass));
}

static
void
exception_throw_callback (
	MonoProfiler *prof,
	MonoObject *exc)
{
	write_event_exception_thrown (exc);
}

static
void
exception_clause_callback (
	MonoProfiler *prof,
	MonoMethod *method,
	uint32_t clause_num,
	MonoExceptionEnum clause_type,
	MonoObject *exc)
{
	write_event_exception_clause (method, clause_num, clause_type, exc);
}

static
void
monitor_contention_callback (
	MonoProfiler *prof,
	MonoObject *obj)
{
	write_event_monitor_contention_start (obj);
}

static
void
monitor_acquired_callback (
	MonoProfiler *prof,
	MonoObject *obj)
{
	write_event_monitor_contention_stop (obj);
}

static
void
monitor_failed_callback (
	MonoProfiler *prof,
	MonoObject *obj)
{
	write_event_monitor_contention_stop (obj);
}

static
void
jit_code_buffer_callback (
	MonoProfiler *prof,
	const mono_byte *buffer,
	uint64_t size,
	MonoProfilerCodeBufferType type,
	const void *data)
{
	write_event_method_jit_memory_allocated_for_code ((const uint8_t *)buffer, size, type, data);
}

static
uint32_t
gc_heap_dump_requests_inc (void)
{
	EP_ASSERT (ep_rt_mono_is_runtime_initialized ());
	return ep_rt_atomic_inc_uint32_t (&_gc_heap_dump_requests);
}

static
uint32_t
gc_heap_dump_requests_dec (void)
{
	EP_ASSERT (ep_rt_mono_is_runtime_initialized ());
	return ep_rt_atomic_dec_uint32_t (&_gc_heap_dump_requests);
}

static
bool
gc_heap_dump_requested (void)
{
	if (!ep_rt_mono_is_runtime_initialized ())
		return false;
	return ep_rt_volatile_load_uint32_t(&_gc_heap_dump_requests) != 0 ? true : false;
}

static
uint32_t
gc_heap_dump_count_inc (void)
{
	EP_ASSERT (ep_rt_mono_is_runtime_initialized ());
	return ep_rt_atomic_inc_uint32_t (&_gc_heap_dump_count);
}

static
bool
gc_heap_dump_mem_file_buffer_init (GCHeapDumpMemFileBuffer *file_buffer)
{
	// Called on GC thread so no need to call MONO_ENTER_GC_SAFE/MONO_EXIT_GC_SAFE
	// around IO functions.

	EP_ASSERT (file_buffer);

	file_buffer->fd = g_file_open_tmp ("mono_gc_heap_dump_XXXXXX", &file_buffer->name, NULL);
	file_buffer->start = g_malloc (MAX_EVENT_BYTE_COUNT);
	file_buffer->current = file_buffer->start;
	file_buffer->end = file_buffer->start + MAX_EVENT_BYTE_COUNT;

	return file_buffer->fd != -1 && file_buffer->start;
}

#ifndef g_close
#ifdef G_OS_WIN32
#define g_close _close
#else
#define g_close close
#endif
#endif

static
void
gc_heap_dump_mem_file_buffer_fini (GCHeapDumpMemFileBuffer *file_buffer)
{
	// Called on GC thread so no need to call MONO_ENTER_GC_SAFE/MONO_EXIT_GC_SAFE
	// around IO functions.

	if (!file_buffer)
		return;

	if (file_buffer->fd != -1) {
		g_close (file_buffer->fd);
		if (file_buffer->name) {
			g_unlink (file_buffer->name);
			g_free (file_buffer->name);
		}
		g_free (file_buffer->start);
	}
}

static
bool
gc_heap_dump_mem_file_buffer_write_all (
	int fd,
	const uint8_t *buffer,
	size_t len)
{
	// Called on GC thread (during GC) while holding GC locks,
	// before/during/after STW so no need to MONO_ENTER_GC_SAFE/MONO_EXIT_GC_SAFE
	// around async safe IO functions.

	size_t offset = 0;
	int nwritten;

	do {
		nwritten = g_write (fd, buffer + offset, GSIZE_TO_UINT32 (len - offset));
		if (nwritten > 0)
			offset += nwritten;
	} while ((nwritten > 0 && offset < len) || (nwritten == -1 && errno == EINTR));

	return nwritten == len;
}

static
bool
gc_heap_dump_mem_file_buffer_flush (GCHeapDumpMemFileBuffer *file_buffer)
{
	EP_ASSERT (file_buffer);
	EP_ASSERT (file_buffer->fd != -1);
	EP_ASSERT (file_buffer->start);

	bool result = true;
	uint64_t size = (uint64_t)(file_buffer->current - file_buffer->start);

	result &= gc_heap_dump_mem_file_buffer_write_all (file_buffer->fd, (const uint8_t *)&size, sizeof (size));
	result &= gc_heap_dump_mem_file_buffer_write_all (file_buffer->fd, file_buffer->start, GUINT64_TO_SIZE (size));

	file_buffer->current = file_buffer->start;

	return result;
}

#ifndef g_lseek
#ifdef G_OS_WIN32
#define g_lseek _lseek
#else
#define g_lseek lseek
#endif
#endif

static
bool
gc_heap_dump_mem_file_buffer_reset_func (void *context)
{
	// Called on GC thread (during GC) while holding GC locks,
	// but after STW so no need to MONO_ENTER_GC_SAFE/MONO_EXIT_GC_SAFE
	// around async safe IO functions.

	EP_ASSERT (context);

	bool result = true;
	GCHeapDumpMemFileBuffer *file_buffer = (GCHeapDumpMemFileBuffer *)context;

	EP_ASSERT (file_buffer->fd != -1);

	result &= gc_heap_dump_mem_file_buffer_flush (file_buffer);
	result &= g_lseek (file_buffer->fd, 0, SEEK_SET) != -1;

	return result;
}

static
uint8_t *
gc_heap_dump_mem_file_buffer_alloc_func (
	void *context,
	size_t size)
{
	EP_ASSERT (context);

	GCHeapDumpMemFileBuffer *file_buffer = (GCHeapDumpMemFileBuffer *)context;

	if (size > MAX_EVENT_BYTE_COUNT)
		return NULL;

	if (file_buffer->current + size >= file_buffer->end)
		if (!gc_heap_dump_mem_file_buffer_flush (file_buffer))
			return NULL;

	uint8_t *result = file_buffer->current;
	file_buffer->current = file_buffer->current + size;
	return result;
}

static
bool
gc_heap_dump_mem_file_buffer_read (
	int fd,
	uint8_t *buffer,
	size_t len)
{
	// Called on GC thread (during GC) while holding GC locks,
	// but after STW so no need to MONO_ENTER_GC_SAFE/MONO_EXIT_GC_SAFE
	// around async safe IO functions.

	size_t offset = 0;
	int nread;

	do {
		nread = g_read (fd, buffer + offset, GSIZE_TO_UINT32 (len - offset));
		if (nread > 0)
			offset += nread;
	} while ((nread > 0 && offset < len) || (nread == -1 && errno == EINTR));

	return nread == len;
}

static
const uint8_t *
gc_heap_dump_mem_file_buffer_get_next_buffer_func (
	void *context,
	size_t *next_buffer_size)
{
	EP_ASSERT (context);

	GCHeapDumpMemFileBuffer *file_buffer = (GCHeapDumpMemFileBuffer *)context;

	bool result = false;

	uint64_t max_size = (uint64_t)(file_buffer->end - file_buffer->start);
	uint64_t size = 0;

	EP_ASSERT (file_buffer->fd != -1);

	if (gc_heap_dump_mem_file_buffer_read (file_buffer->fd, (uint8_t *)&size, sizeof (size)))
		if (size <= max_size)
			result = gc_heap_dump_mem_file_buffer_read (file_buffer->fd, file_buffer->start, GUINT64_TO_SIZE (size));

	file_buffer->current = file_buffer->start;
	*next_buffer_size = GUINT64_TO_SIZE (size);

	return result ? file_buffer->start : NULL;
}

static
GCHeapDumpBuffer *
gc_heap_dump_context_buffer_alloc (void)
{
	GCHeapDumpMemFileBuffer *file_buffer = g_new0 (GCHeapDumpMemFileBuffer, 1);
	GCHeapDumpBuffer *buffer = g_new0 (GCHeapDumpBuffer, 1);

	buffer->context = file_buffer;
	buffer->reset_func = gc_heap_dump_mem_file_buffer_reset_func;
	buffer->alloc_func = gc_heap_dump_mem_file_buffer_alloc_func;
	buffer->get_next_buffer_func = gc_heap_dump_mem_file_buffer_get_next_buffer_func;

	if (!gc_heap_dump_mem_file_buffer_init (file_buffer)) {
		gc_heap_dump_mem_file_buffer_fini (file_buffer);
		buffer = NULL;
	}

	return buffer;
}

static
void
gc_heap_dump_context_buffer_free (GCHeapDumpBuffer *buffer)
{
	if (!buffer)
		return;

	gc_heap_dump_mem_file_buffer_fini ((GCHeapDumpMemFileBuffer *)buffer->context);

	g_free (buffer->context);
	g_free (buffer);
}

static
bool
gc_heap_dump_context_alloc_bulk_data (
	GCHeapDumpBulkData *data,
	size_t len,
	size_t count)
{
	data->data_start = g_malloc (len);
	data->data_current = data->data_start;
	data->data_end = data->data_start + len;
	data->max_count = GSIZE_TO_UINT32 (count);
	data->index = 0;
	data->count = 0;

	return data->data_start;
}

static
void
gc_heap_dump_context_free_bulk_data (GCHeapDumpBulkData *data)
{
	g_free (data->data_start);
}

static
void
gc_heap_dump_context_clear_bulk_data (GCHeapDumpBulkData *data)
{
	if (data->data_start)
		memset (data->data_start, 0, data->data_end - data->data_start);
	data->data_current = data->data_start;
	data->count = 0;
}

static
bool
gc_heap_dump_context_init (
	GCHeapDumpContext *context,
	EVENTPIPE_TRACE_CONTEXT trace_context,
	uint32_t gc_reason,
	uint32_t gc_type,
	uint32_t gc_count,
	uint32_t gc_depth)
{
	EP_ASSERT (context);

	bool result = true;

	context->trace_context = trace_context;
	context->gc_reason = gc_reason;
	context->gc_type = gc_type;
	context->gc_count = gc_count;
	context->gc_depth = gc_depth;
	context->retry_count = 0;
	context->state = GC_HEAP_DUMP_CONTEXT_STATE_INIT;

	if (is_gc_heap_dump_enabled (context)) {
		context->bulk_type_logger = bulk_type_event_logger_alloc ();

		context->buffer = gc_heap_dump_context_buffer_alloc ();

		const size_t bulk_nodes_max_data_len = (MAX_EVENT_BYTE_COUNT - 0x100);
		const size_t bulk_nodes_max_count = bulk_nodes_max_data_len / BULK_NODE_EVENT_TYPE_SIZE;

		const size_t bulk_edges_max_data_len = (MAX_EVENT_BYTE_COUNT - 0x100);
		const size_t bulk_edges_max_count = bulk_edges_max_data_len / BULK_EDGE_EVENT_TYPE_SIZE;

		const size_t bulk_root_edges_max_data_len = (MAX_EVENT_BYTE_COUNT - 0x100);
		const size_t bulk_root_edges_max_count = bulk_root_edges_max_data_len / BULK_ROOT_EDGE_EVENT_TYPE_SIZE;

		const size_t bulk_root_cwt_elem_edges_max_data_len = (MAX_EVENT_BYTE_COUNT - 0x100);
		const size_t bulk_root_cwt_elem_edges_max_count = bulk_root_cwt_elem_edges_max_data_len / BULK_ROOT_CWT_ELEM_EDGE_EVENT_TYPE_SIZE;

		const size_t bulk_root_static_vars_max_data_len = (MAX_EVENT_BYTE_COUNT - 0x30);
		const size_t bulk_root_static_vars_max_count = bulk_root_static_vars_max_data_len / BULK_ROOT_STATIC_VAR_EVENT_TYPE_SIZE;

		gc_heap_dump_context_alloc_bulk_data (&context->bulk_nodes, bulk_nodes_max_data_len, bulk_nodes_max_count);
		gc_heap_dump_context_alloc_bulk_data (&context->bulk_edges, bulk_edges_max_data_len, bulk_edges_max_count);
		gc_heap_dump_context_alloc_bulk_data (&context->bulk_root_edges, bulk_root_edges_max_data_len, bulk_root_edges_max_count);
		gc_heap_dump_context_alloc_bulk_data (&context->bulk_root_cwt_elem_edges, bulk_root_cwt_elem_edges_max_data_len, bulk_root_cwt_elem_edges_max_count);
		gc_heap_dump_context_alloc_bulk_data (&context->bulk_root_static_vars, bulk_root_static_vars_max_data_len, bulk_root_static_vars_max_count);

		result = context->bulk_type_logger &&
			context->buffer &&
			context->bulk_nodes.data_start &&
			context->bulk_edges.data_start &&
			context->bulk_root_edges.data_start &&
			context->bulk_root_cwt_elem_edges.data_start &&
			context->bulk_root_static_vars.data_start;
	}

	return result;
}

static
void
gc_heap_dump_context_fini (GCHeapDumpContext *context)
{
	if (context) {
		if (is_gc_heap_dump_enabled (context)) {
			if (context->gc_roots)
				dn_vector_ptr_free (context->gc_roots);

			gc_heap_dump_context_free_bulk_data (&context->bulk_root_static_vars);
			gc_heap_dump_context_free_bulk_data (&context->bulk_root_cwt_elem_edges);
			gc_heap_dump_context_free_bulk_data (&context->bulk_root_edges);
			gc_heap_dump_context_free_bulk_data (&context->bulk_edges);
			gc_heap_dump_context_free_bulk_data (&context->bulk_nodes);

			gc_heap_dump_context_buffer_free (context->buffer);

			if (context->bulk_type_logger)
				bulk_type_event_logger_free (context->bulk_type_logger);
		}

		memset (context, 0, sizeof (GCHeapDumpContext));
	}
}

static
GCHeapDumpContext *
gc_heap_dump_context_alloc (
	EVENTPIPE_TRACE_CONTEXT trace_context,
	uint32_t gc_reason,
	uint32_t gc_type,
	uint32_t gc_count,
	uint32_t gc_depth)
{
	GCHeapDumpContext *context = g_new0 (GCHeapDumpContext, 1);
	gc_heap_dump_context_init (
		context,
		trace_context,
		gc_reason,
		gc_type,
		gc_count,
		gc_depth);

	return context;
}

static
void
gc_heap_dump_context_free (GCHeapDumpContext *context)
{
	gc_heap_dump_context_fini (context);
	g_free (context);
}

static
GCHeapDumpContext *
gc_heap_dump_context_get (void)
{
	EventPipeMonoThreadData *thread_data = ep_rt_mono_thread_data_get_or_create ();
	return thread_data ? thread_data->gc_heap_dump_context : NULL;
}

static
void
gc_heap_dump_context_set (GCHeapDumpContext *context)
{
	EventPipeMonoThreadData *thread_data = ep_rt_mono_thread_data_get_or_create ();
	if (thread_data) {
		if (thread_data->gc_heap_dump_context)
			gc_heap_dump_context_free (thread_data->gc_heap_dump_context);
		thread_data->gc_heap_dump_context = context;
	}
}

static
void
gc_heap_dump_context_reset (void)
{
	gc_heap_dump_context_set (NULL);
}

static
int32_t
gc_roots_sort_compare_func (
	const void *a,
	const void *b)
{
	EP_ASSERT (a && b);

	GCRootData *root_a = *(GCRootData **)a;
	GCRootData *root_b = *(GCRootData **)b;

	EP_ASSERT (root_a && root_b);
	if (root_a->start == root_b->start)
		return 0;
	return (root_a->start > root_b->start) ? 1 : -1;
}

static
void
gc_heap_dump_context_build_roots (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	ep_rt_spin_lock_requires_lock_not_held (&_gc_lock);

	EP_SPIN_LOCK_ENTER (&_gc_lock, section1)
		if (!context->gc_roots) {
			dn_vector_ptr_custom_alloc_params_t params = { 0, };
			params.capacity = dn_umap_size (&_gc_roots_table);
			context->gc_roots = dn_vector_ptr_custom_alloc (&params);

		} else {
			dn_vector_ptr_clear (context->gc_roots);
		}

		DN_UMAP_FOREACH_BEGIN (const mono_byte *, key, GCRootData *, value, &_gc_roots_table) {
			DN_UNREFERENCED_PARAMETER (key);
			dn_vector_ptr_push_back (context->gc_roots, value);
		} DN_UMAP_FOREACH_END;
	EP_SPIN_LOCK_EXIT (&_gc_lock, section1)

	dn_vector_ptr_sort (context->gc_roots, gc_roots_sort_compare_func);

ep_on_exit:
	ep_rt_spin_lock_requires_lock_not_held (&_gc_lock);
	return;

ep_on_error:
	ep_exit_error_handler ();
}

static
GCRootData *
gc_heap_dump_context_find_root (
	GCHeapDumpContext *context,
	uintptr_t root)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	const uint8_t *base = (const uint8_t *)dn_vector_ptr_data (context->gc_roots);
	const uint8_t *p;
	size_t lim;
	int32_t cmp;
	for (lim = dn_vector_ptr_size (context->gc_roots); lim; lim >>= 1) {
		p = base + ((lim >> 1) * dn_vector_ptr_element_size);

		GCRootData *gc_root_data = *(GCRootData **)p;
		EP_ASSERT (gc_root_data);

		if (gc_root_data->start <= root && gc_root_data->end > root)
			cmp = 0;
		else
			cmp = (root > gc_root_data->start) ? 1 : -1;

		if (!cmp)
			return *(GCRootData **)p;
		else if (cmp > 0) {
			base = p + dn_vector_ptr_element_size;
			lim--;
		}
	}

	return NULL;
}

static
void
gc_heap_dump_context_clear_nodes (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
	gc_heap_dump_context_clear_bulk_data (&context->bulk_nodes);
}

static
void
gc_heap_dump_context_clear_edges (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
	gc_heap_dump_context_clear_bulk_data (&context->bulk_edges);
}

static
void
gc_heap_dump_context_clear_root_edges (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
	gc_heap_dump_context_clear_bulk_data (&context->bulk_root_edges);
}

static
void
gc_heap_dump_context_clear_root_cwt_elem_edges (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
	gc_heap_dump_context_clear_bulk_data (&context->bulk_root_cwt_elem_edges);
}

static
void
gc_heap_dump_context_clear_root_static_vars (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
	gc_heap_dump_context_clear_bulk_data (&context->bulk_root_static_vars);
}

static
void
flush_gc_event_bulk_nodes (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (!context->bulk_nodes.count)
		return;

	FireEtwGCBulkNode (
		context->bulk_nodes.index,
		context->bulk_nodes.count,
		clr_instance_get_id (),
		BULK_NODE_EVENT_TYPE_SIZE,
		context->bulk_nodes.data_start,
		NULL,
		NULL);

	context->bulk_nodes.index++;
	gc_heap_dump_context_clear_nodes (context);
}

static
void
fire_gc_event_bulk_node (
	GCHeapDumpContext *context,
	uintptr_t address,
	uint64_t size,
	uintptr_t type,
	uint64_t edge_count)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	bool update_previous_bulk_node = (size == 0 && context->bulk_nodes.count != 0);

	if (update_previous_bulk_node)
		context->bulk_nodes.count--;

	if (context->bulk_nodes.count == context->bulk_nodes.max_count)
		flush_gc_event_bulk_nodes (context);

	// Mono profiler object reference callback might split bulk node
	// into several calls if it contains more edges than internal buffer size.
	// Mono profiler pass an object size of 0 to identify this case.
	if (update_previous_bulk_node) {
		context->bulk_nodes.data_current -= sizeof (edge_count);
		uint64_t previous_edge_count;
		memcpy (&previous_edge_count, context->bulk_nodes.data_current, sizeof (edge_count));
		edge_count = GUINT64_FROM_LE (previous_edge_count) + edge_count;
		ep_write_buffer_uint64_t (&context->bulk_nodes.data_current, edge_count);
	} else {
		uint64_t type_id = type;
		MonoVTable *vtable = (MonoVTable *)type;
		if (vtable && vtable->klass) {
			MonoType *klass_type = m_class_get_byval_arg (vtable->klass);
			bulk_type_log_type_and_parameters_if_necessary (context->bulk_type_logger, klass_type, TYPE_LOG_BEHAVIOR_IF_FIRST_TIME);
			type_id = get_typeid_for_type (klass_type);
		}

		// NOTE, must match manifest GCBulkNode values type.
		ep_write_buffer_uintptr_t (&context->bulk_nodes.data_current, address);
		ep_write_buffer_uint64_t (&context->bulk_nodes.data_current, size);
		ep_write_buffer_uint64_t (&context->bulk_nodes.data_current, type_id);
		ep_write_buffer_uint64_t (&context->bulk_nodes.data_current, edge_count);
	}

	context->bulk_nodes.count++;
}

static
void
flush_gc_event_bulk_edges (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (!context->bulk_edges.count)
		return;

	FireEtwGCBulkEdge (
		context->bulk_edges.index,
		context->bulk_edges.count,
		clr_instance_get_id (),
		BULK_EDGE_EVENT_TYPE_SIZE,
		context->bulk_edges.data_start,
		NULL,
		NULL);

	context->bulk_edges.index++;
	gc_heap_dump_context_clear_edges (context);
}

static
void
fire_gc_event_bulk_edge (
	GCHeapDumpContext *context,
	uintptr_t address,
	uint32_t ref_field_id)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (context->bulk_edges.count == context->bulk_edges.max_count)
		flush_gc_event_bulk_edges (context);

	// NOTE, must match manifest GCBulkEdge values type.
	ep_write_buffer_uintptr_t (&context->bulk_edges.data_current, address);
	ep_write_buffer_uint32_t (&context->bulk_edges.data_current, ref_field_id);

	context->bulk_edges.count++;
}

static
void
fire_gc_event_object_reference (
	GCHeapDumpContext *context,
	const uint8_t *data,
	uint32_t payload_size)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	uintptr_t object_address;
	uintptr_t object_type;
	uint64_t object_size;
	uint64_t edge_count;

	EP_ASSERT (data + sizeof (object_address) <= data + payload_size);
	memcpy (&object_address, data, sizeof (object_address));
	data += sizeof (object_address);

	EP_ASSERT (data + sizeof (object_size) <= data + payload_size);
	memcpy (&object_size, data, sizeof (object_size));
	data += sizeof (object_size);

	EP_ASSERT (data + sizeof (object_type) <= data + payload_size);
	memcpy (&object_type, data, sizeof (object_type));
	data += sizeof (object_type);

	EP_ASSERT (data + sizeof (edge_count) <= data + payload_size);
	memcpy (&edge_count, data, sizeof (edge_count));
	data += sizeof (edge_count);

	fire_gc_event_bulk_node (
		context,
		object_address,
		object_size,
		object_type,
		edge_count);

	EP_ASSERT (data + (edge_count * sizeof (object_address)) <= data + payload_size);
	for (uint32_t i = 0; i < edge_count; i++) {
		memcpy (&object_address, data, sizeof (object_address));
		data += sizeof (object_address);

		fire_gc_event_bulk_edge (
			context,
			object_address,
			0);
	}
}

static
int
buffer_gc_event_object_reference_callback (
	MonoObject *obj,
	MonoClass *klass,
	uintptr_t size,
	uintptr_t num,
	MonoObject **refs,
	uintptr_t *offsets,
	void *data)
{
	if (!data)
		return 1;

	GCHeapDumpContext *context = (GCHeapDumpContext *)data;

	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	uintptr_t object_address = (uintptr_t)SGEN_POINTER_UNTAG_ALL (obj);
	uintptr_t object_type = (uintptr_t)SGEN_POINTER_UNTAG_ALL (mono_object_get_vtable_internal (obj));
	uint64_t object_size = (uint64_t)size;
	uint64_t edge_count = (uint64_t)num;

	/* account for object alignment */
	object_size += 7;
	object_size &= ~7;

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT_OBJECT_REF;
	gc_event_data.payload_size =
		sizeof (object_address) +
		sizeof (object_size) +
		sizeof (object_type) +
		sizeof (edge_count) +
		GUINT64_TO_UINT32 (edge_count * sizeof (uintptr_t));

	EP_ASSERT (context->buffer);
	EP_ASSERT (context->buffer->context);

	uint8_t *buffer = context->buffer->alloc_func (context->buffer->context, sizeof (gc_event_data) + gc_event_data.payload_size);
	if (buffer) {
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		memcpy (buffer, &object_address, sizeof (object_address));
		buffer += sizeof (object_address);

		memcpy (buffer, &object_size, sizeof (object_size));
		buffer += sizeof (object_size);

		memcpy (buffer, &object_type, sizeof (object_type));
		buffer += sizeof (object_type);

		memcpy (buffer, &edge_count, sizeof (edge_count));
		buffer += sizeof (edge_count);

		for (uint64_t i = 0; i < edge_count; i++) {
			object_address = (uintptr_t)SGEN_POINTER_UNTAG_ALL (refs [i]);
			memcpy (buffer, &object_address, sizeof (object_address));
			buffer += sizeof (object_address);
		}
	}

	return 0;
}

static
void
flush_gc_event_bulk_root_static_vars (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (!context->bulk_root_static_vars.count)
		return;

	FireEtwGCBulkRootStaticVar (
		context->bulk_root_static_vars.count,
		(uint64_t)mono_get_root_domain (),
		clr_instance_get_id (),
		GPTRDIFF_TO_INT (context->bulk_root_static_vars.data_current - context->bulk_root_static_vars.data_start),
		context->bulk_root_static_vars.data_start,
		NULL,
		NULL);

	context->bulk_root_static_vars.index++;
	gc_heap_dump_context_clear_root_static_vars (context);
}

static
void
fire_gc_event_bulk_root_static_var (
	GCHeapDumpContext *context,
	GCRootData *root_data,
	uintptr_t address,
	uintptr_t object)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
	EP_ASSERT (root_data);
	EP_ASSERT (root_data->source == MONO_ROOT_SOURCE_STATIC);

	MonoVTable *vtable = (MonoVTable *)(root_data->key);
	uint32_t static_var_flags = 0;
	uint64_t type_id = (uint64_t)vtable;
	const ep_char8_t *static_var_name = "?";

	if (vtable && vtable->klass) {
		ERROR_DECL (error);
		gpointer iter = NULL;
		MonoClassField *field;
		uint32_t offset = GPTRDIFF_TO_UINT32 (address - root_data->start);
		while ((field = mono_class_get_fields_internal (vtable->klass, &iter))) {
			if (mono_field_get_flags (field) & FIELD_ATTRIBUTE_LITERAL)
				continue;
			if (!(mono_field_get_flags (field) & FIELD_ATTRIBUTE_STATIC))
				continue;
			if (mono_field_is_deleted (field) || m_field_is_from_update (field))
				continue;

			if (mono_field_get_offset (field) == offset) {
				static_var_name = mono_field_get_name (field);
				break;
			}
		}
		if (!is_ok (error))
			mono_error_cleanup (error);
		error_init_reuse (error);
	}

	size_t name_len = static_var_name && static_var_name [0] != '\0'
		? strlen (static_var_name)
		: 0;

	size_t event_size = BULK_ROOT_STATIC_VAR_EVENT_TYPE_SIZE + ((name_len +1) * sizeof (ep_char16_t));

	if (context->bulk_root_static_vars.data_end <=  context->bulk_root_static_vars.data_current + event_size)
		flush_gc_event_bulk_root_static_vars (context);

	if (context->bulk_root_static_vars.data_end <=  context->bulk_root_static_vars.data_current + event_size)
		return;

	if (vtable && vtable->klass) {
		MonoType *klass_type = m_class_get_byval_arg (vtable->klass);
		bulk_type_log_type_and_parameters_if_necessary (context->bulk_type_logger, klass_type, TYPE_LOG_BEHAVIOR_IF_FIRST_TIME);
		type_id = get_typeid_for_type (klass_type);
	}

	// NOTE, needs to match manifest GCBulkRootStaticVar values type.
	ep_write_buffer_uint64_t (&context->bulk_root_static_vars.data_current, (uint64_t)address);
	ep_write_buffer_uint64_t (&context->bulk_root_static_vars.data_current, (uint64_t)object);
	ep_write_buffer_uint64_t (&context->bulk_root_static_vars.data_current, type_id);
	ep_write_buffer_uint32_t (&context->bulk_root_static_vars.data_current, static_var_flags);
	write_buffer_string_utf8_to_utf16_t (&context->bulk_root_static_vars.data_current, static_var_name, GSIZE_TO_UINT32 (name_len));

	context->bulk_root_static_vars.count++;
}

static
void
flush_gc_event_bulk_root_edges (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (!context->bulk_root_edges.count)
		return;

	FireEtwGCBulkRootEdge (
		context->bulk_root_edges.index,
		context->bulk_root_edges.count,
		clr_instance_get_id (),
		BULK_ROOT_EDGE_EVENT_TYPE_SIZE,
		context->bulk_root_edges.data_start,
		NULL,
		NULL);

	context->bulk_root_edges.index++;
	gc_heap_dump_context_clear_root_edges (context);
}

static
void
flush_gc_event_bulk_root_cwt_elem_edges (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (!context->bulk_root_cwt_elem_edges.count)
		return;

	FireEtwGCBulkRootConditionalWeakTableElementEdge (
		context->bulk_root_cwt_elem_edges.index,
		context->bulk_root_cwt_elem_edges.count,
		clr_instance_get_id (),
		BULK_ROOT_CWT_ELEM_EDGE_EVENT_TYPE_SIZE,
		context->bulk_root_cwt_elem_edges.data_start,
		NULL,
		NULL);

	context->bulk_root_cwt_elem_edges.index++;
	gc_heap_dump_context_clear_root_cwt_elem_edges (context);
}

static
void
fire_gc_event_bulk_root_cwt_elem_edge (
	GCHeapDumpContext *context,
	uintptr_t address,
	uintptr_t key,
	uintptr_t value)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (context->bulk_root_cwt_elem_edges.count == context->bulk_root_cwt_elem_edges.max_count)
		flush_gc_event_bulk_root_cwt_elem_edges (context);

	// NOTE, must match manifest GCBulkRootConditionalWeakTableElementEdge values type.
	ep_write_buffer_uintptr_t (&context->bulk_root_cwt_elem_edges.data_current, key);
	ep_write_buffer_uintptr_t (&context->bulk_root_cwt_elem_edges.data_current, value);
	ep_write_buffer_uintptr_t (&context->bulk_root_cwt_elem_edges.data_current, address);

	context->bulk_root_cwt_elem_edges.count++;
}

static
void
fire_gc_event_bulk_root_edge (
	GCHeapDumpContext *context,
	uintptr_t address,
	uintptr_t object)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (context->bulk_root_edges.count == context->bulk_root_edges.max_count)
		flush_gc_event_bulk_root_edges (context);

	uint8_t root_kind = GC_ROOT_KIND_OTHER;
	uint32_t root_flags = GC_ROOT_FLAGS_NONE;
	uintptr_t root_id = 0;

	GCRootData *gc_root = gc_heap_dump_context_find_root (context, address);
	if (gc_root) {
		if (gc_root->source == MONO_ROOT_SOURCE_STATIC) {
			fire_gc_event_bulk_root_static_var (
				context,
				gc_root,
				address,
				object);
			return;
		}

		if (gc_root->source == MONO_ROOT_SOURCE_EPHEMERON) {
			// Should be ephemeron key, but current profiler
			// API won't report it and key is only validated
			// to be none 0 and a aligned pointer by gcdump.
			// Use object as key until we get key reported
			// in gc_roots profiler callback.
			uintptr_t key = object;
			uintptr_t value = object;
			fire_gc_event_bulk_root_cwt_elem_edge (
				context,
				root_id,
				key,
				value);
			return;
		}

		switch (gc_root->source) {
		case MONO_ROOT_SOURCE_STACK :
			root_kind = GC_ROOT_KIND_STACK;
			root_id = address;
			break;
		case MONO_ROOT_SOURCE_FINALIZER_QUEUE :
			root_kind = GC_ROOT_KIND_FINALIZER;
			break;
		case MONO_ROOT_SOURCE_THREAD_STATIC :
			root_kind = GC_ROOT_KIND_HANDLE;
			root_id = address;
			break;
		case MONO_ROOT_SOURCE_GC_HANDLE :
			root_kind = GC_ROOT_KIND_HANDLE;
			root_flags = GCONSTPOINTER_TO_INT (gc_root->key) != 0 ? GC_ROOT_FLAGS_PINNING : GC_ROOT_FLAGS_NONE;
			root_id = address;
			break;
		case MONO_ROOT_SOURCE_HANDLE :
			root_kind = GC_ROOT_KIND_HANDLE;
			root_id = address;
			break;
		case MONO_ROOT_SOURCE_TOGGLEREF :
			root_kind = GC_ROOT_KIND_HANDLE;
			root_flags = GC_ROOT_FLAGS_REFCOUNTED;
			root_id = address;
			break;
		default :
			break;
		}
	}

	// NOTE, needs to match manifest GCBulkRootEdge values type.
	ep_write_buffer_uintptr_t (&context->bulk_root_edges.data_current, object);
	ep_write_buffer_uint8_t (&context->bulk_root_edges.data_current, root_kind);
	ep_write_buffer_uint32_t (&context->bulk_root_edges.data_current, root_flags);
	ep_write_buffer_uintptr_t (&context->bulk_root_edges.data_current, root_id);

	context->bulk_root_edges.count++;
}

static
void
fire_gc_event_roots (
	GCHeapDumpContext *context,
	const uint8_t *data,
	uint32_t payload_size)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	uint64_t count;
	uintptr_t address;
	uintptr_t object;

	EP_ASSERT (data + sizeof (count) <= data + payload_size);
	memcpy (&count, data, sizeof (count));
	data += sizeof (count);

	EP_ASSERT (data + (count * (sizeof (address) + sizeof (object))) <= data + payload_size);
	for (uint32_t i = 0; i < count; i++) {
		memcpy (&address, data, sizeof (address));
		data += sizeof (address);

		memcpy (&object, data, sizeof (object));
		data += sizeof (object);

		fire_gc_event_bulk_root_edge (
			context,
			address,
			object);
	}
}

static
void
buffer_gc_event_roots_callback (
	MonoProfiler *prof,
	uint64_t count,
	const mono_byte *const * addresses,
	MonoObject *const * objects)
{
	GCHeapDumpContext *context = gc_heap_dump_context_get ();
	if (!context)
		return;

	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT_ROOTS;
	gc_event_data.payload_size =
		(uint32_t)(sizeof (count) +
		(count * (sizeof (uintptr_t) + sizeof (uintptr_t) + sizeof (uintptr_t))));

	EP_ASSERT (context->buffer);
	EP_ASSERT (context->buffer->context);

	uint8_t *buffer = context->buffer->alloc_func (context->buffer->context, sizeof (gc_event_data) + gc_event_data.payload_size);
	if (buffer) {
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		memcpy (buffer, &count, sizeof (count));
		buffer += sizeof (count);

		for (uint64_t i = 0; i < count; i++) {
			uintptr_t address = (uintptr_t)addresses [i];
			uintptr_t object = (uintptr_t)SGEN_POINTER_UNTAG_ALL (objects [i]);

			memcpy (buffer, &address, sizeof (address));
			buffer += sizeof (address);

			memcpy (buffer, &object, sizeof (object));
			buffer += sizeof (object);
		}
	}
}

static
void
flush_gc_events (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	flush_gc_event_bulk_nodes (context);
	flush_gc_event_bulk_edges (context);
	flush_gc_event_bulk_root_edges (context);
	flush_gc_event_bulk_root_cwt_elem_edges (context);
	flush_gc_event_bulk_root_static_vars (context);

	if (context->bulk_type_logger && is_keyword_and_level_enabled (&context->trace_context, EP_EVENT_LEVEL_INFORMATIONAL, TYPE_KEYWORD))
		bulk_type_fire_bulk_type_event (context->bulk_type_logger);
}

static
void
fire_buffered_gc_events (GCHeapDumpContext *context)
{
	EP_ASSERT (is_gc_heap_dump_enabled (context));
	EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);

	if (context->buffer) {
		const uint8_t *buffer = NULL;
		const uint8_t *buffer_end = NULL;
		size_t size = 0;

		context->buffer->reset_func (context->buffer->context);

		while ((buffer = context->buffer->get_next_buffer_func (context->buffer->context, &size))) {
			buffer_end = buffer + size;
			while (buffer < buffer_end) {
				if (buffer + sizeof (BufferedGCEvent) > buffer_end)
					break;

				BufferedGCEvent *gc_event = (BufferedGCEvent *)buffer;
				buffer += sizeof (BufferedGCEvent);

				if (buffer + gc_event->payload_size > buffer_end)
					break;

				switch (gc_event->type) {
				case BUFFERED_GC_EVENT_OBJECT_REF :
					fire_gc_event_object_reference (context, buffer, gc_event->payload_size);
					break;
				case BUFFERED_GC_EVENT_ROOTS :
					fire_gc_event_roots (context, buffer, gc_event->payload_size);
					break;
				default:
					EP_ASSERT (!"Unknown buffered GC event type.");
				}

				buffer += gc_event->payload_size;
			}
		}

		flush_gc_events (context);
	}
}

static
void
calculate_live_keywords (
	uint64_t *live_keywords,
	bool *trigger_heap_dump)
{
	uint64_t keywords[] = { GC_HEAP_COLLECT_KEYWORD };
	uint64_t count[] = { 0 };

	ep_requires_lock_held ();

	EP_ASSERT (G_N_ELEMENTS (keywords) == G_N_ELEMENTS (count));
	*live_keywords = ep_rt_mono_session_calculate_and_count_all_keywords (
		ep_config_get_public_provider_name_utf8 (),
		keywords,
		count,
		G_N_ELEMENTS (count));

	*trigger_heap_dump = ep_rt_mono_is_runtime_initialized ();
	*trigger_heap_dump &= is_keword_enabled (*live_keywords, GC_KEYWORD);
	*trigger_heap_dump &= is_keword_enabled (*live_keywords, GC_HEAP_COLLECT_KEYWORD);
	*trigger_heap_dump &= count [0] > _gc_heap_dump_trigger_count;

	_gc_heap_dump_trigger_count = count [0];

	ep_requires_lock_held ();
}

// TODO: If/when we can unload vtables, we would need to temporary
// root the vtable pointers currently stored in buffered gc events.
// Once all events are fired, we can remove root from GC.
static
void
gc_event_callback (
	MonoProfiler *prof,
	MonoProfilerGCEvent gc_event,
	uint32_t generation,
	mono_bool serial)
{
	switch (gc_event) {
	case MONO_GC_EVENT_POST_STOP_WORLD:
	{
		GCHeapDumpContext *context = gc_heap_dump_context_get ();
		if (is_gc_heap_dump_enabled (context)) {
			EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
			mono_profiler_set_gc_roots_callback (_ep_rt_mono_default_profiler_provider, buffer_gc_event_roots_callback);
		}
		break;
	}
	case MONO_GC_EVENT_PRE_START_WORLD:
	{
		GCHeapDumpContext *context = gc_heap_dump_context_get ();
		if (is_gc_heap_dump_enabled (context)) {
			EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
			mono_gc_walk_heap (0, buffer_gc_event_object_reference_callback, context);
			mono_profiler_set_gc_roots_callback (_ep_rt_mono_default_profiler_provider, NULL);
		}
		break;
	}
	case MONO_GC_EVENT_POST_START_WORLD:
	{
		GCHeapDumpContext *context = gc_heap_dump_context_get ();
		if (is_gc_heap_dump_enabled (context)) {
			EP_ASSERT (context->state == GC_HEAP_DUMP_CONTEXT_STATE_DUMP);
			gc_heap_dump_context_build_roots (context);
		}
		break;
	}
	default:
		break;
	}
}

static
void
gc_heap_dump_trigger_callback (MonoProfiler *prof)
{
	bool notify_finalizer = false;
	GCHeapDumpContext *heap_dump_context = gc_heap_dump_context_get ();

	if (!heap_dump_context && gc_heap_dump_requested ()) {
		EP_LOCK_ENTER (section1)
			if (gc_heap_dump_requested ()) {
				EVENTPIPE_TRACE_CONTEXT context = RUNTIME_PROVIDER_CONTEXT;
				if (!dn_vector_empty (&_gc_heap_dump_requests_data)) {
					context = *dn_vector_back_t (&_gc_heap_dump_requests_data, EVENTPIPE_TRACE_CONTEXT);
					dn_vector_pop_back (&_gc_heap_dump_requests_data);
				}
				gc_heap_dump_requests_dec ();

				uint32_t gc_count = gc_heap_dump_count_inc ();
				uint32_t gc_depth = mono_gc_max_generation () + 1;
				uint32_t gc_reason = GC_REASON_INDUCED;
				uint32_t gc_type = GC_TYPE_NGC;

				heap_dump_context =
					gc_heap_dump_context_alloc (
						context,
						gc_reason,
						gc_type,
						gc_count,
						gc_depth);

				gc_heap_dump_context_set (heap_dump_context);
			}
		EP_LOCK_EXIT (section1)
	}

	if (heap_dump_context) {
		switch (heap_dump_context->state) {
		case GC_HEAP_DUMP_CONTEXT_STATE_INIT :
		{
			bool all_started = false;
			EP_LOCK_ENTER (section2)
				all_started = ep_rt_mono_sesion_has_all_started ();
			EP_LOCK_EXIT (section2)

			if (!all_started && heap_dump_context->retry_count < 5) {
				heap_dump_context->retry_count++;
				notify_finalizer = true;
				break;
			}

			heap_dump_context->state = GC_HEAP_DUMP_CONTEXT_STATE_START;
			// Fallthrough
		}
		case GC_HEAP_DUMP_CONTEXT_STATE_START :
		{
			FireEtwGCStart_V2 (
				heap_dump_context->gc_count,
				heap_dump_context->gc_depth,
				heap_dump_context->gc_reason,
				heap_dump_context->gc_type,
				clr_instance_get_id (),
				0,
				NULL,
				NULL);

			heap_dump_context->state = GC_HEAP_DUMP_CONTEXT_STATE_DUMP;
			notify_finalizer = true;
			break;
		}
		case GC_HEAP_DUMP_CONTEXT_STATE_DUMP :
		{
			bool gc_dump = true;
			gc_dump &= EventPipeEventEnabledGCBulkNode ();
			gc_dump &= EventPipeEventEnabledGCBulkEdge ();
			gc_dump &= EventPipeEventEnabledGCBulkRootEdge ();
			gc_dump &= EventPipeEventEnabledGCBulkRootStaticVar ();

			if (gc_dump) {
				mono_profiler_set_gc_event_callback (_ep_rt_mono_default_profiler_provider, gc_event_callback);
				mono_gc_collect (mono_gc_max_generation ());
				mono_profiler_set_gc_event_callback (_ep_rt_mono_default_profiler_provider, NULL);
				fire_buffered_gc_events (heap_dump_context);
			}

			heap_dump_context->state = GC_HEAP_DUMP_CONTEXT_STATE_END;
			notify_finalizer = true;
			break;
		}
		case GC_HEAP_DUMP_CONTEXT_STATE_END :
		{
			FireEtwGCEnd_V1 (
				heap_dump_context->gc_count,
				heap_dump_context->gc_depth,
				clr_instance_get_id (),
				NULL,
				NULL);

			gc_heap_dump_context_reset ();
			heap_dump_context = NULL;
			break;
		}
		default :
			g_assert_not_reached ();
		}
	}

	if (!heap_dump_context) {
		EP_LOCK_ENTER (section3)
			bool gc_enabled = is_keword_enabled(RUNTIME_PROVIDER_CONTEXT.EnabledKeywordsBitmask, GC_KEYWORD);
			bool gc_dump_enabled = is_keword_enabled(RUNTIME_PROVIDER_CONTEXT.EnabledKeywordsBitmask, GC_HEAP_COLLECT_KEYWORD);
			if (!(gc_enabled && gc_dump_enabled))
				mono_profiler_set_gc_finalized_callback (_ep_rt_mono_default_profiler_provider, NULL);
		EP_LOCK_EXIT (section3)
	}

	if (notify_finalizer) {
		ep_rt_thread_sleep (200 * NUM_NANOSECONDS_IN_1_MS);
		mono_gc_finalize_notify ();
	}

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
EP_CALLBACK_CALLTYPE
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

	EP_ASSERT (_ep_rt_mono_default_profiler_provider != NULL);

	EP_LOCK_ENTER (section1)
		uint64_t live_keywords = 0;
		bool trigger_heap_dump = false;
		calculate_live_keywords (&live_keywords, &trigger_heap_dump);

		if (is_keword_enabled(live_keywords, JIT_KEYWORD)) {
			mono_profiler_set_jit_begin_callback (_ep_rt_mono_default_profiler_provider, jit_begin_callback);
			mono_profiler_set_jit_failed_callback (_ep_rt_mono_default_profiler_provider, jit_failed_callback);
			mono_profiler_set_jit_done_callback (_ep_rt_mono_default_profiler_provider, jit_done_callback);
		} else {
			mono_profiler_set_jit_begin_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_jit_failed_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_jit_done_callback (_ep_rt_mono_default_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, LOADER_KEYWORD)) {
			mono_profiler_set_image_loaded_callback (_ep_rt_mono_default_profiler_provider, image_loaded_callback);
			mono_profiler_set_image_unloaded_callback (_ep_rt_mono_default_profiler_provider, image_unloaded_callback);
			mono_profiler_set_assembly_loaded_callback (_ep_rt_mono_default_profiler_provider, assembly_loaded_callback);
			mono_profiler_set_assembly_unloaded_callback (_ep_rt_mono_default_profiler_provider, assembly_unloaded_callback);
		} else {
			mono_profiler_set_image_loaded_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_image_unloaded_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_assembly_loaded_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_assembly_unloaded_callback (_ep_rt_mono_default_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, TYPE_DIAGNOSTIC_KEYWORD)) {
			mono_profiler_set_class_loading_callback (_ep_rt_mono_default_profiler_provider, class_loading_callback);
			mono_profiler_set_class_failed_callback (_ep_rt_mono_default_profiler_provider, class_failed_callback);
			mono_profiler_set_class_loaded_callback (_ep_rt_mono_default_profiler_provider, class_loaded_callback);
		} else {
			mono_profiler_set_class_loading_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_class_failed_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_class_loaded_callback (_ep_rt_mono_default_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, EXCEPTION_KEYWORD)) {
			mono_profiler_set_exception_throw_callback (_ep_rt_mono_default_profiler_provider, exception_throw_callback);
			mono_profiler_set_exception_clause_callback (_ep_rt_mono_default_profiler_provider, exception_clause_callback);
		} else {
			mono_profiler_set_exception_throw_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_exception_clause_callback (_ep_rt_mono_default_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, CONTENTION_KEYWORD)) {
			mono_profiler_set_monitor_contention_callback (_ep_rt_mono_default_profiler_provider, monitor_contention_callback);
			mono_profiler_set_monitor_acquired_callback (_ep_rt_mono_default_profiler_provider, monitor_acquired_callback);
			mono_profiler_set_monitor_failed_callback (_ep_rt_mono_default_profiler_provider, monitor_failed_callback);
		} else {
			mono_profiler_set_monitor_contention_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_monitor_acquired_callback (_ep_rt_mono_default_profiler_provider, NULL);
			mono_profiler_set_monitor_failed_callback (_ep_rt_mono_default_profiler_provider, NULL);
		}

		// Disabled in gc_heap_dump_trigger_callback when no longer needed.
		if (is_keword_enabled(live_keywords, GC_KEYWORD) && is_keword_enabled(live_keywords, GC_HEAP_COLLECT_KEYWORD))
			mono_profiler_set_gc_finalized_callback (_ep_rt_mono_default_profiler_provider, gc_heap_dump_trigger_callback);

		RUNTIME_PROVIDER_CONTEXT.Level = level;
		RUNTIME_PROVIDER_CONTEXT.EnabledKeywordsBitmask = live_keywords;
		RUNTIME_PROVIDER_CONTEXT.IsEnabled = (live_keywords != 0 ? true : false);

		if (trigger_heap_dump) {
			EP_ASSERT (ep_rt_mono_is_runtime_initialized ());
			dn_vector_push_back (&_gc_heap_dump_requests_data, RUNTIME_PROVIDER_CONTEXT);
			gc_heap_dump_requests_inc ();
			mono_gc_finalize_notify ();
		}
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

static
GCRootData *
gc_root_data_alloc (
	uintptr_t start,
	uintptr_t size,
	MonoGCRootSource source,
	const void *key,
	const char *name)
{
	GCRootData *root_data = g_new0 (GCRootData, 1);
	root_data->start = start;
	root_data->end = start + size;
	root_data->key = key;
	//root_data->name = g_strdup (name);
	root_data->source = source;
	return root_data;
}

static
void
gc_root_data_free (void *data)
{
	GCRootData *root_data = (GCRootData *)data;
	if (root_data) {
		g_free (root_data->name);
		g_free (root_data);
	}
}

static
void
gc_root_register_callback (
	MonoProfiler *prof,
	const mono_byte *start,
	uintptr_t size,
	MonoGCRootSource source,
	const void *key,
	const char *name)
{
	ep_rt_spin_lock_requires_lock_not_held (&_gc_lock);

	GCRootData *root_data = gc_root_data_alloc ((uintptr_t)start, size, source, key, name);
	EP_SPIN_LOCK_ENTER (&_gc_lock, section1)
		dn_umap_insert_or_assign (&_gc_roots_table, (void *)start, root_data);
	EP_SPIN_LOCK_EXIT (&_gc_lock, section1)

ep_on_exit:
	ep_rt_spin_lock_requires_lock_not_held (&_gc_lock);
	return;

ep_on_error:
	ep_exit_error_handler ();
}

static
void
gc_root_unregister_callback (
	MonoProfiler *prof,
	const mono_byte *start)
{
	ep_rt_spin_lock_requires_lock_not_held (&_gc_lock);

	GCRootData *root_data = NULL;
	EP_SPIN_LOCK_ENTER (&_gc_lock, section1)
		dn_umap_extract_key (&_gc_roots_table, start, NULL, (void **)&root_data);
	EP_SPIN_LOCK_EXIT (&_gc_lock, section1)

	g_free (root_data);

ep_on_exit:
	ep_rt_spin_lock_requires_lock_not_held (&_gc_lock);
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_rt_mono_runtime_provider_component_init (void)
{
	ep_rt_spin_lock_alloc (&_gc_lock);

	dn_umap_custom_init_params_t params = {0,};
	params.value_dispose_func = gc_root_data_free;

	dn_umap_custom_init (&_gc_roots_table, &params);

	dn_vector_init_t (&_gc_heap_dump_requests_data, EVENTPIPE_TRACE_CONTEXT);

	EP_ASSERT (_ep_rt_mono_default_profiler_provider != NULL);
	mono_profiler_set_gc_root_register_callback (_ep_rt_mono_default_profiler_provider, gc_root_register_callback);
	mono_profiler_set_gc_root_unregister_callback (_ep_rt_mono_default_profiler_provider, gc_root_unregister_callback);
}

void
ep_rt_mono_runtime_provider_init (void)
{
	MonoMethodSignature *method_signature = mono_metadata_signature_alloc (mono_get_corlib (), 1);
	if (method_signature) {
		method_signature->params[0] = m_class_get_byval_arg (mono_get_object_class());
		method_signature->ret = m_class_get_byval_arg (mono_get_void_class());

		ERROR_DECL (error);
		MonoClass *runtime_helpers = mono_class_from_name_checked (mono_get_corlib (), "System.Runtime.CompilerServices", "RuntimeHelpers", error);
		if (is_ok (error) && runtime_helpers) {
			MonoMethodBuilder *method_builder = mono_mb_new (runtime_helpers, "CompileMethod", MONO_WRAPPER_RUNTIME_INVOKE);
			if (method_builder) {
				_runtime_helper_compile_method = mono_mb_create_method (method_builder, method_signature, 1);
				mono_mb_free (method_builder);
			}
		}
		mono_error_cleanup (error);
		mono_metadata_free_method_signature (method_signature);

		if (_runtime_helper_compile_method) {
			_runtime_helper_compile_method_jitinfo = (MonoJitInfo *)g_new0 (MonoJitInfo, 1);
			if (_runtime_helper_compile_method) {
				_runtime_helper_compile_method_jitinfo->code_start = MINI_FTNPTR_TO_ADDR (_runtime_helper_compile_method);
				_runtime_helper_compile_method_jitinfo->code_size = 20;
				_runtime_helper_compile_method_jitinfo->d.method = _runtime_helper_compile_method;
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
				_monitor_enter_v4_method = mono_method_desc_search_in_class (desc, monitor);
				mono_method_desc_free (desc);

				if (_monitor_enter_v4_method) {
					_monitor_enter_v4_method_jitinfo = (MonoJitInfo *)g_new0 (MonoJitInfo, 1);
					if (_monitor_enter_v4_method_jitinfo) {
						_monitor_enter_v4_method_jitinfo->code_start = MINI_FTNPTR_TO_ADDR (_monitor_enter_v4_method);
						_monitor_enter_v4_method_jitinfo->code_size = 20;
						_monitor_enter_v4_method_jitinfo->d.method = _monitor_enter_v4_method;
					}
				}
			}

			desc = mono_method_desc_new ("Monitor:Enter(object)", FALSE);
			if (desc) {
				_monitor_enter_method = mono_method_desc_search_in_class (desc, monitor);
				mono_method_desc_free (desc);

				if (_monitor_enter_method ) {
					_monitor_enter_method_jitinfo = (MonoJitInfo *)g_new0 (MonoJitInfo, 1);
					if (_monitor_enter_method_jitinfo) {
						_monitor_enter_method_jitinfo->code_start = MINI_FTNPTR_TO_ADDR (_monitor_enter_method);
						_monitor_enter_method_jitinfo->code_size = 20;
						_monitor_enter_method_jitinfo->d.method = _monitor_enter_method;
					}
				}
			}
		}
		mono_error_cleanup (error);
	}
}

void
ep_rt_mono_runtime_provider_fini (void)
{
	if (_sampled_thread_callstacks)
		g_array_free (_sampled_thread_callstacks, TRUE);
	_sampled_thread_callstacks = NULL;

	_max_sampled_thread_count = 32;

	g_free (_runtime_helper_compile_method_jitinfo);
	_runtime_helper_compile_method_jitinfo = NULL;

	if (_runtime_helper_compile_method)
		mono_free_method (_runtime_helper_compile_method);
	_runtime_helper_compile_method = NULL;

	g_free (_monitor_enter_method_jitinfo);
	_monitor_enter_method_jitinfo = NULL;
	_monitor_enter_method = NULL;

	g_free (_monitor_enter_v4_method_jitinfo);
	_monitor_enter_v4_method_jitinfo = NULL;
	_monitor_enter_v4_method = NULL;

	if (_ep_rt_mono_default_profiler_provider) {
		mono_profiler_set_jit_begin_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_jit_failed_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_jit_done_callback (_ep_rt_mono_default_profiler_provider, NULL);

		mono_profiler_set_image_loaded_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_image_unloaded_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_assembly_loaded_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_assembly_unloaded_callback (_ep_rt_mono_default_profiler_provider, NULL);

		mono_profiler_set_class_loading_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_class_failed_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_class_loaded_callback (_ep_rt_mono_default_profiler_provider, NULL);

		mono_profiler_set_exception_throw_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_exception_clause_callback (_ep_rt_mono_default_profiler_provider, NULL);

		mono_profiler_set_monitor_contention_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_monitor_acquired_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_monitor_failed_callback (_ep_rt_mono_default_profiler_provider, NULL);

		mono_profiler_set_gc_root_register_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_gc_root_unregister_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_gc_finalized_callback (_ep_rt_mono_default_profiler_provider, NULL);
	}

	_gc_heap_dump_requests = 0;
	_gc_heap_dump_count = 0;
	_gc_heap_dump_trigger_count = 0;

	dn_vector_dispose (&_gc_heap_dump_requests_data);
	memset (&_gc_heap_dump_requests_data, 0, sizeof (_gc_heap_dump_requests_data));

	dn_umap_dispose (&_gc_roots_table);
	memset (&_gc_roots_table, 0, sizeof (_gc_roots_table));

	ep_rt_spin_lock_free (&_gc_lock);
}

void
ep_rt_mono_runtime_provider_thread_started_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	thread_started_callback (prof, tid);
}

void
ep_rt_mono_runtime_provider_thread_stopped_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	thread_stopped_callback (prof, tid);
}

#endif /* ENABLE_PERFTRACING */

MONO_EMPTY_SOURCE_FILE(eventpipe_rt_mono_runtime_provider);
