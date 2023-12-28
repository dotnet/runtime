/*
 * Optimizations for interpreter codegen
 */

#include "mintops.h"
#include "transform.h"

/*
 * VAR OFFSET ALLOCATOR
 */

// Allocates var at the offset that tos points to, also updating it.
static int
alloc_var_offset (TransformData *td, int local, gint32 *ptos)
{
	int size, offset;

	offset = *ptos;
	size = td->vars [local].size;

	if (td->vars [local].simd)
		offset = ALIGN_TO (offset, MINT_SIMD_ALIGNMENT);

	td->vars [local].offset = offset;

	*ptos = ALIGN_TO (offset + size, MINT_STACK_SLOT_SIZE);

	return td->vars [local].offset;
}

int
interp_alloc_global_var_offset (TransformData *td, int var)
{
	return alloc_var_offset (td, var, &td->total_locals_size);
}

static void
set_var_live_range (TransformData *td, int var, int ins_index)
{
	// We don't track liveness yet for global vars
	if (td->vars [var].global)
		return;
	if (td->vars [var].live_start == -1)
		td->vars [var].live_start = ins_index;
	td->vars [var].live_end = ins_index;
}

static void
set_var_live_range_cb (TransformData *td, int *pvar, gpointer data)
{
	set_var_live_range (td, *pvar, (int)(gsize)data);
}

static void
initialize_global_var (TransformData *td, int var, int bb_index)
{
	// Check if already handled
	if (td->vars [var].global)
		return;

	if (td->vars [var].bb_index == -1) {
		td->vars [var].bb_index = bb_index;
	} else if (td->vars [var].bb_index != bb_index) {
		// var used in multiple basic blocks
		if (td->verbose_level)
			g_print ("alloc global var %d to offset %d\n", var, td->total_locals_size);
		interp_alloc_global_var_offset (td, var);
		td->vars [var].global = TRUE;
	}
}

static void
initialize_global_var_cb (TransformData *td, int *pvar, gpointer data)
{
	initialize_global_var (td, *pvar, (int)(gsize)data);
}

static void
initialize_global_vars (TransformData *td)
{
	InterpBasicBlock *bb;

	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins;

		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			int opcode = ins->opcode;
			if (opcode == MINT_NOP) {
				continue;
			} else if (opcode == MINT_LDLOCA_S) {
				int var = ins->sregs [0];
				// If global flag is set, it means its offset was already allocated
				if (!td->vars [var].global) {
					if (td->verbose_level)
						g_print ("alloc ldloca global var %d to offset %d\n", var, td->total_locals_size);
					interp_alloc_global_var_offset (td, var);
					td->vars [var].global = TRUE;
				}
			}
			interp_foreach_ins_var (td, ins, (gpointer)(gsize)bb->index, initialize_global_var_cb);
		}
	}
	td->total_locals_size = ALIGN_TO (td->total_locals_size, MINT_STACK_ALIGNMENT);
}

// Data structure used for offset allocation of call args
typedef struct {
	InterpInst **active_calls;
	int active_calls_count;
	int active_calls_capacity;
	// A deferred call stack implemented as a linked list
	GSList *deferred_calls;
} ActiveCalls;

static void
init_active_calls (TransformData *td, ActiveCalls *ac)
{
	ac->active_calls_count = 0;
	ac->active_calls_capacity = 5;
	ac->active_calls = (InterpInst**)mono_mempool_alloc (td->mempool, ac->active_calls_capacity * sizeof (InterpInst*));
	ac->deferred_calls = NULL;
}

static void
reinit_active_calls (TransformData *td, ActiveCalls *ac)
{
	ac->active_calls_count = 0;
	ac->deferred_calls = NULL;
}

static void
add_active_call (TransformData *td, ActiveCalls *ac, InterpInst *call)
{
	// Check if already added
	if (call->flags & INTERP_INST_FLAG_ACTIVE_CALL)
		return;

	if (ac->active_calls_count == ac->active_calls_capacity) {
		InterpInst **old = ac->active_calls;
		ac->active_calls_capacity *= 2;
		ac->active_calls = (InterpInst**)mono_mempool_alloc (td->mempool, ac->active_calls_capacity * sizeof (InterpInst*));
		memcpy (ac->active_calls, old, ac->active_calls_count * sizeof (InterpInst*));
	}
	ac->active_calls [ac->active_calls_count] = call;
	ac->active_calls_count++;

	// Mark a flag on it so we don't have to lookup the array with every argument store.
	call->flags |= INTERP_INST_FLAG_ACTIVE_CALL;
}

/**
 * Function allocates offsets of resolved calls following a constraint
 * where the base offset of a call must be greater than the offset of any argument of other active call args.
 *
 * Function first removes the call from an array of active calls. If a match is found,
 * the call is removed from the array by moving the last entry into its place. Otherwise, it is a call without arguments.
 *
 * If there are active calls, the call in question is push onto the stack as a deferred call.
 * The call contains a list of other active calls on which it depends. Those calls need to be resolved first in order to determine optimal base offset for the call in question.
 * Otherwise, if there are no active calls, function starts resolving the call in question and deferred calls from the stack.
 *
 * For each call, function computes the base offset, the offset of each call argument starting from a base offset, and stores the computed call offset into a InterpInst.
 * The base offset is computed as max offset of all call offsets on which the call depends.
 * Stack ensures that all call offsets on which the call depends are calculated before the call in question, by deferring calls from the last to the first one.
 */
static void
end_active_call (TransformData *td, ActiveCalls *ac, InterpInst *call)
{
	// Remove call from array
	for (int i = 0; i < ac->active_calls_count; i++) {
		if (ac->active_calls [i] == call) {
			ac->active_calls_count--;
			// Since this entry is removed, move the last entry into it
			if (ac->active_calls_count > 0 && i < ac->active_calls_count)
				ac->active_calls [i] = ac->active_calls [ac->active_calls_count];
			break;
		}
	}

	// Push active call that should be resolved onto the stack
	call->info.call_info->call_deps = NULL;
	if (ac->active_calls_count) {
		for (int i = 0; i < ac->active_calls_count; i++)
			call->info.call_info->call_deps = g_slist_prepend_mempool (td->mempool, call->info.call_info->call_deps, ac->active_calls [i]);
		ac->deferred_calls = g_slist_prepend_mempool (td->mempool, ac->deferred_calls, call);
	} else {
		// If no other active calls, current active call and all deferred calls can be resolved from the stack
		InterpInst *deferred_call = call;
		while (deferred_call) {
			// `base_offset` is a relative offset (to the start of the call args stack) where the args for this call reside.
			// The deps for a call represent the list of active calls at the moment when the call ends. This means that all deps for a call end after the call in question.
			// Given we iterate over the list of deferred calls from the last to the first one to end, all deps of a call are guaranteed to have been processed at this point.
			int base_offset = 0;
			for (GSList *list = deferred_call->info.call_info->call_deps; list; list = list->next) {
				int end_offset = ((InterpInst*)list->data)->info.call_info->call_end_offset;
				if (end_offset > base_offset)
					base_offset = end_offset;
			}
			deferred_call->info.call_info->call_offset = base_offset;
			// Compute to offset of each call argument
			int *call_args = deferred_call->info.call_info->call_args;
			if (call_args && (*call_args != -1)) {
				int var = *call_args;
				while (var != -1) {
					alloc_var_offset (td, var, &base_offset);
					call_args++;
					var = *call_args;
				}
			}
			deferred_call->info.call_info->call_end_offset = ALIGN_TO (base_offset, MINT_STACK_ALIGNMENT);

			if (ac->deferred_calls) {
				deferred_call = (InterpInst*) ac->deferred_calls->data;
				ac->deferred_calls = ac->deferred_calls->next;
			} else
				deferred_call = NULL;
		}
	}
}

// Data structure used for offset allocation of local vars
typedef struct {
	int var;
	gboolean is_alive;
} ActiveVar;

typedef struct {
	ActiveVar *active_vars;
	int active_vars_count;
	int active_vars_capacity;
} ActiveVars;

static void
init_active_vars (TransformData *td, ActiveVars *av)
{
	av->active_vars_count = 0;
	av->active_vars_capacity = MAX (td->vars_size / td->bb_count, 10);
	av->active_vars = (ActiveVar*)mono_mempool_alloc (td->mempool, av->active_vars_capacity * sizeof (ActiveVars));
}

static void
reinit_active_vars (TransformData *td, ActiveVars *av)
{
	av->active_vars_count = 0;
}

static void
add_active_var (TransformData *td, ActiveVars *av, int var)
{
	if (av->active_vars_count == av->active_vars_capacity) {
		av->active_vars_capacity *= 2;
		ActiveVar *new_array = (ActiveVar*)mono_mempool_alloc (td->mempool, av->active_vars_capacity * sizeof (ActiveVar));
		memcpy (new_array, av->active_vars, av->active_vars_count * sizeof (ActiveVar));
		av->active_vars = new_array;
	}
	av->active_vars [av->active_vars_count].var = var;
	av->active_vars [av->active_vars_count].is_alive = TRUE;
	av->active_vars_count++;
}

static void
end_active_var (TransformData *td, ActiveVars *av, int var)
{
	// Iterate over active vars, set the entry associated with var as !is_alive
	for (int i = 0; i < av->active_vars_count; i++) {
		if (av->active_vars [i].var == var) {
			av->active_vars [i].is_alive = FALSE;
			return;
		}
	}
}

static void
compact_active_vars (TransformData *td, ActiveVars *av, gint32 *current_offset)
{
	if (!av->active_vars_count)
		return;
	int i = av->active_vars_count - 1;
	while (i >= 0 && !av->active_vars [i].is_alive) {
		av->active_vars_count--;
		*current_offset = td->vars [av->active_vars [i].var].offset;
		i--;
	}
}

static void
dump_active_vars (TransformData *td, ActiveVars *av)
{
	if (td->verbose_level) {
		g_print ("active :");
		for (int i = 0; i < av->active_vars_count; i++) {
			if (av->active_vars [i].is_alive)
				g_print (" %d (end %d),", av->active_vars [i].var, td->vars [av->active_vars [i].var].live_end);
		}
		g_print ("\n");
	}
}

void
interp_alloc_offsets (TransformData *td)
{
	InterpBasicBlock *bb;
	ActiveCalls ac;
	ActiveVars av;

	if (td->verbose_level)
		g_print ("\nvar offset allocator iteration\n");

	initialize_global_vars (td);

	init_active_vars (td, &av);
	init_active_calls (td, &ac);

	int final_total_locals_size = td->total_locals_size;
	// We now have the top of stack offset. All local regs are allocated after this offset, with each basic block
	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins;
		int ins_index = 0;
		if (td->verbose_level)
			g_print ("BB%d\n", bb->index);

		reinit_active_calls (td, &ac);
		reinit_active_vars (td, &av);

		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			if (ins->opcode == MINT_NOP)
				continue;
			if (ins->opcode == MINT_NEWOBJ || ins->opcode == MINT_NEWOBJ_VT ||
					ins->opcode == MINT_NEWOBJ_SLOW || ins->opcode == MINT_NEWOBJ_STRING) {
				// The offset allocator assumes that the liveness of destination var starts
				// after the source vars, which means the destination var can be allocated
				// at the same offset as some of the arguments. However, for newobj opcodes,
				// the created object is set before the call is made. We solve this by making
				// sure that the dreg is not allocated in the param area, so there is no
				// risk of conflicts.
				td->vars [ins->dreg].no_call_args = TRUE;
			}
			if (ins->flags & INTERP_INST_FLAG_CALL) {
				if (ins->info.call_info && ins->info.call_info->call_args) {
					int *call_args = ins->info.call_info->call_args;
					guint16 pair_sregs [MINT_MOV_PAIRS_MAX];
					guint16 pair_dregs [MINT_MOV_PAIRS_MAX];
					int num_pairs = 0;
					int var = *call_args;

					while (var != -1) {
						if (td->vars [var].global ||
								!td->var_values || td->var_values [var].ref_count > 1 ||
								td->vars [var].no_call_args) {
							// Some vars can't be allocated on the call args stack, since the constraint is that
							// call args vars die after the call. This isn't necessarily true for global vars or
							// vars that are used by other instructions aside from the call.
							// We need to copy the var into a new tmp var
							int new_var = interp_create_var (td, td->vars [var].type);
							td->vars [new_var].call = ins;
							td->vars [new_var].call_args = TRUE;

							int mt = mono_mint_type (td->vars [var].type);
							if (mt != MINT_TYPE_VT && num_pairs < MINT_MOV_PAIRS_MAX && var <= G_MAXUINT16 && new_var <= G_MAXUINT16) {
								// We store these in the instruction data slots so we do this optimizations only if they fit
								pair_sregs [num_pairs] = (guint16)var;
								pair_dregs [num_pairs] = (guint16)new_var;
								num_pairs++;
								// The arg of the call is no longer global
								*call_args = new_var;
							} else {
								int opcode = interp_get_mov_for_type (mt, FALSE);
								InterpInst *new_inst = interp_insert_ins_bb (td, bb, ins->prev, opcode);
								interp_ins_set_dreg (new_inst, new_var);
								interp_ins_set_sreg (new_inst, var);
								if (opcode == MINT_MOV_VT)
									new_inst->data [0] = GINT_TO_UINT16 (td->vars [var].size);
								// The arg of the call is no longer global
								*call_args = new_var;
								// Also update liveness for this instruction
								interp_foreach_ins_var (td, new_inst, (gpointer)(gsize)ins_index, set_var_live_range_cb);
								ins_index++;
							}
						} else {
							// Flag this var as it has special storage on the call args stack
							td->vars [var].call = ins;
							td->vars [var].call_args = TRUE;
						}
						call_args++;
						var = *call_args;
					}
					if (num_pairs > 0) {
						int i;
						for (i = 0; i < num_pairs; i++) {
							set_var_live_range (td, pair_sregs [i], ins_index);
							set_var_live_range (td, pair_dregs [i], ins_index);
						}
						if (num_pairs == 1) {
							int mt = mono_mint_type (td->vars [pair_sregs [0]].type);
							int opcode = interp_get_mov_for_type (mt, FALSE);
							InterpInst *new_inst = interp_insert_ins_bb (td, bb, ins->prev, opcode);
							interp_ins_set_dreg (new_inst, pair_dregs [0]);
							interp_ins_set_sreg (new_inst, pair_sregs [0]);
						} else {
							// Squash together multiple moves to the param area into a single opcode
							int opcode = MINT_MOV_8_2 + num_pairs - 2;
							InterpInst *new_inst = interp_insert_ins_bb (td, bb, ins->prev, opcode);
							int k = 0;
							for (i = 0; i < num_pairs; i++) {
								new_inst->data [k++] = pair_dregs [i];
								new_inst->data [k++] = pair_sregs [i];
							}
						}
						ins_index++;
					}
				}
			}
			// Set live_start and live_end for every referenced local that is not global
			interp_foreach_ins_var (td, ins, (gpointer)(gsize)ins_index, set_var_live_range_cb);
			ins_index++;
		}
		gint32 current_offset = td->total_locals_size;

		ins_index = 0;
		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			int opcode = ins->opcode;
			gboolean is_call = ins->flags & INTERP_INST_FLAG_CALL;

			if (opcode == MINT_NOP)
				continue;

			if (td->verbose_level) {
				g_print ("\tins_index %d\t", ins_index);
                                interp_dump_ins (ins, td->data_items);
			}

			// Expire source vars. We first mark them as not alive and then compact the array
			for (int i = 0; i < mono_interp_op_sregs [opcode]; i++) {
				int var = ins->sregs [i];
				if (var == MINT_CALL_ARGS_SREG)
					continue;
				if (!td->vars [var].global && td->vars [var].live_end == ins_index) {
					g_assert (!td->vars [var].call_args);
					end_active_var (td, &av, var);
				}
			}
			if (opcode >= MINT_MOV_8_2 && opcode <= MINT_MOV_8_4) {
				// These opcodes have multiple dvars, which overcomplicate things, so they are
				// marked as having no svars/dvars, for now. Special case it.
				int num_pairs = 2 + opcode - MINT_MOV_8_2;
				for (int i = 0; i < num_pairs; i++) {
					int var = ins->data [2 * i + 1];
					if (!td->vars [var].global && td->vars [var].live_end == ins_index)
						end_active_var (td, &av, var);
				}
			}

			if (is_call)
				end_active_call (td, &ac, ins);

			compact_active_vars (td, &av, &current_offset);

			// Alloc dreg local starting at the stack_offset
			if (mono_interp_op_dregs [opcode]) {
				int var = ins->dreg;

				if (td->vars [var].call_args) {
					add_active_call (td, &ac, td->vars [var].call);
				} else if (!td->vars [var].global && td->vars [var].offset == -1) {
					alloc_var_offset (td, var, &current_offset);
					if (current_offset > final_total_locals_size)
						final_total_locals_size = current_offset;

					if (td->verbose_level)
						g_print ("alloc var %d to offset %d\n", var, td->vars [var].offset);

					if (td->vars [var].live_end > ins_index) {
						// if dreg is still used in the basic block, add it to the active list
						add_active_var (td, &av, var);
					} else {
						current_offset = td->vars [var].offset;
					}
				}
			}
			if (td->verbose_level)
				dump_active_vars (td, &av);
			ins_index++;
		}
	}
	final_total_locals_size = ALIGN_TO (final_total_locals_size, MINT_STACK_ALIGNMENT);

	// Iterate over all call args locals, update their final offset (aka add td->total_locals_size to them)
	// then also update td->total_locals_size to account for this space.
	td->param_area_offset = final_total_locals_size;
	for (unsigned int i = 0; i < td->vars_size; i++) {
		// These are allocated separately at the end of the stack
		if (td->vars [i].call_args) {
			td->vars [i].offset += td->param_area_offset;
			final_total_locals_size = MAX (td->vars [i].offset + td->vars [i].size, final_total_locals_size);
		}
	}
	td->total_locals_size = ALIGN_TO (final_total_locals_size, MINT_STACK_ALIGNMENT);
}

/*
 * DOMINANCE COMPUTATION
 */

static GString*
interp_get_bb_links (InterpBasicBlock *bb)
{
	GString *str = g_string_new ("");

	if (bb->in_count) {
		g_string_append_printf (str, "IN (%d", bb->in_bb [0]->index);
		for (int i = 1; i < bb->in_count; i++)
			g_string_append_printf (str, " %d", bb->in_bb [i]->index);
		g_string_append_printf (str, "), ");
	} else {
		g_string_append_printf (str, "IN (nil), ");
	}

	if (bb->out_count) {
		g_string_append_printf (str, "OUT (%d", bb->out_bb [0]->index);
		for (int i = 1; i < bb->out_count; i++)
			g_string_append_printf (str, " %d", bb->out_bb [i]->index);
		g_string_append_printf (str, ")");
	} else {
		g_string_append_printf (str, "OUT (nil)");
	}

	return str;
}

static void
dfs_visit (InterpBasicBlock *bb, int *pos, InterpBasicBlock **bb_array)
{
	int dfs_index = *pos;

	bb_array [dfs_index] = bb;
	bb->dfs_index = dfs_index;
	*pos = dfs_index + 1;
	for (int i = 0; i < bb->out_count; i++) {
		InterpBasicBlock *out_bb = bb->out_bb [i];
		if (out_bb->dfs_index == -1)
			dfs_visit (out_bb, pos, bb_array);
	}
}

