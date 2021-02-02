#include <config.h>
#include <glib.h>
#include <mono/utils/mono-compiler.h>

#ifdef ENABLE_NETCORE
#include <mono/metadata/icall-decl.h>

#if defined(ENABLE_PERFTRACING) && !defined(DISABLE_EVENTPIPE)
#include <eventpipe/ep-rt-config.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-event.h>
#include <eventpipe/ep-event-instance.h>
#include <eventpipe/ep-session.h>

#include <mono/utils/checked-build.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-rand.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-internals.h>
#include <mono/mini/mini-runtime.h>

// Rundown flags.
#define METHOD_FLAGS_DYNAMIC_METHOD 0x1
#define METHOD_FLAGS_GENERIC_METHOD 0x2
#define METHOD_FLAGS_SHARED_GENERIC_METHOD 0x4
#define METHOD_FLAGS_JITTED_METHOD 0x8
#define METHOD_FLAGS_JITTED_HELPER_METHOD 0x10

#define MODULE_FLAGS_NATIVE_MODULE 0x2
#define MODULE_FLAGS_DYNAMIC_MODULE 0x4
#define MODULE_FLAGS_MANIFEST_MODULE 0x8

#define ASSEMBLY_FLAGS_DYNAMIC_ASSEMBLY 0x2
#define ASSEMBLY_FLAGS_NATIVE_ASSEMBLY 0x4
#define ASSEMBLY_FLAGS_COLLECTIBLE_ASSEMBLY 0x8

#define DOMAIN_FLAGS_DEFAULT_DOMAIN 0x1
#define DOMAIN_FLAGS_EXECUTABLE_DOMAIN 0x2

typedef enum _EventPipeActivityControlCode {
	EP_ACTIVITY_CONTROL_GET_ID = 1,
	EP_ACTIVITY_CONTROL_SET_ID = 2,
	EP_ACTIVITY_CONTROL_CREATE_ID = 3,
	EP_ACTIVITY_CONTROL_GET_SET_ID = 4,
	EP_ACTIVITY_CONTROL_CREATE_SET_ID = 5
} EventPipeActivityControlCode;

typedef struct _EventPipeProviderConfigurationNative {
	gunichar2 *provider_name;
	uint64_t keywords;
	uint32_t logging_level;
	gunichar2 *filter_data;
} EventPipeProviderConfigurationNative;

typedef struct _EventPipeSessionInfo {
	int64_t starttime_as_utc_filetime;
	int64_t start_timestamp;
	int64_t timestamp_frequency;
} EventPipeSessionInfo;

typedef struct _EventPipeEventInstanceData {
	intptr_t provider_id;
	uint32_t event_id;
	uint32_t thread_id;
	int64_t timestamp;
	uint8_t activity_id [EP_ACTIVITY_ID_SIZE];
	uint8_t related_activity_id [EP_ACTIVITY_ID_SIZE];
	const uint8_t *payload;
	uint32_t payload_len;
} EventPipeEventInstanceData;

typedef struct _EventPipeFireMethodEventsData{
	MonoDomain *domain;
	uint8_t *buffer;
	size_t buffer_size;
	ep_rt_mono_fire_method_rundown_events_func method_events_func;
} EventPipeFireMethodEventsData;

gboolean ep_rt_mono_initialized;
MonoNativeTlsKey ep_rt_mono_thread_holder_tls_id;
gpointer ep_rt_mono_rand_provider;

static ep_rt_thread_holder_alloc_func thread_holder_alloc_callback_func;
static ep_rt_thread_holder_free_func thread_holder_free_callback_func;

/*
 * Forward declares of all static functions.
 */

static
gboolean
rand_try_get_bytes_func (
	guchar *buffer,
	gssize buffer_size,
	MonoError *error);

static
EventPipeThread *
eventpipe_thread_get (void);

static
EventPipeThread *
eventpipe_thread_get_or_create (void);

static
void
eventpipe_thread_exited (void);

static
void
profiler_eventpipe_thread_exited (
	MonoProfiler *prof,
	uintptr_t tid);

static
gpointer
eventpipe_thread_attach (gboolean background_thread);

static
void
eventpipe_thread_detach (void);

