/*
 * Create trampolines to invoke arbitrary functions.
 * 
 * Copyright (C) Ximian Inc.
 * 
 * Author: Paolo Molaro (lupus@ximian.com)
 * 
 */

#include "config.h"
#include <stdlib.h>
#include "x86-codegen.h"
#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/marshal.h"

/*
 * The resulting function takes the form:
 * void func (void (*callme)(), void *retval, void *this_obj, stackval *arguments);
 */
#define FUNC_ADDR_POS	8
#define RETVAL_POS	12
#define THIS_POS	16
#define ARGP_POS	20
#define LOC_POS	-4

#define ARG_SIZE	sizeof (stackval)

MonoPIFunc
mono_create_trampoline (MonoMethod *method, int runtime)
{
	MonoMethodSignature *sig;
	unsigned char *p, *code_buffer;
	guint32 local_size = 0, stack_size = 0, code_size = 50;
	guint32 arg_pos, simpletype;
	int i, stringp;
	int need_marshal;
	GList *free_locs = NULL;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime)
		need_marshal = 0;
	else
		need_marshal = 1;

	sig = method->signature;
	
	if (sig->hasthis) {
		stack_size += sizeof (gpointer);
		code_size += 5;
	}
	
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			stack_size += sizeof (gpointer);
			code_size += 20;
			local_size++;
			continue;
		}
		simpletype = sig->params [i]->type;
