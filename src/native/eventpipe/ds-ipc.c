#include "ds-rt-config.h"

#ifdef ENABLE_PERFTRACING
#if !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES)

#define DS_IMPL_IPC_GETTER_SETTER
#define DS_IMPL_IPC_PAL_GETTER_SETTER
#include "ds-ipc.h"
#include "ds-protocol.h"
#include "ep.h"
#include "ds-rt.h"

/*
 * Globals and volatile access functions.
 */

static volatile uint32_t _ds_shutting_down_state = 0;
static ds_rt_port_array_t _ds_port_array = { 0 };

// set this in get_next_available_stream, and then expose a callback that
// allows us to track which connections have sent their ResumeRuntime commands
static DiagnosticsPort *_ds_current_port = NULL;

static const uint32_t _ds_default_poll_handle_array_size = 16;

#define NUM_NANOSECONDS_IN_1_MS 1000000

static
inline
bool
load_shutting_down_state (void)
{
	return (ep_rt_volatile_load_uint32_t (&_ds_shutting_down_state) != 0) ? true : false;
}

static
inline
void
store_shutting_down_state (bool state)
{
	ep_rt_volatile_store_uint32_t (&_ds_shutting_down_state, state ? 1 : 0);
}

/*
 * Forward declares of all static functions.
 */

static
uint32_t
ipc_stream_factory_get_next_timeout (uint32_t current_timout_ms);

static
void
ipc_stream_factory_split_port_config (
	ep_char8_t *config,
	const ep_char8_t *delimiters,
	ds_rt_port_config_array_t *config_array);

static
bool
ipc_stream_factory_build_and_add_port (
	DiagnosticsPortBuilder *builder,
	ds_ipc_error_callback_func callback,
	bool default_port);

static
void
ipc_log_poll_handles (ds_rt_ipc_poll_handle_array_t *ipc_poll_handles);

static
void
connect_port_free_func (void *object);

static
bool
connect_port_get_ipc_poll_handle_func (
	void *object,
	DiagnosticsIpcPollHandle *handle,
	ds_ipc_error_callback_func callback);

static
DiagnosticsIpcStream *
connect_port_get_connected_stream_func (
	void *object,
	ds_ipc_error_callback_func callback);

static
void
connect_port_reset (
	void *object,
	ds_ipc_error_callback_func callback);

static
void
listen_port_free_func (void *object);

static
bool
listen_port_get_ipc_poll_handle_func (
	void *object,
	DiagnosticsIpcPollHandle *handle,
	ds_ipc_error_callback_func callback);

static
DiagnosticsIpcStream *
listen_port_get_connected_stream_func (
	void *object,
	ds_ipc_error_callback_func callback);

static
void
listen_port_reset (
	void *object,
	ds_ipc_error_callback_func callback);

/*
 * IpcStreamFactory.
 */

static
inline
uint32_t
ipc_stream_factory_get_next_timeout (uint32_t current_timeout_ms)
{
	if (current_timeout_ms == DS_IPC_TIMEOUT_INFINITE)
		return DS_IPC_POLL_TIMEOUT_MIN_MS;
	else
		return (current_timeout_ms >= DS_IPC_POLL_TIMEOUT_MAX_MS) ?
			DS_IPC_POLL_TIMEOUT_MAX_MS :
			(uint32_t)((float)current_timeout_ms * DS_IPC_POLL_TIMEOUT_FALLOFF_FACTOR);
}

static
void
ipc_stream_factory_split_port_config (
	ep_char8_t *config,
	const ep_char8_t *delimiters,
	ds_rt_port_config_array_t *config_array)
{
	ep_char8_t *part = NULL;
	ep_char8_t *context = NULL;
	ep_char8_t *cursor = config;

	EP_ASSERT (config != NULL);
	EP_ASSERT (delimiters != NULL);
	EP_ASSERT (config_array != NULL);

	part = ep_rt_utf8_string_strtok (cursor, delimiters, &context);
	while (part) {
		ds_rt_port_config_array_append (config_array, part);
		part = ep_rt_utf8_string_strtok (NULL, delimiters, &context);
	}
}

