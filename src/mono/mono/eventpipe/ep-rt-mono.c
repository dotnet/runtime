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
#include <mono/metadata/profiler.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-internals.h>
#include <mono/metadata/gc-internals.h>
#include <mono/mini/mini-runtime.h>
#include <runtime_version.h>

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

// Rundown events.
EventPipeProvider *EventPipeProviderDotNETRuntimeRundown = NULL;
EventPipeEvent *EventPipeEventMethodDCEndVerbose_V1 = NULL;
EventPipeEvent *EventPipeEventDCEndInit_V1 = NULL;
EventPipeEvent *EventPipeEventDCEndComplete_V1 = NULL;
EventPipeEvent *EventPipeEventMethodDCEndILToNativeMap = NULL;
EventPipeEvent *EventPipeEventDomainModuleDCEnd_V1 = NULL;
EventPipeEvent *EventPipeEventModuleDCEnd_V2 = NULL;
EventPipeEvent *EventPipeEventAssemblyDCEnd_V1 = NULL;
EventPipeEvent *EventPipeEventAppDomainDCEnd_V1 = NULL;
EventPipeEvent *EventPipeEventRuntimeInformationDCStart = NULL;

// Runtime private events.
EventPipeProvider *EventPipeProviderDotNETRuntimePrivate = NULL;
EventPipeEvent *EventPipeEventEEStartupStart_V1 = NULL;

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

typedef struct _EventPipeSampleProfileData {
	EventPipeStackContents stack_contents;
	uint64_t thread_id;
	uintptr_t thread_ip;
	uint32_t payload_data;
} EventPipeSampleProfileData;

// Rundown flags.
#define RUNTIME_SKU_CORECLR 0x2
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

/*
 * Forward declares of all static functions.
 */

static
bool
resize_buffer (
	uint8_t **buffer,
	size_t *size,
	size_t current_size,
	size_t new_size,
	bool *fixed_buffer);

static
bool
write_buffer (
	const uint8_t *value,
	size_t value_size,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer);

static
bool
write_buffer_string_utf8_t (
	const ep_char8_t *value,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer);

