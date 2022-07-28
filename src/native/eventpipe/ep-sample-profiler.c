#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES)

#define EP_IMPL_SAMPLE_PROFILER_GETTER_SETTER
#include "ep.h"
#include "ep-sample-profiler.h"
#include "ep-event.h"
#include "ep-provider-internals.h"
#include "ep-rt.h"

#define NUM_NANOSECONDS_IN_1_MS 1000000

static volatile uint32_t _profiling_enabled = (uint32_t)false;
static EventPipeProvider *_sampling_provider = NULL;
static EventPipeEvent *_thread_time_event = NULL;
static ep_rt_wait_event_handle_t _thread_shutdown_event;
static uint64_t _sampling_rate_in_ns = NUM_NANOSECONDS_IN_1_MS; // 1ms
static bool _time_period_is_set = false;
static volatile uint32_t _can_start_sampling = (uint32_t)false;
static int32_t _ref_count = 0;

#ifdef HOST_WIN32
#include <mmsystem.h>
static PVOID _time_begin_period_func = NULL;
static PVOID _time_end_period_func = NULL;
static HINSTANCE _multimedia_library_handle = NULL;

typedef MMRESULT(WINAPI *time_period_func)(UINT uPeriod);
#endif

/*
 * Forward declares of all static functions.
 */

EP_RT_DEFINE_THREAD_FUNC (sampling_thread);

static
void
sample_profiler_set_time_granularity (void);

static
void
sample_profiler_reset_time_granularity (void);

static
bool
sample_profiler_load_dependencies (void);

static
void
sample_profiler_unload_dependencies (void);

static
void
sample_profiler_enable (void);

/*
 * EventPipeSampleProfiler.
 */

static
inline
bool
sample_profiler_load_profiling_enabled (void)
{
	return (ep_rt_volatile_load_uint32_t (&_profiling_enabled) != 0) ? true : false;
}

static
inline
void
sample_profiler_store_profiling_enabled (bool enabled)
{
	ep_rt_volatile_store_uint32_t (&_profiling_enabled, enabled ? 1 : 0);
}

static
inline
void
sample_profiler_store_can_start_sampling (bool start_sampling)
{
	ep_rt_volatile_store_uint32_t (&_can_start_sampling, start_sampling ? 1 : 0);
}

EP_RT_DEFINE_THREAD_FUNC (sampling_thread)
{
	EP_ASSERT (data != NULL);
	if (data == NULL)
		return 1;

	ep_rt_thread_params_t *thread_params = (ep_rt_thread_params_t *)data;

	if (thread_params->thread && ep_rt_thread_has_started (thread_params->thread)) {
		EP_GCX_PREEMP_ENTER
			while (sample_profiler_load_profiling_enabled ()) {
				// Sample all threads.
				ep_rt_sample_profiler_write_sampling_event_for_threads (thread_params->thread, _thread_time_event);
				// Wait until it's time to sample again.
				ep_rt_thread_sleep (_sampling_rate_in_ns);
			}
		EP_GCX_PREEMP_EXIT
	}

	// Signal disable () that the thread has been destroyed.
	ep_rt_wait_event_set (&_thread_shutdown_event);

	return (ep_rt_thread_start_func_return_t)0;
}

static
void
sample_profiler_set_time_granularity (void)
{
#ifdef HOST_WIN32
	// Attempt to set the systems minimum timer period to the sampling rate
	// If the sampling rate is lower than the current system setting (16ms by default),
	// this will cause the OS to wake more often for scheduling descsion, allowing us to take samples
	// Note that is effects a system-wide setting and when set low will increase the amount of time
	// the OS is on-CPU, decreasing overall system performance and increasing power consumption
	if (_time_begin_period_func != NULL) {
		if (((time_period_func)_time_begin_period_func)((uint32_t)(_sampling_rate_in_ns / NUM_NANOSECONDS_IN_1_MS)) == TIMERR_NOERROR) {
			_time_period_is_set = true;
		}
	}
#endif //HOST_WIN32
}

static
void
sample_profiler_reset_time_granularity (void)
{
#ifdef HOST_WIN32
	// End the modifications we had to the timer period in enable.
	if (_time_end_period_func != NULL) {
		if (((time_period_func)_time_end_period_func)((uint32_t)(_sampling_rate_in_ns / NUM_NANOSECONDS_IN_1_MS)) == TIMERR_NOERROR) {
			_time_period_is_set = false;
		}
	}
#endif //HOST_WIN32
}

static
bool
sample_profiler_load_dependencies (void)
{
#ifdef HOST_WIN32
	if (_ref_count > 0)
		return true; // Already loaded.

#ifdef WszLoadLibrary
	_multimedia_library_handle = WszLoadLibrary (L"winmm.dll");
#else
	_multimedia_library_handle = LoadLibraryW (L"winmm.dll");
#endif

	if (_multimedia_library_handle != NULL) {
		_time_begin_period_func = (PVOID)GetProcAddress (_multimedia_library_handle, "timeBeginPeriod");
		_time_end_period_func = (PVOID)GetProcAddress (_multimedia_library_handle, "timeEndPeriod");
	}

	return _multimedia_library_handle != NULL && _time_begin_period_func != NULL && _time_end_period_func != NULL;
#else
	return true;
#endif //HOST_WIN32
}

