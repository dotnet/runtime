// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <errno.h>
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
#include <mono/metadata/mono-debug.h>

#include <mono/metadata/mono-private-unstable.h>

#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-dl-fallback.h>
#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>

#include "pinvoke.h"

#ifdef GEN_PINVOKE
#include "wasm_m2n_invoke.g.h"
#endif
#include "gc-common.h"
#include "driver.h"


#if !defined(ENABLE_AOT) || defined(EE_MODE_LLVMONLY_INTERP)
#define NEED_INTERP 1
#ifndef LINK_ICALLS
// FIXME: llvm+interp mode needs this to call icalls
#define NEED_NORMAL_ICALL_TABLES 1
#endif
#endif

void mono_wasm_enable_debugging (int);

int mono_wasm_register_root (char *start, size_t size, const char *name);
void mono_wasm_deregister_root (char *addr);

void mono_ee_interp_init (const char *opts);
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
extern void mono_wasm_register_timezones_bundle();
#ifdef WASM_SINGLE_FILE
extern void mono_wasm_register_assemblies_bundle();
#ifndef INVARIANT_GLOBALIZATION
extern void mono_wasm_register_icu_bundle();
#endif /* INVARIANT_GLOBALIZATION */
extern const unsigned char* mono_wasm_get_bundled_file (const char *name, int* out_length);
#endif /* WASM_SINGLE_FILE */

extern const char* dotnet_wasi_getentrypointassemblyname();
int32_t mono_wasi_load_icu_data(const void* pData);
void load_icu_data (void);

int mono_wasm_enable_gc = 1;

int
mono_string_instance_is_interned (MonoString *str_raw);

void mono_trace_init (void);

#define g_new(type, size)  ((type *) malloc (sizeof (type) * (size)))
#define g_new0(type, size) ((type *) calloc (sizeof (type), (size)))

static MonoDomain *root_domain;

#define RUNTIMECONFIG_BIN_FILE "runtimeconfig.bin"

static void
wasi_trace_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data)
{
	if (strcmp(log_level, "error") == 0)
		fprintf(stderr, "[MONO] %s: %s\n", log_level, message);
	else
		printf("[MONO] %s: %s\n", log_level, message);
	if (fatal) {
		// make it trap so we could see the stack trace
		// (*(int*)(void*)-1)++;
		exit(1);
	}

}

typedef uint32_t target_mword;
typedef target_mword SgenDescriptor;
typedef SgenDescriptor MonoGCDescriptor;

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

