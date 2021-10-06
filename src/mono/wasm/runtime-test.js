// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// -*- mode: js; js-indent-level: 4; -*-
//
// Run runtime tests under a JS shell or a browser
//
"use strict";

//glue code to deal with the differences between chrome, ch, d8, jsc and sm.
var is_browser = typeof window != "undefined";

// if the engine doesn't provide a console
if (typeof (console) === "undefined") {
	var console = {
		log: globalThis.print,
		clear: function () { }
	};
}
globalThis.testConsole = console;

//define arguments for later
var allRuntimeArguments = null;
try {
	if (is_browser) {
		// We expect to be run by tests/runtime/run.js which passes in the arguments using http parameters
		const url = new URL(decodeURI(window.location));
		allRuntimeArguments = [];
		for (let param of url.searchParams) {
			if (param[0] == "arg") {
				allRuntimeArguments.push(param[1]);
			}
		}

	} else if (typeof arguments !== "undefined" && typeof arguments !== "null") {
		allRuntimeArguments = arguments;
	} else if (typeof process !== 'undefined' && typeof process.argv !== "undefined") {
		allRuntimeArguments = process.argv.slice(2);
	} else if (typeof scriptArgs !== "undefined") {
		allRuntimeArguments = scriptArgs;
	} else if (typeof WScript !== "undefined" && WScript.Arguments) {
		allRuntimeArguments = WScript.Arguments;
	} else {
		allRuntimeArguments = [];
	}
} catch (e) {
	console.log(e);
}

function proxyMethod (prefix, func, asJson) {
	return function() {
		const args = [...arguments];
		var payload= args[0];
		if(payload === undefined) payload = 'undefined';
		else if(payload === null) payload = 'null';
		else if(typeof payload === 'function') payload = payload.toString();
		else if(typeof payload !== 'string') {
			try{
				payload = JSON.stringify(payload);
			}catch(e){
				payload = payload.toString();
			}
		}

		if (asJson) {
			func (JSON.stringify({
				method: prefix,
				payload: payload,
				arguments: args
			}));
		} else {
			func([prefix + payload, ...args.slice(1)]);
		}
	};
};

var methods = ["debug", "trace", "warn", "info", "error"];
for (var m of methods) {
	if (typeof(console[m]) != "function") {
		console[m] = proxyMethod(`console.${m}: `, console.log, false);
	}
}

function proxyJson (func) {
	for (var m of ["log", ...methods])
		console[m] = proxyMethod(`console.${m}`,func, true);
}

if (is_browser) {
	const consoleUrl = `${window.location.origin}/console`.replace('http://', 'ws://');

	let consoleWebSocket = new WebSocket(consoleUrl);
	consoleWebSocket.onopen = function(event) {
		proxyJson(function (msg) { consoleWebSocket.send (msg); });
		globalThis.testConsole.log("browser: Console websocket connected.");
	};
	consoleWebSocket.onerror = function(event) {
		console.log(`websocket error: ${event}`);
	};
}
//proxyJson(console.log);


let print = globalThis.testConsole.log;
let printErr = globalThis.testConsole.error;

if (typeof crypto === 'undefined') {
	// **NOTE** this is a simple insecure polyfill for testing purposes only
	// /dev/random doesn't work on js shells, so define our own
	// See library_fs.js:createDefaultDevices ()
	var crypto = {
		getRandomValues: function (buffer) {
			for (var i = 0; i < buffer.length; i++)
				buffer [i] = (Math.random () * 256) | 0;
		}
	}
}

if (typeof performance == 'undefined') {
	// performance.now() is used by emscripten and doesn't work in JSC
	var performance = {
		now: function () {
			return Date.now ();
		}
	}
}

//end of all the nice shell glue code.

function test_exit (exit_code) {
	if (is_browser) {
		// Notify the selenium script
		Module.exit_code = exit_code;
		Module.print ("WASM EXIT " + exit_code);
		var tests_done_elem = document.createElement ("label");
		tests_done_elem.id = "tests_done";
		tests_done_elem.innerHTML = exit_code.toString ();
		document.body.appendChild (tests_done_elem);
	} else {
		Module.wasm_exit (exit_code);
	}
}

