/**
 * \file
 * Constant folding support
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.  http://www.ximian.com
 */
#include <config.h>

#include "mini.h"
#include "ir-emit.h"

/* WTF is this doing here?!?!? */
int
mono_is_power_of_two (guint32 val)
{
	int i, j, k;

	for (i = 0, j = 1, k = 0xfffffffe; i < 32; ++i, j = j << 1, k = k << 1) {
		if (val & j)
			break;
	}
	if (i == 32 || val & k)
		return -1;
	return i;
}

#ifndef G_MININT32
#define MYGINT32_MAX 2147483647
#define G_MININT32 (-MYGINT32_MAX -1)
#endif

#define FOLD_UNOP(name,op)	\
	case name:	\
	    dest->inst_c0 = op arg1->inst_c0; \
        break;

#define FOLD_BINOP(name, op) \
	case name:	\
	    dest->inst_c0 = arg1->inst_c0 op arg2->inst_c0;	\
        break;

#define FOLD_BINOPC(name,op,cast)	\
	case name:	\
	    dest->inst_c0 = (cast)arg1->inst_c0 op (cast)arg2->inst_c0;	\
        break;

#define FOLD_BINOP2_IMM(name, op) \
	case name:	\
	    dest->inst_c0 = arg1->inst_c0 op ins->inst_imm;	\
        break;

#define FOLD_BINOPC2_IMM(name, op, cast) \
	case name:	\
	    dest->inst_c0 = (cast)arg1->inst_c0 op (cast)ins->inst_imm;	\
        break;

#define FOLD_BINOPCXX(name,op,cast)	\
	case name:	\
	    res = (cast)arg1->inst_c0 op (cast)arg2->inst_c0;	\
        break; \

#define ALLOC_DEST(cfg, dest, ins) do { \
    if (!(dest)) { \
        MONO_INST_NEW ((cfg), (dest), -1); \
        (dest)->dreg = (ins)->dreg; \
    } \
} while (0)

#ifndef DISABLE_JIT

/**
 * mono_constant_fold_ins:
 *
 * Perform constant folding on INS, using ARG1 and ARG2 as the arguments. If OVERWRITE is
 * true, then store the result back into INS and return INS. Otherwise allocate a new ins,
 * store the result into it and return it. If constant folding cannot be performed, return
 * NULL.
 */