int
mono_wasm_add_assembly (const char *name, const unsigned char *data, unsigned int size)
{
	/*printf("wasi: mono_wasm_add_assembly: %s size: %u\n", name, size);*/
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

typedef struct WasmSatelliteAssembly_ WasmSatelliteAssembly;

struct WasmSatelliteAssembly_ {
	MonoBundledSatelliteAssembly *assembly;
	WasmSatelliteAssembly *next;
};

static WasmSatelliteAssembly *satellite_assemblies;
static int satellite_assembly_count;

char* gai_strerror(int code) {
	char* result = malloc(256);
	sprintf(result, "Error code %i", code);
	return result;
}

void
mono_wasm_add_satellite_assembly (const char *name, const char *culture, const unsigned char *data, unsigned int size)
{
	WasmSatelliteAssembly *entry = g_new0 (WasmSatelliteAssembly, 1);
	entry->assembly = mono_create_new_bundled_satellite_assembly (name, culture, data, size);
	entry->next = satellite_assemblies;
	satellite_assemblies = entry;
	++satellite_assembly_count;
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
icall_table_lookup (MonoMethod *method, char *classname, char *methodname, char *sigstart, int32_t *out_flags)
{
	uint32_t token = mono_method_get_token (method);
	assert (token);
	assert ((token & MONO_TOKEN_METHOD_DEF) == MONO_TOKEN_METHOD_DEF);
	uint32_t token_idx = token - MONO_TOKEN_METHOD_DEF;

	int *indexes = NULL;
	int indexes_size = 0;
	uint8_t *flags = NULL;
	void **funcs = NULL;

	*out_flags = 0;

	const char *image_name = mono_image_get_name (mono_class_get_image (mono_method_get_class (method)));

#if defined(ICALL_TABLE_corlib)
	if (!strcmp (image_name, "System.Private.CoreLib")) {
		indexes = corlib_icall_indexes;
		indexes_size = sizeof (corlib_icall_indexes) / 4;
		flags = corlib_icall_flags;
		funcs = corlib_icall_funcs;
		assert (sizeof (corlib_icall_indexes [0]) == 4);
	}
#endif
#ifdef ICALL_TABLE_System
	if (!strcmp (image_name, "System")) {
		indexes = System_icall_indexes;
		indexes_size = sizeof (System_icall_indexes) / 4;
		flags = System_icall_flags;
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
	*out_flags = flags [idx];

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

void
mono_wasm_register_bundled_satellite_assemblies (void)
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

#ifndef INVARIANT_GLOBALIZATION
void load_icu_data (void)
{
#ifdef WASM_SINGLE_FILE
	mono_wasm_register_icu_bundle();

	int length = -1;
	const unsigned char* buffer = mono_wasm_get_bundled_file("icudt.dat", &length);
	if (!buffer) {
		printf("Could not load icudt.dat from the bundle");
		assert(buffer);
	}
#else /* WASM_SINGLE_FILE */
	FILE *fileptr;
	unsigned char *buffer;
	long filelen;
	char filename[256];
	sprintf(filename, "./icudt.dat");

	fileptr = fopen(filename, "rb");
	if (fileptr == 0) {
		printf("Failed to load %s\n", filename);
		fflush(stdout);
	}

	fseek(fileptr, 0, SEEK_END);
	filelen = ftell(fileptr);
	rewind(fileptr);

	buffer = (unsigned char *)malloc(filelen * sizeof(char));
	if(!fread(buffer, filelen, 1, fileptr)) {
		printf("Failed to load %s\n", filename);
		fflush(stdout);
	}
	fclose(fileptr);
#endif /* WASM_SINGLE_FILE */

	assert(mono_wasi_load_icu_data(buffer));
}
#endif /* INVARIANT_GLOBALIZATION */

void
cleanup_runtime_config (MonovmRuntimeConfigArguments *args, void *user_data)
{
	free (args);
	free (user_data);
}

void
mono_wasm_load_runtime (const char *unused, int debug_level)
{
	const char *interp_opts = "";

#ifndef INVARIANT_GLOBALIZATION
	char* invariant_globalization = monoeg_g_getenv ("DOTNET_SYSTEM_GLOBALIZATION_INVARIANT");
	if (strcmp(invariant_globalization, "true") != 0 && strcmp(invariant_globalization, "1") != 0)
		load_icu_data();
#endif /* INVARIANT_GLOBALIZATION */


	char* debugger_fd = monoeg_g_getenv ("DEBUGGER_FD");
	if (debugger_fd != 0)
	{
		const char *debugger_str = "--debugger-agent=transport=wasi_socket,debugger_fd=%-2s,loglevel=0";
		char *debugger_str_with_fd = (char *)malloc (sizeof (char) * (strlen(debugger_str) + strlen(debugger_fd) + 1));
		snprintf (debugger_str_with_fd, strlen(debugger_str) + strlen(debugger_fd) + 1, debugger_str, debugger_fd);
		mono_jit_parse_options (1, &debugger_str_with_fd);
		mono_debug_init (MONO_DEBUG_FORMAT_MONO);
		// Disable optimizations which interfere with debugging
		interp_opts = "-all";
		free (debugger_str_with_fd);
	}
	// When the list of app context properties changes, please update RuntimeConfigReservedProperties for
	// target _WasmGenerateRuntimeConfig in WasmApp.targets file
	const char *appctx_keys[2];
	appctx_keys [0] = "APP_CONTEXT_BASE_DIRECTORY";
	appctx_keys [1] = "RUNTIME_IDENTIFIER";

	const char *appctx_values[2];
	appctx_values [0] = "/";
	appctx_values [1] = "wasi-wasm";

	const char *file_name = RUNTIMECONFIG_BIN_FILE;
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

	mono_wasm_register_timezones_bundle();
#ifdef WASM_SINGLE_FILE
	mono_wasm_register_assemblies_bundle();
#endif
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
	mono_ee_interp_init (interp_opts);
	mono_marshal_ilgen_init ();
	mono_method_builder_ilgen_init ();
	mono_sgen_mono_ilgen_init ();

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
	mono_trace_set_log_handler (wasi_trace_logger, NULL);

	root_domain = mono_jit_init_version ("mono", NULL);
	mono_thread_set_main (mono_thread_current ());
}

MonoAssembly*
mono_wasm_assembly_load (const char *name)
{
	assert (name);
	MonoImageOpenStatus status;
	MonoAssemblyName* aname = mono_assembly_name_new (name);

	MonoAssembly *res = mono_assembly_load (aname, NULL, &status);
	mono_assembly_name_free (aname);

	return res;
}

MonoAssembly*
mono_wasm_get_corlib (const char *namespace, const char *name)
{
	MonoAssembly* result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_image_get_assembly (mono_get_corlib());
	MONO_EXIT_GC_UNSAFE;
	return result;
}

MonoClass*
mono_wasm_assembly_find_class (MonoAssembly *assembly, const char *namespace, const char *name)
{
	assert (assembly);
	MonoClass *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_from_name (mono_assembly_get_image (assembly), namespace, name);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

MonoMethod*
mono_wasm_assembly_find_method (MonoClass *klass, const char *name, int arguments)
{
	assert (klass);
	MonoMethod* result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_class_get_method_from_name (klass, name, arguments);
	MONO_EXIT_GC_UNSAFE;
	return result;
}


void
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

MonoMethod*
mono_wasi_assembly_get_entry_point (MonoAssembly *assembly)
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

	end:
	MONO_EXIT_GC_UNSAFE;
	return method;
}

int
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
	case MONO_TYPE_CHAR:
		return *(short*)ptr;
	default:
		printf ("Invalid type %d to mono_unbox_int\n", mono_type_get_type (type));
		return 0;
	}
}


void
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

void
mono_wasm_string_get_data (
	MonoString *string, mono_unichar2 **outChars, int *outLengthBytes, int *outIsInterned
) {
	mono_wasm_string_get_data_ref(&string, outChars, outLengthBytes, outIsInterned);
}

void add_assembly(const char* base_dir, const char *name) {
	FILE *fileptr;
	unsigned char *buffer;
	long filelen;
	char filename[256];
	sprintf(filename, "%s/%s", base_dir, name);
	// printf("Loading %s...\n", filename);

	fileptr = fopen(filename, "rb");
	if (fileptr == 0) {
		printf("Failed to load %s\n", filename);
		fflush(stdout);
	}

	fseek(fileptr, 0, SEEK_END);
	filelen = ftell(fileptr);
	rewind(fileptr);

	buffer = (unsigned char *)malloc(filelen * sizeof(char));
	if(!fread(buffer, filelen, 1, fileptr)) {
		printf("Failed to load %s\n", filename);
		fflush(stdout);
	}
	fclose(fileptr);

	assert(mono_wasm_add_assembly(name, buffer, filelen));
}

MonoMethod* lookup_dotnet_method(const char* assembly_name, const char* namespace, const char* type_name, const char* method_name, int num_params) {
	MonoAssembly* assembly = mono_wasm_assembly_load (assembly_name);
	assert (assembly);
	MonoClass* class = mono_wasm_assembly_find_class (assembly, namespace, type_name);
	assert (class);
	MonoMethod* method = mono_wasm_assembly_find_method (class, method_name, num_params);
	assert (method);
	return method;
}

MonoArray*
mono_wasm_string_array_new (int size)
{
	return mono_array_new (root_domain, mono_get_string_class (), size);
}

#ifdef _WASI_DEFAULT_MAIN
/*
 * with wasmtime, this is run as:
 *  $ wasmtime run--dir . dotnet.wasm MainAssembly [args]
 *
 *
 * arg0: dotnet.wasm
 * arg1: MainAssembly
 * arg2-..: args
 */
int main(int argc, char * argv[]) {
	if (argc < 2) {
		printf("Error: First argument must be the name of the main assembly\n");
		return 1;
	}

	mono_set_assemblies_path("managed");
	mono_wasm_load_runtime("", 0);

	const char *assembly_name = argv[1];
	MonoAssembly* assembly = mono_wasm_assembly_load (assembly_name);
	if (!assembly) {
		printf("Could not load assembly %s\n", assembly_name);
		return 1;
	}
	MonoMethod* entry_method = mono_wasi_assembly_get_entry_point (assembly);
	if (!entry_method) {
		fprintf(stderr, "Could not find entrypoint in the assembly.\n");
		exit(1);
	}

	MonoObject* out_exc;
	MonoObject* out_res;
	// Managed app will see: arg0: MainAssembly, arg1-.. [args]
	int ret = mono_runtime_run_main(entry_method, argc - 1, &argv[1], &out_exc);
	if (out_exc) {
		mono_print_unhandled_exception(out_exc);
		exit(1);
	}
	return ret < 0 ? -ret : ret;
}
#endif
