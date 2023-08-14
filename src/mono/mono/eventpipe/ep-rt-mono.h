// Implementation of ep-rt.h targeting Mono runtime.
#ifndef __EVENTPIPE_RT_MONO_H__
#define __EVENTPIPE_RT_MONO_H__

#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-rt-config.h>
#include <eventpipe/ep-thread.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-provider.h>
#include <eventpipe/ep-session-provider.h>

#include <glib.h>
#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-coop-mutex.h>
#include <mono/utils/mono-proclib.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-rand.h>
#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/w32api.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/w32event.h>
#include <mono/metadata/metadata-internals.h>
#include "mono/utils/mono-logger-internals.h"
#include <runtime_version.h>
#include <mono/metadata/profiler.h>

#undef EP_INFINITE_WAIT
#define EP_INFINITE_WAIT MONO_INFINITE_WAIT

#undef EP_GCX_PREEMP_ENTER
#define EP_GCX_PREEMP_ENTER {

#undef EP_GCX_PREEMP_EXIT
#define EP_GCX_PREEMP_EXIT }

#undef EP_ALWAYS_INLINE
#define EP_ALWAYS_INLINE MONO_ALWAYS_INLINE

#undef EP_NEVER_INLINE
#define EP_NEVER_INLINE MONO_NEVER_INLINE

#undef EP_ALIGN_UP
#define EP_ALIGN_UP(val,align) ALIGN_TO(val,align)

extern char *_ep_rt_mono_os_cmd_line;
extern mono_lazy_init_t _ep_rt_mono_os_cmd_line_init;
extern char *_ep_rt_mono_managed_cmd_line;
extern mono_lazy_init_t _ep_rt_mono_managed_cmd_line_init;
extern ep_rt_spin_lock_handle_t _ep_rt_mono_config_lock;
extern char * ep_rt_mono_get_managed_cmd_line (void);
extern char * ep_rt_mono_get_os_cmd_line (void);
extern ep_rt_file_handle_t ep_rt_mono_file_open_write (const ep_char8_t *path);
extern bool ep_rt_mono_file_close (ep_rt_file_handle_t handle);
extern bool ep_rt_mono_file_write (ep_rt_file_handle_t handle, const uint8_t *buffer, uint32_t numbytes, uint32_t *byteswritten);
extern void * ep_rt_mono_thread_attach (bool background_thread);
extern void * ep_rt_mono_thread_attach_2 (bool background_thread, EventPipeThreadType thread_type);
extern void ep_rt_mono_thread_detach (void);
extern void ep_rt_mono_component_init (void);
extern void ep_rt_mono_init (void);
extern void ep_rt_mono_init_finish (void);
extern void ep_rt_mono_fini (void);
extern bool ep_rt_mono_walk_managed_stack_for_thread (ep_rt_thread_handle_t thread, EventPipeStackContents *stack_contents);
extern bool ep_rt_mono_method_get_simple_assembly_name (ep_rt_method_desc_t *method, ep_char8_t *name, size_t name_len);
extern bool ep_rt_mono_method_get_full_name (ep_rt_method_desc_t *method, ep_char8_t *name, size_t name_len);
extern void ep_rt_mono_provider_config_init (EventPipeProviderConfiguration *provider_config);
extern void ep_rt_mono_init_providers_and_events (void);
extern bool ep_rt_mono_providers_validate_all_disabled (void);
extern bool ep_rt_mono_sample_profiler_write_sampling_event_for_threads (ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event);
extern bool ep_rt_mono_rand_try_get_bytes (uint8_t *buffer,size_t buffer_size);
extern void ep_rt_mono_execute_rundown (dn_vector_ptr_t *execution_checkpoints);
extern int64_t ep_rt_mono_perf_counter_query (void);
extern int64_t ep_rt_mono_perf_frequency_query (void);
extern void ep_rt_mono_system_time_get (EventPipeSystemTime *system_time);
extern int64_t ep_rt_mono_system_timestamp_get (void);
extern void ep_rt_mono_os_environment_get_utf16 (dn_vector_ptr_t *os_env);
extern MonoNativeTlsKey _ep_rt_mono_thread_holder_tls_id;
extern EventPipeThread * ep_rt_mono_thread_get_or_create (void);

static
inline
char *
os_command_line_get (void)
{
	return ep_rt_mono_get_os_cmd_line ();
}

static
inline
char **
os_command_line_get_ref (void)
{
	return &_ep_rt_mono_os_cmd_line;
}

static
inline
mono_lazy_init_t *
os_command_line_get_init (void)
{
	return &_ep_rt_mono_os_cmd_line_init;
}

static
inline
void
os_command_line_lazy_init (void)
{
	if (!*os_command_line_get_ref ())
		*os_command_line_get_ref () = os_command_line_get ();
}

static
inline
void
os_command_line_lazy_clean (void)
{
	g_free (*os_command_line_get_ref ());
	*os_command_line_get_ref () = NULL;
}

static
inline
char *
managed_command_line_get (void)
{
	return ep_rt_mono_get_managed_cmd_line ();
}

static
inline
char **
managed_command_line_get_ref (void)
{
	return &_ep_rt_mono_managed_cmd_line;
}

static
inline
mono_lazy_init_t *
managed_command_line_get_init (void)
{
	return &_ep_rt_mono_managed_cmd_line_init;
}

static
inline
void
managed_command_line_lazy_init (void)
{
	if (!*managed_command_line_get_ref ())
		*managed_command_line_get_ref () = managed_command_line_get ();
}

static
inline
void
managed_command_line_lazy_clean (void)
{
	g_free (*managed_command_line_get_ref ());
	*managed_command_line_get_ref () = NULL;
}

static
inline
ep_rt_spin_lock_handle_t *
ep_rt_mono_config_lock_get (void)
{
	return &_ep_rt_mono_config_lock;
}

/*
* Helpers
*/

static
inline
EventPipeThreadHolder *
thread_holder_alloc_func (void)
{
	EventPipeThreadHolder *instance = ep_thread_holder_alloc (ep_thread_alloc());
	if (instance)
		ep_thread_register (ep_thread_holder_get_thread (instance));
	return instance;
}

