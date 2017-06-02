/**
 * \file
 * Branch optimizations support
 *
 * Authors:
 *   Patrik Torstensson (Patrik.Torstesson at gmail.com)
 *
 * (C) 2005 Ximian, Inc.  http://www.ximian.com
 * Copyright 2011 Xamarin Inc.  http://www.xamarin.com
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#include <mono/utils/mono-compiler.h>
#ifndef DISABLE_JIT

#include "mini.h"

/*
 * Returns true if @bb is a basic block which falls through the next block.
 * TODO verify if it helps to check if the bb last ins is a branch to its successor. 
 */
static gboolean
mono_bb_is_fall_through (MonoCompile *cfg, MonoBasicBlock *bb)
{
	return  bb->next_bb && bb->next_bb->region == bb->region && /*fall throught between regions is not really interesting or useful*/
			(bb->last_ins == NULL || !MONO_IS_BRANCH_OP (bb->last_ins)); /*and the last op can't be a branch too*/
}

/*
 * Used by the arch code to replace the exception handling
 * with a direct branch. This is safe to do if the 
 * exception object isn't used, no rethrow statement and
 * no filter statement (verify).
 *
 */
MonoInst *
mono_branch_optimize_exception_target (MonoCompile *cfg, MonoBasicBlock *bb, const char * exname)
{
	MonoMethodHeader *header = cfg->header;
	MonoExceptionClause *clause;
	MonoClass *exclass;
	int i;

	if (!(cfg->opt & MONO_OPT_EXCEPTION))
		return NULL;

	if (bb->region == -1 || !MONO_BBLOCK_IS_IN_REGION (bb, MONO_REGION_TRY))
		return NULL;

	exclass = mono_class_load_from_name (mono_get_corlib (), "System", exname);
	/* search for the handler */
	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, bb->real_offset)) {
			if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE && clause->data.catch_class && mono_class_is_assignable_from (clause->data.catch_class, exclass)) {
				MonoBasicBlock *tbb;

				/* get the basic block for the handler and 
				 * check if the exception object is used.
				 * Flag is set during method_to_ir due to 
				 * pop-op is optmized away in codegen (burg).
				 */
				tbb = cfg->cil_offset_to_bb [clause->handler_offset];
				if (tbb && tbb->flags & BB_EXCEPTION_DEAD_OBJ && !(tbb->flags & BB_EXCEPTION_UNSAFE)) {
					MonoBasicBlock *targetbb = tbb;
					gboolean unsafe = FALSE;

					/* Check if this catch clause is ok to optimize by
					 * looking for the BB_EXCEPTION_UNSAFE in every BB that
					 * belongs to the same region. 
					 *
					 * UNSAFE flag is set during method_to_ir (OP_RETHROW)
					 */
					while (!unsafe && tbb->next_bb && tbb->region == tbb->next_bb->region) {
						if (tbb->next_bb->flags & BB_EXCEPTION_UNSAFE)  {
							unsafe = TRUE;
							break;
						}
						tbb = tbb->next_bb;
					}

					if (!unsafe) {
						MonoInst *jump;

						/* Create dummy inst to allow easier integration in
						 * arch dependent code (opcode ignored)
						 */
						MONO_INST_NEW (cfg, jump, OP_BR);

						/* Allocate memory for our branch target */
						jump->inst_i1 = (MonoInst *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
						jump->inst_true_bb = targetbb;

						if (cfg->verbose_level > 2) 
							g_print ("found exception to optimize - returning branch to BB%d (%s) (instead of throw) for method %s:%s\n", targetbb->block_num, clause->data.catch_class->name, cfg->method->klass->name, cfg->method->name);

						return jump;
					} 

					return NULL;
				} else {
					/* Branching to an outer clause could skip inner clauses */
					return NULL;
				}
			} else {
				/* Branching to an outer clause could skip inner clauses */
				return NULL;
			}
		}
	}

	return NULL;
}

static const int int_cmov_opcodes [] = {
	OP_CMOV_IEQ,
	OP_CMOV_INE_UN,
	OP_CMOV_ILE,
	OP_CMOV_IGE,
	OP_CMOV_ILT,
	OP_CMOV_IGT,
	OP_CMOV_ILE_UN,
	OP_CMOV_IGE_UN,
	OP_CMOV_ILT_UN,
	OP_CMOV_IGT_UN
};

static const int long_cmov_opcodes [] = {
	OP_CMOV_LEQ,
	OP_CMOV_LNE_UN,
	OP_CMOV_LLE,
	OP_CMOV_LGE,
	OP_CMOV_LLT,
	OP_CMOV_LGT,
	OP_CMOV_LLE_UN,
	OP_CMOV_LGE_UN,
	OP_CMOV_LLT_UN,
	OP_CMOV_LGT_UN
};

static G_GNUC_UNUSED int
br_to_br_un (int opcode)
{
	switch (opcode) {
	case OP_IBGT:
		return OP_IBGT_UN;
		break;
	case OP_IBLE:
		return OP_IBLE_UN;
		break;
	case OP_LBGT:
		return OP_LBGT_UN;
		break;
	case OP_LBLE:
		return OP_LBLE_UN;
		break;
	default:
		g_assert_not_reached ();
		return -1;
	}
}

/**
 * mono_replace_ins:
 *
 *   Replace INS with its decomposition which is stored in a series of bblocks starting
 * at FIRST_BB and ending at LAST_BB. On enter, PREV points to the predecessor of INS. 
 * On return, it will be set to the last ins of the decomposition.
 */
