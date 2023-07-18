// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Implementation of ep-rt.h targeting CoreCLR runtime.
#ifndef __EVENTPIPE_RT_CORECLR_H__
#define __EVENTPIPE_RT_CORECLR_H__

#include <eventpipe/ep-rt-config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-thread.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-provider.h>
#include <eventpipe/ep-session-provider.h>
#include "fstream.h"
#include "typestring.h"
#include "clrversion.h"
#include "hostinformation.h"

#undef EP_INFINITE_WAIT
#define EP_INFINITE_WAIT INFINITE

#undef EP_GCX_PREEMP_ENTER
#define EP_GCX_PREEMP_ENTER { GCX_PREEMP();

#undef EP_GCX_PREEMP_EXIT
#define EP_GCX_PREEMP_EXIT }

#undef EP_ALWAYS_INLINE
#define EP_ALWAYS_INLINE FORCEINLINE

#undef EP_NEVER_INLINE
#define EP_NEVER_INLINE NOINLINE

#undef EP_ALIGN_UP
#define EP_ALIGN_UP(val,align) ALIGN_UP(val,align)

static
inline
ep_rt_lock_handle_t *
ep_rt_coreclr_config_lock_get (void)
{
	STATIC_CONTRACT_NOTHROW;

	extern ep_rt_lock_handle_t _ep_rt_coreclr_config_lock_handle;
	return &_ep_rt_coreclr_config_lock_handle;
}

static
inline
const ep_char8_t *
ep_rt_entrypoint_assembly_name_get_utf8 (void)
{
	STATIC_CONTRACT_NOTHROW;

	AppDomain *app_domain_ref = nullptr;
	Assembly *assembly_ref = nullptr;

	app_domain_ref = GetAppDomain ();
	if (app_domain_ref != nullptr)
	{
		assembly_ref = app_domain_ref->GetRootAssembly ();
		if (assembly_ref != nullptr)
		{
			return reinterpret_cast<const ep_char8_t*>(assembly_ref->GetSimpleName ());
		}
	}

	// get the name from the host if we can't get assembly info, e.g., if the runtime is
	// suspended before an assembly is loaded.
	// We'll cache the value in a static function global as the caller expects the lifetime of this value
	// to outlast the calling function.
	static const ep_char8_t* entrypoint_assembly_name = nullptr;
	if (entrypoint_assembly_name == nullptr)
	{
		const ep_char8_t* entrypoint_assembly_name_local;
		SString assembly_name;
		if (HostInformation::GetProperty (HOST_PROPERTY_ENTRY_ASSEMBLY_NAME, assembly_name))
		{
			entrypoint_assembly_name_local = reinterpret_cast<const ep_char8_t*>(assembly_name.GetCopyOfUTF8String ());
		}
		else
		{
			// fallback to the empty string
			// Allocate a new empty string here so we consistently allocate with the same allocator no matter our code-path.
			entrypoint_assembly_name_local = new ep_char8_t [1] { '\0' };
		}
		// Try setting this entrypoint name as the cached value.
		// If someone else beat us to it, free the memory we allocated.
		// We want to only leak the one global copy of the entrypoint name,
		// not multiple copies.
		if (InterlockedCompareExchangeT(&entrypoint_assembly_name, entrypoint_assembly_name_local, nullptr) != nullptr)
		{
			delete[] entrypoint_assembly_name_local;
		}
	}

	return entrypoint_assembly_name;
}

static
const ep_char8_t *
ep_rt_runtime_version_get_utf8 (void)
{
	STATIC_CONTRACT_NOTHROW;

	return reinterpret_cast<const ep_char8_t*>(CLR_PRODUCT_VERSION);
}

/*
 * Little-Endian Conversion.
 */

static
EP_ALWAYS_INLINE
uint16_t
ep_rt_val_uint16_t (uint16_t value)
{
	return value;
}

static
EP_ALWAYS_INLINE
uint32_t
ep_rt_val_uint32_t (uint32_t value)
{
	return value;
}

static
EP_ALWAYS_INLINE
uint64_t
ep_rt_val_uint64_t (uint64_t value)
{
	return value;
}

static
EP_ALWAYS_INLINE
int16_t
ep_rt_val_int16_t (int16_t value)
{
	return value;
}

static
EP_ALWAYS_INLINE
int32_t
ep_rt_val_int32_t (int32_t value)
{
	return value;
}

static
EP_ALWAYS_INLINE
int64_t
ep_rt_val_int64_t (int64_t value)
{
	return value;
}

static
EP_ALWAYS_INLINE
uintptr_t
ep_rt_val_uintptr_t (uintptr_t value)
{
	return value;
}

/*
* Atomics.
*/

static
inline
uint32_t
ep_rt_atomic_inc_uint32_t (volatile uint32_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<uint32_t>(InterlockedIncrement ((volatile LONG *)(value)));
}

static
inline
uint32_t
ep_rt_atomic_dec_uint32_t (volatile uint32_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<uint32_t>(InterlockedDecrement ((volatile LONG *)(value)));
}

static
inline
int32_t
ep_rt_atomic_inc_int32_t (volatile int32_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int32_t>(InterlockedIncrement ((volatile LONG *)(value)));
}

static
inline
int32_t
ep_rt_atomic_dec_int32_t (volatile int32_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int32_t>(InterlockedDecrement ((volatile LONG *)(value)));
}

static
inline
int64_t
ep_rt_atomic_inc_int64_t (volatile int64_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int64_t>(InterlockedIncrement64 ((volatile LONG64 *)(value)));
}

static
inline
int64_t
ep_rt_atomic_dec_int64_t (volatile int64_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int64_t>(InterlockedDecrement64 ((volatile LONG64 *)(value)));
}

static
inline
size_t
ep_rt_atomic_compare_exchange_size_t (volatile size_t *target, size_t expected, size_t value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<size_t>(InterlockedCompareExchangeT<size_t> (target, value, expected));
}