static void
interp_compute_dfs_indexes (TransformData *td)
{
	int dfs_index = 0;
	// Sort bblocks in reverse postorder
	td->bblocks = (InterpBasicBlock**)mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock*) * td->bb_count);
	g_assert (!td->entry_bb->in_count);
	dfs_visit (td->entry_bb, &dfs_index, td->bblocks);
	td->bblocks_count = dfs_index;

	// Visit also bblocks reachable from eh handlers. These bblocks are not linked
	// to the main cfg (where we do dominator computation, ssa transformation etc)
	if (td->header->num_clauses > 0) {
		InterpBasicBlock *current = td->entry_bb;
		while (current != NULL) {
			if (current->reachable && current->dfs_index == -1) {
				current->dfs_index = dfs_index;
				td->bblocks [dfs_index] = current;
				dfs_index++;
			}
			current = current->next_bb;
		}
	}
	td->bblocks_count_eh = dfs_index;

	if (td->verbose_level) {
		InterpBasicBlock *bb;
		g_print ("\nBASIC BLOCK GRAPH:\n");
		for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
			GString* bb_info = interp_get_bb_links (bb);
			g_print ("BB%d: DFS%s(%d), %s\n", bb->index, (bb->dfs_index >= td->bblocks_count) ? "_EH" : "" , bb->dfs_index, bb_info->str);
			g_string_free (bb_info, TRUE);
		}
	}
}

static InterpBasicBlock*
dom_intersect (InterpBasicBlock **idoms, InterpBasicBlock *bb1, InterpBasicBlock *bb2)
{
	while (bb1 != bb2) {
		while (bb1->dfs_index < bb2->dfs_index)
			bb2 = idoms [bb2->dfs_index];
		while (bb2->dfs_index < bb1->dfs_index)
			bb1 = idoms [bb1->dfs_index];
	}
	return bb1;
}

static gboolean
is_bblock_ssa_cfg (TransformData *td, InterpBasicBlock *bb)
{
	// bblocks with uninitialized dfs_index are unreachable
	if (bb->dfs_index == -1)
		return FALSE;
	if (bb->dfs_index < td->bblocks_count)
		return TRUE;
	return FALSE;
}

static void
interp_compute_dominators (TransformData *td)
{
	InterpBasicBlock **idoms = (InterpBasicBlock**)mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock*) * td->bb_count);

	idoms [0] = td->entry_bb;
	gboolean changed = TRUE;
	while (changed) {
		changed = FALSE;
		// all bblocks in reverse post order except entry
		for (int i = 1; i < td->bblocks_count; i++) {
			InterpBasicBlock *bb = td->bblocks [i];
			InterpBasicBlock *new_idom = NULL;
			// pick candidate idom from first processed predecessor of it
			int j;
			for (j = 0; j < bb->in_count; j++) {
                                InterpBasicBlock *in_bb = bb->in_bb [j];
                                if (is_bblock_ssa_cfg (td, in_bb) && idoms [in_bb->dfs_index]) {
                                        new_idom = in_bb;
                                        break;
                                }
                        }

			// intersect new_idom with dominators from the other predecessors
			for (; j < bb->in_count; j++) {
				InterpBasicBlock *in_bb = bb->in_bb [j];
				if (is_bblock_ssa_cfg (td, in_bb) && idoms [in_bb->dfs_index])
					new_idom = dom_intersect (idoms, in_bb, new_idom);
			}

			// check if we obtained new idom
			if (idoms [i] != new_idom) {
				idoms [i] = new_idom;
				changed = TRUE;
			}
		}
	}

	td->idoms = idoms;

	// Build `dominated` bblock list for each bblock
	for (int i = 1; i < td->bblocks_count; i++) {
		InterpBasicBlock *bb = td->bblocks [i];
		InterpBasicBlock *idom = td->idoms [i];
		if (idom)
			idom->dominated = g_slist_prepend (idom->dominated, bb);
	}

	if (td->verbose_level) {
		InterpBasicBlock *bb;
		g_print ("\nBASIC BLOCK IDOMS:\n");
		for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
			if (!is_bblock_ssa_cfg (td, bb))
				continue;
			g_print ("IDOM (BB%d) = BB%d\n", bb->index, td->idoms [bb->dfs_index]->index);
		}

		g_print ("\nBASIC BLOCK DOMINATED:\n");
		for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
			if (!is_bblock_ssa_cfg (td, bb))
				continue;
			if (bb->dominated) {
				g_print ("DOMINATED (BB%d)  = {", bb->index);
				GSList *dominated = bb->dominated;
				while (dominated) {
					InterpBasicBlock *dominated_bb = (InterpBasicBlock*)dominated->data;
					g_print (" BB%d", dominated_bb->index);
					dominated = dominated->next;
				}
				g_print (" }\n");
			}
		}
	}
}

static void
interp_compute_dominance_frontier (TransformData *td)
{
	int bitsize = mono_bitset_alloc_size (td->bb_count, 0);
	char *mem = (char *)mono_mempool_alloc0 (td->mempool, bitsize * td->bb_count);

	for (int i = 0; i < td->bblocks_count; i++) {
		td->bblocks [i]->dfrontier = mono_bitset_mem_new (mem, td->bb_count, 0);
		mem += bitsize;
	}

	for (int i = 0; i < td->bblocks_count; i++) {
		InterpBasicBlock *bb = td->bblocks [i];

		if (bb->in_count > 1) {
			for (int j = 0; j < bb->in_count; ++j) {
				InterpBasicBlock *p = bb->in_bb [j];
				if (!is_bblock_ssa_cfg (td, p))
					continue;

				g_assert (p->dfs_index || p == td->entry_bb);

				while (p != td->idoms [bb->dfs_index]) {
					g_assert (bb->dfs_index < td->bblocks_count);
					mono_bitset_set_fast (p->dfrontier, bb->dfs_index);
					p = td->idoms [p->dfs_index];
				}
			}
		}
	}

	if (td->verbose_level) {
		InterpBasicBlock *bb;
		g_print ("\nBASIC BLOCK DFRONTIERS:\n");
		for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
			if (!is_bblock_ssa_cfg (td, bb))
				continue;
			g_print ("DFRONTIER (BB%d) = {", bb->index);
			int i;
			mono_bitset_foreach_bit (bb->dfrontier, i, td->bb_count) {
				g_print (" BB%d", td->bblocks [i]->index);
			}
			g_print (" }\n");
		}
	}
}

static void
interp_compute_dominance (TransformData *td)
{
	/*
	 * A dominator for a bblock n, is a bblock that is reached on every path to n. Dominance is transitive.
	 * An immediate dominator for a bblock n, is the bblock that dominates n, but doesn't dominate any other
	 * dominators of n, meaning it is the closest dominator to n. The dominance frontier of a node V is the set
	 * of nodes where the dominance stops. This means that it is the set of nodes where node V doesn't dominate
	 * it, but it does dominate a predecessor of it (including if the predecessor is V itself).
	 *
	 * The dominance frontier is relevant for SSA computation since, for a var defined in a bblock, the DF of bblock
	 * represents the set of bblocks where we need to add a PHI opcode for that variable.
	 */
	interp_compute_dfs_indexes (td);

	interp_compute_dominators (td);

	interp_compute_dominance_frontier (td);
}

/*
 * SSA TRANSFORMATION
 */
static void
compute_eh_var_cb (TransformData *td, int *pvar, gpointer data)
{
	int var = *pvar;
	td->vars [var].eh_var = TRUE;
}

static void
interp_compute_eh_vars (TransformData *td)
{
	// FIXME we can now remove EH bblocks. This means some vars can stop being EH vars

	// EH bblocks are stored separately and are not reachable from the non-EF control flow
	// path. Any var reachable from EH bblocks will not be in SSA form.
	for (int i = td->bblocks_count; i < td->bblocks_count_eh; i++) {
		InterpBasicBlock *bb = td->bblocks [i];
		for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
			if (ins->opcode == MINT_LDLOCA_S)
				td->vars [ins->sregs [0]].eh_var = TRUE;
			interp_foreach_ins_var (td, ins, bb, compute_eh_var_cb);
		}
	}

	// If we have a try block that might catch exceptions, then we can't do any propagation
	// of the values defined in the block since an exception could interrupt the normal control
	// flow. All vars defined in this block will not be in SSA form.
	for (unsigned int i = 0; i < td->header->num_clauses; i++) {
		MonoExceptionClause *c = &td->header->clauses [i];
		if (c->flags == MONO_EXCEPTION_CLAUSE_NONE ||
				c->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			InterpBasicBlock *bb = td->offset_to_bb [c->try_offset];
			int try_end = c->try_offset + c->try_len;
			g_assert (bb);
			while (bb->il_offset != -1 && bb->il_offset < try_end) {
				for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
					if (mono_interp_op_dregs [ins->opcode])
						td->vars [ins->dreg].eh_var = TRUE;
				}
				bb = bb->next_bb;
			}
		}
	}

	td->eh_vars_computed = TRUE;
}

static void
interp_compute_ssa_vars (TransformData *td)
{
	if (!td->eh_vars_computed)
		interp_compute_eh_vars (td);

	for (unsigned int i = 0; i < td->vars_size; i++) {
		if (td->vars [i].indirects > 0) {
			td->vars [i].no_ssa = TRUE;
			td->vars [i].has_indirects = TRUE;
		} else {
			td->vars [i].has_indirects = FALSE;
			if (td->vars [i].eh_var)
				td->vars [i].no_ssa = TRUE;
			else
				td->vars [i].no_ssa = FALSE;
		}
	}
}

static gboolean
var_is_ssa_form (TransformData *td, int var)
{
	if (td->vars [var].no_ssa)
		return FALSE;

	return TRUE;
}

static gboolean
var_has_indirects (TransformData *td, int var)
{
	if (td->vars [var].has_indirects)
		return TRUE;

	return FALSE;
}

static InterpVarValue*
get_var_value (TransformData *td, int var)
{
	if (var_is_ssa_form (td, var))
		return &td->var_values [var];

	if (var_has_indirects (td, var))
		return NULL;

	// No ssa var, check if we have a def set for the current bblock
	if (td->var_values [var].def) {
		if (td->var_values [var].liveness.bb_index == td->cbb->index)
			return &td->var_values [var];
	}
	return NULL;

}

static InterpInst*
get_var_value_def (TransformData *td, int var)
{
	InterpVarValue *val = get_var_value (td, var);
	if (val)
		return val->def;
	return NULL;
}

static int
get_var_value_type (TransformData *td, int var)
{
	InterpVarValue *val = get_var_value (td, var);
	if (val)
		return val->type;
	return VAR_VALUE_NONE;
}

static void
compute_global_var_cb (TransformData *td, int *pvar, gpointer data)
{
	int var = *pvar;
	InterpBasicBlock *bb = (InterpBasicBlock*)data;
	InterpVar *var_data = &td->vars [var];
	if (!var_is_ssa_form (td, var) || td->vars [var].ext_index == -1)
		return;
	// If var is used in another block than the one that it is declared then mark it as global
	if (var_data->declare_bbs && var_data->declare_bbs->data != bb) {
		int ext_index = td->vars [var].ext_index;
		td->renamable_vars [ext_index].ssa_global = TRUE;
	}
}

// We obtain the list of global vars, as well as the list of bblocks where each one of the global vars is declared.
static void
interp_compute_global_vars (TransformData *td)
{
	InterpBasicBlock *bb;
	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		if (!is_bblock_ssa_cfg (td, bb))
			continue;
		InterpInst *ins;
		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			if (mono_interp_op_dregs [ins->opcode] && var_is_ssa_form (td, ins->dreg)) {
				// Save the list of bblocks where a global var is defined in
				InterpVar *var_data = &td->vars [ins->dreg];
				if (!var_data->declare_bbs) {
					var_data->declare_bbs = g_slist_prepend (NULL, bb);
				} else {
					int ext_index = interp_create_renamable_var (td, ins->dreg);
					if (!g_slist_find (var_data->declare_bbs, bb)) {
						// Var defined in multiple bblocks, it is ssa global
						var_data->declare_bbs = g_slist_prepend (var_data->declare_bbs, bb);
						td->renamable_vars [ext_index].ssa_global = TRUE;
					}
				}
			}
		}
	}

	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		if (!is_bblock_ssa_cfg (td, bb))
			continue;
		InterpInst *ins;
		for (ins = bb->first_ins; ins != NULL; ins = ins->next)
			interp_foreach_ins_svar (td, ins, bb, compute_global_var_cb);
	}

	if (td->verbose_level) {
		g_print ("\nSSA GLOBALS:\n");
		for (unsigned int i = 0; i < td->renamable_vars_size; i++) {
			if (td->renamable_vars [i].ssa_global) {
				int var = td->renamable_vars [i].var_index;
				g_print ("DECLARE_BB (%d) = {", var);
				GSList *l = td->vars [var].declare_bbs;
				while (l) {
					g_print (" BB%d", ((InterpBasicBlock*)l->data)->index);
					l = l->next;
				}
				g_print (" }\n");
			}
		}
	}
}

static void
compute_gen_set_cb (TransformData *td, int *pvar, gpointer data)
{
	int var = *pvar;
	InterpBasicBlock *bb = (InterpBasicBlock*)data;

	int ext_index = td->vars [var].ext_index;
	if (ext_index == -1)
		return;

	if (!td->renamable_vars [ext_index].ssa_global)
		return;

	if (!mono_bitset_test_fast (bb->kill_set, ext_index))
		mono_bitset_set_fast (bb->gen_set, ext_index);
}

// For each bblock, computes the kill set (the set of vars defined by the bblock)
// and gen set (the set of vars used by the bblock, with the definition not being
// in the bblock).
static void
compute_gen_kill_sets (TransformData *td)
{
	int bitsize = mono_bitset_alloc_size (td->renamable_vars_size, 0);
	char *mem = (char *)mono_mempool_alloc0 (td->mempool, bitsize * td->bb_count * 4);

	for (int i = 0; i < td->bblocks_count; i++) {
		InterpBasicBlock *bb = td->bblocks [i];

		bb->gen_set = mono_bitset_mem_new (mem, td->renamable_vars_size, 0);
		mem += bitsize;
		bb->kill_set = mono_bitset_mem_new (mem, td->renamable_vars_size, 0);
		mem += bitsize;
		bb->live_in_set = mono_bitset_mem_new (mem, td->renamable_vars_size, 0);
		mem += bitsize;
		bb->live_out_set = mono_bitset_mem_new (mem, td->renamable_vars_size, 0);
		mem += bitsize;

		for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
			interp_foreach_ins_svar (td, ins, bb, compute_gen_set_cb);
			if (mono_interp_op_dregs [ins->opcode]) {
				int ext_index = td->vars [ins->dreg].ext_index;
				if (ext_index != -1 && td->renamable_vars [ext_index].ssa_global)
					mono_bitset_set_fast (bb->kill_set, ext_index);
			}
		}
	}
}

// Compute live_in and live_out sets
// For a bblock, live_in contains all vars that are live at exit of bblock and not redefined,
// together with all vars used in the bblock without being defined. For a bblock, live_out set
// contains all vars that are live_in any successor. This computation starts with empty sets
// (starting to generate live vars from the gen sets) and it is run iteratively until the
// computation converges.
static void
recompute_live_out (TransformData *td, InterpBasicBlock *bb)
{
	for (int i = 0; i < bb->out_count; i++) {
		InterpBasicBlock *sbb = bb->out_bb [i];

		// Recompute live_in_set for each successor of bb
		mono_bitset_copyto_fast (sbb->live_out_set, sbb->live_in_set);
		mono_bitset_sub_fast (sbb->live_in_set, sbb->kill_set);
		mono_bitset_union_fast (sbb->live_in_set, sbb->gen_set);

		// Recompute live_out_set of bb, by adding the live_in_set of each successor
		mono_bitset_union_fast (bb->live_out_set, sbb->live_in_set);
	}
}

// For each bblock, compute LiveIn, LiveOut sets tracking liveness for the previously computed global vars
static void
interp_compute_pruned_ssa_liveness (TransformData *td)
{
	compute_gen_kill_sets (td);

	gboolean changed = TRUE;
	while (changed) {
		changed = FALSE;
		for (int i = 0; i < td->bblocks_count; i++) {
			InterpBasicBlock *bb = td->bblocks [i];
			guint32 prev_count = mono_bitset_count (bb->live_out_set);
			recompute_live_out (td, bb);
			if (prev_count != mono_bitset_count (bb->live_out_set))
				changed = TRUE;
		}
	}

	if (td->verbose_level) {
		InterpBasicBlock *bb;
		g_print ("\nBASIC BLOCK LIVENESS:\n");
		for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
			unsigned int i;
			if (!is_bblock_ssa_cfg (td, bb))
				continue;
			g_print ("BB%d\n\tLIVE_IN = {", bb->index);
			mono_bitset_foreach_bit (bb->live_in_set, i, td->renamable_vars_size) {
				g_print (" %d", td->renamable_vars [i].var_index);
			}
			g_print (" }\n\tLIVE_OUT = {", bb->index);
			mono_bitset_foreach_bit (bb->live_out_set, i, td->renamable_vars_size) {
				g_print (" %d", td->renamable_vars [i].var_index);
			}
			g_print (" }\n");
		}
	}
}

static gboolean
bb_has_phi (TransformData *td, InterpBasicBlock *bb, int var)
{
	InterpInst *ins = bb->first_ins;
	while (ins) {
		if (ins->opcode == MINT_PHI) {
			if (ins->dreg == var)
				return TRUE;
		} else if (ins->opcode == MINT_DEAD_PHI) {
			MonoBitSet *bitset = ins->info.dead_phi_vars;
			int ext_index = td->vars [var].ext_index;
			if (mono_bitset_test_fast (bitset, ext_index))
				return TRUE;
		} else {
			// if we have a phi it is at the start of the bb
			return FALSE;
		}
		ins = ins->next;
	}
	return FALSE;
}

static void
bb_insert_phi (TransformData *td, InterpBasicBlock *bb, int var)
{
	InterpInst *first_ins = NULL;
	// We keep dead phi as first instruction so we can find it quickly
	if (bb->first_ins && bb->first_ins->opcode == MINT_DEAD_PHI)
		first_ins = bb->first_ins;
	InterpInst *phi = interp_insert_ins_bb (td, bb, first_ins, MINT_PHI);
	if (td->verbose_level)
		g_print ("BB%d NEW_PHI %d\n", bb->index, var);

	phi->dreg = var;
	phi->info.args = (int*)mono_mempool_alloc (td->mempool, (bb->in_count + 1) * sizeof (int));
	int i;
	for (i = 0; i < bb->in_count; i++)
		phi->info.args [i] = var;
	phi->info.args [i] = -1;
}

static void
bb_insert_dead_phi (TransformData *td, InterpBasicBlock *bb, int var)
{
	MonoBitSet *bitset;
	if (bb->first_ins && bb->first_ins->opcode == MINT_DEAD_PHI) {
		bitset = bb->first_ins->info.dead_phi_vars;
	} else {
		InterpInst *phi = interp_insert_ins_bb (td, bb, NULL, MINT_DEAD_PHI);
		gpointer mem = mono_mempool_alloc0 (td->mempool, mono_bitset_alloc_size (td->renamable_vars_size, 0));
		phi->info.dead_phi_vars = bitset = mono_bitset_mem_new (mem, td->renamable_vars_size, 0);
	}
	int ext_index = td->vars [var].ext_index;
	mono_bitset_set_fast (bitset, ext_index);
	if (td->verbose_level)
		g_print ("BB%d NEW_DEAD_PHI %d\n", bb->index, var);

}

