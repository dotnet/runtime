/*
 * Create trampolines to invoke arbitrary functions.
 * 
 * Copyright (C) Radek Doulik
 * 
 */

#include "config.h"
#include <stdlib.h>
#include "ppc-codegen.h"
#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"

#ifdef NEED_MPROTECT
#include <sys/mman.h>
#include <limits.h>    /* for PAGESIZE */
#ifndef PAGESIZE
#define PAGESIZE 4096
#endif
#endif

/* void
fake_func (gdouble (*callme)(), stackval *retval, void *this_obj, stackval *arguments)
{
	guint32 i = 0xc002becd;

	callme = (gpointer) 0x100fabcd;

	*(gpointer*)retval = (gpointer)(*callme) (arguments [0].data.p, arguments [1].data.p, arguments [2].data.p);
	*(gdouble*) retval = (gdouble)(*callme) (arguments [0].data.f);
} */

#define MIN_CACHE_LINE 8

static void inline
flush_icache (guint8 *code, guint size)
{
	guint i;
	guint8 *p;

	p = code;
	for (i = 0; i < size; i += MIN_CACHE_LINE, p += MIN_CACHE_LINE) {
		asm ("dcbst 0,%0;" : : "r"(p) : "memory");
	}
	asm ("sync");
	p = code;
	for (i = 0; i < size; i += MIN_CACHE_LINE, p += MIN_CACHE_LINE) {
		asm ("icbi 0,%0; sync;" : : "r"(p) : "memory");
	}
	asm ("sync");
	asm ("isync");
}

#define NOT_IMPLEMENTED(x) \
                g_error ("FIXME: %s is not yet implemented. (trampoline)", x);

#define PROLOG_INS 8
#define CALL_INS   2
#define EPILOG_INS 6
#define MINIMAL_STACK_SIZE 5
#define FLOAT_REGS 8
#define GENERAL_REGS 8

static void inline
add_general (guint *gr, guint *stack_size, guint *code_size, gboolean simple)
{
	if (simple) {
		if (*gr >= GENERAL_REGS) {
			*stack_size += 4;
			*code_size += 8;    /* load from stack, save on stack */
		} else {
			*code_size += 4;    /* load from stack */
		}
	} else {
		if (*gr >= GENERAL_REGS - 1) {
			*stack_size += 8 + (*stack_size % 8);
			*code_size += 16;   /* 2x load from stack, save to stack */
		} else {
			*code_size += 16;   /* 2x load from stack */
		}
		(*gr) ++;
	}
	(*gr) ++;
}

static void inline
calculate_sizes (MonoMethod *method, guint *stack_size, guint *code_size, guint *strings, gint runtime)
{
	MonoMethodSignature *sig;
	guint i, fr, gr;
	guint32 simpletype;

	fr = gr = 0;
	*stack_size = MINIMAL_STACK_SIZE*4;
	*code_size  = (PROLOG_INS + CALL_INS + EPILOG_INS)*4;
	*strings = 0;

	sig = method->signature;
	if (sig->hasthis) {
		add_general (&gr, stack_size, code_size, TRUE);
	}

	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			add_general (&gr, stack_size, code_size, TRUE);
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
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
			add_general (&gr, stack_size, code_size, TRUE);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			if (mono_class_value_size (sig->params [i]->data.klass, NULL) != 4)
				g_error ("can only marshal enums, not generic structures (size: %d)",
					 mono_class_value_size (sig->params [i]->data.klass, NULL));
			add_general (&gr, stack_size, code_size, TRUE);
			*code_size += 4;
			break;
		case MONO_TYPE_STRING:
			if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime) {
				add_general (&gr, stack_size, code_size, TRUE);
				break;
			}
			(*strings) ++;
			*code_size += 12*4;
			*stack_size += 4;
			break;
		case MONO_TYPE_I8:
			add_general (&gr, stack_size, code_size, FALSE);
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (fr < 7) {
				*code_size += 4;
				fr ++;
			} else {
				NOT_IMPLEMENTED ("R8 arg");
			}
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	if (sig->ret->byref) {
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
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_STRING:
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
			NOT_IMPLEMENTED ("valuetype");
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	if (*strings) {
		/* space to keep parameters and prepared strings */
		 *stack_size += 8;
		 *code_size += 16;
		 if (sig->hasthis) {
			 *stack_size += 4;
			 *code_size  += 12;
		 }
	}
	/* align stack size to 16 */
	printf ("      stack size: %d (%d)\n       code size: %d\n", (*stack_size + 15) & ~15, *stack_size, *code_size);
	*stack_size = (*stack_size + 15) & ~15;

}

