/*
 * ssa.c: Static single assign form support for the JIT compiler.
 *
 * Author:
 *    Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include <string.h>
#include <mono/metadata/debug-helpers.h>

#include "mini.h"

extern guint8 mono_burg_arity [];

#define USE_ORIGINAL_VARS
#define CREATE_PRUNED_SSA

//#define DEBUG_SSA 1

#define NEW_PHI(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_PHI;	\
		(dest)->inst_c0 = (val);	\
	} while (0)

#define NEW_ICONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_ICONST;	\
		(dest)->inst_c0 = (val);	\
		(dest)->type = STACK_I4;	\
	} while (0)


static GList*
g_list_prepend_mempool (GList* l, MonoMemPool* mp, gpointer datum)
{
	GList* n = mono_mempool_alloc (mp, sizeof (GList));
	n->next = l;
	n->prev = NULL;
	n->data = datum;
	return n;
}

static void 
unlink_target (MonoBasicBlock *bb, MonoBasicBlock *target)
{
	int i;

	for (i = 0; i < bb->out_count; i++) {
		if (bb->out_bb [i] == target) {
			bb->out_bb [i] = bb->out_bb [--bb->out_count];
			break;
		}
	}
	for (i = 0; i < target->in_count; i++) {
		if (target->in_bb [i] == bb) {
			target->in_bb [i] = target->in_bb [--target->in_count];
			break;
			
		}
	}
}

static void
unlink_unused_bblocks (MonoCompile *cfg) 
{
	int i, j;
	MonoBasicBlock *bb;

	g_assert (cfg->comp_done & MONO_COMP_REACHABILITY);

	for (bb = cfg->bb_entry; bb && bb->next_bb;) {
		if (!(bb->next_bb->flags & BB_REACHABLE)) {
			bb->next_bb = bb->next_bb->next_bb;
		} else 
			bb = bb->next_bb;
	}

	for (i = 1; i < cfg->num_bblocks; i++) {
		bb = cfg->bblocks [i];
	       
		if (!(bb->flags & BB_REACHABLE)) {
			for (j = 0; j < bb->in_count; j++) {
				unlink_target (bb->in_bb [j], bb);	
			}
			for (j = 0; j < bb->out_count; j++) {
				unlink_target (bb, bb->out_bb [j]);	
			}
		}
 
	}
}



static void
replace_usage (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *inst, MonoInst **stack)
{
	int arity;

	if (!inst)
		return;

	arity = mono_burg_arity [inst->opcode];

	if ((inst->ssa_op == MONO_SSA_LOAD || inst->ssa_op == MONO_SSA_MAYBE_LOAD) && 
	    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG)) {
		MonoInst *new_var;
		int idx = inst->inst_i0->inst_c0;
			
		if (stack [idx]) {
			new_var = stack [idx];
		} else {
			new_var = cfg->varinfo [idx];

			if ((new_var->opcode != OP_ARG) && (new_var->opcode != OP_LOCAL)) {
				/* uninitialized variable ? */
				g_warning ("using uninitialized variables %d in BB%d (%s)", idx, bb->block_num,
					   mono_method_full_name (cfg->method, TRUE));
				//g_assert_not_reached ();
			}
		}
#ifdef DEBUG_SSA
		printf ("REPLACE BB%d %d %d\n", bb->block_num, idx, new_var->inst_c0);
#endif
		inst->inst_i0 = new_var;
	} else {

		if (arity) {
			if (inst->ssa_op != MONO_SSA_STORE)
				replace_usage (cfg, bb, inst->inst_left, stack);
			if (arity > 1)
				replace_usage (cfg, bb, inst->inst_right, stack);
		}
	}
}

static int
extends_live (MonoInst *inst)
{
	int arity;

	if (!inst)
		return 0;

	arity = mono_burg_arity [inst->opcode];

	if (inst->ssa_op == MONO_SSA_LOAD && 
	    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG)) {
		return 1;
	} else {
		if (arity) {
			if (inst->ssa_op != MONO_SSA_STORE)
				if (extends_live (inst->inst_left))
					return 1;
			if (arity > 1)
				if (extends_live (inst->inst_right))
					return 1;
		}
	}

	return 0;
}

static int
replace_usage_new (MonoCompile *cfg, MonoInst *inst, int varnum, MonoInst *rep)
{
	int arity;

	if (!inst)
		return 0;

	arity = mono_burg_arity [inst->opcode];

	if ((inst->ssa_op == MONO_SSA_LOAD) && 
	    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG) &&
	    inst->inst_i0->inst_c0 == varnum && rep->type == inst->type) {
		*inst = *rep;
		return 1;
	} else {
		if (arity) {
			if (inst->ssa_op != MONO_SSA_STORE)
				if (replace_usage_new (cfg, inst->inst_left, varnum, rep))
					return 1;
			if (arity > 1)
				if (replace_usage_new (cfg, inst->inst_right, varnum, rep))
					return 1;
		}
	}

	return 0;
}

