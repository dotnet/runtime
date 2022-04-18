#ifdef HOST_WASI
#include <glib.h>
#include <mono/mini/debugger-agent-external.h>
#include <mono/component/debugger-engine.h>
#include <mono/metadata/components.h>

static int conn_fd;
static int log_level = 1;
static int retry_receive_message = 5000;

__attribute__((import_module("wasi_snapshot_preview1")))
__attribute__((import_name("sock_accept")))
int sock_accept(int fd, int fdflags, int* result_ptr);


static int
wasi_transport_recv (void *buf, int len)
{
	int res;
	int total = 0;
	int fd = conn_fd;
	int again = 0;

	do {
		res = read (fd, (char *) buf + total, len - total);
		if (res > 0)
			total += res;
		again++;
	} while ((res > 0 && total < len) || (res == -1 && again < retry_receive_message));
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
 * socket_transport_connect:
 *
 *   Connect/Listen on HOST:PORT. If HOST is NULL, generate an address and listen on it.
 */
static void
wasi_transport_connect (const char *address)
{
	bool handshake_ok = FALSE;
	
	while (!handshake_ok)
	{
        sock_accept (4, 4, &conn_fd);
        int res = write (conn_fd, (const char*)"", 0);
        if (conn_fd == -1 || res == -1)
        {
            sleep(1);
            continue;
        }
        handshake_ok = mono_component_debugger ()->transport_handshake ();
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
    mono_debugger_agent_receive_and_process_command (FALSE);
    return;
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
	retry_receive_message = 500;
    mono_debugger_agent_receive_and_process_command (FALSE);
    retry_receive_message = 5000;
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

#endif