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
fake_func (gpointer (*callme)(), void *retval, void *this_obj, stackval *arguments)
{
	*(gpointer*)retval = (*callme) (arguments [0].data.p, arguments [1].data.p, arguments [2].data.p);
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

#define NOT_IMPLEMENTED \
                g_error ("FIXME: Not yet implemented. (trampoline)");

#define PROLOG_INS 8
#define CALL_INS   2
#define EPILOG_INS 6
#define MINIMAL_STACK_SIZE 4
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
calculate_sizes (MonoMethod *method, guint *stack_size, guint *code_size)
{
	MonoMethodSignature *sig;
	guint i, fr, gr;
	guint32 simpletype;

	fr = gr = 0;
	*stack_size = MINIMAL_STACK_SIZE*4;
	*code_size  = (PROLOG_INS + CALL_INS + EPILOG_INS)*4;

	sig = method->signature;
	if (sig->hasthis) {
		add_general (&gr, stack_size, code_size, TRUE);
	}

	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			g_error ("FIXME, trampoline: byref");
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
			break;
		case MONO_TYPE_STRING:
			NOT_IMPLEMENTED;
			break;
		case MONO_TYPE_I8:
			add_general (&gr, stack_size, code_size, FALSE);
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			NOT_IMPLEMENTED;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	if (sig->ret->byref) {
		g_error ("trampoline, retval byref - TODO");
	} else {
		simpletype = sig->ret->type;
enum_retvalue:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			*code_size += 8;
			break;
		case MONO_TYPE_STRING:
			NOT_IMPLEMENTED;
			break;
		case MONO_TYPE_I8:
			*code_size += 12;
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

	/* align stack size to 16 */
	printf ("      stack size: %d (%d)\n       code size: %d\n", (*stack_size + 15) & ~15, *stack_size, *code_size);
	*stack_size = (*stack_size + 15) & ~15;

}

static inline guint8 *
emit_prolog (guint8 *p, guint stack_size)
{
	/* function prolog */
	stwu (p, r1, -stack_size, r1);            /* sp      <--- sp - 48, sp[0] <---- sp     save sp, allocate stack */
	mflr (p, r0);                             /* r0      <--- LR */
	stw  (p, r31, stack_size - 4, r1);        /* sp[44]  <--- r31     save r31 */
	stw  (p, r0, stack_size + 4, r1);         /* sp[52]  <--- LR      save return address in "callme" stack frame */
	mr   (p, r31, r1);                        /* r31     <--- sp */

	/* handle our parameters */
	mr   (p, r14, r6);                        /* keep "arguments" in register */
	mr   (p, r0, r3);                         /* keep "callme" in register */
	stw  (p, r4, 8, r31);                     /* preserve "retval", sp[8] */

	return p;
}

#define SAVE_4_IN_GENERIC_REGISTER \
			if (gr < GENERAL_REGS) { \
				lwz  (p, r3 + gr, i*16, r14); \
				gr ++; \
			} else { \
				NOT_IMPLEMENTED; \
			}

inline static guint8*
emit_save_parameters (guint8 *p, MonoMethod *method)
{
	MonoMethodSignature *sig;
	guint i, fr, gr;
	guint32 simpletype;

	fr = gr = 0;

	sig = method->signature;
	if (sig->hasthis) {
		g_warning ("FIXME: trampoline, decide on MONO_CALL_THISCALL");
		mr (p, r3, r5);
		gr ++;
	}

	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			g_error ("FIXME, trampoline: byref");
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
			SAVE_4_IN_GENERIC_REGISTER;
			break;
		case MONO_TYPE_STRING:
			NOT_IMPLEMENTED;
			break;
		case MONO_TYPE_I8:
			if (gr < 7) {
				g_warning ("check endianess");
				lwz  (p, r3 + gr, i*16, r14);
				gr ++;
				lwz  (p, r3 + gr, i*17, r14);
				gr ++;
			} else {
				NOT_IMPLEMENTED;
			}
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			NOT_IMPLEMENTED;
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
emit_call_and_store_retval (guint8 *p, MonoMethod *method)
{
	/* call "callme" */
	mtlr (p, r0);
	blrl (p);

	/* get return value */
	lwz  (p, r9, 8, r31);        /* load "retval" address */
	stw  (p, r3, 0, r9);          /* save return value (r3) to "retval" */

	return p;
}

static inline guint8 *
emit_epilog (guint8 *p)
{
	/* function epilog */
	lwz  (p, r11, 0,  r1);        /* r11     <--- sp[0]   load backchain from caller's function */
	lwz  (p, r0, 4, r11);         /* r0      <--- r11[4]  load return address */
	mtlr (p, r0);                 /* LR      <--- r0      set return address */
	lwz  (p, r31, -4, r11);       /* r31     <--- r11[-4] restore r31 */
	mr   (p, r1, r11);            /* sp      <--- r11     restore stack */
	blr  (p);                     /* return */

	return p;
}

MonoPIFunc
mono_create_trampoline (MonoMethod *method)
{
	guint8 *p, *code_buffer;
	guint stack_size, code_size;

	printf ("\nPInvoke [start emiting]\n");

	calculate_sizes (method, &stack_size, &code_size);

	p = code_buffer = alloc_code_memory (code_size);
	p = emit_prolog (p, stack_size);
	p = emit_save_parameters (p, method);
	p = emit_call_and_store_retval (p, method);
	p = emit_epilog (p);

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


