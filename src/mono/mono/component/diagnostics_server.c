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
	volatile int32_t buf_full;
} WasmIpcStreamQueue;

extern void
mono_wasm_diagnostic_server_stream_signal_work_available (WasmIpcStreamQueue *queue, int32_t current_thread);

static void
queue_wake_reader (void *ptr) {
	/* asynchronously invoked on the ds server thread by the writer. */
	WasmIpcStreamQueue *q = (WasmIpcStreamQueue *)ptr;
	mono_wasm_diagnostic_server_stream_signal_work_available (q, 0);
}

static void
queue_wake_reader_now (WasmIpcStreamQueue *q)
{
	// call only from the diagnostic server thread!
	mono_wasm_diagnostic_server_stream_signal_work_available (q, 1);
}

static int32_t
queue_push_sync (WasmIpcStreamQueue *q, const uint8_t *buf, uint32_t buf_size, uint32_t *bytes_written)
{
	/* to be called on the writing thread */
	/* single-writer, so there is no write contention */
	q->buf = (uint8_t*)buf;
	q->count = buf_size;
	/* there's one instance where a thread other than the
	 * streaming thread is writing: in ep_file_initialize_file
	 * (called from ep_session_start_streaming), there's a write
	 * from either the main thread (if the streaming was deferred
	 * until ep_finish_init is called) or the diagnostic thread if
	 * the session is started later.
	 */
	pthread_t cur = pthread_self ();
	gboolean will_wait = TRUE;
	mono_atomic_store_i32 (&q->buf_full, 1);
	if (cur == ds_thread_id) {
		queue_wake_reader_now (q);
		/* doesn't return until the buffer is empty again; no need to wait */
		will_wait = FALSE;
	} else {
		emscripten_dispatch_to_thread (ds_thread_id, EM_FUNC_SIG_VI, &queue_wake_reader, NULL, q);
	}
	// wait until the reader reads the value
	int r = 0;
	if (G_LIKELY (will_wait)) {
		while (mono_atomic_load_i32 (&q->buf_full) != 0) {
			if (G_UNLIKELY (mono_threads_wasm_is_browser_thread ())) {
				/* can't use memory.atomic.wait32 on the main thread, spin instead */
				/* this lets Emscripten run queued calls on the main thread */
				emscripten_thread_sleep (1);
			} else  {
				r = mono_wasm_atomic_wait_i32 (&q->buf_full, 1, -1);
				if (G_UNLIKELY (r == 2)) {
					/* timed out with infinite wait?? */
					return -1;
				}
				/* if r == 0 (blocked and woken) or r == 1 (not equal), go around again and check if buf_full is now 0 */
			}
		}
	}
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
	WasmIpcStream *stream = (WasmIpcStream*)self;
	// push the special buf value -1 to signal stream close.
	int r = queue_push_sync (&stream->queue, (void*)(intptr_t)-1, 0, NULL);
	return r == 0;
}


#endif
