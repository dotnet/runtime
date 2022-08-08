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

#if !defined (HOST_WASM) || defined (DISABLE_THREADS)
static MonoComponentDiagnosticsServer fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &diagnostics_server_available },
	&ds_server_init,
	&ds_server_shutdown,
	&ds_server_pause_for_diagnostics_monitor,
	&ds_server_disable
};

#else /* !defined (HOST_WASM) || defined (DISABLE_THREADS) */

static bool
ds_server_wasm_init (void);

static bool
ds_server_wasm_shutdown (void);

static void
ds_server_wasm_pause_for_diagnostics_monitor (void);

static void
ds_server_wasm_disable (void);

static MonoComponentDiagnosticsServer fn_table = {
	{ MONO_COMPONENT_ITF_VERSION, &diagnostics_server_available },
	&ds_server_wasm_init,
	&ds_server_wasm_shutdown,
	&ds_server_wasm_pause_for_diagnostics_monitor,
	&ds_server_wasm_disable,
};

typedef struct _MonoWasmDiagnosticServerOptions {
	int32_t suspend; /* set from JS! */
	MonoCoopSem suspend_resume;
} MonoWasmDiagnosticServerOptions;

static MonoWasmDiagnosticServerOptions wasm_ds_options;
static pthread_t ds_thread_id;

extern void
mono_wasm_diagnostic_server_on_runtime_server_init (MonoWasmDiagnosticServerOptions *out_options);

EMSCRIPTEN_KEEPALIVE void
mono_wasm_diagnostic_server_resume_runtime_startup (void);

static bool
ds_server_wasm_init (void)
{
	/* called on the main thread when the runtime is sufficiently initialized */
	mono_coop_sem_init (&wasm_ds_options.suspend_resume, 0);
	mono_wasm_diagnostic_server_on_runtime_server_init(&wasm_ds_options);
	return true;
}


static bool
ds_server_wasm_shutdown (void)
{
	mono_coop_sem_destroy (&wasm_ds_options.suspend_resume);
	return true;
}

static void
ds_server_wasm_pause_for_diagnostics_monitor (void)
{
	/* wait until the DS receives a resume */
	if (wasm_ds_options.suspend) {
		/* WISH: it would be better if we split mono_runtime_init_checked() (and runtime
		 * initialization in general) into two separate functions that we could call from
		 * JS, and wait for the resume event in JS.  That would allow the browser to remain
		 * responsive.
		 *
		 * (We can't pause earlier because we need to start up enough of the runtime that DS
		 * can call ep_enable_2() and get session IDs back.  Which seems to require
		 * mono_jit_init_version() to be called. )
		 *
		 * With the current setup we block the browser UI.  Emscripten still processes its
		 * queued work in futex_wait_busy, so at least other pthreads aren't waiting for us.
		 * But the user can't interact with the browser tab at all. Even the JS console is
		 * not displayed.
		 */
		int res = mono_coop_sem_wait(&wasm_ds_options.suspend_resume, MONO_SEM_FLAGS_NONE);
		g_assert (res == 0);
	}
}


static void
ds_server_wasm_disable (void)
{
	/* DS disable seems to only be called for the AOT compiler, which should never get here on
	 * HOST_WASM */
	g_assert_not_reached ();
}

/* Allocated by mono_wasm_diagnostic_server_create_thread,
 * then ownership passed to server_thread.
 */
static char*
ds_websocket_url;

extern void mono_wasm_diagnostic_server_on_server_thread_created (char *websocket_url);

/*
 * diagnostic server pthread lifetime:
 *
 * server_thread called: no runtime yet
 * server_thread calls emscripten_exit_with_live_runtime - thread is live in JS; no C stack.
 * {runtime starts}
 * server_thread_attach_to_runtime called from JS: MonoThreadInfo* for server_thread is set,
 *    thread transitions to GC Unsafe mode and immediately transitions to GC Safe before returning to JS.
 * server loop (diagnostics/server_pthread/index.ts serverLoop) starts
 *   - diagnostic server calls into the runtime
 */
static void*
server_thread (void* unused_arg G_GNUC_UNUSED)
{
	g_assert (ds_websocket_url != NULL);
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

	if (!websocket_url)
		return FALSE;

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
	mono_thread_info_set_flags (MONO_THREAD_INFO_FLAGS_NO_GC | MONO_THREAD_INFO_FLAGS_NO_SAMPLE);
	/* diagnostic server thread is now in GC Unsafe mode
	 * we're returning to JS, so switch to GC Safe mode
	 */
	gpointer stackdata;
	mono_threads_enter_gc_safe_region_unbalanced (&stackdata);

}

void
mono_wasm_diagnostic_server_post_resume_runtime (void)
{
	MONO_ENTER_GC_UNSAFE;
	if (wasm_ds_options.suspend) {
		/* wake the main thread */
		mono_coop_sem_post (&wasm_ds_options.suspend_resume);
	}
	MONO_EXIT_GC_UNSAFE;
}

#define QUEUE_CLOSE_SENTINEL ((uint8_t*)(intptr_t)-1)

/* single-reader single-writer one-element queue. See
 * src/mono/wasm/runtime/diagnostics/server_pthread/stream-queue.ts
 */
typedef struct WasmIpcStreamQueue {
	uint8_t *buf; /* or QUEUE_CLOSE_SENTINEL */
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
	MONO_ENTER_GC_SAFE;
	// call only from the diagnostic server thread!
	mono_wasm_diagnostic_server_stream_signal_work_available (q, 1);
	MONO_EXIT_GC_SAFE;
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
		gboolean is_browser_thread_inited = FALSE;
		gboolean is_browser_thread = FALSE;
		while (mono_atomic_load_i32 (&q->buf_full) != 0) {
			if (G_UNLIKELY (!is_browser_thread_inited)) {
					is_browser_thread = mono_threads_wasm_is_browser_thread ();
					is_browser_thread_inited = TRUE;
			}
			if (G_UNLIKELY (is_browser_thread)) {
				/* can't use memory.atomic.wait32 on the main thread, spin instead */
				/* this lets Emscripten run queued calls on the main thread */
				MONO_ENTER_GC_SAFE;
				emscripten_thread_sleep (1);
				MONO_EXIT_GC_SAFE;
			} else  {
				MONO_ENTER_GC_SAFE;
				r = mono_wasm_atomic_wait_i32 (&q->buf_full, 1, -1);
				MONO_EXIT_GC_SAFE;
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
	IpcStream *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	g_assert (G_STRUCT_OFFSET(WasmIpcStream, queue) == 4); // keep in sync with mono_wasm_diagnostic_server_get_stream_queue
	WasmIpcStream *stream = g_new0 (WasmIpcStream, 1);
	ep_ipc_stream_init (&stream->stream, &wasm_ipc_stream_vtable);
	result = &stream->stream;
	MONO_EXIT_GC_UNSAFE;
	return result;
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
	int r = queue_push_sync (&stream->queue, QUEUE_CLOSE_SENTINEL, 0, NULL);
	return r == 0;
}

#endif /* !defined (HOST_WASM) || defined (DISABLE_THREADS) */

static bool
diagnostics_server_available (void)
{
	return true;
}

MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_init (void)
{
	return &fn_table;
}
