/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * Create trampolines to invoke arbitrary functions.
 * 
 * Copyright (C) Ximian Inc.
 *
 * Authors: Paolo Molaro (lupus@ximian.com)
 *          Jeffrey Stedfast <fejj@ximian.com>
 *	    Mark Crichton <crichton@gimp.org>
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
#define MINV_POS 8

#define ARG_SIZE	sizeof (stackval)

/* Some assembly... */
#define flushi(addr)    __asm__ __volatile__ ("flush %0"::"r"(addr):"memory")


/* WARNING:  This code WILL BREAK.  We do not currently check the status
 * of the registers.  Things can get trampled.  You have been warned.
 */

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
calculate_sizes (MonoMethodSignature *sig, guint32 *local_size, guint32 *stack_size, guint32 *code_size, gboolean string_ctor)
{
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
		case MONO_TYPE_STRING:
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
		case MONO_TYPE_PTR:
		case MONO_TYPE_STRING:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			code += 2;
			break;
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
	
#ifdef DEBUG_SPARC_TRAMP
	fprintf (stderr, "\tstack size: %d (%d)\n\tcode size: %d\n", 
		 STACKALIGN(stack), stack, code); 
#endif
	
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
mono_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	guint32 *p, *code_buffer;
	guint32 local_size, stack_size, code_size;
	guint32 arg_pos, simpletype;
	static GHashTable *cache = NULL;
	int i, stringp, cur_out_reg;
	MonoPIFunc res;

	if (!cache)
		cache = g_hash_table_new ((GHashFunc)mono_signature_hash,
		            (GCompareFunc)mono_metadata_signature_equal);

	if ((res = (MonoPIFunc)g_hash_table_lookup(cache, sig)))
		return res;

	calculate_sizes (sig, &local_size, &stack_size, &code_size, string_ctor);
	
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
	
	if (sig->hasthis) {
		sparc_mov_reg_reg (p, sparc_i2, cur_out_reg);
		cur_out_reg++;
	}
	
	/* Push arguments in reverse order. */
	stringp = 0;
	for (i = 0; i < sig->param_count; i++) {
		arg_pos = ARG_SIZE * i;
		
		if (sig->params[i]->byref) {

#ifdef DEBUG_SPARC_TRAMP
			fprintf (stderr, "\tpushing params[%d] (byref):"\
					 " type=%s;\n", i
					 ,mono_type(sig->params[i]->type));
#endif

			sparc_ld_imm (p, sparc_i3, arg_pos, cur_out_reg);
			cur_out_reg++;
			continue;
		}
		
		simpletype = sig->params[i]->type;
enum_marshal:

#ifdef DEBUG_SPARC_TRAMP
		fprintf (stderr, "\tpushing params[%d]: type=%s;\n",
                         i, mono_type (simpletype));
#endif

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
		case MONO_TYPE_STRING:
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
	if (sig->ret->byref || string_ctor) {
		sparc_st (p, sparc_o0, sparc_i1, 0);
	} else {
		simpletype = sig->ret->type;

#ifdef DEBUG_SPARC_TRAMP
                fprintf (stderr, "\tret type: %s;\n", mono_type (simpletype));
#endif

enum_retvalue:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			sparc_stb (p, sparc_o0, sparc_i1, 0);
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			sparc_sth (p, sparc_o0, sparc_i1, 0);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_STRING:
		case MONO_TYPE_PTR:
			sparc_st (p, sparc_o0, sparc_i1, 0);
			break;
		case MONO_TYPE_R4:
			sparc_stf (p, sparc_f0, sparc_i1, 0);
			break;
		case MONO_TYPE_R8:
			sparc_stdf (p, sparc_f0, sparc_i1, 0);
			break;
		case MONO_TYPE_I8:
			sparc_std (p, sparc_o0, sparc_i1, 0);
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
	 * Standard epilog.
	 * 8 may be 12 when returning structures (to skip unimp opcode).
	 */
	sparc_jmpl_imm (p, sparc_i7, 8, sparc_zero);
	sparc_restore (p, sparc_zero, sparc_zero, sparc_zero);

#if DEBUG_SPARC_TRAMP
	{
		unsigned char *inptr, *inend;
		
		inptr = (unsigned char *) code_buffer;
		inend = (unsigned char *) p;
		
		fprintf (stderr,".text\n.align 4\n.globl main\n.type main,function\nmain:\n");
		while (inptr < inend) {
			fprintf (stderr, ".byte 0x%x\n", *inptr);
			inptr++;
		}
		fflush (stderr);
	}
#endif

	res = (MonoPIFunc)g_memdup (code_buffer, 4 * (p - code_buffer));

	/* So here's the deal...
	 * UltraSPARC will flush a whole cache line at a time
	 * BUT, older SPARCs won't.
	 * So, be compatable and flush dwords at a time...
	 */

	for (i = 0; i < ((p - code_buffer)/2); i++)
		flushi((res + (i*8)));

	g_hash_table_insert(cache, sig, res);

	return res;
}

void *
mono_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoJitInfo *ji;
	guint32 stack_size;
	unsigned char *p, *code_buffer;

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL && method->addr) {
		ji = g_new0 (MonoJitInfo, 1);
		ji->method = method;
		ji->code_size = 1;
		ji->code_start = method->addr;

		mono_jit_info_table_add (mono_root_domain, ji);
		return method->addr;
	}

	sig = method->signature;

	code_buffer = p = alloca (1024); /* Ok, this might overflow. */

	stack_size = STACKALIGN(((sig->param_count + 1) * 4) + MINFRAME);

	/* Prologue */
	/* SPARC rocks, 'nuff said */
	sparc_save_imm(p, sparc_sp, -stack_size, sparc_sp);

	/* Initialize the structure with zeros.  GO GO GADGET G0! */
	sparc_st(p, sparc_g0, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex)), 0);
	sparc_st(p, sparc_g0, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex_handler)), 0);
	sparc_st(p, sparc_g0, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, child)), 0);
	sparc_st(p, sparc_g0, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, parent)), 0);

	/* set the method pointer */
	/* 32 bit runtime -- Any thoughts on doing sparc64? */
	sparc_ld_imm(p, (guint32) method >> 16, 0, sparc_o0);
	sparc_or_imm(p, 0, sparc_o0, (guint32) method & 0xffff, sparc_o0);
	sparc_st(p, sparc_o0, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, method)),0);
	{
		unsigned char *inptr, *inend;

		inptr = (unsigned char *) code_buffer;
		inend = (unsigned char *) p;

		fprintf (stderr,".text\n.align 4\n.globl main\n.type main,function\nmain:\n");
		while (inptr < inend) {
			fprintf (stderr, ".byte 0x%x\n", *inptr);
			inptr++;
		}
		fflush (stderr);
        }

	return 0xdeadbeef;
}

MonoMethod*
mono_method_pointer_get (void *code)
{
	g_warning("mono_method_pointer_get: IMPLEMENT ME\n");
	return NULL;
}
