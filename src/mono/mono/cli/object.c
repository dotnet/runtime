/*
 * object.c: Object creation for the Mono runtime
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <mono/cli/object.h>

/**
 * mono_object_new:
 * @image: Context where the type_token is hosted
 * @type_token: a token of the type that we want to create
 *
 * Returns: A newly created object whose definition is
 * looked up using @type_token in the @image image
 */
void *
mono_object_new (MonoImage *image, guint32 type_token)
{
}