enum_calc_size:
		switch (simpletype) {
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
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
			stack_size += 4;
			code_size += i < 10 ? 5 : 8;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			if (mono_class_value_size (sig->params [i]->data.klass, NULL) != 4)
				g_error ("can only marshal enums, not generic structures (size: %d)", mono_class_value_size (sig->params [i]->data.klass, NULL));
			stack_size += 4;
			code_size += i < 10 ? 5 : 8;
			break;
		case MONO_TYPE_STRING:
			stack_size += 4;
			code_size += 20;
			local_size++;
			break;
		case MONO_TYPE_I8:
			stack_size += 8;
			code_size += i < 10 ? 5 : 8;
			break;
		case MONO_TYPE_R8:
			stack_size += 8;
			code_size += i < 10 ? 7 : 10;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}
	/*
	 * FIXME: take into account large return values.
	 */

	code_buffer = p = alloca (code_size);

	/*
	 * Standard function prolog.
	 */
	x86_push_reg (p, X86_EBP);
	x86_mov_reg_reg (p, X86_EBP, X86_ESP, 4);
	/*
	 * We store some local vars here to handle string pointers.
	 * and align to 16 byte boundary...
	 */
	if (local_size) {
		x86_alu_reg_imm (p, X86_SUB, X86_ESP, local_size * 4);
		stack_size = (stack_size * local_size * 4) % 16;
	} else {
		stack_size = stack_size % 16;
	}
	if (stack_size)
		x86_alu_reg_imm (p, X86_SUB, X86_ESP, stack_size);

	/*
	 * EDX has the pointer to the args.
	 */
	x86_mov_reg_membase (p, X86_EDX, X86_EBP, ARGP_POS, 4);

	/*
	 * Push arguments in reverse order.
	 */
	stringp = 0;
	for (i = sig->param_count; i; --i) {
		arg_pos = ARG_SIZE * (i - 1);
		if (sig->params [i - 1]->byref) {
			if (!need_marshal) {
				x86_push_membase (p, X86_EDX, arg_pos);
				continue;
			}
			if (sig->params [i - 1]->type == MONO_TYPE_SZARRAY &&
			    sig->params [i - 1]->data.type->type == MONO_TYPE_STRING) {
				x86_mov_reg_membase (p, X86_EAX, X86_EDX, arg_pos, 4);
				x86_push_regp (p, X86_EAX);
				x86_mov_reg_imm (p, X86_EDX, mono_marshal_string_array);
				x86_call_reg (p, X86_EDX);
				x86_alu_reg_imm (p, X86_ADD, X86_ESP, 4);
				/*
				 * Store the pointer in a local we'll free later.
				 */
				stringp++;
				x86_mov_membase_reg (p, X86_EBP, LOC_POS * stringp, X86_EAX, 4);
				free_locs = g_list_prepend (free_locs, GUINT_TO_POINTER (LOC_POS * stringp));
				/* load the pointer and push it */
				x86_lea_membase (p, X86_EAX, X86_EBP, LOC_POS * stringp);
				x86_push_reg (p, X86_EAX);
				/* restore pointer to args in EDX */
				x86_mov_reg_membase (p, X86_EDX, X86_EBP, ARGP_POS, 4);
			} else {
				x86_push_membase (p, X86_EDX, arg_pos);
			}
			continue;
		}
		simpletype = sig->params [i - 1]->type;
enum_marshal:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_R4:
			x86_push_membase (p, X86_EDX, arg_pos);
			break;
		case MONO_TYPE_VALUETYPE:
			if (!sig->params [i - 1]->data.klass->enumtype) {
				/* it's a structure that fits in 4 bytes, need to push the value pointed to */
				x86_mov_reg_membase (p, X86_EAX, X86_EDX, arg_pos, 4);
				x86_push_regp (p, X86_EAX);
			} else {
				/* it's an enum value */
				simpletype = sig->params [i - 1]->data.klass->enum_basetype->type;
				goto enum_marshal;
			}
			break;
		case MONO_TYPE_STRING:
			/* 
			 * If it is an internalcall we assume it's the object we want.
			 * Yet another reason why MONO_TYPE_STRING should not be used to indicate char*.
			 */
			if (!need_marshal) {
				x86_push_membase (p, X86_EDX, arg_pos);
				break;
			}
			/*if (frame->method->flags & PINVOKE_ATTRIBUTE_CHAR_SET_ANSI*/
			x86_push_membase (p, X86_EDX, arg_pos);
			x86_mov_reg_imm (p, X86_EDX, mono_string_to_utf8);
			x86_call_reg (p, X86_EDX);
			x86_alu_reg_imm (p, X86_ADD, X86_ESP, 4);
			x86_push_reg (p, X86_EAX);
			/*
			 * Store the pointer in a local we'll free later.
			 */
			stringp++;
			x86_mov_membase_reg (p, X86_EBP, LOC_POS * stringp, X86_EAX, 4);
			free_locs = g_list_prepend (free_locs, GUINT_TO_POINTER (LOC_POS * stringp));
			/*
			 * we didn't save the reg: restore it here.
			 */
			if (i > 1)
				x86_mov_reg_membase (p, X86_EDX, X86_EBP, ARGP_POS, 4);
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_R8:
			x86_push_membase (p, X86_EDX, arg_pos + 4);
			x86_push_membase (p, X86_EDX, arg_pos);
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i - 1]->type);
		}
	}

	if (sig->hasthis) {
		if (sig->call_convention != MONO_CALL_THISCALL) {
			x86_mov_reg_membase (p, X86_EDX, X86_EBP, THIS_POS, 4);
			x86_push_reg (p, X86_EDX);
		} else {
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, THIS_POS, 4);
		}
	}

	/* 
	 * Insert call to function 
	 */
	x86_mov_reg_membase (p, X86_EDX, X86_EBP, FUNC_ADDR_POS, 4);
	x86_call_reg (p, X86_EDX);

	/*
	 * Handle retval.
	 * Small integer and pointer values are in EAX.
	 * Long integers are in EAX:EDX.
	 * FP values are on the FP stack.
	 */

	if (sig->ret->byref || 
	    (method->klass == mono_defaults.string_class &&
	     *method->name == '.' && !strcmp (method->name, ".ctor"))) {
		x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
		x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
	} else {
		simpletype = sig->ret->type;
enum_retvalue:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_mov_regp_reg (p, X86_ECX, X86_EAX, 1);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_mov_regp_reg (p, X86_ECX, X86_EAX, 2);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
			break;
		case MONO_TYPE_STRING: 
			if (!need_marshal) {
				x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
				x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
				break;
			}

			/* If the argument is non-null, then convert the value back */
			x86_alu_reg_reg (p, X86_OR, X86_EAX, X86_EAX);
			x86_branch8 (p, X86_CC_EQ, 11, FALSE);
			x86_push_reg (p, X86_EAX);
			x86_mov_reg_imm (p, X86_EDX, mono_string_new_wrapper);
			x86_call_reg (p, X86_EDX);
			x86_alu_reg_imm (p, X86_ADD, X86_ESP, 4);

			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
			break;
		case MONO_TYPE_R4:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_fst_membase (p, X86_ECX, 0, FALSE, TRUE);
			break;
		case MONO_TYPE_R8:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_fst_membase (p, X86_ECX, 0, TRUE, TRUE);
			break;
		case MONO_TYPE_I8:
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
			x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
			x86_mov_membase_reg (p, X86_ECX, 4, X86_EDX, 4);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/*
	 * free the allocated strings.
	 */
	if (need_marshal) {
		GList* tmp;
		for (tmp = free_locs; tmp; tmp = tmp->next) {
			x86_mov_reg_imm (p, X86_EDX, g_free);
			x86_push_membase (p, X86_EBP, GPOINTER_TO_UINT (tmp->data));
			x86_call_reg (p, X86_EDX);
		}
		g_list_free (free_locs);
	}
	/*
	 * Standard epilog.
	 */
	x86_leave (p);
	x86_ret (p);

	g_assert (p - code_buffer < code_size);
	return g_memdup (code_buffer, p - code_buffer);
}

#define MINV_POS  (- sizeof (MonoInvocation))
#define STACK_POS (MINV_POS - sizeof (stackval) * sig->param_count)
#define OBJ_POS   8
#define TYPE_OFFSET (G_STRUCT_OFFSET (stackval, type))

