// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
/**
 *
 * Private unstable APIs.
 *
 * WARNING: The declarations and behavior of functions in this header are NOT STABLE and can be modified or removed at
 * any time.
 *
 */
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_install_load_aot_data_hook, (MonoLoadAotDataFunc load_func, MonoFreeAotDataFunc free_func, void* user_data))

MONO_API_FUNCTION(int, monovm_initialize, (int propertyCount, const char **propertyKeys, const char **propertyValues))

MONO_API_FUNCTION(int, monovm_runtimeconfig_initialize, (MonovmRuntimeConfigArguments *arg, MonovmRuntimeConfigArgumentsCleanup cleanup_fn, void *user_data))

// The wrapper MonoCoreRuntimeProperties struct can be stack-allocated or freed, but the structs inside it _must_ be heap-allocated and never freed, as they are not copied to avoid extra allocations
MONO_API_FUNCTION(int, monovm_initialize_preparsed, (MonoCoreRuntimeProperties *parsed_properties, int propertyCount, const char **propertyKeys, const char **propertyValues))

//#ifdef HOST_WASM
MONO_API_FUNCTION(void, mono_wasm_install_get_native_to_interp_tramp, (MonoWasmGetNativeToInterpTramp cb))
//#endif
