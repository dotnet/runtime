// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

var Module = {
	no_global_exports: true,
	config: null,

	preInit: async function () {
		await Module.MONO.mono_wasm_load_config("./mono-config.json"); // sets Module.MONO.config implicitly
	},

	// Called when the runtime is initialized and wasm is ready
	onRuntimeInitialized: function () {
		if (!Module.MONO.config || Module.MONO.config.error) {
			console.log("An error occured while loading the config file");
			return;
		}

		Module.MONO.config.loaded_cb = function () {
			App.init();
		};
		// For custom logging patch the functions below
		/*
		Module.INTERNAL.logging = {
			trace: function (domain, log_level, message, isFatal, dataPtr) {},
			debugger: function (level, message) {}
		};
		Module.MONO.mono_wasm_setenv ("MONO_LOG_LEVEL", "debug");
		Module.MONO.mono_wasm_setenv ("MONO_LOG_MASK", "all");
		*/

		Module.MONO.config.environment_variables = {
			"DOTNET_MODIFIABLE_ASSEMBLIES": "debug"
		};
		Module.MONO.mono_load_runtime_and_bcl_args(Module.MONO.config)
	},
};