static
inline
ep_char8_t *
ep_rt_atomic_compare_exchange_utf8_string (ep_char8_t *volatile *target, ep_char8_t *expected, ep_char8_t *value)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<ep_char8_t *>(InterlockedCompareExchangeT<ep_char8_t *> (target, value, expected));
}

/*
 * EventPipe.
 */

static
void
ep_rt_init (void)
{
	STATIC_CONTRACT_NOTHROW;

	extern ep_rt_lock_handle_t _ep_rt_coreclr_config_lock_handle;
	extern CrstStatic _ep_rt_coreclr_config_lock;

	_ep_rt_coreclr_config_lock_handle.lock = &_ep_rt_coreclr_config_lock;
	_ep_rt_coreclr_config_lock_handle.lock->InitNoThrow (CrstEventPipe, (CrstFlags)(CRST_REENTRANCY | CRST_TAKEN_DURING_SHUTDOWN | CRST_HOST_BREAKABLE));

	if (CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeProcNumbers) != 0) {
#ifndef TARGET_UNIX
		// setup the windows processor group offset table
		uint16_t groups = ::GetActiveProcessorGroupCount ();
		extern uint32_t *_ep_rt_coreclr_proc_group_offsets;
		_ep_rt_coreclr_proc_group_offsets = new (nothrow) uint32_t [groups];
		if (_ep_rt_coreclr_proc_group_offsets) {
			uint32_t procs = 0;
			for (uint16_t i = 0; i < procs; ++i) {
				_ep_rt_coreclr_proc_group_offsets [i] = procs;
				procs += GetActiveProcessorCount (i);
			}
		}
#endif
	}
}

static
inline
void
ep_rt_init_finish (void)
{
	STATIC_CONTRACT_NOTHROW;
}

static
inline
void
ep_rt_shutdown (void)
{
	STATIC_CONTRACT_NOTHROW;
}

static
inline
bool
ep_rt_config_acquire (void)
{
	STATIC_CONTRACT_NOTHROW;
	return ep_rt_lock_acquire (ep_rt_coreclr_config_lock_get ());
}

static
inline
bool
ep_rt_config_release (void)
{
	STATIC_CONTRACT_NOTHROW;
	return ep_rt_lock_release (ep_rt_coreclr_config_lock_get ());
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_config_requires_lock_held (void)
{
	STATIC_CONTRACT_NOTHROW;
	ep_rt_lock_requires_lock_held (ep_rt_coreclr_config_lock_get ());
}

static
inline
void
ep_rt_config_requires_lock_not_held (void)
{
	STATIC_CONTRACT_NOTHROW;
	ep_rt_lock_requires_lock_not_held (ep_rt_coreclr_config_lock_get ());
}
#endif

static
inline
bool
ep_rt_walk_managed_stack_for_thread (
	ep_rt_thread_handle_t thread,
	EventPipeStackContents *stack_contents)
{
	STATIC_CONTRACT_NOTHROW;
	extern bool ep_rt_coreclr_walk_managed_stack_for_thread (ep_rt_thread_handle_t thread, EventPipeStackContents *stack_contents);
	return ep_rt_coreclr_walk_managed_stack_for_thread (thread, stack_contents);
}

static
inline
bool
ep_rt_method_get_simple_assembly_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (method != NULL);
	EP_ASSERT (name != NULL);

	const ep_char8_t *assembly_name = method->GetLoaderModule ()->GetAssembly ()->GetSimpleName ();
	if (!assembly_name)
		return false;

	size_t assembly_name_len = strlen (assembly_name) + 1;
	size_t to_copy = assembly_name_len < name_len ? assembly_name_len : name_len;
	memcpy (name, assembly_name, to_copy);
	name [to_copy - 1] = 0;

	return true;
}

static
bool
ep_rt_method_get_full_name (
	ep_rt_method_desc_t *method,
	ep_char8_t *name,
	size_t name_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (method != NULL);
	EP_ASSERT (name != NULL);

	bool result = true;
	EX_TRY
	{
		SString method_name;

		TypeString::AppendMethodInternal (method_name, method, TypeString::FormatNamespace | TypeString::FormatSignature);
		const ep_char8_t *method_name_utf8 = method_name.GetUTF8 ();
		if (method_name_utf8) {
			size_t method_name_utf8_len = strlen (method_name_utf8) + 1;
			size_t to_copy = method_name_utf8_len < name_len ? method_name_utf8_len : name_len;
			memcpy (name, method_name_utf8, to_copy);
			name [to_copy - 1] = 0;
		} else {
			result = false;
		}
	}
	EX_CATCH
	{
		result = false;
	}
	EX_END_CATCH(SwallowAllExceptions);

	return result;
}

static
inline
void
ep_rt_provider_config_init (EventPipeProviderConfiguration *provider_config)
{
	STATIC_CONTRACT_NOTHROW;

	if (!ep_rt_utf8_string_compare (ep_config_get_rundown_provider_name_utf8 (), ep_provider_config_get_provider_name (provider_config))) {
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.Level = (UCHAR) ep_provider_config_get_logging_level (provider_config);
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.EnabledKeywordsBitmask = ep_provider_config_get_keywords (provider_config);
		MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled = true;
	}
}

// This function is auto-generated from /src/scripts/genEventPipe.py
#ifdef TARGET_UNIX
extern "C" void InitProvidersAndEvents ();
#else
extern void InitProvidersAndEvents ();
#endif

