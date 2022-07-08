// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <mono/component/diagnostics_server.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-compiler.h>
#include <eventpipe/ds-server.h>
#ifdef HOST_WASM
#include <eventpipe/ep-ipc-stream.h>
#include <mono/component/event_pipe-wasm.h>
#include <mono/utils/mono-coop-semaphore.h>
#include <mono/utils/mono-threads-wasm.h>
#include <emscripten/emscripten.h>
#include <emscripten/threading.h>
#endif

static bool
diagnostics_server_available (void);

#ifndef HOST_WASM
static MonoComponentDiagnosticsServer fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &diagnostics_server_available },
	&ds_server_init,
	&ds_server_shutdown,
	&ds_server_pause_for_diagnostics_monitor,
	&ds_server_disable
};
#else
typedef struct _MonoWasmDiagnosticServerOptions {
	int32_t suspend; /* set from JS! */
	MonoCoopSem suspend_resume;
} MonoWasmDiagnosticServerOptions;

static MonoWasmDiagnosticServerOptions wasm_ds_options;
static pthread_t ds_thread_id;

static bool
ds_server_wasm_init (void);

static bool
ds_server_wasm_shutdown (void);

static void
ds_server_wasm_pause_for_diagnostics_monitor (void);

static void
ds_server_wasm_disable (void);

extern void
mono_wasm_diagnostic_server_on_runtime_server_init (MonoWasmDiagnosticServerOptions *out_options);

EMSCRIPTEN_KEEPALIVE void
mono_wasm_diagnostic_server_resume_runtime_startup (void);


static MonoComponentDiagnosticsServer fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &diagnostics_server_available },
	&ds_server_wasm_init,
	&ds_server_wasm_shutdown,
	&ds_server_wasm_pause_for_diagnostics_monitor,
	&ds_server_wasm_disable,
};
#endif

static bool
diagnostics_server_available (void)
{
	EM_ASM({
			console.log ("diagnostic server available");
		});
	return true;
}

MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_init (void)
{
	EM_ASM({
			console.log ("diagnostic server component init");
		});
	return &fn_table;
}

#ifdef HOST_WASM

static bool
ds_server_wasm_init (void)
{
	/* called on the main thread when the runtime is sufficiently initialized */
	EM_ASM({
			console.log ("ds_server_wasm_init");
		});
	mono_coop_sem_init (&wasm_ds_options.suspend_resume, 0);
	mono_wasm_diagnostic_server_on_runtime_server_init(&wasm_ds_options);
	return true;
}


static bool
ds_server_wasm_shutdown (void)
{
	EM_ASM({
			console.log ("ds_server_wasm_shutdown");
		});
	return true;
}

static void
ds_server_wasm_pause_for_diagnostics_monitor (void)
{
	EM_ASM({
			console.log ("ds_server_wasm_pause_for_diagnostics_monitor");
		});

	/* wait until the DS receives a resume */
	if (wasm_ds_options.suspend) {
		const guint timeout = 50;
		const guint warn_threshold = 5000;
		guint cumulative_timeout = 0;
		while (true) {
			MonoSemTimedwaitRet res = mono_coop_sem_timedwait (&wasm_ds_options.suspend_resume, timeout, MONO_SEM_FLAGS_ALERTABLE);
			if (res == MONO_SEM_TIMEDWAIT_RET_SUCCESS || res == MONO_SEM_TIMEDWAIT_RET_ALERTED)
				break;
			else {
				/* timed out */
				cumulative_timeout += timeout;
				if (cumulative_timeout > warn_threshold) {
					EM_ASM({
							console.log ("ds_server_wasm_pause_for_diagnostics_monitor paused for 5 seconds");
						});
					cumulative_timeout = 0;
				}
			}
		}
	}
}


static void
ds_server_wasm_disable (void)
{
	EM_ASM({
			console.log ("ds_server_wasm_disable");
		});
}

/* Allocated by mono_wasm_diagnostic_server_create_thread,
 * then ownership passed to server_thread.
 */
static char*
ds_websocket_url;

extern void mono_wasm_diagnostic_server_on_server_thread_created (char *websocket_url);

static void*
server_thread (void* unused_arg G_GNUC_UNUSED)
{
	char* ws_url = g_strdup (ds_websocket_url);
	g_free (ds_websocket_url);
	ds_websocket_url = NULL;
	mono_wasm_diagnostic_server_on_server_thread_created (ws_url);
	// "exit" from server_thread, but keep the pthread alive and responding to events
	emscripten_exit_with_live_runtime ();
}

gboolean
mono_wasm_diagnostic_server_create_thread (const char *websocket_url, pthread_t *out_thread_id)
{
	pthread_t thread;

	g_assert (!ds_websocket_url);
	ds_websocket_url = g_strdup (websocket_url);
	if (!pthread_create (&thread, NULL, server_thread, NULL)) {
		*out_thread_id = thread;
		return TRUE;
	}
	memset(out_thread_id, 0, sizeof(pthread_t));
	return FALSE;
}
	
