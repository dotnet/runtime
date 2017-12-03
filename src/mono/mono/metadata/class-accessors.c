/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/tabledefs.h>


typedef enum {
	PROP_MARSHAL_INFO = 1, /* MonoMarshalType */
	PROP_REF_INFO_HANDLE = 2, /* gchandle */
	PROP_EXCEPTION_DATA = 3, /* MonoErrorBoxed* */
	PROP_NESTED_CLASSES = 4, /* GList* */
	PROP_PROPERTY_INFO = 5, /* MonoClassPropertyInfo* */
	PROP_EVENT_INFO = 6, /* MonoClassEventInfo* */
	PROP_FIELD_DEF_VALUES = 7, /* MonoFieldDefaultValue* */
	PROP_DECLSEC_FLAGS = 8, /* guint32 */
	PROP_WEAK_BITMAP = 9
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
	return ((MonoClassGenericInst*)klass)->generic_class;
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
		return ((MonoClassGenericInst*)klass)->generic_class;
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
	switch (klass->class_kind) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		return ((MonoClassDef*)klass)->flags;
	case MONO_CLASS_GINST:
		return mono_class_get_flags (((MonoClassGenericInst*)klass)->generic_class->container_class);
	case MONO_CLASS_GPARAM:
		return TYPE_ATTRIBUTE_PUBLIC;
	case MONO_CLASS_ARRAY:
		/* all arrays are marked serializable and sealed, bug #42779 */
		return TYPE_ATTRIBUTE_CLASS | TYPE_ATTRIBUTE_SERIALIZABLE | TYPE_ATTRIBUTE_SEALED | TYPE_ATTRIBUTE_PUBLIC;
	case MONO_CLASS_POINTER:
		return TYPE_ATTRIBUTE_CLASS | (mono_class_get_flags (klass->element_class) & TYPE_ATTRIBUTE_VISIBILITY_MASK);
	}
	g_assert_not_reached ();
}

void
mono_class_set_flags (MonoClass *klass, guint32 flags)
{
	g_assert (klass->class_kind == MONO_CLASS_DEF || klass->class_kind == MONO_CLASS_GTD);
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

	return ((MonoClassGtd*)klass)->generic_container;
}

MonoGenericContainer*
mono_class_try_get_generic_container (MonoClass *klass)
{
	if (mono_class_is_gtd (klass))
		return ((MonoClassGtd*)klass)->generic_container;
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

	return ((MonoClassDef*)klass)->first_method_idx;
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

	g_assert (mono_class_has_static_metadata (klass));

	return ((MonoClassDef*)klass)->first_field_idx;
}

void
mono_class_set_first_field_idx (MonoClass *klass, guint32 idx)
{
	g_assert (mono_class_has_static_metadata (klass));

	((MonoClassDef*)klass)->first_field_idx = idx;
}

guint32
mono_class_get_method_count (MonoClass *klass)
{
	switch (klass->class_kind) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		return ((MonoClassDef*)klass)->method_count;
	case MONO_CLASS_GINST:
		return mono_class_get_method_count (((MonoClassGenericInst*)klass)->generic_class->container_class);
	case MONO_CLASS_GPARAM:
		return 0;
	case MONO_CLASS_ARRAY:
		return ((MonoClassArray*)klass)->method_count;
	case MONO_CLASS_POINTER:
		return 0;
	default:
		g_assert_not_reached ();
		return 0;
	}
}

void
mono_class_set_method_count (MonoClass *klass, guint32 count)
{
	switch (klass->class_kind) {
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
	default:
		g_assert_not_reached ();
		break;
	}
}

guint32
mono_class_get_field_count (MonoClass *klass)
{
	switch (klass->class_kind) {
	case MONO_CLASS_DEF:
	case MONO_CLASS_GTD:
		return ((MonoClassDef*)klass)->field_count;
	case MONO_CLASS_GINST:
		return mono_class_get_field_count (((MonoClassGenericInst*)klass)->generic_class->container_class);
	case MONO_CLASS_GPARAM:
	case MONO_CLASS_ARRAY:
	case MONO_CLASS_POINTER:
		return 0;
	default:
		g_assert_not_reached ();
		return 0;
	}
}

void
mono_class_set_field_count (MonoClass *klass, guint32 count)
{
	switch (klass->class_kind) {
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
	default:
		g_assert_not_reached ();
		break;
	}
}

MonoMarshalType*
mono_class_get_marshal_info (MonoClass *klass)
{
	return mono_property_bag_get (&klass->infrequent_data, PROP_MARSHAL_INFO);
}

