/*
 * object.c: Object creation for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <stdlib.h>
#include <stdio.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>

/**
 * mono_object_allocate:
 * @size: number of bytes to allocate
 *
 * This is a very simplistic routine until we have our GC-aware
 * memory allocator. 
 *
 * Returns: an allocated object of size @size, or NULL on failure.
 */
static void *
mono_object_allocate (size_t size)
{
	void *o = calloc (1, size);

	return o;
}

/**
 * mono_object_free:
 *
 * Frees the memory used by the object.  Debugging purposes
 * only, as we will have our GC system.
 */
void
mono_object_free (MonoObject *o)
{
	MonoClass *c = o->klass;
	
	memset (o, 0, c->instance_size);
	free (o);
}

/**
 * mono_new_object:
 * @klass: the class of the object that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @klass
 */
MonoObject *
mono_new_object (MonoClass *klass)
{
	MonoObject *o;

	if (!klass->metadata_inited)
		mono_class_metadata_init (klass);

	o = mono_object_allocate (klass->instance_size);
	o->klass = klass;

	return o;
}

/**
 * mono_new_object_from_token:
 * @image: Context where the type_token is hosted
 * @token: a token of the type that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @token in the @image image
 */
MonoObject *
mono_new_object_from_token  (MonoImage *image, guint32 token)
{
	MonoClass *class;

	class = mono_class_get (image, token);

	return mono_new_object (class);
}


/**
 * mono_object_clone:
 * @obj: the object to clone
 *
 * Returns: A newly created object who is a shallow copy of @obj
 */
MonoObject *
mono_object_clone (MonoObject *obj)
{
	MonoObject *o;
	int size;
	
	size = obj->klass->instance_size;
	o = mono_object_allocate (size);
	
	memcpy (o, obj, size);

	return o;
}

/*
 * mono_new_szarray:
 * @image: image where the object is being referenced
 * @eclass: element class
 * @n: number of array elements
 *
 * This routine creates a new szarray with @n elements of type @token
 */
MonoObject *
mono_new_szarray (MonoClass *eclass, guint32 n)
{
	MonoClass *c;
	MonoObject *o;
	MonoArrayObject *ao;
	MonoArrayClass *ac;

	c = mono_array_class_get (eclass, 1);
	g_assert (c != NULL);

	o = mono_new_object (c);

	ao = (MonoArrayObject *)o;
	ac = (MonoArrayClass *)c;

	ao->bounds = g_malloc0 (sizeof (MonoArrayBounds));
	ao->bounds [0].length = n;
	ao->bounds [0].lower_bound = 0;

	ao->vector = g_malloc0 (n * mono_array_element_size (ac));

	return o;
}

/**
 * mono_new_utf16_string:
 * @text: a pointer to an utf16 string
 * @len: the length of the string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoObject *
mono_new_utf16_string (const char *text, gint32 len)
{
	MonoObject *s;
	MonoArrayObject *ca;

	s = mono_new_object (mono_defaults.string_class);
	g_assert (s != NULL);

	ca = (MonoArrayObject *)mono_new_szarray (mono_defaults.string_class, len);
	g_assert (ca != NULL);
	
	((MonoStringObject *)s)->c_str = ca;
	((MonoStringObject *)s)->length = len;

	memcpy (ca->vector, text, len * 2);

	return s;
}

/**
 * mono_new_string:
 * @text: a pointer to an utf8 string
 *
 * Returns: A newly created string object which contains @text.
 */
MonoObject *
mono_new_string (const char *text)
{
	MonoObject *o;
	guint16 *ut;
	int i, l;

	/* fixme: use some kind of unicode library here */

	l = strlen (text);
	ut = g_malloc (l*2);

	for (i = 0; i < l; i++)
		ut [i] = text[i];
	
	o = mono_new_utf16_string ((char *)ut, l);

	g_free (ut);

	return o;
}

/**
 * mono_value_box:
 * @class: the class of the value
 * @value: a pointer to the unboxed data
 *
 * Returns: A newly created object which contains @value.
 */
MonoObject *
mono_value_box (MonoClass *class, gpointer value)
{
	MonoObject *res;
	int size;

	g_assert (class->valuetype);

	size = mono_class_instance_size (class);
	res = mono_object_allocate (size);
	res->klass = class;

	size = size - sizeof (MonoObject);

	memcpy ((char *)res + sizeof (MonoObject), value, size);

	return res;
}

/**
 * mono_object_isinst:
 * @obj: an object
 * @klass: a pointer to a class 
 *
 * Returns: #TRUE if @obj is derived from @klass
 */
gboolean
mono_object_isinst (MonoObject *obj, MonoClass *klass)
{
	MonoClass *oklass = obj->klass;

	while (oklass) {
		if (oklass == klass)
			return TRUE;
		oklass = oklass->parent;
	}
	return FALSE;
}

