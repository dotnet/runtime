/**
 * \file
 * Sequence Points functions
 *
 * Authors:
 *   Marcos Henrich (marcos.henrich@xamarin.com)
 *
 * Copyright 2014 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "mini.h"
#include "mini-runtime.h"
#include "seq-points.h"

static void
insert_pred_seq_point (MonoInst *last_seq_ins, MonoInst *ins, GSList **next)
{
	GSList *l;
	int src_index = last_seq_ins->backend.size;
	int dst_index = ins->backend.size;

	/* bb->in_bb might contain duplicates */
	for (l = next [src_index]; l; l = l->next)
		if (GPOINTER_TO_UINT (l->data) == dst_index)
			break;
	if (!l)
		next [src_index] = g_slist_append (next [src_index], GUINT_TO_POINTER (dst_index));
}

static void
recursively_make_pred_seq_points (MonoCompile *cfg, MonoBasicBlock *bb)
{
	const gpointer MONO_SEQ_SEEN_LOOP = GINT_TO_POINTER(-1);

	GArray *predecessors = g_array_new (FALSE, TRUE, sizeof (gpointer));
	GHashTable *seen = g_hash_table_new_full (g_direct_hash, NULL, NULL, NULL);

	// Insert/remove sentinel into the memoize table to detect loops containing bb
	bb->pred_seq_points = (MonoInst**)MONO_SEQ_SEEN_LOOP;

	for (int i = 0; i < bb->in_count; ++i) {
		MonoBasicBlock *in_bb = bb->in_bb [i];

		// This bb has the last seq point, append it and continue
		if (in_bb->last_seq_point != NULL) {
			predecessors = g_array_append_val (predecessors, in_bb->last_seq_point);
			continue;
		}

		// We've looped or handled this before, exit early.
		// No last sequence points to find.
		if (in_bb->pred_seq_points == MONO_SEQ_SEEN_LOOP)
			continue;

		// Take sequence points from incoming basic blocks

		if (in_bb == cfg->bb_entry)
			continue;

		if (in_bb->pred_seq_points == NULL)
			recursively_make_pred_seq_points (cfg, in_bb);

		// Union sequence points with incoming bb's
		for (guint j = 0; j < in_bb->num_pred_seq_points; j++) {
			if (!g_hash_table_lookup (seen, in_bb->pred_seq_points [j])) {
				g_array_append_val (predecessors, in_bb->pred_seq_points [j]);
				g_hash_table_insert (seen, in_bb->pred_seq_points [j], (gpointer)&MONO_SEQ_SEEN_LOOP);
			}
		}
		// predecessors = g_array_append_vals (predecessors, in_bb->pred_seq_points, in_bb->num_pred_seq_points);
	}

	g_hash_table_destroy (seen);

	if (predecessors->len != 0) {
		bb->pred_seq_points = (MonoInst **)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst *) * predecessors->len);
		bb->num_pred_seq_points = predecessors->len;

		for (guint newer = 0; newer < bb->num_pred_seq_points; newer++) {
			bb->pred_seq_points [newer] = g_array_index(predecessors, MonoInst*, newer);
		}
	}

	g_array_free (predecessors, TRUE);
}

static void
collect_pred_seq_points (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, GSList **next)
{
	// Doesn't have a last sequence point, must find from incoming basic blocks
	if (bb->pred_seq_points == NULL && bb != cfg->bb_entry)
		recursively_make_pred_seq_points (cfg, bb);

	for (guint i = 0; i < bb->num_pred_seq_points; i++)
		insert_pred_seq_point (bb->pred_seq_points [i], ins, next);

	return;
}

