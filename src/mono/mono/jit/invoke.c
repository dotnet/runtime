/*
 * invoke.c: runtime invoke code
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/arch/x86/x86-codegen.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/profiler-private.h>

#include "jit.h"
#include "codegen.h"

/*
 * this returns a helper method to invoke a method with a user supplied
 * stack frame. The returned method has the following signature:
 * invoke_method_with_frame ((gpointer code, gpointer frame, int frame_size);
 */
static gpointer
get_invoke_method_with_frame (void)
{
	static guint8 *start;
	guint8 *code;

	if (start)
		return start;

	start = code = g_malloc (64);

	/* Prolog */
	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);

	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 16, 4);
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, X86_EAX);

	x86_push_membase (code, X86_EBP, 16);
	x86_push_membase (code, X86_EBP, 12);
	x86_lea_membase (code, X86_EAX, X86_ESP, 2*4);
	x86_push_reg (code, X86_EAX);
	x86_call_code (code, memcpy);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);

	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 8, 4);
	x86_call_reg (code, X86_EAX);

	x86_mov_reg_membase (code, X86_ECX, X86_EBP, 16, 4);
	x86_alu_reg_reg (code, X86_ADD, X86_ESP, X86_ECX);

	/* Epilog */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);
	x86_leave (code);
	x86_ret (code);
	
	g_assert ((code - start) < 64);

	return start;
}

/**
 * arch_runtime_invoke:
 * @method: the method to invoke
 * @obj: this pointer
 * @params: array of parameter values.
 *
 * TODO: very ugly piece of code. we should replace that with a method-specific 
 * trampoline (as suggested by Paolo).
 */
MonoObject*
arch_runtime_invoke (MonoMethod *method, void *obj, void **params)
{
	static guint64 (*invoke_int64) (gpointer code, gpointer frame, int frame_size) = NULL;
	static double (*invoke_double) (gpointer code, gpointer frame, int frame_size) = NULL;
	MonoObject *retval;
	MonoMethodSignature *sig; 
	int i, tmp, type, sp = 0;
	void *ret;
	int frame_size = 0;
	gpointer *frame;
	gpointer code;
	
	sig = method->signature;

	/* allocate ret object. */
	if (sig->ret->type == MONO_TYPE_VOID) {
		retval = NULL;
		ret = NULL;
	} else {
		MonoClass *klass = mono_class_from_mono_type (sig->ret);
		if (klass->valuetype) {
			retval = mono_object_new (mono_domain_get (), klass);
			ret = ((char*)retval) + sizeof (MonoObject);
		} else {
			ret = &retval;
		}
	}
   
	if (ISSTRUCT (sig->ret))
		frame_size += sizeof (gpointer);
	
	if (sig->hasthis) 
		frame_size += sizeof (gpointer);

	for (i = 0; i < sig->param_count; ++i) {
		int align;
		frame_size += mono_type_stack_size (sig->params [i], &align);
	}

	frame = alloca (frame_size);

	if (ISSTRUCT (sig->ret))
		frame [sp++] = ret;

	if (sig->hasthis) 
		frame [sp++] = obj;
		

	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			frame [sp++] = params [i];
			continue;
		}
		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			tmp = *(MonoBoolean*)params [i];
			frame [sp++] = (gpointer)tmp;			
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			tmp = *(gint16*)params [i];
			frame [sp++] = (gpointer)tmp;			
			break;
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			frame [sp++] = (gpointer)*(gint32*)params [i];
			break;
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			frame [sp++] = (gpointer)*(gint32*)params [i];
			frame [sp++] = (gpointer)*(((gint32*)params [i]) + 1);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				type = sig->params [i]->data.klass->enum_basetype->type;
				goto handle_enum;
			} else {
				g_warning ("generic valutype %s not handled in runtime invoke", sig->params [i]->data.klass->name);
			}
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:  
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			frame [sp++] = params [i];
			break;
		default:
			g_error ("type 0x%x not handled in arch_runtime_invoke", sig->params [i]->type);
		}
	}

	code = arch_compile_method (method);

	if (!invoke_int64)
		invoke_int64 = (gpointer)invoke_double = get_invoke_method_with_frame ();

	type = sig->ret->type;
