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
#include <string.h>
#include "sparc-codegen.h"
#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/debug-helpers.h"


#define ARG_SIZE	sizeof (stackval)
#define PROLOG_INS 1
#define CALL_INS 2  /* Max 2.  1 for the jmpl and 1 for the nop */
#define EPILOG_INS 2
#define MINIMAL_STACK_SIZE 23
#define FLOAT_REGS 32
#define OUT_REGS 6
#define LOCAL_REGS 8

#define NOT_IMPL(x) g_error("FIXME: %s", x);

/* Some assembly... */
#define flushi(addr)    __asm__ __volatile__ ("flush %0"::"r"(addr):"memory")

static char*
sig_to_name (MonoMethodSignature *sig, const char *prefix)
{
        int i;
        char *result;
        GString *res = g_string_new ("");

        if (prefix) {
                g_string_append (res, prefix);
                g_string_append_c (res, '_');
        }

        mono_type_get_desc (res, sig->ret, TRUE);

        for (i = 0; i < sig->param_count; ++i) {
                g_string_append_c (res, '_');
                mono_type_get_desc (res, sig->params [i], TRUE);
        }
        result = res->str;
        g_string_free (res, FALSE);
        return result;
}

static void
add_general (guint *gr, guint *stack_size, guint *code_size, gboolean simple)
{
	if (simple) {
		if (*gr >= OUT_REGS) {
			*stack_size += 4;
			*code_size += 12;
		} else {
			*code_size += 4;
		}
	} else {
		if (*gr >= OUT_REGS - 1) {
			*stack_size += 8 + (*stack_size % 8);
			*code_size += 16;
		} else {
			*code_size += 16;
		}
		(*gr)++;
	}
	(*gr)++;
}