static void
mono_ssa_rename_vars (MonoCompile *cfg, int max_vars, MonoBasicBlock *bb, MonoInst **stack) 
{
	MonoInst *inst, *new_var;
	int i, j, idx;
	GList *tmp;
	MonoInst **new_stack;

#ifdef DEBUG_SSA
	printf ("RENAME VARS BB%d %s\n", bb->block_num, mono_method_full_name (cfg->method, TRUE));
#endif

	for (inst = bb->code; inst; inst = inst->next) {
		if (inst->opcode != OP_PHI)
			replace_usage (cfg, bb, inst, stack);

		if (inst->ssa_op == MONO_SSA_STORE && 
		    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG)) {
			idx = inst->inst_i0->inst_c0;
			g_assert (idx < max_vars);

			if (!stack [idx] && bb == cfg->bb_init) {
				new_var = cfg->varinfo [idx];
			} else {
				new_var = mono_compile_create_var (cfg, inst->inst_i0->inst_vtype,  inst->inst_i0->opcode);
				new_var->flags = inst->inst_i0->flags;
			}
#ifdef DEBUG_SSA
			printf ("DEF %d %d\n", idx, new_var->inst_c0);
#endif
			inst->inst_i0 = new_var;

#ifdef USE_ORIGINAL_VARS
			cfg->vars [new_var->inst_c0]->reg = idx;
#endif

			stack [idx] = new_var;
		}
	}

	for (i = 0; i < bb->out_count; i++) {
		MonoBasicBlock *n = bb->out_bb [i];

		for (j = 0; j < n->in_count; j++)
			if (n->in_bb [j] == bb)
				break;
		
		for (inst = n->code; inst; inst = inst->next) {
			if (inst->ssa_op == MONO_SSA_STORE && inst->inst_i1->opcode == OP_PHI) {
				idx = inst->inst_i1->inst_c0;
				if (stack [idx])
					new_var = stack [idx];
				else
					new_var = cfg->varinfo [idx];
#ifdef DEBUG_SSA
				printf ("FOUND PHI %d (%d, %d)\n", idx, j, new_var->inst_c0);
#endif
				inst->inst_i1->inst_phi_args [j + 1] = new_var->inst_c0;
				
			}
		}
	}

	if (bb->dominated) {
		new_stack = g_new (MonoInst*, max_vars);
		for (tmp = bb->dominated; tmp; tmp = tmp->next) {
			memcpy (new_stack, stack, sizeof (MonoInst *) * max_vars); 
			mono_ssa_rename_vars (cfg, max_vars, (MonoBasicBlock *)tmp->data, new_stack);
		}
		g_free (new_stack);
	}
}

void
mono_ssa_compute (MonoCompile *cfg)
{
	int i, idx;
	MonoBitSet *set;
	MonoMethodVar *vinfo = g_new0 (MonoMethodVar, cfg->num_varinfo);
	MonoInst *inst, *store, **stack;

	g_assert (!(cfg->comp_done & MONO_COMP_SSA));

	/* we dont support methods containing exception clauses */
	g_assert (mono_method_get_header (cfg->method)->num_clauses == 0);
	g_assert (!cfg->disable_ssa);

	//printf ("COMPUTS SSA %s %d\n", mono_method_full_name (cfg->method, TRUE), cfg->num_varinfo);

#ifdef CREATE_PRUNED_SSA
	/* we need liveness for pruned SSA */
	if (!(cfg->comp_done & MONO_COMP_LIVENESS))
		mono_analyze_liveness (cfg);
#endif

	mono_compile_dominator_info (cfg, MONO_COMP_DOM | MONO_COMP_IDOM | MONO_COMP_DFRONTIER);

	for (i = 0; i < cfg->num_varinfo; ++i) {
		vinfo [i].def_in = mono_bitset_new (cfg->num_bblocks, 0);
		vinfo [i].idx = i;
		/* implizit reference at start */
		mono_bitset_set (vinfo [i].def_in, 0);
	}
	for (i = 0; i < cfg->num_bblocks; ++i) {
		for (inst = cfg->bblocks [i]->code; inst; inst = inst->next) {
			if (inst->ssa_op == MONO_SSA_STORE) {
				idx = inst->inst_i0->inst_c0;
				g_assert (idx < cfg->num_varinfo);
				mono_bitset_set (vinfo [idx].def_in, i);
			} 
		}
	}

	/* insert phi functions */
	for (i = 0; i < cfg->num_varinfo; ++i) {
		set = mono_compile_iterated_dfrontier (cfg, vinfo [i].def_in);
		vinfo [i].dfrontier = set;
		mono_bitset_foreach_bit (set, idx, cfg->num_bblocks) {
			MonoBasicBlock *bb = cfg->bblocks [idx];

			/* fixme: create pruned SSA? we would need liveness information for that */

			if (bb == cfg->bb_exit)
				continue;

			if ((cfg->comp_done & MONO_COMP_LIVENESS) && !mono_bitset_test_fast (bb->live_in_set, i)) {
				//printf ("%d is not live in BB%d %s\n", i, bb->block_num, mono_method_full_name (cfg->method, TRUE));
				continue;
			}

			NEW_PHI (cfg, inst, i);

			inst->inst_phi_args =  mono_mempool_alloc0 (cfg->mempool, sizeof (int) * (cfg->bblocks [idx]->in_count + 1));
			inst->inst_phi_args [0] = cfg->bblocks [idx]->in_count;

			store = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));
			if (!cfg->varinfo [i]->inst_vtype->type)
				g_assert_not_reached ();
			store->opcode = mono_type_to_stind (cfg->varinfo [i]->inst_vtype);
			store->ssa_op = MONO_SSA_STORE;
			store->inst_i0 = cfg->varinfo [i];
			store->inst_i1 = inst;
			store->klass = store->inst_i0->klass;
	     
			store->next = bb->code;
			bb->code = store;

#ifdef DEBUG_SSA
			printf ("ADD PHI BB%d %s\n", cfg->bblocks [idx]->block_num, mono_method_full_name (cfg->method, TRUE));
#endif
		}
	}

	/* free the stuff */
	for (i = 0; i < cfg->num_varinfo; ++i)
		mono_bitset_free (vinfo [i].def_in);
	g_free (vinfo);


	stack = alloca (sizeof (MonoInst *) * cfg->num_varinfo);
		
	for (i = 0; i < cfg->num_varinfo; i++)
		stack [i] = NULL;

	mono_ssa_rename_vars (cfg, cfg->num_varinfo, cfg->bb_entry, stack);

	cfg->comp_done |= MONO_COMP_SSA;
}