function fail_exec (reason) {
	Module.print (reason);
	test_exit (1);
}

function inspect_object (o) {
	var r = "";
	for(var p in o) {
		var t = typeof o[p];
		r += "'" + p + "' => '" + t + "', ";
	}
	return r;
}

// Preprocess arguments
console.info("Arguments: " + allRuntimeArguments);
var profilers = [];
var setenv = {};
var runtime_args = [];
var enable_gc = true;
var enable_zoneinfo = false;
var working_dir='/';
while (allRuntimeArguments !== undefined && allRuntimeArguments.length > 0) {
	if (allRuntimeArguments [0].startsWith ("--profile=")) {
		var arg = allRuntimeArguments [0].substring ("--profile=".length);

		profilers.push (arg);

		allRuntimeArguments = allRuntimeArguments.slice (1);
	} else if (allRuntimeArguments [0].startsWith ("--setenv=")) {
		var arg = allRuntimeArguments [0].substring ("--setenv=".length);
		var parts = arg.split ('=');
		if (parts.length != 2)
			fail_exec ("Error: malformed argument: '" + allRuntimeArguments [0]);
		setenv [parts [0]] = parts [1];
		allRuntimeArguments = allRuntimeArguments.slice (1);
	} else if (allRuntimeArguments [0].startsWith ("--runtime-arg=")) {
		var arg = allRuntimeArguments [0].substring ("--runtime-arg=".length);
		runtime_args.push (arg);
		allRuntimeArguments = allRuntimeArguments.slice (1);
	} else if (allRuntimeArguments [0] == "--disable-on-demand-gc") {
		enable_gc = false;
		allRuntimeArguments = allRuntimeArguments.slice (1);
	} else if (allRuntimeArguments [0].startsWith ("--working-dir=")) {
		var arg = allRuntimeArguments [0].substring ("--working-dir=".length);
		working_dir = arg;
		allRuntimeArguments = allRuntimeArguments.slice (1);
	} else {
		break;
	}
}

// cheap way to let the testing infrastructure know we're running in a browser context (or not)
setenv["IsBrowserDomSupported"] = is_browser.toString().toLowerCase();

function writeContentToFile(content, path)
{
	var stream = FS.open(path, 'w+');
	FS.write(stream, content, 0, content.length, 0);
	FS.close(stream);
}

function loadScript (url)
{
	if (is_browser) {
		var script = document.createElement ("script");
		script.src = url;
		document.head.appendChild (script);
	} else {
		load (url);
	}
}

var Module = {
	mainScriptUrlOrBlob: "dotnet.js",
	config: null,
	print,
	printErr,

    preInit: async function() {
        await MONO.mono_wasm_load_config("./mono-config.json"); // sets Module.config implicitly
    },

	onAbort: function(x) {
		print ("ABORT: " + x);
		var err = new Error();
		print ("Stacktrace: \n");
		print (err.stack);
		test_exit (1);
	},

	onRuntimeInitialized: function () {
		// Have to set env vars here to enable setting MONO_LOG_LEVEL etc.
		for (var variable in setenv) {
			MONO.mono_wasm_setenv (variable, setenv [variable]);
		}

		if (!enable_gc) {
			Module.ccall ('mono_wasm_enable_on_demand_gc', 'void', ['number'], [0]);
		}

		Module.config.loaded_cb = function () {
			let wds = FS.stat (working_dir);
			if (wds === undefined || !FS.isDir (wds.mode)) {
				fail_exec (`Could not find working directory ${working_dir}`);
				return;
			}

			FS.chdir (working_dir);
			App.init ();
		};
		Module.config.fetch_file_cb = function (asset) {
			// console.log("fetch_file_cb('" + asset + "')");
			// for testing purposes add BCL assets to VFS until we special case File.Open
			// to identify when an assembly from the BCL is being open and resolve it correctly.
			/*
			var content = new Uint8Array (read (asset, 'binary'));
			var path = asset.substr(Module.config.deploy_prefix.length);
			writeContentToFile(content, path);
			*/

			if (typeof window != 'undefined') {
				return fetch (asset, { credentials: 'same-origin' });
			} else {
				// The default mono_load_runtime_and_bcl defaults to using
				// fetch to load the assets.  It also provides a way to set a
				// fetch promise callback.
				// Here we wrap the file read in a promise and fake a fetch response
				// structure.
				return new Promise ((resolve, reject) => {
					var bytes = null, error = null;
					try {
						bytes = read (asset, 'binary');
					} catch (exc) {
						console.log('v8 file read failed ' + asset + ' ' + exc)
						error = exc;
					}
					var response = { ok: (bytes && !error), url: asset,
						arrayBuffer: function () {
							return new Promise ((resolve2, reject2) => {
								if (error)
									reject2 (error);
								else
									resolve2 (new Uint8Array (bytes));
						}
					)}
					}
					resolve (response);
				})
			}
		};

		MONO.mono_load_runtime_and_bcl_args (Module.config);
	},
};
loadScript ("dotnet.js");