static
void
ep_rt_init_providers_and_events (void)
{
	STATIC_CONTRACT_NOTHROW;

	EX_TRY
	{
		InitProvidersAndEvents ();
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
}

static
inline
bool
ep_rt_providers_validate_all_disabled (void)
{
	STATIC_CONTRACT_NOTHROW;

	return (!MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled &&
		!MICROSOFT_WINDOWS_DOTNETRUNTIME_PRIVATE_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled &&
		!MICROSOFT_WINDOWS_DOTNETRUNTIME_RUNDOWN_PROVIDER_DOTNET_Context.EventPipeProvider.IsEnabled);
}

static
inline
void
ep_rt_prepare_provider_invoke_callback (EventPipeProviderCallbackData *provider_callback_data)
{
	STATIC_CONTRACT_NOTHROW;
}

static
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
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (callback_func != NULL);

	EX_TRY
	{
		(*callback_func)(
			source_id,
			is_enabled,
			level,
			match_any_keywords,
			match_all_keywords,
			filter_data,
			callback_data);
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
}

/*
 * EventPipeProviderConfiguration.
 */

static
inline
bool
ep_rt_config_value_get_enable (void)
{
	STATIC_CONTRACT_NOTHROW;
	return CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EnableEventPipe) != 0;
}

static
inline
ep_char8_t *
ep_rt_config_value_get_config (void)
{
	STATIC_CONTRACT_NOTHROW;
	CLRConfigStringHolder value(CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeConfig));
	return ep_rt_utf16_to_utf8_string (reinterpret_cast<ep_char16_t *>(value.GetValue ()), -1);
}

static
inline
ep_char8_t *
ep_rt_config_value_get_output_path (void)
{
	STATIC_CONTRACT_NOTHROW;
	CLRConfigStringHolder value(CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeOutputPath));
	return ep_rt_utf16_to_utf8_string (reinterpret_cast<ep_char16_t *>(value.GetValue ()), -1);
}

static
inline
uint32_t
ep_rt_config_value_get_circular_mb (void)
{
	STATIC_CONTRACT_NOTHROW;
	return CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeCircularMB);
}

static
inline
bool
ep_rt_config_value_get_output_streaming (void)
{
	STATIC_CONTRACT_NOTHROW;
	return CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeOutputStreaming) != 0;
}

static
inline
bool
ep_rt_config_value_get_enable_stackwalk (void)
{
	STATIC_CONTRACT_NOTHROW;
	return CLRConfig::GetConfigValue(CLRConfig::INTERNAL_EventPipeEnableStackwalk) != 0;
}

/*
 * EventPipeSampleProfiler.
 */

static
inline
void
ep_rt_sample_profiler_write_sampling_event_for_threads (
	ep_rt_thread_handle_t sampling_thread,
	EventPipeEvent *sampling_event)
{
	STATIC_CONTRACT_NOTHROW;

	extern void ep_rt_coreclr_sample_profiler_write_sampling_event_for_threads (ep_rt_thread_handle_t sampling_thread, EventPipeEvent *sampling_event);
	ep_rt_coreclr_sample_profiler_write_sampling_event_for_threads (sampling_thread, sampling_event);
}

static
inline
void
ep_rt_notify_profiler_provider_created (EventPipeProvider *provider)
{
	STATIC_CONTRACT_NOTHROW;

#ifndef DACCESS_COMPILE
		// Let the profiler know the provider has been created so it can register if it wants to
		BEGIN_PROFILER_CALLBACK (CORProfilerTrackEventPipe ());
		(&g_profControlBlock)->EventPipeProviderCreated (provider);
		END_PROFILER_CALLBACK ();
#endif // DACCESS_COMPILE
}

/*
 * Arrays.
 */

static
inline
uint8_t *
ep_rt_byte_array_alloc (size_t len)
{
	STATIC_CONTRACT_NOTHROW;
	return new (nothrow) uint8_t [len];
}

static
inline
void
ep_rt_byte_array_free (uint8_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;

	if (ptr)
		delete [] ptr;
}

/*
 * Event.
 */

static
void
ep_rt_wait_event_alloc (
	ep_rt_wait_event_handle_t *wait_event,
	bool manual,
	bool initial)
{
	STATIC_CONTRACT_NOTHROW;

	EP_ASSERT (wait_event != NULL);
	EP_ASSERT (wait_event->event == NULL);

	wait_event->event = new (nothrow) CLREventStatic ();
	if (wait_event->event) {
		EX_TRY
		{
			if (manual)
				wait_event->event->CreateManualEvent (initial);
			else
				wait_event->event->CreateAutoEvent (initial);
		}
		EX_CATCH {}
		EX_END_CATCH(SwallowAllExceptions);
	}
}

static
inline
void
ep_rt_wait_event_free (ep_rt_wait_event_handle_t *wait_event)
{
	STATIC_CONTRACT_NOTHROW;

	if (wait_event != NULL && wait_event->event != NULL) {
		wait_event->event->CloseEvent ();
		delete wait_event->event;
		wait_event->event = NULL;
	}
}

static
inline
bool
ep_rt_wait_event_set (ep_rt_wait_event_handle_t *wait_event)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

	return wait_event->event->Set ();
}

static
int32_t
ep_rt_wait_event_wait (
	ep_rt_wait_event_handle_t *wait_event,
	uint32_t timeout,
	bool alertable)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

	int32_t result;
	EX_TRY
	{
		result = wait_event->event->Wait (timeout, alertable);
	}
	EX_CATCH
	{
		result = -1;
	}
	EX_END_CATCH(SwallowAllExceptions);
	return result;
}

static
inline
EventPipeWaitHandle
ep_rt_wait_event_get_wait_handle (ep_rt_wait_event_handle_t *wait_event)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (wait_event != NULL && wait_event->event != NULL);

	return reinterpret_cast<EventPipeWaitHandle>(wait_event->event->GetHandleUNHOSTED ());
}

static
inline
bool
ep_rt_wait_event_is_valid (ep_rt_wait_event_handle_t *wait_event)
{
	STATIC_CONTRACT_NOTHROW;

	if (wait_event == NULL || wait_event->event == NULL)
		return false;

	return wait_event->event->IsValid ();
}

/*
 * Misc.
 */

static
inline
int
ep_rt_get_last_error (void)
{
	STATIC_CONTRACT_NOTHROW;
	return ::GetLastError ();
}