static inline guint8 *
emit_prolog (guint8 *p, MonoMethod *method, guint stack_size, guint strings)
{
	/* function prolog */
	ppc_stwu (p, ppc_r1, -stack_size, ppc_r1);     /* sp      <--- sp - stack_size, sp[0] <---- sp save sp, alloc stack */
	ppc_mflr (p, ppc_r0);                          /* r0      <--- LR */
	ppc_stw  (p, ppc_r31, stack_size - 4, ppc_r1); /* sp[+4]  <--- r31     save r31 */
	ppc_stw  (p, ppc_r0, stack_size + 4, ppc_r1);  /* sp[-4]  <--- LR      save return address for "callme" */
	ppc_mr   (p, ppc_r31, ppc_r1);                 /* r31     <--- sp */

	/* handle our parameters */
	if (strings) {
		ppc_stw  (p, ppc_r30, stack_size - 16, ppc_r1);
		ppc_stw  (p, ppc_r29, stack_size - 12, ppc_r1);
		if (method->signature->hasthis) {
			ppc_stw  (p, ppc_r28, 24, ppc_r1);
		}
		ppc_mr   (p, ppc_r30, ppc_r6);                        /* args */
		ppc_mr   (p, ppc_r29, ppc_r3);                        /* callme */
		if (method->signature->hasthis) {
			ppc_mr   (p, ppc_r28, ppc_r5);                /* this */
		}
	} else {
		ppc_mr   (p, ppc_r12, ppc_r6);                        /* keep "arguments" in register */
		ppc_mr   (p, ppc_r0, ppc_r3);                         /* keep "callme" in register */
	}
	ppc_stw  (p, ppc_r4, 8, ppc_r31);                             /* preserve "retval", sp[+8] */

	return p;
}

#define ARG_BASE strings ? ppc_r30 : ppc_r12
#define SAVE_4_IN_GENERIC_REGISTER \
			if (gr < GENERAL_REGS) { \
				ppc_lwz  (p, ppc_r3 + gr, i*16, ARG_BASE); \
				gr ++; \
			} else { \
				NOT_IMPLEMENTED("save on stack"); \
			}
#define SAVE_2_IN_GENERIC_REGISTER \
			if (gr < GENERAL_REGS) { \
				ppc_lhz  (p, ppc_r3 + gr, i*16, ARG_BASE); \
				gr ++; \
			} else { \
				NOT_IMPLEMENTED("save on stack"); \
			}
#define SAVE_1_IN_GENERIC_REGISTER \
			if (gr < GENERAL_REGS) { \
				ppc_lbz  (p, ppc_r3 + gr, i*16, ARG_BASE); \
				gr ++; \
			} else { \
				NOT_IMPLEMENTED("save on stack"); \
			}