void
mono_replace_ins (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, MonoInst **prev, MonoBasicBlock *first_bb, MonoBasicBlock *last_bb)
{
	MonoInst *next = ins->next;

	if (next && next->opcode == OP_NOP) {
		/* Avoid NOPs following branches */
		ins->next = next->next;
		next = next->next;
	}

	if (first_bb == last_bb) {
		/* 
		 * Only one replacement bb, merge the code into
		 * the current bb.
		 */

		/* Delete links between the first_bb and its successors */
		while (first_bb->out_count)
			mono_unlink_bblock (cfg, first_bb, first_bb->out_bb [0]);

		/* Head */
		if (*prev) {
			(*prev)->next = first_bb->code;
			first_bb->code->prev = (*prev);
		} else {
			bb->code = first_bb->code;
		}

		/* Tail */
		last_bb->last_ins->next = next;
		if (next)
			next->prev = last_bb->last_ins;
		else
			bb->last_ins = last_bb->last_ins;
		*prev = last_bb->last_ins;
		bb->has_array_access |= first_bb->has_array_access;
	} else {
		int i, count;
		MonoBasicBlock **tmp_bblocks, *tmp;
		MonoInst *last;

		/* Multiple BBs */

		/* Set region/real_offset */
		for (tmp = first_bb; tmp; tmp = tmp->next_bb) {
			tmp->region = bb->region;
			tmp->real_offset = bb->real_offset;
		}

		/* Split the original bb */
		if (ins->next)
			ins->next->prev = NULL;
		ins->next = NULL;
		bb->last_ins = ins;

		/* Merge the second part of the original bb into the last bb */
		if (last_bb->last_ins) {
			last_bb->last_ins->next = next;
			if (next)
				next->prev = last_bb->last_ins;
		} else {
			last_bb->code = next;
		}
		last_bb->has_array_access |= bb->has_array_access;

		if (next) {
			for (last = next; last->next != NULL; last = last->next)
				;
			last_bb->last_ins = last;
		}

		for (i = 0; i < bb->out_count; ++i)
			mono_link_bblock (cfg, last_bb, bb->out_bb [i]);

		/* Merge the first (dummy) bb to the original bb */
		if (*prev) {
			(*prev)->next = first_bb->code;
			first_bb->code->prev = (*prev);
		} else {
			bb->code = first_bb->code;
		}
		bb->last_ins = first_bb->last_ins;
		bb->has_array_access |= first_bb->has_array_access;

		/* Delete the links between the original bb and its successors */
		tmp_bblocks = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoBasicBlock*) * bb->out_count);
		memcpy (tmp_bblocks, bb->out_bb, sizeof (MonoBasicBlock*) * bb->out_count);
		count = bb->out_count;
		for (i = 0; i < count; ++i)
			mono_unlink_bblock (cfg, bb, tmp_bblocks [i]);

		/* Add links between the original bb and the first_bb's successors */
		for (i = 0; i < first_bb->out_count; ++i) {
			MonoBasicBlock *out_bb = first_bb->out_bb [i];

			mono_link_bblock (cfg, bb, out_bb);
		}
		/* Delete links between the first_bb and its successors */
		for (i = 0; i < bb->out_count; ++i) {
			MonoBasicBlock *out_bb = bb->out_bb [i];

			mono_unlink_bblock (cfg, first_bb, out_bb);
		}
		last_bb->next_bb = bb->next_bb;
		bb->next_bb = first_bb->next_bb;

		*prev = NULL;
	}
}

