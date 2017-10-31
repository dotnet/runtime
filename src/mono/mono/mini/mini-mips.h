/**
 * \file
 */

#ifndef __MONO_MINI_MIPS_H__
#define __MONO_MINI_MIPS_H__

#include <glib.h>
#include <mono/arch/mips/mips-codegen.h>
#include <mono/utils/mono-context.h>

#if _MIPS_SIM == _ABIO32
/* o32 fully supported */
#elif _MIPS_SIM == _ABIN32
/* n32 under development */
#warning "MIPS using n32 - under development"
#else
/* o64 not supported */
/* n64 not supported */
#error "MIPS unsupported ABI"
#endif


#define MONO_ARCH_CPU_SPEC mono_mips_desc

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

#define MONO_SAVED_GREGS 32
#define MONO_SAVED_FREGS 32


#if SIZEOF_REGISTER == 4
#define IREG_SIZE	4
#define FREG_SIZE	4
typedef gfloat		mips_freg;

#elif SIZEOF_REGISTER == 8

#define IREG_SIZE	8
#define FREG_SIZE	8
typedef gdouble		mips_freg;

#else
#error Unknown REGISTER_SIZE
#endif

/*
 * at and t0 used internally
 * v0, v1 aren't here for clarity reasons
 * a0, a1, a2, a3 are for arguments
 * Use t9 for indirect calls to match the ABI
 */

#define MIPS_V_REGS	((1 << mips_v0) | \
			 (1 << mips_v1))
#if _MIPS_SIM == _ABIO32
#define MIPS_T_REGS	((1 << mips_t0) | \
			 (1 << mips_t1) | \
			 (1 << mips_t2) | \
			 (1 << mips_t3) | \
			 (1 << mips_t4) | \
			 (1 << mips_t5) | \
			 (1 << mips_t6) | \
			 (1 << mips_t7))
#elif _MIPS_SIM == _ABIN32
#define MIPS_T_REGS	((1 << mips_t0) | \
			 (1 << mips_t1) | \
			 (1 << mips_t2) | \
			 (1 << mips_t3))
#endif


#define MIPS_S_REGS	((1 << mips_s0) | \
			 (1 << mips_s1) | \
			 (1 << mips_s2) | \
			 (1 << mips_s3) | \
			 (1 << mips_s4) | \
			 (1 << mips_s5) | \
			 (1 << mips_s6) | \
			 (1 << mips_s7) | \
			 (1 << mips_fp))
#if _MIPS_SIM == _ABIO32
#define MIPS_A_REGS	((1 << mips_a0) | \
			 (1 << mips_a1) | \
			 (1 << mips_a2) | \
			 (1 << mips_a3))
#elif _MIPS_SIM == _ABIN32
#define MIPS_A_REGS	((1 << mips_a0) | \
			 (1 << mips_a1) | \
			 (1 << mips_a2) | \
			 (1 << mips_a3) | \
			 (1 << mips_a4) | \
			 (1 << mips_a5) | \
			 (1 << mips_a6) | \
			 (1 << mips_a7))
#endif

#define mips_temp mips_t8

#define MONO_ARCH_CALLEE_REGS		(MIPS_T_REGS | MIPS_V_REGS | MIPS_A_REGS)
#define MONO_ARCH_CALLEE_SAVED_REGS	MIPS_S_REGS
#define MIPS_ARG_REGS			MIPS_A_REGS

#if 0
#define MIPS_FP_PAIR(reg)	((1 << (reg)) | (1 << ((reg)+1)))
#else
/* Only put the even regs in */
#define MIPS_FP_PAIR(reg)	(1 << (reg))
#endif

#if _MIPS_SIM == _ABIO32
#define MONO_ARCH_CALLEE_FREGS		(MIPS_FP_PAIR(mips_f0) |	\
					 MIPS_FP_PAIR(mips_f2) |	\
					 MIPS_FP_PAIR(mips_f4) |	\
					 MIPS_FP_PAIR(mips_f6) |	\
					 MIPS_FP_PAIR(mips_f8) |	\
					 MIPS_FP_PAIR(mips_f10) |	\
					 MIPS_FP_PAIR(mips_f12) |	\
					 MIPS_FP_PAIR(mips_f14) |	\
					 MIPS_FP_PAIR(mips_f16) |	\
					 MIPS_FP_PAIR(mips_f18))

#define MONO_ARCH_CALLEE_SAVED_FREGS	(MIPS_FP_PAIR(mips_f20) |	\
					 MIPS_FP_PAIR(mips_f22) |	\
					 MIPS_FP_PAIR(mips_f24) |	\
					 MIPS_FP_PAIR(mips_f26) |	\
					 MIPS_FP_PAIR(mips_f28) |	\
					 MIPS_FP_PAIR(mips_f30))