#ifndef USE_ORIGINAL_VARS
static GPtrArray *
mono_ssa_get_allocatable_vars (MonoCompile *cfg)
{
	GHashTable *type_hash;
	GPtrArray *varlist_array = g_ptr_array_new ();
	int tidx, i;

	g_assert (cfg->comp_done & MONO_COMP_LIVENESS);

	type_hash = g_hash_table_new (NULL, NULL);

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos > vmv->range.last_use.abs_pos)
			continue;

		if (ins->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT) || 
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG) || vmv->reg != -1)
			continue;

		g_assert (ins->inst_vtype);
		g_assert (vmv->reg == -1);
		g_assert (i == vmv->idx);

		if (!(tidx = (int)g_hash_table_lookup (type_hash, ins->inst_vtype))) {
			GList *vars = g_list_append (NULL, vmv);
			g_ptr_array_add (varlist_array, vars);
			g_hash_table_insert (type_hash, ins->inst_vtype, (gpointer)varlist_array->len);
		} else {
			tidx--;
			g_ptr_array_index (varlist_array, tidx) =
				mono_varlist_insert_sorted (cfg, g_ptr_array_index (varlist_array, tidx), vmv, FALSE);
		}
	}

	g_hash_table_destroy (type_hash);

	return varlist_array;
}
#endif

static void
mono_ssa_replace_copies (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *inst, char *is_live)
{
	int arity;

	if (!inst)
		return;

	arity = mono_burg_arity [inst->opcode];

	if ((inst->ssa_op == MONO_SSA_LOAD || inst->ssa_op == MONO_SSA_MAYBE_LOAD || inst->ssa_op == MONO_SSA_STORE) && 
	    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG)) {
		MonoInst *new_var;
		int idx = inst->inst_i0->inst_c0;
		MonoMethodVar *mv = cfg->vars [idx];

		if (mv->reg != -1 && mv->reg != mv->idx) {
		       
			is_live [mv->reg] = 1;

			new_var = cfg->varinfo [mv->reg];

#if 0
			printf ("REPLACE COPY BB%d %d %d\n", bb->block_num, idx, new_var->inst_c0);
			g_assert (cfg->varinfo [mv->reg]->inst_vtype == cfg->varinfo [idx]->inst_vtype);
#endif
			inst->inst_i0 = new_var;
		} else {
			is_live [mv->idx] = 1;
		}
	}


	if (arity) {
		mono_ssa_replace_copies (cfg, bb, inst->inst_left, is_live);
		if (arity > 1)
			mono_ssa_replace_copies (cfg, bb, inst->inst_right, is_live);
	}

	if (inst->ssa_op == MONO_SSA_STORE && inst->inst_i1->ssa_op == MONO_SSA_LOAD &&
	    inst->inst_i0->inst_c0 == inst->inst_i1->inst_i0->inst_c0) {
		inst->ssa_op = MONO_SSA_NOP;
		inst->opcode = CEE_NOP;
	}

}

void
mono_ssa_remove (MonoCompile *cfg)
{
	MonoInst *inst, *phi;
	char *is_live;
	int i, j;
#ifndef USE_ORIGINAL_VARS
	GPtrArray *varlist_array;
	GList *active;
#endif
	g_assert (cfg->comp_done & MONO_COMP_SSA);

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		for (inst = bb->code; inst; inst = inst->next) {
			if (inst->ssa_op == MONO_SSA_STORE && inst->inst_i1->opcode == OP_PHI) {
				
				phi = inst->inst_i1;
				g_assert (phi->inst_phi_args [0] == bb->in_count);

				for (j = 0; j < bb->in_count; j++) {
					MonoBasicBlock *pred = bb->in_bb [j];
					int idx = phi->inst_phi_args [j + 1];
					MonoMethodVar *mv = cfg->vars [idx];

					if (mv->reg != -1 && mv->reg != mv->idx) {
						//printf ("PHICOPY %d %d -> %d\n", idx, mv->reg, inst->inst_i0->inst_c0);
						idx = mv->reg;
					}

					
					if (idx != inst->inst_i0->inst_c0) {
#ifdef DEBUG_SSA
						printf ("MOVE %d to %d in BB%d\n", idx, inst->inst_i0->inst_c0, pred->block_num);
#endif
						mono_add_varcopy_to_end (cfg, pred, idx, inst->inst_i0->inst_c0);
					}
				}

				/* remove the phi functions */
				inst->opcode = CEE_NOP;
				inst->ssa_op = MONO_SSA_NOP;
			} 
		}
	}
	
