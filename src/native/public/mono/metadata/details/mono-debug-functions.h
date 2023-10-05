// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(mono_bool, mono_debug_enabled, (void))

MONO_API_FUNCTION(void, mono_debug_init, (MonoDebugFormat format))
MONO_API_FUNCTION(void, mono_debug_open_image_from_memory, (MonoImage *image, const mono_byte *raw_contents, int size))
MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_debug_cleanup, (void))

MONO_API_FUNCTION(void, mono_debug_close_image, (MonoImage *image))

MONO_API_FUNCTION(void, mono_debug_domain_unload, (MonoDomain *domain))
MONO_API_FUNCTION(void, mono_debug_domain_create, (MonoDomain *domain))

MONO_API_FUNCTION(MonoDebugMethodAddress *, mono_debug_add_method, (MonoMethod *method, MonoDebugMethodJitInfo *jit, MonoDomain *domain))

MONO_API_FUNCTION(void, mono_debug_remove_method, (MonoMethod *method, MonoDomain *domain))

MONO_API_FUNCTION(MonoDebugMethodInfo *, mono_debug_lookup_method, (MonoMethod *method))

MONO_API_FUNCTION(MonoDebugMethodAddressList *, mono_debug_lookup_method_addresses, (MonoMethod *method))

MONO_API_FUNCTION(MonoDebugMethodJitInfo*, mono_debug_find_method, (MonoMethod *method, MonoDomain *domain))

MONO_API_FUNCTION(MonoDebugHandle *, mono_debug_get_handle, (MonoImage *image))

MONO_API_FUNCTION(void, mono_debug_free_method_jit_info, (MonoDebugMethodJitInfo *jit))


MONO_API_FUNCTION(void, mono_debug_add_delegate_trampoline, (void* code, int size))

MONO_API_FUNCTION(MonoDebugLocalsInfo*, mono_debug_lookup_locals, (MonoMethod *method))

MONO_API_FUNCTION(MonoDebugMethodAsyncInfo*, mono_debug_lookup_method_async_debug_info, (MonoMethod *method))

MONO_API_FUNCTION(MonoDebugSourceLocation *, mono_debug_method_lookup_location, (MonoDebugMethodInfo *minfo, int il_offset))

/*
 * Line number support.
 */

MONO_API_FUNCTION(MonoDebugSourceLocation *, mono_debug_lookup_source_location, (MonoMethod *method, uint32_t address, MonoDomain *domain))

MONO_API_FUNCTION(int32_t, mono_debug_il_offset_from_address, (MonoMethod *method, MonoDomain *domain, uint32_t native_offset))

MONO_API_FUNCTION(void, mono_debug_free_source_location, (MonoDebugSourceLocation *location))

MONO_API_FUNCTION(char *, mono_debug_print_stack_frame, (MonoMethod *method, uint32_t native_offset, MonoDomain *domain))

/*
 * Mono Debugger support functions
 *
 * These methods are used by the JIT while running inside the Mono Debugger.
 */

MONO_API_FUNCTION(int, mono_debugger_method_has_breakpoint, (MonoMethod *method))
MONO_API_FUNCTION(int, mono_debugger_insert_breakpoint, (const char *method_name, mono_bool include_namespace))

MONO_API_FUNCTION(void, mono_set_is_debugger_attached, (mono_bool attached))
MONO_API_FUNCTION(mono_bool, mono_is_debugger_attached, (void))
