/*
 * local-propagation.c: Local constant, copy and tree propagation.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Massimiliano Mantione (massi@ximian.com)
 *
 * (C) 2006 Novell, Inc.  http://www.novell.com
 */


#include <string.h>
#include <stdio.h>

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/opcodes.h>
#include "mini.h"


#define MONO_DEBUG_LOCAL_PROP 0
#define MONO_DEBUG_TREE_MOVER 0
#define MONO_DUMP_TREE_MOVER 0
#define MONO_APPLY_TREE_MOVER_TO_SINGLE_METHOD 0
#define MONO_APPLY_TREE_MOVER_TO_COUNTED_METHODS 0

struct MonoTreeMoverActSlot;
typedef struct MonoTreeMoverDependencyNode {
	struct MonoTreeMoverActSlot *used_slot;
	struct MonoTreeMoverActSlot *affected_slot;
	struct MonoTreeMoverDependencyNode *next_used_local;
	struct MonoTreeMoverDependencyNode *next_affected_local;
	struct MonoTreeMoverDependencyNode *previous_affected_local;
	gboolean use_is_direct;
} MonoTreeMoverDependencyNode;

struct MonoTreeMoverTreeMove;
typedef struct MonoTreeMoverAffectedMove {
	struct MonoTreeMoverTreeMove *affected_move;
	struct MonoTreeMoverAffectedMove *next_affected_move;
} MonoTreeMoverAffectedMove;

typedef struct MonoTreeMoverDependencyFromDeadDefinition {
	struct MonoTreeMoverActSlot *defined_slot;
	MonoInst *dead_definition;
	struct MonoTreeMoverDependencyFromDeadDefinition *next;
} MonoTreeMoverDependencyFromDeadDefinition;

typedef struct MonoTreeMoverTreeMove {
	struct MonoTreeMoverActSlot *defined_slot;
	MonoInst *definition;
	MonoInst **use;
	MonoTreeMoverAffectedMove *affected_moves;
	struct MonoTreeMoverDependencyFromDeadDefinition *slots_that_must_be_safe;
	struct MonoTreeMoverTreeMove *next;
	gboolean tree_reads_memory;
	gboolean move_is_safe;
	gboolean skip_this_move;
	gboolean prevent_forwarding;
} MonoTreeMoverTreeMove;

typedef struct MonoTreeMoverActSlot {
	MonoTreeMoverDependencyNode *used_locals;
	MonoTreeMoverDependencyNode *last_used_local;
	MonoTreeMoverDependencyNode *affected_locals;
	MonoTreeMoverTreeMove *pending_move;
	gboolean pending_move_is_ready;
	gboolean waiting_flag;
	gboolean unsafe_flag;
	gboolean pending_move_is_forwarded;
} MonoTreeMoverActSlot;

typedef struct MonoTreeMover {
	MonoMemPool *pool;
	MonoCompile *cfg;
	
	MonoTreeMoverDependencyNode *free_nodes;
	MonoTreeMoverTreeMove *free_moves;

	MonoTreeMoverActSlot *ACT;
	MonoTreeMoverTreeMove *scheduled_moves;


	MonoTreeMoverDependencyNode *used_nodes;
	MonoTreeMoverDependencyNode *last_used_node;
	gboolean tree_has_side_effects;
	gboolean tree_reads_memory;
} MonoTreeMover;

inline static MonoTreeMoverDependencyNode*
tree_mover_new_node (MonoTreeMover *tree_mover) {
	MonoTreeMoverDependencyNode *node;
	
	if (tree_mover->free_nodes != NULL) {
		node = tree_mover->free_nodes;
		tree_mover->free_nodes = tree_mover->free_nodes->next_used_local;
		node->next_used_local = NULL;
		node->next_affected_local = NULL;
		node->previous_affected_local = NULL;
	} else {
		node = (MonoTreeMoverDependencyNode*) mono_mempool_alloc0 (tree_mover->pool, sizeof (MonoTreeMoverDependencyNode));
	}
	
	return node;
}

inline static void
tree_mover_new_slot_move (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	MonoTreeMoverTreeMove *move;
	
	if (tree_mover->free_moves != NULL) {
		move = tree_mover->free_moves;
		tree_mover->free_moves = tree_mover->free_moves->next;
		memset (move, 0, sizeof (MonoTreeMoverTreeMove));
	} else {
		move = (MonoTreeMoverTreeMove*) mono_mempool_alloc0 (tree_mover->pool, sizeof (MonoTreeMoverTreeMove));
	}
	
	slot->pending_move = move;
}

inline static void
tree_mover_dispose_used_nodes (MonoTreeMover *tree_mover) {
	tree_mover->last_used_node->next_used_local = tree_mover->free_nodes;
	tree_mover->free_nodes = tree_mover->used_nodes;
	tree_mover->used_nodes = NULL;
	tree_mover->last_used_node = NULL;
}

inline static void
tree_mover_dispose_slot_nodes (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	slot->last_used_local->next_used_local = tree_mover->free_nodes;
	tree_mover->free_nodes = slot->used_locals;
	slot->used_locals = NULL;
	slot->last_used_local = NULL;
}

inline static void
tree_mover_dispose_slot_move (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	slot->pending_move->next = tree_mover->free_moves;
	tree_mover->free_moves = slot->pending_move;
	slot->pending_move = NULL;
}

inline static MonoTreeMoverActSlot*
tree_mover_slot_from_index (MonoTreeMover *tree_mover, int index) {
	return & (tree_mover->ACT [index]);
}

inline static int
tree_mover_slot_to_index (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	return slot - tree_mover->ACT;
}

