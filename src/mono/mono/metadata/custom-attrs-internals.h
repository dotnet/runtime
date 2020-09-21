/**
 * \file
 */

#ifndef __MONO_METADATA_CUSTOM_ATTRS_INTERNALS_H__
#define __MONO_METADATA_CUSTOM_ATTRS_INTERNALS_H__

#include <mono/metadata/object.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/reflection.h>

MonoCustomAttrInfo*
mono_custom_attrs_from_builders (MonoImage *alloc_img, MonoImage *image, MonoArray *cattrs);

typedef gboolean (*MonoAssemblyMetadataCustomAttrIterFunc) (MonoImage *image, guint32 typeref_scope_token, const gchar* nspace, const gchar* name, guint32 method_token, gpointer user_data);

void
mono_assembly_metadata_foreach_custom_attr (MonoAssembly *assembly, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data);

void
mono_class_metadata_foreach_custom_attr (MonoClass *klass, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data);

void
mono_method_metadata_foreach_custom_attr (MonoMethod *method, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data);

gboolean
mono_assembly_is_weak_field (MonoImage *image, guint32 field_idx);

void
mono_assembly_init_weak_fields (MonoImage *image);

void
mono_reflection_create_custom_attr_data_args (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len, MonoArrayHandleOut typed_args_out, MonoArrayHandleOut named_args_out, CattrNamedArg **named_arg_info, MonoError *error);

void
mono_reflection_create_custom_attr_data_args_noalloc (MonoImage *image, MonoMethod *method, const guchar *data, guint32 len,
													  gpointer **typed_args_out, gpointer **named_args_out, int *num_named_args,
													  CattrNamedArg **named_arg_info, MonoError *error);

#endif  /* __MONO_METADATA_REFLECTION_CUSTOM_ATTRS_INTERNALS_H__ */