static void
calculate_sizes (MonoMethodSignature *sig, guint *stack_size, guint *code_size,
		 gboolean string_ctor, gboolean *use_memcpy)
{
	guint i, fr, gr;
	guint32 simpletype;
	
	fr = gr = 0;
	*stack_size = MINIMAL_STACK_SIZE * 4;
	*code_size = (PROLOG_INS + CALL_INS + EPILOG_INS) * 4;
	
	/* function arguments */
	if (sig->hasthis)
		add_general (&gr, stack_size, code_size, TRUE);
	
	for (i = 0; i < sig->param_count; i++) {
		if (sig->params[i]->byref) {
			add_general (&gr, stack_size, code_size, TRUE);
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
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_R4:
		case MONO_TYPE_SZARRAY:
			add_general (&gr, stack_size, code_size, TRUE);
			break;
		case MONO_TYPE_VALUETYPE: {
			gint size;
			if (sig->params[i]->data.klass->enumtype) {
				simpletype = sig->params[i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			size = mono_class_value_size (sig->params[i]->data.klass, NULL);
			if (size != 4) {
				fprintf(stderr, "copy %d byte struct on stack\n", size);
				*use_memcpy = TRUE;
				*code_size += 8*4;
				*stack_size += (size + 3) & (~3);
				if (gr > OUT_REGS) {
					*code_size += 4;
					*stack_size += 4;
				}
			} else {
				add_general (&gr, stack_size, code_size, TRUE);	
				*code_size += 4;
			}
			break;
		}
		case MONO_TYPE_I8:
		case MONO_TYPE_R8:
			add_general (&gr, stack_size, code_size, FALSE);
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params[i]->type);
		}
	}
	
	/* function return value */
	if (sig->ret->byref || string_ctor) {
		*code_size += 8;
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
			*code_size += 8;
			break;
		case MONO_TYPE_I8:
			*code_size += 12;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			*code_size += 8;
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}
	
	if (*use_memcpy) {
		*stack_size += 8;
		*code_size += 24;
		if (sig->hasthis) {
			*stack_size += 4;
			*code_size += 4;
		}
	}
	
	*stack_size = (*stack_size + 15) & (~15);
}	
	
static inline guint32 *
emit_epilog (guint32 *p, MonoMethodSignature *sig, guint stack_size)
{
	/*
	 * Standard epilog.
	 * 8 may be 12 when returning structures (to skip unimp opcode).
	 */
	sparc_jmpl_imm (p, sparc_i7, 8, sparc_zero);
	sparc_restore (p, sparc_zero, sparc_zero, sparc_zero);
	
	return p;
}

static inline guint32 *
emit_prolog (guint32 *p, MonoMethodSignature *sig, guint stack_size)
{
	/* yes kids, it is this simple! */
	sparc_save_imm (p, sparc_sp, -stack_size, sparc_sp);
	return p;
}

#define ARG_BASE sparc_i3 /* pointer to args in i3 */
#define SAVE_4_IN_GENERIC_REGISTER \
              if (gr < OUT_REGS) { \
                      sparc_ld_imm (p, ARG_BASE, i*ARG_SIZE, sparc_o0 + gr); \
                      gr++; \
              } else { \
	              g_error("FIXME: SAVE_4_IN_GENERIC_REGISTER"); \
              }

#define SAVE_4_VAL_IN_GENERIC_REGISTER \
              if (gr < OUT_REGS) { \
		      g_warning("DOCTOR! LOOK OUT: %p", p); \
                      sparc_ld_imm (p, ARG_BASE, i*ARG_SIZE, sparc_l0); \
                      sparc_ld_imm (p, sparc_l0, 0, sparc_o0 + gr); \
                      gr++; \
              } else { \
                      g_error("FIXME: SAVE_4_VAL_IN_GENERIC_REGISTER"); \
              }

static inline guint32*
emit_save_parameters (guint32 *p, MonoMethodSignature *sig, guint stack_size,
		      gboolean use_memcpy)
{
	guint i, fr, gr, stack_par_pos, struct_pos, cur_struct_pos;
	guint32 simpletype;

	fr = gr = 0;
	stack_par_pos = MINIMAL_STACK_SIZE * 4;

	if (sig->hasthis) {
		if (use_memcpy) {
			/* we don't need to save a thing. */
		} else 
			sparc_mov_reg_reg (p, sparc_i2, sparc_o0);
		gr ++;
	}

	if (use_memcpy) {
		cur_struct_pos = struct_pos = stack_par_pos;
		for (i = 0; i < sig->param_count; i++) {
			if (sig->params[i]->byref)
				continue;
			if (sig->params[i]->type == MONO_TYPE_VALUETYPE &&
			    !sig->params[i]->data.klass->enumtype) {
				gint size;
				
				size = mono_class_value_size (sig->params[i]->data.klass, NULL);
				if (size != 4) {
					/* need to call memcpy here */
					sparc_add_imm (p, 0, sparc_sp, stack_par_pos, sparc_o0);
					sparc_ld_imm (p, sparc_i3, i*16, sparc_o1);
					sparc_set (p, (guint32)size, sparc_o2);
					sparc_set (p, (guint32)memcpy, sparc_l0);
					sparc_jmpl_imm (p, sparc_l0, 0, sparc_callsite);
					sparc_nop (p);
					stack_par_pos += (size*2 + 3) & (~3);
				}
			}
		}
	}

	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->byref) {
		MonoClass *klass = sig->ret->data.klass;
		if (!klass->enumtype) {
			gint size = mono_class_native_size (klass, NULL);

			fprintf(stderr, "retval value type size: %d\n", size);
			if (size > 8) {
				sparc_ld_imm (p, sparc_sp, stack_size - 12,
					      sparc_o0);
				sparc_ld_imm (p, sparc_o0, 0, sparc_o0);
				gr ++;
			} else {
				g_error ("FIXME: size <= 8 not implemented");
			}
		}
	}

	fprintf(stderr, "%s\n", sig_to_name(sig, FALSE));

	for (i = 0; i < sig->param_count; i++) {
		if (sig->params[i]->byref) {
			SAVE_4_IN_GENERIC_REGISTER;
			continue;
		}
		simpletype = sig->params[i]->type;
	enum_calc_size:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
                case MONO_TYPE_I1:
                case MONO_TYPE_U1:
                case MONO_TYPE_I2:
                case MONO_TYPE_U2:
                case MONO_TYPE_CHAR:
                case MONO_TYPE_I4:
                case MONO_TYPE_U4:
		case MONO_TYPE_R4:
                case MONO_TYPE_I:
                case MONO_TYPE_U:
                case MONO_TYPE_PTR:
                case MONO_TYPE_CLASS:
                case MONO_TYPE_OBJECT:
                case MONO_TYPE_STRING:
                case MONO_TYPE_SZARRAY:
			SAVE_4_IN_GENERIC_REGISTER;
			break;
		case MONO_TYPE_VALUETYPE: {
			gint size;
			if (sig->params[i]->data.klass->enumtype) {
				simpletype = sig->params[i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			size = mono_class_value_size (sig->params[i]->data.klass, NULL);
			if (size == 4) {
				SAVE_4_VAL_IN_GENERIC_REGISTER;
			} else {
				if (gr < OUT_REGS) {
					sparc_add_imm (p, 0, sparc_sp,
					       cur_struct_pos, sparc_o0 + gr);
					gr ++;
				} else {
					sparc_ld_imm (p, sparc_sp,
						      cur_struct_pos,
						      sparc_l1);
					sparc_st_imm (p, sparc_l1,
						     sparc_sp,
						     stack_par_pos);
				}
				cur_struct_pos += (size + 3) & (~3);
			}
			break;
		}

		case MONO_TYPE_I8:
		case MONO_TYPE_R8:
			/* this will break in subtle ways... */
			if (gr < 5) {
				sparc_ld_imm (p, ARG_BASE, i*ARG_SIZE, sparc_o0 + gr);
				gr ++;
				
				if (gr >= OUT_REGS) {
					NOT_IMPL("split reg/stack")
						break;
				} else {
					sparc_ld_imm (p, ARG_BASE, 
						      (i*ARG_SIZE) + 4,
						      sparc_o0 + gr);
				}
				gr ++;
			} else {
				NOT_IMPL("FIXME: I8/R8 on stack");
			}
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params[i]->type);
		}
	}

	return p;
}