#ifndef USE_ORIGINAL_VARS
	/* we compute liveness again */
	cfg->comp_done &= ~MONO_COMP_LIVENESS;
	mono_analyze_liveness (cfg);

	varlist_array = mono_ssa_get_allocatable_vars (cfg);

	for (i = 0; i < varlist_array->len; i++) {
		GList *l, *regs, *vars = g_ptr_array_index (varlist_array, i);
		MonoMethodVar *vmv, *amv;
		
		if (g_list_length (vars) <= 1) {
			continue;
		}

		active = NULL;
		regs = NULL;

		for (l = vars; l; l = l->next) {
			vmv = l->data;

			/* expire old intervals in active */
			while (active) {
				amv = (MonoMethodVar *)active->data;

				if (amv->range.last_use.abs_pos >= vmv->range.first_use.abs_pos)
					break;

				active = g_list_delete_link (active, active);
				regs = g_list_prepend (regs, (gpointer)amv->reg);
			}

			if (!regs)
				regs = g_list_prepend (regs, (gpointer)vmv->idx);

			vmv->reg = (int)regs->data;
			regs = g_list_delete_link (regs, regs);
			active = mono_varlist_insert_sorted (cfg, active, vmv, TRUE);		
		}

		g_list_free (active);
		g_list_free (regs);
		g_list_free (vars);
	}

	g_ptr_array_free (varlist_array, TRUE);

#endif

	is_live = alloca (cfg->num_varinfo);
	memset (is_live, 0, cfg->num_varinfo);

	for (i = 0; i < cfg->num_bblocks; ++i) {
		MonoBasicBlock *bb = cfg->bblocks [i];

		for (inst = bb->code; inst; inst = inst->next)
			mono_ssa_replace_copies (cfg, bb, inst, is_live);
	}

	for (i = 0; i < cfg->num_varinfo; ++i) {
		cfg->vars [i]->reg = -1;
		if (!is_live [i]) {
			cfg->varinfo [i]->flags |= MONO_INST_IS_DEAD;
		}
	}

	if (cfg->comp_done & MONO_COMP_REACHABILITY)
		unlink_unused_bblocks (cfg);

	cfg->comp_done &= ~MONO_COMP_SSA;
}


#define IS_CALL(op) (op == CEE_CALLI || op == CEE_CALL || op == CEE_CALLVIRT || (op >= OP_VOIDCALL && op <= OP_CALL_MEMBASE))

typedef struct {
	MonoBasicBlock *bb;
	MonoInst *inst;
} MonoVarUsageInfo;

static void
analyze_dev_use (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *root, MonoInst *inst)
{
	MonoMethodVar *info;
	int i, idx, arity;

	if (!inst)
		return;

	arity = mono_burg_arity [inst->opcode];

	if ((inst->ssa_op == MONO_SSA_STORE) && 
	    (inst->inst_i0->opcode == OP_LOCAL /*|| inst->inst_i0->opcode == OP_ARG */)) {
		idx = inst->inst_i0->inst_c0;
		info = cfg->vars [idx];
		//printf ("%d defined in BB%d %p\n", idx, bb->block_num, root);
		if (info->def) {
			g_warning ("more than one definition of variable %d in %s", idx,
				   mono_method_full_name (cfg->method, TRUE));
			g_assert_not_reached ();
		}
		if (!IS_CALL (inst->inst_i1->opcode) /* && inst->inst_i1->opcode == OP_ICONST */) {
			g_assert (inst == root);
			info->def = root;
			info->def_bb = bb;
		}

		if (inst->inst_i1->opcode == OP_PHI) {
			for (i = inst->inst_i1->inst_phi_args [0]; i > 0; i--) {
				MonoVarUsageInfo *ui = mono_mempool_alloc (cfg->mempool, sizeof (MonoVarUsageInfo));
				idx = inst->inst_i1->inst_phi_args [i];	
				info = cfg->vars [idx];
				//printf ("FOUND %d\n", idx);
				ui->bb = bb;
				ui->inst = root;
				info->uses = g_list_prepend_mempool (info->uses, cfg->mempool, ui);
			}
		}
	}

	if ((inst->ssa_op == MONO_SSA_LOAD || inst->ssa_op == MONO_SSA_MAYBE_LOAD) && 
	    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG)) {
		MonoVarUsageInfo *ui = mono_mempool_alloc (cfg->mempool, sizeof (MonoVarUsageInfo));
		idx = inst->inst_i0->inst_c0;	
		info = cfg->vars [idx];
		//printf ("FOUND %d\n", idx);
		ui->bb = bb;
		ui->inst = root;
		info->uses = g_list_prepend_mempool (info->uses, cfg->mempool, ui);
	} else {
		if (arity) {
			//if (inst->ssa_op != MONO_SSA_STORE)
			analyze_dev_use (cfg, bb, root, inst->inst_left);
			if (arity > 1)
				analyze_dev_use (cfg, bb, root, inst->inst_right);
		}
	}
}


/* avoid unnecessary copies of variables:
 * Y <= X; Z = Y; is translated to Z = X;
 */
