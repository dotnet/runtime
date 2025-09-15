// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <emscripten.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <sys/types.h>
#include <uchar.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/class.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/assembly.h>
#include <mono/jit/jit.h>

#include "wasm-config.h"
#include "gc-common.h"

//JS funcs
extern void SystemJSInterop_ReleaseCSOwnedObject (int js_handle);
extern void SystemJSInterop_ResolveOrRejectPromise (void *args);
extern void SystemJSInterop_CancelPromise (int task_holder_gc_handle);
extern void SystemJS_ConsoleClear ();
extern void mono_wasm_set_entrypoint_breakpoint (int entry_point_metadata_token);
extern void mono_wasm_trace_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data);
extern void SystemJSInterop_InvokeJSFunction (int function_js_handle, void *args);

extern int mono_runtime_run_module_cctor (MonoImage *image, MonoError *error);

typedef void (*background_job_cb)(void);
typedef int (*ds_job_cb)(void* data);

void SystemJSInterop_BindAssemblyExports (char *assembly_name);
void SystemJSInterop_AssemblyGetEntryPoint (char *assembly_name, int auto_insert_breakpoint, MonoMethod **method_out);
void SystemJSInterop_GetAssemblyExport (char *assembly_name, char *namespace, char *classname, char *methodname, int signature_hash, MonoMethod **method_out);

#ifndef DISABLE_THREADS
void SystemJSInterop_ReleaseCSOwnedObjectPost (pthread_t target_tid, int js_handle);
void SystemJSInterop_ResolveOrRejectPromisePost (pthread_t target_tid, void *args);
void SystemJSInterop_CancelPromisePost (pthread_t target_tid, int task_holder_gc_handle);

extern void SystemJSInterop_InstallWebWorkerInteropJS (int context_gc_handle);
void SystemJSInterop_InstallWebWorkerInterop (int context_gc_handle, void* beforeSyncJSImport, void* afterSyncJSImport, void* pumpHandler);
extern void SystemJSInterop_UninstallWebWorkerInterop ();
extern void SystemJSInterop_InvokeJSImportSync (void* signature, void* args);
void SystemJSInterop_InvokeJSImportAsyncPost (pthread_t target_tid, void* signature, void* args);
void SystemJSInterop_InvokeJSImportSyncSend (pthread_t target_tid, void* signature, void* args);
void SystemJSInterop_InvokeJSFunctionSend (pthread_t target_tid, int function_js_handle, void *args);
extern void mono_threads_wasm_async_run_in_target_thread_vi (pthread_t target_thread, void (*func) (gpointer), gpointer user_data1);
extern void mono_threads_wasm_async_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2);
extern void mono_threads_wasm_sync_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer args);
extern void SystemJS_WarnAboutBlockingWait (void* ptr, int32_t length);
#else
extern void* SystemJSInterop_BindJSImportST (void *signature);
extern void SystemJSInterop_InvokeJSImportST (int function_handle, void *args);
#endif /* DISABLE_THREADS */

// JS-based globalization
extern char16_t* SystemJS_GetLocaleInfo (const uint16_t* locale, int32_t localeLength, const uint16_t* culture, int32_t cultureLength, const uint16_t* result, int32_t resultMaxLength, int *resultLength);