static
inline
void
thread_holder_free_func (EventPipeThreadHolder * thread_holder)
{
	if (thread_holder) {
		ep_thread_unregister (ep_thread_holder_get_thread (thread_holder));
		ep_thread_holder_free (thread_holder);
	}
}

static
inline
MonoNativeThreadId
ep_rt_mono_native_thread_id_get (void)
{
	return mono_native_thread_id_get ();
}

static
inline
gboolean
ep_rt_mono_native_thread_id_equals (MonoNativeThreadId id1, MonoNativeThreadId id2)
{
	return mono_native_thread_id_equals (id1, id2);
}

static
inline
void
ep_rt_mono_thread_setup (bool background_thread)
{
	ep_rt_mono_thread_attach (background_thread);
}

static
inline
void
ep_rt_mono_thread_setup_2 (bool background_thread, EventPipeThreadType thread_type)
{
	ep_rt_mono_thread_attach_2 (background_thread, thread_type);
}

static
inline
void
ep_rt_mono_thread_teardown (void)
{
	ep_rt_mono_thread_detach ();
}

/*
 * Little-Endian Conversion.
 */

static
EP_ALWAYS_INLINE
uint16_t
ep_rt_val_uint16_t (uint16_t value)
{
	return GUINT16_TO_LE (value);
}

static
EP_ALWAYS_INLINE
uint32_t
ep_rt_val_uint32_t (uint32_t value)
{
	return GUINT32_TO_LE (value);
}

static
EP_ALWAYS_INLINE
uint64_t
ep_rt_val_uint64_t (uint64_t value)
{
	return GUINT64_TO_LE (value);
}

static
EP_ALWAYS_INLINE
int16_t
ep_rt_val_int16_t (int16_t value)
{
	return (int16_t)GUINT16_TO_LE ((uint16_t)value);
}

static
EP_ALWAYS_INLINE
int32_t
ep_rt_val_int32_t (int32_t value)
{
	return (int32_t)GUINT32_TO_LE ((uint32_t)value);
}

static
EP_ALWAYS_INLINE
int64_t
ep_rt_val_int64_t (int64_t value)
{
	return (int64_t)GUINT64_TO_LE ((uint64_t)value);
}

static
EP_ALWAYS_INLINE
uintptr_t
ep_rt_val_uintptr_t (uintptr_t value)
{
#if SIZEOF_VOID_P == 4
	return (uintptr_t)GUINT32_TO_LE ((uint32_t)value);
#else
	return (uintptr_t)GUINT64_TO_LE ((uint64_t)value);
#endif
}

/*
* Atomics.
*/

static
inline
uint32_t
ep_rt_atomic_inc_uint32_t (volatile uint32_t *value)
{
	return (uint32_t)mono_atomic_inc_i32 ((volatile gint32 *)value);
}

static
inline
uint32_t
ep_rt_atomic_dec_uint32_t (volatile uint32_t *value)
{
	return (uint32_t)mono_atomic_dec_i32 ((volatile gint32 *)value);
}

static
inline
int32_t
ep_rt_atomic_inc_int32_t (volatile int32_t *value)
{
	return (int32_t)mono_atomic_inc_i32 ((volatile gint32 *)value);
}

static
inline
int32_t
ep_rt_atomic_dec_int32_t (volatile int32_t *value)
{
	return (int32_t)mono_atomic_dec_i32 ((volatile gint32 *)value);
}

static
inline
int64_t
ep_rt_atomic_inc_int64_t (volatile int64_t *value)
{
	return (int64_t)mono_atomic_inc_i64 ((volatile gint64 *)value);
}

static
inline
int64_t
ep_rt_atomic_dec_int64_t (volatile int64_t *value)
{
	return (int64_t)mono_atomic_dec_i64 ((volatile gint64 *)value);
}

static
inline
size_t
ep_rt_atomic_compare_exchange_size_t (volatile size_t *target, size_t expected, size_t value)
{
#if SIZEOF_SIZE_T == 8
	return (size_t)(mono_atomic_cas_i64((volatile gint64*)(target), (gint64)(value), (gint64)(expected)));
#else
	return (size_t)(mono_atomic_cas_i32 ((volatile gint32 *)(target), (gint32)(value), (gint32)(expected)));
#endif
}

static
inline
ep_char8_t *
ep_rt_atomic_compare_exchange_utf8_string (ep_char8_t *volatile *target, ep_char8_t *expected, ep_char8_t *value)
{
	return (ep_char8_t *)mono_atomic_cas_ptr ((volatile gpointer *)target, (gpointer)value, (gpointer)expected);
}

/*
 * EventPipe.
 */

static
inline
void
ep_rt_init (void)
{
	ep_rt_mono_init ();
}

static
inline
void
ep_rt_init_finish (void)
{
	ep_rt_mono_init_finish ();
}

static
inline
void
ep_rt_shutdown (void)
{
	mono_lazy_cleanup (managed_command_line_get_init (), managed_command_line_lazy_clean);
	mono_lazy_cleanup (os_command_line_get_init (), os_command_line_lazy_clean);

	ep_rt_mono_fini ();
}

static
inline
bool
ep_rt_config_acquire (void)
{
	return ep_rt_spin_lock_acquire (ep_rt_mono_config_lock_get ());
}

static
inline
bool
ep_rt_config_release (void)
{
	return ep_rt_spin_lock_release (ep_rt_mono_config_lock_get ());
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_config_requires_lock_held (void)
{
	ep_rt_spin_lock_requires_lock_held (ep_rt_mono_config_lock_get ());
}

static
inline
void
ep_rt_config_requires_lock_not_held (void)
{
	ep_rt_spin_lock_requires_lock_not_held (ep_rt_mono_config_lock_get ());
}
#endif

static
inline
bool
ep_rt_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents)
{
	return ep_rt_mono_walk_managed_stack_for_thread (thread, stack_contents);
}

static
inline
bool
ep_rt_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	return ep_rt_mono_method_get_simple_assembly_name (method, name, name_len);
}

static
inline
bool
ep_rt_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	return ep_rt_mono_method_get_full_name (method, name, name_len);
}

static
inline
void
ep_rt_provider_config_init (EventPipeProviderConfiguration *provider_config)
{
	ep_rt_mono_provider_config_init (provider_config);
}

