// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

var BindingSupportLib = {
	$BINDING__postset: 'BINDING.export_functions (Module);',
	$BINDING: {
		BINDING_ASM: "[System.Private.Runtime.InteropServices.JavaScript]System.Runtime.InteropServices.JavaScript.Runtime",
		mono_wasm_object_registry: [],
		mono_wasm_ref_counter: 0,
		mono_wasm_free_list: [],
		mono_wasm_owned_objects_frames: [],
		mono_wasm_owned_objects_LMF: [],
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

			Array.prototype[Symbol.for("wasm type")] = 1;
			ArrayBuffer.prototype[Symbol.for("wasm type")] = 2;
			DataView.prototype[Symbol.for("wasm type")] = 3;
			Function.prototype[Symbol.for("wasm type")] =  4;
			Map.prototype[Symbol.for("wasm type")] = 5;
			if (typeof SharedArrayBuffer !== 'undefined')
				SharedArrayBuffer.prototype[Symbol.for("wasm type")] =  6;
			Int8Array.prototype[Symbol.for("wasm type")] = 10;
			Uint8Array.prototype[Symbol.for("wasm type")] = 11;
			Uint8ClampedArray.prototype[Symbol.for("wasm type")] = 12;
			Int16Array.prototype[Symbol.for("wasm type")] = 13;
			Uint16Array.prototype[Symbol.for("wasm type")] = 14;
			Int32Array.prototype[Symbol.for("wasm type")] = 15;
			Uint32Array.prototype[Symbol.for("wasm type")] = 16;
			Float32Array.prototype[Symbol.for("wasm type")] = 17;
			Float64Array.prototype[Symbol.for("wasm type")] = 18;

			this.assembly_load = Module.cwrap ('mono_wasm_assembly_load', 'number', ['string']);
			this.find_corlib_class = Module.cwrap ('mono_wasm_find_corlib_class', 'number', ['string', 'string']);
			this.find_class = Module.cwrap ('mono_wasm_assembly_find_class', 'number', ['number', 'string', 'string']);
			this._find_method = Module.cwrap ('mono_wasm_assembly_find_method', 'number', ['number', 'string', 'number']);
			this.invoke_method = Module.cwrap ('mono_wasm_invoke_method', 'number', ['number', 'number', 'number', 'number']);
			this.mono_string_get_utf8 = Module.cwrap ('mono_wasm_string_get_utf8', 'number', ['number']);
			this.mono_wasm_string_from_utf16 = Module.cwrap ('mono_wasm_string_from_utf16', 'number', ['number', 'number']);
			this.mono_wasm_get_obj_class = Module.cwrap ('mono_wasm_get_obj_class', 'number', ['number']);
			this.mono_get_obj_type = Module.cwrap ('mono_wasm_get_obj_type', 'number', ['number']);
			this.mono_array_length = Module.cwrap ('mono_wasm_array_length', 'number', ['number']);
			this.mono_array_get = Module.cwrap ('mono_wasm_array_get', 'number', ['number', 'number']);
			this.mono_obj_array_new = Module.cwrap ('mono_wasm_obj_array_new', 'number', ['number']);
			this.mono_obj_array_set = Module.cwrap ('mono_wasm_obj_array_set', 'void', ['number', 'number', 'number']);
			this.mono_wasm_register_bundled_satellite_assemblies = Module.cwrap ('mono_wasm_register_bundled_satellite_assemblies', 'void', [ ]);
			this.mono_wasm_try_unbox_primitive_and_get_type = Module.cwrap ('mono_wasm_try_unbox_primitive_and_get_type', 'number', ['number', 'number', 'number']);
			this.mono_wasm_box_primitive = Module.cwrap ('mono_wasm_box_primitive', 'number', ['number', 'number', 'number']);
			this.mono_wasm_intern_string = Module.cwrap ('mono_wasm_intern_string', 'number', ['number']);
			this.assembly_get_entry_point = Module.cwrap ('mono_wasm_assembly_get_entry_point', 'number', ['number']);
			this.mono_wasm_get_delegate_invoke = Module.cwrap ('mono_wasm_get_delegate_invoke', 'number', ['number']);
			this.mono_wasm_string_array_new = Module.cwrap ('mono_wasm_string_array_new', 'number', ['number']);
			this.mono_wasm_unbox_rooted = Module.cwrap ('mono_wasm_unbox_rooted', 'number', ['number']);
			this.mono_wasm_get_class_for_bind_or_invoke = Module.cwrap ('mono_wasm_get_class_for_bind_or_invoke', 'number', ['number', 'number']);
			this.mono_wasm_class_get_type = Module.cwrap ('mono_wasm_class_get_type', 'number', ['number']);
			this.mono_wasm_type_get_class = Module.cwrap ('mono_wasm_type_get_class', 'number', ['number']);
			this.mono_wasm_get_type_name = Module.cwrap ('mono_wasm_get_type_name', 'string', ['number']);

			this._box_buffer = Module._malloc(32);
			this._unbox_buffer_size = 65536;
			this._unbox_buffer = Module._malloc(this._unbox_buffer_size);
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

			this._are_promises_supported = ((typeof Promise === "object") || (typeof Promise === "function")) && (typeof Promise.resolve === "function");

			this._empty_string = "";
			this._empty_string_ptr = 0;
			this._interned_string_full_root_buffers = [];
			this._interned_string_current_root_buffer = null;
			this._interned_string_current_root_buffer_count = 0;
			this._interned_js_string_table = new Map ();
			this._custom_marshaler_info_cache = new Map ();

			// HACK: This method needs to be the absolute first one we bind, because
			//  the process of binding other methods relies on it.
			this.make_marshal_signature_info = bind_runtime_method ("MakeMarshalSignatureInfo", "iiii");

			this.get_custom_marshaler_info = bind_runtime_method ("GetCustomMarshalerInfoForType", "is");

			// NOTE: The bound methods have a _ prefix on their names to ensure
			//  that any code relying on the old get_method/call_method pattern will
			//  break in a more understandable way.
			this._bind_js_obj = bind_runtime_method ("BindJSObject", "iii");
			this._bind_core_clr_obj = bind_runtime_method ("BindCoreCLRObject", "ii");
			this._bind_existing_obj = bind_runtime_method ("BindExistingObject", "mi");
			this._unbind_raw_obj_and_free = bind_runtime_method ("UnBindRawJSObjectAndFree", "ii");
			this._get_js_id = bind_runtime_method ("GetJSObjectId", "m");
			this._get_raw_mono_obj = bind_runtime_method ("GetDotNetObject", "i!");

			this._is_simple_array = bind_runtime_method ("IsSimpleArray", "m");
			this.setup_js_cont = get_method ("SetupJSContinuation");

			this.create_tcs = get_method ("CreateTaskSource");
			this.set_tcs_result = get_method ("SetTaskSourceResult");
			this.set_tcs_failure = get_method ("SetTaskSourceFailure");
			this.tcs_get_task_and_bind = get_method ("GetTaskAndBind");
			this.get_call_sig = get_method ("GetCallSignature");

			this._object_to_string = bind_runtime_method ("ObjectToString", "m");
			this.get_date_value = get_method ("GetDateValue");
			this.create_date_time = get_method ("CreateDateTime");
			this.create_uri = get_method ("CreateUri");

			this.safehandle_addref = get_method ("SafeHandleAddRef");
			this.safehandle_release = get_method ("SafeHandleRelease");
			this.safehandle_get_handle = get_method ("SafeHandleGetHandle");
			this.safehandle_release_by_handle = get_method ("SafeHandleReleaseByHandle");

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
				throw new Error ("Expected string argument");

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
				return this.mono_wasm_require_handle(js_handle);
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

		js_array_to_mono_array: function (js_array, asString = false) {
			var mono_array = asString ? this.mono_wasm_string_array_new (js_array.length) : this.mono_obj_array_new (js_array.length);
			let [arrayRoot, elemRoot] = MONO.mono_wasm_new_roots ([mono_array, 0]);

			try {
				for (var i = 0; i < js_array.length; ++i) {
					var obj = js_array[i];
					if (asString)
						obj = obj.toString ();

					elemRoot.value = this.js_to_mono_obj (obj);
					this.mono_obj_array_set (arrayRoot.value, i, elemRoot.value);
				}

				return mono_array;
			} finally {
				MONO.mono_wasm_release_roots (arrayRoot, elemRoot);
			}
		},

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
		
		_unbox_delegate_rooted: function (mono_obj) {
			var obj = this.extract_js_obj (mono_obj);
			obj.__mono_delegate_alive__ = true;
			// FIXME: Should we root the object as long as this function has not been GCd?
			return function () {
				// TODO: Just use Function.bind
				return BINDING.invoke_delegate (obj, arguments);
			};
		},

		_unbox_task_rooted: function (mono_obj) {
			if (!this._are_promises_supported)
				throw new Error ("Promises are not supported thus 'System.Threading.Tasks.Task' can not work in this context.");

			var obj = this.extract_js_obj (mono_obj);
			var cont_obj = null;
			var promise = new Promise (function (resolve, reject) {
				cont_obj = {
					resolve: resolve,
					reject: reject
				};
			});

			this.call_method (this.setup_js_cont, null, "mo", [ mono_obj, cont_obj ]);
			obj.__mono_js_cont__ = cont_obj.__mono_gchandle__;
			cont_obj.__mono_js_task__ = obj.__mono_gchandle__;
			return promise;
		},

		_unbox_safehandle_rooted: function (mono_obj) {
			var addRef = true;
			var js_handle = this.call_method(this.safehandle_get_handle, null, "mi", [ mono_obj, addRef ]);
			var requiredObject = BINDING.mono_wasm_require_handle (js_handle);
			if (addRef)
			{
				if (typeof this.mono_wasm_owned_objects_LMF === "undefined")
					this.mono_wasm_owned_objects_LMF = [];

				this.mono_wasm_owned_objects_LMF.push(js_handle);
			}
			return requiredObject;
		},

		_unbox_mono_obj_rooted_with_known_nonprimitive_type: function (mono_obj, type, klass) {
			//See MARSHAL_TYPE_ defines in driver.c
			switch (type) {
				case 26: // int64
				case 27: // uint64
					// TODO: Fix this once emscripten offers HEAPI64/HEAPU64 or can return them
					throw new Error ("int64 not available");
				case 3: // string
				case 29: // interned string
					return this.conv_string (mono_obj);
				case 4: // struct
					return this.extract_js_obj_with_possible_converter (mono_obj, klass);
				case 5: // delegate
					return this._unbox_delegate_rooted (mono_obj);
				case 6: // Task
					return this._unbox_task_rooted (mono_obj);
				case 7: // ref type
					return this.extract_js_obj_with_possible_converter (mono_obj, klass);
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
					var dateValue = this.call_method(this.get_date_value, null, "m", [ mono_obj ]);
					return new Date(dateValue);
				case 21: // clr .NET DateTimeOffset
					var dateoffsetValue = this._object_to_string (mono_obj);
					return dateoffsetValue;
				case 22: // clr .NET Uri
					var uriValue = this._object_to_string (mono_obj);
					return uriValue;
				case 23: // clr .NET SafeHandle
					return this._unbox_safehandle_rooted (mono_obj);
				case 30:
					return undefined;
				default:
					throw new Error ("no idea on how to unbox object kind " + type + " at offset " + mono_obj);
			}
		},

		_unbox_mono_obj_root: function (root) {
			var mono_obj = root.value;
			if (mono_obj === 0)
				return undefined;

			var type = this.mono_wasm_try_unbox_primitive_and_get_type (mono_obj, this._unbox_buffer, this._unbox_buffer_size);
			switch (type) {
				case 1: // int
					return Module.HEAP32[this._unbox_buffer / 4];
				case 4: // struct
					return this._unbox_struct_rooted (this._unbox_buffer, mono_obj);
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
					var klass = Module.HEAPU32[this._unbox_buffer / 4];
					return this._unbox_mono_obj_rooted_with_known_nonprimitive_type (mono_obj, type, klass);
			}
		},

		create_task_completion_source: function () {
			return this.call_method (this.create_tcs, null, "i", [ -1 ]);
		},

		set_task_result: function (tcs, result) {
			tcs.is_mono_tcs_result_set = true;
			this.call_method (this.set_tcs_result, null, "oo", [ tcs, result ]);
			if (tcs.is_mono_tcs_task_bound)
				this.free_task_completion_source(tcs);
		},

		set_task_failure: function (tcs, reason) {
			tcs.is_mono_tcs_result_set = true;
			this.call_method (this.set_tcs_failure, null, "os", [ tcs, reason.toString () ]);
			if (tcs.is_mono_tcs_task_bound)
				this.free_task_completion_source(tcs);
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

		js_to_mono_obj: function (js_obj) {
			this.bindings_lazy_init ();

			// determines if the javascript object is a Promise or Promise like which can happen
			// when using an external Promise library.  The javascript object should be marshalled
			// as managed Task objects.
			//
			// Example is when Bluebird is included in a web page using a script tag, it overwrites the
			// global Promise object by default with its own version of Promise.
			function isThenable() {
				// When using an external Promise library the Promise.resolve may not be sufficient
				// to identify the object as a Promise.
				return Promise.resolve(js_obj) === js_obj ||
						((typeof js_obj === "object" || typeof js_obj === "function") && typeof js_obj.then === "function")
			}

			switch (true) {
				case js_obj === null:
				case typeof js_obj === "undefined":
					return 0;
				case typeof js_obj === "number": {
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
				case isThenable() === true:
					var the_task = this.try_extract_mono_obj (js_obj);
					if (the_task)
						return the_task;
					// FIXME: We need to root tcs for an appropriate timespan, at least until the Task
					//  is resolved
					var tcs = this.create_task_completion_source ();
					js_obj.then (function (result) {
						BINDING.set_task_result (tcs, result);
					}, function (reason) {
						BINDING.set_task_failure (tcs, reason);
					})
					return this.get_task_and_bind (tcs, js_obj);
				case js_obj.constructor.name === "Date":
					// We may need to take into account the TimeZone Offset
					return this.call_method(this.create_date_time, null, "d!", [ js_obj.getTime() ]);
				default:
					return this.extract_mono_obj (js_obj);
			}
		},
		js_to_mono_uri: function (js_obj) {
			this.bindings_lazy_init ();

			switch (true) {
				case js_obj === null:
				case typeof js_obj === "undefined":
					return 0;
				case typeof js_obj === "symbol":
				case typeof js_obj === "string":
					return this.call_method(this.create_uri, null, "s!", [ js_obj ])
				default:
					return this.extract_mono_obj (js_obj);
			}
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
				var arrayType = js_obj[Symbol.for("wasm type")];
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
		wasm_binding_obj_new: function (js_obj_id, ownsHandle, type)
		{
			return this._bind_js_obj (js_obj_id, ownsHandle, type);
		},
		wasm_bind_existing: function (mono_obj, js_id)
		{
			return this._bind_existing_obj (mono_obj, js_id);
		},

		wasm_bind_core_clr_obj: function (js_id, gc_handle)
		{
			return this._bind_core_clr_obj (js_id, gc_handle);
		},

		wasm_get_js_id: function (mono_obj)
		{
			return this._get_js_id (mono_obj);
		},

		wasm_get_raw_obj: function (gchandle)
		{
			return this._get_raw_mono_obj (gchandle);
		},

		try_extract_mono_obj:function (js_obj) {
			if (js_obj === null || typeof js_obj === "undefined" || typeof js_obj.__mono_gchandle__ === "undefined")
				return 0;
			return this.wasm_get_raw_obj (js_obj.__mono_gchandle__);
		},

		mono_method_get_call_signature: function(method, mono_obj) {
			this.bindings_lazy_init ();

			return this.call_method (this.get_call_sig, null, "im", [ method, mono_obj ]);
		},

		get_task_and_bind: function (tcs, js_obj) {
			var gc_handle = this.mono_wasm_free_list.length ? this.mono_wasm_free_list.pop() : this.mono_wasm_ref_counter++;
			var task_gchandle = this.call_method (this.tcs_get_task_and_bind, null, "oi", [ tcs, gc_handle + 1 ]);
			js_obj.__mono_gchandle__ = task_gchandle;
			this.mono_wasm_object_registry[gc_handle] = js_obj;
			this.free_task_completion_source(tcs);
			tcs.is_mono_tcs_task_bound = true;
			js_obj.__mono_bound_tcs__ = tcs.__mono_gchandle__;
			tcs.__mono_bound_task__ = js_obj.__mono_gchandle__;
			return this.wasm_get_raw_obj (js_obj.__mono_gchandle__);
		},

		free_task_completion_source: function (tcs) {
			if (tcs.is_mono_tcs_result_set)
			{
				this._unbind_raw_obj_and_free (tcs.__mono_gchandle__);
			}
			if (tcs.__mono_bound_task__)
			{
				this._unbind_raw_obj_and_free (tcs.__mono_bound_task__);
			}
		},

		extract_mono_obj: function (js_obj) {
			if (js_obj === null || typeof js_obj === "undefined")
				return 0;

			var result = null;
			var gc_handle = js_obj.__mono_gchandle__;
			if (gc_handle) {
				result = this.wasm_get_raw_obj (gc_handle);

				// It's possible the managed object corresponding to this JS object was collected,
				//  in which case we need to make a new one.
				if (!result) {
					delete js_obj.__mono_gchandle__;
					delete js_obj.is_mono_bridged_obj;
				}
			}

			if (!result) {
				gc_handle = this.mono_wasm_register_obj(js_obj);
				result = this.wasm_get_raw_obj (gc_handle);
			}

			return result;
		},

		extract_js_obj_with_possible_converter: function (mono_obj, klass) {
			if (mono_obj == 0)
				return null;

			var converter = this._get_struct_unboxer_for_class (klass);
			if (converter)
				return converter (mono_obj);

			return this.extract_js_obj (mono_obj);
		},

		extract_js_obj: function (mono_obj) {
			if (mono_obj == 0)
				return null;

			var js_id = this.wasm_get_js_id (mono_obj);
			if (js_id > 0)
				return this.mono_wasm_require_handle(js_id);

			var gcHandle = this.mono_wasm_free_list.length ? this.mono_wasm_free_list.pop() : this.mono_wasm_ref_counter++;
			var js_obj = {
				__mono_gchandle__: this.wasm_bind_existing(mono_obj, gcHandle + 1),
				is_mono_bridged_obj: true
			};

			this.mono_wasm_object_registry[gcHandle] = js_obj;
			return js_obj;
		},

		_create_named_function: function (name, argumentNames, body, closure) {
			var result = null, keys = null, closureArgumentList = null, closureArgumentNames = null;

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
			result.set ('o', { steps: [{ convert: this.js_to_mono_obj.bind (this) }], size: 0, needs_root: true });
			result.set ('u', { steps: [{ convert: this.js_to_mono_uri.bind (this) }], size: 0, needs_root: true });

			// result.set ('k', { steps: [{ convert: this.js_to_mono_enum.bind (this), indirect: 'i64'}], size: 8});
			result.set ('j', { steps: [{ convert: this.js_to_mono_enum.bind (this), indirect: 'i32'}], size: 8});

			result.set ('i', { steps: [{ indirect: 'i32'}], size: 8});
			result.set ('l', { steps: [{ indirect: 'i64'}], size: 8});
			result.set ('f', { steps: [{ indirect: 'float'}], size: 8});
			result.set ('d', { steps: [{ indirect: 'double'}], size: 8});

			this._primitive_converters = result;
			return result;
		},

		name_for_marshal_type: function (mtype) {
			var MarshalTypeValues = {
				"INT": 1,
				"FP64": 2,
				"STRING": 3,
				"VT": 4,
				"DELEGATE": 5,
				"TASK": 6,
				"OBJECT": 7,
				"BOOL": 8,
				"ENUM": 9,
				"DATE": 20,
				"DATEOFFSET": 21,
				"URI": 22,
				"SAFEHANDLE": 23,
				"ARRAY_BYTE": 10,
				"ARRAY_UBYTE": 11,
				"ARRAY_UBYTE_C": 12,
				"ARRAY_SHORT": 13,
				"ARRAY_USHORT": 14,
				"ARRAY_INT": 15,
				"ARRAY_UINT": 16,
				"ARRAY_FLOAT": 17,
				"ARRAY_DOUBLE": 18,
				"FP32": 24,
				"UINT32": 25,
				"INT64": 26,
				"UINT64": 27,
				"CHAR": 28,
				"STRING_INTERNED": 29,
				"VOID": 30,
			};

			// FIXME
			var MarshalTypeNames = {};
			for (var k in MarshalTypeValues)
				MarshalTypeNames[MarshalTypeValues[k]] = k;

			return MarshalTypeNames[mtype] || String(mtype);
		},

		get_method_signature_info: function (classPtr, methodPtr) {
			// MakeMarshalSignatureInfo is a managed method, so we'll get called
			//  during the process of binding it
			if (!this.make_marshal_signature_info)
				return null;

			if (!methodPtr)
				throw new Error("Method ptr not provided");
				
			if (!this._method_signature_info_table)
				this._method_signature_info_table = new Map ();
			var result = this._method_signature_info_table.get (methodPtr);
			var classMismatch = !!result && (result.classPtr !== classPtr);
			if (!result) {
				var typePtr = classPtr 
					? this.mono_wasm_class_get_type (classPtr) 
					: 0;
				var typeName = typePtr 
					? this.mono_wasm_get_type_name(typePtr)
					: "null";
				// console.log(`Calling MakeMarshalSignatureInfo for classPtr ${classPtr}, typePtr ${typePtr} and methodPtr ${methodPtr} (typeName ${typeName})`);
				var json = this.make_marshal_signature_info (typePtr, methodPtr);
				if (!json)
					throw new Error (`MakeMarshalSignatureInfo failed`);				

				// console.log(json);
				var result = JSON.parse(json);
				result.classPtr = classPtr;

				if (classMismatch)
					console.log("WARNING: Class ptr mismatch for signature info, so caching is disabled");
				else
					this._method_signature_info_table.set (methodPtr, result);
			}
			return result;
		},

		_compile_post_filter: function (classPtr, boundConverter, js) {
			if (!js)
				return boundConverter;
			
			var closure = { 
				MONO: MONO,
				BINDING: this,
				classPtr: classPtr, 
				// (value) => filtered_value
				boundConverter: boundConverter 
			};
			var body = [
				"var value = boundConverter (js_value), filteredValue = null;",
				// "console.log(`value === ${value}`);",
				`{ filteredValue = ${js}; }`,
				// "console.log(`filteredValue === ${filteredValue}`);",
				"return filteredValue;"
			];
			
			var bodyJs = body.join ("\r\n");
			var result = this._create_named_function(
				"post_filtered_converter_for_class" + classPtr, 
				["js_value"], bodyJs, closure
			);

			// console.log("compile result", result);
			return result;
		},

		_pick_result_chara_for_marshal_type: function (mtype) {
			var signatureChForMtype = {
				1: 'i',
				2: 'd',
				3: 's',
				7: 'o',
				9: 'j',
				22: 'u', // FIXME
				24: 'f',
				25: 'i',
				26: 'l',
				27: 'l',
				28: 'i', // FIXME
			};
			if (mtype === 4)
				throw new Error ("ManagedToJS cannot return a struct");
			return signatureChForMtype[mtype] || 'a';
		},

		_get_custom_marshaler_info_for_type: function (typePtr) {
			if (!typePtr)
				return null;
			if (!MONO._custom_marshaler_name_table)
				return null;

			if (!this._custom_marshaler_info_cache)
				this._custom_marshaler_info_cache = new Map ();
			
			var result;
			if (!this._custom_marshaler_info_cache.has (typePtr)) {
				var fullName = this.mono_wasm_get_type_name (typePtr);
				var marshalerFullName = MONO._custom_marshaler_name_table[fullName];
				if (!marshalerFullName) {
					// console.log (`No custom marshaler configured for ${fullName}`);
					this._custom_marshaler_info_cache[typePtr] = null;
					return null;
				}
				var json = this.get_custom_marshaler_info (typePtr, marshalerFullName);
				result = JSON.parse(json);
				this._custom_marshaler_info_cache.set (typePtr, result);
			} else {
				result = this._custom_marshaler_info_cache.get (typePtr);
			}

			// console.log(result);

			return result;
		},

		_get_custom_marshaler_info_for_class: function (classPtr) {
			if (!classPtr)
				return null;
			var typePtr = this.mono_wasm_class_get_type (classPtr);
			return this._get_custom_marshaler_info_for_type (typePtr);
		},

		_get_struct_unboxer_for_class: function (classPtr) {
			if (!this._struct_unboxer_cache)
				this._struct_unboxer_cache = new Map ();

			if (!this._struct_unboxer_cache.has (classPtr)) {
				var typePtr = this.mono_wasm_class_get_type(classPtr);
				var info = this._get_custom_marshaler_info_for_class (classPtr);
				// HACK
				if (!info)
					info = {};
				if (info.error)
					console.error(`Error while configuring automatic converter for type ${this.mono_wasm_get_type_name(typePtr)}: ${info.error}`);

				var postFilter = info.postFilter;

				// console.log ("postFilter", postFilter);

				var convMethod = info.outputPtr;
				if (!convMethod) {
					if (info.typePtr)
						console.error(`Automatic converter for type ${this.mono_wasm_get_type_name(typePtr)} has no suitable ToJavaScript method`);
					this._struct_unboxer_cache.set (classPtr, null);
				} else {
					var signature = "m";
					var boundConverter = this.bind_method (
						convMethod, 0, signature, "ToJavaScript_class" + classPtr
					);

					this._struct_unboxer_cache.set (classPtr, this._compile_post_filter (classPtr, boundConverter, postFilter));
				}
			}

			return this._struct_unboxer_cache.get (classPtr);
		},

		_unbox_struct_rooted: function (unbox_buffer, mono_obj) {
			var objSize = Module.HEAP32[(unbox_buffer / 4) | 0];
			var classPtr = Module.HEAP32[((unbox_buffer / 4) | 0) + 1];
			var dataOffset = unbox_buffer + 8;
			if (!classPtr)
				throw new Error("classPtr is null or undefined");

			// console.log (`objSize ${objSize} classPtr ${classPtr} dataOffset ${dataOffset}`);

			var unboxer = this._get_struct_unboxer_for_class(classPtr);
			if (!unboxer) {
				var className = this.mono_wasm_get_type_name(this.mono_wasm_class_get_type(classPtr));
				throw new Error ("No CustomJavaScriptMarshaler found for struct type " + className);
			}

			// FIXME: Pass a ReadOnlySpan or ReadOnlyMemory instead of a bare pointer
			return unboxer (dataOffset);
		},

		_compile_pre_filter: function (classPtr, boundConverter, js) {
			if (!js)
				return boundConverter;
			
			var closure = { 
				MONO: MONO,
				BINDING: this,
				classPtr: classPtr, 
				// (js_obj, method, parmIdx) => value
				boundConverter: boundConverter 
			};
			var body = [
				"var filteredValue = null;",
				// "console.log(`preFilter(${value})`);",
				`{ filteredValue = ${js}; }`,
				// "console.log(`preFilter === ${filteredValue}`);",
				"var convertedResult = boundConverter (filteredValue, method, parmIdx);",
				// "console.log(`convertedResult === ${convertedResult}`);",
				"return convertedResult;"
			];
			
			var bodyJs = body.join ("\r\n");
			var result = this._create_named_function(
				"pre_filtered_converter_for_class" + classPtr, 
				["value", "method", "parmIdx"], bodyJs, closure
			);

			// console.log("compile result", result);
			return result;
		},

		_pick_automatic_converter_for_user_type: function (methodPtr, args_marshal, typePtr) {
			if (!typePtr)
				throw new Error("typePtr is null or undefined");

			if (!this._automatic_converter_table)
				this._automatic_converter_table = new Map ();
			if (!this._automatic_converter_table.has (typePtr)) {
					
				var info = this._get_custom_marshaler_info_for_type (typePtr);
				// HACK
				if (!info)
					info = {};
				if (info.error)
					console.error(`Error while configuring automatic converter for type ${this.mono_wasm_get_type_name(typePtr)}: ${info.error}`);

				var preFilter = info.preFilter;

				// console.log ("preFilter", preFilter);

				var convMethod = info.inputPtr;
				if (!convMethod) {
					if (info.typePtr)
						console.error(`Automatic converter for type ${this.mono_wasm_get_type_name(typePtr)} has no suitable FromJavaScript method`);
					this._automatic_converter_table.set (typePtr, null);
					return null;
				}

				var classPtr = this.mono_wasm_type_get_class (typePtr);

				// FIXME
				var sigInfo = this.get_method_signature_info (0, convMethod);
				// Return unboxed so it can go directly into the arguments list
				var signature = this._pick_result_chara_for_marshal_type (sigInfo.parameters[0].marshalType) + "!";
				// console.log("jstm signature", signature);
				var boundConverter = this.bind_method (
					convMethod, 0, signature, "FromJavaScript_type" + typePtr
				);

				var result = this._compile_pre_filter (classPtr, boundConverter, preFilter);

				this._automatic_converter_table.set (typePtr, result);
			}
			return this._automatic_converter_table.get (typePtr);
		},

		_pick_automatic_converter: function (methodPtr, args_marshal, paramRecord) {
			var result = {
				size: 0,
				needs_unbox: false,
				needs_root: true,
				key: 'a'
			};

			/*
			console.log("paramRecord", JSON.stringify(paramRecord));
			if (paramRecord.typePtr)
				console.log("name", this.mono_wasm_get_type_name(paramRecord.typePtr));
			*/

			switch (paramRecord.marshalType) {
				case 4: // Struct
					result.needs_unbox = true;
					; // FIXME: Fall-through
				case 7: // OBJECT
					var res = this._pick_automatic_converter_for_user_type (methodPtr, args_marshal, paramRecord.typePtr);
					if (res) {
						result.convert = res;
						break;
					}
					; // FIXME: Fall-through
				default:
					// FIXME
					// console.log("found no automatic converter for mtype", paramRecord.marshalType);
					result.convert = this.js_to_mono_obj.bind(this);
					break;
			}

			return result;
		},

		// FIXME
		_create_converter_for_marshal_string: function (classPtr, method, args_marshal) {
			var sigInfo = this.get_method_signature_info (classPtr, method);

			var primitiveConverters = this._primitive_converters;
			if (!primitiveConverters)
				primitiveConverters = this._create_primitive_converters ();

			var steps = [];
			var size = 0;
			var is_result_definitely_unmarshaled = false,
				is_result_possibly_unmarshaled = false,
				result_unmarshaled_if_argc = -1,
				needs_root_buffer = false,
				depends_on_method_arguments = false;

			for (var i = 0; i < args_marshal.length; ++i) {
				var key = args_marshal[i];

				if (key === "a") {
					if (!method)
						throw new Error ("Cannot use automatic argument type handling without a method ptr");
					depends_on_method_arguments = true;
					var step = this._pick_automatic_converter(method, args_marshal, sigInfo.parameters[i]);
					if (!step)
						throw new Error (`Failed to select an automatic converter for parameter #${i} of method ${method}`);
					steps.push(step);
					needs_root_buffer = true;
					size += step.size;
					continue;
				}

				if (i === args_marshal.length - 1) {
					if (key === "!") {
						is_result_definitely_unmarshaled = true;
						continue;
					} else if (key === "m") {
						is_result_possibly_unmarshaled = true;
						result_unmarshaled_if_argc = args_marshal.length - 1;
					}
				} else if (key === "!") {
					throw new Error ("! must be at the end of the signature");
				}

				conv = primitiveConverters.get (key);
				if (!conv)
					throw new Error (`Unknown parameter type ${key}`);

				var localStep = Object.create (conv.steps[0]);
				localStep.size = conv.size;
				if (conv.needs_root)
					needs_root_buffer = true;
				localStep.needs_root = conv.needs_root;
				localStep.key = key;
				steps.push (localStep);
				size += conv.size;
			}

			return {
				method: depends_on_method_arguments ? method : null,
				steps: steps, size: size, args_marshal: args_marshal,
				is_result_definitely_unmarshaled: is_result_definitely_unmarshaled,
				is_result_possibly_unmarshaled: is_result_possibly_unmarshaled,
				result_unmarshaled_if_argc: result_unmarshaled_if_argc,
				needs_root_buffer: needs_root_buffer
			};
		},

		_get_converter_for_marshal_string: function (classPtr, method, args_marshal) {
			if (!this._signature_converters)
				this._signature_converters = new Map();

			var converter = this._signature_converters.get (args_marshal);
			var map = null;
			if (converter instanceof Map) {
				map = converter;
				converter = map.get (method);
				// console.log (`method-keyed converter map for signature '${args_marshal}' result for method ${method}:`, converter);
			}

			if (!converter) {
				converter = this._create_converter_for_marshal_string (classPtr, method, args_marshal);
				if (converter.method) {
					if (!map)
						this._signature_converters.set (args_marshal, map = new Map ());
					// console.log (`compiled converter for signature '${args_marshal}' and method '${method}'`);
					map.set (converter.method, converter);
				} else {
					this._signature_converters.set (args_marshal, converter);
				}
			}

			return converter;
		},

		_compile_converter_for_marshal_string: function (classPtr, method, args_marshal) {
			var converter = this._get_converter_for_marshal_string (classPtr, method, args_marshal);
			if (typeof (converter.args_marshal) !== "string")
				throw new Error ("Corrupt converter for '" + args_marshal + "'");

			if (converter.compiled_function && converter.compiled_variadic_function)
				return converter;

			var converterName = args_marshal.replace ("!", "_result_unmarshaled");
			// Disambiguate different auto converters in the debugger and stack traces
			if (args_marshal.indexOf("a") >= 0)
				converterName += "_for_method" + method;
			converter.name = converterName;

			var body = [];
			var argumentNames = ["buffer", "rootBuffer", "method"];

			// worst-case allocation size instead of allocating dynamically, plus padding
			var bufferSizeBytes = converter.size + (args_marshal.length * 4) + 16;
			var rootBufferSize = args_marshal.length;
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
					body.push (`var ${valueKey} = ${closureKey} (${argKey}, method, ${i});`);
				} else {
					body.push (`var ${valueKey} = ${argKey};`);
				}

				if (step.needs_root)
					body.push (`rootBuffer.set (${i}, ${valueKey});`);

				// HACK: needs_unbox indicates that we were passed a pointer to a managed object, and either
				//  it was already rooted by our caller or (needs_root = true) by us. Now we can unbox it and
				//  pass the raw address of its boxed value into the callee.
				if (step.needs_unbox) {
					closure.mono_wasm_unbox_rooted = this.mono_wasm_unbox_rooted;
					body.push (`${valueKey} = mono_wasm_unbox_rooted (${valueKey});`);
				}

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

			bodyJs = body.join ("\r\n");
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

		_get_buffer_for_method_call: function (converter, token) {
			if (!converter)
				return 0;

			var result = 0;
			if (token !== null)	{
				result = token.scratchBuffer;
				token.scratchBuffer = 0;
			} else {
				result = converter.scratchBuffer;
				converter.scratchBuffer = 0;
			}
			return result;
		},

		_get_args_root_buffer_for_method_call: function (converter, token) {
			if (!converter)
				return null;

			if (!converter.needs_root_buffer)
				return null;

			var result = null;
			if (token !== null) {
				result = token.scratchRootBuffer;
				token.scratchRootBuffer = null;
			} else {
				result = converter.scratchRootBuffer;
				converter.scratchRootBuffer = null;
			}

			if (result === null) {
				// TODO: Expand the converter's heap allocation and then use
				//  mono_wasm_new_root_buffer_from_pointer instead. Not that important
				//  at present because the scratch buffer will be reused unless we are
				//  recursing through a re-entrant call
				result = MONO.mono_wasm_new_root_buffer (converter.steps.length);
				result.converter = converter;
			}

			return result;
		},

		_release_args_root_buffer_from_method_call: function (converter, token, argsRootBuffer) {
			if (!argsRootBuffer || !converter)
				return;

			// Store the arguments root buffer for re-use in later calls
			if ((token !== null) && (token.scratchRootBuffer == null)) {
				argsRootBuffer.clear ();
				token.scratchRootBuffer = argsRootBuffer;
			} else if (!converter.scratchRootBuffer) {
				argsRootBuffer.clear ();
				converter.scratchRootBuffer = argsRootBuffer;
			} else {
				argsRootBuffer.release ();
			}
		},

		_release_buffer_from_method_call: function (converter, token, buffer) {
			if (!converter || !buffer)
				return;

			if ((token !== null) && !token.scratchBuffer)
				token.scratchBuffer = buffer | 0;
			else if (!converter.scratchBuffer)
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

			var token = null;
			var buffer = 0, converter = null, argsRootBuffer = null;
			var is_result_marshaled = true;

			// try {
				// check if the method signature needs argument mashalling
				if (needs_converter) {
					var classPtr = this.mono_wasm_get_class_for_bind_or_invoke (this_arg, method);
					if (!classPtr)
						throw new Error (`Could not get class ptr for call_method with this (${this_arg}) and method (${method})`);

					converter = this._compile_converter_for_marshal_string (classPtr, method, args_marshal);

					is_result_marshaled = this._decide_if_result_is_marshaled (converter, args.length);

					argsRootBuffer = this._get_args_root_buffer_for_method_call (converter, null);

					var scratchBuffer = this._get_buffer_for_method_call (converter, null);

					buffer = converter.compiled_variadic_function (scratchBuffer, argsRootBuffer, method, args);
				}

				return this._call_method_with_converted_args (method, this_arg, converter, token, buffer, is_result_marshaled, argsRootBuffer);
			/*
			} catch (exc) {
				console.log("while calling method", this._method_descriptions.get(method) || method);
				throw exc;
			}
			*/
		},

		_handle_exception_for_call: function (
			converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer
		) {
			var exc = this._convert_exception_for_method_call (resultRoot.value, exceptionRoot.value);
			if (!exc)
				return;

			this._teardown_after_call (converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);
			throw exc;
		},

		_handle_exception_and_produce_result_for_call: function (
			converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled
		) {
			this._handle_exception_for_call (converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);

			if (is_result_marshaled)
				result = this._unbox_mono_obj_root (resultRoot);
			else
				result = resultRoot.value;

			this._teardown_after_call (converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);
			return result;
		},

		_teardown_after_call: function (converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer) {
			this._release_args_root_buffer_from_method_call (converter, token, argsRootBuffer);
			this._release_buffer_from_method_call (converter, token, buffer | 0);

			if (resultRoot) {
				if ((token !== null) && (token.scratchResultRoot == null))
					token.scratchResultRoot = resultRoot;
				else
					resultRoot.release ();
			}
			if (exceptionRoot) {
				if ((token !== null) && (token.scratchExceptionRoot == null))
					token.scratchExceptionRoot = exceptionRoot;
				else
					exceptionRoot.release ();
			}
		},

		_get_method_description: function (method) {
			if (!this._method_descriptions)
				this._method_descriptions = new Map();

			var result = this._method_descriptions.get (method);
			if (!result)
				result = "method#" + method;
			return result;
		},

		_call_method_with_converted_args: function (method, this_arg, converter, token, buffer, is_result_marshaled, argsRootBuffer) {
			var resultRoot = null, exceptionRoot = null;
			if (token !== null) {
				resultRoot = token.scratchResultRoot;
				exceptionRoot = token.scratchExceptionRoot;
				token.scratchResultRoot = null;
				token.scratchExceptionRoot = null;
			}
			if (resultRoot === null)
				resultRoot = MONO.mono_wasm_new_root ();
			if (exceptionRoot === null)
				exceptionRoot = MONO.mono_wasm_new_root ();
			resultRoot.value = this.invoke_method (method, this_arg, buffer, exceptionRoot.get_address ());
			return this._handle_exception_and_produce_result_for_call (converter, token, buffer, resultRoot, exceptionRoot, argsRootBuffer, is_result_marshaled);
		},

		bind_method: function (method, this_arg, args_marshal, friendly_name) {
			this.bindings_lazy_init ();

			this_arg = this_arg | 0;

			if (!this._bound_method_cache)
				this._bound_method_cache = new Map();

			// We implement a simple lookup cache here to prevent repeated bind_method calls on the same target
			//  from exhausting the set of available scratch roots. This is mostly useful for automated tests,
			//  but it may also save some naive callers from rare runtime failures
			var cacheKey = `m${method}_t${this_arg}_a${args_marshal}`;
			if (this._bound_method_cache.has(cacheKey)) {
				var cacheHit = this._bound_method_cache.get(cacheKey);
				return cacheHit;
			}

			var converter = null;
			if (typeof (args_marshal) === "string") {
				var classPtr = this.mono_wasm_get_class_for_bind_or_invoke (this_arg, method);
				if (!classPtr)
					throw new Error (`Could not get class ptr for bind_method with this (${this_arg}) and method (${method})`);
				converter = this._compile_converter_for_marshal_string (classPtr, method, args_marshal);
			}

			var token = {
				friendlyName: friendly_name,
				method: method,
				converter: converter,
				scratchRootBuffer: null,
				scratchBuffer: 0,
				scratchResultRoot: MONO.mono_wasm_new_root (),
				scratchExceptionRoot: MONO.mono_wasm_new_root ()
			};
			var closure = {
				library_mono: MONO,
				binding_support: this,
				method: method,
				this_arg: this_arg,
				token: token
			};

			var converterKey = "converter_" + converter.name;

			if (converter)
				closure[converterKey] = converter;

			var argumentNames = [];
			var body = [
				"let resultRoot = token.scratchResultRoot, exceptionRoot = token.scratchExceptionRoot;",
				"token.scratchResultRoot = null;",
				"token.scratchExceptionRoot = null;",
				"if (resultRoot === null)",
				"	resultRoot = library_mono.mono_wasm_new_root ();",
				"if (exceptionRoot === null)",
				"	exceptionRoot = library_mono.mono_wasm_new_root ();",
				""
			];

			if (converter) {
				body.push(
					`let argsRootBuffer = binding_support._get_args_root_buffer_for_method_call (${converterKey}, token);`,
					`let scratchBuffer = binding_support._get_buffer_for_method_call (${converterKey}, token);`,
					`let buffer = ${converterKey}.compiled_function (`,
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
				body.push("let argsRootBuffer = null, buffer = 0;");
			}

			if (converter.is_result_definitely_unmarshaled) {
				body.push ("let is_result_marshaled = false;");
			} else if (converter.is_result_possibly_unmarshaled) {
				body.push (`let is_result_marshaled = arguments.length !== ${converter.result_unmarshaled_if_argc};`);
			} else {
				body.push ("let is_result_marshaled = true;");
			}

			// We inline a bunch of the invoke and marshaling logic here in order to eliminate the GC pressure normally
			//  created by the unboxing part of the call process. Because unbox_mono_obj(_rooted) can return non-numeric
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
				`binding_support._handle_exception_for_call (${converterKey}, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);`,
				"",
				"let resultPtr = resultRoot.value, result = undefined;"
			);

			if (converter.is_result_possibly_unmarshaled)
				body.push("if (!is_result_marshaled) ");
			
			if (converter.is_result_definitely_unmarshaled || converter.is_result_possibly_unmarshaled)
				body.push("    result = resultPtr;");
			
			if (!converter.is_result_definitely_unmarshaled)
				body.push(
					"if (is_result_marshaled && (resultPtr !== 0)) {",
					// For the common scenario where the return type is a primitive, we want to try and unbox it directly
					//  into our existing heap allocation and then read it out of the heap. Doing this all in one operation
					//  means that we only need to enter a gc safe region twice (instead of 3+ times with the normal,
					//  slower check-type-and-then-unbox flow which has extra checks since unbox verifies the type).
					"    let unbox_buffer = binding_support._unbox_buffer;",
					"    let resultType = binding_support.mono_wasm_try_unbox_primitive_and_get_type (resultPtr, unbox_buffer, binding_support._unbox_buffer_size);",
					"    switch (resultType) {",
					"    case 1:", // int
					"        result = Module.HEAP32[unbox_buffer / 4]; break;",
					"    case 4:", // struct
					"        result = binding_support._unbox_struct_rooted (unbox_buffer, resultPtr); break;",
					"    case 25:", // uint32
					"        result = Module.HEAPU32[unbox_buffer / 4]; break;",
					"    case 24:", // float32
					"        result = Module.HEAPF32[unbox_buffer / 4]; break;",
					"    case 2:", // float64
					"        result = Module.HEAPF64[unbox_buffer / 8]; break;",
					"    case 8:", // boolean
					"        result = (Module.HEAP32[unbox_buffer / 4]) !== 0; break;",
					"    case 28:", // char
					"        result = String.fromCharCode(Module.HEAP32[unbox_buffer / 4]); break;",
					"    default:",
					"        var klass = Module.HEAPU32[unbox_buffer / 4];",
					"        result = binding_support._unbox_mono_obj_rooted_with_known_nonprimitive_type (resultPtr, resultType, klass); break;",
					"    }",
					"}"
				);

			body.push(
				"",
				`binding_support._teardown_after_call (${converterKey}, token, buffer, resultRoot, exceptionRoot, argsRootBuffer);`,
				"return result;"
			);

			bodyJs = body.join ("\r\n");

			if (friendly_name) {
				var escapeRE = /[^A-Za-z0-9_]/g;
				friendly_name = friendly_name.replace(escapeRE, "_");
			}

			var displayName = "managed_" + (friendly_name || method);

			if (this_arg)
				displayName += "_with_this_" + this_arg;

			var result = this._create_named_function(displayName, argumentNames, bodyJs, closure);

			// HACK: If the bound method has a this-arg, we don't want to store it into the cache
			//  since this indicates that the caller may be binding lots of methods onto instances
			if (!this_arg)
				this._bound_method_cache.set(cacheKey, result);

			return result;
		},

		invoke_delegate: function (delegate_obj, js_args) {
			this.bindings_lazy_init ();

			// Check to make sure the delegate is still alive on the CLR side of things.
			if (typeof delegate_obj.__mono_delegate_alive__ !== "undefined") {
				if (!delegate_obj.__mono_delegate_alive__)
					throw new Error("The delegate target that is being invoked is no longer available.  Please check if it has been prematurely GC'd.");
			}

			var [delegateRoot] = MONO.mono_wasm_new_roots ([this.extract_mono_obj (delegate_obj)]);
			try {
				if (typeof delegate_obj.__mono_delegate_invoke__ === "undefined")
					delegate_obj.__mono_delegate_invoke__ = this.mono_wasm_get_delegate_invoke(delegateRoot.value);
				if (!delegate_obj.__mono_delegate_invoke__)
					throw new Error("System.Delegate Invoke method can not be resolved.");

				if (typeof delegate_obj.__mono_delegate_invoke_sig__ === "undefined")
					delegate_obj.__mono_delegate_invoke_sig__ = Module.mono_method_get_call_signature (delegate_obj.__mono_delegate_invoke__, delegateRoot.value);

				return this.call_method (delegate_obj.__mono_delegate_invoke__, delegateRoot.value, delegate_obj.__mono_delegate_invoke_sig__, js_args);
			} finally {
				MONO.mono_wasm_release_roots (delegateRoot);
			}
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
						args[0] = BINDING.js_array_to_mono_array (args[0], true);

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
		// Object wrapping helper functions to handle reference handles that will
		// be used in managed code.
		mono_wasm_register_obj: function(obj) {

			var gc_handle = undefined;
			if (obj !== null && typeof obj !== "undefined")
			{
				gc_handle = obj.__mono_gchandle__;

				if (typeof gc_handle === "undefined") {
					var handle = this.mono_wasm_free_list.length ?
								this.mono_wasm_free_list.pop() : this.mono_wasm_ref_counter++;
					obj.__mono_jshandle__ = handle;
					// Obtain the JS -> C# type mapping.
					var wasm_type = obj[Symbol.for("wasm type")];
					obj.__owns_handle__ = true;
					gc_handle = obj.__mono_gchandle__ = this.wasm_binding_obj_new(handle + 1, obj.__owns_handle__, typeof wasm_type === "undefined" ? -1 : wasm_type);
					this.mono_wasm_object_registry[handle] = obj;

				}
			}
			return gc_handle;
		},
		mono_wasm_require_handle: function(handle) {
			if (handle > 0)
				return this.mono_wasm_object_registry[handle - 1];
			return null;
		},
		mono_wasm_unregister_obj: function(js_id) {
			var obj = this.mono_wasm_object_registry[js_id - 1];
			if (typeof obj  !== "undefined" && obj !== null) {
				// if this is the global object then do not
				// unregister it.
				if (globalThis === obj)
					return obj;

				var gc_handle = obj.__mono_gchandle__;
				if (typeof gc_handle  !== "undefined") {

					obj.__mono_gchandle__ = undefined;
					obj.__mono_jshandle__ = undefined;

					// If we are unregistering a delegate then mark it as not being alive
					// this will be checked in the delegate invoke and throw an appropriate
					// error.
					if (typeof obj.__mono_delegate_alive__ !== "undefined")
						obj.__mono_delegate_alive__ = false;

					this.mono_wasm_object_registry[js_id - 1] = undefined;
					this.mono_wasm_free_list.push(js_id - 1);
				}
			}
			return obj;
		},
		mono_wasm_free_handle: function(handle) {
			this.mono_wasm_unregister_obj(handle);
		},
		mono_wasm_free_raw_object: function(js_id) {
			var obj = this.mono_wasm_object_registry[js_id - 1];
			if (typeof obj  !== "undefined" && obj !== null) {
				// if this is the global object then do not
				// unregister it.
				if (globalThis === obj)
					return obj;

				var gc_handle = obj.__mono_gchandle__;
				if (typeof gc_handle  !== "undefined") {

					obj.__mono_gchandle__ = undefined;
					obj.__mono_jshandle__ = undefined;

					this.mono_wasm_object_registry[js_id - 1] = undefined;
					this.mono_wasm_free_list.push(js_id - 1);
				}
			}
			return obj;
		},
		mono_wasm_parse_args : function (args) {
			var js_args = this.mono_array_to_js_array(args);
			this.mono_wasm_save_LMF();
			return js_args;
		},
		mono_wasm_save_LMF : function () {
			//console.log("save LMF: " + BINDING.mono_wasm_owned_objects_frames.length)
			BINDING.mono_wasm_owned_objects_frames.push(BINDING.mono_wasm_owned_objects_LMF);
			BINDING.mono_wasm_owned_objects_LMF = undefined;
		},
		mono_wasm_unwind_LMF : function () {
			var __owned_objects__ = this.mono_wasm_owned_objects_frames.pop();
			// Release all managed objects that are loaded into the LMF
			if (typeof __owned_objects__ !== "undefined")
			{
				// Look into passing the array of owned object handles in one pass.
				var refidx;
				for (refidx = 0; refidx < __owned_objects__.length; refidx++)
				{
					var ownerRelease = __owned_objects__[refidx];
					this.call_method(this.safehandle_release_by_handle, null, "i", [ ownerRelease ]);
				}
			}
			//console.log("restore LMF: " + BINDING.mono_wasm_owned_objects_frames.length)

		},
		mono_wasm_convert_return_value: function (ret) {
			this.mono_wasm_unwind_LMF();
			return this.js_to_mono_obj (ret);
		},
	},

	mono_wasm_invoke_js_with_args: function(js_handle, method_name, args, is_exception) {
		BINDING.bindings_lazy_init ();

		var obj = BINDING.get_js_obj (js_handle);
		if (!obj) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		var js_name = BINDING.unbox_mono_obj (method_name);
		if (!js_name || (typeof(js_name) !== "string")) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid method name object '" + method_name + "'");
		}

		var js_args = BINDING.mono_wasm_parse_args(args);

		var res;
		try {
			var m = obj [js_name];
			if (typeof m === "undefined")
				throw new Error("Method: '" + js_name + "' not found for: '" + Object.prototype.toString.call(obj) + "'");
			var res = m.apply (obj, js_args);
			return BINDING.mono_wasm_convert_return_value(res);
		} catch (e) {
			// make sure we release object reference counts on errors.
			BINDING.mono_wasm_unwind_LMF();
			var res = e.toString ();
			setValue (is_exception, 1, "i32");
			if (res === null || res === undefined)
				res = "unknown exception";
			return BINDING.js_string_to_mono_string (res);
		}
	},
	mono_wasm_get_object_property: function(js_handle, property_name, is_exception) {
		BINDING.bindings_lazy_init ();

		var obj = BINDING.mono_wasm_require_handle (js_handle);
		if (!obj) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		var js_name = BINDING.conv_string (property_name);
		if (!js_name) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid property name object '" + js_name + "'");
		}

		var res;
		try {
			var m = obj [js_name];
			if (m === Object(m) && obj.__is_mono_proxied__)
				m.__is_mono_proxied__ = true;

			return BINDING.js_to_mono_obj (m);
		} catch (e) {
			var res = e.toString ();
			setValue (is_exception, 1, "i32");
			if (res === null || typeof res === "undefined")
				res = "unknown exception";
			return BINDING.js_string_to_mono_string (res);
		}
	},
    mono_wasm_set_object_property: function (js_handle, property_name, value, createIfNotExist, hasOwnProperty, is_exception) {

		BINDING.bindings_lazy_init ();

		var requireObject = BINDING.mono_wasm_require_handle (js_handle);
		if (!requireObject) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		var property = BINDING.conv_string (property_name);
		if (!property) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid property name object '" + property_name + "'");
		}

        var result = false;

		var js_value = BINDING.unbox_mono_obj(value);
		BINDING.mono_wasm_save_LMF();

        if (createIfNotExist) {
            requireObject[property] = js_value;
            result = true;
        }
        else {
			result = false;
			if (!createIfNotExist)
			{
				if (!requireObject.hasOwnProperty(property)) {
					BINDING.mono_wasm_unwind_LMF();
					return false;
				}
			}
            if (hasOwnProperty === true) {
                if (requireObject.hasOwnProperty(property)) {
                    requireObject[property] = js_value;
                    result = true;
                }
            }
            else {
                requireObject[property] = js_value;
                result = true;
            }

		}
		BINDING.mono_wasm_unwind_LMF();
        return BINDING._box_js_bool (result);
	},
	mono_wasm_get_by_index: function(js_handle, property_index, is_exception) {
		BINDING.bindings_lazy_init ();

		var obj = BINDING.mono_wasm_require_handle (js_handle);
		if (!obj) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		try {
			var m = obj [property_index];
			return BINDING.js_to_mono_obj (m);
		} catch (e) {
			var res = e.toString ();
			setValue (is_exception, 1, "i32");
			if (res === null || typeof res === "undefined")
				res = "unknown exception";
			return BINDING.js_string_to_mono_string (res);
		}
	},
	mono_wasm_set_by_index: function(js_handle, property_index, value, is_exception) {
		BINDING.bindings_lazy_init ();

		var obj = BINDING.mono_wasm_require_handle (js_handle);
		if (!obj) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		var js_value = BINDING.unbox_mono_obj(value);
		BINDING.mono_wasm_save_LMF();

		try {
			obj [property_index] = js_value;
			BINDING.mono_wasm_unwind_LMF();
			return true;
		} catch (e) {
			var res = e.toString ();
			setValue (is_exception, 1, "i32");
			if (res === null || typeof res === "undefined")
				res = "unknown exception";
			return BINDING.js_string_to_mono_string (res);
		}
	},
	mono_wasm_get_global_object: function(global_name, is_exception) {
		BINDING.bindings_lazy_init ();

		var js_name = BINDING.conv_string (global_name);

		var globalObj;

		if (!js_name) {
			globalObj = globalThis;
		}
		else {
			globalObj = globalThis[js_name];
		}

		if (globalObj === null || typeof globalObj === undefined) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Global object '" + js_name + "' not found.");
		}

		return BINDING.js_to_mono_obj (globalObj);
	},
	mono_wasm_release_handle: function(js_handle, is_exception) {
		BINDING.bindings_lazy_init ();

		BINDING.mono_wasm_free_handle(js_handle);
	},
	mono_wasm_release_object: function(js_handle, is_exception) {
		BINDING.bindings_lazy_init ();

		BINDING.mono_wasm_free_raw_object(js_handle);
	},
	mono_wasm_bind_core_object: function(js_handle, gc_handle, is_exception) {
		BINDING.bindings_lazy_init ();

		var requireObject = BINDING.mono_wasm_require_handle (js_handle);
		if (!requireObject) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		BINDING.wasm_bind_core_clr_obj(js_handle, gc_handle );
		requireObject.__mono_gchandle__ = gc_handle;
		requireObject.__js_handle__ = js_handle;
		return gc_handle;
	},
	mono_wasm_bind_host_object: function(js_handle, gc_handle, is_exception) {
		BINDING.bindings_lazy_init ();

		var requireObject = BINDING.mono_wasm_require_handle (js_handle);
		if (!requireObject) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		BINDING.wasm_bind_core_clr_obj(js_handle, gc_handle );
		requireObject.__mono_gchandle__ = gc_handle;
		return gc_handle;
	},
	mono_wasm_new: function (core_name, args, is_exception) {
		BINDING.bindings_lazy_init ();

		var js_name = BINDING.conv_string (core_name);

		if (!js_name) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Core object '" + js_name + "' not found.");
		}

		var coreObj = globalThis[js_name];

		if (coreObj === null || typeof coreObj === "undefined") {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("JavaScript host object '" + js_name + "' not found.");
		}

		var js_args = BINDING.mono_wasm_parse_args(args);

		try {

			// This is all experimental !!!!!!
			var allocator = function(constructor, js_args) {
				// Not sure if we should be checking for anything here
				var argsList = new Array();
				argsList[0] = constructor;
				if (js_args)
					argsList = argsList.concat (js_args);
				var tempCtor = constructor.bind.apply (constructor, argsList);
				var obj = new tempCtor ();
				return obj;
			};

			var res = allocator(coreObj, js_args);
			var gc_handle = BINDING.mono_wasm_free_list.length ? BINDING.mono_wasm_free_list.pop() : BINDING.mono_wasm_ref_counter++;
			BINDING.mono_wasm_object_registry[gc_handle] = res;
			return BINDING.mono_wasm_convert_return_value(gc_handle + 1);
		} catch (e) {
			var res = e.toString ();
			setValue (is_exception, 1, "i32");
			if (res === null || res === undefined)
				res = "Error allocating object.";
			return BINDING.js_string_to_mono_string (res);
		}

	},

	mono_wasm_typed_array_to_array: function(js_handle, is_exception) {
		BINDING.bindings_lazy_init ();

		var requireObject = BINDING.mono_wasm_require_handle (js_handle);
		if (!requireObject) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		return BINDING.js_typed_array_to_array(requireObject);
	},
	mono_wasm_typed_array_copy_to: function(js_handle, pinned_array, begin, end, bytes_per_element, is_exception) {
		BINDING.bindings_lazy_init ();

		var requireObject = BINDING.mono_wasm_require_handle (js_handle);
		if (!requireObject) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		var res = BINDING.typedarray_copy_to(requireObject, pinned_array, begin, end, bytes_per_element);
		return BINDING.js_to_mono_obj (res)
	},
	mono_wasm_typed_array_from: function(pinned_array, begin, end, bytes_per_element, type, is_exception) {
		BINDING.bindings_lazy_init ();
		var res = BINDING.typed_array_from(pinned_array, begin, end, bytes_per_element, type);
		return BINDING.js_to_mono_obj (res)
	},
	mono_wasm_typed_array_copy_from: function(js_handle, pinned_array, begin, end, bytes_per_element, is_exception) {
		BINDING.bindings_lazy_init ();

		var requireObject = BINDING.mono_wasm_require_handle (js_handle);
		if (!requireObject) {
			setValue (is_exception, 1, "i32");
			return BINDING.js_string_to_mono_string ("Invalid JS object handle '" + js_handle + "'");
		}

		var res = BINDING.typedarray_copy_from(requireObject, pinned_array, begin, end, bytes_per_element);
		return BINDING.js_to_mono_obj (res)
	},


};

autoAddDeps(BindingSupportLib, '$BINDING')
mergeInto(LibraryManager.library, BindingSupportLib)