static
void
eventpipe_fire_method_events (
	MonoJitInfo *ji,
	EventPipeFireMethodEventsData *events_data);

static
void
eventpipe_fire_method_events_func (
	MonoJitInfo *ji,
	gpointer user_data);

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
eventpipe_walk_managed_stack_for_thread_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	gpointer data);

static
gboolean
eventpipe_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents);

static
gboolean
eventpipe_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len);

static
gboolean
evetpipe_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len);

static
void
delegate_callback_data_free_func (
	EventPipeCallback callback_func,
	void *callback_data);

static
void
delegate_callback_func (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_context);

static
gboolean
rand_try_get_bytes_func (
	guchar *buffer,
	gssize buffer_size,
	MonoError *error)
{
	g_assert (ep_rt_mono_rand_provider != NULL);
	return mono_rand_try_get_bytes (&ep_rt_mono_rand_provider, buffer, buffer_size, error);
}

static
EventPipeThread *
eventpipe_thread_get (void)
{
	EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (ep_rt_mono_thread_holder_tls_id);
	return thread_holder ? ep_thread_holder_get_thread (thread_holder) : NULL;
}

static
EventPipeThread *
eventpipe_thread_get_or_create (void)
{
	EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (ep_rt_mono_thread_holder_tls_id);
	if (!thread_holder && thread_holder_alloc_callback_func) {
		thread_holder = thread_holder_alloc_callback_func ();
		mono_native_tls_set_value (ep_rt_mono_thread_holder_tls_id, thread_holder);
	}
	return ep_thread_holder_get_thread (thread_holder);
}

static
void
eventpipe_thread_exited (void)
{
	if (ep_rt_mono_initialized) {
		EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (ep_rt_mono_thread_holder_tls_id);
		if (thread_holder && thread_holder_free_callback_func)
			thread_holder_free_callback_func (thread_holder);
		mono_native_tls_set_value (ep_rt_mono_thread_holder_tls_id, NULL);
	}
}

static
void
profiler_eventpipe_thread_exited (
	MonoProfiler *prof,
	uintptr_t tid)
{
	eventpipe_thread_exited ();
}

static
gpointer
eventpipe_thread_attach (gboolean background_thread)
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

static
void
eventpipe_thread_detach (void)
{
	MonoThread *current_thread = mono_thread_current ();
	if (current_thread)
		mono_thread_internal_detach (current_thread);
}

static
void
eventpipe_fire_method_events (
	MonoJitInfo *ji,
	EventPipeFireMethodEventsData *events_data)
{
	g_assert_checked (ji != NULL);
	g_assert_checked (events_data->domain != NULL);
	g_assert_checked (events_data->method_events_func != NULL);

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

	//TODO: Optimize string formatting into functions accepting GString to reduce heap alloc.

	MonoMethod *method = jinfo_get_method (ji);
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

		method_name = method->name;
		method_signature = mono_signature_full_name (method->signature);

		if (method->klass) {
			module_id = (uint64_t)m_class_get_image (method->klass);
			kind = m_class_get_class_kind (method->klass);
			if (kind == MONO_CLASS_GTD || kind == MONO_CLASS_GINST)
				method_flags |= METHOD_FLAGS_GENERIC_METHOD;
			method_namespace = mono_type_get_name_full (m_class_get_byval_arg (method->klass), MONO_TYPE_NAME_FORMAT_IL);
		}
	}

	uint16_t offset_entries = 0;
	uint32_t *il_offsets = NULL;
	uint32_t *native_offsets = NULL;

	MonoDebugMethodJitInfo *debug_info = method ? mono_debug_find_method (method, events_data->domain) : NULL;
	if (debug_info) {
		offset_entries = debug_info->num_line_numbers;
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

		mono_debug_free_method_jit_info (debug_info);
	}

	if (events_data->buffer && !il_offsets && !native_offsets) {
		// No IL offset -> Native offset mapping available. Put all code on IL offset 0.
		g_assert_checked (events_data->buffer_size >= sizeof (uint32_t) * 2);
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
		NULL);

	g_free (method_namespace);
	g_free (method_signature);
}