static void
insert_phi_nodes (TransformData *td)
{
	if (td->verbose_level)
		g_print ("\nINSERT PHI NODES:\n");
	for (unsigned int i = 0; i < td->renamable_vars_size; i++) {
		if (!td->renamable_vars [i].ssa_global)
			continue;

		// For every definition of this var, we add a phi node at the start of
		// all bblocks in the dominance frontier of the defining bblock.
		int var = td->renamable_vars [i].var_index;
		GSList *workset = g_slist_copy (td->vars [var].declare_bbs);
		while (workset) {
			GSList *old_head = workset;
			InterpBasicBlock *bb = (InterpBasicBlock*)workset->data;
			workset = workset->next;
			g_free (old_head);
			g_assert (is_bblock_ssa_cfg (td, bb));
			int j;
			mono_bitset_foreach_bit (bb->dfrontier, j, td->bb_count) {
				InterpBasicBlock *bd = td->bblocks [j];
				g_assert (is_bblock_ssa_cfg (td, bb));
				if (!bb_has_phi (td, bd, var)) {
					if (mono_bitset_test_fast (bd->live_in_set, i)) {
						td->renamable_vars [i].ssa_fixed = TRUE;
						bb_insert_phi (td, bd, var);
					} else {
						// We need this only for vars that are ssa fixed, but it is not clear
						// if the current var is fixed or not. We will ignore these opcodes if
						// the var is not actually ssa fixed.
						bb_insert_dead_phi (td, bd, var);
					}
					if (!g_slist_find (workset, bd))
						workset = g_slist_prepend (workset, bd);
				}
			}
		}
	}
}

// Additional fixed vars, in addition to vars that are args to phi nodes
static void
insert_tiering_defs (TransformData *td)
{
	for (int i = 0; i < td->bblocks_count; i++) {
		InterpBasicBlock *bb = td->bblocks [i];
		if (!bb->patchpoint_bb)
			continue;

		// All IL locals live at entry to this bb have to be fixed
		for (unsigned int k = 0; k < td->renamable_vars_size; k++) {
			int var_index = td->renamable_vars [k].var_index;
			if (td->vars [var_index].il_global && mono_bitset_test_fast (bb->live_in_set, k)) {
				td->renamable_vars [k].ssa_fixed = TRUE;

				// Patchpoints introduce some complications since some variables have to be
				// accessed from same offset between unoptimized and optimized methods.
				//
				// Consider the following scenario
				// BB0 -> BB2       BB0: TMP <- def; IL_VAR <- TMP
				// | ^              BB1: Use IL_VAR
				// v |              BB2: Use IL_VAR
				// BB1
				//
				//     BB1 is a basic block containing a patchpoint, BB0 dominates both BB1 and BB2.
				// IL_VAR is used both in BB1 and BB2. In BB1, in optimized code, we could normally
				// replace use of IL_VAR with use of TMP. However, this is incorrect, because TMP
				// can be allocated at a different offset from IL_VAR and, if we enter the method
				// from the patchpoint in BB1, the data at var TMP would not be initialized since
				// we only copy the IL var space.
				//     Even if we prevent the copy propagation in BB1, then tiering is still broken.
				// In BB2 we could replace use of IL_VAR with TMP, and we end up hitting the same problem.
				// Optimized code will attempt to access value of IL_VAR from the offset of TMP_VAR,
				// which is not initialized if we enter from the patchpoint in BB1.
				//     We solve these issues by inserting a MINT_DEF_TIER_VAR in BB1. This instruction
				// prevents cprop of the IL_VAR in the patchpoint bblock since MINT_DEF_TIER_VAR is seen
				// as a redefinition. In addition to that, in BB2 we now have 2 reaching definitions for
				// IL_VAR, the original one from BB0 and the one from patchpoint bblock from BB1. This
				// will force a phi definition in BB2 and we will once again be force to access IL_VAR
				// from the original offset that is equal to the one in unoptimized method.
				InterpInst *def = interp_insert_ins_bb (td, bb, NULL, MINT_DEF_TIER_VAR);
				def->sregs [0] = var_index;
				def->dreg = var_index;
				InterpVar *var_data = &td->vars [var_index];
				// Record the new declaration for this var. Phi nodes insertion phase will account for this
				if (!g_slist_find (var_data->declare_bbs, bb))
					var_data->declare_bbs = g_slist_prepend (var_data->declare_bbs, bb);
				if (td->verbose_level) {
					g_print ("insert patchpoint var define in BB%d:\n\t", bb->index);
					interp_dump_ins (def, td->data_items);
				}
			}
		}
	}
}

static int
get_renamed_var (TransformData *td, int var, gboolean def_arg)
{
	int ext_index = td->vars [var].ext_index;
	g_assert (ext_index != -1);
	int renamed_var = interp_create_var (td, td->vars [var].type);
	td->vars [renamed_var].def_arg = def_arg;

	if (td->renamable_vars [ext_index].ssa_fixed) {
		td->vars [renamed_var].renamed_ssa_fixed = TRUE;
		interp_create_renamed_fixed_var (td, renamed_var, var);
	} else {
		// Renamed var reference the orignal var through the ext_index
		td->vars [renamed_var].ext_index = ext_index;
	}
	td->renamable_vars [ext_index].ssa_stack = g_slist_prepend (td->renamable_vars [ext_index].ssa_stack, (gpointer)(gsize)renamed_var);
	return renamed_var;
}

static void
rename_ins_var_cb (TransformData *td, int *pvar, gpointer data)
{
	int var = *pvar;
	int ext_index = td->vars [var].ext_index;
	if (ext_index != -1) {
		int renamed_var = (int)(gsize)td->renamable_vars [ext_index].ssa_stack->data;
		g_assert (renamed_var != -1);
		*pvar = renamed_var;
	}
}

static void
rename_phi_args_in_out_bbs (TransformData *td, InterpBasicBlock *bb)
{
        for (int i = 0; i < bb->out_count; i++) {
                InterpBasicBlock *bb_out = bb->out_bb [i];

		int aindex;
                for (aindex = 0; aindex < bb_out->in_count; aindex++)
                        if (bb_out->in_bb [aindex] == bb)
                                break;

		for (InterpInst *ins = bb_out->first_ins; ins != NULL; ins = ins->next) {
			if (ins->opcode == MINT_PHI) {
				int var = ins->info.args [aindex];
				int ext_index = td->vars [var].ext_index;
				GSList *stack = td->renamable_vars [ext_index].ssa_stack;
				ins->info.args [aindex] = (int)(gsize)stack->data;
			} else if (ins->opcode == MINT_DEAD_PHI) {
				continue;
			} else if (ins->opcode != MINT_NOP) {
				break;
			}
                }
        }
}

static void
rename_vars_in_bb (TransformData *td, InterpBasicBlock *bb)
{
	InterpInst *ins;

	// Rename vars defined with MINT_PHI
	for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
		if (ins->opcode == MINT_PHI) {
			ins->dreg = get_renamed_var (td, ins->dreg, FALSE);
		} else if (ins->opcode == MINT_DEAD_PHI) {
			unsigned int ext_index;
			mono_bitset_foreach_bit (ins->info.dead_phi_vars, ext_index, td->renamable_vars_size) {
				if (td->renamable_vars [ext_index].ssa_fixed) {
					// we push an invalid var that will be just a marker for marking var live limits
					td->renamable_vars [ext_index].ssa_stack = g_slist_prepend (td->renamable_vars [ext_index].ssa_stack, (gpointer)(gsize)-1);
				}
			}
		} else {
			break;
		}
	}

	InterpLivenessPosition current_liveness;
	current_liveness.bb_index = bb->index;
	current_liveness.ins_index = 0;

	// Use renamed definition for sources
	for (; ins != NULL; ins = ins->next) {
		if (interp_ins_is_nop (ins) || ins->opcode == MINT_DEAD_PHI)
			continue;
		ins->flags |= INTERP_INST_FLAG_LIVENESS_MARKER;
		current_liveness.ins_index++;

		interp_foreach_ins_svar (td, ins, NULL, rename_ins_var_cb);
		if (!mono_interp_op_dregs [ins->opcode] || td->vars [ins->dreg].ext_index == -1)
			continue;

		if (ins->opcode == MINT_DEF_ARG) {
			ins->dreg = get_renamed_var (td, ins->dreg, TRUE);
		} else if (mono_interp_op_dregs [ins->opcode]) {
			g_assert (!td->vars [ins->dreg].renamed_ssa_fixed);
			int renamable_ext_index = td->vars [ins->dreg].ext_index;
			if (td->renamable_vars [renamable_ext_index].ssa_fixed &&
					td->renamable_vars [renamable_ext_index].ssa_stack) {
				// Mark the exact liveness end limit for the ssa fixed var that is overwritten (the old entry on the stack)
				int renamed_var = (int)(gsize)td->renamable_vars [renamable_ext_index].ssa_stack->data;
				if (renamed_var != -1) {
					g_assert (td->vars [renamed_var].renamed_ssa_fixed);
					int renamed_var_ext = td->vars [renamed_var].ext_index;
					InterpLivenessPosition *liveness_ptr = (InterpLivenessPosition*)mono_mempool_alloc (td->mempool, sizeof (InterpLivenessPosition));
					*liveness_ptr = current_liveness;
					td->renamed_fixed_vars [renamed_var_ext].live_limit_bblocks = g_slist_prepend (td->renamed_fixed_vars [renamed_var_ext].live_limit_bblocks, liveness_ptr);
				}
			}
			ins->dreg = get_renamed_var (td, ins->dreg, FALSE);
		}
	}

	rename_phi_args_in_out_bbs (td, bb);

	// Rename recursively every successor of bb in the dominator tree
	GSList *dominated = bb->dominated;
	while (dominated) {
		InterpBasicBlock *dominated_bb = (InterpBasicBlock*)dominated->data;
		rename_vars_in_bb (td, dominated_bb);
		dominated = dominated->next;
	}

	// All vars currently on the ssa stack are live until the end of the bblock
	for (unsigned int i = 0; i < td->renamable_vars_size; i++) {
		if (td->renamable_vars [i].ssa_fixed && td->renamable_vars [i].ssa_stack) {
			int renamed_var = (int)(gsize)td->renamable_vars [i].ssa_stack->data;
			if (renamed_var != -1) {
				g_assert (td->vars [renamed_var].renamed_ssa_fixed);
				int renamed_var_ext = td->vars [renamed_var].ext_index;
				if (!td->renamed_fixed_vars [renamed_var_ext].live_out_bblocks) {
					gpointer mem = mono_mempool_alloc0 (td->mempool, mono_bitset_alloc_size (td->bb_count, 0));
					td->renamed_fixed_vars [renamed_var_ext].live_out_bblocks = mono_bitset_mem_new (mem, td->bb_count, 0);
				}

				mono_bitset_set_fast (td->renamed_fixed_vars [renamed_var_ext].live_out_bblocks, bb->index);
			}
		}
	}

	// Pop from the stack any new vars defined in this bblock
	for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
		if (mono_interp_op_dregs [ins->opcode]) {
			int ext_index = td->vars [ins->dreg].ext_index;
			if (ext_index == -1)
				continue;
			if (td->vars [ins->dreg].renamed_ssa_fixed)
				ext_index = td->renamed_fixed_vars [ext_index].renamable_var_ext_index;
			GSList *prev_head = td->renamable_vars [ext_index].ssa_stack;
			td->renamable_vars [ext_index].ssa_stack = prev_head->next;
			g_free (prev_head);
		} else if (ins->opcode == MINT_DEAD_PHI) {
			unsigned int ext_index;
			mono_bitset_foreach_bit (ins->info.dead_phi_vars, ext_index, td->renamable_vars_size) {
				if (td->renamable_vars [ext_index].ssa_fixed) {
					GSList *prev_head = td->renamable_vars [ext_index].ssa_stack;
					td->renamable_vars [ext_index].ssa_stack = prev_head->next;
					g_free (prev_head);
				}
			}
			interp_clear_ins (ins);
		}
	}
}

static void
rename_vars (TransformData *td)
{
	rename_vars_in_bb (td, td->entry_bb);

	if (td->verbose_level) {
		g_print ("\nFIXED SSA VARS LIVENESS LIMIT:\n");
		for (unsigned int i = 0; i < td->renamed_fixed_vars_size; i++) {
			g_print ("FIXED VAR %d\n\tNO LIVE LIMIT BBLOCKS: {", td->renamed_fixed_vars [i].var_index);
			MonoBitSet *live_out_bblocks = td->renamed_fixed_vars [i].live_out_bblocks;
			if (live_out_bblocks) {
				int j;
				mono_bitset_foreach_bit (live_out_bblocks, j, td->bb_count) {
					g_print (" BB%d", j);
				}
			}
			g_print (" }\n");
			g_print ("\tLIVE LIMIT BBLOCKS: {");
			GSList *live_limit_bblocks = td->renamed_fixed_vars [i].live_limit_bblocks;
			while (live_limit_bblocks) {
				InterpLivenessPosition *live_limit = (InterpLivenessPosition*)live_limit_bblocks->data;
				g_print (" (BB%d, %d)", live_limit->bb_index, live_limit->ins_index);
				live_limit_bblocks = live_limit_bblocks->next;
			}
			g_print (" }\n");
		}
	}
}

static void
interp_compute_ssa (TransformData *td)
{
	if (td->verbose_level) {
		g_print ("\nIR before SSA compute:\n");
		mono_interp_print_td_code (td);
	}

	MONO_TIME_TRACK (mono_interp_stats.ssa_compute_dominance_time, interp_compute_dominance (td));

	interp_compute_ssa_vars (td);

	MONO_TIME_TRACK (mono_interp_stats.ssa_compute_global_vars_time, interp_compute_global_vars (td));

	MONO_TIME_TRACK (mono_interp_stats.ssa_compute_pruned_liveness_time, interp_compute_pruned_ssa_liveness (td));

	insert_tiering_defs (td);

	insert_phi_nodes (td);

	MONO_TIME_TRACK (mono_interp_stats.ssa_rename_vars_time, rename_vars (td));

	if (td->verbose_level) {
		g_print ("\nIR after SSA compute:\n");
		mono_interp_print_td_code (td);
	}
}

static void
revert_ssa_rename_cb (TransformData *td, int *pvar, gpointer data)
{
	int var = *pvar;
	int ext_index = td->vars [var].ext_index;
	if (ext_index == -1)
		return;

	int new_var = -1;
	if (td->vars [var].renamed_ssa_fixed) {
		int renamable_var_ext_index = td->renamed_fixed_vars [ext_index].renamable_var_ext_index;
		new_var = td->renamable_vars [renamable_var_ext_index].var_index;
	} else if (td->vars [var].def_arg) {
		new_var = td->renamable_vars [ext_index].var_index;
	}

	if (new_var != -1) {
		*pvar = new_var;
		// Offset allocator checks ref_count to detect single use vars. Keep it updated
		td->var_values [new_var].ref_count += td->var_values [var].ref_count;
	}
}

static void
interp_exit_ssa (TransformData *td)
{
	// Remove all MINT_PHI opcodes and revert ssa renaming
	for (InterpBasicBlock *bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins;
		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			if (ins->opcode == MINT_PHI || ins->opcode == MINT_DEAD_PHI)
				ins->opcode = MINT_NOP;
			else
				interp_foreach_ins_var (td, ins, NULL, revert_ssa_rename_cb);

			ins->flags &= ~INTERP_INST_FLAG_LIVENESS_MARKER;
		}
	}

	// Free memory and restore state
	for (unsigned int i = 0; i < td->vars_size; i++) {
		if (td->vars [i].declare_bbs) {
			g_slist_free (td->vars [i].declare_bbs);
			td->vars [i].declare_bbs = NULL;
		}
		td->vars [i].ext_index = -1;
	}

	for (InterpBasicBlock *bb = td->entry_bb; bb != NULL; bb = bb->next_bb)	{
		if (bb->dominated) {
			g_slist_free (bb->dominated);
			bb->dominated = NULL;
		}
		bb->dfs_index = -1;
		bb->gen_set = NULL;
		bb->kill_set = NULL;
		bb->live_in_set = NULL;
		bb->live_out_set = NULL;
	}

	for (unsigned int i = 0; i < td->renamable_vars_size; i++) {
		if (td->renamable_vars [i].ssa_stack) {
			g_slist_free (td->renamable_vars [i].ssa_stack);
			td->renamable_vars [i].ssa_stack = NULL;
		}
	}
	td->renamable_vars_size = 0;

	for (unsigned int i = 0; i < td->renamed_fixed_vars_size; i++) {
		if (td->renamed_fixed_vars [i].live_limit_bblocks) {
			g_slist_free (td->renamed_fixed_vars [i].live_limit_bblocks);
			td->renamed_fixed_vars [i].live_limit_bblocks = NULL;
		}
	}
	td->renamed_fixed_vars_size = 0;
}

/*
 * BASIC BLOCK OPTIMIZATION
 */

static void
mark_bb_as_dead (TransformData *td, InterpBasicBlock *bb, InterpBasicBlock *replace_bb)
{
	// Update IL offset to bb mapping so that offset_to_bb doesn't point to dead
	// bblocks. This mapping can still be needed when computing clause ranges. Since
	// multiple IL offsets can end up pointing to same bblock after optimizations,
	// make sure we update mapping for all of them
	//
	// To avoid scanning the entire offset_to_bb array, we scan only in the vicinity
	// of the IL offset of bb. We can stop search when we encounter a different bblock.
	g_assert (bb->il_offset >= 0);
	for (int il_offset = bb->il_offset; il_offset >= 0; il_offset--) {
		if (td->offset_to_bb [il_offset] == bb)
			td->offset_to_bb [il_offset] = replace_bb;
		else if (td->offset_to_bb [il_offset])
			break;
	}
	for (guint32 il_offset = bb->il_offset + 1; il_offset < td->header->code_size; il_offset++) {
		if (td->offset_to_bb [il_offset] == bb)
			td->offset_to_bb [il_offset] = replace_bb;
		else if (td->offset_to_bb [il_offset])
			break;
	}

	if (bb->dominated)
		g_slist_free (bb->dominated);
	bb->dead = TRUE;
	// bb should never be used/referenced after this
}

/* Merges two consecutive bbs (in code order) into a single one */
static void
interp_merge_bblocks (TransformData *td, InterpBasicBlock *bb, InterpBasicBlock *bbadd)
{
	g_assert (bbadd->in_count == 1 && bbadd->in_bb [0] == bb);
	g_assert (bb->next_bb == bbadd);

	// Remove the branch instruction to the invalid bblock
	if (bb->last_ins) {
		InterpInst *last_ins = (bb->last_ins->opcode != MINT_NOP) ? bb->last_ins : interp_prev_ins (bb->last_ins);
		if (last_ins) {
			if (last_ins->opcode == MINT_BR) {
				g_assert (last_ins->info.target_bb == bbadd);
				interp_clear_ins (last_ins);
			} else if (last_ins->opcode == MINT_SWITCH) {
				// Weird corner case where empty switch can branch by default to next instruction
				last_ins->opcode = MINT_NOP;
			}
		}
	}

	// Append all instructions from bbadd to bb
	if (bb->last_ins) {
		if (bbadd->first_ins) {
			bb->last_ins->next = bbadd->first_ins;
			bbadd->first_ins->prev = bb->last_ins;
			bb->last_ins = bbadd->last_ins;
		}
	} else {
		bb->first_ins = bbadd->first_ins;
		bb->last_ins = bbadd->last_ins;
	}
	bb->next_bb = bbadd->next_bb;

	// Fixup bb links
	bb->out_count = bbadd->out_count;
	bb->out_bb = bbadd->out_bb;
	for (int i = 0; i < bbadd->out_count; i++) {
		for (int j = 0; j < bbadd->out_bb [i]->in_count; j++) {
			if (bbadd->out_bb [i]->in_bb [j] == bbadd)
				bbadd->out_bb [i]->in_bb [j] = bb;
		}
	}

	mark_bb_as_dead (td, bbadd, bb);
}

// array must contain ref
static void
remove_bblock_ref (InterpBasicBlock **array, InterpBasicBlock *ref, int len)
{
	int i = 0;
	while (array [i] != ref)
		i++;
	i++;
	while (i < len) {
		array [i - 1] = array [i];
		i++;
	}
}

