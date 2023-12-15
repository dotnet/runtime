#include <glib.h>
#include <mono/mini/debugger-agent-external.h>
#include <mono/component/debugger-engine.h>
#include <mono/metadata/components.h>

#ifdef HOST_WASI
static int conn_fd;
static int log_level = 1;
static int retry_receive_message = 100;
static int connection_wait_us = 1000;

__attribute__((import_module("wasi_snapshot_preview1")))
__attribute__((import_name("sock_accept")))
int sock_accept(int fd, int fdflags, int* result_ptr);

static long long
time_in_milliseconds (void)
{
	struct timeval tv;
	gettimeofday(&tv,NULL);
	return (((long long)tv.tv_sec)*1000)+(tv.tv_usec/1000);
}

static int
wasi_transport_recv (void *buf, int len)
{
	int res;
	int total = 0;
	int fd = conn_fd;
	int num_recv_calls  = 0;

	do {
		res = read (fd, (char *) buf + total, len - total);
		if (res > 0)
			total += res;
		num_recv_calls++;
		if ((res > 0 && total < len) || (res == -1 && num_recv_calls < retry_receive_message)) {
			// Wasmtime on Windows doesn't seem to be able to sleep for short periods like 1ms so we'll have to spinlock
			long long start = time_in_milliseconds ();
			while ((time_in_milliseconds () - start) < (connection_wait_us/1000));
		} else {
			break;
		}
	} while (true);
	return total;
}

static gboolean
wasi_transport_send (void *data, int len)
{
	int res;
	do {
		res = write (conn_fd, (const char*)data, len);
	} while (res == -1);

	return res == len;
}

/*
 * wasi_transport_connect:
 *
 *   listen on the socket that is already opened by wasm runtime
 */
static void
wasi_transport_connect (const char *socket_fd)
{
	PRINT_DEBUG_MSG (1, "wasi_transport_connect.\n");
	bool handshake_ok = FALSE;
	conn_fd = -1;
	char *ptr;
	PRINT_DEBUG_MSG (1, "Waiting for connection from client, socket fd=%s.\n", socket_fd);
	long socket_fd_long = strtol(socket_fd, &ptr, 10);
	if (socket_fd_long == 0) {
		PRINT_DEBUG_MSG (1, "Invalid socket fd = %s.\n", socket_fd);
		return;
	}
	while (!handshake_ok) {
		int ret_accept = sock_accept (socket_fd_long, __WASI_FDFLAGS_NONBLOCK, &conn_fd);
		if (ret_accept < 0) {
			PRINT_DEBUG_MSG (1, "Error while accepting connection from client = %d.\n", ret_accept);
			return;
		}
		int res = -1;
		if (conn_fd != -1) {
			res = write (conn_fd, (const char*)"", 0);
			if (res != -1) {
				handshake_ok = mono_component_debugger ()->transport_handshake ();
			}
		}
		if (conn_fd == -1 || res == -1) {
			sleep(1);
			continue;
		}
	}
	PRINT_DEBUG_MSG (1, "Accepted connection from client, socket fd=%d.\n", conn_fd);
}

static void
wasi_transport_close1 (void)
{
/*	shutdown (conn_fd, SHUT_RD);
	shutdown (listen_fd, SHUT_RDWR);
	close (listen_fd);*/
}

static void
wasi_transport_close2 (void)
{
//	shutdown (conn_fd, SHUT_RDWR);
}

static void 
mono_wasi_start_debugger_thread (MonoError *error)
{
	mono_debugger_agent_receive_and_process_command ();
	connection_wait_us = 250;
}

static void
mono_wasi_suspend_vm (void)
{
}

static void
mono_wasi_debugger_init (void)
{
	DebuggerTransport trans;
	trans.name = "wasi_socket";
	trans.send = wasi_transport_send;
	trans.connect = wasi_transport_connect;
	trans.recv = wasi_transport_recv;
	trans.send = wasi_transport_send;
	trans.close1 = wasi_transport_close1;

	mono_debugger_agent_register_transport (&trans);

	mono_debugger_agent_init_internal();
	
	mono_debugger_agent_initialize_function_pointers(mono_wasi_start_debugger_thread, mono_wasi_suspend_vm, mono_wasi_suspend_current);
}

static void 
mono_wasi_receive_and_process_command_from_debugger_agent (void)
{
	retry_receive_message = 2; //when it comes from interpreter we don't want to spend a long time waiting for messages
	mono_debugger_agent_receive_and_process_command ();
	retry_receive_message = 50; //when it comes from debugger we are waiting for a debugger command (resume/step), we want to retry more times
}

static void
mono_wasi_single_step_hit (void)
{
	mono_wasm_save_thread_context ();
	mono_de_process_single_step (mono_wasm_get_tls (), FALSE);
}

static void
mono_wasi_breakpoint_hit (void)
{
	mono_wasm_save_thread_context ();
	mono_de_process_breakpoint (mono_wasm_get_tls (), FALSE);
}

void
mini_wasi_debugger_add_function_pointers (MonoComponentDebugger* fn_table)
{
	fn_table->init = mono_wasi_debugger_init;
	fn_table->receive_and_process_command_from_debugger_agent = mono_wasi_receive_and_process_command_from_debugger_agent;
	fn_table->mono_wasm_breakpoint_hit = mono_wasi_breakpoint_hit;
	fn_table->mono_wasm_single_step_hit = mono_wasi_single_step_hit;
}

#else // HOST_WASI

void
mini_wasi_debugger_add_function_pointers (MonoComponentDebugger* fn_table)
{
}

#endif
