/*
 * message.c: stack <-> message translation 
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>

#include <mono/metadata/metadata.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>

#include "jit.h"
#include "message.h"

/**
 * mono_method_return_message_restore:
 * @method: method info
 * @stack: pointer to the stack arguments
 * @result: result to restore
 * @out_args: out arguments to restore
 *
 * Restore results from message based processing back to the stack.
 */
void
mono_method_return_message_restore (MonoMethod *method, gpointer stack, 
				    MonoObject *result, MonoArray *out_args)
{
	MonoMethodSignature *sig = method->signature;
	MonoClass *class;
	int i, j, type, size, align;
	gpointer resp, vt_resp;
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

	/* restore return value */

	resp = &result;
	vt_resp = (char *)result + sizeof (MonoObject);

	if (sig->ret->byref) {
		asm ("movl (%0),%%eax" : : "r" (resp) : "eax");
		return;
	}

	type = sig->ret->type;
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
		if (sig->ret->data.klass->enumtype) {
			type = sig->ret->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			memcpy (*((gpointer *)stack), vt_resp, 
				mono_class_value_size (sig->ret->data.klass, NULL));
		}
		break;
	default:
		g_error ("type 0x%x not handled in remoting invoke", sig->ret->type);

	}
}

/**
 * mono_method_call_message_new:
 * @method: method info
 * @stack: pointer to the stack arguments
 *
 * Translates arguments on the stack into a Message.
 */

MonoMethodMessage *
mono_method_call_message_new (MonoMethod *method, gpointer stack)
{
	MonoDomain *domain = mono_domain_get ();
	MonoMethodSignature *sig = method->signature;
	MonoMethodMessage *msg;
	MonoString *name;
	int i, type, size, align;
	char *cpos = stack;
	char **names;
	guint8 arg_type;

	msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class); 
	
	mono_message_init (domain, msg, mono_method_get_object (domain, method), NULL);

	/* the first argument is an implizit reference for valuetype 
	 * return values */
	if (ISSTRUCT (sig->ret))
		cpos += 4;

	if (sig->hasthis)
		cpos += 4;
	
	for (i = 0; i < sig->param_count; i++) {
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
	
	return msg;
}

gpointer
mono_load_remote_field (MonoObject *this, MonoClass *klass, MonoClassField *field)
{
	static MonoMethod *getter = NULL;
	MonoDomain *domain = mono_domain_get ();
	MonoClass *field_class;
	MonoMethodMessage *msg;
	MonoArray *out_args;
	MonoObject *exc;
	MonoObject *res;

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

	printf ("TEST %p %p %p %p \n", ((MonoTransparentProxy *)this)->rp, msg, &exc, &out_args);
	mono_remoting_invoke (((MonoTransparentProxy *)this)->rp, msg, &exc, &out_args);

	res = mono_array_get (out_args, MonoObject *, 0);

	if (field_class->valuetype) {
		printf ("XTEST %d\n", *((int *)(((char *)res) + sizeof (MonoObject))));
		return ((char *)res) + sizeof (MonoObject);
	} else
		return &res;
}