static void
interp_unlink_bblocks (InterpBasicBlock *from, InterpBasicBlock *to)
{
	remove_bblock_ref (from->out_bb, to, from->out_count);
	from->out_count--;
	remove_bblock_ref (to->in_bb, from, to->in_count);
	to->in_count--;
}

static void
interp_handle_unreachable_bblock (TransformData *td, InterpBasicBlock *bb)
{
	for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
		if (ins->opcode == MINT_LDLOCA_S) {
			td->vars [ins->sregs [0]].indirects--;
			if (!td->vars [ins->sregs [0]].indirects) {
				if (td->verbose_level)
					g_print ("Remove bblock %d, var %d no longer indirect\n", bb->index, ins->sregs [0]);
				td->need_optimization_retry = TRUE;
			}
		}

		// If preserve is set, even if we know this bblock is unreachable, we still have to keep
		// it alive (for now at least). We just remove all instructions from it in this case.
		if (bb->preserve)
			interp_clear_ins (ins);
	}
}

static void
interp_remove_bblock (TransformData *td, InterpBasicBlock *bb, InterpBasicBlock *prev_bb)
{
	while (bb->in_count)
		interp_unlink_bblocks (bb->in_bb [0], bb);
	while (bb->out_count)
		interp_unlink_bblocks (bb, bb->out_bb [0]);
	prev_bb->next_bb = bb->next_bb;
	mark_bb_as_dead (td, bb, bb->next_bb);
}

void
interp_link_bblocks (TransformData *td, InterpBasicBlock *from, InterpBasicBlock *to)
{
	int i;
	gboolean found = FALSE;

	for (i = 0; i < from->out_count; ++i) {
		if (to == from->out_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (!found) {
		InterpBasicBlock **newa = (InterpBasicBlock**)mono_mempool_alloc (td->mempool, sizeof (InterpBasicBlock*) * (from->out_count + 1));
		for (i = 0; i < from->out_count; ++i)
			newa [i] = from->out_bb [i];
		newa [i] = to;
		from->out_count++;
		from->out_bb = newa;
	}

	found = FALSE;
	for (i = 0; i < to->in_count; ++i) {
		if (from == to->in_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (!found) {
		InterpBasicBlock **newa = (InterpBasicBlock**)mono_mempool_alloc (td->mempool, sizeof (InterpBasicBlock*) * (to->in_count + 1));
		for (i = 0; i < to->in_count; ++i)
			newa [i] = to->in_bb [i];
		newa [i] = from;
		to->in_count++;
		to->in_bb = newa;
	}
}

static void
interp_mark_reachable_bblocks (TransformData *td)
{
	InterpBasicBlock **queue = mono_mempool_alloc0 (td->mempool, td->bb_count * sizeof (InterpBasicBlock*));
	InterpBasicBlock *current;
	int cur_index = 0;
	int next_position = 0;

	current = td->entry_bb;
	while (current != NULL) {
		current->reachable = FALSE;
		current = current->next_bb;
	}

	queue [next_position++] = td->entry_bb;
	td->entry_bb->reachable = TRUE;

retry:
	// We have the roots, traverse everything else
	while (cur_index < next_position) {
		current = queue [cur_index++];
		for (int i = 0; i < current->out_count; i++) {
			InterpBasicBlock *child = current->out_bb [i];
			if (!child->reachable) {
				queue [next_position++] = child;
				child->reachable = TRUE;
			}
		}
	}

	if (td->header->num_clauses) {
		gboolean needs_retry = FALSE;
		current = td->entry_bb;
		while (current != NULL) {
			if (current->try_bblock && !current->reachable && current->try_bblock->reachable) {
				// Try bblock is reachable and the handler is not yet marked
				queue [next_position++] = current;
				current->reachable = TRUE;
				needs_retry = TRUE;
			}
			current = current->next_bb;
		}

		if (needs_retry)
			goto retry;
	}
}

/**
 * Returns TRUE if instruction or previous instructions defines at least one of the variables, FALSE otherwise.
 */

static gboolean
interp_prev_block_defines_var (InterpInst *ins, int var1, int var2)
{
	// Check max of 5 instructions
	for (int i = 0; i < 5; i++) {
		ins = interp_prev_ins (ins);
		if (!ins)
			return FALSE;
		if (mono_interp_op_dregs [ins->opcode] && (ins->dreg == var1 || ins->dreg == var2))
			return TRUE;
	}
	return FALSE;
}

/**
 * Check if the given basic block has a known pattern for inlining into callers blocks, if so, return a pointer to the conditional branch instruction.
 *
 * The known patterns are:
 * - `branch`: a conditional branch instruction.
 * - `ldc; branch`: a load instruction followed by a binary conditional branch.
 * - `ldc; compare; branch`: a load instruction followed by a compare instruction and a unary conditional branch.
 */
static InterpInst*
interp_inline_into_callers (InterpInst *first, int *lookup_var1, int *lookup_var2) {
	// pattern `branch`
	if (MINT_IS_CONDITIONAL_BRANCH (first->opcode)) {
		*lookup_var1 = first->sregs [0];
		*lookup_var2 = (mono_interp_op_dregs [first->opcode] > 1) ? first->sregs [1] : -1;
		return first;
	}

	if (MINT_IS_LDC_I4 (first->opcode)) {
		InterpInst *second = interp_next_ins (first);
		if (!second)
			return NULL;
		*lookup_var2 = -1;
		gboolean first_var_defined = first->dreg == second->sregs [0];
		gboolean second_var_defined = first->dreg == second->sregs [1];
		// pattern `ldc; binop conditional branch`
		if (MINT_IS_BINOP_CONDITIONAL_BRANCH (second->opcode) && (first_var_defined || second_var_defined)) {
			*lookup_var1 = first_var_defined ? second->sregs [1] : second->sregs [0];
			return second;
		}

		InterpInst *third = interp_next_ins (second);
		if (!third)
			return NULL;
		// pattern `ldc; compare; conditional branch`
		if (MINT_IS_COMPARE (second->opcode) && (first_var_defined || second_var_defined)
			&& MINT_IS_UNOP_CONDITIONAL_BRANCH (third->opcode) && second->dreg == third->sregs [0]) {
			*lookup_var1 = first_var_defined ? second->sregs [1] : second->sregs [0];
			return third;
		}
	}

	return NULL;
}

InterpInst*
interp_first_ins (InterpBasicBlock *bb)
{
	InterpInst *ins = bb->first_ins;
	if (!ins || !interp_ins_is_nop (ins))
		return ins;
	while (ins && interp_ins_is_nop (ins))
		ins = ins->next;
	return ins;
}


static InterpInst*
interp_last_ins (InterpBasicBlock *bb)
{
	InterpInst *ins = bb->last_ins;
	if (!ins || !interp_ins_is_nop (ins))
		return ins;
	return interp_prev_ins (ins);
}

static void
interp_reorder_bblocks (TransformData *td)
{
	InterpBasicBlock *bb;
	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		if (bb->preserve)
			continue;
		// We do optimizations below where we reduce the in count of bb, but it is ideal to have
		// this bblock remain alive so we can correctly resolve mapping from unoptimized method.
		// We could in theory address this and attempt to remove bb, but this scenario is extremely
		// rare and doesn't seem worth the investment.
		if (bb->patchpoint_data)
			continue;
		InterpInst *first = interp_first_ins (bb);
		if (!first)
			continue;
		int lookup_var1, lookup_var2;
		InterpInst *cond_ins = interp_inline_into_callers (first, &lookup_var1, &lookup_var2);
		if (cond_ins) {
			// This means this bblock match a pattern for inlining into callers, with a conditional branch
			int i = 0;
			while (i < bb->in_count) {
				InterpBasicBlock *in_bb = bb->in_bb [i];
				InterpInst *last_ins = interp_last_ins (in_bb);
				if (last_ins && last_ins->opcode == MINT_BR && interp_prev_block_defines_var (last_ins, lookup_var1, lookup_var2)) {
					// This bblock is reached unconditionally from one of its parents
					// Move the conditional branch inside the parent to facilitate propagation
					// of condition value.
					InterpBasicBlock *cond_true_bb = cond_ins->info.target_bb;
					InterpBasicBlock *next_bb = bb->next_bb;

					// Parent bb will do the conditional branch
					interp_unlink_bblocks (in_bb, bb);
					// Remove ending MINT_BR
					interp_clear_ins (last_ins);
					// Copy all instructions one by one, from interp_first_ins (bb) to the end of the in_bb
					InterpInst *copy_ins = first;
					while (copy_ins) {
						InterpInst *new_ins = interp_insert_ins_bb (td, in_bb, in_bb->last_ins, copy_ins->opcode);
						new_ins->dreg = copy_ins->dreg;
						new_ins->sregs [0] = copy_ins->sregs [0];
						if (mono_interp_op_sregs [copy_ins->opcode] > 1)
							new_ins->sregs [1] = copy_ins->sregs [1];

						new_ins->data [0] = copy_ins->data [0];
						if (copy_ins->opcode == MINT_LDC_I4)
							new_ins->data [1] = copy_ins->data [1];

						copy_ins = interp_next_ins (copy_ins);
					}
					in_bb->last_ins->info.target_bb = cond_true_bb;
					interp_link_bblocks (td, in_bb, cond_true_bb);

					// Create new fallthrough bb between in_bb and in_bb->next_bb
					InterpBasicBlock *new_bb = interp_alloc_bb (td);
					new_bb->next_bb = in_bb->next_bb;
					in_bb->next_bb = new_bb;
					new_bb->il_offset = in_bb->il_offset;
					interp_link_bblocks (td, in_bb, new_bb);

					InterpInst *new_inst = interp_insert_ins_bb (td, new_bb, NULL, MINT_BR);
					new_inst->info.target_bb = next_bb;

					interp_link_bblocks (td, new_bb, next_bb);
					if (td->verbose_level) {
						GString* bb_info = interp_get_bb_links (bb);
						GString* in_bb_info = interp_get_bb_links (in_bb);
						GString* new_bb_info = interp_get_bb_links (new_bb);
						g_print ("Moved cond branch BB%d into BB%d, new BB%d\n", bb->index, in_bb->index, new_bb->index);
						g_print ("\tBB%d: %s\n", bb->index, bb_info->str);
						g_print ("\tBB%d: %s\n", in_bb->index, in_bb_info->str);
						g_print ("\tBB%d: %s\n", new_bb->index, new_bb_info->str);
						g_string_free (bb_info, TRUE);
						g_string_free (in_bb_info, TRUE);
						g_string_free (new_bb_info, TRUE);
					}
					// Since we changed links, in_bb might have changed, loop again from the start
					i = 0;
				} else {
					i++;
				}
			}
		} else if (first->opcode == MINT_BR) {
			// All bblocks jumping into this bblock can jump directly into the br target since it is the single instruction of the bb
			int i = 0;
			while (i < bb->in_count) {
				InterpBasicBlock *in_bb = bb->in_bb [i];
				InterpInst *last_ins = interp_last_ins (in_bb);
				if (last_ins && (MINT_IS_CONDITIONAL_BRANCH (last_ins->opcode) ||
						MINT_IS_UNCONDITIONAL_BRANCH (last_ins->opcode)) &&
						last_ins->info.target_bb == bb) {
					InterpBasicBlock *target_bb = first->info.target_bb;
					last_ins->info.target_bb = target_bb;
					interp_unlink_bblocks (in_bb, bb);
					interp_link_bblocks (td, in_bb, target_bb);
					if (td->verbose_level) {
						GString* bb_info = interp_get_bb_links (bb);
						GString* in_bb_info = interp_get_bb_links (in_bb);
						GString* target_bb_info = interp_get_bb_links (target_bb);
						g_print ("Propagated target bb BB%d into BB%d\n", target_bb->index, in_bb->index);
						g_print ("\tBB%d: %s\n", bb->index, bb_info->str);
						g_print ("\tBB%d: %s\n", in_bb->index, in_bb_info->str);
						g_print ("\tBB%d: %s\n", target_bb->index, target_bb_info->str);
						g_string_free (bb_info, TRUE);
						g_string_free (in_bb_info, TRUE);
						g_string_free (target_bb_info, TRUE);
					}
					i = 0;
				} else {
					i++;
				}
			}
		}
	}
}

// Traverse the list of basic blocks and merge adjacent blocks
static void
interp_optimize_bblocks (TransformData *td)
{
	InterpBasicBlock *bb = td->entry_bb;

	interp_reorder_bblocks (td);

	interp_mark_reachable_bblocks (td);

	while (TRUE) {
		InterpBasicBlock *next_bb = bb->next_bb;
		if (!next_bb)
			break;
		if (!next_bb->reachable) {
			interp_handle_unreachable_bblock (td, next_bb);
			if (next_bb->preserve) {
				if (td->verbose_level)
					g_print ("Removed BB%d, cleared instructions only\n", next_bb->index);
			} else {
				if (td->verbose_level)
					g_print ("Removed BB%d\n", next_bb->index);
				interp_remove_bblock (td, next_bb, bb);
				continue;
			}
		} else if (bb->out_count == 1 && bb->out_bb [0] == next_bb && next_bb->in_count == 1 && !next_bb->preserve && !next_bb->patchpoint_data) {
			g_assert (next_bb->in_bb [0] == bb);
			interp_merge_bblocks (td, bb, next_bb);
			if (td->verbose_level)
				g_print ("Merged BB%d and BB%d\n", bb->index, next_bb->index);
			continue;
		}

		bb = next_bb;
	}
}

static void
decrement_ref_count (TransformData *td, int *varp, gpointer data)
{
	int var = *varp;
	td->var_values [var].ref_count--;
	// FIXME we could clear recursively
	if (!td->var_values [var].ref_count)
		*(gboolean*)data = TRUE;
}

static void
interp_var_deadce (TransformData *td)
{
	gboolean need_retry;

retry:
	need_retry = FALSE;

	// Kill instructions that are storing into unreferenced vars
	for (InterpBasicBlock *bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
			if (MINT_NO_SIDE_EFFECTS (ins->opcode) ||
					ins->opcode == MINT_LDLOCA_S) {
				int dreg = ins->dreg;
				if (var_has_indirects (td, dreg))
					continue;

				if (!td->var_values [dreg].ref_count) {
					if (td->verbose_level) {
						g_print ("kill dead ins:\n\t");
						interp_dump_ins (ins, td->data_items);
					}
					if (ins->opcode == MINT_LDLOCA_S) {
						td->vars [ins->sregs [0]].indirects--;
						if (!td->vars [ins->sregs [0]].indirects) {
							if (td->verbose_level)
								g_print ("Kill ldloca, var %d no longer indirect\n", ins->sregs [0]);
							td->need_optimization_retry = TRUE;
						}
					}

					interp_foreach_ins_svar (td, ins, &need_retry, decrement_ref_count);

					interp_clear_ins (ins);
				}
			}
		}
	}

	if (need_retry)
		goto retry;
}

static InterpInst*
interp_inst_replace_with_i8_const (TransformData *td, InterpInst *ins, gint64 ct)
{
	int size = mono_interp_oplen [ins->opcode];
	int dreg = ins->dreg;

	if (size < 5) {
		ins = interp_insert_ins (td, ins, MINT_LDC_I8);
		interp_clear_ins (ins->prev);
	} else {
			ins->opcode = MINT_LDC_I8;
	}
	WRITE64_INS (ins, 0, &ct);
	ins->dreg = dreg;

	return ins;
}

static gint64
interp_get_const_from_ldc_i8 (InterpInst *ins)
{
	switch (ins->opcode) {
	case MINT_LDC_I8_0: return 0;
	case MINT_LDC_I8_S: return (gint64)(gint16)ins->data [0];
	case MINT_LDC_I8: return READ64 (&ins->data [0]);
	default:
		g_assert_not_reached ();
	}
}

static int
interp_get_mt_for_ldind (int ldind_op)
{
        switch (ldind_op) {
                case MINT_LDIND_I1: return MINT_TYPE_I1;
                case MINT_LDIND_U1: return MINT_TYPE_U1;
                case MINT_LDIND_I2: return MINT_TYPE_I2;
                case MINT_LDIND_U2: return MINT_TYPE_U2;
                case MINT_LDIND_I4: return MINT_TYPE_I4;
                case MINT_LDIND_I8: return MINT_TYPE_I8;
                case MINT_LDIND_R4: return MINT_TYPE_R4;
                case MINT_LDIND_R8: return MINT_TYPE_R8;
                default:
                        g_assert_not_reached ();
        }
        return -1;
}

#define INTERP_FOLD_UNOP(opcode,val_type,field,op) \
	case opcode: \
		result.type = val_type; \
		result.field = op val->field; \
		break;

#define INTERP_FOLD_CONV(opcode,val_type_dst,field_dst,val_type_src,field_src,cast_type) \
	case opcode: \
		result.type = val_type_dst; \
		result.field_dst = (cast_type)val->field_src; \
		break;

#define INTERP_FOLD_CONV_FULL(opcode,val_type_dst,field_dst,val_type_src,field_src,cast_type,cond) \
	case opcode: \
		if (!(cond)) return ins; \
		result.type = val_type_dst; \
		result.field_dst = (cast_type)val->field_src; \
		break;

static InterpInst*
interp_fold_unop (TransformData *td, InterpInst *ins)
{
	// ins should be an unop, therefore it should have a single dreg and a single sreg
	int dreg = ins->dreg;
	int sreg = ins->sregs [0];
	InterpVarValue *val = get_var_value (td, sreg);
	InterpVarValue result;

	if (!val)
		return ins;
	if (val->type != VAR_VALUE_I4 && val->type != VAR_VALUE_I8)
		return ins;

	// Top of the stack is a constant
	switch (ins->opcode) {
		INTERP_FOLD_UNOP (MINT_ADD1_I4, VAR_VALUE_I4, i, 1+);
		INTERP_FOLD_UNOP (MINT_ADD1_I8, VAR_VALUE_I8, l, 1+);
		INTERP_FOLD_UNOP (MINT_SUB1_I4, VAR_VALUE_I4, i, -1+);
		INTERP_FOLD_UNOP (MINT_SUB1_I8, VAR_VALUE_I8, l, -1+);
		INTERP_FOLD_UNOP (MINT_NEG_I4, VAR_VALUE_I4, i, -);
		INTERP_FOLD_UNOP (MINT_NEG_I8, VAR_VALUE_I8, l, -);
		INTERP_FOLD_UNOP (MINT_NOT_I4, VAR_VALUE_I4, i, ~);
		INTERP_FOLD_UNOP (MINT_NOT_I8, VAR_VALUE_I8, l, ~);
		INTERP_FOLD_UNOP (MINT_CEQ0_I4, VAR_VALUE_I4, i, 0 ==);

		INTERP_FOLD_CONV (MINT_CONV_I1_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, gint8);
		INTERP_FOLD_CONV (MINT_CONV_I1_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, gint8);
		INTERP_FOLD_CONV (MINT_CONV_U1_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, guint8);
		INTERP_FOLD_CONV (MINT_CONV_U1_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, guint8);

		INTERP_FOLD_CONV (MINT_CONV_I2_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, gint16);
		INTERP_FOLD_CONV (MINT_CONV_I2_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, gint16);
		INTERP_FOLD_CONV (MINT_CONV_U2_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, guint16);
		INTERP_FOLD_CONV (MINT_CONV_U2_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, guint16);

		INTERP_FOLD_CONV (MINT_CONV_I8_I4, VAR_VALUE_I8, l, VAR_VALUE_I4, i, gint32);
		INTERP_FOLD_CONV (MINT_CONV_I8_U4, VAR_VALUE_I8, l, VAR_VALUE_I4, i, guint32);

		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I1_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, gint8, val->i >= G_MININT8 && val->i <= G_MAXINT8);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I1_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, gint8, val->l >= G_MININT8 && val->l <= G_MAXINT8);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I1_U4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, gint8, val->i >= 0 && val->i <= G_MAXINT8);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I1_U8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, gint8, val->l >= 0 && val->l <= G_MAXINT8);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U1_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, guint8, val->i >= 0 && val->i <= G_MAXUINT8);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U1_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, guint8, val->l >= 0 && val->l <= G_MAXUINT8);

		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I2_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, gint16, val->i >= G_MININT16 && val->i <= G_MAXINT16);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I2_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, i, gint16, val->l >= G_MININT16 && val->l <= G_MAXINT16);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I2_U4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, gint16, val->i >= 0 && val->i <= G_MAXINT16);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I2_U8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, gint16, val->l >= 0 && val->l <= G_MAXINT16);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U2_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, guint16, val->i >= 0 && val->i <= G_MAXUINT16);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U2_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, guint16, val->l >= 0 && val->l <= G_MAXUINT16);

		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I4_U4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, gint32, val->i >= 0);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I4_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, gint32, val->l >= G_MININT32 && val->l <= G_MAXINT32);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I4_U8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, gint32, val->l >= 0 && val->l <= G_MAXINT32);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U4_I4, VAR_VALUE_I4, i, VAR_VALUE_I4, i, guint32, val->i >= 0);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U4_I8, VAR_VALUE_I4, i, VAR_VALUE_I8, l, guint32, val->l >= 0 && val->l <= G_MAXINT32);

		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_I8_U8, VAR_VALUE_I8, l, VAR_VALUE_I8, l, gint64, val->l >= 0);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U8_I4, VAR_VALUE_I8, l, VAR_VALUE_I4, i, guint64, val->i >= 0);
		INTERP_FOLD_CONV_FULL (MINT_CONV_OVF_U8_I8, VAR_VALUE_I8, l, VAR_VALUE_I8, l, guint64, val->l >= 0);

		default:
			return ins;
	}

	// We were able to compute the result of the ins instruction. We replace the unop
	// with a LDC of the constant. We leave alone the sregs of this instruction, for
	// deadce to kill the instructions initializing them.
	if (result.type == VAR_VALUE_I4)
		ins = interp_get_ldc_i4_from_const (td, ins, result.i, dreg);
	else if (result.type == VAR_VALUE_I8)
		ins = interp_inst_replace_with_i8_const (td, ins, result.l);
	else
		g_assert_not_reached ();

	if (td->verbose_level) {
		g_print ("Fold unop :\n\t");
		interp_dump_ins (ins, td->data_items);
	}

	td->var_values [sreg].ref_count--;
	result.def = ins;
	result.ref_count = td->var_values [dreg].ref_count; // preserve ref count
	td->var_values [dreg] = result;

	return ins;
}