static void
mono_ssa_avoid_copies (MonoCompile *cfg)
{
	MonoInst *inst, *next;
	MonoBasicBlock *bb;
	MonoMethodVar *i1, *i2;

	g_assert ((cfg->comp_done & MONO_COMP_SSA_DEF_USE));

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		for (inst = bb->code; inst; inst = inst->next) {
			if (inst->ssa_op == MONO_SSA_STORE && inst->inst_i0->opcode == OP_LOCAL &&
			    !IS_CALL (inst->inst_i1->opcode) && inst->inst_i1->opcode != OP_PHI && !inst->flags) {
				i1 = cfg->vars [inst->inst_i0->inst_c0];

/* fixme: compiling mcs does not work when I enable this */
#if 0
				if (g_list_length (i1->uses) == 1 && !extends_live (inst->inst_i1)) {
					MonoVarUsageInfo *vi = (MonoVarUsageInfo *)i1->uses->data;
					u = vi->inst;

					//printf ("VAR %d %s\n", i1->idx, mono_method_full_name (cfg->method, TRUE));
					//mono_print_tree (inst); printf ("\n");
					//mono_print_tree (u); printf ("\n");

					if (replace_usage_new (cfg, u, inst->inst_i0->inst_c0,  inst->inst_i1)) {
														
						//mono_print_tree (u); printf ("\n");
							
						inst->opcode = CEE_NOP;
						inst->ssa_op = MONO_SSA_NOP;
					}
				}
#endif			
				if ((next = inst->next) && next->ssa_op == MONO_SSA_STORE && next->inst_i0->opcode == OP_LOCAL &&
				    next->inst_i1->ssa_op == MONO_SSA_LOAD &&  next->inst_i1->inst_i0->opcode == OP_LOCAL &&
				    next->inst_i1->inst_i0->inst_c0 == inst->inst_i0->inst_c0 && g_list_length (i1->uses) == 1 &&
				    inst->opcode == next->opcode && inst->inst_i0->type == next->inst_i0->type) {
					i2 = cfg->vars [next->inst_i0->inst_c0];
					//printf ("ELIM. COPY in BB%d %s\n", bb->block_num, mono_method_full_name (cfg->method, TRUE));
					inst->inst_i0 = next->inst_i0;
					i2->def = inst;
					i1->def = NULL;
					i1->uses = NULL;
					next->opcode = CEE_NOP;
					next->ssa_op = MONO_SSA_NOP;
				}
			}
		}
	}
}

static void
mono_ssa_create_def_use (MonoCompile *cfg) 
{
	MonoBasicBlock *bb;

	g_assert (!(cfg->comp_done & MONO_COMP_SSA_DEF_USE));

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *inst;
		for (inst = bb->code; inst; inst = inst->next) {
			analyze_dev_use (cfg, bb, inst, inst);
		}
	}

	cfg->comp_done |= MONO_COMP_SSA_DEF_USE;
}

static int
simulate_compare (int opcode, int a, int b)
{
	switch (opcode) {
	case CEE_BEQ:
		return a == b;
	case CEE_BGE:
		return a >= b;
	case CEE_BGT:
		return a > b;
	case CEE_BLE:
		return a <= b;
	case CEE_BLT:
		return a < b;
	case CEE_BNE_UN:
		return a != b;
	case CEE_BGE_UN:
		return (unsigned)a >= (unsigned)b;
	case CEE_BGT_UN:
		return (unsigned)a > (unsigned)b;
	case CEE_BLE_UN:
		return (unsigned)a <= (unsigned)b;
	case CEE_BLT_UN:
		return (unsigned)a < (unsigned)b;
	default:
		g_assert_not_reached ();
	}

	return 0;
}

static int
simulate_long_compare (int opcode, gint64 a, gint64 b)
{
	switch (opcode) {
	case CEE_BEQ:
		return a == b;
	case CEE_BGE:
		return a >= b;
	case CEE_BGT:
		return a > b;
	case CEE_BLE:
		return a <= b;
	case CEE_BLT:
		return a < b;
	case CEE_BNE_UN:
		return a != b;
	case CEE_BGE_UN:
		return (guint64)a >= (guint64)b;
	case CEE_BGT_UN:
		return (guint64)a > (guint64)b;
	case CEE_BLE_UN:
		return (guint64)a <= (guint64)b;
	case CEE_BLT_UN:
		return (guint64)a < (guint64)b;
	default:
		g_assert_not_reached ();
	}

	return 0;
}

#define EVAL_CXX(name,op,cast)	\
	case name:	\
		if ((inst->inst_i0->opcode == OP_COMPARE) || (inst->inst_i0->opcode == OP_LCOMPARE)) { \
			r1 = evaluate_const_tree (cfg, inst->inst_i0->inst_i0, &a, carray); \
			r2 = evaluate_const_tree (cfg, inst->inst_i0->inst_i1, &b, carray); \
			if (r1 == 1 && r2 == 1) { \
				*res = ((cast)a op (cast)b); \
				return 1; \
			} else { \
				return MAX (r1, r2); \
			} \
		} \
		break;

