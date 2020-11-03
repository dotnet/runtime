#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ds-rt-config.h"

// Option to include all internal source files into ds-server.c.
#ifdef DS_INCLUDE_SOURCE_FILES
#define DS_FORCE_INCLUDE_SOURCE_FILES
#include "ds-ipc.c"
#ifdef HOST_WIN32
#include "ds-ipc-win32.c"
#else
#include "ds-ipc-posix.c"
#endif
#include "ds-protocol.c"
#include "ds-eventpipe-protocol.c"
#include "ds-process-protocol.c"
#include "ds-dump-protocol.c"
#include "ds-profiler-protocol.c"
#else
#define DS_IMPL_SERVER_GETTER_SETTER
#include "ds-server.h"
#include "ds-ipc.h"
#include "ds-protocol.h"
#include "ds-process-protocol.h"
#include "ds-eventpipe-protocol.h"
#include "ds-dump-protocol.h"
#include "ds-profiler-protocol.h"
#include "ep-stream.h"
#endif

#ifdef FEATURE_AUTO_TRACE
// TODO: Implement
#include "ds-autotrace.h"
#endif

/*
 * Globals and volatile access functions.
 */

static volatile uint32_t _server_shutting_down_state = 0;
static ep_rt_wait_event_handle_t _server_resume_runtime_startup_event = { 0 };
static bool _server_disabled = false;

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
void
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
	DS_LOG_ERROR_2 ("Failed to create diagnostic IPC: error (%d): %s.\n", code, message);
}

static
void
server_error_callback_close (
	const ep_char8_t *message,
	uint32_t code)
{
	EP_ASSERT (message != NULL);
	DS_LOG_ERROR_2 ("Failed to close diagnostic IPC: error (%d): %s.\n", code, message);
}

static
void
server_protocol_helper_unknown_command (
	DiagnosticsIpcMessage *message,
	DiagnosticsIpcStream *stream)
{
	DS_LOG_WARNING_1 ("Received unknown request type (%d)\n", ds_ipc_message_header_get_commandset (ds_ipc_message_get_header (&message)));
	ds_ipc_message_send_error (stream, DS_IPC_E_UNKNOWN_COMMAND);
	ds_ipc_stream_free (stream);
}

static
void
server_warning_callback (
	const ep_char8_t *message,
	uint32_t code)
{
	EP_ASSERT (message != NULL);
	DS_LOG_WARNING_2 ("warning (%d): %s.\n", code, message);
}

EP_RT_DEFINE_THREAD_FUNC (server_thread)
{
	EP_ASSERT (server_volatile_load_shutting_down_state () == true || ds_ipc_stream_factory_has_active_ports () == true);

	if (!ds_ipc_stream_factory_has_active_ports ()) {
		DS_LOG_ERROR_0 ("Diagnostics IPC listener was undefined\n");
		return 1;
	}

	ep_rt_thread_setup (true);

	while (!server_volatile_load_shutting_down_state ()) {
		DiagnosticsIpcStream *stream = ds_ipc_stream_factory_get_next_available_stream (server_warning_callback);
		if (!stream)
			continue;

#ifdef FEATURE_AUTO_TRACE
		// TODO: Implement
		auto_trace_signal();
#endif

		DiagnosticsIpcMessage message;
		ds_ipc_message_init (&message);
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

		DS_LOG_INFO_2 ("DiagnosticServer - received IPC message with command set (%d) and command id (%d)\n", ds_ipc_message_header_get_commandset (ds_ipc_message_get_header (&message)), ds_ipc_header_get_commandid (ds_ipc_message_get_header (&message)));

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
#ifdef FEATURE_PROFAPI_ATTACH_DETACH
		case DS_SERVER_COMMANDSET_PROFILER:
			ds_profiler_protocol_helper_handle_ipc_message (&message, stream);
			break;
#endif // FEATURE_PROFAPI_ATTACH_DETACH
		default:
			server_protocol_helper_unknown_command (&message, stream);
			break;
		}

		ds_ipc_message_fini (&message);
	}

	ep_rt_thread_teardown ();

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
	ds_ipc_stream_factory_init ();

	if (_server_disabled || !ds_rt_config_value_get_enable ())
		return true;

	bool success = false;

	// Initialize the RuntimeIndentifier before use
	ds_ipc_advertise_cookie_v1_init ();

	// Ports can fail to be configured
	bool any_errors = !ds_ipc_stream_factory_configure (server_error_callback_create);
	if (any_errors)
		DS_LOG_ERROR_0 ("At least one Diagnostic Port failed to be configured.\n");

	if (ds_ipc_stream_factory_any_suspended_ports ())
		ep_rt_wait_event_alloc (&_server_resume_runtime_startup_event, true, false);

	if (ds_ipc_stream_factory_has_active_ports ()) {
#ifdef FEATURE_AUTO_TRACE
		// TODO: Implement.
		auto_trace_init();
		auto_trace_launch();
#endif
		ep_rt_thread_id_t thread_id = 0;

		if (!ep_rt_thread_create ((void *)server_thread, NULL, (void *)&thread_id)) {
			// Failed to create IPC thread.
			ds_ipc_stream_factory_close_ports (NULL);
			DS_LOG_ERROR_1 ("Failed to create diagnostic server thread (%d).\n", ep_rt_get_last_error ());
			ep_raise_error ();
		} else {
#ifdef FEATURE_AUTO_TRACE
			// TODO: Implement.
			auto_trace_wait();
#endif
			success = true;
		}
	}

