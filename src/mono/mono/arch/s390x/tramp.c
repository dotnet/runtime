/*------------------------------------------------------------------*/
/* 								    */
/* Name        - tramp.c					    */
/* 								    */
/* Function    - Create trampolines to invoke arbitrary functions.  */
/* 								    */
/* Name	       - Neale Ferguson.				    */
/* 								    */
/* Date        - October, 2002					    */
/* 								    */
/* 								    */
/*------------------------------------------------------------------*/

/*------------------------------------------------------------------*/
/*                 D e f i n e s                                    */
/*------------------------------------------------------------------*/

#define PROLOG_INS 	24	/* Size of emitted prolog	    */
#define CALL_INS   	4	/* Size of emitted call 	    */
#define EPILOG_INS 	18	/* Size of emitted epilog	    */

#define DEBUG(x)

/*========================= End of Defines =========================*/

/*------------------------------------------------------------------*/
/*                 I n c l u d e s                                  */
/*------------------------------------------------------------------*/

#ifdef NEED_MPROTECT
# include <sys/mman.h>
# include <limits.h>    /* for PAGESIZE */
# ifndef PAGESIZE
#  define PAGESIZE 4096
# endif
#endif

#include "config.h"
#include <stdlib.h>
#include <string.h>
#include "s390x-codegen.h"
#include "mono/metadata/class.h"
#include "mono/metadata/tabledefs.h"
#include "mono/interpreter/interp.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/marshal.h"

/*========================= End of Includes ========================*/

/*------------------------------------------------------------------*/
/*                 T y p e d e f s                                  */
/*------------------------------------------------------------------*/

/*------------------------------------------------------------------*/
/* Structure used to accummulate size of stack, code, and locals    */
/*------------------------------------------------------------------*/
typedef struct {
	guint stack_size,
	      local_size,
	      code_size,
	      retStruct;
} size_data;

/*========================= End of Typedefs ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- add_general                                       */
/*                                                                  */
/* Function	- Determine code and stack size incremements for a  */
/* 		  parameter.                                        */
/*                                                                  */
/*------------------------------------------------------------------*/

