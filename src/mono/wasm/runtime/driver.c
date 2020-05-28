// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
#include <emscripten.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <dlfcn.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/threads.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-dl-fallback.h>
#include <mono/jit/jit.h>

#include "pinvoke-table.h"

#ifdef CORE_BINDINGS
void core_initialize_internals ();
#endif

// Blazor specific custom routines - see dotnet_support.js for backing code
extern void* mono_wasm_invoke_js_marshalled (MonoString **exceptionMessage, void *asyncHandleLongPtr, MonoString *funcName, MonoString *argsJson);
extern void* mono_wasm_invoke_js_unmarshalled (MonoString **exceptionMessage, MonoString *funcName, void* arg0, void* arg1, void* arg2);

void mono_wasm_enable_debugging (int);

void mono_ee_interp_init (const char *opts);
void mono_marshal_ilgen_init (void);
void mono_method_builder_ilgen_init (void);
void mono_sgen_mono_ilgen_init (void);
void mono_icall_table_init (void);
void mono_aot_register_module (void **aot_info);
char *monoeg_g_getenv(const char *variable);
int monoeg_g_setenv(const char *variable, const char *value, int overwrite);
void mono_free (void*);
int32_t mini_parse_debug_option (const char *option);

static MonoClass* datetime_class;
static MonoClass* datetimeoffset_class;
static MonoClass* uri_class;

int mono_wasm_enable_gc;

/* Not part of public headers */
#define MONO_ICALL_TABLE_CALLBACKS_VERSION 2

typedef struct {
	int version;
	void* (*lookup) (MonoMethod *method, char *classname, char *methodname, char *sigstart, int32_t *uses_handles);
	const char* (*lookup_icall_symbol) (void* func);
} MonoIcallTableCallbacks;

void
mono_install_icall_table_callbacks (const MonoIcallTableCallbacks *cb);

int mono_regression_test_step (int verbose_level, char *image, char *method_name);
void mono_trace_init (void);

#define g_new(type, size)  ((type *) malloc (sizeof (type) * (size)))
#define g_new0(type, size) ((type *) calloc (sizeof (type), (size)))

static MonoDomain *root_domain;

static MonoString*
mono_wasm_invoke_js (MonoString *str, int *is_exception)
{
	if (str == NULL)
		return NULL;

	mono_unichar2 *native_val = mono_string_chars (str);
	int native_len = mono_string_length (str) * 2;

	mono_unichar2 *native_res = (mono_unichar2*)EM_ASM_INT ({
		var str = MONO.string_decoder.decode ($0, $0 + $1);
		try {
			var res = eval (str);
			if (res === null || res == undefined)
				return 0;
			res = res.toString ();
			setValue ($2, 0, "i32");
		} catch (e) {
			res = e.toString ();
			setValue ($2, 1, "i32");
			if (res === null || res === undefined)
				res = "unknown exception";
		}
		var buff = Module._malloc((res.length + 1) * 2);
		stringToUTF16 (res, buff, (res.length + 1) * 2);
		return buff;
	}, (int)native_val, native_len, is_exception);

	if (native_res == NULL)
		return NULL;

	MonoString *res = mono_string_from_utf16 (native_res);
	free (native_res);
	return res;
}

static void
wasm_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data)
{
	if (fatal) {
		EM_ASM(
			   var err = new Error();
			   console.log ("Stacktrace: \n");
			   console.log (err.stack);
			   );

		fprintf (stderr, "%s\n", message);
		fflush (stderr);

		abort ();
	} else {
		fprintf (stdout, "L: %s\n", message);
	}
}

#ifdef DRIVER_GEN
#include "driver-gen.c"
#endif

typedef struct WasmAssembly_ WasmAssembly;

struct WasmAssembly_ {
	MonoBundledAssembly assembly;
	WasmAssembly *next;
};

static WasmAssembly *assemblies;
static int assembly_count;

