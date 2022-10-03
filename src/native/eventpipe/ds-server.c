#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_SERVER_GETTER_SETTER
#include "ds-server.h"
#include "ds-ipc.h"
#include "ds-protocol.h"
#include "ds-process-protocol.h"
#include "ds-eventpipe-protocol.h"
#include "ds-dump-protocol.h"
#include "ds-profiler-protocol.h"
#include "ds-rt.h"

/*
 * Globals and volatile access functions.
 */

static volatile uint32_t _server_shutting_down_state = 0;
static ep_rt_wait_event_handle_t _server_resume_runtime_startup_event = { 0 };
static bool _server_disabled = false;
static volatile bool _is_paused_for_startup = false;

static
inline
bool
server_volatile_load_shutting_down_state (void)
{
	return (ep_rt_volatile_load_uint32_t (&_server_shutting_down_state) != 0) ? true : false;
}

static
inline
void
server_volatile_store_shutting_down_state (bool state)
{
	ep_rt_volatile_store_uint32_t (&_server_shutting_down_state, state ? 1 : 0);
}

/*
 * Forward declares of all static functions.
 */

static
void
server_error_callback_create (
	const ep_char8_t *message,
	uint32_t code);

static
void
server_error_callback_close (
	const ep_char8_t *message,
	uint32_t code);

static
void
server_warning_callback (
	const ep_char8_t *message,
	uint32_t code);

static
bool
server_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream);

/*
 * DiagnosticServer.
 */

static
void
server_error_callback_create (
	const ep_char8_t *message,
	uint32_t code)
{
	EP_ASSERT (message != NULL);
	DS_LOG_ERROR_2 ("Failed to create diagnostic IPC: error (%d): %s.", code, message);
}

static
void
server_error_callback_close (
	const ep_char8_t *message,
	uint32_t code)
{
	EP_ASSERT (message != NULL);
	DS_LOG_ERROR_2 ("Failed to close diagnostic IPC: error (%d): %s.", code, message);
}

static
bool
server_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	DS_LOG_WARNING_1 ("Received unknown request type (%d)", ds_ipc_header_get_commandset (ds_ipc_message_get_header_ref (message)));
	ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_COMMAND);
	ds_ipc_stream_free (stream);
	return true;
}

static
void
server_warning_callback (
	const ep_char8_t *message,
	uint32_t code)
{
	EP_ASSERT (message != NULL);
	DS_LOG_WARNING_2 ("warning (%d): %s.", code, message);
}

EP_RT_DEFINE_THREAD_FUNC (server_thread)
{
	EP_ASSERT (server_volatile_load_shutting_down_state () || ds_ipc_stream_factory_has_active_ports ());

	if (!ds_ipc_stream_factory_has_active_ports ()) {
#ifndef DS_IPC_DISABLE_LISTEN_PORTS
		DS_LOG_ERROR_0 ("Diagnostics IPC listener was undefined");
#endif
		return 1;
	}

	while (!server_volatile_load_shutting_down_state ()) {
		DiagnosticsIpcStream *stream = ds_ipc_stream_factory_get_next_available_stream (server_warning_callback);
		if (!stream)
			continue;

		ds_rt_auto_trace_signal ();

		DiagnosticsIpcMessage message;
		if (!ds_ipc_message_init (&message))
			continue;

		if (!ds_ipc_message_initialize_stream (&message, stream)) {
			ds_ipc_message_send_error (stream, DS_IPC_E_BAD_ENCODING);
			ds_ipc_stream_free (stream);
			ds_ipc_message_fini (&message);
			continue;
		}

		if (ep_rt_utf8_string_compare (
			(const ep_char8_t *)ds_ipc_header_get_magic_ref (ds_ipc_message_get_header_ref (&message)),
			(const ep_char8_t *)DOTNET_IPC_V1_MAGIC) != 0) {

			ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_MAGIC);
			ds_ipc_stream_free (stream);
			ds_ipc_message_fini (&message);
			continue;
		}

		DS_LOG_INFO_2 ("DiagnosticServer - received IPC message with command set (%d) and command id (%d)", ds_ipc_header_get_commandset (ds_ipc_message_get_header_ref (&message)), ds_ipc_header_get_commandid (ds_ipc_message_get_header_ref (&message)));

		switch ((DiagnosticsServerCommandSet)ds_ipc_header_get_commandset (ds_ipc_message_get_header_ref (&message))) {
		case DS_SERVER_COMMANDSET_EVENTPIPE:
			ds_eventpipe_protocol_helper_handle_ipc_message (&message, stream);
			break;
		case DS_SERVER_COMMANDSET_DUMP:
			ds_dump_protocol_helper_handle_ipc_message (&message, stream);
			break;
		case DS_SERVER_COMMANDSET_PROCESS:
			ds_process_protocol_helper_handle_ipc_message (&message, stream);
			break;
		case DS_SERVER_COMMANDSET_PROFILER:
			ds_profiler_protocol_helper_handle_ipc_message (&message, stream);
			break;
		default:
			server_protocol_helper_unknown_command (&message, stream);
			break;
		}

		ds_ipc_message_fini (&message);
	}

	return (ep_rt_thread_start_func_return_t)0;
}

