/*
 * branch-opts.c: Branch optimizations support 
 *
 * Authors:
 *   Patrik Torstensson (Patrik.Torstesson at gmail.com)
 *
 * (C) 2005 Ximian, Inc.  http://www.ximian.com
 */
 #include "mini.h"
 
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
	MonoMethod *method = cfg->method;
	MonoMethodHeader *header = mono_method_get_header (method);
	MonoExceptionClause *clause;
	MonoClass *exclass;
	int i;

	if (!(cfg->opt & MONO_OPT_EXCEPTION))
		return NULL;

	if (bb->region == -1 || !MONO_BBLOCK_IS_IN_REGION (bb, MONO_REGION_TRY))
		return NULL;

	exclass = mono_class_from_name (mono_get_corlib (), "System", exname);
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
						jump->inst_i1 = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
						jump->inst_true_bb = targetbb;

						if (cfg->verbose_level > 2) 
							g_print ("found exception to optimize - returning branch to BB%d (%s) (instead of throw) for method %s:%s\n", targetbb->block_num, clause->data.catch_class->name, cfg->method->klass->name, cfg->method->name);

						return jump;
					} 

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

void
mono_if_conversion (MonoCompile *cfg)
{
#ifdef MONO_ARCH_HAVE_CMOV_OPS
	MonoBasicBlock *bb;
	gboolean changed = FALSE;

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
			MonoInst *prev, *compare, *branch, *ins1, *ins2, *cmov, *move, *tmp;
			gboolean simple, ret;
			int dreg, tmp_reg;
			CompType comp_type;

			/* 
			 * Check that bb1 and bb2 are 'simple' and both assign to the same
			 * variable.
			 */
			/* FIXME: Get rid of the nops earlier */
			ins1 = bb1->code;
			while (ins1 && ins1->opcode == OP_NOP)
				ins1 = ins1->next;
			ins2 = bb2->code;
			while (ins2 && ins2->opcode == OP_NOP)
				ins2 = ins2->next;
			if (!(ins1 && ins2 && ins1->dreg == ins2->dreg && ins1->dreg != -1))
				continue;

			simple = TRUE;
			for (tmp = ins1->next; tmp; tmp = tmp->next)
				if (!((tmp->opcode == OP_NOP) || (tmp->opcode == OP_BR)))
					simple = FALSE;
					
			for (tmp = ins2->next; tmp; tmp = tmp->next)
				if (!((tmp->opcode == OP_NOP) || (tmp->opcode == OP_BR)))
					simple = FALSE;

			if (!simple)
				continue;

			/* We move ins1/ins2 before the compare so they should have no side effect */
			if (!(MONO_INS_HAS_NO_SIDE_EFFECT (ins1) && MONO_INS_HAS_NO_SIDE_EFFECT (ins2)))
				continue;

			if (bb->last_ins && (bb->last_ins->opcode == OP_BR_REG || bb->last_ins->opcode == OP_BR))
				continue;

			/* Find the compare instruction */
			/* FIXME: Optimize this using prev */
			prev = NULL;
			compare = bb->code;
			g_assert (compare);
			while (compare->next && !MONO_IS_COND_BRANCH_OP (compare->next)) {
				prev = compare;
				compare = compare->next;
			}
			g_assert (compare->next && MONO_IS_COND_BRANCH_OP (compare->next));
			branch = compare->next;

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
				printf ("\t\t"); mono_print_ins (compare->next);
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
			MONO_REMOVE_INS (bb1, ins1);
			MONO_REMOVE_INS (bb2, ins2);

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
			branch->inst_target_bb = bb1->out_bb [0];
			mono_link_bblock (cfg, bb, branch->inst_target_bb);

			/* Reorder bblocks */
			mono_unlink_bblock (cfg, bb, bb1);
			mono_unlink_bblock (cfg, bb, bb2);
			mono_unlink_bblock (cfg, bb1, bb1->out_bb [0]);
			mono_unlink_bblock (cfg, bb2, bb2->out_bb [0]);
			mono_remove_bblock (cfg, bb1);
			mono_remove_bblock (cfg, bb2);

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
			MonoInst *prev, *compare, *branch, *ins1, *cmov, *tmp;
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

			ins1 = code_bb->code;

			if (!ins1)
				continue;

			/* Check that code_bb is simple */
			simple = TRUE;
			for (tmp = ins1->next; tmp; tmp = tmp->next)
				if (!((tmp->opcode == OP_NOP) || (tmp->opcode == OP_BR)))
					simple = FALSE;

			if (!simple)
				continue;

			/* We move ins1 before the compare so it should have no side effect */
			if (!MONO_INS_HAS_NO_SIDE_EFFECT (ins1))
				continue;

			if (bb->last_ins && bb->last_ins->opcode == OP_BR_REG)
				continue;

			/* Find the compare instruction */
			/* FIXME: Optimize this using prev */
			prev = NULL;
			compare = bb->code;
			g_assert (compare);
			while (compare->next && !MONO_IS_COND_BRANCH_OP (compare->next)) {
				prev = compare;
				compare = compare->next;
			}
			g_assert (compare->next && MONO_IS_COND_BRANCH_OP (compare->next));
			branch = compare->next;

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

			if (cfg->verbose_level > 2) {
				printf ("\tBranch -> CMove optimization (2) in BB%d on\n", bb->block_num);
				printf ("\t\t"); mono_print_ins (compare);
				printf ("\t\t"); mono_print_ins (compare->next);
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
				goto restart;
			}
		}
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
			mono_local_cprop2 (cfg);
		mono_local_deadce (cfg);
	}
#endif
}
