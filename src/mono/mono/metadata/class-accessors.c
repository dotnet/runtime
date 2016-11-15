/*
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <mono/metadata/class-internals.h>
#include <mono/metadata/tabledefs.h>


enum InfrequentDataKind {
	PROP_MARSHAL_INFO = 1, /* MonoMarshalType */
	PROP_EXT = 2, /* MonoClassExt */
	PROP_REF_INFO_HANDLE = 3, /* gchandle */
};

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
 * @klass: the MonoClass to act on
 *
 * Return the TypeAttributes flags of @klass.
 * See the TYPE_ATTRIBUTE_* definitions on tabledefs.h for the different values.
 *
 * Returns: The type flags
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
mono_class_get_marshal_info (MonoClass *class)
{
	return mono_property_bag_get (&class->infrequent_data, PROP_MARSHAL_INFO);
}

void
mono_class_set_marshal_info (MonoClass *class, MonoMarshalType *marshal_info)
{
	marshal_info->head.tag = PROP_MARSHAL_INFO;
	mono_property_bag_add (&class->infrequent_data, marshal_info);
}

MonoClassExt*
mono_class_get_ext (MonoClass *class)
{
	return mono_property_bag_get (&class->infrequent_data, PROP_EXT);
}

void
mono_class_set_ext (MonoClass *class, MonoClassExt *ext)
{
	ext->head.tag = PROP_EXT;
	mono_property_bag_add (&class->infrequent_data, ext);
}

typedef struct {
	MonoPropertyBagItem head;
	guint32 value;
} Uint32Property;

guint32
mono_class_get_ref_info_handle (MonoClass *class)
{
	Uint32Property *prop = mono_property_bag_get (&class->infrequent_data, PROP_REF_INFO_HANDLE);
	return prop ? prop->value : 0;
}

guint32
mono_class_set_ref_info_handle (MonoClass *class, guint32 value)
{
	if (!value) {
		Uint32Property *prop = mono_property_bag_get (&class->infrequent_data, PROP_REF_INFO_HANDLE);
		if (prop)
			prop->value = 0;
		return 0;
	}

	Uint32Property *prop = mono_class_alloc (class, sizeof (Uint32Property));
	prop->head.tag = PROP_REF_INFO_HANDLE;
	prop->value = value;
	prop = mono_property_bag_add (&class->infrequent_data, prop);
	return prop->value;
}
