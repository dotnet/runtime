#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-rt-config.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-rt.h>
#include <eventpipe/ep.h>
#include <eventpipe/ep-session.h>

#include <mono/utils/mono-lazy-init.h>
#include <mono/utils/mono-time.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-rand.h>
#include <mono/metadata/profiler.h>
#include <mono/mini/mini-runtime.h>
#include <minipal/getexepath.h>
#include <runtime_version.h>
#include <clretwallmain.h>

extern void InitProvidersAndEvents (void);

// EventPipe init state.
static gboolean _eventpipe_initialized;

// Runtime init state.
gboolean _ep_rt_mono_runtime_initialized;

// EventPipe TLS key.
MonoNativeTlsKey _ep_rt_mono_thread_holder_tls_id;
static MonoNativeTlsKey _thread_data_tls_id;

// Random byte provider.
static gpointer _rand_provider;

// EventPipe global config lock.
ep_rt_spin_lock_handle_t _ep_rt_mono_config_lock = {0};

// OS cmd line.
mono_lazy_init_t _ep_rt_mono_os_cmd_line_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
char *_ep_rt_mono_os_cmd_line = NULL;

// Managed cmd line.
mono_lazy_init_t _ep_rt_mono_managed_cmd_line_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
char *_ep_rt_mono_managed_cmd_line = NULL;

// Mono profilers (shared with runtime provider).
MonoProfilerHandle _ep_rt_mono_default_profiler_provider = NULL;

// Providers
EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context = {0};
EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context = {0};
EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context = {0};
EVENTPIPE_TRACE_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_DOTNET_Context = {0};
EVENTPIPE_TRACE_CONTEXT MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_DOTNET_Context = {0};

#define RUNTIME_PROVIDER_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context
#define RUNTIME_PRIVATE_PROVIDER_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context
#define RUNTIME_RUNDOWN_PROVIDER_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context
#define RUNTIME_STRESS_PROVIDER_CONTEXT MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_DOTNET_Context
#define RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_DOTNET_Context

void
ep_rt_mono_thread_exited (void);

bool
ep_rt_mono_rand_try_get_bytes (
	uint8_t *buffer,
	size_t buffer_size)
{
	EP_ASSERT (_rand_provider != NULL);

	ERROR_DECL (error);
	return mono_rand_try_get_bytes (&_rand_provider, (guchar *)buffer, (gssize)buffer_size, error);
}

char *
ep_rt_mono_get_managed_cmd_line (void)
{
	return mono_runtime_get_managed_cmd_line ();
}

char *
ep_rt_mono_get_os_cmd_line (void)
{
	MONO_REQ_GC_NEUTRAL_MODE;

	// we only return the native host here since getting the full commandline is complicated and
	// it's not super important to have the correct value since it'll only be used during startup
	// until we have the managed commandline
	char *host_path = minipal_getexepath ();

	// minipal_getexepath doesn't use Mono APIs to allocate strings so
	// we can't use g_free (which the callers of this method expect to do)
	// so create another copy and return that one
	char *res = g_strdup (host_path);
	free (host_path);
	return res;
}

#ifdef HOST_WIN32

