/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class-abi-details.h>
#ifdef MONO_CLASS_DEF_PRIVATE
#include <mono/metadata/abi-details.h>
#define REALLY_INCLUDE_CLASS_DEF 1
#include <mono/metadata/class-private-definition.h>
#undef REALLY_INCLUDE_CLASS_DEF
#endif

typedef enum {
	PROP_MARSHAL_INFO = 1, /* MonoMarshalType */
	PROP_REF_INFO_HANDLE = 2, /* gchandle */
	PROP_EXCEPTION_DATA = 3, /* MonoErrorBoxed* */
	PROP_NESTED_CLASSES = 4, /* GList* */
	PROP_PROPERTY_INFO = 5, /* MonoClassPropertyInfo* */
	PROP_EVENT_INFO = 6, /* MonoClassEventInfo* */
	PROP_FIELD_DEF_VALUES = 7, /* MonoFieldDefaultValue* */
	PROP_DECLSEC_FLAGS = 8, /* guint32 */
	PROP_WEAK_BITMAP = 9,
	PROP_DIM_CONFLICTS = 10, /* GSList of MonoMethod* */
	PROP_FIELD_DEF_VALUES_2BYTESWIZZLE = 11, /* MonoFieldDefaultValue* with default values swizzled at 2 byte boundaries*/
	PROP_FIELD_DEF_VALUES_4BYTESWIZZLE = 12, /* MonoFieldDefaultValue* with default values swizzled at 4 byte boundaries*/
	PROP_FIELD_DEF_VALUES_8BYTESWIZZLE = 13, /* MonoFieldDefaultValue* with default values swizzled at 8 byte boundaries*/
	PROP_METADATA_UPDATE_INFO = 14, /* MonoClassMetadataUpdateInfo* */
}  InfrequentDataKind;

/* Accessors based on class kind*/

/*
* mono_class_get_generic_class:
*
*   Return the MonoGenericClass of @klass, which MUST be a generic instance.
*/
MonoGenericClass*
mono_class_get_generic_class (MonoClass *klass)
{
	g_assert (mono_class_is_ginst (klass));
	return m_classgenericinst_get_generic_class ((MonoClassGenericInst*)klass);
}

/*
* mono_class_try_get_generic_class:
*
*   Return the MonoGenericClass if @klass is a ginst, NULL otherwise
*/
MonoGenericClass*
mono_class_try_get_generic_class (MonoClass *klass)
{
	if (mono_class_is_ginst (klass))
		return m_classgenericinst_get_generic_class ((MonoClassGenericInst*)klass);
	return NULL;
}

/**
 * mono_class_get_flags:
 * \param klass the MonoClass to act on
 * \returns the \c TypeAttributes flags of \p klass.
 * See the \c TYPE_ATTRIBUTE_* definitions in \c tabledefs.h for the different values.
 */
guint32
mono_class_get_flags (MonoClass *klass)
{
	g_assert (klass);
	guint32 kind = m_class_get_class_kind (klass);
	switch (kind) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		return m_classdef_get_flags ((MonoClassDef*)klass);
	case MONO_CLASS_GINST:
		return mono_class_get_flags (m_classgenericinst_get_generic_class ((MonoClassGenericInst*)klass)->container_class);
	case MONO_CLASS_GPARAM:
		return TYPE_ATTRIBUTE_PUBLIC;
	case MONO_CLASS_ARRAY:
		/* all arrays are marked serializable and sealed, bug #42779 */
		return TYPE_ATTRIBUTE_CLASS | TYPE_ATTRIBUTE_SERIALIZABLE | TYPE_ATTRIBUTE_SEALED | TYPE_ATTRIBUTE_PUBLIC;
	case MONO_CLASS_POINTER:
		if (m_class_get_byval_arg (klass)->type == MONO_TYPE_FNPTR)
			return TYPE_ATTRIBUTE_SEALED | TYPE_ATTRIBUTE_PUBLIC;
		return TYPE_ATTRIBUTE_CLASS | (mono_class_get_flags (m_class_get_element_class (klass)) & TYPE_ATTRIBUTE_VISIBILITY_MASK);
	case MONO_CLASS_GC_FILLER:
		g_assertf (0, "%s: unexpected GC filler class", __func__);
		break;
	}
	g_assert_not_reached ();
}

void
mono_class_set_flags (MonoClass *klass, guint32 flags)
{
	g_assert (m_class_get_class_kind (klass) == MONO_CLASS_DEF || m_class_get_class_kind (klass) == MONO_CLASS_GTD);
	((MonoClassDef*)klass)->flags = flags;
}