void
mono_wasm_diagnostic_server_thread_attach_to_runtime (void)
{
	ds_thread_id = pthread_self();
	MonoThread *thread = mono_thread_internal_attach (mono_get_root_domain ());
	mono_thread_set_state (thread, ThreadState_Background);
	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NO_SAMPLE);
	/* diagnostic server thread is now in GC Unsafe mode */
}

void
mono_wasm_diagnostic_server_post_resume_runtime (void)
{
	if (wasm_ds_options.suspend) {
		/* wake the main thread */
		mono_coop_sem_post (&wasm_ds_options.suspend_resume);
	}
}

/* single-reader single-writer one-element queue. See
 * src/mono/wasm/runtime/diagnostics/server_pthread/stream-queue.ts
 */ 
typedef struct WasmIpcStreamQueue {
	uint8_t *buf;
	int32_t count;
	volatile int32_t write_done;
} WasmIpcStreamQueue;

extern void
mono_wasm_diagnostic_server_stream_signal_work_available (WasmIpcStreamQueue *queue);

static void
queue_wake_reader (void *ptr) {
	/* asynchronously invoked on the ds server thread by the writer. */
	WasmIpcStreamQueue *q = (WasmIpcStreamQueue *)ptr;
	mono_wasm_diagnostic_server_stream_signal_work_available (q);
}

static int32_t
queue_push_sync (WasmIpcStreamQueue *q, const uint8_t *buf, uint32_t buf_size, uint32_t *bytes_written)
{
	/* to be called on the writing thread */
	/* single-writer, so there is no write contention */
	q->buf = (uint8_t*)buf;
	q->count = buf_size;
	emscripten_dispatch_to_thread (ds_thread_id, EM_FUNC_SIG_VI, NULL, queue_wake_reader, q);
	// wait until the reader reads the value
	int r = mono_wasm_atomic_wait_i32 (&q->write_done, 0, -1);
	if (G_UNLIKELY (r != 0)) {
		return -1;
	}
	if (mono_atomic_load_i32 (&q->write_done) != 0)
		return -1;
	if (bytes_written)
		*bytes_written = buf_size;
	return 0;
}

typedef struct {
	IpcStream stream;
	WasmIpcStreamQueue queue;
}  WasmIpcStream;

static void
wasm_ipc_stream_free (void *self);
static bool
wasm_ipc_stream_read (void *self, uint8_t *buffer, uint32_t bytes_to_read, uint32_t *bytes_read, uint32_t timeout_ms);
static bool
wasm_ipc_stream_write (void *self, const uint8_t *buffer, uint32_t bytes_to_write, uint32_t *bytes_written, uint32_t timeout_ms);
static bool
wasm_ipc_stream_flush (void *self);
static bool
wasm_ipc_stream_close (void *self);

static IpcStreamVtable wasm_ipc_stream_vtable = {
	&wasm_ipc_stream_free,
	&wasm_ipc_stream_read,
	&wasm_ipc_stream_write,
	&wasm_ipc_stream_flush,
	&wasm_ipc_stream_close,
};

EMSCRIPTEN_KEEPALIVE IpcStream *
mono_wasm_diagnostic_server_create_stream (void)
{
	g_assert (G_STRUCT_OFFSET(WasmIpcStream, queue) == 4); // keep in sync with mono_wasm_diagnostic_server_get_stream_queue
	WasmIpcStream *stream = g_new0 (WasmIpcStream, 1);
	ep_ipc_stream_init (&stream->stream, &wasm_ipc_stream_vtable);
	return &stream->stream;
}

static void
wasm_ipc_stream_free (void *self)
{
	g_free (self);
}
static bool
wasm_ipc_stream_read (void *self, uint8_t *buffer, uint32_t bytes_to_read, uint32_t *bytes_read, uint32_t timeout_ms)
{
	/* our reader is in JS */
	g_assert_not_reached();
}
static bool
wasm_ipc_stream_write (void *self, const uint8_t *buffer, uint32_t bytes_to_write, uint32_t *bytes_written, uint32_t timeout_ms)
{
	WasmIpcStream *stream = (WasmIpcStream *)self;
	g_assert (timeout_ms == EP_INFINITE_WAIT); // pass it down to the queue if the timeout param starts being used
	int r = queue_push_sync (&stream->queue, buffer, bytes_to_write, bytes_written);
	return r == 0;
}

static bool
wasm_ipc_stream_flush (void *self)
{
	return true;
}
static bool
wasm_ipc_stream_close (void *self)
{
	// TODO: signal the writer to close
	EM_ASM({
			console.log ("wasm_ipc_stream_close");
		});
	return true;
}


#endif
