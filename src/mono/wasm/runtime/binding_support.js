// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

"use strict";

var BindingSupportLib = {
	$BINDING__postset: 'BINDING.export_functions (Module);',
	$BINDING: {
		BINDING_ASM: "[System.Private.Runtime.InteropServices.JavaScript]System.Runtime.InteropServices.JavaScript.Runtime",

		// this is array, not map. We maintain list of gaps in _js_handle_free_list so that it could be as compact as possible
		_cs_owned_objects_by_js_handle: [],
		_js_handle_free_list: [],
		_next_js_handle: 1,

		ws_send_buffer_blocking_threshold: 65536,

		mono_wasm_marshal_enum_as_int: true,
		mono_bindings_init: function (binding_asm) {
			this.BINDING_ASM = binding_asm;
		},

		export_functions: function (module) {
			module ["mono_bindings_init"] = BINDING.mono_bindings_init.bind(BINDING);
			module ["mono_bind_method"] = BINDING.bind_method.bind(BINDING);
			module ["mono_method_invoke"] = BINDING.call_method.bind(BINDING);
			module ["mono_method_get_call_signature"] = BINDING.mono_method_get_call_signature.bind(BINDING);
			module ["mono_method_resolve"] = BINDING.resolve_method_fqn.bind(BINDING);
			module ["mono_bind_static_method"] = BINDING.bind_static_method.bind(BINDING);
			module ["mono_call_static_method"] = BINDING.call_static_method.bind(BINDING);
			module ["mono_bind_assembly_entry_point"] = BINDING.bind_assembly_entry_point.bind(BINDING);
			module ["mono_call_assembly_entry_point"] = BINDING.call_assembly_entry_point.bind(BINDING);
			module ["mono_intern_string"] = BINDING.mono_intern_string.bind(BINDING);
		},

		bindings_lazy_init: function () {
			if (this.init)
				return;

			// avoid infinite recursion
			this.init = true;
			this.wasm_type_symbol = Symbol.for("wasm type");
			this.js_owned_gc_handle_symbol = Symbol.for("wasm js_owned_gc_handle");
			this.cs_owned_js_handle_symbol = Symbol.for("wasm cs_owned_js_handle");
			this.delegate_invoke_symbol = Symbol.for("wasm delegate_invoke");
			this.delegate_invoke_signature_symbol = Symbol.for("wasm delegate_invoke_signature");
			this.promise_control_symbol = Symbol.for("wasm promise_control");
			this.listener_registration_count_symbol = Symbol.for("wasm listener_registration_count");
			this.wasm_ws_pending_send_buffer = Symbol.for("wasm ws_pending_send_buffer");
			this.wasm_ws_pending_send_buffer_offset = Symbol.for("wasm ws_pending_send_buffer_offset");
			this.wasm_ws_pending_send_buffer_type = Symbol.for("wasm ws_pending_send_buffer_type");
			this.wasm_ws_pending_receive_event_queue = Symbol.for("wasm ws_pending_receive_event_queue");
			this.wasm_ws_pending_receive_promise_queue = Symbol.for("wasm ws_pending_receive_promise_queue");
			this.wasm_ws_pending_open_promise = Symbol.for("wasm ws_pending_open_promise");
			this.wasm_ws_pending_close_promises = Symbol.for("wasm ws_pending_close_promises");
			this.wasm_ws_pending_send_promises = Symbol.for("wasm ws_pending_send_promises");
			this.wasm_ws_is_aborted = Symbol.for("wasm ws_is_aborted");

			// please keep System.Runtime.InteropServices.JavaScript.Runtime.MappedType in sync
			Object.prototype[this.wasm_type_symbol] = 0;
			Array.prototype[this.wasm_type_symbol] = 1;
			ArrayBuffer.prototype[this.wasm_type_symbol] = 2;
			DataView.prototype[this.wasm_type_symbol] = 3;
			Function.prototype[this.wasm_type_symbol] =  4;
			Map.prototype[this.wasm_type_symbol] = 5;
			if (typeof SharedArrayBuffer !== 'undefined')
				SharedArrayBuffer.prototype[this.wasm_type_symbol] =  6;
			Int8Array.prototype[this.wasm_type_symbol] = 10;
			Uint8Array.prototype[this.wasm_type_symbol] = 11;
			Uint8ClampedArray.prototype[this.wasm_type_symbol] = 12;
			Int16Array.prototype[this.wasm_type_symbol] = 13;
			Uint16Array.prototype[this.wasm_type_symbol] = 14;
			Int32Array.prototype[this.wasm_type_symbol] = 15;
			Uint32Array.prototype[this.wasm_type_symbol] = 16;
			Float32Array.prototype[this.wasm_type_symbol] = 17;
			Float64Array.prototype[this.wasm_type_symbol] = 18;

			this.assembly_load = Module.cwrap ('mono_wasm_assembly_load', 'number', ['string']);
			this.find_corlib_class = Module.cwrap ('mono_wasm_find_corlib_class', 'number', ['string', 'string']);
			this.find_class = Module.cwrap ('mono_wasm_assembly_find_class', 'number', ['number', 'string', 'string']);
			this._find_method = Module.cwrap ('mono_wasm_assembly_find_method', 'number', ['number', 'string', 'number']);
			this.invoke_method = Module.cwrap ('mono_wasm_invoke_method', 'number', ['number', 'number', 'number', 'number']);
			this.mono_string_get_utf8 = Module.cwrap ('mono_wasm_string_get_utf8', 'number', ['number']);
			this.mono_wasm_string_from_utf16 = Module.cwrap ('mono_wasm_string_from_utf16', 'number', ['number', 'number']);
			this.mono_get_obj_type = Module.cwrap ('mono_wasm_get_obj_type', 'number', ['number']);
			this.mono_array_length = Module.cwrap ('mono_wasm_array_length', 'number', ['number']);
			this.mono_array_get = Module.cwrap ('mono_wasm_array_get', 'number', ['number', 'number']);
			this.mono_obj_array_new = Module.cwrap ('mono_wasm_obj_array_new', 'number', ['number']);
			this.mono_obj_array_set = Module.cwrap ('mono_wasm_obj_array_set', 'void', ['number', 'number', 'number']);
			this.mono_wasm_register_bundled_satellite_assemblies = Module.cwrap ('mono_wasm_register_bundled_satellite_assemblies', 'void', [ ]);
			this.mono_wasm_try_unbox_primitive_and_get_type = Module.cwrap ('mono_wasm_try_unbox_primitive_and_get_type', 'number', ['number', 'number']);
			this.mono_wasm_box_primitive = Module.cwrap ('mono_wasm_box_primitive', 'number', ['number', 'number', 'number']);
			this.mono_wasm_intern_string = Module.cwrap ('mono_wasm_intern_string', 'number', ['number']);
			this.assembly_get_entry_point = Module.cwrap ('mono_wasm_assembly_get_entry_point', 'number', ['number']);
			this.mono_wasm_get_delegate_invoke = Module.cwrap ('mono_wasm_get_delegate_invoke', 'number', ['number']);
			this.mono_wasm_string_array_new = Module.cwrap ('mono_wasm_string_array_new', 'number', ['number']);

			this._box_buffer = Module._malloc(16);
			this._unbox_buffer = Module._malloc(16);
			this._class_int32 = this.find_corlib_class ("System", "Int32");
			this._class_uint32 = this.find_corlib_class ("System", "UInt32");
			this._class_double = this.find_corlib_class ("System", "Double");
			this._class_boolean = this.find_corlib_class ("System", "Boolean");

			// receives a byteoffset into allocated Heap with a size.
			this.mono_typed_array_new = Module.cwrap ('mono_wasm_typed_array_new', 'number', ['number','number','number','number']);

			var binding_fqn_asm = this.BINDING_ASM.substring(this.BINDING_ASM.indexOf ("[") + 1, this.BINDING_ASM.indexOf ("]")).trim();
			var binding_fqn_class = this.BINDING_ASM.substring (this.BINDING_ASM.indexOf ("]") + 1).trim();

			this.binding_module = this.assembly_load (binding_fqn_asm);
			if (!this.binding_module)
				throw "Can't find bindings module assembly: " + binding_fqn_asm;

			var namespace = null, classname = null;
			if (binding_fqn_class !== null && typeof binding_fqn_class !== "undefined")
			{
				namespace = "System.Runtime.InteropServices.JavaScript";
				classname = binding_fqn_class.length > 0 ? binding_fqn_class : "Runtime";
				if (binding_fqn_class.indexOf(".") != -1) {
					var idx = binding_fqn_class.lastIndexOf(".");
					namespace = binding_fqn_class.substring (0, idx);
					classname = binding_fqn_class.substring (idx + 1);
				}
			}

			var wasm_runtime_class = this.find_class (this.binding_module, namespace, classname);
			if (!wasm_runtime_class)
				throw "Can't find " + binding_fqn_class + " class";

			var get_method = function(method_name) {
				var res = BINDING.find_method (wasm_runtime_class, method_name, -1);
				if (!res)
					throw "Can't find method " + namespace + "." + classname + ":" + method_name;
				return res;
			};

			var bind_runtime_method = function (method_name, signature) {
				var method = get_method (method_name);
				return BINDING.bind_method (method, 0, signature, "BINDINGS_" + method_name);
			};

			this.get_call_sig = get_method ("GetCallSignature");

			// NOTE: The bound methods have a _ prefix on their names to ensure
			//  that any code relying on the old get_method/call_method pattern will
			//  break in a more understandable way.

			this._get_cs_owned_object_by_js_handle = bind_runtime_method ("GetCSOwnedObjectByJSHandle", "ii!");
			this._get_cs_owned_object_js_handle = bind_runtime_method ("GetCSOwnedObjectJSHandle", 'mi');
			this._try_get_cs_owned_object_js_handle = bind_runtime_method ("TryGetCSOwnedObjectJSHandle", "mi");
			this._create_cs_owned_proxy = bind_runtime_method ("CreateCSOwnedProxy", "iii!");

			this._get_js_owned_object_by_gc_handle = bind_runtime_method ("GetJSOwnedObjectByGCHandle", "i!");
			this._get_js_owned_object_gc_handle = bind_runtime_method ("GetJSOwnedObjectGCHandle", "m");
			this._release_js_owned_object_by_gc_handle = bind_runtime_method ("ReleaseJSOwnedObjectByGCHandle", "i");

			this._create_tcs = bind_runtime_method ("CreateTaskSource","");
			this._set_tcs_result = bind_runtime_method ("SetTaskSourceResult","io");
			this._set_tcs_failure = bind_runtime_method ("SetTaskSourceFailure","is");
			this._get_tcs_task = bind_runtime_method ("GetTaskSourceTask","i!");
			this._task_from_result = bind_runtime_method ("TaskFromResult","o!");
			this._setup_js_cont = bind_runtime_method ("SetupJSContinuation", "mo");
			
			this._object_to_string = bind_runtime_method ("ObjectToString", "m");
			this._get_date_value = bind_runtime_method ("GetDateValue", "m");
			this._create_date_time = bind_runtime_method ("CreateDateTime", "d!");
			this._create_uri = bind_runtime_method ("CreateUri","s!");
			this._is_simple_array = bind_runtime_method ("IsSimpleArray", "m");

			this._are_promises_supported = ((typeof Promise === "object") || (typeof Promise === "function")) && (typeof Promise.resolve === "function");
			this.isThenable = (js_obj) => {
				// When using an external Promise library like Bluebird the Promise.resolve may not be sufficient
				// to identify the object as a Promise.
				return Promise.resolve(js_obj) === js_obj ||
						((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function")
			};

			this._empty_string = "";
			this._empty_string_ptr = 0;
			this._interned_string_full_root_buffers = [];
			this._interned_string_current_root_buffer = null;
			this._interned_string_current_root_buffer_count = 0;
			this._interned_js_string_table = new Map ();

			this._js_owned_object_table = new Map ();
			// NOTE: FinalizationRegistry and WeakRef are missing on Safari below 14.1
			this._use_finalization_registry = typeof globalThis.FinalizationRegistry === "function";
			this._use_weak_ref = typeof globalThis.WeakRef === "function";

			if (this._use_finalization_registry) {
				this._js_owned_object_registry = new globalThis.FinalizationRegistry(this._js_owned_object_finalized.bind(this));
			}
		},

		_js_owned_object_finalized: function (gc_handle) {
			// The JS object associated with this gc_handle has been collected by the JS GC.
			// As such, it's not possible for this gc_handle to be invoked by JS anymore, so
			//  we can release the tracking weakref (it's null now, by definition),
			//  and tell the C# side to stop holding a reference to the managed object.
			// "The FinalizationRegistry callback is called potentially multiple times"
			if (this._js_owned_object_table.delete(gc_handle)) {
				this._release_js_owned_object_by_gc_handle(gc_handle);
			}
		},

		_lookup_js_owned_object: function (gc_handle) {
			if (!gc_handle)
				return null;
			var wr = this._js_owned_object_table.get(gc_handle);
			if (wr) {
				return wr.deref();
				// TODO: could this be null before _js_owned_object_finalized was called ?
				// TODO: are there race condition consequences ?
			}
			return null;
		},

		_register_js_owned_object: function (gc_handle, js_obj) {
			var wr;
			if (this._use_weak_ref) {
				wr = new WeakRef(js_obj);
			}
			else {
				// this is trivial WeakRef replacement, which holds strong refrence, instead of weak one, when the browser doesn't support it
				wr = {
					deref: () => {
						return js_obj;
					}
				}
			}

			this._js_owned_object_table.set(gc_handle, wr);
		},

		_wrap_js_thenable_as_task: function (thenable) {
			this.bindings_lazy_init ();
			if (!thenable)
				return null;

			// hold strong JS reference to thenable while in flight
			// ideally, this should be hold alive by lifespan of the resulting C# Task, but this is good cheap aproximation
			var thenable_js_handle = BINDING.mono_wasm_get_js_handle(thenable);

			// Note that we do not implement promise/task roundtrip. 
			// With more complexity we could recover original instance when this Task is marshaled back to JS.
			// TODO optimization: return the tcs.Task on this same call instead of _get_tcs_task
			const tcs_gc_handle = this._create_tcs();
			thenable.then ((result) => {
				this._set_tcs_result(tcs_gc_handle, result);
				// let go of the thenable reference
				this._mono_wasm_release_js_handle(thenable_js_handle);

				// when FinalizationRegistry is not supported by this browser, we will do immediate cleanup after promise resolve/reject
				if (!this._use_finalization_registry) {
					this._release_js_owned_object_by_gc_handle(tcs_gc_handle);
				}
			}, (reason) => {
				this._set_tcs_failure(tcs_gc_handle, reason ? reason.toString() : "");
				// let go of the thenable reference
				this._mono_wasm_release_js_handle(thenable_js_handle);

				// when FinalizationRegistry is not supported by this browser, we will do immediate cleanup after promise resolve/reject
				if (!this._use_finalization_registry) {
					this._release_js_owned_object_by_gc_handle(tcs_gc_handle);
				}
			});

			// collect the TaskCompletionSource with its Task after js doesn't hold the thenable anymore
			if (this._use_finalization_registry) {
				this._js_owned_object_registry.register(thenable, tcs_gc_handle);
			}

			// returns raw pointer to tcs.Task
			return {
				task_ptr: this._get_tcs_task(tcs_gc_handle),
				then_js_handle: thenable_js_handle,
			};
		},
		_create_cancelable_promise: function (afterResolve, afterReject) {
			var promise_control = null;
			const promise = new Promise(function (resolve, reject) {
				promise_control = {
					isDone: false,
					resolve: (data) => {
						if (!promise_control.isDone) {
							promise_control.isDone = true;
							resolve(data);
							if (afterResolve) {
								afterResolve();
							}
						}
					},
					reject: (reason) => {
						if (!promise_control.isDone) {
							promise_control.isDone = true;
							reject(reason);
							if (afterReject) {
								afterReject();
							}
						}
					}
				};
			});
			promise[this.promise_control_symbol] = promise_control;
			return { promise, promise_control }
		},
		_unbox_task_root_as_promise: function (root) {
			this.bindings_lazy_init ();
			const self = this;
			if (root.value === 0)
				return null;

			if (!this._are_promises_supported)
				throw new Error ("Promises are not supported thus 'System.Threading.Tasks.Task' can not work in this context.");

			// get strong reference to Task
			const gc_handle = this._get_js_owned_object_gc_handle(root.value);

			// see if we have js owned instance for this gc_handle already
			var result = this._lookup_js_owned_object(gc_handle);

			// If the promise for this gc_handle was already collected (or was never created)
			if (!result) {
				const explicitFinalization = self._use_finalization_registry
					? null
					: () => self._js_owned_object_finalized(gc_handle);

				const { promise, promise_control } = this._create_cancelable_promise(explicitFinalization, explicitFinalization);

				// note that we do not implement promise/task roundtrip
				// With more complexity we could recover original instance when this promise is marshaled back to C#.
				result = promise;

				// register C# side of the continuation
				this._setup_js_cont(root.value, promise_control);

				// register for GC of the Task after the JS side is done with the promise
				if (this._use_finalization_registry) {
					this._js_owned_object_registry.register(result, gc_handle);
				}

				// register for instance reuse
				this._register_js_owned_object(gc_handle, result);
			}

			return result;
		},

		_unbox_ref_type_root_as_js_object: function (root) {
			this.bindings_lazy_init ();
			if (root.value === 0)
				return null;

			// this could be JSObject proxy of a js native object
			// we don't need in-flight reference as we already have it rooted here
			var js_handle = this._try_get_cs_owned_object_js_handle (root.value, false);
			if (js_handle) {
				if (js_handle===-1){
					throw new Error("Cannot access a disposed JSObject at " + root.value);
				}
				return this.mono_wasm_get_jsobj_from_js_handle(js_handle);
			}
			// otherwise this is C# only object
	
			// get strong reference to Object
			const gc_handle = this._get_js_owned_object_gc_handle(root.value);

			// see if we have js owned instance for this gc_handle already
			var result = this._lookup_js_owned_object(gc_handle);

			// If the JS object for this gc_handle was already collected (or was never created)
			if (!result) {
				result = {};

				// keep the gc_handle so that we could easily convert it back to original C# object for roundtrip
				result[BINDING.js_owned_gc_handle_symbol]=gc_handle;

				// NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
				if (this._use_finalization_registry) {
					// register for GC of the C# object after the JS side is done with the object
					this._js_owned_object_registry.register(result, gc_handle);
				}

				// register for instance reuse
				// NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef
				this._register_js_owned_object(gc_handle, result);
			}

			return result;
		},

		_wrap_delegate_root_as_function: function (root) {
			this.bindings_lazy_init ();
			if (root.value === 0)
				return null;

			// get strong reference to the Delegate
			const gc_handle = this._get_js_owned_object_gc_handle(root.value);
			return this._wrap_delegate_gc_handle_as_function(gc_handle);
		},

		_wrap_delegate_gc_handle_as_function: function (gc_handle, after_listener_callback) {
			this.bindings_lazy_init ();

			// see if we have js owned instance for this gc_handle already
			var result = this._lookup_js_owned_object(gc_handle);

			// If the function for this gc_handle was already collected (or was never created)
			if (!result) {
				// note that we do not implement function/delegate roundtrip
				result = function() {
					const delegateRoot = MONO.mono_wasm_new_root (BINDING.get_js_owned_object_by_gc_handle(gc_handle));
					try {
						const res = BINDING.call_method(result[BINDING.delegate_invoke_symbol], delegateRoot.value, result[BINDING.delegate_invoke_signature_symbol], arguments);
						if (after_listener_callback) { 
							after_listener_callback(); 
						}
						return res;
					} finally {
						delegateRoot.release();
					}
				};

				// bind the method
				const delegateRoot = MONO.mono_wasm_new_root (BINDING.get_js_owned_object_by_gc_handle(gc_handle));
				try {
					if (typeof result[BINDING.delegate_invoke_symbol] === "undefined"){
						result[BINDING.delegate_invoke_symbol] = BINDING.mono_wasm_get_delegate_invoke(delegateRoot.value);
						if (!result[BINDING.delegate_invoke_symbol]){
							throw new Error("System.Delegate Invoke method can not be resolved.");
						}
					}

					if (typeof result[BINDING.delegate_invoke_signature_symbol] === "undefined"){
						result[BINDING.delegate_invoke_signature_symbol] = Module.mono_method_get_call_signature (result[BINDING.delegate_invoke_symbol], delegateRoot.value);
					}
				} finally {
					delegateRoot.release();
				}

				// NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry. Except in case of EventListener where we cleanup after unregistration.
				if (this._use_finalization_registry) {
					// register for GC of the deleate after the JS side is done with the function
					this._js_owned_object_registry.register(result, gc_handle);
				}

				// register for instance reuse
				// NOTE: this would be leaking C# objects when the browser doesn't support FinalizationRegistry/WeakRef. Except in case of EventListener where we cleanup after unregistration.
				this._register_js_owned_object(gc_handle, result);
			}

			return result;
		},

		wrap_error: function (is_exception, ex) {
			var res = "unknown exception";
			if (ex) {
				res = ex.toString()
			}
			if (is_exception) {
				setValue(is_exception, 1, "i32");
			}
			return BINDING.js_string_to_mono_string (res);
		},

		// Ensures the string is already interned on both the managed and JavaScript sides,
		//  then returns the interned string value (to provide fast reference comparisons like C#)
		mono_intern_string: function (string) {
			if (string.length === 0)
				return this._empty_string;

			var ptr = this.js_string_to_mono_string_interned (string);
			var result = MONO.interned_string_table.get (ptr);
			return result;
		},

		_store_string_in_intern_table: function (string, ptr, internIt) {
			if (!ptr)
				throw new Error ("null pointer passed to _store_string_in_intern_table");
			else if (typeof (ptr) !== "number")
				throw new Error (`non-pointer passed to _store_string_in_intern_table: ${typeof(ptr)}`);
			
			const internBufferSize = 8192;

			if (this._interned_string_current_root_buffer_count >= internBufferSize) {
				this._interned_string_full_root_buffers.push (this._interned_string_current_root_buffer);
				this._interned_string_current_root_buffer = null;
			}
			if (!this._interned_string_current_root_buffer) {
				this._interned_string_current_root_buffer = MONO.mono_wasm_new_root_buffer (internBufferSize, "interned strings");
				this._interned_string_current_root_buffer_count = 0;
			}

			var rootBuffer = this._interned_string_current_root_buffer;
			var index = this._interned_string_current_root_buffer_count++;
			rootBuffer.set (index, ptr);

			// Store the managed string into the managed intern table. This can theoretically
			//  provide a different managed object than the one we passed in, so update our
			//  pointer (stored in the root) with the result.
			if (internIt)
				rootBuffer.set (index, ptr = this.mono_wasm_intern_string (ptr));

			if (!ptr)
				throw new Error ("mono_wasm_intern_string produced a null pointer");

			this._interned_js_string_table.set (string, ptr);
			if (!MONO.interned_string_table)
				MONO.interned_string_table = new Map();
			MONO.interned_string_table.set (ptr, string);

			if ((string.length === 0) && !this._empty_string_ptr)
				this._empty_string_ptr = ptr;
			
			return ptr;
		},

		js_string_to_mono_string_interned: function (string) {
			var text = (typeof (string) === "symbol")
				? (string.description || Symbol.keyFor(string) || "<unknown Symbol>")
				: string;
			
			if ((text.length === 0) && this._empty_string_ptr)
				return this._empty_string_ptr;

			var ptr = this._interned_js_string_table.get (string);
			if (ptr)
				return ptr;

			ptr = this.js_string_to_mono_string_new (text);
			ptr = this._store_string_in_intern_table (string, ptr, true);

			return ptr;
		},

		js_string_to_mono_string: function (string) {
			if (string === null)
				return null;
			else if (typeof (string) === "symbol")
				return this.js_string_to_mono_string_interned (string);
			else if (typeof (string) !== "string")
				throw new Error ("Expected string argument, got "+ typeof (string));

			// Always use an interned pointer for empty strings
			if (string.length === 0)
				return this.js_string_to_mono_string_interned (string);

			// Looking up large strings in the intern table will require the JS runtime to
			//  potentially hash them and then do full byte-by-byte comparisons, which is
			//  very expensive. Because we can not guarantee it won't happen, try to minimize
			//  the cost of this and prevent performance issues for large strings
			if (string.length <= 256) {
				var interned = this._interned_js_string_table.get (string);
				if (interned)
					return interned;
			}

			return this.js_string_to_mono_string_new (string);
		},
				
		js_string_to_mono_string_new: function (string) {
			var buffer = Module._malloc ((string.length + 1) * 2);
			var buffer16 = (buffer / 2) | 0;
			for (var i = 0; i < string.length; i++)
				Module.HEAP16[buffer16 + i] = string.charCodeAt (i);
			Module.HEAP16[buffer16 + string.length] = 0;
			var result = this.mono_wasm_string_from_utf16 (buffer, string.length);
			Module._free (buffer);
			return result;
		},

		find_method: function (klass, name, n) {
			var result = this._find_method(klass, name, n);
			if (result) {
				if (!this._method_descriptions)
					this._method_descriptions = new Map();
				this._method_descriptions.set(result, name);
			}
			return result;
		},

		get_js_obj: function (js_handle) {
			if (js_handle > 0)
				return this.mono_wasm_get_jsobj_from_js_handle(js_handle);
			return null;
		},

		_get_string_from_intern_table: function (mono_obj) {
			if (!MONO.interned_string_table)
				return undefined;
			return MONO.interned_string_table.get (mono_obj);
		},

		conv_string: function (mono_obj) {
			return MONO.string_decoder.copy (mono_obj);
		},

		is_nested_array: function (ele) {
			return this._is_simple_array(ele);
		},

		mono_array_to_js_array: function (mono_array) {
			if (mono_array === 0)
				return null;

			var arrayRoot = MONO.mono_wasm_new_root (mono_array);
			try {
				return this._mono_array_root_to_js_array (arrayRoot);
			} finally {
				arrayRoot.release();
			}
		},

		_mono_array_root_to_js_array: function (arrayRoot) {
			if (arrayRoot.value === 0)
				return null;

			let elemRoot = MONO.mono_wasm_new_root ();

			try {
				var len = this.mono_array_length (arrayRoot.value);
				var res = new Array (len);
				for (var i = 0; i < len; ++i)
				{
					elemRoot.value = this.mono_array_get (arrayRoot.value, i);

					if (this.is_nested_array (elemRoot.value))
						res[i] = this._mono_array_root_to_js_array (elemRoot);
					else
						res[i] = this._unbox_mono_obj_root (elemRoot);
				}
			} finally {
				elemRoot.release ();
			}

			return res;
		},

		js_array_to_mono_array: function (js_array, asString, should_add_in_flight) {
			var mono_array = asString ? this.mono_wasm_string_array_new (js_array.length) : this.mono_obj_array_new (js_array.length);
			let [arrayRoot, elemRoot] = MONO.mono_wasm_new_roots ([mono_array, 0]);

			try {
				for (var i = 0; i < js_array.length; ++i) {
					var obj = js_array[i];
					if (asString)
						obj = obj.toString ();

					elemRoot.value = this._js_to_mono_obj (should_add_in_flight, obj);
					this.mono_obj_array_set (arrayRoot.value, i, elemRoot.value);
				}

				return mono_array;
			} finally {
				MONO.mono_wasm_release_roots (arrayRoot, elemRoot);
			}
		},

		// this is only used from Blazor
		js_to_mono_obj: function (js_obj) {
			return this._js_to_mono_obj(false, js_obj)
		},

		// this is only used from Blazor
		unbox_mono_obj: function (mono_obj) {
			if (mono_obj === 0)
				return undefined;

			var root = MONO.mono_wasm_new_root (mono_obj);
			try {
				return this._unbox_mono_obj_root (root);
			} finally {
				root.release();
			}
		},

		_unbox_cs_owned_root_as_js_object: function (root) {
			// we don't need in-flight reference as we already have it rooted here
			var js_handle = this._get_cs_owned_object_js_handle(root.value, false);
			var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle (js_handle);
			return js_obj;
		},

		_unbox_mono_obj_root_with_known_nonprimitive_type: function (root, type) {
			if (root.value === undefined)
				throw new Error(`Expected a root but got ${root}`);
			
			//See MARSHAL_TYPE_ defines in driver.c
			switch (type) {
				case 26: // int64
				case 27: // uint64
					// TODO: Fix this once emscripten offers HEAPI64/HEAPU64 or can return them
					throw new Error ("int64 not available");
				case 3: // string
				case 29: // interned string
					return this.conv_string (root.value);
				case 4: //vts
					throw new Error ("no idea on how to unbox value types");
				case 5: // delegate
					return this._wrap_delegate_root_as_function (root);
				case 6: // Task
					return this._unbox_task_root_as_promise (root);
				case 7: // ref type
					return this._unbox_ref_type_root_as_js_object (root);
				case 10: // arrays
				case 11:
				case 12:
				case 13:
				case 14:
				case 15:
				case 16:
				case 17:
				case 18:
					throw new Error ("Marshalling of primitive arrays are not supported.  Use the corresponding TypedArray instead.");
				case 20: // clr .NET DateTime
					var dateValue = this._get_date_value(root.value);
					return new Date(dateValue);
				case 21: // clr .NET DateTimeOffset
					var dateoffsetValue = this._object_to_string (root.value);
					return dateoffsetValue;
				case 22: // clr .NET Uri
					var uriValue = this._object_to_string (root.value);
					return uriValue;
				case 23: // clr .NET SafeHandle/JSObject
					return this._unbox_cs_owned_root_as_js_object (root);
				case 30:
					return undefined;
				default:
					throw new Error (`no idea on how to unbox object kind ${type} at offset ${root.value} (root address is ${root.get_address()})`);
			}
		},

		_unbox_mono_obj_root: function (root) {
			if (root.value === 0)
				return undefined;

			var type = this.mono_wasm_try_unbox_primitive_and_get_type (root.value, this._unbox_buffer);
			switch (type) {
				case 1: // int
					return Module.HEAP32[this._unbox_buffer / 4];
				case 25: // uint32
					return Module.HEAPU32[this._unbox_buffer / 4];
				case 24: // float32
					return Module.HEAPF32[this._unbox_buffer / 4];
				case 2: // float64
					return Module.HEAPF64[this._unbox_buffer / 8];
				case 8: // boolean
					return (Module.HEAP32[this._unbox_buffer / 4]) !== 0;
				case 28: // char
					return String.fromCharCode(Module.HEAP32[this._unbox_buffer / 4]);
				default:
					return this._unbox_mono_obj_root_with_known_nonprimitive_type (root, type);
			}
		},

		// https://github.com/Planeshifter/emscripten-examples/blob/master/01_PassingArrays/sum_post.js
		js_typedarray_to_heap: function(typedArray){
			var numBytes = typedArray.length * typedArray.BYTES_PER_ELEMENT;
			var ptr = Module._malloc(numBytes);
			var heapBytes = new Uint8Array(Module.HEAPU8.buffer, ptr, numBytes);
			heapBytes.set(new Uint8Array(typedArray.buffer, typedArray.byteOffset, numBytes));
			return heapBytes;
		},

		_box_js_int: function (js_obj) {
			Module.HEAP32[this._box_buffer / 4] = js_obj;
			return this.mono_wasm_box_primitive (this._class_int32, this._box_buffer, 4);
		},

		_box_js_uint: function (js_obj) {
			Module.HEAPU32[this._box_buffer / 4] = js_obj;
			return this.mono_wasm_box_primitive (this._class_uint32, this._box_buffer, 4);
		},

		_box_js_double: function (js_obj) {
			Module.HEAPF64[this._box_buffer / 8] = js_obj;
			return this.mono_wasm_box_primitive (this._class_double, this._box_buffer, 8);
		},

		_box_js_bool: function (js_obj) {
			Module.HEAP32[this._box_buffer / 4] = js_obj ? 1 : 0;
			return this.mono_wasm_box_primitive (this._class_boolean, this._box_buffer, 4);
		},

		_js_to_mono_uri: function (should_add_in_flight, js_obj) {
			this.bindings_lazy_init ();

			switch (true) {
				case js_obj === null:
				case typeof js_obj === "undefined":
					return 0;
				case typeof js_obj === "symbol":
				case typeof js_obj === "string":
					return this._create_uri(js_obj)
				default:
					return this._extract_mono_obj (should_add_in_flight, js_obj);
			}
		},
		_js_to_mono_obj: function (should_add_in_flight, js_obj) {
			this.bindings_lazy_init ();

			switch (true) {
				case js_obj === null:
				case typeof js_obj === "undefined":
					return 0;
				case typeof js_obj === "number": {
					let result  = null;
					if ((js_obj | 0) === js_obj)
						result = this._box_js_int (js_obj);
					else if ((js_obj >>> 0) === js_obj)
						result = this._box_js_uint (js_obj);
					else
						result = this._box_js_double (js_obj);

					if (!result)
						throw new Error (`Boxing failed for ${js_obj}`);

					return result;
				} case typeof js_obj === "string":
					return this.js_string_to_mono_string (js_obj);
				case typeof js_obj === "symbol":
					return this.js_string_to_mono_string_interned (js_obj);
				case typeof js_obj === "boolean":
					return this._box_js_bool (js_obj);
				case this.isThenable(js_obj) === true:
					var { task_ptr } = this._wrap_js_thenable_as_task(js_obj);
					// task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
					return task_ptr;
				case js_obj.constructor.name === "Date":
					// getTime() is always UTC
					return this._create_date_time(js_obj.getTime());
				default:
					return this._extract_mono_obj (should_add_in_flight, js_obj);
			}
		},

		_extract_mono_obj: function (should_add_in_flight, js_obj) {
			if (js_obj === null || typeof js_obj === "undefined")
				return 0;

			var result = null;
			if (js_obj[BINDING.js_owned_gc_handle_symbol]) {
				// for js_owned_gc_handle we don't want to create new proxy
				// since this is strong gc_handle we don't need to in-flight reference
				result = this.get_js_owned_object_by_gc_handle (js_obj[BINDING.js_owned_gc_handle_symbol]);
				return result;
			}
			if (js_obj[BINDING.cs_owned_js_handle_symbol]) {
				result = this.get_cs_owned_object_by_js_handle (js_obj[BINDING.cs_owned_js_handle_symbol], should_add_in_flight);

				// It's possible the managed object corresponding to this JS object was collected,
				//  in which case we need to make a new one.
				if (!result) {
					delete js_obj[BINDING.cs_owned_js_handle_symbol];
				}
			}

			if (!result) {
				// Obtain the JS -> C# type mapping.
				const wasm_type = js_obj[this.wasm_type_symbol];
				const wasm_type_id = typeof wasm_type === "undefined" ? 0 : wasm_type;

				var js_handle = BINDING.mono_wasm_get_js_handle(js_obj);

				result = this._create_cs_owned_proxy(js_handle, wasm_type_id, should_add_in_flight);
			}

			return result;
		},

		has_backing_array_buffer: function (js_obj) {
			return typeof SharedArrayBuffer !== 'undefined'
				? js_obj.buffer instanceof ArrayBuffer || js_obj.buffer instanceof SharedArrayBuffer
				: js_obj.buffer instanceof ArrayBuffer;
		},

		js_typed_array_to_array : function (js_obj) {

			// JavaScript typed arrays are array-like objects and provide a mechanism for accessing
			// raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
			// split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
			//  is an object representing a chunk of data; it has no format to speak of, and offers no
			// mechanism for accessing its contents. In order to access the memory contained in a buffer,
			// you need to use a view. A view provides a context — that is, a data type, starting offset,
			// and number of elements — that turns the data into an actual typed array.
			// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
			if (!!(this.has_backing_array_buffer(js_obj) && js_obj.BYTES_PER_ELEMENT))
			{
				var arrayType = js_obj[this.wasm_type_symbol];
				var heapBytes = this.js_typedarray_to_heap(js_obj);
				var bufferArray = this.mono_typed_array_new(heapBytes.byteOffset, js_obj.length, js_obj.BYTES_PER_ELEMENT, arrayType);
				Module._free(heapBytes.byteOffset);
				return bufferArray;
			}
			else {
				throw new Error("Object '" + js_obj + "' is not a typed array");
			}


		},
		// Copy the existing typed array to the heap pointed to by the pinned array address
		// 	 typed array memory -> copy to heap -> address of managed pinned array
		typedarray_copy_to : function (typed_array, pinned_array, begin, end, bytes_per_element) {

			// JavaScript typed arrays are array-like objects and provide a mechanism for accessing
			// raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
			// split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
			//  is an object representing a chunk of data; it has no format to speak of, and offers no
			// mechanism for accessing its contents. In order to access the memory contained in a buffer,
			// you need to use a view. A view provides a context — that is, a data type, starting offset,
			// and number of elements — that turns the data into an actual typed array.
			// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
			if (!!(this.has_backing_array_buffer(typed_array) && typed_array.BYTES_PER_ELEMENT))
			{
				// Some sanity checks of what is being asked of us
				// lets play it safe and throw an error here instead of assuming to much.
				// Better safe than sorry later
				if (bytes_per_element !== typed_array.BYTES_PER_ELEMENT)
					throw new Error("Inconsistent element sizes: TypedArray.BYTES_PER_ELEMENT '" + typed_array.BYTES_PER_ELEMENT + "' sizeof managed element: '" + bytes_per_element + "'");

				// how much space we have to work with
				var num_of_bytes = (end - begin) * bytes_per_element;
				// how much typed buffer space are we talking about
				var view_bytes = typed_array.length * typed_array.BYTES_PER_ELEMENT;
				// only use what is needed.
				if (num_of_bytes > view_bytes)
					num_of_bytes = view_bytes;

				// offset index into the view
				var offset = begin * bytes_per_element;

				// Create a view over the heap pointed to by the pinned array address
				var heapBytes = new Uint8Array(Module.HEAPU8.buffer, pinned_array + offset, num_of_bytes);
				// Copy the bytes of the typed array to the heap.
				heapBytes.set(new Uint8Array(typed_array.buffer, typed_array.byteOffset, num_of_bytes));

				return num_of_bytes;
			}
			else {
				throw new Error("Object '" + typed_array + "' is not a typed array");
			}

		},
		// Copy the pinned array address from pinned_array allocated on the heap to the typed array.
		// 	 adress of managed pinned array -> copy from heap -> typed array memory
		typedarray_copy_from : function (typed_array, pinned_array, begin, end, bytes_per_element) {

			// JavaScript typed arrays are array-like objects and provide a mechanism for accessing
			// raw binary data. (...) To achieve maximum flexibility and efficiency, JavaScript typed arrays
			// split the implementation into buffers and views. A buffer (implemented by the ArrayBuffer object)
			//  is an object representing a chunk of data; it has no format to speak of, and offers no
			// mechanism for accessing its contents. In order to access the memory contained in a buffer,
			// you need to use a view. A view provides a context — that is, a data type, starting offset,
			// and number of elements — that turns the data into an actual typed array.
			// https://developer.mozilla.org/en-US/docs/Web/JavaScript/Typed_arrays
			if (!!(this.has_backing_array_buffer(typed_array) && typed_array.BYTES_PER_ELEMENT))
			{
				// Some sanity checks of what is being asked of us
				// lets play it safe and throw an error here instead of assuming to much.
				// Better safe than sorry later
				if (bytes_per_element !== typed_array.BYTES_PER_ELEMENT)
					throw new Error("Inconsistent element sizes: TypedArray.BYTES_PER_ELEMENT '" + typed_array.BYTES_PER_ELEMENT + "' sizeof managed element: '" + bytes_per_element + "'");

				// how much space we have to work with
				var num_of_bytes = (end - begin) * bytes_per_element;
				// how much typed buffer space are we talking about
				var view_bytes = typed_array.length * typed_array.BYTES_PER_ELEMENT;
				// only use what is needed.
				if (num_of_bytes > view_bytes)
					num_of_bytes = view_bytes;

				// Create a new view for mapping
				var typedarrayBytes = new Uint8Array(typed_array.buffer, 0, num_of_bytes);
				// offset index into the view
				var offset = begin * bytes_per_element;
				// Set view bytes to value from HEAPU8
				typedarrayBytes.set(Module.HEAPU8.subarray(pinned_array + offset, pinned_array + offset + num_of_bytes));
				return num_of_bytes;
			}
			else {
				throw new Error("Object '" + typed_array + "' is not a typed array");
			}

		},
		// Creates a new typed array from pinned array address from pinned_array allocated on the heap to the typed array.
		// 	 adress of managed pinned array -> copy from heap -> typed array memory
		typed_array_from : function (pinned_array, begin, end, bytes_per_element, type) {

			// typed array
			var newTypedArray = 0;

			switch (type)
			{
				case 5:
					newTypedArray = new Int8Array(end - begin);
					break;
				case 6:
					newTypedArray = new Uint8Array(end - begin);
					break;
				case 7:
					newTypedArray = new Int16Array(end - begin);
					break;
				case 8:
					newTypedArray = new Uint16Array(end - begin);
					break;
				case 9:
					newTypedArray = new Int32Array(end - begin);
					break;
				case 10:
					newTypedArray = new Uint32Array(end - begin);
					break;
				case 13:
					newTypedArray = new Float32Array(end - begin);
					break;
				case 14:
					newTypedArray = new Float64Array(end - begin);
					break;
				case 15:  // This is a special case because the typed array is also byte[]
					newTypedArray = new Uint8ClampedArray(end - begin);
					break;
			}

			this.typedarray_copy_from(newTypedArray, pinned_array, begin, end, bytes_per_element);
			return newTypedArray;
		},

		js_to_mono_enum: function (js_obj, method, parmIdx) {
			this.bindings_lazy_init ();

			if (typeof (js_obj) !== "number")
				throw new Error (`Expected numeric value for enum argument, got '${js_obj}'`);

			return js_obj | 0;
		},

		get_js_owned_object_by_gc_handle: function (gc_handle)
		{
			if(!gc_handle){
				return 0;
			}
			// this is always strong gc_handle
			return this._get_js_owned_object_by_gc_handle (gc_handle);
		},

		// when should_add_in_flight === true, the JSObject would be temporarily hold by Normal gc_handle, so that it would not get collected during transition to the managed stack.
		// its InFlight gc_handle would be freed when the instance arrives to managed side via Interop.Runtime.ReleaseInFlight
		get_cs_owned_object_by_js_handle: function (js_handle, should_add_in_flight)
		{
			if(!js_handle){
				return 0;
			}
			return this._get_cs_owned_object_by_js_handle (js_handle, should_add_in_flight);
		},

		mono_method_get_call_signature: function(method, mono_obj) {
			let instanceRoot = MONO.mono_wasm_new_root (mono_obj);
			try {
				this.bindings_lazy_init ();

				return this.call_method (this.get_call_sig, null, "im", [ method, instanceRoot.value ]);
			} finally {
				instanceRoot.release();
			}
		},

		_create_named_function: function (name, argumentNames, body, closure) {
			var result = null, closureArgumentList = null, closureArgumentNames = null;

			if (closure) {
				closureArgumentNames = Object.keys (closure);
				closureArgumentList = new Array (closureArgumentNames.length);
				for (var i = 0, l = closureArgumentNames.length; i < l; i++)
					closureArgumentList[i] = closure[closureArgumentNames[i]];
			}

			var constructor = this._create_rebindable_named_function (name, argumentNames, body, closureArgumentNames);
			result = constructor.apply (null, closureArgumentList);

			return result;
		},

		_create_rebindable_named_function: function (name, argumentNames, body, closureArgNames) {
			var strictPrefix = "\"use strict\";\r\n";
			var uriPrefix = "", escapedFunctionIdentifier = "";

			if (name) {
				uriPrefix = "//# sourceURL=https://mono-wasm.invalid/" + name + "\r\n";
				escapedFunctionIdentifier = name;
			} else {
				escapedFunctionIdentifier = "unnamed";
			}

			var rawFunctionText = "function " + escapedFunctionIdentifier + "(" +
				argumentNames.join(", ") +
				") {\r\n" +
				body +
				"\r\n};\r\n";

			var lineBreakRE = /\r(\n?)/g;

			rawFunctionText =
				uriPrefix + strictPrefix +
				rawFunctionText.replace(lineBreakRE, "\r\n    ") +
				`    return ${escapedFunctionIdentifier};\r\n`;

			var result = null, keys = null;

			if (closureArgNames) {
				keys = closureArgNames.concat ([rawFunctionText]);
			} else {
				keys = [rawFunctionText];
			}

			result = Function.apply (Function, keys);
			return result;
		},

		_create_primitive_converters: function () {
			var result = new Map ();
			result.set ('m', { steps: [{ }], size: 0});
			result.set ('s', { steps: [{ convert: this.js_string_to_mono_string.bind (this) }], size: 0, needs_root: true });
			result.set ('S', { steps: [{ convert: this.js_string_to_mono_string_interned.bind (this) }], size: 0, needs_root: true });
			// note we also bind first argument to false for both _js_to_mono_obj and _js_to_mono_uri, 
			// because we will root the reference, so we don't need in-flight reference
			// also as those are callback arguments and we don't have platform code which would release the in-flight reference on C# end
			result.set ('o', { steps: [{ convert: this._js_to_mono_obj.bind (this, false) }], size: 0, needs_root: true });
			result.set ('u', { steps: [{ convert: this._js_to_mono_uri.bind (this, false) }], size: 0, needs_root: true });

			// result.set ('k', { steps: [{ convert: this.js_to_mono_enum.bind (this), indirect: 'i64'}], size: 8});
			result.set ('j', { steps: [{ convert: this.js_to_mono_enum.bind (this), indirect: 'i32'}], size: 8});

			result.set ('i', { steps: [{ indirect: 'i32'}], size: 8});
			result.set ('l', { steps: [{ indirect: 'i64'}], size: 8});
			result.set ('f', { steps: [{ indirect: 'float'}], size: 8});
			result.set ('d', { steps: [{ indirect: 'double'}], size: 8});

			this._primitive_converters = result;
			return result;
		},

		_create_converter_for_marshal_string: function (args_marshal) {
			var primitiveConverters = this._primitive_converters;
			if (!primitiveConverters)
				primitiveConverters = this._create_primitive_converters ();

			var steps = [];
			var size = 0;
			var is_result_definitely_unmarshaled = false,
				is_result_possibly_unmarshaled = false,
				result_unmarshaled_if_argc = -1,
				needs_root_buffer = false;

			for (var i = 0; i < args_marshal.length; ++i) {
				var key = args_marshal[i];

				if (i === args_marshal.length - 1) {
					if (key === "!") {
						is_result_definitely_unmarshaled = true;
						continue;
					} else if (key === "m") {
						is_result_possibly_unmarshaled = true;
						result_unmarshaled_if_argc = args_marshal.length - 1;
					}
				} else if (key === "!")
					throw new Error ("! must be at the end of the signature");

				var conv = primitiveConverters.get (key);
				if (!conv)
					throw new Error ("Unknown parameter type " + key);

				var localStep = Object.create (conv.steps[0]);
				localStep.size = conv.size;
				if (conv.needs_root)
					needs_root_buffer = true;
				localStep.needs_root = conv.needs_root;
				localStep.key = args_marshal[i];
				steps.push (localStep);
				size += conv.size;
			}

			return {
				steps: steps, size: size, args_marshal: args_marshal,
				is_result_definitely_unmarshaled: is_result_definitely_unmarshaled,
				is_result_possibly_unmarshaled: is_result_possibly_unmarshaled,
				result_unmarshaled_if_argc: result_unmarshaled_if_argc,
				needs_root_buffer: needs_root_buffer
			};
		},

		_get_converter_for_marshal_string: function (args_marshal) {
			if (!this._signature_converters)
				this._signature_converters = new Map();

			var converter = this._signature_converters.get (args_marshal);
			if (!converter) {
				converter = this._create_converter_for_marshal_string (args_marshal);
				this._signature_converters.set (args_marshal, converter);
			}

			return converter;
		},

		_compile_converter_for_marshal_string: function (args_marshal) {
			var converter = this._get_converter_for_marshal_string (args_marshal);
			if (typeof (converter.args_marshal) !== "string")
				throw new Error ("Corrupt converter for '" + args_marshal + "'");

			if (converter.compiled_function && converter.compiled_variadic_function)
				return converter;

			var converterName = args_marshal.replace("!", "_result_unmarshaled");
			converter.name = converterName;

			var body = [];
			var argumentNames = ["buffer", "rootBuffer", "method"];

			// worst-case allocation size instead of allocating dynamically, plus padding
			var bufferSizeBytes = converter.size + (args_marshal.length * 4) + 16;

			// ensure the indirect values are 8-byte aligned so that aligned loads and stores will work
			var indirectBaseOffset = ((((args_marshal.length * 4) + 7) / 8) | 0) * 8;

			var closure = {};
			var indirectLocalOffset = 0;

			body.push (
				`if (!buffer) buffer = Module._malloc (${bufferSizeBytes});`,
				`var indirectStart = buffer + ${indirectBaseOffset};`,
				"var indirect32 = (indirectStart / 4) | 0, indirect64 = (indirectStart / 8) | 0;",
				"var buffer32 = (buffer / 4) | 0;",
				""
			);

			for (let i = 0; i < converter.steps.length; i++) {
				var step = converter.steps[i];
				var closureKey = "step" + i;
				var valueKey = "value" + i;

				var argKey = "arg" + i;
				argumentNames.push (argKey);

				if (step.convert) {
					closure[closureKey] = step.convert;
					body.push (`var ${valueKey} = ${closureKey}(${argKey}, method, ${i});`);
				} else {
					body.push (`var ${valueKey} = ${argKey};`);
				}

				if (step.needs_root)
					body.push (`rootBuffer.set (${i}, ${valueKey});`);

				if (step.indirect) {
					var heapArrayName = null;

					switch (step.indirect) {
						case "u32":
							heapArrayName = "HEAPU32";
							break;
						case "i32":
							heapArrayName = "HEAP32";
							break;
						case "float":
							heapArrayName = "HEAPF32";
							break;
						case "double":
							body.push (`Module.HEAPF64[indirect64 + ${(indirectLocalOffset / 8)}] = ${valueKey};`);
							break;
						case "i64":
							body.push (`Module.setValue (indirectStart + ${indirectLocalOffset}, ${valueKey}, 'i64');`);
							break;
						default:
							throw new Error ("Unimplemented indirect type: " + step.indirect);
					}

					if (heapArrayName)
						body.push (`Module.${heapArrayName}[indirect32 + ${(indirectLocalOffset / 4)}] = ${valueKey};`);

					body.push (`Module.HEAP32[buffer32 + ${i}] = indirectStart + ${indirectLocalOffset};`, "");
					indirectLocalOffset += step.size;
				} else {
					body.push (`Module.HEAP32[buffer32 + ${i}] = ${valueKey};`, "");
					indirectLocalOffset += 4;
				}
			}

			body.push ("return buffer;");

			var bodyJs = body.join ("\r\n"), compiledFunction = null, compiledVariadicFunction = null;
			try {
				compiledFunction = this._create_named_function("converter_" + converterName, argumentNames, bodyJs, closure);
				converter.compiled_function = compiledFunction;
			} catch (exc) {
				converter.compiled_function = null;
				console.warn("compiling converter failed for", bodyJs, "with error", exc);
				throw exc;
			}

			argumentNames = ["existingBuffer", "rootBuffer", "method", "args"];
			closure = {
				converter: compiledFunction
			};
			body = [
				"return converter(",
				"  existingBuffer, rootBuffer, method,"
			];

			for (let i = 0; i < converter.steps.length; i++) {
				body.push(
					"  args[" + i +
					(
						(i == converter.steps.length - 1)
							? "]"
							: "], "
					)
				);
			}

			body.push(");");

			var bodyJs = body.join ("\r\n");
			try {
				compiledVariadicFunction = this._create_named_function("variadic_converter_" + converterName, argumentNames, bodyJs, closure);
				converter.compiled_variadic_function = compiledVariadicFunction;
			} catch (exc) {
				converter.compiled_variadic_function = null;
				console.warn("compiling converter failed for", bodyJs, "with error", exc);
				throw exc;
			}

			converter.scratchRootBuffer = null;
			converter.scratchBuffer = 0 | 0;

			return converter;
		},

		_verify_args_for_method_call: function (args_marshal, args) {
			var has_args = args && (typeof args === "object") && args.length > 0;
			var has_args_marshal = typeof args_marshal === "string";

			if (has_args) {
				if (!has_args_marshal)
					throw new Error ("No signature provided for method call.");
				else if (args.length > args_marshal.length)
					throw new Error ("Too many parameter values. Expected at most " + args_marshal.length + " value(s) for signature " + args_marshal);
			}

			return has_args_marshal && has_args;
		},

		_get_buffer_for_method_call: function (converter) {
			if (!converter)
				return 0;

			var result = converter.scratchBuffer;
			converter.scratchBuffer = 0;
			return result;
		},

		_get_args_root_buffer_for_method_call: function (converter) {
			if (!converter)
				return null;

			if (!converter.needs_root_buffer)
				return null;

			var result;
			if (converter.scratchRootBuffer) {
				result = converter.scratchRootBuffer;
				converter.scratchRootBuffer = null;
			} else {
				// TODO: Expand the converter's heap allocation and then use
				//  mono_wasm_new_root_buffer_from_pointer instead. Not that important
				//  at present because the scratch buffer will be reused unless we are
				//  recursing through a re-entrant call
				result = MONO.mono_wasm_new_root_buffer (converter.steps.length);
				result.converter = converter;
			}
			return result;
		},

		_release_args_root_buffer_from_method_call: function (converter, argsRootBuffer) {
			if (!argsRootBuffer || !converter)
				return;

			// Store the arguments root buffer for re-use in later calls
			if (!converter.scratchRootBuffer) {
				argsRootBuffer.clear ();
				converter.scratchRootBuffer = argsRootBuffer;
			} else {
				argsRootBuffer.release ();
			}
		},

		_release_buffer_from_method_call: function (converter, buffer) {
			if (!converter || !buffer)
				return;

			if (!converter.scratchBuffer)
				converter.scratchBuffer = buffer | 0;
			else
				Module._free (buffer | 0);
		},

		_convert_exception_for_method_call: function (result, exception) {
			if (exception === 0)
				return null;

			var msg = this.conv_string (result);
			var err = new Error (msg); //the convention is that invoke_method ToString () any outgoing exception
			// console.warn ("error", msg, "at location", err.stack);
			return err;
		},

		_maybe_produce_signature_warning: function (converter) {
			if (converter.has_warned_about_signature)
				return;

			console.warn ("MONO_WASM: Deprecated raw return value signature: '" + converter.args_marshal + "'. End the signature with '!' instead of 'm'.");
			converter.has_warned_about_signature = true;
		},

		_decide_if_result_is_marshaled: function (converter, argc) {
			if (!converter)
				return true;

			if (
				converter.is_result_possibly_unmarshaled &&
				(argc === converter.result_unmarshaled_if_argc)
			) {
				if (argc < converter.result_unmarshaled_if_argc)
					throw new Error(["Expected >= ", converter.result_unmarshaled_if_argc, "argument(s) but got", argc, "for signature " + converter.args_marshal].join(" "));

				this._maybe_produce_signature_warning (converter);
				return false;
			} else {
				if (argc < converter.steps.length)
					throw new Error(["Expected", converter.steps.length, "argument(s) but got", argc, "for signature " + converter.args_marshal].join(" "));

				return !converter.is_result_definitely_unmarshaled;
			}
		},

		/*
		args_marshal is a string with one character per parameter that tells how to marshal it, here are the valid values:

		i: int32
		j: int32 - Enum with underlying type of int32
		l: int64
		k: int64 - Enum with underlying type of int64
		f: float
		d: double
		s: string
		S: interned string
		o: js object will be converted to a C# object (this will box numbers/bool/promises)
		m: raw mono object. Don't use it unless you know what you're doing

		to suppress marshaling of the return value, place '!' at the end of args_marshal, i.e. 'ii!' instead of 'ii'
		*/
		call_method: function (method, this_arg, args_marshal, args) {
			this.bindings_lazy_init ();

			// HACK: Sometimes callers pass null or undefined, coerce it to 0 since that's what wasm expects
			this_arg = this_arg | 0;

			// Detect someone accidentally passing the wrong type of value to method
			if ((method | 0) !== method)
				throw new Error (`method must be an address in the native heap, but was '${method}'`);
			if (!method)
				throw new Error ("no method specified");

			var needs_converter = this._verify_args_for_method_call (args_marshal, args);

			var buffer = 0, converter = null, argsRootBuffer = null;
			var is_result_marshaled = true;

			// check if the method signature needs argument mashalling
			if (needs_converter) {
				converter = this._compile_converter_for_marshal_string (args_marshal);

				is_result_marshaled = this._decide_if_result_is_marshaled (converter, args.length);

				argsRootBuffer = this._get_args_root_buffer_for_method_call (converter);

				var scratchBuffer = this._get_buffer_for_method_call (converter);

				buffer = converter.compiled_variadic_function (scratchBuffer, argsRootBuffer, method, args);
			}
			return this._call_method_with_converted_args (method, this_arg, converter, buffer, is_result_marshaled, argsRootBuffer);
		},

		_handle_exception_for_call: function (
			converter, buffer, resultRoot, exceptionRoot, argsRootBuffer
		) {
			var exc = this._convert_exception_for_method_call (resultRoot.value, exceptionRoot.value);
			if (!exc)
				return;

			this._teardown_after_call (converter, buffer, resultRoot, exceptionRoot, argsRootBuffer);
			throw exc;
		},

		_handle_exception_and_produce_result_for_call: function (
			converter, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled
		) {
			this._handle_exception_for_call (converter, buffer, resultRoot, exceptionRoot, argsRootBuffer);

			let result = resultRoot.value;

			if (is_result_marshaled)
				result = this._unbox_mono_obj_root (resultRoot);

			this._teardown_after_call (converter, buffer, resultRoot, exceptionRoot, argsRootBuffer);
			return result;
		},

		_teardown_after_call: function (converter, buffer, resultRoot, exceptionRoot, argsRootBuffer) {
			this._release_args_root_buffer_from_method_call (converter, argsRootBuffer);
			this._release_buffer_from_method_call (converter, buffer | 0);

			if (resultRoot)
				resultRoot.release ();
			if (exceptionRoot)
				exceptionRoot.release ();
		},

		_get_method_description: function (method) {
			if (!this._method_descriptions)
				this._method_descriptions = new Map();

			var result = this._method_descriptions.get (method);
			if (!result)
				result = "method#" + method;
			return result;
		},

		_call_method_with_converted_args: function (method, this_arg, converter, buffer, is_result_marshaled, argsRootBuffer) {
			var resultRoot = MONO.mono_wasm_new_root (), exceptionRoot = MONO.mono_wasm_new_root ();
			resultRoot.value = this.invoke_method (method, this_arg, buffer, exceptionRoot.get_address ());
			return this._handle_exception_and_produce_result_for_call (converter, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled);
		},

		bind_method: function (method, this_arg, args_marshal, friendly_name) {
			this.bindings_lazy_init ();

			this_arg = this_arg | 0;

			var converter = null;
			if (typeof (args_marshal) === "string")
				converter = this._compile_converter_for_marshal_string (args_marshal);

			var closure = {
				library_mono: MONO,
				binding_support: this,
				method: method,
				this_arg: this_arg
			};

			var converterKey = "converter_" + converter.name;

			if (converter)
				closure[converterKey] = converter;

			var argumentNames = [];
			var body = [
				"var resultRoot = library_mono.mono_wasm_new_root (), exceptionRoot = library_mono.mono_wasm_new_root ();",
				""
			];

			if (converter) {
				body.push(
					`var argsRootBuffer = binding_support._get_args_root_buffer_for_method_call (${converterKey});`,
					`var scratchBuffer = binding_support._get_buffer_for_method_call (${converterKey});`,
					`var buffer = ${converterKey}.compiled_function (`,
					"    scratchBuffer, argsRootBuffer, method,"
				);

				for (var i = 0; i < converter.steps.length; i++) {
					var argName = "arg" + i;
					argumentNames.push(argName);
					body.push(
						"    " + argName +
						(
							(i == converter.steps.length - 1)
								? ""
								: ", "
						)
					);
				}

				body.push(");");

			} else {
				body.push("var argsRootBuffer = null, buffer = 0;");
			}

			if (converter.is_result_definitely_unmarshaled) {
				body.push ("var is_result_marshaled = false;");
			} else if (converter.is_result_possibly_unmarshaled) {
				body.push (`var is_result_marshaled = arguments.length !== ${converter.result_unmarshaled_if_argc};`);
			} else {
				body.push ("var is_result_marshaled = true;");
			}

			// We inline a bunch of the invoke and marshaling logic here in order to eliminate the GC pressure normally
			//  created by the unboxing part of the call process. Because unbox_mono_obj(_root) can return non-numeric
			//  types, v8 and spidermonkey allocate and store its result on the heap (in the nursery, to be fair).
			// For a bound method however, we know the result will always be the same type because C# methods have known
			//  return types. Inlining the invoke and marshaling logic means that even though the bound method has logic
			//  for handling various types, only one path through the method (for its appropriate return type) will ever
			//  be taken, and the JIT will see that the 'result' local and thus the return value of this function are
			//  always of the exact same type. All of the branches related to this end up being predicted and low-cost.
			// The end result is that bound method invocations don't always allocate, so no more nursery GCs. Yay! -kg
			body.push(
				"",
				"resultRoot.value = binding_support.invoke_method (method, this_arg, buffer, exceptionRoot.get_address ());",
				`binding_support._handle_exception_for_call (${converterKey}, buffer, resultRoot, exceptionRoot, argsRootBuffer);`,
				"",
				"var result = undefined;",
				"if (!is_result_marshaled) ",
				"    result = resultRoot.value;",
				"else if (resultRoot.value !== 0) {",
				// For the common scenario where the return type is a primitive, we want to try and unbox it directly
				//  into our existing heap allocation and then read it out of the heap. Doing this all in one operation
				//  means that we only need to enter a gc safe region twice (instead of 3+ times with the normal,
				//  slower check-type-and-then-unbox flow which has extra checks since unbox verifies the type).
				"    var resultType = binding_support.mono_wasm_try_unbox_primitive_and_get_type (resultRoot.value, buffer);",
				"    switch (resultType) {",
				"    case 1:", // int
				"        result = Module.HEAP32[buffer / 4]; break;",
				"    case 25:", // uint32
				"        result = Module.HEAPU32[buffer / 4]; break;",
				"    case 24:", // float32
				"        result = Module.HEAPF32[buffer / 4]; break;",
				"    case 2:", // float64
				"        result = Module.HEAPF64[buffer / 8]; break;",
				"    case 8:", // boolean
				"        result = (Module.HEAP32[buffer / 4]) !== 0; break;",
				"    case 28:", // char
				"        result = String.fromCharCode(Module.HEAP32[buffer / 4]); break;",
				"    default:",
				"        result = binding_support._unbox_mono_obj_root_with_known_nonprimitive_type (resultRoot, resultType); break;",
				"    }",
				"}",
				"",
				`binding_support._teardown_after_call (${converterKey}, buffer, resultRoot, exceptionRoot, argsRootBuffer);`,
				"return result;"
			);

			var bodyJs = body.join ("\r\n");

			if (friendly_name) {
				var escapeRE = /[^A-Za-z0-9_]/g;
				friendly_name = friendly_name.replace(escapeRE, "_");
			}

			var displayName = "managed_" + (friendly_name || method);

			if (this_arg)
				displayName += "_with_this_" + this_arg;

			return this._create_named_function(displayName, argumentNames, bodyJs, closure);
		},

		resolve_method_fqn: function (fqn) {
			this.bindings_lazy_init ();

			var assembly = fqn.substring(fqn.indexOf ("[") + 1, fqn.indexOf ("]")).trim();
			fqn = fqn.substring (fqn.indexOf ("]") + 1).trim();

			var methodname = fqn.substring(fqn.indexOf (":") + 1);
			fqn = fqn.substring (0, fqn.indexOf (":")).trim ();

			var namespace = "";
			var classname = fqn;
			if (fqn.indexOf(".") != -1) {
				var idx = fqn.lastIndexOf(".");
				namespace = fqn.substring (0, idx);
				classname = fqn.substring (idx + 1);
			}

			if (!assembly.trim())
				throw new Error("No assembly name specified");
			if (!classname.trim())
				throw new Error("No class name specified");
			if (!methodname.trim())
				throw new Error("No method name specified");

			var asm = this.assembly_load (assembly);
			if (!asm)
				throw new Error ("Could not find assembly: " + assembly);

			var klass = this.find_class(asm, namespace, classname);
			if (!klass)
				throw new Error ("Could not find class: " + namespace + ":" + classname + " in assembly " + assembly);

			var method = this.find_method (klass, methodname, -1);
			if (!method)
				throw new Error ("Could not find method: " + methodname);
			return method;
		},

		call_static_method: function (fqn, args, signature) {
			this.bindings_lazy_init ();

			var method = this.resolve_method_fqn (fqn);

			if (typeof signature === "undefined")
				signature = Module.mono_method_get_call_signature (method);

			return this.call_method (method, null, signature, args);
		},

		bind_static_method: function (fqn, signature) {
			this.bindings_lazy_init ();

			var method = this.resolve_method_fqn (fqn);

			if (typeof signature === "undefined")
				signature = Module.mono_method_get_call_signature (method);

			return BINDING.bind_method (method, null, signature, fqn);
		},

		bind_assembly_entry_point: function (assembly, signature) {
			this.bindings_lazy_init ();

			var asm = this.assembly_load (assembly);
			if (!asm)
				throw new Error ("Could not find assembly: " + assembly);

			var method = this.assembly_get_entry_point(asm);
			if (!method)
				throw new Error ("Could not find entry point for assembly: " + assembly);

			if (typeof signature === "undefined")
				signature = Module.mono_method_get_call_signature (method);

			return function() {
				try {
					var args = [...arguments];
					if (args.length > 0 && Array.isArray (args[0]))
						args[0] = BINDING.js_array_to_mono_array (args[0], true, false);

					let result = BINDING.call_method (method, null, signature, args);
					return Promise.resolve (result);
				} catch (error) {
					return Promise.reject (error);
				}
			};
		},
		call_assembly_entry_point: function (assembly, args, signature) {
			return this.bind_assembly_entry_point (assembly, signature) (...args)
		},
		mono_wasm_get_jsobj_from_js_handle: function(js_handle) {
			if (js_handle > 0)
				return this._cs_owned_objects_by_js_handle[js_handle];
			return null;
		},
		mono_wasm_get_js_handle: function(js_obj) {
			if(js_obj[BINDING.cs_owned_js_handle_symbol]){
				return js_obj[BINDING.cs_owned_js_handle_symbol];
			}
			var js_handle = this._js_handle_free_list.length ? this._js_handle_free_list.pop() : this._next_js_handle++;
			// note _cs_owned_objects_by_js_handle is list, not Map. That's why we maintain _js_handle_free_list.
			this._cs_owned_objects_by_js_handle[js_handle] = js_obj;
			js_obj[BINDING.cs_owned_js_handle_symbol] = js_handle;
			return js_handle;
		},
		_mono_wasm_release_js_handle: function(js_handle) {
			var obj = BINDING._cs_owned_objects_by_js_handle[js_handle];
			if (typeof obj  !== "undefined" && obj !== null) {
				// if this is the global object then do not
				// unregister it.
				if (globalThis === obj)
					return obj;

				if (typeof obj[BINDING.cs_owned_js_handle_symbol]  !== "undefined") {
					obj[BINDING.cs_owned_js_handle_symbol] = undefined;
				}

				BINDING._cs_owned_objects_by_js_handle[js_handle] = undefined;
				BINDING._js_handle_free_list.push(js_handle);
			}
			return obj;
		},
		Queue: function Queue() {
			// amortized time, By Kate Morley http://code.iamkate.com/ under CC0 1.0

			// initialise the queue and offset
			var queue = [];
			var offset = 0;

			// Returns the length of the queue.
			this.getLength = function () {
				return (queue.length - offset);
			}

			// Returns true if the queue is empty, and false otherwise.
			this.isEmpty = function () {
				return (queue.length == 0);
			}

			/* Enqueues the specified item. The parameter is:
			*
			* item - the item to enqueue
			*/
			this.enqueue = function (item) {
				queue.push(item);
			}

			/* Dequeues an item and returns it. If the queue is empty, the value
			* 'undefined' is returned.
			*/
			this.dequeue = function () {

				// if the queue is empty, return immediately
				if (queue.length == 0) return undefined;

				// store the item at the front of the queue
				var item = queue[offset];
				
				// for GC's sake
				queue[offset] = null;

				// increment the offset and remove the free space if necessary
				if (++offset * 2 >= queue.length) {
					queue = queue.slice(offset);
					offset = 0;
				}

				// return the dequeued item
				return item;
			}

			/* Returns the item at the front of the queue (without dequeuing it). If the
			 * queue is empty then undefined is returned.
			 */
			this.peek = function () {
				return (queue.length > 0 ? queue[offset] : undefined);
			}

			this.drain = function (onEach) {
				while (this.getLength()) {
					var item = this.dequeue();
					onEach(item);
				}
			}
		},
		_text_encoder_utf8: undefined,
		_mono_wasm_web_socket_on_message: function (ws, event) {
			const event_queue = ws[this.wasm_ws_pending_receive_event_queue];
			const promise_queue = ws[this.wasm_ws_pending_receive_promise_queue];

			if (typeof event.data === 'string') {
				if (this._text_encoder_utf8 === undefined) {
					this._text_encoder_utf8 = new TextEncoder();
				}
				event_queue.enqueue({
					type: 0,// WebSocketMessageType.Text
					// according to the spec https://encoding.spec.whatwg.org/
					// - Unpaired surrogates will get replaced with 0xFFFD
					// - utf8 encode specifically is defined to never throw
					data: this._text_encoder_utf8.encode(event.data),
					offset: 0
				})
			}
			else {
				if (event.data.constructor.name !== "ArrayBuffer") {
					throw new Error('ERR19: WebSocket receive expected ArrayBuffer');
				}
				event_queue.enqueue({
					type: 1,// WebSocketMessageType.Binary
					data: new Uint8Array(event.data),
					offset: 0
				})
			}
			if (promise_queue.getLength() && event_queue.getLength() > 1) {
				throw new Error("ERR20: Invalid WS state");// assert
			}
			while (promise_queue.getLength() && event_queue.getLength()) {
				const promise_control = promise_queue.dequeue();
				this._mono_wasm_web_socket_receive_buffering(event_queue,
					promise_control.buffer_root, promise_control.buffer_offset, promise_control.buffer_length,
					promise_control.response_root);
				promise_control.resolve(null)
			}
			MONO.prevent_timer_throttling();
		},
		_mono_wasm_web_socket_receive_buffering: function (event_queue, buffer_root, buffer_offset, buffer_length, response_root) {
			const event = event_queue.peek();

			const count = Math.min(buffer_length, event.data.length - event.offset);
			if (count > 0) {
				const targetView = Module.HEAPU8.subarray(buffer_root.value + buffer_offset, buffer_root.value + buffer_offset + buffer_length);
				const sourceView = event.data.subarray(event.offset, event.offset + count);
				targetView.set(sourceView, 0);
				event.offset += count;
			}
			const end_of_message = event.data.length === event.offset ? 1 : 0;
			if (end_of_message) {
				event_queue.dequeue();
			}
			setValue(response_root.value + 0, count, "i32");
			setValue(response_root.value + 4, event.type, "i32");
			setValue(response_root.value + 8, end_of_message, "i32");
		},

		_text_decoder_utf8: undefined,
		_mono_wasm_web_socket_send_buffering: function (ws, buffer_root, buffer_offset, length, message_type, end_of_message) {
			var buffer = ws[this.wasm_ws_pending_send_buffer];
			var offset = 0;
			var message_ptr = buffer_root.value + buffer_offset;

			if (buffer) {
				offset = ws[this.wasm_ws_pending_send_buffer_offset];
				// match desktop WebSocket behavior by copying message_type of the first part
				message_type = ws[this.wasm_ws_pending_send_buffer_type];
				// if not empty message, append to existing buffer
				if (length !== 0) {
					const view = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
					if (offset + length > buffer.length) {
						const newbuffer = new Uint8Array((offset + length + 50) * 1.5); // exponential growth
						newbuffer.set(buffer, 0, offset);// copy previous buffer
						newbuffer.set(view, offset);// append copy at the end
						ws[this.wasm_ws_pending_send_buffer] = buffer = newbuffer;
					}
					else {
						buffer.set(view, offset);// append copy at the end
					}
					offset += length;
					ws[this.wasm_ws_pending_send_buffer_offset] = offset;
				}
			}
			else if (!end_of_message) {
				// create new buffer
				if (length !== 0) {
					const view = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
					buffer = new Uint8Array(view); // copy
					offset = length;
					ws[this.wasm_ws_pending_send_buffer_offset] = offset;
					ws[this.wasm_ws_pending_send_buffer] = buffer;
				}
				ws[this.wasm_ws_pending_send_buffer_type] = message_type;
			}
			else {
				// use the buffer only localy
				if (length !== 0) {
					const memoryView = Module.HEAPU8.subarray(message_ptr, message_ptr + length);
					buffer = memoryView; // send will make a copy
					offset = length;
				}
			}
			// buffer was updated, do we need to trim and convert it to final format ?
			if (end_of_message) {
				if (offset == 0) {
					return new Uint8Array();
				}
				if (message_type === 0) {
					// text, convert from UTF-8 bytes to string, because of bad browser API
					if (this._text_decoder_utf8 === undefined) {
						// we do not validate outgoing data https://github.com/dotnet/runtime/issues/59214
						this._text_decoder_utf8 = new TextDecoder('utf-8', { fatal: false });
					}

					// See https://github.com/whatwg/encoding/issues/172
					var bytes = typeof SharedArrayBuffer !== 'undefined' && buffer instanceof SharedArrayBuffer
						? buffer.slice(0, offset)
						: buffer.subarray(0, offset);
					return this._text_decoder_utf8.decode(bytes);
				} else {
					// binary, view to used part of the buffer
					return buffer.subarray(0, offset);
				}
			}
			return null;
		},
		_mono_wasm_web_socket_send_and_wait: function (ws, buffer, thenable_js_handle) {
			// send and return promise
			ws.send(buffer);
			ws[this.wasm_ws_pending_send_buffer] = null;

			// if the remaining send buffer is small, we don't block so that the throughput doesn't suffer. 
			// Otherwise we block so that we apply some backpresure to the application sending large data.
			// this is different from Managed implementation
			if (ws.bufferedAmount < BINDING.ws_send_buffer_blocking_threshold) {
				return null; // no promise
			}

			// block the promise/task until the browser passed the buffer to OS
			const { promise, promise_control } = BINDING._create_cancelable_promise();
			const pending = ws[BINDING.wasm_ws_pending_send_promises];
			pending.push(promise_control);

			var nextDelay = 1;
			const polling_check = () => {
				// was it all sent yet ?
				if (ws.bufferedAmount === 0) {
					promise_control.resolve(null);
				}
				else if (ws.readyState != WebSocket.OPEN) {
					// only reject if the data were not sent
					// bufferedAmount does not reset to zero once the connection closes
					promise_control.reject("InvalidState: The WebSocket is not connected.");
				} 
				else if(!promise_control.isDone) {
					globalThis.setTimeout(polling_check, nextDelay);
					// exponentially longer delays, up to 1000ms
					nextDelay = Math.min(nextDelay * 1.5, 1000);
					return;
				}
				// remove from pending
				const index = pending.indexOf(promise_control);
				if (index > -1) {
					pending.splice(index, 1);
				}
			};

			globalThis.setTimeout(polling_check, 0);

			const { task_ptr, then_js_handle } = BINDING._wrap_js_thenable_as_task(promise);
			// task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
			setValue(thenable_js_handle, then_js_handle, "i32");

			return task_ptr;
		},
	},
	mono_wasm_invoke_js_with_args: function(js_handle, method_name, args, is_exception) {
		let argsRoot = MONO.mono_wasm_new_root (args), nameRoot = MONO.mono_wasm_new_root (method_name);
		try {
			BINDING.bindings_lazy_init ();

			var js_name = BINDING.conv_string (nameRoot.value);
			if (!js_name || (typeof(js_name) !== "string")) {
				return BINDING.wrap_error(is_exception, "ERR12: Invalid method name object '" + nameRoot.value + "'");
			}

			var obj = BINDING.get_js_obj (js_handle);
			if (!obj) {
				return BINDING.wrap_error(is_exception, "ERR13: Invalid JS object handle '" + js_handle + "' while invoking '"+js_name+"'");
			}

			var js_args = BINDING._mono_array_root_to_js_array(argsRoot);

			var res;
			try {
				var m = obj [js_name];
				if (typeof m === "undefined")
					throw new Error("Method: '" + js_name + "' not found for: '" + Object.prototype.toString.call(obj) + "'");
				var res = m.apply (obj, js_args);
				return BINDING._js_to_mono_obj(true, res);
			} catch (ex) {
				return BINDING.wrap_error(is_exception, ex);
			}
		} finally {
			argsRoot.release();
			nameRoot.release();
		}
	},
	mono_wasm_get_object_property: function(js_handle, property_name, is_exception) {
		BINDING.bindings_lazy_init ();

		var nameRoot = MONO.mono_wasm_new_root (property_name);
		try {
			var js_name = BINDING.conv_string (nameRoot.value);
			if (!js_name) {
				return BINDING.wrap_error(is_exception, "Invalid property name object '" + nameRoot.value + "'");
			}

			var obj = BINDING.mono_wasm_get_jsobj_from_js_handle (js_handle);
			if (!obj) {
				return BINDING.wrap_error(is_exception, "ERR01: Invalid JS object handle '" + js_handle + "' while geting '"+js_name+"'");
			}

			try {
				var m = obj [js_name];

				return BINDING._js_to_mono_obj (true, m);
			} catch (ex) {
				return BINDING.wrap_error(is_exception, ex);
			}
		} finally {
			nameRoot.release();
		}
	},
    mono_wasm_set_object_property: function (js_handle, property_name, value, createIfNotExist, hasOwnProperty, is_exception) {
		var valueRoot = MONO.mono_wasm_new_root (value), nameRoot = MONO.mono_wasm_new_root (property_name);
		try {
			BINDING.bindings_lazy_init ();
			var property = BINDING.conv_string (nameRoot.value);
			if (!property) {
				return BINDING.wrap_error(is_exception, "Invalid property name object '" + property_name + "'");
			}

			var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle (js_handle);
			if (!js_obj) {
				return BINDING.wrap_error(is_exception, "ERR02: Invalid JS object handle '" + js_handle + "' while setting '"+property+"'");
			}

			var result = false;

			var js_value = BINDING._unbox_mono_obj_root(valueRoot);

			if (createIfNotExist) {
				js_obj[property] = js_value;
				result = true;
			}
			else {
				result = false;
				if (!createIfNotExist)
				{
					if (!js_obj.hasOwnProperty(property))
						return false;
				}
				if (hasOwnProperty === true) {
					if (js_obj.hasOwnProperty(property)) {
						js_obj[property] = js_value;
						result = true;
					}
				}
				else {
					js_obj[property] = js_value;
					result = true;
				}
			}
			return BINDING._box_js_bool (result);
		} finally {
			nameRoot.release();
			valueRoot.release();
		}
	},
	mono_wasm_get_by_index: function(js_handle, property_index, is_exception) {
		BINDING.bindings_lazy_init ();

		var obj = BINDING.mono_wasm_get_jsobj_from_js_handle (js_handle);
		if (!obj) {
			return BINDING.wrap_error(is_exception, "ERR03: Invalid JS object handle '" + js_handle + "' while getting ["+property_index+"]");
		}

		try {
			var m = obj [property_index];
			return BINDING._js_to_mono_obj (true, m);
		} catch (ex) {
			return BINDING.wrap_error(is_exception, ex);
		}
	},
	mono_wasm_set_by_index: function(js_handle, property_index, value, is_exception) {
		var valueRoot = MONO.mono_wasm_new_root (value);
		try {
			BINDING.bindings_lazy_init ();

			var obj = BINDING.mono_wasm_get_jsobj_from_js_handle (js_handle);
			if (!obj) {
				return BINDING.wrap_error(is_exception, "ERR04: Invalid JS object handle '" + js_handle + "' while setting ["+property_index+"]");
			}

			var js_value = BINDING._unbox_mono_obj_root(valueRoot);

			try {
				obj [property_index] = js_value;
				return true;
			} catch (ex) {
				return BINDING.wrap_error(is_exception, ex);
			}
		} finally {
			valueRoot.release();
		}
	},
	mono_wasm_get_global_object: function(global_name, is_exception) {
		var nameRoot = MONO.mono_wasm_new_root (global_name);
		try {
			BINDING.bindings_lazy_init ();

			var js_name = BINDING.conv_string (nameRoot.value);

			var globalObj;

			if (!js_name) {
				globalObj = globalThis;
			}
			else {
				globalObj = globalThis[js_name];
			}

			// TODO returning null may be useful when probing for browser features
			if (globalObj === null || typeof globalObj === undefined) {
				return BINDING.wrap_error(is_exception, "Global object '" + js_name + "' not found.");
			}

			return BINDING._js_to_mono_obj (true, globalObj);
		} finally {
			nameRoot.release();
		}
	},
	mono_wasm_release_cs_owned_object: function(js_handle) {
		BINDING.bindings_lazy_init ();
		BINDING._mono_wasm_release_js_handle(js_handle);
	},
	mono_wasm_create_cs_owned_object: function (core_name, args, is_exception) {
		var argsRoot = MONO.mono_wasm_new_root (args), nameRoot = MONO.mono_wasm_new_root (core_name);
		try {
			BINDING.bindings_lazy_init ();

			var js_name = BINDING.conv_string (nameRoot.value);

			if (!js_name) {
				return BINDING.wrap_error(is_exception, "Invalid name @" + nameRoot.value);
			}

			var coreObj = globalThis[js_name];

			if (coreObj === null || typeof coreObj === "undefined") {
				return BINDING.wrap_error(is_exception, "JavaScript host object '" + js_name + "' not found.");
			}

			var js_args = BINDING._mono_array_root_to_js_array(argsRoot);

			try {

				// This is all experimental !!!!!!
				var allocator = function(constructor, js_args) {
					// Not sure if we should be checking for anything here
					var argsList = new Array();
					argsList[0] = constructor;
					if (js_args)
						argsList = argsList.concat (js_args);
					var tempCtor = constructor.bind.apply (constructor, argsList);
					var js_obj = new tempCtor ();
					return js_obj;
				};

				var js_obj = allocator(coreObj, js_args);
				var js_handle = BINDING.mono_wasm_get_js_handle(js_obj);
				// returns boxed js_handle int, because on exception we need to return String on same method signature
				// here we don't have anything to in-flight reference, as the JSObject doesn't exist yet
				return BINDING._js_to_mono_obj(false, js_handle);
			} catch (ex) {
				return BINDING.wrap_error(is_exception, ex);
			}
		} finally {
			argsRoot.release();
			nameRoot.release();
		}
	},
	mono_wasm_typed_array_to_array: function(js_handle, is_exception) {
		BINDING.bindings_lazy_init ();

		var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle (js_handle);
		if (!js_obj) {
			return BINDING.wrap_error(is_exception, "ERR06: Invalid JS object handle '" + js_handle + "'");
		}

		// returns pointer to C# array
		return BINDING.js_typed_array_to_array(js_obj, false);
	},
	mono_wasm_typed_array_copy_to: function(js_handle, pinned_array, begin, end, bytes_per_element, is_exception) {
		BINDING.bindings_lazy_init ();

		var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle (js_handle);
		if (!js_obj) {
			return BINDING.wrap_error(is_exception, "ERR07: Invalid JS object handle '" + js_handle + "'");
		}

		var res = BINDING.typedarray_copy_to(js_obj, pinned_array, begin, end, bytes_per_element);
		// returns num_of_bytes boxed
		return BINDING._js_to_mono_obj (false, res)
	},
	mono_wasm_typed_array_from: function(pinned_array, begin, end, bytes_per_element, type, is_exception) {
		BINDING.bindings_lazy_init ();
		var res = BINDING.typed_array_from(pinned_array, begin, end, bytes_per_element, type);
		// returns JS typed array like Int8Array, to be wraped with JSObject proxy
		return BINDING._js_to_mono_obj (true, res)
	},
	mono_wasm_typed_array_copy_from: function(js_handle, pinned_array, begin, end, bytes_per_element, is_exception) {
		BINDING.bindings_lazy_init ();

		var js_obj = BINDING.mono_wasm_get_jsobj_from_js_handle (js_handle);
		if (!js_obj) {
			return BINDING.wrap_error(is_exception, "ERR08: Invalid JS object handle '" + js_handle + "'");
		}

		var res = BINDING.typedarray_copy_from(js_obj, pinned_array, begin, end, bytes_per_element);
		// returns num_of_bytes boxed
		return BINDING._js_to_mono_obj (false, res)
	},
	mono_wasm_add_event_listener: function (objHandle, name, listener_gc_handle, optionsHandle) {
		var nameRoot = MONO.mono_wasm_new_root (name);
		try {
			BINDING.bindings_lazy_init ();
			var sName = BINDING.conv_string(nameRoot.value);

			var obj = BINDING.mono_wasm_get_jsobj_from_js_handle(objHandle);
			if (!obj)
				throw new Error("ERR09: Invalid JS object handle for '"+sName+"'");

			const prevent_timer_throttling = !MONO.isChromium || obj.constructor.name !== 'WebSocket'
				? null
				: MONO.prevent_timer_throttling;

			var listener = BINDING._wrap_delegate_gc_handle_as_function(listener_gc_handle, prevent_timer_throttling);
			if (!listener)
				throw new Error("ERR10: Invalid listener gc_handle");

			var options = optionsHandle
				? BINDING.mono_wasm_get_jsobj_from_js_handle(optionsHandle)
				: null;

			if(!BINDING._use_finalization_registry){
				// we are counting registrations because same delegate could be registered into multiple sources
				listener[BINDING.listener_registration_count_symbol] = listener[BINDING.listener_registration_count_symbol] ? listener[BINDING.listener_registration_count_symbol] + 1 : 1;
			}

			if (options)
				obj.addEventListener(sName, listener, options);
			else
				obj.addEventListener(sName, listener);
			return 0;
		} catch (ex) {
			return BINDING.wrap_error(null, ex);
		} finally {
			nameRoot.release();
		}
	},
	mono_wasm_remove_event_listener: function (objHandle, name, listener_gc_handle, capture) {
		var nameRoot = MONO.mono_wasm_new_root (name);
		try {
			BINDING.bindings_lazy_init ();
			var obj = BINDING.mono_wasm_get_jsobj_from_js_handle(objHandle);
			if (!obj)
				throw new Error("ERR11: Invalid JS object handle");
			var listener = BINDING._lookup_js_owned_object(listener_gc_handle);
			// Removing a nonexistent listener should not be treated as an error
			if (!listener)
				return;
			var sName = BINDING.conv_string(nameRoot.value);

			obj.removeEventListener(sName, listener, !!capture);
			// We do not manually remove the listener from the delegate registry here,
			//  because that same delegate may have been used as an event listener for
			//  other events or event targets. The GC will automatically clean it up
			//  and trigger the FinalizationRegistry handler if it's unused

			// When FinalizationRegistry is not supported by this browser, we cleanup manuall after unregistration
			if (!BINDING._use_finalization_registry) {
				listener[BINDING.listener_registration_count_symbol]--;
				if (listener[BINDING.listener_registration_count_symbol] === 0) {
					BINDING._js_owned_object_finalized(listener_gc_handle);
				}
			}

			return 0;
		} catch (ex) {
			return BINDING.wrap_error(null, ex);
		} finally {
			nameRoot.release();
		}
	},
	mono_wasm_cancel_promise: function (thenable_js_handle, is_exception) {
		try {
			const promise = BINDING.mono_wasm_get_jsobj_from_js_handle(thenable_js_handle)
			const promise_control = promise[BINDING.promise_control_symbol];
			promise_control.reject("OperationCanceledException");
		}
		catch (ex) {
			return BINDING.wrap_error(is_exception, ex);
		}
	},
	mono_wasm_web_socket_open: function (uri, subProtocols, on_close, web_socket_js_handle, thenable_js_handle, is_exception) {
		const uri_root = MONO.mono_wasm_new_root(uri);
		const sub_root = MONO.mono_wasm_new_root(subProtocols);
		const on_close_root = MONO.mono_wasm_new_root(on_close);
		try {
			const js_uri = BINDING.conv_string(uri_root.value);
			if (!js_uri) {
				return BINDING.wrap_error(is_exception, "ERR12: Invalid uri '" + uri_root.value + "'");
			}

			const js_subs = BINDING._mono_array_root_to_js_array(sub_root);

			const js_on_close = BINDING._wrap_delegate_root_as_function(on_close_root);

			const ws = new globalThis.WebSocket(js_uri, js_subs);
			var { promise, promise_control: open_promise_control } = BINDING._create_cancelable_promise();

			ws[BINDING.wasm_ws_pending_receive_event_queue] = new BINDING.Queue();
			ws[BINDING.wasm_ws_pending_receive_promise_queue] = new BINDING.Queue();
			ws[BINDING.wasm_ws_pending_open_promise] = open_promise_control;
			ws[BINDING.wasm_ws_pending_send_promises] = [];
			ws[BINDING.wasm_ws_pending_close_promises] = [];
			ws.binaryType = "arraybuffer";
			const local_on_open = (ev) => {
				if (ws[BINDING.wasm_ws_is_aborted]) return;
				open_promise_control.resolve(null);
				MONO.prevent_timer_throttling();
			}
			const local_on_message = (ev) => {
				if (ws[BINDING.wasm_ws_is_aborted]) return;
				BINDING._mono_wasm_web_socket_on_message(ws, ev);
				MONO.prevent_timer_throttling();
			}
			const local_on_close = (ev) => {
				ws.removeEventListener('message', local_on_message);
				if (ws[BINDING.wasm_ws_is_aborted]) return;
				js_on_close(ev.code, ev.reason);

				// this reject would not do anything if there was already "open" before it.
				open_promise_control.reject(ev.reason);

				for (var close_promise_control of ws[BINDING.wasm_ws_pending_close_promises]) {
					close_promise_control.resolve();
				}

				// send close to any pending receivers, to wake them
				ws[BINDING.wasm_ws_pending_receive_promise_queue].drain(receive_promise_control => {
					const response_root = receive_promise_control.response_root;
					setValue(response_root.value + 0, 0, "i32");// count
					setValue(response_root.value + 4, 2, "i32");// type:close
					setValue(response_root.value + 8, 1, "i32");// end_of_message: true
					receive_promise_control.resolve(null);
				});
			}
			ws.addEventListener('message', local_on_message);
			ws.addEventListener('open', local_on_open, { once: true });
			ws.addEventListener('close', local_on_close, { once: true });

			var ws_js_handle = BINDING.mono_wasm_get_js_handle(ws);
			setValue(web_socket_js_handle, ws_js_handle, "i32");

			var { task_ptr, then_js_handle } = BINDING._wrap_js_thenable_as_task(promise);
			// task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
			setValue(thenable_js_handle, then_js_handle, "i32");

			return task_ptr;
		}
		catch (ex) {
			return BINDING.wrap_error(is_exception, ex);
		}
		finally {
			uri_root.release();
			sub_root.release();
			on_close_root.release();
		}
	},
	mono_wasm_web_socket_send: function (webSocket_js_handle, buffer_ptr, offset, length, message_type, end_of_message, thenable_js_handle, is_exception) {
		const buffer_root = MONO.mono_wasm_new_root(buffer_ptr);
		try {
			const ws = BINDING.mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle)
			if (!ws)
				throw new Error("ERR17: Invalid JS object handle " + webSocket_js_handle);

			if (ws.readyState != WebSocket.OPEN) {
				throw new Error("InvalidState: The WebSocket is not connected.");
			}

			const whole_buffer = BINDING._mono_wasm_web_socket_send_buffering(ws, buffer_root, offset, length, message_type, end_of_message);

			if (!end_of_message) {
				return null; // we are done buffering synchronously, no promise
			}
			return BINDING._mono_wasm_web_socket_send_and_wait(ws, whole_buffer, thenable_js_handle);
		}
		catch (ex) {
			return BINDING.wrap_error(is_exception, ex);
		}
		finally {
			buffer_root.release();
		}
	},
	mono_wasm_web_socket_receive: function (webSocket_js_handle, buffer_ptr, offset, length, response_ptr, thenable_js_handle, is_exception) {
		const buffer_root = MONO.mono_wasm_new_root(buffer_ptr);
		const response_root = MONO.mono_wasm_new_root(response_ptr);
		const release_buffer = () => {
			buffer_root.release();
			response_root.release();
		}

		try {
			const ws = BINDING.mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle)
			if (!ws)
				throw new Error("ERR18: Invalid JS object handle " + webSocket_js_handle);
			const event_queue = ws[BINDING.wasm_ws_pending_receive_event_queue];
			const promise_queue = ws[BINDING.wasm_ws_pending_receive_promise_queue];

			const readyState = ws.readyState;
			if (readyState != WebSocket.OPEN && readyState != WebSocket.CLOSING) {
				throw new Error("InvalidState: The WebSocket is not connected.");
			}

			if (event_queue.getLength()) {
				if (promise_queue.getLength() != 0) {
					throw new Error("ERR20: Invalid WS state");// assert
				}
				// finish synchronously
				BINDING._mono_wasm_web_socket_receive_buffering(event_queue, buffer_root, offset, length, response_root);
				release_buffer();

				setValue(thenable_js_handle, 0, "i32");
				return null;
			}

			const { promise, promise_control } = BINDING._create_cancelable_promise(release_buffer, release_buffer);
			promise_control.buffer_root = buffer_root;
			promise_control.buffer_offset = offset;
			promise_control.buffer_length = length;
			promise_control.response_root = response_root;
			promise_queue.enqueue(promise_control);

			const { task_ptr, then_js_handle } = BINDING._wrap_js_thenable_as_task(promise);
			// task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
			setValue(thenable_js_handle, then_js_handle, "i32");
			return task_ptr;
		}
		catch (ex) {
			return BINDING.wrap_error(is_exception, ex);
		}
	},
	mono_wasm_web_socket_close: function (webSocket_js_handle, code, reason, wait_for_close_received, thenable_js_handle, is_exception) {
		const reason_root = MONO.mono_wasm_new_root(reason);
		try {
			const ws = BINDING.mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle)
			if (!ws)
				throw new Error("ERR19: Invalid JS object handle " + webSocket_js_handle);

			if (ws.readyState == WebSocket.CLOSED) {
				return null;// no promise
			}

			const js_reason = BINDING.conv_string(reason_root.value);

			if (wait_for_close_received) {
				const { promise, promise_control } = BINDING._create_cancelable_promise();
				ws[BINDING.wasm_ws_pending_close_promises].push(promise_control);

				if (js_reason) {
					ws.close(code, js_reason);
				} else {
					ws.close(code);
				}

				var { task_ptr, then_js_handle } = BINDING._wrap_js_thenable_as_task(promise);
				// task_ptr above is not rooted, we need to return it to mono without any intermediate mono call which could cause GC
				setValue(thenable_js_handle, then_js_handle, "i32");

				return task_ptr;
			}
			else {
				if (!BINDING.mono_wasm_web_socket_close_warning) {
					BINDING.mono_wasm_web_socket_close_warning = true;
					console.warn('WARNING: Web browsers do not support closing the output side of a WebSocket. CloseOutputAsync has closed the socket and discarded any incoming messages.');
				}
				if (js_reason) {
					ws.close(code, js_reason);
				} else {
					ws.close(code);
				}
				setValue(thenable_js_handle, 0, "i32");
				return null;// no promise
			}
		}
		catch (ex) {
			return BINDING.wrap_error(is_exception, ex);
		}
		finally {
			reason_root.release();
		}
	},
	mono_wasm_web_socket_abort: function (webSocket_js_handle, is_exception) {
		try {
			const ws = BINDING.mono_wasm_get_jsobj_from_js_handle(webSocket_js_handle)
			if (!ws)
				throw new Error("ERR18: Invalid JS object handle " + webSocket_js_handle);
			
			ws[BINDING.wasm_ws_is_aborted] = true;
			const open_promise_control = ws[BINDING.wasm_ws_pending_open_promise];
			if (open_promise_control) {
				open_promise_control.reject("OperationCanceledException");
			}
			for(var close_promise_control of ws[BINDING.wasm_ws_pending_close_promises]){
				close_promise_control.reject("OperationCanceledException");
			}
			for (var send_promise_control of ws[BINDING.wasm_ws_pending_send_promises]) {
				send_promise_control.reject("OperationCanceledException")
			}

			ws[BINDING.wasm_ws_pending_receive_promise_queue].drain(receive_promise_control => {
				receive_promise_control.reject("OperationCanceledException");
			});

			// this is different from Managed implementation
			ws.close(1000, 'Connection was aborted.');
		}
		catch (ex) {
			return BINDING.wrap_error(is_exception, ex);
		}
	},
};

autoAddDeps(BindingSupportLib, '$BINDING')
mergeInto(LibraryManager.library, BindingSupportLib)
