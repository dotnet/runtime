// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <mono/component/diagnostics_server.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-compiler.h>
#include <eventpipe/ds-server.h>
#ifdef HOST_WASM
#include <mono/component/event_pipe-wasm.h>
#include <emscripten/emscripten.h>
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
static bool
ds_server_wasm_init (void);

static bool
ds_server_wasm_shutdown (void);

static void
ds_server_wasm_pause_for_diagnostics_monitor (void);

static void
ds_server_wasm_disable (void);

extern void
mono_wasm_diagnostic_server_on_runtime_server_init (void);

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
	EM_ASM({
			console.log ("ds_server_wasm_init");
		});
	mono_wasm_diagnostic_server_on_runtime_server_init();
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
	

#endif;
