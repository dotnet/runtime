// @externs
/**
 * @typedef WasmId
 * @type {object}
 * @property {string} idStr - full object id string
 * @property {string} scheme - eg, object, valuetype, array ..
 * @property {string} value - string part after `dotnet:scheme:` of the id string
 * @property {object} o - value parsed as JSON
 */

/**
 * @typedef WasmRoot - a single address in the managed heap, visible to the GC
 * @type {object}
 * @property {ManagedPointer} value - pointer into the managed heap, stored in the root
 * @property {function} get_address - retrieves address of the root in wasm memory
 * @property {function} get - retrieves pointer value
 * @property {function} set - updates the pointer
 * @property {function} release - releases the root storage for future use
 */

/**
 * @typedef WasmRootBuffer - a collection of addresses in the managed heap, visible to the GC
 * @type {object}
 * @property {number} length - number of elements the root buffer can hold
 * @property {function} get_address - retrieves address of an element in wasm memory, by index
 * @property {function} get - retrieves an element by index
 * @property {function} set - sets an element's value by index
 * @property {function} release - releases the root storage for future use
 */

/**
 * @typedef ManagedPointer
 * @type {number} - address in the managed heap
 */

/**
 * @typedef NativePointer
 * @type {number} - address in wasm memory
 */

/**
 * @typedef Event
 * @type {object}
 * @property {string} eventName - name of the event being raised
 * @property {object} eventArgs - arguments for the event itself
 */
 
var ___cxa_is_pointer_type = function(type) {};
var ___cxa_can_catch = function(caughtType, thrownType, exceptionThrowBuf) {};
/**
 * @suppress {duplicate}
 */
var addRunDependency = function(type) {};
/**
 * @suppress {duplicate}
 */
var removeRunDependency = function(type) {};
/**
 * @suppress {duplicate}
 */
var cwrap = function(ident, returnType, argTypes, opts) {};
/**
 * @suppress {duplicate}
 */
var converter = function() {};
/**
 * @suppress {duplicate}
 */
var this_arg = {};
/**
 * @suppress {duplicate}
 */
var WasmRoot = {};
WasmRoot.value = 0;
WasmRoot.get_address = function() {};
WasmRoot.get = function() {};
WasmRoot.set = function() {};
WasmRoot.release = function() {};
/**
 * @suppress {duplicate}
 */
var converter_so = {};
converter_so.compiled_function = function() {};
/**
 * @const
 * @suppress {duplicate}
 */