const IGNORE_PARAM_COUNT = -1;

var App = {
	init: function () {
		var wasm_set_main_args = Module.cwrap ('mono_wasm_set_main_args', 'void', ['number', 'number']);
		var wasm_strdup = Module.cwrap ('mono_wasm_strdup', 'number', ['string']);

		Module.wasm_exit = Module.cwrap ('mono_wasm_exit', 'void', ['number']);

		console.info("Initializing.....");

		for (var i = 0; i < profilers.length; ++i) {
			var init = Module.cwrap ('mono_wasm_load_profiler_' + profilers [i], 'void', ['string'])

			init ("");
		}

		if (allRuntimeArguments.length == 0) {
			fail_exec ("Missing required --run argument");
			return;
		}

		if (allRuntimeArguments[0] == "--regression") {
			var exec_regression = Module.cwrap ('mono_wasm_exec_regression', 'number', ['number', 'string'])

			var res = 0;
				try {
					res = exec_regression (10, allRuntimeArguments[1]);
					Module.print ("REGRESSION RESULT: " + res);
				} catch (e) {
					Module.print ("ABORT: " + e);
					print (e.stack);
					res = 1;
				}

			if (res)
				fail_exec ("REGRESSION TEST FAILED");

			return;
		}

		if (runtime_args.length > 0)
			MONO.mono_wasm_set_runtime_options (runtime_args);

		if (allRuntimeArguments[0] == "--run") {
			// Run an exe
			if (allRuntimeArguments.length == 1) {
				fail_exec ("Error: Missing main executable argument.");
				return;
			}

			var main_assembly_name = allRuntimeArguments[1];
			var app_args = allRuntimeArguments.slice (2);

			var main_argc = allRuntimeArguments.length - 2 + 1;
			var main_argv = Module._malloc (main_argc * 4);
			var aindex = 0;
			Module.setValue (main_argv + (aindex * 4), wasm_strdup (allRuntimeArguments [1]), "i32")
			aindex += 1;
			for (var i = 2; i < allRuntimeArguments.length; ++i) {
				Module.setValue (main_argv + (aindex * 4), wasm_strdup (allRuntimeArguments [i]), "i32");
				aindex += 1;
			}
			wasm_set_main_args (main_argc, main_argv);

			// Automatic signature isn't working correctly
			let result = Module.mono_call_assembly_entry_point (main_assembly_name, [app_args], "m");
			let onError = function (error)
			{
				console.error (error);
				if (error.stack)
					console.error (error.stack);

				test_exit (1);
			}
			try {
				result.then (test_exit).catch (onError);
			} catch (error) {
				onError(error);
			}

		} else {
			fail_exec ("Unhandled argument: " + allRuntimeArguments [0]);
		}
	},
	call_test_method: function (method_name, args, signature) {
		if ((arguments.length > 2) && (typeof (signature) !== "string"))
			throw new Error("Invalid number of arguments for call_test_method");

		var fqn = "[System.Private.Runtime.InteropServices.JavaScript.Tests]System.Runtime.InteropServices.JavaScript.Tests.HelperMarshal:" + method_name;
		try {
			return BINDING.call_static_method(fqn, args || [], signature);
		} catch (exc) {
			console.error("exception thrown in", fqn);
			throw exc;
		}
	}
};
