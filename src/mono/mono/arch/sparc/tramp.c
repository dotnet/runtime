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
#include "mono/metadata/marshal.h"


#define ARG_SIZE	sizeof (stackval)
#define PROLOG_INS 1
#define CALL_INS 3  /* Max 3.  1 for the jmpl and 1 for the nop and 1 for the possible unimp */
#define EPILOG_INS 2
#define FLOAT_REGS 32
#define OUT_REGS 6
#define LOCAL_REGS 8
#define SLOT_SIZE sizeof(gpointer)
#if SPARCV9
#define MINIMAL_STACK_SIZE 22
#define BIAS 2047
#define FRAME_ALIGN 16
#else
#define MINIMAL_STACK_SIZE 23
#define BIAS 0
#define FRAME_ALIGN 8
#endif

#define NOT_IMPL(x) g_error("FIXME: %s", x);
/*#define DEBUG(a) a*/
#define DEBUG(a)

/* Some assembly... */
#ifdef __GNUC__
#define flushi(addr)    __asm__ __volatile__ ("flush %0"::"r"(addr):"memory")
#else
static void flushi(void *addr)
{
    asm("flush %i0");
}
#endif

static char*
sig_to_name (MonoMethodSignature *sig, const char *prefix)
{
        int i;
        char *result;
        GString *res = g_string_new ("");
	char *p;

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
	p = result;
	/* remove chars Sun's asssembler doesn't like */
	while (*p != '\0') {
		if (*p == '.' || *p == '/')
			*p = '_';
		else if (*p == '&')
			*p = '$';
		else if (*p == '[' || *p == ']')
			*p = 'X';
		p++;
	}
        g_string_free (res, FALSE);
        return result;
}

static void
sparc_disassemble_code (guint32 *code_buffer, guint32 *p, const char *id)
{
	guchar *cp;
	FILE *ofd;

	if (!(ofd = fopen ("/tmp/test.s", "w")))
		g_assert_not_reached();

	fprintf (ofd, "%s:\n", id);

	for (cp = (guchar *)code_buffer; cp < (guchar *)p; cp++)
		fprintf (ofd, ".byte %d\n", *cp);

	fclose (ofd);

#ifdef __GNUC__
	system ("as /tmp/test.s -o /tmp/test.o;objdump -d /tmp/test.o");
#else
	/* this assumes we are using Sun tools as we aren't GCC */
#if SPARCV9
	system ("as -xarch=v9 /tmp/test.s -o /tmp/test.o;dis /tmp/test.o");
#else
	system ("as /tmp/test.s -o /tmp/test.o;dis /tmp/test.o");
#endif
#endif
}