inline static guint8*
emit_save_parameters (guint8 *p, MonoMethod *method, guint stack_size, guint strings, gint runtime)
{
	MonoMethodSignature *sig;
	guint i, fr, gr, act_strs;
	guint32 simpletype;

	fr = gr = 0;
	act_strs = 0;
	sig = method->signature;

	if (strings) {
		for (i = 0; i < sig->param_count; ++i) {
			if (!sig->params [i]->byref && sig->params [i]->type == MONO_TYPE_STRING
			    && !((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime)) {
				ppc_lis  (p, ppc_r0,     (guint32) mono_string_to_utf8 >> 16);
				ppc_lwz  (p, ppc_r3, i*16, ppc_r30);
				ppc_ori  (p, ppc_r0, ppc_r0, (guint32) mono_string_to_utf8 & 0xffff);
				ppc_mtlr (p, ppc_r0);
				ppc_blrl (p);
				ppc_stw  (p, ppc_r3, stack_size - 20 - act_strs, ppc_r31);
				act_strs += 4;
			}
		}
	}

	if (sig->hasthis) {
		ppc_mr (p, ppc_r3, ppc_r5);
		gr ++;
	}

	act_strs = 0;
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			SAVE_4_IN_GENERIC_REGISTER;
			continue;
		}
		simpletype = sig->params [i]->type;
	enum_calc_size:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			SAVE_1_IN_GENERIC_REGISTER;
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			SAVE_2_IN_GENERIC_REGISTER;
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
			SAVE_4_IN_GENERIC_REGISTER;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			if (mono_class_value_size (sig->params [i]->data.klass, NULL) != 4)
				g_error ("can only marshal enums, not generic structures (size: %d)",
					 mono_class_value_size (sig->params [i]->data.klass, NULL));
			if (gr < GENERAL_REGS) {
				ppc_lwz  (p, ppc_r3 + gr, i*16, ARG_BASE);
				ppc_lwz  (p, ppc_r3 + gr, 0, ppc_r3 + gr);
				gr ++;
			} else {
				NOT_IMPLEMENTED ("save value type on stack");
			}
			break;
		case MONO_TYPE_STRING:
			if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime) {
				SAVE_4_IN_GENERIC_REGISTER;
			} else {
				if (gr < 8) {
					ppc_lwz (p, ppc_r3 + gr, stack_size - 20 - act_strs, ppc_r31);
					gr ++;
					act_strs += 4;
				} else
					NOT_IMPLEMENTED ("string on stack");
			}
			break;
		case MONO_TYPE_I8:
			if (gr < 7) {
				g_warning ("check endianess");
				ppc_lwz  (p, ppc_r3 + gr, i*16, ARG_BASE);
				gr ++;
				ppc_lwz  (p, ppc_r3 + gr, i*17, ARG_BASE);
				gr ++;
			} else {
				NOT_IMPLEMENTED ("i8 on stack");
			}
			break;
		case MONO_TYPE_R4:
			if (fr < 7) {
				ppc_lfs  (p, ppc_f1 + fr, i*16, ARG_BASE);
				fr ++;
			} else {
				NOT_IMPLEMENTED ("r4 on stack");
			}
			break;
		case MONO_TYPE_R8:
			if (fr < 7) {
				ppc_lfd  (p, ppc_f1 + fr, i*16, ARG_BASE);
				fr ++;
			} else {
				NOT_IMPLEMENTED ("r8 on stack");
			}
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	return p;
}

static inline guint8 *
alloc_code_memory (guint code_size)
{
	guint8 *p;

#ifdef NEED_MPROTECT
	p = g_malloc (code_size + PAGESIZE - 1);

	/* Align to a multiple of PAGESIZE, assumed to be a power of two */
	p = (char *)(((int) p + PAGESIZE-1) & ~(PAGESIZE-1));
#else
	p = g_malloc (code_size);
#endif
	printf ("           align: %p (%d)\n", p, (guint)p % 4);

	return p;
}

