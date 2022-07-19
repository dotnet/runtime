// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_EVENT_PIPE_WASM_H
#define _MONO_COMPONENT_EVENT_PIPE_WASM_H

#include <stdint.h>
#include <eventpipe/ep-ipc-pal-types-forward.h>
#include <eventpipe/ep-types-forward.h>
#include <glib.h>

#ifdef HOST_WASM

#include <pthread.h>
#include <emscripten.h>

G_BEGIN_DECLS

#if SIZEOF_VOID_P == 4
/* EventPipeSessionID is 64 bits, which is awkward to work with in JS.
   Fortunately the actual session IDs are derived from pointers which
   are 32-bit on wasm32, so the top bits are zero. */
typedef uint32_t MonoWasmEventPipeSessionID;
#else
#error "EventPipeSessionID is 64-bits, update the JS side to work with it"
#endif

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_event_pipe_enable (const ep_char8_t *output_path,
			     IpcStream *ipc_stream,
			     uint32_t circular_buffer_size_in_mb,
			     const ep_char8_t *providers,
			     /* EventPipeSessionType session_type = EP_SESSION_TYPE_FILE, */
			     /* EventPipieSerializationFormat format = EP_SERIALIZATION_FORMAT_NETTRACE_V4, */
			     /* bool */ gboolean rundown_requested,
			     /* EventPipeSessionSycnhronousCallback sync_callback = NULL, */
			     /* void *callback_additional_data, */
			     MonoWasmEventPipeSessionID *out_session_id);

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_event_pipe_session_start_streaming (MonoWasmEventPipeSessionID session_id);

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_event_pipe_session_disable (MonoWasmEventPipeSessionID session_id);

EMSCRIPTEN_KEEPALIVE gboolean
mono_wasm_diagnostic_server_create_thread (const char *websocket_url, pthread_t *out_thread_id);

EMSCRIPTEN_KEEPALIVE void
mono_wasm_diagnostic_server_thread_attach_to_runtime (void);

EMSCRIPTEN_KEEPALIVE void
mono_wasm_diagnostic_server_post_resume_runtime (void);

EMSCRIPTEN_KEEPALIVE IpcStream *
mono_wasm_diagnostic_server_create_stream (void);

G_END_DECLS

#endif /* HOST_WASM */


#endif /* _MONO_COMPONENT_EVENT_PIPE_WASM_H */

