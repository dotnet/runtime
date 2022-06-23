// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <mono/component/diagnostics_server.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-compiler.h>
#include <eventpipe/ds-server.h>
#ifdef HOST_BROWSER
#include <emscripten/emscripten.h>
#endif

static bool
diagnostics_server_available (void);

#ifndef HOST_BROWSER
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

#ifdef HOST_BROWSER

static bool
ds_server_wasm_init (void)
{
	EM_ASM({
			console.log ("ds_server_wasm_init");
		});
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

#endif;