static
inline
void
ep_rt_init_providers_and_events (void)
{
	ep_rt_mono_init_providers_and_events ();
}

static
inline
bool
ep_rt_providers_validate_all_disabled (void)
{
	return ep_rt_mono_providers_validate_all_disabled ();
}

static
inline
void
ep_rt_prepare_provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data)
{
	;
}

static
inline
void
ep_rt_provider_invoke_callback (
	EventPipeCallback callback_func,
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	EP_ASSERT (callback_func != NULL);
	(*callback_func)(
		source_id,
		is_enabled,
		level,
		match_any_keywords,
		match_all_keywords,
		filter_data,
		callback_data);
}

/*
 * EventPipeProviderConfiguration.
 */

static
inline
bool
ep_rt_config_value_get_enable (void)
{
	bool enable = false;
	gchar *value = g_getenv ("DOTNET_EnableEventPipe");
	if (!value)
		value = g_getenv ("COMPlus_EnableEventPipe");
	if (value && atoi (value) == 1)
		enable = true;
	g_free (value);
	return enable;
}

static
inline
ep_char8_t *
ep_rt_config_value_get_config (void)
{
	gchar *value = g_getenv ("DOTNET_EventPipeConfig");
	if (!value)
		value = g_getenv ("COMPlus_EventPipeConfig");
	return (ep_char8_t *)value;
}

static
inline
ep_char8_t *
ep_rt_config_value_get_output_path (void)
{
	gchar *value = g_getenv ("DOTNET_EventPipeOutputPath");
	if (!value)
		value = g_getenv ("COMPlus_EventPipeOutputPath");
	return (ep_char8_t *)value;
}

static
inline
uint32_t
ep_rt_config_value_get_circular_mb (void)
{
	uint32_t circular_mb = 0;
	gchar *value = g_getenv ("DOTNET_EventPipeCircularMB");
	if (!value)
		value = g_getenv ("COMPlus_EventPipeCircularMB");
	if (value)
		circular_mb = strtoul (value, NULL, 10);
	g_free (value);
	return circular_mb;
}

static
inline
bool
ep_rt_config_value_get_output_streaming (void)
{
	bool enable = false;
	gchar *value = g_getenv ("DOTNET_EventPipeOutputStreaming");
	if (!value)
		value = g_getenv ("COMPlus_EventPipeOutputStreaming");
	if (value && atoi (value) == 1)
		enable = true;
	g_free (value);
	return enable;
}

static
inline
uint32_t
ep_rt_config_value_get_rundown (void)
{
	uint32_t value_uint32_t = 1;
	gchar *value = g_getenv ("DOTNET_EventPipeRundown");
	if (!value)
		value = g_getenv ("COMPlus_EventPipeRundown");
	if (value)
		value_uint32_t = (uint32_t)atoi (value);
	g_free (value);
	return value_uint32_t;
}

static
inline
bool
ep_rt_config_value_get_enable_stackwalk (void)
{
	uint32_t value_uint32_t = 1;
	gchar *value = g_getenv ("DOTNET_EventPipeEnableStackwalk");
	if (!value)
		value = g_getenv ("COMPlus_EventPipeEnableStackwalk");
	if (value)
		value_uint32_t = (uint32_t)atoi (value);
	g_free (value);
	return value_uint32_t != 0;
}

/*
 * EventPipeSampleProfiler.
 */

static
void
ep_rt_sample_profiler_write_sampling_event_for_threads (ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event)
{
	ep_rt_mono_sample_profiler_write_sampling_event_for_threads (sampling_thread, sampling_event);
}

static
void
ep_rt_notify_profiler_provider_created (EventPipeProvider *provider)
{
	;
}

/*
 * Arrays.
 */

static
inline
uint8_t *
ep_rt_byte_array_alloc (size_t len)
{
	return g_new(uint8_t, len);
}

static
inline
void
ep_rt_byte_array_free (uint8_t *ptr)
{
	g_free (ptr);
}

/*
 * Event.
 */

static
inline
void
ep_rt_wait_event_alloc (
	ep_rt_wait_event_handle_t *wait_event,
	bool manual,
	bool initial)
{
	//TODO, replace with low level PAL implementation.
	EP_ASSERT (wait_event != NULL);
	wait_event->event = mono_w32event_create (manual, initial);
}

static
inline
void
ep_rt_wait_event_free (ep_rt_wait_event_handle_t *wait_event)
{
	//TODO, replace with low level PAL implementation.
	if (wait_event != NULL && wait_event->event != NULL) {
		mono_w32event_close (wait_event->event);
		wait_event->event = NULL;
	}
}

static
inline
bool
ep_rt_wait_event_set (ep_rt_wait_event_handle_t *wait_event)
{
	//TODO, replace with low level PAL implementation.
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);
	mono_w32event_set (wait_event->event);
	return true;
}

static
inline
int32_t
ep_rt_wait_event_wait (
	ep_rt_wait_event_handle_t *wait_event,
	uint32_t timeout,
	bool alertable)
{
	//TODO, replace with low level PAL implementation.
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);
	return (int32_t)mono_w32handle_wait_one (wait_event->event, timeout, alertable);
}

static
inline
EventPipeWaitHandle
ep_rt_wait_event_get_wait_handle (ep_rt_wait_event_handle_t *wait_event)
{
	EP_ASSERT (wait_event != NULL);
	return (EventPipeWaitHandle)wait_event->event;
}

static
inline
bool
ep_rt_wait_event_is_valid (ep_rt_wait_event_handle_t *wait_event)
{
	if (wait_event == NULL || wait_event->event == NULL || wait_event->event == INVALID_HANDLE_VALUE)
		return false;
	else
		return true;
}

/*
 * Misc.
 */

static
inline
int
ep_rt_get_last_error (void)
{
#ifdef HOST_WIN32
	return GetLastError ();
#else
	return errno;
#endif
}

static
inline
bool
ep_rt_process_detach (void)
{
	// This is set to early in Mono compared to coreclr and only represent runtime
	// shutting down. EventPipe won't be shutdown to late on Mono, so always return FALSE.
	return FALSE;
}

static
inline
bool
ep_rt_process_shutdown (void)
{
	return ep_rt_process_detach ();
}

