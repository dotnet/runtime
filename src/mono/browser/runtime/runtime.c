// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Common runtime code shared between emscripten and wasi.
// Use #ifdef HOST_BROWSER/HOST_WASI for target specific code.
//

#ifdef __EMSCRIPTEN__
#define HOST_BROWSER 1
#else
#define HOST_WASI 1
#endif

#ifdef HOST_BROWSER
#include <emscripten.h>
#include <emscripten/stack.h>
#include <dlfcn.h>
#endif
#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <math.h>
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

#include "runtime.h"

#ifdef DRIVER_GEN
#include "driver-gen.c"
#endif

#ifdef HOST_WASI
#define EMSCRIPTEN_KEEPALIVE
#endif

/* Missing from public headers */
char *mono_fixup_symbol_name (const char *prefix, const char *key, const char *suffix);
void mono_icall_table_init (void);
void mono_wasm_enable_debugging (int);
void mono_ee_interp_init (const char *opts);
void mono_marshal_ilgen_init (void);
void mono_method_builder_ilgen_init (void);
void mono_sgen_mono_ilgen_init (void);
char *monoeg_g_getenv(const char *variable);
int monoeg_g_setenv(const char *variable, const char *value, int overwrite);
int32_t mini_parse_debug_option (const char *option);
char *mono_method_get_full_name (MonoMethod *method);
void mono_trace_init (void);
MonoMethod *mono_marshal_get_managed_wrapper (MonoMethod *method, MonoClass *delegate_klass, MonoGCHandle target_handle, MonoError *error);

/* Not part of public headers */
#define MONO_ICALL_TABLE_CALLBACKS_VERSION 3

typedef struct {
	int version;
	void* (*lookup) (MonoMethod *method, char *classname, char *methodname, char *sigstart, int32_t *flags);
	const char* (*lookup_icall_symbol) (void* func);
} MonoIcallTableCallbacks;

void
mono_install_icall_table_callbacks (const MonoIcallTableCallbacks *cb);

#define g_new(type, size)  ((type *) malloc (sizeof (type) * (size)))
#define g_new0(type, size) ((type *) calloc (sizeof (type), (size)))

#if !defined(ENABLE_AOT) || defined(EE_MODE_LLVMONLY_INTERP)
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

static void
init_icall_table (void)
{
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
}

/*
 * get_native_to_interp:
 *
 *   Return a pointer to a wasm function which can be used to enter the interpreter to
 * execute METHOD from native code.
 * EXTRA_ARG is the argument passed to the interp entry functions in the runtime.
 */
static void*
get_native_to_interp (MonoMethod *method, void *extra_arg)
{
	void *addr = NULL;
	MONO_ENTER_GC_UNSAFE;
	MonoClass *klass = mono_method_get_class (method);
	MonoImage *image = mono_class_get_image (klass);
	MonoAssembly *assembly = mono_image_get_assembly (image);
	MonoAssemblyName *aname = mono_assembly_get_name (assembly);
	const char *name = mono_assembly_name_get_name (aname);
	const char *namespace = mono_class_get_namespace (klass);
	const char *class_name = mono_class_get_name (klass);
	const char *method_name = mono_method_get_name (method);
	MonoMethodSignature *sig = mono_method_signature (method);
	uint32_t param_count = mono_signature_get_param_count (sig);
	uint32_t token = mono_method_get_token (method);

	char buf [128];
	char *key = buf;
	int len;
	if (name != NULL) {
		// the key must match the one used in PInvokeTableGenerator
		len = snprintf (key, sizeof(buf), "%s#%d:%s:%s:%s", method_name, param_count, name, namespace, class_name);

		if (len >= sizeof (buf)) {
			// The key is too long, try again with a larger buffer
			key = g_new (char, len + 1);
			snprintf (key, len + 1, "%s#%d:%s:%s:%s", method_name, param_count, name, namespace, class_name);
		}

		addr = wasm_dl_get_native_to_interp (token, key, extra_arg);

		if (key != buf)
			free (key);
	}
	MONO_EXIT_GC_UNSAFE;
	return addr;
}

static void *sysglobal_native_handle = (void *)0xDeadBeef;

