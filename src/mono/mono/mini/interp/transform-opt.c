/*
 * Optimizations for interpreter codegen
 */

#include "mintops.h"
#include "transform.h"

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
								!td->local_ref_count || td->local_ref_count [var] > 1 ||
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

static gboolean
interp_remove_bblock (TransformData *td, InterpBasicBlock *bb, InterpBasicBlock *prev_bb)
{
	gboolean needs_cprop = FALSE;

	for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
		if (ins->opcode == MINT_LDLOCA_S) {
			td->vars [ins->sregs [0]].indirects--;
			if (!td->vars [ins->sregs [0]].indirects) {
				// We can do cprop now through this local. Run cprop again.
				needs_cprop = TRUE;
			}
		}
	}
	while (bb->in_count)
		interp_unlink_bblocks (bb->in_bb [0], bb);
	while (bb->out_count)
		interp_unlink_bblocks (bb, bb->out_bb [0]);
	prev_bb->next_bb = bb->next_bb;
	mark_bb_as_dead (td, bb, bb->next_bb);

	return needs_cprop;
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

	// FIXME There is no need to force eh bblocks to remain alive
	current = td->entry_bb;
	while (current != NULL) {
		if (current->eh_block || current->patchpoint_data) {
			queue [next_position++] = current;
			current->reachable = TRUE;
		} else {
			current->reachable = FALSE;
		}
		current = current->next_bb;
	}

	queue [next_position++] = td->entry_bb;
	td->entry_bb->reachable = TRUE;

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
		if (bb->eh_block)
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
static gboolean
interp_optimize_bblocks (TransformData *td)
{
	InterpBasicBlock *bb = td->entry_bb;
	gboolean needs_cprop = FALSE;

	interp_reorder_bblocks (td);

	interp_mark_reachable_bblocks (td);

	while (TRUE) {
		InterpBasicBlock *next_bb = bb->next_bb;
		if (!next_bb)
			break;
		if (!next_bb->reachable) {
			if (td->verbose_level)
				g_print ("Removed BB%d\n", next_bb->index);
			needs_cprop |= interp_remove_bblock (td, next_bb, bb);
			continue;
		} else if (bb->out_count == 1 && bb->out_bb [0] == next_bb && next_bb->in_count == 1 && !next_bb->eh_block && !next_bb->patchpoint_data) {
			g_assert (next_bb->in_bb [0] == bb);
			interp_merge_bblocks (td, bb, next_bb);
			if (td->verbose_level)
				g_print ("Merged BB%d and BB%d\n", bb->index, next_bb->index);
			needs_cprop = TRUE;
			continue;
		}

		bb = next_bb;
	}
	return needs_cprop;
}