static
inline
void
ep_rt_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	ep_rt_mono_rand_try_get_bytes ((guchar *)activity_id, EP_ACTIVITY_ID_SIZE);

	const uint16_t version_mask = 0xF000;
	const uint16_t random_guid_version = 0x4000;
	const uint8_t clock_seq_hi_and_reserved_mask = 0xC0;
	const uint8_t clock_seq_hi_and_reserved_value = 0x80;

	// Modify bits indicating the type of the GUID
	uint8_t *activity_id_c = activity_id + sizeof (uint32_t) + sizeof (uint16_t);
	uint8_t *activity_id_d = activity_id + sizeof (uint32_t) + sizeof (uint16_t) + sizeof (uint16_t);

	uint16_t c;
	memcpy (&c, activity_id_c, sizeof (c));

	uint8_t d;
	memcpy (&d, activity_id_d, sizeof (d));

	// time_hi_and_version
	c = ((c & ~version_mask) | random_guid_version);
	// clock_seq_hi_and_reserved
	d = ((d & ~clock_seq_hi_and_reserved_mask) | clock_seq_hi_and_reserved_value);

	memcpy (activity_id_c, &c, sizeof (c));
	memcpy (activity_id_d, &d, sizeof (d));
}

static
inline
bool
ep_rt_is_running (void)
{
	return !ep_rt_process_detach ();
}

static
inline
void
ep_rt_execute_rundown (dn_vector_ptr_t *execution_checkpoints)
{
	if (ep_rt_config_value_get_rundown () > 0) {
		// Ask the runtime to emit rundown events.
		if (/*is_running &&*/ !ep_rt_process_shutdown ()) {
			ep_rt_mono_execute_rundown (execution_checkpoints);
		}
	}
}

/*
 * Objects.
 */

#undef ep_rt_object_alloc
#define ep_rt_object_alloc(obj_type) (g_new0 (obj_type, 1))

#undef ep_rt_object_array_alloc
#define ep_rt_object_array_alloc(obj_type,size) (g_new0 (obj_type, size))

static
inline
void
ep_rt_object_array_free (void *ptr)
{
	g_free (ptr);
}

static
inline
void
ep_rt_object_free (void *ptr)
{
	g_free (ptr);
}

/*
 * PAL.
 */

typedef struct _rt_mono_thread_params_internal_t {
	ep_rt_thread_params_t thread_params;
	bool background_thread;
} rt_mono_thread_params_internal_t;

#undef EP_RT_DEFINE_THREAD_FUNC
#define EP_RT_DEFINE_THREAD_FUNC(name) static mono_thread_start_return_t WINAPI name (gpointer data)

EP_RT_DEFINE_THREAD_FUNC (ep_rt_thread_mono_start_func)
{
	rt_mono_thread_params_internal_t *thread_params = (rt_mono_thread_params_internal_t *)data;

	ep_rt_mono_thread_setup_2 (thread_params->background_thread, thread_params->thread_params.thread_type);

	thread_params->thread_params.thread = ep_rt_thread_get_handle ();
	mono_thread_start_return_t result = thread_params->thread_params.thread_func (thread_params);

	ep_rt_mono_thread_teardown ();

	g_free (thread_params);

	return result;
}

static
inline
bool
ep_rt_thread_create (
	void *thread_func,
	void *params,
	EventPipeThreadType thread_type,
	void *id)
{
	rt_mono_thread_params_internal_t *thread_params = g_new0 (rt_mono_thread_params_internal_t, 1);
	if (thread_params) {
		thread_params->thread_params.thread_type = thread_type;
		thread_params->thread_params.thread_func = (ep_rt_thread_start_func)thread_func;
		thread_params->thread_params.thread_params = params;
		thread_params->background_thread = true;
		return (mono_thread_platform_create_thread (ep_rt_thread_mono_start_func, thread_params, NULL, (ep_rt_thread_id_t *)id) == TRUE) ? true : false;
	}

	return false;
}

static
inline
void
ep_rt_set_server_name(void)
{
	mono_native_thread_set_name(mono_native_thread_id_get(), ".NET EventPipe");
}

static
inline
void
ep_rt_thread_sleep (uint64_t ns)
{
	MONO_REQ_GC_UNSAFE_MODE;
	if (ns == 0) {
		mono_thread_info_yield ();
	} else {
		MONO_ENTER_GC_SAFE;
		g_usleep ((gulong)(ns / 1000));
		MONO_EXIT_GC_SAFE;
	}
}

static
inline
uint32_t
ep_rt_current_process_get_id (void)
{
	return (uint32_t)mono_process_current_pid ();
}

static
inline
uint32_t
ep_rt_current_processor_get_number (void)
{
	return 0xFFFFFFFF;
}

static
inline
uint32_t
ep_rt_processors_get_count (void)
{
	return (uint32_t)mono_cpu_count ();
}

static
inline
ep_rt_thread_id_t
ep_rt_current_thread_get_id (void)
{
	return mono_native_thread_id_get ();
}

static
inline
int64_t
ep_rt_perf_counter_query (void)
{
	return ep_rt_mono_perf_counter_query ();
}

static
inline
int64_t
ep_rt_perf_frequency_query (void)
{
	return ep_rt_mono_perf_frequency_query ();
}

static
inline
void
ep_rt_system_time_get (EventPipeSystemTime *system_time)
{
	ep_rt_mono_system_time_get (system_time);
}

static
inline
int64_t
ep_rt_system_timestamp_get (void)
{
	return ep_rt_mono_system_timestamp_get ();
}

static
inline
int32_t
ep_rt_system_get_alloc_granularity (void)
{
	return (int32_t)mono_valloc_granule ();
}

static
inline
const ep_char8_t *
ep_rt_os_command_line_get (void)
{
	if (!mono_lazy_is_initialized (os_command_line_get_init ())) {
		char *cmd_line = os_command_line_get ();
		if (!cmd_line)
			return NULL;
		g_free (cmd_line);
	}

	mono_lazy_initialize (os_command_line_get_init (), os_command_line_lazy_init);
	EP_ASSERT (*os_command_line_get_ref () != NULL);
	return *os_command_line_get_ref ();
}