static
bool
ipc_stream_factory_build_and_add_port (
	DiagnosticsPortBuilder *builder,
	ds_ipc_error_callback_func callback,
	bool default_port)
{
	EP_ASSERT (builder != NULL);
	EP_ASSERT (callback != NULL);

	bool result = false;
	DiagnosticsIpc *ipc = NULL;

#ifndef DS_IPC_DISABLE_DEFAULT_LISTEN_PORT
	if (!default_port && builder->type == DS_PORT_TYPE_LISTEN) {
		// Ignore listen type (see conversation in https://github.com/dotnet/runtime/pull/40499 for details)
		DS_LOG_INFO_0 ("ipc_stream_factory_build_and_add_port - Ignoring LISTEN port configuration");
		return true;
	}
#endif

	if (builder->type == DS_PORT_TYPE_LISTEN) {
#ifndef DS_IPC_DISABLE_LISTEN_PORTS
		ipc = ds_ipc_alloc (builder->path, DS_IPC_CONNECTION_MODE_LISTEN, callback);
		ep_raise_error_if_nok (ipc != NULL);
		ep_raise_error_if_nok (ds_ipc_listen (ipc, callback));
		ep_raise_error_if_nok (ds_rt_port_array_append (&_ds_port_array, (DiagnosticsPort *)ds_listen_port_alloc (ipc, builder)));
#else
		DS_LOG_INFO_0 ("ipc_stream_factory_build_and_add_port - LISTEN ports disabled");
		ep_raise_error ();
#endif
	} else if (builder->type == DS_PORT_TYPE_CONNECT) {
#ifndef DS_IPC_DISABLE_CONNECT_PORTS
		ipc = ds_ipc_alloc (builder->path, DS_IPC_CONNECTION_MODE_CONNECT, callback);
		ep_raise_error_if_nok (ipc != NULL);
		ep_raise_error_if_nok (ds_rt_port_array_append (&_ds_port_array, (DiagnosticsPort *)ds_connect_port_alloc (ipc, builder)));
#else
		DS_LOG_INFO_0 ("ipc_stream_factory_build_and_add_port - CONNECT ports disabled");
		ep_raise_error ();
#endif
	}

	result = true;

ep_on_exit:
	return result;

ep_on_error:
	EP_ASSERT (!result);
	ds_ipc_free (ipc);
	ep_exit_error_handler ();
}

static
void
ipc_log_poll_handles (ds_rt_ipc_poll_handle_array_t *ipc_poll_handles)
{
	// TODO: Should this be debug only?
	DiagnosticsIpcPollHandle ipc_poll_handle;
	ep_char8_t buffer [DS_IPC_MAX_TO_STRING_LEN];
	uint32_t connection_id = 0;

	ds_rt_ipc_poll_handle_array_iterator_t ipc_poll_handles_iterator = ds_rt_ipc_poll_handle_array_iterator_begin (ipc_poll_handles);
	while (!ds_rt_ipc_poll_handle_array_iterator_end (ipc_poll_handles, &ipc_poll_handles_iterator)) {
		ipc_poll_handle = ds_rt_ipc_poll_handle_array_iterator_value (&ipc_poll_handles_iterator);
		if (ipc_poll_handle.ipc) {
			if (!(ds_ipc_to_string (ipc_poll_handle.ipc, buffer, (uint32_t)ARRAY_SIZE (buffer)) > 0))
				buffer [0] = '\0';
			DS_LOG_DEBUG_2 ("\tSERVER IpcPollHandle[%d] = %s", connection_id, buffer);
		} else {
			if (!(ds_ipc_stream_to_string (ipc_poll_handle.stream, buffer, (uint32_t)ARRAY_SIZE (buffer))))
				buffer [0] = '\0';
			DS_LOG_DEBUG_2 ("\tCLIENT IpcPollHandle[%d] = %s", connection_id, buffer);
		}
		ds_rt_ipc_poll_handle_array_iterator_next (&ipc_poll_handles_iterator);
		connection_id++;
	}
}

bool
ds_ipc_stream_factory_init (void)
{
	ep_ipc_stream_factory_callback_set (ds_ipc_stream_factory_any_suspended_ports);

	ds_rt_port_array_alloc (&_ds_port_array);
	return ds_rt_port_array_is_valid (&_ds_port_array);
}

