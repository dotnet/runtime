/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
/*
 * Create trampolines to invoke arbitrary functions.
 * 
 * Copyright (C) Ximian Inc.
 *
 * Authors: Laramie Leavitt (lar@leavitt.us)
 *
 *
 */

/* A typical Alpha stack frame looks like this */
/*
fun:			     // called from outside the module.
        ldgp gp,0(pv)        // load the global pointer
fun..ng:		     // called from inside the module.
	lda sp, -SIZE( sp )  // grow the stack downwards.

	stq ra, 0(sp)        // save the return address.

	stq s0, 8(sp)        // callee-saved registers.
	stq s1, 16(sp)       // ...

	// Move the arguments to the argument registers...
	
    	mov addr, pv         // Load the callee address
	jsr  ra, (pv)        // call the method.
	ldgp gp, 0(ra)	     // restore gp

	// return value is in v0
	
	ldq ra, 0(sp)        // free stack frame
	ldq s0, 8(sp)        // restore callee-saved registers.
	ldq s1, 16(sp)       
	ldq sp, 32(sp)       // restore stack pointer

	ret zero, (ra), 1    // return.

// assuming that the procedure is in a0.
#define emit_prologue( p )  \
	alpha_ldah( p, alpha_gp, alpha_pv, 0 );	     \
	alpha_lda( p, alpha_sp, alpha_sp, -32 );   \
	alpha_lda( p, alpha_gp, alpha_gp, 0 );	    \
	alpha_stq( p, alpha_ra, alpha_sp, 0 );	    \
	alpha_stq( p, alpha_fp, alpha_sp, 8 );	    \
	alpha_mov( p, alpha_sp, alpha_fp )

#define emit_move_a0_to_pv( p ) \
	alpha_mov( p, alpha_a0, alpha_pv )

#define emit_call( p ) \
	alpha_jsr( p, alpha_ra, alpha_pv, 0 );    \
	alpha_ldah( p, alpha_gp, alpha_ra, 0 );	  \
	alpha_lda( p, alpha_gp, alpha_gp, 0 );	  \

#define emit_epilogue( p ) \
	alpha_mov( p, alpha_fp, alpha_sp ); \
	alpha_ldq( p, alpha_ra, alpha_sp, 0 ); \
	alpha_ldq( p, alpha_fp, alpha_sp, 8 ); \
	alpha_lda( p, alpha_sp, alpha_sp, 32 ); \
	alpha_ret( p, alpha_ra )

*/
/*****************************************************/

#include "config.h"
#include <stdlib.h>
#include <string.h>

#include "alpha-codegen.h"

#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/debug-helpers.h"

#define AXP_GENERAL_REGS     6
#define AXP_MIN_STACK_SIZE   32

#define PROLOG_INS	     6
#define CALL_INS             3
#define EPILOG_INS	     5

/*****************************************************/

typedef struct {
	guint i_regs;
	guint f_regs;
	guint stack_size;
	guint code_size;
} size_data;	


static char*
sig_to_name (MonoMethodSignature *sig, const char *prefix)
{
    /* from sparc.c.  this should be global */

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


static void inline
add_general ( size_data *sz, gboolean simple)
{
    // we don't really know yet, so just put something in here.
    if ( sz->i_regs >= AXP_GENERAL_REGS) 
    {
	sz->stack_size += 8;
    }

    // ...and it probably doesn't matter if our code size is a
    // little large...

    sz->code_size += 12;
    sz->i_regs ++;
}

static void
calculate_sizes (MonoMethodSignature *sig, 
		 size_data *sz, 
		 gboolean string_ctor)
{
	guint i, size;
	guint32 simpletype, align;

	sz->i_regs     = 0;
	sz->f_regs     = 0;
	sz->stack_size = AXP_MIN_STACK_SIZE;
	sz->code_size  = 4 * (PROLOG_INS + CALL_INS + EPILOG_INS);

	if (sig->hasthis) {
		add_general (&gr, sz, TRUE);
	}

	for (i = 0; i < sig->param_count; ++i) {
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
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
		case MONO_TYPE_I8:
			add_general (&gr, sz, TRUE);
			break;
		case MONO_TYPE_VALUETYPE:
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/* align stack size to 8 */
	sz->stack_size = (sz->stack_size + 8) & ~8;
	sz->local_size = (sz->local_size + 8) & ~8;
}

/*								    */
/* 		  void func (void (*callme)(), void *retval, 	    */
/*			     void *this_obj, stackval *arguments);  */
static inline guint8 *
emit_prolog (guint8 *p, MonoMethodSignature *sig, size_data *sz)
{
	guint stack_size;

	stack_size = sz->stack_size;

	/* function prolog */
	alpha_ldah( p, alpha_gp, alpha_pv, 0 );	     
	alpha_lda( p, alpha_sp, alpha_sp, -stack_size );   
	alpha_lda( p, alpha_gp, alpha_gp, 0  );

	/* save ra, fp */
	alpha_stq( p, alpha_ra, alpha_sp, 0  );
	alpha_stq( p, alpha_fp, alpha_sp, 8  );

	/* store the return parameter */
	alpha_stq( p, alpha_a0, alpha_sp, 16 );	
	alpha_stq( p, alpha_a1, alpha_sp, 24 );	

	/* load fp into sp */
	alpha_mov( p, alpha_sp, alpha_fp )

	return p;
}

static inline guint8 *
emit_epilog (guint8 *p, MonoMethodSignature *sig, size_data *sz)
{
        alpha_mov( p, alpha_fp, alpha_sp ); 

	/* restore fp, ra, sp */
	alpha_ldq( p, alpha_ra, alpha_sp, 0 ); 
	alpha_ldq( p, alpha_fp, alpha_sp, 8 ); 
	alpha_lda( p, alpha_sp, alpha_sp, 32 ); 
	
	/* return */
	alpha_ret( p, alpha_ra );
}

static inline guint8 *
emit_call( guint8 *p, MonoMethodSignature *sig, size_data *sz )
{
	/* move a0 into pv, ready to call */
	alpha_mov( p, alpha_a0, alpha_pv );
	
	/* call arg */
	alpha_jsr( p, alpha_ra, alpha_pv, 0 );

	/* reload the gp */
	alpha_ldah( p, alpha_gp, alpha_ra, 0 );	  
	alpha_lda( p, alpha_gp, alpha_gp, 0 );	  
}


MonoPIFunc
mono_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	g_error ("Unsupported arch");
	return NULL;
}

void *
mono_create_method_pointer (MonoMethod *method)
{
	g_error ("Unsupported arch");
	return NULL;
}

