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

// min SIZE = 48
// our call must look like this.

call_func:
	ldgp gp, 0(pv)
call_func..ng:
	.prologue
        lda sp, -SIZE(sp)  // grow stack SIZE bytes.
        stq ra, SIZE-48(sp)   // store ra
	stq fp, SIZE-40(sp)   // store fp (frame pointer)        
	stq a0, SIZE-32(sp)   // store args. a0 = func
	stq a1, SIZE-24(sp)   // a1 = retval
	stq a2, SIZE-16(sp)   // a2 = this
        stq a3, SIZE-8(sp)    // a3 = args
	mov sp, fp            // set frame pointer
	mov pv, a0            // func
	
	.calling_arg_this	
	mov a1, a2
	
	.calling_arg_6plus	
	ldq t0, POS(a3)
	stq t0, 0(sp)        
	ldq t1, POS(a3)
	stq t1, 8(sp)        
	... SIZE-56 ...

	mov zero,a1
        mov zero,a2
        mov zero,a3
        mov zero,a4
        mov zero,a5
	
	.do_call
	jsr ra, (pv)    // call func
	ldgp gp, 0(ra)  // restore gp.
	mov v0, t1      // move return value into t1
	
	.do_store_retval
	ldq t0, SIZE-24(fp) // load retval into t2
	stl t1, 0(t0)       // store value.

	.finished	
        mov fp,sp
        ldq ra,SIZE-48(sp)
        ldq fp,SIZE-40(sp)
        lda sp,SIZE(sp)
        ret zero,(ra),1
			
	
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
#define AXP_MIN_STACK_SIZE   24
#define ARG_SIZE   sizeof(stackval)
#define ARG_LOC(x) (x * sizeof( stackval ) )

/*****************************************************/

/*								    */
/* 		  void func (void (*callme)(), void *retval, 	    */
/*			     void *this_obj, stackval *arguments);  */
static inline guint8 *
emit_prolog (guint8 *p, const gint SIZE, int hasthis )
{
	// 9 instructions.
	alpha_ldah( p, alpha_gp, alpha_pv, 0 );  
	alpha_lda( p, alpha_sp, alpha_sp, -SIZE ); // grow stack down SIZE
	alpha_lda( p, alpha_gp, alpha_gp, 0 );     // ldgp gp, 0(pv)
	
	/* TODO: we really don't need to store everything.
	   alpha_a1: We have to store this in order to return the retval.
	   
	   alpha_a0: func pointer can be moved directly to alpha_pv
	   alpha_a3: don't need args after we are finished.
	   alpha_a2: will be moved into alpha_a0... if hasthis is true.
	*/
	/* store parameters on stack.*/
	alpha_stq( p, alpha_ra, alpha_sp, SIZE-24 ); // ra
	alpha_stq( p, alpha_fp, alpha_sp, SIZE-16 ); // fp
	alpha_stq( p, alpha_a1, alpha_sp, SIZE-8 );  // retval
	
	/* set the frame pointer */
	alpha_mov1( p, alpha_sp, alpha_fp );
       
	/* move the args into t0, pv */
	alpha_mov1( p, alpha_a0, alpha_pv );
	alpha_mov1( p, alpha_a3, alpha_t0 );

	// Move the this pointer into a0.	
	if( hasthis )
	    alpha_mov1( p, alpha_a2, alpha_a0 );
	return p;
}

static inline guint8 *
emit_call( guint8 *p , const gint SIZE )
{
	// 3 instructions
	/* call func */
	alpha_jsr( p, alpha_ra, alpha_pv, 0 );     // jsr ra, 0(pv)

	/* reload the gp */
	alpha_ldah( p, alpha_gp, alpha_ra, 0 );  
	alpha_lda( p, alpha_gp, alpha_gp, 0 );     // ldgp gp, 0(ra)
	
	return p;
}

static inline guint8 *
emit_store_return_default(guint8 *p, const gint SIZE )
{
	// 2 instructions.
	
	/* TODO: This probably do different stuff based on the value.  
	   you know, like stq/l/w. and s/f.
	*/
	alpha_ldq( p, alpha_t0, alpha_fp, SIZE-8 );  // load void * retval
	alpha_stq( p, alpha_v0, alpha_t0, 0 );       // store the result to *retval.
	return p;
}


static inline guint8 *
emit_epilog (guint8 *p, const gint SIZE )
{       
	// 5 instructions.
	alpha_mov1( p, alpha_fp, alpha_sp );

	/* restore fp, ra, sp */
	alpha_ldq( p, alpha_ra, alpha_sp, SIZE-24 ); 
	alpha_ldq( p, alpha_fp, alpha_sp, SIZE-16 ); 
	alpha_lda( p, alpha_sp, alpha_sp, SIZE ); 
	
	/* return */
	alpha_ret( p, alpha_ra, 1 );
	return p;
}

