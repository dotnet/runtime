#ifndef __MONO_MINI_S390X_H__
#define __MONO_MINI_S390X_H__

#include <mono/arch/s390x/s390x-codegen.h>
#include <mono/utils/mono-context.h>
#include <signal.h>

#define MONO_ARCH_CPU_SPEC s390x_cpu_desc

#define MONO_MAX_IREGS 16
#define MONO_MAX_FREGS 16

/*-------------------------------------------*/
/* Parameters used by the register allocator */
/*-------------------------------------------*/

#define S390_LONG(loc, opy, op, r, ix, br, off)					\
	if (mono_hwcap_s390x_has_ld) {						\
		if (s390_is_imm20(off)) {					\
			s390_##opy (loc, r, ix, br, off);			\
		} else {							\
			s390_basr (code, s390_r13, 0);				\
			s390_j    (code, 6);					\
			s390_llong(code, off);					\
			s390_lg   (code, s390_r13, 0, s390_r13, 4);		\
			s390_##op (code, r, s390_r13, br, 0);			\
		}								\
	} else {								\
		if (s390_is_uimm12(off)) {					\
			s390_##op (loc, r, ix, br, off);			\
		} else {							\
			s390_basr (code, s390_r13, 0);				\
			s390_j    (code, 6);					\
			s390_llong(code, off);					\
			s390_lg   (code, s390_r13, 0, s390_r13, 4);		\
			s390_##op (code, r, s390_r13, br, 0);			\
		}								\
	}

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

typedef struct MonoCompileArch {
	gpointer    litpool;
	glong	    litsize;
	int         bkchain_reg;
} MonoCompileArch;

typedef struct
{
	void *prev;
	void *unused[5];
	void *regs[8];
	void *return_address;
} MonoS390StackFrame;

typedef struct
{
	char	n3:1;		// N3 instructions present
	char	zArch:1;	// z/Architecture mode installed
	char	zAct:1;		// z/Architecture mode active
	char	date:1;		// DATE enhancement facility
	char	idte1:1;	// IDTE present (PST)
	char	idte2:1;	// IDTE present (REG)
	char	asnlx:1;	// ASN and LX reuse facility
	char	stfle:1;	// STFLE installed
	char	zDATe:1;	// Enhanced DAT in z mode
	char	srstat:1;	// Sense running status facility
	char	cSSKE:1;	// Conditional SSKE facility
	char	topo:1;		// COnfiguration topology facility
	char	xTrans2:1;	// Extended translation facility 2
	char	msgSec:1;	// Message security facility
	char	longDsp:1;	// Long displacement facility
	char	hiPerfLD:1;	// High performance long displacement facility
	char	hfpMAS:1;	// HFP multiply-and-add/subtrace facility
	char	xImm:1;		// Extended immediate facility
	char	xTrans3:1;	// Extended translation facility 3
	char	hfpUnX:1;	// HFP unnormalized extension facility
	char	etf2:1;		// ETF2-enhancement facility
	char	stckf:1;	// Store-clock-fast facility
	char	parse:1;	// Parsing enhancement facility
	char	mvcos:1;	// MVCOS facility
	char	todSteer:1;	// TOD-clock steering facility
	char	etf3:1;		// ETF3-enhancement facility
	char	xCPUtm:1;	// Extract CPU time facility
	char	csst:1;		// Compare-swap-and-store facility
	char	csst2:1;	// Compare-swap-and-store facility 2
	char	giX:1;		// General instructions extension facility
	char	exX:1;		// Execute extensions facility
	char	ibm:1;		// IBM internal use
	char	fps:1;		// Floating point support enhancement
	char	dfp:1;		// Decimal floating point facility
	char	hiDFP:1;	// High Performance DFP facility
	char	pfpo:1;		// PFPO instruction facility
} __attribute__((aligned(8))) facilityList_t;
	
// #define MONO_ARCH_SIGSEGV_ON_ALTSTACK		1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 		1
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS		1
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS		1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW  		1
#define MONO_ARCH_NEED_DIV_CHECK			1
#define MONO_ARCH_HAVE_ATOMIC_ADD 			1
#define MONO_ARCH_HAVE_ATOMIC_EXCHANGE 			1
#define MONO_ARCH_SIGNAL_STACK_SIZE 			256*1024
#define MONO_ARCH_HAVE_DECOMPOSE_OPTS 			1
#define MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE	1
#define MONO_ARCH_HAVE_IMT 				1
#define MONO_ARCH_HAVE_TLS_GET				1
#define MONO_ARCH_ENABLE_MONO_LMF_VAR			1
#define MONO_ARCH_IMT_REG				s390_r9
#define MONO_ARCH_VTABLE_REG				MONO_ARCH_IMT_REG
#define MONO_ARCH_RGCTX_REG				MONO_ARCH_IMT_REG
#define MONO_ARCH_THIS_AS_FIRST_ARG     		1
#define MONO_ARCH_HAVE_XP_UNWIND			1
#define MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX		1
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED			1
#define MONO_ARCH_HAVE_CONTEXT_SET_INT_REG		1
#define MONO_ARCH_USE_SIGACTION 			1
#define MONO_ARCH_GC_MAPS_SUPPORTED			1
#define MONO_ARCH_GSHARED_SUPPORTED			1
#define MONO_ARCH_MONITOR_ENTER_ADJUSTMENT		1

#define S390_STACK_ALIGNMENT		 8
#define S390_FIRST_ARG_REG 		s390_r2
#define S390_LAST_ARG_REG 		s390_r6
#define S390_FIRST_FPARG_REG 		s390_f0
#define S390_LAST_FPARG_REG 		s390_f6
#define S390_PASS_STRUCTS_BY_VALUE 	 1
#define S390_SMALL_RET_STRUCT_IN_REG	 1

#define S390_NUM_REG_ARGS (S390_LAST_ARG_REG-S390_FIRST_ARG_REG+1)
#define S390_NUM_REG_FPARGS ((S390_LAST_FPARG_REG-S390_FIRST_FPARG_REG)/2)

/*===============================================*/
/* Definitions used by mini-codegen.c            */
/*===============================================*/

/*------------------------------------------------------*/
/* use s390_r2-s390_r6 as parm registers                */
/* s390_r0, s390_r1, s390_r12, s390_r13 used internally */
/* s390_r8..s390_r11 are used for global regalloc       */
/* s390_r15 is the stack pointer                        */
/*------------------------------------------------------*/

#define MONO_ARCH_CALLEE_REGS (0xfc)

#define MONO_ARCH_CALLEE_SAVED_REGS 0xff80

/*----------------------------------------*/
/* use s390_f1/s390_f3-s390_f15 as temps  */
/*----------------------------------------*/

#define MONO_ARCH_CALLEE_FREGS (0xfffe)

#define MONO_ARCH_CALLEE_SAVED_FREGS 0

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0

#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 'o') ? s390_r2 : 		\
					((desc == 'g') ? s390_f0 : - 1))

#define MONO_ARCH_INST_IS_FLOAT(desc)  ((desc == 'f') || (desc == 'g'))

#define MONO_ARCH_INST_SREG2_MASK(ins) (0)

#define MONO_ARCH_INST_IS_REGPAIR(desc) (0)
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hr) -1

#define MONO_ARCH_IS_GLOBAL_IREG(reg) 0

#define MONO_ARCH_FRAME_ALIGNMENT 8
#define MONO_ARCH_CODE_ALIGNMENT 32

#define MONO_ARCH_RETREG1 s390_r2

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
		__asm__ volatile("lgr   %0,15" : "=r" (sframe));	\
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
