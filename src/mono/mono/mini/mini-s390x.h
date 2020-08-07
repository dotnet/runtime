/**
 * \file
 */

#ifndef __MONO_MINI_S390X_H__
#define __MONO_MINI_S390X_H__

#include <mono/arch/s390x/s390x-codegen.h>
#include <mono/utils/mono-context.h>
#include <signal.h>

#define MONO_ARCH_CPU_SPEC mono_s390x_cpu_desc

#define MONO_MAX_IREGS 16
#define MONO_MAX_FREGS 16

/*-------------------------------------------*/
/* Parameters used by the register allocator */
/*-------------------------------------------*/

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	gulong      ebp;
	gulong      eip;
	gulong	    pregs[6];
	gulong	    gregs[16];
	gdouble     fregs[16];
};

/**
 * Platform-specific compile control information
 */
typedef struct MonoCompileArch {
	int         bkchain_reg;	/** Register being used as stack backchain */
	uint32_t    used_fp_regs;	/** Floating point register use mask */
	int	    fpSize;		/** Size of floating point save area */
	MonoInst    *ss_tramp_var;	/** Single-step variable */
	MonoInst    *bp_tramp_var;	/** Breakpoint variable */
} MonoCompileArch;

typedef struct
{
	void *prev;
	void *unused[5];
	void *regs[8];
	void *return_address;
} MonoS390StackFrame;

/* Structure used by the sequence points */
struct SeqPointInfo {
	gpointer ss_tramp_addr;
	gpointer bp_addrs [MONO_ZERO_LEN_ARRAY];
};

#define MONO_ARCH_SIGSEGV_ON_ALTSTACK			1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 		1
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS		1
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS		1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW  		1
#define MONO_ARCH_NEED_DIV_CHECK			1
#define MONO_ARCH_SIGNAL_STACK_SIZE 			256*1024
#define MONO_ARCH_HAVE_DECOMPOSE_OPTS 			1
#define MONO_ARCH_IMT_REG				s390_r9
#define MONO_ARCH_VTABLE_REG				S390_FIRST_ARG_REG
#define MONO_ARCH_RGCTX_REG				MONO_ARCH_IMT_REG
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED			1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG		1
#define MONO_ARCH_USE_SIGACTION 			1
#define MONO_ARCH_GC_MAPS_SUPPORTED			1
#define MONO_ARCH_GSHARED_SUPPORTED			1
#define MONO_ARCH_MONITOR_ENTER_ADJUSTMENT		1
#define MONO_ARCH_HAVE_INVALIDATE_METHOD		1
#define MONO_ARCH_HAVE_OP_GENERIC_CLASS_INIT		1
#define MONO_ARCH_HAVE_SETUP_ASYNC_CALLBACK		1
#define MONO_ARCH_HAVE_TRACK_FPREGS			1
#define MONO_ARCH_HAVE_OPTIMIZED_DIV			1
#define MONO_ARCH_HAVE_OP_TAILCALL_MEMBASE		1
#define MONO_ARCH_HAVE_OP_TAILCALL_REG			1
#define MONO_ARCH_HAVE_SDB_TRAMPOLINES			1
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX	1

#define S390_STACK_ALIGNMENT		 8
#define S390_FIRST_ARG_REG 		s390_r2
#define S390_LAST_ARG_REG 		s390_r6
#define S390_FIRST_FPARG_REG 		s390_f0
#define S390_LAST_FPARG_REG 		s390_f6

#define S390_FP_SAVE_MASK		0xf0

/*===============================================*/
/* Definitions used by mini-codegen.c            */
/*===============================================*/

/*------------------------------------------------------*/
/* use s390_r2-s390_r6 as parm registers                */
/* s390_r0, s390_r1, s390_r12, s390_r13 used internally */
/* s390_r8..s390_r10 are used for global regalloc       */
/* -- except for s390_r9 which is used as IMT pointer   */
/* s390_r11 is sometimes used as the frame pointer      */
/* s390_r15 is the stack pointer                        */
/*------------------------------------------------------*/

#define MONO_ARCH_CALLEE_REGS (0x00fc)

#define MONO_ARCH_CALLEE_SAVED_REGS 0xfd00

/*----------------------------------------*/
/* use s390_f1/s390_f3-s390_f15 as temps  */
/*----------------------------------------*/

#define MONO_ARCH_CALLEE_FREGS (0xfffe)

#define MONO_ARCH_CALLEE_SAVED_FREGS 0

#define MONO_ARCH_USE_FPSTACK FALSE