inline static void
tree_mover_add_used_node (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot, gboolean use_is_direct) {
	MonoTreeMoverDependencyNode *node;
	
	node = tree_mover_new_node (tree_mover);
	node->used_slot = slot;
	node->affected_slot = NULL;
	node->use_is_direct = use_is_direct;
	if (tree_mover->last_used_node != NULL) {
		tree_mover->last_used_node->next_used_local = node;
	} else {\
		tree_mover->used_nodes = node;
	}\
	tree_mover->last_used_node = node;
}

inline static void
tree_mover_link_affecting_node (MonoTreeMoverDependencyNode *node, MonoTreeMoverActSlot *affected_slot) {
	MonoTreeMoverActSlot *affecting_slot = node->used_slot;
	node->affected_slot = affected_slot;
	node->next_affected_local = affecting_slot->affected_locals;
	affecting_slot->affected_locals = node;
	if (node->next_affected_local != NULL) {
		node->next_affected_local->previous_affected_local = node;
	}
	node->previous_affected_local = NULL;
}

inline static void
tree_mover_unlink_affecting_node (MonoTreeMoverDependencyNode *node) {
	if (node->next_affected_local != NULL) {
		node->next_affected_local->previous_affected_local = node->previous_affected_local;
	}
	if (node->previous_affected_local != NULL) {
		node->previous_affected_local->next_affected_local = node->next_affected_local;
	} else {
		MonoTreeMoverActSlot *slot = node->used_slot;
		slot->affected_locals = node->next_affected_local;
	}
	node->next_affected_local = NULL;
	node->previous_affected_local = NULL;
	node->affected_slot = NULL;
}

inline static void
tree_mover_link_affected_moves (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *source_slot, MonoTreeMoverActSlot *destination_slot) {
	MonoTreeMoverAffectedMove *node = (MonoTreeMoverAffectedMove*) mono_mempool_alloc0 (tree_mover->pool, sizeof (MonoTreeMoverAffectedMove));
	node->affected_move = destination_slot->pending_move; 
	node->next_affected_move = source_slot->pending_move->affected_moves;
	source_slot->pending_move->affected_moves = node;
}


inline static void
tree_mover_record_pending_move (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot, gboolean move_is_safe) {
	if (slot->pending_move_is_ready) {
		slot->pending_move->move_is_safe = move_is_safe;
		slot->pending_move->next = tree_mover->scheduled_moves;
		tree_mover->scheduled_moves = slot->pending_move;
		slot->pending_move = NULL;
		slot->pending_move_is_ready = FALSE;
	}
}

inline static void
tree_mover_clear_forwarding_dependency (MonoTreeMoverActSlot *slot) {
	if (slot->pending_move_is_forwarded) {
		MonoTreeMoverDependencyFromDeadDefinition *dependency = slot->pending_move->slots_that_must_be_safe;
		while (dependency != NULL) {
			if (dependency->defined_slot == slot) {
				dependency->defined_slot = NULL;
			}
			dependency = dependency->next;
		}
		slot->pending_move = NULL;
	}
}

inline static void
tree_mover_enforce_forwarding_dependency (MonoTreeMoverActSlot *slot) {
	if (slot->pending_move_is_forwarded) {
		slot->pending_move->skip_this_move = TRUE;
		slot->pending_move_is_forwarded = FALSE;
		slot->pending_move = NULL;
	}
}

inline static void
tree_mover_clean_act_slot_dependency_nodes (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	MonoTreeMoverDependencyNode *current_node = slot->used_locals;
	while (current_node != NULL) {
		tree_mover_unlink_affecting_node (current_node);
		current_node = current_node->next_used_local;
	}
	if (slot->used_locals != NULL) {
		tree_mover_dispose_slot_nodes (tree_mover, slot);
	}
}

inline static void
tree_mover_clean_act_slot_pending_move (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	if (slot->pending_move != NULL) {
		if (! slot->pending_move_is_forwarded) {
			tree_mover_dispose_slot_move (tree_mover, slot);
		} else {\
			slot->pending_move = NULL;
		}
	}
	slot->pending_move_is_ready = FALSE;
	slot->pending_move_is_forwarded = FALSE;
}

inline static void
tree_mover_clean_act_slot (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	tree_mover_clean_act_slot_dependency_nodes (tree_mover, slot);
	tree_mover_clean_act_slot_pending_move (tree_mover, slot);
}

inline static void
tree_mover_kill_act_slot_for_definition (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	tree_mover_record_pending_move (tree_mover, slot, TRUE);
	tree_mover_clear_forwarding_dependency (slot);
	tree_mover_clean_act_slot (tree_mover, slot);
}

inline static void
tree_mover_kill_act_slot_because_it_is_affected (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	if ((! slot->pending_move_is_ready) && (! slot->pending_move_is_forwarded)) {
		tree_mover_clean_act_slot (tree_mover, slot);
	}
}

inline static void
tree_mover_kill_act_slot_for_use (MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	tree_mover_enforce_forwarding_dependency (slot);
	tree_mover_clean_act_slot (tree_mover, slot);
}

inline static void
tree_mover_kill_act_for_indirect_local_definition (MonoTreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		MonoTreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		if (slot->pending_move != NULL) {
			slot->pending_move->prevent_forwarding = TRUE;
		}
		tree_mover_kill_act_slot_because_it_is_affected (tree_mover, slot);
	}
}

inline static void
tree_mover_kill_act_for_indirect_global_definition (MonoTreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		MonoTreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		if ((slot->pending_move != NULL) && slot->pending_move->tree_reads_memory) {
			tree_mover_kill_act_slot_because_it_is_affected (tree_mover, slot);
		}
	}
}

inline static void
tree_mover_kill_act_for_indirect_use (MonoTreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		MonoTreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		tree_mover_kill_act_slot_for_use (tree_mover, slot);
	}
}

inline static void
tree_mover_clear_act_recording_moves (MonoTreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		MonoTreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		tree_mover_record_pending_move (tree_mover, slot, FALSE);
		tree_mover_clean_act_slot (tree_mover, slot);
	}
}