void
ds_ipc_stream_factory_fini (void)
{
	// TODO: Race between server thread and shutdown, _ds_port_array and ports can not be freed without resolving
	// that race first. Diagnostic server thread is currently designed to not break waits on
	// shutdown unless clients activity wakes server thread.
	/*ds_rt_port_array_iterator_t iterator = ds_rt_port_array_iterator_begin (&_ds_port_array);
	while (!ds_rt_port_array_iterator_end (&_ds_port_array, &iterator)) {
		ds_port_free_vcall (ds_rt_port_array_iterator_value (&iterator));
		ds_rt_port_array_iterator_next (&iterator);
	}

	ds_rt_port_array_free (&_ds_port_array);*/

	ep_ipc_stream_factory_callback_set (NULL);
}

bool
ds_ipc_stream_factory_configure (ds_ipc_error_callback_func callback)
{
	bool result = true;

	ep_char8_t *ports = ds_rt_config_value_get_ports ();
	if (ports) {
		DS_RT_DECLARE_LOCAL_PORT_CONFIG_ARRAY (port_configs);
		DS_RT_DECLARE_LOCAL_PORT_CONFIG_ARRAY (port_config_parts);

		ds_rt_port_config_array_init (&port_configs);
		ds_rt_port_config_array_init (&port_config_parts);

		if (ds_rt_port_config_array_is_valid (&port_configs) && ds_rt_port_config_array_is_valid (&port_config_parts)) {
			ipc_stream_factory_split_port_config (ports, ";", &port_configs);
			ds_rt_port_config_array_reverse_iterator_t port_configs_iterator = ds_rt_port_config_array_reverse_iterator_begin (&port_configs);
			while (!ds_rt_port_config_array_reverse_iterator_end(&port_configs, &port_configs_iterator)) {
				ep_char8_t *port_config = ds_rt_port_config_array_reverse_iterator_value (&port_configs_iterator);
				DS_LOG_INFO_1 ("ds_ipc_stream_factory_configure - Attempted to create Diagnostic Port from \"%s\".", port_config ? port_config : "");
				if (port_config) {
					ds_rt_port_config_array_clear (&port_config_parts);
					ipc_stream_factory_split_port_config (port_config, ",", &port_config_parts);

					size_t port_config_parts_index = ds_rt_port_config_array_size (&port_config_parts);
					if (port_config_parts_index != 0) {
						DiagnosticsPortBuilder port_builder;
						if (ds_port_builder_init (&port_builder)) {
							ds_rt_port_config_array_reverse_iterator_t port_config_parts_iterator = ds_rt_port_config_array_reverse_iterator_begin (&port_config_parts);
							while (!ds_rt_port_config_array_reverse_iterator_end(&port_config_parts, &port_config_parts_iterator)) {
								if (port_config_parts_index == 1)
									port_builder.path = ds_rt_port_config_array_reverse_iterator_value (&port_config_parts_iterator);
								else
									ds_port_builder_set_tag (&port_builder, ds_rt_port_config_array_reverse_iterator_value (&port_config_parts_iterator));
								ds_rt_port_config_array_reverse_iterator_next (&port_config_parts_iterator);
								port_config_parts_index--;
							}
							if (!ep_rt_utf8_string_is_null_or_empty (port_builder.path)) {
									const bool build_success = ipc_stream_factory_build_and_add_port (&port_builder, callback, false);
									DS_LOG_INFO_1 ("ds_ipc_stream_factory_configure - Diagnostic Port creation %s", build_success ? "succeeded" : "failed");
									result &= build_success;
							} else {
								DS_LOG_INFO_0("ds_ipc_stream_factory_configure - Ignoring port configuration with empty address");
							}
							ds_port_builder_fini (&port_builder);
						} else {
							result &= false;
						}
					} else {
						result &= false;
					}
				}
				ds_rt_port_config_array_reverse_iterator_next (&port_configs_iterator);
			}
		} else {
			result &= false;
		}

		ds_rt_port_config_array_fini (&port_config_parts);
		ds_rt_port_config_array_fini (&port_configs);
		ep_rt_utf8_string_free (ports);
	}

#ifndef DS_IPC_DISABLE_DEFAULT_LISTEN_PORT
	// create the default listen port
	uint32_t port_suspend = ds_rt_config_value_get_default_port_suspend ();

	DiagnosticsPortBuilder default_port_builder;
	if (ds_port_builder_init (&default_port_builder)) {
		default_port_builder.path = NULL;
		default_port_builder.suspend_mode = port_suspend > 0 ? DS_PORT_SUSPEND_MODE_SUSPEND : DS_PORT_SUSPEND_MODE_NOSUSPEND;
		default_port_builder.type = DS_PORT_TYPE_LISTEN;

		result &= ipc_stream_factory_build_and_add_port (&default_port_builder, callback, true);

		ds_port_builder_fini (&default_port_builder);
	} else {
		result &= false;
	}
#else
	DS_LOG_DEBUG_0 ("ds_ipc_stream_factory_configure - Ignoring default LISTEN port");
#endif

	return result;
}

