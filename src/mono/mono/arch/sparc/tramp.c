/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * Create trampolines to invoke arbitrary functions.
 * 
 * Copyright (C) Ximian Inc.
 * 
 * Authors: Paolo Molaro (lupus@ximian.com)
 *          Jeffrey Stedfast <fejj@ximian.com>
 * 
 */

#include "config.h"
#include <stdlib.h>
#include "sparc-codegen.h"
#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"


#define FUNC_ADDR_POS	sparc_i0
#define RETVAL_POS	sparc_i1
#define THIS_POS	sparc_i2
#define ARGP_POS	sparc_i3
#define LOC_POS	-4

#define ARG_SIZE	sizeof (stackval)

static void
fake_func (void (*callme)(gpointer, gpointer), stackval *retval, void *this_obj, stackval *arguments)
{
	//*(gpointer*)retval = (gpointer)(*callme) (arguments [0].data.p, arguments [1].data.p, arguments [2].data.p);
	//*(gdouble*) retval = (gdouble)(*callme) (arguments [0].data.f);
	
	/* internal_from_handle() */
	/* return (gpointer)(*callme) (((MonoType *)arguments [0].data.p)->data.klass); */
	
	/* InitializeArray() */
	return (*callme) (arguments [0].data.p, arguments [1].data.p);
}

static const char *
mono_type (int type)
{
	switch (type) {
	case MONO_TYPE_END:
		return "MONO_TYPE_END";
	case MONO_TYPE_VOID:
		return "MONO_TYPE_VOID";
	case MONO_TYPE_BOOLEAN:
		return "MONO_TYPE_BOOLEAN";
	case MONO_TYPE_CHAR:
		return "MONO_TYPE_CHAR";
	case MONO_TYPE_I1:
		return "MONO_TYPE_I1";
	case MONO_TYPE_U1:
		return "MONO_TYPE_U1";
	case MONO_TYPE_I2:
		return "MONO_TYPE_I2";
	case MONO_TYPE_U2:
		return "MONO_TYPE_U2";
	case MONO_TYPE_I4:
		return "MONO_TYPE_I4";
	case MONO_TYPE_U4:
		return "MONO_TYPE_U4";
	case MONO_TYPE_I8:
		return "MONO_TYPE_I8";
	case MONO_TYPE_U8:
		return "MONO_TYPE_U8";
	case MONO_TYPE_R4:
		return "MONO_TYPE_R4";
	case MONO_TYPE_R8:
		return "MONO_TYPE_R8";
	case MONO_TYPE_STRING:
		return "MONO_TYPE_STRING";
	case MONO_TYPE_PTR:
		return "MONO_TYPE_PTR";
	case MONO_TYPE_BYREF:
		return "MONO_TYPE_BYREF";
	case MONO_TYPE_VALUETYPE:
		return "MONO_TYPE_VALUETYPE";
	case MONO_TYPE_CLASS:
		return "MONO_TYPE_CLASS";
	case MONO_TYPE_ARRAY:
		return "MONO_TYPE_ARRAY";
	case MONO_TYPE_TYPEDBYREF:
		return "MONO_TYPE_TYPEBYREF";
	case MONO_TYPE_I:
		return "MONO_TYPE_I";
	case MONO_TYPE_U:
		return "MONO_TYPE_U";
	case MONO_TYPE_FNPTR:
		return "MONO_TYPE_FNPTR";
	case MONO_TYPE_OBJECT:
		return "MONO_TYPE_OBJECT";
	case MONO_TYPE_SZARRAY:
		return "MONO_TYPE_SZARRAY";
	case MONO_TYPE_CMOD_REQD:
		return "MONO_TYPE_CMOD_REQD";
	case MONO_TYPE_CMOD_OPT:
		return "MONO_TYPE_CMOD_OPT";
	case MONO_TYPE_INTERNAL:
		return "MONO_TYPE_INTERNAL";
	case MONO_TYPE_MODIFIER:
		return "MONO_TYPE_MODIFIER";
	case MONO_TYPE_SENTINEL:
		return "MONO_TYPE_SENTINEL";
	case MONO_TYPE_PINNED:
		return "MONO_TYPE_PINNED";
	}
	
	return "??";
}

static void
calculate_sizes (MonoMethod *method, guint32 *local_size, guint32 *stack_size, guint32 *code_size, int runtime)
{
	MonoMethodSignature *sig = method->signature;
	guint32 local = 0, stack = 0, code = 6;
	guint32 simpletype;
	int i;
	
	/* function arguments */
	if (sig->hasthis)
		code++;
	
	for (i = 0; i < sig->param_count; i++) {
		if (sig->params[i]->byref) {
			stack += sizeof (gpointer);
			code += i < 6 ? 1 : 3;
			continue;
		}
		
		simpletype = sig->params[i]->type;
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
			stack += 4;
			code += i < 6 ? 1 : 3;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params[i]->data.klass->enumtype) {
				simpletype = sig->params[i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			if (mono_class_value_size (sig->params[i]->data.klass, NULL) != 4)
				g_error ("can only marshal enums, not generic structures (size: %d)",
					 mono_class_value_size (sig->params[i]->data.klass, NULL));
			stack += 4;
			code += i < 6 ? 1 : 3;
			break;
		case MONO_TYPE_STRING:
			if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime) {
				stack += 4;
				code += i < 6 ? 1 : 3;
				break;
			}
			
			stack += 4;
			code += 5;
			local++;
			break;
		case MONO_TYPE_I8:
			stack += 8;
			code += i < 6 ? 2 : 3;
			break;
		case MONO_TYPE_R8:
			stack += 8;
			code += i < 6 ? 2 : 3;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params[i]->type);
		}
	}
	
	/* function return value */
	if (sig->ret->byref) {
		code += 2;
	} else {
		simpletype = sig->ret->type;
	enum_retvalue:
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
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			code += 2;
			break;
#if 0
		case MONO_TYPE_STRING:
			code += 2;
			if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) && !runtime) {
				code += 4;
			}
			break;