void
mono_if_conversion (MonoCompile *cfg)
{
#ifdef MONO_ARCH_HAVE_CMOV_OPS
	MonoBasicBlock *bb;
	gboolean changed = FALSE;
	int filter = FILTER_NOP | FILTER_IL_SEQ_POINT;

	if (!(cfg->opt & MONO_OPT_CMOV))
		return;

	// FIXME: Make this work with extended bblocks

	/* 
	 * This pass requires somewhat optimized IR code so it should be run after
	 * local cprop/deadce. Also, it should be run before dominator computation, since
	 * it changes control flow.
	 */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoBasicBlock *bb1, *bb2;

	restart:
		/* Look for the IR code generated from cond ? a : b
		 * which is:
		 * BB:
		 * b<cond> [BB1BB2]
		 * BB1:
		 * <var> <- <a>
		 * br BB3
		 * BB2:
		 * <var> <- <b>
		 * br BB3
		 */
		if (!(bb->out_count == 2 && !bb->extended))
			continue;

		bb1 = bb->out_bb [0];
		bb2 = bb->out_bb [1];

		if (bb1->in_count == 1 && bb2->in_count == 1 && bb1->out_count == 1 && bb2->out_count == 1 && bb1->out_bb [0] == bb2->out_bb [0]) {
			MonoInst *compare, *branch, *ins1, *ins2, *cmov, *move, *tmp;
			MonoBasicBlock *true_bb, *false_bb;
			gboolean simple, ret;
			int dreg, tmp_reg;
			CompType comp_type;

			branch = mono_bb_last_inst (bb, filter);

			if (!branch || branch->opcode == OP_BR_REG || branch->opcode == OP_BR)
				continue;

			/* Find the compare instruction */
			compare = mono_inst_prev (branch, filter);
			if (!compare)
				continue;

			if (!MONO_IS_COND_BRANCH_OP (branch))
				/* This can happen if a cond branch is optimized away */
				continue;

			true_bb = branch->inst_true_bb;
			false_bb = branch->inst_false_bb;

			/* 
			 * Check that bb1 and bb2 are 'simple' and both assign to the same
			 * variable.
			 */
			/* FIXME: Get rid of the nops earlier */
			ins1 = mono_bb_first_inst (true_bb, filter);
			ins2 = mono_bb_first_inst (false_bb, filter);

			if (!(ins1 && ins2 && ins1->dreg == ins2->dreg && ins1->dreg != -1))
				continue;

			simple = TRUE;
			for (tmp = ins1->next; tmp; tmp = tmp->next)
				if (!((tmp->opcode == OP_NOP) || (tmp->opcode == OP_IL_SEQ_POINT) || (tmp->opcode == OP_BR)))
					simple = FALSE;
					
			for (tmp = ins2->next; tmp; tmp = tmp->next)
				if (!((tmp->opcode == OP_NOP) || (tmp->opcode == OP_IL_SEQ_POINT) || (tmp->opcode == OP_BR)))
					simple = FALSE;

			if (!simple)
				continue;

			/* We move ins1/ins2 before the compare so they should have no side effect */
			if (!(MONO_INS_HAS_NO_SIDE_EFFECT (ins1) && MONO_INS_HAS_NO_SIDE_EFFECT (ins2)))
				continue;

			/* Moving ins1/ins2 could change the comparison */
			/* FIXME: */
			if (!((compare->sreg1 != ins1->dreg) && (compare->sreg2 != ins1->dreg)))
				continue;

			/* FIXME: */
			comp_type = mono_opcode_to_type (branch->opcode, compare->opcode);
			if (!((comp_type == CMP_TYPE_I) || (comp_type == CMP_TYPE_L)))
				continue;

			/* FIXME: */
			/* ins->type might not be set */
			if (INS_INFO (ins1->opcode) [MONO_INST_DEST] != 'i')
				continue;

			if (cfg->verbose_level > 2) {
				printf ("\tBranch -> CMove optimization in BB%d on\n", bb->block_num);
				printf ("\t\t"); mono_print_ins (compare);
				printf ("\t\t"); mono_print_ins (mono_inst_next (compare, filter));
				printf ("\t\t"); mono_print_ins (ins1);
				printf ("\t\t"); mono_print_ins (ins2);
			}

			changed = TRUE;

			//printf ("HIT!\n");

			/* Assignments to the return register must remain at the end of bbs */
			if (cfg->ret)
				ret = ins1->dreg == cfg->ret->dreg;
			else
				ret = FALSE;

			tmp_reg = mono_alloc_dreg (cfg, STACK_I4);
			dreg = ins1->dreg;

			/* Rewrite ins1 to emit to tmp_reg */
			ins1->dreg = tmp_reg;

			if (ret) {
				dreg = mono_alloc_dreg (cfg, STACK_I4);
				ins2->dreg = dreg;
			}

			/* Remove ins1/ins2 from bb1/bb2 */
			MONO_REMOVE_INS (true_bb, ins1);
			MONO_REMOVE_INS (false_bb, ins2);

			/* Move ins1 and ins2 before the comparison */
			/* ins1 comes first to avoid ins1 overwriting an argument of ins2 */
			mono_bblock_insert_before_ins (bb, compare, ins2);
			mono_bblock_insert_before_ins (bb, ins2, ins1);

			/* Add cmov instruction */
			MONO_INST_NEW (cfg, cmov, OP_NOP);
			cmov->dreg = dreg;
			cmov->sreg1 = dreg;
			cmov->sreg2 = tmp_reg;
			switch (mono_opcode_to_type (branch->opcode, compare->opcode)) {
			case CMP_TYPE_I:
				cmov->opcode = int_cmov_opcodes [mono_opcode_to_cond (branch->opcode)];
				break;
			case CMP_TYPE_L:
				cmov->opcode = long_cmov_opcodes [mono_opcode_to_cond (branch->opcode)];
				break;
			default:
				g_assert_not_reached ();
			}
			mono_bblock_insert_after_ins (bb, compare, cmov);

			if (ret) {
				/* Add an extra move */
				MONO_INST_NEW (cfg, move, OP_MOVE);
				move->dreg = cfg->ret->dreg;
				move->sreg1 = dreg;
				mono_bblock_insert_after_ins (bb, cmov, move);
			}

			/* Rewrite the branch */
			branch->opcode = OP_BR;
			branch->inst_target_bb = true_bb->out_bb [0];
			mono_link_bblock (cfg, bb, branch->inst_target_bb);

			/* Reorder bblocks */
			mono_unlink_bblock (cfg, bb, true_bb);
			mono_unlink_bblock (cfg, bb, false_bb);
			mono_unlink_bblock (cfg, true_bb, true_bb->out_bb [0]);
			mono_unlink_bblock (cfg, false_bb, false_bb->out_bb [0]);
			mono_remove_bblock (cfg, true_bb);
			mono_remove_bblock (cfg, false_bb);

			/* Merge bb and its successor if possible */
			if ((bb->out_bb [0]->in_count == 1) && (bb->out_bb [0] != cfg->bb_exit) &&
				(bb->region == bb->out_bb [0]->region)) {
				mono_merge_basic_blocks (cfg, bb, bb->out_bb [0]);
				goto restart;
			}
		}

		/* Look for the IR code generated from if (cond) <var> <- <a>
		 * which is:
		 * BB:
		 * b<cond> [BB1BB2]
		 * BB1:
		 * <var> <- <a>
		 * br BB2
		 */

		if ((bb2->in_count == 1 && bb2->out_count == 1 && bb2->out_bb [0] == bb1) ||
			(bb1->in_count == 1 && bb1->out_count == 1 && bb1->out_bb [0] == bb2)) {
			MonoInst *compare, *branch, *ins1, *cmov, *tmp;
			gboolean simple;
			int dreg, tmp_reg;
			CompType comp_type;
			CompRelation cond;
			MonoBasicBlock *next_bb, *code_bb;

			/* code_bb is the bblock containing code, next_bb is the successor bblock */
			if (bb2->in_count == 1 && bb2->out_count == 1 && bb2->out_bb [0] == bb1) {
				code_bb = bb2;
				next_bb = bb1;
			} else {
				code_bb = bb1;
				next_bb = bb2;
			}

			ins1 = mono_bb_first_inst (code_bb, filter);

			if (!ins1)
				continue;

			/* Check that code_bb is simple */
			simple = TRUE;
			for (tmp = ins1; tmp; tmp = tmp->next)
				if (!((tmp->opcode == OP_NOP) || (tmp->opcode == OP_IL_SEQ_POINT) || (tmp->opcode == OP_BR)))
					simple = FALSE;

			if (!simple)
				continue;

			/* We move ins1 before the compare so it should have no side effect */
			if (!MONO_INS_HAS_NO_SIDE_EFFECT (ins1))
				continue;

			branch = mono_bb_last_inst (bb, filter);

			if (!branch || branch->opcode == OP_BR_REG)
				continue;

			/* Find the compare instruction */
			compare = mono_inst_prev (branch, filter);
			if (!compare)
				continue;

			if (!MONO_IS_COND_BRANCH_OP (branch))
				/* This can happen if a cond branch is optimized away */
				continue;

			/* FIXME: */
			comp_type = mono_opcode_to_type (branch->opcode, compare->opcode);
			if (!((comp_type == CMP_TYPE_I) || (comp_type == CMP_TYPE_L)))
				continue;

			/* FIXME: */
			/* ins->type might not be set */
			if (INS_INFO (ins1->opcode) [MONO_INST_DEST] != 'i')
				continue;

			/* FIXME: */
			if (cfg->ret && ins1->dreg == cfg->ret->dreg)
				continue;

			if (!(cfg->opt & MONO_OPT_DEADCE))
				/* 
				 * It is possible that dreg is never set before, so we can't use
				 * it as an sreg of the cmov instruction (#582322).
				 */
				continue;

			if (cfg->verbose_level > 2) {
				printf ("\tBranch -> CMove optimization (2) in BB%d on\n", bb->block_num);
				printf ("\t\t"); mono_print_ins (compare);
				printf ("\t\t"); mono_print_ins (mono_inst_next (compare, filter));
				printf ("\t\t"); mono_print_ins (ins1);
			}

			changed = TRUE;

			//printf ("HIT!\n");

			tmp_reg = mono_alloc_dreg (cfg, STACK_I4);
			dreg = ins1->dreg;

			/* Rewrite ins1 to emit to tmp_reg */
			ins1->dreg = tmp_reg;

			/* Remove ins1 from code_bb */
			MONO_REMOVE_INS (code_bb, ins1);

			/* Move ins1 before the comparison */
			mono_bblock_insert_before_ins (bb, compare, ins1);

			/* Add cmov instruction */
			MONO_INST_NEW (cfg, cmov, OP_NOP);
			cmov->dreg = dreg;
			cmov->sreg1 = dreg;
			cmov->sreg2 = tmp_reg;
			cond = mono_opcode_to_cond (branch->opcode);
			if (branch->inst_false_bb == code_bb)
				cond = mono_negate_cond (cond);
			switch (mono_opcode_to_type (branch->opcode, compare->opcode)) {
			case CMP_TYPE_I:
				cmov->opcode = int_cmov_opcodes [cond];
				break;
			case CMP_TYPE_L:
				cmov->opcode = long_cmov_opcodes [cond];
				break;
			default:
				g_assert_not_reached ();
			}
			mono_bblock_insert_after_ins (bb, compare, cmov);

			/* Rewrite the branch */
			branch->opcode = OP_BR;
			branch->inst_target_bb = next_bb;
			mono_link_bblock (cfg, bb, branch->inst_target_bb);

			/* Nullify the branch at the end of code_bb */
			if (code_bb->code) {
				branch = code_bb->code;
				MONO_DELETE_INS (code_bb, branch);
			}

			/* Reorder bblocks */
			mono_unlink_bblock (cfg, bb, code_bb);
			mono_unlink_bblock (cfg, code_bb, next_bb);

			/* Merge bb and its successor if possible */
			if ((bb->out_bb [0]->in_count == 1) && (bb->out_bb [0] != cfg->bb_exit) &&
				(bb->region == bb->out_bb [0]->region)) {
				mono_merge_basic_blocks (cfg, bb, bb->out_bb [0]);

				/* 
				 * bbn might have fallen through to the next bb without a branch, 
				 * have to add one now (#474718).
				 * FIXME: Maybe need to do this more generally in 
				 * merge_basic_blocks () ?
				 */
				if (!(bb->last_ins && MONO_IS_BRANCH_OP (bb->last_ins)) && bb->out_count) {
					MONO_INST_NEW (cfg, ins1, OP_BR);
					ins1->inst_target_bb = bb->out_bb [0];
					MONO_ADD_INS (bb, ins1);
				}
				goto restart;
			}
		}
	}

	/*
	 * Optimize checks like: if (v < 0 || v > limit) by changing then to unsigned
	 * compares. This isn't really if conversion, but it easier to do here than in
	 * optimize_branches () since the IR is already optimized.
	 */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoBasicBlock *bb1, *bb2, *next_bb;
		MonoInst *branch1, *branch2, *compare1, *ins, *next;

		/* Look for the IR code generated from if (<var> < 0 || v > <limit>)
		 * after branch opts which is:
		 * BB:
		 * icompare_imm R [0]
		 * int_blt [BB1BB2]
		 * BB2:
		 * icompare_imm R [<limit>]
		 * int_ble [BB3BB1]
		 */
		if (!(bb->out_count == 2 && !bb->extended))
			continue;

		bb1 = bb->out_bb [0];
		bb2 = bb->out_bb [1];

		// FIXME: Add more cases

		/* Check structure */
		if (!(bb1->in_count == 2 && bb1->in_bb [0] == bb && bb1->in_bb [1] == bb2 && bb2->in_count == 1 && bb2->out_count == 2))
			continue;

		next_bb = bb2;

		/* Check first branch */
		branch1 = mono_bb_last_inst (bb, filter);
		if (!(branch1 && ((branch1->opcode == OP_IBLT) || (branch1->opcode == OP_LBLT)) && (branch1->inst_false_bb == next_bb)))
			continue;

		/* Check second branch */
		branch2 = mono_bb_last_inst (next_bb, filter);
		if (!branch2)
			continue;

		/* mcs sometimes generates inverted branches */
		if (((branch2->opcode == OP_IBGT) || (branch2->opcode == OP_LBGT)) && branch2->inst_true_bb == branch1->inst_true_bb)
			;
		else if (((branch2->opcode == OP_IBLE) || (branch2->opcode == OP_LBLE)) && branch2->inst_false_bb == branch1->inst_true_bb)
			;
		else
			continue;

		/* Check first compare */
		compare1 = mono_inst_prev (mono_bb_last_inst (bb, filter), filter);
		if (!(compare1 && ((compare1->opcode == OP_ICOMPARE_IMM) || (compare1->opcode == OP_LCOMPARE_IMM)) && compare1->inst_imm == 0))
			continue;

		/* Check second bblock */
		ins = mono_bb_first_inst (next_bb, filter);
		if (!ins)
			continue;
		next = mono_inst_next (ins, filter);
		if (((ins->opcode == OP_ICOMPARE_IMM) || (ins->opcode == OP_LCOMPARE_IMM)) && ins->sreg1 == compare1->sreg1 && next == branch2) {
			/* The second arg must be positive */
			if (ins->inst_imm < 0)
				continue;
		} else if (((ins->opcode == OP_LDLEN) || (ins->opcode == OP_STRLEN)) && ins->dreg != compare1->sreg1 && next && next->opcode == OP_ICOMPARE && next->sreg1 == compare1->sreg1 && next->sreg2 == ins->dreg && mono_inst_next (next, filter) == branch2) {
			/* Another common case: if (index < 0 || index > arr.Length) */
		} else {
			continue;
		}

		if (cfg->verbose_level > 2) {
			printf ("\tSigned->unsigned compare optimization in BB%d on\n", bb->block_num);
			printf ("\t\t"); mono_print_ins (compare1);
			printf ("\t\t"); mono_print_ins (mono_inst_next (compare1, filter));
			printf ("\t\t"); mono_print_ins (ins);
		}

		/* Rewrite the first compare+branch */
		MONO_DELETE_INS (bb, compare1);
		branch1->opcode = OP_BR;
		mono_unlink_bblock (cfg, bb, branch1->inst_true_bb);
		mono_unlink_bblock (cfg, bb, branch1->inst_false_bb);
		branch1->inst_target_bb = next_bb;
		mono_link_bblock (cfg, bb, next_bb);		

		/* Rewrite the second branch */
		branch2->opcode = br_to_br_un (branch2->opcode);

		mono_merge_basic_blocks (cfg, bb, next_bb);
	}

