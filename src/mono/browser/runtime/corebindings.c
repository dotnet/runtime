// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <emscripten.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <sys/types.h>

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
extern void mono_wasm_release_cs_owned_object (int js_handle);
extern void mono_wasm_resolve_or_reject_promise (void *args);
extern void mono_wasm_cancel_promise (int task_holder_gc_handle);
extern void mono_wasm_console_clear ();
extern void mono_wasm_set_entrypoint_breakpoint (int entry_point_metadata_token);
extern void mono_wasm_trace_logger (const char *log_domain, const char *log_level, const char *message, mono_bool fatal, void *user_data);
extern void mono_wasm_invoke_js_function (int function_js_handle, void *args);

extern int mono_runtime_run_module_cctor (MonoImage *image, MonoError *error);

typedef void (*background_job_cb)(void);

void mono_wasm_bind_assembly_exports (char *assembly_name);
void mono_wasm_assembly_get_entry_point (char *assembly_name, int auto_insert_breakpoint, MonoMethod **method_out);
void mono_wasm_get_assembly_export (char *assembly_name, char *namespace, char *classname, char *methodname, MonoMethod **method_out);

#ifndef DISABLE_THREADS
void mono_wasm_release_cs_owned_object_post (pthread_t target_tid, int js_handle);
void mono_wasm_resolve_or_reject_promise_post (pthread_t target_tid, void *args);
void mono_wasm_cancel_promise_post (pthread_t target_tid, int task_holder_gc_handle);

extern void mono_wasm_install_js_worker_interop (int context_gc_handle);
extern void mono_wasm_uninstall_js_worker_interop ();
extern void mono_wasm_invoke_jsimport (void* signature, void* args);
void mono_wasm_invoke_jsimport_async_post (pthread_t target_tid, void* signature, void* args);
void mono_wasm_invoke_jsimport_sync_send (pthread_t target_tid, void* signature, void* args);
void mono_wasm_invoke_js_function_send (pthread_t target_tid, int function_js_handle, void *args);
extern void mono_threads_wasm_async_run_in_target_thread_vi (pthread_t target_thread, void (*func) (gpointer), gpointer user_data1);
extern void mono_threads_wasm_async_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2);
extern void mono_threads_wasm_sync_run_in_target_thread_vii (pthread_t target_thread, void (*func) (gpointer, gpointer), gpointer user_data1, gpointer user_data2);
#else
extern void mono_wasm_bind_js_import (void *signature, int *is_exception, MonoObject **result);
extern void mono_wasm_invoke_jsimport_ST (int function_handle, void *args);
#endif /* DISABLE_THREADS */