handle_enum_2:
	switch (type) {
	case MONO_TYPE_VOID:
		invoke_int64 (code, frame, frame_size);		
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
	case MONO_TYPE_CLASS:  
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_STRING:
		*((guint32 *)ret) = invoke_int64 (code, frame, frame_size);		
		break;
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		*((guint64 *)ret) = invoke_int64 (code, frame, frame_size);		
		break;
	case MONO_TYPE_R4:
		*((float *)ret) = invoke_double (code, frame, frame_size);		
		break;
	case MONO_TYPE_R8:
		*((double *)ret) = invoke_double (code, frame, frame_size);		
		break;
	case MONO_TYPE_VALUETYPE:
		if (sig->ret->data.klass->enumtype) {
			type = sig->ret->data.klass->enum_basetype->type;
			goto handle_enum_2;
		} else { 
			invoke_int64 (code, frame, frame_size);	
		}
		break;
	default:
		g_error ("return type 0x%x not handled in arch_runtime_invoke", type);
	}

	return retval;
}

/**
 * arch_create_delegate_trampoline:
 * @delegate: pointer to a Delegate object
 *
 * This trampoline is called when we invoke delegates from unmanaged code
 */
static gpointer
arch_create_delegate_trampoline (MonoDelegate *delegate)
{
	MonoMethod *method, *invoke;
	MonoMethodSignature *sig;
	MonoClass *klass;
	guint8 *code, *start, *invoke_code;
	int i, align, arg_size = 0;;

	if (!delegate)
		return NULL;

	/* fixme: add the delegate to a scanned hash
	 * so that is is never destroyed, or store
	 * this wrapper inside a new field in the delegate */

	klass = ((MonoObject *)delegate)->vtable->klass;
	g_assert (klass->delegate);

	if (delegate->delegate_trampoline)
		return delegate->delegate_trampoline;

	method = delegate->method_info->method;
	sig = method->signature;

	if (sig->param_count) {
		for (i = 0; i < sig->param_count; ++i)
			arg_size += mono_type_stack_size (sig->params [i], &align);
	}

	invoke = 0;
	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == 'I' && 
		    !strcmp ("Invoke", klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == sig->param_count) {
			invoke = klass->methods [i];
		}
	}
	g_assert (invoke);
	invoke_code = arch_compile_method (invoke);

	/* fixme: when do we free this code ? */

	code = start = g_malloc (64 + arg_size);

	/* start of original frame */
	x86_lea_membase (code, X86_ECX, X86_ESP, 4);

	/* allocate stack frame */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, arg_size);

	/* fixme: mybe we need to transform char* to Strings */

	/* memcopy activation frame to the stack */
	x86_push_imm (code, arg_size);
	x86_push_reg (code, X86_ECX);
	x86_lea_membase (code, X86_ECX, X86_ESP, 8);
	x86_push_reg (code, X86_ECX);
	x86_call_code (code, memcpy);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);

	/* call delegate invoke */
	x86_push_imm (code, delegate);
	x86_call_code (code, invoke_code);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 4);

	x86_ret (code);

	delegate->delegate_trampoline = start;

	return start;
}

