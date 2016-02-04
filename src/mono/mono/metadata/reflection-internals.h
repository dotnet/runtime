/* 
 * Copyright 2014 Xamarin Inc
 */
#ifndef __MONO_METADATA_REFLECTION_INTERNALS_H__
#define __MONO_METADATA_REFLECTION_INTERNALS_H__

#include <mono/metadata/reflection.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-error.h>

MonoObject*
mono_custom_attrs_get_attr_checked (MonoCustomAttrInfo *ainfo, MonoClass *attr_klass, MonoError *error);

char*
mono_identifier_unescape_type_name_chars (char* identifier);

MonoImage *
mono_find_dynamic_image_owner (void *ptr);

MonoReflectionType*
mono_type_get_object_checked (MonoDomain *domain, MonoType *type, MonoError *error);

MonoReflectionField*
mono_field_get_object_checked (MonoDomain *domain, MonoClass *klass, MonoClassField *field, MonoError *error);

MonoReflectionMethod*
mono_method_get_object_checked (MonoDomain *domain, MonoMethod *method, MonoClass *refclass, MonoError *error);

MonoReflectionModule*
mono_module_get_object_checked (MonoDomain *domain, MonoImage *image, MonoError *error);


#endif /* __MONO_METADATA_REFLECTION_INTERNALS_H__ */