static
void
eventpipe_fire_method_events_func (
	MonoJitInfo *ji,
	gpointer user_data)
{
	EventPipeFireMethodEventsData *events_data = (EventPipeFireMethodEventsData *)user_data;
	g_assert_checked (events_data != NULL);

	if (ji && !ji->is_trampoline && !ji->async)
		eventpipe_fire_method_events (ji, events_data);
}

static
void
eventpipe_fire_assembly_events (
	MonoDomain *domain,
	MonoAssembly *assembly,
	ep_rt_mono_fire_assembly_rundown_events_func assembly_events_func)
{
	g_assert_checked (domain != NULL);
	g_assert_checked (assembly != NULL);
	g_assert_checked (assembly_events_func != NULL);

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
	g_assert_checked (domain_events_func != NULL);
	g_assert_checked (assembly_events_func != NULL);
	g_assert_checked (method_events_func != NULL);

	// Under netcore we only have root domain.
	MonoDomain *root_domain = mono_get_root_domain ();
	if (root_domain) {
		uint64_t domain_id = (uint64_t)root_domain;

		// Iterate all functions in use (both JIT and AOT).
		EventPipeFireMethodEventsData events_data;
		events_data.domain = root_domain;
		events_data.buffer_size = 1024 * sizeof(uint32_t);
		events_data.buffer = g_new (uint8_t, events_data.buffer_size);
		events_data.method_events_func = method_events_func;
		mono_jit_info_table_foreach_internal (root_domain, eventpipe_fire_method_events_func, &events_data);
		g_free (events_data.buffer);

		// Iterate all assemblies in domain.
		GPtrArray *assemblies = mono_domain_get_assemblies (root_domain, FALSE);
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

static
gboolean
eventpipe_walk_managed_stack_for_thread_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	gpointer data)
{
	g_assert_checked (frame != NULL);
	g_assert_checked (data != NULL);

	switch (frame->type) {
	case FRAME_TYPE_DEBUGGER_INVOKE:
	case FRAME_TYPE_MANAGED_TO_NATIVE:
	case FRAME_TYPE_TRAMPOLINE:
	case FRAME_TYPE_INTERP_TO_MANAGED:
	case FRAME_TYPE_INTERP_TO_MANAGED_WITH_CTX:
		return FALSE;
	case FRAME_TYPE_MANAGED:
	case FRAME_TYPE_INTERP:
		if (!frame->ji)
			return FALSE;
		MonoMethod *method = frame->ji->async ? NULL : frame->actual_method;
		ep_stack_contents_append ((EventPipeStackContents *)data, (uintptr_t)((uint8_t*)frame->ji->code_start + frame->native_offset), method);
		return ep_stack_contents_get_length ((EventPipeStackContents *)data) >= EP_MAX_STACK_DEPTH;
	default:
		g_assert_not_reached ();
		return FALSE;
	}
}

static
gboolean
eventpipe_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents)
{
	g_assert (thread != NULL && stack_contents != NULL);

	if (thread == ep_rt_thread_get_handle ())
		mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (eventpipe_walk_managed_stack_for_thread_func, NULL, MONO_UNWIND_SIGNAL_SAFE, stack_contents);
	else
		mono_get_eh_callbacks ()->mono_walk_stack_with_state (eventpipe_walk_managed_stack_for_thread_func, mono_thread_info_get_suspend_state (thread), MONO_UNWIND_SIGNAL_SAFE, stack_contents);

	return TRUE;
}

static
gboolean
eventpipe_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	g_assert_checked (method != NULL);
	g_assert_checked (name != NULL);

	MonoClass *method_class = mono_method_get_class (method);
	MonoImage *method_image = method_class ? mono_class_get_image (method_class) : NULL;
	const ep_char8_t *assembly_name = method_image ? mono_image_get_name (method_image) : NULL;

	if (!assembly_name)
		return FALSE;

	g_strlcpy (name, assembly_name, name_len);
	return TRUE;
}

static
gboolean
evetpipe_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	g_assert_checked (method != NULL);
	g_assert_checked (name != NULL);

	char *full_method_name = mono_method_get_name_full (method, TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL);
	if (!full_method_name)
		return FALSE;

	g_strlcpy (name, full_method_name, name_len);

	g_free (full_method_name);
	return TRUE;
}

