// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <emscripten.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/class.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/jit/jit.h>

#include "wasm-config.h"
#include "gc-common.h"

//JS funcs
extern void mono_wasm_release_cs_owned_object (int js_handle);
extern void mono_wasm_resolve_or_reject_promise(void *data);

typedef void (*background_job_cb)(void);

#ifndef DISABLE_THREADS
extern void mono_wasm_install_js_worker_interop (int context_gc_handle);
extern void mono_wasm_uninstall_js_worker_interop ();
extern void mono_wasm_bind_cs_function(MonoString **fully_qualified_name, int signature_hash, void* signatures, int *is_exception, MonoObject **result);
extern void mono_wasm_invoke_import_async(void* args, void* signature);
extern void mono_wasm_invoke_import_sync(void* args, void* signature);
extern void mono_wasm_invoke_js_function(int function_js_handle, void *args);
#else
extern void mono_wasm_bind_cs_function(MonoString **fully_qualified_name, int signature_hash, void* signatures, int *is_exception, MonoObject **result);
extern void mono_wasm_bind_js_import(void *signature, int *is_exception, MonoObject **result);
extern void mono_wasm_invoke_js_import(int function_handle, void *args);
extern void mono_wasm_invoke_js_function(int function_js_handle, void *args);
#endif /* DISABLE_THREADS */

// HybridGlobalization
extern void mono_wasm_change_case_invariant(const uint16_t* src, int32_t srcLength, uint16_t* dst, int32_t dstLength, mono_bool bToUpper, int *is_exception, MonoObject** ex_result);
extern void mono_wasm_change_case(MonoString **culture, const uint16_t* src, int32_t srcLength, uint16_t* dst, int32_t dstLength, mono_bool bToUpper, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_compare_string(MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern mono_bool mono_wasm_starts_with(MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern mono_bool mono_wasm_ends_with(MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_index_of(MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, mono_bool fromBeginning, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_get_calendar_info(MonoString **culture, int32_t calendarId, const uint16_t* result, int32_t resultLength, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_get_culture_info(MonoString **culture, const uint16_t* result, int32_t resultLength, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_get_first_day_of_week(MonoString **culture, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_get_first_week_of_year(MonoString **culture, int *is_exception, MonoObject** ex_result);

void bindings_initialize_internals (void)
{
	mono_add_internal_call ("Interop/Runtime::ReleaseCSOwnedObject", mono_wasm_release_cs_owned_object);
	mono_add_internal_call ("Interop/Runtime::ResolveOrRejectPromise", mono_wasm_resolve_or_reject_promise);

#ifndef	ENABLE_JS_INTEROP_BY_VALUE
	mono_add_internal_call ("Interop/Runtime::RegisterGCRoot", mono_wasm_register_root);
	mono_add_internal_call ("Interop/Runtime::DeregisterGCRoot", mono_wasm_deregister_root);
#endif /* ENABLE_JS_INTEROP_BY_VALUE */

#ifndef DISABLE_THREADS
	mono_add_internal_call ("Interop/Runtime::InstallWebWorkerInterop", mono_wasm_install_js_worker_interop);
	mono_add_internal_call ("Interop/Runtime::UninstallWebWorkerInterop", mono_wasm_uninstall_js_worker_interop);
	mono_add_internal_call ("Interop/Runtime::BindCSFunction", mono_wasm_bind_cs_function);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportSync", mono_wasm_invoke_import_sync);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImportAsync", mono_wasm_invoke_import_async);
	mono_add_internal_call ("Interop/Runtime::InvokeJSFunction", mono_wasm_invoke_js_function);
#else
	mono_add_internal_call ("Interop/Runtime::BindCSFunction", mono_wasm_bind_cs_function);
	mono_add_internal_call ("Interop/Runtime::BindJSImport", mono_wasm_bind_js_import);
	mono_add_internal_call ("Interop/Runtime::InvokeJSImport", mono_wasm_invoke_js_import);
	mono_add_internal_call ("Interop/Runtime::InvokeJSFunction", mono_wasm_invoke_js_function);
#endif /* DISABLE_THREADS */

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
}