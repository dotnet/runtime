// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <emscripten.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <math.h>
#include <dlfcn.h>
#include <sys/stat.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/object.h>
#include <mono/metadata/debug-helpers.h>
// FIXME: unavailable in emscripten
// #include <mono/metadata/gc-internals.h>

#include <mono/metadata/mono-private-unstable.h>

#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-dl-fallback.h>
#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>

#include "wasm-config.h"
#include "pinvoke.h"

#ifdef GEN_PINVOKE
#include "wasm_m2n_invoke.g.h"
#endif
#include "gc-common.h"

#ifdef CORE_BINDINGS
void core_initialize_internals ();
#endif

extern MonoString* mono_wasm_invoke_js (MonoString *str, int *is_exception);
extern void mono_wasm_set_entrypoint_breakpoint (const char* assembly_name, int method_token);

// Blazor specific custom routines - see dotnet_support.js for backing code
extern void* mono_wasm_invoke_js_blazor (MonoString **exceptionMessage, void *callInfo, void* arg0, void* arg1, void* arg2);

void mono_wasm_enable_debugging (int);

static int _marshal_type_from_mono_type (int mono_type, MonoClass *klass, MonoType *type);

int mono_wasm_register_root (char *start, size_t size, const char *name);
void mono_wasm_deregister_root (char *addr);

void mono_ee_interp_init (const char *opts);
void mono_marshal_lightweight_init (void);
void mono_marshal_ilgen_init (void);
void mono_method_builder_ilgen_init (void);
void mono_sgen_mono_ilgen_init (void);
void mono_icall_table_init (void);
void mono_aot_register_module (void **aot_info);
char *monoeg_g_getenv(const char *variable);
int monoeg_g_setenv(const char *variable, const char *value, int overwrite);
int32_t monoeg_g_hasenv(const char *variable);
void mono_free (void*);
int32_t mini_parse_debug_option (const char *option);
char *mono_method_get_full_name (MonoMethod *method);

static void mono_wasm_init_finalizer_thread (void);

#define MARSHAL_TYPE_NULL 0
#define MARSHAL_TYPE_INT 1
#define MARSHAL_TYPE_FP64 2
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
#define MARSHAL_TYPE_SAFEHANDLE 23

// typed array marshaling
#define MARSHAL_ARRAY_BYTE 10
#define MARSHAL_ARRAY_UBYTE 11
#define MARSHAL_ARRAY_UBYTE_C 12
#define MARSHAL_ARRAY_SHORT 13
#define MARSHAL_ARRAY_USHORT 14
#define MARSHAL_ARRAY_INT 15
#define MARSHAL_ARRAY_UINT 16
#define MARSHAL_ARRAY_FLOAT 17
#define MARSHAL_ARRAY_DOUBLE 18

#define MARSHAL_TYPE_FP32 24
#define MARSHAL_TYPE_UINT32 25
#define MARSHAL_TYPE_INT64 26
#define MARSHAL_TYPE_UINT64 27
#define MARSHAL_TYPE_CHAR 28
#define MARSHAL_TYPE_STRING_INTERNED 29
#define MARSHAL_TYPE_VOID 30
#define MARSHAL_TYPE_POINTER 32

// errors
#define MARSHAL_ERROR_BUFFER_TOO_SMALL 512
#define MARSHAL_ERROR_NULL_CLASS_POINTER 513
#define MARSHAL_ERROR_NULL_TYPE_POINTER 514

static MonoClass* datetime_class;
static MonoClass* datetimeoffset_class;
static MonoClass* uri_class;
static MonoClass* task_class;
static MonoClass* safehandle_class;
static MonoClass* voidtaskresult_class;

static int resolved_datetime_class = 0,
	resolved_datetimeoffset_class = 0,
	resolved_uri_class = 0,
	resolved_task_class = 0,
	resolved_safehandle_class = 0,
	resolved_voidtaskresult_class = 0;

int mono_wasm_enable_gc = 1;

/* Not part of public headers */
#define MONO_ICALL_TABLE_CALLBACKS_VERSION 2

typedef struct {
	int version;
	void* (*lookup) (MonoMethod *method, char *classname, char *methodname, char *sigstart, int32_t *uses_handles);
	const char* (*lookup_icall_symbol) (void* func);
} MonoIcallTableCallbacks;

int
mono_string_instance_is_interned (MonoString *str_raw);

void
mono_install_icall_table_callbacks (const MonoIcallTableCallbacks *cb);

int mono_regression_test_step (int verbose_level, char *image, char *method_name);
void mono_trace_init (void);

#define g_new(type, size)  ((type *) malloc (sizeof (type) * (size)))
#define g_new0(type, size) ((type *) calloc (sizeof (type), (size)))

static MonoDomain *root_domain;

#define RUNTIMECONFIG_BIN_FILE "runtimeconfig.bin"

extern void mono_wasm_trace_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data);

static void
wasm_trace_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data)
{
	mono_wasm_trace_logger(log_domain, log_level, message, fatal, user_data);
	if (fatal)
		exit (1);
}

typedef uint32_t target_mword;
typedef target_mword SgenDescriptor;
typedef SgenDescriptor MonoGCDescriptor;
MONO_API int   mono_gc_register_root (char *start, size_t size, MonoGCDescriptor descr, MonoGCRootSource source, void *key, const char *msg);
void  mono_gc_deregister_root (char* addr);