void
mono_eventpipe_init (
	EventPipeMonoFuncTable *table,
	ep_rt_thread_holder_alloc_func thread_holder_alloc_func,
	ep_rt_thread_holder_free_func thread_holder_free_func)
{
	if (table != NULL) {
		table->ep_rt_mono_cpu_count = mono_cpu_count;
		table->ep_rt_mono_process_current_pid = mono_process_current_pid;
		table->ep_rt_mono_native_thread_id_get = mono_native_thread_id_get;
		table->ep_rt_mono_native_thread_id_equals = mono_native_thread_id_equals;
		table->ep_rt_mono_runtime_is_shutting_down = mono_runtime_is_shutting_down;
		table->ep_rt_mono_rand_try_get_bytes = rand_try_get_bytes_func;
		table->ep_rt_mono_thread_get = eventpipe_thread_get;
		table->ep_rt_mono_thread_get_or_create = eventpipe_thread_get_or_create;
		table->ep_rt_mono_thread_exited = eventpipe_thread_exited;
		table->ep_rt_mono_thread_info_sleep = mono_thread_info_sleep;
		table->ep_rt_mono_thread_info_yield = mono_thread_info_yield;
		table->ep_rt_mono_w32file_close = mono_w32file_close;
		table->ep_rt_mono_w32file_create = mono_w32file_create;
		table->ep_rt_mono_w32file_write = mono_w32file_write;
		table->ep_rt_mono_w32event_create = mono_w32event_create;
		table->ep_rt_mono_w32event_close = mono_w32event_close;
		table->ep_rt_mono_w32event_set = mono_w32event_set;
		table->ep_rt_mono_w32hadle_wait_one = mono_w32handle_wait_one;
		table->ep_rt_mono_valloc = mono_valloc;
		table->ep_rt_mono_vfree = mono_vfree;
		table->ep_rt_mono_valloc_granule = mono_valloc_granule;
		table->ep_rt_mono_thread_platform_create_thread = mono_thread_platform_create_thread;
		table->ep_rt_mono_thread_attach = eventpipe_thread_attach;
		table->ep_rt_mono_thread_detach = eventpipe_thread_detach;
		table->ep_rt_mono_get_os_cmd_line = mono_get_os_cmd_line;
		table->ep_rt_mono_get_managed_cmd_line = mono_runtime_get_managed_cmd_line;
		table->ep_rt_mono_execute_rundown = eventpipe_execute_rundown;
		table->ep_rt_mono_walk_managed_stack_for_thread = eventpipe_walk_managed_stack_for_thread;
		table->ep_rt_mono_method_get_simple_assembly_name = eventpipe_method_get_simple_assembly_name;
		table->ep_rt_mono_method_get_full_name = evetpipe_method_get_full_name;
	}

	thread_holder_alloc_callback_func = thread_holder_alloc_func;
	thread_holder_free_callback_func = thread_holder_free_func;
	mono_native_tls_alloc (&ep_rt_mono_thread_holder_tls_id, NULL);

	mono_100ns_ticks ();
	mono_rand_open ();
	ep_rt_mono_rand_provider = mono_rand_init (NULL, 0);

	ep_rt_mono_initialized = TRUE;

	MonoProfilerHandle profiler = mono_profiler_create (NULL);
	mono_profiler_set_thread_stopped_callback (profiler, profiler_eventpipe_thread_exited);
}

void
mono_eventpipe_fini (void)
{
	if (ep_rt_mono_initialized)
		mono_rand_close (ep_rt_mono_rand_provider);

	ep_rt_mono_rand_provider = NULL;
	thread_holder_alloc_callback_func = NULL;
	thread_holder_free_callback_func = NULL;
	ep_rt_mono_initialized = FALSE;
}

static
void
delegate_callback_data_free_func (
	EventPipeCallback callback_func,
	void *callback_data)
{
	if (callback_data)
		mono_gchandle_free_internal ((MonoGCHandle)callback_data);
}