#if 0
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoBasicBlock *bb1, *bb2;
		MonoInst *prev, *compare, *branch, *ins1, *ins2, *cmov, *move, *tmp;
		gboolean simple, ret;
		int dreg, tmp_reg;
		CompType comp_type;

		/* Look for the IR code generated from if (cond) <var> <- <a>
		 * after branch opts which is:
		 * BB:
		 * compare
		 * b<cond> [BB1]
		 * <var> <- <a>
		 * BB1:
		 */
		if (!(bb->out_count == 1 && bb->extended && bb->code && bb->code->next && bb->code->next->next))
			continue;

		mono_print_bb (bb, "");

		/* Find the compare instruction */
		prev = NULL;
		compare = bb->code;
		g_assert (compare);
		while (compare->next->next && compare->next->next != bb->last_ins) {
			prev = compare;
			compare = compare->next;
		}
		branch = compare->next;
		if (!MONO_IS_COND_BRANCH_OP (branch))
			continue;
	}
#endif

	if (changed) {
		if (cfg->opt & MONO_OPT_BRANCH)
			mono_optimize_branches (cfg);
		/* Merging bblocks could make some variables local */
		mono_handle_global_vregs (cfg);
		if (cfg->opt & (MONO_OPT_CONSPROP | MONO_OPT_COPYPROP))
			mono_local_cprop (cfg);
		if (cfg->opt & MONO_OPT_DEADCE)
			mono_local_deadce (cfg);
	}
#endif
}

