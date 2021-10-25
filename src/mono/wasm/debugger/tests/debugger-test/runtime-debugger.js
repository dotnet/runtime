// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

var Module = {
	config: null,

	preInit: async function () {
		await MONO.mono_wasm_load_config("./mono-config.json"); // sets MONO.config implicitly
	},

	// Called when the runtime is initialized and wasm is ready
	onRuntimeInitialized: function () {
		if (!MONO.config || MONO.config.error) {
			console.log("An error occured while loading the config file");
			return;
		}

		MONO.config.loaded_cb = function () {
			App.init();
		};
		// For custom logging patch the functions below
		/*
		INTERNAL.logging = {
			trace: function (domain, log_level, message, isFatal, dataPtr) {},
			debugger: function (level, message) {}
		};
		MONO.mono_wasm_setenv ("MONO_LOG_LEVEL", "debug");
		MONO.mono_wasm_setenv ("MONO_LOG_MASK", "all");
		*/

		MONO.config.environment_variables = {
			"DOTNET_MODIFIABLE_ASSEMBLIES": "debug"
		};
		MONO.mono_load_runtime_and_bcl_args(MONO.config)
	},
};