MonoInst*
mono_constant_fold_ins (MonoCompile *cfg, MonoInst *ins, MonoInst *arg1, MonoInst *arg2, gboolean overwrite)
{
	MonoInst *dest = NULL;

	if (overwrite)
		dest = ins;

	switch (ins->opcode) {
	case OP_IMUL:
	case OP_IADD:
	case OP_IAND:
	case OP_IOR:
	case OP_IXOR:
		if (arg2->opcode == OP_ICONST) {
			if (arg1->opcode == OP_ICONST) {
				ALLOC_DEST (cfg, dest, ins);
				switch (ins->opcode) {
					FOLD_BINOP (OP_IMUL, *);
					FOLD_BINOP (OP_IADD, +);
					FOLD_BINOP (OP_IAND, &);
					FOLD_BINOP (OP_IOR, |);
					FOLD_BINOP (OP_IXOR, ^);
				}
				dest->opcode = OP_ICONST;
				MONO_INST_NULLIFY_SREGS (dest);
			}
		} else if (arg1->opcode == OP_ICONST) {
			/* 
			 * This is commutative so swap the arguments, allowing the _imm variant
			 * to be used later.
			 */
			if (mono_op_to_op_imm (ins->opcode) != -1) {
				ALLOC_DEST (cfg, dest, ins);
				dest->opcode = mono_op_to_op_imm (ins->opcode);
				dest->sreg1 = ins->sreg2;
				dest->sreg2 = -1;
				dest->inst_imm = arg1->inst_c0;
			}
		}
		break;
	case OP_IMUL_IMM:
	case OP_IADD_IMM:
	case OP_IAND_IMM:
	case OP_IOR_IMM:
	case OP_IXOR_IMM:
	case OP_ISUB_IMM:
	case OP_ISHL_IMM:
	case OP_ISHR_IMM:
	case OP_ISHR_UN_IMM:
	case OP_SHL_IMM:
		if (arg1->opcode == OP_ICONST) {
			ALLOC_DEST (cfg, dest, ins);
			switch (ins->opcode) {
				FOLD_BINOP2_IMM (OP_IMUL_IMM, *);
				FOLD_BINOP2_IMM (OP_IADD_IMM, +);
				FOLD_BINOP2_IMM (OP_IAND_IMM, &);
				FOLD_BINOP2_IMM (OP_IOR_IMM, |);
				FOLD_BINOP2_IMM (OP_IXOR_IMM, ^);
				FOLD_BINOP2_IMM (OP_ISUB_IMM, -);
				FOLD_BINOPC2_IMM (OP_ISHL_IMM, <<, gint32);
				FOLD_BINOPC2_IMM (OP_ISHR_IMM, >>, gint32);
				FOLD_BINOPC2_IMM (OP_ISHR_UN_IMM, >>, guint32);
				FOLD_BINOP2_IMM (OP_SHL_IMM, <<);
			}
			dest->opcode = OP_ICONST;
			MONO_INST_NULLIFY_SREGS (dest);
		}
		break;
	case OP_ISUB:
	case OP_ISHL:
	case OP_ISHR:
	case OP_ISHR_UN:
		if ((arg1->opcode == OP_ICONST) && (arg2->opcode == OP_ICONST)) {
			ALLOC_DEST (cfg, dest, ins);
			switch (ins->opcode) {
				FOLD_BINOP (OP_ISUB, -);
				FOLD_BINOP (OP_ISHL, <<);
				FOLD_BINOP (OP_ISHR, >>);
				FOLD_BINOPC (OP_ISHR_UN, >>, guint32);
			}
			dest->opcode = OP_ICONST;
			MONO_INST_NULLIFY_SREGS (dest);
		}
		break;
	case OP_IDIV:
	case OP_IDIV_UN:
	case OP_IREM:
	case OP_IREM_UN:
		if ((arg1->opcode == OP_ICONST) && (arg2->opcode == OP_ICONST)) {
			if ((arg2->inst_c0 == 0) || ((arg1->inst_c0 == G_MININT32) && (arg2->inst_c0 == -1)))
				return NULL;
			ALLOC_DEST (cfg, dest, ins);
			switch (ins->opcode) {
				FOLD_BINOPC (OP_IDIV, /, gint32);
				FOLD_BINOPC (OP_IDIV_UN, /, guint32);
				FOLD_BINOPC (OP_IREM, %, gint32);
				FOLD_BINOPC (OP_IREM_UN, %, guint32);
			}
			dest->opcode = OP_ICONST;
			MONO_INST_NULLIFY_SREGS (dest);
		}
		break;
	case OP_IDIV_IMM:
	case OP_IDIV_UN_IMM:
	case OP_IREM_IMM:
	case OP_IREM_UN_IMM:
		if (arg1->opcode == OP_ICONST) {
			if ((ins->inst_imm == 0) || ((arg1->inst_c0 == G_MININT32) && (ins->inst_imm == -1)))
				return NULL;
			ALLOC_DEST (cfg, dest, ins);
			switch (ins->opcode) {
				FOLD_BINOPC2_IMM (OP_IDIV_IMM, /, gint32);
				FOLD_BINOPC2_IMM (OP_IDIV_UN_IMM, /, guint32);
				FOLD_BINOPC2_IMM (OP_IREM_IMM, %, gint32);
				FOLD_BINOPC2_IMM (OP_IREM_UN_IMM, %, guint32);
			default:
				g_assert_not_reached ();
			}
			dest->opcode = OP_ICONST;
			MONO_INST_NULLIFY_SREGS (dest);
		}
		break;
		/* case OP_INEG: */
	case OP_INOT:
	case OP_INEG:
		if (arg1->opcode == OP_ICONST) {
			/* INEG sets cflags on x86, and the LNEG decomposition depends on that */
#if SIZEOF_REGISTER == 4
			if (ins->opcode == OP_INEG)
				return NULL;
#endif
			ALLOC_DEST (cfg, dest, ins);
			switch (ins->opcode) {
				FOLD_UNOP (OP_INEG,-);
				FOLD_UNOP (OP_INOT,~);
			}
			dest->opcode = OP_ICONST;
			MONO_INST_NULLIFY_SREGS (dest);
		}
		break;
	case OP_MOVE:
#if SIZEOF_REGISTER == 8
		if ((arg1->opcode == OP_ICONST) || (arg1->opcode == OP_I8CONST)) {
#else
		if (arg1->opcode == OP_ICONST) {
#endif
			ALLOC_DEST (cfg, dest, ins);
			dest->opcode = arg1->opcode;
			MONO_INST_NULLIFY_SREGS (dest);
			dest->inst_c0 = arg1->inst_c0;
		}
		break;
	case OP_VMOVE:
		if (arg1->opcode == OP_VZERO) {
			ALLOC_DEST (cfg, dest, ins);
			dest->opcode = OP_VZERO;
			dest->sreg1 = -1;
		}
		break;
	case OP_XMOVE:
		if (arg1->opcode == OP_XZERO) {
			ALLOC_DEST (cfg, dest, ins);
			dest->opcode = OP_XZERO;
			dest->sreg1 = -1;
		}
		break;
	case OP_COMPARE:
	case OP_ICOMPARE:
	case OP_COMPARE_IMM:
	case OP_ICOMPARE_IMM: {
		MonoInst dummy_arg2;
		if (ins->sreg2 == -1) {
			arg2 = &dummy_arg2;
			arg2->opcode = OP_ICONST;
			arg2->inst_c0 = ins->inst_imm;
		}

		if ((arg1->opcode == OP_ICONST) && (arg2->opcode == OP_ICONST) && ins->next) {
			MonoInst *next = ins->next;
			gboolean res = FALSE;

			switch (next->opcode) {
			case OP_CEQ:
			case OP_ICEQ:
			case OP_CGT:
			case OP_ICGT:
			case OP_CGT_UN:
			case OP_ICGT_UN:
			case OP_CLT:
			case OP_ICLT:
			case OP_CLT_UN:
			case OP_ICLT_UN:
				switch (next->opcode) {
					FOLD_BINOPCXX (OP_CEQ,==,gint32);
					FOLD_BINOPCXX (OP_ICEQ,==,gint32);
					FOLD_BINOPCXX (OP_CGT,>,gint32);
					FOLD_BINOPCXX (OP_ICGT,>,gint32);
					FOLD_BINOPCXX (OP_CGT_UN,>,guint32);
					FOLD_BINOPCXX (OP_ICGT_UN,>,guint32);
					FOLD_BINOPCXX (OP_CLT,<,gint32);
					FOLD_BINOPCXX (OP_ICLT,<,gint32);
					FOLD_BINOPCXX (OP_CLT_UN,<,guint32);
					FOLD_BINOPCXX (OP_ICLT_UN,<,guint32);
				}

				if (overwrite) {
					NULLIFY_INS (ins);
					next->opcode = OP_ICONST;
					next->inst_c0 = res;
					MONO_INST_NULLIFY_SREGS (next);
				} else {
					ALLOC_DEST (cfg, dest, ins);
					dest->opcode = OP_ICONST;
					dest->inst_c0 = res;
				}
				break;
			case OP_IBEQ:
			case OP_IBNE_UN:
			case OP_IBGT:
			case OP_IBGT_UN:
			case OP_IBGE:
			case OP_IBGE_UN:
			case OP_IBLT:
			case OP_IBLT_UN:
			case OP_IBLE:
			case OP_IBLE_UN:
				switch (next->opcode) {
					FOLD_BINOPCXX (OP_IBEQ,==,gint32);
					FOLD_BINOPCXX (OP_IBNE_UN,!=,guint32);
					FOLD_BINOPCXX (OP_IBGT,>,gint32);
					FOLD_BINOPCXX (OP_IBGT_UN,>,guint32);
					FOLD_BINOPCXX (OP_IBGE,>=,gint32);
					FOLD_BINOPCXX (OP_IBGE_UN,>=,guint32);
					FOLD_BINOPCXX (OP_IBLT,<,gint32);
					FOLD_BINOPCXX (OP_IBLT_UN,<,guint32);
					FOLD_BINOPCXX (OP_IBLE,<=,gint32);
					FOLD_BINOPCXX (OP_IBLE_UN,<=,guint32);
				}

				if (overwrite) {
					/* 
					 * Can't nullify OP_COMPARE here since the decompose long branch 
					 * opcodes depend on it being executed. Also, the branch might not
					 * be eliminated after all if loop opts is disabled, for example.
					 */
					if (res)
						next->flags |= MONO_INST_CFOLD_TAKEN;
					else
						next->flags |= MONO_INST_CFOLD_NOT_TAKEN;
				} else {
					ALLOC_DEST (cfg, dest, ins);
					dest->opcode = OP_ICONST;
					dest->inst_c0 = res;
				}
				break;
			case OP_COND_EXC_EQ:
				res = arg1->inst_c0 == arg2->inst_c0;
				if (!res) {
					if (overwrite) {
						NULLIFY_INS (ins);
						NULLIFY_INS (next);
					} else {
						ALLOC_DEST (cfg, dest, ins);
						dest->opcode = OP_ICONST;
						dest->inst_c0 = res;
					}
				}
				break;
			case OP_NOP:
			case OP_BR:
				/* This happens when a conditional branch is eliminated */
				if (next->next == NULL) {
					/* Last ins */
					if (overwrite)
						NULLIFY_INS (ins);
				}
				break;
			default:
				return NULL;
			}
		}
		if ((arg1->opcode == OP_PCONST) && (arg2->opcode == OP_PCONST) && ins->next) {
			MonoInst *next = ins->next;

			if (next->opcode == OP_LCEQ) {
				gboolean res = arg1->inst_p0 == arg2->inst_p0;
				if (overwrite) {
					NULLIFY_INS (ins);
					next->opcode = OP_ICONST;
					next->inst_c0 = res;
					MONO_INST_NULLIFY_SREGS (next);
				} else {
					ALLOC_DEST (cfg, dest, ins);
					dest->opcode = OP_ICONST;
					dest->inst_c0 = res;
				}
				break;
			}
		}
		break;
	}
	case OP_FMOVE:
		if (arg1->opcode == OP_R8CONST) {
			ALLOC_DEST (cfg, dest, ins);
			dest->opcode = OP_R8CONST;
			dest->sreg1 = -1;
			dest->inst_p0 = arg1->inst_p0;
		}
		break;

		/*
		 * TODO: 
		 * 	conv.* opcodes.
		 * 	*ovf* opcodes? It's slow and hard to do in C.
		 *      switch can be replaced by a simple jump 
		 */
	default:
		return NULL;
	}
		
    return dest;
}	


#endif /* DISABLE_JIT */
