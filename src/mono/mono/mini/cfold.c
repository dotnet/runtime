/*
 * cfold.c: Constant folding support
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.  http://www.ximian.com
 */
#include "mini.h"

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

#define FOLD_BINOP(name,op)	\
	case name:	\
		if (inst->inst_i0->opcode != OP_ICONST)	\
			return;	\
		if (inst->inst_i1->opcode == OP_ICONST) {	\
			inst->opcode = OP_ICONST;	\
			inst->inst_c0 = inst->inst_i0->inst_c0 op inst->inst_i1->inst_c0;	\
		} \
                return;

/*
 * We try to put constants on the left side of a commutative operation
 * because it reduces register pressure and it matches the usual cpu 
 * instructions with immediates.
 */
#define FOLD_BINOPCOMM(name,op)	\
	case name:	\
		if (inst->inst_i0->opcode == OP_ICONST)	{\
			if (inst->inst_i1->opcode == OP_ICONST) {	\
				inst->opcode = OP_ICONST;	\
				inst->inst_c0 = inst->inst_i0->inst_c0 op inst->inst_i1->inst_c0;	\
                                return; \
			} else { \
				MonoInst *tmp = inst->inst_i0;	\
				inst->inst_i0 = inst->inst_i1;	\
				inst->inst_i1 = tmp;	\
                       } \
		} \
		if (inst->inst_i1->opcode == OP_ICONST && inst->opcode == CEE_ADD) {	\
			if (inst->inst_i1->inst_c0 == 0) { \
				*inst = *(inst->inst_i0); \
				return;	\
			} \
		} \
		if (inst->inst_i1->opcode == OP_ICONST && inst->opcode == CEE_MUL) {	\
 		        int power2;	\
			if (inst->inst_i1->inst_c0 == 1) {	\
				*inst = *(inst->inst_i0);	\
				return;	\
			} else if (inst->inst_i1->inst_c0 == -1) {	\
				inst->opcode = CEE_NEG;	\
				return;	\
			}	\
 		        power2 = mono_is_power_of_two (inst->inst_i1->inst_c0);	\
		     	if (power2 < 0) return;	\
			inst->opcode = CEE_SHL;	\
			inst->inst_i1->inst_c0 = power2;	\
		} \
                return;

#ifndef G_MININT32
#define MYGINT32_MAX 2147483647
#define G_MININT32 (-MYGINT32_MAX -1)
#endif

/* 
 * We can't let this cause a division by zero exception since the division 
 * might not be executed during runtime.
 */
#define FOLD_BINOPZ(name,op,cast)	\
	case name:	\
		if (inst->inst_i1->opcode == OP_ICONST && inst->opcode == CEE_REM_UN && inst->inst_i1->inst_c0 == 2) {	\
			inst->opcode = CEE_AND;	\
			inst->inst_i1->inst_c0 = 1;	\
			return;	\
		}	\
		if (inst->inst_i1->opcode == OP_ICONST) {	\
			if (!inst->inst_i1->inst_c0) return;	\
			if (inst->inst_i0->opcode == OP_ICONST) {	\
                if ((inst->inst_i0->inst_c0 == G_MININT32) && (inst->inst_i1->inst_c0 == -1)) \
                    return; \
				inst->inst_c0 = (cast)inst->inst_i0->inst_c0 op (cast)inst->inst_i1->inst_c0;	\
				inst->opcode = OP_ICONST;	\
			} else {	\
				int power2 = mono_is_power_of_two (inst->inst_i1->inst_c0);	\
				if (power2 < 0) return;	\
				if (inst->opcode == CEE_REM_UN) {	\
					inst->opcode = CEE_AND;	\
					inst->inst_i1->inst_c0 = (1 << power2) - 1;	\
				} else if (inst->opcode == CEE_DIV_UN) {	\
					inst->opcode = CEE_SHR_UN;	\
					inst->inst_i1->inst_c0 = power2;	\
				}	\
			}	\
		} \
                return;
	
#define FOLD_BINOPA(name,op,cast)	\
	case name:	\
		if (inst->inst_i0->opcode != OP_ICONST)	\
			return;	\
		if (inst->inst_i1->opcode == OP_ICONST) {	\
			inst->opcode = OP_ICONST;	\
			inst->inst_c0 = (cast)inst->inst_i0->inst_c0 op (cast)inst->inst_i1->inst_c0;	\
		} \
                return;
	
#define FOLD_CXX(name,op,cast)	\
	case name:	\
		if (inst->inst_i0->opcode != OP_COMPARE)	\
			return;	\
		if (inst->inst_i0->inst_i0->opcode != OP_ICONST)	\
			return;	\
		if (inst->inst_i0->inst_i1->opcode == OP_ICONST) {	\
			inst->opcode = OP_ICONST;	\
			inst->inst_c0 = (cast)inst->inst_i0->inst_i0->inst_c0 op (cast)inst->inst_i0->inst_i1->inst_c0;	\
		} \
                return;