static inline guint8 *
emit_call_and_store_retval (guint8 *p, MonoMethod *method, guint strings)
{
	MonoMethodSignature *sig = method->signature;
	guint32 simpletype;

	/* call "callme" */
	ppc_mtlr (p, strings ? ppc_r29 : ppc_r0);
	ppc_blrl (p);

	/* get return value */
	if (sig->ret->byref) {
		ppc_lwz  (p, ppc_r9, 8, ppc_r31);        /* load "retval" address */
		ppc_stw  (p, ppc_r3, 0, ppc_r9);         /* save return value (r3) to "retval" */
	} else {
		simpletype = sig->ret->type;
enum_retvalue:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			ppc_lwz  (p, ppc_r9, 8, ppc_r31);        /* load "retval" address */
			ppc_stb  (p, ppc_r3, 0, ppc_r9);         /* save return value (r3) to "retval" */
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			ppc_lwz  (p, ppc_r9, 8, ppc_r31);        /* load "retval" address */
			ppc_sth  (p, ppc_r3, 0, ppc_r9);         /* save return value (r3) to "retval" */
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
			ppc_lwz  (p, ppc_r9, 8, ppc_r31);        /* load "retval" address */
			ppc_stw  (p, ppc_r3, 0, ppc_r9);         /* save return value (r3) to "retval" */
			break;
		case MONO_TYPE_R4:
			ppc_lwz  (p, ppc_r9, 8, ppc_r31);        /* load "retval" address */
			ppc_stfs (p, ppc_f1, 0, ppc_r9);         /* save return value (f1) to "retval" */
			break;
		case MONO_TYPE_R8:
			ppc_lwz  (p, ppc_r9, 8, ppc_r31);        /* load "retval" address */
			ppc_stfd (p, ppc_f1, 0, ppc_r9);         /* save return value (f1) to "retval" */
			break;
		case MONO_TYPE_I8:
			g_warning ("check endianess");
			ppc_lwz  (p, ppc_r9, 8, ppc_r31);        /* load "retval" address */
			ppc_stw  (p, ppc_r3, 0, ppc_r9);         /* save return value (r3) to "retval" */
			ppc_stw  (p, ppc_r4, 4, ppc_r9);         /* save return value (r3) to "retval" */
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			NOT_IMPLEMENTED ("retval valuetype");
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	return p;
}

static inline guint8 *
emit_epilog (guint8 *p, MonoMethod *method, guint stack_size, guint strings, gboolean runtime)
{
	if (strings) {
		MonoMethodSignature *sig = method->signature;
		guint i, act_strs;

		/* free allocated memory */
		act_strs = 0;
		for (i = 0; i < sig->param_count; ++i) {
			if (!sig->params [i]->byref && sig->params [i]->type == MONO_TYPE_STRING
			    && !((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || runtime)) {
				ppc_lis  (p, ppc_r0,     (guint32) g_free >> 16);
				ppc_lwz  (p, ppc_r3, stack_size - 20 - act_strs, ppc_r31);
				ppc_ori  (p, ppc_r0, ppc_r0, (guint32) g_free & 0xffff);
				ppc_mtlr (p, ppc_r0);
				ppc_blrl (p);
				act_strs += 4;
			}
		}

		/* restore volatile registers */
		ppc_lwz  (p, ppc_r30, stack_size - 16, ppc_r1);
		ppc_lwz  (p, ppc_r29, stack_size - 12, ppc_r1);
		if (method->signature->hasthis) {
			ppc_lwz  (p, ppc_r28, 24, ppc_r1);
		}
	}

	/* function epilog */
	ppc_lwz  (p, ppc_r11, 0,  ppc_r1);        /* r11     <--- sp[0]   load backchain from caller's function */
	ppc_lwz  (p, ppc_r0, 4, ppc_r11);         /* r0      <--- r11[4]  load return address */
	ppc_mtlr (p, ppc_r0);                     /* LR      <--- r0      set return address */
	ppc_lwz  (p, ppc_r31, -4, ppc_r11);       /* r31     <--- r11[-4] restore r31 */
	ppc_mr   (p, ppc_r1, ppc_r11);            /* sp      <--- r11     restore stack */
	ppc_blr  (p);                             /* return */

	return p;
}

MonoPIFunc
mono_create_trampoline (MonoMethod *method, int runtime)
{
	guint8 *p, *code_buffer;
	guint stack_size, code_size, strings;

	printf ("\nPInvoke [start emiting] %s\n", method->name);
	calculate_sizes (method, &stack_size, &code_size, &strings, runtime);

	p = code_buffer = alloc_code_memory (code_size);
	p = emit_prolog (p, method, stack_size, strings);
	p = emit_save_parameters (p, method, stack_size, strings, runtime);
	p = emit_call_and_store_retval (p, method, strings);
	p = emit_epilog (p, method, stack_size, strings, runtime);

	/* {
		guchar *cp;
		printf (".text\n.align 4\n.globl main\n.type main,@function\nmain:\n");
		for (cp = code_buffer; cp < p; cp++) {
			printf (".byte 0x%x\n", *cp);
		}
		} */

#ifdef NEED_MPROTECT
	if (mprotect (code_buffer, 1024, PROT_READ | PROT_WRITE | PROT_EXEC)) {
		g_error ("Cannot mprotect trampoline\n");
	}
#endif

	printf ("emited code size: %d\n", p - code_buffer);
	flush_icache (code_buffer, p - code_buffer);

	printf ("PInvoke [end emiting]\n");

	return (MonoPIFunc) code_buffer;
	/* return fake_func; */
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
	return NULL;
}


/*
 * mono_create_method_pointer () will insert a pointer to the MonoMethod
 * so that the interp can easily get at the data: this function will retrieve 
 * the method from the code stream.
 */
MonoMethod*
mono_method_pointer_get (void *code)
{
	return NULL;
}

