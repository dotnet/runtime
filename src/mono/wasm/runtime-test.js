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
function proxyMethod (prefix, func, asJson) {
	return function() {
		let args = [...arguments];
		if (asJson) {
			func (JSON.stringify({
				method: prefix,
				payload: args[0],
				arguments: args
			}));
		} else {
			func([prefix + args[0], ...args.slice(1)]);
		}
	};
};

const methods = ["debug", "trace", "warn", "info", "error"];
for (let m of methods) {
	if (typeof(console[m]) !== "function") {
		console[m] = proxyMethod(`console.${m}: `, console.log, false);
	}
}

function proxyJson (func) {
	for (let m of ["log", ...methods])
		console[m] = proxyMethod(`console.${m}`,func, true);
}

if (is_browser) {
	const consoleUrl = `${window.location.origin}/console`.replace('http://', 'ws://');

	let consoleWebSocket = new WebSocket(consoleUrl);
	consoleWebSocket.onopen = function(event) {
		proxyJson(function (msg) { consoleWebSocket.send (msg); });
		console.log("browser: Console websocket connected.");
	};
	consoleWebSocket.onerror = function(event) {
		console.error(`websocket error: ${event}`);
	};
}

if (is_node) {
	var crypto = require('crypto');
} else if (typeof crypto === 'undefined') {
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
if (is_node) {
	var { performance } = require("perf_hooks");
} else if (typeof performance === 'undefined') {
	// performance.now() is used by emscripten and doesn't work in JSC
	var performance = {
		now: function () {
			return Date.now ();
		}
	}
}

// get arguments
let testArguments = [];
try {
	if (is_node) {
		testArguments = process.argv.slice (2);

	} else if (is_browser) {
		// We expect to be run by tests/runtime/run.js which passes in the arguments using http parameters
		const url = new URL (decodeURI (window.location));
		for (let param of url.searchParams) {
			if (param [0] == "arg") {
				testArguments.push (param [1]);
			}
		}

	} else if (typeof arguments === "undefined") {
		if (typeof scriptArgs !== "undefined") {
			testArguments = scriptArgs;
		
		} else if (typeof WScript  !== "undefined" && WScript.Arguments) {
			testArguments = WScript.Arguments;
		}
	} else {
		testArguments = arguments;
	}
} catch (e) {
	console.error(e);
}

// abstract all IO into a compact universally available method so that it is consistent and reliable
const IOHandler = {
	/** Load js file into project and evaluate it
	 * @type {(file: string) => Promise<void>} 
	 * @param {string} file path to the file to load
	*/
	load: null,
	
	/** Read and return the contents of a file as a string
	 * @type {(file: string) => Promise<string>} 
	 * @param {string} file the path to the file to read
	 * @return {string} the contents of the file
	*/
	read: null,

	/** Sets up the load and read functions for later
	 * @type {() => void}
	 */
	init: function() {
		// load: function that loads and executes a script
		let loadFunc = globalThis.load; // shells (v8, JavaScriptCore, Spidermonkey)
		if (!loadFunc) {
			if (typeof WScript  !== "undefined") { // Chakra
				loadFunc = WScript.LoadScriptFile;

			} else if (is_node) { // NodeJS
				loadFunc = async function (file) {
					let req = require(file);

					// sometimes the require returns a function which returns a promise (such as in dotnet.js).
					// othertimes it returns the variable or object that is needed. We handle both cases
					if (typeof(req) === 'function') {
						req = await req(Module); // pass Module so emsdk can use it
					}

					// add to the globalThis the file under the namespace of the upercase filename without js extension
					globalThis[file.substring(2,file.length - 3).replace("-","_").toUpperCase()] = req;
				};
			} else if (is_browser) { // vanila JS in browser
				loadFunc = function (file) {
					const script = document.createElement ("script");
					script.src = file;
					document.head.appendChild (script);
				}
			}
		}
		IOHandler.load = async (file) => await loadFunc(file);

		// read: function that just reads a file into a variable
		let readFunc = globalThis.read; // shells (v8, JavaScriptCore, Spidermonkey)
		if (!readFunc) {
			if (typeof WScript  !== "undefined") {
				readFunc = WScript.LoadBinaryFile; // Chakra

			} else if (is_node) { // NodeJS
				const fs = require ('fs');
				readFunc = function (path) {
					return fs.readFileSync(path).toString();
				};
			} else if (is_browser) {  // vanila JS in browser
				readFunc = fetch;
			}
		}
		IOHandler.read = async (file) => await readFunc(file);
	},

	/** Write the content to a file at a certain path
	 * @type {(content: string, path: string) => void}
	 * @param {string} content the contents to write to the file
	 * @param {string} path the path to which to write the contents
	*/
	writeContentToFile: function(content, path) { // writes a string to a file
		const stream = FS.open(path, 'w+');
		FS.write(stream, content, 0, content.length, 0);
		FS.close(stream);
	},

	/** Returns an async fetch request
	 * @type {(path: string, params: object) => Promise<{ok: boolean, url: string, arrayBuffer: Promise<Uint8Array>}>}
	 * @param {string} path the path to the file to fetch
	 * @param {object} params additional parameters to fetch with. Only used on browser
	 * @returns {Promise<{ok: boolean, url: string, arrayBuffer: Promise<Uint8Array>}>} The result of the request
	 */
	fetch: function(path, params) {
		if (is_browser) {
			return fetch (path, params);

		} else { // shells and node
			return new Promise ((resolve, reject) => {
				let bytes = null, error = null;
				try {
					if (is_node) {
						const fs = require ('fs');
						const buffer = fs.readFileSync(path);
						bytes = buffer.buffer;
					} else {
						bytes = read (path, 'binary');
					}
				} catch (exc) {
					error = exc;
				}
				const response = { 
					ok: (bytes && !error), 
					url: path,
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
			});
		}
	}
};
IOHandler.init();
// end of all the nice shell glue code.

function test_exit (exit_code) {
	if (is_browser) {
		// Notify the selenium script
		Module.exit_code = exit_code;
		console.log ("WASM EXIT " + exit_code);
		const tests_done_elem = document.createElement ("label");
		tests_done_elem.id = "tests_done";
		tests_done_elem.innerHTML = exit_code.toString ();
		document.body.appendChild (tests_done_elem);
	} else if (is_node) {
		Module.exit_code = exit_code;
		console.log ("WASM EXIT " + exit_code);
	} else {
		Module.wasm_exit (exit_code);
	}
}

function fail_exec (reason) {
	console.error (reason);
	test_exit (1);
}

// Preprocess arguments
console.log("Arguments: " + testArguments);
let profilers = [];
let setenv = {};
let runtime_args = [];
let enable_gc = true;
let working_dir='/';
while (testArguments !== undefined && testArguments.length > 0) {
	if (testArguments [0].startsWith ("--profile=")) {
		const arg = testArguments [0].substring ("--profile=".length);

		profilers.push (arg);

		testArguments = testArguments.slice (1);
	} else if (testArguments [0].startsWith ("--setenv=")) {
		const arg = testArguments [0].substring ("--setenv=".length);
		const parts = arg.split ('=');
		if (parts.length != 2)
			fail_exec ("Error: malformed argument: '" + testArguments [0]);
		setenv [parts [0]] = parts [1];
		testArguments = testArguments.slice (1);
	} else if (testArguments [0].startsWith ("--runtime-arg=")) {
		const arg = testArguments [0].substring ("--runtime-arg=".length);
		runtime_args = testArguments.push (arg);
		testArguments = testArguments.slice (1);
	} else if (testArguments [0] == "--disable-on-demand-gc") {
		enable_gc = false;
		testArguments = testArguments.slice (1);
	} else if (testArguments [0].startsWith ("--working-dir=")) {
		const arg = testArguments [0].substring ("--working-dir=".length);
		working_dir = arg;
		testArguments = testArguments.slice (1);
	} else {
		break;
	}
}

// cheap way to let the testing infrastructure know we're running in a browser context (or not)
setenv["IsBrowserDomSupported"] = is_browser.toString().toLowerCase();

// must be var as dotnet.js uses it
var Module = {
	mainScriptUrlOrBlob: "dotnet.js",
	config: null,

	/** Called before the runtime is loaded and before it is run
	 * @type {() => Promise<void>}
	 */
	preInit: async function() {
		await MONO.mono_wasm_load_config("./mono-config.json"); // sets Module.config implicitly
	},

	/** Called after an exception occurs during execution
	 * @type {(x: string|number=) => void}
	 * @param {string|number} x error message
	 */
	onAbort: function(x) {
		console.log ("ABORT: " + x);
		const err = new Error();
		console.log ("Stacktrace: \n");
		console.error (err.stack);
		test_exit (1);
	},

	/** Called after the runtime is loaded but before it is run mostly prepares runtime and config for the tests
	 * @type {() => void}
	 */
	onRuntimeInitialized: function () {
		// Have to set env vars here to enable setting MONO_LOG_LEVEL etc.
		for (let variable in setenv) {
			Module.MONO.mono_wasm_setenv (variable, setenv [variable]);
		}

		if (!enable_gc) {
			Module.ccall ('mono_wasm_enable_on_demand_gc', 'void', ['number'], [0]);
		}

		Module.config.loaded_cb = function () {
			let wds = Module.FS.stat (working_dir);
			if (wds === undefined || !Module.FS.isDir (wds.mode)) {
				fail_exec (`Could not find working directory ${working_dir}`);
				return;
			}

			Module.FS.chdir (working_dir);
			App.init ();
		};
		Module.config.fetch_file_cb = function (asset) {
			// console.log("fetch_file_cb('" + asset + "')");
			// for testing purposes add BCL assets to VFS until we special case File.Open
			// to identify when an assembly from the BCL is being open and resolve it correctly.
			/*
			var content = new Uint8Array (read (asset, 'binary'));
			var path = asset.substr(Module.config.deploy_prefix.length);
			IOHandler.writeContentToFile(content, path);
			*/
			return IOHandler.fetch (asset, { credentials: 'same-origin' });
		};

		Module.MONO.mono_load_runtime_and_bcl_args (Module.config);
	},
};

globalThis.Module = Module; // needed so that dotnet.js can access the module

const App = {
	/** Runs the tests (runtime is now loaded and running)
	 * @type {() => void}
	 */
	init: function () {
		const wasm_set_main_args = Module.cwrap ('mono_wasm_set_main_args', 'void', ['number', 'number']);
		const wasm_strdup = Module.cwrap ('mono_wasm_strdup', 'number', ['string']);

		Module.wasm_exit = Module.cwrap ('mono_wasm_exit', 'void', ['number']);

		console.info("Initializing.....");

		for (let i = 0; i < profilers.length; ++i) {
			const init = Module.cwrap ('mono_wasm_load_profiler_' + profilers [i], 'void', ['string']);
			init ("");
		}

		if (testArguments.length == 0) {
			fail_exec ("Missing required --run argument");
			return;
		}

		if (testArguments[0] == "--regression") {
			const exec_regression = Module.cwrap ('mono_wasm_exec_regression', 'number', ['number', 'string']);

			let res = 0;
				try {
					res = exec_regression (10, testArguments[1]);
					console.log ("REGRESSION RESULT: " + res);
				} catch (e) {
					console.error ("ABORT: " + e);
					console.error (e.stack);
					res = 1;
				}

			if (res)
				fail_exec ("REGRESSION TEST FAILED");

			return;
		}

		if (runtime_args.length > 0)
			Module.MONO.mono_wasm_set_runtime_options (runtime_args);

		if (testArguments[0] == "--run") {
			// Run an exe
			if (testArguments.length == 1) {
				fail_exec ("Error: Missing main executable argument.");
				return;
			}

			main_assembly_name = testArguments[1];
			const app_args = testArguments.slice (2);

			const main_argc = testArguments.length - 2 + 1;
			const main_argv = Module._malloc (main_argc * 4);
			let aindex = 0;
			Module.setValue (main_argv + (aindex * 4), wasm_strdup (testArguments [1]), "i32");
			aindex += 1;
			for (let i = 2; i < testArguments.length; ++i) {
				Module.setValue (main_argv + (aindex * 4), wasm_strdup (testArguments [i]), "i32");
				aindex += 1;
			}
			wasm_set_main_args (main_argc, main_argv);

			// Automatic signature isn't working correctly
			const result = Module.mono_call_assembly_entry_point (main_assembly_name, [app_args], "m");
			const onError = function (error)
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
			fail_exec ("Unhandled argument: " + testArguments [0]);
		}
	},

	/** Runs a particular test
	 * @type {(method_name: string, args: any[], signature: any?) => return number}
	 */
	call_test_method: function (method_name, args, signature) {
		// note: arguments here is the array of arguments passsed to this function
		if ((arguments.length > 2) && (typeof (signature) !== "string"))
			throw new Error("Invalid number of arguments for call_test_method");

		const fqn = "[System.Private.Runtime.InteropServices.JavaScript.Tests]System.Runtime.InteropServices.JavaScript.Tests.HelperMarshal:" + method_name;
		try {
			return Module.BINDING.call_static_method(fqn, args || [], signature);
		} catch (exc) {
			console.error("exception thrown in", fqn);
			throw exc;
		}
	}
};

// load the config and runtime files which will start the runtime init and subsiquently the tests
// uses promise chain as loading is async but we can't use await here
IOHandler
	.load ("./dotnet.js")
	.catch(function(err) {
		console.error(err);
		fail_exec("failed to load the mono-config.js or dotnet.js files");
	});
