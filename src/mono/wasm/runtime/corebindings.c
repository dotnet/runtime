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
extern void mono_wasm_bind_js_function(MonoString **function_name, MonoString **module_name, void *signature, int* function_js_handle, int *is_exception, MonoObject **result);
extern void mono_wasm_invoke_bound_function(int function_js_handle, void *data);
extern void mono_wasm_invoke_import(int fn_handle, void *data);
extern void mono_wasm_bind_cs_function(MonoString **fully_qualified_name, int signature_hash, void* signatures, int *is_exception, MonoObject **result);
extern void mono_wasm_marshal_promise(void *data);

typedef void (*background_job_cb)(void);
void mono_main_thread_schedule_background_job (background_job_cb cb);
void mono_current_thread_schedule_background_job (background_job_cb cb);

#ifndef DISABLE_LEGACY_JS_INTEROP
extern void mono_wasm_invoke_js_with_args_ref (int js_handle, MonoString **method, MonoArray **args, int *is_exception, MonoObject **result);
extern void mono_wasm_get_object_property_ref (int js_handle, MonoString **propertyName, int *is_exception, MonoObject **result);
extern void mono_wasm_set_object_property_ref (int js_handle, MonoString **propertyName, MonoObject **value, int createIfNotExist, int hasOwnProperty, int *is_exception, MonoObject **result);
extern void mono_wasm_get_by_index_ref (int js_handle, int property_index, int *is_exception, MonoObject **result);
extern void mono_wasm_set_by_index_ref (int js_handle, int property_index, MonoObject **value, int *is_exception, MonoObject **result);
extern void mono_wasm_get_global_object_ref (MonoString **global_name, int *is_exception, MonoObject **result);
extern void mono_wasm_typed_array_to_array_ref (int js_handle, int *is_exception, MonoObject **result);
extern void mono_wasm_create_cs_owned_object_ref (MonoString **core_name, MonoArray **args, int *is_exception, MonoObject** result);
extern void mono_wasm_typed_array_from_ref (int ptr, int begin, int end, int bytes_per_element, int type, int *is_exception, MonoObject** result);

// Blazor specific custom routines - see dotnet_support.js for backing code
extern void* mono_wasm_invoke_js_blazor (MonoString **exceptionMessage, void *callInfo, void* arg0, void* arg1, void* arg2);
#endif /* DISABLE_LEGACY_JS_INTEROP */

// HybridGlobalization
extern void mono_wasm_change_case_invariant(const uint16_t* src, int32_t srcLength, uint16_t* dst, int32_t dstLength, mono_bool bToUpper, int *is_exception, MonoObject** ex_result);
extern void mono_wasm_change_case(MonoString **culture, const uint16_t* src, int32_t srcLength, uint16_t* dst, int32_t dstLength, mono_bool bToUpper, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_compare_string(MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern mono_bool mono_wasm_starts_with(MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern mono_bool mono_wasm_ends_with(MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_index_of(MonoString **culture, const uint16_t* str1, int32_t str1Length, const uint16_t* str2, int32_t str2Length, int32_t options, mono_bool fromBeginning, int *is_exception, MonoObject** ex_result);
extern mono_bool mono_wasm_is_normalized(int32_t normalizationForm, MonoString **src, int *is_exception, MonoObject** ex_result);
extern int mono_wasm_normalize_string(int32_t normalizationForm, MonoString **src, uint16_t* dst, int32_t dstLength, int *is_exception, MonoObject** ex_result);

void bindings_initialize_internals (void)
{
	mono_add_internal_call ("Interop/Runtime::ReleaseCSOwnedObject", mono_wasm_release_cs_owned_object);
	mono_add_internal_call ("Interop/Runtime::BindJSFunction", mono_wasm_bind_js_function);
	mono_add_internal_call ("Interop/Runtime::InvokeJSFunction", mono_wasm_invoke_bound_function);
	mono_add_internal_call ("Interop/Runtime::InvokeImport", mono_wasm_invoke_import);
	mono_add_internal_call ("Interop/Runtime::BindCSFunction", mono_wasm_bind_cs_function);
	mono_add_internal_call ("Interop/Runtime::MarshalPromise", mono_wasm_marshal_promise);
	mono_add_internal_call ("Interop/Runtime::RegisterGCRoot", mono_wasm_register_root);
	mono_add_internal_call ("Interop/Runtime::DeregisterGCRoot", mono_wasm_deregister_root);
#ifndef DISABLE_LEGACY_JS_INTEROP
	// legacy
	mono_add_internal_call ("Interop/Runtime::InvokeJSWithArgsRef", mono_wasm_invoke_js_with_args_ref);
	mono_add_internal_call ("Interop/Runtime::GetObjectPropertyRef", mono_wasm_get_object_property_ref);
	mono_add_internal_call ("Interop/Runtime::SetObjectPropertyRef", mono_wasm_set_object_property_ref);
	mono_add_internal_call ("Interop/Runtime::GetByIndexRef", mono_wasm_get_by_index_ref);
	mono_add_internal_call ("Interop/Runtime::SetByIndexRef", mono_wasm_set_by_index_ref);
	mono_add_internal_call ("Interop/Runtime::GetGlobalObjectRef", mono_wasm_get_global_object_ref);
	mono_add_internal_call ("Interop/Runtime::TypedArrayToArrayRef", mono_wasm_typed_array_to_array_ref);
	mono_add_internal_call ("Interop/Runtime::CreateCSOwnedObjectRef", mono_wasm_create_cs_owned_object_ref);
	mono_add_internal_call ("Interop/Runtime::TypedArrayFromRef", mono_wasm_typed_array_from_ref);

	// Blazor specific custom routines - see dotnet_support.js for backing code
	mono_add_internal_call ("WebAssembly.JSInterop.InternalCalls::InvokeJS", mono_wasm_invoke_js_blazor);
#endif /* DISABLE_LEGACY_JS_INTEROP */
	mono_add_internal_call ("Interop/JsGlobalization::ChangeCaseInvariant", mono_wasm_change_case_invariant);
	mono_add_internal_call ("Interop/JsGlobalization::ChangeCase", mono_wasm_change_case);
	mono_add_internal_call ("Interop/JsGlobalization::CompareString", mono_wasm_compare_string);
	mono_add_internal_call ("Interop/JsGlobalization::StartsWith", mono_wasm_starts_with);
	mono_add_internal_call ("Interop/JsGlobalization::EndsWith", mono_wasm_ends_with);
	mono_add_internal_call ("Interop/JsGlobalization::IndexOf", mono_wasm_index_of);
	mono_add_internal_call ("Interop/JsGlobalization::IsNormalized", mono_wasm_is_normalized);
	mono_add_internal_call ("Interop/JsGlobalization::NormalizeString", mono_wasm_normalize_string);
}