// Polling timeout semantics
// If client connection is opted in
//   and connection succeeds => set timeout to infinite
//   and connection fails => set timeout to minimum and scale by falloff factor
// else => set timeout to (uint32_t)-1 (infinite)
//
// If an agent closes its socket while we're still connected,
// Poll will return and let us know which connection hung up

DiagnosticsIpcStream *
ds_ipc_stream_factory_get_next_available_stream (ds_ipc_error_callback_func callback)
{
	DS_LOG_DEBUG_0 ("ds_ipc_stream_factory_get_next_available_stream - ENTER");

	DiagnosticsIpcStream *stream = NULL;
	DiagnosticsIpcPollHandle ipc_poll_handle;
	ds_rt_port_array_t *ports = &_ds_port_array;
	DiagnosticsPort *port = NULL;

	uint32_t poll_timeout_ms = DS_IPC_TIMEOUT_INFINITE;
	bool connect_success = true;
	uint32_t poll_attempts = 0;

	DS_RT_DECLARE_LOCAL_IPC_POLL_HANDLE_ARRAY (ipc_poll_handles);

	ds_rt_ipc_poll_handle_array_init_capacity (&ipc_poll_handles, _ds_default_poll_handle_array_size);
	ep_raise_error_if_nok (ds_rt_ipc_poll_handle_array_is_valid (&ipc_poll_handles));

	while (!stream) {
		connect_success = true;
		ds_rt_port_array_iterator_t ports_iterator = ds_rt_port_array_iterator_begin (ports);
		while (!ds_rt_port_array_iterator_end (ports, &ports_iterator)) {
			port = ds_rt_port_array_iterator_value (&ports_iterator);
			if (ds_port_get_ipc_poll_handle_vcall (port, &ipc_poll_handle, callback))
				ep_raise_error_if_nok (ds_rt_ipc_poll_handle_array_append (&ipc_poll_handles, ipc_poll_handle));
			else
				connect_success = false;

			ds_rt_port_array_iterator_next (&ports_iterator);
		}

		poll_timeout_ms = connect_success ?
			DS_IPC_TIMEOUT_INFINITE :
			ipc_stream_factory_get_next_timeout (poll_timeout_ms);

		int32_t ret_val;
		if ( ds_rt_ipc_poll_handle_array_size (&ipc_poll_handles) > 0) {
			poll_attempts++;
			DS_LOG_DEBUG_2 ("ds_ipc_stream_factory_get_next_available_stream - Poll attempt: %d, timeout: %dms.", poll_attempts, poll_timeout_ms);
			ipc_log_poll_handles (&ipc_poll_handles);
			ret_val = ds_ipc_poll (ds_rt_ipc_poll_handle_array_data (&ipc_poll_handles), ds_rt_ipc_poll_handle_array_size (&ipc_poll_handles), poll_timeout_ms, callback);
		} else {
			if (poll_timeout_ms == DS_IPC_TIMEOUT_INFINITE)
				poll_timeout_ms = DS_IPC_POLL_TIMEOUT_MAX_MS;
			DS_LOG_DEBUG_1 ("ds_ipc_stream_factory_get_next_available_stream - Nothing to poll, sleeping using timeout: %dms.", poll_timeout_ms);
			ep_rt_thread_sleep ((uint64_t)poll_timeout_ms * NUM_NANOSECONDS_IN_1_MS);
			ret_val = 0; // timeout
		}

		bool saw_error = false;

		if (ret_val != 0) {
			uint32_t connection_id = 0;
			ds_rt_ipc_poll_handle_array_iterator_t ipc_poll_handles_iterator = ds_rt_ipc_poll_handle_array_iterator_begin (&ipc_poll_handles);
			while (!ds_rt_ipc_poll_handle_array_iterator_end (&ipc_poll_handles, &ipc_poll_handles_iterator)) {
				ipc_poll_handle = ds_rt_ipc_poll_handle_array_iterator_value (&ipc_poll_handles_iterator);
				port = (DiagnosticsPort *)ipc_poll_handle.user_data;
				switch (ipc_poll_handle.events) {
				case DS_IPC_POLL_EVENTS_HANGUP:
					EP_ASSERT (port != NULL);
					ds_port_reset_vcall (port, callback);
					DS_LOG_INFO_2 ("ds_ipc_stream_factory_get_next_available_stream - HUP :: Poll attempt: %d, connection %d hung up. Connect is reset.", poll_attempts, connection_id);
					poll_timeout_ms = DS_IPC_POLL_TIMEOUT_MIN_MS;
					break;
				case DS_IPC_POLL_EVENTS_SIGNALED:
					EP_ASSERT (port != NULL);
					if (!stream) {  // only use first signaled stream; will get others on subsequent calls
						stream = ds_port_get_connected_stream_vcall (port, callback);
						if (!stream)
							saw_error = true;
						_ds_current_port = port;
					}
					DS_LOG_DEBUG_2 ("ds_ipc_stream_factory_get_next_available_stream - SIG :: Poll attempt: %d, connection %d signalled.", poll_attempts, connection_id);
					break;
				case DS_IPC_POLL_EVENTS_ERR:
					ds_port_reset_vcall ((DiagnosticsPort *)ipc_poll_handle.user_data, callback);
					DS_LOG_INFO_2 ("ds_ipc_stream_factory_get_next_available_stream - ERR :: Poll attempt: %d, connection %d errored. Connection is reset.", poll_attempts, connection_id);
					saw_error = true;
					break;
				case DS_IPC_POLL_EVENTS_NONE:
					DS_LOG_INFO_2 ("ds_ipc_stream_factory_get_next_available_stream - NON :: Poll attempt: %d, connection %d had no events.", poll_attempts, connection_id);
					break;
				default:
					DS_LOG_INFO_2 ("ds_ipc_stream_factory_get_next_available_stream - UNK :: Poll attempt: %d, connection %d had invalid PollEvent.", poll_attempts, connection_id);
					saw_error = true;
					break;
				}

				ds_rt_ipc_poll_handle_array_iterator_next (&ipc_poll_handles_iterator);
				connection_id++;
			}
		}

		if (!stream && saw_error) {
			_ds_current_port = NULL;
			ep_raise_error ();
		}

		// clear the view.
		ds_rt_ipc_poll_handle_array_clear (&ipc_poll_handles);
	}

ep_on_exit:
	DS_LOG_DEBUG_2 ("ds_ipc_stream_factory_get_next_available_stream - EXIT :: Poll attempt: %d, stream using handle %d.", poll_attempts, stream ? ds_ipc_stream_get_handle_int32_t (stream) : -1);
	ds_rt_ipc_poll_handle_array_fini (&ipc_poll_handles);
	return stream;

ep_on_error:
	stream = NULL;
	ep_exit_error_handler ();
}