#define EVAL_BINOP(name,op)	\
	case name:	\
		r1 = evaluate_const_tree (cfg, inst->inst_i0, &a, carray); \
		r2 = evaluate_const_tree (cfg, inst->inst_i1, &b, carray); \
		if (r1 == 1 && r2 == 1) { \
			*res = (a op b); \
			return 1; \
		} else { \
			return MAX (r1, r2); \
		} \
		break;


/* fixme: this only works for interger constants, but not for other types (long, float) */
static int
evaluate_const_tree (MonoCompile *cfg, MonoInst *inst, int *res, MonoInst **carray)
{
	MonoInst *c0;
	int a, b, r1, r2;

	if (!inst)
		return 0;

	if (inst->ssa_op == MONO_SSA_LOAD && 
	    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG) &&
	    (c0 = carray [inst->inst_i0->inst_c0])) {
		*res = c0->inst_c0;
		return 1;
	}

	switch (inst->opcode) {
	case OP_ICONST:
		*res = inst->inst_c0;
		return 1;

	EVAL_CXX (OP_CEQ,==,gint32)
	EVAL_CXX (OP_CGT,>,gint32)
	EVAL_CXX (OP_CGT_UN,>,guint32)
	EVAL_CXX (OP_CLT,<,gint32)
	EVAL_CXX (OP_CLT_UN,<,guint32)

	EVAL_BINOP (CEE_ADD,+)
	EVAL_BINOP (CEE_SUB,-)
	EVAL_BINOP (CEE_MUL,*)
	EVAL_BINOP (CEE_AND,&)
	EVAL_BINOP (CEE_OR,|)
	EVAL_BINOP (CEE_XOR,^)
	EVAL_BINOP (CEE_SHL,<<)
	EVAL_BINOP (CEE_SHR,>>)

	default:
		return 2;
	}

	return 2;
}

static void
fold_tree (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *inst, MonoInst **carray)
{
	MonoInst *c0;
	int arity, a, b;

	if (!inst)
		return;

	arity = mono_burg_arity [inst->opcode];

	if (inst->ssa_op == MONO_SSA_STORE && 
	    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG) &&
	    inst->inst_i1->opcode == OP_PHI && (c0 = carray [inst->inst_i0->inst_c0])) {
		//{static int cn = 0; printf ("PHICONST %d %d %s\n", cn++, c0->inst_c0, mono_method_full_name (cfg->method, TRUE));}
		*inst->inst_i1 = *c0;		
	} else if (inst->ssa_op == MONO_SSA_LOAD && 
	    (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG) &&
	    (c0 = carray [inst->inst_i0->inst_c0])) {
		//{static int cn = 0; printf ("YCCOPY %d %d %s\n", cn++, c0->inst_c0, mono_method_full_name (cfg->method, TRUE));}
		*inst = *c0;
	} else {

		if (arity) {
			fold_tree (cfg, bb, inst->inst_left, carray);
			if (arity > 1)
				fold_tree (cfg, bb, inst->inst_right, carray);
			mono_constant_fold_inst (inst, NULL); 
		}
	}

	if ((inst->opcode >= CEE_BEQ && inst->opcode <= CEE_BLT_UN) &&
	    ((inst->inst_i0->opcode == OP_COMPARE) || (inst->inst_i0->opcode == OP_LCOMPARE))) {
		MonoInst *v0 = inst->inst_i0->inst_i0;
		MonoInst *v1 = inst->inst_i0->inst_i1;
		MonoBasicBlock *target = NULL;

		/* hack for longs to optimize the simply cases */
		if (v0->opcode == OP_I8CONST && v1->opcode == OP_I8CONST) {
			if (simulate_long_compare (inst->opcode, v0->inst_l, v1->inst_l)) {
				//unlink_target (bb, inst->inst_false_bb);
				target = inst->inst_true_bb;
			} else {
				//unlink_target (bb, inst->inst_true_bb);
				target = inst->inst_false_bb;
			}			
		} else if (evaluate_const_tree (cfg, v0, &a, carray) == 1 &&
			   evaluate_const_tree (cfg, v1, &b, carray) == 1) {				
			if (simulate_compare (inst->opcode, a, b)) {
				//unlink_target (bb, inst->inst_false_bb);
				target = inst->inst_true_bb;
			} else {
				//unlink_target (bb, inst->inst_true_bb);
				target = inst->inst_false_bb;
			}
		}

		if (target) {
			bb->out_bb [0] = target;
			bb->out_count = 1;
			inst->opcode = CEE_BR;
			inst->inst_target_bb = target;
		}
	} else if (inst->opcode == CEE_SWITCH && evaluate_const_tree (cfg, inst->inst_left, &a, carray) == 1) {
		bb->out_bb [0] = inst->inst_many_bb [a];
		bb->out_count = 1;
		inst->inst_target_bb = bb->out_bb [0];
		inst->opcode = CEE_BR;
	}

}

static void
change_varstate (MonoCompile *cfg, GList **cvars, MonoMethodVar *info, int state, MonoInst *c0, MonoInst **carray)
{
	if (info->cpstate >= state)
		return;

	info->cpstate = state;

	//printf ("SETSTATE %d to %d\n", info->idx, info->cpstate);

	if (state == 1)
		carray [info->idx] = c0;
	else
		carray [info->idx] = NULL;

	if (!g_list_find (*cvars, info)) {
		*cvars = g_list_prepend (*cvars, info);
	}
}

