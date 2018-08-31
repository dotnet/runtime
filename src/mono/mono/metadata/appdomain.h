/**
 * \file
 * AppDomain functions
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_APPDOMAIN_H_
#define _MONO_METADATA_APPDOMAIN_H_

#include <mono/utils/mono-publib.h>

#include <mono/utils/mono-forward.h>
#include <mono/metadata/object.h>
#include <mono/metadata/reflection.h>

MONO_BEGIN_DECLS

typedef void (*MonoThreadStartCB) (intptr_t tid, void* stack_start,
				   void* func);
typedef void (*MonoThreadAttachCB) (intptr_t tid, void* stack_start);

typedef struct _MonoAppDomain MonoAppDomain;

typedef void (*MonoDomainFunc) (MonoDomain *domain, void* user_data);

MONO_API MonoDomain*
mono_init                  (const char *filename);

MONO_API MonoDomain *
mono_init_from_assembly    (const char *domain_name, const char *filename);

MONO_API MonoDomain *
mono_init_version          (const char *domain_name, const char *version);

MONO_API MonoDomain*
mono_get_root_domain       (void);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_runtime_init          (MonoDomain *domain, MonoThreadStartCB start_cb,
			    MonoThreadAttachCB attach_cb);

MONO_API void
mono_runtime_cleanup       (MonoDomain *domain);

MONO_API void
mono_install_runtime_cleanup (MonoDomainFunc func);

MONO_API void
mono_runtime_quit (void);

MONO_API void
mono_runtime_set_shutting_down (void);

MONO_API mono_bool
mono_runtime_is_shutting_down (void);

MONO_API const char*
mono_check_corlib_version (void);

MONO_API MonoDomain *
mono_domain_create         (void);

MONO_API MONO_RT_EXTERNAL_ONLY MonoDomain *
mono_domain_create_appdomain (char *friendly_name, char *configuration_file);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_domain_set_config (MonoDomain *domain, const char *base_dir, const char *config_file_name);

MONO_API MonoDomain *
mono_domain_get            (void);

MONO_API MonoDomain *
mono_domain_get_by_id      (int32_t domainid);

MONO_API int32_t
mono_domain_get_id         (MonoDomain *domain);

MONO_API const char *
mono_domain_get_friendly_name (MonoDomain *domain);

MONO_API mono_bool
mono_domain_set            (MonoDomain *domain, mono_bool force);

MONO_API void
mono_domain_set_internal   (MonoDomain *domain);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_domain_unload (MonoDomain *domain);

MONO_API void
mono_domain_try_unload (MonoDomain *domain, MonoObject **exc);

MONO_API mono_bool
mono_domain_is_unloading   (MonoDomain *domain);

MONO_API MONO_RT_EXTERNAL_ONLY MonoDomain *
mono_domain_from_appdomain (MonoAppDomain *appdomain);

MONO_API void
mono_domain_foreach        (MonoDomainFunc func, void* user_data);

MONO_API MonoAssembly *
mono_domain_assembly_open  (MonoDomain *domain, const char *name);

MONO_API mono_bool
mono_domain_finalize       (MonoDomain *domain, uint32_t timeout);

MONO_API void
mono_domain_free           (MonoDomain *domain, mono_bool force);

MONO_API mono_bool
mono_domain_has_type_resolve (MonoDomain *domain);

MONO_API MONO_RT_EXTERNAL_ONLY MonoReflectionAssembly *
mono_domain_try_type_resolve (MonoDomain *domain, char *name, MonoObject *tb);

MONO_API mono_bool
mono_domain_owns_vtable_slot (MonoDomain *domain, void* vtable_slot);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_context_init 				   (MonoDomain *domain);

MONO_API MONO_RT_EXTERNAL_ONLY void
mono_context_set				   (MonoAppContext *new_context);

MONO_API MonoAppContext * 
mono_context_get				   (void);

MONO_API int32_t
mono_context_get_id         (MonoAppContext *context);

MONO_API int32_t
mono_context_get_domain_id  (MonoAppContext *context);

MONO_API MonoJitInfo *
mono_jit_info_table_find   (MonoDomain *domain, void* addr);

/* MonoJitInfo accessors */

MONO_API void*
mono_jit_info_get_code_start (MonoJitInfo* ji);

MONO_API int
mono_jit_info_get_code_size (MonoJitInfo* ji);

MONO_API MonoMethod*
mono_jit_info_get_method (MonoJitInfo* ji);


MONO_API MonoImage*
mono_get_corlib            (void);

MONO_API MonoClass*
mono_get_object_class      (void);

MONO_API MonoClass*
mono_get_byte_class        (void);

MONO_API MonoClass*
mono_get_void_class        (void);

MONO_API MonoClass*
mono_get_boolean_class     (void);

MONO_API MonoClass*
mono_get_sbyte_class       (void);

MONO_API MonoClass*
mono_get_int16_class       (void);

MONO_API MonoClass*
mono_get_uint16_class      (void);

MONO_API MonoClass*
mono_get_int32_class       (void);

MONO_API MonoClass*
mono_get_uint32_class      (void);

MONO_API MonoClass*
mono_get_intptr_class         (void);

MONO_API MonoClass*
mono_get_uintptr_class        (void);

MONO_API MonoClass*
mono_get_int64_class       (void);

MONO_API MonoClass*
mono_get_uint64_class      (void);

MONO_API MonoClass*
mono_get_single_class      (void);

MONO_API MonoClass*
mono_get_double_class      (void);

MONO_API MonoClass*
mono_get_char_class        (void);

MONO_API MonoClass*
mono_get_string_class      (void);

MONO_API MonoClass*
mono_get_enum_class        (void);

MONO_API MonoClass*
mono_get_array_class       (void);

MONO_API MonoClass*
mono_get_thread_class       (void);

MONO_API MonoClass*
mono_get_exception_class    (void);

MONO_API void
mono_security_enable_core_clr (void);

typedef mono_bool (*MonoCoreClrPlatformCB) (const char *image_name);

MONO_API void
mono_security_set_core_clr_platform_callback (MonoCoreClrPlatformCB callback);

MONO_END_DECLS

#endif /* _MONO_METADATA_APPDOMAIN_H_ */

