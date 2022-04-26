// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file does not have ifdef guards, it is meant to be included multiple times with different definitions of MONO_API_FUNCTION
#ifndef MONO_API_FUNCTION
#error "MONO_API_FUNCTION(ret,name,args) macro not defined before including function declaration header"
#endif

MONO_API_FUNCTION(MonoDomain*, mono_init, (const char *root_domain_name))

/**
 * This function is deprecated, use mono_init instead. Ignores filename parameter.
 */
MONO_API_FUNCTION(MonoDomain *, mono_init_from_assembly, (const char *root_domain_name, const char *filename))

/**
 * This function is deprecated, use mono_init instead. Ignores version parameter.
 */
MONO_API_FUNCTION(MonoDomain *, mono_init_version, (const char *root_domain_name, const char *version))

MONO_API_FUNCTION(MonoDomain*, mono_get_root_domain, (void))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_runtime_init, (MonoDomain *domain, MonoThreadStartCB start_cb, MonoThreadAttachCB attach_cb))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_runtime_cleanup, (MonoDomain *domain))

MONO_API_FUNCTION(void, mono_install_runtime_cleanup, (MonoDomainFunc func))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_runtime_quit, (void))

MONO_API_FUNCTION(void, mono_runtime_set_shutting_down, (void))

MONO_API_FUNCTION(mono_bool, mono_runtime_is_shutting_down, (void))

MONO_API_FUNCTION(const char*, mono_check_corlib_version, (void))

MONO_API_FUNCTION(MonoDomain *, mono_domain_create, (void))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoDomain *, mono_domain_create_appdomain, (char *friendly_name, char *configuration_file))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_domain_set_config, (MonoDomain *domain, const char *base_dir, const char *config_file_name))

MONO_API_FUNCTION(MonoDomain *, mono_domain_get, (void))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoDomain *, mono_domain_get_by_id, (int32_t domainid))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY int32_t, mono_domain_get_id, (MonoDomain *domain))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY const char *, mono_domain_get_friendly_name, (MonoDomain *domain))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY mono_bool, mono_domain_set, (MonoDomain *domain, mono_bool force))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_domain_set_internal, (MonoDomain *domain))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_domain_unload, (MonoDomain *domain))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_domain_try_unload, (MonoDomain *domain, MonoObject **exc))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY mono_bool, mono_domain_is_unloading, (MonoDomain *domain))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoDomain *, mono_domain_from_appdomain, (MonoAppDomain *appdomain))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_domain_foreach, (MonoDomainFunc func, void* user_data))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAssembly *, mono_domain_assembly_open, (MonoDomain *domain, const char *name))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_domain_ensure_entry_assembly, (MonoDomain *domain, MonoAssembly *assembly))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY mono_bool, mono_domain_finalize, (MonoDomain *domain, uint32_t timeout))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_domain_free, (MonoDomain *domain, mono_bool force))

MONO_API_FUNCTION(mono_bool, mono_domain_has_type_resolve, (MonoDomain *domain))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoReflectionAssembly *, mono_domain_try_type_resolve, (MonoDomain *domain, char *name, MonoObject *tb))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY mono_bool, mono_domain_owns_vtable_slot, (MonoDomain *domain, void* vtable_slot))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_context_init, (MonoDomain *domain))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY void, mono_context_set, (MonoAppContext *new_context))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoAppContext *, mono_context_get, (void))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY int32_t, mono_context_get_id, (MonoAppContext *context))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY int32_t, mono_context_get_domain_id, (MonoAppContext *context))

MONO_API_FUNCTION(MONO_RT_EXTERNAL_ONLY MonoJitInfo *, mono_jit_info_table_find, (MonoDomain *domain, void* addr))

/* MonoJitInfo accessors */

MONO_API_FUNCTION(void*, mono_jit_info_get_code_start, (MonoJitInfo* ji))

MONO_API_FUNCTION(int, mono_jit_info_get_code_size, (MonoJitInfo* ji))

MONO_API_FUNCTION(MonoMethod*, mono_jit_info_get_method, (MonoJitInfo* ji))


MONO_API_FUNCTION(MonoImage*, mono_get_corlib, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_object_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_byte_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_void_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_boolean_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_sbyte_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_int16_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_uint16_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_int32_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_uint32_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_intptr_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_uintptr_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_int64_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_uint64_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_single_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_double_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_char_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_string_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_enum_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_array_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_thread_class, (void))

MONO_API_FUNCTION(MonoClass*, mono_get_exception_class, (void))

MONO_API_FUNCTION(void, mono_security_enable_core_clr, (void))

MONO_API_FUNCTION(void, mono_security_set_core_clr_platform_callback, (MonoCoreClrPlatformCB callback))