static
bool
write_runtime_info_dc_start (
	const uint16_t clr_instance_id,
	const uint16_t sku_id,
	const uint16_t bcl_major_version,
	const uint16_t bcl_minor_version,
	const uint16_t bcl_build_number,
	const uint16_t bcl_qfe_number,
	const uint16_t vm_major_version,
	const uint16_t vm_minor_version,
	const uint16_t vm_build_number,
	const uint16_t vm_qfe_number,
	const uint32_t startup_flags,
	const uint8_t startup_mode,
	const ep_char8_t *cmd_line,
	const uint8_t * object_guid,
	const ep_char8_t *runtime_dll_path,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
write_runtime_info_dc_start (
	const uint16_t clr_instance_id,
	const uint16_t sku_id,
	const uint16_t bcl_major_version,
	const uint16_t bcl_minor_version,
	const uint16_t bcl_build_number,
	const uint16_t bcl_qfe_number,
	const uint16_t vm_major_version,
	const uint16_t vm_minor_version,
	const uint16_t vm_build_number,
	const uint16_t vm_qfe_number,
	const uint32_t startup_flags,
	const uint8_t startup_mode,
	const ep_char8_t *cmd_line,
	const uint8_t * object_guid,
	const ep_char8_t *runtime_dll_path,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
write_event_dc_end_complete_v1 (
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
write_event_method_dc_end_il_to_native_map (
	const uint64_t method_id,
	const uint64_t rejit_id,
	const uint8_t method_extent,
	const uint16_t count_of_map_entries,
	const uint32_t *il_offsets,
	const uint32_t *native_offsets,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
write_event_method_dc_end_verbose_v1 (
	const uint64_t method_id,
	const uint64_t module_id,
	const uint64_t method_start_address,
	const uint32_t method_size,
	const uint32_t method_token,
	const uint32_t method_flags,
	const ep_char8_t *method_namespace,
	const ep_char8_t *method_name,
	const ep_char8_t *method_signature,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
write_event_module_dc_end_v2 (
	const uint64_t module_id,
	const uint64_t assembly_id,
	const uint32_t module_flags,
	const uint32_t reserved_1,
	const ep_char8_t *module_il_path,
	const ep_char8_t *module_native_path,
	const uint16_t clr_instance_id,
	const uint8_t *managed_pdb_signature,
	const uint32_t managed_pdb_age,
	const ep_char8_t *managed_pdb_build_path,
	const uint8_t *native_pdb_signature,
	const uint32_t native_pdb_age,
	const ep_char8_t *native_pdb_build_path,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
write_event_module_dc_end_v2 (
	const uint64_t module_id,
	const uint64_t assembly_id,
	const uint32_t module_flags,
	const uint32_t reserved_1,
	const ep_char8_t *module_il_path,
	const ep_char8_t *module_native_path,
	const uint16_t clr_instance_id,
	const uint8_t *managed_pdb_signature,
	const uint32_t managed_pdb_age,
	const ep_char8_t *managed_pdb_build_path,
	const uint8_t *native_pdb_signature,
	const uint32_t native_pdb_age,
	const ep_char8_t *native_pdb_build_path,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
write_event_assembly_dc_end_v1 (
	const uint64_t assembly_id,
	const uint64_t domain_id,
	const uint64_t binding_id,
	const uint32_t assembly_flags,
	const ep_char8_t *fully_qualified_name,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
bool
write_event_domain_dc_end_v1 (
	const uint64_t domain_id,
	const uint32_t domain_flags,
	const ep_char8_t *domain_name,
	const uint32_t domain_index,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

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
init_dotnet_runtime_rundown (void);

static
bool
write_event_ee_startup_start_v1 (
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id);

static
void
init_dotnet_runtime_private (void);

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

/*
 * Forward declares of all private functions (accessed using extern in ep-rt-mono.h).
 */

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
ep_rt_mono_execute_rundown (void);

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
resize_buffer (
	uint8_t **buffer,
	size_t *size,
	size_t current_size,
	size_t new_size,
	bool *fixed_buffer)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (size != NULL);
	EP_ASSERT (fixed_buffer != NULL);

	new_size = (size_t)(new_size * 1.5);
	if (new_size < *size) {
		EP_ASSERT (!"Overflow");
		return false;
	}

	if (new_size < 32)
		new_size = 32;

	uint8_t *new_buffer;
	new_buffer = ep_rt_byte_array_alloc (new_size);
	ep_raise_error_if_nok (new_buffer != NULL);

	memcpy (new_buffer, *buffer, current_size);

	if (!*fixed_buffer)
		ep_rt_byte_array_free (*buffer);

	*buffer = new_buffer;
	*size = new_size;
	*fixed_buffer = false;

	return true;

ep_on_error:
	return false;
}

static
bool
write_buffer (
	const uint8_t *value,
	size_t value_size,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer)
{
	EP_ASSERT (value != NULL);
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (offset != NULL);
	EP_ASSERT (size != NULL);
	EP_ASSERT (fixed_buffer != NULL);

	if ((value_size + *offset) > *size)
		ep_raise_error_if_nok (resize_buffer (buffer, size, *offset, *size + value_size, fixed_buffer));

	memcpy (*buffer + *offset, value, value_size);
	*offset += value_size;

	return true;

ep_on_error:
	return false;
}

static
bool
write_buffer_string_utf8_t (
	const ep_char8_t *value,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer)
{
	if (!value)
		return true;

	GFixedBufferCustomAllocatorData custom_alloc_data;
	custom_alloc_data.buffer = *buffer + *offset;
	custom_alloc_data.buffer_size = *size - *offset;
	custom_alloc_data.req_buffer_size = 0;

	if (!g_utf8_to_utf16_custom_alloc (value, -1, NULL, NULL, g_fixed_buffer_custom_allocator, &custom_alloc_data, NULL)) {
		ep_raise_error_if_nok (resize_buffer (buffer, size, *offset, *size + custom_alloc_data.req_buffer_size, fixed_buffer));
		custom_alloc_data.buffer = *buffer + *offset;
		custom_alloc_data.buffer_size = *size - *offset;
		custom_alloc_data.req_buffer_size = 0;
		ep_raise_error_if_nok (g_utf8_to_utf16_custom_alloc (value, -1, NULL, NULL, g_fixed_buffer_custom_allocator, &custom_alloc_data, NULL) != NULL);
	}

	*offset += custom_alloc_data.req_buffer_size;
	return true;

ep_on_error:
	return false;
}

static
inline
bool
write_buffer_guid_t (
	const uint8_t *value,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer)
{
	return write_buffer (value, EP_GUID_SIZE, buffer, offset, size, fixed_buffer);
}

static
inline
bool
write_buffer_uint8_t (
	const uint8_t *value,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer)
{
	return write_buffer (value, sizeof (uint8_t), buffer, offset, size, fixed_buffer);
}

static
inline
bool
write_buffer_uint16_t (
	const uint16_t *value,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer)
{
	return write_buffer ((const uint8_t *)value, sizeof (uint16_t), buffer, offset, size, fixed_buffer);
}

static
inline
bool
write_buffer_uint32_t (
	const uint32_t *value,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer)
{
	return write_buffer ((const uint8_t *)value, sizeof (uint32_t), buffer, offset, size, fixed_buffer);
}

static
inline
bool
write_buffer_uint64_t (
	const uint64_t *value,
	uint8_t **buffer,
	size_t *offset,
	size_t *size,
	bool *fixed_buffer)
{
	return write_buffer ((const uint8_t *)value, sizeof (uint64_t), buffer, offset, size, fixed_buffer);
}

static
bool
write_runtime_info_dc_start (
	const uint16_t clr_instance_id,
	const uint16_t sku_id,
	const uint16_t bcl_major_version,
	const uint16_t bcl_minor_version,
	const uint16_t bcl_build_number,
	const uint16_t bcl_qfe_number,
	const uint16_t vm_major_version,
	const uint16_t vm_minor_version,
	const uint16_t vm_build_number,
	const uint16_t vm_qfe_number,
	const uint32_t startup_flags,
	const uint8_t startup_mode,
	const ep_char8_t *cmd_line,
	const uint8_t * object_guid,
	const ep_char8_t *runtime_dll_path,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventRuntimeInformationDCStart != NULL);

	if (!ep_event_is_enabled (EventPipeEventRuntimeInformationDCStart))
		return true;

	uint8_t stack_buffer [153];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&sku_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&bcl_major_version, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&bcl_minor_version, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&bcl_build_number, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&bcl_qfe_number, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&vm_major_version, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&vm_minor_version, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&vm_build_number, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&vm_qfe_number, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&startup_flags, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint8_t (&startup_mode, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (cmd_line, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_guid_t (object_guid, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (runtime_dll_path, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventRuntimeInformationDCStart, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_dc_end_init_v1 (
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventDCEndInit_V1 != NULL);

	if (!ep_event_is_enabled (EventPipeEventDCEndInit_V1))
		return true;

	uint8_t stack_buffer [32];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventDCEndInit_V1, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_dc_end_complete_v1 (
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventDCEndComplete_V1 != NULL);

	if (!ep_event_is_enabled (EventPipeEventDCEndComplete_V1))
		return true;

	uint8_t stack_buffer [32];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventDCEndComplete_V1, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_method_dc_end_il_to_native_map (
	const uint64_t method_id,
	const uint64_t rejit_id,
	const uint8_t method_extent,
	const uint16_t count_of_map_entries,
	const uint32_t *il_offsets,
	const uint32_t *native_offsets,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventMethodDCEndILToNativeMap != NULL);

	if (!ep_event_is_enabled (EventPipeEventMethodDCEndILToNativeMap))
		return true;

	uint8_t stack_buffer [32];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint64_t (&method_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint64_t (&rejit_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint8_t (&method_extent, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&count_of_map_entries, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer ((const uint8_t *)il_offsets, sizeof (const uint32_t) * (int32_t)count_of_map_entries, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer ((const uint8_t *)native_offsets, sizeof (const uint32_t) * (int32_t)count_of_map_entries, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventMethodDCEndILToNativeMap, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_method_dc_end_verbose_v1 (
	const uint64_t method_id,
	const uint64_t module_id,
	const uint64_t method_start_address,
	const uint32_t method_size,
	const uint32_t method_token,
	const uint32_t method_flags,
	const ep_char8_t *method_namespace,
	const ep_char8_t *method_name,
	const ep_char8_t *method_signature,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventMethodDCEndVerbose_V1 != NULL);

	if (!ep_event_is_enabled (EventPipeEventMethodDCEndVerbose_V1))
		return true;

	uint8_t stack_buffer [230];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint64_t (&method_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint64_t (&module_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint64_t (&method_start_address, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&method_size, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&method_token, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&method_flags, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (method_namespace, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (method_name, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (method_signature, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventMethodDCEndVerbose_V1, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_module_dc_end_v2 (
	const uint64_t module_id,
	const uint64_t assembly_id,
	const uint32_t module_flags,
	const uint32_t reserved_1,
	const ep_char8_t *module_il_path,
	const ep_char8_t *module_native_path,
	const uint16_t clr_instance_id,
	const uint8_t *managed_pdb_signature,
	const uint32_t managed_pdb_age,
	const ep_char8_t *managed_pdb_build_path,
	const uint8_t *native_pdb_signature,
	const uint32_t native_pdb_age,
	const ep_char8_t *native_pdb_build_path,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventModuleDCEnd_V2 != NULL);

	if (!ep_event_is_enabled (EventPipeEventModuleDCEnd_V2))
		return true;

	uint8_t stack_buffer [290];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint64_t (&module_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint64_t (&assembly_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&module_flags, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&reserved_1, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (module_il_path, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (module_native_path, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_guid_t (managed_pdb_signature, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&managed_pdb_age, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (managed_pdb_build_path, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_guid_t (native_pdb_signature, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&native_pdb_age, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (native_pdb_build_path, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventModuleDCEnd_V2, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_domain_module_dc_end_v1 (
	const uint64_t module_id,
	const uint64_t assembly_id,
	const uint64_t domain_id,
	const uint32_t module_flags,
	const uint32_t reserved_1,
	const ep_char8_t *module_il_path,
	const ep_char8_t *module_native_path,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventDomainModuleDCEnd_V1 != NULL);

	if (!ep_event_is_enabled (EventPipeEventDomainModuleDCEnd_V1))
		return true;

	uint8_t stack_buffer [162];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint64_t (&module_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint64_t (&assembly_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint64_t (&domain_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&module_flags, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&reserved_1, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (module_il_path, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (module_native_path, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventDomainModuleDCEnd_V1, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_assembly_dc_end_v1 (
	const uint64_t assembly_id,
	const uint64_t domain_id,
	const uint64_t binding_id,
	const uint32_t assembly_flags,
	const ep_char8_t *fully_qualified_name,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventAssemblyDCEnd_V1 != NULL);

	if (!ep_event_is_enabled (EventPipeEventAssemblyDCEnd_V1))
		return true;

	uint8_t stack_buffer [94];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint64_t (&assembly_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint64_t (&domain_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint64_t (&binding_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&assembly_flags, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (fully_qualified_name, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventAssemblyDCEnd_V1, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_domain_dc_end_v1 (
	const uint64_t domain_id,
	const uint32_t domain_flags,
	const ep_char8_t *domain_name,
	const uint32_t domain_index,
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventAppDomainDCEnd_V1 != NULL);

	if (!ep_event_is_enabled (EventPipeEventAppDomainDCEnd_V1))
		return true;

	uint8_t stack_buffer [82];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint64_t (&domain_id, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&domain_flags, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_string_utf8_t (domain_name, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint32_t (&domain_index, &buffer, &offset, &size, &fixed_buffer);
	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventAppDomainDCEnd_V1, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

static
bool
write_event_ee_startup_start_v1 (
	const uint16_t clr_instance_id,
	const uint8_t *activity_id,
	const uint8_t *related_activity_id)
{
	EP_ASSERT (EventPipeEventEEStartupStart_V1 != NULL);

	if (!ep_event_is_enabled (EventPipeEventEEStartupStart_V1))
		return true;

	uint8_t stack_buffer [32];
	uint8_t *buffer = stack_buffer;
	size_t offset = 0;
	size_t size = sizeof (stack_buffer);
	bool fixed_buffer = true;
	bool success = true;

	success &= write_buffer_uint16_t (&clr_instance_id, &buffer, &offset, &size, &fixed_buffer);

	ep_raise_error_if_nok (success);

	ep_write_event (EventPipeEventEEStartupStart_V1, buffer, (uint32_t)offset, activity_id, related_activity_id);

ep_on_exit:
	if (!fixed_buffer)
		ep_rt_byte_array_free (buffer);
	return success;

ep_on_error:
	EP_ASSERT (!success);
	ep_exit_error_handler ();
}

// Mapping FireEtw* CoreClr functions.
#define FireEtwRuntimeInformationDCStart(...) write_runtime_info_dc_start(__VA_ARGS__,NULL,NULL)
#define FireEtwDCEndInit_V1(...) write_event_dc_end_init_v1(__VA_ARGS__,NULL,NULL)
#define FireEtwMethodDCEndILToNativeMap(...) write_event_method_dc_end_il_to_native_map(__VA_ARGS__,NULL,NULL)
#define FireEtwMethodDCEndVerbose_V1(...) write_event_method_dc_end_verbose_v1(__VA_ARGS__,NULL,NULL)
#define FireEtwModuleDCEnd_V2(...) write_event_module_dc_end_v2(__VA_ARGS__,NULL,NULL)
#define FireEtwDomainModuleDCEnd_V1(...) write_event_domain_module_dc_end_v1(__VA_ARGS__,NULL,NULL)
#define FireEtwAssemblyDCEnd_V1(...) write_event_assembly_dc_end_v1(__VA_ARGS__,NULL,NULL)
#define FireEtwAppDomainDCEnd_V1(...) write_event_domain_dc_end_v1(__VA_ARGS__,NULL,NULL)
#define FireEtwDCEndComplete_V1(...) write_event_dc_end_complete_v1(__VA_ARGS__,NULL,NULL)
#define FireEtwEEStartupStart_V1(...) write_event_ee_startup_start_v1(__VA_ARGS__,NULL,NULL)

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
	void *user_data)
{
	FireEtwMethodDCEndILToNativeMap (
		method_id,
		0,
		0,
		count_of_map_entries,
		il_offsets,
		native_offsets,
		clr_instance_get_id ());

	FireEtwMethodDCEndVerbose_V1 (
		method_id,
		module_id,
		method_start_address,
		method_size,
		method_token,
		method_flags,
		method_namespace,
		method_name,
		method_signature,
		clr_instance_get_id ());

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
		native_pdb_build_path);

	FireEtwDomainModuleDCEnd_V1 (
		module_id,
		assembly_id,
		domain_id,
		module_flags,
		reserved_flags,
		module_il_path,
		module_native_path,
		clr_instance_get_id ());

	FireEtwAssemblyDCEnd_V1 (
		assembly_id,
		domain_id,
		binding_id,
		assembly_flags,
		assembly_name,
		clr_instance_get_id ());

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
		clr_instance_get_id ());
}

static
void
init_dotnet_runtime_rundown (void)
{
	//TODO: Add callback method to enable/disable more native events getting into EventPipe (when enabled).
	EP_ASSERT (EventPipeProviderDotNETRuntimeRundown == NULL);
	EventPipeProviderDotNETRuntimeRundown = ep_create_provider (ep_config_get_rundown_provider_name_utf8 (), NULL, NULL, NULL);

	EP_ASSERT (EventPipeEventMethodDCEndVerbose_V1 == NULL);
	EventPipeEventMethodDCEndVerbose_V1 = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 144, 48, 1, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);

	EP_ASSERT (EventPipeEventDCEndComplete_V1 == NULL);
	EventPipeEventDCEndComplete_V1 = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 146, 131128, 1, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);

	EP_ASSERT (EventPipeEventDCEndInit_V1 == NULL);
	EventPipeEventDCEndInit_V1 = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 148, 131128, 1, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);

	EP_ASSERT (EventPipeEventMethodDCEndILToNativeMap == NULL);
	EventPipeEventMethodDCEndILToNativeMap = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 150, 131072, 0, EP_EVENT_LEVEL_VERBOSE, true, NULL, 0);

	EP_ASSERT (EventPipeEventDomainModuleDCEnd_V1 == NULL);
	EventPipeEventDomainModuleDCEnd_V1 = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 152, 8, 1, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);

	EP_ASSERT (EventPipeEventModuleDCEnd_V2 == NULL);
	EventPipeEventModuleDCEnd_V2 = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 154, 536870920, 2, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);

	EP_ASSERT (EventPipeEventAssemblyDCEnd_V1 == NULL);
	EventPipeEventAssemblyDCEnd_V1 = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 156, 8, 1, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);

	EP_ASSERT (EventPipeEventAppDomainDCEnd_V1 == NULL);
	EventPipeEventAppDomainDCEnd_V1 = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 158, 8, 1, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);

	EP_ASSERT (EventPipeEventRuntimeInformationDCStart == NULL);
	EventPipeEventRuntimeInformationDCStart = ep_provider_add_event (EventPipeProviderDotNETRuntimeRundown, 187, 0, 0, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);
}

static
void
init_dotnet_runtime_private (void)
{
	//TODO: Add callback method to enable/disable more native events getting into EventPipe (when enabled).
	EP_ASSERT (EventPipeProviderDotNETRuntimePrivate == NULL);
	EventPipeProviderDotNETRuntimePrivate = ep_create_provider (ep_config_get_private_provider_name_utf8 (), NULL, NULL, NULL);

	EP_ASSERT (EventPipeEventEEStartupStart_V1 == NULL);
	EventPipeEventEEStartupStart_V1 = ep_provider_add_event (EventPipeProviderDotNETRuntimePrivate, 80, 2147483648, 1, EP_EVENT_LEVEL_INFORMATIONAL, true, NULL, 0);
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
		NULL);

	g_free (method_namespace);
	g_free (method_signature);
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
		if (method && !m_method_is_wrapper (method))
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

		// Iterate all functions in use (both JIT and AOT).
		EventPipeFireMethodEventsData events_data;
		events_data.domain = root_domain;
		events_data.buffer_size = 1024 * sizeof(uint32_t);
		events_data.buffer = g_new (uint8_t, events_data.buffer_size);
		events_data.method_events_func = method_events_func;
		mono_jit_info_table_foreach_internal (eventpipe_fire_method_events_func, &events_data);
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

static
gboolean
eventpipe_walk_managed_stack_for_thread_func (
	MonoStackFrameInfo *frame,
	MonoContext *ctx,
	void *data)
{
	EP_ASSERT (frame != NULL);
	EP_ASSERT (data != NULL);

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
		if (method && !m_method_is_wrapper (method))
			ep_stack_contents_append ((EventPipeStackContents *)data, (uintptr_t)((uint8_t*)frame->ji->code_start + frame->native_offset), method);
		return ep_stack_contents_get_length ((EventPipeStackContents *)data) >= EP_MAX_STACK_DEPTH;
	default:
		EP_UNREACHABLE ("eventpipe_walk_managed_stack_for_thread_func");
		return FALSE;
	}
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

	EventPipeSampleProfileData *sample_data = (EventPipeSampleProfileData *)data;

	if (sample_data->payload_data == EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR) {
		if (frame->type == FRAME_TYPE_MANAGED_TO_NATIVE)
			sample_data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_EXTERNAL;
		else
			sample_data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_MANAGED;
	}

	return eventpipe_walk_managed_stack_for_thread_func (frame, ctx, &sample_data->stack_contents);
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

void
ep_rt_mono_init (void)
{
	mono_native_tls_alloc (&_ep_rt_mono_thread_holder_tls_id, NULL);

	mono_100ns_ticks ();
	mono_rand_open ();
	_ep_rt_mono_rand_provider = mono_rand_init (NULL, 0);

	_ep_rt_mono_initialized = TRUE;

	MonoProfilerHandle profiler = mono_profiler_create (NULL);
	mono_profiler_set_thread_stopped_callback (profiler, profiler_eventpipe_thread_exited);
}

void
ep_rt_mono_init_finish (void)
{
	if (mono_runtime_get_no_exec ())
		return;

	// Managed init of diagnostics classes, like registration of RuntimeEventSource (if available).
	ERROR_DECL (error);

	MonoClass *runtime_event_source = mono_class_from_name_checked (mono_defaults.corlib, "System.Diagnostics.Tracing", "RuntimeEventSource", error);
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
	init_dotnet_runtime_rundown ();
	init_dotnet_runtime_private ();
}

void
ep_rt_mono_fini_providers_and_events (void)
{
	// dotnet/runtime: issue 12775: EventPipe shutdown race conditions
	// Deallocating providers/events here might cause AV if a WriteEvent
	// was to occur. Thus, we are not doing this cleanup.

	// ep_delete_provider (EventPipeProviderDotNETRuntimePrivate);
	// ep_delete_provider (EventPipeProviderDotNETRuntimeRundown);
}

bool
ep_rt_mono_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents)
{
	EP_ASSERT (thread != NULL && stack_contents != NULL);

	if (thread == ep_rt_thread_get_handle ())
		mono_get_eh_callbacks ()->mono_walk_stack_with_ctx (eventpipe_walk_managed_stack_for_thread_func, NULL, MONO_UNWIND_SIGNAL_SAFE, stack_contents);
	else
		mono_get_eh_callbacks ()->mono_walk_stack_with_state (eventpipe_walk_managed_stack_for_thread_func, mono_thread_info_get_suspend_state (thread), MONO_UNWIND_SIGNAL_SAFE, stack_contents);

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
		_ep_rt_mono_sampled_thread_callstacks = g_array_sized_new (FALSE, FALSE, sizeof (EventPipeSampleProfileData), _ep_rt_mono_max_sampled_thread_count);

	// Make sure there is room based on previous max number of sampled threads.
	// NOTE, there is a chance there are more threads than max, if that's the case we will
	// miss those threads in this sample, but will be included in next when max has been adjusted.
	g_array_set_size (_ep_rt_mono_sampled_thread_callstacks, _ep_rt_mono_max_sampled_thread_count);

	uint32_t filtered_thread_count = 0;
	uint32_t sampled_thread_count = 0;

	mono_stop_world (MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE);

	// Record all info needed in sample events while runtime is suspended, must be async safe.
	FOREACH_THREAD_SAFE_EXCLUDE (thread_info, MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE) {
		if (!mono_thread_info_is_running (thread_info)) {
			MonoThreadUnwindState *thread_state = mono_thread_info_get_suspend_state (thread_info);
			if (thread_state->valid) {
				if (sampled_thread_count < _ep_rt_mono_max_sampled_thread_count) {
					EventPipeSampleProfileData *data = &g_array_index (_ep_rt_mono_sampled_thread_callstacks, EventPipeSampleProfileData, sampled_thread_count);
					data->thread_id = ep_rt_thread_id_t_to_uint64_t (mono_thread_info_get_tid (thread_info));
					data->thread_ip = (uintptr_t)MONO_CONTEXT_GET_IP (&thread_state->ctx);
					data->payload_data = EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR;
					ep_stack_contents_reset (&data->stack_contents);
					mono_get_eh_callbacks ()->mono_walk_stack_with_state (eventpipe_sample_profiler_walk_managed_stack_for_thread_func, thread_state, MONO_UNWIND_SIGNAL_SAFE, data);
					sampled_thread_count++;
				}
			}
		}
		filtered_thread_count++;
	} FOREACH_THREAD_SAFE_END

	mono_restart_world (MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE);

	// Fire sample event for threads. Must be done after runtime is resumed since it's not async safe.
	// Since we can't keep thread info around after runtime as been suspended, use an empty
	// adapter instance and only set recorded tid as parameter inside adapter.
	THREAD_INFO_TYPE adapter = { { 0 } };
	for (uint32_t i = 0; i < sampled_thread_count; ++i) {
		EventPipeSampleProfileData *data = &g_array_index (_ep_rt_mono_sampled_thread_callstacks, EventPipeSampleProfileData, i);
		if (data->payload_data != EP_SAMPLE_PROFILER_SAMPLE_TYPE_ERROR && ep_stack_contents_get_length(&data->stack_contents) > 0) {
			mono_thread_info_set_tid (&adapter, ep_rt_uint64_t_to_thread_id_t (data->thread_id));
			ep_write_sample_profile_event (sampling_thread, sampling_event, &adapter, &data->stack_contents, (uint8_t *)&data->payload_data, sizeof (data->payload_data));
		}
	}

	// Current thread count will be our next maximum sampled threads.
	_ep_rt_mono_max_sampled_thread_count = filtered_thread_count;

	return true;
}

void
ep_rt_mono_execute_rundown (void)
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
		runtime_module_path);

	FireEtwDCEndInit_V1 (clr_instance_get_id ());

	eventpipe_execute_rundown (
		fire_domain_rundown_events_func,
		fire_assembly_rundown_events_func,
		fire_method_rundown_events_func);

	FireEtwDCEndComplete_V1 (clr_instance_get_id ());
}

bool
ep_rt_mono_write_event_ee_startup_start (void)
{
	return FireEtwEEStartupStart_V1 (clr_instance_get_id ());
}

#endif /* ENABLE_PERFTRACING */

MONO_EMPTY_SOURCE_FILE(eventpipe_rt_mono);