void
mono_nullify_basic_block (MonoBasicBlock *bb) 
{
	bb->in_count = 0;
	bb->out_count = 0;
	bb->in_bb = NULL;
	bb->out_bb = NULL;
	bb->next_bb = NULL;
	bb->code = bb->last_ins = NULL;
	bb->cil_code = NULL;
}

static void 
replace_out_block (MonoBasicBlock *bb, MonoBasicBlock *orig,  MonoBasicBlock *repl)
{
	int i;

	for (i = 0; i < bb->out_count; i++) {
		MonoBasicBlock *ob = bb->out_bb [i];
		if (ob == orig) {
			if (!repl) {
				if (bb->out_count > 1) {
					bb->out_bb [i] = bb->out_bb [bb->out_count - 1];
				}
				bb->out_count--;
			} else {
				bb->out_bb [i] = repl;
			}
		}
	}
}

static void 
replace_in_block (MonoBasicBlock *bb, MonoBasicBlock *orig, MonoBasicBlock *repl)
{
	int i;

	for (i = 0; i < bb->in_count; i++) {
		MonoBasicBlock *ib = bb->in_bb [i];
		if (ib == orig) {
			if (!repl) {
				if (bb->in_count > 1) {
					bb->in_bb [i] = bb->in_bb [bb->in_count - 1];
				}
				bb->in_count--;
			} else {
				bb->in_bb [i] = repl;
			}
		}
	}
}

static void
replace_out_block_in_code (MonoBasicBlock *bb, MonoBasicBlock *orig, MonoBasicBlock *repl)
{
	MonoInst *ins;
	
	for (ins = bb->code; ins != NULL; ins = ins->next) {
		switch (ins->opcode) {
		case OP_BR:
			if (ins->inst_target_bb == orig)
				ins->inst_target_bb = repl;
			break;
		case OP_CALL_HANDLER:
			if (ins->inst_target_bb == orig)
				ins->inst_target_bb = repl;
			break;
		case OP_SWITCH: {
			int i;
			int n = GPOINTER_TO_INT (ins->klass);
			for (i = 0; i < n; i++ ) {
				if (ins->inst_many_bb [i] == orig)
					ins->inst_many_bb [i] = repl;
			}
			break;
		}
		default:
			if (MONO_IS_COND_BRANCH_OP (ins)) {
				if (ins->inst_true_bb == orig)
					ins->inst_true_bb = repl;
				if (ins->inst_false_bb == orig)
					ins->inst_false_bb = repl;
			} else if (MONO_IS_JUMP_TABLE (ins)) {
				int i;
				MonoJumpInfoBBTable *table = (MonoJumpInfoBBTable *)MONO_JUMP_TABLE_FROM_INS (ins);
				for (i = 0; i < table->table_size; i++ ) {
					if (table->table [i] == orig)
						table->table [i] = repl;
				}
			}

			break;
		}
	}
}

/**
  * Check if a bb is useless (is just made of NOPs and ends with an
  * unconditional branch, or nothing).
  * If it is so, unlink it from the CFG and nullify it, and return TRUE.
  * Otherwise, return FALSE;
  */
static gboolean
remove_block_if_useless (MonoCompile *cfg, MonoBasicBlock *bb, MonoBasicBlock *previous_bb) {
	MonoBasicBlock *target_bb = NULL;
	MonoInst *inst;

	/* Do not touch handlers */
	if (bb->region != -1) {
		bb->not_useless = TRUE;
		return FALSE;
	}
	
	MONO_BB_FOR_EACH_INS (bb, inst) {
		switch (inst->opcode) {
		case OP_NOP:
		case OP_IL_SEQ_POINT:
			break;
		case OP_BR:
			target_bb = inst->inst_target_bb;
			break;
		default:
			bb->not_useless = TRUE;
			return FALSE;
		}
	}
	
	if (target_bb == NULL) {
		if ((bb->out_count == 1) && (bb->out_bb [0] == bb->next_bb)) {
			target_bb = bb->next_bb;
		} else {
			/* Do not touch empty BBs that do not "fall through" to their next BB (like the exit BB) */
			return FALSE;
		}
	}
	
	/* Do not touch BBs following a switch (they are the "default" branch) */
	if ((previous_bb->last_ins != NULL) && (previous_bb->last_ins->opcode == OP_SWITCH)) {
		return FALSE;
	}
	
	/* Do not touch BBs following the entry BB and jumping to something that is not */
	/* thiry "next" bb (the entry BB cannot contain the branch) */
	if ((previous_bb == cfg->bb_entry) && (bb->next_bb != target_bb)) {
		return FALSE;
	}

	/* 
	 * Do not touch BBs following a try block as the code in 
	 * mini_method_compile needs them to compute the length of the try block.
	 */
	if (MONO_BBLOCK_IS_IN_REGION (previous_bb, MONO_REGION_TRY))
		return FALSE;
	
	/* Check that there is a target BB, and that bb is not an empty loop (Bug 75061) */
	if ((target_bb != NULL) && (target_bb != bb)) {
		int i;

		if (cfg->verbose_level > 1) {
			printf ("remove_block_if_useless, removed BB%d\n", bb->block_num);
		}
		
		/* unlink_bblock () modifies the bb->in_bb array so can't use a for loop here */
		while (bb->in_count) {
			MonoBasicBlock *in_bb = bb->in_bb [0];
			mono_unlink_bblock (cfg, in_bb, bb);
			mono_link_bblock (cfg, in_bb, target_bb);
			replace_out_block_in_code (in_bb, bb, target_bb);
		}
		
		mono_unlink_bblock (cfg, bb, target_bb);
		if (previous_bb != cfg->bb_entry && mono_bb_is_fall_through (cfg, previous_bb)) {
			for (i = 0; i < previous_bb->out_count; i++) {
				if (previous_bb->out_bb [i] == target_bb) {
					MonoInst *jump;
					MONO_INST_NEW (cfg, jump, OP_BR);
					MONO_ADD_INS (previous_bb, jump);
					jump->cil_code = previous_bb->cil_code;
					jump->inst_target_bb = target_bb;
					break;
				}
			}
		}
		
		previous_bb->next_bb = bb->next_bb;
		mono_nullify_basic_block (bb);
		
		return TRUE;
	} else {
		return FALSE;
	}
}