var Module = {};
Module.FS_createDataFile = function(a,b,c,d,e,f) {};
Module.FS_createDevice = function(a,b,c,d) {};
Module.FS_createLazyFile = function(a,b,c,d,e) {};
Module.FS_createPath = function(a,b) {};
Module.FS_createPreloadedFile = function(a,b,c,d,e,f,g,h,r,q) {};
Module.FS_unlink = function(a) {};
Module.HEAP8 = {};
Module.HEAP16 = {};
Module.HEAP32 = {};
Module.HEAPF32 = {};
Module.HEAPF64 = {};
Module.HEAPU8 = {};
Module.HEAPU16 = {};
Module.HEAPU32 = {}
Module.UTF8ArrayToString = function(a,b,c) {};
Module.UTF8ToString = function(a,b) {};
Module.addFunction = function(a,b) {};
Module.addRunDependency = function() {};
Module.asm = {};
Module.ccall = function(a,b,c,d) {};
Module.cwrap = function(a,b,c,d) {};
Module.getValue = function(a,b) {};
Module.instantiateWasm = function(e,t) {};
Module.mono_bind_assembly_entry_point = function() {};
Module.mono_bind_method = function() {};
Module.mono_bind_static_method = function() {};
Module.mono_bindings_init = function() {};
Module.mono_call_assembly_entry_point = function() {};
Module.mono_call_static_method = function() {};
Module.mono_intern_string = function() {};
Module.mono_load_runtime_and_bcl = function() {};
Module.mono_load_runtime_and_bcl_args = function() {};
Module.mono_method_get_call_signature = function() {};
Module.mono_method_invoke = function() {};
Module.mono_method_resolve = function() {};
Module.mono_wasm_get_icudt_name = function() {};
Module.mono_wasm_get_loaded_files = function() {};
Module.mono_wasm_globalization_init = function() {};
Module.mono_wasm_load_bytes_into_heap = function() {};
Module.mono_wasm_load_icu_data = function() {};
Module.mono_wasm_new_root = function() {};
Module.mono_wasm_new_root_buffer = function() {};
Module.mono_wasm_new_root_buffer_from_pointer = function() {};
Module.mono_wasm_new_roots = function() {};
Module.mono_wasm_release_roots = function() {};
Module.postRun = [];
Module.preRun = [];
Module.preloadPlugins = [];
Module.preloadedAudios = {};
Module.preloadedImages = {};
Module.print = function() {}; 
Module.printErr = function() {};
Module.pump_message = function() {};
Module.removeRunDependency = function() {};
Module.run = function() {};
Module.setValue = function(a,b,c) {};
Module.stackAlloc = function() {};
Module.stackRestore = function() {};
Module.stackSave = function() {};
Module.___errno_location = function() {};
Module.___wasm_call_ctors = function() {};
Module.__get_daylight = function() {};
Module.__get_timezone = function() {};
Module.__get_tzname = function() {};
Module._free = function() {};
Module._htons = function() {};
Module._malloc = function() {};
Module._memalign = function() {};
Module._memset = function() {};
Module._mono_background_exec = function() {};
Module._mono_print_method_from_ip = function() {};
Module._mono_set_timeout_exec = function() {};
Module._mono_unbox_int = function() {};
Module._mono_wasm_add_assembly = function() {};
Module._mono_wasm_add_satellite_assembly = function() {};
Module._mono_wasm_array_get = function() {};
Module._mono_wasm_array_length = function() {};
Module._mono_wasm_assembly_find_class = function() {};
Module._mono_wasm_assembly_find_method = function() {};
Module._mono_wasm_assembly_get_entry_point = function() {};
Module._mono_wasm_assembly_load = function() {};
Module._mono_wasm_box_primitive = function() {};
Module._mono_wasm_clear_all_breakpoints = function() {};
Module._mono_wasm_current_bp_id = function() {};
Module._mono_wasm_deregister_root = function() {};
Module._mono_wasm_enable_on_demand_gc = function() {};
Module._mono_wasm_enum_frames = function() {};
Module._mono_wasm_exec_regression = function() {};
Module._mono_wasm_exit = function() {};
Module._mono_wasm_find_corlib_class = function() {};
Module._mono_wasm_get_array_values = function() {};
Module._mono_wasm_get_delegate_invoke = function() {};
Module._mono_wasm_get_deref_ptr_value = function() {};
Module._mono_wasm_get_icudt_name = function() {};
Module._mono_wasm_get_local_vars = function() {};
Module._mono_wasm_get_obj_type = function() {};
Module._mono_wasm_get_object_properties = function() {};
Module._mono_wasm_intern_string = function() {};
Module._mono_wasm_invoke_getter_on_object = function() {};
Module._mono_wasm_invoke_getter_on_value = function() {};
Module._mono_wasm_invoke_method = function() {};
Module._mono_wasm_load_icu_data = function() {};
Module._mono_wasm_load_runtime = function() {};
Module._mono_wasm_obj_array_new = function() {};
Module._mono_wasm_obj_array_set = function() {};
Module._mono_wasm_parse_runtime_options = function() {};
Module._mono_wasm_pause_on_exceptions = function() {};
Module._mono_wasm_register_bundled_satellite_assemblies = function() {};
Module._mono_wasm_register_root = function() {};
Module._mono_wasm_remove_breakpoint = function() {};
Module._mono_wasm_set_breakpoint = function() {};
Module._mono_wasm_set_is_debugger_attached = function() {};
Module._mono_wasm_set_main_args = function() {};
Module._mono_wasm_set_value_on_object = function() {};
Module._mono_wasm_set_variable_on_frame = function() {};
Module._mono_wasm_setenv = function() {};
Module._mono_wasm_setup_single_step = function() {};
Module._mono_wasm_strdup = function() {};
Module._mono_wasm_string_array_new = function() {};
Module._mono_wasm_string_convert = function() {};
Module._mono_wasm_string_from_js = function() {};
Module._mono_wasm_string_from_utf16 = function() {};
Module._mono_wasm_string_get_utf8 = function() {};
Module._mono_wasm_try_unbox_primitive_and_get_type = function() {};
Module._mono_wasm_typed_array_new = function() {};
Module._mono_wasm_unbox_enum = function() {};
Module._ntohs = function() {};
Module._putchar = function() {};
Module._setThrew = function() {};
/**
 * @const
 * @suppress {duplicate}
 */