ep_rt_file_handle_t
ep_rt_mono_file_open_write (const ep_char8_t *path)
{
	if (!path)
		return INVALID_HANDLE_VALUE;

	ep_char16_t *path_utf16 = ep_rt_utf8_to_utf16le_string (path);

	if (!path_utf16)
		return INVALID_HANDLE_VALUE;

	ep_rt_file_handle_t res;
	MONO_ENTER_GC_SAFE;
	res = (ep_rt_file_handle_t)CreateFileW (path_utf16, GENERIC_WRITE, FILE_SHARE_READ, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	MONO_EXIT_GC_SAFE;
	ep_rt_utf16_string_free (path_utf16);

	return res;
}

bool
ep_rt_mono_file_close (ep_rt_file_handle_t handle)
{
	bool res;
	MONO_ENTER_GC_SAFE;
	res = CloseHandle (handle);
	MONO_EXIT_GC_SAFE;
	return res;
}

static
void
win32_io_interrupt_handler (void *ignored)
{
}

bool
ep_rt_mono_file_write (
	ep_rt_file_handle_t handle,
	const uint8_t *buffer,
	uint32_t numbytes,
	uint32_t *byteswritten)
{
	MONO_REQ_GC_UNSAFE_MODE;

	bool res;
	MonoThreadInfo *info = mono_thread_info_current ();
	gboolean alerted = FALSE;

	if (info) {
		mono_thread_info_install_interrupt (win32_io_interrupt_handler, NULL, &alerted);
		if (alerted) {
			return false;
		}
		mono_win32_enter_blocking_io_call (info, handle);
	}

	MONO_ENTER_GC_SAFE;
	if (info && mono_thread_info_is_interrupt_state (info)) {
		res = false;
	} else {
		res = WriteFile (handle, buffer, numbytes, (PDWORD)byteswritten, NULL) ? true : false;
	}
	MONO_EXIT_GC_SAFE;

	if (info) {
		mono_win32_leave_blocking_io_call (info, handle);
		mono_thread_info_uninstall_interrupt (&alerted);
	}

	return res;
}

#else

#include <fcntl.h>
#include <unistd.h>

ep_rt_file_handle_t
ep_rt_mono_file_open_write (const ep_char8_t *path)
{
	int fd;
	mode_t perms = 0666;

	if (!path)
		return INVALID_HANDLE_VALUE;

	MONO_ENTER_GC_SAFE;
	fd = creat (path, perms);
	MONO_EXIT_GC_SAFE;

	if (fd == -1)
		return INVALID_HANDLE_VALUE;

	return (ep_rt_file_handle_t)(ptrdiff_t)fd;
}

bool
ep_rt_mono_file_close (ep_rt_file_handle_t handle)
{
	int fd = (int)(ptrdiff_t)handle;

	MONO_ENTER_GC_SAFE;
	close (fd);
	MONO_EXIT_GC_SAFE;

	return true;
}

bool
ep_rt_mono_file_write (
	ep_rt_file_handle_t handle,
	const uint8_t *buffer,
	uint32_t numbytes,
	uint32_t *byteswritten)
{
	MONO_REQ_GC_UNSAFE_MODE;

	int fd = (int)(ptrdiff_t)handle;
	uint32_t ret;
	MonoThreadInfo *info = mono_thread_info_current ();

	if (byteswritten != NULL)
		*byteswritten = 0;

	do {
		MONO_ENTER_GC_SAFE;
		ret = write (fd, buffer, numbytes);
		MONO_EXIT_GC_SAFE;
	} while (ret == -1 && errno == EINTR &&
		 !mono_thread_info_is_interrupt_state (info));

	if (ret == -1) {
		if (errno == EINTR)
			ret = 0;
		else
			return false;
	}

	if (byteswritten != NULL)
		*byteswritten = ret;

	return true;
}

#endif // HOST_WIN32

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

EventPipeMonoThreadData *
ep_rt_mono_thread_data_get_or_create (void)
{
	EventPipeMonoThreadData *thread_data = (EventPipeMonoThreadData *)mono_native_tls_get_value (_thread_data_tls_id);
	if (!thread_data) {
		thread_data = ep_rt_object_alloc (EventPipeMonoThreadData);
		mono_native_tls_set_value (_thread_data_tls_id, thread_data);
	}
	return thread_data;
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
	if (_eventpipe_initialized) {
		EventPipeThreadHolder *thread_holder = (EventPipeThreadHolder *)mono_native_tls_get_value (_ep_rt_mono_thread_holder_tls_id);
		if (thread_holder)
			thread_holder_free_func (thread_holder);
		mono_native_tls_set_value (_ep_rt_mono_thread_holder_tls_id, NULL);

		EventPipeMonoThreadData *thread_data = (EventPipeMonoThreadData *)mono_native_tls_get_value (_thread_data_tls_id);
		if (thread_data)
			ep_rt_object_free (thread_data);
		mono_native_tls_set_value (_thread_data_tls_id, NULL);
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
#if defined (HAVE_CLOCK_MONOTONIC) && (defined(HOST_IOS) || defined(HOST_OSX) || defined(HOST_WATCHOS) || defined(HOST_TVOS))
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

		milliseconds = (uint16_t)(time_val.tv_usec / MSECS_TO_MIS);

		old_seconds = ut_ptr->tm_sec;
		new_seconds = time_val.tv_sec % 60;

		/* just in case we reached the next second in the interval between time () and gettimeofday () */
		if (old_seconds != new_seconds)
			milliseconds = 999;
	}

	ep_system_time_set (
		system_time,
		(uint16_t)(1900 + ut_ptr->tm_year),
		(uint16_t)ut_ptr->tm_mon + 1,
		(uint16_t)ut_ptr->tm_wday,
		(uint16_t)ut_ptr->tm_mday,
		(uint16_t)ut_ptr->tm_hour,
		(uint16_t)ut_ptr->tm_min,
		(uint16_t)ut_ptr->tm_sec,
		milliseconds);
}

static
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
#if defined (HOST_OSX)
G_BEGIN_DECLS
gchar ***_NSGetEnviron(void);
G_END_DECLS
#define environ (*_NSGetEnviron())
#else
static char *_ep_rt_mono_environ[1] = { NULL };
#define environ _ep_rt_mono_environ
#endif /* defined (HOST_OSX) */
#else
G_BEGIN_DECLS
extern char **environ;
G_END_DECLS
#endif /* defined (__APPLE__) */
#endif /* !defined (HOST_WIN32) */

void
ep_rt_mono_os_environment_get_utf16 (dn_vector_ptr_t *os_env)
{
	EP_ASSERT (os_env != NULL);
#ifdef HOST_WIN32
	LPWSTR envs = GetEnvironmentStringsW ();
	if (envs) {
		LPWSTR next = envs;
		while (*next) {
			dn_vector_ptr_push_back (os_env, ep_rt_utf16_string_dup (next));
			next += ep_rt_utf16_string_len (next) + 1;
		}
		FreeEnvironmentStringsW (envs);
	}
#else
	gchar **next = NULL;
	for (next = environ; *next != NULL; ++next)
		dn_vector_ptr_push_back (os_env, ep_rt_utf8_to_utf16le_string (*next));
#endif
}

void
ep_rt_mono_init_providers_and_events (void)
{
	InitProvidersAndEvents ();
}

void
ep_rt_mono_provider_config_init (EventPipeProviderConfiguration *provider_config)
{
	if (!ep_rt_utf8_string_compare (ep_config_get_rundown_provider_name_utf8 (), ep_provider_config_get_provider_name (provider_config))) {
		RUNTIME_RUNDOWN_PROVIDER_CONTEXT.Level = (uint8_t)ep_provider_config_get_logging_level (provider_config);
		RUNTIME_RUNDOWN_PROVIDER_CONTEXT.EnabledKeywordsBitmask = ep_provider_config_get_keywords (provider_config);
		RUNTIME_RUNDOWN_PROVIDER_CONTEXT.IsEnabled = true;
	}
}

bool
ep_rt_mono_providers_validate_all_disabled (void)
{
	return (!RUNTIME_PROVIDER_CONTEXT.IsEnabled &&
		!RUNTIME_PRIVATE_PROVIDER_CONTEXT.IsEnabled &&
		!RUNTIME_RUNDOWN_PROVIDER_CONTEXT.IsEnabled &&
		!RUNTIME_STRESS_PROVIDER_CONTEXT.IsEnabled &&
		!RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT.IsEnabled);
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

static
bool
is_keword_enabled (uint64_t enabled_keywords, uint64_t keyword)
{
	return (enabled_keywords & keyword) == keyword;
}

uint64_t
ep_rt_mono_session_calculate_and_count_all_keywords (
	const ep_char8_t *provider,
	uint64_t keywords[],
	uint64_t count[],
	size_t len)
{
	ep_requires_lock_held ();

	uint64_t keywords_for_all_sessions = 0;

	for (int i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; i++) {
		EventPipeSession *session = ep_volatile_load_session_without_barrier (i);
		if (session) {
			EventPipeSessionProviderList *providers = ep_session_get_providers (session);
			EP_ASSERT (providers != NULL);

			EventPipeSessionProvider *session_provider = ep_session_provider_list_find_by_name (ep_session_provider_list_get_providers (providers), provider);
			if (session_provider) {
				uint64_t session_keywords = ep_session_provider_get_keywords (session_provider);
				for (uint64_t j = 0; j < len; j++) {
					if (is_keword_enabled (session_keywords, keywords [j]))
						count [j]++;
				}
				keywords_for_all_sessions = keywords_for_all_sessions | session_keywords;
			}
		}
	}

	ep_requires_lock_held ();

	return keywords_for_all_sessions;
}

bool
ep_rt_mono_sesion_has_all_started (void)
{
	ep_requires_lock_held ();

	bool all_started = true;
	for (uint32_t i = 0; i < EP_MAX_NUMBER_OF_SESSIONS; ++i) {
		EventPipeSession *session = ep_volatile_load_session_without_barrier (i);
		if (session) {
			all_started = ep_session_has_started (session);
			if (!all_started)
				break;
		}
	}

	ep_requires_lock_held ();

	return all_started;
}

static
void
runtime_initialized_callback (MonoProfiler *prof)
{
	_ep_rt_mono_runtime_initialized = TRUE;
}

static
void
thread_started_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	ep_rt_mono_runtime_provider_thread_started_callback (prof, tid);
}

static
void
thread_stopped_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	ep_rt_mono_runtime_provider_thread_stopped_callback (prof, tid);
	ep_rt_mono_thread_exited ();
}

