/*
 * message.c: stack <-> message translation 
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <string.h>

#include <mono/metadata/metadata.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>

#include "jit.h"

/**
 * mono_load_remote_field:
 * @this: pointer to an object
 * @klass: klass of the object containing @field
 * @field: the field to load
 * @res: a storage to store the result
 *
 * This method is called by the runtime on attempts to load fields of
 * transparent proxy objects. @this points to such TP, @klass is the class of
 * the object containing @field. @res is only a storage location which can be
 * used to store the result.
 *
 * Returns: an address pointing to the value of field.
 */
gpointer
mono_load_remote_field (MonoObject *this, MonoClass *klass, MonoClassField *field, gpointer *res)
{
	static MonoMethod *getter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;

	g_assert (this->vtable->klass == mono_defaults.transparent_proxy_class);

	if (!getter) {
		int i;

		for (i = 0; i < mono_defaults.object_class->method.count; ++i) {
			MonoMethod *cm = mono_defaults.object_class->methods [i];
	       
			if (!strcmp (cm->name, "FieldGetter")) {
				getter = cm;
				break;
			}
		}
		g_assert (getter);
	}
	
	field_class = mono_class_from_mono_type (field->type);

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	out_args = mono_array_new (domain, mono_defaults.object_class, 1);
	mono_message_init (domain, msg, mono_method_get_object (domain, getter), out_args);

	mono_array_set (msg->args, gpointer, 0, mono_string_new (domain, klass->name));
	mono_array_set (msg->args, gpointer, 1, mono_string_new (domain, field->name));

	mono_remoting_invoke ((MonoObject *)((MonoTransparentProxy *)this)->rp, msg, &exc, &out_args);

	*res = mono_array_get (out_args, MonoObject *, 0);

	if (field_class->valuetype) {
		return ((char *)*res) + sizeof (MonoObject);
	} else
		return res;
}

/**
 * mono_store_remote_field:
 * @this: pointer to an object
 * @klass: klass of the object containing @field
 * @field: the field to load
 * @val: the value/object to store
 *
 * This method is called by the runtime on attempts to store fields of
 * transparent proxy objects. @this points to such TP, @klass is the class of
 * the object containing @field. @val is the new value to store in @field.
 */
void
mono_store_remote_field (MonoObject *this, MonoClass *klass, MonoClassField *field, gpointer val)
{
	static MonoMethod *setter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;
	MonoObject *arg;

	g_assert (this->vtable->klass == mono_defaults.transparent_proxy_class);

	if (!setter) {
		int i;

		for (i = 0; i < mono_defaults.object_class->method.count; ++i) {
			MonoMethod *cm = mono_defaults.object_class->methods [i];
	       
			if (!strcmp (cm->name, "FieldSetter")) {
				setter = cm;
				break;
			}
		}
		g_assert (setter);
	}

	field_class = mono_class_from_mono_type (field->type);

	if (field_class->valuetype)
		arg = mono_value_box (domain, field_class, val);
	else 
		arg = *((MonoObject **)val);
		

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
	mono_message_init (domain, msg, mono_method_get_object (domain, setter), NULL);

	mono_array_set (msg->args, gpointer, 0, mono_string_new (domain, klass->name));
	mono_array_set (msg->args, gpointer, 1, mono_string_new (domain, field->name));
	mono_array_set (msg->args, gpointer, 2, arg);

	mono_remoting_invoke ((MonoObject *)((MonoTransparentProxy *)this)->rp, msg, &exc, &out_args);
}