static
void
delegate_callback_func (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_context)
{

	/*internal unsafe delegate void EtwEnableCallback(
		in Guid sourceId,
		int isEnabled,
		byte level,
		long matchAnyKeywords,
		long matchAllKeywords,
		EVENT_FILTER_DESCRIPTOR* filterData,
		void* callbackContext);*/

	MonoGCHandle delegate_object_handle = (MonoGCHandle)callback_context;
	MonoObject *delegate_object = delegate_object_handle ? mono_gchandle_get_target_internal (delegate_object_handle) : NULL;
	if (delegate_object) {
		void *params [7];
		params [0] = (void *)source_id;
		params [1] = (void *)&is_enabled;
		params [2] = (void *)&level;
		params [3] = (void *)&match_any_keywords;
		params [4] = (void *)&match_all_keywords;
		params [5] = (void *)filter_data;
		params [6] = NULL;

		ERROR_DECL (error);
		mono_runtime_delegate_invoke_checked (delegate_object, params, error);
	}
}

gconstpointer
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_CreateProvider (
	MonoStringHandle provider_name,
	MonoDelegateHandle callback_func,
	MonoError *error)
{
	EventPipeProvider *provider = NULL;
	void *callback_data = NULL;

	if (MONO_HANDLE_IS_NULL (provider_name)) {
		mono_error_set_argument_null (error, "providerName", "");
		return NULL;
	}

	if (!MONO_HANDLE_IS_NULL (callback_func))
		callback_data = (void *)mono_gchandle_new_weakref_internal (MONO_HANDLE_RAW (MONO_HANDLE_CAST (MonoObject, callback_func)), FALSE);

	char *provider_name_utf8 = mono_string_handle_to_utf8 (provider_name, error);
	if (is_ok (error) && provider_name_utf8) {
		provider = ep_create_provider (provider_name_utf8, delegate_callback_func, delegate_callback_data_free_func, callback_data);
	}

	g_free (provider_name_utf8);
	return provider;
}

intptr_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DefineEvent (
	intptr_t provider_handle,
	uint32_t event_id,
	int64_t keywords,
	uint32_t event_version,
	uint32_t level,
	const uint8_t *metadata,
	uint32_t metadata_len)
{
	g_assert (provider_handle != 0);

	EventPipeProvider *provider = (EventPipeProvider *)provider_handle;
	EventPipeEvent *ep_event = ep_provider_add_event (provider, event_id, (uint64_t)keywords, event_version, (EventPipeEventLevel)level, /* needStack = */ true, metadata, metadata_len);

	g_assert (ep_event != NULL);
	return (intptr_t)ep_event;
}

void
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DeleteProvider (intptr_t provider_handle)
{
	if (provider_handle) {
		ep_delete_provider ((EventPipeProvider *)provider_handle);
	}
}

void
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Disable (uint64_t session_id)
{
	ep_disable (session_id);
}

uint64_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Enable (
	const gunichar2 *output_file,
	/* EventPipeSerializationFormat */int32_t format,
	uint32_t circular_buffer_size_mb,
	/* EventPipeProviderConfigurationNative[] */const void *providers,
	uint32_t providers_len)
{
	ERROR_DECL (error);
	EventPipeSessionID session_id = 0;
	char *output_file_utf8 = NULL;

	if (circular_buffer_size_mb == 0 || format > EP_SERIALIZATION_FORMAT_COUNT || providers_len == 0 || providers == NULL)
		return 0;

	if (output_file)
		output_file_utf8 = mono_utf16_to_utf8 (output_file, g_utf16_len (output_file), error);

	EventPipeProviderConfigurationNative *native_config_providers = (EventPipeProviderConfigurationNative *)providers;
	EventPipeProviderConfiguration *config_providers = g_new0 (EventPipeProviderConfiguration, providers_len);

	if (config_providers) {
		for (int i = 0; i < providers_len; ++i) {
			ep_provider_config_init (
				&config_providers[i],
				native_config_providers[i].provider_name ? mono_utf16_to_utf8 (native_config_providers[i].provider_name, g_utf16_len (native_config_providers[i].provider_name), error) : NULL,
				native_config_providers [i].keywords,
				(EventPipeEventLevel)native_config_providers [i].logging_level,
				native_config_providers[i].filter_data ? mono_utf16_to_utf8 (native_config_providers[i].filter_data, g_utf16_len (native_config_providers[i].filter_data), error) : NULL);
		}
	}

	session_id = ep_enable (
		output_file_utf8,
		circular_buffer_size_mb,
		config_providers,
		providers_len,
		output_file != NULL ? EP_SESSION_TYPE_FILE : EP_SESSION_TYPE_LISTENER,
		(EventPipeSerializationFormat)format,
		true,
		NULL,
		NULL);
	ep_start_streaming (session_id);

	if (config_providers) {
		for (int i = 0; i < providers_len; ++i) {
			ep_provider_config_fini (&config_providers[i]);
			g_free ((ep_char8_t *)ep_provider_config_get_provider_name (&config_providers[i]));
			g_free ((ep_char8_t *)ep_provider_config_get_filter_data (&config_providers[i]));
		}
	}

	g_free (output_file_utf8);
	return session_id;
}