static
inline
ep_rt_file_handle_t
ep_rt_file_open_write (const ep_char8_t *path)
{
	ep_rt_file_handle_t res = ep_rt_mono_file_open_write (path);

	return (res != INVALID_HANDLE_VALUE) ? res : NULL;
}

static
inline
bool
ep_rt_file_close (ep_rt_file_handle_t file_handle)
{
	ep_return_false_if_nok (file_handle != NULL);
	return ep_rt_mono_file_close (file_handle);
}

static
inline
bool
ep_rt_file_write (
	ep_rt_file_handle_t file_handle,
	const uint8_t *buffer,
	uint32_t bytes_to_write,
	uint32_t *bytes_written)
{
	ep_return_false_if_nok (file_handle != NULL);
	EP_ASSERT (buffer != NULL);

	bool result = ep_rt_mono_file_write (file_handle, buffer, bytes_to_write, bytes_written);
	if (result)
		*bytes_written = bytes_to_write;

	return result;
}

static
inline
uint8_t *
ep_rt_valloc0 (size_t buffer_size)
{
	uint8_t *buffer = (uint8_t *)mono_valloc (NULL, buffer_size, MONO_MMAP_READ | MONO_MMAP_WRITE, MONO_MEM_ACCOUNT_PROFILER);
#ifdef EP_CHECKED_BUILD
	for (size_t i = 0; i < buffer_size; i++)
		EP_ASSERT (buffer [i] == 0);
#endif
	return buffer;
}

static
inline
void
ep_rt_vfree (
	uint8_t *buffer,
	size_t buffer_size)
{
	if (buffer)
		mono_vfree (buffer, buffer_size, MONO_MEM_ACCOUNT_PROFILER);
}

static
inline
uint32_t
ep_rt_temp_path_get (
	ep_char8_t *buffer,
	uint32_t buffer_len)
{
	EP_ASSERT (buffer != NULL);
	EP_ASSERT (buffer_len > 0);

	const ep_char8_t *path = g_get_tmp_dir ();
	int32_t result = snprintf (buffer, buffer_len, "%s", path);
	if (result <= 0 || GINT32_TO_UINT32(result) >= buffer_len)
		ep_raise_error ();

	if (buffer [result - 1] != G_DIR_SEPARATOR) {
		if (GINT32_TO_UINT32(result) >= buffer_len - 1)
			ep_raise_error ();
		buffer [result++] = G_DIR_SEPARATOR;
		buffer [result] = '\0';
	}

ep_on_exit:
	return result;

ep_on_error:
	result = 0;
	ep_exit_error_handler ();
}

static
inline
void
ep_rt_os_environment_get_utf16 (dn_vector_ptr_t *os_env)
{
	ep_rt_mono_os_environment_get_utf16 (os_env);
}

/*
* Lock.
*/

static
bool
ep_rt_lock_acquire (ep_rt_lock_handle_t *lock)
{
	EP_UNREACHABLE ("Not implemented on Mono.");
}

static
bool
ep_rt_lock_release (ep_rt_lock_handle_t *lock)
{
	EP_UNREACHABLE ("Not implemented on Mono.");
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_lock_requires_lock_held (const ep_rt_lock_handle_t *lock)
{
	EP_UNREACHABLE ("Not implemented on Mono.");
}

static
inline
void
ep_rt_lock_requires_lock_not_held (const ep_rt_lock_handle_t *lock)
{
	EP_UNREACHABLE ("Not implemented on Mono.");
}
#endif

/*
* SpinLock.
*/

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_spin_lock_set_owning_thread_id (
	ep_rt_spin_lock_handle_t *spin_lock,
	MonoNativeThreadId thread_id)
{
MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
	if (sizeof (spin_lock->owning_thread_id) == sizeof (uint32_t))
		ep_rt_volatile_store_uint32_t ((uint32_t *)&spin_lock->owning_thread_id, MONO_NATIVE_THREAD_ID_TO_UINT (thread_id));
	else if (sizeof (spin_lock->owning_thread_id) == sizeof (uint64_t))
		ep_rt_volatile_store_uint64_t ((uint64_t *)&spin_lock->owning_thread_id, MONO_NATIVE_THREAD_ID_TO_UINT (thread_id));
	else
		spin_lock->owning_thread_id = thread_id;
MONO_RESTORE_WARNING
}

static
inline
MonoNativeThreadId
ep_rt_spin_lock_get_owning_thread_id (const ep_rt_spin_lock_handle_t *spin_lock)
{
MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
	if (sizeof (spin_lock->owning_thread_id) == sizeof (uint32_t))
		return MONO_UINT_TO_NATIVE_THREAD_ID (ep_rt_volatile_load_uint32_t ((const uint32_t *)&spin_lock->owning_thread_id));
	else if (sizeof (spin_lock->owning_thread_id) == sizeof (uint64_t))
		return MONO_UINT_TO_NATIVE_THREAD_ID (ep_rt_volatile_load_uint64_t ((const uint64_t *)&spin_lock->owning_thread_id));
	else
		return spin_lock->owning_thread_id;
MONO_RESTORE_WARNING
}
#endif

static
inline
void
ep_rt_spin_lock_alloc (ep_rt_spin_lock_handle_t *spin_lock)
{
#ifdef EP_CHECKED_BUILD
	ep_rt_spin_lock_set_owning_thread_id (spin_lock, MONO_UINT_TO_NATIVE_THREAD_ID (0));
#endif
	spin_lock->lock = g_new0 (MonoCoopMutex, 1);
	if (spin_lock->lock)
		mono_coop_mutex_init (spin_lock->lock);
}

static
inline
void
ep_rt_spin_lock_free (ep_rt_spin_lock_handle_t *spin_lock)
{
	if (spin_lock && spin_lock->lock) {
		mono_coop_mutex_destroy (spin_lock->lock);
		g_free (spin_lock->lock);
		spin_lock->lock = NULL;
	}
}

static
inline
bool
ep_rt_spin_lock_acquire (ep_rt_spin_lock_handle_t *spin_lock)
{
	if (spin_lock && spin_lock->lock) {
		mono_coop_mutex_lock (spin_lock->lock);
#ifdef EP_CHECKED_BUILD
		ep_rt_spin_lock_set_owning_thread_id (spin_lock, ep_rt_mono_native_thread_id_get ());
#endif
	}
	return true;
}

static
inline
bool
ep_rt_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock)
{
	if (spin_lock && spin_lock->lock) {
#ifdef EP_CHECKED_BUILD
		ep_rt_spin_lock_set_owning_thread_id (spin_lock, MONO_UINT_TO_NATIVE_THREAD_ID (0));
#endif
		mono_coop_mutex_unlock (spin_lock->lock);
	}
	return true;
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_spin_lock_requires_lock_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
	g_assert (ep_rt_mono_native_thread_id_equals (ep_rt_spin_lock_get_owning_thread_id (spin_lock), ep_rt_mono_native_thread_id_get ()));
}