void
ds_ipc_stream_factory_resume_current_port (void)
{
	if (_ds_current_port != NULL)
		_ds_current_port->has_resumed_runtime = true;
}

bool
ds_ipc_stream_factory_any_suspended_ports (void)
{
	bool any_suspended_ports = false;
	ds_rt_port_array_iterator_t iterator = ds_rt_port_array_iterator_begin (&_ds_port_array);
	while (!ds_rt_port_array_iterator_end (&_ds_port_array, &iterator)) {
		DiagnosticsPort *port = ds_rt_port_array_iterator_value (&iterator);
		any_suspended_ports |= !(port->suspend_mode == DS_PORT_SUSPEND_MODE_NOSUSPEND || port->has_resumed_runtime);
		ds_rt_port_array_iterator_next (&iterator);
	}
	return any_suspended_ports;
}

bool
ds_ipc_stream_factory_has_active_ports (void)
{
	return !load_shutting_down_state () &&
		ds_rt_port_array_size (&_ds_port_array) > 0;
}

void
ds_ipc_stream_factory_close_ports (ds_ipc_error_callback_func callback)
{
	ds_rt_port_array_iterator_t iterator = ds_rt_port_array_iterator_begin (&_ds_port_array);
	while (!ds_rt_port_array_iterator_end (&_ds_port_array, &iterator)) {
		ds_port_close (ds_rt_port_array_iterator_value (&iterator), false, callback);
		ds_rt_port_array_iterator_next (&iterator);
	}
}