EMSCRIPTEN_KEEPALIVE int
mono_wasm_register_root (char *start, size_t size, const char *name)
{
	int result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_gc_register_root (start, size, (MonoGCDescriptor)NULL, MONO_ROOT_SOURCE_EXTERNAL, NULL, name ? name : "mono_wasm_register_root");
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_deregister_root (char *addr)
{
	MONO_ENTER_GC_UNSAFE;
	mono_gc_deregister_root (addr);
	MONO_EXIT_GC_UNSAFE;
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

EMSCRIPTEN_KEEPALIVE int
mono_wasm_add_assembly (const char *name, const unsigned char *data, unsigned int size)
{
	int len = strlen (name);
	if (!strcasecmp (".pdb", &name [len - 4])) {
		char *new_name = strdup (name);
		//FIXME handle debugging assemblies with .exe extension
		strcpy (&new_name [len - 3], "dll");
		mono_register_symfile_for_assembly (new_name, data, size);
		return 1;
	}
	WasmAssembly *entry = g_new0 (WasmAssembly, 1);
	entry->assembly.name = strdup (name);
	entry->assembly.data = data;
	entry->assembly.size = size;
	entry->next = assemblies;
	assemblies = entry;
	++assembly_count;
	return mono_has_pdb_checksum ((char*)data, size);
}

int
mono_wasm_assembly_already_added (const char *assembly_name)
{
	if (assembly_count == 0)
		return 0;

	WasmAssembly *entry = assemblies;
	while (entry != NULL) {
		int entry_name_minus_extn_len = strlen(entry->assembly.name) - 4;
		if (entry_name_minus_extn_len == strlen(assembly_name) && strncmp (entry->assembly.name, assembly_name, entry_name_minus_extn_len) == 0)
			return 1;
		entry = entry->next;
	}

	return 0;
}

const unsigned char *
mono_wasm_get_assembly_bytes (const char *assembly_name, unsigned int *size)
{
	if (assembly_count == 0)
		return 0;

	WasmAssembly *entry = assemblies;
	while (entry != NULL) {
		if (strcmp (entry->assembly.name, assembly_name) == 0)
		{
			*size = entry->assembly.size;
			return entry->assembly.data;
		}
		entry = entry->next;
	}
	return NULL;
}

typedef struct WasmSatelliteAssembly_ WasmSatelliteAssembly;

struct WasmSatelliteAssembly_ {
	MonoBundledSatelliteAssembly *assembly;
	WasmSatelliteAssembly *next;
};

static WasmSatelliteAssembly *satellite_assemblies;
static int satellite_assembly_count;

EMSCRIPTEN_KEEPALIVE void
mono_wasm_add_satellite_assembly (const char *name, const char *culture, const unsigned char *data, unsigned int size)
{
	WasmSatelliteAssembly *entry = g_new0 (WasmSatelliteAssembly, 1);
	entry->assembly = mono_create_new_bundled_satellite_assembly (name, culture, data, size);
	entry->next = satellite_assemblies;
	satellite_assemblies = entry;
	++satellite_assembly_count;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_setenv (const char *name, const char *value)
{
	monoeg_g_setenv (strdup (name), strdup (value), 1);
}

static void *sysglobal_native_handle;

static void*
wasm_dl_load (const char *name, int flags, char **err, void *user_data)
{
	void* handle = wasm_dl_lookup_pinvoke_table (name);
	if (handle)
		return handle;

	if (!strcmp (name, "System.Globalization.Native"))
		return sysglobal_native_handle;

#if WASM_SUPPORTS_DLOPEN
	return dlopen(name, flags);
#endif

	return NULL;
}

static void*
wasm_dl_symbol (void *handle, const char *name, char **err, void *user_data)
{
	if (handle == sysglobal_native_handle)
		assert (0);

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

#if defined(ICALL_TABLE_mscorlib)
	if (!strcmp (image_name, "mscorlib")) {
		indexes = mscorlib_icall_indexes;
		indexes_size = sizeof (mscorlib_icall_indexes) / 4;
		handles = mscorlib_icall_handles;
		funcs = mscorlib_icall_funcs;
		assert (sizeof (mscorlib_icall_indexes [0]) == 4);
	}
#endif
#if defined(ICALL_TABLE_corlib)
	if (!strcmp (image_name, "System.Private.CoreLib")) {
		indexes = corlib_icall_indexes;
		indexes_size = sizeof (corlib_icall_indexes) / 4;
		handles = corlib_icall_handles;
		funcs = corlib_icall_funcs;
		assert (sizeof (corlib_icall_indexes [0]) == 4);
	}
#endif
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
}

static const char*
icall_table_lookup_symbol (void *func)
{
	assert (0);
	return NULL;
}

#endif

/*
 * get_native_to_interp:
 *
 *   Return a pointer to a wasm function which can be used to enter the interpreter to
 * execute METHOD from native code.
 * EXTRA_ARG is the argument passed to the interp entry functions in the runtime.
 */
void*
get_native_to_interp (MonoMethod *method, void *extra_arg)
{
	void *addr;

	MONO_ENTER_GC_UNSAFE;
	MonoClass *klass = mono_method_get_class (method);
	MonoImage *image = mono_class_get_image (klass);
	MonoAssembly *assembly = mono_image_get_assembly (image);
	MonoAssemblyName *aname = mono_assembly_get_name (assembly);
	const char *name = mono_assembly_name_get_name (aname);
	const char *class_name = mono_class_get_name (klass);
	const char *method_name = mono_method_get_name (method);
	char key [128];
	int len;

	assert (strlen (name) < 100);
	snprintf (key, sizeof(key), "%s_%s_%s", name, class_name, method_name);
	len = strlen (key);
	for (int i = 0; i < len; ++i) {
		if (key [i] == '.')
			key [i] = '_';
	}

	addr = wasm_dl_get_native_to_interp (key, extra_arg);
	MONO_EXIT_GC_UNSAFE;
	return addr;
}

void mono_initialize_internals ()
{
	mono_add_internal_call ("Interop/Runtime::InvokeJS", mono_wasm_invoke_js);
	// TODO: what happens when two types in different assemblies have the same FQN?

	// Blazor specific custom routines - see dotnet_support.js for backing code
	mono_add_internal_call ("WebAssembly.JSInterop.InternalCalls::InvokeJS", mono_wasm_invoke_js_blazor);

#ifdef CORE_BINDINGS
	core_initialize_internals();
#endif

}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_register_bundled_satellite_assemblies ()
{
	/* In legacy satellite_assembly_count is always false */
	if (satellite_assembly_count) {
		MonoBundledSatelliteAssembly **satellite_bundle_array =  g_new0 (MonoBundledSatelliteAssembly *, satellite_assembly_count + 1);
		WasmSatelliteAssembly *cur = satellite_assemblies;
		int i = 0;
		while (cur) {
			satellite_bundle_array [i] = cur->assembly;
			cur = cur->next;
			++i;
		}
		mono_register_bundled_satellite_assemblies ((const MonoBundledSatelliteAssembly **)satellite_bundle_array);
	}
}

void mono_wasm_link_icu_shim (void);

void
cleanup_runtime_config (MonovmRuntimeConfigArguments *args, void *user_data)
{
	free (args);
	free (user_data);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_load_runtime (const char *unused, int debug_level)
{
	const char *interp_opts = "";

#ifndef INVARIANT_GLOBALIZATION
	mono_wasm_link_icu_shim ();
#endif

	// We should enable this as part of the wasm build later
#ifndef DISABLE_THREADS
	monoeg_g_setenv ("MONO_THREADS_SUSPEND", "coop", 0);
	monoeg_g_setenv ("MONO_SLEEP_ABORT_LIMIT", "5000", 0);
#endif

#ifdef DEBUG
	// monoeg_g_setenv ("MONO_LOG_LEVEL", "debug", 0);
	// monoeg_g_setenv ("MONO_LOG_MASK", "gc", 0);
    // Setting this env var allows Diagnostic.Debug to write to stderr.  In a browser environment this
    // output will be sent to the console.  Right now this is the only way to emit debug logging from
    // corlib assemblies.
	// monoeg_g_setenv ("COMPlus_DebugWriteToStdErr", "1", 0);
#endif
	// When the list of app context properties changes, please update RuntimeConfigReservedProperties for
	// target _WasmGenerateRuntimeConfig in WasmApp.targets file
	const char *appctx_keys[2];
	appctx_keys [0] = "APP_CONTEXT_BASE_DIRECTORY";
	appctx_keys [1] = "RUNTIME_IDENTIFIER";

	const char *appctx_values[2];
	appctx_values [0] = "/";
	appctx_values [1] = "browser-wasm";

	char *file_name = RUNTIMECONFIG_BIN_FILE;
	int str_len = strlen (file_name) + 1; // +1 is for the "/"
	char *file_path = (char *)malloc (sizeof (char) * (str_len +1)); // +1 is for the terminating null character
	int num_char = snprintf (file_path, (str_len + 1), "/%s", file_name);
	struct stat buffer;

	assert (num_char > 0 && num_char == str_len);

	if (stat (file_path, &buffer) == 0) {
		MonovmRuntimeConfigArguments *arg = (MonovmRuntimeConfigArguments *)malloc (sizeof (MonovmRuntimeConfigArguments));
		arg->kind = 0;
		arg->runtimeconfig.name.path = file_path;
		monovm_runtimeconfig_initialize (arg, cleanup_runtime_config, file_path);
	} else {
		free (file_path);
	}

	monovm_initialize (2, appctx_keys, appctx_values);

	mini_parse_debug_option ("top-runtime-invoke-unhandled");

	mono_dl_fallback_register (wasm_dl_load, wasm_dl_symbol, NULL, NULL);
	mono_wasm_install_get_native_to_interp_tramp (get_native_to_interp);
	
#ifdef GEN_PINVOKE
	mono_wasm_install_interp_to_native_callback (mono_wasm_interp_to_native_callback);
#endif

#ifdef ENABLE_AOT
	monoeg_g_setenv ("MONO_AOT_MODE", "aot", 1);

	// Defined in driver-gen.c
	register_aot_modules ();
#ifdef EE_MODE_LLVMONLY_INTERP
	mono_jit_set_aot_mode (MONO_AOT_MODE_LLVMONLY_INTERP);
#else
	mono_jit_set_aot_mode (MONO_AOT_MODE_LLVMONLY);
#endif
#else
	mono_jit_set_aot_mode (MONO_AOT_MODE_INTERP_ONLY);

	/*
	 * debug_level > 0 enables debugging and sets the debug log level to debug_level
	 * debug_level == 0 disables debugging and enables interpreter optimizations
	 * debug_level < 0 enabled debugging and disables debug logging.
	 *
	 * Note: when debugging is enabled interpreter optimizations are disabled.
	 */
	if (debug_level) {
		// Disable optimizations which interfere with debugging
		interp_opts = "-all";
		mono_wasm_enable_debugging (debug_level);
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
	mono_marshal_lightweight_init ();
	mono_marshal_ilgen_init();
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

	mono_wasm_register_bundled_satellite_assemblies ();
	mono_trace_init ();
	mono_trace_set_log_handler (wasm_trace_logger, NULL);
	root_domain = mono_jit_init_version ("mono", NULL);

	mono_initialize_internals();

	mono_thread_set_main (mono_thread_current ());

	// TODO: we can probably delay starting the finalizer thread even longer - maybe from JS
	// once we're done with loading and are about to begin running some managed code.
	mono_wasm_init_finalizer_thread ();
}

EMSCRIPTEN_KEEPALIVE MonoAssembly*
mono_wasm_assembly_load (const char *name)
{
	assert (name);
	MonoImageOpenStatus status;
	MonoAssemblyName* aname = mono_assembly_name_new (name);

	MonoAssembly *res = mono_assembly_load (aname, NULL, &status);
	mono_assembly_name_free (aname);

	return res;
}

EMSCRIPTEN_KEEPALIVE MonoAssembly*
mono_wasm_get_corlib ()
{
	MonoAssembly* result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_image_get_assembly (mono_get_corlib());
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE MonoClass*
mono_wasm_assembly_find_class (MonoAssembly *assembly, const char *namespace, const char *name)
{
	assert (assembly);
	MonoClass *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_from_name (mono_assembly_get_image (assembly), namespace, name);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE MonoMethod*
mono_wasm_assembly_find_method (MonoClass *klass, const char *name, int arguments)
{
	assert (klass);
	MonoMethod* result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_get_method_from_name (klass, name, arguments);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE MonoMethod*
mono_wasm_get_delegate_invoke_ref (MonoObject **delegate)
{
	MonoMethod * result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_get_delegate_invoke(mono_object_get_class (*delegate));
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_box_primitive_ref (MonoClass *klass, void *value, int value_size, PPVOLATILE(MonoObject) result)
{
	assert (klass);

	MONO_ENTER_GC_UNSAFE;
	MonoType *type = mono_class_get_type (klass);
	int alignment;

	if (mono_type_size (type, &alignment) <= value_size)
		// TODO: use mono_value_box_checked and propagate error out
		store_volatile(result, mono_value_box (root_domain, klass, value));

	MONO_EXIT_GC_UNSAFE;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_invoke_method_ref (MonoMethod *method, MonoObject **this_arg_in, void *params[], MonoObject **_out_exc, MonoObject **out_result)
{
	PPVOLATILE(MonoObject) out_exc = _out_exc;
	PVOLATILE(MonoObject) temp_exc = NULL;
	if (out_exc)
		*out_exc = NULL;
	else
		out_exc = &temp_exc;

	MONO_ENTER_GC_UNSAFE;
	if (out_result) {
		*out_result = NULL;
		PVOLATILE(MonoObject) invoke_result = mono_runtime_invoke (method, this_arg_in ? *this_arg_in : NULL, params, (MonoObject **)out_exc);
		store_volatile(out_result, invoke_result);
	} else {
		mono_runtime_invoke (method, this_arg_in ? *this_arg_in : NULL, params, (MonoObject **)out_exc);
	}

	if (*out_exc && out_result) {
		PVOLATILE(MonoObject) exc2 = NULL;
		store_volatile(out_result, (MonoObject*)mono_object_to_string (*out_exc, (MonoObject **)&exc2));
		if (exc2)
			store_volatile(out_result, (MonoObject*)mono_string_new (root_domain, "Exception Double Fault"));
	}
	MONO_EXIT_GC_UNSAFE;
}

// deprecated
MonoObject*
mono_wasm_invoke_method (MonoMethod *method, MonoObject *this_arg, void *params[], MonoObject **out_exc)
{
	PVOLATILE(MonoObject) result = NULL;
	mono_wasm_invoke_method_ref (method, &this_arg, params, out_exc, (MonoObject **)&result);

	if (result) {
		MONO_ENTER_GC_UNSAFE;
		MonoMethodSignature *sig = mono_method_signature (method);
		MonoType *type = mono_signature_get_return_type (sig);
		// If the method return type is void return null
		// This gets around a memory access crash when the result return a value when
		// a void method is invoked.
		if (mono_type_get_type (type) == MONO_TYPE_VOID)
			result = NULL;
		MONO_EXIT_GC_UNSAFE;
	}

	return result;
}

EMSCRIPTEN_KEEPALIVE MonoMethod*
mono_wasm_assembly_get_entry_point (MonoAssembly *assembly, int auto_insert_breakpoint)
{
	MonoImage *image;
	MonoMethod *method;

	MONO_ENTER_GC_UNSAFE;
	image = mono_assembly_get_image (assembly);
	uint32_t entry = mono_image_get_entry_point (image);
	if (!entry)
		goto end;

	mono_domain_ensure_entry_assembly (root_domain, assembly);
	method = mono_get_method (image, entry, NULL);

	/*
	 * If the entry point looks like a compiler generated wrapper around
	 * an async method in the form "<Name>" then try to look up the async methods
	 * "<Name>$" and "Name" it could be wrapping.  We do this because the generated
	 * sync wrapper will call task.GetAwaiter().GetResult() when we actually want
	 * to yield to the host runtime.
	 */
	if (mono_method_get_flags (method, NULL) & 0x0800 /* METHOD_ATTRIBUTE_SPECIAL_NAME */) {
		const char *name = mono_method_get_name (method);
		int name_length = strlen (name);

		if ((*name != '<') || (name [name_length - 1] != '>'))
			goto end;

		MonoClass *klass = mono_method_get_class (method);
		assert(klass);
		char *async_name = malloc (name_length + 2);
		snprintf (async_name, name_length + 2, "%s$", name);

		// look for "<Name>$"
		MonoMethodSignature *sig = mono_method_get_signature (method, image, mono_method_get_token (method));
		MonoMethod *async_method = mono_class_get_method_from_name (klass, async_name, mono_signature_get_param_count (sig));
		if (async_method != NULL) {
			free (async_name);
			method = async_method;
			goto end;
		}

		// look for "Name" by trimming the first and last character of "<Name>"
		async_name [name_length - 1] = '\0';
		async_method = mono_class_get_method_from_name (klass, async_name + 1, mono_signature_get_param_count (sig));

		free (async_name);
		if (async_method != NULL)
			method = async_method;
	}

	end:
	MONO_EXIT_GC_UNSAFE;
	if (auto_insert_breakpoint)
	{
		MonoAssemblyName *aname = mono_assembly_get_name (assembly);
		const char *name = mono_assembly_name_get_name (aname);
		if (name != NULL)
			mono_wasm_set_entrypoint_breakpoint(name, mono_method_get_token (method));
	}
	return method;
}

// TODO: ref
EMSCRIPTEN_KEEPALIVE char *
mono_wasm_string_get_utf8 (MonoString *str)
{
	char * result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_string_to_utf8 (str); //XXX JS is responsible for freeing this
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE MonoString *
mono_wasm_string_from_js (const char *str)
{
	PVOLATILE(MonoString) result = NULL;
	MONO_ENTER_GC_UNSAFE;
	if (str)
		result = mono_string_new (root_domain, str);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_string_from_utf16_ref (const mono_unichar2 * chars, int length, MonoString **result)
{
	assert (length >= 0);

	MONO_ENTER_GC_UNSAFE;
	if (chars) {
		mono_gc_wbarrier_generic_store_atomic(result, (MonoObject *)mono_string_new_utf16 (root_domain, chars, length));
	} else {
		mono_gc_wbarrier_generic_store_atomic(result, NULL);
	}
	MONO_EXIT_GC_UNSAFE;
}

static int
class_is_task (MonoClass *klass)
{
	if (!klass)
		return 0;

	int result;
	MONO_ENTER_GC_UNSAFE;
	if (!task_class && !resolved_task_class) {
		task_class = mono_class_from_name (mono_get_corlib(), "System.Threading.Tasks", "Task");
		resolved_task_class = 1;
	}

	result = task_class && (klass == task_class || mono_class_is_subclass_of(klass, task_class, 0));
	MONO_EXIT_GC_UNSAFE;
	return result;
}

static MonoClass*
_get_uri_class(MonoException** exc)
{
	MonoAssembly* assembly = mono_wasm_assembly_load ("System");
	if (!assembly)
		return NULL;
	MonoClass* klass = mono_wasm_assembly_find_class(assembly, "System", "Uri");
	return klass;
}

static void
_ensure_classes_resolved ()
{
	MONO_ENTER_GC_UNSAFE;
	if (!datetime_class && !resolved_datetime_class) {
		datetime_class = mono_class_from_name (mono_get_corlib(), "System", "DateTime");
		resolved_datetime_class = 1;
	}
	if (!datetimeoffset_class && !resolved_datetimeoffset_class) {
		datetimeoffset_class = mono_class_from_name (mono_get_corlib(), "System", "DateTimeOffset");
		resolved_datetimeoffset_class = 1;
	}
	if (!uri_class && !resolved_uri_class) {
		PVOLATILE(MonoException) exc = NULL;
		uri_class = _get_uri_class((MonoException **)&exc);
		resolved_uri_class = 1;
	}
	if (!safehandle_class && !resolved_safehandle_class) {
		safehandle_class = mono_class_from_name (mono_get_corlib(), "System.Runtime.InteropServices", "SafeHandle");
		resolved_safehandle_class = 1;
	}
	if (!voidtaskresult_class && !resolved_voidtaskresult_class) {
		voidtaskresult_class = mono_class_from_name (mono_get_corlib(), "System.Threading.Tasks", "VoidTaskResult");
		resolved_voidtaskresult_class = 1;
	}
	MONO_EXIT_GC_UNSAFE;
}

// This must be run inside a GC unsafe region
static int
_marshal_type_from_mono_type (int mono_type, MonoClass *klass, MonoType *type)
{
	switch (mono_type) {
	// case MONO_TYPE_CHAR: prob should be done not as a number?
	case MONO_TYPE_VOID:
		return MARSHAL_TYPE_VOID;
	case MONO_TYPE_BOOLEAN:
		return MARSHAL_TYPE_BOOL;
	case MONO_TYPE_I: // IntPtr
	case MONO_TYPE_U: // UIntPtr
	case MONO_TYPE_PTR:
		return MARSHAL_TYPE_POINTER;
	case MONO_TYPE_I1:
	case MONO_TYPE_I2:
	case MONO_TYPE_I4:
		return MARSHAL_TYPE_INT;
	case MONO_TYPE_CHAR:
		return MARSHAL_TYPE_CHAR;
	case MONO_TYPE_U1:
	case MONO_TYPE_U2:
	case MONO_TYPE_U4:  // The distinction between this and signed int is
						// important due to how numbers work in JavaScript
		return MARSHAL_TYPE_UINT32;
	case MONO_TYPE_I8:
		return MARSHAL_TYPE_INT64;
	case MONO_TYPE_U8:
		return MARSHAL_TYPE_UINT64;
	case MONO_TYPE_R4:
		return MARSHAL_TYPE_FP32;
	case MONO_TYPE_R8:
		return MARSHAL_TYPE_FP64;
	case MONO_TYPE_STRING:
		return MARSHAL_TYPE_STRING;
	case MONO_TYPE_SZARRAY:  { // simple zero based one-dim-array
		if (klass) {
		MonoClass *eklass = mono_class_get_element_class (klass);
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
		} else {
			return MARSHAL_TYPE_OBJECT;
		}
	}
	default:
		_ensure_classes_resolved ();

		if (klass) {
		if (klass == datetime_class)
			return MARSHAL_TYPE_DATE;
		if (klass == datetimeoffset_class)
			return MARSHAL_TYPE_DATEOFFSET;
		if (uri_class && mono_class_is_assignable_from(uri_class, klass))
			return MARSHAL_TYPE_URI;
		if (klass == voidtaskresult_class)
			return MARSHAL_TYPE_VOID;
		if (mono_class_is_enum (klass))
			return MARSHAL_TYPE_ENUM;
			if (type && !mono_type_is_reference (type)) //vt
			return MARSHAL_TYPE_VT;
		if (mono_class_is_delegate (klass))
			return MARSHAL_TYPE_DELEGATE;
		if (class_is_task(klass))
			return MARSHAL_TYPE_TASK;
			if (safehandle_class && (klass == safehandle_class || mono_class_is_subclass_of(klass, safehandle_class, 0)))
			return MARSHAL_TYPE_SAFEHANDLE;
		}

		return MARSHAL_TYPE_OBJECT;
	}
}

// FIXME: Ref
EMSCRIPTEN_KEEPALIVE MonoClass *
mono_wasm_get_obj_class (MonoObject *obj)
{
	if (!obj)
		return NULL;

	return mono_object_get_class (obj);
}

// This code runs inside a gc unsafe region
static int
_wasm_get_obj_type_ref_impl (PPVOLATILE(MonoObject) obj)
{
	if (!obj || !*obj)
		return 0;

	/* Process obj before calling into the runtime, class_from_name () can invoke managed code */
	MonoClass *klass = mono_object_get_class (*obj);
	if (!klass)
		return MARSHAL_ERROR_NULL_CLASS_POINTER;
	if ((klass == mono_get_string_class ()) &&
		mono_string_instance_is_interned ((MonoString *)*obj))
		return MARSHAL_TYPE_STRING_INTERNED;

	MonoType *type = mono_class_get_type (klass);
	if (!type)
		return MARSHAL_ERROR_NULL_TYPE_POINTER;

	int mono_type = mono_type_get_type (type);

	return _marshal_type_from_mono_type (mono_type, klass, type);
}

// FIXME: Ref
EMSCRIPTEN_KEEPALIVE int
mono_wasm_get_obj_type (MonoObject *obj)
{
	int result;
	MONO_ENTER_GC_UNSAFE;
	result = _wasm_get_obj_type_ref_impl(&obj);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

// This code runs inside a gc unsafe region
static int
_mono_wasm_try_unbox_primitive_and_get_type_ref_impl (PVOLATILE(MonoObject) obj, void *result, int result_capacity) {
	void **resultP = result;
	int *resultI = result;
	uint32_t *resultU = result;
	int64_t *resultL = result;
	float *resultF = result;
	double *resultD = result;

	/* Process obj before calling into the runtime, class_from_name () can invoke managed code */
	MonoClass *klass = mono_object_get_class (obj);
	if (!klass)
		return MARSHAL_ERROR_NULL_CLASS_POINTER;

	MonoType *type = mono_class_get_type (klass), *original_type = type;
	if (!type)
		return MARSHAL_ERROR_NULL_TYPE_POINTER;

	if ((klass == mono_get_string_class ()) &&
		mono_string_instance_is_interned ((MonoString *)obj)) {
		*resultL = 0;
		*resultP = type;
		return MARSHAL_TYPE_STRING_INTERNED;
	}

	if (mono_class_is_enum (klass))
		type = mono_type_get_underlying_type (type);

	if (!type)
		return MARSHAL_ERROR_NULL_TYPE_POINTER;

	int mono_type = mono_type_get_type (type);

	if (mono_type == MONO_TYPE_GENERICINST) {
		// HACK: While the 'any other type' fallback is valid for classes, it will do the
		//  wrong thing for structs, so we need to make sure the valuetype handler is used
		if (mono_type_generic_inst_is_valuetype (type))
			mono_type = MONO_TYPE_VALUETYPE;
	}

	// FIXME: We would prefer to unbox once here but it will fail if the value isn't unboxable

	switch (mono_type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			*resultI = *(signed char*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_U1:
			*resultU = *(unsigned char*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			*resultI = *(short*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_U2:
			*resultU = *(unsigned short*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_I:
			*resultI = *(int*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_U4:
			*resultU = *(uint32_t*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_R4:
			*resultF = *(float*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_R8:
			*resultD = *(double*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_PTR:
			*resultU = (uint32_t)(*(void**)mono_object_unbox (obj));
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			// FIXME: At present the javascript side of things can't handle this,
			//  but there's no reason not to future-proof this API
			*resultL = *(int64_t*)mono_object_unbox (obj);
			break;
		case MONO_TYPE_VALUETYPE:
			{
				int obj_size = mono_object_get_size (obj),
					required_size = (sizeof (int)) + (sizeof (MonoType *)) + obj_size;

				// Check whether this struct has special-case marshaling
				// FIXME: Do we need to null out obj before this?
				int marshal_type = _marshal_type_from_mono_type (mono_type, klass, original_type);
				if (marshal_type != MARSHAL_TYPE_VT)
					return marshal_type;

				// Check whether the result buffer is big enough for the struct and padding
				if (result_capacity < required_size)
					return MARSHAL_ERROR_BUFFER_TOO_SMALL;

				// Store a header before the struct data with the size of the data and its MonoType
				*resultP = type;
				int * resultSize = (int *)(resultP + 1);
				*resultSize = obj_size;
				void * resultVoid = (resultP + 2);
				void * unboxed = mono_object_unbox (obj);
				memcpy (resultVoid, unboxed, obj_size);
				return MARSHAL_TYPE_VT;
			}
			break;
		default:
			// If we failed to do a fast unboxing, return the original type information so
			//  that the caller can do a proper, slow unboxing later
			// HACK: Store the class pointer into the result buffer so our caller doesn't
			//  have to call back into the native runtime later to get it
			*resultP = type;
			int fallbackResultType = _marshal_type_from_mono_type (mono_type, klass, original_type);
			assert (fallbackResultType != MARSHAL_TYPE_VT);
			return fallbackResultType;
	}

	// We successfully performed a fast unboxing here so use the type information
	//  matching what we unboxed (i.e. an enum's underlying type instead of its type)
	int resultType = _marshal_type_from_mono_type (mono_type, klass, type);
	assert (resultType != MARSHAL_TYPE_VT);
	return resultType;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_try_unbox_primitive_and_get_type_ref (MonoObject **objRef, void *result, int result_capacity)
{
	if (!result)
		return MARSHAL_ERROR_BUFFER_TOO_SMALL;

	int retval;
	int *resultI = result;
	int64_t *resultL = result;

	if (result_capacity >= sizeof (int64_t))
		*resultL = 0;
	else if (result_capacity >= sizeof (int))
		*resultI = 0;

	if (result_capacity < 16)
		return MARSHAL_ERROR_BUFFER_TOO_SMALL;

	if (!objRef || !(*objRef))
		return MARSHAL_TYPE_NULL;

	MONO_ENTER_GC_UNSAFE;
	retval = _mono_wasm_try_unbox_primitive_and_get_type_ref_impl (*objRef, result, result_capacity);
	MONO_EXIT_GC_UNSAFE;
	return retval;
}

// FIXME: Ref
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

EMSCRIPTEN_KEEPALIVE void
mono_wasm_array_get_ref (MonoArray **array, int idx, MonoObject **result)
{
	MONO_ENTER_GC_UNSAFE;
	mono_gc_wbarrier_generic_store_atomic(result, mono_array_get (*array, MonoObject*, idx));
	MONO_EXIT_GC_UNSAFE;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_obj_array_new_ref (int size, MonoArray **result)
{
	MONO_ENTER_GC_UNSAFE;
	mono_gc_wbarrier_generic_store_atomic(result, (MonoObject *)mono_array_new (root_domain, mono_get_object_class (), size));
	MONO_EXIT_GC_UNSAFE;
}

// Deprecated
EMSCRIPTEN_KEEPALIVE MonoArray*
mono_wasm_obj_array_new (int size)
{
	PVOLATILE(MonoArray) result = NULL;
	mono_wasm_obj_array_new_ref(size, (MonoArray **)&result);
	return result;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_obj_array_set (MonoArray *array, int idx, MonoObject *obj)
{
	mono_array_setref (array, idx, obj);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_obj_array_set_ref (MonoArray **array, int idx, MonoObject **obj)
{
	MONO_ENTER_GC_UNSAFE;
	mono_array_setref (*array, idx, *obj);
	MONO_EXIT_GC_UNSAFE;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_string_array_new_ref (int size, MonoArray **result)
{
	MONO_ENTER_GC_UNSAFE;
	mono_gc_wbarrier_generic_store_atomic(result, (MonoObject *)mono_array_new (root_domain, mono_get_string_class (), size));
	MONO_EXIT_GC_UNSAFE;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exec_regression (int verbose_level, char *image)
{
	return mono_regression_test_step (verbose_level, image, NULL) ? 0 : 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exit (int exit_code)
{
	mono_jit_cleanup (root_domain);
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
mono_wasm_enable_on_demand_gc (int enable)
{
	mono_wasm_enable_gc = enable ? 1 : 0;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_intern_string_ref (MonoString **string)
{
	MONO_ENTER_GC_UNSAFE;
	mono_gc_wbarrier_generic_store_atomic(string, (MonoObject *)mono_string_intern (*string));
	MONO_EXIT_GC_UNSAFE;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_string_get_data_ref (
	MonoString **string, mono_unichar2 **outChars, int *outLengthBytes, int *outIsInterned
) {
	MONO_ENTER_GC_UNSAFE;
	if (!string || !(*string)) {
		if (outChars)
			*outChars = 0;
		if (outLengthBytes)
			*outLengthBytes = 0;
		if (outIsInterned)
			*outIsInterned = 1;
	} else {
		if (outChars)
			*outChars = mono_string_chars (*string);
		if (outLengthBytes)
			*outLengthBytes = mono_string_length (*string) * 2;
		if (outIsInterned)
			*outIsInterned = mono_string_instance_is_interned (*string);
	}
	MONO_EXIT_GC_UNSAFE;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_string_get_data (
	MonoString *string, mono_unichar2 **outChars, int *outLengthBytes, int *outIsInterned
) {
	mono_wasm_string_get_data_ref(&string, outChars, outLengthBytes, outIsInterned);
}

EMSCRIPTEN_KEEPALIVE MonoType *
mono_wasm_class_get_type (MonoClass *klass)
{
	if (!klass)
		return NULL;
	MonoType *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_get_type (klass);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE MonoClass *
mono_wasm_type_get_class (MonoType *type)
{
	if (!type)
		return NULL;
	MonoClass *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_type_get_class (type);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

EMSCRIPTEN_KEEPALIVE char *
mono_wasm_get_type_name (MonoType * typePtr) {
	return mono_type_get_name_full (typePtr, MONO_TYPE_NAME_FORMAT_REFLECTION);
}

EMSCRIPTEN_KEEPALIVE char *
mono_wasm_get_type_aqn (MonoType * typePtr) {
	return mono_type_get_name_full (typePtr, MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_write_managed_pointer_unsafe (PPVOLATILE(MonoObject) destination, PVOLATILE(MonoObject) source) {
	store_volatile(destination, source);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_copy_managed_pointer (PPVOLATILE(MonoObject) destination, PPVOLATILE(MonoObject) source) {
	copy_volatile(destination, source);
}

#ifdef ENABLE_AOT_PROFILER

void mono_profiler_init_aot (const char *desc);

EMSCRIPTEN_KEEPALIVE void
mono_wasm_load_profiler_aot (const char *desc)
{
	mono_profiler_init_aot (desc);
}

#endif

static void
mono_wasm_init_finalizer_thread (void)
{
	// At this time we don't use a dedicated thread for finalization even if threading is enabled.
	// Finalizers periodically run on the main thread
#if 0
	mono_gc_init_finalizer_thread ();
#endif
}

#define I52_ERROR_NONE 0
#define I52_ERROR_NON_INTEGRAL 1
#define I52_ERROR_OUT_OF_RANGE 2

#define U52_MAX_VALUE ((1ULL << 53) - 1)
#define I52_MAX_VALUE ((1LL << 53) - 1)
#define I52_MIN_VALUE -I52_MAX_VALUE

EMSCRIPTEN_KEEPALIVE double mono_wasm_i52_to_f64 (int64_t *source, int *error) {
	int64_t value = *source;

	if ((value < I52_MIN_VALUE) || (value > I52_MAX_VALUE)) {
		*error = I52_ERROR_OUT_OF_RANGE;
		return NAN;
	}

	*error = I52_ERROR_NONE;
	return (double)value;
}

EMSCRIPTEN_KEEPALIVE double mono_wasm_u52_to_f64 (uint64_t *source, int *error) {
	uint64_t value = *source;

	if (value > U52_MAX_VALUE) {
		*error = I52_ERROR_OUT_OF_RANGE;
		return NAN;
	}

	*error = I52_ERROR_NONE;
	return (double)value;
}

EMSCRIPTEN_KEEPALIVE int mono_wasm_f64_to_u52 (uint64_t *destination, double value) {
	if ((value < 0) || (value > U52_MAX_VALUE))
		return I52_ERROR_OUT_OF_RANGE;
	if (floor(value) != value)
		return I52_ERROR_NON_INTEGRAL;

	*destination = (uint64_t)value;
	return I52_ERROR_NONE;
}

EMSCRIPTEN_KEEPALIVE int mono_wasm_f64_to_i52 (int64_t *destination, double value) {
	if ((value < I52_MIN_VALUE) || (value > I52_MAX_VALUE))
		return I52_ERROR_OUT_OF_RANGE;
	if (floor(value) != value)
		return I52_ERROR_NON_INTEGRAL;

	*destination = (int64_t)value;
	return I52_ERROR_NONE;
}