static
void
sample_profiler_unload_dependencies (void)
{
#ifdef HOST_WIN32
	if (_multimedia_library_handle != NULL) {
		FreeLibrary (_multimedia_library_handle);
		_multimedia_library_handle = NULL;
		_time_begin_period_func = NULL;
		_time_end_period_func = NULL;
	}
#endif //HOST_WIN32
}

static
void
sample_profiler_enable (void)
{
	EP_ASSERT (_sampling_provider != NULL);
	EP_ASSERT (_thread_time_event != NULL);

	ep_requires_lock_held ();

	if (!sample_profiler_load_profiling_enabled ()) {
		sample_profiler_store_profiling_enabled (true);

		EP_ASSERT (!ep_rt_wait_event_is_valid (&_thread_shutdown_event));
		ep_rt_wait_event_alloc (&_thread_shutdown_event, true, false);
		if (!ep_rt_wait_event_is_valid (&_thread_shutdown_event))
			EP_UNREACHABLE ("Unable to create sample profiler event.");

		ep_rt_thread_id_t thread_id = ep_rt_uint64_t_to_thread_id_t (0);
		if (!ep_rt_thread_create ((void *)sampling_thread, NULL, EP_THREAD_TYPE_SAMPLING, &thread_id))
			EP_UNREACHABLE ("Unable to create sample profiler thread.");

		sample_profiler_set_time_granularity ();
	}
}

void
ep_sample_profiler_init (EventPipeProviderCallbackDataQueue *provider_callback_data_queue)
{
	ep_requires_lock_held ();

	if (!_sampling_provider) {
		_sampling_provider = provider_create_register (ep_config_get_sample_profiler_provider_name_utf8 (), NULL, NULL, NULL, provider_callback_data_queue);
		ep_raise_error_if_nok (_sampling_provider != NULL);
		_thread_time_event = provider_add_event (
			_sampling_provider,
			0, /* eventID */
			0, /* keywords */
			0, /* eventVersion */
			EP_EVENT_LEVEL_INFORMATIONAL,
			false /* NeedStack */,
			NULL,
			0);
		ep_raise_error_if_nok (_thread_time_event != NULL);
	}

ep_on_exit:
	ep_requires_lock_held ();
	return;

ep_on_error:

	ep_exit_error_handler ();
}

void
ep_sample_profiler_shutdown (void)
{
	ep_requires_lock_held ();

	EP_ASSERT (_ref_count == 0);

	provider_unregister_delete (_sampling_provider);

	_sampling_provider = NULL;
	_thread_time_event = NULL;

	_can_start_sampling = false;
}

void
ep_sample_profiler_enable (void)
{
	EP_ASSERT (_sampling_provider != NULL);
	EP_ASSERT (_thread_time_event != NULL);

	ep_requires_lock_held ();

	// Check to see if the sample profiler event is enabled. If it is not, do not spin up the sampling thread.
	if (!ep_event_is_enabled (_thread_time_event))
		return;

	sample_profiler_load_dependencies ();

	if (_can_start_sampling)
		sample_profiler_enable ();

	++_ref_count;
	EP_ASSERT (_ref_count > 0);
}

void
ep_sample_profiler_disable (void)
{
	EP_ASSERT (_ref_count > 0);

	ep_requires_lock_held ();

	// Bail early if profiling is not enabled.
	if (!sample_profiler_load_profiling_enabled ())
		return;

	if (_ref_count == 1) {
		EP_ASSERT (!ep_rt_process_detach ());

		// The sampling thread will watch this value and exit
		// when profiling is disabled.
		sample_profiler_store_profiling_enabled (false);

		// Wait for the sampling thread to clean itself up.
		ep_rt_wait_event_wait (&_thread_shutdown_event, EP_INFINITE_WAIT, false);
		ep_rt_wait_event_free (&_thread_shutdown_event);

		if (_time_period_is_set)
			sample_profiler_reset_time_granularity ();

		sample_profiler_unload_dependencies ();
	}

	--_ref_count;
	EP_ASSERT (_ref_count >= 0);
}

void
ep_sample_profiler_can_start_sampling (void)
{
	ep_requires_lock_held ();

	sample_profiler_store_can_start_sampling (true);
	if (_ref_count > 0)
		sample_profiler_enable ();
}

void
ep_sample_profiler_set_sampling_rate (uint64_t nanoseconds)
{
	// If the time period setting was modified by us,
	// make sure to change it back before changing our period
	// and losing track of what we set it to
	if (_time_period_is_set)
		sample_profiler_reset_time_granularity ();

	_sampling_rate_in_ns = nanoseconds;

	if (!_time_period_is_set)
		sample_profiler_set_time_granularity ();
}

uint64_t
ep_sample_profiler_get_sampling_rate (void)
{
	return _sampling_rate_in_ns;
}

#endif /* !defined(EP_INCLUDE_SOURCE_FILES) || defined(EP_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(EP_INCLUDE_SOURCE_FILES) && !defined(EP_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_eventpipe_sample_profiler;
const char quiet_linker_empty_file_warning_eventpipe_sample_profiler = 0;
#endif