EMSCRIPTEN_KEEPALIVE void
mono_wasm_add_assembly (const char *name, const unsigned char *data, unsigned int size)
{
	int len = strlen (name);
	if (!strcasecmp (".pdb", &name [len - 4])) {
		char *new_name = strdup (name);
		//FIXME handle debugging assemblies with .exe extension
		strcpy (&new_name [len - 3], "dll");
		mono_register_symfile_for_assembly (new_name, data, size);
		return;
	}
	WasmAssembly *entry = g_new0 (WasmAssembly, 1);
	entry->assembly.name = strdup (name);
	entry->assembly.data = data;
	entry->assembly.size = size;
	entry->next = assemblies;
	assemblies = entry;
	++assembly_count;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_setenv (const char *name, const char *value)
{
	monoeg_g_setenv (strdup (name), strdup (value), 1);
}

#ifdef ENABLE_NETCORE
static void *sysglobal_native_handle;
#endif

static void*
wasm_dl_load (const char *name, int flags, char **err, void *user_data)
{
	for (int i = 0; i < sizeof (pinvoke_tables) / sizeof (void*); ++i) {
		if (!strcmp (name, pinvoke_names [i]))
			return pinvoke_tables [i];
	}

#ifdef ENABLE_NETCORE
	if (!strcmp (name, "System.Globalization.Native"))
		return sysglobal_native_handle;
#endif

#if WASM_SUPPORTS_DLOPEN
	return dlopen(name, flags);
#endif

	return NULL;
}

static mono_bool
wasm_dl_is_pinvoke_tables (void* handle)
{
	for (int i = 0; i < sizeof (pinvoke_tables) / sizeof (void*); ++i) {
		if (pinvoke_tables [i] == handle) {
			return 1;
		}
	}
	return 0;
}

static void*
wasm_dl_symbol (void *handle, const char *name, char **err, void *user_data)
{
#ifdef ENABLE_NETCORE
	if (handle == sysglobal_native_handle)
		assert (0);
#endif

#if WASM_SUPPORTS_DLOPEN
	if (!wasm_dl_is_pinvoke_tables (handle)) {
		return dlsym (handle, name);
	}
#endif

	PinvokeImport *table = (PinvokeImport*)handle;
	for (int i = 0; table [i].name; ++i) {
		if (!strcmp (table [i].name, name))
			return table [i].func;
	}
	return NULL;
}

#ifdef ENABLE_NETCORE
/* Missing System.Native symbols */
int SystemNative_CloseNetworkChangeListenerSocket (int a) { return 0; }
int SystemNative_CreateNetworkChangeListenerSocket (int a) { return 0; }
void SystemNative_ReadEvents (int a,int b) {}
int SystemNative_SchedGetAffinity (int a,int b) { return 0; }
int SystemNative_SchedSetAffinity (int a,int b) { return 0; }
#endif

#if !defined(ENABLE_AOT) || defined(EE_MODE_LLVMONLY_INTERP)
#define NEED_INTERP 1
#ifndef LINK_ICALLS
// FIXME: llvm+interp mode needs this to call icalls
#define NEED_NORMAL_ICALL_TABLES 1
#endif
#endif

#ifdef LINK_ICALLS

#include "icall-table.h"

static int
compare_int (const void *k1, const void *k2)
{
	return *(int*)k1 - *(int*)k2;
}

static void*
icall_table_lookup (MonoMethod *method, char *classname, char *methodname, char *sigstart, int32_t *uses_handles)
{
	uint32_t token = mono_method_get_token (method);
	assert (token);
	assert ((token & MONO_TOKEN_METHOD_DEF) == MONO_TOKEN_METHOD_DEF);
	uint32_t token_idx = token - MONO_TOKEN_METHOD_DEF;

	int *indexes = NULL;
	int indexes_size = 0;
	uint8_t *handles = NULL;
	void **funcs = NULL;

	*uses_handles = 0;

	const char *image_name = mono_image_get_name (mono_class_get_image (mono_method_get_class (method)));

#ifdef ICALL_TABLE_mscorlib
	if (!strcmp (image_name, "mscorlib") || !strcmp (image_name, "System.Private.CoreLib")) {
		indexes = mscorlib_icall_indexes;
		indexes_size = sizeof (mscorlib_icall_indexes) / 4;
		handles = mscorlib_icall_handles;
		funcs = mscorlib_icall_funcs;
		assert (sizeof (mscorlib_icall_indexes [0]) == 4);
	}
#ifdef ICALL_TABLE_System
	if (!strcmp (image_name, "System")) {
		indexes = System_icall_indexes;
		indexes_size = sizeof (System_icall_indexes) / 4;
		handles = System_icall_handles;
		funcs = System_icall_funcs;
	}
#endif
	assert (indexes);

	void *p = bsearch (&token_idx, indexes, indexes_size, 4, compare_int);
	if (!p) {
		return NULL;
		printf ("wasm: Unable to lookup icall: %s\n", mono_method_get_name (method));
		exit (1);
	}

	uint32_t idx = (int*)p - indexes;
	*uses_handles = handles [idx];

	//printf ("ICALL: %s %x %d %d\n", methodname, token, idx, (int)(funcs [idx]));

	return funcs [idx];
#endif
}

static const char*
icall_table_lookup_symbol (void *func)
{
	assert (0);
	return NULL;
}

#endif

void mono_initialize_internals ()
{
	mono_add_internal_call ("WebAssembly.Runtime::InvokeJS", mono_wasm_invoke_js);
	// TODO: what happens when two types in different assemblies have the same FQN?

	// Blazor specific custom routines - see dotnet_support.js for backing code
	mono_add_internal_call ("WebAssembly.JSInterop.InternalCalls::InvokeJSMarshalled", mono_wasm_invoke_js_marshalled);
	mono_add_internal_call ("WebAssembly.JSInterop.InternalCalls::InvokeJSUnmarshalled", mono_wasm_invoke_js_unmarshalled);

#ifdef CORE_BINDINGS
	core_initialize_internals();
#endif

}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_load_runtime (const char *managed_path, int enable_debugging)
{
	const char *interp_opts = "";

	monoeg_g_setenv ("MONO_LOG_LEVEL", "debug", 0);
	monoeg_g_setenv ("MONO_LOG_MASK", "gc", 0);
#ifdef ENABLE_NETCORE
	monoeg_g_setenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1", 0);
#endif

	mini_parse_debug_option ("top-runtime-invoke-unhandled");

	mono_dl_fallback_register (wasm_dl_load, wasm_dl_symbol, NULL, NULL);

#ifdef ENABLE_AOT
	// Defined in driver-gen.c
	register_aot_modules ();
#ifdef EE_MODE_LLVMONLY_INTERP
	mono_jit_set_aot_mode (MONO_AOT_MODE_LLVMONLY_INTERP);
#else
	mono_jit_set_aot_mode (MONO_AOT_MODE_LLVMONLY);
#endif
#else
	mono_jit_set_aot_mode (MONO_AOT_MODE_INTERP_LLVMONLY);
	if (enable_debugging) {
		// Disable optimizations which interfere with debugging
		interp_opts = "-all";
		mono_wasm_enable_debugging (enable_debugging);
	}
#endif

#ifdef LINK_ICALLS
	/* Link in our own linked icall table */
	static const MonoIcallTableCallbacks mono_icall_table_callbacks =
	{
		MONO_ICALL_TABLE_CALLBACKS_VERSION,
		icall_table_lookup,
		icall_table_lookup_symbol
	};
	mono_install_icall_table_callbacks (&mono_icall_table_callbacks);
#endif

#ifdef NEED_NORMAL_ICALL_TABLES
	mono_icall_table_init ();
#endif
#ifdef NEED_INTERP
	mono_ee_interp_init (interp_opts);
	mono_marshal_ilgen_init ();
	mono_method_builder_ilgen_init ();
	mono_sgen_mono_ilgen_init ();
#endif

	if (assembly_count) {
		MonoBundledAssembly **bundle_array = g_new0 (MonoBundledAssembly*, assembly_count + 1);
		WasmAssembly *cur = assemblies;
		int i = 0;
		while (cur) {
			bundle_array [i] = &cur->assembly;
			cur = cur->next;
			++i;
		}
		mono_register_bundled_assemblies ((const MonoBundledAssembly **)bundle_array);
	}

	mono_trace_init ();
	mono_trace_set_log_handler (wasm_logger, NULL);
	root_domain = mono_jit_init_version ("mono", "v4.0.30319");

	mono_initialize_internals();

	mono_thread_set_main (mono_thread_current ());
}

EMSCRIPTEN_KEEPALIVE MonoAssembly*
mono_wasm_assembly_load (const char *name)
{
	MonoImageOpenStatus status;
	MonoAssemblyName* aname = mono_assembly_name_new (name);
	if (!name)
		return NULL;

	MonoAssembly *res = mono_assembly_load (aname, NULL, &status);
	mono_assembly_name_free (aname);

	return res;
}

EMSCRIPTEN_KEEPALIVE MonoClass*
mono_wasm_assembly_find_class (MonoAssembly *assembly, const char *namespace, const char *name)
{
	return mono_class_from_name (mono_assembly_get_image (assembly), namespace, name);
}

EMSCRIPTEN_KEEPALIVE MonoMethod*
mono_wasm_assembly_find_method (MonoClass *klass, const char *name, int arguments)
{
	return mono_class_get_method_from_name (klass, name, arguments);
}

EMSCRIPTEN_KEEPALIVE MonoObject*
mono_wasm_invoke_method (MonoMethod *method, MonoObject *this_arg, void *params[], MonoObject **out_exc)
{
	MonoObject *exc = NULL;
	MonoObject *res;

	if (out_exc)
		*out_exc = NULL;
	res = mono_runtime_invoke (method, this_arg, params, &exc);
	if (exc) {
		if (out_exc)
			*out_exc = exc;

		MonoObject *exc2 = NULL;
		res = (MonoObject*)mono_object_to_string (exc, &exc2);
		if (exc2)
			res = (MonoObject*) mono_string_new (root_domain, "Exception Double Fault");
		return res;
	}

	MonoMethodSignature *sig = mono_method_signature (method);
	MonoType *type = mono_signature_get_return_type (sig);
	// If the method return type is void return null
	// This gets around a memory access crash when the result return a value when
	// a void method is invoked.
	if (mono_type_get_type (type) == MONO_TYPE_VOID)
		return NULL;

	return res;
}

EMSCRIPTEN_KEEPALIVE MonoMethod*
mono_wasm_assembly_get_entry_point (MonoAssembly *assembly)
{
	MonoImage *image;
	MonoMethod *method;

	image = mono_assembly_get_image (assembly);
	uint32_t entry = mono_image_get_entry_point (image);
	if (!entry)
		return NULL;

	return mono_get_method (image, entry, NULL);
}

EMSCRIPTEN_KEEPALIVE char *
mono_wasm_string_get_utf8 (MonoString *str)
{
	return mono_string_to_utf8 (str); //XXX JS is responsible for freeing this
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_string_convert (MonoString *str)
{
	if (str == NULL)
		return;

	mono_unichar2 *native_val = mono_string_chars (str);
	int native_len = mono_string_length (str) * 2;

	EM_ASM ({
		MONO.string_decoder.decode($0, $0 + $1, true);
	}, (int)native_val, native_len);
}

EMSCRIPTEN_KEEPALIVE MonoString *
mono_wasm_string_from_js (const char *str)
{
	if (str)
		return mono_string_new (root_domain, str);
	else
		return NULL;
}

static int
class_is_task (MonoClass *klass)
{
	if (!strcmp ("System.Threading.Tasks", mono_class_get_namespace (klass)) &&
		(!strcmp ("Task", mono_class_get_name (klass)) || !strcmp ("Task`1", mono_class_get_name (klass))))
		return 1;

	return 0;
}

MonoClass* mono_get_uri_class(MonoException** exc)
{
	MonoAssembly* assembly = mono_wasm_assembly_load ("System");
	if (!assembly)
		return NULL;
	MonoClass* klass = mono_wasm_assembly_find_class(assembly, "System", "Uri");
	return klass;
}

#define MARSHAL_TYPE_INT 1
#define MARSHAL_TYPE_FP 2
#define MARSHAL_TYPE_STRING 3
#define MARSHAL_TYPE_VT 4
#define MARSHAL_TYPE_DELEGATE 5
#define MARSHAL_TYPE_TASK 6
#define MARSHAL_TYPE_OBJECT 7
#define MARSHAL_TYPE_BOOL 8
#define MARSHAL_TYPE_ENUM 9
#define MARSHAL_TYPE_DATE 20
#define MARSHAL_TYPE_DATEOFFSET 21
#define MARSHAL_TYPE_URI 22

// typed array marshalling
#define MARSHAL_ARRAY_BYTE 11
#define MARSHAL_ARRAY_UBYTE 12
#define MARSHAL_ARRAY_SHORT 13
#define MARSHAL_ARRAY_USHORT 14
#define MARSHAL_ARRAY_INT 15
#define MARSHAL_ARRAY_UINT 16
#define MARSHAL_ARRAY_FLOAT 17
#define MARSHAL_ARRAY_DOUBLE 18

EMSCRIPTEN_KEEPALIVE int
mono_wasm_get_obj_type (MonoObject *obj)
{
	if (!obj)
		return 0;

	if (!datetime_class)
		datetime_class = mono_class_from_name (mono_get_corlib(), "System", "DateTime");
	if (!datetimeoffset_class)
		datetimeoffset_class = mono_class_from_name (mono_get_corlib(), "System", "DateTimeOffset");
	if (!uri_class) {
		MonoException** exc = NULL;
		uri_class = mono_get_uri_class(exc);
	}

	MonoClass *klass = mono_object_get_class (obj);
	MonoType *type = mono_class_get_type (klass);

	switch (mono_type_get_type (type)) {
	// case MONO_TYPE_CHAR: prob should be done not as a number?
	case MONO_TYPE_BOOLEAN:
		return MARSHAL_TYPE_BOOL;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_I:	// IntPtr
		return MARSHAL_TYPE_INT;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return MARSHAL_TYPE_FP;
	case MONO_TYPE_STRING:
		return MARSHAL_TYPE_STRING;
	case MONO_TYPE_SZARRAY:  { // simple zero based one-dim-array
		MonoClass *eklass = mono_class_get_element_class(klass);
		MonoType *etype = mono_class_get_type (eklass);

		switch (mono_type_get_type (etype)) {
			case MONO_TYPE_U1:
				return MARSHAL_ARRAY_UBYTE;
			case MONO_TYPE_I1:
				return MARSHAL_ARRAY_BYTE;
			case MONO_TYPE_U2:
				return MARSHAL_ARRAY_USHORT;
			case MONO_TYPE_I2:
				return MARSHAL_ARRAY_SHORT;
			case MONO_TYPE_U4:
				return MARSHAL_ARRAY_UINT;
			case MONO_TYPE_I4:
				return MARSHAL_ARRAY_INT;
			case MONO_TYPE_R4:
				return MARSHAL_ARRAY_FLOAT;
			case MONO_TYPE_R8:
				return MARSHAL_ARRAY_DOUBLE;
			default:
				return MARSHAL_TYPE_OBJECT;
		}
	}
	default:
		if (klass == datetime_class)
			return MARSHAL_TYPE_DATE;
		if (klass == datetimeoffset_class)
			return MARSHAL_TYPE_DATEOFFSET;
		if (uri_class && mono_class_is_assignable_from(uri_class, klass))
			return MARSHAL_TYPE_URI;
		if (mono_class_is_enum (klass))
			return MARSHAL_TYPE_ENUM;
		if (!mono_type_is_reference (type)) //vt
			return MARSHAL_TYPE_VT;
		if (mono_class_is_delegate (klass))
			return MARSHAL_TYPE_DELEGATE;
		if (class_is_task(klass))
			return MARSHAL_TYPE_TASK;

		return MARSHAL_TYPE_OBJECT;
	}
}

EMSCRIPTEN_KEEPALIVE int
mono_unbox_int (MonoObject *obj)
{
	if (!obj)
		return 0;
	MonoType *type = mono_class_get_type (mono_object_get_class(obj));

	void *ptr = mono_object_unbox (obj);
	switch (mono_type_get_type (type)) {
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
		return *(signed char*)ptr;
	case MONO_TYPE_U1:
		return *(unsigned char*)ptr;
	case MONO_TYPE_I2:
		return *(short*)ptr;
	case MONO_TYPE_U2:
		return *(unsigned short*)ptr;
	case MONO_TYPE_I4:
	case MONO_TYPE_I:
		return *(int*)ptr;
	case MONO_TYPE_U4:
		return *(unsigned int*)ptr;
	// WASM doesn't support returning longs to JS
	// case MONO_TYPE_I8:
	// case MONO_TYPE_U8:
	default:
		printf ("Invalid type %d to mono_unbox_int\n", mono_type_get_type (type));
		return 0;
	}
}

EMSCRIPTEN_KEEPALIVE double
mono_wasm_unbox_float (MonoObject *obj)
{
	if (!obj)
		return 0;
	MonoType *type = mono_class_get_type (mono_object_get_class(obj));

	void *ptr = mono_object_unbox (obj);
	switch (mono_type_get_type (type)) {
	case MONO_TYPE_R4:
		return *(float*)ptr;
	case MONO_TYPE_R8:
		return *(double*)ptr;
	default:
		printf ("Invalid type %d to mono_wasm_unbox_float\n", mono_type_get_type (type));
		return 0;
	}
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_array_length (MonoArray *array)
{
	return mono_array_length (array);
}

EMSCRIPTEN_KEEPALIVE MonoObject*
mono_wasm_array_get (MonoArray *array, int idx)
{
	return mono_array_get (array, MonoObject*, idx);
}

EMSCRIPTEN_KEEPALIVE MonoArray*
mono_wasm_obj_array_new (int size)
{
	return mono_array_new (root_domain, mono_get_object_class (), size);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_obj_array_set (MonoArray *array, int idx, MonoObject *obj)
{
	mono_array_setref (array, idx, obj);
}

EMSCRIPTEN_KEEPALIVE MonoArray*
mono_wasm_string_array_new (int size)
{
	return mono_array_new (root_domain, mono_get_string_class (), size);
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exec_regression (int verbose_level, char *image)
{
	return mono_regression_test_step (verbose_level, image, NULL) ? 0 : 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exit (int exit_code)
{
	exit (exit_code);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_set_main_args (int argc, char* argv[])
{
	mono_runtime_set_main_args (argc, argv);
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_strdup (const char *s)
{
	return (int)strdup (s);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_parse_runtime_options (int argc, char* argv[])
{
	mono_jit_parse_options (argc, argv);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_enable_on_demand_gc (void)
{
	mono_wasm_enable_gc = 1;
}

// Returns the local timezone default is UTC.
EM_JS(size_t, mono_wasm_timezone_get_local_name, (),
{
	var res = "UTC";
	try {
		res = Intl.DateTimeFormat().resolvedOptions().timeZone;
	} catch(e) {}

	var buff = Module._malloc((res.length + 1) * 2);
	stringToUTF16 (res, buff, (res.length + 1) * 2);
	return buff;
})

void
mono_timezone_get_local_name (MonoString **result)
{
	// WASM returns back an int pointer to a string UTF16 buffer.
	// We then cast to `mono_unichar2*`.  Returning `mono_unichar2*` from the JavaScript call will
	// result in cast warnings from the compiler.
	mono_unichar2 *tzd_local_name = (mono_unichar2*)mono_wasm_timezone_get_local_name ();
	*result = mono_string_from_utf16 (tzd_local_name);
	free (tzd_local_name);
}