#define INTERP_FOLD_UNOP_BR(_opcode,_cond) \
	case _opcode: \
		if (_cond) { \
			ins->opcode = MINT_BR; \
			if (cbb->next_bb != ins->info.target_bb) \
				interp_unlink_bblocks (cbb, cbb->next_bb); \
			for (InterpInst *it = ins->next; it != NULL; it = it->next) \
				interp_clear_ins (it); \
		} else { \
			interp_clear_ins (ins); \
			interp_unlink_bblocks (cbb, ins->info.target_bb); \
		} \
		break;

static InterpInst*
interp_fold_unop_cond_br (TransformData *td, InterpBasicBlock *cbb, InterpInst *ins)
{
	// ins should be an unop conditional branch, therefore it should have a single sreg
	int sreg = ins->sregs [0];
	InterpVarValue *val = get_var_value (td, sreg);

	if (!val)
		return ins;
	if (val->type != VAR_VALUE_I4 && val->type != VAR_VALUE_I8 && val->type != VAR_VALUE_NON_NULL)
		return ins;

	if (val->type == VAR_VALUE_NON_NULL) {
		switch (ins->opcode) {
			INTERP_FOLD_UNOP_BR (MINT_BRFALSE_I4, FALSE);
			INTERP_FOLD_UNOP_BR (MINT_BRFALSE_I8, FALSE);
			INTERP_FOLD_UNOP_BR (MINT_BRTRUE_I4, TRUE);
			INTERP_FOLD_UNOP_BR (MINT_BRTRUE_I8, TRUE);

			default:
				return ins;
		}
	} else {
		// Top of the stack is a constant
		switch (ins->opcode) {
			INTERP_FOLD_UNOP_BR (MINT_BRFALSE_I4, val->i == 0);
			INTERP_FOLD_UNOP_BR (MINT_BRFALSE_I8, val->l == 0);
			INTERP_FOLD_UNOP_BR (MINT_BRTRUE_I4, val->i != 0);
			INTERP_FOLD_UNOP_BR (MINT_BRTRUE_I8, val->l != 0);

			default:
				return ins;
		}
	}

	if (td->verbose_level) {
		g_print ("Fold unop cond br :\n\t");
		interp_dump_ins (ins, td->data_items);
	}

	td->var_values [sreg].ref_count--;
	return ins;
}

#define INTERP_FOLD_BINOP(opcode,local_type,field,op) \
	case opcode: \
		result.type = local_type; \
		result.field = val1->field op val2->field; \
		break;

#define INTERP_FOLD_BINOP_FULL(opcode,local_type,field,op,cast_type,cond) \
	case opcode: \
		if (!(cond)) return ins; \
		result.type = local_type; \
		result.field = (cast_type)val1->field op (cast_type)val2->field; \
		break;

#define INTERP_FOLD_SHIFTOP(opcode,local_type,field,shift_op,cast_type) \
	case opcode: \
		result.type = local_type; \
		result.field = (cast_type)val1->field shift_op val2->i; \
		break;

#define INTERP_FOLD_RELOP(opcode,local_type,field,relop,cast_type) \
	case opcode: \
		result.type = VAR_VALUE_I4; \
		result.i = (cast_type) val1->field relop (cast_type) val2->field; \
		break;


static InterpInst*
interp_fold_binop (TransformData *td, InterpInst *ins, gboolean *folded)
{
	// ins should be a binop, therefore it should have a single dreg and two sregs
	int dreg = ins->dreg;
	int sreg1 = ins->sregs [0];
	int sreg2 = ins->sregs [1];
	InterpVarValue *val1 = get_var_value (td, sreg1);
	InterpVarValue *val2 = get_var_value (td, sreg2);
	InterpVarValue result;

	*folded = FALSE;

	if (!val1 || !val2)
		return ins;
	if (val1->type != VAR_VALUE_I4 && val1->type != VAR_VALUE_I8)
		return ins;
	if (val2->type != VAR_VALUE_I4 && val2->type != VAR_VALUE_I8)
		return ins;

	// Top two values of the stack are constants
	switch (ins->opcode) {
		INTERP_FOLD_BINOP (MINT_ADD_I4, VAR_VALUE_I4, i, +);
		INTERP_FOLD_BINOP (MINT_ADD_I8, VAR_VALUE_I8, l, +);
		INTERP_FOLD_BINOP (MINT_SUB_I4, VAR_VALUE_I4, i, -);
		INTERP_FOLD_BINOP (MINT_SUB_I8, VAR_VALUE_I8, l, -);
		INTERP_FOLD_BINOP (MINT_MUL_I4, VAR_VALUE_I4, i, *);
		INTERP_FOLD_BINOP (MINT_MUL_I8, VAR_VALUE_I8, l, *);

		INTERP_FOLD_BINOP (MINT_AND_I4, VAR_VALUE_I4, i, &);
		INTERP_FOLD_BINOP (MINT_AND_I8, VAR_VALUE_I8, l, &);
		INTERP_FOLD_BINOP (MINT_OR_I4, VAR_VALUE_I4, i, |);
		INTERP_FOLD_BINOP (MINT_OR_I8, VAR_VALUE_I8, l, |);
		INTERP_FOLD_BINOP (MINT_XOR_I4, VAR_VALUE_I4, i, ^);
		INTERP_FOLD_BINOP (MINT_XOR_I8, VAR_VALUE_I8, l, ^);

		INTERP_FOLD_SHIFTOP (MINT_SHL_I4, VAR_VALUE_I4, i, <<, gint32);
		INTERP_FOLD_SHIFTOP (MINT_SHL_I8, VAR_VALUE_I8, l, <<, gint64);
		INTERP_FOLD_SHIFTOP (MINT_SHR_I4, VAR_VALUE_I4, i, >>, gint32);
		INTERP_FOLD_SHIFTOP (MINT_SHR_I8, VAR_VALUE_I8, l, >>, gint64);
		INTERP_FOLD_SHIFTOP (MINT_SHR_UN_I4, VAR_VALUE_I4, i, >>, guint32);
		INTERP_FOLD_SHIFTOP (MINT_SHR_UN_I8, VAR_VALUE_I8, l, >>, guint64);

		INTERP_FOLD_RELOP (MINT_CEQ_I4, VAR_VALUE_I4, i, ==, gint32);
		INTERP_FOLD_RELOP (MINT_CEQ_I8, VAR_VALUE_I8, l, ==, gint64);
		INTERP_FOLD_RELOP (MINT_CNE_I4, VAR_VALUE_I4, i, !=, gint32);
		INTERP_FOLD_RELOP (MINT_CNE_I8, VAR_VALUE_I8, l, !=, gint64);

		INTERP_FOLD_RELOP (MINT_CGT_I4, VAR_VALUE_I4, i, >, gint32);
		INTERP_FOLD_RELOP (MINT_CGT_I8, VAR_VALUE_I8, l, >, gint64);
		INTERP_FOLD_RELOP (MINT_CGT_UN_I4, VAR_VALUE_I4, i, >, guint32);
		INTERP_FOLD_RELOP (MINT_CGT_UN_I8, VAR_VALUE_I8, l, >, guint64);

		INTERP_FOLD_RELOP (MINT_CGE_I4, VAR_VALUE_I4, i, >=, gint32);
		INTERP_FOLD_RELOP (MINT_CGE_I8, VAR_VALUE_I8, l, >=, gint64);
		INTERP_FOLD_RELOP (MINT_CGE_UN_I4, VAR_VALUE_I4, i, >=, guint32);
		INTERP_FOLD_RELOP (MINT_CGE_UN_I8, VAR_VALUE_I8, l, >=, guint64);

		INTERP_FOLD_RELOP (MINT_CLT_I4, VAR_VALUE_I4, i, <, gint32);
		INTERP_FOLD_RELOP (MINT_CLT_I8, VAR_VALUE_I8, l, <, gint64);
		INTERP_FOLD_RELOP (MINT_CLT_UN_I4, VAR_VALUE_I4, i, <, guint32);
		INTERP_FOLD_RELOP (MINT_CLT_UN_I8, VAR_VALUE_I8, l, <, guint64);

		INTERP_FOLD_RELOP (MINT_CLE_I4, VAR_VALUE_I4, i, <=, gint32);
		INTERP_FOLD_RELOP (MINT_CLE_I8, VAR_VALUE_I8, l, <=, gint64);
		INTERP_FOLD_RELOP (MINT_CLE_UN_I4, VAR_VALUE_I4, i, <=, guint32);
		INTERP_FOLD_RELOP (MINT_CLE_UN_I8, VAR_VALUE_I8, l, <=, guint64);

		INTERP_FOLD_BINOP_FULL (MINT_DIV_I4, VAR_VALUE_I4, i, /, gint32, val2->i != 0 && (val1->i != G_MININT32 || val2->i != -1));
		INTERP_FOLD_BINOP_FULL (MINT_DIV_I8, VAR_VALUE_I8, l, /, gint64, val2->l != 0 && (val1->l != G_MININT64 || val2->l != -1));
		INTERP_FOLD_BINOP_FULL (MINT_DIV_UN_I4, VAR_VALUE_I4, i, /, guint32, val2->i != 0);
		INTERP_FOLD_BINOP_FULL (MINT_DIV_UN_I8, VAR_VALUE_I8, l, /, guint64, val2->l != 0);

		INTERP_FOLD_BINOP_FULL (MINT_REM_I4, VAR_VALUE_I4, i, %, gint32, val2->i != 0 && (val1->i != G_MININT32 || val2->i != -1));
		INTERP_FOLD_BINOP_FULL (MINT_REM_I8, VAR_VALUE_I8, l, %, gint64, val2->l != 0 && (val1->l != G_MININT64 || val2->l != -1));
		INTERP_FOLD_BINOP_FULL (MINT_REM_UN_I4, VAR_VALUE_I4, i, %, guint32, val2->i != 0);
		INTERP_FOLD_BINOP_FULL (MINT_REM_UN_I8, VAR_VALUE_I8, l, %, guint64, val2->l != 0);

		default:
			return ins;
	}

	// We were able to compute the result of the ins instruction. We replace the binop
	// with a LDC of the constant. We leave alone the sregs of this instruction, for
	// deadce to kill the instructions initializing them.
	*folded = TRUE;
	if (result.type == VAR_VALUE_I4)
		ins = interp_get_ldc_i4_from_const (td, ins, result.i, dreg);
	else if (result.type == VAR_VALUE_I8)
		ins = interp_inst_replace_with_i8_const (td, ins, result.l);
	else
		g_assert_not_reached ();

	if (td->verbose_level) {
		g_print ("Fold binop :\n\t");
		interp_dump_ins (ins, td->data_items);
	}

	td->var_values [sreg1].ref_count--;
	td->var_values [sreg2].ref_count--;
	result.def = ins;
	result.ref_count = td->var_values [dreg].ref_count; // preserve ref count
	td->var_values [dreg] = result;

	return ins;
}

// Due to poor current design, the branch op might not be the last instruction in the bblock
// (in case we fallthrough and need to have the stack locals match the ones from next_bb, done
// in fixup_newbb_stack_locals). If that's the case, clear all these mov's. This helps bblock
// merging quickly find the MINT_BR opcode.
#define INTERP_FOLD_BINOP_BR(_opcode,_local_type,_cond) \
	case _opcode: \
		if (_cond) { \
			ins->opcode = MINT_BR; \
			if (cbb->next_bb != ins->info.target_bb) \
				interp_unlink_bblocks (cbb, cbb->next_bb); \
			for (InterpInst *it = ins->next; it != NULL; it = it->next) \
				interp_clear_ins (it); \
		} else { \
			interp_clear_ins (ins); \
			interp_unlink_bblocks (cbb, ins->info.target_bb); \
		} \
		break;

static InterpInst*
interp_fold_binop_cond_br (TransformData *td, InterpBasicBlock *cbb, InterpInst *ins)
{
	// ins should be a conditional binop, therefore it should have only two sregs
	int sreg1 = ins->sregs [0];
	int sreg2 = ins->sregs [1];
	InterpVarValue *val1 = get_var_value (td, sreg1);
	InterpVarValue *val2 = get_var_value (td, sreg2);

	if (!val1 || !val2)
		return ins;
	if (val1->type != VAR_VALUE_I4 && val1->type != VAR_VALUE_I8)
		return ins;
	if (val2->type != VAR_VALUE_I4 && val2->type != VAR_VALUE_I8)
		return ins;

	switch (ins->opcode) {
		INTERP_FOLD_BINOP_BR (MINT_BEQ_I4, VAR_VALUE_I4, val1->i == val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BEQ_I8, VAR_VALUE_I8, val1->l == val2->l);
		INTERP_FOLD_BINOP_BR (MINT_BGE_I4, VAR_VALUE_I4, val1->i >= val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BGE_I8, VAR_VALUE_I8, val1->l >= val2->l);
		INTERP_FOLD_BINOP_BR (MINT_BGT_I4, VAR_VALUE_I4, val1->i > val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BGT_I8, VAR_VALUE_I8, val1->l > val2->l);
		INTERP_FOLD_BINOP_BR (MINT_BLT_I4, VAR_VALUE_I4, val1->i < val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BLT_I8, VAR_VALUE_I8, val1->l < val2->l);
		INTERP_FOLD_BINOP_BR (MINT_BLE_I4, VAR_VALUE_I4, val1->i <= val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BLE_I8, VAR_VALUE_I8, val1->l <= val2->l);

		INTERP_FOLD_BINOP_BR (MINT_BNE_UN_I4, VAR_VALUE_I4, val1->i != val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BNE_UN_I8, VAR_VALUE_I8, val1->l != val2->l);
		INTERP_FOLD_BINOP_BR (MINT_BGE_UN_I4, VAR_VALUE_I4, (guint32)val1->i >= (guint32)val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BGE_UN_I8, VAR_VALUE_I8, (guint64)val1->l >= (guint64)val2->l);
		INTERP_FOLD_BINOP_BR (MINT_BGT_UN_I4, VAR_VALUE_I4, (guint32)val1->i > (guint32)val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BGT_UN_I8, VAR_VALUE_I8, (guint64)val1->l > (guint64)val2->l);
		INTERP_FOLD_BINOP_BR (MINT_BLE_UN_I4, VAR_VALUE_I4, (guint32)val1->i <= (guint32)val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BLE_UN_I8, VAR_VALUE_I8, (guint64)val1->l <= (guint64)val2->l);
		INTERP_FOLD_BINOP_BR (MINT_BLT_UN_I4, VAR_VALUE_I4, (guint32)val1->i < (guint32)val2->i);
		INTERP_FOLD_BINOP_BR (MINT_BLT_UN_I8, VAR_VALUE_I8, (guint64)val1->l < (guint64)val2->l);

		default:
			return ins;
	}
	if (td->verbose_level) {
		g_print ("Fold binop cond br :\n\t");
		interp_dump_ins (ins, td->data_items);
	}

	td->var_values [sreg1].ref_count--;
	td->var_values [sreg2].ref_count--;
	return ins;
}

static void
write_v128_element (gpointer v128_addr, InterpVarValue *val, int index, int el_size)
{
	gpointer el_addr = (gint8*)v128_addr + index * el_size;
	g_assert ((gint8*)el_addr < ((gint8*)v128_addr + 16));
	switch (el_size) {
		case 1: *(gint8*)el_addr = (gint8)val->i; break;
		case 2: *(gint16*)el_addr = (gint16)val->i; break;
		case 4: *(gint32*)el_addr = val->i; break; // this also handles r4
		case 8: *(gint64*)el_addr = val->l; break;
		default:
			g_assert_not_reached ();
	}
}

static InterpInst*
interp_fold_simd_create (TransformData *td, InterpBasicBlock *cbb, InterpInst *ins)
{
	int *args = ins->info.call_info->call_args;
	int index = 0;
	int var = args [index];
	while (var != -1) {
		InterpVarValue *val = get_var_value (td, var);
		if (!val)
			return ins;
		if (val->type != VAR_VALUE_I4 && val->type != VAR_VALUE_I8 && val->type != VAR_VALUE_R4)
			return ins;
		index++;
		var = args [index];
	}

	// If we reached this point, it means that all args of the simd_create are constants
	// We can replace the simd_create with simd_ldc
	int el_size = 16 / index;
	int dreg = ins->dreg;

	ins = interp_insert_ins (td, ins, MINT_SIMD_V128_LDC);
	interp_clear_ins (ins->prev);
        interp_ins_set_dreg (ins, dreg);

	gpointer v128_addr = &ins->data [0];

	index = 0;
	var = args [index];
	while (var != -1) {
		InterpVarValue *val = &td->var_values [var];
		write_v128_element (v128_addr, val, index, el_size);
		val->ref_count--;
		index++;
		var = args [index];
	}

	if (td->verbose_level) {
		g_print ("Fold simd create:\n\t");
		interp_dump_ins (ins, td->data_items);
	}

	td->var_values [dreg].def = ins;
	td->var_values [dreg].type = VAR_VALUE_NONE;

	return ins;
}