var MONO = {};
MONO.mono_wasm_load_data_archive = function() {};
MONO.mono_wasm_load_bytes_into_heap = function() {};
MONO.mono_wasm_load_icu_data = function() {};
MONO.mono_wasm_setenv = function() {};
MONO.mono_wasm_runtime_ready = function() {};
MONO.pump_message = function() {};
MONO.mono_load_runtime_and_bcl = function() {};
MONO.mono_load_runtime_and_bcl_args = function() {};
MONO.mono_wasm_get_icudt_name = function() {};
MONO.mono_wasm_globalization_init = function() {};
MONO.mono_wasm_get_loaded_files = function() {};
/**
 * @param {number} capacity - the maximum number of elements the buffer can hold.
 * @param {string} [msg] - a description of the root buffer (for debugging)
 * @returns {WasmRootBuffer}
 */
MONO.mono_wasm_new_root_buffer = function(capacity, msg) {};
/**
 * @param {NativePointer} offset - the offset of the root buffer in the native heap.
 * @param {number} capacity - the maximum number of elements the buffer can hold.
 * @param {string} [msg] - a description of the root buffer (for debugging)
 * @returns {WasmRootBuffer}
 */
MONO.mono_wasm_new_root_buffer_from_pointer = function(offset, capacity, msg) {};
/**
 * @param {ManagedPointer} [value] - an address in the managed heap to initialize the root with (or 0)
 * @returns {WasmRoot}
 */
MONO.mono_wasm_new_root = function(value) {};
/**
 * @param {(number | ManagedPointer[])} count_or_values - either a number of roots or an array of pointers
 * @returns {WasmRoot[]}
 */
MONO.mono_wasm_new_roots = function(count_or_values) {};
/**
 * @param {... WasmRoot} roots
 */
MONO.mono_wasm_release_roots = function() {};
/**
 * @const
 * @suppress {duplicate}
 */
var BINDING = {};
BINDING.mono_bindings_init = function() {};
BINDING.mono_bind_method = function() {};
BINDING.mono_method_invoke = function() {};
BINDING.mono_method_get_call_signature = function() {};
BINDING.mono_method_resolve = function() {};
BINDING.mono_bind_static_method = function() {};
BINDING.mono_call_static_method = function() {};
BINDING.mono_bind_assembly_entry_point = function() {};
BINDING.mono_call_assembly_entry_point = function() {};
BINDING.mono_intern_string = function() {};
BINDING.bind_static_method = function() {};
BINDING.js_string_to_mono_string = function() {};
BINDING.conv_string = function() {};
BINDING.js_typed_array_to_array = function() {};
BINDING.mono_array_to_js_array = function() {};
BINDING.js_to_mono_obj = function() {};
BINDING.mono_obj_array_new = function() {};
BINDING.mono_obj_array_set = function() {};
BINDING.js_typed_array_to_array = function() {};
BINDING.unbox_mono_obj = function() {};
/**
 * @const
 * @suppress {duplicate}
 */
var library_mono = {};
/**
 * @return {WasmRoot}
 */
library_mono.mono_wasm_new_root = function() {};
/**
 * @const
 * @suppress {duplicate}
 */
var binding_support = {};
binding_support._get_args_root_buffer_for_method_call = function() {};
binding_support._get_buffer_for_method_call = function() {};
binding_support.invoke_method = function() {};
binding_support._handle_exception_for_call = function() {};
binding_support.mono_wasm_try_unbox_primitive_and_get_type = function() {};
binding_support.mono_wasm_try_unbox_primitive_and_get_type = function() {};
binding_support._unbox_mono_obj_rooted_with_known_nonprimitive_type = function() {};