void bindings_initialize_internals (void)
{
#ifndef	ENABLE_JS_INTEROP_BY_VALUE
	mono_add_internal_call ("Interop/Runtime::RegisterGCRoot", SystemJSInterop_RegisterGCRoot);
	mono_add_internal_call ("Interop/Runtime::DeregisterGCRoot", SystemJSInterop_DeregisterGCRoot);
#endif /* ENABLE_JS_INTEROP_BY_VALUE */

#ifndef DISABLE_THREADS
	mono_add_internal_call ("Interop/Runtime::ReleaseCSOwnedObjectPost", SystemJSInterop_ReleaseCSOwnedObjectPost);
	mono_add_internal_call ("Interop/Runtime::ResolveOrRejectPromisePost", SystemJSInterop_ResolveOrRejectPromisePost);
	mono_add_internal_call ("Interop/Runtime::InstallWebWorkerInterop", SystemJSInterop_InstallWebWorkerInterop);
	mono_add_internal_call ("Interop/Runtime::UninstallWebWorkerInterop", SystemJSInterop_UninstallWebWorkerInterop);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportSync", SystemJSInterop_InvokeJSImportSync);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportSyncSend", SystemJSInterop_InvokeJSImportSyncSend);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportAsyncPost", SystemJSInterop_InvokeJSImportAsyncPost);
	mono_add_internal_call ("Interop/Runtime::InvokeJSFunctionSend", SystemJSInterop_InvokeJSFunctionSend);
	mono_add_internal_call ("Interop/Runtime::CancelPromisePost", SystemJSInterop_CancelPromisePost);
	mono_add_internal_call ("System.Threading.Thread::WarnAboutBlockingWait", SystemJS_WarnAboutBlockingWait);
#else
	mono_add_internal_call ("Interop/Runtime::BindJSImportST", SystemJSInterop_BindJSImportST);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportST", SystemJSInterop_InvokeJSImportST);
#endif /* DISABLE_THREADS */

	mono_add_internal_call ("Interop/Runtime::ReleaseCSOwnedObject", SystemJSInterop_ReleaseCSOwnedObject);
	mono_add_internal_call ("Interop/Runtime::ResolveOrRejectPromise", SystemJSInterop_ResolveOrRejectPromise);
	mono_add_internal_call ("Interop/Runtime::InvokeJSFunction", SystemJSInterop_InvokeJSFunction);
	mono_add_internal_call ("Interop/Runtime::CancelPromise", SystemJSInterop_CancelPromise);
	mono_add_internal_call ("Interop/Runtime::AssemblyGetEntryPoint", SystemJSInterop_AssemblyGetEntryPoint);
	mono_add_internal_call ("Interop/Runtime::BindAssemblyExports", SystemJSInterop_BindAssemblyExports);
	mono_add_internal_call ("Interop/Runtime::GetAssemblyExport", SystemJSInterop_GetAssemblyExport);
	mono_add_internal_call ("System.ConsolePal::Clear", SystemJS_ConsoleClear);

	// JS-based globalization
	mono_add_internal_call ("Interop/JsGlobalization::GetLocaleInfo", SystemJS_GetLocaleInfo);
}

static MonoAssembly* _mono_wasm_assembly_load (char *assembly_name)
{
	assert (assembly_name);
	MonoImageOpenStatus status;
	MonoAssemblyName* aname = mono_assembly_name_new (assembly_name);
	assert (aname);

	MonoAssembly *res = mono_assembly_load (aname, NULL, &status);
	mono_assembly_name_free (aname);
	free (assembly_name);

	return res;
}

void SystemJSInterop_AssemblyGetEntryPoint (char *assembly_name, int auto_insert_breakpoint, MonoMethod **method_out)
{
	assert (assembly_name);
	*method_out = NULL;
	MonoAssembly* assembly = _mono_wasm_assembly_load (assembly_name);
	if(!assembly)
		goto end;

	MonoImage *image;
	MonoMethod *method = NULL;

	image = mono_assembly_get_image (assembly);
	uint32_t entry = mono_image_get_entry_point (image);
	if (!entry)
		goto end;

	mono_domain_ensure_entry_assembly (mono_get_root_domain (), assembly);
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
	if (auto_insert_breakpoint && method)
	{
		mono_wasm_set_entrypoint_breakpoint(mono_method_get_token (method));
	}
	*method_out = method;
}

