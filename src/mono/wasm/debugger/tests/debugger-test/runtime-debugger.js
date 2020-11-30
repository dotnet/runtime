// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var Module = { 
	onRuntimeInitialized: function () {
		config.loaded_cb = function () {
			App.init ();
		};
		// For custom logging patch the functions below
		/*
		MONO.logging = {
			trace: function (domain, log_level, message, isFatal, dataPtr) {},
			debugger: function (level, message) {}
		};
		MONO.mono_wasm_setenv ("MONO_LOG_LEVEL", "debug");
		MONO.mono_wasm_setenv ("MONO_LOG_MASK", "all");
		*/
		MONO.mono_load_runtime_and_bcl_args (config)
	},
};