static
inline
void
ep_rt_spin_lock_requires_lock_not_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
	g_assert (!ep_rt_mono_native_thread_id_equals (ep_rt_spin_lock_get_owning_thread_id (spin_lock), ep_rt_mono_native_thread_id_get ()));
}
#endif

static
bool
ep_rt_spin_lock_is_valid (const ep_rt_spin_lock_handle_t *spin_lock)
{
	return (spin_lock != NULL && spin_lock->lock != NULL);
}

/*
 * String.
 */

static
inline
int
ep_rt_utf8_string_compare (
	const ep_char8_t *str1,
	const ep_char8_t *str2)
{
	return strcmp ((const char *)str1, (const char *)str2);
}

static
inline
int
ep_rt_utf8_string_compare_ignore_case (
	const ep_char8_t *str1,
	const ep_char8_t *str2)
{
	return g_strcasecmp ((const char *)str1, (const char *)str2);
}

static
inline
bool
ep_rt_utf8_string_is_null_or_empty (const ep_char8_t *str)
{
	if (str == NULL)
		return true;

	while (*str) {
		if (!isspace(*str))
			return false;
		str++;
	}
	return true;
}

static
inline
ep_char8_t *
ep_rt_utf8_string_dup (const ep_char8_t *str)
{
	return g_strdup (str);
}

static
inline
ep_char8_t *
ep_rt_utf8_string_dup_range (const ep_char8_t *str, const ep_char8_t *strEnd)
{
	ptrdiff_t byte_len = strEnd - str;
	ep_char8_t *buffer = g_new(ep_char8_t, byte_len + 1);
	if (buffer != NULL)
	{
		memcpy (buffer, str, byte_len);
		buffer [byte_len] = '\0';
	}
	return buffer;
}


static
inline
ep_char8_t *
ep_rt_utf8_string_strtok (
	ep_char8_t *str,
	const ep_char8_t *delimiter,
	ep_char8_t **context)
{
	return strtok_r (str, delimiter, context);
}

#undef ep_rt_utf8_string_snprintf
#define ep_rt_utf8_string_snprintf( \
	str, \
	str_len, \
	format, ...) \
g_snprintf ((gchar *)str, (gulong)str_len, (const gchar *)format, __VA_ARGS__)

static
inline
bool
ep_rt_utf8_string_replace (
	ep_char8_t **str,
	const ep_char8_t *strSearch,
	const ep_char8_t *strReplacement
)
{
	if ((*str) == NULL)
		return false;

	ep_char8_t* strFound = strstr(*str, strSearch);
	if (strFound != NULL)
	{
		size_t strSearchLen = strlen(strSearch);
		size_t newStrSize = strlen(*str) + strlen(strReplacement) - strSearchLen + 1;
		ep_char8_t *newStr =  g_new(ep_char8_t, newStrSize);
		if (newStr == NULL)
		{
			*str = NULL;
			return false;
		}
		ep_rt_utf8_string_snprintf(newStr, newStrSize, "%.*s%s%s", (int)(strFound - (*str)), *str, strReplacement, strFound + strSearchLen);
		ep_rt_utf8_string_free(*str);
		*str = newStr;
		return true;
	}
	return false;
}

static
inline
ep_char16_t *
ep_rt_utf8_to_utf16le_string (
	const ep_char8_t *str,
	size_t len)
{
	return (ep_char16_t *)(g_utf8_to_utf16le ((const gchar *)str, (glong)len, NULL, NULL, NULL));
}

static
inline
ep_char16_t *
ep_rt_utf16_string_dup (const ep_char16_t *str)
{
	size_t str_size = (ep_rt_utf16_string_len (str) + 1) * sizeof (ep_char16_t);
	ep_char16_t *str_dup = (ep_char16_t *)malloc (str_size);
	if (str_dup)
		memcpy (str_dup, str, str_size);
	return str_dup;
}

static
inline
void
ep_rt_utf8_string_free (ep_char8_t *str)
{
	g_free (str);
}

static
inline
size_t
ep_rt_utf16_string_len (const ep_char16_t *str)
{
	return g_utf16_len ((const gunichar2 *)str);
}

static
inline
ep_char8_t *
ep_rt_utf16_to_utf8_string (
	const ep_char16_t *str,
	size_t len)
{
	return g_utf16_to_utf8 ((const gunichar2 *)str, (glong)len, NULL, NULL, NULL);
}

static
inline
ep_char8_t *
ep_rt_utf16le_to_utf8_string (
	const ep_char16_t *str,
	size_t len)
{
	return g_utf16le_to_utf8 ((const gunichar2 *)str, (glong)len, NULL, NULL, NULL);
}

static
inline
void
ep_rt_utf16_string_free (ep_char16_t *str)
{
	g_free (str);
}

static
inline
const ep_char8_t *
ep_rt_managed_command_line_get (void)
{
	if (!mono_lazy_is_initialized (managed_command_line_get_init ())) {
		char *cmd_line = managed_command_line_get ();
		if (!cmd_line)
			return NULL;
		g_free (cmd_line);
	}

	mono_lazy_initialize (managed_command_line_get_init (), managed_command_line_lazy_init);
	EP_ASSERT (*managed_command_line_get_ref () != NULL);
	return *managed_command_line_get_ref ();
}