static
inline
bool
ep_rt_process_detach (void)
{
	STATIC_CONTRACT_NOTHROW;
	return (bool)g_fProcessDetach;
}

static
inline
bool
ep_rt_process_shutdown (void)
{
	STATIC_CONTRACT_NOTHROW;
	return (bool)g_fEEShutDown;
}

static
inline
void
ep_rt_create_activity_id (
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	CoCreateGuid (reinterpret_cast<GUID *>(activity_id));
}

static
inline
bool
ep_rt_is_running (void)
{
	STATIC_CONTRACT_NOTHROW;
	return (bool)g_fEEStarted;
}

static
inline
void
ep_rt_execute_rundown (dn_vector_ptr_t *execution_checkpoints)
{
	STATIC_CONTRACT_NOTHROW;

	//TODO: Write execution checkpoint rundown events.
	if (CLRConfig::GetConfigValue (CLRConfig::INTERNAL_EventPipeRundown) > 0) {
		// Ask the runtime to emit rundown events.
		if (g_fEEStarted && !g_fEEShutDown)
			ETW::EnumerationLog::EndRundown ();
	}
}

/*
 * Objects.
 */

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_alloc
#define ep_rt_object_alloc(obj_type) (new (nothrow) obj_type())

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_array_alloc
#define ep_rt_object_array_alloc(obj_type,size) (new (nothrow) obj_type [size]())

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_array_free
#define ep_rt_object_array_free(obj_ptr) do { if (obj_ptr) delete [] obj_ptr; } while(0)

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_object_free
#define ep_rt_object_free(obj_ptr) do { if (obj_ptr) delete obj_ptr; } while(0)

/*
 * PAL.
 */

typedef struct _rt_coreclr_thread_params_internal_t {
	ep_rt_thread_params_t thread_params;
} rt_coreclr_thread_params_internal_t;

#undef EP_RT_DEFINE_THREAD_FUNC
#define EP_RT_DEFINE_THREAD_FUNC(name) static ep_rt_thread_start_func_return_t WINAPI name (LPVOID data)

EP_RT_DEFINE_THREAD_FUNC (ep_rt_thread_coreclr_start_func)
{
	STATIC_CONTRACT_NOTHROW;

	rt_coreclr_thread_params_internal_t *thread_params = reinterpret_cast<rt_coreclr_thread_params_internal_t *>(data);
	DWORD result = thread_params->thread_params.thread_func (thread_params);
	if (thread_params->thread_params.thread)
		::DestroyThread (thread_params->thread_params.thread);
	delete thread_params;
	return result;
}

static
bool
ep_rt_thread_create (
	void *thread_func,
	void *params,
	EventPipeThreadType thread_type,
	void *id)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (thread_func != NULL);

	bool result = false;

	EX_TRY
	{
		if (thread_type == EP_THREAD_TYPE_SERVER)
		{
			DWORD thread_id = 0;
			HANDLE server_thread = ::CreateThread (nullptr, 0, reinterpret_cast<LPTHREAD_START_ROUTINE>(thread_func), nullptr, 0, &thread_id);
			if (server_thread != NULL)
			{
				if (id)
				{
					*reinterpret_cast<DWORD *>(id) = thread_id;
				}
				::CloseHandle (server_thread);
				result = true;
			}
		}
		else if (thread_type == EP_THREAD_TYPE_SESSION || thread_type == EP_THREAD_TYPE_SAMPLING)
		{
			rt_coreclr_thread_params_internal_t *thread_params = new (nothrow) rt_coreclr_thread_params_internal_t ();
			if (thread_params)
			{
				thread_params->thread_params.thread_type = thread_type;
				thread_params->thread_params.thread = SetupUnstartedThread ();
				thread_params->thread_params.thread_func = reinterpret_cast<LPTHREAD_START_ROUTINE>(thread_func);
				thread_params->thread_params.thread_params = params;

				if (thread_params->thread_params.thread->CreateNewThread (0, ep_rt_thread_coreclr_start_func, thread_params))
				{
					if (id)
					{
						*reinterpret_cast<DWORD *>(id) = thread_params->thread_params.thread->GetThreadId ();
					}
					thread_params->thread_params.thread->SetBackground (TRUE);
					thread_params->thread_params.thread->StartThread ();
					result = true;
				}
				else
				{
					delete thread_params;
				}
			}
		}
	}
	EX_CATCH
	{
		result = false;
	}
	EX_END_CATCH(SwallowAllExceptions);

	return result;
}

static
inline
void
ep_rt_set_server_name(void)
{
	::SetThreadName(GetCurrentThread(), W(".NET EventPipe"));
}

static
inline
void
ep_rt_thread_sleep (uint64_t ns)
{
	STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX
	PAL_nanosleep (ns);
#else  //TARGET_UNIX
	const uint32_t NUM_NANOSECONDS_IN_1_MS = 1000000;
	ClrSleepEx (static_cast<DWORD>(ns / NUM_NANOSECONDS_IN_1_MS), FALSE);
#endif //TARGET_UNIX
}

static
inline
uint32_t
ep_rt_current_process_get_id (void)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<uint32_t>(GetCurrentProcessId ());
}

static
inline
uint32_t
ep_rt_current_processor_get_number (void)
{
	STATIC_CONTRACT_NOTHROW;

#ifndef TARGET_UNIX
	extern uint32_t *_ep_rt_coreclr_proc_group_offsets;
	if (_ep_rt_coreclr_proc_group_offsets) {
		PROCESSOR_NUMBER proc;
		GetCurrentProcessorNumberEx (&proc);
		return _ep_rt_coreclr_proc_group_offsets [proc.Group] + proc.Number;
	}
#endif
	return 0xFFFFFFFF;
}

static
inline
uint32_t
ep_rt_processors_get_count (void)
{
	STATIC_CONTRACT_NOTHROW;

	SYSTEM_INFO sys_info = {};
	GetSystemInfo (&sys_info);
	return static_cast<uint32_t>(sys_info.dwNumberOfProcessors);
}

