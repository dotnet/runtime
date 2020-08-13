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
		mono_wasm_marshal_enum_as_int: false,
		mono_bindings_init: function (binding_asm) {
			this.BINDING_ASM = binding_asm;
		},

		export_functions: function (module) {
			module ["mono_bindings_init"] = BINDING.mono_bindings_init.bind(BINDING);
			module ["mono_method_invoke"] = BINDING.call_method.bind(BINDING);
			module ["mono_method_get_call_signature"] = BINDING.mono_method_get_call_signature.bind(BINDING);
			module ["mono_method_resolve"] = BINDING.resolve_method_fqn.bind(BINDING);
			module ["mono_bind_static_method"] = BINDING.bind_static_method.bind(BINDING);
			module ["mono_call_static_method"] = BINDING.call_static_method.bind(BINDING);
			module ["mono_bind_assembly_entry_point"] = BINDING.bind_assembly_entry_point.bind(BINDING);
			module ["mono_call_assembly_entry_point"] = BINDING.call_assembly_entry_point.bind(BINDING);
		},

		bindings_lazy_init: function () {
			if (this.init)
				return;
		
			Array.prototype[Symbol.for("wasm type")] = 1;
			ArrayBuffer.prototype[Symbol.for("wasm type")] = 2;
			DataView.prototype[Symbol.for("wasm type")] = 3;
			Function.prototype[Symbol.for("wasm type")] =  4;
			Map.prototype[Symbol.for("wasm type")] = 5;
			if (typeof SharedArrayBuffer !== "undefined")
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
			this.find_class = Module.cwrap ('mono_wasm_assembly_find_class', 'number', ['number', 'string', 'string']);
			this.find_method = Module.cwrap ('mono_wasm_assembly_find_method', 'number', ['number', 'string', 'number']);
			this.invoke_method = Module.cwrap ('mono_wasm_invoke_method', 'number', ['number', 'number', 'number', 'number']);
			this.mono_string_get_utf8 = Module.cwrap ('mono_wasm_string_get_utf8', 'number', ['number']);
			this.js_string_to_mono_string = Module.cwrap ('mono_wasm_string_from_js', 'number', ['string']);
			this.mono_get_obj_type = Module.cwrap ('mono_wasm_get_obj_type', 'number', ['number']);
			this.mono_unbox_int = Module.cwrap ('mono_unbox_int', 'number', ['number']);
			this.mono_unbox_float = Module.cwrap ('mono_wasm_unbox_float', 'number', ['number']);
			this.mono_array_length = Module.cwrap ('mono_wasm_array_length', 'number', ['number']);
			this.mono_array_get = Module.cwrap ('mono_wasm_array_get', 'number', ['number', 'number']);
			this.mono_obj_array_new = Module.cwrap ('mono_wasm_obj_array_new', 'number', ['number']);
			this.mono_obj_array_set = Module.cwrap ('mono_wasm_obj_array_set', 'void', ['number', 'number', 'number']);
			this.mono_unbox_enum = Module.cwrap ('mono_wasm_unbox_enum', 'number', ['number']);
			this.assembly_get_entry_point = Module.cwrap ('mono_wasm_assembly_get_entry_point', 'number', ['number']);

			// receives a byteoffset into allocated Heap with a size.
			this.mono_typed_array_new = Module.cwrap ('mono_wasm_typed_array_new', 'number', ['number','number','number','number']);

			var binding_fqn_asm = this.BINDING_ASM.substring(this.BINDING_ASM.indexOf ("[") + 1, this.BINDING_ASM.indexOf ("]")).trim();
			var binding_fqn_class = this.BINDING_ASM.substring (this.BINDING_ASM.indexOf ("]") + 1).trim();
			
			this.binding_module = this.assembly_load (binding_fqn_asm);
			if (!this.binding_module)
				throw "Can't find bindings module assembly: " + binding_fqn_asm;

			if (binding_fqn_class !== null && typeof binding_fqn_class !== "undefined")
			{
				var namespace = "System.Runtime.InteropServices.JavaScript";
				var classname = binding_fqn_class.length > 0 ? binding_fqn_class : "Runtime";
				if (binding_fqn_class.indexOf(".") != -1) {
					var idx = binding_fqn_class.lastIndexOf(".");
					namespace = binding_fqn_class.substring (0, idx);
					classname = binding_fqn_class.substring (idx + 1);
				}
			}

			var wasm_runtime_class = this.find_class (this.binding_module, namespace, classname)
			if (!wasm_runtime_class)
				throw "Can't find " + binding_fqn_class + " class";

			var get_method = function(method_name) {
				var res = BINDING.find_method (wasm_runtime_class, method_name, -1)
				if (!res)
					throw "Can't find method " + namespace + "." + classname + ":" + method_name;
				return res;
			}
			this.bind_js_obj = get_method ("BindJSObject");
			this.bind_core_clr_obj = get_method ("BindCoreCLRObject");
			this.bind_existing_obj = get_method ("BindExistingObject");
			this.unbind_raw_obj_and_free = get_method ("UnBindRawJSObjectAndFree");			
			this.get_js_id = get_method ("GetJSObjectId");
			this.get_raw_mono_obj = get_method ("GetDotNetObject");

			this.box_js_int = get_method ("BoxInt");
			this.box_js_double = get_method ("BoxDouble");
			this.box_js_bool = get_method ("BoxBool");
			this.is_simple_array = get_method ("IsSimpleArray");
			this.setup_js_cont = get_method ("SetupJSContinuation");

			this.create_tcs = get_method ("CreateTaskSource");
			this.set_tcs_result = get_method ("SetTaskSourceResult");
			this.set_tcs_failure = get_method ("SetTaskSourceFailure");
			this.tcs_get_task_and_bind = get_method ("GetTaskAndBind");
			this.get_call_sig = get_method ("GetCallSignature");

			this.object_to_string = get_method ("ObjectToString");
			this.get_date_value = get_method ("GetDateValue");
			this.create_date_time = get_method ("CreateDateTime");
			this.create_uri = get_method ("CreateUri");

			this.safehandle_addref = get_method ("SafeHandleAddRef");
			this.safehandle_release = get_method ("SafeHandleRelease");
			this.safehandle_get_handle = get_method ("SafeHandleGetHandle");
			this.safehandle_release_by_handle = get_method ("SafeHandleReleaseByHandle");

			this.init = true;
		},

		get_js_obj: function (js_handle) {
			if (js_handle > 0)
				return this.mono_wasm_require_handle(js_handle);
			return null;
		},

		conv_string: function (mono_obj) {
			return MONO.string_decoder.copy (mono_obj);
		},

		is_nested_array: function (ele) {
			return this.call_method (this.is_simple_array, null, "mi", [ ele ]);
		},

		mono_array_to_js_array: function (mono_array) {
			if (mono_array == 0)
				return null;

			var res = [];
			var len = this.mono_array_length (mono_array);
			for (var i = 0; i < len; ++i)
			{
				var ele = this.mono_array_get (mono_array, i);
				if (this.is_nested_array(ele))
					res.push(this.mono_array_to_js_array(ele));
				else
					res.push (this.unbox_mono_obj (ele));
			}

			return res;
		},

		js_array_to_mono_array: function (js_array) {
			var mono_array = this.mono_obj_array_new (js_array.length);
			for (var i = 0; i < js_array.length; ++i) {
				this.mono_obj_array_set (mono_array, i, this.js_to_mono_obj (js_array [i]));
			}
			return mono_array;
		},

		unbox_mono_obj: function (mono_obj) {
			if (mono_obj == 0)
				return undefined;
			var type = this.mono_get_obj_type (mono_obj);
			//See MARSHAL_TYPE_ defines in driver.c
			switch (type) {
			case 1: // int
				return this.mono_unbox_int (mono_obj);
			case 2: // float
				return this.mono_unbox_float (mono_obj);
			case 3: //string
				return this.conv_string (mono_obj);
			case 4: //vts
				throw new Error ("no idea on how to unbox value types");
			case 5: { // delegate
				var obj = this.extract_js_obj (mono_obj);
				obj.__mono_delegate_alive__ = true;
				return function () {
					return BINDING.invoke_delegate (obj, arguments);
				};
			}
			case 6: {// Task

				if (typeof Promise === "undefined" || typeof Promise.resolve === "undefined")
					throw new Error ("Promises are not supported thus C# Tasks can not work in this context.");

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
			}

			case 7: // ref type
				return this.extract_js_obj (mono_obj);

			case 8: // bool
				return this.mono_unbox_int (mono_obj) != 0;

			case 9: // enum

				if(this.mono_wasm_marshal_enum_as_int)
				{
					return this.mono_unbox_enum (mono_obj);
				}
				else
				{
					enumValue = this.call_method(this.object_to_string, null, "m", [ mono_obj ]);
				}

				return enumValue;

			case 10: // arrays
			case 11: 
			case 12: 
			case 13: 
			case 14: 
			case 15: 
			case 16: 
			case 17: 
			case 18:
			{
				throw new Error ("Marshalling of primitive arrays are not supported.  Use the corresponding TypedArray instead.");
			}
			case 20: // clr .NET DateTime
				var dateValue = this.call_method(this.get_date_value, null, "md", [ mono_obj ]);
				return new Date(dateValue);
			case 21: // clr .NET DateTimeOffset
				var dateoffsetValue = this.call_method(this.object_to_string, null, "m", [ mono_obj ]);
				return dateoffsetValue;
			case 22: // clr .NET Uri
				var uriValue = this.call_method(this.object_to_string, null, "m", [ mono_obj ]);
				return uriValue;
			case 23: // clr .NET SafeHandle
				var addRef = true;
				var js_handle = this.call_method(this.safehandle_get_handle, null, "mii", [ mono_obj, addRef ]);
				var requiredObject = BINDING.mono_wasm_require_handle (js_handle);
				if (addRef)
				{
					if (typeof this.mono_wasm_owned_objects_LMF === "undefined")
						this.mono_wasm_owned_objects_LMF = [];

					this.mono_wasm_owned_objects_LMF.push(js_handle);
				}
				return requiredObject;
			default:
				throw new Error ("no idea on how to unbox object kind " + type);
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
				case typeof js_obj === "number":
					if (parseInt(js_obj) == js_obj)
						return this.call_method (this.box_js_int, null, "im", [ js_obj ]);
					return this.call_method (this.box_js_double, null, "dm", [ js_obj ]);
				case typeof js_obj === "string":
					return this.js_string_to_mono_string (js_obj);
				case typeof js_obj === "boolean":
					return this.call_method (this.box_js_bool, null, "im", [ js_obj ]);
				case isThenable() === true:
					var the_task = this.try_extract_mono_obj (js_obj);
					if (the_task)
						return the_task;
					var tcs = this.create_task_completion_source ();
					js_obj.then (function (result) {
						BINDING.set_task_result (tcs, result);
					}, function (reason) {
						BINDING.set_task_failure (tcs, reason);
					})
					return this.get_task_and_bind (tcs, js_obj);
				case js_obj.constructor.name === "Date":
					// We may need to take into account the TimeZone Offset
					return this.call_method(this.create_date_time, null, "dm", [ js_obj.getTime() ]);
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
				case typeof js_obj === "string":
					return this.call_method(this.create_uri, null, "sm", [ js_obj ])
				default:
					return this.extract_mono_obj (js_obj);
			}
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
			if (!!(js_obj.buffer instanceof ArrayBuffer && js_obj.BYTES_PER_ELEMENT)) 
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
			if (!!(typed_array.buffer instanceof ArrayBuffer && typed_array.BYTES_PER_ELEMENT)) 
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
			if (!!(typed_array.buffer instanceof ArrayBuffer && typed_array.BYTES_PER_ELEMENT)) 
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
		js_to_mono_enum: function (method, parmIdx, js_obj) {
			this.bindings_lazy_init ();
    
			if (js_obj === null || typeof js_obj === "undefined")
				return 0;

			var monoObj = this.js_to_mono_obj(js_obj);
			// Check enum contract
			var monoEnum = this.call_method(this.object_to_enum, null, "iimm", [ method, parmIdx, monoObj ])
			// return the unboxed enum value.
			return this.mono_unbox_enum(monoEnum);
		},
		wasm_binding_obj_new: function (js_obj_id, ownsHandle, type)
		{
			return this.call_method (this.bind_js_obj, null, "iii", [js_obj_id, ownsHandle, type]);
		},
		wasm_bind_existing: function (mono_obj, js_id)
		{
			return this.call_method (this.bind_existing_obj, null, "mi", [mono_obj, js_id]);
		},

		wasm_bind_core_clr_obj: function (js_id, gc_handle)
		{
			return this.call_method (this.bind_core_clr_obj, null, "ii", [js_id, gc_handle]);
		},

		wasm_get_js_id: function (mono_obj)
		{
			return this.call_method (this.get_js_id, null, "m", [mono_obj]);
		},

		wasm_get_raw_obj: function (gchandle)
		{
			return this.call_method (this.get_raw_mono_obj, null, "im", [gchandle]);
		},

		try_extract_mono_obj:function (js_obj) {
			if (js_obj === null || typeof js_obj === "undefined" || typeof js_obj.__mono_gchandle__ === "undefined")
				return 0;
			return this.wasm_get_raw_obj (js_obj.__mono_gchandle__);
		},

		mono_method_get_call_signature: function(method) {
			this.bindings_lazy_init ();

			return this.call_method (this.get_call_sig, null, "i", [ method ]);
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
				this.call_method (this.unbind_raw_obj_and_free, null, "ii", [ tcs.__mono_gchandle__ ]);
			}
			if (tcs.__mono_bound_task__)
			{
				this.call_method (this.unbind_raw_obj_and_free, null, "ii", [ tcs.__mono_bound_task__ ]);
			}
		},

		extract_mono_obj: function (js_obj) {

			if (js_obj === null || typeof js_obj === "undefined")
				return 0;

			if (!js_obj.is_mono_bridged_obj) {
				var gc_handle = this.mono_wasm_register_obj(js_obj);
				return this.wasm_get_raw_obj (gc_handle);
			}


			return this.wasm_get_raw_obj (js_obj.__mono_gchandle__);
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

		/*
		args_marshal is a string with one character per parameter that tells how to marshal it, here are the valid values:

		i: int32
		j: int32 - Enum with underlying type of int32
		l: int64 
		k: int64 - Enum with underlying type of int64
		f: float
		d: double
		s: string
		o: js object will be converted to a C# object (this will box numbers/bool/promises)
		m: raw mono object. Don't use it unless you know what you're doing

		additionally you can append 'm' to args_marshal beyond `args.length` if you don't want the return value marshaled
		*/
		call_method: function (method, this_arg, args_marshal, args) {
			this.bindings_lazy_init ();

			// Allocate memory for error
			var has_args = args !== null && typeof args !== "undefined" && args.length > 0;
			var has_args_marshal = args_marshal !== null && typeof args_marshal !== "undefined" && args_marshal.length > 0;

			if (has_args_marshal && (!has_args || args.length > args_marshal.length))
				throw Error("Parameter count mismatch.");

			var args_start = null;
			var buffer = null;
			var exception_out = null;

			// check if the method signature needs argument mashalling
			if (has_args_marshal && has_args) {
				var i;

				var converters = this.converters;
				if (!converters) {
					converters = new Map ();
					converters.set ('m', { steps: [{ }], size: 0});
					converters.set ('s', { steps: [{ convert: this.js_string_to_mono_string.bind (this)}], size: 0});
					converters.set ('o', { steps: [{ convert: this.js_to_mono_obj.bind (this)}], size: 0});
					converters.set ('u', { steps: [{ convert: this.js_to_mono_uri.bind (this)}], size: 0});
					converters.set ('k', { steps: [{ convert: this.js_to_mono_enum.bind (this), indirect: 'i64'}], size: 8});
					converters.set ('j', { steps: [{ convert: this.js_to_mono_enum.bind (this), indirect: 'i32'}], size: 8});
					converters.set ('i', { steps: [{ indirect: 'i32'}], size: 8});
					converters.set ('l', { steps: [{ indirect: 'i64'}], size: 8});
					converters.set ('f', { steps: [{ indirect: 'float'}], size: 8});
					converters.set ('d', { steps: [{ indirect: 'double'}], size: 8});
					this.converters = converters;
				}

				var converter = converters.get (args_marshal);
				if (!converter) {
					var steps = [];
					var size = 0;

					for (i = 0; i < args_marshal.length; ++i) {
						var conv = this.converters.get (args_marshal[i]);
						if (!conv)
							throw Error ("Unknown parameter type " + type);

						steps.push (conv.steps[0]);
						size += conv.size;
					}
					converter = { steps: steps, size: size };
					converters.set (args_marshal, converter);
				}

				// assume at least 8 byte alignment from malloc
				buffer = Module._malloc (converter.size + (args.length * 4) + 4);
				var indirect_start = buffer; // buffer + buffer % 8
				exception_out = indirect_start + converter.size;
				args_start = exception_out + 4;

				var slot = args_start;
				var indirect_value = indirect_start;
				for (i = 0; i < args.length; ++i) {
					var handler = converter.steps[i];
					var obj = handler.convert ? handler.convert (args[i], method, i) : args[i];

					if (handler.indirect) {
						Module.setValue (indirect_value, obj, handler.indirect);
						obj = indirect_value;
						indirect_value += 8;
					}

					Module.setValue (slot, obj, "*");
					slot += 4;
				}
			} else {
				// only marshal the exception
				exception_out = buffer = Module._malloc (4);
			}

			Module.setValue (exception_out, 0, "*");

			var res = this.invoke_method (method, this_arg, args_start, exception_out);
			var eh_res = Module.getValue (exception_out, "*");

			Module._free (buffer);

			if (eh_res != 0) {
				var msg = this.conv_string (res);
				throw new Error (msg); //the convention is that invoke_method ToString () any outgoing exception
			}

			if (has_args_marshal && has_args) {
				if (args_marshal.length >= args.length && args_marshal [args.length] === "m")
					return res;
			}

			return this.unbox_mono_obj (res);
		},

		invoke_delegate: function (delegate_obj, js_args) {
			this.bindings_lazy_init ();

			// Check to make sure the delegate is still alive on the CLR side of things.
			if (typeof delegate_obj.__mono_delegate_alive__ !== "undefined") {
				if (!delegate_obj.__mono_delegate_alive__)
					throw new Error("The delegate target that is being invoked is no longer available.  Please check if it has been prematurely GC'd.");
			}

			if (!this.delegate_dynamic_invoke) {
				if (!this.corlib)
					this.corlib = this.assembly_load ("System.Private.CoreLib");
				if (!this.delegate_class)
					this.delegate_class = this.find_class (this.corlib, "System", "Delegate");
				if (!this.delegate_class)
				{
					throw new Error("System.Delegate class can not be resolved.");
				}
				this.delegate_dynamic_invoke = this.find_method (this.delegate_class, "DynamicInvoke", -1);
			}
			var mono_args = this.js_array_to_mono_array (js_args);
			if (!this.delegate_dynamic_invoke)
				throw new Error("System.Delegate.DynamicInvoke method can not be resolved.");
			// Note: the single 'm' passed here is causing problems with AOT.  Changed to "mo" again.  
			// This may need more analysis if causes problems again.
			return this.call_method (this.delegate_dynamic_invoke, this.extract_mono_obj (delegate_obj), "mo", [ mono_args ]);
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

			var asm = this.assembly_load (assembly);
			if (!asm)
				throw new Error ("Could not find assembly: " + assembly);

			var klass = this.find_class(asm, namespace, classname);
			if (!klass)
				throw new Error ("Could not find class: " + namespace + ":" +classname);

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

			return function() {
				return BINDING.call_method (method, null, signature, arguments);
			};
		},
		bind_assembly_entry_point: function (assembly) {
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
				return BINDING.call_method (method, null, signature, arguments);
			};
		},
		call_assembly_entry_point: function (assembly, args, signature) {
			this.bindings_lazy_init ();

			var asm = this.assembly_load (assembly);
			if (!asm)
				throw new Error ("Could not find assembly: " + assembly);

			var method = this.assembly_get_entry_point(asm);
			if (!method)
				throw new Error ("Could not find entry point for assembly: " + assembly);

			if (typeof signature === "undefined")
				signature = Module.mono_method_get_call_signature (method);

			return this.call_method (method, null, signature, args);
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
				if (typeof ___mono_wasm_global___ !== "undefined" && ___mono_wasm_global___ === obj)
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
				if (typeof ___mono_wasm_global___ !== "undefined" && ___mono_wasm_global___ === obj)
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
		mono_wasm_get_global: function() {
			function testGlobal(obj) {
				obj['___mono_wasm_global___'] = obj;
				var success = typeof ___mono_wasm_global___ === 'object' && obj['___mono_wasm_global___'] === obj;
				if (!success) {
					delete obj['___mono_wasm_global___'];
				}
				return success;
			}
			if (typeof ___mono_wasm_global___ === 'object') {
				return ___mono_wasm_global___;
			}
			if (typeof global === 'object' && testGlobal(global)) {
				___mono_wasm_global___ = global;
			} else if (typeof window === 'object' && testGlobal(window)) {
				___mono_wasm_global___ = window;
			} else if (testGlobal((function(){return Function;})()('return this')())) {

				___mono_wasm_global___ = (function(){return Function;})()('return this')();

			}
			if (typeof ___mono_wasm_global___ === 'object') {
				return ___mono_wasm_global___;
			}
			throw Error('Unable to get mono wasm global object.');
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

		var js_name = BINDING.conv_string (method_name);
		if (!js_name) {
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
				if (!requireObject.hasOwnProperty(property))
					return false;
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
        return BINDING.call_method (BINDING.box_js_bool, null, "im", [ result ]);
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

		var globalObj = undefined;

		if (!js_name) {
			globalObj = BINDING.mono_wasm_get_global();
		}
		else {
			globalObj = BINDING.mono_wasm_get_global()[js_name];
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

		var coreObj = BINDING.mono_wasm_get_global()[js_name];

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
					argsList = argsList.concat(js_args);
				var obj = new (constructor.bind.apply(constructor, argsList ));
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