static gboolean
interp_local_deadce (TransformData *td)
{
	int *local_ref_count = td->local_ref_count;
	gboolean needs_dce = FALSE;
	gboolean needs_cprop = FALSE;

	for (unsigned int i = 0; i < td->vars_size; i++) {
		g_assert (local_ref_count [i] >= 0);
		g_assert (td->vars [i].indirects >= 0);
		if (td->vars [i].indirects || td->vars [i].dead)
			continue;
		if (!local_ref_count [i]) {
			needs_dce = TRUE;
			td->vars [i].dead = TRUE;
		} else if (!td->vars [i].unknown_use) {
			if (!td->vars [i].local_only) {
				// The value of this var is not passed between multiple basic blocks
				td->vars [i].local_only = TRUE;
				if (td->verbose_level)
					g_print ("Var %d is local only\n", i);
				needs_cprop = TRUE;
			}
		}
		td->vars [i].unknown_use = FALSE;
	}

	// Return early if all locals are alive
	if (!needs_dce)
		return needs_cprop;

	// Kill instructions that don't use stack and are storing into dead locals
	for (InterpBasicBlock *bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
			if (MINT_NO_SIDE_EFFECTS (ins->opcode) ||
					ins->opcode == MINT_LDLOCA_S) {
				int dreg = ins->dreg;
				if (td->vars [dreg].dead) {
					if (td->verbose_level) {
						g_print ("kill dead ins:\n\t");
						interp_dump_ins (ins, td->data_items);
					}

					if (ins->opcode == MINT_LDLOCA_S) {
						td->vars [ins->sregs [0]].indirects--;
						if (!td->vars [ins->sregs [0]].indirects) {
							// We can do cprop now through this local. Run cprop again.
							needs_cprop = TRUE;
						}
					}
					interp_clear_ins (ins);
					// FIXME This is lazy. We should update the ref count for the sregs and redo deadce.
					needs_cprop = TRUE;
				}
			}
		}
	}
	return needs_cprop;
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
interp_fold_unop (TransformData *td, InterpVarValue *local_defs, InterpInst *ins)
{
	int *local_ref_count = td->local_ref_count;
	// ins should be an unop, therefore it should have a single dreg and a single sreg
	int dreg = ins->dreg;
	int sreg = ins->sregs [0];
	InterpVarValue *val = &local_defs [sreg];
	InterpVarValue result;

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

	local_ref_count [sreg]--;
	result.ins = ins;
	result.ref_count = 0;
	local_defs [dreg] = result;

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
interp_fold_unop_cond_br (TransformData *td, InterpBasicBlock *cbb, InterpVarValue *local_defs, InterpInst *ins)
{
	int *local_ref_count = td->local_ref_count;
	// ins should be an unop conditional branch, therefore it should have a single sreg
	int sreg = ins->sregs [0];
	InterpVarValue *val = &local_defs [sreg];

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

	local_ref_count [sreg]--;
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
interp_fold_binop (TransformData *td, InterpVarValue *local_defs, InterpInst *ins, gboolean *folded)
{
	int *local_ref_count = td->local_ref_count;
	// ins should be a binop, therefore it should have a single dreg and two sregs
	int dreg = ins->dreg;
	int sreg1 = ins->sregs [0];
	int sreg2 = ins->sregs [1];
	InterpVarValue *val1 = &local_defs [sreg1];
	InterpVarValue *val2 = &local_defs [sreg2];
	InterpVarValue result;

	*folded = FALSE;

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

	local_ref_count [sreg1]--;
	local_ref_count [sreg2]--;
	result.ins = ins;
	result.ref_count = 0;
	local_defs [dreg] = result;
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
interp_fold_binop_cond_br (TransformData *td, InterpBasicBlock *cbb, InterpVarValue *local_defs, InterpInst *ins)
{
	int *local_ref_count = td->local_ref_count;
	// ins should be a conditional binop, therefore it should have only two sregs
	int sreg1 = ins->sregs [0];
	int sreg2 = ins->sregs [1];
	InterpVarValue *val1 = &local_defs [sreg1];
	InterpVarValue *val2 = &local_defs [sreg2];

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

	local_ref_count [sreg1]--;
	local_ref_count [sreg2]--;
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
interp_fold_simd_create (TransformData *td, InterpBasicBlock *cbb, InterpVarValue *local_defs, InterpInst *ins)
{
	int *local_ref_count = td->local_ref_count;

	int *args = ins->info.call_info->call_args;
	int index = 0;
	int var = args [index];
	while (var != -1) {
		InterpVarValue *val = &local_defs [var];
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
		InterpVarValue *val = &local_defs [var];
		write_v128_element (v128_addr, val, index, el_size);
		val->ref_count--;
		local_ref_count [var]--;
		index++;
		var = args [index];
	}

	if (td->verbose_level) {
		g_print ("Fold simd create:\n\t");
		interp_dump_ins (ins, td->data_items);
	}

	local_defs [dreg].ins = ins;
	local_defs [dreg].type = VAR_VALUE_NONE;

	return ins;
}

static void
cprop_sreg (TransformData *td, InterpInst *ins, int *psreg, InterpVarValue *local_defs)
{
	int *local_ref_count = td->local_ref_count;
	int sreg = *psreg;

	local_ref_count [sreg]++;
	local_defs [sreg].ref_count++;
	if (local_defs [sreg].type == VAR_VALUE_OTHER_VAR) {
		int cprop_local = local_defs [sreg].var;

		// We are trying to replace sregs [i] with its def local (cprop_local), but cprop_local has since been
		// modified, so we can't use it.
		if (local_defs [cprop_local].ins != NULL && local_defs [cprop_local].def_index > local_defs [sreg].def_index)
			return;

		if (td->verbose_level)
			g_print ("cprop %d -> %d:\n\t", sreg, cprop_local);
		local_ref_count [sreg]--;
		*psreg = cprop_local;
		local_ref_count [cprop_local]++;
		if (td->verbose_level)
			interp_dump_ins (ins, td->data_items);
	} else if (!local_defs [sreg].ins) {
		td->vars [sreg].unknown_use = TRUE;
	}
}

static void
clear_local_defs (TransformData *td, int *pvar, void *data)
{
	int var = *pvar;
	InterpVarValue *local_defs = (InterpVarValue*) data;
	local_defs [var].type = VAR_VALUE_NONE;
	local_defs [var].ins = NULL;
	local_defs [var].ref_count = 0;
}

static void
clear_unused_defs (TransformData *td, int *pvar, void *data)
{
	int var = *pvar;
	if (!td->vars [var].local_only)
		return;
	if (td->vars [var].indirects)
		return;

	InterpVarValue *local_def = &((InterpVarValue*) data) [var];
	InterpInst *def_ins = local_def->ins;
	if (!def_ins)
		return;
	if (local_def->ref_count)
		return;

	// This is a local only var that is defined in this bblock and its value is not used
	// at all in this bblock. Clear the definition
	if (MINT_NO_SIDE_EFFECTS (def_ins->opcode)) {
		for (int i = 0; i < mono_interp_op_sregs [def_ins->opcode]; i++)
			td->local_ref_count [def_ins->sregs [i]]--;
		if (td->verbose_level) {
			g_print ("kill unused local def:\n\t");
			interp_dump_ins (def_ins, td->data_items);
		}
		interp_clear_ins (def_ins);
	}
}

static void
interp_cprop (TransformData *td)
{
	InterpVarValue *local_defs = (InterpVarValue*) g_malloc (td->vars_size * sizeof (InterpVarValue));
	int *local_ref_count = (int*) g_malloc (td->vars_size * sizeof (int));
	InterpBasicBlock *bb;
	gboolean needs_retry;
	int ins_index;
	int iteration_count = 0;

	td->local_ref_count = local_ref_count;
retry:
	needs_retry = FALSE;
	memset (local_ref_count, 0, td->vars_size * sizeof (int));

	if (td->verbose_level)
		g_print ("\ncprop iteration %d\n", iteration_count++);

	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins;
		ins_index = 0;

		// Set cbb since we do some instruction inserting below
		td->cbb = bb;

		for (ins = bb->first_ins; ins != NULL; ins = ins->next)
			interp_foreach_ins_var (td, ins, local_defs, clear_local_defs);

		if (td->verbose_level) {
			GString* bb_info = interp_get_bb_links (bb);
			g_print ("\nBB%d: %s\n", bb->index, bb_info->str);
			g_string_free (bb_info, TRUE);
		}

		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			int opcode = ins->opcode;

			if (opcode == MINT_NOP)
				continue;

			int num_sregs = mono_interp_op_sregs [opcode];
			int num_dregs = mono_interp_op_dregs [opcode];
			gint32 *sregs = &ins->sregs [0];
			gint32 dreg = ins->dreg;

			if (td->verbose_level && ins->opcode != MINT_NOP && ins->opcode != MINT_IL_SEQ_POINT)
				interp_dump_ins (ins, td->data_items);

			for (int i = 0; i < num_sregs; i++) {
				if (sregs [i] == MINT_CALL_ARGS_SREG) {
					if (ins->info.call_info && ins->info.call_info->call_args) {
						int *call_args = ins->info.call_info->call_args;
						while (*call_args != -1) {
							cprop_sreg (td, ins, call_args, local_defs);
							call_args++;
						}
					}
				} else {
					cprop_sreg (td, ins, &sregs [i], local_defs);
				}
			}

			if (num_dregs) {
				// Check if the previous definition of this var was used at all.
				// If it wasn't we can just clear the instruction
				//
				// MINT_MOV_DST_OFF doesn't fully write to the var, so we special case it here
				if (local_defs [dreg].ins != NULL &&
						local_defs [dreg].ref_count == 0 &&
						!td->vars [dreg].indirects &&
						opcode != MINT_MOV_DST_OFF) {
					InterpInst *prev_def = local_defs [dreg].ins;
					if (MINT_NO_SIDE_EFFECTS (prev_def->opcode)) {
						for (int i = 0; i < mono_interp_op_sregs [prev_def->opcode]; i++)
							local_ref_count [prev_def->sregs [i]]--;
						interp_clear_ins (prev_def);
					}
				}
				local_defs [dreg].type = VAR_VALUE_NONE;
				local_defs [dreg].ins = ins;
				local_defs [dreg].def_index = ins_index;
			}

			// We always store to the full i4, except as part of STIND opcodes. These opcodes can be
			// applied to a local var only if that var has LDLOCA applied to it
			if ((opcode >= MINT_MOV_I4_I1 && opcode <= MINT_MOV_I4_U2) && !td->vars [sregs [0]].indirects) {
				ins->opcode = MINT_MOV_4;
				opcode = MINT_MOV_4;
			}

			if (opcode == MINT_MOV_4 || opcode == MINT_MOV_8 || opcode == MINT_MOV_VT) {
				int sreg = sregs [0];
				if (dreg == sreg) {
					if (td->verbose_level)
						g_print ("clear redundant mov\n");
					interp_clear_ins (ins);
					local_ref_count [sreg]--;
				} else if (td->vars [sreg].indirects || td->vars [dreg].indirects) {
					// Don't bother with indirect locals
				} else if (local_defs [sreg].type == VAR_VALUE_I4 || local_defs [sreg].type == VAR_VALUE_I8) {
					// Replace mov with ldc
					gboolean is_i4 = local_defs [sreg].type == VAR_VALUE_I4;
					g_assert (!td->vars [sreg].indirects);
					local_defs [dreg].type = local_defs [sreg].type;
					if (is_i4) {
						int ct = local_defs [sreg].i;
						ins = interp_get_ldc_i4_from_const (td, ins, ct, dreg);
						local_defs [dreg].i = ct;
					} else {
						gint64 ct = local_defs [sreg].l;
						ins = interp_inst_replace_with_i8_const (td, ins, ct);
						local_defs [dreg].l = ct;
					}
					local_defs [dreg].ins = ins;
					local_ref_count [sreg]--;
					if (td->verbose_level) {
						g_print ("cprop loc %d -> ct :\n\t", sreg);
						interp_dump_ins (ins, td->data_items);
					}
				} else if (local_defs [sreg].ins != NULL &&
						td->vars [sreg].execution_stack &&
						!td->vars [dreg].execution_stack &&
						interp_prev_ins (ins) == local_defs [sreg].ins &&
						!(interp_prev_ins (ins)->flags & INTERP_INST_FLAG_PROTECTED_NEWOBJ)) {
					// hackish temporary optimization that won't be necessary in the future
					// We replace `local1 <- ?, local2 <- local1` with `local2 <- ?, local1 <- local2`
					// if local1 is execution stack local and local2 is normal global local. This makes
					// it more likely for `local1 <- local2` to be killed, while before we always needed
					// to store to the global local, which is likely accessed by other instructions.
					InterpInst *def = local_defs [sreg].ins;
					int original_dreg = def->dreg;

					def->dreg = dreg;
					ins->dreg = original_dreg;
					sregs [0] = dreg;

					local_defs [dreg].type = VAR_VALUE_NONE;
					local_defs [dreg].ins = def;
					local_defs [dreg].def_index = local_defs [original_dreg].def_index;
					local_defs [dreg].ref_count++;
					local_defs [original_dreg].type = VAR_VALUE_OTHER_VAR;
					local_defs [original_dreg].ins = ins;
					local_defs [original_dreg].var = dreg;
					local_defs [original_dreg].def_index = ins_index;
					local_defs [original_dreg].ref_count--;

					local_ref_count [original_dreg]--;
					local_ref_count [dreg]++;

					if (td->verbose_level) {
						g_print ("cprop dreg:\n\t");
						interp_dump_ins (def, td->data_items);
						g_print ("\t");
						interp_dump_ins (ins, td->data_items);
					}
				} else {
					if (td->verbose_level)
						g_print ("local copy %d <- %d\n", dreg, sreg);
					local_defs [dreg].type = VAR_VALUE_OTHER_VAR;
					local_defs [dreg].var = sreg;
				}
			} else if (opcode == MINT_LDLOCA_S) {
				// The local that we are taking the address of is not a sreg but still referenced
				local_ref_count [ins->sregs [0]]++;
			} else if (MINT_IS_LDC_I4 (opcode)) {
				local_defs [dreg].type = VAR_VALUE_I4;
				local_defs [dreg].i = interp_get_const_from_ldc_i4 (ins);
			} else if (MINT_IS_LDC_I8 (opcode)) {
				local_defs [dreg].type = VAR_VALUE_I8;
				local_defs [dreg].l = interp_get_const_from_ldc_i8 (ins);
			} else if (opcode == MINT_LDC_R4) {
				guint32 val_u = READ32 (&ins->data [0]);
				float f = *(float*)(&val_u);
				local_defs [dreg].type = VAR_VALUE_R4;
				local_defs [dreg].f = f;
			} else if (ins->opcode == MINT_LDPTR) {
#if SIZEOF_VOID_P == 8
				local_defs [dreg].type = VAR_VALUE_I8;
				local_defs [dreg].l = (gint64)td->data_items [ins->data [0]];
#else
				local_defs [dreg].type = VAR_VALUE_I4;
				local_defs [dreg].i = (gint32)td->data_items [ins->data [0]];
#endif
			} else if (MINT_IS_UNOP (opcode)) {
				ins = interp_fold_unop (td, local_defs, ins);
			} else if (MINT_IS_UNOP_CONDITIONAL_BRANCH (opcode)) {
				ins = interp_fold_unop_cond_br (td, bb, local_defs, ins);
			} else if (MINT_IS_SIMD_CREATE (opcode)) {
				ins = interp_fold_simd_create (td, bb, local_defs, ins);
			} else if (MINT_IS_BINOP (opcode)) {
				gboolean folded;
				ins = interp_fold_binop (td, local_defs, ins, &folded);
				if (!folded) {
					int sreg = -1;
					guint16 mov_op = 0;
					if ((opcode == MINT_MUL_I4 || opcode == MINT_DIV_I4) &&
							local_defs [ins->sregs [1]].type == VAR_VALUE_I4 &&
							local_defs [ins->sregs [1]].i == 1) {
						sreg = ins->sregs [0];
						mov_op = MINT_MOV_4;
					} else if ((opcode == MINT_MUL_I8 || opcode == MINT_DIV_I8) &&
							local_defs [ins->sregs [1]].type == VAR_VALUE_I8 &&
							local_defs [ins->sregs [1]].l == 1) {
						sreg = ins->sregs [0];
						mov_op = MINT_MOV_8;
					} else if (opcode == MINT_MUL_I4 &&
							local_defs [ins->sregs [0]].type == VAR_VALUE_I4 &&
							local_defs [ins->sregs [0]].i == 1) {
						sreg = ins->sregs [1];
						mov_op = MINT_MOV_4;
					} else if (opcode == MINT_MUL_I8 &&
							local_defs [ins->sregs [0]].type == VAR_VALUE_I8 &&
							local_defs [ins->sregs [0]].l == 1) {
						sreg = ins->sregs [1];
						mov_op = MINT_MOV_8;
					}
					if (sreg != -1) {
						ins->opcode = mov_op;
						ins->sregs [0] = sreg;
						if (td->verbose_level) {
							g_print ("Replace idempotent binop :\n\t");
							interp_dump_ins (ins, td->data_items);
						}
						needs_retry = TRUE;
					}
				}
			} else if (MINT_IS_BINOP_CONDITIONAL_BRANCH (opcode)) {
				ins = interp_fold_binop_cond_br (td, bb, local_defs, ins);
			} else if (MINT_IS_LDIND (opcode)) {
				InterpInst *ldloca = local_defs [sregs [0]].ins;
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
						local_ref_count [sregs [0]]--;
						interp_ins_set_sreg (ins, local);

						if (td->verbose_level) {
							g_print ("Replace ldloca/ldind pair :\n\t");
							interp_dump_ins (ins, td->data_items);
						}
						needs_retry = TRUE;
					}
				}
			} else if (MINT_IS_LDFLD (opcode)) {
				InterpInst *ldloca = local_defs [sregs [0]].ins;
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int mt = ins->opcode - MINT_LDFLD_I1;
					int local = ldloca->sregs [0];
					// Allow ldloca instruction to be killed
					local_ref_count [sregs [0]]--;
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
					}

					if (td->verbose_level) {
						g_print ("Replace ldloca/ldfld pair :\n\t");
						interp_dump_ins (ins, td->data_items);
					}
					needs_retry = TRUE;
				}
			} else if (opcode == MINT_INITOBJ) {
				InterpInst *ldloca = local_defs [sregs [0]].ins;
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
					local_ref_count [sregs [0]]--;
					ins->dreg = local;

					if (td->verbose_level) {
						g_print ("Replace ldloca/initobj pair :\n\t");
						interp_dump_ins (ins, td->data_items);
					}
					needs_retry = TRUE;
				}
			} else if (opcode == MINT_LDOBJ_VT) {
				InterpInst *ldloca = local_defs [sregs [0]].ins;
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int ldsize = ins->data [0];
					int local = ldloca->sregs [0];
					local_ref_count [sregs [0]]--;

					if (ldsize == td->vars [local].size) {
						// Replace LDLOCA + LDOBJ_VT with MOV_VT
						ins->opcode = MINT_MOV_VT;
						sregs [0] = local;
						needs_retry = TRUE;
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
				InterpInst *ldloca = local_defs [sregs [0]].ins;
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int stsize = ins->data [0];
					int local = ldloca->sregs [0];

					if (stsize == td->vars [local].size) {
						// Replace LDLOCA + STOBJ_VT with MOV_VT
						local_ref_count [sregs [0]]--;
						ins->opcode = MINT_MOV_VT;
						sregs [0] = sregs [1];
						ins->dreg = local;
						needs_retry = TRUE;

						if (td->verbose_level) {
							g_print ("Replace ldloca/stobj_vt pair :\n\t");
							interp_dump_ins (ins, td->data_items);
						}
					}
				}
			} else if (MINT_IS_STIND (opcode)) {
				InterpInst *ldloca = local_defs [sregs [0]].ins;
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int local = ldloca->sregs [0];
					int mt = td->vars [local].mt;
					if (mt != MINT_TYPE_VT) {
						// We have an 8 byte local, just replace the stind with a mov
						local_ref_count [sregs [0]]--;
						// We make the assumption that the STIND matches the local type
						ins->opcode = GINT_TO_OPCODE (interp_get_mov_for_type (mt, TRUE));
						interp_ins_set_dreg (ins, local);
						interp_ins_set_sreg (ins, sregs [1]);

						if (td->verbose_level) {
							g_print ("Replace ldloca/stind pair :\n\t");
							interp_dump_ins (ins, td->data_items);
						}
						needs_retry = TRUE;
					}
				}
			} else if (MINT_IS_STFLD (opcode)) {
				InterpInst *ldloca = local_defs [sregs [0]].ins;
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int mt = ins->opcode - MINT_STFLD_I1;
					int local = ldloca->sregs [0];
					local_ref_count [sregs [0]]--;
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
						interp_ins_set_sreg (ins, sregs [1]);
						ins->data [0] = GINT_TO_UINT16 (foffset);
						ins->data [1] = GINT_TO_UINT16 (mt);
						ins->data [2] = vtsize;

						interp_clear_ins (ins->prev);
					}
					if (td->verbose_level) {
						g_print ("Replace ldloca/stfld pair (off %p) :\n\t", (void *)(uintptr_t) ldloca->il_offset);
						interp_dump_ins (ins, td->data_items);
					}
					needs_retry = TRUE;
				}
			} else if (opcode == MINT_GETITEM_SPAN) {
				InterpInst *ldloca = local_defs [sregs [0]].ins;
				if (ldloca != NULL && ldloca->opcode == MINT_LDLOCA_S) {
					int local = ldloca->sregs [0];
					// Allow ldloca instruction to be killed
					local_ref_count [sregs [0]]--;
					// Instead of loading from the indirect pointer pass directly the vt var
					ins->opcode = MINT_GETITEM_LOCALSPAN;
					sregs [0] = local;
					needs_retry = TRUE;
				}
			} else if (opcode == MINT_CKNULL) {
				InterpInst *def = local_defs [sregs [0]].ins;
				if (def && def->opcode == MINT_LDLOCA_S) {
					// CKNULL on LDLOCA is a NOP
					ins->opcode = MINT_MOV_P;
					needs_retry = TRUE;
				}
			} else if (opcode == MINT_BOX) {
				// TODO Add more relevant opcodes
				local_defs [dreg].type = VAR_VALUE_NON_NULL;
			}

			ins_index++;
		}

		for (ins = bb->first_ins; ins != NULL; ins = ins->next)
			interp_foreach_ins_var (td, ins, local_defs, clear_unused_defs);
	}

	needs_retry |= interp_local_deadce (td);
	if (mono_interp_opt & INTERP_OPT_BBLOCKS)
		needs_retry |= interp_optimize_bblocks (td);

	if (needs_retry)
		goto retry;

	g_free (local_defs);
}