int32_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_EventActivityIdControl (
	uint32_t control_code,
	/* GUID * */uint8_t *activity_id)
{
	int32_t result = 0;
	ep_rt_thread_activity_id_handle_t activity_id_handle = ep_thread_get_activity_id_handle ();

	if (activity_id_handle == NULL)
		return 1;

	uint8_t current_activity_id [EP_ACTIVITY_ID_SIZE];
	EventPipeActivityControlCode activity_control_code = (EventPipeActivityControlCode)control_code;
	switch (activity_control_code) {
	case EP_ACTIVITY_CONTROL_GET_ID:
		ep_thread_get_activity_id (activity_id_handle, activity_id, EP_ACTIVITY_ID_SIZE);
		break;
	case EP_ACTIVITY_CONTROL_SET_ID:
		ep_thread_set_activity_id (activity_id_handle, activity_id, EP_ACTIVITY_ID_SIZE);
		break;
	case EP_ACTIVITY_CONTROL_CREATE_ID:
		ep_thread_create_activity_id (activity_id, EP_ACTIVITY_ID_SIZE);
		break;
	case EP_ACTIVITY_CONTROL_GET_SET_ID:
		ep_thread_get_activity_id (activity_id_handle, current_activity_id, EP_ACTIVITY_ID_SIZE);
		ep_thread_set_activity_id (activity_id_handle, activity_id, EP_ACTIVITY_ID_SIZE);
		memcpy (activity_id, current_activity_id, EP_ACTIVITY_ID_SIZE);
		break;
	case EP_ACTIVITY_CONTROL_CREATE_SET_ID:
		ep_thread_get_activity_id (activity_id_handle, activity_id, EP_ACTIVITY_ID_SIZE);
		ep_thread_create_activity_id (current_activity_id, EP_ACTIVITY_ID_SIZE);
		ep_thread_set_activity_id (activity_id_handle, current_activity_id, EP_ACTIVITY_ID_SIZE);
		break;
	default:
		result = 1;
		break;
	}

	return result;
}

MonoBoolean
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetNextEvent (
	uint64_t session_id,
	/* EventPipeEventInstanceData * */void *instance)
{
	g_assert (instance != NULL);

	EventPipeEventInstance *const next_instance = ep_get_next_event (session_id);
	EventPipeEventInstanceData *const data = (EventPipeEventInstanceData *)instance;
	if (next_instance && data) {
		const EventPipeEvent *const ep_event = ep_event_instance_get_ep_event (next_instance);
		if (ep_event) {
			data->provider_id = (intptr_t)ep_event_get_provider (ep_event);
			data->event_id = ep_event_get_event_id (ep_event);
		}
		data->thread_id = ep_event_instance_get_thread_id (next_instance);
		data->timestamp = ep_event_instance_get_timestamp (next_instance);
		memcpy (&data->activity_id, ep_event_instance_get_activity_id_cref (next_instance), EP_ACTIVITY_ID_SIZE);
		memcpy (&data->related_activity_id, ep_event_instance_get_related_activity_id_cref (next_instance), EP_ACTIVITY_ID_SIZE);
		data->payload = ep_event_instance_get_data (next_instance);
		data->payload_len = ep_event_instance_get_data_len (next_instance);
	}

	return next_instance != NULL;
}