#endif
		case MONO_TYPE_I8:
			code += 3;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			code += 2;
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}
	
#define STACKALIGN(x) (((x) + 15) & (~15))
#define MINFRAME      ((16 + 1 + 6) * 4)      /* minimum size stack frame, in bytes:
					       * 16 for registers, 1 for "hidden param",
					       * and 6 in which a callee can store it's
					       * arguments.
					       */
	
	stack += MINFRAME + (local * 4);
	
	fprintf (stderr, "\tstack size: %d (%d)\n\tcode size: %d\n", STACKALIGN(stack), stack, code);
	
	*local_size = local;
	*stack_size = STACKALIGN(stack);
	*code_size = code;
}

static MonoString *
mono_string_new_wrapper (const char *text)
{
	return text ? mono_string_new (mono_domain_get (), text) : NULL;
}

MonoPIFunc
mono_create_trampoline (MonoMethod *method, int runtime)
{
	MonoMethodSignature *sig;
	guint32 *p, *code_buffer;
	guint32 local_size, stack_size, code_size;
	guint32 arg_pos, simpletype;
	int i, stringp, cur_out_reg;
	
	sig = method->signature;
	
	fprintf (stderr, "\nPInvoke [start emiting] %s\n", method->name);
	calculate_sizes (method, &local_size, &stack_size, &code_size, runtime);
	
	code_buffer = p = alloca (code_size * 4);
	cur_out_reg = sparc_o0;
	
	/* Standard function prolog. */
	sparc_save_imm (p, sparc_sp, -stack_size, sparc_sp);
#if 0
	/* gcc seems to want to store %i0 through %i3 for some reason */
	sparc_st_imm (p, sparc_i0, sparc_fp, 68);
	sparc_st_imm (p, sparc_i1, sparc_fp, 72);
	sparc_st_imm (p, sparc_i2, sparc_fp, 76);
	sparc_st_imm (p, sparc_i3, sparc_fp, 80);
#endif
	
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
		cur_out_reg++;
	}
	
	/* Push arguments in reverse order. */
	stringp = 0;
	for (i = 0; i < sig->param_count; i++) {
		arg_pos = ARG_SIZE * i;
		
		if (sig->params[i]->byref) {
			fprintf (stderr, "\tpushing params[%d] (byref): type=%s;\n", i, mono_type (sig->params[i]->type));
			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			cur_out_reg++;
			continue;
		}
		
		simpletype = sig->params[i]->type;
enum_marshal:
		fprintf (stderr, "\tpushing params[%d]: type=%s;\n", i, mono_type (simpletype));
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
		case MONO_TYPE_R4:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			cur_out_reg++;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params[i]->data.klass->enumtype) {
				/* it's an enum value */
				simpletype = sig->params[i]->data.klass->enum_basetype->type;
				goto enum_marshal;
			} else {
				/*sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);*/
				sparc_ld_imm (p, sparc_i3, arg_pos, sparc_l0);
				sparc_ld (p, sparc_l0, 0, cur_out_reg);
				cur_out_reg++;
			}
			break;
		case MONO_TYPE_STRING:
			if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime) {
				sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
				cur_out_reg++;
				break;
			}
			
#if 0
			sparc_sethi (p, mono_string_to_utf8, sparc_l0);
			sparc_or_imm (p, 0, sparc_l0, mono_string_to_utf8, sparc_l1);
			
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
#endif
			fprintf (stderr, "MONO_TYPE_STRING not yet fully supported.\n");
			exit (1);
			break;
		case MONO_TYPE_I8:
			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			cur_out_reg++;
			sparc_ld_imm (p, sparc_i3, arg_pos + 4, cur_out_reg);
			cur_out_reg++;
			break;
		case MONO_TYPE_R8:
			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			cur_out_reg++;
			sparc_ld_imm (p, sparc_i3, arg_pos + 4, cur_out_reg);
			cur_out_reg++;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}
	
	/* call the function */
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
	
#if 0
	/* free the allocated strings... */
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
	
	{
		unsigned char *inptr, *inend;
		
		inptr = (unsigned char *) code_buffer;
		inend = (unsigned char *) p;
		
		printf (".text\n.align 4\n.globl main\n.type main,function\nmain:\n");
		while (inptr < inend) {
			printf (".byte 0x%x\n", *inptr);
			inptr++;
		}
		fflush (stdout);
	}
	
	fprintf (stderr, "PInvoke [finish emiting] %s\n", method->name);
	
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
