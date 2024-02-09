/*
 * \file Definitions of getters for the fields of struct _MonoClass
 *
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/* No include guards - this file is meant to be included multiple times.
 * Before including the file define the following macros:
 * MONO_CLASS_GETTER(funcname, rettype, optref, argtype, fieldname)
 *
 * MONO_CLASS_OFFSET(funcname, argtype, fieldname)
 */

/* Accessors for _MonoClass fields. */
MONO_CLASS_GETTER(m_class_get_element_class, MonoClass *,  , MonoClass, element_class)
MONO_CLASS_GETTER(m_class_get_cast_class, MonoClass *,  , MonoClass, cast_class)
MONO_CLASS_GETTER(m_class_get_supertypes, MonoClass **, , MonoClass, supertypes)
MONO_CLASS_GETTER(m_class_get_idepth, guint16,  , MonoClass, idepth)
MONO_CLASS_GETTER(m_class_get_rank, guint8,  , MonoClass, rank)
MONO_CLASS_GETTER(m_class_get_instance_size, int, , MonoClass, instance_size)
MONO_CLASS_GETTER(m_class_is_inited, gboolean, , MonoClass, inited)
MONO_CLASS_GETTER(m_class_is_size_inited, gboolean, , MonoClass, size_inited)
MONO_CLASS_GETTER(m_class_is_valuetype, gboolean, , MonoClass, valuetype)
MONO_CLASS_GETTER(m_class_is_enumtype, gboolean, , MonoClass, enumtype)
MONO_CLASS_GETTER(m_class_is_blittable, gboolean, , MonoClass, blittable)
MONO_CLASS_GETTER(m_class_any_field_has_auto_layout, gboolean, , MonoClass, any_field_has_auto_layout)
MONO_CLASS_GETTER(m_class_is_unicode, gboolean, , MonoClass, unicode)
MONO_CLASS_GETTER(m_class_was_typebuilder, gboolean, , MonoClass, wastypebuilder)
MONO_CLASS_GETTER(m_class_is_array_special_interface, gboolean, , MonoClass, is_array_special_interface)
MONO_CLASS_GETTER(m_class_is_byreflike, gboolean, , MonoClass, is_byreflike)
MONO_CLASS_GETTER(m_class_is_inlinearray, gboolean, , MonoClass, is_inlinearray)
MONO_CLASS_GETTER(m_class_inlinearray_value, gint32, , MonoClass, inlinearray_value)
MONO_CLASS_GETTER(m_class_get_min_align, guint8, , MonoClass, min_align)
MONO_CLASS_GETTER(m_class_get_packing_size, guint, , MonoClass, packing_size)
MONO_CLASS_GETTER(m_class_is_ghcimpl, gboolean, , MonoClass, ghcimpl)
MONO_CLASS_GETTER(m_class_has_finalize, gboolean, , MonoClass, has_finalize)
MONO_CLASS_GETTER(m_class_is_delegate, gboolean, , MonoClass, delegate)
MONO_CLASS_GETTER(m_class_is_gc_descr_inited, gboolean, , MonoClass, gc_descr_inited)
MONO_CLASS_GETTER(m_class_has_cctor, gboolean,  , MonoClass, has_cctor)
MONO_CLASS_GETTER(m_class_has_references, gboolean, , MonoClass, has_references)
MONO_CLASS_GETTER(m_class_has_static_refs, gboolean, , MonoClass, has_static_refs)
MONO_CLASS_GETTER(m_class_has_no_special_static_fields, gboolean, , MonoClass, no_special_static_fields)
MONO_CLASS_GETTER(m_class_is_nested_classes_inited, gboolean, , MonoClass, nested_classes_inited)
MONO_CLASS_GETTER(m_class_get_class_kind, guint8, , MonoClass, class_kind)
MONO_CLASS_GETTER(m_class_is_interfaces_inited, gboolean, , MonoClass, interfaces_inited)
MONO_CLASS_GETTER(m_class_is_simd_type, gboolean, , MonoClass, simd_type)
MONO_CLASS_GETTER(m_class_is_has_finalize_inited, gboolean, , MonoClass, has_finalize_inited)
MONO_CLASS_GETTER(m_class_is_fields_inited, gboolean, , MonoClass, fields_inited)
MONO_CLASS_GETTER(m_class_has_failure, gboolean, , MonoClass, has_failure)
MONO_CLASS_GETTER(m_class_has_deferred_failure, gboolean, , MonoClass, has_deferred_failure)
MONO_CLASS_GETTER(m_class_has_weak_fields, gboolean, , MonoClass, has_weak_fields)
MONO_CLASS_GETTER(m_class_has_dim_conflicts, gboolean, , MonoClass, has_dim_conflicts)
MONO_CLASS_GETTER(m_class_get_parent, MonoClass *, , MonoClass, parent)
MONO_CLASS_GETTER(m_class_get_nested_in, MonoClass *, ,  MonoClass, nested_in)
MONO_CLASS_GETTER(m_class_get_image, MonoImage *, , MonoClass, image)
MONO_CLASS_GETTER(m_class_get_name, const char *, , MonoClass, name)
MONO_CLASS_GETTER(m_class_get_name_space, const char *, , MonoClass, name_space)
MONO_CLASS_GETTER(m_class_get_type_token, guint32, , MonoClass, type_token)
MONO_CLASS_GETTER(m_class_get_vtable_size, int, , MonoClass, vtable_size)
MONO_CLASS_GETTER(m_class_get_interface_count, guint16, , MonoClass, interface_count)
MONO_CLASS_GETTER(m_class_get_interface_id, guint32, , MonoClass, interface_id)
MONO_CLASS_GETTER(m_class_get_max_interface_id, guint32, , MonoClass, max_interface_id)
MONO_CLASS_GETTER(m_class_get_interface_offsets_count, guint16, , MonoClass, interface_offsets_count)
MONO_CLASS_GETTER(m_class_get_interfaces_packed, MonoClass **, , MonoClass, interfaces_packed)
MONO_CLASS_GETTER(m_class_get_interface_offsets_packed, guint16 *, , MonoClass, interface_offsets_packed)
MONO_CLASS_GETTER(m_class_get_interface_bitmap, guint8 *, , MonoClass, interface_bitmap)
MONO_CLASS_GETTER(m_class_get_interfaces, MonoClass **, , MonoClass, interfaces)
MONO_CLASS_GETTER(m_class_get_sizes, union _MonoClassSizes, , MonoClass, sizes)
MONO_CLASS_GETTER(m_class_get_fields, MonoClassField *, , MonoClass, fields)
MONO_CLASS_GETTER(m_class_get_methods, MonoMethod **, ,  MonoClass, methods)
MONO_CLASS_GETTER(m_class_get_this_arg, MonoType*, &, MonoClass, this_arg)
MONO_CLASS_GETTER(m_class_get_byval_arg, MonoType*, &, MonoClass, _byval_arg)
MONO_CLASS_GETTER(m_class_get_gc_descr, MonoGCDescriptor, , MonoClass, gc_descr)
MONO_CLASS_GETTER(m_class_get_runtime_vtable, MonoVTable*, , MonoClass, runtime_vtable)
MONO_CLASS_GETTER(m_class_get_vtable, MonoMethod **, , MonoClass, vtable)
MONO_CLASS_GETTER(m_class_get_infrequent_data, MonoPropertyBag*, &, MonoClass, infrequent_data)