bool
ds_ipc_stream_factory_shutdown (ds_ipc_error_callback_func callback)
{
	if (load_shutting_down_state ())
		return true;

	store_shutting_down_state (true);

	ds_rt_port_array_iterator_t iterator = ds_rt_port_array_iterator_begin (&_ds_port_array);
	while (!ds_rt_port_array_iterator_end (&_ds_port_array, &iterator)) {
		ds_port_close (ds_rt_port_array_iterator_value (&iterator), true, callback);
		ds_rt_port_array_iterator_next (&iterator);
	}

	_ds_current_port = NULL;
	return true;
}

/*
 * DiagnosticsPort.
 */

DiagnosticsPort *
ds_port_init (
	DiagnosticsPort *port,
	DiagnosticsPortVtable *vtable,
	DiagnosticsIpc *ipc,
	DiagnosticsPortBuilder *builder)
{
	EP_ASSERT (port != NULL);
	EP_ASSERT (vtable != NULL);
	EP_ASSERT (ipc != NULL);
	EP_ASSERT (builder != NULL);

	port->vtable = vtable;
	port->suspend_mode = builder->suspend_mode;
	port->type = builder->type;
	port->ipc = ipc;
	port->stream = NULL;
	port->has_resumed_runtime = false;

	return port;
}

void
ds_port_fini (DiagnosticsPort *port)
{
	return;
}

void
ds_port_free_vcall (DiagnosticsPort *port)
{
	ep_return_void_if_nok (port != NULL);

	EP_ASSERT (port->vtable != NULL);
	DiagnosticsPortVtable *vtable = port->vtable;

	EP_ASSERT (vtable->free_func != NULL);
	vtable->free_func (port);
}

bool
ds_port_get_ipc_poll_handle_vcall (
	DiagnosticsPort *port,
	DiagnosticsIpcPollHandle *handle,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (port != NULL);
	EP_ASSERT (port->vtable != NULL);

	DiagnosticsPortVtable *vtable = port->vtable;

	EP_ASSERT (vtable->get_ipc_poll_handle_func != NULL);
	return vtable->get_ipc_poll_handle_func (port, handle, callback);
}

DiagnosticsIpcStream *
ds_port_get_connected_stream_vcall (
	DiagnosticsPort *port,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (port != NULL);
	EP_ASSERT (port->vtable != NULL);

	DiagnosticsPortVtable *vtable = port->vtable;

	EP_ASSERT (vtable->get_connected_stream_func != NULL);
	return vtable->get_connected_stream_func (port, callback);
}

void
ds_port_reset_vcall (
	DiagnosticsPort *port,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (port != NULL);
	EP_ASSERT (port->vtable != NULL);

	DiagnosticsPortVtable *vtable = port->vtable;

	EP_ASSERT (vtable->reset_func != NULL);
	vtable->reset_func (port, callback);
}

void
ds_port_close (
	DiagnosticsPort *port,
	bool is_shutdown,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (port != NULL);
	if (port->ipc)
		ds_ipc_close (port->ipc, is_shutdown, callback);
	if (port->stream && !is_shutdown)
		ds_ipc_stream_close (port->stream, callback);
}