static gboolean
can_extend_var_liveness (TransformData *td, int var, InterpLivenessPosition cur_liveness)
{
	if (!td->vars [var].renamed_ssa_fixed)
		return TRUE;

	InterpRenamedFixedVar *fixed_var_ext = &td->renamed_fixed_vars [td->vars [var].ext_index];

	// If var was already live at the end of this bblocks, there is no liveness extension happening
	if (fixed_var_ext->live_out_bblocks && mono_bitset_test_fast (fixed_var_ext->live_out_bblocks, cur_liveness.bb_index))
		return TRUE;

	GSList *bb_liveness = fixed_var_ext->live_limit_bblocks;
	while (bb_liveness) {
		InterpLivenessPosition *liveness_limit = (InterpLivenessPosition*)bb_liveness->data;
		if (cur_liveness.bb_index == liveness_limit->bb_index) {
			if (cur_liveness.ins_index <= liveness_limit->ins_index)
				return TRUE;
			else
				return FALSE;
		} else {
			bb_liveness = bb_liveness->next;
		}
	}

	return FALSE;
}

static void
replace_svar_use (TransformData *td, int *pvar, gpointer data)
{
	int *var_pair = (int*)data;
	int old_var = var_pair [0];
	if (*pvar == old_var) {
		int new_var = var_pair [1];
		td->var_values [old_var].ref_count--;
		td->var_values [new_var].ref_count++;
		*pvar = new_var;
		if (td->verbose_level)
			g_print ("\treplace svar use: %d -> %d\n", old_var, new_var);
	}
}

static void
replace_svar_uses (TransformData *td, InterpInst *first, InterpInst *last, int old_var, int new_var)
{
	int *var_pair = alloca (2 * sizeof (int));
	var_pair [0] = old_var;
	var_pair [1] = new_var;
	for (InterpInst *ins = first; ins != last; ins = ins->next)
		interp_foreach_ins_svar (td, ins, var_pair, replace_svar_use);
}

static void
cprop_svar (TransformData *td, InterpInst *ins, int *pvar, InterpLivenessPosition current_liveness)
{
	int var = *pvar;
	if (var_has_indirects (td, var))
		return;

	InterpVarValue *val = get_var_value (td, var);
	if (val && val->type == VAR_VALUE_OTHER_VAR) {
		// var <- cprop_var;
		// ....
		// use var;
		int cprop_var = val->var;
		if (td->vars [var].renamed_ssa_fixed && !td->vars [cprop_var].renamed_ssa_fixed) {
			// ssa fixed vars are likely to live, keep using them
			val->ref_count++;
		} else {
			gboolean can_cprop = FALSE;
			// If var is fixed ssa, we can extend liveness if it doesn't overlap with other renamed
			// vars. If the var is not ssa, we do cprop only within the same bblock. 
			if (var_is_ssa_form (td, cprop_var)) {
				can_cprop = can_extend_var_liveness (td, cprop_var, current_liveness);
			} else {
				InterpVarValue *cprop_var_val = get_var_value (td, cprop_var);
				gboolean var_def_in_cur_bb = val->liveness.bb_index == td->cbb->index;
				if (!var_def_in_cur_bb) {
					// var definition was not in current bblock so it might no longer contain
					// the current value of cprop_var because cprop_var is not in ssa form and
					// we don't keep track its value over multiple basic blocks
					can_cprop = FALSE;
				} else if (!cprop_var_val) {
					// Previously in this bblock, var is recorded as having the value of cprop_var and
					// cprop_var is not defined in the current bblock. This means that var will still
					// contain the value of cprop_var
					can_cprop = TRUE;
				} else {
					// Previously in this bblock, var is recorded as having the value of cprop_var and
					// cprop_var is defined in the current bblock. This means that var will contain the
					// value of cprop_var only if last known cprop_var redefinition was before the var definition.
					g_assert (cprop_var_val->liveness.bb_index == val->liveness.bb_index);
					can_cprop = cprop_var_val->liveness.ins_index < val->liveness.ins_index;
				}
			}

			if (can_cprop) {
				if (td->verbose_level)
					g_print ("cprop %d -> %d:\n\t", var, cprop_var);
				td->var_values [cprop_var].ref_count++;
				*pvar = cprop_var;
				if (td->verbose_level)
					interp_dump_ins (ins, td->data_items);
			} else {
				val->ref_count++;
			}
		}
	} else {
		td->var_values [var].ref_count++;
	}

	// Mark the last use for a renamable fixed var
	var = *pvar;
	if (td->vars [var].renamed_ssa_fixed) {
		int ext_index = td->renamed_fixed_vars [td->vars [var].ext_index].renamable_var_ext_index;
		td->renamable_vars [ext_index].last_use_liveness = current_liveness;
	}
}

static gboolean
can_cprop_dreg (TransformData *td, InterpInst *mov_ins)
{
	int dreg = mov_ins->dreg;
	int sreg = mov_ins->sregs [0];

	// sreg = def
	// mov sreg -> dreg

	InterpVarValue *sreg_val = get_var_value (td, sreg);
	if (!sreg_val)
		return FALSE;
	// We only apply this optimization if the definition is in the same bblock as this use
	if (sreg_val->liveness.bb_index != td->cbb->index)
		return FALSE;
	if (td->var_values [sreg].def->opcode == MINT_DEF_ARG)
		return FALSE;
	if (sreg_val->def->flags & INTERP_INST_FLAG_PROTECTED_NEWOBJ)
		return FALSE;
	// reordering moves might break conversions
	if (td->vars [dreg].mt != td->vars [sreg].mt)
		return FALSE;

	if (var_is_ssa_form (td, sreg)) {
		// check if dreg is a renamed ssa fixed var (likely to remain alive)
		if (td->vars [dreg].renamed_ssa_fixed && !td->vars [sreg].renamed_ssa_fixed) {
			InterpLivenessPosition last_use_liveness = td->renamable_vars [td->renamed_fixed_vars [td->vars [dreg].ext_index].renamable_var_ext_index].last_use_liveness;
			if (last_use_liveness.bb_index != td->cbb->index ||
					sreg_val->liveness.ins_index >= last_use_liveness.ins_index) {
				// No other conflicting renamed fixed vars (of dreg) are used in this bblock, or their
				// last use predates the definition. This means we can tweak def of sreg to store directly
				// into dreg and patch all intermediary instructions to use dreg instead.
				return TRUE;
			}
		}
	} else if (!var_is_ssa_form (td, dreg)) {
		// Neither sreg nor dreg are in SSA form. IL globals are likely to remain alive
		// We ensure that stores to no SSA vars, that are il globals, are not reordered.
		// For simplicity, we apply the optimization only if the def and move are adjacent.
		if (td->vars [dreg].il_global && !td->vars [sreg].il_global && mov_ins == interp_next_ins (sreg_val->def))
			return TRUE;
	}

	return FALSE;
}

static void
interp_cprop (TransformData *td)
{
	if (td->verbose_level)
		g_print ("\nCPROP:\n");

	// FIXME
	// There is no need to zero, if we pay attention to phi args vars. They
	// can be used before the definition.
	td->var_values = (InterpVarValue*) mono_mempool_alloc0 (td->mempool, td->vars_size * sizeof (InterpVarValue));

	// Traverse in dfs order. This guarantees that we always reach the definition first before the
	// use of the var. Exception is only for phi nodes, where we don't care about the definition
	// anyway.
	for (int bb_dfs_index = 0; bb_dfs_index < td->bblocks_count_eh; bb_dfs_index++) {
		InterpBasicBlock *bb = td->bblocks [bb_dfs_index];

		if (td->verbose_level) {
			GString* bb_info = interp_get_bb_links (bb);
			g_print ("\nBB%d: %s\n", bb->index, bb_info->str);
			g_string_free (bb_info, TRUE);
		}

		InterpLivenessPosition current_liveness;
		current_liveness.bb_index = bb->index;
		current_liveness.ins_index = 0;
		// Set cbb since we do some instruction inserting below
		td->cbb = bb;
		for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
			int opcode, num_sregs, num_dregs;
			gint32 *sregs;
			gint32 dreg;
			// LIVENESS_MARKER is set only for non-eh bblocks
			if (bb->dfs_index >= td->bblocks_count || bb->dfs_index == -1 || (ins->flags & INTERP_INST_FLAG_LIVENESS_MARKER))
				current_liveness.ins_index++;

			if (interp_ins_is_nop (ins))
				continue;

retry_instruction:
			opcode = ins->opcode;
			num_sregs = mono_interp_op_sregs [opcode];
			num_dregs = mono_interp_op_dregs [opcode];
			sregs = &ins->sregs [0];
			dreg = ins->dreg;

			if (td->verbose_level)
				interp_dump_ins (ins, td->data_items);

			if (opcode == MINT_DEF_TIER_VAR) {
				// We can't do any var propagation into this instruction since it will be deleted
				// dreg and sreg should always be identical, a ssa fixed var.
				td->var_values [sregs [0]].ref_count++;
			} else if (num_sregs) {
				for (int i = 0; i < num_sregs; i++) {
					if (sregs [i] == MINT_CALL_ARGS_SREG) {
						if (ins->info.call_info && ins->info.call_info->call_args) {
							int *call_args = ins->info.call_info->call_args;
							while (*call_args != -1) {
								cprop_svar (td, ins, call_args, current_liveness);
								call_args++;
							}
						}
					} else {
						cprop_svar (td, ins, &sregs [i], current_liveness);
					}
				}
			} else if (opcode == MINT_PHI) {
				// no cprop but add ref counts
				int *args = ins->info.args;
				while (*args != -1) {
					td->var_values [*args].ref_count++;
					args++;
				}
			}

			if (num_dregs) {
				InterpVarValue *dval = &td->var_values [dreg];
				dval->type = VAR_VALUE_NONE;
				dval->def = ins;
				dval->liveness = current_liveness;
			}

			// We always store to the full i4, except as part of STIND opcodes. These opcodes can be
			// applied to a local var only if that var has LDLOCA applied to it
			if ((opcode >= MINT_MOV_I4_I1 && opcode <= MINT_MOV_I4_U2) && !var_has_indirects (td, sregs [0])) {
				ins->opcode = MINT_MOV_4;
				opcode = MINT_MOV_4;
			}

			if (opcode == MINT_MOV_4 || opcode == MINT_MOV_8 || opcode == MINT_MOV_VT) {
				int sreg = sregs [0];
				if (dreg == sreg) {
					if (td->verbose_level)
						g_print ("clear redundant mov\n");
					interp_clear_ins (ins);
					td->var_values [sreg].ref_count--;
				} else if (var_has_indirects (td, sreg) || var_has_indirects (td, dreg)) {
					// Don't bother with indirect locals
				} else if (get_var_value_type (td, sreg) == VAR_VALUE_I4 || get_var_value_type (td, sreg) == VAR_VALUE_I8) {
					// Replace mov with ldc
					gboolean is_i4 = td->var_values [sreg].type == VAR_VALUE_I4;
					td->var_values [dreg].type = td->var_values [sreg].type;
					if (is_i4) {
						int ct = td->var_values [sreg].i;
						ins = interp_get_ldc_i4_from_const (td, ins, ct, dreg);
						td->var_values [dreg].i = ct;
					} else {
						gint64 ct = td->var_values [sreg].l;
						ins = interp_inst_replace_with_i8_const (td, ins, ct);
						td->var_values [dreg].l = ct;
					}
					td->var_values [dreg].def = ins;
					td->var_values [sreg].ref_count--;
					if (td->verbose_level) {
						g_print ("cprop loc %d -> ct :\n\t", sreg);
						interp_dump_ins (ins, td->data_items);
					}
				} else if (can_cprop_dreg (td, ins)) {
					int dreg_ref_count = td->var_values [dreg].ref_count;
					td->var_values [dreg] = td->var_values [sreg];
					td->var_values [dreg].ref_count = dreg_ref_count;
					td->var_values [dreg].def->dreg = dreg;

					if (td->verbose_level) {
						g_print ("cprop fixed dreg %d:\n\t", dreg);
						interp_dump_ins (td->var_values [dreg].def, td->data_items);
					}
					// Overwrite all uses of sreg with dreg up to this point
					replace_svar_uses (td, td->var_values [dreg].def->next, ins, sreg, dreg);

					// Transform `mov dreg <- sreg` into `mov sreg <- dreg` in case sreg is still used
					ins->dreg = sreg;
					ins->sregs [0] = dreg;
					td->var_values [dreg].ref_count++;
					td->var_values [sreg].ref_count--;

					td->var_values [sreg].def = ins;
					td->var_values [sreg].type = VAR_VALUE_OTHER_VAR;
					td->var_values [sreg].var = dreg;
					td->var_values [sreg].liveness = current_liveness;
					if (td->verbose_level) {
						g_print ("\t");
						interp_dump_ins (ins, td->data_items);
					}
				} else {
					if (td->verbose_level)
						g_print ("local copy %d <- %d\n", dreg, sreg);
					td->var_values [dreg].type = VAR_VALUE_OTHER_VAR;
					td->var_values [dreg].var = sreg;
				}
			} else if (opcode == MINT_LDLOCA_S) {
				// The local that we are taking the address of is not a sreg but still referenced
				td->var_values [ins->sregs [0]].ref_count++;
			} else if (MINT_IS_LDC_I4 (opcode)) {
				td->var_values [dreg].type = VAR_VALUE_I4;
				td->var_values [dreg].i = interp_get_const_from_ldc_i4 (ins);
			} else if (MINT_IS_LDC_I8 (opcode)) {
				td->var_values [dreg].type = VAR_VALUE_I8;
				td->var_values [dreg].l = interp_get_const_from_ldc_i8 (ins);
			} else if (opcode == MINT_LDC_R4) {
				guint32 val_u = READ32 (&ins->data [0]);
				float f = *(float*)(&val_u);
				td->var_values [dreg].type = VAR_VALUE_R4;
				td->var_values [dreg].f = f;
			} else if (ins->opcode == MINT_LDPTR) {
#if SIZEOF_VOID_P == 8
				td->var_values [dreg].type = VAR_VALUE_I8;
				td->var_values [dreg].l = (gint64)td->data_items [ins->data [0]];
#else
				td->var_values [dreg].type = VAR_VALUE_I4;
				td->var_values [dreg].i = (gint32)td->data_items [ins->data [0]];
#endif
			} else if (MINT_IS_UNOP (opcode)) {
				ins = interp_fold_unop (td, ins);
			} else if (MINT_IS_UNOP_CONDITIONAL_BRANCH (opcode)) {
				ins = interp_fold_unop_cond_br (td, bb, ins);
			} else if (MINT_IS_SIMD_CREATE (opcode)) {
				ins = interp_fold_simd_create (td, bb, ins);
			} else if (MINT_IS_BINOP (opcode)) {
				gboolean folded;
				ins = interp_fold_binop (td, ins, &folded);
				if (!folded) {
					int sreg = -1;
					guint16 mov_op = 0;
					InterpVarValue *vv0 = get_var_value (td, ins->sregs [0]);
					InterpVarValue *vv1 = get_var_value (td, ins->sregs [1]);
					if (vv1) {
						if ((opcode == MINT_MUL_I4 || opcode == MINT_DIV_I4) &&
								vv1->type == VAR_VALUE_I4 &&
								vv1->i == 1) {
							sreg = ins->sregs [0];
							mov_op = MINT_MOV_4;
						} else if ((opcode == MINT_MUL_I8 || opcode == MINT_DIV_I8) &&
								vv1->type == VAR_VALUE_I8 &&
								vv1->l == 1) {
							sreg = ins->sregs [0];
							mov_op = MINT_MOV_8;
						}
					} else if (vv0) {
						if (opcode == MINT_MUL_I4 &&
								vv0->type == VAR_VALUE_I4 &&
								vv0->i == 1) {
							sreg = ins->sregs [1];
							mov_op = MINT_MOV_4;
						} else if (opcode == MINT_MUL_I8 &&
								vv0->type == VAR_VALUE_I8 &&
								vv0->l == 1) {
							sreg = ins->sregs [1];
							mov_op = MINT_MOV_8;
						}
					}
					if (sreg != -1) {
						td->var_values [ins->sregs [0]].ref_count--;
						td->var_values [ins->sregs [1]].ref_count--;
						ins->opcode = mov_op;
						ins->sregs [0] = sreg;
						if (td->verbose_level) {
							g_print ("Replace idempotent binop :\n\t");
							interp_dump_ins (ins, td->data_items);
						}
						goto retry_instruction;
					}
				}
			} else if (MINT_IS_BINOP_CONDITIONAL_BRANCH (opcode)) {
				ins = interp_fold_binop_cond_br (td, bb, ins);
			} else if (MINT_IS_LDIND (opcode)) {
				InterpInst *ldloca = get_var_value_def (td, sregs [0]);
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int local = ldloca->sregs [0];
					int mt = td->vars [local].mt;
					if (mt != MINT_TYPE_VT) {
						// LDIND cannot simply be replaced with a mov because it might also include a
						// necessary conversion (especially when we do cprop and do moves between vars of
						// different types).
						int ldind_mt = interp_get_mt_for_ldind (opcode);
						switch (ldind_mt) {
							case MINT_TYPE_I1: ins->opcode = MINT_CONV_I1_I4; break;
							case MINT_TYPE_U1: ins->opcode = MINT_CONV_U1_I4; break;
							case MINT_TYPE_I2: ins->opcode = MINT_CONV_I2_I4; break;
							case MINT_TYPE_U2: ins->opcode = MINT_CONV_U2_I4; break;
							default:
								ins->opcode = GINT_TO_OPCODE (interp_get_mov_for_type (ldind_mt, FALSE));
								break;
						}
						td->var_values [sregs [0]].ref_count--;
						interp_ins_set_sreg (ins, local);

						if (td->verbose_level) {
							g_print ("Replace ldloca/ldind pair :\n\t");
							interp_dump_ins (ins, td->data_items);
						}
					}
				}
			} else if (MINT_IS_LDFLD (opcode)) {
				InterpInst *ldloca = get_var_value_def (td, sregs [0]);
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int mt = ins->opcode - MINT_LDFLD_I1;
					int local = ldloca->sregs [0];
					// Allow ldloca instruction to be killed
					td->var_values [sregs [0]].ref_count--;
					if (td->vars [local].mt == (ins->opcode - MINT_LDFLD_I1) && ins->data [0] == 0) {
						// Replace LDLOCA + LDFLD with LDLOC, when the loading field represents
						// the entire local. This is the case with loading the only field of an
						// IntPtr. We don't handle value type loads.
						ins->opcode = GINT_TO_OPCODE (interp_get_mov_for_type (mt, TRUE));
						// The dreg of the MOV is the same as the dreg of the LDFLD
						sregs [0] = local;
					} else {
						// Add mov.src.off to load directly from the local var space without use of ldloca.
						int foffset = ins->data [0];
						guint16 ldsize = 0;
						if (mt == MINT_TYPE_VT)
							ldsize = ins->data [1];

						// This loads just a part of the local valuetype
						ins = interp_insert_ins (td, ins, MINT_MOV_SRC_OFF);
						interp_ins_set_dreg (ins, ins->prev->dreg);
						interp_ins_set_sreg (ins, local);
						ins->data [0] = GINT_TO_UINT16 (foffset);
						ins->data [1] = GINT_TO_UINT16 (mt);
						if (mt == MINT_TYPE_VT)
							ins->data [2] = ldsize;

						interp_clear_ins (ins->prev);
						td->var_values [ins->dreg].def = ins;
					}

					if (td->verbose_level) {
						g_print ("Replace ldloca/ldfld pair :\n\t");
						interp_dump_ins (ins, td->data_items);
					}
				}
			} else if (opcode == MINT_INITOBJ) {
				InterpInst *ldloca = get_var_value_def (td, sregs [0]);
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int size = ins->data [0];
					int local = ldloca->sregs [0];
					// Replace LDLOCA + INITOBJ with or LDC
					if (size <= 4)
						ins->opcode = MINT_LDC_I4_0;
					else if (size <= 8)
						ins->opcode = MINT_LDC_I8_0;
					else
						ins->opcode = MINT_INITLOCAL;
					td->var_values [sregs [0]].ref_count--;
					ins->dreg = local;

					if (td->verbose_level) {
						g_print ("Replace ldloca/initobj pair :\n\t");
						interp_dump_ins (ins, td->data_items);
					}
				}
			} else if (opcode == MINT_LDOBJ_VT) {
				InterpInst *ldloca = get_var_value_def (td, sregs [0]);
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int ldsize = ins->data [0];
					int local = ldloca->sregs [0];
					td->var_values [sregs [0]].ref_count--;

					if (ldsize == td->vars [local].size) {
						// Replace LDLOCA + LDOBJ_VT with MOV_VT
						ins->opcode = MINT_MOV_VT;
						sregs [0] = local;
					} else {
						// This loads just a part of the local valuetype
						ins = interp_insert_ins (td, ins, MINT_MOV_SRC_OFF);
						interp_ins_set_dreg (ins, ins->prev->dreg);
						interp_ins_set_sreg (ins, local);
						ins->data [0] = 0;
						ins->data [1] = MINT_TYPE_VT;
						ins->data [2] = GINT_TO_UINT16 (ldsize);

						interp_clear_ins (ins->prev);
					}
					if (td->verbose_level) {
						g_print ("Replace ldloca/ldobj_vt pair :\n\t");
						interp_dump_ins (ins, td->data_items);
					}
				}
			} else if (opcode == MINT_STOBJ_VT || opcode == MINT_STOBJ_VT_NOREF) {
				InterpInst *ldloca = get_var_value_def (td, sregs [0]);
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int stsize = ins->data [0];
					int local = ldloca->sregs [0];

					if (stsize == td->vars [local].size) {
						// Replace LDLOCA + STOBJ_VT with MOV_VT
						td->var_values [sregs [0]].ref_count--;
						ins->opcode = MINT_MOV_VT;
						sregs [0] = sregs [1];
						ins->dreg = local;

						if (td->verbose_level) {
							g_print ("Replace ldloca/stobj_vt pair :\n\t");
							interp_dump_ins (ins, td->data_items);
						}
					}
				}
			} else if (MINT_IS_STIND (opcode)) {
				InterpInst *ldloca = get_var_value_def (td, sregs [0]);
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int local = ldloca->sregs [0];
					int mt = td->vars [local].mt;
					if (mt != MINT_TYPE_VT) {
						// We have an 8 byte local, just replace the stind with a mov
						td->var_values [sregs [0]].ref_count--;
						// We make the assumption that the STIND matches the local type
						ins->opcode = GINT_TO_OPCODE (interp_get_mov_for_type (mt, TRUE));
						interp_ins_set_dreg (ins, local);
						interp_ins_set_sreg (ins, sregs [1]);

						if (td->verbose_level) {
							g_print ("Replace ldloca/stind pair :\n\t");
							interp_dump_ins (ins, td->data_items);
						}
					}
				}
			} else if (MINT_IS_STFLD (opcode)) {
				InterpInst *ldloca = get_var_value_def (td, sregs [0]);
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int mt = ins->opcode - MINT_STFLD_I1;
					int local = ldloca->sregs [0];
					td->var_values [sregs [0]].ref_count--;
					// Allow ldloca instruction to be killed
					if (td->vars [local].mt == (ins->opcode - MINT_STFLD_I1) && ins->data [0] == 0) {
						ins->opcode = GINT_TO_OPCODE (interp_get_mov_for_type (mt, FALSE));
						// The sreg of the MOV is the same as the second sreg of the STFLD
						ins->dreg = local;
						sregs [0] = sregs [1];
					} else {
						// Add mov.dst.off to store directly int the local var space without use of ldloca.
						int foffset = ins->data [0];
						guint16 vtsize = 0;
						if (mt == MINT_TYPE_VT) {
							vtsize = ins->data [1];
						}
#ifdef NO_UNALIGNED_ACCESS
						else {
							// As with normal loads/stores we use memcpy for unaligned 8 byte accesses
							if ((mt == MINT_TYPE_I8 || mt == MINT_TYPE_R8) && foffset % SIZEOF_VOID_P != 0)
								vtsize = 8;
						}
#endif

						// This stores just to part of the dest valuetype
						ins = interp_insert_ins (td, ins, MINT_MOV_DST_OFF);
						interp_ins_set_dreg (ins, local);
						interp_ins_set_sregs2 (ins, sregs [1], local);
						ins->data [0] = GINT_TO_UINT16 (foffset);
						ins->data [1] = GINT_TO_UINT16 (mt);
						ins->data [2] = vtsize;

						interp_clear_ins (ins->prev);

						// MINT_MOV_DST_OFF doesn't work if dreg is allocated at the same location as the
						// field value to be stored, because its behavior is not atomic in nature. We first
						// copy the original whole vt, potentially overwritting the new field value.
						ins = interp_insert_ins (td, ins, MINT_DUMMY_USE);
						interp_ins_set_sreg (ins, sregs [1]);
						td->var_values [sregs [1]].ref_count++;
					}
					if (td->verbose_level) {
						g_print ("Replace ldloca/stfld pair (off %p) :\n\t", (void *)(uintptr_t) ldloca->il_offset);
						interp_dump_ins (ins, td->data_items);
					}
				}
			} else if (opcode == MINT_GETITEM_SPAN) {
				InterpInst *ldloca = get_var_value_def (td, sregs [0]);
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int local = ldloca->sregs [0];
					// Allow ldloca instruction to be killed
					td->var_values [sregs [0]].ref_count--;
					// Instead of loading from the indirect pointer pass directly the vt var
					ins->opcode = MINT_GETITEM_LOCALSPAN;
					sregs [0] = local;
				}
			} else if (opcode == MINT_CKNULL) {
				InterpInst *def = get_var_value_def (td, sregs [0]);
				if (def && def->opcode == MINT_LDLOCA_S) {
					// CKNULL on LDLOCA is a NOP
					ins->opcode = MINT_MOV_P;
					td->var_values [ins->sregs [0]].ref_count--;
					goto retry_instruction;
				}
			} else if (opcode == MINT_BOX) {
				// TODO Add more relevant opcodes
				td->var_values [dreg].type = VAR_VALUE_NON_NULL;
			}
		}
	}
}

