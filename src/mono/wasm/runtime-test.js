// -*- mode: js; js-indent-level: 4; -*-
//
// Run runtime tests under a JS shell or a browser
//

//glue code to deal with the differences between chrome, ch, d8, jsc and sm.
const is_browser = typeof window != "undefined";
const is_node = !is_browser && typeof process != 'undefined';

// if the engine doesn't provide a console
if (typeof (console) === "undefined") {
	console = {
		log: globalThis.print,
		clear: function () { }
	};
}


globalThis.testConsole = console;

function proxyMethod (prefix, proxyFunc, asJson) {
	return function() {
		var args = [...arguments];
		if (asJson) {
			proxyFunc (JSON.stringify({
				method: prefix,
				payload: args[0],
				arguments: args
			}));
		} else {
			proxyFunc([prefix + args[0], ...args.slice(1)]);
		}
	};
};

var methods = ["debug", "trace", "warn", "info", "error"];
for (var m of methods) {
	if (typeof(console[m]) != "function") {
		console[m] = proxyMethod(`console.${m}: `, console.log, false);
	}
}

function proxyJson (consoleFunc) {
	for (var m of ["log", ...methods])
		console[m] = proxyMethod(`console.${m}`, consolefunc, true);
}

if (is_browser) {
	const consoleUrl = `${window.location.origin}/console`.replace('http://', 'ws://');

	let consoleWebSocket = new WebSocket(consoleUrl);
	consoleWebSocket.onopen = function(event) {
		proxyJson(function (msg) { consoleWebSocket.send (msg); });
		testConsole.log("browser: Console websocket connected.");
	};
	consoleWebSocket.onerror = function(event) {
		console.log(`websocket error: ${event}`);
	};

	// We expect to be run by tests/runtime/run.js which passes in the arguments using http parameters
	var url = new URL (decodeURI (window.location));
	arguments = [];
	for (var v of url.searchParams) {
		if (v [0] == "arg") {
			arguments.push (v [1]);
		}
	}
}

