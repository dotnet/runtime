/*
 * exception.c: Exception handling
 *
 * Authors:
 *	Paolo Molaro (lupus@ximian.com)
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <mono/metadata/exception.h>
#include <mono/metadata/class.h>

static MonoExceptionClassInitFunc ex_init_class = NULL;
static MonoExceptionObjectInitFunc ex_init_obj = NULL;

void
mono_exception_install_handlers(MonoExceptionClassInitFunc class_init,
				MonoExceptionObjectInitFunc obj_init)
{
	ex_init_class = class_init;
	ex_init_obj = obj_init;
}

MonoObject*
mono_exception_from_name (MonoImage *image, const char *name_space,
			  const char *name)
{
	MonoClass *klass;
	MonoObject *o;

	klass = mono_class_from_name (image, name_space, name);

	o = mono_object_new (klass);
	g_assert (o != NULL);

	if(!klass->inited && ex_init_class) {
		ex_init_class(klass);
	}
	
	if(ex_init_obj) {
		ex_init_obj(o, klass);
	}

	return o;
}