void
mono_save_seq_point_info (MonoCompile *cfg, MonoJitInfo *jinfo)
{
	MonoBasicBlock *bb;
	GSList *bb_seq_points, *l;
	MonoInst *last;
	int seq_info_size;
	GSList **next = NULL;
	SeqPoint* seq_points;
	GByteArray* array;
	gboolean has_debug_data = cfg->gen_sdb_seq_points;

	if (!cfg->seq_points)
		return;

	seq_points = g_new0 (SeqPoint, cfg->seq_points->len);

	for (guint i = 0; i < cfg->seq_points->len; ++i) {
		SeqPoint *sp = &seq_points [i];
		MonoInst *ins = (MonoInst *)g_ptr_array_index (cfg->seq_points, i);
		sp->il_offset = GTMREG_TO_INT (ins->inst_imm);
		sp->native_offset = GTMREG_TO_INT (ins->inst_offset);
		if (ins->flags & MONO_INST_NONEMPTY_STACK)
			sp->flags |= MONO_SEQ_POINT_FLAG_NONEMPTY_STACK;
		if (ins->flags & MONO_INST_NESTED_CALL)
			sp->flags |= MONO_SEQ_POINT_FLAG_NESTED_CALL;
		/* Used below */
		ins->backend.size = i;
	}

	if (has_debug_data) {
		/*
		 * For each sequence point, compute the list of sequence points immediately
		 * following it, this is needed to implement 'step over' in the debugger agent.
		 */
		next = g_new0 (GSList*, cfg->seq_points->len);
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			bb_seq_points = g_slist_reverse (bb->seq_points);
			last = NULL;
			for (l = bb_seq_points; l; l = l->next) {
				MonoInst *ins = (MonoInst *)l->data;

				if (ins->inst_imm == METHOD_ENTRY_IL_OFFSET || ins->inst_imm == METHOD_EXIT_IL_OFFSET)
				/* Used to implement method entry/exit events */
					continue;
				if (ins->inst_offset == SEQ_POINT_NATIVE_OFFSET_DEAD_CODE)
					continue;

				if (last != NULL) {
					/* Link with the previous seq point in the same bb */
					next [last->backend.size] = g_slist_append (next [last->backend.size], GUINT_TO_POINTER (ins->backend.size));
				} else {
					/* Link with the last bb in the previous bblocks */
					collect_pred_seq_points (cfg, bb, ins, next);
				}

				last = ins;
			}

			/* The second case handles endfinally opcodes which are in a separate bb by themselves */
			if ((bb->last_ins && bb->last_ins->opcode == OP_ENDFINALLY && bb->seq_points) || (bb->out_count == 1 && bb->out_bb [0]->code && bb->out_bb [0]->code->opcode == OP_ENDFINALLY)) {
				MonoBasicBlock *bb2;
				MonoInst *endfinally_seq_point = NULL;

				/*
				 * The ENDFINALLY branches are not represented in the cfg, so link it with all seq points starting bbs.
				 */
				l = g_slist_last (bb->seq_points);
				if (l) {
					endfinally_seq_point = (MonoInst *)l->data;

					for (bb2 = bb->next_bb; bb2; bb2 = bb2->next_bb) {
						l = g_slist_last (bb2->seq_points);
						if (l) {
							MonoInst *ins = (MonoInst *)l->data;

							if (!(ins->inst_imm == METHOD_ENTRY_IL_OFFSET || ins->inst_imm == METHOD_EXIT_IL_OFFSET) && ins != endfinally_seq_point)
								next [endfinally_seq_point->backend.size] = g_slist_append (next [endfinally_seq_point->backend.size], GUINT_TO_POINTER (ins->backend.size));
						}
					}
				}
			}
		}

		if (cfg->verbose_level > 2) {
			printf ("\nSEQ POINT MAP: \n");

			for (guint i = 0; i < cfg->seq_points->len; ++i) {
				SeqPoint *sp = &seq_points [i];

				if (!next [i])
					continue;

				printf ("\tIL0x%x[0x%0x] ->", sp->il_offset, sp->native_offset);
				for (l = next [i]; l; l = l->next) {
					int next_index = GPOINTER_TO_UINT (l->data);
					printf (" IL0x%x", seq_points [next_index].il_offset);
				}
				printf ("\n");
			}
		}
	}

	array = g_byte_array_new ();

	{ /* Add sequence points to seq_point_info */
		SeqPoint zero_seq_point = {0};
		SeqPoint* last_seq_point = &zero_seq_point;

		for (guint i = 0; i < cfg->seq_points->len; ++i) {
			SeqPoint *sp = &seq_points [i];
			GSList* next_list = NULL;

			if (has_debug_data)
				next_list = next[i];

			if (mono_seq_point_info_add_seq_point (array, sp, last_seq_point, next_list, has_debug_data))
				last_seq_point = sp;

			if (has_debug_data)
				g_slist_free (next [i]);
		}
	}

	g_free (seq_points);

	if (has_debug_data)
		g_free (next);

	cfg->seq_point_info = mono_seq_point_info_new (array->len, TRUE, array->data, has_debug_data, &seq_info_size);
	mono_atomic_fetch_add_i32 (&mono_jit_stats.allocated_seq_points_size, seq_info_size);

	g_byte_array_free (array, TRUE);

	// FIXME: dynamic methods
	if (!cfg->compile_aot) {
		// FIXME:
		MonoJitMemoryManager *jit_mm = get_default_jit_mm ();
		jit_mm_lock (jit_mm);
		// FIXME: The lookup can fail if the method is JITted recursively though a type cctor
		MonoSeqPointInfo *existing_seq_points = NULL;
		if (!g_hash_table_lookup_extended (jit_mm->seq_points, cfg->method_to_register, NULL, (gpointer *)&existing_seq_points)) {
			g_hash_table_insert (jit_mm->seq_points, cfg->method_to_register, cfg->seq_point_info);
		} else {
			mono_seq_point_info_free (cfg->seq_point_info);
			cfg->seq_point_info = existing_seq_points;
		}
		jit_mm_unlock (jit_mm);

		g_assert (jinfo);
		jinfo->seq_points = cfg->seq_point_info;
	}

	g_ptr_array_free (cfg->seq_points, TRUE);
	cfg->seq_points = NULL;
}