/* Accessors for _MonoClassDef fields. */
MONO_CLASS_GETTER(m_classdef_get_klass, MonoClass*, &, MonoClassDef, klass)
MONO_CLASS_GETTER(m_classdef_get_flags, guint32, , MonoClassDef, flags)
MONO_CLASS_GETTER(m_classdef_get_first_method_idx, guint32, ,  MonoClassDef, first_method_idx)
MONO_CLASS_GETTER(m_classdef_get_first_field_idx, guint32, , MonoClassDef, first_field_idx)
MONO_CLASS_GETTER(m_classdef_get_method_count, guint32, ,  MonoClassDef, method_count)
MONO_CLASS_GETTER(m_classdef_get_field_count, guint32, ,  MonoClassDef, field_count)
MONO_CLASS_GETTER(m_classdef_get_next_class_cache, MonoClass **, &, MonoClassDef, next_class_cache)

/* Accessors for _MonoClassGtd fields. */
MONO_CLASS_GETTER(m_classgtd_get_klass, MonoClassDef*, &, MonoClassGtd, klass)
MONO_CLASS_GETTER(m_classgtd_get_generic_container, MonoGenericContainer*, , MonoClassGtd, generic_container)
MONO_CLASS_GETTER(m_classgtd_get_canonical_inst, MonoType*, &, MonoClassGtd, canonical_inst)

/* Accessors for _MonoClassGenericInst fields. */
MONO_CLASS_GETTER(m_classgenericinst_get_klass, MonoClass*, &, MonoClassGenericInst, klass)
MONO_CLASS_GETTER(m_classgenericinst_get_generic_class, MonoGenericClass*, , MonoClassGenericInst, generic_class)

/* Accessors for _MonoClassGenericParam fields. */
MONO_CLASS_GETTER(m_classgenericparam_get_klass, MonoClass*, &, MonoClassGenericParam, klass)

/* Accessors for _MonoClassArray fields. */
MONO_CLASS_GETTER(m_classarray_get_klass, MonoClass*, &, MonoClassArray, klass)
MONO_CLASS_GETTER(m_classarray_get_method_count, guint32, , MonoClassArray, method_count)

/* Accessors for _MonoClassPointer fields. */
MONO_CLASS_GETTER(m_classpointer_get_klass, MonoClass*, &, MonoClassPointer, klass)

MONO_CLASS_OFFSET(m_class_offsetof_interface_bitmap, MonoClass, interface_bitmap)
MONO_CLASS_OFFSET(m_class_offsetof_byval_arg, MonoClass, _byval_arg)
MONO_CLASS_OFFSET(m_class_offsetof_cast_class, MonoClass, cast_class)
MONO_CLASS_OFFSET(m_class_offsetof_element_class, MonoClass, element_class)
MONO_CLASS_OFFSET(m_class_offsetof_idepth, MonoClass, idepth)
MONO_CLASS_OFFSET(m_class_offsetof_instance_size, MonoClass, instance_size)
MONO_CLASS_OFFSET(m_class_offsetof_interface_id, MonoClass, interface_id)
MONO_CLASS_OFFSET(m_class_offsetof_max_interface_id, MonoClass, max_interface_id)
MONO_CLASS_OFFSET(m_class_offsetof_parent, MonoClass, parent)
MONO_CLASS_OFFSET(m_class_offsetof_rank, MonoClass, rank)
MONO_CLASS_OFFSET(m_class_offsetof_sizes, MonoClass, sizes)
MONO_CLASS_OFFSET(m_class_offsetof_supertypes, MonoClass, supertypes)
MONO_CLASS_OFFSET(m_class_offsetof_class_kind, MonoClass, class_kind)