void
ds_server_disable (void)
{
	_server_disabled = true;
}

bool
ds_server_init (void)
{
	if (!ds_ipc_stream_factory_init ())
		return false;

	if (_server_disabled || !ds_rt_config_value_get_enable ())
		return true;

	bool result = false;

	// Initialize PAL layer.
	if (!ds_ipc_pal_init ()) {
		DS_LOG_ERROR_1 ("Failed to initialize PAL layer (%d).", ep_rt_get_last_error ());
		ep_raise_error ();
	}

	// Initialize the RuntimeIdentifier before use
	ds_ipc_advertise_cookie_v1_init ();

	// Ports can fail to be configured
	if (!ds_ipc_stream_factory_configure (server_error_callback_create))
		DS_LOG_ERROR_0 ("At least one Diagnostic Port failed to be configured.");

	if (ds_ipc_stream_factory_any_suspended_ports ()) {
		ep_rt_wait_event_alloc (&_server_resume_runtime_startup_event, true, false);
		ep_raise_error_if_nok (ep_rt_wait_event_is_valid (&_server_resume_runtime_startup_event));
	}

	if (ds_ipc_stream_factory_has_active_ports ()) {
		ds_rt_auto_trace_init ();
		ds_rt_auto_trace_launch ();

		ep_rt_thread_id_t thread_id = ep_rt_uint64_t_to_thread_id_t (0);

		if (!ep_rt_thread_create ((void *)server_thread, NULL, EP_THREAD_TYPE_SERVER, (void *)&thread_id)) {
			// Failed to create IPC thread.
			ds_ipc_stream_factory_close_ports (NULL);
			DS_LOG_ERROR_1 ("Failed to create diagnostic server thread (%d).", ep_rt_get_last_error ());
			ep_raise_error ();
		} else {
			ds_rt_auto_trace_wait ();
		}
	}

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ep_exit_error_handler ();
}

bool
ds_server_shutdown (void)
{
	server_volatile_store_shutting_down_state (true);

	if (ds_ipc_stream_factory_has_active_ports ())
		ds_ipc_stream_factory_shutdown (server_error_callback_close);

	ds_ipc_stream_factory_fini ();
	ds_ipc_pal_shutdown ();
	return true;
}

// This method will block runtime bring-up IFF DOTNET_DefaultDiagnosticPortSuspend != NULL and DOTNET_DiagnosticPorts != 0 (it's default state)
// The _ds_resume_runtime_startup_event event will be signaled when the Diagnostics Monitor uses the ResumeRuntime Diagnostics IPC Command
void
ds_server_pause_for_diagnostics_monitor (void)
{
    _is_paused_for_startup = true;

	if (ds_ipc_stream_factory_any_suspended_ports ()) {
		EP_ASSERT (ep_rt_wait_event_is_valid (&_server_resume_runtime_startup_event));
		DS_LOG_ALWAYS_0 ("The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command.");

		if (ep_rt_wait_event_wait (&_server_resume_runtime_startup_event, 5000, false) != 0) {
			ds_rt_server_log_pause_message ();
			DS_LOG_ALWAYS_0 ("The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command and has waited 5 seconds.");
			ep_rt_wait_event_wait (&_server_resume_runtime_startup_event, EP_INFINITE_WAIT, false);
		}
	}

	// allow wait failures to fall through and the runtime to continue coming up
}

void
ds_server_resume_runtime_startup (void)
{
	ds_ipc_stream_factory_resume_current_port ();
	if (!ds_ipc_stream_factory_any_suspended_ports () && ep_rt_wait_event_is_valid (&_server_resume_runtime_startup_event)) {
		ep_rt_wait_event_set (&_server_resume_runtime_startup_event);
        _is_paused_for_startup = false;
	}
}

bool
ds_server_is_paused_in_startup (void)
{
	return _is_paused_for_startup;
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(DS_INCLUDE_SOURCE_FILES) && !defined(DS_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_diagnostics_server;
const char quiet_linker_empty_file_warning_diagnostics_server = 0;
#endif
