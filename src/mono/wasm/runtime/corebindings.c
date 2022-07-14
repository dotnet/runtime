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
extern void mono_wasm_invoke_js_with_args_ref (int js_handle, MonoString **method, MonoArray **args, int *is_exception, MonoObject **result);
extern void mono_wasm_get_object_property_ref (int js_handle, MonoString **propertyName, int *is_exception, MonoObject **result);
extern void mono_wasm_get_by_index_ref (int js_handle, int property_index, int *is_exception, MonoObject **result);
extern void mono_wasm_set_object_property_ref (int js_handle, MonoString **propertyName, MonoObject **value, int createIfNotExist, int hasOwnProperty, int *is_exception, MonoObject **result);
extern void mono_wasm_set_by_index_ref (int js_handle, int property_index, MonoObject **value, int *is_exception, MonoObject **result);
extern void mono_wasm_get_global_object_ref (MonoString **global_name, int *is_exception, MonoObject **result);
extern void mono_wasm_release_cs_owned_object (int js_handle);
extern void mono_wasm_create_cs_owned_object_ref (MonoString **core_name, MonoArray **args, int *is_exception, MonoObject** result);
extern void mono_wasm_typed_array_to_array_ref (int js_handle, int *is_exception, MonoObject **result);
extern void mono_wasm_typed_array_from_ref (int ptr, int begin, int end, int bytes_per_element, int type, int *is_exception, MonoObject** result);

extern void mono_wasm_bind_js_function(MonoString **function_name, MonoString **module_name, void *signature, int* function_js_handle, int *is_exception, MonoObject **result);
extern void mono_wasm_invoke_bound_function(int function_js_handle, void *data);
extern void mono_wasm_bind_cs_function(MonoString **fully_qualified_name, int signature_hash, void* signatures, int *is_exception, MonoObject **result);
extern void mono_wasm_marshal_promise(void *data);


void core_initialize_internals ()
{
	mono_add_internal_call ("Interop/Runtime::InvokeJSWithArgsRef", mono_wasm_invoke_js_with_args_ref);
	mono_add_internal_call ("Interop/Runtime::GetObjectPropertyRef", mono_wasm_get_object_property_ref);
	mono_add_internal_call ("Interop/Runtime::GetByIndexRef", mono_wasm_get_by_index_ref);
	mono_add_internal_call ("Interop/Runtime::SetObjectPropertyRef", mono_wasm_set_object_property_ref);
	mono_add_internal_call ("Interop/Runtime::SetByIndexRef", mono_wasm_set_by_index_ref);
	mono_add_internal_call ("Interop/Runtime::GetGlobalObjectRef", mono_wasm_get_global_object_ref);
	mono_add_internal_call ("Interop/Runtime::CreateCSOwnedObjectRef", mono_wasm_create_cs_owned_object_ref);
	mono_add_internal_call ("Interop/Runtime::ReleaseCSOwnedObject", mono_wasm_release_cs_owned_object);
	mono_add_internal_call ("Interop/Runtime::TypedArrayToArrayRef", mono_wasm_typed_array_to_array_ref);
	mono_add_internal_call ("Interop/Runtime::TypedArrayFromRef", mono_wasm_typed_array_from_ref);

	mono_add_internal_call ("Interop/Runtime::BindJSFunction", mono_wasm_bind_js_function);
	mono_add_internal_call ("Interop/Runtime::InvokeJSFunction", mono_wasm_invoke_bound_function);
	mono_add_internal_call ("Interop/Runtime::BindCSFunction", mono_wasm_bind_cs_function);
	mono_add_internal_call ("Interop/Runtime::MarshalPromise", mono_wasm_marshal_promise);
	mono_add_internal_call ("Interop/Runtime::RegisterGCRoot", mono_wasm_register_root);
	mono_add_internal_call ("Interop/Runtime::DeregisterGCRoot", mono_wasm_deregister_root);
}