static void
visit_inst (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *inst, GList **cvars, GList **bblist, MonoInst **carray)
{
	g_assert (inst);

	if (inst->opcode == CEE_SWITCH) {
		int r1, i, a;

		r1 = evaluate_const_tree (cfg, inst->inst_left, &a, carray);
		if (r1 == 1) {
			MonoBasicBlock *tb = inst->inst_many_bb [a];
			if (!(tb->flags &  BB_REACHABLE)) {
				tb->flags |= BB_REACHABLE;
				*bblist = g_list_prepend (*bblist, tb);
			}
		} else if (r1 == 2) {
			for (i = (int)inst->klass; i >= 0; i--) {
				MonoBasicBlock *tb = inst->inst_many_bb [i];
				if (!(tb->flags &  BB_REACHABLE)) {
					tb->flags |= BB_REACHABLE;
					*bblist = g_list_prepend (*bblist, tb);
				}
			}
		}
	} else if ((inst->opcode >= CEE_BEQ && inst->opcode <= CEE_BLT_UN) &&
	    ((inst->inst_i0->opcode == OP_COMPARE) || (inst->inst_i0->opcode == OP_LCOMPARE))) {
		int a, b, r1, r2;
		MonoInst *v0 = inst->inst_i0->inst_i0;
		MonoInst *v1 = inst->inst_i0->inst_i1;

		r1 = evaluate_const_tree (cfg, v0, &a, carray);
		r2 = evaluate_const_tree (cfg, v1, &b, carray);

		if (r1 == 1 && r2 == 1) {
			MonoBasicBlock *target;
				
			if (simulate_compare (inst->opcode, a, b)) {
				target = inst->inst_true_bb;
			} else {
				target = inst->inst_false_bb;
			}
			if (!(target->flags &  BB_REACHABLE)) {
				target->flags |= BB_REACHABLE;
				*bblist = g_list_prepend (*bblist, target);
			}
		} else if (r1 == 2 || r2 == 2) {
			if (!(inst->inst_true_bb->flags &  BB_REACHABLE)) {
				inst->inst_true_bb->flags |= BB_REACHABLE;
				*bblist = g_list_prepend (*bblist, inst->inst_true_bb);
			}
			if (!(inst->inst_false_bb->flags &  BB_REACHABLE)) {
				inst->inst_false_bb->flags |= BB_REACHABLE;
				*bblist = g_list_prepend (*bblist, inst->inst_false_bb);
			}
		}	
	} else if (inst->ssa_op == MONO_SSA_STORE && 
		   (inst->inst_i0->opcode == OP_LOCAL || inst->inst_i0->opcode == OP_ARG)) {
		MonoMethodVar *info = cfg->vars [inst->inst_i0->inst_c0];
		MonoInst *i1 = inst->inst_i1;
		int res;
		
		if (info->cpstate < 2) {
			if (i1->opcode == OP_ICONST) { 
				change_varstate (cfg, cvars, info, 1, i1, carray);
			} else if (i1->opcode == OP_PHI) {
				MonoInst *c0 = NULL;
				int j;

				for (j = 1; j <= i1->inst_phi_args [0]; j++) {
					MonoMethodVar *mv = cfg->vars [i1->inst_phi_args [j]];
					MonoInst *src = mv->def;

					if (mv->def_bb && !(mv->def_bb->flags & BB_REACHABLE)) {
						continue;
					}

					if (!mv->def || !src || src->ssa_op != MONO_SSA_STORE ||
					    !(src->inst_i0->opcode == OP_LOCAL || src->inst_i0->opcode == OP_ARG) ||
					    mv->cpstate == 2) {
						change_varstate (cfg, cvars, info, 2, NULL, carray);
						break;
					}
					
					if (mv->cpstate == 0)
						continue;

					//g_assert (src->inst_i1->opcode == OP_ICONST);
					g_assert (carray [mv->idx]);

					if (!c0) {
						c0 = carray [mv->idx];
					}
					
					if (carray [mv->idx]->inst_c0 != c0->inst_c0) {
						change_varstate (cfg, cvars, info, 2, NULL, carray);
						break;
					}
				}
				
				if (c0 && info->cpstate < 1) {
					change_varstate (cfg, cvars, info, 1, c0, carray);
				}
			} else {
				int state = evaluate_const_tree (cfg, i1, &res, carray);
				if (state == 1) {
					NEW_ICONST (cfg, i1, res);
					change_varstate (cfg, cvars, info, 1, i1, carray);
				} else {
					change_varstate (cfg, cvars, info, 2, NULL, carray);
				}
			}
		}
	}
}