void
mono_class_set_marshal_info (MonoClass *klass, MonoMarshalType *marshal_info)
{
	marshal_info->head.tag = PROP_MARSHAL_INFO;
	mono_property_bag_add (&klass->infrequent_data, marshal_info);
}

typedef struct {
	MonoPropertyBagItem head;
	guint32 value;
} Uint32Property;

guint32
mono_class_get_ref_info_handle (MonoClass *klass)
{
	Uint32Property *prop = mono_property_bag_get (&klass->infrequent_data, PROP_REF_INFO_HANDLE);
	return prop ? prop->value : 0;
}

guint32
mono_class_set_ref_info_handle (MonoClass *klass, guint32 value)
{
	if (!value) {
		Uint32Property *prop = mono_property_bag_get (&klass->infrequent_data, PROP_REF_INFO_HANDLE);
		if (prop)
			prop->value = 0;
		return 0;
	}

	Uint32Property *prop = mono_class_alloc (klass, sizeof (Uint32Property));
	prop->head.tag = PROP_REF_INFO_HANDLE;
	prop->value = value;
	prop = mono_property_bag_add (&klass->infrequent_data, prop);
	return prop->value;
}

typedef struct {
	MonoPropertyBagItem head;
	gpointer value;
} PointerProperty;

static void
set_pointer_property (MonoClass *klass, InfrequentDataKind property, gpointer value)
{
	PointerProperty *prop = mono_class_alloc (klass, sizeof (PointerProperty));
	prop->head.tag = property;
	prop->value = value;
	mono_property_bag_add (&klass->infrequent_data, prop);
}

static gpointer
get_pointer_property (MonoClass *klass, InfrequentDataKind property)
{
	PointerProperty *prop = (PointerProperty*)mono_property_bag_get (&klass->infrequent_data, property);
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
	return mono_property_bag_get (&klass->infrequent_data, PROP_PROPERTY_INFO);
}

void
mono_class_set_property_info (MonoClass *klass, MonoClassPropertyInfo *info)
{
	info->head.tag = PROP_PROPERTY_INFO;
	mono_property_bag_add (&klass->infrequent_data, info);
}

MonoClassEventInfo*
mono_class_get_event_info (MonoClass *klass)
{
	return mono_property_bag_get (&klass->infrequent_data, PROP_EVENT_INFO);
}

void
mono_class_set_event_info (MonoClass *klass, MonoClassEventInfo *info)
{
	info->head.tag = PROP_EVENT_INFO;
	mono_property_bag_add (&klass->infrequent_data, info);
}

MonoFieldDefaultValue*
mono_class_get_field_def_values (MonoClass *klass)
{
	return (MonoFieldDefaultValue*)get_pointer_property (klass, PROP_FIELD_DEF_VALUES);
}

void
mono_class_set_field_def_values (MonoClass *klass, MonoFieldDefaultValue *values)
{
	set_pointer_property (klass, PROP_FIELD_DEF_VALUES, values);
}

guint32
mono_class_get_declsec_flags (MonoClass *klass)
{
	Uint32Property *prop = mono_property_bag_get (&klass->infrequent_data, PROP_DECLSEC_FLAGS);
	return prop ? prop->value : 0;
}

void
mono_class_set_declsec_flags (MonoClass *klass, guint32 value)
{
	Uint32Property *prop = mono_class_alloc (klass, sizeof (Uint32Property));
	prop->head.tag = PROP_DECLSEC_FLAGS;
	prop->value = value;
	mono_property_bag_add (&klass->infrequent_data, prop);
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

MonoType*
mono_class_gtd_get_canonical_inst (MonoClass *klass)
{
	g_assert (mono_class_is_gtd (klass));
	return &((MonoClassGtd*)klass)->canonical_inst;
}

typedef struct {
	MonoPropertyBagItem head;

	int nbits;
	gsize *bits;
} WeakBitmapData;

void
mono_class_set_weak_bitmap (MonoClass *klass, int nbits, gsize *bits)
{
	WeakBitmapData *info = mono_class_alloc (klass, sizeof (WeakBitmapData));
	info->nbits = nbits;
	info->bits = bits;

	info->head.tag = PROP_WEAK_BITMAP;
	mono_property_bag_add (&klass->infrequent_data, info);
}

gsize*
mono_class_get_weak_bitmap (MonoClass *klass, int *nbits)
{
	WeakBitmapData *prop = mono_property_bag_get (&klass->infrequent_data, PROP_WEAK_BITMAP);

	g_assert (prop);
	*nbits = prop->nbits;
	return prop->bits;
}
