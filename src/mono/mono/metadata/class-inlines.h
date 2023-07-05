/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_CLASS_INLINES_H__
#define __MONO_METADATA_CLASS_INLINES_H__

#include <mono/metadata/class-internals.h>
#include <mono/metadata/tabledefs.h>

static inline MonoType*
mono_get_void_type (void)
{
	return m_class_get_byval_arg (mono_defaults.void_class);
}

static inline MonoType*
mono_get_int32_type (void)
{
	return m_class_get_byval_arg (mono_defaults.int32_class);
}

static inline MonoType*
mono_get_int_type (void)
{
	return m_class_get_byval_arg (mono_defaults.int_class);
}

static inline MonoType*
mono_get_object_type (void)
{
	return m_class_get_byval_arg (mono_defaults.object_class);
}

static inline gboolean
mono_class_is_def (MonoClass *klass)
{
	return m_class_get_class_kind (klass) == MONO_CLASS_DEF;
}

static inline gboolean
m_class_is_array (MonoClass *klass)
{
	return m_class_get_rank (klass) != 0;
}

static inline gboolean
mono_class_is_gtd (MonoClass *klass)
{
	return m_class_get_class_kind (klass) == MONO_CLASS_GTD;
}

static inline gboolean
mono_class_is_ginst (MonoClass *klass)
{
	return m_class_get_class_kind (klass) == MONO_CLASS_GINST;
}

static inline gboolean
mono_class_is_gparam (MonoClass *klass)
{
	return m_class_get_class_kind (klass) == MONO_CLASS_GPARAM;
}

static inline gboolean
mono_class_is_array (MonoClass *klass)
{
	return m_class_get_class_kind (klass) == MONO_CLASS_ARRAY;
}

static inline gboolean
mono_class_is_pointer (MonoClass *klass)
{
	return m_class_get_class_kind (klass) == MONO_CLASS_POINTER;
}

static inline gboolean
mono_class_is_abstract (MonoClass *klass)
{
	return mono_class_get_flags (klass) & TYPE_ATTRIBUTE_ABSTRACT;
}

static inline gboolean
mono_class_is_interface (MonoClass *klass)
{
	return mono_class_get_flags (klass) & TYPE_ATTRIBUTE_INTERFACE;
}

static inline gboolean
mono_class_is_sealed (MonoClass *klass)
{
	return mono_class_get_flags (klass) & TYPE_ATTRIBUTE_SEALED;
}

static inline gboolean
mono_class_is_before_field_init (MonoClass *klass)
{
	return mono_class_get_flags (klass) & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT;
}

static inline gboolean
mono_class_is_auto_layout (MonoClass *klass)
{
	return (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT;
}

static inline gboolean
mono_class_is_explicit_layout (MonoClass *klass)
{
	return (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT;
}

static inline gboolean
mono_class_is_public (MonoClass *klass)
{
	return mono_class_get_flags (klass) & TYPE_ATTRIBUTE_PUBLIC;
}

static inline gboolean
mono_class_has_static_metadata (MonoClass *klass)
{
	return m_class_get_type_token (klass) && !m_class_get_image (klass)->dynamic && !mono_class_is_ginst (klass);
}

static inline gboolean
m_class_is_abstract (MonoClass *klass)
{
	return (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_ABSTRACT) != 0;
}

static inline gboolean
m_class_is_interface (MonoClass *klass)
{
	return MONO_CLASS_IS_INTERFACE_INTERNAL (klass);
}

static inline gboolean
m_class_is_gtd (MonoClass *klass)
{
	return mono_class_is_gtd (klass);
}

static inline gboolean
m_class_is_string (MonoClass *klass)
{
	return klass == mono_defaults.string_class;
}

static inline gboolean
m_class_is_primitive (MonoClass *klass)
{
	return mono_type_is_primitive (m_class_get_byval_arg (klass));
}

static inline gboolean
m_class_is_native_pointer (MonoClass *klass)
{
	MonoType *t = m_class_get_byval_arg (klass);

	return t->type == MONO_TYPE_PTR || t->type == MONO_TYPE_FNPTR;
}

static inline gboolean
m_class_is_nullable (MonoClass *klass)
{
	return mono_class_is_nullable (klass);
}

static inline MonoClass*
m_class_get_nullable_elem_class (MonoClass *klass)
{
	return m_class_get_cast_class (klass);
}

static inline gboolean
m_class_is_runtime_type (MonoClass *klass)
{
	return klass == mono_defaults.runtimetype_class;
}

static inline gboolean
m_class_is_auto_layout (MonoClass *klass)
{
	guint32 layout = (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK);

	return layout == TYPE_ATTRIBUTE_AUTO_LAYOUT;
}

static inline gboolean
m_class_is_sealed (MonoClass *klass)
{
	return mono_class_get_flags (klass) & TYPE_ATTRIBUTE_SEALED;
}

static inline gboolean
m_class_is_ginst (MonoClass *klass)
{
	return mono_class_is_ginst (klass);
}

static inline gboolean
m_class_is_private (MonoClass *klass)
{
	return (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_VISIBILITY_MASK) == TYPE_ATTRIBUTE_NOT_PUBLIC;
}

static inline gboolean
m_method_is_static (MonoMethod *method)
{
	return (method->flags & METHOD_ATTRIBUTE_STATIC) != 0;
}

static inline gboolean
m_method_is_virtual (MonoMethod *method)
{
	return (method->flags & METHOD_ATTRIBUTE_VIRTUAL) != 0;
}

static inline gboolean
m_method_is_abstract (MonoMethod *method)
{
        return (method->flags & METHOD_ATTRIBUTE_ABSTRACT) != 0;
}

static inline gboolean
m_method_is_final (MonoMethod *method)
{
        return (method->flags & METHOD_ATTRIBUTE_FINAL) != 0;
}

static inline gboolean
m_method_is_icall (MonoMethod *method)
{
	return (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) != 0;
}

static inline gboolean
m_method_is_synchronized (MonoMethod *method)
{
	return (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) != 0;
}

static inline gboolean
m_method_is_aggressive_inlining (MonoMethod *method)
{
	return (method->iflags & METHOD_IMPL_ATTRIBUTE_AGGRESSIVE_INLINING) != 0;
}

static inline gboolean
m_method_is_pinvoke (MonoMethod *method)
{
	return (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) != 0;
}

static inline gboolean
m_method_is_wrapper (MonoMethod *method)
{
	return method->wrapper_type != 0;
}

static inline void
m_field_set_parent (MonoClassField *field, MonoClass *klass)
{
	uintptr_t old_flags = m_field_get_meta_flags (field);
	field->parent_and_flags = ((uintptr_t)klass) | old_flags;
}

static inline void
m_field_set_meta_flags (MonoClassField *field, unsigned int flags)
{
	field->parent_and_flags |= (field->parent_and_flags & ~MONO_CLASS_FIELD_META_FLAG_MASK) | flags;
}

#endif