static void
add_general (guint *gr, guint *stack_size, guint *code_size, gboolean simple)
{
	if (simple) {
		if (*gr >= OUT_REGS) {
			*stack_size += SLOT_SIZE;
			*code_size += 12;
		} else {
			*code_size += 4;
		}
	} else {
		if (*gr >= OUT_REGS - 1) {
			*stack_size += 8 + (*stack_size % 8); /* ???64 */
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
	*stack_size = MINIMAL_STACK_SIZE * SLOT_SIZE;
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
		case MONO_TYPE_R4:
#if SPARCV9
			(*code_size) += 4; /* for the fdtos */
#else
			(*code_size) += 12;
			(*stack_size) += 4;
#endif
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
		case MONO_TYPE_SZARRAY:
			add_general (&gr, stack_size, code_size, TRUE);
			break;
		case MONO_TYPE_VALUETYPE: {
			gint size;
			guint32 align;
			if (sig->params[i]->data.klass->enumtype) {
				simpletype = sig->params[i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			size = mono_class_native_size (sig->params[i]->data.klass, &align);
#if SPARCV9
			if (size != 4) {
#else
			if (1) {
#endif
				DEBUG(fprintf(stderr, "copy %d byte struct on stack\n", size));
				*use_memcpy = TRUE;
				*code_size += 8*4;

				*stack_size = (*stack_size + (align - 1)) & (~(align -1));
				*stack_size += (size + 3) & (~3);
				if (gr > OUT_REGS) {
					*code_size += 4;
					*stack_size += 4;
				}
			} else {
				add_general (&gr, stack_size, code_size, TRUE);	
#if SPARCV9
				*code_size += 8;
#else
				*code_size += 4;
#endif
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
		case MONO_TYPE_VALUETYPE: {
			gint size;
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			size = mono_class_native_size (sig->ret->data.klass, NULL);
#if SPARCV9
			if (size <= 32)
				*code_size += 8 + (size + 7) / 2;
			else
				*code_size += 8;
#else
			*code_size += 8;
#endif
			break;
		}
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
			*stack_size += SLOT_SIZE;
			*code_size += 4;
		}
	}
	
	*stack_size = (*stack_size + (FRAME_ALIGN - 1)) & (~(FRAME_ALIGN -1));
}	
	
static inline guint32 *
emit_epilog (guint32 *p, MonoMethodSignature *sig, guint stack_size)
{
	int ret_offset = 8;

	/*
	 * Standard epilog.
	 * 8 may be 12 when returning structures (to skip unimp opcode).
	 */
#if !SPARCV9
	if (sig != NULL && !sig->ret->byref && sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->data.klass->enumtype)
		ret_offset = 12;
#endif
	sparc_jmpl_imm (p, sparc_i7, ret_offset, sparc_zero);
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

#if SPARCV9
#define sparc_st_ptr(a,b,c,d) sparc_stx(a,b,c,d)
#define sparc_st_imm_ptr(a,b,c,d) sparc_stx_imm(a,b,c,d)
#define sparc_ld_ptr(a,b,c,d) sparc_ldx(a,b,c,d)
#define sparc_ld_imm_ptr(a,b,c,d) sparc_ldx_imm(a,b,c,d)
#else
#define sparc_st_ptr(a,b,c,d) sparc_st(a,b,c,d)
#define sparc_st_imm_ptr(a,b,c,d) sparc_st_imm(a,b,c,d)
#define sparc_ld_ptr(a,b,c,d) sparc_ld(a,b,c,d)
#define sparc_ld_imm_ptr(a,b,c,d) sparc_ld_imm(a,b,c,d)
#endif

/* synonyms for when values are really widened scalar values */
#define sparc_st_imm_word sparc_st_imm_ptr

#define ARG_BASE sparc_i3 /* pointer to args in i3 */
#define SAVE_PTR_IN_GENERIC_REGISTER \
              if (gr < OUT_REGS) { \
                      sparc_ld_imm_ptr (p, ARG_BASE, i*ARG_SIZE, sparc_o0 + gr); \
                      gr++; \
              } else { \
		      sparc_ld_imm_ptr (p, ARG_BASE, i*ARG_SIZE, sparc_l0); \
		      sparc_st_imm_ptr (p, sparc_l0, sparc_sp, stack_par_pos); \
		      stack_par_pos += SLOT_SIZE; \
              }

#if SPARCV9
/* This is a half hearted attempt at coping with structs by value - the 
   actual convention is complicated when floats & doubles are involved as
   you end up with fields in different registers on/off the stack.
   It will take more time to get right... */
static guint32 *
v9_struct_arg(guint32 *p, int arg_index, MonoClass *klass, int size, guint *p_gr)
{
	MonoMarshalType *info = mono_marshal_load_type_info (klass);
	int off = 0;
	int index = 0;
        guint gr = *p_gr;
	sparc_ld_imm_ptr (p, ARG_BASE, arg_index*ARG_SIZE, sparc_l0);
	if (size > 8) {
		if (info->fields [index].field->type->type == MONO_TYPE_R8) {
			sparc_lddf_imm (p, sparc_l0, 0, sparc_f0 + 2 * gr);
			index++;
		}
		else {
			sparc_ldx_imm (p, sparc_l0, 0, sparc_o0 + gr);
			index++; /* FIXME could be multiple fields in one register */
		}
		gr++;
		size -= 8;
		off = 8;
	}
	if (size > 0) {
		if (info->fields [index].field->type->type == MONO_TYPE_R8) {
			sparc_lddf_imm (p, sparc_l0, off, sparc_f0 + 2 * gr);
			index++;
		}
		else {
			/* will load extra garbage off end of short structs ... */
			sparc_ldx_imm (p, sparc_l0, off, sparc_o0 + gr);
		}
		gr++;
	} 
        *p_gr = gr;
	return p;
}
#endif

static inline guint32*
emit_save_parameters (guint32 *p, MonoMethodSignature *sig, guint stack_size,
		      gboolean use_memcpy)
{
	guint i, fr, gr, stack_par_pos, struct_pos, cur_struct_pos;
	guint32 simpletype;

	fr = gr = 0;
	stack_par_pos = MINIMAL_STACK_SIZE * SLOT_SIZE + BIAS;

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
				guint32 align;
				
				size = mono_class_native_size (sig->params[i]->data.klass, &align);
#if SPARCV9
				if (size != 4) {
#else
				if (1) {
#endif
					/* Add alignment */
					stack_par_pos = (stack_par_pos + (align - 1)) & (~(align - 1));
					/* need to call memcpy here */
					sparc_add_imm (p, 0, sparc_sp, stack_par_pos, sparc_o0);
					sparc_ld_imm_ptr (p, sparc_i3, i*16, sparc_o1);
					sparc_set (p, (guint32)size, sparc_o2);
					sparc_set_ptr (p, (void *)memmove, sparc_l0);
					sparc_jmpl_imm (p, sparc_l0, 0, sparc_callsite);
					sparc_nop (p);
					stack_par_pos += (size + (SLOT_SIZE - 1)) & (~(SLOT_SIZE - 1));
				}
			}
		}
	}

	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->byref) {
		MonoClass *klass = sig->ret->data.klass;
		if (!klass->enumtype) {
			gint size = mono_class_native_size (klass, NULL);

			DEBUG(fprintf(stderr, "retval value type size: %d\n", size));
#if SPARCV9
			if (size > 32) {
#else
			{
#endif
				/* pass on buffer in interp.c to called function */
				sparc_ld_imm_ptr (p, sparc_i1, 0, sparc_l0);
				sparc_st_imm_ptr (p, sparc_l0, sparc_sp, 64);
			}
		}
	}

	DEBUG(fprintf(stderr, "%s\n", sig_to_name(sig, FALSE)));

	for (i = 0; i < sig->param_count; i++) {
		if (sig->params[i]->byref) {
			SAVE_PTR_IN_GENERIC_REGISTER;
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
                        if (gr < OUT_REGS) {
                                sparc_ld_imm (p, ARG_BASE, i*ARG_SIZE, sparc_o0 + gr);
                                gr++;
                        } else {
                                sparc_ld_imm (p, ARG_BASE, i*ARG_SIZE, sparc_l0);
                                sparc_st_imm_word (p, sparc_l0, sparc_sp, stack_par_pos);
                                stack_par_pos += SLOT_SIZE;
                        }
                        break;

		case MONO_TYPE_R4:
#if SPARCV9
			sparc_lddf_imm (p, ARG_BASE, i*ARG_SIZE, sparc_f30); /* fix using this fixed reg */
			sparc_fdtos(p, sparc_f30, sparc_f0 + 2 * gr + 1);
			gr++;
			break;
#else
			/* Convert from double to single */
			sparc_lddf_imm (p, ARG_BASE, i*ARG_SIZE, sparc_f0);
			sparc_fdtos (p, sparc_f0, sparc_f0);

			/*
			 * FIXME: Is there an easier way to do an
			 * freg->ireg move ?
			 */
			sparc_stf_imm (p, sparc_f0, sparc_sp, stack_par_pos);

			if (gr < OUT_REGS) {
				sparc_ld_imm (p, sparc_sp, stack_par_pos, sparc_o0 + gr);
				gr++;
			} else {
				sparc_ldf_imm (p, sparc_sp, stack_par_pos, sparc_f0);
				sparc_stf_imm (p, sparc_f0, sparc_sp, stack_par_pos);
				stack_par_pos += SLOT_SIZE;
			}
			break;
#endif

                case MONO_TYPE_I:
                case MONO_TYPE_U:
                case MONO_TYPE_PTR:
                case MONO_TYPE_CLASS:
                case MONO_TYPE_OBJECT:
                case MONO_TYPE_STRING:
                case MONO_TYPE_SZARRAY:
			SAVE_PTR_IN_GENERIC_REGISTER;
			break;
		case MONO_TYPE_VALUETYPE: {
			gint size;
			guint32 align;
			MonoClass *klass = sig->params[i]->data.klass;
			if (klass->enumtype) {
				simpletype = klass->enum_basetype->type;
				goto enum_calc_size;
			}
			size = mono_class_native_size (klass, &align);
#if SPARCV9
			if (size <= 16) {
				if (gr < OUT_REGS) {
					p = v9_struct_arg(p, i, klass, size, &gr);
				} else {
					sparc_ld_imm_ptr (p, ARG_BASE, i*ARG_SIZE, sparc_l0);
					sparc_ld_imm (p, sparc_l0, 0, sparc_l0);
					sparc_st_imm_word (p, sparc_l0, sparc_sp, stack_par_pos);
					stack_par_pos += SLOT_SIZE;
				}
				break;
			}
#else
			/* 
			 * FIXME: The 32bit ABI docs do not mention that small
			 * structures are passed in registers.
			 */

			/*
			if (size == 4) {
				if (gr < OUT_REGS) {
					sparc_ld_imm_ptr (p, ARG_BASE, i*ARG_SIZE, sparc_l0);
					sparc_ld_imm (p, sparc_l0, 0, sparc_o0 + gr);
					gr++;
				} else {
					sparc_ld_imm_ptr (p, ARG_BASE, i*ARG_SIZE, sparc_l0);
					sparc_ld_imm (p, sparc_l0, 0, sparc_l0);
					sparc_st_imm_word (p, sparc_l0, sparc_sp, stack_par_pos);
					stack_par_pos += SLOT_SIZE;
				}
				break;
			}
			*/
#endif

			cur_struct_pos = (cur_struct_pos + (align - 1)) & (~(align - 1));
			if (gr < OUT_REGS) {
				sparc_add_imm (p, 0, sparc_sp,
					       cur_struct_pos, sparc_o0 + gr);
				gr ++;
			} else {
				sparc_ld_imm_ptr (p, sparc_sp,
						  cur_struct_pos,
						  sparc_l1);
				sparc_st_imm_ptr (p, sparc_l1,
						  sparc_sp,
						  stack_par_pos);
			}
			cur_struct_pos += (size + (SLOT_SIZE - 1)) & (~(SLOT_SIZE - 1));
			break;
		}

#if SPARCV9
		case MONO_TYPE_I8:
			if (gr < OUT_REGS) {
				sparc_ldx_imm (p, ARG_BASE, i*ARG_SIZE, sparc_o0 + gr);
				gr++;
			} else {
				sparc_ldx_imm (p, ARG_BASE, i*ARG_SIZE, sparc_l0);
				sparc_stx_imm (p, sparc_l0, sparc_sp, stack_par_pos);
				stack_par_pos += SLOT_SIZE;
			}
			break;
		case MONO_TYPE_R8:
			sparc_lddf_imm (p, ARG_BASE, i*ARG_SIZE, sparc_f0 + 2 * i);
			break;
#else
		case MONO_TYPE_I8:
		case MONO_TYPE_R8:
			if (gr < (OUT_REGS - 1)) {
				sparc_ld_imm (p, ARG_BASE, i*ARG_SIZE, sparc_o0 + gr);
				gr ++;
				
				sparc_ld_imm (p, ARG_BASE, 
					      (i*ARG_SIZE) + 4,
					      sparc_o0 + gr);
				gr ++;
			} else if (gr == (OUT_REGS - 1)) {
				/* Split register/stack */
				sparc_ld_imm (p, ARG_BASE, i*ARG_SIZE, sparc_o0 + gr);
				gr ++;

				sparc_ld_imm (p, ARG_BASE, (i*ARG_SIZE) + 4, sparc_l0);
				sparc_st_imm (p, sparc_l0, sparc_sp, stack_par_pos);
				stack_par_pos += SLOT_SIZE;
			} else {
				sparc_ld_imm (p, ARG_BASE, i*ARG_SIZE, sparc_l0);
				sparc_st_imm (p, sparc_l0, sparc_sp, stack_par_pos);
				stack_par_pos += SLOT_SIZE;

				sparc_ld_imm (p, ARG_BASE, (i*ARG_SIZE) + 4, sparc_l0);
				sparc_st_imm (p, sparc_l0, sparc_sp, stack_par_pos);
				stack_par_pos += SLOT_SIZE;
			}
			break;
#endif
		default:
			g_error ("Can't trampoline 0x%x", sig->params[i]->type);
		}
	}

	g_assert ((stack_par_pos - BIAS) <= stack_size);

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
#if !SPARCV9
	if (sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->data.klass->enumtype) {
		int size = mono_class_native_size (sig->ret->data.klass, NULL);
		sparc_unimp (p, size & 4095);
	}
#endif

	/* get return value */
	if (sig->ret->byref || string_ctor) {
		sparc_st_ptr (p, sparc_o0, sparc_i1, 0);
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
                        sparc_st (p, sparc_o0, sparc_i1, 0);
                        break;
                case MONO_TYPE_I:
                case MONO_TYPE_U:
                case MONO_TYPE_CLASS:
                case MONO_TYPE_OBJECT:
                case MONO_TYPE_SZARRAY:
                case MONO_TYPE_ARRAY:
                case MONO_TYPE_STRING:
                case MONO_TYPE_PTR:
                        sparc_st_ptr (p, sparc_o0, sparc_i1, 0);
                        break;
                case MONO_TYPE_R4:
                        sparc_stf (p, sparc_f0, sparc_i1, 0);
                        break;
                case MONO_TYPE_R8:
                        sparc_stdf (p, sparc_f0, sparc_i1, 0);
                        break;
                case MONO_TYPE_I8:
#if SPARCV9
                        sparc_stx (p, sparc_o0, sparc_i1, 0);
#else
                        sparc_std (p, sparc_o0, sparc_i1, 0);
#endif
                        break;
                case MONO_TYPE_VALUETYPE: {
			gint size;
                        if (sig->ret->data.klass->enumtype) {
                                simpletype = sig->ret->data.klass->enum_basetype->type;
                                goto enum_retval;
                        }
#if SPARCV9
			size = mono_class_native_size (sig->ret->data.klass, NULL);
			if (size <= 32) {
				int n_regs = size / 8;
				int j;
				sparc_ldx_imm (p, sparc_i1, 0, sparc_i1);
				/* wrong if there are floating values in the struct... */
				for (j = 0; j < n_regs; j++) {
		                        sparc_stx_imm (p, sparc_o0 + j, sparc_i1, j * 8);
				}
				size -= n_regs * 8;
				if (size > 0) {
					int last_reg = sparc_o0 + n_regs;
					/* get value right aligned in register */
					sparc_srlx_imm(p, last_reg, 64 - 8 * size, last_reg);
					if ((size & 1) != 0) {
			                        sparc_stb_imm (p, last_reg, sparc_i1, n_regs * 8 + size - 1);
			                        size--;
						if (size > 0)
							sparc_srlx_imm(p, last_reg, 8, last_reg);
					}
					if ((size & 2) != 0) {
			                        sparc_sth_imm (p, last_reg, sparc_i1, n_regs * 8 + size - 2);
			                        size -= 2;
						if (size > 0)
							sparc_srlx_imm(p, last_reg, 16, last_reg);
					}
					if ((size & 4) != 0)
			                        sparc_st_imm (p, last_reg, sparc_i1, n_regs * 8);
				}
			}
#endif
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
mono_arch_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
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
	/* we don't return structs here so pass in NULL as signature */
	p = emit_epilog (p, NULL, stack_size);

	g_assert(p <= code_buffer + (code_size / 4));
	
	DEBUG(sparc_disassemble_code (code_buffer, p, sig_to_name(sig, NULL)));

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

#define MINV_POS (MINIMAL_STACK_SIZE * SLOT_SIZE + BIAS)

void *
mono_arch_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoJitInfo *ji;
	guint stack_size, code_size, stackval_arg_pos, local_pos;
	guint i, local_start, reg_param = 0, stack_param, cpos, vt_cur;
	guint32 align = 0;
	guint32 *p, *code_buffer;
	gint *vtbuf;
	gint32 simpletype;

	code_size = 1024; /* these should be calculated... */
	stack_size = 1024;
	stack_param = 0;

	sig = method->signature;

	p = code_buffer = g_malloc (code_size);

	DEBUG(fprintf(stderr, "Delegate [start emiting] %s\n", method->name));
	DEBUG(fprintf(stderr, "%s\n", sig_to_name(sig, FALSE)));

	p = emit_prolog (p, sig, stack_size);

	/* fill MonoInvocation */
	sparc_st_imm_ptr (p, sparc_g0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex)));
	sparc_st_imm_ptr (p, sparc_g0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex_handler)));
	sparc_st_imm_ptr (p, sparc_g0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, parent)));

	sparc_set_ptr (p, (void *)method, sparc_l0);
	sparc_st_imm_ptr (p, sparc_l0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, method)));

	stackval_arg_pos = MINV_POS + sizeof (MonoInvocation);
	local_start = local_pos = stackval_arg_pos + (sig->param_count + 1) * sizeof (stackval);

	if (sig->hasthis) {
		sparc_st_imm_ptr (p, sparc_i0, sparc_sp,
			  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, obj)));
		reg_param = 1;
	} 

	if (sig->param_count) {
		gint save_count = MIN (OUT_REGS, sig->param_count + sig->hasthis);
		for (i = reg_param; i < save_count; i++) {
			sparc_st_imm_ptr (p, sparc_i0 + i, sparc_sp, local_pos);
			local_pos += SLOT_SIZE;
		}
	}

	/* prepare space for valuetypes */
        vt_cur = local_pos;
        vtbuf = alloca (sizeof(int)*sig->param_count);
        cpos = 0;
        for (i = 0; i < sig->param_count; i++) {
                MonoType *type = sig->params [i];
                vtbuf [i] = -1;
                if (!sig->params[i]->byref && type->type == MONO_TYPE_VALUETYPE) {
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
        cpos += SLOT_SIZE - 1;
        cpos &= ~(SLOT_SIZE - 1);
	
	local_pos += cpos;
	
	/* set MonoInvocation::stack_args */
	sparc_add_imm (p, 0, sparc_sp, stackval_arg_pos, sparc_l0);
	sparc_st_imm_ptr (p, sparc_l0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, stack_args)));

	/* add stackval arguments */
	for (i=0; i < sig->param_count; i++) {
		int stack_offset;
		int type;
		if (reg_param < OUT_REGS) {
			stack_offset = local_start + i * SLOT_SIZE;
			reg_param++;
		} else {
			stack_offset = stack_size + 8 + stack_param;
			stack_param++;
		}

		if (!sig->params[i]->byref) {
			type = sig->params[i]->type;
		enum_arg:
			switch (type) {
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_STRING:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_PTR:
			case MONO_TYPE_R8:
				break;
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
				stack_offset += SLOT_SIZE - 4;
				break;
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				stack_offset += SLOT_SIZE - 2;
				break;
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_BOOLEAN:
				stack_offset += SLOT_SIZE - 1;
				break;
	                case MONO_TYPE_VALUETYPE:
	                        if (sig->params[i]->data.klass->enumtype) {
	                                type = sig->params[i]->data.klass->enum_basetype->type;
	                                goto enum_arg;
	                        }
				g_assert(vtbuf[i] >= 0);
	                        break;
			default:
				g_error ("can not cope with delegate arg type %d", type);
			}
		}
	
		sparc_add_imm (p, 0, sparc_sp, stack_offset, sparc_o2);

		if (vtbuf[i] >= 0) {
			sparc_add_imm (p, 0, sparc_sp, vt_cur, sparc_o1);
			sparc_st_imm_ptr (p, sparc_o1, sparc_sp, stackval_arg_pos);
			sparc_add_imm (p, 0, sparc_sp, stackval_arg_pos, 
				       sparc_o1);
			sparc_ld_imm_ptr (p, sparc_o2, 0, sparc_o2);
			vt_cur += vtbuf[i];
		} else {
			sparc_add_imm (p, 0, sparc_sp, stackval_arg_pos, 
				       sparc_o1);
		}

		sparc_set_ptr (p, (void *)sig->params[i], sparc_o0);
		sparc_set (p, (guint32)sig->pinvoke, sparc_o3);

		/* YOU make the CALL! */
		sparc_set_ptr (p, (void *)stackval_from_data, sparc_l0);
		sparc_jmpl_imm (p, sparc_l0, 0, sparc_callsite);
		sparc_nop (p);
                stackval_arg_pos += sizeof(stackval);
	}

	/* return value storage */
	/* Align to dword */
	stackval_arg_pos = (stackval_arg_pos + (8 - 1)) & (~(8 -1));
	if (sig->param_count) {
		sparc_add_imm (p, 0, sparc_sp, stackval_arg_pos, sparc_l0);
	}
	if (!sig->ret->byref && sig->ret->type == MONO_TYPE_VALUETYPE && !sig->ret->data.klass->enumtype) {
#if !SPARCV9
		/* pass on callers buffer */
		sparc_ld_imm_ptr (p, sparc_fp, 64, sparc_l1);
		sparc_st_imm_ptr (p, sparc_l1, sparc_l0, 0);
#else
		sparc_add_imm (p, 0, sparc_l0, sizeof(stackval), sparc_l1);
		sparc_st_imm_ptr (p, sparc_l1, sparc_l0, 0);
#endif
	}

	sparc_st_imm_ptr (p, sparc_l0, sparc_sp,
		  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, retval)));

	/* call ves_exec_method */
	sparc_add_imm (p, 0, sparc_sp, MINV_POS, sparc_o0);
	sparc_set_ptr (p, (void *)ves_exec_method, sparc_l0);
	sparc_jmpl_imm (p, sparc_l0, 0, sparc_callsite);
	sparc_nop (p);

	/* move retval from stackval to proper place (r3/r4/...) */
        if (sig->ret->byref) {
		sparc_ld_imm_ptr (p, sparc_sp, stackval_arg_pos, sparc_i0 );
        } else {
        enum_retvalue:
                switch (sig->ret->type) {
                case MONO_TYPE_VOID:
                        break;
                case MONO_TYPE_BOOLEAN:
                case MONO_TYPE_I1:
                case MONO_TYPE_U1:
                case MONO_TYPE_I2:
                case MONO_TYPE_U2:
                case MONO_TYPE_I4:
                case MONO_TYPE_U4:
                        sparc_ld_imm (p, sparc_sp, stackval_arg_pos, sparc_i0);
                        break;
                case MONO_TYPE_I:
                case MONO_TYPE_U:
                case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
                case MONO_TYPE_CLASS:
                        sparc_ld_imm_ptr (p, sparc_sp, stackval_arg_pos, sparc_i0);
                        break;
                case MONO_TYPE_I8:
                case MONO_TYPE_U8:
#if SPARCV9
                        sparc_ldx_imm (p, sparc_sp, stackval_arg_pos, sparc_i0);
#else
                        sparc_ld_imm (p, sparc_sp, stackval_arg_pos, sparc_i0);
                        sparc_ld_imm (p, sparc_sp, stackval_arg_pos + 4, sparc_i1);
#endif
                        break;
                case MONO_TYPE_R4:
                        sparc_lddf_imm (p, sparc_sp, stackval_arg_pos, sparc_f0);
			sparc_fdtos(p, sparc_f0, sparc_f0);
                        break;
                case MONO_TYPE_R8:
                        sparc_lddf_imm (p, sparc_sp, stackval_arg_pos, sparc_f0);
                        break;
                case MONO_TYPE_VALUETYPE: {
			gint size;
			gint reg = sparc_i0;
                        if (sig->ret->data.klass->enumtype) {
                                simpletype = sig->ret->data.klass->enum_basetype->type;
                                goto enum_retvalue;
                        }
#if SPARCV9
                        size = mono_class_native_size (sig->ret->data.klass, NULL);
                        sparc_ldx_imm (p, sparc_sp, stackval_arg_pos, sparc_l0);
			if (size <= 16) {
				gint off = 0;
				if (size >= 8) {
		                        sparc_ldx_imm (p, sparc_l0, 0, reg);
					size -= 8;
					off += 8;
					reg++;
				}
				if (size > 0)
		                        sparc_ldx_imm (p, sparc_l0, off, reg);
			} else
	                        NOT_IMPL("value type as ret val from delegate");
#endif
                        break;
		}
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
	
	mono_jit_info_table_add (mono_get_root_domain (), ji);

	DEBUG(sparc_disassemble_code (code_buffer, p, method->name));

	DEBUG(fprintf(stderr, "Delegate [end emiting] %s\n", method->name));

	return ji->code_start;
}