gpointer
arch_create_native_wrapper (MonoMethod *method)
{
	MonoMethodSignature *csig = method->signature;
	MonoJitInfo *ji;
	guint8 *code, *start;
	int i, align, locals = 0, arg_size = 0;
	gboolean pinvoke = FALSE;
	GList *free_list = NULL;
	gboolean end_invoke = FALSE;

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		pinvoke = TRUE;

	/* compute the size of the activation frame */

	if (csig->hasthis)
		arg_size += sizeof (gpointer);

	if (csig->ret->type == MONO_TYPE_VALUETYPE) {
		g_assert (!csig->ret->byref);
		arg_size += sizeof (gpointer);		
	}
		
	for (i = 0; i < csig->param_count; ++i) {
		arg_size += mono_type_stack_size (csig->params [i], &align);
		if (pinvoke && (csig->params [i]->type == MONO_TYPE_STRING))
			locals++;
		if (pinvoke && (csig->params [i]->type == MONO_TYPE_OBJECT) &&
		    csig->params [i]->data.klass->delegate)
			locals++;
	}

	start = code = g_malloc (512);

	if (mono_jit_profile) {
		x86_push_imm (code, method);
		x86_mov_reg_imm (code, X86_EAX, mono_profiler_method_enter);
		x86_call_reg (code, X86_EAX);
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
	}
	/* save LMF - the instruction pointer is already on the 
	 * stack (return address) */

	/* save all caller saved regs */
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);
	x86_push_reg (code, X86_EBP);

	/* save method info */
	x86_push_imm (code, method);
	
	/* get the address of lmf for the current thread */
	x86_call_code (code, arch_get_lmf_addr);
	/* push lmf */
	x86_push_reg (code, X86_EAX); 
	/* push *lfm (previous_lmf) */
	x86_push_membase (code, X86_EAX, 0);
	/* *(lmf) = ESP */
	x86_mov_membase_reg (code, X86_EAX, 0, X86_ESP, 4);

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		guint8 *l1, *l2;

		if (method->klass->parent == mono_defaults.multicastdelegate_class &&
		    *method->name == 'E' && !strcmp (method->name, "EndInvoke"))
			end_invoke = TRUE;

		if (arg_size) {
			/* repush all arguments */
			/* load argument size -4 into ECX */
			x86_mov_reg_imm (code, X86_ECX, (arg_size - 4));
			/* load source address */
			x86_lea_membase (code, X86_ESI, X86_ESP, sizeof (MonoLMF));
			/* allocate destination */
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, arg_size);
			/* load destination address */
			x86_mov_reg_reg (code, X86_EDI, X86_ESP, 4);

			l1 = code;
			x86_mov_reg_memindex (code, X86_EAX, X86_ESI, 0, X86_ECX, 0, 4);
			x86_mov_memindex_reg (code, X86_EDI, 0, X86_ECX, 0, X86_EAX, 4);
			x86_alu_reg_imm (code, X86_SUB, X86_ECX, 4);
			l2 = code;
			x86_branch8 (code, X86_CC_GEZ, l1 - (l2 + 2), FALSE); 
		}

	} else if (pinvoke) {
		int offset = arg_size + (locals <<2) + sizeof (MonoLMF) - 4;
		int l = 0;
		
		/* allocate locals */
		if (locals) {
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, (locals<<2));
			x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
		}

		for (i = csig->param_count - 1; i >= 0; i--) {
			MonoType *t = csig->params [i];
			int type;

			if (t->byref) {
				x86_push_membase (code, X86_ESP, offset);
				continue;
			}

			type = t->type;
enum_marshal:
			switch (type) {
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_PTR:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_TYPEDBYREF:
			case MONO_TYPE_R4:
				x86_push_membase (code, X86_ESP, offset);
				break;
			case MONO_TYPE_FNPTR:
				/* fixme: dont know when this is used */
				g_assert_not_reached ();
				break;
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
				if (t->data.klass->delegate) {
					x86_push_membase (code, X86_ESP, offset);
					x86_call_code (code, arch_create_delegate_trampoline);
					x86_mov_membase_reg (code, X86_ESP, 0, X86_EAX, 4);
				} else
					x86_push_membase (code, X86_ESP, offset);
				break;
			case MONO_TYPE_STRING:
				x86_push_membase (code, X86_ESP, offset);
				x86_call_code (code, mono_string_to_utf8);
				x86_mov_membase_reg (code, X86_ESP, 0, X86_EAX, 4);
				free_list = g_list_prepend (free_list, (gpointer)l);
				x86_mov_membase_reg (code, X86_EBP, l, X86_EAX, 4);
				l+= 4;
				break;
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_R8:
				x86_push_membase (code, X86_ESP, offset);
				x86_push_membase (code, X86_ESP, offset);
				break;
			case MONO_TYPE_VALUETYPE:
				if (t->data.klass->enumtype) {
					type = t->data.klass->enum_basetype->type;
					goto enum_marshal;
				} else {
					int j, size;
					size = mono_type_stack_size (csig->params [i], &align);
					size = size >> 2;
					for (j = 0; j < size; j++)
						x86_push_membase (code, X86_ESP, offset);
				}
				break;
			default:
				g_error ("type 0x%02x unknown", t->type);

			}
		}			

		if (csig->ret->type == MONO_TYPE_VALUETYPE) {
			g_assert (!csig->ret->byref);
			x86_push_membase (code, X86_ESP, offset);
		}

		if (csig->hasthis) {
			x86_push_membase (code, X86_ESP, offset);
		}

	} else {
		g_assert_not_reached ();
	}

	if (method->addr) {
		/* special case EndInvoke - we pass the MonoMethod as first parameter */
		if (end_invoke)
			x86_push_imm (code, method);
		/* call the native code */
		x86_call_code (code, method->addr);
	} else {
		/* raise exception */
		x86_push_imm (code, "NotImplementedException");              
		x86_call_code (code, arch_get_throw_exception_by_name ());
	}

	/* free pinvoke string args */
	if (free_list) {
		GList *l;

		x86_push_reg (code, X86_EAX);
		x86_push_reg (code, X86_EDX);
		
		for (l = free_list; l; l = l->next) {
			x86_push_membase (code, X86_EBP, ((int)l->data));
			x86_call_code (code, g_free);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
		}

		x86_pop_reg (code, X86_EDX);
		x86_pop_reg (code, X86_EAX);

		g_list_free (free_list);
	}

	/* remove arguments from stack */
	if (arg_size || locals || end_invoke)
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + (locals<<2) + 
				 (end_invoke ? 4 : 0));

	if (pinvoke && !csig->ret->byref && (csig->ret->type == MONO_TYPE_STRING)) {
		/* If the argument is non-null, then convert the value back */
		x86_alu_reg_reg (code, X86_OR, X86_EAX, X86_EAX);
		x86_branch8 (code, X86_CC_EQ, 9, FALSE);
		x86_push_reg (code, X86_EAX);
		x86_call_code (code, mono_string_new_wrapper);
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
	}
	
	/* restore the LMF */
	
	/* ebx = previous_lmf */
	x86_pop_reg (code, X86_EBX);
	/* edi = lmf */
	x86_pop_reg (code, X86_EDI);
	/* *(lmf) = previous_lmf */
	x86_mov_membase_reg (code, X86_EDI, 0, X86_EBX, 4);

	/* discard method info */
	x86_pop_reg (code, X86_ESI);

	/* restore caller saved regs */
	x86_pop_reg (code, X86_EBP);
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);

	if (mono_jit_profile) {
		x86_push_reg (code, X86_EAX);
		x86_push_reg (code, X86_EDX);
		x86_push_imm (code, method);
		x86_mov_reg_imm (code, X86_EAX, mono_profiler_method_leave);
		x86_call_reg (code, X86_EAX);
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
		x86_pop_reg (code, X86_EDX);
		x86_pop_reg (code, X86_EAX);
	}
	x86_ret (code);

	/* we store a dummy jit info (code size 4), so that mono_delegate_ctor
	 * is able to find a method info for icalls and pinvoke methods */
	ji = mono_mempool_alloc0 (mono_root_domain->mp, sizeof (MonoJitInfo));
	ji->method = method;
	ji->code_start = start;
	ji->code_size = 4;
	ji->used_regs = 0;
	ji->num_clauses = 0;
	mono_jit_info_table_add (mono_root_domain, ji);

	g_assert ((code - start) < 512);

	return start;
}

