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

static void
arch_return_value (MonoType *return_type, MonoObject *result, gpointer stack)
{
	gpointer resp, vt_resp;
	int type;

	resp = &result;
	vt_resp = (char *)result + sizeof (MonoObject);

	if (return_type->byref) {
		asm ("movl (%0),%%eax" : : "r" (resp) : "eax");
		return;
	}

	type = return_type->type;
 handle_enum:
	switch (type) {
	case MONO_TYPE_VOID:
		/* nothing to do */
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_CHAR:
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
		asm ("movl (%0),%%eax" : : "r" (vt_resp) : "eax");
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS: 
		asm ("movl (%0),%%eax" : : "r" (resp) : "eax");
		break;
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		asm ("movl 0(%0),%%eax;"
		     "movl 4(%0),%%edx" 
		     : : "r"(vt_resp)
		     : "eax", "edx");
		break;
	case MONO_TYPE_R4:
		asm ("fld (%0)" : : "r" (vt_resp) : "st", "st(1)" );
		break;
	case MONO_TYPE_R8:
		asm ("fldl (%0)" : : "r" (vt_resp) : "st", "st(1)" );
		break;
	case MONO_TYPE_VALUETYPE:
		if (return_type->data.klass->enumtype) {
			type = return_type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			memcpy (*((gpointer *)stack), vt_resp, 
				  mono_class_value_size (return_type->data.klass, NULL));
		}
		break;
	default:
		g_error ("type 0x%x not handled in remoting invoke", return_type->type);
		
	}
}

/**
 * arch_method_return_message_restore:
 * @method: method info
 * @stack: pointer to the stack arguments
 * @result: result to restore
 * @out_args: out arguments to restore
 *
 * Restore results from message based processing back to the stack.
 */
void
arch_method_return_message_restore (MonoMethod *method, gpointer stack, 
				    MonoObject *result, MonoArray *out_args)
{
	MonoMethodSignature *sig = method->signature;
	MonoClass *class;
	int i, j, type, size, align;
	char *cpos = stack;

	if (ISSTRUCT (sig->ret))
		cpos += 4;

	if (sig->hasthis)
		cpos += 4;
	
	for (i = 0, j = 0; i < sig->param_count; i++) {
		size = mono_type_stack_size (sig->params [i], &align);
		
		if (sig->params [i]->byref) {
			char *arg = mono_array_get (out_args, gpointer, j);
			type = sig->params [i]->type;
			class = mono_class_from_mono_type (sig->params [i]);
			
			switch (type) {
			case MONO_TYPE_VOID:
				g_assert_not_reached ();
				break;
			case MONO_TYPE_U1:
			case MONO_TYPE_I1:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_U2:
			case MONO_TYPE_I2:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_U4:
			case MONO_TYPE_I4:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
			case MONO_TYPE_VALUETYPE: {
				memcpy (*((gpointer *)cpos), arg + sizeof (MonoObject), size); 
				break;
			}
			case MONO_TYPE_STRING:
			case MONO_TYPE_CLASS: 
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
				*((MonoObject **)cpos) = (MonoObject *)arg;
				break;
			default:
				g_assert_not_reached ();
			}

			j++;
		}

		cpos += size;
	}

	arch_return_value (sig->ret, result, stack);
}

/**
 * arch_method_call_message_new:
 * @method: method info
 * @stack: pointer to the stack arguments
 *
 * Translates arguments on the stack into a Message.
 */

MonoMethodMessage *
arch_method_call_message_new (MonoMethod *method, gpointer stack, MonoMethod *invoke, 
			      MonoDelegate **cb, MonoObject **state)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethodSignature *sig = method->signature;
	MonoMethodMessage *msg;
	int i, count, type, size, align;
	char *cpos = stack;

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class); 
	
	if (invoke) {
		mono_message_init (domain, msg, mono_method_get_object (domain, invoke), NULL);
		count =  sig->param_count - 2;
	} else {
		mono_message_init (domain, msg, mono_method_get_object (domain, method), NULL);
		count =  sig->param_count;
	}
	/* the first argument is an implizit reference for valuetype 
	 * return values */
	if (!invoke && ISSTRUCT (sig->ret))
		cpos += 4;

	if (sig->hasthis)
		cpos += 4;


	for (i = 0; i < count; i++) {
		gpointer vpos;
		MonoClass *class;
		MonoObject *arg;

		size = mono_type_stack_size (sig->params [i], &align);

		if (sig->params [i]->byref)
			vpos = *((gpointer *)cpos);
		else 
			vpos = cpos;

		type = sig->params [i]->type;
		class = mono_class_from_mono_type (sig->params [i]);

		if (class->valuetype)
			arg = mono_value_box (domain, class, vpos);
		else 
			arg = *((MonoObject **)vpos);
		      
		mono_array_set (msg->args, gpointer, i, arg);
		cpos += size;
	}

	if (invoke) {
		*cb = *((MonoDelegate **)cpos);
		cpos += sizeof (MonoObject *);
		*state = *((MonoObject **)cpos);
	}
	
	return msg;
}

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