static
inline
ep_rt_thread_id_t
ep_rt_current_thread_get_id (void)
{
	STATIC_CONTRACT_NOTHROW;

#ifdef TARGET_UNIX
	return static_cast<ep_rt_thread_id_t>(::PAL_GetCurrentOSThreadId ());
#else
	return static_cast<ep_rt_thread_id_t>(::GetCurrentThreadId ());
#endif
}

static
inline
int64_t
ep_rt_perf_counter_query (void)
{
	STATIC_CONTRACT_NOTHROW;

	LARGE_INTEGER value;
	if (QueryPerformanceCounter (&value))
		return static_cast<int64_t>(value.QuadPart);
	else
		return 0;
}

static
inline
int64_t
ep_rt_perf_frequency_query (void)
{
	STATIC_CONTRACT_NOTHROW;

	LARGE_INTEGER value;
	if (QueryPerformanceFrequency (&value))
		return static_cast<int64_t>(value.QuadPart);
	else
		return 0;
}

static
inline
void
ep_rt_system_time_get (EventPipeSystemTime *system_time)
{
	STATIC_CONTRACT_NOTHROW;

	SYSTEMTIME value;
	GetSystemTime (&value);

	EP_ASSERT(system_time != NULL);
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

static
inline
int64_t
ep_rt_system_timestamp_get (void)
{
	STATIC_CONTRACT_NOTHROW;

	FILETIME value;
	GetSystemTimeAsFileTime (&value);
	return static_cast<int64_t>(((static_cast<uint64_t>(value.dwHighDateTime)) << 32) | static_cast<uint64_t>(value.dwLowDateTime));
}

static
inline
int32_t
ep_rt_system_get_alloc_granularity (void)
{
	STATIC_CONTRACT_NOTHROW;
	return static_cast<int32_t>(g_SystemInfo.dwAllocationGranularity);
}

static
inline
const ep_char8_t *
ep_rt_os_command_line_get (void)
{
	STATIC_CONTRACT_NOTHROW;
	EP_UNREACHABLE ("Can not reach here");

	return NULL;
}

static
ep_rt_file_handle_t
ep_rt_file_open_write (const ep_char8_t *path)
{
	STATIC_CONTRACT_NOTHROW;

	ep_char16_t *path_utf16 = ep_rt_utf8_to_utf16le_string (path, -1);
	ep_return_null_if_nok (path_utf16 != NULL);

	CFileStream *file_stream = new (nothrow) CFileStream ();
	if (file_stream && FAILED (file_stream->OpenForWrite (reinterpret_cast<LPWSTR>(path_utf16)))) {
		delete file_stream;
		file_stream = NULL;
	}

	ep_rt_utf16_string_free (path_utf16);
	return static_cast<ep_rt_file_handle_t>(file_stream);
}

static
inline
bool
ep_rt_file_close (ep_rt_file_handle_t file_handle)
{
	STATIC_CONTRACT_NOTHROW;

	// Closed in destructor.
	if (file_handle)
		delete file_handle;
	return true;
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
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (buffer != NULL);

	ep_return_false_if_nok (file_handle != NULL);

	ULONG out_count;
	HRESULT result = reinterpret_cast<CFileStream *>(file_handle)->Write (buffer, bytes_to_write, &out_count);
	*bytes_written = static_cast<uint32_t>(out_count);
	return result == S_OK;
}

static
inline
uint8_t *
ep_rt_valloc0 (size_t buffer_size)
{
	STATIC_CONTRACT_NOTHROW;
	return reinterpret_cast<uint8_t *>(ClrVirtualAlloc (NULL, buffer_size, MEM_COMMIT, PAGE_READWRITE));
}

static
inline
void
ep_rt_vfree (
	uint8_t *buffer,
	size_t buffer_size)
{
	STATIC_CONTRACT_NOTHROW;

	if (buffer)
		ClrVirtualFree (buffer, 0, MEM_RELEASE);
}

static
inline
uint32_t
ep_rt_temp_path_get (
	ep_char8_t *buffer,
	uint32_t buffer_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_UNREACHABLE ("Can not reach here");

	return 0;
}

static
void
ep_rt_os_environment_get_utf16 (dn_vector_ptr_t *env_array)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (env_array != NULL);

	LPWSTR envs = GetEnvironmentStringsW ();
	if (envs) {
		LPWSTR next = envs;
		while (*next) {
			dn_vector_ptr_push_back (env_array, ep_rt_utf16_string_dup (reinterpret_cast<const ep_char16_t *>(next)));
			next += ep_rt_utf16_string_len (reinterpret_cast<const ep_char16_t *>(next)) + 1;
		}
		FreeEnvironmentStringsW (envs);
	}
}

/*
* Lock.
*/

static
bool
ep_rt_lock_acquire (ep_rt_lock_handle_t *lock)
{
	STATIC_CONTRACT_NOTHROW;

	bool result = true;
	EX_TRY
	{
		if (lock) {
			CrstBase::CrstHolderWithState holder (lock->lock);
			holder.SuppressRelease ();
		}
	}
	EX_CATCH
	{
		result = false;
	}
	EX_END_CATCH(SwallowAllExceptions);

	return result;
}