void
mono_merge_basic_blocks (MonoCompile *cfg, MonoBasicBlock *bb, MonoBasicBlock *bbn) 
{
	MonoInst *inst;
	MonoBasicBlock *prev_bb;
	int i;

	/* There may be only one control flow edge between two BBs that we merge, and it should connect these BBs together. */
	g_assert (bb->out_count == 1 && bbn->in_count == 1 && bb->out_bb [0] == bbn && bbn->in_bb [0] == bb);

	bb->has_array_access |= bbn->has_array_access;
	bb->extended |= bbn->extended;

	mono_unlink_bblock (cfg, bb, bbn);
	for (i = 0; i < bbn->out_count; ++i)
		mono_link_bblock (cfg, bb, bbn->out_bb [i]);
	while (bbn->out_count)
		mono_unlink_bblock (cfg, bbn, bbn->out_bb [0]);

	/* Handle the branch at the end of the bb */
	if (bb->has_call_handler) {
		for (inst = bb->code; inst != NULL; inst = inst->next) {
			if (inst->opcode == OP_CALL_HANDLER) {
				g_assert (inst->inst_target_bb == bbn);
				NULLIFY_INS (inst);
			}
		}
	}
	if (bb->has_jump_table) {
		for (inst = bb->code; inst != NULL; inst = inst->next) {
			if (MONO_IS_JUMP_TABLE (inst)) {
				int i;
				MonoJumpInfoBBTable *table = (MonoJumpInfoBBTable *)MONO_JUMP_TABLE_FROM_INS (inst);
				for (i = 0; i < table->table_size; i++ ) {
					/* Might be already NULL from a previous merge */
					if (table->table [i])
						g_assert (table->table [i] == bbn);
					table->table [i] = NULL;
				}
				/* Can't nullify this as later instructions depend on it */
			}
		}
	}
	if (bb->last_ins && MONO_IS_COND_BRANCH_OP (bb->last_ins)) {
		g_assert (bb->last_ins->inst_false_bb == bbn);
		bb->last_ins->inst_false_bb = NULL;
		bb->extended = TRUE;
	} else if (bb->last_ins && MONO_IS_BRANCH_OP (bb->last_ins)) {
		NULLIFY_INS (bb->last_ins);
	}

	bb->has_call_handler |= bbn->has_call_handler;
	bb->has_jump_table |= bbn->has_jump_table;

	if (bb->last_ins) {
		if (bbn->code) {
			bb->last_ins->next = bbn->code;
			bbn->code->prev = bb->last_ins;
			bb->last_ins = bbn->last_ins;
		}
	} else {
		bb->code = bbn->code;
		bb->last_ins = bbn->last_ins;
	}


	/* Check if the control flow predecessor is also the linear IL predecessor. */
	if (bbn->in_bb [0]->next_bb == bbn)
		prev_bb = bbn->in_bb [0];
	else
		/* If it isn't, look for one among all basic blocks. */
		for (prev_bb = cfg->bb_entry; prev_bb && prev_bb->next_bb != bbn; prev_bb = prev_bb->next_bb)
			;
	if (prev_bb) {
		prev_bb->next_bb = bbn->next_bb;
	} else {
		/* bbn might not be in the bb list yet */
		if (bb->next_bb == bbn)
			bb->next_bb = bbn->next_bb;
	}
	mono_nullify_basic_block (bbn);

	/* 
	 * If bbn fell through to its next bblock, have to add a branch, since bb
	 * will not fall though to the same bblock (#513931).
	 */
	if (bb->last_ins && bb->out_count == 1 && bb->out_bb [0] != bb->next_bb && !MONO_IS_BRANCH_OP (bb->last_ins)) {
		MONO_INST_NEW (cfg, inst, OP_BR);
		inst->inst_target_bb = bb->out_bb [0];
		MONO_ADD_INS (bb, inst);
	}
}

static void
move_basic_block_to_end (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoBasicBlock *bbn, *next;

	next = bb->next_bb;

	/* Find the previous */
	for (bbn = cfg->bb_entry; bbn->next_bb && bbn->next_bb != bb; bbn = bbn->next_bb)
		;
	if (bbn->next_bb) {
		bbn->next_bb = bb->next_bb;
	}

	/* Find the last */
	for (bbn = cfg->bb_entry; bbn->next_bb; bbn = bbn->next_bb)
		;
	bbn->next_bb = bb;
	bb->next_bb = NULL;

	/* Add a branch */
	if (next && (!bb->last_ins || ((bb->last_ins->opcode != OP_NOT_REACHED) && (bb->last_ins->opcode != OP_BR) && (bb->last_ins->opcode != OP_BR_REG) && (!MONO_IS_COND_BRANCH_OP (bb->last_ins))))) {
		MonoInst *ins;

		MONO_INST_NEW (cfg, ins, OP_BR);
		MONO_ADD_INS (bb, ins);
		mono_link_bblock (cfg, bb, next);
		ins->inst_target_bb = next;
	}		
}

/*
 * mono_remove_block:
 *
 *   Remove BB from the control flow graph
 */
void
mono_remove_bblock (MonoCompile *cfg, MonoBasicBlock *bb) 
{
	MonoBasicBlock *tmp_bb;

	for (tmp_bb = cfg->bb_entry; tmp_bb && tmp_bb->next_bb != bb; tmp_bb = tmp_bb->next_bb)
		;

	g_assert (tmp_bb);
	tmp_bb->next_bb = bb->next_bb;
}