static void*
wasm_dl_load (const char *name, int flags, char **err, void *user_data)
{
#if WASM_SUPPORTS_DLOPEN
	if (!name)
		return dlopen(NULL, flags);
#else
	if (!name)
		return NULL;
#endif

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

int
import_compare_name (const void *k1, const void *k2)
{
	const PinvokeImport *e1 = (const PinvokeImport*)k1;
	const PinvokeImport *e2 = (const PinvokeImport*)k2;

	return strcmp (e1->name, e2->name);
}

static void*
wasm_dl_symbol (void *handle, const char *name, char **err, void *user_data)
{
	assert (handle != sysglobal_native_handle);

#if WASM_SUPPORTS_DLOPEN
	if (!wasm_dl_is_pinvoke_tables (handle)) {
		return dlsym (handle, name);
	}
#endif
	PinvokeTable* index = (PinvokeTable*)handle;
	PinvokeImport key = { name, NULL };
    PinvokeImport* result = (PinvokeImport *)bsearch(&key, index->imports, index->count, sizeof(PinvokeImport), import_compare_name);
    if (!result) {
        // *err = g_strdup_printf ("Symbol not found: %s", name);
        return NULL;
    }
    return result->func;
}

MonoDomain *
mono_wasm_load_runtime_common (int debug_level, MonoLogCallback log_callback, const char *interp_opts)
{
	MonoDomain *domain;

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
		interp_opts = "-all,simd";
		mono_wasm_enable_debugging (debug_level);
	}
#endif

	init_icall_table ();

	mono_ee_interp_init (interp_opts);
	mono_marshal_ilgen_init();
	mono_method_builder_ilgen_init ();
	mono_sgen_mono_ilgen_init ();

	mono_trace_init ();
	mono_trace_set_log_handler (log_callback, NULL);
	domain = mono_jit_init_version ("mono", NULL);
	mono_thread_set_main (mono_thread_current ());

	return domain;
}

// TODO https://github.com/dotnet/runtime/issues/98366
EMSCRIPTEN_KEEPALIVE MonoAssembly*
mono_wasm_assembly_load (const char *name)
{
	MonoAssembly *res;
	assert (name);
	MonoImageOpenStatus status;
	MONO_ENTER_GC_UNSAFE;
	MonoAssemblyName* aname = mono_assembly_name_new (name);
	res = mono_assembly_load (aname, NULL, &status);
	mono_assembly_name_free (aname);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

// TODO https://github.com/dotnet/runtime/issues/98366
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

// TODO https://github.com/dotnet/runtime/issues/98366
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

MonoMethod*
mono_wasm_get_method_matching (MonoImage *image, uint32_t token, MonoClass *klass, const char* name, int param_count)
{
	MonoMethod *result = NULL;
	MONO_ENTER_GC_UNSAFE;
	MonoMethod *method = mono_get_method (image, token, klass);
	MonoMethodSignature *sig = mono_method_signature (method);
	// Lookp by token but verify the name and param count in case assembly was trimmed
	if (mono_signature_get_param_count (sig) == param_count) {
		const char *method_name = mono_method_get_name (method);
		if (!strcmp (method_name, name)) {
			result = method;
		}
	}
	// If the token lookup failed, try to find the method by name and param count
	if (!result) {
		result = mono_class_get_method_from_name (klass, name, param_count);
	}
	MONO_EXIT_GC_UNSAFE;
	return result;
}

/*
 * mono_wasm_marshal_get_managed_wrapper:
 * Creates a wrapper for a function pointer to a method marked with
 * UnamangedCallersOnlyAttribute.
 * This wrapper ensures that the interpreter initializes the pointers.
 */
void
mono_wasm_marshal_get_managed_wrapper (const char* assemblyName, const char* namespaceName, const char* typeName, const char* methodName, uint32_t token, int param_count)
{
	MonoError error;
	mono_error_init (&error);
	MONO_ENTER_GC_UNSAFE;
	MonoAssembly* assembly = mono_wasm_assembly_load (assemblyName);
	assert (assembly);
	MonoImage *image = mono_assembly_get_image (assembly);
	assert (image);
	MonoClass* klass = mono_class_from_name (image, namespaceName, typeName);
	assert (klass);
	MonoMethod *method = mono_wasm_get_method_matching (image, token, klass, methodName, param_count);
	assert (method);
	MonoMethod *managedWrapper = mono_marshal_get_managed_wrapper (method, NULL, 0, &error);
	assert (managedWrapper);
	mono_compile_method (managedWrapper);
	MONO_EXIT_GC_UNSAFE;
}