static void calculate_size(MonoMethodSignature *sig, int * INSTRUCTIONS, int * STACK )
{
	int alpharegs;
	
	alpharegs = AXP_GENERAL_REGS - (sig->hasthis?1:0);

	*STACK        = AXP_MIN_STACK_SIZE;
	*INSTRUCTIONS = 20;  // Base: 20 instructions.
	
	if( sig->param_count - alpharegs > 0 )
	{
		*STACK += ARG_SIZE * (sig->param_count - alpharegs );
		// plus 3 (potential) for each stack parameter. 
		*INSTRUCTIONS += ( sig->param_count - alpharegs ) * 3;
		// plus 2 (potential) for each register parameter. 
		*INSTRUCTIONS += ( alpharegs * 2 );
	}
	else
	{
		// plus 2 (potential) for each register parameter. 
		*INSTRUCTIONS += ( sig->param_count * 2 );
	}
}

MonoPIFunc
mono_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	unsigned char *p;
	unsigned char *buffer;
	MonoType* param;

	int i, pos;
	int alpharegs;
	int hasthis;
	int STACK_SIZE;
	int BUFFER_SIZE;
	int simple_type;
	int regbase;
	
	// Set up basic stuff.  like has this.	
	hasthis = !!sig->hasthis;
	alpharegs = AXP_GENERAL_REGS - hasthis;
	regbase  = hasthis?alpha_a1:alpha_a0 ;
	
	// Make a ballpark estimate for now.
	calculate_size( sig, &BUFFER_SIZE, &STACK_SIZE );
	
	// convert to the correct number of bytes.
	BUFFER_SIZE = BUFFER_SIZE * 4;

	
	// allocate.	
	buffer = p = malloc(BUFFER_SIZE);
	memset( buffer, 0, BUFFER_SIZE );
	pos = 0;
	
	// Ok, start creating this thing.
	p = emit_prolog( p, STACK_SIZE, hasthis );

	// copy everything into the correct register/stack space
	for (i = sig->param_count; --i >= 0; ) 
	{
		param = sig->params [i];
		
		if( param->byref )
		{
			if( i > alpharegs )
			{
				// load into temp register, then store on the stack 
				alpha_ldq( p, alpha_t1, alpha_t0, ARG_LOC( i ));
				alpha_stl( p, alpha_t1, alpha_sp, pos );
				pos += 8;
				
				if( pos > 128 )
					g_error( "Too large." );
			}
			else
			{
				// load into register
				alpha_ldq( p, regbase + i, alpha_t0, ARG_LOC( i ) );
			}
		}
		else
		{
			simple_type = param->type;
			if( simple_type == MONO_TYPE_VALUETYPE )
			{
                        	if (sig->ret->data.klass->enumtype)
                                	simple_type = sig->ret->data.klass->enum_basetype->type;
                        }
			
			switch (simple_type) 
			{
			case MONO_TYPE_VOID:
				break;
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
			case MONO_TYPE_I8:
				// 8 bytes
				if( i > alpharegs )
				{
					// load into temp register, then store on the stack
					alpha_ldq( p, alpha_t1, alpha_t0, ARG_LOC( i ) );
					alpha_stq( p, alpha_t1, alpha_sp, pos );
					pos += 8;
				}
				else
				{
					// load into register
					alpha_ldq( p, regbase + i, alpha_t0, ARG_LOC(i) );
				}
				break;
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
				/*
				// floating point... Maybe this does the correct thing.
				if( i > alpharegs )
				{
					alpha_ldq( p, alpha_t1, alpha_t0, ARG_LOC( i ) );
					alpha_cpys( p, alpha_ft1, alpha_ft1, alpha_ft2 );
					alpha_stt( p, alpha_ft2, alpha_sp, pos );
					pos += 8;
				}
				else
				{
					alpha_ldq( p, alpha_t1, alpha_t0, ARG_LOC(i) );
					alpha_cpys( p, alpha_ft1, alpha_ft1, alpha_fa0 + i + hasthis );
				}
				break;
				*/
			case MONO_TYPE_VALUETYPE:
				g_error ("Not implemented: ValueType as parameter to delegate." );
				break;
			default:
				g_error( "Not implemented." );
				break;	
			}
		}
	}
	
	// Now call the function and store the return parameter.
	p = emit_call( p, STACK_SIZE );
	p = emit_store_return_default( p, STACK_SIZE );
	p = emit_epilog( p, STACK_SIZE );

	if( p > buffer + BUFFER_SIZE )
		g_error( "Buffer overflow." );
	
	return (MonoPIFunc)buffer;
}

void *
mono_create_method_pointer (MonoMethod *method)
{
	g_error ("Unsupported arch");
	return NULL;
}