/*
 * DiagnosticsPortBuilder.
 */

DiagnosticsPortBuilder *
ds_port_builder_init (DiagnosticsPortBuilder *builder)
{
	EP_ASSERT (builder != NULL);
	builder->path = NULL;
	builder->suspend_mode = DS_PORT_SUSPEND_MODE_SUSPEND;
	builder->type = DS_PORT_TYPE_CONNECT;

	return builder;
}

void
ds_port_builder_fini (DiagnosticsPortBuilder *builder)
{
	return;
}

void
ds_port_builder_set_tag (
	DiagnosticsPortBuilder *builder,
	ep_char8_t *tag)
{
	EP_ASSERT (builder != NULL);
	EP_ASSERT (tag != NULL);

	if (ep_rt_utf8_string_compare_ignore_case (tag, "listen") == 0)
		builder->type = DS_PORT_TYPE_LISTEN;
	else if (ep_rt_utf8_string_compare_ignore_case (tag, "connect") == 0)
		builder->type = DS_PORT_TYPE_CONNECT;
	else if (ep_rt_utf8_string_compare_ignore_case (tag, "nosuspend") == 0)
		builder->suspend_mode = DS_PORT_SUSPEND_MODE_NOSUSPEND;
	else if (ep_rt_utf8_string_compare_ignore_case (tag, "suspend") == 0)
		builder->suspend_mode = DS_PORT_SUSPEND_MODE_SUSPEND;
	else
		// don't mutate if it's not a valid option
		DS_LOG_INFO_1 ("ds_port_builder_set_tag - Unknown tag '%s'.", tag);
}

/*
 * DiagnosticsConnectPort.
 */

static
void
connect_port_free_func (void *object)
{
	EP_ASSERT (object != NULL);
	ds_connect_port_free ((DiagnosticsConnectPort *)object);
}

static
bool
connect_port_get_ipc_poll_handle_func (
	void *object,
	DiagnosticsIpcPollHandle *handle,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (object != NULL);
	EP_ASSERT (handle != NULL);

	bool success = false;
	DiagnosticsConnectPort *connect_port = (DiagnosticsConnectPort *)object;
	DiagnosticsIpcStream *connection = NULL;

	DS_LOG_DEBUG_0 ("connect_port_get_ipc_poll_handle - ENTER.");

	if (!connect_port->port.stream) {
		DS_LOG_DEBUG_0 ("connect_port_get_ipc_poll_handle - cache was empty, trying to reconnect!");
		// cache is empty, reconnect, e.g., there was a disconnect
		bool timed_out = false;
		connection = ds_ipc_connect (connect_port->port.ipc, 100 /*ms*/, callback, &timed_out);
		if (!connection) {
			if (callback && !timed_out)
				callback("Failed to connect to client connection", -1);
			else if (timed_out)
				DS_LOG_DEBUG_0 ("connect_port_get_ipc_poll_handle - reconnect attempt timed out.");
			ep_raise_error ();
		}

		ep_char8_t buffer [DS_IPC_MAX_TO_STRING_LEN];
		if (!(ds_ipc_stream_to_string (connection, buffer, (uint32_t)ARRAY_SIZE (buffer))))
			buffer [0] = '\0';
		DS_LOG_DEBUG_1 ("connect_port_get_ipc_poll_handle - returned connection %s", buffer);

		if (!ds_icp_advertise_v1_send (connection)) {
			if (callback)
				callback("Failed to send advertise message", -1);
			ep_raise_error ();
		}

		//Transfer ownership.
		connect_port->port.stream = connection;
		connection = NULL;
	}

	handle->ipc = NULL;
	handle->stream = connect_port->port.stream;
	handle->events = 0;
	handle->user_data = object;

	success = true;

ep_on_exit:
	DS_LOG_DEBUG_0 ("connect_port_get_ipc_poll_handle - EXIT.");
	return success;

ep_on_error:
	ds_ipc_stream_free (connection);
	success = false;
	ep_exit_error_handler ();
}

