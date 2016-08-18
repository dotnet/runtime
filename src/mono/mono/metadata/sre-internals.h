/* 
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_SRE_INTERNALS_H__
#define __MONO_METADATA_SRE_INTERNALS_H__

#include <mono/metadata/object-internals.h>

typedef struct _ArrayMethod ArrayMethod;
typedef struct _GenericParamTableEntry GenericParamTableEntry;

void
mono_reflection_emit_init (void);

gpointer
mono_image_g_malloc0 (MonoImage *image, guint size);

gboolean
mono_is_sre_type_builder (MonoClass *klass);

gboolean
mono_is_sre_generic_instance (MonoClass *klass);

gboolean
mono_is_sre_method_on_tb_inst (MonoClass *klass);

gboolean
mono_is_sre_ctor_builder (MonoClass *klass);

gboolean
mono_is_sre_ctor_on_tb_inst (MonoClass *klass);

gboolean
mono_is_sr_field_on_inst (MonoClassField *field);

gboolean
mono_is_sr_mono_cmethod (MonoClass *klass);

gboolean
mono_is_sr_mono_property (MonoClass *klass);

gboolean
mono_reflection_create_generic_class (MonoReflectionTypeBuilder *tb, MonoError *error);

MonoMethod*
mono_reflection_method_builder_to_mono_method (MonoReflectionMethodBuilder *mb, MonoError *error);

MonoType*
mono_reflection_get_field_on_inst_generic_type (MonoClassField *field);

MonoMethod*
mono_reflection_method_on_tb_inst_get_handle (MonoReflectionMethodOnTypeBuilderInst *m, MonoError *error);

gpointer
mono_reflection_resolve_object (MonoImage *image, MonoObject *obj, MonoClass **handle_class, MonoGenericContext *context, MonoError *error);

void
mono_sre_array_method_free (ArrayMethod *am);

void
mono_sre_generic_param_table_entry_free (GenericParamTableEntry *entry);

#endif  /* __MONO_METADATA_SRE_INTERNALS_H__ */

