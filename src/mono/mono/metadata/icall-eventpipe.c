#include <config.h>
#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/icall-decl.h>

#if defined(ENABLE_PERFTRACING) && !defined(DISABLE_EVENTPIPE)
#include <mono/metadata/components.h>
#include <mono/metadata/assembly-internals.h>

/*
 * Forward declares of all static functions.
 */

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
		provider = mono_component_event_pipe ()->create_provider (provider_name_utf8, delegate_callback_func, delegate_callback_data_free_func, callback_data);
	}

	g_free (provider_name_utf8);
	return (gconstpointer)provider;
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
	EventPipeEvent *ep_event = mono_component_event_pipe ()->provider_add_event (provider, event_id, (uint64_t)keywords, event_version, (EventPipeEventLevel)level, /* needStack = */ true, metadata, metadata_len);

	g_assert (ep_event != NULL);
	return (intptr_t)ep_event;
}

void
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_DeleteProvider (intptr_t provider_handle)
{
	if (provider_handle)
		mono_component_event_pipe ()->delete_provider ((EventPipeProvider *)provider_handle);
}

void
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_Disable (uint64_t session_id)
{
	mono_component_event_pipe ()->disable (session_id);
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

	session_id = mono_component_event_pipe ()->enable (
		output_file_utf8,
		circular_buffer_size_mb,
		(EventPipeProviderConfigurationNative *)providers,
		providers_len,
		output_file != NULL ? EP_SESSION_TYPE_FILE : EP_SESSION_TYPE_LISTENER,
		(EventPipeSerializationFormat)format,
		true,
		NULL,
		NULL);

	mono_component_event_pipe ()->start_streaming (session_id);

	g_free (output_file_utf8);
	return (uint64_t)session_id;
}

int32_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_EventActivityIdControl (
	uint32_t control_code,
	/* GUID * */uint8_t *activity_id)
{
	return mono_component_event_pipe ()->thread_ctrl_activity_id ((EventPipeActivityControlCode)control_code, activity_id, EP_ACTIVITY_ID_SIZE) ? 0 : 1;
}

MonoBoolean
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetNextEvent (
	uint64_t session_id,
	/* EventPipeEventInstanceData * */void *instance)
{
	return mono_component_event_pipe ()->get_next_event ((EventPipeSessionID)session_id, (EventPipeEventInstanceData *)instance) ? TRUE : FALSE;
}

intptr_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetProvider (const gunichar2 *provider_name)
{
	ERROR_DECL (error);
	char * provider_name_utf8 = NULL;
	EventPipeProvider *provider = NULL;

	if (provider_name) {
		provider_name_utf8 = mono_utf16_to_utf8 (provider_name, g_utf16_len (provider_name), error);
		provider = mono_component_event_pipe()->get_provider (provider_name_utf8);
	}

	g_free (provider_name_utf8);
	return (intptr_t)provider;
}

MonoBoolean
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetSessionInfo (
	uint64_t session_id,
	/* EventPipeSessionInfo * */void *session_info)
{
	return mono_component_event_pipe()->get_session_info (session_id, (EventPipeSessionInfo *)session_info) ? TRUE : FALSE;
}

intptr_t
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetWaitHandle (uint64_t session_id)
{
	return (intptr_t) mono_component_event_pipe()->get_wait_handle ((EventPipeSessionID)session_id);
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
	mono_component_event_pipe()->write_event_2 (ep_event, (EventData *)event_data, event_data_len, activity_id, related_activity_id);
}

// NOTE, keep in sync with EventPipe.Mono.cs, RuntimeCounters.
typedef enum {
	EP_RT_COUNTERS_ASSEMBLY_COUNT,
	EP_RT_COUNTERS_EXCEPTION_COUNT,
	EP_RT_COUNTERS_GC_NURSERY_SIZE_BYTES,
	EP_RT_COUNTERS_GC_MAJOR_SIZE_BYTES,
	EP_RT_COUNTERS_GC_LARGE_OBJECT_SIZE_BYTES,
	EP_RT_COUNTERS_GC_LAST_PERCENT_TIME_IN_GC,
	EP_RT_COUNTERS_JIT_IL_BYTES_JITTED,
	EP_RT_COUNTERS_JIT_METOHODS_JITTED
} EventPipeRuntimeCounters;