void SystemJSInterop_BindAssemblyExports (char *assembly_name)
{
	MonoError error;
	MonoAssembly* assembly;
	MonoImage *image;
	MonoClass *klass;
	MonoMethod *method;
	PVOLATILE(MonoObject) temp_exc = NULL;

	assert (assembly_name);
	assembly = _mono_wasm_assembly_load (assembly_name);
	assert (assembly);
	image = mono_assembly_get_image (assembly);
	assert (image);

	klass = mono_class_from_name (image, "System.Runtime.InteropServices.JavaScript", "__GeneratedInitializer");
	if (klass) {
		method = mono_class_get_method_from_name (klass, "__Register_", -1);
		if (method) {
			mono_runtime_invoke (method, NULL, NULL, (MonoObject **)&temp_exc);
			if (temp_exc) {
				PVOLATILE(MonoObject) exc2 = NULL;
				store_volatile((MonoObject**)&temp_exc, (MonoObject*)mono_object_to_string ((MonoObject*)temp_exc, (MonoObject **)&exc2));
				if (exc2) {
					mono_wasm_trace_logger ("jsinterop", "critical", "SystemJSInterop_BindAssemblyExports unexpected double fault", 1, NULL);
				} else {
					mono_wasm_trace_logger ("jsinterop", "critical", mono_string_to_utf8((MonoString*)temp_exc), 1, NULL);
				}
				abort ();
			}
		}
	}
	else if (!mono_runtime_run_module_cctor(image, &error)) {
		//g_print ("Failed to run module constructor due to %s\n", mono_error_get_message (error));
	}
}

void SystemJSInterop_GetAssemblyExport (char *assembly_name, char *namespace, char *classname, char *methodname, int signature_hash, MonoMethod **method_out)
{
	MonoError error;
	MonoAssembly* assembly;
	MonoImage *image;
	MonoClass *klass;
	MonoMethod *method=NULL;
    char real_method_name_buffer[4096];
	*method_out = NULL;

	assert (assembly_name);
	assembly = _mono_wasm_assembly_load (assembly_name);
	assert (assembly);
	image = mono_assembly_get_image (assembly);
	assert (image);

	klass = mono_class_from_name (image, namespace, classname);
	assert (klass);

    snprintf(real_method_name_buffer, 4096, "__Wrapper_%s_%d", methodname, signature_hash);

	method = mono_class_get_method_from_name (klass, real_method_name_buffer, -1);
	assert (method);

	*method_out = method;
    // This is freed by _mono_wasm_assembly_load for some reason
    // free (assembly_name);
	free (namespace);
	free (classname);
	free (methodname);
}

#ifndef DISABLE_THREADS

void* before_sync_js_import;
void* after_sync_js_import;
void* synchronization_context_pump_handler;

void SystemJSInterop_InstallWebWorkerInterop (int context_gc_handle, void* beforeSyncJSImport, void* afterSyncJSImport, void* pumpHandler)
{
	before_sync_js_import = beforeSyncJSImport;
	after_sync_js_import = afterSyncJSImport;
	synchronization_context_pump_handler = pumpHandler;
	SystemJSInterop_InstallWebWorkerInteropJS (context_gc_handle);
}

// async
void SystemJSInterop_ReleaseCSOwnedObjectPost (pthread_t target_tid, int js_handle)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))SystemJSInterop_ReleaseCSOwnedObject, (gpointer)js_handle);
}

// async
void SystemJSInterop_ResolveOrRejectPromisePost (pthread_t target_tid, void* args)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))SystemJSInterop_ResolveOrRejectPromise, (gpointer)args);
}

// async
void SystemJSInterop_CancelPromisePost (pthread_t target_tid, int task_holder_gc_handle)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))SystemJSInterop_CancelPromise, (gpointer)task_holder_gc_handle);
}

// async
void SystemJSInterop_InvokeJSImportAsyncPost (pthread_t target_tid, void* signature, void* args)
{
	mono_threads_wasm_async_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))SystemJSInterop_InvokeJSImportSync, (gpointer)signature, (gpointer)args);
}

// sync
void SystemJSInterop_InvokeJSImportSyncSend (pthread_t target_tid, void* signature, void* args)
{
	mono_threads_wasm_sync_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))SystemJSInterop_InvokeJSImportSync, (gpointer)signature, (gpointer)args);
}

// sync
void SystemJSInterop_InvokeJSFunctionSend (pthread_t target_tid, int function_js_handle, void *args)
{
	mono_threads_wasm_sync_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))SystemJSInterop_InvokeJSFunction, (gpointer)function_js_handle, (gpointer)args);
}

#endif /* DISABLE_THREADS */
