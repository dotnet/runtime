#ifndef __MONO_MINI_S390_H__
#define __MONO_MINI_S390_H__

#include <mono/arch/s390/s390-codegen.h>
#include <signal.h>

#define MONO_MAX_IREGS 16
#define MONO_MAX_FREGS 16

#define MONO_ARCH_FRAME_ALIGNMENT 8

#define MONO_EMIT_NEW_MOVE(cfg,dest,offset,src,imm,size) do { 			\
                MonoInst *inst; 						\
		int tmpr = 0;							\
		int sReg, dReg;							\
										\
		inst = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		if (size > 256) {						\
			tmpr = mono_regstate_next_int (cfg->rs);		\
			MONO_EMIT_NEW_ICONST(cfg,tmpr,size);			\
			inst->dreg	  = dest;				\
			inst->inst_offset = offset;				\
			inst->sreg1	  = src;				\
			inst->inst_imm	  = imm;				\
			inst->sreg2	  = tmpr;				\
		} else {							\
			if (s390_is_uimm12(offset)) {				\
				inst->dreg	  = dest;			\
				inst->inst_offset = offset;			\
			} else {						\
				dReg = mono_regstate_next_int (cfg->rs);	\
				MONO_EMIT_NEW_BIALU_IMM(cfg, OP_ADD_IMM,	\
					dReg, dest, offset);			\
				inst->dreg	  = dReg;			\
				inst->inst_offset = 0;				\
			}							\
			if (s390_is_uimm12(imm)) {  				\
				inst->sreg1	  = src; 			\
				inst->inst_imm    = imm;   			\
			} else {						\
				sReg = mono_regstate_next_int (cfg->rs);	\
				MONO_EMIT_NEW_BIALU_IMM(cfg, OP_ADD_IMM,	\
					sReg, src, imm);   			\
				inst->sreg1	  = sReg;			\
				inst->inst_imm    = 0;				\
			}							\
		}								\
                inst->opcode 	  = OP_S390_MOVE; 				\
		inst->unused	  = size;					\
	        mono_bblock_add_inst (cfg->cbb, inst); 				\
	} while (0)

#define MONO_OUTPUT_VTR(cfg, size, dr, sr, so) do {				\
	switch (size) {								\
		case 1:								\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU1_MEMBASE,	\
				dr, sr, so);					\
		break;								\
		case 2:								\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU2_MEMBASE,	\
				dr, sr, so);					\
		break;								\
		case 4:								\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOAD_MEMBASE,	\
				dr, sr, so);					\
		break;								\
		case 8:								\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOAD_MEMBASE,	\
				dr, sr, so);					\
			dr++; so += sizeof(guint32);				\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOAD_MEMBASE,	\
				dr, sr, so);					\
		break;								\
	}									\
} while (0)

#define MONO_OUTPUT_VTS(cfg, size, dr, dx, sr, so) do {				\
	int tmpr;								\
	switch (size) {								\
		case 1:								\
			tmpr = mono_regstate_next_int (cfg->rs);		\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU1_MEMBASE,	\
				tmpr, sr, so);					\
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,  \
				tmpr, dr, dx);					\
		break;								\
		case 2:								\
			tmpr = mono_regstate_next_int (cfg->rs);		\
			MONO_EMIT_NEW_LOAD_MEMBASE_OP(cfg, OP_LOADU2_MEMBASE,	\
				tmpr, sr, so);					\
			MONO_EMIT_NEW_STORE_MEMBASE(cfg, OP_STORE_MEMBASE_REG,  \
				tmpr, dr, dx);					\
		break;								\
		case 4:								\
		case 8:								\
			MONO_EMIT_NEW_MOVE (cfg, dr, dx, sr, so, size);		\
		break;								\
	}									\
} while (0)

/* fixme: align to 16byte instead of 32byte (we align to 32byte to get 
 * reproduceable results for benchmarks */
#define MONO_ARCH_CODE_ALIGNMENT 32

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	gulong      ebp;
	gulong      eip;
	gulong	    gregs[16];
	gdouble     fregs[16];
};

typedef struct ucontext MonoContext;

typedef struct MonoCompileArch {
} MonoCompileArch;

#define MONO_ARCH_EMULATE_FCONV_TO_I8 	1
#define MONO_ARCH_EMULATE_LCONV_TO_R8 	1
#define MONO_ARCH_EMULATE_LCONV_TO_R4 	1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_LMUL 		1
#define MONO_ARCH_HAVE_IS_INT_OVERFLOW  1
#define MONO_ARCH_NEED_DIV_CHECK	1

#define MONO_ARCH_USE_SIGACTION 	1

#define S390_STACK_ALIGNMENT		 8
#define S390_FIRST_ARG_REG 		s390_r2
#define S390_LAST_ARG_REG 		s390_r6
#define S390_FIRST_FPARG_REG 		s390_f0
#define S390_LAST_FPARG_REG 		s390_f2
#define S390_PASS_STRUCTS_BY_VALUE 	 1
#define S390_SMALL_RET_STRUCT_IN_REG	 1

#define S390_NUM_REG_ARGS (S390_LAST_ARG_REG-S390_FIRST_ARG_REG+1)
#define S390_NUM_REG_FPARGS (S390_LAST_FPARG_REG-S390_FIRST_FPARG_REG)

#define S390_OFFSET(b, t)	(guchar *) ((gint32) (b) - (gint32) (t))
#define S390_RELATIVE(b, t)     (guchar *) ((((gint32) (b) - (gint32) (t))) / 2)

#define CODEPTR(c, o) (o) = (short *) ((guint32) c - 2)
#define PTRSLOT(c, o) *(o) = (short) ((guint32) c - (guint32) (o) + 2)/2

#define S390_CC_EQ			8
#define S390_ALIGN(v, a)	(((a) > 0 ? (((v) + ((a) - 1)) & ~((a) - 1)) : (v)))

/*------------------------------------------------------------------*/
/*                                                                  */
/* Name		- s390_patch                                        */
/*                                                                  */
/* Function	- Patch the code with a given value.		    */
/*                                                                  */
/*------------------------------------------------------------------*/

static void inline
s390_patch (guchar *code, gint32 target)
{
	gint32 *offset = (gint32 *) code;
	
	if (target != 00) {
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
	s390_lr  (code, s390_r13, frame_reg);				\
									\
	lmfOffset = stack_usage -  sizeof(MonoLMF);			\
									\
	/*-------------------------------------------------*/		\
	/* r13 = my lmf					   */		\
	/*-------------------------------------------------*/		\
	s390_ahi (code, s390_r13, lmfOffset);				\
									\
	/*-------------------------------------------------*/		\
	/* r6 = &jit_tls->lmf				   */		\
	/*-------------------------------------------------*/		\
	s390_l   (code, s390_r6, 0, s390_r13, 				\
		  G_STRUCT_OFFSET(MonoLMF, lmf_addr));			\
									\
	/*-------------------------------------------------*/		\
	/* r0 = lmf.previous_lmf			   */		\
	/*-------------------------------------------------*/		\
	s390_l   (code, s390_r0, 0, s390_r13, 				\
		  G_STRUCT_OFFSET(MonoLMF, previous_lmf));		\
									\
	/*-------------------------------------------------*/		\
	/* jit_tls->lmf = previous_lmf			   */		\
	/*-------------------------------------------------*/		\
	s390_l   (code, s390_r13, 0, s390_r6, 0);			\
	s390_st  (code, s390_r0, 0, s390_r6, 0);			\
} while (0)

/*========================= End of Function ========================*/

#endif /* __MONO_MINI_S390_H__ */  