static
const ep_char8_t *
ep_rt_diagnostics_command_line_get (void)
{
	const ep_char8_t * cmd_line = ep_rt_managed_command_line_get ();

	// if the managed command line isn't available yet (e.g. because we're during runtime startup) then fallback to the OS command line.
	// Checkout https://github.com/dotnet/coreclr/pull/24433 for more information about this fall back.
	if (cmd_line == NULL)
		cmd_line = ep_rt_os_command_line_get ();

	return cmd_line;
}

static
inline
const ep_char8_t *
ep_rt_entrypoint_assembly_name_get_utf8 (void)
{
	MonoAssembly *main_assembly = mono_assembly_get_main ();
	if (!main_assembly || !main_assembly->image)
		return "";

	const char *assembly_name = m_image_get_assembly_name (mono_assembly_get_main ()->image);
	if (!assembly_name)
		return "";

	return (const ep_char8_t*)assembly_name;
}

static
inline
const ep_char8_t *
ep_rt_runtime_version_get_utf8 (void)
{
	return (const ep_char8_t *)EGLIB_TOSTRING (RuntimeProductVersion);
}

/*
 * Thread.
 */
static
inline
void
ep_rt_thread_setup (void)
{
	ep_rt_mono_thread_setup (false);
}

static
inline
EventPipeThread *
ep_rt_thread_get (void)
{
	EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (_ep_rt_mono_thread_holder_tls_id);
	return thread_holder ? ep_thread_holder_get_thread (thread_holder) : NULL;
}

static
inline
EventPipeThread *
ep_rt_thread_get_or_create (void)
{
	EventPipeThread *thread = ep_rt_thread_get ();
	if (!thread) {
		thread = ep_rt_mono_thread_get_or_create ();
	}
	return thread;
}

static
inline
ep_rt_thread_handle_t
ep_rt_thread_get_handle (void)
{
	return mono_thread_info_current ();
}

static
inline
ep_rt_thread_id_t
ep_rt_thread_get_id (ep_rt_thread_handle_t thread_handle)
{
	return mono_thread_info_get_tid (thread_handle);
}

static
inline
uint64_t
ep_rt_thread_id_t_to_uint64_t (ep_rt_thread_id_t thread_id)
{
	return (uint64_t)MONO_NATIVE_THREAD_ID_TO_UINT (thread_id);
}

static
inline
ep_rt_thread_id_t
ep_rt_uint64_t_to_thread_id_t (uint64_t thread_id)
{
	return MONO_UINT_TO_NATIVE_THREAD_ID (thread_id);
}

static
inline
bool
ep_rt_thread_has_started (ep_rt_thread_handle_t thread_handle)
{
	return thread_handle == ep_rt_thread_get_handle ();
}

static
inline
ep_rt_thread_activity_id_handle_t
ep_rt_thread_get_activity_id_handle (void)
{
	return ep_rt_thread_get_or_create ();
}

static
inline
const uint8_t *
ep_rt_thread_get_activity_id_cref (ep_rt_thread_activity_id_handle_t activity_id_handle)
{
	EP_UNREACHABLE ("EP_THREAD_INCLUDE_ACTIVITY_ID should have been defined on Mono");
	return NULL;
}

static
inline
void
ep_rt_thread_get_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	EP_ASSERT (activity_id_handle != NULL);
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	memcpy (activity_id, ep_thread_get_activity_id_cref (activity_id_handle), EP_ACTIVITY_ID_SIZE);
}

static
inline
void
ep_rt_thread_set_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	const uint8_t *activity_id,
	uint32_t activity_id_len)
{
	EP_ASSERT (activity_id_handle != NULL);
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	memcpy (ep_thread_get_activity_id_ref (activity_id_handle), activity_id, EP_ACTIVITY_ID_SIZE);
}

static
inline
int32_t
ep_rt_mono_thread_sleep (uint32_t ms, bool alertable)
{
	gboolean alerted = false;
	if (alertable)
		return (int32_t)mono_thread_info_sleep (ms, &alerted);
	else
		return (int32_t)mono_thread_info_sleep (ms, NULL);
}

static
inline
bool
ep_rt_mono_thread_yield (void)
{
	return (mono_thread_info_yield () == TRUE) ? true : false;
}

// See src/coreclr/vm/spinlock.h for details.
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
	#define EP_SLEEP_START_THRESHOLD (5 * 1024)
#else
	#define EP_SLEEP_START_THRESHOLD (32 * 1024)
#endif

#undef EP_YIELD_WHILE
#define EP_YIELD_WHILE(condition) { \
	int32_t __switch_count = 0; \
	while (condition) { \
		if (++__switch_count >= EP_SLEEP_START_THRESHOLD) { \
			ep_rt_mono_thread_sleep (1, false); \
		} \
	} \
	{ \
		ep_rt_mono_thread_yield (); \
	} \
}

/*
 * Volatile.
 */

static
inline
uint32_t
ep_rt_volatile_load_uint32_t (const volatile uint32_t *ptr)
{
	return (uint32_t)mono_atomic_load_i32 ((volatile gint32 *)ptr);
}

static
inline
uint32_t
ep_rt_volatile_load_uint32_t_without_barrier (const volatile uint32_t *ptr)
{
	uint32_t value = *ptr;
	return value;
}

static
inline
void
ep_rt_volatile_store_uint32_t (
	volatile uint32_t *ptr,
	uint32_t value)
{
	mono_atomic_store_i32 ((volatile gint32 *)ptr, (gint32)value);
}

