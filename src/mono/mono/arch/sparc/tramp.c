
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
#include "sparc-codegen.h"
#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"

/*
 * The resulting function takes the form:
 * void func (void (*callme)(), void *retval, void *this_obj, stackval *arguments);
 */
#define FUNC_ADDR_POS	sparc_i0
#define RETVAL_POS	sparc_i1
#define THIS_POS	sparc_i2
#define ARGP_POS	sparc_i3
#define LOC_POS	-4

#define ARG_SIZE	sizeof (stackval)

MonoPIFunc
mono_create_trampoline (MonoMethod *method, int runtime)
{
	MonoMethodSignature *sig;
	guint32 *p, *code_buffer;
	guint32 local_size = 0, stack_size = 0, code_size = 6;
	guint32 arg_pos, simpletype;
	int i, stringp, cur_out_reg;

	sig = method->signature;
	
	if (sig->hasthis)
		code_size ++;
	
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			stack_size += sizeof (gpointer);
			code_size += i < 6 ? 1 : 3;
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
			code_size += i < 6 ? 1 : 3;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			if (mono_class_value_size (sig->params [i]->data.klass, NULL) != 4)
				g_error ("can only marshal enums, not generic structures (size: %d)", mono_class_value_size (sig->params [i]->data.klass, NULL));
			stack_size += 4;
			code_size += i < 6 ? 1 : 3;
			break;
		case MONO_TYPE_STRING:
			stack_size += 4;
			code_size += 5;
			local_size++;
			break;
		case MONO_TYPE_I8:
			stack_size += 8;
			code_size += i < 6 ? 2 : 3;
			break;
		case MONO_TYPE_R8:
			stack_size += 8;
			code_size += i < 6 ? 2 : 3;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}
	/*
	 * FIXME: take into account large return values.
	 */

	code_buffer = p = alloca (code_size * 4);
	cur_out_reg = sparc_o0;

	/*
	 * Standard function prolog.
	 */
	sparc_save_imm (p, sparc_sp, -112-stack_size, sparc_sp);
	/*
	 * We store some local vars here to handle string pointers.
	 * and align to 16 byte boundary...
	 */
#if 0
	if (local_size) {
		x86_alu_reg_imm (p, X86_SUB, X86_ESP, local_size * 4);
		stack_size = (stack_size * local_size * 4) % 16;
	} else {
		stack_size = stack_size % 16;
	}
	if (stack_size)
		x86_alu_reg_imm (p, X86_SUB, X86_ESP, stack_size);
#endif

	/*
	 * %i3 has the pointer to the args.
	 */

	if (sig->hasthis) {
		sparc_mov_reg_reg (p, sparc_i2, cur_out_reg);
		++cur_out_reg;
	}

	/*
	 * Push arguments in reverse order.
	 */
	stringp = 0;
	for (i = 0; i < sig->param_count; ++i) {
		arg_pos = ARG_SIZE * i;
		if (sig->params [i]->byref) {
			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			++cur_out_reg;
			continue;
		}
		simpletype = sig->params [i]->type;
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
			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			++cur_out_reg;
			break;
		case MONO_TYPE_VALUETYPE:
			if (!sig->params [i]->data.klass->enumtype) {
				/* it's a structure that fits in 4 bytes, need to push the value pointed to */
				/*x86_mov_reg_membase (p, X86_EAX, X86_EDX, arg_pos, 4);
				x86_push_regp (p, X86_EAX);*/
				g_assert (0);
			} else {
				/* it's an enum value */
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_marshal;
			}
			break;
		case MONO_TYPE_R8:
			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			++cur_out_reg;
			sparc_ld_imm (p, sparc_i3, arg_pos+4, cur_out_reg);
			++cur_out_reg;
			break;
#if 0
		case MONO_TYPE_STRING:
			/* 
			 * If it is an internalcall we assume it's the object we want.
			 * Yet another reason why MONO_TYPE_STRING should not be used to indicate char*.
			 */
			if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime) {
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
			/*
			 * we didn't save the reg: restore it here.
			 */
			if (i > 1)
				x86_mov_reg_membase (p, X86_EDX, X86_EBP, ARGP_POS, 4);
			break;
#endif
		case MONO_TYPE_I8:
			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			++cur_out_reg;
			sparc_ld_imm (p, sparc_i3, arg_pos+4, cur_out_reg);
			++cur_out_reg;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	/* 
	 * Insert call to function 
	 */
	sparc_jmpl_imm (p, sparc_i0, 0, sparc_callsite);
	sparc_nop (p);

	/*
	 * Handle retval.
	 * Small integer and pointer values are in EAX.
	 * Long integers are in EAX:EDX.
	 * FP values are on the FP stack.
	 */
#if 0
	if (sig->ret->byref) {
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
			if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime) {
				x86_mov_reg_membase (p, X86_ECX, X86_EBP, RETVAL_POS, 4);
				x86_mov_regp_reg (p, X86_ECX, X86_EAX, 4);
				break;
			}

			/* If the argument is non-null, then convert the value back */
			x86_alu_reg_reg (p, X86_OR, X86_EAX, X86_EAX);
			x86_branch8 (p, X86_CC_EQ, 11, FALSE);
			x86_push_reg (p, X86_EAX);
			x86_mov_reg_imm (p, X86_EDX, mono_string_new);
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
#endif
	/*
	 * free the allocated strings.
	 */
#if 0
	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)) {
		if (local_size)
			x86_mov_reg_imm (p, X86_EDX, g_free);
		for (i = 1; i <= local_size; ++i) {
			x86_push_membase (p, X86_EBP, LOC_POS * i);
			x86_call_reg (p, X86_EDX);
		}
	}
#endif
	/*
	 * Standard epilog.
	 * 8 may be 12 when returning structures (to skip unimp opcode).
	 */
	sparc_jmpl_imm (p, sparc_i7, 8, sparc_zero);
	sparc_restore (p, sparc_zero, sparc_zero, sparc_zero);

	/* FIXME: need to flush */
	return g_memdup (code_buffer, 4 * (p - code_buffer));
}

void *
mono_create_method_pointer (MonoMethod *method)
{
	return NULL;
}

MonoMethod*
mono_method_pointer_get (void *code)
{
	return NULL;
}