static inline guint32 *
alloc_code_memory (guint code_size)
{
	guint32 *p;

	p = g_malloc(code_size);

	return p;
}

static inline guint32 *
emit_call_and_store_retval (guint32 *p, MonoMethodSignature *sig,
			    guint stack_size, gboolean string_ctor)
{
	guint32 simpletype;

	/* call "callme" */
	sparc_jmpl_imm (p, sparc_i0, 0, sparc_callsite);
	sparc_nop (p);

	/* get return value */
	if (sig->ret->byref || string_ctor) {
		sparc_st (p, sparc_o0, sparc_i1, 0);
	} else {
		simpletype = sig->ret->type;
	enum_retval:
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
                                goto enum_retval;
                        }
                case MONO_TYPE_VOID:
                        break;
                default:
                        g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}
	return p;
}

MonoPIFunc
mono_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	guint32 *p, *code_buffer;
	guint stack_size, code_size, i;
	gboolean use_memcpy = FALSE;
	static GHashTable *cache = NULL;
	MonoPIFunc res;

	if (!cache)
		cache = g_hash_table_new ((GHashFunc)mono_signature_hash,
				 (GCompareFunc)mono_metadata_signature_equal);
	
	if ((res = (MonoPIFunc)g_hash_table_lookup(cache, sig)))
		return res;

	calculate_sizes (sig, &stack_size, &code_size, 
			 string_ctor, &use_memcpy);

	p = code_buffer = alloc_code_memory (code_size);
	p = emit_prolog (p, sig, stack_size);
	p = emit_save_parameters (p, sig, stack_size, use_memcpy);
	p = emit_call_and_store_retval (p, sig, stack_size, string_ctor);
	p = emit_epilog (p, sig, stack_size);
	
#if 0
	{
                guchar *cp;
                fprintf (stderr,".text\n.align 4\n.globl main\n.type main,@function\nmain:\n");
                for (cp = code_buffer; cp < p; cp++) {
                        fprintf (stderr, ".byte 0x%x\n", *cp);
                }
	}
#endif

	/* So here's the deal...
	 * UltraSPARC will flush a whole cache line at a time
	 * BUT, older SPARCs won't.
	 * So, be compatable and flush dwords at a time...
	 */

	for (i = 0; i < ((p - code_buffer)/2); i++)
		flushi((code_buffer + (i*8)));

	g_hash_table_insert(cache, sig, code_buffer);

	return (MonoPIFunc)code_buffer;
}