// Int8Array 		| int8_t	| byte or SByte (signed byte)
// Uint8Array		| uint8_t	| byte or Byte (unsigned byte)
// Uint8ClampedArray| uint8_t	| byte or Byte (unsigned byte)
// Int16Array		| int16_t	| short (signed short)
// Uint16Array		| uint16_t	| ushort (unsigned short)
// Int32Array		| int32_t	| int (signed integer)
// Uint32Array		| uint32_t	| uint (unsigned integer)
// Float32Array		| float		| float
// Float64Array		| double	| double
// typed array marshalling
// Keep in sync with driver.c
#define MARSHAL_ARRAY_BYTE 10
#define MARSHAL_ARRAY_UBYTE 11
#define MARSHAL_ARRAY_UBYTE_C 12 // alias of MARSHAL_ARRAY_UBYTE
#define MARSHAL_ARRAY_SHORT 13
#define MARSHAL_ARRAY_USHORT 14
#define MARSHAL_ARRAY_INT 15
#define MARSHAL_ARRAY_UINT 16
#define MARSHAL_ARRAY_FLOAT 17
#define MARSHAL_ARRAY_DOUBLE 18

EMSCRIPTEN_KEEPALIVE void
mono_wasm_typed_array_new_ref (char *arr, int length, int size, int type, PPVOLATILE(MonoArray) result)
{
	MONO_ENTER_GC_UNSAFE;
	MonoClass * typeClass = mono_get_byte_class(); // default is Byte
	switch (type) {
	case MARSHAL_ARRAY_BYTE:
		typeClass = mono_get_sbyte_class();
		break;
	case MARSHAL_ARRAY_SHORT:
		typeClass = mono_get_int16_class();
		break;
	case MARSHAL_ARRAY_USHORT:
		typeClass = mono_get_uint16_class();
		break;
	case MARSHAL_ARRAY_INT:
		typeClass = mono_get_int32_class();
		break;
	case MARSHAL_ARRAY_UINT:
		typeClass = mono_get_uint32_class();
		break;
	case MARSHAL_ARRAY_FLOAT:
		typeClass = mono_get_single_class();
		break;
	case MARSHAL_ARRAY_DOUBLE:
		typeClass = mono_get_double_class();
		break;
	case MARSHAL_ARRAY_UBYTE:
	case MARSHAL_ARRAY_UBYTE_C:
		typeClass = mono_get_byte_class();
		break;
	default:
		printf ("Invalid marshal type %d in mono_wasm_typed_array_new", type);
		abort();
	}

	PVOLATILE(MonoArray) buffer;

	buffer = mono_array_new (mono_get_root_domain(), typeClass, length);
	memcpy(mono_array_addr_with_size(buffer, sizeof(char), 0), arr, length * size);

	store_volatile((PPVOLATILE(MonoObject))result, (MonoObject *)buffer);
	MONO_EXIT_GC_UNSAFE;
}

// TODO: Remove - no longer used? If not, convert to ref
EMSCRIPTEN_KEEPALIVE int
mono_wasm_unbox_enum (PVOLATILE(MonoObject) obj)
{
	if (!obj)
		return 0;

	int result = 0;
	MONO_ENTER_GC_UNSAFE;
	PVOLATILE(MonoType) type = mono_class_get_type (mono_object_get_class(obj));

	PVOLATILE(void) ptr = mono_object_unbox (obj);
	switch (mono_type_get_type(mono_type_get_underlying_type (type))) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
		result = *(unsigned char*)ptr;
		break;
	case MONO_TYPE_I2:
		result = *(short*)ptr;
		break;
	case MONO_TYPE_U2:
		result = *(unsigned short*)ptr;
		break;
	case MONO_TYPE_I4:
		result = *(int*)ptr;
		break;
	case MONO_TYPE_U4:
		result = *(unsigned int*)ptr;
		break;
	// WASM doesn't support returning longs to JS
	// case MONO_TYPE_I8:
	// case MONO_TYPE_U8:
	default:
		printf ("Invalid type %d to mono_unbox_enum\n", mono_type_get_type(mono_type_get_underlying_type (type)));
		break;
	}
	MONO_EXIT_GC_UNSAFE;
	return result;
}