#elif _MIPS_SIM == _ABIN32
#define MONO_ARCH_CALLEE_FREGS		(MIPS_FP_PAIR(mips_f0) |	\
					 MIPS_FP_PAIR(mips_f1) |	\
					 MIPS_FP_PAIR(mips_f2) |	\
					 MIPS_FP_PAIR(mips_f3) |	\
					 MIPS_FP_PAIR(mips_f4) |	\
					 MIPS_FP_PAIR(mips_f5) |	\
					 MIPS_FP_PAIR(mips_f6) |	\
					 MIPS_FP_PAIR(mips_f7) |	\
					 MIPS_FP_PAIR(mips_f8) |	\
					 MIPS_FP_PAIR(mips_f9) |	\
					 MIPS_FP_PAIR(mips_f10) |	\
					 MIPS_FP_PAIR(mips_f11) |	\
					 MIPS_FP_PAIR(mips_f12) |	\
					 MIPS_FP_PAIR(mips_f13) |	\
					 MIPS_FP_PAIR(mips_f14) |	\
					 MIPS_FP_PAIR(mips_f15) |	\
					 MIPS_FP_PAIR(mips_f16) |	\
					 MIPS_FP_PAIR(mips_f17) |	\
					 MIPS_FP_PAIR(mips_f18) |	\
					 MIPS_FP_PAIR(mips_f19))

#define MONO_ARCH_CALLEE_SAVED_FREGS	(MIPS_FP_PAIR(mips_f20) |	\
					 MIPS_FP_PAIR(mips_f21) |	\
					 MIPS_FP_PAIR(mips_f22) |	\
					 MIPS_FP_PAIR(mips_f23) |	\
					 MIPS_FP_PAIR(mips_f24) |	\
					 MIPS_FP_PAIR(mips_f25) |	\
					 MIPS_FP_PAIR(mips_f26) |	\
					 MIPS_FP_PAIR(mips_f27) |	\
					 MIPS_FP_PAIR(mips_f28) |	\
					 MIPS_FP_PAIR(mips_f29) |	\
					 MIPS_FP_PAIR(mips_f30) |	\
					 MIPS_FP_PAIR(mips_f31))
#endif

#define mips_ftemp mips_f18

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0

/* Parameters used by the register allocator */

/* On Mips, for regpairs, the lower-numbered reg is most significant
 * This is true in both big and little endian
 */

#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define RET_REG1 mips_v0
#define RET_REG2 mips_v1
#else
#define RET_REG1 mips_v1
#define RET_REG2 mips_v0
#endif

#define MONO_ARCH_INST_SREG2_MASK(ins)		(0)
#define MONO_ARCH_INST_IS_REGPAIR(desc)		((desc) == 'V' || (desc) == 'l')
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (((desc) == 'l') ? ((hreg1) + 1) : (((desc) == 'V') ? RET_REG2 : -1))
#define MONO_ARCH_INST_IS_FLOAT(desc)		((desc == 'f') || (desc == 'g'))

// This define is called to get specific dest register as defined
// by md file (letter after "dest"). Overwise return -1

#define MONO_ARCH_INST_FIXED_REG(desc)		(((desc) == '0') ? mips_zero : (((desc) == 'a') ? mips_at : ((((desc) == 'v')) ? mips_v0 : (((desc) == 'V') ? RET_REG1 : (((desc) == 'g') ? mips_f0 : -1)))))

#define MONO_ARCH_FRAME_ALIGNMENT 8

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

void mips_patch (guint32 *code, guint32 target);

#define MIPS_LMF_MAGIC1	0xa5a5a5a5
#define MIPS_LMF_MAGIC2	0xc3c3c3c3

/* Registers saved in lmf->iregs */
#define MIPS_LMF_IREGMASK (0xffffffff & ~((1 << mips_zero) | (1 << mips_at) | MONO_ARCH_CALLEE_REGS))

struct MonoLMF {
	gpointer	previous_lmf;
	gpointer	lmf_addr;
	MonoMethod	*method;
	gpointer	eip;
	mgreg_t     iregs [MONO_SAVED_GREGS];
	mips_freg	fregs [MONO_SAVED_FREGS];
	gulong		magic;
};

typedef struct MonoCompileArch {
	gpointer    cinfo;
	guint		iregs_offset;
	guint		lmf_offset;
	guint		local_alloc_offset;
	guint		spillvar_offset;
	guint		spillvar_offset_float;
	guint		tracing_offset;
	guint		long_branch;
	gboolean    omit_fp;
	gboolean    omit_fp_computed;
} MonoCompileArch;

#if SIZEOF_REGISTER == 4
#define MONO_ARCH_EMULATE_FCONV_TO_I8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8 1
#define MONO_ARCH_EMULATE_LCONV_TO_R4 1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1
#endif

