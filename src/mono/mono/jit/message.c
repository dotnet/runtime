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

	msg->method = mono_method_get_object (domain, method);

	msg->args = mono_array_new (domain, mono_defaults.object_class, sig->param_count);
	msg->arg_types = mono_array_new (domain, mono_defaults.byte_class, sig->param_count);

	names = g_new (char *, sig->param_count);
	mono_method_get_param_names (method, (const char **) names);
	msg->names = mono_array_new (domain, mono_defaults.string_class, sig->param_count);
	
	for (i = 0; i < sig->param_count; i++) {
		 name = mono_string_new (domain, names [i]);
		 mono_array_set (msg->names, gpointer, i, name);	
	}

	g_free (names);

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

		if (sig->params [i]->byref) {
			arg_type = 2;
			vpos = *((gpointer *)cpos);
			if (sig->params [i]->attrs & PARAM_ATTRIBUTE_IN)
				arg_type = 1;
		} else {
			vpos = cpos;
			arg_type = 1;
		}

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
		case MONO_TYPE_U:
		case MONO_TYPE_I:
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_VALUETYPE:
			arg = mono_value_box (domain, class, vpos);
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS: 
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			arg = *((MonoObject **)vpos);
			break;
		default:
			g_assert_not_reached ();
		}
       
		mono_array_set (msg->args, gpointer, i, arg);
		mono_array_set (msg->arg_types, guint8, i, arg_type);
		cpos += size;
	}
	
	return msg;
}