static
bool
ep_rt_lock_release (ep_rt_lock_handle_t *lock)
{
	STATIC_CONTRACT_NOTHROW;

	bool result = true;
	EX_TRY
	{
		if (lock) {
			CrstBase::UnsafeCrstInverseHolder holder (lock->lock);
			holder.SuppressRelease ();
		}
	}
	EX_CATCH
	{
		result = false;
	}
	EX_END_CATCH(SwallowAllExceptions);

	return result;
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_lock_requires_lock_held (const ep_rt_lock_handle_t *lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (((ep_rt_lock_handle_t *)lock)->lock->OwnedByCurrentThread ());
}

static
inline
void
ep_rt_lock_requires_lock_not_held (const ep_rt_lock_handle_t *lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (lock->lock == NULL || !((ep_rt_lock_handle_t *)lock)->lock->OwnedByCurrentThread ());
}
#endif

/*
* SpinLock.
*/

static
void
ep_rt_spin_lock_alloc (ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;

	EX_TRY
	{
		spin_lock->lock = new (nothrow) SpinLock ();
		spin_lock->lock->Init (LOCK_TYPE_DEFAULT);
	}
	EX_CATCH {}
	EX_END_CATCH(SwallowAllExceptions);
}

static
inline
void
ep_rt_spin_lock_free (ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;

	if (spin_lock && spin_lock->lock) {
		delete spin_lock->lock;
		spin_lock->lock = NULL;
	}
}

static
inline
bool
ep_rt_spin_lock_acquire (ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));

	SpinLock::AcquireLock (spin_lock->lock);
	return true;
}

static
inline
bool
ep_rt_spin_lock_release (ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));

	SpinLock::ReleaseLock (spin_lock->lock);
	return true;
}

#ifdef EP_CHECKED_BUILD
static
inline
void
ep_rt_spin_lock_requires_lock_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (ep_rt_spin_lock_is_valid (spin_lock));
	EP_ASSERT (spin_lock->lock->OwnedByCurrentThread ());
}

static
inline
void
ep_rt_spin_lock_requires_lock_not_held (const ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (spin_lock->lock == NULL || !spin_lock->lock->OwnedByCurrentThread ());
}
#endif

static
inline
bool
ep_rt_spin_lock_is_valid (const ep_rt_spin_lock_handle_t *spin_lock)
{
	STATIC_CONTRACT_NOTHROW;
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
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (str1 != NULL && str2 != NULL);

	return strcmp (reinterpret_cast<const char *>(str1), reinterpret_cast<const char *>(str2));
}

static
inline
int
ep_rt_utf8_string_compare_ignore_case (
	const ep_char8_t *str1,
	const ep_char8_t *str2)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (str1 != NULL && str2 != NULL);

	return _stricmp (reinterpret_cast<const char *>(str1), reinterpret_cast<const char *>(str2));
}

static
inline
bool
ep_rt_utf8_string_is_null_or_empty (const ep_char8_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (str == NULL)
		return true;

	while (*str) {
		if (!isspace (*str))
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
	STATIC_CONTRACT_NOTHROW;

	if (!str)
		return NULL;

	return _strdup (str);
}

static
inline
ep_char8_t *
ep_rt_utf8_string_dup_range (const ep_char8_t *str, const ep_char8_t *strEnd)
{
	ptrdiff_t byte_len = strEnd - str;
	ep_char8_t *buffer = reinterpret_cast<ep_char8_t *>(malloc(byte_len + 1));
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
	STATIC_CONTRACT_NOTHROW;
	return strtok_s (str, delimiter, context);
}

// STATIC_CONTRACT_NOTHROW
#undef ep_rt_utf8_string_snprintf
#define ep_rt_utf8_string_snprintf( \
	str, \
	str_len, \
	format, ...) \
sprintf_s (reinterpret_cast<char *>(str), static_cast<size_t>(str_len), reinterpret_cast<const char *>(format), __VA_ARGS__)