static
DiagnosticsIpcStream *
connect_port_get_connected_stream_func (
	void *object,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (object != NULL);

	DiagnosticsConnectPort *connect_port = (DiagnosticsConnectPort *)object;
	DiagnosticsIpcStream *stream = connect_port->port.stream;
	connect_port->port.stream = NULL;
	return stream;
}

static
void
connect_port_reset (
	void *object,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (object != NULL);

	DiagnosticsConnectPort *connect_port = (DiagnosticsConnectPort *)object;
	ds_ipc_stream_free (connect_port->port.stream);
	connect_port->port.stream = NULL;
}

static DiagnosticsPortVtable connect_port_vtable = {
	connect_port_free_func,
	connect_port_get_ipc_poll_handle_func,
	connect_port_get_connected_stream_func,
	connect_port_reset };

DiagnosticsConnectPort *
ds_connect_port_alloc (
	DiagnosticsIpc *ipc,
	DiagnosticsPortBuilder *builder)
{
	DiagnosticsConnectPort * instance = ep_rt_object_alloc (DiagnosticsConnectPort);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ds_port_init (
		(DiagnosticsPort *)instance,
		&connect_port_vtable,
		ipc,
		builder) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ds_connect_port_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ds_connect_port_free (DiagnosticsConnectPort *connect_port)
{
	ep_return_void_if_nok (connect_port != NULL);
	ds_port_fini (&connect_port->port);
	ep_rt_object_free (connect_port);
}

/*
 * DiagnosticsListenPort.
 */

static
void
listen_port_free_func (void *object)
{
	EP_ASSERT (object != NULL);
	ds_listen_port_free ((DiagnosticsListenPort *)object);
}

static
bool
listen_port_get_ipc_poll_handle_func (
	void *object,
	DiagnosticsIpcPollHandle *handle,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (object != NULL);
	EP_ASSERT (handle != NULL);

	DiagnosticsListenPort *listen_port = (DiagnosticsListenPort *)object;

	handle->ipc = listen_port->port.ipc;
	handle->stream = NULL;
	handle->events = 0;
	handle->user_data = object;

	return true;
}

static
DiagnosticsIpcStream *
listen_port_get_connected_stream_func (
	void *object,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (object != NULL);

	DiagnosticsListenPort *listen_port = (DiagnosticsListenPort *)object;
	return ds_ipc_accept (listen_port->port.ipc, callback);
}

static
void
listen_port_reset (
	void *object,
	ds_ipc_error_callback_func callback)
{
	EP_ASSERT (object != NULL);
	return;
}

static DiagnosticsPortVtable listen_port_vtable = {
	listen_port_free_func,
	listen_port_get_ipc_poll_handle_func,
	listen_port_get_connected_stream_func,
	listen_port_reset };

DiagnosticsListenPort *
ds_listen_port_alloc (
	DiagnosticsIpc *ipc,
	DiagnosticsPortBuilder *builder)
{
	DiagnosticsListenPort * instance = ep_rt_object_alloc (DiagnosticsListenPort);
	ep_raise_error_if_nok (instance != NULL);

	ep_raise_error_if_nok (ds_port_init (
		(DiagnosticsPort *)instance,
		&listen_port_vtable,
		ipc,
		builder) != NULL);

ep_on_exit:
	return instance;

ep_on_error:
	ds_listen_port_free (instance);
	instance = NULL;
	ep_exit_error_handler ();
}

void
ds_listen_port_free (DiagnosticsListenPort *listen_port)
{
	ep_return_void_if_nok (listen_port != NULL);
	ds_port_fini (&listen_port->port);
	ep_rt_object_free (listen_port);
}

#endif /* !defined(DS_INCLUDE_SOURCE_FILES) || defined(DS_FORCE_INCLUDE_SOURCE_FILES) */
#endif /* ENABLE_PERFTRACING */

#if !defined(ENABLE_PERFTRACING) || (defined(DS_INCLUDE_SOURCE_FILES) && !defined(DS_FORCE_INCLUDE_SOURCE_FILES))
extern const char quiet_linker_empty_file_warning_diagnostics_ipc;
const char quiet_linker_empty_file_warning_diagnostics_ipc = 0;
#endif