static void inline
add_general (guint *gr, size_data *sz, gboolean simple)
{
	if (simple) {
		if (*gr >= GENERAL_REGS) {
			sz->stack_size += sizeof(long);
			sz->code_size  += 12;
		} else {
			sz->code_size += 8;
		}
	} else {
		if (*gr >= GENERAL_REGS - 1) {
			sz->stack_size += 8 + (sz->stack_size % 8);
			sz->code_size  += 10;
		} else {
			sz->code_size += 8;
		}
		(*gr) ++;
	}
	(*gr) ++;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- calculate_sizes                                   */
/*                                                                  */
/* Function	- Determine the amount of space required for code   */
/* 		  and stack. In addition determine starting points  */
/*		  for stack-based parameters, and area for struct-  */
/*		  ures being returned on the stack.		    */
/*                                                                  */
/*------------------------------------------------------------------*/

static void inline
calculate_sizes (MonoMethodSignature *sig, size_data *sz,
		 gboolean string_ctor)
{
	guint i, fr, gr, size;
	guint32 simpletype, align;

	fr             = 0;
	gr             = 2;
	sz->retStruct  = 0;
	sz->stack_size = S390_MINIMAL_STACK_SIZE;
	sz->code_size  = (PROLOG_INS + CALL_INS + EPILOG_INS);
	sz->local_size = 0;

	if (sig->hasthis) {
		add_general (&gr, sz, TRUE);
	}

	/*----------------------------------------------------------*/
	/* We determine the size of the return code/stack in case we*/
	/* need to reserve a register to be used to address a stack */
	/* area that the callee will use.			    */
	/*----------------------------------------------------------*/

	if (m_type_is_byref (sig->ret) || string_ctor) {
		sz->code_size += 8;
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
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_STRING:
			sz->code_size += 4;
			break;
		case MONO_TYPE_I8:
			sz->code_size += 4;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			gr++;
			if (sig->pinvoke && !sig->marshalling_disabled)
				size = mono_class_native_size (sig->ret->data.klass, &align);
			else
                        	size = mono_class_value_size (sig->ret->data.klass, &align);
			if (align > 1)
				sz->code_size += 10;
			switch (size) {
				/*----------------------------------*/
				/* On S/390, structures of size 1,  */
				/* 2, 4, and 8 bytes are returned   */
				/* in (a) register(s).		    */
				/*----------------------------------*/
				case 1:
				case 2:
				case 4:
				case 8:
					sz->code_size  += 16;
					sz->stack_size += 4;
					break;
				default:
					sz->retStruct   = 1;
					sz->code_size  += 32;
			}
                        break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/*----------------------------------------------------------*/
	/* We determine the size of the parameter code and stack    */
	/* requirements by checking the types and sizes of the      */
	/* parameters.						    */
	/*----------------------------------------------------------*/

	for (i = 0; i < sig->param_count; ++i) {
		if (m_type_is_byref (sig->params [i])) {
			add_general (&gr, sz, TRUE);
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
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
			add_general (&gr, sz, TRUE);
			break;
		case MONO_TYPE_SZARRAY:
			add_general (&gr, sz, TRUE);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			if (sig->pinvoke && !sig->marshalling_disabled)
				size = mono_class_native_size (sig->params [i]->data.klass, &align);
			else
				size = mono_class_value_size (sig->params [i]->data.klass, &align);
			DEBUG(printf("%d typesize: %d (%d)\n",i,size,align));
			switch (size) {
				/*----------------------------------*/
				/* On S/390, structures of size 1,  */
				/* 2, 4, and 8 bytes are passed in  */
				/* (a) register(s).		    */
				/*----------------------------------*/
				case 0:
				case 1:
				case 2:
				case 4:
					add_general(&gr, sz, TRUE);
					break;
				case 8:
					add_general(&gr, sz, FALSE);
					break;
				default:
					sz->local_size += (size + (size % align));
					sz->code_size  += 40;
			}
			break;
		case MONO_TYPE_I8:
			add_general (&gr, sz, FALSE);
			break;
		case MONO_TYPE_R4:
			if (fr < FLOAT_REGS) {
				sz->code_size += 4;
				fr++;
			}
			else {
				sz->code_size  += 4;
				sz->stack_size += 8;
			}
			break;
		case MONO_TYPE_R8:
			if (fr < FLOAT_REGS) {
				sz->code_size += 4;
				fr++;
			} else {
				sz->code_size  += 4;
				sz->stack_size += 8 + (sz->stack_size % 8);
			}
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}


	/* align stack size to 8 */
	DEBUG (printf ("      stack size: %d (%d)\n"
		       "       code size: %d\n"
		       "      local size: %d\n",
		      (sz->stack_size + 8) & ~8, sz->stack_size,
		      (sz->code_size),(sz->local_size + 8) & ~8));
	sz->stack_size = (sz->stack_size + 8) & ~8;
	sz->local_size = (sz->local_size + 8) & ~8;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- emit_prolog                                       */
/*                                                                  */
/* Function	- Create the instructions that implement the stand- */
/* 		  ard function prolog according to the S/390 ABI.   */
/*                                                                  */
/*------------------------------------------------------------------*/

static guint8 *
emit_prolog (guint8 *p, MonoMethodSignature *sig, size_data *sz)
{
	guint stack_size;

	stack_size = sz->stack_size + sz->local_size;

	/* function prolog */
	s390_stmg(p, s390_r6, s390_r14, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_lg	 (p, s390_r7, 0, STK_BASE, MINV_POS);
	s390_lgr (p, s390_r11, STK_BASE);
	s390_aghi(p, STK_BASE, -stack_size);
	s390_stg (p, s390_r11, 0, STK_BASE, 0);

	/*-----------------------------------------*/
	/* Save:				   */
	/* - address of "callme"                   */
	/* - address of "retval"		   */
	/* - address of "arguments"		   */
	/*-----------------------------------------*/
	s390_lgr (p, s390_r9, s390_r2);
	s390_lgr (p, s390_r8, s390_r3);
	s390_lgr (p, s390_r10, s390_r5);

	return p;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- emit_save_parameters                              */
/*                                                                  */
/* Function	- Create the instructions that load registers with  */
/*		  parameters, place others on the stack according   */
/* 		  to the S/390 ABI.				    */
/*                                                                  */
/*		  The resulting function takes the form:	    */
/* 		  void func (void (*callme)(), void *retval, 	    */
/*			     void *this_obj, stackval *arguments);  */
/*                                                                  */
/*------------------------------------------------------------------*/

inline static guint8*
emit_save_parameters (guint8 *p, MonoMethodSignature *sig, size_data *sz)
{
	guint i, fr, gr, act_strs, align,
	      stack_par_pos, size, local_pos;
	guint32 simpletype;

	/*----------------------------------------------------------*/
	/* If a structure on stack is being returned, reserve r2    */
	/* to point to an area where it can be passed.              */
	/*----------------------------------------------------------*/
	if (sz->retStruct)
		gr    = 1;
	else
		gr    = 0;
	fr            = 0;
	act_strs      = 0;
	stack_par_pos = S390_MINIMAL_STACK_SIZE;
	local_pos     = sz->stack_size;

	if (sig->hasthis) {
		s390_lr (p, s390_r2 + gr, s390_r4);
		gr++;
	}

	act_strs = 0;
	for (i = 0; i < sig->param_count; ++i) {
		DEBUG(printf("par: %d type: %d ref: %d\n",i,sig->params[i]->type,m_type_is_byref (sig->params[i])));
		if (m_type_is_byref (sig->params [i])) {
			if (gr < GENERAL_REGS) {
				s390_lg (p, s390_r2 + gr, 0, ARG_BASE, STKARG);
				gr ++;
			} else {
				s390_lg (p, s390_r0, 0, ARG_BASE, STKARG);
				s390_stg(p, s390_r0, 0, STK_BASE, stack_par_pos);
				stack_par_pos += sizeof(long);
			}
			continue;
		}
		simpletype = sig->params [i]->type;
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
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
			if (gr < GENERAL_REGS) {
				s390_lg (p, s390_r2 + gr, 0, ARG_BASE, STKARG);
				gr ++;
			} else {
				s390_lg (p, s390_r0, 0, ARG_BASE, STKARG);
				s390_stg(p, s390_r0, 0, STK_BASE, stack_par_pos);
			        stack_par_pos += sizeof(long);
			}
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}
			if (sig->pinvoke && !sig->marshalling_disabled)
				size = mono_class_native_size (sig->params [i]->data.klass, &align);
			else
				size = mono_class_value_size (sig->params [i]->data.klass, &align);
			DEBUG(printf("parStruct - size %d pinvoke: %d\n",size,sig->pinvoke));
			switch (size) {
				case 0:
				case 1:
				case 2:
				case 4:
					if (gr < GENERAL_REGS) {
						s390_lg (p, s390_r2 + gr, 0,ARG_BASE, STKARG);
						s390_lgf(p, s390_r2 + gr, 0, s390_r2 + gr, 0);
						gr++;
					} else {
						stack_par_pos += (stack_par_pos % align);
						s390_lg (p, s390_r10, 0,ARG_BASE, STKARG);
						s390_lgf(p, s390_r10, 0, s390_r10, 0);
						s390_st (p, s390_r10, 0, STK_BASE, stack_par_pos);
						stack_par_pos += sizeof(long);
					}
					break;
				case 8:
					if (gr < GENERAL_REGS) {
						s390_lg (p, s390_r2 + gr, 0, ARG_BASE, STKARG);
						s390_lg (p, s390_r2 + gr, 0, s390_r2 + gr, 0);
					} else {
						stack_par_pos += (stack_par_pos % align);
						s390_lg  (p, s390_r10, 0, ARG_BASE, STKARG);
						s390_mvc (p, sizeof(long long), STK_BASE, stack_par_pos, s390_r10, 0);
						stack_par_pos += sizeof(long long);
					}
					break;
				default:
					if (size <= 256) {
						local_pos += (local_pos % align);
						s390_lg  (p, s390_r13, 0, ARG_BASE, STKARG);
						s390_mvc (p, size, STK_BASE, local_pos, s390_r13, 0);
						s390_la	 (p, s390_r13, 0, STK_BASE, local_pos);
						local_pos += size;
					} else {
						local_pos += (local_pos % align);
						s390_bras (p, s390_r13, 4);
						s390_llong(p, size);
						s390_lg   (p, s390_r1, 0, s390_r13, 0);
						s390_lg   (p, s390_r0, 0, ARG_BASE, STKARG);
						s390_lgr  (p, s390_r14, s390_r12);
						s390_la   (p, s390_r12, 0, STK_BASE, local_pos);
						s390_lgr  (p, s390_r13, s390_r1);
						s390_mvcl (p, s390_r12, s390_r0);
						s390_lgr  (p, s390_r12, s390_r14);
						s390_la	  (p, s390_r13, 0, STK_BASE, local_pos);
						local_pos += size;
					}
					if (gr < GENERAL_REGS) {
						s390_lgr(p, s390_r2 + gr, s390_r13);
						gr++;
					} else {
						s390_stg(p, s390_r13, 0, STK_BASE, stack_par_pos);
						stack_par_pos += sizeof(long);
					}
			}
			break;
		case MONO_TYPE_I8:
			if (gr < GENERAL_REGS) {
				s390_lg (p, s390_r2 + gr, 0, ARG_BASE, STKARG);
				gr += 2;
			} else {
				*(guint32 *) p += 7;
				*(guint32 *) p &= ~7;
				s390_mvc (p, sizeof(long long), STK_BASE, stack_par_pos, ARG_BASE, STKARG);
				stack_par_pos += sizeof(long long) + (stack_par_pos % sizeof(long long));
			}
			break;
		case MONO_TYPE_R4:
			if (fr < FLOAT_REGS) {
				s390_le  (p, s390_r0 + fr, 0, ARG_BASE, STKARG);
				fr++;
			} else {
				s390_mvc  (p, sizeof(float), STK_BASE, stack_par_pos, ARG_BASE, STKARG);
				stack_par_pos += sizeof(float);
			}
			break;
		case MONO_TYPE_R8:
			if (fr < FLOAT_REGS) {
				s390_ld  (p, s390_r0 + fr, 0, ARG_BASE, STKARG);
				fr++;
			} else {
				*(guint32 *) p += 7;
				*(guint32 *) p &= ~7;
				s390_mvc  (p, sizeof(double), STK_BASE, stack_par_pos, ARG_BASE, STKARG);
				stack_par_pos += sizeof(long long) + (stack_par_pos % sizeof(long long));
			}
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	/*----------------------------------------------------------*/
	/* If we're returning a structure but not in a register     */
	/* then point the result area for the called routine        */
	/*----------------------------------------------------------*/
	if (sz->retStruct) {
		s390_lg (p, s390_r2, 0, s390_r8, 0);
	}

	return p;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- alloc_code_memory				    */
/*                                                                  */
/* Function	- Allocate space to place the emitted code.	    */
/*                                                                  */
/*------------------------------------------------------------------*/

static guint8 *
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
	DEBUG (printf ("           align: %p (%d)\n", p, (guint)p % 4));

	return p;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- emit_call_and_store_retval			    */
/*                                                                  */
/* Function	- Emit code that will implement the call to the     */
/*	          desired function, and unload the result according */
/*		  to the S390 ABI for the type of value returned    */
/*                                                                  */
/*------------------------------------------------------------------*/

static guint8 *
emit_call_and_store_retval (guint8 *p, MonoMethodSignature *sig,
			    size_data *sz, gboolean string_ctor)
{
	guint32 simpletype;
	guint   retSize, align;

	/* call "callme" */
	s390_basr (p, s390_r14, s390_r9);

	/* get return value */
	if (m_type_is_byref (sig->ret) || string_ctor) {
		s390_stg(p, s390_r2, 0, s390_r8, 0);
	} else {
		simpletype = sig->ret->type;
enum_retvalue:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			s390_stc (p, s390_r2, 0, s390_r8, 0);
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			s390_sth (p, s390_r2, 0, s390_r8, 0);
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
			s390_st (p, s390_r2, 0, s390_r8, 0);
			break;
		case MONO_TYPE_R4:
			s390_ste (p, s390_f0, 0, s390_r8, 0);
			break;
		case MONO_TYPE_R8:
			s390_std (p, s390_f0, 0, s390_r8, 0);
			break;
		case MONO_TYPE_I8:
			s390_stg (p, s390_r2, 0, s390_r8, 0);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			if (sig->pinvoke && !sig->marshalling_disabled)
				retSize = mono_class_native_size (sig->ret->data.klass, &align);
			else
				retSize = mono_class_value_size (sig->ret->data.klass, &align);
printf("Returning %d bytes for type %d (%d)\n",retSize,simpletype,sig->pinvoke);
			switch(retSize) {
			case 0:
				break;
			case 1:
				s390_stc (p, s390_r2, 0, s390_r8, 0);
				break;
			case 2:
				s390_sth (p, s390_r2, 0, s390_r8, 0);
				break;
			case 4:
				s390_st (p, s390_r2, 0, s390_r8, 0);
				break;
			case 8:
				s390_stg (p, s390_r2, 0, s390_r8, 0);
				break;
			default: ;
				/*------------------------------------------*/
				/* The callee has already placed the result */
				/* in the required area			    */
				/*------------------------------------------*/
			}
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x",
				 sig->ret->type);
		}
	}

	return p;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- emit_epilog                                       */
/*                                                                  */
/* Function	- Create the instructions that implement the stand- */
/* 		  ard function epilog according to the S/390 ABI.   */
/*                                                                  */
/*------------------------------------------------------------------*/

static guint8 *
emit_epilog (guint8 *p, MonoMethodSignature *sig, size_data *sz)
{
	/* function epilog */
	s390_lg  (p, STK_BASE, 0, STK_BASE, 0);
	s390_lg  (p, s390_r4, 0, STK_BASE, S390_RET_ADDR_OFFSET);
	s390_lmg (p, s390_r6, STK_BASE, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_br  (p, s390_r4);

	return p;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_trampoline.			    */
/*                                                                  */
/* Function	- Create the code that will allow a mono method to  */
/* 		  invoke a system subroutine.			    */
/*                                                                  */
/*------------------------------------------------------------------*/

MonoPIFunc
mono_arch_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor)
{
	guint8 *p, *code_buffer;
	size_data sz;

	DEBUG (printf ("\nPInvoke [start emitting]\n"));
	calculate_sizes (sig, &sz, string_ctor);

	p = code_buffer = alloc_code_memory (sz.code_size);
	p = emit_prolog (p, sig, &sz);
	p = emit_save_parameters (p, sig, &sz);
	p = emit_call_and_store_retval (p, sig, &sz, string_ctor);
	p = emit_epilog (p, sig, &sz);

#ifdef NEED_MPROTECT
	if (mprotect (code_buffer, 1024, PROT_READ | PROT_WRITE | PROT_EXEC)) {
		g_error ("Cannot mprotect trampoline\n");
	}
#endif

	DEBUG (printf ("emitted code size: %d\n", p - code_buffer));

	DEBUG (printf ("PInvoke [end emitting]\n"));

	return (MonoPIFunc) code_buffer;
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- mono_arch_create_method_pointer		    */
/*                                                                  */
/* Function	- Returns a pointer to a native function that can   */
/* 		  be used to call the specified method. 	    */
/*                                                                  */
/*		  The function created will receive the arguments   */
/*		  according to the calling convention specified in  */
/*                in the method. 				    */
/*		  						    */
/*		  This function works by creating a MonoInvocation  */
/*		  structure, filling the fields in and calling      */
/*		  ves_exec_method() on it.			    */
/*								    */
/* Logic:							    */
/* ------							    */
/*  mono_arch_create_method_pointer (MonoMethod *method)	    */
/*	create the unmanaged->managed wrapper		 	    */
/*	register it with mono_jit_info_table_add()		    */
/*								    */
/*  What does the unmanaged->managed wrapper do?	   	    */
/*	allocate a MonoInvocation structure (inv) on the stack	    */
/*      allocate an array of stackval on the stack with length =    */
/*          method->signature->param_count + 1 [call it stack_args] */
/*	set inv->ex, inv->ex_handler, inv->parent to NULL	    */
/*	set inv->method to method				    */
/*	if method is an instance method, set inv->obj to the 	    */
/*	    'this' argument (the first argument) else set to NULL   */
/*	for each argument to the method call:			    */
/*		stackval_from_data (sig->params[i], &stack_args[i], */
/*				    arg, sig->pinvoke);		    */
/*		Where:						    */
/*		------						    */
/*		sig 		- is method->signature		    */
/*	        &stack_args[i]	- is the pointer to the ith element */
/*				  in the stackval array		    */
/*		arg		- is a pointer to the argument re-  */
/*				  ceived by the function according  */
/*				  to the call convention. If it     */
/*				  gets passed in a register, save   */
/*				  on the stack first.		    */
/*								    */
/*	set inv->retval to the address of the last element of       */
/*	    stack_args [recall we allocated param_count+1 of them]  */
/*	call ves_exec_method(inv)				    */
/*	copy the returned value from inv->retval where the calling  */
/* 	    convention expects to find it on return from the wrap-  */
/*	    per [if it's a structure, use stackval_to_data]	    */
/*                                                                  */
/*------------------------------------------------------------------*/

void *
mono_arch_create_method_pointer (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoJitInfo *ji;
	guint8 *p, *code_buffer;
	guint i, align = 0, simple_type, retSize, reg_save = 0,
		stackval_arg_pos, local_pos, float_pos,
		local_start, reg_param = 0, stack_param,
		this_flag, arg_pos, fpr_param, parSize;
	guint32 simpletype;
	size_data sz;
	int *vtbuf, cpos, vt_cur;

	sz.code_size   = 1024;
	sz.stack_size  = 1024;
	stack_param     = 0;
	fpr_param       = 0;
	arg_pos	        = 0;

	sig = method->signature;

	p = code_buffer = g_malloc (sz.code_size);

	DEBUG (printf ("\nDelegate [start emitting] %s at 0x%08x\n",
		       method->name,p));

	/*----------------------------------------------------------*/
	/* prolog 					     	    */
	/*----------------------------------------------------------*/
	s390_stmg(p, s390_r6, STK_BASE, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_lg  (p, s390_r7, 0, STK_BASE, MINV_POS);
	s390_lgr (p, s390_r0, STK_BASE);
	s390_aghi(p, STK_BASE, -(sz.stack_size+MINV_POS));
	s390_stg (p, s390_r0, 0, STK_BASE, 0);
	s390_la	 (p, s390_r8, 0, STK_BASE, 4);
	s390_lgr (p, s390_r10, s390_r8);
	s390_lghi(p, s390_r9, sz.stack_size+92);
	s390_lghi(p, s390_r11, 0);
	s390_mvcl(p, s390_r8, s390_r10);

	/*----------------------------------------------------------*/
	/* Let's fill MonoInvocation - first zero some fields 	    */
	/*----------------------------------------------------------*/
	s390_lghi (p, s390_r0, 0);
	s390_stg  (p, s390_r0, 0, STK_BASE, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex)));
	s390_stg  (p, s390_r0, 0, STK_BASE, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, ex_handler)));
	s390_stg  (p, s390_r0, 0, STK_BASE, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, parent)));
	s390_lghi (p, s390_r0, 1);
	s390_stg  (p, s390_r0, 0, STK_BASE, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, invoke_trap)));

	/*----------------------------------------------------------*/
	/* set method pointer 					    */
	/*----------------------------------------------------------*/
	s390_bras (p, s390_r13, 4);
	s390_llong(p, method);
	s390_lg	  (p, s390_r0, 0, s390_r13, 0);
	s390_stg  (p, s390_r0, 0, STK_BASE, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, method)));

	local_start = local_pos = MINV_POS +
		      sizeof (MonoInvocation) + (sig->param_count + 1) * sizeof (stackval);
	this_flag   = (sig->hasthis ? 1 : 0);

	/*----------------------------------------------------------*/
	/* if we are returning a structure, checks it's length to   */
	/* see if there's a "hidden" parameter that points to the   */
	/* area. If necessary save this hidden parameter for later  */
	/*----------------------------------------------------------*/
	if (MONO_TYPE_ISSTRUCT(sig->ret)) {
		if (sig->pinvoke && !sig->marshalling_disabled)
			retSize = mono_class_native_size (sig->ret->data.klass, &align);
		else
			retSize = mono_class_value_size (sig->ret->data.klass, &align);
		switch(retSize) {
			case 0:
			case 1:
			case 2:
			case 4:
			case 8:
				sz.retStruct = 0;
				break;
			default:
				sz.retStruct = 1;
				s390_lgr(p, s390_r8, s390_r2);
				reg_save = 1;
		}
	} else  {
		reg_save = 0;
	}

	if (this_flag) {
		s390_stg (p, s390_r2 + reg_save, 0, STK_BASE,
			  (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, obj)));
		reg_param++;
	} else {
		s390_stg (p, s390_r2 + reg_save, 0, STK_BASE, local_pos);
		local_pos += sizeof(int);
		s390_stg (p, s390_r0, 0, STK_BASE,
			 (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, obj)));
	}

	s390_stmg (p, s390_r3 + reg_param, s390_r6, STK_BASE, local_pos);
	local_pos += 4 * sizeof(long);
	float_pos  = local_pos;
	s390_std (p, s390_f0, 0, STK_BASE, local_pos);
	local_pos += sizeof(double);
	s390_std (p, s390_f2, 0, STK_BASE, local_pos);
	local_pos += sizeof(double);

	/*----------------------------------------------------------*/
	/* prepare space for valuetypes			     	    */
	/*----------------------------------------------------------*/
	vt_cur = local_pos;
	vtbuf  = alloca (sizeof(int)*sig->param_count);
	cpos   = 0;
	for (i = 0; i < sig->param_count; i++) {
		MonoType *type = sig->params [i];
		vtbuf [i] = -1;
		DEBUG(printf("par: %d type: %d ref: %d\n",i,type->type,m_type_is_byref (type)));
		if (type->type == MONO_TYPE_VALUETYPE) {
			MonoClass *klass = type->data.klass;
			gint size;

			if (klass->enumtype)
				continue;
			if (sig->pinvoke && !sig->marshalling_disabled)
				size = mono_class_native_size (sig->ret->data.klass, &align);
			else
				size = mono_class_value_size (sig->ret->data.klass, &align);
			cpos += align - 1;
			cpos &= ~(align - 1);
			vtbuf [i] = cpos;
			cpos += size;
		}
	}
	cpos += 3;
	cpos &= ~3;

	local_pos += cpos;

	/*----------------------------------------------------------*/
	/* set MonoInvocation::stack_args			    */
	/*----------------------------------------------------------*/
	stackval_arg_pos = MINV_POS + sizeof (MonoInvocation);
	s390_la  (p, s390_r0, 0, STK_BASE, stackval_arg_pos);
	s390_stg (p, s390_r0, 0, STK_BASE, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, stack_args)));

	/*----------------------------------------------------------*/
	/* add stackval arguments 				    */
	/*----------------------------------------------------------*/
	for (i = 0; i < sig->param_count; ++i) {
		if (m_type_is_byref (sig->params [i])) {
			ADD_ISTACK_PARM(0, 1);
		} else {
			simple_type = sig->params [i]->type;
		enum_savechk:
			switch (simple_type) {
			case MONO_TYPE_I8:
				ADD_ISTACK_PARM(-1, 2);
				break;
			case MONO_TYPE_R4:
				ADD_RSTACK_PARM(1);
				break;
			case MONO_TYPE_R8:
				ADD_RSTACK_PARM(2);
				break;
			case MONO_TYPE_VALUETYPE:
				if (sig->params [i]->data.klass->enumtype) {
					simple_type = sig->params [i]->data.klass->enum_basetype->type;
					goto enum_savechk;
				}
				if (sig->pinvoke && !sig->marshalling_disabled)
					parSize = mono_class_native_size (sig->params [i]->data.klass, &align);
				else
					parSize = mono_class_value_size (sig->params [i]->data.klass, &align);
				switch(parSize) {
				case 0:
				case 1:
				case 2:
				case 4:
					ADD_PSTACK_PARM(0, 1);
					break;
				case 8:
					ADD_PSTACK_PARM(-1, 2);
					break;
				default:
					ADD_TSTACK_PARM;
				}
				break;
			default:
				ADD_ISTACK_PARM(0, 1);
			}
		}

		if (vtbuf [i] >= 0) {
			s390_la	 (p, s390_r3, 0, STK_BASE, vt_cur);
			s390_stg (p, s390_r3, 0, STK_BASE, stackval_arg_pos);
			s390_la	 (p, s390_r3, 0, STK_BASE, stackval_arg_pos);
			vt_cur += vtbuf [i];
		} else {
			s390_la  (p, s390_r3, 0, STK_BASE, stackval_arg_pos);
		}

		/*--------------------------------------*/
		/* Load the parameter registers for the */
		/* call to stackval_from_data		*/
		/*--------------------------------------*/
		s390_bras (p, s390_r13, 8);
		s390_llong(p, sig->params [i]);
		s390_llong(p, sig->pinvoke);
		s390_llong(p, stackval_from_data);
		s390_lg   (p, s390_r2, 0, s390_r13, 0);
		s390_lg	  (p, s390_r5, 0, s390_r13, 4);
		s390_lg	  (p, s390_r1, 0, s390_r13, 8);
		s390_basr (p, s390_r14, s390_r1);

		stackval_arg_pos += sizeof(stackval);

		/* fixme: alignment */
		DEBUG (printf ("arg_pos %d --> ", arg_pos));
		if (sig->pinvoke && !sig->marshalling_disabled)
			arg_pos += mono_type_native_stack_size (sig->params [i], &align);
		else
			arg_pos += mono_type_stack_size (sig->params [i], &align);

		DEBUG (printf ("%d\n", stackval_arg_pos));
	}

	/*----------------------------------------------------------*/
	/* Set return area pointer. 				    */
	/*----------------------------------------------------------*/
	s390_la (p, s390_r10, 0, STK_BASE, stackval_arg_pos);
	s390_stg(p, s390_r10, 0, STK_BASE, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, retval)));
	if (sig->ret->type == MONO_TYPE_VALUETYPE && !m_type_is_byref (sig->ret)) {
		MonoClass *klass  = sig->ret->data.klass;
		if (!klass->enumtype) {
			s390_la (p, s390_r9, 0, s390_r10, sizeof(stackval));
			s390_st (p, s390_r9, 0,STK_BASE, stackval_arg_pos);
			stackval_arg_pos += sizeof(stackval);
		}
	}

	/*----------------------------------------------------------*/
	/* call ves_exec_method					    */
	/*----------------------------------------------------------*/
	s390_bras (p, s390_r13, 4);
	s390_llong(p, ves_exec_method);
	s390_lg	  (p, s390_r1, 0, s390_r13, 0);
	s390_la	  (p, s390_r2, 0, STK_BASE, MINV_POS);
	s390_basr (p, s390_r14, s390_r1);

	/*----------------------------------------------------------*/
	/* move retval from stackval to proper place (r3/r4/...)    */
	/*----------------------------------------------------------*/
	DEBUG(printf("retType: %d byRef: %d\n",sig->ret->type,m_type_is_byref (sig->ret)));
	if (m_type_is_byref (sig->ret)) {
		DEBUG (printf ("ret by ref\n"));
		s390_stg(p, s390_r2, 0, s390_r10, 0);
	} else {
	enum_retvalue:
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U1:
			s390_lghi(p, s390_r2, 0);
			s390_ic  (p, s390_r2, 0, s390_r10, 0);
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			s390_lh (p, s390_r2, 0,s390_r10, 0);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			s390_lgf(p, s390_r2, 0, s390_r10, 0);
			break;
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_I8:
			s390_lg (p, s390_r2, 0, s390_r10, 0);
			break;
		case MONO_TYPE_R4:
			s390_le (p, s390_f0, 0, s390_r10, 0);
			break;
		case MONO_TYPE_R8:
			s390_ld (p, s390_f0, 0, s390_r10, 0);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			/*---------------------------------*/
			/* Call stackval_to_data to return */
			/* the structure		   */
			/*---------------------------------*/
			s390_bras (p, s390_r13, 8);
			s390_llong(p, sig->ret);
			s390_llong(p, sig->pinvoke);
			s390_llong(p, stackval_to_data);
			s390_lg	  (p, s390_r2, 0, s390_r13, 0);
			s390_lg	  (p, s390_r3, 0, STK_BASE, (MINV_POS + G_STRUCT_OFFSET (MonoInvocation, retval)));
			if (sz.retStruct) {
				/*------------------------------------------*/
				/* Get stackval_to_data to set result area  */
				/*------------------------------------------*/
				s390_lgr (p, s390_r4, s390_r8);
			} else {
				/*------------------------------------------*/
				/* Give stackval_to_data a temp result area */
				/*------------------------------------------*/
				s390_la (p, s390_r4, 0, STK_BASE, stackval_arg_pos);
			}
			s390_lg   (p, s390_r5, 0,s390_r13, 4);
			s390_lg   (p, s390_r1, 0, s390_r13, 8);
			s390_basr (p, s390_r14, s390_r1);
			switch (retSize) {
				case 0:
					break;
				case 1:
					s390_lghi(p, s390_r2, 0);
					s390_ic  (p, s390_r2, 0, s390_r10, 0);
					break;
				case 2:
					s390_lh (p, s390_r2, 0, s390_r10, 0);
					break;
				case 4:
					s390_lgf(p, s390_r2, 0, s390_r10, 0);
					break;
				case 8:
					s390_lg (p, s390_r2, 0, s390_r10, 0);
					break;
				default: ;
					/*-------------------------------------------------*/
					/* stackval_to_data has placed data in result area */
					/*-------------------------------------------------*/
			}
			break;
		default:
			g_error ("Type 0x%x not handled yet in thunk creation",
				 sig->ret->type);
			break;
		}
	}

	/*----------------------------------------------------------*/
	/* epilog 						    */
	/*----------------------------------------------------------*/
	s390_lg   (p, STK_BASE, 0, STK_BASE, 0);
	s390_lg   (p, s390_r4, 0, STK_BASE, S390_RET_ADDR_OFFSET);
	s390_lmg  (p, s390_r6, STK_BASE, STK_BASE, S390_REG_SAVE_OFFSET);
	s390_br   (p, s390_r4);

	DEBUG (printf ("emitted code size: %d\n", p - code_buffer));

	DEBUG (printf ("Delegate [end emitting]\n"));

	ji = g_new0 (MonoJitInfo, 1);
	ji->method = method;
	ji->code_size = p - code_buffer;
	ji->code_start = code_buffer;

	mono_jit_info_table_add (mono_get_root_domain (), ji);

	return ji->code_start;
}

/*========================= End of Function ========================*/