void
mono_test_interp_cprop (TransformData *td)
{
	interp_cprop (td);
}

static gboolean
get_sreg_imm (TransformData *td, int sreg, gint16 *imm, int result_mt)
{
	InterpInst *def = td->vars [sreg].def;
	if (def != NULL && td->local_ref_count [sreg] == 1) {
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
	InterpBasicBlock *bb;
	int *local_ref_count = td->local_ref_count;

	interp_compute_native_offset_estimates (td);

	// Add some actual super instructions
	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins;
		int noe;

		// Set cbb since we do some instruction inserting below
		td->cbb = bb;
		noe = bb->native_offset_estimate;
		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			int opcode = ins->opcode;
			if (MINT_IS_NOP (opcode))
				continue;
			if (mono_interp_op_dregs [opcode] && !td->vars [ins->dreg].global)
				td->vars [ins->dreg].def = ins;

			if (opcode == MINT_RET || (opcode >= MINT_RET_I1 && opcode <= MINT_RET_U2)) {
				// ldc + ret -> ret.imm
				int sreg = ins->sregs [0];
				gint16 imm;
				if (get_sreg_imm (td, sreg, &imm, (opcode == MINT_RET) ? MINT_TYPE_I2 : opcode - MINT_RET_I1)) {
					InterpInst *def = td->vars [sreg].def;
					int ret_op = MINT_IS_LDC_I4 (def->opcode) ? MINT_RET_I4_IMM : MINT_RET_I8_IMM;
					InterpInst *new_inst = interp_insert_ins (td, ins, ret_op);
					new_inst->data [0] = imm;
					interp_clear_ins (def);
					interp_clear_ins (ins);
					local_ref_count [sreg]--;

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
					interp_clear_ins (td->vars [sreg_imm].def);
					interp_clear_ins (ins);
					local_ref_count [sreg_imm]--;
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
					interp_clear_ins (td->vars [sreg_imm].def);
					interp_clear_ins (ins);
					local_ref_count [sreg_imm]--;
					if (td->verbose_level) {
						g_print ("superins: ");
						interp_dump_ins (new_inst, td->data_items);
					}
				}
			} else if (opcode == MINT_MUL_I4_IMM || opcode == MINT_MUL_I8_IMM) {
				int sreg = ins->sregs [0];
				InterpInst *def = td->vars [sreg].def;
				if (def != NULL && td->local_ref_count [sreg] == 1) {
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
						local_ref_count [sreg]--;
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
					interp_clear_ins (td->vars [sreg_imm].def);
					interp_clear_ins (ins);
					local_ref_count [sreg_imm]--;
					if (td->verbose_level) {
						g_print ("superins: ");
						interp_dump_ins (new_inst, td->data_items);
					}
				} else if (opcode == MINT_SHL_I4 || opcode == MINT_SHL_I8) {
					int amount_var = ins->sregs [1];
					InterpInst *amount_def = td->vars [amount_var].def;
					if (amount_def != NULL && td->local_ref_count [amount_var] == 1 && amount_def->opcode == MINT_AND_I4) {
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

								local_ref_count [amount_var]--;
								local_ref_count [mask_var]--;

								interp_clear_ins (td->vars [mask_var].def);
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
				InterpInst *def = td->vars [sreg_imm].def;
				if (def != NULL && td->local_ref_count [sreg_imm] == 1) {
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
						local_ref_count [sreg_imm]--;
						if (td->verbose_level) {
							g_print ("lower div.un: ");
							interp_dump_ins (new_inst, td->data_items);
						}
					}
				}
			} else if (MINT_IS_LDIND_INT (opcode)) {
				int sreg_base = ins->sregs [0];
				InterpInst *def = td->vars [sreg_base].def;
				if (def != NULL && td->local_ref_count [sreg_base] == 1) {
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
						local_ref_count [sreg_base]--;
						if (td->verbose_level) {
							g_print ("superins: ");
							interp_dump_ins (new_inst, td->data_items);
						}
					}
				}
			} else if (MINT_IS_LDIND_OFFSET (opcode)) {
				int sreg_off = ins->sregs [1];
				InterpInst *def = td->vars [sreg_off].def;
				if (def != NULL && td->local_ref_count [sreg_off] == 1) {
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
						local_ref_count [sreg_off]--;
						if (td->verbose_level) {
							g_print ("method %s:%s, superins: ", m_class_get_name (td->method->klass), td->method->name);
							interp_dump_ins (new_inst, td->data_items);
						}
					}
				}
			} else if (MINT_IS_STIND_INT (opcode)) {
				int sreg_base = ins->sregs [0];
				InterpInst *def = td->vars [sreg_base].def;
				if (def != NULL && td->local_ref_count [sreg_base] == 1) {
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
						local_ref_count [sreg_base]--;
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
				InterpInst *def = td->vars [obj_sreg].def;
				if (def != NULL && def->opcode == MINT_CKNULL && interp_prev_ins (ins) == def &&
						def->dreg == obj_sreg && local_ref_count [obj_sreg] == 1) {
					if (td->verbose_level) {
						g_print ("remove redundant cknull (%s): ", td->method->name);
						interp_dump_ins (def, td->data_items);
					}
					ins->sregs [0] = def->sregs [0];
					interp_clear_ins (def);
					local_ref_count [obj_sreg]--;
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
						interp_clear_ins (td->vars [sreg_imm].def);
						interp_clear_ins (ins);
						local_ref_count [sreg_imm]--;
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
					InterpInst *def = td->vars [cond_sreg].def;
					if (def != NULL && local_ref_count [cond_sreg] == 1) {
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
							local_ref_count [cond_sreg]--;
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
				InterpInst *def = td->vars [sreg_src].def;
				if (def != NULL && interp_prev_ins (ins) == def && def->opcode == MINT_LDOBJ_VT && ins->data [0] == def->data [0] && td->local_ref_count [sreg_src] == 1) {
					InterpInst *new_inst = interp_insert_ins (td, ins, MINT_CPOBJ_VT_NOREF);
					new_inst->sregs [0] = ins->sregs [0]; // dst
					new_inst->sregs [1] = def->sregs [0]; // src
					new_inst->data [0] = ins->data [0];   // size

					interp_clear_ins (def);
					interp_clear_ins (ins);
					local_ref_count [sreg_src]--;
					if (td->verbose_level) {
						g_print ("superins: ");
						interp_dump_ins (new_inst, td->data_items);
					}
				}
			}
			noe += interp_get_ins_length (ins);
		}
	}
}

void
interp_optimize_code (TransformData *td)
{
	if (mono_interp_opt & INTERP_OPT_BBLOCKS)
		interp_optimize_bblocks (td);

	if (mono_interp_opt & INTERP_OPT_CPROP)
		MONO_TIME_TRACK (mono_interp_stats.cprop_time, interp_cprop (td));

	// After this point control optimizations on control flow can no longer happen, so we can determine
	// which vars are global. This helps speed up the super instructions pass, which only operates on
	// single def, single use local vars.
	initialize_global_vars (td);

	if ((mono_interp_opt & INTERP_OPT_SUPER_INSTRUCTIONS) &&
			(mono_interp_opt & INTERP_OPT_CPROP))
		MONO_TIME_TRACK (mono_interp_stats.super_instructions_time, interp_super_instructions (td));
}