void
ep_rt_mono_component_init (void)
{
	ep_rt_spin_lock_alloc (&_ep_rt_mono_config_lock);

	RUNTIME_PROVIDER_CONTEXT = MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_EVENTPIPE_Context;
	RUNTIME_PRIVATE_PROVIDER_CONTEXT = MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_EVENTPIPE_Context;
	RUNTIME_RUNDOWN_PROVIDER_CONTEXT = MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_EVENTPIPE_Context;
	RUNTIME_STRESS_PROVIDER_CONTEXT = MICROSOFT_WINDOWS_DOTNETRUNTIME_STRESS_PROVIDER_EVENTPIPE_Context;
	RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT = MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_EVENTPIPE_Context;

	_ep_rt_mono_default_profiler_provider = mono_profiler_create (NULL);

	char *diag_env = g_getenv("MONO_DIAGNOSTICS");
	if (diag_env) {
		int diag_argc = 1;
		char **diag_argv = g_new (char *, 1);
		if (diag_argv) {
			diag_argv [0] = NULL;
			if (!mono_parse_options_from (diag_env, &diag_argc, &diag_argv)) {
				for (int i = 0; i < diag_argc; ++i) {
					if (diag_argv [i]) {
						if (strncmp (diag_argv [i], "--diagnostic-ports=", 19) == 0) {
							char *diag_ports_env = g_getenv("DOTNET_DiagnosticPorts");
							if (diag_ports_env)
								mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_DIAGNOSTICS, "DOTNET_DiagnosticPorts environment variable already set, ignoring --diagnostic-ports used in MONO_DIAGNOSTICS environment variable");
							else
								g_setenv ("DOTNET_DiagnosticPorts", diag_argv [i] + 19, TRUE);
							g_free (diag_ports_env);
						} else if (ep_rt_mono_profiler_provider_parse_options (diag_argv [i])) {
							;
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

	ep_rt_mono_runtime_provider_component_init ();
	ep_rt_mono_profiler_provider_component_init ();
}

void
ep_rt_mono_init (void)
{
	EP_ASSERT (_ep_rt_mono_default_profiler_provider != NULL);

	mono_native_tls_alloc (&_ep_rt_mono_thread_holder_tls_id, NULL);
	mono_native_tls_alloc (&_thread_data_tls_id, NULL);

	mono_100ns_ticks ();
	mono_rand_open ();
	_rand_provider = mono_rand_init (NULL, 0);

	ep_rt_mono_runtime_provider_init ();
	ep_rt_mono_profiler_provider_init ();

	mono_profiler_set_runtime_initialized_callback (_ep_rt_mono_default_profiler_provider, runtime_initialized_callback);
	mono_profiler_set_thread_started_callback (_ep_rt_mono_default_profiler_provider, thread_started_callback);
	mono_profiler_set_thread_stopped_callback (_ep_rt_mono_default_profiler_provider, thread_stopped_callback);

	_eventpipe_initialized = TRUE;
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
	ep_rt_mono_runtime_provider_fini ();
	ep_rt_mono_profiler_provider_fini ();

	if (_eventpipe_initialized)
		mono_rand_close (_rand_provider);

	_rand_provider = NULL;
	_eventpipe_initialized = FALSE;

	_ep_rt_mono_runtime_initialized = FALSE;

	if (_ep_rt_mono_default_profiler_provider) {
		mono_profiler_set_runtime_initialized_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_thread_started_callback (_ep_rt_mono_default_profiler_provider, NULL);
		mono_profiler_set_thread_stopped_callback (_ep_rt_mono_default_profiler_provider, NULL);
	}
	_ep_rt_mono_default_profiler_provider = NULL;

	if (_ep_rt_mono_thread_holder_tls_id)
		mono_native_tls_free (_ep_rt_mono_thread_holder_tls_id);
	_ep_rt_mono_thread_holder_tls_id = 0;

	if (_thread_data_tls_id)
		mono_native_tls_free (_thread_data_tls_id);
	_thread_data_tls_id = 0;

	_ep_rt_mono_os_cmd_line_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
	_ep_rt_mono_os_cmd_line = NULL;

	_ep_rt_mono_managed_cmd_line_init = MONO_LAZY_INIT_STATUS_NOT_INITIALIZED;
	_ep_rt_mono_managed_cmd_line = NULL;

	ep_rt_spin_lock_free (&_ep_rt_mono_config_lock);
}

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntimeRundown (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	RUNTIME_RUNDOWN_PROVIDER_CONTEXT.Level = level;
	RUNTIME_RUNDOWN_PROVIDER_CONTEXT.EnabledKeywordsBitmask = match_any_keywords;
	RUNTIME_RUNDOWN_PROVIDER_CONTEXT.IsEnabled = (is_enabled == 1 ? true : false);
}

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntimePrivate (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	RUNTIME_PRIVATE_PROVIDER_CONTEXT.Level = level;
	RUNTIME_PRIVATE_PROVIDER_CONTEXT.EnabledKeywordsBitmask = match_any_keywords;
	RUNTIME_PRIVATE_PROVIDER_CONTEXT.IsEnabled = (is_enabled == 1 ? true : false);
}

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntimeStress (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	RUNTIME_STRESS_PROVIDER_CONTEXT.Level = level;
	RUNTIME_STRESS_PROVIDER_CONTEXT.EnabledKeywordsBitmask = match_any_keywords;
	RUNTIME_STRESS_PROVIDER_CONTEXT.IsEnabled = (is_enabled == 1 ? true : false);
}

#endif /* ENABLE_PERFTRACING */

MONO_EMPTY_SOURCE_FILE(eventpipe_rt_mono);