#define FOLD_UNOP(name,op)	\
	case name:	\
		if (inst->inst_i0->opcode == OP_ICONST) {	\
 		        inst->opcode = OP_ICONST;	\
		        inst->inst_c0 = op inst->inst_i0->inst_c0; \
                } else if (inst->inst_i0->opcode == OP_I8CONST) { \
 		        inst->opcode = OP_I8CONST;	\
		        inst->inst_l = op inst->inst_i0->inst_l; \
                } return;

#define FOLD_BRBINOP(name,op,cast)	\
	case name:	\
		if (inst->inst_i0->opcode != OP_COMPARE)	\
			return;	\
		if (inst->inst_i0->inst_i0->opcode != OP_ICONST)	\
			return;	\
		if (inst->inst_i0->inst_i1->opcode == OP_ICONST) {	\
			if ((cast)inst->inst_i0->inst_i0->inst_c0 op (cast)inst->inst_i0->inst_i1->inst_c0)	\
				inst->opcode = CEE_BR;	\
			else	\
				inst->opcode = CEE_NOP;	\
		} \
                return;

/*
 * Helper function to do constant expression evaluation.
 * We do constant folding of integers only, FP stuff is much more tricky,
 * int64 probably not worth it.
 */
void
mono_constant_fold_inst (MonoInst *inst, gpointer data)
{
	switch (inst->opcode) {

	/* FIXME: the CEE_B* don't contain operands, need to use the OP_COMPARE instruction */
	/*FOLD_BRBINOP (CEE_BEQ,==,gint32)
	FOLD_BRBINOP (CEE_BGE,>=,gint32)
	FOLD_BRBINOP (CEE_BGT,>,gint32)
	FOLD_BRBINOP (CEE_BLE,<=,gint32)
	FOLD_BRBINOP (CEE_BLT,<,gint32)
	FOLD_BRBINOP (CEE_BNE_UN,!=,guint32)
	FOLD_BRBINOP (CEE_BGE_UN,>=,guint32)
	FOLD_BRBINOP (CEE_BGT_UN,>,guint32)
	FOLD_BRBINOP (CEE_BLE_UN,<=,guint32)
	FOLD_BRBINOP (CEE_BLT_UN,<,guint32)*/

	FOLD_BINOPCOMM (CEE_MUL,*)

	FOLD_BINOPCOMM (CEE_ADD,+)
	FOLD_BINOP (CEE_SUB,-)
	FOLD_BINOPZ (CEE_DIV,/,gint32)
	FOLD_BINOPZ (CEE_DIV_UN,/,guint32)
	FOLD_BINOPZ (CEE_REM,%,gint32)
	FOLD_BINOPZ (CEE_REM_UN,%,guint32)
	FOLD_BINOPCOMM (CEE_AND,&)
	FOLD_BINOPCOMM (CEE_OR,|)
	FOLD_BINOPCOMM (CEE_XOR,^)
	FOLD_BINOP (CEE_SHL,<<)
	FOLD_BINOP (CEE_SHR,>>)
	case CEE_SHR_UN:
		if (inst->inst_i0->opcode != OP_ICONST)
			return;
		if (inst->inst_i1->opcode == OP_ICONST) {
			inst->opcode = OP_ICONST;
			inst->inst_c0 = (guint32)inst->inst_i0->inst_c0 >> (guint32)inst->inst_i1->inst_c0;
		}
		return;
	FOLD_UNOP (CEE_NEG,-)
	FOLD_UNOP (CEE_NOT,~)
	FOLD_CXX (OP_CEQ,==,gint32)
	FOLD_CXX (OP_CGT,>,gint32)
	FOLD_CXX (OP_CGT_UN,>,guint32)
	FOLD_CXX (OP_CLT,<,gint32)
	FOLD_CXX (OP_CLT_UN,<,guint32)
	case CEE_CONV_I8:
		if (inst->inst_i0->opcode == OP_ICONST) {
			inst->opcode = OP_I8CONST;
			inst->inst_l = inst->inst_i0->inst_c0;
		}
		return;
	case CEE_CONV_I:
	case CEE_CONV_U:
		if (inst->inst_i0->opcode == OP_ICONST) {
			inst->opcode = OP_ICONST;
			inst->inst_c0 = inst->inst_i0->inst_c0;
		} else if (inst->inst_i0->opcode == CEE_LDIND_I) {
			*inst = *inst->inst_i0;
		}
		return;
	/* we should be able to handle isinst and castclass as well */
	case CEE_ISINST:
	case CEE_CASTCLASS:
	/*
	 * TODO: 
	 * 	conv.* opcodes.
	 * 	*ovf* opcodes? I'ts slow and hard to do in C.
	 *      switch can be replaced by a simple jump 
	 */
#if SIZEOF_VOID_P == 4
	case CEE_CONV_I4:
		if ((inst->inst_left->type == STACK_I4) || (inst->inst_left->type == STACK_PTR)) {
			*inst = *inst->inst_left;
		}
		break;	
#endif
	default:
		return;
	}
}

void
mono_constant_fold (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		for (ins = bb->code; ins; ins = ins->next)	
			mono_inst_foreach (ins, mono_constant_fold_inst, NULL);
	}
}