void
mono_remove_critical_edges (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	MonoBasicBlock *previous_bb;
	
	if (cfg->verbose_level > 3) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			int i;
			printf ("remove_critical_edges, BEFORE BB%d (in:", bb->block_num);
			for (i = 0; i < bb->in_count; i++) {
				printf (" %d", bb->in_bb [i]->block_num);
			}
			printf (") (out:");
			for (i = 0; i < bb->out_count; i++) {
				printf (" %d", bb->out_bb [i]->block_num);
			}
			printf (")");
			if (bb->last_ins != NULL) {
				printf (" ");
				mono_print_ins (bb->last_ins);
			}
			printf ("\n");
		}
	}
	
	for (previous_bb = cfg->bb_entry, bb = previous_bb->next_bb; bb != NULL; previous_bb = previous_bb->next_bb, bb = bb->next_bb) {
		if (bb->in_count > 1) {
			int in_bb_index;
			for (in_bb_index = 0; in_bb_index < bb->in_count; in_bb_index++) {
				MonoBasicBlock *in_bb = bb->in_bb [in_bb_index];
				/* 
				 * Have to remove non-critical edges whose source ends with a BR_REG
				 * ins too, since inserting a computation before the BR_REG could 
				 * overwrite the sreg1 of the ins.
				 */
				if ((in_bb->out_count > 1) || (in_bb->out_count == 1 && in_bb->last_ins && in_bb->last_ins->opcode == OP_BR_REG)) {
					MonoBasicBlock *new_bb = (MonoBasicBlock *)mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
					new_bb->block_num = cfg->num_bblocks++;
//					new_bb->real_offset = bb->real_offset;
					new_bb->region = bb->region;
					
					/* Do not alter the CFG while altering the BB list */
					if (mono_bb_is_fall_through (cfg, previous_bb)) {
						if (previous_bb != cfg->bb_entry) {
							int i;
							/* Make sure previous_bb really falls through bb */
							for (i = 0; i < previous_bb->out_count; i++) {
								if (previous_bb->out_bb [i] == bb) {
									MonoInst *jump;
									MONO_INST_NEW (cfg, jump, OP_BR);
									MONO_ADD_INS (previous_bb, jump);
									jump->cil_code = previous_bb->cil_code;
									jump->inst_target_bb = bb;
									break;
								}
							}
						} else {
							/* We cannot add any inst to the entry BB, so we must */
							/* put a new BB in the middle to hold the OP_BR */
							MonoInst *jump;
							MonoBasicBlock *new_bb_after_entry = (MonoBasicBlock *)mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
							new_bb_after_entry->block_num = cfg->num_bblocks++;
//							new_bb_after_entry->real_offset = bb->real_offset;
							new_bb_after_entry->region = bb->region;
							
							MONO_INST_NEW (cfg, jump, OP_BR);
							MONO_ADD_INS (new_bb_after_entry, jump);
							jump->cil_code = bb->cil_code;
							jump->inst_target_bb = bb;

							mono_unlink_bblock (cfg, previous_bb, bb);
							mono_link_bblock (cfg, new_bb_after_entry, bb);
							mono_link_bblock (cfg, previous_bb, new_bb_after_entry);
							
							previous_bb->next_bb = new_bb_after_entry;
							previous_bb = new_bb_after_entry;

							if (cfg->verbose_level > 2) {
								printf ("remove_critical_edges, added helper BB%d jumping to BB%d\n", new_bb_after_entry->block_num, bb->block_num);
							}
						}
					}
					
					/* Insert new_bb in the BB list */
					previous_bb->next_bb = new_bb;
					new_bb->next_bb = bb;
					previous_bb = new_bb;
					
					/* Setup in_bb and out_bb */
					new_bb->in_bb = (MonoBasicBlock **)mono_mempool_alloc ((cfg)->mempool, sizeof (MonoBasicBlock*));
					new_bb->in_bb [0] = in_bb;
					new_bb->in_count = 1;
					new_bb->out_bb = (MonoBasicBlock **)mono_mempool_alloc ((cfg)->mempool, sizeof (MonoBasicBlock*));
					new_bb->out_bb [0] = bb;
					new_bb->out_count = 1;
					
					/* Relink in_bb and bb to (from) new_bb */
					replace_out_block (in_bb, bb, new_bb);
					replace_out_block_in_code (in_bb, bb, new_bb);
					replace_in_block (bb, in_bb, new_bb);
					
					if (cfg->verbose_level > 2) {
						printf ("remove_critical_edges, removed critical edge from BB%d to BB%d (added BB%d)\n", in_bb->block_num, bb->block_num, new_bb->block_num);
					}
				}
			}
		}
	}
	
	if (cfg->verbose_level > 3) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			int i;
			printf ("remove_critical_edges, AFTER BB%d (in:", bb->block_num);
			for (i = 0; i < bb->in_count; i++) {
				printf (" %d", bb->in_bb [i]->block_num);
			}
			printf (") (out:");
			for (i = 0; i < bb->out_count; i++) {
				printf (" %d", bb->out_bb [i]->block_num);
			}
			printf (")");
			if (bb->last_ins != NULL) {
				printf (" ");
				mono_print_ins (bb->last_ins);
			}
			printf ("\n");
		}
	}
}

/*
 * Optimizes the branches on the Control Flow Graph
 *
 */