void
mono_test_interp_cprop (TransformData *td)
{
	interp_cprop (td);
}

static gboolean
get_sreg_imm (TransformData *td, int sreg, gint16 *imm, int result_mt)
{
	if (var_has_indirects (td, sreg))
		return FALSE;
	InterpInst *def = get_var_value_def (td, sreg);
	if (!def)
		return FALSE;
	InterpVarValue *sreg_val = &td->var_values [sreg];
	if (sreg_val->ref_count == 1) {
		gint64 ct;
		if (MINT_IS_LDC_I4 (def->opcode))
			ct = interp_get_const_from_ldc_i4 (def);
		else if (MINT_IS_LDC_I8 (def->opcode))
			ct = interp_get_const_from_ldc_i8 (def);
		else
			return FALSE;
		gint64 min_val, max_val;
		// We only propagate the immediate only if it fits into the desired type,
		// so we don't accidentaly handle conversions wrong
		switch (result_mt) {
			case MINT_TYPE_I1:
				min_val = G_MININT8;
				max_val = G_MAXINT8;
				break;
			case MINT_TYPE_I2:
				min_val = G_MININT16;
				max_val = G_MAXINT16;
				break;
			case MINT_TYPE_U1:
				min_val = 0;
				max_val = G_MAXUINT8;
				break;
			case MINT_TYPE_U2:
				min_val = 0;
				max_val = G_MAXINT16;
				break;
			default:
				g_assert_not_reached ();

		}
		if (ct >= min_val && ct <= max_val) {
			*imm = (gint16)ct;
			return TRUE;
		}
	}
	return FALSE;
}

static int
get_binop_condbr_imm_sp (int opcode)
{
	switch (opcode) {
		case MINT_BEQ_I4: return MINT_BEQ_I4_IMM_SP;
		case MINT_BEQ_I8: return MINT_BEQ_I8_IMM_SP;
		case MINT_BGE_I4: return MINT_BGE_I4_IMM_SP;
		case MINT_BGE_I8: return MINT_BGE_I8_IMM_SP;
		case MINT_BGT_I4: return MINT_BGT_I4_IMM_SP;
		case MINT_BGT_I8: return MINT_BGT_I8_IMM_SP;
		case MINT_BLT_I4: return MINT_BLT_I4_IMM_SP;
		case MINT_BLT_I8: return MINT_BLT_I8_IMM_SP;
		case MINT_BLE_I4: return MINT_BLE_I4_IMM_SP;
		case MINT_BLE_I8: return MINT_BLE_I8_IMM_SP;
		case MINT_BNE_UN_I4: return MINT_BNE_UN_I4_IMM_SP;
		case MINT_BNE_UN_I8: return MINT_BNE_UN_I8_IMM_SP;
		case MINT_BGE_UN_I4: return MINT_BGE_UN_I4_IMM_SP;
		case MINT_BGE_UN_I8: return MINT_BGE_UN_I8_IMM_SP;
		case MINT_BGT_UN_I4: return MINT_BGT_UN_I4_IMM_SP;
		case MINT_BGT_UN_I8: return MINT_BGT_UN_I8_IMM_SP;
		case MINT_BLE_UN_I4: return MINT_BLE_UN_I4_IMM_SP;
		case MINT_BLE_UN_I8: return MINT_BLE_UN_I8_IMM_SP;
		case MINT_BLT_UN_I4: return MINT_BLT_UN_I4_IMM_SP;
		case MINT_BLT_UN_I8: return MINT_BLT_UN_I8_IMM_SP;
		default: return MINT_NOP;
	}
}

static int
get_binop_condbr_sp (int opcode)
{
	switch (opcode) {
		case MINT_BEQ_I4: return MINT_BEQ_I4_SP;
		case MINT_BEQ_I8: return MINT_BEQ_I8_SP;
		case MINT_BGE_I4: return MINT_BGE_I4_SP;
		case MINT_BGE_I8: return MINT_BGE_I8_SP;
		case MINT_BGT_I4: return MINT_BGT_I4_SP;
		case MINT_BGT_I8: return MINT_BGT_I8_SP;
		case MINT_BLT_I4: return MINT_BLT_I4_SP;
		case MINT_BLT_I8: return MINT_BLT_I8_SP;
		case MINT_BLE_I4: return MINT_BLE_I4_SP;
		case MINT_BLE_I8: return MINT_BLE_I8_SP;
		case MINT_BNE_UN_I4: return MINT_BNE_UN_I4_SP;
		case MINT_BNE_UN_I8: return MINT_BNE_UN_I8_SP;
		case MINT_BGE_UN_I4: return MINT_BGE_UN_I4_SP;
		case MINT_BGE_UN_I8: return MINT_BGE_UN_I8_SP;
		case MINT_BGT_UN_I4: return MINT_BGT_UN_I4_SP;
		case MINT_BGT_UN_I8: return MINT_BGT_UN_I8_SP;
		case MINT_BLE_UN_I4: return MINT_BLE_UN_I4_SP;
		case MINT_BLE_UN_I8: return MINT_BLE_UN_I8_SP;
		case MINT_BLT_UN_I4: return MINT_BLT_UN_I4_SP;
		case MINT_BLT_UN_I8: return MINT_BLT_UN_I8_SP;
		default: return MINT_NOP;
	}
}

static int
get_unop_condbr_sp (int opcode)
{
	switch (opcode) {
		case MINT_BRFALSE_I4: return MINT_BRFALSE_I4_SP;
		case MINT_BRFALSE_I8: return MINT_BRFALSE_I8_SP;
		case MINT_BRTRUE_I4: return MINT_BRTRUE_I4_SP;
		case MINT_BRTRUE_I8: return MINT_BRTRUE_I8_SP;
		default: return MINT_NOP;
	}
}