MonoSeqPointInfo*
mono_get_seq_points (MonoMethod *method)
{
	ERROR_DECL (error);
	MonoSeqPointInfo *seq_points;
	MonoMethod *declaring_generic_method = NULL, *shared_method = NULL;
	MonoJitMemoryManager *jit_mm;

	if (method->is_inflated) {
		declaring_generic_method = mono_method_get_declaring_generic_method (method);
		shared_method = mini_get_shared_method_full (method, SHARE_MODE_NONE, error);
		mono_error_assert_ok (error);
	}

	// FIXME:
	jit_mm = get_default_jit_mm ();
	jit_mm_lock (jit_mm);
	seq_points = (MonoSeqPointInfo *)g_hash_table_lookup (jit_mm->seq_points, method);
	if (!seq_points && method->is_inflated) {
		/* generic sharing + aot */
		seq_points = (MonoSeqPointInfo *)g_hash_table_lookup (jit_mm->seq_points, declaring_generic_method);
		if (!seq_points)
			seq_points = (MonoSeqPointInfo *)g_hash_table_lookup (jit_mm->seq_points, shared_method);
	}
	jit_mm_unlock (jit_mm);

	return seq_points;
}

/*
 * mono_find_next_seq_point_for_native_offset:
 *
 *   Find the first sequence point after NATIVE_OFFSET.
 */
gboolean
mono_find_next_seq_point_for_native_offset (MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info, SeqPoint* seq_point)
{
	MonoSeqPointInfo *seq_points;

	seq_points = mono_get_seq_points (method);
	if (!seq_points) {
		if (info)
			*info = NULL;
		return FALSE;
	}
	if (info)
		*info = seq_points;

	return mono_seq_point_find_next_by_native_offset (seq_points, native_offset, seq_point);
}

/*
 * mono_find_prev_seq_point_for_native_offset:
 *
 *   Find the first sequence point before NATIVE_OFFSET.
 */
gboolean
mono_find_prev_seq_point_for_native_offset (MonoMethod *method, gint32 native_offset, MonoSeqPointInfo **info, SeqPoint* seq_point)
{
	MonoSeqPointInfo *seq_points;

	seq_points = mono_get_seq_points (method);
	if (!seq_points) {
		if (info)
			*info = NULL;
		return FALSE;
	}
	if (info)
		*info = seq_points;

	return mono_seq_point_find_prev_by_native_offset (seq_points, native_offset, seq_point);
}

/*
 * mono_find_seq_point:
 *
 *   Find the sequence point corresponding to the IL offset IL_OFFSET, which
 * should be the location of a sequence point.
 */
gboolean
mono_find_seq_point (MonoMethod *method, gint32 il_offset, MonoSeqPointInfo **info, SeqPoint *seq_point)
{
	MonoSeqPointInfo *seq_points;

	seq_points = mono_get_seq_points (method);
	if (!seq_points) {
		if (info)
			*info = NULL;
		return FALSE;
	}
	if (info)
		*info = seq_points;

	return mono_seq_point_find_by_il_offset (seq_points, il_offset, seq_point);
}

void
mono_bb_deduplicate_op_il_seq_points (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n, *prev;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		if (ins->opcode != OP_IL_SEQ_POINT)
			continue;

		prev = mono_inst_prev (ins, FILTER_NOP);

		if (!prev || ins == prev || prev->opcode != OP_IL_SEQ_POINT)
			continue;

		MONO_REMOVE_INS (bb, prev);
	};
}
