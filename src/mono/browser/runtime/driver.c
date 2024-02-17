// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <emscripten.h>
#include <emscripten/stack.h>
#include <stdio.h>
#include <stddef.h>
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
#include "runtime.h"

#include "gc-common.h"

void bindings_initialize_internals ();

char *monoeg_g_getenv(const char *variable);
int monoeg_g_setenv(const char *variable, const char *value, int overwrite);
char *mono_method_get_full_name (MonoMethod *method);

#ifndef INVARIANT_TIMEZONE
extern void mono_register_timezones_bundle (void);
#endif /* INVARIANT_TIMEZONE */
extern void mono_wasm_set_entrypoint_breakpoint (const char* assembly_name, int method_token);

extern void mono_bundled_resources_add_assembly_resource (const char *id, const char *name, const uint8_t *data, uint32_t size, void (*free_func)(void *, void*), void *free_data);
extern void mono_bundled_resources_add_assembly_symbol_resource (const char *id, const uint8_t *data, uint32_t size, void (*free_func)(void *, void *), void *free_data);
extern void mono_bundled_resources_add_satellite_assembly_resource (const char *id, const char *name, const char *culture, const uint8_t *data, uint32_t size, void (*free_func)(void *, void*), void *free_data);

int
mono_string_instance_is_interned (MonoString *str_raw);

int mono_regression_test_step (int verbose_level, char *image, char *method_name);

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

