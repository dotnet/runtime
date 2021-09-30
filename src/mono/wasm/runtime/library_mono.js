// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

const MonoSupportLib = {
	// this line will be executed in mergeInto below
	$MONO__postset: '__dotnet_runtime.export_functions(MONO, Module);',
	// this will become globalThis.MONO
	$MONO: {},
	// the methods would be visible to EMCC linker
	mono_set_timeout: function () { return MONO.mono_set_timeout.call(MONO, arguments) },
	mono_wasm_asm_loaded: function () { return MONO.mono_wasm_asm_loaded.call(MONO, arguments) },
	mono_wasm_fire_debugger_agent_message: function () { return MONO.mono_wasm_fire_debugger_agent_message.call(MONO, arguments) },
	schedule_background_exec: function () { return MONO.schedule_background_exec.call(MONO, arguments) },
};

autoAddDeps(MonoSupportLib, '$MONO')
mergeInto(LibraryManager.library, MonoSupportLib)