static
inline
int
gc_last_percent_time_in_gc (void)
{
	guint64 time_last_gc_100ns = 0;
	guint64 time_since_last_gc_100ns = 0;
	guint64 time_max_gc_100ns = 0;
	mono_gc_get_gctimeinfo (&time_last_gc_100ns, &time_since_last_gc_100ns, &time_max_gc_100ns);

	// Calculate percent of time spend in this GC since end of last GC.
	int percent_time_in_gc_since_last_gc = 0;
	if (time_since_last_gc_100ns != 0)
		percent_time_in_gc_since_last_gc = (int)(time_last_gc_100ns * 100 / time_since_last_gc_100ns);
	return percent_time_in_gc_since_last_gc;
}

static
inline
gint64
get_il_bytes_jitted (void)
{
	gint64 methods_compiled = 0;
	gint64 cil_code_size_bytes = 0;
	gint64 native_code_size_bytes = 0;

	if (mono_get_runtime_callbacks ()->get_jit_stats)
		mono_get_runtime_callbacks ()->get_jit_stats (&methods_compiled, &cil_code_size_bytes, &native_code_size_bytes);
	return cil_code_size_bytes;
}

static
inline
gint32
get_methods_jitted (void)
{
	gint64 methods_compiled = 0;
	gint64 cil_code_size_bytes = 0;
	gint64 native_code_size_bytes = 0;

	if (mono_get_runtime_callbacks ()->get_jit_stats)
		mono_get_runtime_callbacks ()->get_jit_stats (&methods_compiled, &cil_code_size_bytes, &native_code_size_bytes);
	return (gint32)methods_compiled;
}

static
inline
guint32
get_exception_count (void)
{
	guint32 excepion_count = 0;
	if (mono_get_runtime_callbacks ()->get_exception_stats)
		mono_get_runtime_callbacks ()->get_exception_stats (&excepion_count);
	return excepion_count;
}

guint64 ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetRuntimeCounterValue (gint32 id)
{
	EventPipeRuntimeCounters counterID = (EventPipeRuntimeCounters)id;
	switch (counterID) {
	case EP_RT_COUNTERS_ASSEMBLY_COUNT :
		return (guint64)mono_assembly_get_count ();
	case EP_RT_COUNTERS_EXCEPTION_COUNT :
		return (guint64)get_exception_count ();
	case EP_RT_COUNTERS_GC_NURSERY_SIZE_BYTES :
		return (guint64)mono_gc_get_generation_size (0);
	case EP_RT_COUNTERS_GC_MAJOR_SIZE_BYTES :
		return (guint64)mono_gc_get_generation_size (1);
	case EP_RT_COUNTERS_GC_LARGE_OBJECT_SIZE_BYTES :
		return (guint64)mono_gc_get_generation_size (3);
	case EP_RT_COUNTERS_GC_LAST_PERCENT_TIME_IN_GC :
		return (guint64)gc_last_percent_time_in_gc ();
	case EP_RT_COUNTERS_JIT_IL_BYTES_JITTED :
		return (guint64)get_il_bytes_jitted ();
	case EP_RT_COUNTERS_JIT_METOHODS_JITTED :
		return (guint64)get_methods_jitted ();
	default:
		return 0;
	}
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

guint64
ves_icall_System_Diagnostics_Tracing_EventPipeInternal_GetRuntimeCounterValue (gint32 id)
{
	ERROR_DECL (error);
	mono_error_set_not_implemented (error, "System.Diagnostics.Tracing.EventPipeInternal.GetRuntimeCounterValue");
	mono_error_set_pending_exception (error);
	return 0;
}

#endif /* ENABLE_PERFTRACING */