static void
bundled_resources_free_func (void *resource, void *free_data)
{
	free (free_data);
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_add_assembly (const char *name, const unsigned char *data, unsigned int size)
{
	int len = strlen (name);
	if (!strcasecmp (".pdb", &name [len - 4])) {
		char *new_name = strdup (name);
		//FIXME handle debugging assemblies with .exe extension
		strcpy (&new_name [len - 3], "dll");
		mono_bundled_resources_add_assembly_symbol_resource (new_name, data, size, bundled_resources_free_func, new_name);
		return 1;
	}
	char *assembly_name = strdup (name);
	assert (assembly_name);
	mono_bundled_resources_add_assembly_resource (assembly_name, assembly_name, data, size, bundled_resources_free_func, assembly_name);
	return mono_has_pdb_checksum ((char*)data, size);
}

static void
bundled_resources_free_slots_func (void *resource, void *free_data)
{
	if (free_data) {
		void **slots = (void **)free_data;
		for (int i = 0; slots [i]; i++)
			free (slots [i]);
	}
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_add_satellite_assembly (const char *name, const char *culture, const unsigned char *data, unsigned int size)
{
	int id_len = strlen (culture) + 1 + strlen (name); // +1 is for the "/"
	char *id = (char *)malloc (sizeof (char) * (id_len + 1)); // +1 is for the terminating null character
	assert (id);

	int num_char = snprintf (id, (id_len + 1), "%s/%s", culture, name);
	assert (num_char > 0 && num_char == id_len);

	char *satellite_assembly_name = strdup (name);
	assert (satellite_assembly_name);

	char *satellite_assembly_culture = strdup (culture);
	assert (satellite_assembly_culture);

	void **slots = malloc (sizeof (void *) * 4);
	assert (slots);
	slots [0] = id;
	slots [1] = satellite_assembly_name;
	slots [2] = satellite_assembly_culture;
	slots [3] = NULL;

	mono_bundled_resources_add_satellite_assembly_resource (id, satellite_assembly_name, satellite_assembly_culture, data, size, bundled_resources_free_slots_func, slots);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_setenv (const char *name, const char *value)
{
	assert (name);
	assert (value);
	monoeg_g_setenv (strdup (name), strdup (value), 1);
}

EMSCRIPTEN_KEEPALIVE char *
mono_wasm_getenv (const char *name)
{
	return monoeg_g_getenv (name); // JS must free
}

void mono_wasm_link_icu_shim (void);

void
cleanup_runtime_config (MonovmRuntimeConfigArguments *args, void *user_data)
{
	free (args);
	free (user_data);
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_load_runtime (int debug_level)
{
	const char *interp_opts = "";

#ifndef INVARIANT_GLOBALIZATION
	mono_wasm_link_icu_shim ();
#endif

	// When the list of app context properties changes, please update RuntimeConfigReservedProperties for
	// target _WasmGenerateRuntimeConfig in BrowserWasmApp.targets file
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

#ifndef INVARIANT_TIMEZONE
	mono_register_timezones_bundle ();
#endif /* INVARIANT_TIMEZONE */

	root_domain = mono_wasm_load_runtime_common (debug_level, wasm_trace_logger, interp_opts);

	bindings_initialize_internals();
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_invoke_jsexport (MonoMethod *method, void* args)
{
	PVOLATILE(MonoObject) temp_exc = NULL;

	void *invoke_args[1] = { args };

	MONO_ENTER_GC_UNSAFE;
	mono_runtime_invoke (method, NULL, args ? invoke_args : NULL, (MonoObject **)&temp_exc);

	// this failure is unlikely because it would be runtime error, not application exception.
	// the application exception is passed inside JSMarshalerArguments `args`
	// so, if that happens, we should abort the runtime
	if (temp_exc) {
		PVOLATILE(MonoObject) exc2 = NULL;
		store_volatile((MonoObject**)&temp_exc, (MonoObject*)mono_object_to_string ((MonoObject*)temp_exc, (MonoObject **)&exc2));
		if (exc2) {
			mono_wasm_trace_logger ("jsinterop", "critical", "mono_wasm_invoke_jsexport unexpected double fault", 1, NULL);
		} else {
			mono_wasm_trace_logger ("jsinterop", "critical", mono_string_to_utf8((MonoString*)temp_exc), 1, NULL);
		}
		abort ();
	}
	MONO_EXIT_GC_UNSAFE;
}

#ifndef DISABLE_THREADS

extern void mono_threads_wasm_async_run_in_target_thread_vii (void* target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2);
extern void mono_threads_wasm_sync_run_in_target_thread_vii (void* target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2);

static void
mono_wasm_invoke_jsexport_async_post_cb (MonoMethod *method, void* args)
{
	mono_wasm_invoke_jsexport (method, args);
	// TODO assert receiver_should_free ?
	free (args);
}

// async
EMSCRIPTEN_KEEPALIVE void
mono_wasm_invoke_jsexport_async_post (void* target_thread, MonoMethod *method, void* args /*JSMarshalerArguments*/)
{
	mono_threads_wasm_async_run_in_target_thread_vii(target_thread, (void (*)(gpointer, gpointer))mono_wasm_invoke_jsexport_async_post_cb, method, args);
}

// sync
EMSCRIPTEN_KEEPALIVE void
mono_wasm_invoke_jsexport_async_send (void* target_thread, MonoMethod *method, void* args /*JSMarshalerArguments*/)
{
	mono_threads_wasm_sync_run_in_target_thread_vii(target_thread, (void (*)(gpointer, gpointer))mono_wasm_invoke_jsexport, method, args);
}

#endif /* DISABLE_THREADS */

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

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exec_regression (int verbose_level, char *image)
{
	return mono_regression_test_step (verbose_level, image, NULL) ? 0 : 1;
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_exit (int exit_code)
{
	if (exit_code == 0)
	{
		mono_jit_cleanup (root_domain);
	}
	fflush (stdout);
	fflush (stderr);
	emscripten_force_exit (exit_code);
}

EMSCRIPTEN_KEEPALIVE int
mono_wasm_abort ()
{
	abort ();
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
mono_wasm_profiler_init_aot (const char *desc)
{
	mono_profiler_init_aot (desc);
}

#endif

#ifdef ENABLE_BROWSER_PROFILER

void mono_profiler_init_browser (const char *desc);

EMSCRIPTEN_KEEPALIVE void
mono_wasm_profiler_init_browser (const char *desc)
{
	mono_profiler_init_browser (desc);
}

#endif

EMSCRIPTEN_KEEPALIVE void
mono_wasm_init_finalizer_thread (void)
{
	// in the single threaded build, finalizers periodically run on the main thread instead.
#ifndef DISABLE_THREADS
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

// JS is responsible for freeing this
EMSCRIPTEN_KEEPALIVE const char * mono_wasm_method_get_full_name (MonoMethod *method) {
	return mono_method_get_full_name(method);
}

EMSCRIPTEN_KEEPALIVE const char * mono_wasm_method_get_name (MonoMethod *method) {
	return mono_method_get_name(method);
}

EMSCRIPTEN_KEEPALIVE float mono_wasm_get_f32_unaligned (const float *src) {
	return *src;
}

EMSCRIPTEN_KEEPALIVE double mono_wasm_get_f64_unaligned (const double *src) {
	return *src;
}

EMSCRIPTEN_KEEPALIVE int32_t mono_wasm_get_i32_unaligned (const int32_t *src) {
	return *src;
}

EMSCRIPTEN_KEEPALIVE int mono_wasm_is_zero_page_reserved () {
	// If the stack is above the first 512 bytes of memory this indicates that it is safe
	//  to optimize out null checks for operations that also do a bounds check, like string
	//  and array element loads. (We already know that Emscripten malloc will never allocate
	//  data at 0.) This is the default behavior for Emscripten release builds and is
	//  controlled by the emscripten GLOBAL_BASE option (default value 1024).
	// clang/llvm may perform this optimization if --low-memory-unused is set.
	// https://github.com/emscripten-core/emscripten/issues/19389
	return (emscripten_stack_get_base() > 512) && (emscripten_stack_get_end() > 512);
}

// this will return bool value if the object is a bool, otherwise it will return -1 or error
// we use it in Blazor's renderBatch as internal only
EMSCRIPTEN_KEEPALIVE int
mono_wasm_read_as_bool_or_null_unsafe (PVOLATILE(MonoObject) obj) {

	int result = -1;

	MONO_ENTER_GC_UNSAFE;

	MonoClass *klass = mono_object_get_class (obj);
	if (!klass) {
		goto end;
	}

	MonoType *type = mono_class_get_type (klass);
	if (!type) {
		goto end;
	}

	int mono_type = mono_type_get_type (type);
	if (MONO_TYPE_BOOLEAN == mono_type) {
		result = ((signed char*)mono_object_unbox (obj) == 0 ? 0 : 1);
	}

	end:
	MONO_EXIT_GC_UNSAFE;
	return result;
}