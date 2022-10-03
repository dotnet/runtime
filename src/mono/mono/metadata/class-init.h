/**
 * \file
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_CLASS_INIT_H__
#define __MONO_METADATA_CLASS_INIT_H__

#include <glib.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/class-internals.h>

MONO_COMPONENT_API gboolean
mono_class_init_internal (MonoClass *klass);

void
mono_classes_init (void);

MonoClass *
mono_class_create_from_typedef (MonoImage *image, guint32 type_token, MonoError *error);

MonoClass*
mono_class_create_generic_inst (MonoGenericClass *gclass);

MonoClass *
mono_class_create_bounded_array (MonoClass *element_class, uint32_t rank, mono_bool bounded);

MONO_COMPONENT_API MonoClass *
mono_class_create_array (MonoClass *element_class, uint32_t rank);

MONO_COMPONENT_API MonoClass *
mono_class_create_generic_parameter (MonoGenericParam *param);

MonoClass *
mono_class_create_ptr (MonoType *type);

MonoClass *
mono_class_create_fnptr (MonoMethodSignature *sig);

void
mono_class_setup_vtable_general (MonoClass *klass, MonoMethod **overrides, int onum, GList *in_setup);

void
mono_class_init_sizes (MonoClass *klass);

void
mono_class_setup_basic_field_info (MonoClass *klass);

void
mono_class_setup_fields (MonoClass *klass);

MONO_COMPONENT_API void
mono_class_setup_methods (MonoClass *klass);

void
mono_class_setup_properties (MonoClass *klass);

void
mono_class_setup_events (MonoClass *klass);

void
mono_class_layout_fields (MonoClass *klass, int base_instance_size, int packing_size, int real_size, gboolean sre);

void
mono_class_setup_interface_offsets (MonoClass *klass);

MONO_COMPONENT_API void
mono_class_setup_vtable (MonoClass *klass);

void
mono_class_setup_parent    (MonoClass *klass, MonoClass *parent);

void
mono_class_setup_mono_type (MonoClass *klass);

void
mono_class_setup_has_finalizer (MonoClass *klass);

void
mono_class_setup_nested_types (MonoClass *klass);

MonoClass *
mono_class_create_array_fill_type (void);

void
mono_class_set_runtime_vtable (MonoClass *klass, MonoVTable *vtable);

#endif