void
mono_optimize_branches (MonoCompile *cfg)
{
	int i, count = 0, changed = FALSE;
	MonoBasicBlock *bb, *bbn;
	guint32 niterations;
	MonoInst *bbn_first_inst;
	int filter = FILTER_IL_SEQ_POINT;

	/*
	 * Some crazy loops could cause the code below to go into an infinite
	 * loop, see bug #53003 for an example. To prevent this, we put an upper
	 * bound on the number of iterations.
	 */
	if (cfg->num_bblocks > 1000)
		niterations = cfg->num_bblocks * 2;
	else
		niterations = 1000;
	
	do {
		MonoBasicBlock *previous_bb;
		changed = FALSE;
		niterations --;

		/* we skip the entry block (exit is handled specially instead ) */
		for (previous_bb = cfg->bb_entry, bb = cfg->bb_entry->next_bb; bb; previous_bb = bb, bb = bb->next_bb) {
			count ++;
			if (count == 1000) {
				mono_threads_safepoint ();
				count = 0;
			}
			/* dont touch code inside exception clauses */
			if (bb->region != -1)
				continue;

			if (!bb->not_useless && remove_block_if_useless (cfg, bb, previous_bb)) {
				changed = TRUE;
				continue;
			}

			if ((bbn = bb->next_bb) && bbn->in_count == 0 && bbn != cfg->bb_exit && bb->region == bbn->region) {
				if (cfg->verbose_level > 2)
					g_print ("nullify block triggered %d\n", bbn->block_num);

				bb->next_bb = bbn->next_bb;

				for (i = 0; i < bbn->out_count; i++)
					replace_in_block (bbn->out_bb [i], bbn, NULL);

				mono_nullify_basic_block (bbn);			
				changed = TRUE;
			}

			if (bb->out_count == 1) {
				bbn = bb->out_bb [0];

				/* conditional branches where true and false targets are the same can be also replaced with OP_BR */
				if (bb->last_ins && (bb->last_ins->opcode != OP_BR) && MONO_IS_COND_BRANCH_OP (bb->last_ins)) {
					bb->last_ins->opcode = OP_BR;
					bb->last_ins->inst_target_bb = bb->last_ins->inst_true_bb;
					changed = TRUE;
					if (cfg->verbose_level > 2)
						g_print ("cond branch removal triggered in %d %d\n", bb->block_num, bb->out_count);
				}

				if (bb->region == bbn->region && bb->next_bb == bbn) {
					/* the block are in sequence anyway ... */

					/* branches to the following block can be removed */
					if (bb->last_ins && bb->last_ins->opcode == OP_BR && !bbn->out_of_line) {
						NULLIFY_INS (bb->last_ins);
						changed = TRUE;
						if (cfg->verbose_level > 2)
							g_print ("br removal triggered %d -> %d\n", bb->block_num, bbn->block_num);
					}

					if (bbn->in_count == 1 && !bb->extended) {
						if (bbn != cfg->bb_exit) {
							if (cfg->verbose_level > 2)
								g_print ("block merge triggered %d -> %d\n", bb->block_num, bbn->block_num);
							mono_merge_basic_blocks (cfg, bb, bbn);
							changed = TRUE;
							continue;
						}

						//mono_print_bb_code (bb);
					}
				}
			}

			if ((bbn = bb->next_bb) && bbn->in_count == 0 && bbn != cfg->bb_exit && bb->region == bbn->region) {
				if (cfg->verbose_level > 2) {
					g_print ("nullify block triggered %d\n", bbn->block_num);
				}
				bb->next_bb = bbn->next_bb;

				for (i = 0; i < bbn->out_count; i++)
					replace_in_block (bbn->out_bb [i], bbn, NULL);

				mono_nullify_basic_block (bbn);			
				changed = TRUE;
				continue;
			}

			if (bb->out_count == 1) {
				bbn = bb->out_bb [0];

				if (bb->last_ins && bb->last_ins->opcode == OP_BR) {
					bbn = bb->last_ins->inst_target_bb;
					bbn_first_inst = mono_bb_first_inst (bbn, filter);
					if (bb->region == bbn->region && bbn_first_inst && bbn_first_inst->opcode == OP_BR &&
						bbn_first_inst->inst_target_bb != bbn &&
						bbn_first_inst->inst_target_bb->region == bb->region) {
						
						if (cfg->verbose_level > 2)
							g_print ("branch to branch triggered %d -> %d -> %d\n", bb->block_num, bbn->block_num, bbn_first_inst->inst_target_bb->block_num);

						replace_in_block (bbn, bb, NULL);
						replace_out_block (bb, bbn, bbn_first_inst->inst_target_bb);
						mono_link_bblock (cfg, bb, bbn_first_inst->inst_target_bb);
						bb->last_ins->inst_target_bb = bbn_first_inst->inst_target_bb;
						changed = TRUE;
						continue;
					}
				}
			} else if (bb->out_count == 2) {
				if (bb->last_ins && MONO_IS_COND_BRANCH_NOFP (bb->last_ins)) {
					int branch_result;
					MonoBasicBlock *taken_branch_target = NULL, *untaken_branch_target = NULL;

					if (bb->last_ins->flags & MONO_INST_CFOLD_TAKEN)
						branch_result = BRANCH_TAKEN;
					else if (bb->last_ins->flags & MONO_INST_CFOLD_NOT_TAKEN)
						branch_result = BRANCH_NOT_TAKEN;
					else
						branch_result = BRANCH_UNDEF;

					if (branch_result == BRANCH_TAKEN) {
						taken_branch_target = bb->last_ins->inst_true_bb;
						untaken_branch_target = bb->last_ins->inst_false_bb;
					} else if (branch_result == BRANCH_NOT_TAKEN) {
						taken_branch_target = bb->last_ins->inst_false_bb;
						untaken_branch_target = bb->last_ins->inst_true_bb;
					}
					if (taken_branch_target) {
						/* if mono_eval_cond_branch () is ever taken to handle 
						 * non-constant values to compare, issue a pop here.
						 */
						bb->last_ins->opcode = OP_BR;
						bb->last_ins->inst_target_bb = taken_branch_target;
						if (!bb->extended)
							mono_unlink_bblock (cfg, bb, untaken_branch_target);
						changed = TRUE;
						continue;
					}
					bbn = bb->last_ins->inst_true_bb;
					bbn_first_inst = mono_bb_first_inst (bbn, filter);
					if (bb->region == bbn->region && bbn_first_inst && bbn_first_inst->opcode == OP_BR &&
					    bbn_first_inst->inst_target_bb->region == bb->region) {
						if (cfg->verbose_level > 2)		
							g_print ("cbranch1 to branch triggered %d -> (%d) %d (0x%02x)\n", 
								 bb->block_num, bbn->block_num, bbn_first_inst->inst_target_bb->block_num, 
								 bbn_first_inst->opcode);

						/* 
						 * Unlink, then relink bblocks to avoid various
						 * tricky situations when the two targets of the branch
						 * are equal, or will become equal after the change.
						 */
						mono_unlink_bblock (cfg, bb, bb->last_ins->inst_true_bb);
						mono_unlink_bblock (cfg, bb, bb->last_ins->inst_false_bb);

						bb->last_ins->inst_true_bb = bbn_first_inst->inst_target_bb;

						mono_link_bblock (cfg, bb, bb->last_ins->inst_true_bb);
						mono_link_bblock (cfg, bb, bb->last_ins->inst_false_bb);

						changed = TRUE;
						continue;
					}

					bbn = bb->last_ins->inst_false_bb;
					bbn_first_inst = mono_bb_first_inst (bbn, filter);
					if (bbn && bb->region == bbn->region && bbn_first_inst && bbn_first_inst->opcode == OP_BR &&
						bbn_first_inst->inst_target_bb->region == bb->region) {
						if (cfg->verbose_level > 2)
							g_print ("cbranch2 to branch triggered %d -> (%d) %d (0x%02x)\n", 
								 bb->block_num, bbn->block_num, bbn_first_inst->inst_target_bb->block_num, 
								 bbn_first_inst->opcode);

						mono_unlink_bblock (cfg, bb, bb->last_ins->inst_true_bb);
						mono_unlink_bblock (cfg, bb, bb->last_ins->inst_false_bb);

						bb->last_ins->inst_false_bb = bbn_first_inst->inst_target_bb;

						mono_link_bblock (cfg, bb, bb->last_ins->inst_true_bb);
						mono_link_bblock (cfg, bb, bb->last_ins->inst_false_bb);

						changed = TRUE;
						continue;
					}

					bbn = bb->last_ins->inst_false_bb;
					/*
					 * If bb is an extended bb, it could contain an inside branch to bbn.
					 * FIXME: Enable the optimization if that is not true.
					 * If bblocks_linked () is true, then merging bb and bbn
					 * would require addition of an extra branch at the end of bbn 
					 * slowing down loops.
					 */
					if (bbn && bb->region == bbn->region && bbn->in_count == 1 && cfg->enable_extended_bblocks && bbn != cfg->bb_exit && !bb->extended && !bbn->out_of_line && !mono_bblocks_linked (bbn, bb)) {
						g_assert (bbn->in_bb [0] == bb);
						if (cfg->verbose_level > 2)
							g_print ("merge false branch target triggered BB%d -> BB%d\n", bb->block_num, bbn->block_num);
						mono_merge_basic_blocks (cfg, bb, bbn);
						changed = TRUE;
						continue;
					}
				}

				if (bb->last_ins && MONO_IS_COND_BRANCH_NOFP (bb->last_ins)) {
					if (bb->last_ins->inst_false_bb && bb->last_ins->inst_false_bb->out_of_line && (bb->region == bb->last_ins->inst_false_bb->region) && !cfg->disable_out_of_line_bblocks) {
						/* Reverse the branch */
						bb->last_ins->opcode = mono_reverse_branch_op (bb->last_ins->opcode);
						bbn = bb->last_ins->inst_false_bb;
						bb->last_ins->inst_false_bb = bb->last_ins->inst_true_bb;
						bb->last_ins->inst_true_bb = bbn;

						move_basic_block_to_end (cfg, bb->last_ins->inst_true_bb);
						if (cfg->verbose_level > 2)
							g_print ("cbranch to throw block triggered %d.\n", 
									 bb->block_num);
					}
				}
			}
		}
	} while (changed && (niterations > 0));
}

#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (branch_opts);

#endif /* !DISABLE_JIT */