inline static void
tree_mover_set_waiting_flags (MonoTreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		MonoTreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		slot->waiting_flag = TRUE;
	}
}

inline static void
tree_mover_verify_dependency_nodes_are_clear (MonoTreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		MonoTreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		if (slot->affected_locals != NULL) {
			printf ("Slot %d has still affected variables\n", i); 
			g_assert_not_reached ();
		}
		if (slot->used_locals != NULL) {
			printf ("Slot %d has still used variables\n", i); 
			g_assert_not_reached ();
		}
		if (slot->last_used_local != NULL) {
			printf ("Slot %d has still a last used variable\n", i); 
			g_assert_not_reached ();
		}
	}
}


static const guchar stind_needs_conversion[(CEE_STIND_R8-CEE_STIND_REF)+1][STACK_MAX] = {
	/* INV I4    I8    PTR   R8    MP    OBJ   VTYPE */
	{TRUE ,TRUE, TRUE, FALSE,TRUE, FALSE,FALSE,TRUE}, /* CEE_STIND_REF */
	{TRUE ,TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_STIND_I1  */
	{TRUE ,TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_STIND_I2  */
	{TRUE ,FALSE,TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_STIND_I4  */
	{TRUE ,TRUE, FALSE,TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_STIND_I8  */
	{TRUE ,TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_STIND_R4  */
	{TRUE ,TRUE, TRUE, TRUE, FALSE,TRUE, TRUE, TRUE}  /* CEE_STIND_R8  */
};
static const guchar stind_i_needs_conversion[STACK_MAX] = {TRUE ,TRUE, TRUE, FALSE, TRUE, FALSE, FALSE, TRUE};
static const guchar ldind_needs_conversion[(CEE_LDIND_REF-CEE_LDIND_I1)+1][STACK_MAX] = {
	/* INV I4    I8    PTR   R8    MP    OBJ   VTYPE */
	{TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_LDIND_I1  */
	{TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_LDIND_U1  */
	{TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_LDIND_I2  */
	{TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_LDIND_U2  */
	{TRUE, FALSE,TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_LDIND_I4  */
	{TRUE, FALSE,TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_LDIND_U4  */
	{TRUE, TRUE, FALSE,TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_LDIND_I8  */
	{TRUE, TRUE, TRUE, FALSE,TRUE, FALSE,FALSE,TRUE}, /* CEE_LDIND_I   */
	{TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE, TRUE}, /* CEE_LDIND_R4  */
	{TRUE, TRUE, TRUE, TRUE, FALSE,TRUE, TRUE, TRUE}, /* CEE_LDIND_R8  */
	{TRUE, TRUE, TRUE, FALSE,TRUE, FALSE,FALSE,TRUE}  /* CEE_LDIND_REF */
};

#define TREE_MOVER_LDIND_TO_CONV(__opcode) (ldind_to_conv [(__opcode) - CEE_LDIND_I1])
#define TREE_MOVER_STIND_NEEDS_CONVERSION(__opcode,__type) (((__opcode) != CEE_STIND_I) ? (stind_needs_conversion [(__opcode) - CEE_STIND_REF][(__type)]) : (stind_i_needs_conversion [(__type)]))
#define TREE_MOVER_LDIND_NEEDS_CONVERSION(__opcode,__type) (ldind_needs_conversion [(__opcode) - CEE_LDIND_I1][(__type)])

static void
tree_mover_print_act_slot (const char* message, MonoTreeMover *tree_mover, MonoTreeMoverActSlot *slot) {
	MonoTreeMoverDependencyNode *node;
	printf ("  [%s] Slot %d uses {", message, tree_mover_slot_to_index (tree_mover, slot));
	for (node = slot->used_locals; node != NULL; node = node->next_used_local) {
		printf (" %d", tree_mover_slot_to_index (tree_mover, node->used_slot));
	}
	printf (" } affects {");
	for (node = slot->affected_locals; node != NULL; node = node->next_affected_local) {
		printf (" %d", tree_mover_slot_to_index (tree_mover, node->affected_slot));
	}
	printf (" } R%d F%d W%d U%d", slot->pending_move_is_ready, slot->pending_move_is_forwarded, slot->waiting_flag, slot->unsafe_flag);
	if (slot->pending_move != NULL) {
		printf (" DEFINITION:");
		//printf (" DEFINITION[%p]:", slot->pending_move->definition);
		mono_print_tree (slot->pending_move->definition);
	}
	printf ("\n");
}

static MonoTreeMoverTreeMove*
mono_cprop_copy_values (MonoCompile *cfg, MonoTreeMover *tree_mover, MonoInst *tree, MonoInst **acp)
{
	MonoInst *cp;
	int arity;
	MonoTreeMoverTreeMove *pending_move = NULL;

	if (tree->ssa_op == MONO_SSA_LOAD && (tree->inst_i0->opcode == OP_LOCAL || tree->inst_i0->opcode == OP_ARG) &&
	    (cp = acp [tree->inst_i0->inst_c0]) && !tree->inst_i0->flags) {

		if (cp->opcode == OP_ICONST) {
			if (cfg->opt & MONO_OPT_CONSPROP) {
				//{ static int c = 0; printf ("CCOPY %d %d %s\n", c++, cp->inst_c0, mono_method_full_name (cfg->method, TRUE)); }
				if (MONO_DEBUG_LOCAL_PROP) {
					printf ("Propagating constant, tree ");
					mono_print_tree (tree);
					printf (" becomes ");
					mono_print_tree (cp);
					printf ("\n");
				}
				*tree = *cp;
			}
		} else {
			if ((tree->inst_i0->inst_vtype->type == cp->inst_vtype->type) ||
			    (tree->type == STACK_OBJ) || (tree->type == STACK_MP)) {
				if (cfg->opt & MONO_OPT_COPYPROP) {
					//{ static int c = 0; printf ("VCOPY %d\n", ++c); }
					if (MONO_DEBUG_LOCAL_PROP) {
						printf ("Propagating value, tree->inst_i0 ");
						mono_print_tree (tree->inst_i0);
						printf (" becomes ");
						mono_print_tree (cp);
						printf ("\n");
					}
					tree->inst_i0 = cp;
				}
			} else if (MONO_DEBUG_LOCAL_PROP) {
				char* tree_type_name = mono_type_full_name (tree->inst_i0->inst_vtype);
				char* cp_type_name = mono_type_full_name (cp->inst_vtype);
				printf ("Values of tree->inst_i0 ");
				mono_print_tree (tree->inst_i0);
				printf (" and cp ");
				mono_print_tree (cp);
				printf (" have incompatible types in tree ");
				mono_print_tree (tree);
				printf ("\n");
				printf (" MonoType of tree->inst_i0 is: %s\n", tree_type_name);
				printf (" MonoType of cp is: %s\n", cp_type_name);
				g_free (tree_type_name);
				g_free (cp_type_name);
			}
		}
	} else {
		if (MONO_DEBUG_LOCAL_PROP) {
			printf ("Propagation SKIPPED for inst ");
			mono_print_tree (tree);
			printf ("\n");
		}
		if ((tree_mover != NULL) && (cfg->opt & MONO_OPT_CFOLD))
			mono_constant_fold_inst (tree, NULL);

		arity = mono_burg_arity [tree->opcode];

		if (arity) {
			MonoTreeMoverTreeMove *result = mono_cprop_copy_values (cfg, tree_mover, tree->inst_i0, acp);
			if (cfg->opt & MONO_OPT_CFOLD)
				mono_constant_fold_inst (tree, NULL);
			if (result != NULL) {
				result->use = &(tree->inst_i0);
				//printf (" SETTING inst_i0[%p] USE to %p (definition is %p)\n", tree, result->use, result->definition);

			}
			/* The opcode may have changed */
			if (mono_burg_arity [tree->opcode] > 1) {
				if (cfg->opt & MONO_OPT_CFOLD)
					mono_constant_fold_inst (tree, NULL);
				result = mono_cprop_copy_values (cfg, tree_mover, tree->inst_i1, acp);
				if (result != NULL) {
					result->use = &(tree->inst_i1);
					//printf (" SETTING inst_i1[%p] USE to %p (definition is %p)\n", tree, result->use, result->definition);
				}
			}
			mono_constant_fold_inst (tree, NULL);
		}
	}
	
	/* Apply the tree mover after after propagation has been done */
	if ((tree_mover != NULL) && (tree->ssa_op == MONO_SSA_LOAD) &&
			(tree->inst_i0->opcode == OP_LOCAL || tree->inst_i0->opcode == OP_ARG)) {
		guint used_index = tree->inst_i0->inst_c0;
		MonoTreeMoverActSlot *used_slot = &(tree_mover->ACT [used_index]);
		
		/* First, handle waiting flag */
		if (used_slot->waiting_flag) {
			used_slot->unsafe_flag = TRUE;
			used_slot->waiting_flag = FALSE;
		}

		if (!tree->inst_i0->flags) {
			/* Record local use (the tree that contains this use might be movable) */
			tree_mover_add_used_node (tree_mover, used_slot, TRUE);

			/* Start working on the pending move... */
			pending_move = used_slot->pending_move;

			/* If there *is* a pending move... (otherwise, do nothing) */
			if (pending_move != NULL) {
				/* Check slot state */
				if (used_slot->pending_move_is_forwarded) {
					/* If the slot was a "hopefully dead" definition because of a forwarding... */
					if (MONO_DEBUG_TREE_MOVER) {
						printf ("Use should have been dead, killing slot %d: ", used_index);
						mono_print_tree_nl (tree);
						printf ("Also disabling forwarded definition at slot %d: ", tree_mover_slot_to_index (tree_mover, pending_move->defined_slot));
						mono_print_tree_nl (pending_move->definition);
					}
					/* ...clear the slot (which also disables the forwarded definition), and... */
					tree_mover_kill_act_slot_for_use (tree_mover, used_slot);
					/* ...clear the pending_move */
					pending_move = NULL;
				} else if (used_slot->pending_move_is_ready ||
						TREE_MOVER_STIND_NEEDS_CONVERSION (pending_move->definition->opcode, pending_move->definition->inst_i1->type) ||
						TREE_MOVER_LDIND_NEEDS_CONVERSION (tree->opcode, pending_move->definition->inst_i1->type)) {
					/* If the move was already in state [U], or if there are type problems... */
					if (MONO_DEBUG_TREE_MOVER) {
						printf ("Definition has too many, wrong or misplaced uses, killing slot %d: ", used_index);
						mono_print_tree_nl (tree);
					}
					/* ...kill it, and clear the pending_move */
					tree_mover_kill_act_slot_for_use (tree_mover, used_slot);
					pending_move = NULL;
				} else {
					/* All goes well: set slot state to [U] */
					MonoTreeMoverDependencyNode *node = used_slot->used_locals;
					if (MONO_DEBUG_TREE_MOVER) {
						printf ("Setting tree move for slot %d as ready: ", used_index);
						mono_print_tree_nl (tree);
					}
					/* Record indirect uses generated by this move */
					while (node != NULL) {
						tree_mover_add_used_node (tree_mover, node->used_slot, FALSE);
						node = node->next_used_local;
					}

					/* Setup tree as movable */
					used_slot->pending_move_is_ready = TRUE;
				}
			}
		} else {
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Tree has side effects, killing slot %d: ", used_index);
				mono_print_tree_nl (tree);
			}
			/* The whole tree is unmovable (it uses a flagged local) */
			tree_mover->tree_has_side_effects = TRUE;
			/* Moreover, the use of a flagged local kills the definition */
			tree_mover_kill_act_slot_for_use (tree_mover, used_slot);
		}
#if MONO_DUMP_TREE_MOVER
		tree_mover_print_act_slot ("USE", tree_mover, used_slot);
#endif
	}	
	return pending_move;
}

static void
mono_cprop_invalidate_values (MonoInst *tree, MonoTreeMover *tree_mover, MonoInst **acp, int acp_size)
{
	int arity;

	if (tree_mover != NULL) {
		if ((tree->opcode == CEE_NEWARR) || (mono_find_jit_opcode_emulation (tree->opcode) != NULL)) {
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Recording side effect because emulated opcode cannot be moved: ");
				mono_print_tree_nl (tree);
			}
			tree_mover->tree_has_side_effects = TRUE;
		}
	}

	switch (tree->opcode) {
	case CEE_LDIND_REF:
	case CEE_LDIND_I1:
	case CEE_LDIND_I2:
	case CEE_LDIND_I4:
	case CEE_LDIND_U1:
	case CEE_LDIND_U2:
	case CEE_LDIND_U4:
	case CEE_LDIND_I8:
	case CEE_LDIND_R4:
	case CEE_LDIND_R8:
	case CEE_LDIND_I:
	case CEE_LDOBJ:
		if ((tree_mover != NULL) && ((tree->ssa_op == MONO_SSA_NOP) || (tree->ssa_op & MONO_SSA_ADDRESS_TAKEN))) {
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Recording memory read at inst: ");
				mono_print_tree_nl (tree);
			}
			tree_mover->tree_reads_memory = TRUE;
		}
		break;
	case CEE_STIND_I:
	case CEE_STIND_I1:
	case CEE_STIND_I2:
	case CEE_STIND_I4:
	case CEE_STIND_REF:
	case CEE_STIND_I8:
	case CEE_STIND_R4:
	case CEE_STIND_R8:
	case CEE_STOBJ:
		if ((tree->ssa_op == MONO_SSA_NOP) || (tree->ssa_op & MONO_SSA_ADDRESS_TAKEN)) {
			if (MONO_DEBUG_LOCAL_PROP) {
				printf ("Indirect store clears ACP at tree ");
				mono_print_tree (tree);
				printf ("\n");
			}
			memset (acp, 0, sizeof (MonoInst *) * acp_size);
			if (tree_mover != NULL) {
				if (MONO_DEBUG_TREE_MOVER) {
					printf ("Killing all active slots (and recording side effect) because of inst ");
					mono_print_tree_nl (tree);
				}
				/* Note that this does *not* dispose ready moves (state [U]) */
				tree_mover_kill_act_for_indirect_local_definition (tree_mover, acp_size);
				tree_mover->tree_has_side_effects = TRUE;
			}
			return;
		}

		break;
	case CEE_CALL:
	case OP_CALL_REG:
	case CEE_CALLVIRT:
	case OP_LCALL_REG:
	case OP_LCALLVIRT:
	case OP_LCALL:
	case OP_FCALL_REG:
	case OP_FCALLVIRT:
	case OP_FCALL:
	case OP_VCALL_REG:
	case OP_VCALLVIRT:
	case OP_VCALL:
	case OP_VOIDCALL_REG:
	case OP_VOIDCALLVIRT:
	case OP_VOIDCALL: {
		MonoCallInst *call = (MonoCallInst *)tree;
		MonoMethodSignature *sig = call->signature;
		int i, byref = FALSE;

		if (tree_mover != NULL) {
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Recording side effect because of inst ");
				mono_print_tree_nl (tree);
			}
			tree_mover->tree_has_side_effects = TRUE;
		}

		for (i = 0; i < sig->param_count; i++) {
			if (sig->params [i]->byref) {
				byref = TRUE;
				break;
			}
		}

		if (byref) {
			if (MONO_DEBUG_LOCAL_PROP) {
				printf ("Call with byref parameter clears ACP at tree ");
				mono_print_tree (tree);
				printf ("\n");
			}
			memset (acp, 0, sizeof (MonoInst *) * acp_size);
			if (tree_mover != NULL) {
				if (MONO_DEBUG_TREE_MOVER) {
					printf ("Killing all active slots because of inst ");
					mono_print_tree_nl (tree);
				}
				tree_mover_kill_act_for_indirect_use (tree_mover, acp_size);
			}
		} else {
			if (tree_mover != NULL) {
				if (MONO_DEBUG_TREE_MOVER) {
					printf ("Killing all active slots reading memory because of inst ");
					mono_print_tree_nl (tree);
				}
				tree_mover_kill_act_for_indirect_global_definition (tree_mover, acp_size);
			}
		}
		return;
	}
	default:
		break;
	}

	arity = mono_burg_arity [tree->opcode];

	switch (arity) {
	case 0:
		break;
	case 1:
		mono_cprop_invalidate_values (tree->inst_i0, tree_mover, acp, acp_size);
		break;
	case 2:
		mono_cprop_invalidate_values (tree->inst_i0, tree_mover, acp, acp_size);
		mono_cprop_invalidate_values (tree->inst_i1, tree_mover, acp, acp_size);
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
mono_local_cprop_bb (MonoCompile *cfg, MonoTreeMover *tree_mover, MonoBasicBlock *bb, MonoInst **acp, int acp_size)
{
	MonoInst *tree = bb->code;
	int i;

	if (!tree)
		return;

	if (tree_mover != NULL) {
		tree_mover_set_waiting_flags (tree_mover, acp_size);
		if (MONO_DEBUG_TREE_MOVER) {
			printf ("Running tree mover on BB%d\n", bb->block_num);
		}
	}
	for (; tree; tree = tree->next) {
		if (tree_mover != NULL) {
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Running tree mover on tree ");
				mono_print_tree_nl (tree);
			}
			tree_mover->tree_has_side_effects = FALSE;
			tree_mover->tree_reads_memory = FALSE;
		}

		mono_cprop_copy_values (cfg, tree_mover, tree, acp);
		mono_cprop_invalidate_values (tree, tree_mover, acp, acp_size);
		if (MONO_DEBUG_TREE_MOVER) {
			if (tree_mover != NULL) {
				printf ("After the tree walk, tree_mover->tree_has_side_effects is %d\n", tree_mover->tree_has_side_effects);
			}
		}

		if (tree->ssa_op == MONO_SSA_STORE  &&
		    (tree->inst_i0->opcode == OP_LOCAL || tree->inst_i0->opcode == OP_ARG)) {
			MonoInst *i1 = tree->inst_i1;
			MonoTreeMoverActSlot *forwarding_source = NULL;
			gboolean tree_can_be_moved = TRUE;

			acp [tree->inst_i0->inst_c0] = NULL;
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Assignment clears ACP[%d] at tree ", tree->inst_i0->inst_c0);
				mono_print_tree (tree);
				printf ("\n");
			}

			for (i = 0; i < acp_size; i++) {
				if (acp [i] && acp [i]->opcode != OP_ICONST &&
				    acp [i]->inst_c0 == tree->inst_i0->inst_c0) {
					acp [i] = NULL;
					if (MONO_DEBUG_LOCAL_PROP) {
						printf ("  Consequently, ACP[%d] is cleared\n", i);
					}
				}
			}
			
			if (i1->opcode == OP_ICONST) {
				acp [tree->inst_i0->inst_c0] = i1;
				tree_can_be_moved = FALSE;
				if (MONO_DEBUG_LOCAL_PROP) {
					printf ("  Consequently, ACP[%d] becomes constant ", tree->inst_i0->inst_c0);
					mono_print_tree (i1);
					printf ("\n");
				}
				//printf ("DEF1 BB%d %d\n", bb->block_num,tree->inst_i0->inst_c0);
			} else if ((i1->type==STACK_I8) || (i1->opcode==OP_I8CONST) || (i1->opcode==OP_R4CONST) || (i1->opcode==OP_R8CONST) || (i1->opcode==OP_AOTCONST)) {
				tree_can_be_moved = FALSE;
				if (MONO_DEBUG_TREE_MOVER) {
					printf ("Preventing move of constant or long value ");
					mono_print_tree (i1);
					printf ("\n");
				}
			}
			if (i1->ssa_op == MONO_SSA_LOAD &&
			    (i1->inst_i0->opcode == OP_LOCAL || i1->inst_i0->opcode == OP_ARG) &&
			    (i1->inst_i0->inst_c0 != tree->inst_i0->inst_c0)) {
				acp [tree->inst_i0->inst_c0] = i1->inst_i0;
				tree_can_be_moved = FALSE;
				if (MONO_DEBUG_LOCAL_PROP) {
					printf ("  Consequently, ACP[%d] becomes local ", tree->inst_i0->inst_c0);
					mono_print_tree (i1->inst_i0);
					printf ("\n");
				}
				if (tree_mover != NULL) {
					/* Examine the variable *used* in this definition (the "source") */
					forwarding_source = tree_mover_slot_from_index (tree_mover, i1->inst_i0->inst_c0);
					/* Check if source slot is ready to be forwarded */
					if ((! forwarding_source->pending_move_is_ready) || (forwarding_source->pending_move->prevent_forwarding)) {
						/* no forwarding is possible, do nothing */
						forwarding_source = NULL;
					}
				}
				//printf ("DEF2 BB%d %d %d\n", bb->block_num,tree->inst_i0->inst_c0,i1->inst_i0->inst_c0);
			}
			
			/* Apply tree mover */
			if (tree_mover != NULL) {
				guint defined_index = tree->inst_i0->inst_c0;
				MonoTreeMoverActSlot *defined_slot = tree_mover_slot_from_index (tree_mover, defined_index);
				MonoTreeMoverDependencyNode *affected_node;
				
				/* First clear the waiting flag... */
				defined_slot->waiting_flag = FALSE;
				/* ...and kill this slot (but recording any pending move)*/
				tree_mover_kill_act_slot_for_definition (tree_mover, defined_slot);
				if (MONO_DEBUG_TREE_MOVER) {
					printf ("Definition is clearing slot %d\n", defined_index);
				}

				/* Handle "used" nodes... */
				/* Check if this is a forwarding */
				if (forwarding_source == NULL) {
					/* Normal case, no forwarding: */
					/* Check that consprop or copyprop did not already do the job, */
					/* and that the tree has no side effects */
					if (tree_can_be_moved && ! tree_mover->tree_has_side_effects) {
						MonoTreeMoverDependencyNode *affecting_node;
						if (MONO_DEBUG_TREE_MOVER) {
							printf ("Recording definition of slot %d by tree: ", defined_index);
							mono_print_tree_nl (tree);
						}

						/* Then apply the definition */
						tree_mover_new_slot_move (tree_mover, defined_slot);
						defined_slot->pending_move->definition = tree;
						defined_slot->pending_move->defined_slot = defined_slot;
						defined_slot->pending_move->tree_reads_memory = tree_mover->tree_reads_memory;

						/* Setup "used nodes" list */
						defined_slot->used_locals = tree_mover->used_nodes;
						defined_slot->last_used_local = tree_mover->last_used_node;
						tree_mover->used_nodes = NULL;
						tree_mover->last_used_node = NULL;
						/* Link used nodes to "affecting" slots (so affected variables are linked) */
						/* This is needed *now* so that circular definitions are detected */
						for (affecting_node = defined_slot->used_locals; affecting_node != NULL; affecting_node = affecting_node->next_used_local) {
							tree_mover_link_affecting_node (affecting_node, defined_slot);
						}
					} else if (MONO_DEBUG_TREE_MOVER) {
						/* otherwise, do nothing */
						printf ("Skipping definition of slot %d by tree: ", defined_index);
						mono_print_tree_nl (tree);
					}
				} else {
					MonoTreeMoverDependencyFromDeadDefinition *dependency;
					/* forwarding previous definition: */
					if (MONO_DEBUG_TREE_MOVER) {
						printf ("Handling forwarding in slot %d for tree: ", defined_index);
						mono_print_tree_nl (tree);
					}
					/* Setup slot for forwarding */
					defined_slot->pending_move = forwarding_source->pending_move;
					defined_slot->pending_move_is_forwarded = TRUE;
					/* Setup forwarding dependency node */
					dependency = mono_mempool_alloc0 (tree_mover->pool, sizeof (MonoTreeMoverDependencyFromDeadDefinition));
					dependency->defined_slot = defined_slot;
					dependency->dead_definition = tree;
					dependency->next = defined_slot->pending_move->slots_that_must_be_safe;
					defined_slot->pending_move->slots_that_must_be_safe = dependency;
					/* Clear use (put slot back to state [D]) */
					defined_slot->pending_move->use = NULL;
					defined_slot->pending_move->defined_slot->pending_move_is_ready = FALSE;
				}

				/* Then kill all affected definitions waiting for a use */
				affected_node = defined_slot->affected_locals;
				while (affected_node != NULL) {
					MonoTreeMoverDependencyNode *next_affected_node = affected_node->next_affected_local;
					MonoTreeMoverActSlot *affected_slot = affected_node->affected_slot;
					
					if (affected_node->use_is_direct) {
						/* Direct use: kill affected slot */
						if (MONO_DEBUG_TREE_MOVER) {
							printf ("  Direct use, killing slot %d with definition:", tree_mover_slot_to_index (tree_mover, affected_node->affected_slot));
							mono_print_tree_nl (affected_slot->pending_move->definition);
						}
						tree_mover_kill_act_slot_because_it_is_affected (tree_mover, affected_slot);
					} else if ((defined_slot->pending_move!= NULL) &&
							(! defined_slot->pending_move_is_ready) &&
							(! defined_slot->pending_move_is_forwarded) &&
							(affected_slot->pending_move!= NULL) &&
							(! affected_slot->pending_move_is_ready) &&
							(! affected_slot->pending_move_is_forwarded)) {
						if (MONO_DEBUG_TREE_MOVER) {
							printf ("  Indirect use, linking slots %d and %d\n", tree_mover_slot_to_index (tree_mover, affected_node->used_slot), tree_mover_slot_to_index (tree_mover, affected_node->affected_slot));
						}
						tree_mover_link_affected_moves (tree_mover, defined_slot, affected_slot);
						tree_mover_link_affected_moves (tree_mover, affected_slot, defined_slot);
					}
					tree_mover_unlink_affecting_node (affected_node);
					
					if ((next_affected_node != NULL) && (next_affected_node->affected_slot != NULL)) {
						affected_node = next_affected_node;
					} else {
						affected_node = defined_slot->affected_locals;
					}
				}
				if (MONO_DUMP_TREE_MOVER) {
					tree_mover_print_act_slot ("DEFINITION", tree_mover, defined_slot);
				}
			}
		}

		/* After we are done with this tree, clear the tree mover area */
		if ((tree_mover != NULL) && (tree_mover->used_nodes != NULL)) {
			tree_mover_dispose_used_nodes (tree_mover);
		}

		/*
		  if (tree->opcode == CEE_BEQ) {
		  g_assert (tree->inst_i0->opcode == OP_COMPARE);
		  if (tree->inst_i0->inst_i0->opcode == OP_ICONST &&
		  tree->inst_i0->inst_i1->opcode == OP_ICONST) {

		  tree->opcode = CEE_BR;
		  if (tree->inst_i0->inst_i0->opcode == tree->inst_i0->inst_i1->opcode) {
		  tree->inst_target_bb = tree->inst_true_bb;
		  } else {
		  tree->inst_target_bb = tree->inst_false_bb;
		  }
		  }
		  }
		*/
	}
	
	if (tree_mover != NULL) {
		/* At BB end, kill all definitions still waiting for a use */
		tree_mover_clear_act_recording_moves (tree_mover, acp_size);
		if (MONO_DEBUG_TREE_MOVER) {
			tree_mover_verify_dependency_nodes_are_clear (tree_mover, acp_size);
		}
	}
}


#if (MONO_APPLY_TREE_MOVER_TO_SINGLE_METHOD)
static char*
mono_tree_mover_method_name = NULL;
static gboolean check_tree_mover_method_name (MonoCompile *cfg) {
	if (mono_tree_mover_method_name == NULL) {
		mono_tree_mover_method_name = getenv ("MONO_TREE_MOVER_METHOD_NAME");
	}
	if (mono_tree_mover_method_name != NULL) {
		char *method_name = mono_method_full_name (cfg->method, TRUE);
		if (strstr (method_name, mono_tree_mover_method_name) != NULL) {
			g_free (method_name);
			return TRUE;
		} else {
			g_free (method_name);
			return FALSE;
		}
	} else {
		return TRUE;
	}
}
#endif

#if (MONO_APPLY_TREE_MOVER_TO_COUNTED_METHODS)
static int
mono_tree_mover_method_limit = -1;
static int
mono_tree_mover_method_count = 0;
static gboolean check_tree_mover_method_count (MonoCompile *cfg) {
	if (mono_tree_mover_method_limit == -1) {
		char *limit_string = getenv ("MONO_TREE_MOVER_METHOD_LIMIT");
		if (limit_string != NULL) {
			mono_tree_mover_method_limit = atoi (limit_string);
		} else {
			mono_tree_mover_method_limit = -2;
		}
	}
	if (mono_tree_mover_method_limit > -1) {
		mono_tree_mover_method_count ++;
		if (mono_tree_mover_method_count == mono_tree_mover_method_limit) {
			char *method_name = mono_method_full_name (cfg->method, TRUE);
			printf ("Last method compiled with treeprop: %s\n", method_name);
			g_free (method_name);
			
		}
		return (mono_tree_mover_method_count <= mono_tree_mover_method_limit);
	} else {
		return TRUE;
	}
}
#endif

static void
apply_tree_mover (MonoTreeMover *tree_mover, MonoTreeMoverTreeMove *move) {
	MonoTreeMoverDependencyFromDeadDefinition *dependency;
	MonoTreeMoverAffectedMove *affected_move;

	/* Test if this move has been explicitly disabled */
	if (move->skip_this_move) {
		if (MONO_DEBUG_TREE_MOVER) {
			printf ("Move of slot %d must be skipped: ", tree_mover_slot_to_index (tree_mover, move->defined_slot));
			mono_print_tree_nl (move->definition);
		}
		return;
	}
	/* Test if this move is safe */
	if ((! move->move_is_safe) && move->defined_slot->unsafe_flag) {
		if (MONO_DEBUG_TREE_MOVER) {
			printf ("Move of slot %d is unsafe: ", tree_mover_slot_to_index (tree_mover, move->defined_slot));
			mono_print_tree_nl (move->definition);
		}
		return;
	}
	/* Test if this move depends from a definition that should have been dead */
	for (dependency = move->slots_that_must_be_safe; dependency != NULL; dependency = dependency->next) {
		if ((dependency->defined_slot != NULL) && dependency->defined_slot->unsafe_flag) {
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Move of slot %d depended from unsafe slot %d: ", tree_mover_slot_to_index (tree_mover, move->defined_slot), tree_mover_slot_to_index (tree_mover, dependency->defined_slot));
				mono_print_tree_nl (move->definition);
			}
			return;
		}
	}

	if (MONO_DEBUG_TREE_MOVER) {
		printf ("Performing move of slot %d: ", tree_mover_slot_to_index (tree_mover, move->defined_slot));
		mono_print_tree_nl (move->definition);
	}
	/* All tests passed, apply move */
	*(move->use) = move->definition->inst_i1;
	move->definition->opcode = CEE_NOP;
	move->definition->ssa_op = MONO_SSA_NOP;

	/* Then disable moves affected by this move */
	affected_move = move->affected_moves;
	while (affected_move != NULL) {
		if (MONO_DEBUG_TREE_MOVER) {
			printf ("  Consequently, disabling slot %d\n", tree_mover_slot_to_index (tree_mover, affected_move->affected_move->defined_slot));
		}
		affected_move->affected_move->skip_this_move = TRUE;
		affected_move = affected_move->next_affected_move;
	}

	/* Also kill dead dependency definitions */
	for (dependency = move->slots_that_must_be_safe; dependency != NULL; dependency = dependency->next) {
		if (dependency->defined_slot != NULL) {
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("  Consequently, kill dependent definition %d: ", tree_mover_slot_to_index (tree_mover, dependency->defined_slot));
				mono_print_tree_nl (dependency->dead_definition);
			}
			dependency->dead_definition->opcode = CEE_NOP;
			dependency->dead_definition->ssa_op = MONO_SSA_NOP;
		}
	}
}

void
mono_local_cprop (MonoCompile *cfg) {
	MonoBasicBlock *bb;
	MonoInst **acp;
	MonoTreeMover *tree_mover;

	acp = alloca (sizeof (MonoInst *) * cfg->num_varinfo);
	
	if (cfg->opt & MONO_OPT_TREEPROP) {
		MonoMemPool *pool = mono_mempool_new();
		tree_mover = mono_mempool_alloc0(pool, sizeof (MonoTreeMover));
		
		tree_mover->cfg = cfg;
		tree_mover->pool = pool;
		tree_mover->ACT = mono_mempool_alloc0 (pool, sizeof (MonoTreeMoverActSlot) * (cfg->num_varinfo));		
#if (MONO_APPLY_TREE_MOVER_TO_SINGLE_METHOD)
		if (! check_tree_mover_method_name (cfg)) {
			mono_mempool_destroy(tree_mover->pool);
			tree_mover = NULL;
		}
#endif
#if (MONO_APPLY_TREE_MOVER_TO_COUNTED_METHODS)
		if (! check_tree_mover_method_count (cfg)) {
			mono_mempool_destroy(tree_mover->pool);
			tree_mover = NULL;
		}
#endif
	} else {
		tree_mover = NULL;
	}

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (MONO_DEBUG_LOCAL_PROP||MONO_DEBUG_TREE_MOVER) {
			printf ("Applying mono_local_cprop to BB%d\n", bb->block_num);
		}
		memset (acp, 0, sizeof (MonoInst *) * cfg->num_varinfo);
		mono_local_cprop_bb (cfg, tree_mover, bb, acp, cfg->num_varinfo);
	}
	
	if (tree_mover != NULL) {
		MonoTreeMoverTreeMove *move;
		/* Move the movable trees */
		if (MONO_DEBUG_TREE_MOVER) {
			printf ("BEFORE TREE MOVER START\n");
			mono_print_code (cfg);
			printf ("BEFORE TREE MOVER END\n");
			printf ("Applying tree mover...\n");
		}
		for (move = tree_mover->scheduled_moves; move != NULL; move = move->next) {
			apply_tree_mover (tree_mover, move);
		}
		if (MONO_DEBUG_TREE_MOVER) {
			printf ("AFTER TREE MOVER START\n");
			mono_print_code (cfg);
			printf ("AFTER TREE MOVER END\n");
		}
		
		/* Global cleanup of tree mover memory */
		mono_mempool_destroy(tree_mover->pool);
	}
}

