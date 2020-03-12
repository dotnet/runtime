/**
 * \file
 * Copyright 2014 Xamarin Inc
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_REFLECTION_INTERNALS_H__
#define __MONO_METADATA_REFLECTION_INTERNALS_H__

#include <mono/metadata/object-internals.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/class-internals.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>

/* Safely access System.Reflection.Assembly from native code */
TYPED_HANDLE_DECL (MonoReflectionAssembly)

/* Safely access System.Reflection.Emit.TypeBuilder from native code */
TYPED_HANDLE_DECL (MonoReflectionTypeBuilder)

MonoReflectionAssemblyHandle
mono_domain_try_type_resolve_name (MonoDomain *domain, MonoAssembly *assembly, MonoStringHandle name, MonoError *error);

#ifndef ENABLE_NETCORE
MonoReflectionAssemblyHandle
mono_domain_try_type_resolve_typebuilder (MonoDomain *domain, MonoReflectionTypeBuilderHandle typebuilder, MonoError *error);
#endif

MonoReflectionTypeBuilderHandle
mono_class_get_ref_info (MonoClass *klass);

gboolean
mono_reflection_parse_type_checked (char *name, MonoTypeNameParse *info, MonoError *error);

gboolean
mono_reflection_is_usertype (MonoReflectionTypeHandle ref);

MonoReflectionType*
mono_reflection_type_resolve_user_types (MonoReflectionType *type, MonoError *error);

MonoType *
mono_reflection_type_handle_mono_type (MonoReflectionTypeHandle ref_type, MonoError *error);

MonoType*
mono_reflection_get_type_checked (MonoAssemblyLoadContext *alc, MonoImage *rootimage, MonoImage* image, MonoTypeNameParse *info, gboolean ignorecase, gboolean search_mscorlib, gboolean *type_resolve, MonoError *error);

MonoType*
mono_reflection_type_from_name_checked (char *name, MonoAssemblyLoadContext *alc, MonoImage *image, MonoError *error);

guint32
mono_reflection_get_token_checked (MonoObjectHandle obj, MonoError *error);

MonoObject*
mono_custom_attrs_get_attr_checked (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass, MonoError *error);

MonoCustomAttrInfo*
mono_reflection_get_custom_attrs_info_checked (MonoObjectHandle obj, MonoError *error);

MonoArrayHandle
mono_reflection_get_custom_attrs_data_checked (MonoObjectHandle obj, MonoError *error);

MonoArrayHandle
mono_reflection_get_custom_attrs_by_type_handle (MonoObjectHandle obj, MonoClass *attr_klass, MonoError *error);

MonoArrayHandle
mono_reflection_get_custom_attrs_blob_checked (MonoReflectionAssembly *assembly, MonoObject *ctor, MonoArray *ctorArgs, MonoArray *properties, MonoArray *propValues, MonoArray *fields, MonoArray* fieldValues, MonoError *error);

MonoCustomAttrInfo*
mono_custom_attrs_from_index_checked    (MonoImage *image, uint32_t idx, gboolean ignore_missing, MonoError *error);
MonoCustomAttrInfo*
mono_custom_attrs_from_method_checked   (MonoMethod *method, MonoError *error);
MonoCustomAttrInfo*
mono_custom_attrs_from_class_checked   	(MonoClass *klass, MonoError *error);
MonoCustomAttrInfo*
mono_custom_attrs_from_assembly_checked	(MonoAssembly *assembly, gboolean ignore_missing, MonoError *error);
MonoCustomAttrInfo*
mono_custom_attrs_from_property_checked	(MonoClass *klass, MonoProperty *property, MonoError *error);
MonoCustomAttrInfo*
mono_custom_attrs_from_event_checked	(MonoClass *klass, MonoEvent *event, MonoError *error);
MonoCustomAttrInfo*
mono_custom_attrs_from_field_checked	(MonoClass *klass, MonoClassField *field, MonoError *error);
MonoCustomAttrInfo*
mono_custom_attrs_from_param_checked	(MonoMethod *method, uint32_t param, MonoError *error);


char*
mono_identifier_unescape_type_name_chars (char* identifier);

MonoImage *
mono_find_dynamic_image_owner (void *ptr);

MonoReflectionAssemblyHandle
mono_assembly_get_object_handle (MonoDomain *domain, MonoAssembly *assembly, MonoError *error);

MonoReflectionType*
mono_type_get_object_checked (MonoDomain *domain, MonoType *type, MonoError *error);

MonoReflectionTypeHandle
mono_type_get_object_handle (MonoDomain *domain, MonoType *type, MonoError *error);

MonoReflectionField*
mono_field_get_object_checked (MonoDomain *domain, MonoClass *klass, MonoClassField *field, MonoError *error);

MonoReflectionFieldHandle
mono_field_get_object_handle (MonoDomain *domain, MonoClass *klass, MonoClassField *field, MonoError *error);

MonoReflectionMethod*
mono_method_get_object_checked (MonoDomain *domain, MonoMethod *method, MonoClass *refclass, MonoError *error);

MonoReflectionMethodHandle
mono_method_get_object_handle (MonoDomain *domain, MonoMethod *method, MonoClass *refclass, MonoError *error);

MonoReflectionProperty*
mono_property_get_object_checked (MonoDomain *domain, MonoClass *klass, MonoProperty *property, MonoError *error);

MonoReflectionPropertyHandle
mono_property_get_object_handle (MonoDomain *domain, MonoClass *klass, MonoProperty *property, MonoError *error);

MonoReflectionEventHandle
mono_event_get_object_handle (MonoDomain *domain, MonoClass *klass, MonoEvent *event, MonoError *error);

MonoReflectionModuleHandle
mono_module_get_object_handle (MonoDomain *domain, MonoImage *image, MonoError *error);

MonoReflectionModuleHandle
mono_module_file_get_object_handle (MonoDomain *domain, MonoImage *image, int table_index, MonoError *error);

MonoReflectionMethodBodyHandle
mono_method_body_get_object_handle (MonoDomain *domain, MonoMethod *method, MonoError *error);

MonoClass *
mono_class_from_mono_type_handle (MonoReflectionTypeHandle h);

MonoMethod*
mono_runtime_get_caller_no_system_or_reflection (void);

MonoAssembly*
mono_runtime_get_caller_from_stack_mark (MonoStackCrawlMark *stack_mark);

void
mono_reflection_get_param_info_member_and_pos (MonoReflectionParameterHandle p, MonoObjectHandle member_impl, int *out_position);

#endif /* __MONO_METADATA_REFLECTION_INTERNALS_H__ */
