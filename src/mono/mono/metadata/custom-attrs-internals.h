/**
 * \file
 */

#ifndef __MONO_METADATA_CUSTOM_ATTRS_INTERNALS_H__
#define __MONO_METADATA_CUSTOM_ATTRS_INTERNALS_H__

#include <mono/metadata/object.h>
#include <mono/metadata/reflection.h>

MonoCustomAttrInfo*
mono_custom_attrs_from_builders (MonoImage *alloc_img, MonoImage *image, MonoArray *cattrs);

typedef gboolean (*MonoAssemblyMetadataCustomAttrIterFunc) (MonoImage *image, guint32 typeref_scope_token, const gchar* nspace, const gchar* name, guint32 method_token, gpointer user_data);

void
mono_assembly_metadata_foreach_custom_attr (MonoAssembly *assembly, MonoAssemblyMetadataCustomAttrIterFunc func, gpointer user_data);

gboolean
mono_assembly_is_weak_field (MonoImage *image, guint32 field_idx);

void
mono_assembly_init_weak_fields (MonoImage *image);

#endif  /* __MONO_METADATA_REFLECTION_CUSTOM_ATTRS_INTERNALS_H__ */