#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 'o') ? s390_r2 : 		\
					((desc == 'g') ? s390_f0 : 		\
					((desc == 'A') ? S390_FIRST_ARG_REG : -1)))

#define MONO_ARCH_INST_IS_FLOAT(desc)  ((desc == 'f') || (desc == 'g'))

#define MONO_ARCH_INST_SREG2_MASK(ins) (0)

#define MONO_ARCH_INST_IS_REGPAIR(desc) (0)
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hr) -1

#define MONO_ARCH_IS_GLOBAL_IREG(reg) 0

#define MONO_ARCH_FRAME_ALIGNMENT 8
#define MONO_ARCH_CODE_ALIGNMENT 32

/*-----------------------------------------------*/
/* SIMD Related Definitions                      */
/*-----------------------------------------------*/

#define MONO_MAX_XREGS			31
#define MONO_ARCH_CALLEE_XREGS		0x0
#define MONO_ARCH_CALLEE_SAVED_XREGS	0x0

// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 1 // FIXME?

/*-----------------------------------------------*/
/* Macros used to generate instructions          */
/*-----------------------------------------------*/
#define S390_OFFSET(b, t)	(guchar *) ((guint64) (b) - (guint64) (t))
#define S390_RELATIVE(b, t)     (guchar *) ((((guint64) (b) - (guint64) (t))) / 2)

#define CODEPTR(c, o) (o) = (short *) ((guint64) c - 2)
#define PTRSLOT(c, o) *(o) = (short) ((guint64) c - (guint64) (o) + 2)/2

#define S390_CC_EQ			8
#define S390_ALIGN(v, a)	(((a) > 0 ? (((v) + ((a) - 1)) & ~((a) - 1)) : (v)))

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,func) do {			\
		MonoS390StackFrame *sframe;				\
		__asm__ volatile("lgr   %0,%%r15" : "=r" (sframe));	\
		MONO_CONTEXT_SET_BP ((ctx), sframe->prev);		\
		MONO_CONTEXT_SET_SP ((ctx), sframe->prev);		\
		MONO_CONTEXT_SET_IP ((ctx), func);			\
	} while (0)

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf) do { (lmf)->ebp = -1; } while (0)

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- s390_patch_rel                                    */
/*                                                                  */
/* Function	- Patch the code with a given offset. 		    */
/*                                                                  */
/*------------------------------------------------------------------*/

static void inline
s390_patch_rel (guchar *code, guint64 target)
{
	guint32 *offset = (guint32 *) code;
	
	if (target != 0) {
		*offset = (guint32) target;
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- s390_patch_addr                                   */
/*                                                                  */
/* Function	- Patch the code with a given address.		    */
/*                                                                  */
/*------------------------------------------------------------------*/

static void inline
s390_patch_addr (guchar *code, guint64 target)
{
	guint64 *offset = (guint64 *) code;
	
	if (target != 0) {
		*offset = target;
	}
}

/*========================= End of Function ========================*/

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- restoreLMF                                        */
/*                                                                  */
/* Function	- Restore the LMF state prior to exiting a method.  */
/*                                                                  */
/*------------------------------------------------------------------*/

#define restoreLMF(code, frame_reg, stack_usage) do			\
{									\
	int lmfOffset = 0;						\
									\
	s390_lgr (code, s390_r13, frame_reg);				\
									\
	lmfOffset = stack_usage -  sizeof(MonoLMF);			\
									\
	/*-------------------------------------------------*/		\
	/* r13 = my lmf					   */		\
	/*-------------------------------------------------*/		\
	s390_aghi (code, s390_r13, lmfOffset);				\
									\
	/*-------------------------------------------------*/		\
	/* r6 = &jit_tls->lmf				   */		\
	/*-------------------------------------------------*/		\
	s390_lg  (code, s390_r6, 0, s390_r13, 				\
		  G_STRUCT_OFFSET(MonoLMF, lmf_addr));			\
									\
	/*-------------------------------------------------*/		\
	/* r0 = lmf.previous_lmf			   */		\
	/*-------------------------------------------------*/		\
	s390_lg  (code, s390_r0, 0, s390_r13, 				\
		  G_STRUCT_OFFSET(MonoLMF, previous_lmf));		\
									\
	/*-------------------------------------------------*/		\
	/* jit_tls->lmf = previous_lmf			   */		\
	/*-------------------------------------------------*/		\
	s390_lg  (code, s390_r13, 0, s390_r6, 0);			\
	s390_stg (code, s390_r0, 0, s390_r6, 0);			\
} while (0)

/*========================= End of Function ========================*/

#endif /* __MONO_MINI_S390X_H__ */  
