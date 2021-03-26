#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-rt-config.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-rt.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-event.h>
#include <mono/utils/mono-lazy-init.h>
#include <eglib/gmodule.h>
#include <runtime_version.h>

ep_rt_spin_lock_handle_t _ep_rt_mono_config_lock = {0};
EventPipeMonoFuncTable _ep_rt_mono_func_table = {0};

mono_lazy_init_t _ep_rt_mono_os_cmd_line_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
char *_ep_rt_mono_os_cmd_line = NULL;

mono_lazy_init_t _ep_rt_mono_managed_cmd_line_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
char *_ep_rt_mono_managed_cmd_line = NULL;

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
		g_assert_not_reached ();

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

// Rundown flags.
#define RUNTIME_SKU_CORECLR 0x2

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

/*
 * Forward declares of all static rundown functions.
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

void
ep_rt_mono_init_providers_and_events (void)
{
	init_dotnet_runtime_rundown ();
}

void
ep_rt_mono_fini_providers_and_events (void)
{
	// dotnet/runtime: issue 12775: EventPipe shutdown race conditions
	// Deallocating providers/events here might cause AV if a WriteEvent
	// was to occur. Thus, we are not doing this cleanup.

	// ep_delete_provider (EventPipeProviderDotNETRuntimeRundown);
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

	EP_ASSERT (_ep_rt_mono_func_table.ep_rt_mono_execute_rundown != NULL);
	_ep_rt_mono_func_table.ep_rt_mono_execute_rundown (
		fire_domain_rundown_events_func,
		fire_assembly_rundown_events_func,
		fire_method_rundown_events_func);

	FireEtwDCEndComplete_V1 (clr_instance_get_id ());
}

#endif /* ENABLE_PERFTRACING */

MONO_EMPTY_SOURCE_FILE(eventpipe_rt_mono);