if (is_node) {
	arguments = process.argv.slice (2);
	var fs = require ('fs');
	function load (file) {
		eval (fs.readFileSync(file).toString());
	};
	function read (path) {
		return fs.readFileSync(path);
	};
	const { performance, PerformanceObserver } = require("perf_hooks")
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

try {
	if (typeof arguments == "undefined") {
		if (typeof (WScript) != "undefined") {
			arguments = WScript.Arguments;
			load = WScript.LoadScriptFile;
			read = WScript.LoadBinaryFile;
		} else if (typeof (scriptArgs) != "undefined") {
			arguments = scriptArgs;
		}
	}
} catch (e) {
}


if (arguments === undefined)
	arguments = [];

//end of all the nice shell glue code.

// set up a global variable to be accessed in App.init
var testArguments = arguments;

function test_exit (exit_code) {
	if (is_browser) {
		// Notify the selenium script
		Module.exit_code = exit_code;
		console.log ("WASM EXIT " + exit_code);
		var tests_done_elem = document.createElement ("label");
		tests_done_elem.id = "tests_done";
		tests_done_elem.innerHTML = exit_code.toString ();
		document.body.appendChild (tests_done_elem);
	} else {
		Module.wasm_exit (exit_code);
	}
}

function fail_exec (reason) {
	console.log (reason);
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
var args = [...testArguments];
//console.info("Arguments: " + testArguments);
profilers = [];
setenv = {};
runtime_args = [];
enable_gc = true;
enable_zoneinfo = false;
working_dir='/';
while (args !== undefined && args.length > 0) {
	let currentArg = args [0].toString();
	if (currentArg.startsWith ("--profile=")) {
		var arg = currentArg.substring ("--profile=".length);

		profilers.push (arg);

		args = args.slice (1);
	} else if (currentArg.startsWith ("--setenv=")) {
		var arg = currentArg.substring ("--setenv=".length);
		var parts = arg.split ('=');
		if (parts.length != 2)
			fail_exec ("Error: malformed argument: '" + currentArg);
		setenv [parts [0]] = parts [1];
		args = args.slice (1);
	} else if (currentArg.startsWith ("--runtime-arg=")) {
		var arg = currentArg.substring ("--runtime-arg=".length);
		runtime_args.push (arg);
		args = args.slice (1);
	} else if (currentArg == "--disable-on-demand-gc") {
		enable_gc = false;
		args = args.slice (1);
	} else if (currentArg.startsWith ("--working-dir=")) {
		var arg = currentArg.substring ("--working-dir=".length);
		working_dir = arg;
		args = args.slice (1);
	} else {
		break;
	}
}
testArguments = args;

// cheap way to let the testing infrastructure know we're running in a browser context (or not)
setenv["IsBrowserDomSupported"] = is_browser.toString().toLowerCase();

function writeContentToFile(content, path) {
	var stream = FS.open(path, 'w+');
	FS.write(stream, content, 0, content.length, 0);
	FS.close(stream);
}

function loadScript (url){
	if (is_browser) {
		var script = document.createElement ("script");
		script.src = url;
		document.head.appendChild (script);
	} else if (is_node) {
		return read (url).toString();
	} else {
		load (url);
	}
	return "";
}

eval (loadScript ("mono-config.js"));
var Module = {
	mainScriptUrlOrBlob: "dotnet.js",
	config,
	setenv,
	read,
	arguments,
	onAbort: function(x) {
		console.log ("ABORT: " + x);
		var err = new Error();
		console.log ("Stacktrace: \n");
		console.log (err.stack);
		test_exit (1);
	},

	onRuntimeInitialized: function () {
		try {

		// Have to set env vars here to enable setting MONO_LOG_LEVEL etc.
		for (var variable in setenv) {
			Module._mono_wasm_setenv (variable, setenv [variable]);
		}

		if (!enable_gc) {
			Module.ccall ('mono_wasm_enable_on_demand_gc', 'void', ['number'], [0]);
		}

		config.loaded_cb = function () {
			let wds = FS.stat (working_dir);
			if (wds === undefined || !FS.isDir (wds.mode)) {
				fail_exec (`Could not find working directory ${working_dir}`);
				return;
			}

			FS.chdir (working_dir);
			App.init ();
		};
		config.fetch_file_cb = function (asset) {
			//console.log("fetch_file_cb('" + asset + "')");
			// for testing purposes add BCL assets to VFS until we special case File.Open
			// to identify when an assembly from the BCL is being open and resolve it correctly.
			/*
			var content = new Uint8Array (read (asset, 'binary'));
			var path = asset.substr(config.deploy_prefix.length);
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
						bytes = Module.read (asset, 'binary');
					} catch (exc) {
						reject (exc);
					}
					resolve ({
						ok: (bytes && !error),
						url: asset,
						arrayBuffer: () => Promise.resolve (new Uint8Array (bytes))
					});
				});
			}
		};

		Module.mono_load_runtime_and_bcl_args (config);
	} catch (e) {
		console.log (e);
	}
	},
};

eval (loadScript ("dotnet.js"));
//var module = require ('./dotnet.js')(Module).then ((va) => console.log (va));
//globalThis.Module = Module;

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

		if (args.length == 0) {
			fail_exec ("Missing required --run argument");
			return;
		}

		if (args[0] == "--regression") {
			var exec_regression = Module.cwrap ('mono_wasm_exec_regression', 'number', ['number', 'string'])

			var res = 0;
				try {
					res = exec_regression (10, args[1]);
					console.log ("REGRESSION RESULT: " + res);
				} catch (e) {
					console.log ("ABORT: " + e);
					console.log (e.stack);
					res = 1;
				}

			if (res)
				fail_exec ("REGRESSION TEST FAILED");

			return;
		}

		if (runtime_args.length > 0)
			MONO.mono_wasm_set_runtime_options (runtime_args);

		if (args[0] == "--run") {
			// Run an exe
			if (args.length == 1) {
				fail_exec ("Error: Missing main executable argument.");
				return;
			}

			main_assembly_name = args[1];
			var app_args = args.slice (2);

			var main_argc = args.length - 2 + 1;
			var main_argv = Module._malloc (main_argc * 4);
			aindex = 0;
			Module.setValue (main_argv + (aindex * 4), wasm_strdup (args [1]), "i32")
			aindex += 1;
			for (var i = 2; i < args.length; ++i) {
				Module.setValue (main_argv + (aindex * 4), wasm_strdup (args [i]), "i32");
				aindex += 1;
			}
			wasm_set_main_args (main_argc, main_argv);

			// Automatic signature isn't working correctly
			let onError = function (error) {
				console.error (error);
				if (error.stack)
					console.error (error.stack);

				test_exit (1);
			}
			try {
				Module.mono_call_assembly_entry_point (main_assembly_name, [app_args], "m")
					.then (test_exit)
					.catch (onError);
			} catch (error) {
				onError(error);
			}

		} else {
			fail_exec ("Unhandled argument: " + args[0]);
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
