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
#include <mono/cli/cli.h>
#include <mono/cli/object.h>

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
 * mono_object_new:
 * @image: Context where the type_token is hosted
 * @type_token: a token of the type that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @type_token in the @image image
 */
MonoObject *
mono_object_new (MonoImage *image, guint32 type_token)
{
	MonoClass *c;
	MonoObject *o;

	c = mono_class_get (image, type_token);
	o = mono_object_allocate (c->instance_size);
	o->klass = c;

	return o;
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
 * @etype: element type token
 * @n: number of array elements
 *
 * This routine creates a new szarray with @n elements of type @token
 */
MonoObject *
mono_new_szarray (MonoImage *image, guint32 etype, guint32 n)
{
	MonoClass *c;
	MonoObject *o;
	MonoArrayObject *ao;
	MonoArrayClass *ac;

	c = mono_array_class_get (image, etype, 1);
	g_assert (c != NULL);

	o = mono_object_allocate (c->instance_size);
	o->klass = c;

	ao = (MonoArrayObject *)o;
	ac = (MonoArrayClass *)c;

	ao->bounds = g_malloc0 (sizeof (MonoArrayBounds));
	ao->bounds [0].length = n;
	ao->bounds [0].lower_bound = 0;

	ao->vector = g_malloc0 (n * ac->esize);

	return o;
}

MonoObject *
mono_value_box (MonoImage *image, guint32 etype, gpointer val)
{
	MonoObject *res;
	int size;

	res = mono_object_new (image, etype);

	g_assert (res->klass->valuetype);

	size = res->klass->instance_size - sizeof (MonoObject);

	memcpy ((char *)res + sizeof (MonoObject), val, size);

	return res;
}