static void
interp_super_instructions (TransformData *td)
{
	interp_compute_native_offset_estimates (td);

	// Add some actual super instructions
	for (int bb_dfs_index = 0; bb_dfs_index < td->bblocks_count_eh; bb_dfs_index++) {
		InterpBasicBlock *bb = td->bblocks [bb_dfs_index];

		// Set cbb since we do some instruction inserting below
		td->cbb = bb;
		int noe = bb->native_offset_estimate;
		for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
			int opcode = ins->opcode;
			if (MINT_IS_NOP (opcode))
				continue;

			if (mono_interp_op_dregs [opcode] && !var_is_ssa_form (td, ins->dreg) && !var_has_indirects (td, ins->dreg)) {
				InterpVarValue *dval = &td->var_values [ins->dreg];
				dval->type = VAR_VALUE_NONE;
				dval->def = ins;
				dval->liveness.bb_index = bb->index; // only to check if defined in current bblock
			}
			if (opcode == MINT_RET || (opcode >= MINT_RET_I1 && opcode <= MINT_RET_U2)) {
				// ldc + ret -> ret.imm
				int sreg = ins->sregs [0];
				gint16 imm;
				if (get_sreg_imm (td, sreg, &imm, (opcode == MINT_RET) ? MINT_TYPE_I2 : opcode - MINT_RET_I1)) {
					InterpInst *def = td->var_values [sreg].def;
					int ret_op = MINT_IS_LDC_I4 (def->opcode) ? MINT_RET_I4_IMM : MINT_RET_I8_IMM;
					InterpInst *new_inst = interp_insert_ins (td, ins, ret_op);
					new_inst->data [0] = imm;
					interp_clear_ins (def);
					interp_clear_ins (ins);
					td->var_values [sreg].ref_count--; // 0
					if (td->verbose_level) {
						g_print ("superins: ");
						interp_dump_ins (new_inst, td->data_items);
					}
				}
			} else if (opcode == MINT_ADD_I4 || opcode == MINT_ADD_I8 ||
					opcode == MINT_MUL_I4 || opcode == MINT_MUL_I8) {
				int sreg = -1;
				int sreg_imm = -1;
				gint16 imm;
				if (get_sreg_imm (td, ins->sregs [0], &imm, MINT_TYPE_I2)) {
					sreg = ins->sregs [1];
					sreg_imm = ins->sregs [0];
				} else if (get_sreg_imm (td, ins->sregs [1], &imm, MINT_TYPE_I2)) {
					sreg = ins->sregs [0];
					sreg_imm = ins->sregs [1];
				}
				if (sreg != -1) {
					int binop;
					switch (opcode) {
						case MINT_ADD_I4: binop = MINT_ADD_I4_IMM; break;
						case MINT_ADD_I8: binop = MINT_ADD_I8_IMM; break;
						case MINT_MUL_I4: binop = MINT_MUL_I4_IMM; break;
						case MINT_MUL_I8: binop = MINT_MUL_I8_IMM; break;
						default: g_assert_not_reached ();
					}
					InterpInst *new_inst = interp_insert_ins (td, ins, binop);
					new_inst->dreg = ins->dreg;
					new_inst->sregs [0] = sreg;
					new_inst->data [0] = imm;
					interp_clear_ins (td->var_values [sreg_imm].def);
					interp_clear_ins (ins);
					td->var_values [sreg_imm].ref_count--; // 0
					td->var_values [new_inst->dreg].def = new_inst;
					if (td->verbose_level) {
						g_print ("superins: ");
						interp_dump_ins (new_inst, td->data_items);
					}
				}
			} else if (opcode == MINT_SUB_I4 || opcode == MINT_SUB_I8) {
				// ldc + sub -> add.-imm
				gint16 imm;
				int sreg_imm = ins->sregs [1];
				if (get_sreg_imm (td, sreg_imm, &imm, MINT_TYPE_I2) && imm != G_MININT16) {
					int add_op = opcode == MINT_SUB_I4 ? MINT_ADD_I4_IMM : MINT_ADD_I8_IMM;
					InterpInst *new_inst = interp_insert_ins (td, ins, add_op);
					new_inst->dreg = ins->dreg;
					new_inst->sregs [0] = ins->sregs [0];
					new_inst->data [0] = -imm;
					interp_clear_ins (td->var_values [sreg_imm].def);
					interp_clear_ins (ins);
					td->var_values [sreg_imm].ref_count--; // 0
					td->var_values [new_inst->dreg].def = new_inst;
					if (td->verbose_level) {
						g_print ("superins: ");
						interp_dump_ins (new_inst, td->data_items);
					}
				}
			} else if (opcode == MINT_MUL_I4_IMM || opcode == MINT_MUL_I8_IMM) {
				int sreg = ins->sregs [0];
				InterpInst *def = get_var_value_def (td, sreg);
				if (def != NULL && td->var_values [sreg].ref_count == 1) {
					gboolean is_i4 = opcode == MINT_MUL_I4_IMM;
					if ((is_i4 && def->opcode == MINT_ADD_I4_IMM) ||
							(!is_i4 && def->opcode == MINT_ADD_I8_IMM)) {
						InterpInst *new_inst = interp_insert_ins (td, ins, is_i4 ? MINT_ADD_MUL_I4_IMM : MINT_ADD_MUL_I8_IMM);
						new_inst->dreg = ins->dreg;
						new_inst->sregs [0] = def->sregs [0];
						new_inst->data [0] = def->data [0];
						new_inst->data [1] = ins->data [0];
						interp_clear_ins (def);
						interp_clear_ins (ins);
						td->var_values [sreg].ref_count--; // 0
						td->var_values [new_inst->dreg].def = new_inst;
						if (td->verbose_level) {
							g_print ("superins: ");
							interp_dump_ins (new_inst, td->data_items);
						}
					}
				}
			} else if (MINT_IS_BINOP_SHIFT (opcode)) {
				gint16 imm;
				int sreg_imm = ins->sregs [1];
				if (get_sreg_imm (td, sreg_imm, &imm, MINT_TYPE_I2)) {
					// ldc + sh -> sh.imm
					int shift_op = MINT_SHR_UN_I4_IMM + (opcode - MINT_SHR_UN_I4);
					InterpInst *new_inst = interp_insert_ins (td, ins, shift_op);
					new_inst->dreg = ins->dreg;
					new_inst->sregs [0] = ins->sregs [0];
					new_inst->data [0] = imm;
					interp_clear_ins (td->var_values [sreg_imm].def);
					interp_clear_ins (ins);
					td->var_values [sreg_imm].ref_count--; // 0
					td->var_values [new_inst->dreg].def = new_inst;
					if (td->verbose_level) {
						g_print ("superins: ");
						interp_dump_ins (new_inst, td->data_items);
					}
				} else if (opcode == MINT_SHL_I4 || opcode == MINT_SHL_I8) {
					int amount_var = ins->sregs [1];
					InterpInst *amount_def = get_var_value_def (td, amount_var);
					if (amount_def != NULL && td->var_values [amount_var].ref_count == 1 && amount_def->opcode == MINT_AND_I4) {
						int mask_var = amount_def->sregs [1];
						if (get_sreg_imm (td, mask_var, &imm, MINT_TYPE_I2)) {
							// ldc + and + shl -> shl_and_imm
							int new_opcode = -1;
							if (opcode == MINT_SHL_I4 && imm == 31)
								new_opcode = MINT_SHL_AND_I4;
							else if (opcode == MINT_SHL_I8 && imm == 63)
								new_opcode = MINT_SHL_AND_I8;

							if (new_opcode != -1) {
								InterpInst *new_inst = interp_insert_ins (td, ins, new_opcode);
								new_inst->dreg = ins->dreg;
								new_inst->sregs [0] = ins->sregs [0];
								new_inst->sregs [1] = amount_def->sregs [0];

								td->var_values [amount_var].ref_count--; // 0
								td->var_values [mask_var].ref_count--; // 0
								td->var_values [new_inst->dreg].def = new_inst;

								interp_clear_ins (td->var_values [mask_var].def);
								interp_clear_ins (amount_def);
								interp_clear_ins (ins);
								if (td->verbose_level) {
									g_print ("superins: ");
									interp_dump_ins (new_inst, td->data_items);
								}
							}
						}
					}
				}
			} else if (opcode == MINT_DIV_UN_I4 || opcode == MINT_DIV_UN_I8) {
				// ldc + div.un -> shr.imm
				int sreg_imm = ins->sregs [1];
				InterpInst *def = get_var_value_def (td, sreg_imm);
				if (def != NULL && td->var_values [sreg_imm].ref_count == 1) {
					int power2 = -1;
					if (MINT_IS_LDC_I4 (def->opcode)) {
						guint32 ct = interp_get_const_from_ldc_i4 (def);
						power2 = mono_is_power_of_two ((guint32)ct);
					} else if (MINT_IS_LDC_I8 (def->opcode)) {
						guint64 ct = interp_get_const_from_ldc_i8 (def);
						if (ct < G_MAXUINT32)
							power2 = mono_is_power_of_two ((guint32)ct);
					}
					if (power2 > 0) {
						InterpInst *new_inst;
						if (opcode == MINT_DIV_UN_I4)
							new_inst = interp_insert_ins (td, ins, MINT_SHR_UN_I4_IMM);
						else
							new_inst = interp_insert_ins (td, ins, MINT_SHR_UN_I8_IMM);
						new_inst->dreg = ins->dreg;
						new_inst->sregs [0] = ins->sregs [0];
						new_inst->data [0] = GINT_TO_UINT16 (power2);

						interp_clear_ins (def);
						interp_clear_ins (ins);
						td->var_values [sreg_imm].ref_count--;
						td->var_values [new_inst->dreg].def = new_inst;
						if (td->verbose_level) {
							g_print ("lower div.un: ");
							interp_dump_ins (new_inst, td->data_items);
						}
					}
				}
			} else if (MINT_IS_LDIND_INT (opcode)) {
				int sreg_base = ins->sregs [0];
				InterpInst *def = get_var_value_def (td, sreg_base);
				if (def != NULL && td->var_values [sreg_base].ref_count == 1) {
					InterpInst *new_inst = NULL;
					if (def->opcode == MINT_ADD_P) {
						int ldind_offset_op = MINT_LDIND_OFFSET_I1 + (opcode - MINT_LDIND_I1);
						new_inst = interp_insert_ins (td, ins, ldind_offset_op);
						new_inst->dreg = ins->dreg;
						new_inst->sregs [0] = def->sregs [0]; // base
						new_inst->sregs [1] = def->sregs [1]; // off
					} else if (def->opcode == MINT_ADD_P_IMM) {
						int ldind_offset_imm_op = MINT_LDIND_OFFSET_IMM_I1 + (opcode - MINT_LDIND_I1);
						new_inst = interp_insert_ins (td, ins, ldind_offset_imm_op);
						new_inst->dreg = ins->dreg;
						new_inst->sregs [0] = def->sregs [0]; // base
						new_inst->data [0] = def->data [0];   // imm value
					}
					if (new_inst) {
						interp_clear_ins (def);
						interp_clear_ins (ins);
						td->var_values [sreg_base].ref_count--;
						td->var_values [new_inst->dreg].def = new_inst;
						if (td->verbose_level) {
							g_print ("superins: ");
							interp_dump_ins (new_inst, td->data_items);
						}
					}
				}
			} else if (MINT_IS_LDIND_OFFSET (opcode)) {
				int sreg_off = ins->sregs [1];
				InterpInst *def = get_var_value_def (td, sreg_off);
				if (def != NULL && td->var_values [sreg_off].ref_count == 1) {
					if (def->opcode == MINT_MUL_P_IMM || def->opcode == MINT_ADD_P_IMM || def->opcode == MINT_ADD_MUL_P_IMM) {
						int ldind_offset_op = MINT_LDIND_OFFSET_ADD_MUL_IMM_I1 + (opcode - MINT_LDIND_OFFSET_I1);
						InterpInst *new_inst = interp_insert_ins (td, ins, ldind_offset_op);
						new_inst->dreg = ins->dreg;
						new_inst->sregs [0] = ins->sregs [0]; // base
						new_inst->sregs [1] = def->sregs [0]; // off

						// set the add and mul immediates
						switch (def->opcode) {
							case MINT_ADD_P_IMM:
								new_inst->data [0] = def->data [0];
								new_inst->data [1] = 1;
								break;
							case MINT_MUL_P_IMM:
								new_inst->data [0] = 0;
								new_inst->data [1] = def->data [0];
								break;
							case MINT_ADD_MUL_P_IMM:
								new_inst->data [0] = def->data [0];
								new_inst->data [1] = def->data [1];
								break;
						}

						interp_clear_ins (def);
						interp_clear_ins (ins);
						td->var_values [sreg_off].ref_count--; // 0
						td->var_values [new_inst->dreg].def = new_inst;
						if (td->verbose_level) {
							g_print ("superins: ");
							interp_dump_ins (new_inst, td->data_items);
						}
					}
				}
			} else if (MINT_IS_STIND_INT (opcode)) {
				int sreg_base = ins->sregs [0];
				InterpInst *def = get_var_value_def (td, sreg_base);
				if (def != NULL && td->var_values [sreg_base].ref_count == 1) {
					InterpInst *new_inst = NULL;
					if (def->opcode == MINT_ADD_P) {
						int stind_offset_op = MINT_STIND_OFFSET_I1 + (opcode - MINT_STIND_I1);
						new_inst = interp_insert_ins (td, ins, stind_offset_op);
						new_inst->sregs [0] = def->sregs [0]; // base
						new_inst->sregs [1] = def->sregs [1]; // off
						new_inst->sregs [2] = ins->sregs [1]; // value
					} else if (def->opcode == MINT_ADD_P_IMM) {
						int stind_offset_imm_op = MINT_STIND_OFFSET_IMM_I1 + (opcode - MINT_STIND_I1);
						new_inst = interp_insert_ins (td, ins, stind_offset_imm_op);
						new_inst->sregs [0] = def->sregs [0]; // base
						new_inst->sregs [1] = ins->sregs [1]; // value
						new_inst->data [0] = def->data [0];   // imm value
					}
					if (new_inst) {
						interp_clear_ins (def);
						interp_clear_ins (ins);
						td->var_values [sreg_base].ref_count--;
						if (td->verbose_level) {
							g_print ("superins: ");
							interp_dump_ins (new_inst, td->data_items);
						}
					}
				}
			} else if (MINT_IS_LDFLD (opcode)) {
				// cknull + ldfld -> ldfld
				// FIXME This optimization is very limited, it is meant mainly to remove cknull
				// when inlining property accessors. We should have more advanced cknull removal
				// optimzations, so we can catch cases where instructions are not next to each other.
				int obj_sreg = ins->sregs [0];
				InterpInst *def = get_var_value_def (td, obj_sreg);
				if (def != NULL && def->opcode == MINT_CKNULL && interp_prev_ins (ins) == def &&
						def->dreg == obj_sreg && td->var_values [obj_sreg].ref_count == 1) {
					if (td->verbose_level) {
						g_print ("remove redundant cknull: ");
						interp_dump_ins (def, td->data_items);
					}
					ins->sregs [0] = def->sregs [0];
					interp_clear_ins (def);
					td->var_values [obj_sreg].ref_count--;
				}
			} else if (MINT_IS_BINOP_CONDITIONAL_BRANCH (opcode) && interp_is_short_offset (noe, ins->info.target_bb->native_offset_estimate)) {
				gint16 imm;
				int sreg_imm = ins->sregs [1];
				if (get_sreg_imm (td, sreg_imm, &imm, MINT_TYPE_I2)) {
					int condbr_op = get_binop_condbr_imm_sp (opcode);
					if (condbr_op != MINT_NOP) {
						InterpInst *prev_ins = interp_prev_ins (ins);
						// The new instruction does a safepoint
						if (prev_ins && prev_ins->opcode == MINT_SAFEPOINT)
							interp_clear_ins (prev_ins);
						InterpInst *new_ins = interp_insert_ins (td, ins, condbr_op);
						new_ins->sregs [0] = ins->sregs [0];
						new_ins->data [0] = imm;
						new_ins->info.target_bb = ins->info.target_bb;
						interp_clear_ins (td->var_values [sreg_imm].def);
						interp_clear_ins (ins);
						td->var_values [sreg_imm].ref_count--; // 0
						if (td->verbose_level) {
							g_print ("superins: ");
							interp_dump_ins (new_ins, td->data_items);
						}
					}
				} else {
					InterpInst *prev_ins = interp_prev_ins (ins);
					if (prev_ins && prev_ins->opcode == MINT_SAFEPOINT) {
						int condbr_op = get_binop_condbr_sp (opcode);
						if (condbr_op != MINT_NOP) {
							interp_clear_ins (prev_ins);
							ins->opcode = GINT_TO_OPCODE (condbr_op);
							if (td->verbose_level) {
								g_print ("superins: ");
								interp_dump_ins (ins, td->data_items);
							}
						}
					}
				}
			} else if (MINT_IS_UNOP_CONDITIONAL_BRANCH (opcode) && interp_is_short_offset (noe, ins->info.target_bb->native_offset_estimate)) {
				if (opcode == MINT_BRFALSE_I4 || opcode == MINT_BRTRUE_I4) {
					gboolean negate = opcode == MINT_BRFALSE_I4;
					int cond_sreg = ins->sregs [0];
					InterpInst *def = get_var_value_def (td, cond_sreg);
					if (def != NULL && td->var_values [cond_sreg].ref_count == 1) {
						int replace_opcode = -1;
						switch (def->opcode) {
							case MINT_CEQ_I4: replace_opcode = negate ? MINT_BNE_UN_I4 : MINT_BEQ_I4; break;
							case MINT_CEQ_I8: replace_opcode = negate ? MINT_BNE_UN_I8 : MINT_BEQ_I8; break;
							case MINT_CGT_I4: replace_opcode = negate ? MINT_BLE_I4 : MINT_BGT_I4; break;
							case MINT_CGT_I8: replace_opcode = negate ? MINT_BLE_I8 : MINT_BGT_I8; break;
							case MINT_CLT_I4: replace_opcode = negate ? MINT_BGE_I4 : MINT_BLT_I4; break;
							case MINT_CLT_I8: replace_opcode = negate ? MINT_BGE_I8 : MINT_BLT_I8; break;
							case MINT_CGT_UN_I4: replace_opcode = negate ? MINT_BLE_UN_I4 : MINT_BGT_UN_I4; break;
							case MINT_CGT_UN_I8: replace_opcode = negate ? MINT_BLE_UN_I8 : MINT_BGT_UN_I8; break;
							case MINT_CLT_UN_I4: replace_opcode = negate ? MINT_BGE_UN_I4 : MINT_BLT_UN_I4; break;
							case MINT_CLT_UN_I8: replace_opcode = negate ? MINT_BGE_UN_I8 : MINT_BLT_UN_I8; break;
							case MINT_CEQ_R4: replace_opcode = negate ? MINT_BNE_UN_R4 : MINT_BEQ_R4; break;
							case MINT_CEQ_R8: replace_opcode = negate ? MINT_BNE_UN_R8 : MINT_BEQ_R8; break;
							case MINT_CGT_R4: replace_opcode = negate ? MINT_BLE_UN_R4 : MINT_BGT_R4; break;
							case MINT_CGT_R8: replace_opcode = negate ? MINT_BLE_UN_R8 : MINT_BGT_R8; break;
							case MINT_CLT_R4: replace_opcode = negate ? MINT_BGE_UN_R4 : MINT_BLT_R4; break;
							case MINT_CLT_R8: replace_opcode = negate ? MINT_BGE_UN_R8 : MINT_BLT_R8; break;
							case MINT_CGT_UN_R4: replace_opcode = negate ? MINT_BLE_R4 : MINT_BGT_UN_R4; break;
							case MINT_CGT_UN_R8: replace_opcode = negate ? MINT_BLE_R8 : MINT_BGT_UN_R8; break;
							case MINT_CLT_UN_R4: replace_opcode = negate ? MINT_BGE_R4 : MINT_BLT_UN_R4; break;
							case MINT_CLT_UN_R8: replace_opcode = negate ? MINT_BGE_R8 : MINT_BLT_UN_R8; break;
							case MINT_CEQ0_I4: replace_opcode = negate ? MINT_BRTRUE_I4 : MINT_BRFALSE_I4; break; // If def->opcode is MINT_CEQ0_I4 ins->opcode is inverted
							// Add more opcodes
							default:
								break;
						}
						if (replace_opcode != -1) {
							ins->opcode = GINT_TO_UINT16 (replace_opcode);
							ins->sregs [0] = def->sregs [0];
							if (def->opcode != MINT_CEQ0_I4)
								ins->sregs [1] = def->sregs [1];
							interp_clear_ins (def);
							td->var_values [cond_sreg].ref_count--;
							if (td->verbose_level) {
								g_print ("superins: ");
								interp_dump_ins (ins, td->data_items);
							}
							// The newly added opcode could be part of further superinstructions. Retry
							ins = ins->prev;
							continue;
						}
					}
				}
				InterpInst *prev_ins = interp_prev_ins (ins);
				if (prev_ins && prev_ins->opcode == MINT_SAFEPOINT) {
					int condbr_op = get_unop_condbr_sp (opcode);
					if (condbr_op != MINT_NOP) {
						interp_clear_ins (prev_ins);
						ins->opcode = GINT_TO_OPCODE (condbr_op);
						if (td->verbose_level) {
							g_print ("superins: ");
							interp_dump_ins (ins, td->data_items);
						}
					}
				}
			} else if (opcode == MINT_STOBJ_VT_NOREF) {
				int sreg_src = ins->sregs [1];
				InterpInst *def = get_var_value_def (td, sreg_src);
				if (def != NULL && interp_prev_ins (ins) == def && def->opcode == MINT_LDOBJ_VT && ins->data [0] == def->data [0] && td->var_values [sreg_src].ref_count == 1) {
					InterpInst *new_inst = interp_insert_ins (td, ins, MINT_CPOBJ_VT_NOREF);
					new_inst->sregs [0] = ins->sregs [0]; // dst
					new_inst->sregs [1] = def->sregs [0]; // src
					new_inst->data [0] = ins->data [0];   // size

					interp_clear_ins (def);
					interp_clear_ins (ins);
					td->var_values [sreg_src].ref_count--;
					if (td->verbose_level) {
						g_print ("superins: ");
						interp_dump_ins (new_inst, td->data_items);
					}
				}
			} else if (opcode == MINT_MOV_4 || opcode == MINT_MOV_8 || opcode == MINT_MOV_VT) {
				int sreg = ins->sregs [0];
				InterpInst *def = get_var_value_def (td, sreg);
				if (def && td->var_values [sreg].ref_count == 1) {
					// The svar is used only for this mov. Try to get the definition to store directly instead
					if (def->opcode != MINT_DEF_ARG && def->opcode != MINT_PHI && def->opcode != MINT_DEF_TIER_VAR &&
							!(def->flags & INTERP_INST_FLAG_PROTECTED_NEWOBJ)) {
						int dreg = ins->dreg;
						// if var is not ssa or it is a renamed fixed, then we can't replace the dreg
						// since there can be conflicting liveness, unless the instructions are adjacent
						if ((var_is_ssa_form (td, dreg) && !td->vars [dreg].renamed_ssa_fixed) ||
								interp_prev_ins (ins) == def) {
							def->dreg = dreg;

							// Copy var value, while keeping the ref count intact
							int dreg_ref_count = td->var_values [dreg].ref_count;
							td->var_values [dreg] = td->var_values [sreg];
							td->var_values [dreg].ref_count = dreg_ref_count;

							// clear the move
							td->var_values [sreg].ref_count--; // 0
							interp_clear_ins (ins);

							if (td->verbose_level) {
								g_print ("forward dreg: ");
								interp_dump_ins (def, td->data_items);
							}
						}
					}
				}
			}
			noe += interp_get_ins_length (ins);
		}
	}
}

static void
interp_prepare_no_ssa_opt (TransformData *td)
{
	for (unsigned int i = 0; i < td->vars_size; i++) {
		td->vars [i].no_ssa = TRUE;
		td->vars [i].has_indirects = (td->vars [i].indirects > 0) ? TRUE : FALSE;
	}

	if (!td->bblocks)
		td->bblocks = (InterpBasicBlock**)mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock*) * td->bb_count);

	int i = 0;
	for (InterpBasicBlock *bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		td->bblocks [i] = bb;
		i++;
	}
	td->bblocks_count = 0;
	td->bblocks_count_eh = i;
}

static void
interp_remove_ins (InterpBasicBlock *bb, InterpInst *ins)
{
	if (ins->next)
		ins->next->prev = ins->prev;
	else
		bb->last_ins = ins->prev;

	if (ins->prev)
		ins->prev->next = ins->next;
	else
		bb->first_ins = ins->next;
}

static void
interp_remove_nops (TransformData *td)
{
	InterpBasicBlock *bb;
	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins;
		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			if (ins->opcode == MINT_NOP && ins->prev &&
					(ins->il_offset == -1 ||
					ins->prev->il_offset == ins->il_offset)) {
				// This is a NOP instruction that has no relevant il_offset, actually remove it
				interp_remove_ins (bb, ins);
			}

		}
	}
}

void
interp_optimize_code (TransformData *td)
{
	if (mono_interp_opt & INTERP_OPT_BBLOCKS)
		MONO_TIME_TRACK (mono_interp_stats.optimize_bblocks_time, interp_optimize_bblocks (td));

	if (!(mono_interp_opt & INTERP_OPT_SSA))
		td->disable_ssa = TRUE;

	gboolean ssa_enabled_retry = FALSE;

	if (!td->disable_ssa && td->bb_count > 1000) {
		// We have ssa enabled but we are compiling a huge method. Do the first iteration
		// in ssa disabled mode. This should greatly simplify the CFG and the code, so the
		// following iteration with SSA transformation enabled is much faster. In general,
		// for huge methods we end up doing multiple optimization iterations anyway.
		ssa_enabled_retry = TRUE;
		td->disable_ssa = TRUE;
		if (td->verbose_level)
			g_print ("Huge method. SSA disabled for first iteration\n");
	}
optimization_retry:
	td->need_optimization_retry = FALSE;

	if (td->disable_ssa)
		interp_prepare_no_ssa_opt (td);
	else
		MONO_TIME_TRACK (mono_interp_stats.ssa_compute_time, interp_compute_ssa (td));

	if (mono_interp_opt & INTERP_OPT_CPROP)
		MONO_TIME_TRACK (mono_interp_stats.cprop_time, interp_cprop (td));

	interp_var_deadce (td);

	// We run this after var deadce to detect more single use vars. This pass will clear
	// unnecessary instruction on the fly so deadce is no longer needed to run.
	if ((mono_interp_opt & INTERP_OPT_SUPER_INSTRUCTIONS) &&
			(mono_interp_opt & INTERP_OPT_CPROP))
		MONO_TIME_TRACK (mono_interp_stats.super_instructions_time, interp_super_instructions (td));

	if (!td->disable_ssa)
		interp_exit_ssa (td);

	interp_remove_nops (td);

	if (mono_interp_opt & INTERP_OPT_BBLOCKS)
		MONO_TIME_TRACK (mono_interp_stats.optimize_bblocks_time, interp_optimize_bblocks (td));

	if (ssa_enabled_retry) {
		ssa_enabled_retry = FALSE;
		td->disable_ssa = FALSE;
		if (td->verbose_level)
			g_print ("Retry optimization with SSA enabled\n");
		goto optimization_retry;
	} else if (td->need_optimization_retry) {
		if (td->verbose_level)
			g_print ("Retry optimization\n");
		goto optimization_retry;
	}

	if (td->verbose_level) {
		g_print ("\nOptimized IR:\n");
		mono_interp_print_td_code (td);
	}
}