/*
 * mips backend misses some instructions that enable emitting of optimal
 * code on other targets and, additionally, the register allocator gets
 * confused by this optimization, failing to allocate all hw regs.
 */
#if SIZEOF_REGISTER == 4
#define MONO_ARCH_NO_DIV_WITH_MUL
#endif

#if SIZEOF_REGISTER == 8
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS
#endif

#define MIPS_RET_ADDR_OFFSET	(-sizeof(gpointer))
#define MIPS_FP_ADDR_OFFSET	(-8)
#define MIPS_STACK_ALIGNMENT	16
#define MIPS_STACK_PARAM_OFFSET 16		/* from sp to first parameter */
#define MIPS_MINIMAL_STACK_SIZE (4*sizeof(mgreg_t) + 4*sizeof(mgreg_t))
#define MIPS_EXTRA_STACK_SIZE	16		/* from last parameter to top of frame */

#if _MIPS_SIM == _ABIO32
#define MIPS_FIRST_ARG_REG	mips_a0
#define MIPS_LAST_ARG_REG	mips_a3
#define MIPS_FIRST_FPARG_REG	mips_f12
#define MIPS_LAST_FPARG_REG	mips_f14
#elif _MIPS_SIM == _ABIN32
#define MIPS_FIRST_ARG_REG	mips_a0
#define MIPS_LAST_ARG_REG	mips_t3
#define MIPS_FIRST_FPARG_REG	mips_f12
#define MIPS_LAST_FPARG_REG	mips_f19
#endif

#define MONO_ARCH_IMT_REG	mips_t0

#define MONO_ARCH_VTABLE_REG	mips_a0
#define MONO_ARCH_RGCTX_REG	MONO_ARCH_IMT_REG

#define MONO_ARCH_HAVE_DECOMPOSE_OPTS 1
#define MONO_ARCH_HAVE_DECOMPOSE_LONG_OPTS 1

#define MONO_ARCH_HAVE_GENERALIZED_IMT_TRAMPOLINE 1
#define MONO_ARCH_SOFT_DEBUG_SUPPORTED 1
#define MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX 1
#define MONO_ARCH_HAVE_SETUP_RESUME_FROM_SIGNAL_HANDLER_CTX 1
#define MONO_ARCH_GSHARED_SUPPORTED 1

/* set the next to 0 once inssel-mips.brg is updated */
#define MIPS_PASS_STRUCTS_BY_VALUE 1

#define MONO_ARCH_USE_SIGACTION
#define MONO_ARCH_NEED_DIV_CHECK 1
#define MONO_ARCH_NO_IOV_CHECK 1

#define MIPS_NUM_REG_ARGS (MIPS_LAST_ARG_REG-MIPS_FIRST_ARG_REG+1)
#define MIPS_NUM_REG_FPARGS (MIPS_LAST_FPARG_REG-MIPS_FIRST_FPARG_REG+1)

typedef struct {
	unsigned long zero;
	unsigned long at; /* assembler temp */
	unsigned long v0; /* return values */
	unsigned long v1;
	unsigned long a0; /* 4 - func arguments */
	unsigned long a1;
	unsigned long a2;
	unsigned long a3;
	unsigned long t0; /* 8 temporaries */
	unsigned long t1;
	unsigned long t2;
	unsigned long t3;
	unsigned long t4;
	unsigned long t5;
	unsigned long t6;
	unsigned long t7;
	unsigned long s0; /* 16 calle saved */
	unsigned long s1;
	unsigned long s2;
	unsigned long s3;
	unsigned long s4;
	unsigned long s5;
	unsigned long s6;
	unsigned long s7;
	unsigned long t8; /* 24 temps */
	unsigned long t9; /* 25 temp / pic call-through register */
	unsigned long k0; /* 26 kernel-reserved */
	unsigned long k1;
	unsigned long gp; /* 28 */
	unsigned long sp; /* stack pointer */
	unsigned long fp; /* frame pointer */
	unsigned long ra; /* return address */
} MonoMipsStackFrame;

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,func) do {	\
	MONO_CONTEXT_SET_BP ((ctx), __builtin_frame_address (0));			\
	MONO_CONTEXT_SET_SP ((ctx), __builtin_frame_address (0));			\
	MONO_CONTEXT_SET_IP ((ctx), (func));								\
	} while (0)

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf)

/* re-attaches with gdb - sometimes causes executable to hang */
#undef HAVE_BACKTRACE_SYMBOLS

#undef DEBUG_EXCEPTIONS