static
inline
bool
ep_rt_utf8_string_replace (
	ep_char8_t **str,
	const ep_char8_t *strSearch,
	const ep_char8_t *strReplacement
)
{
	STATIC_CONTRACT_NOTHROW;
	if ((*str) == NULL)
		return false;

	ep_char8_t* strFound = strstr(*str, strSearch);
	if (strFound != NULL)
	{
		size_t strSearchLen = strlen(strSearch);
		size_t newStrSize = strlen(*str) + strlen(strReplacement) - strSearchLen + 1;
		ep_char8_t *newStr =  reinterpret_cast<ep_char8_t *>(malloc(newStrSize));
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
ep_char16_t *
ep_rt_utf8_to_utf16le_string (
	const ep_char8_t *str,
	size_t len)
{
	STATIC_CONTRACT_NOTHROW;

	if (!str)
		return NULL;

	COUNT_T len_utf16 = WszMultiByteToWideChar (CP_UTF8, 0, str, static_cast<int>(len), 0, 0);
	if (len_utf16 == 0)
		return NULL;

	if (static_cast<int>(len) != -1)
		len_utf16 += 1;

	ep_char16_t *str_utf16 = reinterpret_cast<ep_char16_t *>(malloc (len_utf16 * sizeof (ep_char16_t)));
	if (!str_utf16)
		return NULL;

	len_utf16 = WszMultiByteToWideChar (CP_UTF8, 0, str, static_cast<int>(len), reinterpret_cast<LPWSTR>(str_utf16), len_utf16);
	if (len_utf16 == 0) {
		free (str_utf16);
		return NULL;
	}

	str_utf16 [len_utf16 - 1] = 0;
	return str_utf16;
}

static
inline
ep_char16_t *
ep_rt_utf16_string_dup (const ep_char16_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (!str)
		return NULL;

	size_t str_size = (ep_rt_utf16_string_len (str) + 1) * sizeof (ep_char16_t);
	ep_char16_t *str_dup = reinterpret_cast<ep_char16_t *>(malloc (str_size));
	if (str_dup)
		memcpy (str_dup, str, str_size);
	return str_dup;
}

static
inline
void
ep_rt_utf8_string_free (ep_char8_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (str)
		free (str);
}

static
inline
size_t
ep_rt_utf16_string_len (const ep_char16_t *str)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (str != NULL);

	return u16_strlen (reinterpret_cast<LPCWSTR>(str));
}

static
ep_char8_t *
ep_rt_utf16_to_utf8_string (
	const ep_char16_t *str,
	size_t len)
{
	STATIC_CONTRACT_NOTHROW;

	if (!str)
		return NULL;

	COUNT_T size_utf8 = WszWideCharToMultiByte (CP_UTF8, 0, reinterpret_cast<LPCWSTR>(str), static_cast<int>(len), NULL, 0, NULL, NULL);
	if (size_utf8 == 0)
		return NULL;

	if (static_cast<int>(len) != -1)
		size_utf8 += 1;

	ep_char8_t *str_utf8 = reinterpret_cast<ep_char8_t *>(malloc (size_utf8));
	if (!str_utf8)
		return NULL;

	size_utf8 = WszWideCharToMultiByte (CP_UTF8, 0, reinterpret_cast<LPCWSTR>(str), static_cast<int>(len), reinterpret_cast<LPSTR>(str_utf8), size_utf8, NULL, NULL);
	if (size_utf8 == 0) {
		free (str_utf8);
		return NULL;
	}

	str_utf8 [size_utf8 - 1] = 0;
	return str_utf8;
}

static
inline
ep_char8_t *
ep_rt_utf16le_to_utf8_string (
	const ep_char16_t *str,
	size_t len)
{
	return ep_rt_utf16_to_utf8_string (str, len);
}

static
inline
void
ep_rt_utf16_string_free (ep_char16_t *str)
{
	STATIC_CONTRACT_NOTHROW;

	if (str)
		free (str);
}

static
inline
const ep_char8_t *
ep_rt_managed_command_line_get (void)
{
	STATIC_CONTRACT_NOTHROW;
	EP_UNREACHABLE ("Can not reach here");

	return NULL;
}

static
const ep_char8_t *
ep_rt_diagnostics_command_line_get (void)
{
	STATIC_CONTRACT_NOTHROW;

	// In coreclr, this value can change over time, specifically before vs after suspension in diagnostics server.
	// The host initializes the runtime in two phases, init and exec assembly. On non-Windows platforms the commandline returned by the runtime
	// is different during each phase. We suspend during init where the runtime has populated the commandline with a
	// mock value (the full path of the executing assembly) and the actual value isn't populated till the exec assembly phase.
	// On Windows this does not apply as the value is retrieved directly from the OS any time it is requested.
	// As a result, we cannot actually cache this value. We need to return the _current_ value.
	// This function needs to handle freeing the string in order to make it consistent with Mono's version.
	// There is a rare chance this may be called on multiple threads, so we attempt to always return the newest value
	// and conservatively leak the old value if it changed. This is extremely rare and should only leak 1 string.
	extern ep_char8_t *volatile _ep_rt_coreclr_diagnostics_cmd_line;

	ep_char8_t *old_cmd_line = _ep_rt_coreclr_diagnostics_cmd_line;
	ep_char8_t *new_cmd_line = ep_rt_utf16_to_utf8_string (reinterpret_cast<const ep_char16_t *>(GetCommandLineForDiagnostics ()), -1);
	if (old_cmd_line && ep_rt_utf8_string_compare (old_cmd_line, new_cmd_line) == 0) {
		// same as old, so free the new one
		ep_rt_utf8_string_free (new_cmd_line);
	} else {
		// attempt an update, and give up if you lose the race
		if (ep_rt_atomic_compare_exchange_utf8_string (&_ep_rt_coreclr_diagnostics_cmd_line, old_cmd_line, new_cmd_line) != old_cmd_line) {
			ep_rt_utf8_string_free (new_cmd_line);
		}
		// NOTE: If there was a value we purposefully leak it since it may still be in use.
		// This leak is *small* (length of the command line) and bounded (should only happen once)
	}

	return _ep_rt_coreclr_diagnostics_cmd_line;
}

/*
 * Thread.
 */

static
inline
EventPipeThreadHolder *
thread_holder_alloc_func (void)
{
	STATIC_CONTRACT_NOTHROW;
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
	STATIC_CONTRACT_NOTHROW;
	if (thread_holder) {
		ep_thread_unregister (ep_thread_holder_get_thread (thread_holder));
		ep_thread_holder_free (thread_holder);
	}
}

class EventPipeCoreCLRThreadHolderTLS {
public:
	EventPipeCoreCLRThreadHolderTLS ()
		: m_threadHolder (NULL)
	{
		STATIC_CONTRACT_NOTHROW;
	}

	~EventPipeCoreCLRThreadHolderTLS ()
	{
		STATIC_CONTRACT_NOTHROW;

		if (m_threadHolder) {
			thread_holder_free_func (m_threadHolder);
			m_threadHolder = NULL;
		}
	}

	static inline EventPipeThreadHolder * getThreadHolder ()
	{
		STATIC_CONTRACT_NOTHROW;
		return g_threadHolderTLS.m_threadHolder;
	}

	static inline EventPipeThreadHolder * createThreadHolder ()
	{
		STATIC_CONTRACT_NOTHROW;

		if (g_threadHolderTLS.m_threadHolder) {
			thread_holder_free_func (g_threadHolderTLS.m_threadHolder);
			g_threadHolderTLS.m_threadHolder = NULL;
		}
		g_threadHolderTLS.m_threadHolder = thread_holder_alloc_func ();
		return g_threadHolderTLS.m_threadHolder;
	}

private:
	EventPipeThreadHolder *m_threadHolder;
	static thread_local EventPipeCoreCLRThreadHolderTLS g_threadHolderTLS;
};

static
void
ep_rt_thread_setup (void)
{
	STATIC_CONTRACT_NOTHROW;

	Thread* thread_handle = SetupThreadNoThrow ();
	EP_ASSERT (thread_handle != NULL);
}

static
inline
EventPipeThread *
ep_rt_thread_get (void)
{
	STATIC_CONTRACT_NOTHROW;

	EventPipeThreadHolder *thread_holder = EventPipeCoreCLRThreadHolderTLS::getThreadHolder ();
	return thread_holder ? ep_thread_holder_get_thread (thread_holder) : NULL;
}

static
inline
EventPipeThread *
ep_rt_thread_get_or_create (void)
{
	STATIC_CONTRACT_NOTHROW;

	EventPipeThreadHolder *thread_holder = EventPipeCoreCLRThreadHolderTLS::getThreadHolder ();
	if (!thread_holder)
		thread_holder = EventPipeCoreCLRThreadHolderTLS::createThreadHolder ();

	return ep_thread_holder_get_thread (thread_holder);
}

static
inline
ep_rt_thread_handle_t
ep_rt_thread_get_handle (void)
{
	STATIC_CONTRACT_NOTHROW;
	return GetThreadNULLOk ();
}

static
inline
ep_rt_thread_id_t
ep_rt_thread_get_id (ep_rt_thread_handle_t thread_handle)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (thread_handle != NULL);

	return ep_rt_uint64_t_to_thread_id_t (thread_handle->GetOSThreadId64 ());
}