static
inline
void
ep_rt_volatile_store_uint32_t_without_barrier (
	volatile uint32_t *ptr,
	uint32_t value)
{
	*ptr = value;
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t (const volatile uint64_t *ptr)
{
	return (uint64_t)mono_atomic_load_i64 ((volatile gint64 *)ptr);
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t_without_barrier (const volatile uint64_t *ptr)
{
	uint64_t value = *ptr;
	return value;
}

static
inline
void
ep_rt_volatile_store_uint64_t (
	volatile uint64_t *ptr,
	uint64_t value)
{
	mono_atomic_store_i64 ((volatile gint64 *)ptr, (gint64)value);
}

static
inline
void
ep_rt_volatile_store_uint64_t_without_barrier (
	volatile uint64_t *ptr,
	uint64_t value)
{
	*ptr = value;
}

static
inline
int64_t
ep_rt_volatile_load_int64_t (const volatile int64_t *ptr)
{
	return mono_atomic_load_i64 ((volatile gint64 *)ptr);
}

static
inline
int64_t
ep_rt_volatile_load_int64_t_without_barrier (const volatile int64_t *ptr)
{
	return *ptr;
}

static
inline
void
ep_rt_volatile_store_int64_t (
	volatile int64_t *ptr,
	int64_t value)
{
	mono_atomic_store_i64 ((volatile gint64 *)ptr, (gint64)value);
}

static
inline
void
ep_rt_volatile_store_int64_t_without_barrier (
	volatile int64_t *ptr,
	int64_t value)
{
	*ptr = value;
}

static
inline
void *
ep_rt_volatile_load_ptr (volatile void **ptr)
{
	return mono_atomic_load_ptr ((volatile gpointer *)ptr);
}

static
inline
void *
ep_rt_volatile_load_ptr_without_barrier (volatile void **ptr)
{
	void *value = (void *)(*ptr);
	return value;
}

static
inline
void
ep_rt_volatile_store_ptr (
	volatile void **ptr,
	void *value)
{
	mono_atomic_store_ptr ((volatile gpointer *)ptr, (gpointer)value);
}

static
inline
void
ep_rt_volatile_store_ptr_without_barrier (
	volatile void **ptr,
	void *value)
{
	*ptr = value;
}

/*
 * EventPipe Native Events.
 */

bool
ep_rt_write_event_ee_startup_start (void);

bool
ep_rt_write_event_threadpool_worker_thread_start (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_worker_thread_stop (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_worker_thread_wait (
	uint32_t active_thread_count,
	uint32_t retired_worker_thread_count,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_min_max_threads (
	uint16_t min_worker_threads,
	uint16_t max_worker_threads,
	uint16_t min_io_completion_threads,
	uint16_t max_io_completion_threads,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_worker_thread_adjustment_sample (
	double throughput,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_worker_thread_adjustment_adjustment (
	double average_throughput,
	uint32_t networker_thread_count,
	/*NativeRuntimeEventSource.ThreadAdjustmentReasonMap*/ int32_t reason,
	uint16_t clr_instance_id);

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
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_io_enqueue (
	intptr_t native_overlapped,
	intptr_t overlapped,
	bool multi_dequeues,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_io_dequeue (
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_working_thread_count (
	uint16_t count,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_threadpool_io_pack (
	intptr_t native_overlapped,
	intptr_t overlapped,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_contention_lock_created (
	intptr_t lock_id,
	intptr_t associated_object_id,
	uint16_t clr_instance_id);

bool
ep_rt_write_event_contention_start (
	uint8_t contention_flags,
	uint16_t clr_instance_id,
	intptr_t lock_id,
	intptr_t associated_object_id,
	uint64_t lock_owner_thread_id);

bool
ep_rt_write_event_contention_stop (
	uint8_t contention_flags,
	uint16_t clr_instance_id,
	double duration_ns);

/*
* EventPipe provider callbacks.
*/

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntime (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data);

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntimeRundown (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data);

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntimePrivate (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data);

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntimeStress (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data);

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntimeMonoProfiler (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data);

/*
* Shared EventPipe provider defines/types/functions.
*/

#define GC_KEYWORD 0x1
#define GC_HANDLE_KEYWORD 0x2
#define LOADER_KEYWORD 0x8
#define JIT_KEYWORD 0x10
#define APP_DOMAIN_RESOURCE_MANAGEMENT_KEYWORD 0x800
#define CONTENTION_KEYWORD 0x4000
#define EXCEPTION_KEYWORD 0x8000
#define THREADING_KEYWORD 0x10000
#define TYPE_KEYWORD 0x80000
#define GC_HEAP_DUMP_KEYWORD 0x100000
#define GC_ALLOCATION_KEYWORD 0x200000
#define GC_MOVES_KEYWORD 0x400000
#define GC_HEAP_COLLECT_KEYWORD 0x800000
#define GC_HEAP_AND_TYPE_NAMES_KEYWORD 0x1000000
#define GC_FINALIZATION_KEYWORD 0x1000000
#define GC_RESIZE_KEYWORD 0x2000000
#define GC_ROOT_KEYWORD 0x4000000
#define GC_HEAP_DUMP_VTABLE_CLASS_REF_KEYWORD 0x8000000
#define METHOD_TRACING_KEYWORD 0x20000000
#define TYPE_DIAGNOSTIC_KEYWORD 0x8000000000
#define TYPE_LOADING_KEYWORD 0x8000000000
#define MONITOR_KEYWORD 0x10000000000
#define METHOD_INSTRUMENTATION_KEYWORD 0x40000000000

// Custom Mono EventPipe thread data.
typedef struct _EventPipeMonoThreadData EventPipeMonoThreadData;
struct _EventPipeMonoThreadData {
	void *gc_heap_dump_context;
	bool prevent_profiler_event_recursion;
};

static
inline
bool
ep_rt_mono_is_runtime_initialized (void)
{
	extern gboolean _ep_rt_mono_runtime_initialized;
	return !!_ep_rt_mono_runtime_initialized;
}

extern EventPipeMonoThreadData * ep_rt_mono_thread_data_get_or_create (void);
extern uint64_t ep_rt_mono_session_calculate_and_count_all_keywords (const ep_char8_t *provider, uint64_t keywords[], uint64_t count[], size_t len);
extern bool ep_rt_mono_sesion_has_all_started (void);

extern void ep_rt_mono_runtime_provider_component_init (void);
extern void ep_rt_mono_runtime_provider_init (void);
extern void ep_rt_mono_runtime_provider_fini (void);
extern void ep_rt_mono_runtime_provider_thread_started_callback (MonoProfiler *prof, uintptr_t tid);
extern void ep_rt_mono_runtime_provider_thread_stopped_callback (MonoProfiler *prof, uintptr_t tid);

extern void ep_rt_mono_profiler_provider_component_init (void);
extern void ep_rt_mono_profiler_provider_init (void);
extern void ep_rt_mono_profiler_provider_fini (void);
extern bool ep_rt_mono_profiler_provider_parse_options (const char *options);

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_MONO_H__ */
