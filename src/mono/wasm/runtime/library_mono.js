// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/**
 * @typedef WasmId
 * @type {object}
 * @property {string} idStr - full object id string
 * @property {string} scheme - eg, object, valuetype, array ..
 * @property {string} value - string part after `dotnet:scheme:` of the id string
 * @property {object} o - value parsed as JSON
 */

var MonoSupportLib = {
	$MONO__postset: 'MONO.export_functions (Module);',
	$MONO: {
		pump_count: 0,
		timeout_queue: [],
		_vt_stack: [],
		mono_wasm_runtime_is_ready : false,
		mono_wasm_ignore_pdb_load_errors: true,

		/** @type {object.<string, object>} */
		_id_table: {},

		pump_message: function () {
			if (!this.mono_background_exec)
				this.mono_background_exec = Module.cwrap ("mono_background_exec", null);
			while (MONO.timeout_queue.length > 0) {
				--MONO.pump_count;
				MONO.timeout_queue.shift()();
			}
			while (MONO.pump_count > 0) {
				--MONO.pump_count;
				this.mono_background_exec ();
			}
		},

		export_functions: function (module) {
			module ["pump_message"] = MONO.pump_message;
			module ["mono_load_runtime_and_bcl"] = MONO.mono_load_runtime_and_bcl;
			module ["mono_load_runtime_and_bcl_args"] = MONO.mono_load_runtime_and_bcl_args;
			module ["mono_wasm_load_bytes_into_heap"] = MONO.mono_wasm_load_bytes_into_heap;
			module ["mono_wasm_load_icu_data"] = MONO.mono_wasm_load_icu_data;
			module ["mono_wasm_globalization_init"] = MONO.mono_wasm_globalization_init;
			module ["mono_wasm_get_loaded_files"] = MONO.mono_wasm_get_loaded_files;
		},

		mono_text_decoder: undefined,
		string_decoder: {
			copy: function (mono_string) {
				if (mono_string == 0)
					return null;

				if (!this.mono_wasm_string_convert)
					this.mono_wasm_string_convert = Module.cwrap ("mono_wasm_string_convert", null, ['number']);

				this.mono_wasm_string_convert (mono_string);
				var result = this.result;
				this.result = undefined;
				return result;
			},
			decode: function (start, end, save) {
				if (!MONO.mono_text_decoder) {
					MONO.mono_text_decoder = typeof TextDecoder !== 'undefined' ? new TextDecoder('utf-16le') : undefined;
				}

				var str = "";
				if (MONO.mono_text_decoder) {
					// When threading is enabled, TextDecoder does not accept a view of a
					// SharedArrayBuffer, we must make a copy of the array first.
					var subArray = typeof SharedArrayBuffer !== 'undefined' && Module.HEAPU8.buffer instanceof SharedArrayBuffer
						? Module.HEAPU8.slice(start, end)
						: Module.HEAPU8.subarray(start, end);

					str = MONO.mono_text_decoder.decode(subArray);
				} else {
					for (var i = 0; i < end - start; i+=2) {
						var char = Module.getValue (start + i, 'i16');
						str += String.fromCharCode (char);
					}
				}
				if (save)
					this.result = str;

				return str;
			},
		},

		mono_wasm_get_exception_object: function() {
			var exception_obj = MONO.active_exception;
			MONO.active_exception = null;
			return exception_obj ;
		},

		mono_wasm_get_call_stack: function() {
			if (!this.mono_wasm_current_bp_id)
				this.mono_wasm_current_bp_id = Module.cwrap ("mono_wasm_current_bp_id", 'number');
			if (!this.mono_wasm_enum_frames)
				this.mono_wasm_enum_frames = Module.cwrap ("mono_wasm_enum_frames", null);

			var bp_id = this.mono_wasm_current_bp_id ();
			this.active_frames = [];
			this.mono_wasm_enum_frames ();

			var the_frames = this.active_frames;
			this.active_frames = [];
			return {
				"breakpoint_id": bp_id,
				"frames": the_frames,
			};
		},

		_fixup_name_value_objects: function (var_list) {
			let out_list = [];

			var i = 0;
			while (i < var_list.length) {
				let o = var_list [i];
				const name = o.name;
				if (name == null || name == undefined) {
					i ++;
					out_list.push (o);
					continue;
				}

				if (i + 1 < var_list.length) {
					o = Object.assign (o, var_list [i + 1]);
				}

				out_list.push (o);
				i += 2;
			}

			return out_list;
		},

		_filter_automatic_properties: function (props) {
			let names_found = {};
			let final_var_list = [];

			for (var i in props) {
				var p = props [i];
				if (p.name in names_found)
					continue;

				if (p.name.endsWith ("k__BackingField"))
					p.name = p.name.replace ("k__BackingField", "")
							.replace ('<', '')
							.replace ('>', '');

				names_found [p.name] = p.name;
				final_var_list.push (p);
			}

			return final_var_list;
		},

		/** Given `dotnet:object:foo:bar`,
		 * returns { scheme:'object', value: 'foo:bar' }
		 *
		 * Given `dotnet:pointer:{ b: 3 }`
		 * returns { scheme:'object', value: '{b:3}`, o: {b:3}
		 *
		 * @param  {string} idStr
		 * @param  {boolean} [throwOnError=false]
		 *
		 * @returns {WasmId}
		 */
		_parse_object_id: function (idStr, throwOnError = false) {
			if (idStr === undefined || idStr == "" || !idStr.startsWith ('dotnet:')) {
				if (throwOnError)
					throw new Error (`Invalid id: ${idStr}`);

				return undefined;
			}

			const [, scheme, ...rest] = idStr.split(':');
			let res = {
				scheme,
				value: rest.join (':'),
				idStr,
				o: {}
			};

			try {
				res.o = JSON.parse(res.value);
			// eslint-disable-next-line no-empty
			} catch (e) {}

			return res;
		},

		/**
		 * @param  {WasmId} id
		 * @returns {object[]}
		 */
		_get_vt_properties: function (id) {
			let entry = this._id_table [id.idStr];
			if (entry !== undefined && entry.members !== undefined)
				return entry.members;

			if (!isNaN (id.o.containerId))
				this._get_object_properties (id.o.containerId, true);
			else if (!isNaN (id.o.arrayId))
				this._get_array_values (id, Number (id.o.arrayIdx), 1, true);
			else
				throw new Error (`Invalid valuetype id (${id.idStr}). Can't get properties for it.`);

			entry = this._get_id_props (id.idStr);
			if (entry !== undefined && entry.members !== undefined)
				return entry.members;

			throw new Error (`Unknown valuetype id: ${id.idStr}`);
		},

		/**
		 *
		 * @callback GetIdArgsCallback
		 * @param {object} var
		 * @param {number} idx
		 * @returns {object}
		 */

		/**
		 * @param  {object[]} vars
		 * @param  {GetIdArgsCallback} getIdArgs
		 * @returns {object}
		 */
		_assign_vt_ids: function (vars, getIdArgs)
		{
			vars.forEach ((v, i) => {
				// we might not have a `.value`, like in case of getters which have a `.get` instead
				const value = v.value;
				if (value === undefined || !value.isValueType)
					return;

				if (value.objectId !== undefined)
					throw new Error (`Bug: Trying to assign valuetype id, but the var already has one: ${v}`);

				value.objectId = this._new_or_add_id_props ({ scheme: 'valuetype', idArgs: getIdArgs (v, i), props: value._props });
				delete value._props;
			});

			return vars;
		},

		//
		// @var_list: [ { index: <var_id>, name: <var_name> }, .. ]
		mono_wasm_get_variables: function(scope, var_list) {
			const numBytes = var_list.length * Int32Array.BYTES_PER_ELEMENT;
			const ptr = Module._malloc(numBytes);
			let heapBytes = new Int32Array(Module.HEAP32.buffer, ptr, numBytes);
			for (let i=0; i<var_list.length; i++) {
				heapBytes[i] = var_list[i].index;
			}

			this._async_method_objectId = 0;
			let { res_ok, res } = this.mono_wasm_get_local_vars_info (scope, heapBytes.byteOffset, var_list.length);
			Module._free(heapBytes.byteOffset);
			if (!res_ok)
				throw new Error (`Failed to get locals for scope ${scope}`);

			if (this._async_method_objectId != 0)
				this._assign_vt_ids (res, v => ({ containerId: this._async_method_objectId, fieldOffset: v.fieldOffset }));

			for (let i in res) {
				const res_name = res [i].name;
				if (this._async_method_objectId != 0) {
					//Async methods are special in the way that local variables can be lifted to generated class fields
					//value of "this" comes here either
					if (res_name !== undefined && res_name.indexOf ('>') > 0) {
						// For async methods, we get the names too, so use that
						// ALTHOUGH, the name wouldn't have `<>` for method args
						res [i].name = res_name.substring (1, res_name.indexOf ('>'));
					}
				} else if (res_name === undefined && var_list [i] !== undefined) {
					// For non-async methods, we just have the var id, but we have the name
					// from the caller
					res [i].name = var_list [i].name;
				}
			}

			this._post_process_details(res);
			return res;
		},

		/**
		 * @param  {number} idNum
		 * @param  {boolean} expandValueTypes
		 * @returns {object}
		 */
		_get_object_properties: function(idNum, expandValueTypes) {
			let { res_ok, res } = this.mono_wasm_get_object_properties_info (idNum, expandValueTypes);
			if (!res_ok)
				throw new Error (`Failed to get properties for ${idNum}`);

			res = MONO._filter_automatic_properties (res);
			res = this._assign_vt_ids (res, v => ({ containerId: idNum, fieldOffset: v.fieldOffset }));
			res = this._post_process_details (res);

			return res;
		},

		/**
		 * @param  {WasmId} id
		 * @param  {number} [startIdx=0]
		 * @param  {number} [count=-1]
		 * @param  {boolean} [expandValueTypes=false]
		 * @returns {object[]}
		 */
		_get_array_values: function (id, startIdx = 0, count = -1, expandValueTypes = false) {
			if (isNaN (id.o.arrayId) || isNaN (startIdx))
				throw new Error (`Invalid array id: ${id.idStr}`);

			let { res_ok, res } = this.mono_wasm_get_array_values_info (id.o.arrayId, startIdx, count, expandValueTypes);
			if (!res_ok)
				throw new Error (`Failed to get properties for array id ${id.idStr}`);

			res = this._assign_vt_ids (res, (_, i) => ({ arrayId: id.o.arrayId, arrayIdx: Number (startIdx) + i}));

			for (let i = 0; i < res.length; i ++) {
				let value = res [i].value;
				if (value.objectId !== undefined && value.objectId.startsWith("dotnet:pointer"))
					this._new_or_add_id_props ({ objectId: value.objectId, props: { varName: `[${i}]` } });
			}
			res = this._post_process_details (res);
			return res;
		},

		_post_process_details: function (details) {
			if (details == undefined)
				return {};

			if (details.length > 0)
				this._extract_and_cache_value_types(details);

			return details;
		},

		/**
		 * Gets the next id number to use for generating ids
		 *
		 * @returns {number}
		 */
		_next_id: function () {
			return ++this._next_id_var;
		},

		_extract_and_cache_value_types: function (var_list) {
			if (var_list == undefined || !Array.isArray (var_list) || var_list.length == 0)
				return var_list;

			for (let i in var_list) {
				let value = var_list [i].value;
				if (value === undefined)
					continue;

				if (value.objectId !== undefined && value.objectId.startsWith ("dotnet:pointer:")) {
					let ptr_args = this._get_id_props (value.objectId);
					if (ptr_args === undefined)
						throw new Error (`Bug: Expected to find an entry for pointer id: ${value.objectId}`);

					// It might have been already set in some cases, like arrays
					// where the name would be `0`, but we want `[0]` for pointers,
					// so the deref would look like `*[0]`
					ptr_args.varName = ptr_args.varName || var_list [i].name;
				}

				if (value.type != "object" || value.isValueType != true || value.expanded != true) // undefined would also give us false
					continue;

				// Generate objectId for expanded valuetypes
				value.objectId = value.objectId || this._new_or_add_id_props ({ scheme: 'valuetype' });

				this._extract_and_cache_value_types (value.members);

				const new_props = Object.assign ({ members: value.members }, value.__extra_vt_props);

				this._new_or_add_id_props ({ objectId: value.objectId, props: new_props });
				delete value.members;
				delete value.__extra_vt_props;
			}

			return var_list;
		},

		_get_cfo_res_details: function (objectId, args) {
			if (!(objectId in this._call_function_res_cache))
				throw new Error(`Could not find any object with id ${objectId}`);

			const real_obj = this._call_function_res_cache [objectId];

			const descriptors = Object.getOwnPropertyDescriptors (real_obj);
			if (args.accessorPropertiesOnly) {
				Object.keys (descriptors).forEach (k => {
					if (descriptors [k].get === undefined)
						Reflect.deleteProperty (descriptors, k);
				});
			}

			let res_details = [];
			Object.keys (descriptors).forEach (k => {
				let new_obj;
				let prop_desc = descriptors [k];
				if (typeof prop_desc.value == "object") {
					// convert `{value: { type='object', ... }}`
					// to      `{ name: 'foo', value: { type='object', ... }}
					new_obj = Object.assign ({ name: k }, prop_desc);
				} else if (prop_desc.value !== undefined) {
					// This is needed for values that were not added by us,
					// thus are like { value: 5 }
					// instead of    { value: { type = 'number', value: 5 }}
					//
					// This can happen, for eg., when `length` gets added for arrays
					// or `__proto__`.
					new_obj = {
						name: k,
						// merge/add `type` and `description` to `d.value`
						value: Object.assign ({ type: (typeof prop_desc.value), description: '' + prop_desc.value },
												prop_desc)
					};
				} else if (prop_desc.get !== undefined) {
					// The real_obj has the actual getter. We are just returning a placeholder
					// If the caller tries to run function on the cfo_res object,
					// that accesses this property, then it would be run on `real_obj`,
					// which *has* the original getter
					new_obj = {
						name: k,
						get: {
							className: "Function",
							description: `get ${k} () {}`,
							type: "function"
						}
					};
				} else {
					new_obj = { name: k, value: { type: "symbol", value: "<Unknown>", description: "<Unknown>"} };
				}

				res_details.push (new_obj);
			});

			return { __value_as_json_string__: JSON.stringify (res_details) };
		},

		/**
		 * Generates a new id, and a corresponding entry for associated properties
		 *    like `dotnet:pointer:{ a: 4 }`
		 * The third segment of that `{a:4}` is the idArgs parameter
		 *
		 * Only `scheme` or `objectId` can be set.
		 * if `scheme`, then a new id is generated, and it's properties set
		 * if `objectId`, then it's properties are updated
		 *
		 * @param {object} args
		 * @param  {string} [args.scheme=undefined] scheme second part of `dotnet:pointer:..`
		 * @param  {string} [args.objectId=undefined] objectId
		 * @param  {object} [args.idArgs={}] The third segment of the objectId
		 * @param  {object} [args.props={}] Properties for the generated id
		 *
		 * @returns {string} generated/updated id string
		 */
		_new_or_add_id_props: function ({ scheme = undefined, objectId = undefined, idArgs = {}, props = {} }) {
			if (scheme === undefined && objectId === undefined)
				throw new Error (`Either scheme or objectId must be given`);

			if (scheme !== undefined && objectId !== undefined)
				throw new Error (`Both scheme, and objectId cannot be given`);

			if (objectId !== undefined && Object.entries (idArgs).length > 0)
				throw new Error (`Both objectId, and idArgs cannot be given`);

			if (Object.entries (idArgs).length == 0) {
				// We want to generate a new id, only if it doesn't have other
				// attributes that it can use to uniquely identify.
				// Eg, we don't do this for `dotnet:valuetype:{containerId:4, fieldOffset: 24}`
				idArgs.num = this._next_id ();
			}

			let idStr;
			if (objectId !== undefined) {
				idStr = objectId;
				const old_props = this._id_table [idStr];
				if (old_props === undefined)
					throw new Error (`ObjectId not found in the id table: ${idStr}`);

				this._id_table [idStr] = Object.assign (old_props, props);
			} else {
				idStr = `dotnet:${scheme}:${JSON.stringify (idArgs)}`;
				this._id_table [idStr] = props;
			}

			return idStr;
		},

		/**
		 * @param  {string} objectId
		 * @returns {object}
		 */
		_get_id_props: function (objectId) {
			return this._id_table [objectId];
		},

		_get_deref_ptr_value: function (objectId) {
			const ptr_args = this._get_id_props (objectId);
			if (ptr_args === undefined)
				throw new Error (`Unknown pointer id: ${objectId}`);

			if (ptr_args.ptr_addr == 0 || ptr_args.klass_addr == 0)
				throw new Error (`Both ptr_addr and klass_addr need to be non-zero, to dereference a pointer. objectId: ${objectId}`);

			const value_addr = new DataView (Module.HEAPU8.buffer).getUint32 (ptr_args.ptr_addr, /* littleEndian */ true);
			let { res_ok, res } = this.mono_wasm_get_deref_ptr_value_info (value_addr, ptr_args.klass_addr);
			if (!res_ok)
				throw new Error (`Failed to dereference pointer ${objectId}`);

			if (res.length > 0) {
				if (ptr_args.varName === undefined)
					throw new Error (`Bug: no varName found for the pointer. objectId: ${objectId}`);

				res [0].name = `*${ptr_args.varName}`;
			}

			res = this._post_process_details (res);
			return res;
		},

		mono_wasm_get_details: function (objectId, args) {
			let id = this._parse_object_id (objectId, true);

			switch (id.scheme) {
				case "object": {
					if (isNaN (id.value))
						throw new Error (`Invalid objectId: ${objectId}. Expected a numeric id.`);

					return this._get_object_properties(id.value, false);
				}

				case "array":
					return this._get_array_values (id);

				case "valuetype":
					return this._get_vt_properties(id);

				case "cfo_res":
					return this._get_cfo_res_details (objectId, args);

				case "pointer": {
					return this._get_deref_ptr_value (objectId);
				}

				default:
					throw new Error(`Unknown object id format: ${objectId}`);
			}
		},

		_cache_call_function_res: function (obj) {
			const id = `dotnet:cfo_res:${this._next_call_function_res_id++}`;
			this._call_function_res_cache[id] = obj;
			return id;
		},

		mono_wasm_release_object: function (objectId) {
			if (objectId in this._cache_call_function_res)
				delete this._cache_call_function_res[objectId];
		},

		/**
		 * @param  {string} objectIdStr objectId
		 * @param  {string} name property name
		 * @returns {object} return value
		 */
		_invoke_getter: function (objectIdStr, name) {
			const id = this._parse_object_id (objectIdStr);
			if (id === undefined)
				throw new Error (`Invalid object id: ${objectIdStr}`);

			let getter_res;
			if (id.scheme == 'object') {
				if (isNaN (id.o) || id.o < 0)
					throw new Error (`Invalid object id: ${objectIdStr}`);

				let { res_ok, res } = this.mono_wasm_invoke_getter_on_object_info (id.o, name);
				if (!res_ok)
					throw new Error (`Invoking getter on ${objectIdStr} failed`);

				getter_res = res;
			} else if (id.scheme == 'valuetype') {
				const id_props = this._get_id_props (objectIdStr);
				if (id_props === undefined)
					throw new Error (`Unknown valuetype id: ${objectIdStr}`);

				if (typeof id_props.value64 !== 'string' || isNaN (id_props.klass))
					throw new Error (`Bug: Cannot invoke getter on ${objectIdStr}, because of missing or invalid klass/value64 fields. idProps: ${JSON.stringify (id_props)}`);

				const dataPtr = Module._malloc (id_props.value64.length);
				const dataHeap = new Uint8Array (Module.HEAPU8.buffer, dataPtr, id_props.value64.length);
				dataHeap.set (new Uint8Array (this._base64_to_uint8 (id_props.value64)));

				let { res_ok, res } = this.mono_wasm_invoke_getter_on_value_info (dataHeap.byteOffset, id_props.klass, name);
				Module._free (dataHeap.byteOffset);

				if (!res_ok) {
					console.debug (`Invoking getter on valuetype ${objectIdStr}, with props: ${JSON.stringify (id_props)} failed`);
					throw new Error (`Invoking getter on valuetype ${objectIdStr} failed`);
				}
				getter_res = res;
			} else {
				throw new Error (`Only object, and valuetypes supported for getters, id: ${objectIdStr}`);
			}

			getter_res = MONO._post_process_details (getter_res);
			return getter_res.length > 0 ? getter_res [0] : {};
		},

		_create_proxy_from_object_id: function (objectId) {
			const details = this.mono_wasm_get_details(objectId);

			if (objectId.startsWith ('dotnet:array:'))
				return details.map (p => p.value);

			let proxy = {};
			Object.keys (details).forEach (p => {
				var prop = details [p];
				if (prop.get !== undefined) {
					// TODO: `set`

					Object.defineProperty (proxy,
							prop.name,
							{ get () { return MONO._invoke_getter (objectId, prop.name); } }
					);
				} else {
					proxy [prop.name] = prop.value;
				}
			});

			return proxy;
		},

		mono_wasm_call_function_on: function (request) {
			if (request.arguments != undefined && !Array.isArray (request.arguments))
				throw new Error (`"arguments" should be an array, but was ${request.arguments}`);

			const objId = request.objectId;
			let proxy;

			if (objId.startsWith ('dotnet:cfo_res:')) {
				if (objId in this._call_function_res_cache)
					proxy = this._call_function_res_cache [objId];
				else
					throw new Error (`Unknown object id ${objId}`);
			} else {
				proxy = this._create_proxy_from_object_id (objId);
			}

			const fn_args = request.arguments != undefined ? request.arguments.map(a => JSON.stringify(a.value)) : [];
			const fn_eval_str = `var fn = ${request.functionDeclaration}; fn.call (proxy, ...[${fn_args}]);`;

			const fn_res = eval (fn_eval_str);
			if (fn_res === undefined)
				return { type: "undefined" };

			if (fn_res === null || (fn_res.subtype === 'null' && fn_res.value === undefined))
				return fn_res;

			// primitive type
			if (Object (fn_res) !== fn_res)
				return fn_res;

			// return .value, if it is a primitive type
			if (fn_res.value !== undefined && Object (fn_res.value.value) !== fn_res.value.value)
				return fn_res.value;

			if (request.returnByValue)
				return {type: "object", value: fn_res};

			const fn_res_id = this._cache_call_function_res (fn_res);
			if (Object.getPrototypeOf (fn_res) == Array.prototype) {
				return {
					type: "object",
					subtype: "array",
					className: "Array",
					description: `Array(${fn_res.length})`,
					objectId: fn_res_id
				};
			} else {
				return { type: "object", className: "Object", description: "Object", objectId: fn_res_id };
			}
		},

		_clear_per_step_state: function () {
			this._next_id_var = 0;
			this._id_table = {};
		},

		mono_wasm_debugger_resume: function () {
			this._clear_per_step_state ();
		},

		mono_wasm_start_single_stepping: function (kind) {
			console.log (">> mono_wasm_start_single_stepping " + kind);
			if (!this.mono_wasm_setup_single_step)
				this.mono_wasm_setup_single_step = Module.cwrap ("mono_wasm_setup_single_step", 'number', [ 'number']);

			this._clear_per_step_state ();

			return this.mono_wasm_setup_single_step (kind);
		},

		mono_wasm_set_pause_on_exceptions: function (state) {
			if (!this.mono_wasm_pause_on_exceptions)
				this.mono_wasm_pause_on_exceptions = Module.cwrap ("mono_wasm_pause_on_exceptions", 'number', [ 'number']);
			var state_enum = 0;
			switch (state) {
				case 'uncaught':
					state_enum = 1; //EXCEPTION_MODE_UNCAUGHT
					break;
				case 'all':
					state_enum = 2; //EXCEPTION_MODE_ALL
					break;
			}
			return this.mono_wasm_pause_on_exceptions (state_enum);
		},

		_register_c_fn: function (name, ...args) {
			Object.defineProperty (this._c_fn_table, name + '_wrapper', { value: Module.cwrap (name, ...args) });
		},

		/**
		 * Calls `Module.cwrap` for the function name,
		 * and creates a wrapper around it that returns
		 *     `{ bool result, object var_info }
		 *
		 * @param  {string} name C function name
		 * @param  {string} ret_type
		 * @param  {string[]} params
		 *
		 * @returns {void}
		 */
		_register_c_var_fn: function (name, ret_type, params) {
			if (ret_type !== 'bool')
				throw new Error (`Bug: Expected a C function signature that returns bool`);

			this._register_c_fn (name, ret_type, params);
			Object.defineProperty (this, name + '_info', {
				value: function (...args) {
					MONO.var_info = [];
					const res_ok = MONO._c_fn_table [name + '_wrapper'] (...args);
					let res = MONO.var_info;
					MONO.var_info = [];
					if (res_ok) {
						res = this._fixup_name_value_objects (res);
						return { res_ok, res };
					}

					return { res_ok, res: undefined };
				}
			});
		},

		mono_wasm_runtime_ready: function () {
			this.mono_wasm_runtime_is_ready = true;
			// DO NOT REMOVE - magic debugger init function
			console.debug ("mono_wasm_runtime_ready", "fe00e07a-5519-4dfe-b35a-f867dbaf2e28");

			this._clear_per_step_state ();

			// FIXME: where should this go?
			this._next_call_function_res_id = 0;
			this._call_function_res_cache = {};

			this._c_fn_table = {};
			this._register_c_var_fn ('mono_wasm_get_object_properties',   'bool', [ 'number', 'bool' ]);
			this._register_c_var_fn ('mono_wasm_get_array_values',        'bool', [ 'number', 'number', 'number', 'bool' ]);
			this._register_c_var_fn ('mono_wasm_invoke_getter_on_object', 'bool', [ 'number', 'string' ]);
			this._register_c_var_fn ('mono_wasm_invoke_getter_on_value',  'bool', [ 'number', 'number', 'string' ]);
			this._register_c_var_fn ('mono_wasm_get_local_vars',          'bool', [ 'number', 'number', 'number']);
			this._register_c_var_fn ('mono_wasm_get_deref_ptr_value',     'bool', [ 'number', 'number']);
		},

		mono_wasm_set_breakpoint: function (assembly, method_token, il_offset) {
			if (!this.mono_wasm_set_bp)
				this.mono_wasm_set_bp = Module.cwrap ('mono_wasm_set_breakpoint', 'number', ['string', 'number', 'number']);

			return this.mono_wasm_set_bp (assembly, method_token, il_offset)
		},

		mono_wasm_remove_breakpoint: function (breakpoint_id) {
			if (!this.mono_wasm_del_bp)
				this.mono_wasm_del_bp = Module.cwrap ('mono_wasm_remove_breakpoint', 'number', ['number']);

			return this.mono_wasm_del_bp (breakpoint_id);
		},

		// Set environment variable NAME to VALUE
		// Should be called before mono_load_runtime_and_bcl () in most cases
		mono_wasm_setenv: function (name, value) {
			if (!this.wasm_setenv)
				this.wasm_setenv = Module.cwrap ('mono_wasm_setenv', null, ['string', 'string']);
			this.wasm_setenv (name, value);
		},

		mono_wasm_set_runtime_options: function (options) {
			if (!this.wasm_parse_runtime_options)
				this.wasm_parse_runtime_options = Module.cwrap ('mono_wasm_parse_runtime_options', null, ['number', 'number']);
			var argv = Module._malloc (options.length * 4);
			var wasm_strdup = Module.cwrap ('mono_wasm_strdup', 'number', ['string']);
			let aindex = 0;
			for (var i = 0; i < options.length; ++i) {
				Module.setValue (argv + (aindex * 4), wasm_strdup (options [i]), "i32");
				aindex += 1;
			}
			this.wasm_parse_runtime_options (options.length, argv);
		},

		//
		// Initialize the AOT profiler with OPTIONS.
		// Requires the AOT profiler to be linked into the app.
		// options = { write_at: "<METHODNAME>", send_to: "<METHODNAME>" }
		// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
		// write_at defaults to 'WebAssembly.Runtime::StopProfile'.
		// send_to defaults to 'WebAssembly.Runtime::DumpAotProfileData'.
		// DumpAotProfileData stores the data into Module.aot_profile_data.
		//
		mono_wasm_init_aot_profiler: function (options) {
			if (options == null)
				options = {}
			if (!('write_at' in options))
				options.write_at = 'WebAssembly.Runtime::StopProfile';
			if (!('send_to' in options))
				options.send_to = 'WebAssembly.Runtime::DumpAotProfileData';
			var arg = "aot:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
			Module.ccall ('mono_wasm_load_profiler_aot', null, ['string'], [arg]);
		},

		// options = { write_at: "<METHODNAME>", send_to: "<METHODNAME>" }
		// <METHODNAME> should be in the format <CLASS>::<METHODNAME>.
		// write_at defaults to 'WebAssembly.Runtime::StopProfile'.
		// send_to defaults to 'WebAssembly.Runtime::DumpCoverageProfileData'.
		// DumpCoverageProfileData stores the data into Module.coverage_profile_data.
		mono_wasm_init_coverage_profiler: function (options) {
			if (options == null)
				options = {}
			if (!('write_at' in options))
				options.write_at = 'WebAssembly.Runtime::StopProfile';
			if (!('send_to' in options))
				options.send_to = 'WebAssembly.Runtime::DumpCoverageProfileData';
			var arg = "coverage:write-at-method=" + options.write_at + ",send-to-method=" + options.send_to;
			Module.ccall ('mono_wasm_load_profiler_coverage', null, ['string'], [arg]);
		},

		_apply_configuration_from_args: function (args) {
			for (var k in (args.environment_variables || {}))
				MONO.mono_wasm_setenv (k, args.environment_variables[k]);

			if (args.runtime_options)
				MONO.mono_wasm_set_runtime_options (args.runtime_options);

			if (args.aot_profiler_options)
				MONO.mono_wasm_init_aot_profiler (args.aot_profiler_options);

			if (args.coverage_profiler_options)
				MONO.mono_wasm_init_coverage_profiler (args.coverage_profiler_options);
		},

		_get_fetch_file_cb_from_args: function (args) {
			if (typeof (args.fetch_file_cb) === "function")
				return args.fetch_file_cb;

			if (ENVIRONMENT_IS_NODE) {
				var fs = require('fs');
				return function (asset) {
					console.log ("MONO_WASM: Loading... " + asset);
					var binary = fs.readFileSync (asset);
					var resolve_func2 = function (resolve, reject) {
						resolve (new Uint8Array (binary));
					};

					var resolve_func1 = function (resolve, reject) {
						var response = {
							ok: true,
							url: asset,
							arrayBuffer: function () {
								return new Promise (resolve_func2);
							}
						};
						resolve (response);
					};

					return new Promise (resolve_func1);
				};
			} else if (typeof (fetch) === "function") {
				return function (asset) {
					return fetch (asset, { credentials: 'same-origin' });
				};
			} else {
				throw new Error ("No fetch_file_cb was provided and this environment does not expose 'fetch'.");
			}
		},

		_handle_loaded_asset: function (ctx, asset, url, blob) {
			var bytes = new Uint8Array (blob);
			if (ctx.tracing)
				console.log ("MONO_WASM: Loaded:", asset.name, "size", bytes.length, "from", url);
			else
				console.log ("MONO_WASM: Loaded:", asset.name);

			var virtualName = asset.virtual_path || asset.name;
			var offset = null;

			switch (asset.behavior) {
				case "assembly":
					ctx.loaded_files.push ({ url: url, file: virtualName});
				case "heap":
				case "icu":
					offset = this.mono_wasm_load_bytes_into_heap (bytes);
					ctx.loaded_assets[virtualName] = [offset, bytes.length];
					break;

				case "vfs":
					// FIXME
					var lastSlash = virtualName.lastIndexOf("/");
					var parentDirectory = (lastSlash > 0)
						? virtualName.substr(0, lastSlash)
						: null;
					var fileName = (lastSlash > 0)
						? virtualName.substr(lastSlash + 1)
						: virtualName;
					if (fileName.startsWith("/"))
						fileName = fileName.substr(1);
					if (parentDirectory) {
						if (ctx.tracing)
							console.log ("MONO_WASM: Creating directory '" + parentDirectory + "'");

						var pathRet = ctx.createPath(
							"/", parentDirectory, true, true // fixme: should canWrite be false?
						);
					} else {
						parentDirectory = "/";
					}

					if (ctx.tracing)
						console.log ("MONO_WASM: Creating file '" + fileName + "' in directory '" + parentDirectory + "'");

					if (!this.mono_wasm_load_data_archive (bytes, parentDirectory)) {
						var fileRet = ctx.createDataFile (
							parentDirectory, fileName,
							bytes, true /* canRead */, true /* canWrite */, true /* canOwn */
						);
					}
					break;

				default:
					throw new Error ("Unrecognized asset behavior:", asset.behavior, "for asset", asset.name);
			}

			if (asset.behavior === "assembly") {
				var hasPpdb = ctx.mono_wasm_add_assembly (virtualName, offset, bytes.length);

				if (!hasPpdb) {
					var index = ctx.loaded_files.findIndex(element => element.file == virtualName);
					ctx.loaded_files.splice(index, 1);
				}
			}
			else if (asset.behavior === "icu") {
				if (this.mono_wasm_load_icu_data (offset))
					ctx.num_icu_assets_loaded_successfully += 1;
				else
					console.error ("Error loading ICU asset", asset.name);
			}
		},

		// deprecated
		mono_load_runtime_and_bcl: function (
			unused_vfs_prefix, deploy_prefix, debug_level, file_list, loaded_cb, fetch_file_cb
		) {
			var args = {
				fetch_file_cb: fetch_file_cb,
				loaded_cb: loaded_cb,
				debug_level: debug_level,
				assembly_root: deploy_prefix,
				assets: []
			};

			for (var i = 0; i < file_list.length; i++) {
				var file_name = file_list[i];
				var behavior;
				if (file_name === "icudt.dat")
					behavior = "icu";
				else // if (file_name.endsWith (".pdb") || file_name.endsWith (".dll"))
					behavior = "assembly";

				args.assets.push ({
					name: file_name,
					behavior: behavior
				});
			}

			return this.mono_load_runtime_and_bcl_args (args);
		},

		// Initializes the runtime and loads assemblies, debug information, and other files.
		// @args is a dictionary-style Object with the following properties:
		//    assembly_root: (required) the subfolder containing managed assemblies and pdbs
		//    debug_level or enable_debugging: (required)
		//    assets: (required) a list of assets to load along with the runtime. each asset
		//     is a dictionary-style Object with the following properties:
		//        name: (required) the name of the asset, including extension.
		//        behavior: (required) determines how the asset will be handled once loaded:
		//          "heap": store asset into the native heap
		//          "assembly": load asset as a managed assembly (or debugging information)
		//          "icu": load asset as an ICU data archive
		//          "vfs": load asset into the virtual filesystem (for fopen, File.Open, etc)
		//        load_remote: (optional) if true, an attempt will be made to load the asset
		//          from each location in @args.remote_sources.
		//        virtual_path: (optional) if specified, overrides the path of the asset in
		//          the virtual filesystem and similar data structures once loaded.
		//        is_optional: (optional) if true, any failure to load this asset will be ignored.
		//    loaded_cb: (required) a function () invoked when loading has completed.
		//    fetch_file_cb: (optional) a function (string) invoked to fetch a given file.
		//      If no callback is provided a default implementation appropriate for the current
		//      environment will be selected (readFileSync in node, fetch elsewhere).
		//      If no default implementation is available this call will fail.
		//    remote_sources: (optional) additional search locations for assets.
		//      sources will be checked in sequential order until the asset is found.
		//      the string "./" indicates to load from the application directory (as with the
		//      files in assembly_list), and a fully-qualified URL like "https://example.com/" indicates
		//      that asset loads can be attempted from a remote server. Sources must end with a "/".
		//    environment_variables: (optional) dictionary-style Object containing environment variables
		//    runtime_options: (optional) array of runtime options as strings
		//    aot_profiler_options: (optional) dictionary-style Object. see the comments for
		//      mono_wasm_init_aot_profiler. If omitted, aot profiler will not be initialized.
		//    coverage_profiler_options: (optional) dictionary-style Object. see the comments for
		//      mono_wasm_init_coverage_profiler. If omitted, coverage profiler will not be initialized.
		//    globalization_mode: (optional) configures the runtime's globalization mode:
		//      "icu": load ICU globalization data from any runtime assets with behavior "icu".
		//      "invariant": operate in invariant globalization mode.
		//      "auto" (default): if "icu" behavior assets are present, use ICU, otherwise invariant.
		//    diagnostic_tracing: (optional) enables diagnostic log messages during startup
		mono_load_runtime_and_bcl_args: function (args) {
			try {
				return this._load_assets_and_runtime (args);
			} catch (exc) {
				console.error ("error in mono_load_runtime_and_bcl_args:", exc);
				throw exc;
			}
		},

		// @bytes must be a typed array. space is allocated for it in the native heap
		//  and it is copied to that location. returns the address of the allocation.
		mono_wasm_load_bytes_into_heap: function (bytes) {
			var memoryOffset = Module._malloc (bytes.length);
			var heapBytes = new Uint8Array (Module.HEAPU8.buffer, memoryOffset, bytes.length);
			heapBytes.set (bytes);
			return memoryOffset;
		},

		num_icu_assets_loaded_successfully: 0,

		// @offset must be the address of an ICU data archive in the native heap.
		// returns true on success.
		mono_wasm_load_icu_data: function (offset) {
			var fn = Module.cwrap ('mono_wasm_load_icu_data', 'number', ['number']);
			var ok = (fn (offset)) === 1;
			if (ok)
				this.num_icu_assets_loaded_successfully++;
			return ok;
		},

		_finalize_startup: function (args, ctx) {
			var loaded_files_with_debug_info = [];

			MONO.loaded_assets = ctx.loaded_assets;
			ctx.loaded_files.forEach(value => loaded_files_with_debug_info.push(value.url));
			MONO.loaded_files = loaded_files_with_debug_info;
			if (ctx.tracing) {
				console.log ("MONO_WASM: loaded_assets: " + JSON.stringify(ctx.loaded_assets));
				console.log ("MONO_WASM: loaded_files: " + JSON.stringify(ctx.loaded_files));
			}

			var load_runtime = Module.cwrap ('mono_wasm_load_runtime', null, ['string', 'number']);

			console.log ("MONO_WASM: Initializing mono runtime");

			this.mono_wasm_globalization_init (args.globalization_mode);

			if (ENVIRONMENT_IS_SHELL || ENVIRONMENT_IS_NODE) {
				try {
					load_runtime ("unused", args.debug_level);
				} catch (ex) {
					print ("MONO_WASM: load_runtime () failed: " + ex);
					print ("MONO_WASM: Stacktrace: \n");
					print (ex.stack);

					var wasm_exit = Module.cwrap ('mono_wasm_exit', null, ['number']);
					wasm_exit (1);
				}
			} else {
				load_runtime ("unused", args.debug_level);
			}

			MONO.mono_wasm_runtime_ready ();
			args.loaded_cb ();
		},

		_load_assets_and_runtime: function (args) {
			if (args.enable_debugging)
				args.debug_level = args.enable_debugging;
			if (args.assembly_list)
				throw new Error ("Invalid args (assembly_list was replaced by assets)");
			if (args.runtime_assets)
				throw new Error ("Invalid args (runtime_assets was replaced by assets)");
			if (args.runtime_asset_sources)
				throw new Error ("Invalid args (runtime_asset_sources was replaced by remote_sources)");
			if (!args.loaded_cb)
				throw new Error ("loaded_cb not provided");

			var ctx = {
				tracing: args.diagnostic_tracing || false,
				pending_count: args.assets.length,
				mono_wasm_add_assembly: Module.cwrap ('mono_wasm_add_assembly', 'number', ['string', 'number', 'number']),
				loaded_assets: Object.create (null),
				// dlls and pdbs, used by blazor and the debugger
				loaded_files: [],
				createPath: Module['FS_createPath'],
				createDataFile: Module['FS_createDataFile']
			};

			if (ctx.tracing)
				console.log ("mono_wasm_load_runtime_with_args", JSON.stringify(args));

			this._apply_configuration_from_args (args);

			var fetch_file_cb = this._get_fetch_file_cb_from_args (args);

			var onPendingRequestComplete = function () {
				--ctx.pending_count;

				if (ctx.pending_count === 0) {
					try {
						MONO._finalize_startup (args, ctx);
					} catch (exc) {
						console.error ("Unhandled exception in _finalize_startup", exc);
						throw exc;
					}
				}
			};

			var processFetchResponseBuffer = function (asset, url, blob) {
				try {
					MONO._handle_loaded_asset (ctx, asset, url, blob);
				} catch (exc) {
					console.error ("Unhandled exception in processFetchResponseBuffer", exc);
					throw exc;
				} finally {
					onPendingRequestComplete ();
				}
			};

			args.assets.forEach (function (asset) {
				var attemptNextSource;
				var sourceIndex = 0;
				var sourcesList = asset.load_remote ? args.remote_sources : [""];

				var handleFetchResponse = function (response) {
					if (!response.ok) {
						try {
							attemptNextSource ();
							return;
						} catch (exc) {
							console.error ("MONO_WASM: Unhandled exception in handleFetchResponse attemptNextSource for asset", asset.name, exc);
							throw exc;
						}
					}

					try {
						var bufferPromise = response ['arrayBuffer'] ();
						bufferPromise.then (processFetchResponseBuffer.bind (this, asset, response.url));
					} catch (exc) {
						console.error ("MONO_WASM: Unhandled exception in handleFetchResponse for asset", asset.name, exc);
						attemptNextSource ();
					}
				};

				attemptNextSource = function () {
					if (sourceIndex >= sourcesList.length) {
						var msg = "MONO_WASM: Failed to load " + asset.name;
						try {
							var isOk = asset.is_optional ||
								(asset.name.match (/\.pdb$/) && MONO.mono_wasm_ignore_pdb_load_errors);

							if (isOk)
								console.log (msg);
							else {
								console.error (msg);
								throw new Error (msg);
							}
						} finally {
							onPendingRequestComplete ();
						}
					}

					var sourcePrefix = sourcesList[sourceIndex];
					sourceIndex++;

					// HACK: Special-case because MSBuild doesn't allow "" as an attribute
					if (sourcePrefix === "./")
						sourcePrefix = "";

					var attemptUrl;
					if (sourcePrefix.trim() === "") {
						if (asset.behavior === "assembly")
							attemptUrl = locateFile (args.assembly_root + "/" + asset.name);
						else
							attemptUrl = asset.name;
					} else {
						attemptUrl = sourcePrefix + asset.name;
					}

					try {
						if (asset.name === attemptUrl) {
							if (ctx.tracing)
								console.log ("Attempting to fetch '" + attemptUrl + "'");
						} else {
							if (ctx.tracing)
								console.log ("Attempting to fetch '" + attemptUrl + "' for", asset.name);
						}
						var fetch_promise = fetch_file_cb (attemptUrl);
						fetch_promise.then (handleFetchResponse);
					} catch (exc) {
						console.error ("MONO_WASM: Error fetching " + attemptUrl, exc);
						attemptNextSource ();
					}
				};

				attemptNextSource ();
			});
		},

		// Performs setup for globalization.
		// @globalization_mode is one of "icu", "invariant", or "auto".
		// "auto" will use "icu" if any ICU data archives have been loaded,
		//  otherwise "invariant".
		mono_wasm_globalization_init: function (globalization_mode) {
			var invariantMode = false;

			if (globalization_mode === "invariant")
				invariantMode = true;

			if (!invariantMode) {
				if (this.num_icu_assets_loaded_successfully > 0) {
					console.log ("MONO_WASM: ICU data archive(s) loaded, disabling invariant mode");
				} else if (globalization_mode !== "icu") {
					console.log ("MONO_WASM: ICU data archive(s) not loaded, using invariant globalization mode");
					invariantMode = true;
				} else {
					var msg = "invariant globalization mode is inactive and no ICU data archives were loaded";
					console.error ("MONO_WASM: ERROR: " + msg);
					throw new Error (msg);
				}
			}

			if (invariantMode)
				this.mono_wasm_setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1");
		},

		// Used by the debugger to enumerate loaded dlls and pdbs
		mono_wasm_get_loaded_files: function() {
			return MONO.loaded_files;
		},

		mono_wasm_get_loaded_asset_table: function() {
			return MONO.loaded_assets;
		},

		mono_wasm_clear_all_breakpoints: function() {
			if (!this.mono_clear_bps)
				this.mono_clear_bps = Module.cwrap ('mono_wasm_clear_all_breakpoints', null);

			this.mono_clear_bps ();
		},

		mono_wasm_add_null_var: function(className)
		{
			let fixed_class_name = MONO._mono_csharp_fixup_class_name(Module.UTF8ToString (className));
			if (!fixed_class_name) {
				// Eg, when a @className is passed from js itself, like
				// mono_wasm_add_null_var ("string")
				fixed_class_name = className;
			}
			MONO.var_info.push ({value: {
				type: "object",
				className: fixed_class_name,
				description: fixed_class_name,
				subtype: "null"
			}});
		},

		_mono_wasm_add_string_var: function(var_value) {
			if (var_value == 0) {
				MONO.mono_wasm_add_null_var ("string");
				return;
			}

			MONO.var_info.push({
				value: {
					type: "string",
					value: var_value,
					description: var_value
				}
			});
		},

		_mono_wasm_add_getter_var: function(className, invokable) {
			const fixed_class_name = MONO._mono_csharp_fixup_class_name (className);
			if (invokable != 0) {
				var name;
				if (MONO.var_info.length > 0)
					name = MONO.var_info [MONO.var_info.length - 1].name;
				name = (name === undefined) ? "" : name;

				MONO.var_info.push({
					get: {
						className: "Function",
						description: `get ${name} () {}`,
						type: "function",
					}
				});
			} else {
				var value = `${fixed_class_name} { get; }`;
				MONO.var_info.push({
					value: {
						type: "symbol",
						description: value,
						value: value,
					}
				});
			}
		},

		_mono_wasm_add_array_var: function(className, objectId, length) {
			const fixed_class_name = MONO._mono_csharp_fixup_class_name(className);
			if (objectId == 0) {
				MONO.mono_wasm_add_null_var (fixed_class_name);
				return;
			}

			MONO.var_info.push({
				value: {
					type: "object",
					subtype: "array",
					className: fixed_class_name,
					description: `${fixed_class_name}(${length})`,
					objectId: this._new_or_add_id_props ({ scheme: 'array', idArgs: { arrayId: objectId } })
				}
			});
		},

		// FIXME: improve
		_base64_to_uint8: function (base64String) {
			const byteCharacters = atob (base64String);
			const byteNumbers = new Array(byteCharacters.length);
			for (let i = 0; i < byteCharacters.length; i++) {
				byteNumbers[i] = byteCharacters.charCodeAt(i);
			}

			return new Uint8Array (byteNumbers);
		},

		_begin_value_type_var: function(className, args) {
			if (args === undefined || (typeof args !== 'object')) {
				console.debug (`_begin_value_type_var: Expected an args object`);
				return;
			}

			const fixed_class_name = MONO._mono_csharp_fixup_class_name(className);
			const toString = args.toString;
			const base64String = btoa (String.fromCharCode (...new Uint8Array (Module.HEAPU8.buffer, args.value_addr, args.value_size)));
			const vt_obj = {
				value: {
					type            : "object",
					className       : fixed_class_name,
					description     : (toString == 0 ? fixed_class_name: Module.UTF8ToString (toString)),
					expanded        : true,
					isValueType     : true,
					__extra_vt_props: { klass: args.klass, value64: base64String },
					members         : []
				}
			};
			if (MONO._vt_stack.length == 0)
				MONO._old_var_info = MONO.var_info;

			MONO.var_info = vt_obj.value.members;
			MONO._vt_stack.push (vt_obj);
		},

		_end_value_type_var: function() {
			let top_vt_obj_popped = MONO._vt_stack.pop ();
			top_vt_obj_popped.value.members = MONO._filter_automatic_properties (
								MONO._fixup_name_value_objects (top_vt_obj_popped.value.members));

			if (MONO._vt_stack.length == 0) {
				MONO.var_info = MONO._old_var_info;
				MONO.var_info.push(top_vt_obj_popped);
			} else {
				var top_obj = MONO._vt_stack [MONO._vt_stack.length - 1];
				top_obj.value.members.push (top_vt_obj_popped);
				MONO.var_info = top_obj.value.members;
			}
		},

		_add_valuetype_unexpanded_var: function(className, args) {
			if (args === undefined || (typeof args !== 'object')) {
				console.debug (`_add_valuetype_unexpanded_var: Expected an args object`);
				return;
			}

			const fixed_class_name = MONO._mono_csharp_fixup_class_name (className);
			const toString = args.toString;

			MONO.var_info.push ({
				value: {
					type: "object",
					className: fixed_class_name,
					description: (toString == 0 ? fixed_class_name : Module.UTF8ToString (toString)),
					isValueType: true
				}
			});
		},


		mono_wasm_add_typed_value: function (type, str_value, value) {
			let type_str = type;
			if (typeof type != 'string')
				type_str = Module.UTF8ToString (type);
				str_value = Module.UTF8ToString (str_value);

			switch (type_str) {
			case "bool": {
				const v = value != 0;
				MONO.var_info.push ({
					value: {
						type: "boolean",
						value: v,
						description: v.toString ()
					}
				});
				break;
			}

			case "char": {
				const v = `${value} '${String.fromCharCode (value)}'`;
				MONO.var_info.push ({
					value: {
						type: "symbol",
						value: v,
						description: v
					}
				});
				break;
			}

			case "number":
				MONO.var_info.push ({
					value: {
						type: "number",
						value: value,
						description: '' + value
					}
				});
				break;

			case "string":
				MONO._mono_wasm_add_string_var (str_value);
				break;

			case "getter":
				MONO._mono_wasm_add_getter_var (str_value, value);
				break;

			case "array":
				MONO._mono_wasm_add_array_var (str_value, value.objectId, value.length);
				break;

			case "begin_vt":
				MONO._begin_value_type_var (str_value, value);
				break;

			case "end_vt":
				MONO._end_value_type_var ();
				break;

			case "unexpanded_vt":
				MONO._add_valuetype_unexpanded_var (str_value, value);
				break;

			case "pointer": {
				const fixed_value_str = MONO._mono_csharp_fixup_class_name (str_value);
				if (value.klass_addr == 0 || value.ptr_addr == 0 || fixed_value_str.startsWith ('(void*')) {
					// null or void*, which we can't deref
					MONO.var_info.push({
						value: {
							type: "symbol",
							value: fixed_value_str,
							description: fixed_value_str
						}
					});
				} else {
					MONO.var_info.push({
						value: {
							type: "object",
							className: fixed_value_str,
							description: fixed_value_str,
							objectId: this._new_or_add_id_props ({ scheme: 'pointer', props: value })
						}
					});
				}
				}
				break;

			default: {
				const msg = `'${str_value}' ${value}`;

				MONO.var_info.push ({
					value: {
						type: "symbol",
						value: msg,
						description: msg
					}
				});
				break;
				}
			}
		},

		_mono_csharp_fixup_class_name: function(className)
		{
			// Fix up generic names like Foo`2<int, string> to Foo<int, string>
			// and nested class names like Foo/Bar to Foo.Bar
			return className.replace(/\//g, '.').replace(/`\d+/g, '');
		},

		mono_wasm_load_data_archive: function (data, prefix) {
			if (data.length < 8)
				return false;

			var dataview = new DataView(data.buffer);
			var magic = dataview.getUint32(0, true);
			//	get magic number
			if (magic != 0x626c6174) {
				return false;
			}
			var manifestSize = dataview.getUint32(4, true);
			if (manifestSize == 0 || data.length < manifestSize + 8)
				return false;

			var manifest;
			try {
				manifestContent = Module.UTF8ArrayToString(data, 8, manifestSize);
				manifest = JSON.parse(manifestContent);
				if (!(manifest instanceof Array))
					return false;
			} catch (exc) {
				return false;
			}

			data = data.slice(manifestSize+8);

			// Create the folder structure
			// /usr/share/zoneinfo
			// /usr/share/zoneinfo/Africa
			// /usr/share/zoneinfo/Asia
			// ..

			var folders = new Set()
			manifest.filter(m => {
				var file = m[0];
				var last = file.lastIndexOf ("/");
				var directory = file.slice (0, last);
				folders.add(directory);
			});
			folders.forEach(folder => {
				Module['FS_createPath'](prefix, folder, true, true);
			});

			for (row of manifest) {
				var name = row[0];
				var length = row[1];
				var bytes = data.slice(0, length);
				Module['FS_createDataFile'](prefix, name, bytes, true, true);
				data = data.slice(length);
			}
			return true;
		}
	},

	mono_wasm_add_typed_value: function (type, str_value, value) {
		MONO.mono_wasm_add_typed_value (type, str_value, value);
	},

	mono_wasm_add_properties_var: function(name, field_offset) {
		MONO.var_info.push({
			name: Module.UTF8ToString (name),
			fieldOffset: field_offset
		});
	},

	mono_wasm_set_is_async_method: function(objectId) {
		MONO._async_method_objectId = objectId;
	},

	mono_wasm_add_enum_var: function(className, members, value) {
		// FIXME: flags
		//

		// group0: Monday:0
		// group1: Monday
		// group2: 0
		const re = new RegExp (`[,]?([^,:]+):(${value}(?=,)|${value}$)`, 'g')
		const members_str = Module.UTF8ToString (members);

		const match = re.exec(members_str);
		const member_name = match == null ? ('' + value) : match [1];

		const fixed_class_name = MONO._mono_csharp_fixup_class_name(Module.UTF8ToString (className));
		MONO.var_info.push({
			value: {
				type: "object",
				className: fixed_class_name,
				description: member_name,
				isEnum: true
			}
		});
	},

	mono_wasm_add_array_item: function(position) {
		MONO.var_info.push({
			name: `${position}`
		});
	},

	mono_wasm_add_obj_var: function(className, toString, objectId) {
		if (objectId == 0) {
			MONO.mono_wasm_add_null_var (className);
			return;
		}

		const fixed_class_name = MONO._mono_csharp_fixup_class_name(Module.UTF8ToString (className));
		MONO.var_info.push({
			value: {
				type: "object",
				className: fixed_class_name,
				description: (toString == 0 ? fixed_class_name : Module.UTF8ToString (toString)),
				objectId: "dotnet:object:"+ objectId,
			}
		});
	},

	/*
	 * @className, and @targetName are in the following format:
	 *
	 *  <ret_type>:[<comma separated list of arg types>]:<method name>
	 */
	mono_wasm_add_func_var: function (className, targetName, objectId) {
		if (objectId == 0) {
			MONO.mono_wasm_add_null_var (
				MONO._mono_csharp_fixup_class_name (Module.UTF8ToString (className)));
			return;
		}

		function args_to_sig (args_str) {
			var parts = args_str.split (":");
			// TODO: min length = 3?
			parts = parts.map (a => MONO._mono_csharp_fixup_class_name (a));

			// method name at the end
			var method_name = parts.pop ();

			// ret type at the beginning
			var ret_sig = parts [0];
			var args_sig = parts.splice (1).join (', ');
			return `${ret_sig} ${method_name} (${args_sig})`;
		}

		let tgt_sig;
		if (targetName != 0)
			tgt_sig = args_to_sig (Module.UTF8ToString (targetName));

		const type_name = MONO._mono_csharp_fixup_class_name (Module.UTF8ToString (className));

		if (objectId == -1) {
			// Target property
			MONO.var_info.push ({
				value: {
					type: "symbol",
					value: tgt_sig,
					description: tgt_sig,
				}
			});
		} else {
			MONO.var_info.push ({
				value: {
					type: "object",
					className: type_name,
					description: tgt_sig,
					objectId: "dotnet:object:" + objectId,
				}
			});
		}
	},

	mono_wasm_add_frame: function(il, method, assembly_name, method_full_name) {
		var parts = Module.UTF8ToString (method_full_name).split (":", 2);
		MONO.active_frames.push( {
			il_pos: il,
			method_token: method,
			assembly_name: Module.UTF8ToString (assembly_name),
			// Extract just the method name from `{class_name}:{method_name}`
			method_name: parts [parts.length - 1]
		});
	},

	schedule_background_exec: function () {
		++MONO.pump_count;
		if (ENVIRONMENT_IS_WEB) {
			window.setTimeout (MONO.pump_message, 0);
		} else if (ENVIRONMENT_IS_WORKER) {
			self.setTimeout (MONO.pump_message, 0);
		} else if (ENVIRONMENT_IS_NODE) {
			global.setTimeout (MONO.pump_message, 0);
		}
	},

	mono_set_timeout: function (timeout, id) {
		if (!this.mono_set_timeout_exec)
			this.mono_set_timeout_exec = Module.cwrap ("mono_set_timeout_exec", null, [ 'number' ]);
		if (ENVIRONMENT_IS_WEB) {
			window.setTimeout (function () {
				this.mono_set_timeout_exec (id);
			}, timeout);
		} else if (ENVIRONMENT_IS_WORKER) {
			self.setTimeout (function () {
				this.mono_set_timeout_exec (id);
			}, timeout);
		} else if (ENVIRONMENT_IS_NODE) {
			global.setTimeout (function () {
				global.mono_set_timeout_exec (id);
			}, timeout);
		} else {
			++MONO.pump_count;
			MONO.timeout_queue.push(function() {
				this.mono_set_timeout_exec (id);
			})
		}
	},

	mono_wasm_fire_bp: function () {
		console.log ("mono_wasm_fire_bp");
		// eslint-disable-next-line no-debugger
		debugger;
	},

	mono_wasm_fire_exception: function (exception_id, message, class_name, uncaught) {
		MONO.active_exception = {
			exception_id: exception_id,
			message     : Module.UTF8ToString (message),
			class_name  : Module.UTF8ToString (class_name),
			uncaught    : uncaught
		};
		debugger;
	},
};

autoAddDeps(MonoSupportLib, '$MONO')
mergeInto(LibraryManager.library, MonoSupportLib)