static
inline
uint64_t
ep_rt_thread_id_t_to_uint64_t (ep_rt_thread_id_t thread_id)
{
	return static_cast<uint64_t>(thread_id);
}

static
inline
ep_rt_thread_id_t
ep_rt_uint64_t_to_thread_id_t (uint64_t thread_id)
{
	return static_cast<ep_rt_thread_id_t>(thread_id);
}

static
inline
bool
ep_rt_thread_has_started (ep_rt_thread_handle_t thread_handle)
{
	STATIC_CONTRACT_NOTHROW;
	return thread_handle != NULL && thread_handle->HasStarted ();
}

static
inline
ep_rt_thread_activity_id_handle_t
ep_rt_thread_get_activity_id_handle (void)
{
	STATIC_CONTRACT_NOTHROW;
	return GetThread ();
}

static
inline
const uint8_t *
ep_rt_thread_get_activity_id_cref (ep_rt_thread_activity_id_handle_t activity_id_handle)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (activity_id_handle != NULL);

	return reinterpret_cast<const uint8_t *>(activity_id_handle->GetActivityId ());
}

static
inline
void
ep_rt_thread_get_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	uint8_t *activity_id,
	uint32_t activity_id_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (activity_id_handle != NULL);
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	memcpy (activity_id, ep_rt_thread_get_activity_id_cref (activity_id_handle), EP_ACTIVITY_ID_SIZE);
}

static
inline
void
ep_rt_thread_set_activity_id (
	ep_rt_thread_activity_id_handle_t activity_id_handle,
	const uint8_t *activity_id,
	uint32_t activity_id_len)
{
	STATIC_CONTRACT_NOTHROW;
	EP_ASSERT (activity_id_handle != NULL);
	EP_ASSERT (activity_id != NULL);
	EP_ASSERT (activity_id_len == EP_ACTIVITY_ID_SIZE);

	activity_id_handle->SetActivityId (reinterpret_cast<LPCGUID>(activity_id));
}

#undef EP_YIELD_WHILE
#define EP_YIELD_WHILE(condition) YIELD_WHILE(condition)

/*
 * Volatile.
 */

static
inline
uint32_t
ep_rt_volatile_load_uint32_t (const volatile uint32_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoad<uint32_t> ((const uint32_t *)ptr);
}

static
inline
uint32_t
ep_rt_volatile_load_uint32_t_without_barrier (const volatile uint32_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoadWithoutBarrier<uint32_t> ((const uint32_t *)ptr);
}

static
inline
void
ep_rt_volatile_store_uint32_t (
	volatile uint32_t *ptr,
	uint32_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStore<uint32_t> ((uint32_t *)ptr, value);
}

static
inline
void
ep_rt_volatile_store_uint32_t_without_barrier (
	volatile uint32_t *ptr,
	uint32_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStoreWithoutBarrier<uint32_t>((uint32_t *)ptr, value);
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t (const volatile uint64_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoad<uint64_t> ((const uint64_t *)ptr);
}

static
inline
uint64_t
ep_rt_volatile_load_uint64_t_without_barrier (const volatile uint64_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoadWithoutBarrier<uint64_t> ((const uint64_t *)ptr);
}

static
inline
void
ep_rt_volatile_store_uint64_t (
	volatile uint64_t *ptr,
	uint64_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStore<uint64_t> ((uint64_t *)ptr, value);
}

static
inline
void
ep_rt_volatile_store_uint64_t_without_barrier (
	volatile uint64_t *ptr,
	uint64_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStoreWithoutBarrier<uint64_t> ((uint64_t *)ptr, value);
}

static
inline
int64_t
ep_rt_volatile_load_int64_t (const volatile int64_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoad<int64_t> ((int64_t *)ptr);
}

static
inline
int64_t
ep_rt_volatile_load_int64_t_without_barrier (const volatile int64_t *ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoadWithoutBarrier<int64_t> ((int64_t *)ptr);
}

static
inline
void
ep_rt_volatile_store_int64_t (
	volatile int64_t *ptr,
	int64_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStore<int64_t> ((int64_t *)ptr, value);
}

static
inline
void
ep_rt_volatile_store_int64_t_without_barrier (
	volatile int64_t *ptr,
	int64_t value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStoreWithoutBarrier<int64_t> ((int64_t *)ptr, value);
}

static
inline
void *
ep_rt_volatile_load_ptr (volatile void **ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoad<void *> ((void **)ptr);
}

static
inline
void *
ep_rt_volatile_load_ptr_without_barrier (volatile void **ptr)
{
	STATIC_CONTRACT_NOTHROW;
	return VolatileLoadWithoutBarrier<void *> ((void **)ptr);
}

static
inline
void
ep_rt_volatile_store_ptr (
	volatile void **ptr,
	void *value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStore<void *> ((void **)ptr, value);
}

static
inline
void
ep_rt_volatile_store_ptr_without_barrier (
	volatile void **ptr,
	void *value)
{
	STATIC_CONTRACT_NOTHROW;
	VolatileStoreWithoutBarrier<void *> ((void **)ptr, value);
}

#endif /* ENABLE_PERFTRACING */
#endif /* __EVENTPIPE_RT_CORECLR_H__ */
