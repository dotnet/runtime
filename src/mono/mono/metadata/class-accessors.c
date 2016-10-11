#include <mono/metadata/class-internals.h>
#include <mono/metadata/tabledefs.h>


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