intptr_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetProvider (const gunichar2 *provider_name)
{
	ERROR_DECL (error);
	char * provider_name_utf8 = NULL;
	EventPipeProvider *provider = NULL;

	if (provider_name) {
		provider_name_utf8 = mono_utf16_to_utf8 (provider_name, g_utf16_len (provider_name), error);
		provider = ep_get_provider (provider_name_utf8);
	}

	g_free (provider_name_utf8);
	return (intptr_t)provider;
}

MonoBoolean
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetSessionInfo (
	uint64_t session_id,
	/* EventPipeSessionInfo * */void *session_info)
{
	bool result = false;
	if (session_info) {
		EventPipeSession *session = ep_get_session ((EventPipeSessionID)session_id);
		if (session) {
			EventPipeSessionInfo *instance = (EventPipeSessionInfo *)session_info;
			instance->starttime_as_utc_filetime = ep_session_get_session_start_time (session);
			instance->start_timestamp = ep_session_get_session_start_timestamp (session);
			instance->timestamp_frequency = ep_perf_frequency_query ();
			result = true;
		}
	}

	return result;
}

intptr_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetWaitHandle (uint64_t session_id)
{
	return (intptr_t)ep_get_wait_handle (session_id);
}

void
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_WriteEventData (
	intptr_t event_handle,
	/* EventData[] */void *event_data,
	uint32_t event_data_len,
	/* GUID * */const uint8_t *activity_id,
	/* GUID * */const uint8_t *related_activity_id)
{
	g_assert (event_handle);
	EventPipeEvent *ep_event = (EventPipeEvent *)event_handle;
	ep_write_event_2 (ep_event, (EventData *)event_data, event_data_len, activity_id, related_activity_id);
}

#else /* ENABLE_PERFTRACING */

gconstpointer
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_CreateProvider (
	MonoStringHandle provider_name,
	MonoDelegateHandle callback_func,
	MonoError *error)
{
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.CreateProvider");
	return NULL;
}

intptr_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DefineEvent (
	intptr_t provider_handle,
	uint32_t event_id,
	int64_t keywords,
	uint32_t event_version,
	uint32_t level,
	const uint8_t *metadata,
	uint32_t metadata_len)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.DefineEvent");
	mono_error_set_pending_exception (error);
	return 0;
}

void
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DeleteProvider (intptr_t provider_handle)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.DeleteProvider");
	mono_error_set_pending_exception (error);
}

void
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Disable (uint64_t session_id)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.Disable");
	mono_error_set_pending_exception (error);
}

uint64_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Enable (
	const gunichar2 *output_file,
	/* EventPipeSerializationFormat */int32_t format,
	uint32_t circular_buffer_size_mb,
	/* EventPipeProviderConfigurationNative[] */const void *providers,
	uint32_t providers_len)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.Enable");
	mono_error_set_pending_exception (error);
	return 0;
}

int32_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_EventActivityIdControl (
	uint32_t control_code,
	/* GUID * */uint8_t *activity_id)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.EventActivityIdControl");
	mono_error_set_pending_exception (error);
	return 0;
}

MonoBoolean
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetNextEvent (
	uint64_t session_id,
	/* EventPipeEventInstanceData * */void *instance)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.GetNextEvent");
	mono_error_set_pending_exception (error);
	return FALSE;
}

intptr_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetProvider (const gunichar2 *provider_name)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.GetProvider");
	mono_error_set_pending_exception (error);
	return 0;
}

MonoBoolean
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetSessionInfo (
	uint64_t session_id,
	/* EventPipeSessionInfo * */void *session_info)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.GetSessionInfo");
	mono_error_set_pending_exception (error);
	return FALSE;
}

intptr_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetWaitHandle (uint64_t session_id)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.GetWaitHandle");
	mono_error_set_pending_exception (error);
	return 0;
}

void
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_WriteEventData (
	intptr_t event_handle,
	/* EventData[] */void *event_data,
	uint32_t event_data_len,
	/* GUID * */const uint8_t *activity_id,
	/* GUID * */const uint8_t *related_activity_id)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.WriteEventData");
	mono_error_set_pending_exception (error);
}

#endif /* ENABLE_PERFTRACING */
#endif /* ENABLE_NETCORE */

MONO_EMPTY_SOURCE_FILE (icall_eventpipe);