/*
 * mono_class_get_generic_container:
 *
 *   Return the generic container of KLASS which should be a generic type definition.
 */
MonoGenericContainer*
mono_class_get_generic_container (MonoClass *klass)
{
	g_assert (mono_class_is_gtd (klass));

	return m_classgtd_get_generic_container ((MonoClassGtd*)klass);
}

MonoGenericContainer*
mono_class_try_get_generic_container (MonoClass *klass)
{
	if (mono_class_is_gtd (klass))
		return m_classgtd_get_generic_container ((MonoClassGtd*)klass);
	return NULL;
}

void
mono_class_set_generic_container (MonoClass *klass, MonoGenericContainer *container)
{
	g_assert (mono_class_is_gtd (klass));

	((MonoClassGtd*)klass)->generic_container = container;
}

/*
 * mono_class_get_first_method_idx:
 *
 *   Return the table index of the first method for metadata classes.
 */
guint32
mono_class_get_first_method_idx (MonoClass *klass)
{
	g_assert (mono_class_has_static_metadata (klass));

	return m_classdef_get_first_method_idx ((MonoClassDef*)klass);
}

void
mono_class_set_first_method_idx (MonoClass *klass, guint32 idx)
{
	g_assert (mono_class_has_static_metadata (klass));

	((MonoClassDef*)klass)->first_method_idx = idx;
}

guint32
mono_class_get_first_field_idx (MonoClass *klass)
{
	if (mono_class_is_ginst (klass))
		return mono_class_get_first_field_idx (mono_class_get_generic_class (klass)->container_class);

	g_assert (klass->type_token && !mono_class_is_ginst (klass));

	return m_classdef_get_first_field_idx ((MonoClassDef*)klass);
}

void
mono_class_set_first_field_idx (MonoClass *klass, guint32 idx)
{
	g_assert (klass->type_token && !mono_class_is_ginst (klass));

	((MonoClassDef*)klass)->first_field_idx = idx;
}

guint32
mono_class_get_method_count (MonoClass *klass)
{
	switch (m_class_get_class_kind (klass)) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		return m_classdef_get_method_count ((MonoClassDef*)klass);
	case MONO_CLASS_GINST:
		return mono_class_get_method_count (m_classgenericinst_get_generic_class ((MonoClassGenericInst*)klass)->container_class);
	case MONO_CLASS_GPARAM:
		return 0;
	case MONO_CLASS_ARRAY:
		return m_classarray_get_method_count ((MonoClassArray*)klass);
	case MONO_CLASS_POINTER:
		return 0;
	case MONO_CLASS_GC_FILLER:
		g_assertf (0, "%s: unexpected GC filler class", __func__);
		return 0;
	default:
		g_assert_not_reached ();
		return 0;
	}
}

void
mono_class_set_method_count (MonoClass *klass, guint32 count)
{
	switch (m_class_get_class_kind (klass)) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		((MonoClassDef*)klass)->method_count = count;
		break;
	case MONO_CLASS_GINST:
		break;
	case MONO_CLASS_GPARAM:
	case MONO_CLASS_POINTER:
		g_assert (count == 0);
		break;
	case MONO_CLASS_ARRAY:
		((MonoClassArray*)klass)->method_count = count;
		break;
	case MONO_CLASS_GC_FILLER:
		g_assertf (0, "%s: unexpected GC filler class", __func__);
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

guint32
mono_class_get_field_count (MonoClass *klass)
{
	switch (m_class_get_class_kind (klass)) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		return m_classdef_get_field_count ((MonoClassDef*)klass);
	case MONO_CLASS_GINST:
		return mono_class_get_field_count (m_classgenericinst_get_generic_class ((MonoClassGenericInst*)klass)->container_class);
	case MONO_CLASS_GPARAM:
	case MONO_CLASS_ARRAY:
	case MONO_CLASS_POINTER:
		return 0;
	case MONO_CLASS_GC_FILLER:
		g_assertf (0, "%s: unexpected GC filler class", __func__);
		return 0;
	default:
		g_assert_not_reached ();
		return 0;
	}
}