/*
 * Returns a pointer to a native function that can be used to
 * call the specified method.
 * The function created will receive the arguments according
 * to the call convention specified in the method.
 * This function works by creating a MonoInvocation structure,
 * filling the fields in and calling ves_exec_method on it.
 * Still need to figure out how to handle the exception stuff
 * across the managed/unmanaged boundary.
 */
void *
mono_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoJitInfo *ji;
	unsigned char *p, *code_buffer;
	gint32 local_size;
	gint32 stackval_pos, arg_pos = 8;
	int i, align;

	/*
	 * If it is a static P/Invoke method, we can just return the pointer
	 * to the method implementation.
	 */
	sig = method->signature;

	code_buffer = p = alloca (512); /* FIXME: check for overflows... */

	local_size = sizeof (MonoInvocation) + sizeof (stackval) * (sig->param_count + 1);
	stackval_pos = -local_size;

	/*
	 * Standard function prolog.
	 */
	x86_push_reg (p, X86_EBP);
	x86_mov_reg_reg (p, X86_EBP, X86_ESP, 4);
	x86_alu_reg_imm (p, X86_SUB, X86_ESP, local_size);

	/*
	 * Initialize MonoInvocation fields, first the ones known now.
	 */
	x86_mov_reg_imm (p, X86_EAX, 0);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex)), X86_EAX, 4);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex_handler)), X86_EAX, 4);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, child)), X86_EAX, 4);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, parent)), X86_EAX, 4);
	/*
	 * Set the method pointer.
	 */
	x86_mov_membase_imm (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, method)), (int)method, 4);

	/*
	 * Handle this.
	 */
	if (sig->hasthis) {
		if (sig->call_convention != MONO_CALL_THISCALL) {
			/*
			 * Grab it from the stack, otherwise it's already in ECX.
			 */
			x86_mov_reg_membase (p, X86_ECX, X86_EBP, OBJ_POS, 4);
			arg_pos += 4;
		}
		x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, obj)), X86_ECX, 4);
	}
	/*
	 * Handle the arguments. stackval_pos is the posset of the stackval array from EBP.
	 * arg_pos is the offset from EBP to the incoming arg on the stack.
	 * We just call stackval_from_data to handle all the (nasty) issues....
	 */
	x86_lea_membase (p, X86_EAX, X86_EBP, stackval_pos);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, stack_args)), X86_EAX, 4);
	for (i = 0; i < sig->param_count; ++i) {
		x86_mov_reg_imm (p, X86_ECX, stackval_from_data);
		x86_lea_membase (p, X86_EDX, X86_EBP, arg_pos);
		x86_lea_membase (p, X86_EAX, X86_EBP, stackval_pos);
		x86_push_reg (p, X86_EDX);
		x86_push_reg (p, X86_EAX);
		x86_push_imm (p, sig->params [i]);
		x86_call_reg (p, X86_ECX);
		x86_alu_reg_imm (p, X86_SUB, X86_ESP, 12);
		stackval_pos += sizeof (stackval);
		arg_pos += mono_type_stack_size (sig->params [i], &align);
	}

	/*
	 * Handle the return value storage area.
	 */
	x86_lea_membase (p, X86_EAX, X86_EBP, stackval_pos);
	x86_mov_membase_reg (p, X86_EBP, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, retval)), X86_EAX, 4);

	/*
	 * Call the method.
	 */
	x86_lea_membase (p, X86_EAX, X86_EBP, MINV_POS);
	x86_push_reg (p, X86_EAX);
	x86_mov_reg_imm (p, X86_EDX, ves_exec_method);
	x86_call_reg (p, X86_EDX);

	/*
	 * Move the return value to the proper place.
	 */
	x86_lea_membase (p, X86_EAX, X86_EBP, stackval_pos);
	if (sig->ret->byref) {
		x86_mov_reg_membase (p, X86_EAX, X86_EAX, 0, 4);
	} else {
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_BOOLEAN:
			x86_mov_reg_membase (p, X86_EAX, X86_EAX, 0, 1);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:
			x86_mov_reg_membase (p, X86_EAX, X86_EAX, 0, 4);
			break;
		case MONO_TYPE_I8:
			x86_mov_reg_membase (p, X86_EDX, X86_EAX, 4, 4);
			x86_mov_reg_membase (p, X86_EAX, X86_EAX, 0, 4);
			break;
		case MONO_TYPE_R8:
			x86_fld_membase (p, X86_EAX, 0, TRUE);
			break;
		default:
			g_error ("Type 0x%x not handled yet in thunk creation", sig->ret->type);
			break;
		}
	}
	
	/*
	 * Standard epilog.
	 */
	x86_leave (p);
	x86_ret (p);

	g_assert (p - code_buffer < 512);

	ji = g_new0 (MonoJitInfo, 1);
	ji->method = method;
	ji->code_size = p - code_buffer;
	ji->code_start = g_memdup (code_buffer, p - code_buffer);

	mono_jit_info_table_add (mono_root_domain, ji);

	return ji->code_start;
}