ep_on_exit:
	return success;

ep_on_error:
	success = false;
	ep_exit_error_handler ();
}

bool
ds_server_shutdown (void)
{
	server_volatile_store_shutting_down_state (true);

	if (ds_ipc_stream_factory_has_active_ports ())
		ds_ipc_stream_factory_shutdown (server_error_callback_close);

	ds_ipc_stream_factory_fini ();
	return true;
}

// This method will block runtime bring-up IFF DOTNET_DefaultDiagnosticPortSuspend != NULL and DOTNET_DiagnosticPorts != 0 (it's default state)
// The _ds_resume_runtime_startup_event event will be signaled when the Diagnostics Monitor uses the ResumeRuntime Diagnostics IPC Command
void
ds_server_pause_for_diagnostics_monitor (void)
{
	ep_char8_t *ports = NULL;
	wchar_t *ports_wcs = NULL;
	int32_t port_suspended = 0;

	if (ds_ipc_stream_factory_any_suspended_ports ()) {
		EP_ASSERT (ep_rt_wait_event_is_valid (&_server_resume_runtime_startup_event));
		DS_LOG_ALWAYS_0 ("The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command.");
		if (ep_rt_wait_event_wait (&_server_resume_runtime_startup_event, 5000, false) != 0) {
			ports = ds_rt_config_value_get_ports ();
			ports_wcs = ports ? ep_rt_utf8_to_wcs_string (ports, -1) : NULL;
			port_suspended = ds_rt_config_value_get_default_port_suspend ();

			printf ("The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command from a Diagnostic Port.\n");
			printf ("DOTNET_DiagnosticPorts=\"%ls\"\n", ports_wcs == NULL ? L"" : ports_wcs);
			printf("DOTNET_DefaultDiagnosticPortSuspend=%d\n", port_suspended);
			fflush (stdout);

			DS_LOG_ALWAYS_0 ("The runtime has been configured to pause during startup and is awaiting a Diagnostics IPC ResumeStartup command and has waited 5 seconds.");
			ep_rt_wait_event_wait (&_server_resume_runtime_startup_event, EP_INFINITE_WAIT, false);
		}
	}

	// allow wait failures to fall through and the runtime to continue coming up

	ep_rt_wcs_string_free (ports_wcs);
	ep_rt_utf8_string_free (ports);
}

void
ds_server_resume_runtime_startup (void)
{
	ds_ipc_stream_factory_resume_current_port ();
	if (!ds_ipc_stream_factory_any_suspended_ports () && ep_rt_wait_event_is_valid (&_server_resume_runtime_startup_event))
		ep_rt_wait_event_set (&_server_resume_runtime_startup_event);
}

#endif /* ENABLE_PERFTRACING */

extern const char quiet_linker_empty_file_warning_diagnostics_server;
const char quiet_linker_empty_file_warning_diagnostics_server = 0;