void
mono_class_set_field_count (MonoClass *klass, guint32 count)
{
	switch (m_class_get_class_kind (klass)) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		((MonoClassDef*)klass)->field_count = count;
		break;
	case MONO_CLASS_GINST:
		break;
	case MONO_CLASS_GPARAM:
	case MONO_CLASS_ARRAY:
	case MONO_CLASS_POINTER:
		g_assert (count == 0);
		break;
	case MONO_CLASS_GC_FILLER:
		g_assertf (0, "%s: unexpected GC filler class", __func__);
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

MonoMarshalType*
mono_class_get_marshal_info (MonoClass *klass)
{
	return (MonoMarshalType*)mono_property_bag_get (m_class_get_infrequent_data (klass), PROP_MARSHAL_INFO);
}

void
mono_class_set_marshal_info (MonoClass *klass, MonoMarshalType *marshal_info)
{
	marshal_info->head.tag = PROP_MARSHAL_INFO;
	mono_property_bag_add (m_class_get_infrequent_data (klass), marshal_info);
}

typedef struct {
	MonoPropertyBagItem head;
	guint32 value;
} Uint32Property;

typedef struct {
	MonoPropertyBagItem head;
	MonoGCHandle value;
} GCHandleProperty;

MonoGCHandle
mono_class_get_ref_info_handle (MonoClass *klass)
{
	GCHandleProperty *prop = (GCHandleProperty*)mono_property_bag_get (m_class_get_infrequent_data (klass), PROP_REF_INFO_HANDLE);
	return prop ? prop->value : NULL;
}

MonoGCHandle
mono_class_set_ref_info_handle (MonoClass *klass, gpointer value)
{
	if (!value) {
		GCHandleProperty *prop = (GCHandleProperty*)mono_property_bag_get (m_class_get_infrequent_data (klass), PROP_REF_INFO_HANDLE);
		if (prop)
			prop->value = NULL;
		return NULL;
	}

	GCHandleProperty *prop = (GCHandleProperty*)mono_class_alloc (klass, sizeof (GCHandleProperty));
	prop->head.tag = PROP_REF_INFO_HANDLE;
	prop->value = value;
	prop = (GCHandleProperty*)mono_property_bag_add (m_class_get_infrequent_data (klass), prop);
	return prop->value;
}

typedef struct {
	MonoPropertyBagItem head;
	gpointer value;
} PointerProperty;

static void
set_pointer_property (MonoClass *klass, InfrequentDataKind property, gpointer value)
{
	PointerProperty *prop = (PointerProperty*)mono_class_alloc (klass, sizeof (PointerProperty));
	prop->head.tag = property;
	prop->value = value;
	mono_property_bag_add (m_class_get_infrequent_data (klass), prop);
}

static gpointer
get_pointer_property (MonoClass *klass, InfrequentDataKind property)
{
	PointerProperty *prop = (PointerProperty*)mono_property_bag_get (m_class_get_infrequent_data (klass), property);
	return prop ? prop->value : NULL;
}

MonoErrorBoxed*
mono_class_get_exception_data (MonoClass *klass)
{
	return (MonoErrorBoxed*)get_pointer_property (klass, PROP_EXCEPTION_DATA);
}

void
mono_class_set_exception_data (MonoClass *klass, MonoErrorBoxed *value)
{
	set_pointer_property (klass, PROP_EXCEPTION_DATA, value);
}

GList*
mono_class_get_nested_classes_property (MonoClass *klass)
{
	return (GList*)get_pointer_property (klass, PROP_NESTED_CLASSES);
}

void
mono_class_set_nested_classes_property (MonoClass *klass, GList *value)
{
	set_pointer_property (klass, PROP_NESTED_CLASSES, value);
}

MonoClassPropertyInfo*
mono_class_get_property_info (MonoClass *klass)
{
	return (MonoClassPropertyInfo*)mono_property_bag_get (m_class_get_infrequent_data (klass), PROP_PROPERTY_INFO);
}

void
mono_class_set_property_info (MonoClass *klass, MonoClassPropertyInfo *info)
{
	info->head.tag = PROP_PROPERTY_INFO;
	mono_property_bag_add (m_class_get_infrequent_data (klass), info);
}

MonoClassEventInfo*
mono_class_get_event_info (MonoClass *klass)
{
	return (MonoClassEventInfo*)mono_property_bag_get (m_class_get_infrequent_data (klass), PROP_EVENT_INFO);
}

void
mono_class_set_event_info (MonoClass *klass, MonoClassEventInfo *info)
{
	info->head.tag = PROP_EVENT_INFO;
	mono_property_bag_add (m_class_get_infrequent_data (klass), info);
}

MonoFieldDefaultValue*
mono_class_get_field_def_values (MonoClass *klass)
{
	return (MonoFieldDefaultValue*)get_pointer_property (klass, PROP_FIELD_DEF_VALUES);
}

MonoFieldDefaultValue*
mono_class_get_field_def_values_with_swizzle (MonoClass *klass, int swizzle)
{
	InfrequentDataKind dataKind = PROP_FIELD_DEF_VALUES;
	if (swizzle == 2)
		dataKind = PROP_FIELD_DEF_VALUES_2BYTESWIZZLE;
	else if (swizzle == 4)
		dataKind = PROP_FIELD_DEF_VALUES_4BYTESWIZZLE;
	else if (swizzle == 8)
		dataKind = PROP_FIELD_DEF_VALUES_8BYTESWIZZLE;
	return (MonoFieldDefaultValue*)get_pointer_property (klass, dataKind);
}


void
mono_class_set_field_def_values (MonoClass *klass, MonoFieldDefaultValue *values)
{
	set_pointer_property (klass, PROP_FIELD_DEF_VALUES, values);
}

void
mono_class_set_field_def_values_with_swizzle (MonoClass *klass, MonoFieldDefaultValue *values, int swizzle)
{
	InfrequentDataKind dataKind = PROP_FIELD_DEF_VALUES;
	if (swizzle == 2)
		dataKind = PROP_FIELD_DEF_VALUES_2BYTESWIZZLE;
	else if (swizzle == 4)
		dataKind = PROP_FIELD_DEF_VALUES_4BYTESWIZZLE;
	else if (swizzle == 8)
		dataKind = PROP_FIELD_DEF_VALUES_8BYTESWIZZLE;
	set_pointer_property (klass, dataKind, values);
}

guint32
mono_class_get_declsec_flags (MonoClass *klass)
{
	Uint32Property *prop = (Uint32Property*)mono_property_bag_get (m_class_get_infrequent_data (klass), PROP_DECLSEC_FLAGS);
	return prop ? prop->value : 0;
}

void
mono_class_set_declsec_flags (MonoClass *klass, guint32 value)
{
	Uint32Property *prop = (Uint32Property*)mono_class_alloc (klass, sizeof (Uint32Property));
	prop->head.tag = PROP_DECLSEC_FLAGS;
	prop->value = value;
	mono_property_bag_add (m_class_get_infrequent_data (klass), prop);
}

void
mono_class_set_is_com_object (MonoClass *klass)
{
#ifndef DISABLE_COM
	mono_loader_lock ();
	klass->is_com_object = 1;
	mono_loader_unlock ();
#endif
}

void
mono_class_set_is_simd_type (MonoClass *klass, gboolean is_simd)
{
	klass->simd_type = is_simd;
}

MonoType*
mono_class_gtd_get_canonical_inst (MonoClass *klass)
{
	g_assert (mono_class_is_gtd (klass));
	return m_classgtd_get_canonical_inst ((MonoClassGtd*)klass);
}

typedef struct {
	MonoPropertyBagItem head;

	int nbits;
	gsize *bits;
} WeakBitmapData;

void
mono_class_set_weak_bitmap (MonoClass *klass, int nbits, gsize *bits)
{
	WeakBitmapData *info = (WeakBitmapData *)mono_class_alloc (klass, sizeof (WeakBitmapData));
	info->nbits = nbits;
	info->bits = bits;

	info->head.tag = PROP_WEAK_BITMAP;
	mono_property_bag_add (m_class_get_infrequent_data (klass), info);
}

gsize*
mono_class_get_weak_bitmap (MonoClass *klass, int *nbits)
{
	WeakBitmapData *prop = (WeakBitmapData*)mono_property_bag_get (m_class_get_infrequent_data (klass), PROP_WEAK_BITMAP);

	g_assert (prop);
	*nbits = prop->nbits;
	return prop->bits;
}

gboolean
mono_class_has_dim_conflicts (MonoClass *klass)
{
	if (klass->has_dim_conflicts)
		return TRUE;

	if (mono_class_is_ginst (klass)) {
		MonoClass *gklass = mono_class_get_generic_class (klass)->container_class;

		return gklass->has_dim_conflicts;
	}

	return FALSE;
}

typedef struct {
	MonoPropertyBagItem head;

	GSList *data;
} DimConflictData;

void
mono_class_set_dim_conflicts (MonoClass *klass, GSList *conflicts)
{
	DimConflictData *info = (DimConflictData*)mono_class_alloc (klass, sizeof (DimConflictData));
	info->data = conflicts;

	g_assert (!mono_class_is_ginst (klass));

	info->head.tag = PROP_DIM_CONFLICTS;
	mono_property_bag_add (&klass->infrequent_data, info);
}

GSList*
mono_class_get_dim_conflicts (MonoClass *klass)
{
	if (mono_class_is_ginst (klass))
		return mono_class_get_dim_conflicts (mono_class_get_generic_class (klass)->container_class);

	DimConflictData *info = (DimConflictData*)mono_property_bag_get (&klass->infrequent_data, PROP_DIM_CONFLICTS);

	g_assert (info);
	return info->data;
}

/**
 * mono_class_set_failure:
 * \param klass class in which the failure was detected
 * \param ex_type the kind of exception/error to be thrown (later)
 * \param ex_data exception data (specific to each type of exception/error)
 *
 * Keep a detected failure informations in the class for later processing.
 * Note that only the first failure is kept.
 *
 * LOCKING: Acquires the loader lock.
 */
gboolean
mono_class_set_failure (MonoClass *klass, MonoErrorBoxed *boxed_error)
{
	g_assert (boxed_error != NULL);

	if (mono_class_has_failure (klass))
		return FALSE;

	mono_loader_lock ();
	klass->has_failure = 1;
	mono_class_set_exception_data (klass, boxed_error);
	mono_loader_unlock ();

	return TRUE;
}

/**
 * mono_class_set_nonblittable:
 * \param klass class which will be marked as not blittable.
 *
 * Mark \c klass as not blittable.
 *
 * LOCKING: Acquires the loader lock.
 */
void
mono_class_set_nonblittable (MonoClass *klass) {
	mono_loader_lock ();
	klass->blittable = FALSE;
	mono_loader_unlock ();
}

/**
 * mono_class_publish_gc_descriptor:
 * \param klass the \c MonoClass whose GC descriptor is to be set
 * \param gc_descr the GC descriptor for \p klass
 *
 * Sets the \c gc_descr_inited and \c gc_descr fields of \p klass.
 * \returns previous value of \c klass->gc_descr_inited
 *
 * LOCKING: Acquires the loader lock.
 */
gboolean
mono_class_publish_gc_descriptor (MonoClass *klass, MonoGCDescriptor gc_descr)
{
	gboolean ret;
	mono_loader_lock ();
	ret = klass->gc_descr_inited;
	klass->gc_descr = gc_descr;
	mono_memory_barrier ();
	klass->gc_descr_inited = TRUE;
	mono_loader_unlock ();
	return ret;
}

MonoClassMetadataUpdateInfo*
mono_class_get_metadata_update_info (MonoClass *klass)
{
	switch (m_class_get_class_kind (klass)) {
	case MONO_CLASS_GTD:
		return NULL;
	case MONO_CLASS_DEF:
		return (MonoClassMetadataUpdateInfo *)get_pointer_property (klass, PROP_METADATA_UPDATE_INFO);
	case MONO_CLASS_GINST:
	case MONO_CLASS_GPARAM:
	case MONO_CLASS_ARRAY:
	case MONO_CLASS_POINTER:
	case MONO_CLASS_GC_FILLER:
		return NULL;
	default:
		g_assert_not_reached ();
	}
}

/*
 * LOCKING: assumes the loader lock is held
 */
void
mono_class_set_metadata_update_info (MonoClass *klass, MonoClassMetadataUpdateInfo *value)
{
	switch (m_class_get_class_kind (klass)) {
	case MONO_CLASS_GTD:
		g_assertf (0, "%s: EnC metadata update info on generic types is not supported", __func__);
		break;
	case MONO_CLASS_DEF:
		set_pointer_property (klass, PROP_METADATA_UPDATE_INFO, value);
		return;
	case MONO_CLASS_GINST:
	case MONO_CLASS_GPARAM:
	case MONO_CLASS_POINTER:
	case MONO_CLASS_GC_FILLER:
		g_assert_not_reached ();
		break;
	default:
		g_assert_not_reached ();
	}
}

gboolean
mono_class_has_metadata_update_info (MonoClass *klass)
{
	switch (m_class_get_class_kind (klass)) {
	case MONO_CLASS_GTD:
		return FALSE;
	case MONO_CLASS_DEF:
		return get_pointer_property (klass, PROP_METADATA_UPDATE_INFO) != NULL;
	case MONO_CLASS_GINST:
	case MONO_CLASS_GPARAM:
	case MONO_CLASS_POINTER:
	case MONO_CLASS_GC_FILLER:
		return FALSE;
	default:
		g_assert_not_reached ();
	}
}


#ifdef MONO_CLASS_DEF_PRIVATE
#define MONO_CLASS_GETTER(funcname, rettype, optref, argtype, fieldname) rettype funcname (argtype *klass) { return optref klass-> fieldname ; }
#define MONO_CLASS_OFFSET(funcname, argtype, fieldname) intptr_t funcname (void) { return MONO_STRUCT_OFFSET (argtype, fieldname); }
#include "class-getters.h"
#undef MONO_CLASS_GETTER
#undef MONO_CLASS_OFFSET
#endif /* MONO_CLASS_DEF_PRIVATE */