#define MINV_POS (MINIMAL_STACK_SIZE * 4)
void *
mono_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoJitInfo *ji;
	guint stack_size, code_size, stackval_arg_pos, local_pos;
	guint i, local_start, reg_param, stack_param, this_flag, cpos, vt_cur;
	guint align = 0;
	guint32 *p, *code_buffer;
	gint *vtbuf;
	gint32 simpletype;

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL && method->addr) {
		ji = g_new0 (MonoJitInfo, 1);
		ji->method = method;
		ji->code_size = 1;
		ji->code_start = method->addr;

		mono_jit_info_table_add (mono_root_domain, ji);
		return method->addr;
	}

	code_size = 1024;
	stack_size = 1024;
	stack_param = 0;

	sig = method->signature;

	p = code_buffer = g_malloc (code_size);

	fprintf(stderr, "Delegate [start emiting] %s\n", method->name);

	p = emit_prolog (p, sig, stack_size);

	/* fill MonoInvocation */
	sparc_st_imm (p, sparc_g0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex)));
	sparc_st_imm (p, sparc_g0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex_handler)));
	sparc_st_imm (p, sparc_g0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, child)));
	sparc_st_imm (p, sparc_g0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, parent)));

	sparc_set (p, (guint32)method, sparc_l0);
	sparc_st_imm (p, sparc_l0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, method)));

	local_start = local_pos = MINV_POS + sizeof (MonoInvocation) + 
		(sig->param_count + 1) * sizeof (stackval);

	if (sig->hasthis) {
		sparc_st_imm (p, sparc_i0, sparc_sp,
			  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, obj)));
		reg_param = 1;
	} else if (sig->param_count) {
		sparc_st_imm (p, sparc_i0, sparc_sp, local_pos);
		local_pos += 4;
		reg_param = 0;
	}

	this_flag = (sig->hasthis ? 1 : 0);

	if (sig->param_count) {
		gint save_count = MIN (OUT_REGS, sig->param_count - 1);
		for (i = reg_param; i < save_count; i++) {
			sparc_st_imm (p, sparc_i1 + i, sparc_sp, local_pos);
			local_pos += 4;
		}
	}

	/* prepare space for valuetypes */
        vt_cur = local_pos;
        vtbuf = alloca (sizeof(int)*sig->param_count);
        cpos = 0;
        for (i = 0; i < sig->param_count; i++) {
                MonoType *type = sig->params [i];
                vtbuf [i] = -1;
                if (type->type == MONO_TYPE_VALUETYPE) {
                        MonoClass *klass = type->data.klass;
                        gint size;
			
                        if (klass->enumtype)
                                continue;
                        size = mono_class_native_size (klass, &align);
                        cpos += align - 1;
                        cpos &= ~(align - 1);
                        vtbuf [i] = cpos;
                        cpos += size;
                }
        }
        cpos += 3;
        cpos &= ~3;
	
	local_pos += cpos;
	
	/* set MonoInvocation::stack_args */
	stackval_arg_pos = MINV_POS + sizeof (MonoInvocation);
	sparc_add_imm (p, 0, sparc_sp, stackval_arg_pos, sparc_l0);
	sparc_st_imm (p, sparc_l0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, stack_args)));

	/* add stackval arguments */
	/* something is bizzare here... */
	for (i=0; i < sig->param_count; i++) {
		if (reg_param < OUT_REGS) {
			sparc_add_imm (p, 0, sparc_sp,
				       local_start + (reg_param - this_flag)*4,
				       sparc_o2);
			reg_param++;
		} else {
			sparc_add_imm (p, 0, sparc_sp,
				       stack_size + 8 + stack_param, sparc_o2);
			stack_param++;
		}

		if (vtbuf[i] >= 0) {
			sparc_add_imm (p, 0, sparc_sp, vt_cur, sparc_o1);
			sparc_st_imm (p, sparc_o1, sparc_sp, stackval_arg_pos);
			sparc_add_imm (p, 0, sparc_sp, stackval_arg_pos, 
				       sparc_o1);
			sparc_ld (p, sparc_o2, 0, sparc_o2);
			vt_cur += vtbuf[i];
		} else {
			sparc_add_imm (p, 0, sparc_sp, stackval_arg_pos, 
				       sparc_o1);
		}

		sparc_set (p, (guint32)sig->params[i], sparc_o0);
		sparc_set (p, (guint32)sig->pinvoke, sparc_o3);

		/* YOU make the CALL! */
		sparc_set (p, (guint32)stackval_from_data, sparc_l0);
		sparc_jmpl_imm (p, sparc_l0, 0, sparc_callsite);
		sparc_nop (p);

		if (sig->pinvoke)
			stackval_arg_pos += 4 * 
				mono_type_native_stack_size (sig->params[i],
							     &align);
		else
			stackval_arg_pos += 4 *
				mono_type_stack_size (sig->params[i], &align);
	}

	/* return value storage */
	if (sig->param_count) {
		sparc_add_imm (p, 0, sparc_sp, stackval_arg_pos, sparc_l0);
	}

	sparc_st_imm (p, sparc_l0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, retval)));

	/* call ves_exec_method */
	sparc_add_imm (p, 0, sparc_sp, MINV_POS, sparc_o0);
	sparc_set (p, (guint32)ves_exec_method, sparc_l0);
	sparc_jmpl_imm (p, sparc_l0, 0, sparc_callsite);
	sparc_nop (p);

	/* move retval from stackval to proper place (r3/r4/...) */
        if (sig->ret->byref) {
		sparc_ld_imm (p, sparc_sp, stackval_arg_pos, sparc_i0 );
        } else {
        enum_retvalue:
                switch (sig->ret->type) {
                case MONO_TYPE_VOID:
                        break;
                case MONO_TYPE_BOOLEAN:
                case MONO_TYPE_I1:
                case MONO_TYPE_U1:
			sparc_ldub_imm (p, sparc_sp, stackval_arg_pos, sparc_i0);
                        break;
                case MONO_TYPE_I2:
                case MONO_TYPE_U2:
                        sparc_lduh_imm (p, sparc_sp, stackval_arg_pos, sparc_i0);
                        break;
                case MONO_TYPE_I4:
                case MONO_TYPE_U4:
                case MONO_TYPE_I:
                case MONO_TYPE_U:
                case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
                case MONO_TYPE_CLASS:
                        sparc_ld_imm (p, sparc_sp, stackval_arg_pos, sparc_i0);
                        break;
                case MONO_TYPE_I8:
                        sparc_ld_imm (p, sparc_sp, stackval_arg_pos, sparc_i0);
                        sparc_ld_imm (p, sparc_sp, stackval_arg_pos + 4, sparc_i1);
                        break;
                case MONO_TYPE_R4:
                        sparc_ldf_imm (p, sparc_sp, stackval_arg_pos, sparc_f0);
                        break;
                case MONO_TYPE_R8:
                        sparc_lddf_imm (p, sparc_sp, stackval_arg_pos, sparc_f0);
                        break;
                case MONO_TYPE_VALUETYPE:
                        if (sig->ret->data.klass->enumtype) {
                                simpletype = sig->ret->data.klass->enum_basetype->type;
                                goto enum_retvalue;
                        }
                        NOT_IMPL("value type as ret val from delegate");
                        break;
                default: 
			g_error ("Type 0x%x not handled yet in thunk creation",
				 sig->ret->type);
                        break;
                }
        }

	p = emit_epilog (p, sig, stack_size);

	for (i = 0; i < ((p - code_buffer)/2); i++)
		flushi((code_buffer + (i*8)));
	
	ji = g_new0 (MonoJitInfo, 1);
        ji->method = method;
        ji->code_size = p - code_buffer;
	ji->code_start = code_buffer;
	
	mono_jit_info_table_add (mono_root_domain, ji);

#if 0
        {
                guchar *cp;
                fprintf (stderr,".text\n.align 4\n.globl main\n.type main,@function\nmain:\n");
                for (cp = code_buffer; cp < p; cp++) {
                        fprintf (stderr, ".byte 0x%x\n", *cp);
                }
        }
#endif

	return ji->code_start;
}