#define	MONO_EMIT_NEW_MIPS_COND_EXC(cfg,cond,sr1,sr2,name) do {	\
                MonoInst *inst; \
		MONO_INST_NEW ((cfg), (inst), cond); \
                inst->inst_p1 = (char*)name; \
		inst->sreg1 = sr1; \
		inst->sreg2 = sr2; \
		MONO_ADD_INS ((cfg)->cbb, inst); \
	} while (0)

#ifndef MONO_EMIT_NEW_COMPARE_EXC
#define MONO_EMIT_NEW_COMPARE_EXC(cfg, cmp_op, sreg1, sreg2, exc) do { \
		switch (OP_MIPS_COND_EXC_##cmp_op) { \
		case OP_MIPS_COND_EXC_EQ: \
			MONO_EMIT_NEW_MIPS_COND_EXC (cfg, OP_MIPS_COND_EXC_EQ, sreg1, sreg2, exc); \
			break; \
		case OP_MIPS_COND_EXC_NE_UN: \
			MONO_EMIT_NEW_MIPS_COND_EXC (cfg, OP_MIPS_COND_EXC_NE_UN, sreg1, sreg2, exc); \
			break; \
		case OP_MIPS_COND_EXC_GT: \
			MONO_EMIT_NEW_BIALU (cfg, OP_MIPS_SLT, mips_at, sreg2, sreg1); \
			MONO_EMIT_NEW_MIPS_COND_EXC (cfg, OP_MIPS_COND_EXC_NE_UN, mips_at, mips_zero, exc); \
			break; \
		case OP_MIPS_COND_EXC_GT_UN: \
			MONO_EMIT_NEW_BIALU (cfg, OP_MIPS_SLTU, mips_at, sreg2, sreg1); \
			MONO_EMIT_NEW_MIPS_COND_EXC (cfg, OP_MIPS_COND_EXC_NE_UN, mips_at, mips_zero, exc); \
			break; \
		case OP_MIPS_COND_EXC_LE: \
			MONO_EMIT_NEW_BIALU (cfg, OP_MIPS_SLT, mips_at, sreg2, sreg1); \
			MONO_EMIT_NEW_MIPS_COND_EXC (cfg, OP_MIPS_COND_EXC_EQ, mips_at, mips_zero, exc); \
			break; \
		case OP_MIPS_COND_EXC_LE_UN: \
			MONO_EMIT_NEW_BIALU (cfg, OP_MIPS_SLTU, mips_at, sreg2, sreg1); \
			MONO_EMIT_NEW_MIPS_COND_EXC (cfg, OP_MIPS_COND_EXC_EQ, mips_at, mips_zero, exc); \
			break; \
		case OP_MIPS_COND_EXC_LT: \
			MONO_EMIT_NEW_BIALU (cfg, OP_MIPS_SLT, mips_at, sreg1, sreg2); \
			MONO_EMIT_NEW_MIPS_COND_EXC (cfg, OP_MIPS_COND_EXC_NE_UN, mips_at, mips_zero, exc); \
			break; \
		case OP_MIPS_COND_EXC_LT_UN: \
			MONO_EMIT_NEW_BIALU (cfg, OP_MIPS_SLTU, mips_at, sreg1, sreg2); \
			MONO_EMIT_NEW_MIPS_COND_EXC (cfg, OP_MIPS_COND_EXC_NE_UN, mips_at, mips_zero, exc); \
			break; \
		default: \
			g_warning ("unknown comparison %s\n", #cmp_op); \
			g_assert_not_reached (); \
		} \
	} while (0)
#endif

#ifndef MONO_EMIT_NEW_COMPARE_IMM_EXC
#define MONO_EMIT_NEW_COMPARE_IMM_EXC(cfg, cmp_op, sreg1, imm, exc) do { \
		guint32 cmp_reg; \
		if (!(imm)) { \
			cmp_reg = mips_zero; \
		} \
		else { \
			cmp_reg = mips_at; \
			MONO_EMIT_NEW_ICONST (cfg, cmp_reg, (imm)); \
		} \
		MONO_EMIT_NEW_COMPARE_EXC (cfg, cmp_op, sreg1, cmp_reg, exc); \
	} while (0)
#endif

#ifndef MONO_EMIT_NEW_ICOMPARE_IMM_EXC
#define MONO_EMIT_NEW_ICOMPARE_IMM_EXC(cfg, cmp_op, sreg1, imm, exc) do { \
		MONO_EMIT_NEW_COMPARE_IMM_EXC(cfg, cmp_op, sreg1, imm, exc); \
	} while (0)
#endif

typedef struct {
	gint8 reg;
	gint8 size;
	int vtsize;
	int offset;
} MonoMIPSArgInfo;

extern guint8 *mips_emit_load_const(guint8 *code, int dreg, mgreg_t v);

#endif /* __MONO_MINI_MIPS_H__ */  