// HybridGlobalization
extern void mono_wasm_change_case_invariant (const uint16_t* src, int32_t srcLength, uint16_t* dst, int32_t dstLength, mono_bool bToUpper, int *is_exception, MonoObject** ex_result);
extern void mono_wasm_change_case (MonoString **culture, const uint16_t* src, int32_t srcLength, uint16_t* dst, int32_t dstLength, mono_bool bToUpper, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_compare_string (MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern mono_bool mono_wasm_starts_with (MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern mono_bool mono_wasm_ends_with (MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_index_of (MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, mono_bool fromBeginning, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_get_calendar_info (MonoString **culture, int32_t calendarId, const uint16_t* result, int32_t resultLength, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_get_culture_info (MonoString **culture, const uint16_t* result, int32_t resultLength, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_get_first_day_of_week (MonoString **culture, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_get_first_week_of_year (MonoString **culture, int *is_exception, MonoObject** ex_result);

void bindings_initialize_internals (void)
{
#ifndef	ENABLE_JS_INTEROP_BY_VALUE
	mono_add_internal_call ("Interop/Runtime::RegisterGCRoot", mono_wasm_register_root);
	mono_add_internal_call ("Interop/Runtime::DeregisterGCRoot", mono_wasm_deregister_root);
#endif /* ENABLE_JS_INTEROP_BY_VALUE */

#ifndef DISABLE_THREADS
	mono_add_internal_call ("Interop/Runtime::ReleaseCSOwnedObjectPost", mono_wasm_release_cs_owned_object_post);
	mono_add_internal_call ("Interop/Runtime::ResolveOrRejectPromisePost", mono_wasm_resolve_or_reject_promise_post);
	mono_add_internal_call ("Interop/Runtime::InstallWebWorkerInterop", mono_wasm_install_js_worker_interop);
	mono_add_internal_call ("Interop/Runtime::UninstallWebWorkerInterop", mono_wasm_uninstall_js_worker_interop);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportSync", mono_wasm_invoke_jsimport);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportSyncSend", mono_wasm_invoke_jsimport_sync_send);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportAsyncPost", mono_wasm_invoke_jsimport_async_post);
	mono_add_internal_call ("Interop/Runtime::InvokeJSFunctionSend", mono_wasm_invoke_js_function_send);
	mono_add_internal_call ("Interop/Runtime::CancelPromisePost", mono_wasm_cancel_promise_post);
#else
	mono_add_internal_call ("Interop/Runtime::BindJSImport", mono_wasm_bind_js_import);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportST", mono_wasm_invoke_jsimport_ST);
#endif /* DISABLE_THREADS */

	mono_add_internal_call ("Interop/Runtime::ReleaseCSOwnedObject", mono_wasm_release_cs_owned_object);
	mono_add_internal_call ("Interop/Runtime::ResolveOrRejectPromise", mono_wasm_resolve_or_reject_promise);
	mono_add_internal_call ("Interop/Runtime::InvokeJSFunction", mono_wasm_invoke_js_function);
	mono_add_internal_call ("Interop/Runtime::CancelPromise", mono_wasm_cancel_promise);
	mono_add_internal_call ("Interop/Runtime::AssemblyGetEntryPoint", mono_wasm_assembly_get_entry_point);
	mono_add_internal_call ("Interop/Runtime::BindAssemblyExports", mono_wasm_bind_assembly_exports);
	mono_add_internal_call ("Interop/Runtime::GetAssemblyExport", mono_wasm_get_assembly_export);

	mono_add_internal_call ("Interop/JsGlobalization::ChangeCaseInvariant", mono_wasm_change_case_invariant);
	mono_add_internal_call ("Interop/JsGlobalization::ChangeCase", mono_wasm_change_case);
	mono_add_internal_call ("Interop/JsGlobalization::CompareString", mono_wasm_compare_string);
	mono_add_internal_call ("Interop/JsGlobalization::StartsWith", mono_wasm_starts_with);
	mono_add_internal_call ("Interop/JsGlobalization::EndsWith", mono_wasm_ends_with);
	mono_add_internal_call ("Interop/JsGlobalization::IndexOf", mono_wasm_index_of);
	mono_add_internal_call ("Interop/JsGlobalization::GetCalendarInfo", mono_wasm_get_calendar_info);
	mono_add_internal_call ("Interop/JsGlobalization::GetCultureInfo", mono_wasm_get_culture_info);
	mono_add_internal_call ("Interop/JsGlobalization::GetFirstDayOfWeek", mono_wasm_get_first_day_of_week);
	mono_add_internal_call ("Interop/JsGlobalization::GetFirstWeekOfYear", mono_wasm_get_first_week_of_year);
	mono_add_internal_call ("System.ConsolePal::Clear", mono_wasm_console_clear);
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

void mono_wasm_assembly_get_entry_point (char *assembly_name, int auto_insert_breakpoint, MonoMethod **method_out)
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

void mono_wasm_bind_assembly_exports (char *assembly_name)
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
					mono_wasm_trace_logger ("jsinterop", "critical", "mono_wasm_bind_assembly_exports unexpected double fault", 1, NULL);
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

void mono_wasm_get_assembly_export (char *assembly_name, char *namespace, char *classname, char *methodname, MonoMethod **method_out)
{
	MonoError error;
	MonoAssembly* assembly;
	MonoImage *image;
	MonoClass *klass;
	MonoMethod *method=NULL;
	*method_out = NULL;

	assert (assembly_name);
	assembly = _mono_wasm_assembly_load (assembly_name);
	assert (assembly);
	image = mono_assembly_get_image (assembly);
	assert (image);

	klass = mono_class_from_name (image, namespace, classname);
	assert (klass);
	method = mono_class_get_method_from_name (klass, methodname, -1);
	assert (method);

	*method_out = method;
	free (namespace);
	free (classname);
	free (methodname);
}

#ifndef DISABLE_THREADS

// async
void mono_wasm_release_cs_owned_object_post (pthread_t target_tid, int js_handle)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))mono_wasm_release_cs_owned_object, (gpointer)js_handle);
}

// async
void mono_wasm_resolve_or_reject_promise_post (pthread_t target_tid, void* args)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))mono_wasm_resolve_or_reject_promise, (gpointer)args);
}

// async
void mono_wasm_cancel_promise_post (pthread_t target_tid, int task_holder_gc_handle)
{
	mono_threads_wasm_async_run_in_target_thread_vi (target_tid, (void (*) (gpointer))mono_wasm_cancel_promise, (gpointer)task_holder_gc_handle);
}

// async
void mono_wasm_invoke_jsimport_async_post (pthread_t target_tid, void* signature, void* args)
{
	mono_threads_wasm_async_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))mono_wasm_invoke_jsimport, (gpointer)signature, (gpointer)args);
}

// sync
void mono_wasm_invoke_jsimport_sync_send (pthread_t target_tid, void* signature, void* args)
{
	mono_threads_wasm_sync_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))mono_wasm_invoke_jsimport, (gpointer)signature, (gpointer)args);
}

// sync
void mono_wasm_invoke_js_function_send (pthread_t target_tid, int function_js_handle, void *args)
{
	mono_threads_wasm_sync_run_in_target_thread_vii (target_tid, (void (*) (gpointer, gpointer))mono_wasm_invoke_js_function, (gpointer)function_js_handle, (gpointer)args);
}

#endif /* DISABLE_THREADS */