void
mono_ssa_cprop (MonoCompile *cfg) 
{
	MonoInst **carray;
	MonoBasicBlock *bb;
	GList *bblock_list, *cvars;
	GList *tmp;
	int i;
	//printf ("SIMPLE OPTS BB%d %s\n", bb->block_num, mono_method_full_name (cfg->method, TRUE));

	carray = g_new0 (MonoInst*, cfg->num_varinfo);

	if (!(cfg->comp_done & MONO_COMP_SSA_DEF_USE))
		mono_ssa_create_def_use (cfg);

	bblock_list = g_list_prepend (NULL, cfg->bb_entry);
	cfg->bb_entry->flags |= BB_REACHABLE;

	memset (carray, 0, sizeof (MonoInst *) * cfg->num_varinfo);

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoMethodVar *info = cfg->vars [i];
		if (!info->def)
			info->cpstate = 2;
	}

	cvars = NULL;

	while (bblock_list) {
		MonoInst *inst;

		bb = (MonoBasicBlock *)bblock_list->data;

		bblock_list = g_list_delete_link (bblock_list, bblock_list);

		g_assert (bb->flags &  BB_REACHABLE);

		if (bb->out_count == 1) {
			if (!(bb->out_bb [0]->flags &  BB_REACHABLE)) {
				bb->out_bb [0]->flags |= BB_REACHABLE;
				bblock_list = g_list_prepend (bblock_list, bb->out_bb [0]);
			}
		}

		for (inst = bb->code; inst; inst = inst->next) {
			visit_inst (cfg, bb, inst, &cvars, &bblock_list, carray);
		}

		while (cvars) {
			MonoMethodVar *info = (MonoMethodVar *)cvars->data;			
			cvars = g_list_delete_link (cvars, cvars);

			for (tmp = info->uses; tmp; tmp = tmp->next) {
				MonoVarUsageInfo *ui = (MonoVarUsageInfo *)tmp->data;
				if (!(ui->bb->flags & BB_REACHABLE))
					continue;
				visit_inst (cfg, ui->bb, ui->inst, &cvars, &bblock_list, carray);
			}
		}
	}

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		MonoInst *inst;
		for (inst = bb->code; inst; inst = inst->next) {
			fold_tree (cfg, bb, inst, carray);
		}
	}

	g_free (carray);

	cfg->comp_done |= MONO_COMP_REACHABILITY;
}

static void
add_to_dce_worklist (MonoCompile *cfg, MonoMethodVar *var, MonoMethodVar *use, GList **wl)
{
	GList *tmp;

	*wl = g_list_prepend (*wl, use);

	for (tmp = use->uses; tmp; tmp = tmp->next) {
		MonoVarUsageInfo *ui = (MonoVarUsageInfo *)tmp->data;
		if (ui->inst == var->def) {
			/* from the mempool */
			use->uses = g_list_remove_link (use->uses, tmp);
			break;
		}
	}	
}

void
mono_ssa_deadce (MonoCompile *cfg) 
{
	int i;
	GList *work_list;

	g_assert (cfg->comp_done & MONO_COMP_SSA);

	//printf ("DEADCE %s\n", mono_method_full_name (cfg->method, TRUE));

	/* fixme: we should update usage infos during cprop, instead of computing it again */
	cfg->comp_done &=  ~MONO_COMP_SSA_DEF_USE;
	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoMethodVar *info = cfg->vars [i];
		info->def = NULL;
		info->uses = NULL;
	}

	if (!(cfg->comp_done & MONO_COMP_SSA_DEF_USE))
		mono_ssa_create_def_use (cfg);

	mono_ssa_avoid_copies (cfg);

	work_list = NULL;
	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoMethodVar *info = cfg->vars [i];
		work_list = g_list_prepend (work_list, info);
	}

	while (work_list) {
		MonoMethodVar *info = (MonoMethodVar *)work_list->data;
		work_list = g_list_delete_link (work_list, work_list);

		if (!info->uses && info->def && (!(cfg->varinfo [info->idx]->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)))) {
			MonoInst *i1;
			//printf ("ELIMINATE %s: ", mono_method_full_name (cfg->method, TRUE)); mono_print_tree (info->def); printf ("\n");

			i1 = info->def->inst_i1;
			if (i1->opcode == OP_PHI) {
				int j;
				for (j = i1->inst_phi_args [0]; j > 0; j--) {
					MonoMethodVar *u = cfg->vars [i1->inst_phi_args [j]];
					add_to_dce_worklist (cfg, info, u, &work_list);
				}
			} else if (i1->ssa_op == MONO_SSA_LOAD &&
				   (i1->inst_i0->opcode == OP_LOCAL || i1->inst_i0->opcode == OP_ARG)) {
					MonoMethodVar *u = cfg->vars [i1->inst_i0->inst_c0];
					add_to_dce_worklist (cfg, info, u, &work_list);
			}

			info->def->opcode = CEE_NOP;
			info->def->ssa_op = MONO_SSA_NOP;
		}

	}
}

#if 0
void
mono_ssa_strength_reduction (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	int i;

	g_assert (cfg->comp_done & MONO_COMP_SSA);
	g_assert (cfg->comp_done & MONO_COMP_LOOPS);
	g_assert (cfg->comp_done & MONO_COMP_SSA_DEF_USE);

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		GList *lp = bb->loop_blocks;

		if (lp) {
			MonoBasicBlock *h = (MonoBasicBlock *)lp->data;

			/* we only consider loops with 2 in bblocks */
			if (!h->in_count == 2)
				continue;

			for (i = 0; i < cfg->num_varinfo; i++) {
				MonoMethodVar *info = cfg->vars [i];
			
				if (info->def && info->def->ssa_op == MONO_SSA_STORE &&
				    info->def->inst_i0->opcode == OP_LOCAL && g_list_find (lp, info->def_bb)) {
					MonoInst *v = info->def->inst_i1;


					printf ("FOUND %d in %s\n", info->idx, mono_method_full_name (cfg->method, TRUE));
				}
			}
		}
	}
}
#endif
