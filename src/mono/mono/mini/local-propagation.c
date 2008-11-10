/*
 * local-propagation.c: Local constant, copy and tree propagation.
 *
 * To make some sense of the tree mover, read mono/docs/tree-mover.txt
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

/* FIXME: Get rid of these */
#define NEW_BIALU(cfg,dest,op,dr,sr1,sr2) do { \
        MONO_INST_NEW ((cfg), (dest), (op)); \
        (dest)->dreg = (dr); \
        (dest)->sreg1 = (sr1); \
        (dest)->sreg2 = (sr2); \
	} while (0)

#define NEW_BIALU_IMM(cfg,dest,op,dr,sr,imm) do { \
        MONO_INST_NEW ((cfg), (dest), (op)); \
        (dest)->dreg = (dr);				 \
        (dest)->sreg1 = (sr);					   \
        (dest)->inst_p1 = (gpointer)(gssize)(imm); \
	} while (0)

#ifndef MONO_ARCH_IS_OP_MEMBASE
#define MONO_ARCH_IS_OP_MEMBASE(opcode) FALSE
#endif

#define MONO_DEBUG_LOCAL_PROP 0
#define MONO_DEBUG_TREE_MOVER 0
#define MONO_DUMP_TREE_MOVER 0
#define MONO_APPLY_TREE_MOVER_TO_SINGLE_METHOD 0
#define MONO_APPLY_TREE_MOVER_TO_COUNTED_METHODS 0

struct TreeMoverActSlot;
/*
 * A node describing one dependency between a tree and a local
 */
typedef struct TreeMoverDependencyNode {
	/* The local used in the tree */
	struct TreeMoverActSlot *used_slot;
	/* The local defined by the tree */
	struct TreeMoverActSlot *affected_slot;
	/* Next in the list of used locals */
	struct TreeMoverDependencyNode *next_used_local;
	/* Next in the list of affected locals */
	struct TreeMoverDependencyNode *next_affected_local;
	/* Previous in the list of affected locals */
	struct TreeMoverDependencyNode *previous_affected_local;
	/* False if the local is used in a tree that defines a used local */
	guchar use_is_direct;
} TreeMoverDependencyNode;

struct TreeMoverTreeMove;
/*
 * A node in a list of affected TreeMoverTreeMove
 */
typedef struct TreeMoverAffectedMove {
	struct TreeMoverTreeMove *affected_move;
	struct TreeMoverAffectedMove *next_affected_move;
} TreeMoverAffectedMove;

/*
 * A node in a list of TreeMoverDependencyFromDeadDefinition
 */
typedef struct TreeMoverDependencyFromDeadDefinition {
	/* The ACT slot of the defined local */
	struct TreeMoverActSlot *defined_slot;
	/* The definition that will hopefully be dead */
	MonoInst *dead_definition;
	/* Next in the list */
	struct TreeMoverDependencyFromDeadDefinition *next;
} TreeMoverDependencyFromDeadDefinition;

/*
 * A "tree move"
 */
typedef struct TreeMoverTreeMove {
	/* ACT slot of the defined local */
	struct TreeMoverActSlot *defined_slot;
	/* Code location of the definition */
	MonoInst *definition;
	/* Code location where the tree must be replaced with the local */
	MonoInst **use;
	/* Moves that must not be performed of we perform this one */
	TreeMoverAffectedMove *affected_moves;
	/* Definitions that must be dead to be allowed to perform this move */
	struct TreeMoverDependencyFromDeadDefinition *slots_that_must_be_safe;
	/* Next in the list of scheduled moves */
	struct TreeMoverTreeMove *next;
	/* The used tree accesses heap memory */
	guchar tree_reads_memory;
	/* A subsequent definitions makes this move globally safe */
	guchar move_is_safe;
	/* This move has been affected by something, ignore it */
	guchar skip_this_move;
	/* "tree forwarding" cannot continue for this definition */
	guchar prevent_forwarding;
} TreeMoverTreeMove;

/*
 * An ACT slot (there is one for each local in the ACT array)
 */
typedef struct TreeMoverActSlot {
	/* List of used locals (directly and indirectly) */
	TreeMoverDependencyNode *used_locals;
	/* Last element (so that we can move all the nodes quickly) */
	TreeMoverDependencyNode *last_used_local;
	/* List of affected locals (definitions that use this local) */
	TreeMoverDependencyNode *affected_locals;
	/* The current pending move */
	TreeMoverTreeMove *pending_move;
	/* True if the move has already met its use use */
	guchar pending_move_is_ready;
	/* See [W] flag */
	guchar waiting_flag;
	/* See [X] flag */
	guchar unsafe_flag;
	/* A "tree forwarding" is in progress */
	guchar pending_move_is_forwarded;
} TreeMoverActSlot;

/*
 * Main tree mover work area
 */
typedef struct TreeMover {
	/* Pool used for allocating everything */
	MonoMemPool *pool;
	/* Current method */
	MonoCompile *cfg;
	
	/* Free (recycled) TreeMoverDependencyNode structs */
	TreeMoverDependencyNode *free_nodes;
	/* Free (recycled) TreeMoverTreeMove structs */
	TreeMoverTreeMove *free_moves;

	/* ACT array */
	TreeMoverActSlot *ACT;
	/* List of tree moves that could be performed */
	TreeMoverTreeMove *scheduled_moves;

	/* The following fields are reset at each tree traversal */
	/* List of used locals */
	TreeMoverDependencyNode *used_nodes;
	/* Last node in the list (to free it in one block) */
	TreeMoverDependencyNode *last_used_node;
	/* The current tree cannot be moved (it can still receive moves!) */
	guchar tree_has_side_effects;
	/* The current tree reads heap locations */
	guchar tree_reads_memory;
} TreeMover;

inline static TreeMoverDependencyNode*
tree_mover_new_node (TreeMover *tree_mover) {
	TreeMoverDependencyNode *node;
	
	if (tree_mover->free_nodes != NULL) {
		node = tree_mover->free_nodes;
		tree_mover->free_nodes = tree_mover->free_nodes->next_used_local;
		node->next_used_local = NULL;
		node->next_affected_local = NULL;
		node->previous_affected_local = NULL;
	} else {
		node = (TreeMoverDependencyNode*) mono_mempool_alloc0 (tree_mover->pool, sizeof (TreeMoverDependencyNode));
	}
	
	return node;
}

inline static void
tree_mover_new_slot_move (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	TreeMoverTreeMove *move;
	
	if (tree_mover->free_moves != NULL) {
		move = tree_mover->free_moves;
		tree_mover->free_moves = tree_mover->free_moves->next;
		memset (move, 0, sizeof (TreeMoverTreeMove));
	} else {
		move = (TreeMoverTreeMove*) mono_mempool_alloc0 (tree_mover->pool, sizeof (TreeMoverTreeMove));
	}
	
	slot->pending_move = move;
}

inline static void
tree_mover_dispose_used_nodes (TreeMover *tree_mover) {
	tree_mover->last_used_node->next_used_local = tree_mover->free_nodes;
	tree_mover->free_nodes = tree_mover->used_nodes;
	tree_mover->used_nodes = NULL;
	tree_mover->last_used_node = NULL;
}

inline static void
tree_mover_dispose_slot_nodes (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	slot->last_used_local->next_used_local = tree_mover->free_nodes;
	tree_mover->free_nodes = slot->used_locals;
	slot->used_locals = NULL;
	slot->last_used_local = NULL;
}

inline static void
tree_mover_dispose_slot_move (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	slot->pending_move->next = tree_mover->free_moves;
	tree_mover->free_moves = slot->pending_move;
	slot->pending_move = NULL;
}

inline static TreeMoverActSlot*
tree_mover_slot_from_index (TreeMover *tree_mover, int index) {
	return & (tree_mover->ACT [index]);
}

inline static int
tree_mover_slot_to_index (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	return slot - tree_mover->ACT;
}

inline static void
tree_mover_add_used_node (TreeMover *tree_mover, TreeMoverActSlot *slot, gboolean use_is_direct) {
	TreeMoverDependencyNode *node;
	
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
tree_mover_link_affecting_node (TreeMoverDependencyNode *node, TreeMoverActSlot *affected_slot) {
	TreeMoverActSlot *affecting_slot = node->used_slot;
	node->affected_slot = affected_slot;
	node->next_affected_local = affecting_slot->affected_locals;
	affecting_slot->affected_locals = node;
	if (node->next_affected_local != NULL) {
		node->next_affected_local->previous_affected_local = node;
	}
	node->previous_affected_local = NULL;
}

inline static void
tree_mover_unlink_affecting_node (TreeMoverDependencyNode *node) {
	if (node->next_affected_local != NULL) {
		node->next_affected_local->previous_affected_local = node->previous_affected_local;
	}
	if (node->previous_affected_local != NULL) {
		node->previous_affected_local->next_affected_local = node->next_affected_local;
	} else {
		TreeMoverActSlot *slot = node->used_slot;
		slot->affected_locals = node->next_affected_local;
	}
	node->next_affected_local = NULL;
	node->previous_affected_local = NULL;
	node->affected_slot = NULL;
}

inline static void
tree_mover_link_affected_moves (TreeMover *tree_mover, TreeMoverActSlot *source_slot, TreeMoverActSlot *destination_slot) {
	TreeMoverAffectedMove *node = (TreeMoverAffectedMove*) mono_mempool_alloc0 (tree_mover->pool, sizeof (TreeMoverAffectedMove));
	node->affected_move = destination_slot->pending_move; 
	node->next_affected_move = source_slot->pending_move->affected_moves;
	source_slot->pending_move->affected_moves = node;
}


inline static void
tree_mover_record_pending_move (TreeMover *tree_mover, TreeMoverActSlot *slot, gboolean move_is_safe) {
	if (slot->pending_move_is_ready) {
		slot->pending_move->move_is_safe = move_is_safe;
		slot->pending_move->next = tree_mover->scheduled_moves;
		tree_mover->scheduled_moves = slot->pending_move;
		slot->pending_move = NULL;
		slot->pending_move_is_ready = FALSE;
	}
}

inline static void
tree_mover_clear_forwarding_dependency (TreeMoverActSlot *slot) {
	if (slot->pending_move_is_forwarded) {
		TreeMoverDependencyFromDeadDefinition *dependency = slot->pending_move->slots_that_must_be_safe;
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
tree_mover_enforce_forwarding_dependency (TreeMoverActSlot *slot) {
	if (slot->pending_move_is_forwarded) {
		slot->pending_move->skip_this_move = TRUE;
		slot->pending_move_is_forwarded = FALSE;
		slot->pending_move = NULL;
	}
}

inline static void
tree_mover_clean_act_slot_dependency_nodes (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	TreeMoverDependencyNode *current_node = slot->used_locals;
	while (current_node != NULL) {
		tree_mover_unlink_affecting_node (current_node);
		current_node = current_node->next_used_local;
	}
	if (slot->used_locals != NULL) {
		tree_mover_dispose_slot_nodes (tree_mover, slot);
	}
}

inline static void
tree_mover_clean_act_slot_pending_move (TreeMover *tree_mover, TreeMoverActSlot *slot) {
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
tree_mover_clean_act_slot (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	tree_mover_clean_act_slot_dependency_nodes (tree_mover, slot);
	tree_mover_clean_act_slot_pending_move (tree_mover, slot);
}

inline static void
tree_mover_kill_act_slot_for_definition (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	tree_mover_record_pending_move (tree_mover, slot, TRUE);
	tree_mover_clear_forwarding_dependency (slot);
	tree_mover_clean_act_slot (tree_mover, slot);
}

inline static void
tree_mover_kill_act_slot_because_it_is_affected (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	if ((! slot->pending_move_is_ready) && (! slot->pending_move_is_forwarded)) {
		tree_mover_clean_act_slot (tree_mover, slot);
	}
}

inline static void
tree_mover_kill_act_slot_for_use (TreeMover *tree_mover, TreeMoverActSlot *slot) {
	tree_mover_enforce_forwarding_dependency (slot);
	tree_mover_clean_act_slot (tree_mover, slot);
}

inline static void
tree_mover_kill_act_for_indirect_local_definition (TreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		TreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		if (slot->pending_move != NULL) {
			slot->pending_move->prevent_forwarding = TRUE;
		}
		tree_mover_kill_act_slot_because_it_is_affected (tree_mover, slot);
	}
}

inline static void
tree_mover_kill_act_for_indirect_global_definition (TreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		TreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		if ((slot->pending_move != NULL) && slot->pending_move->tree_reads_memory) {
			tree_mover_kill_act_slot_because_it_is_affected (tree_mover, slot);
		}
	}
}

inline static void
tree_mover_kill_act_for_indirect_use (TreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		TreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		tree_mover_kill_act_slot_for_use (tree_mover, slot);
	}
}

inline static void
tree_mover_clear_act_recording_moves (TreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		TreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		tree_mover_record_pending_move (tree_mover, slot, FALSE);
		tree_mover_clean_act_slot (tree_mover, slot);
	}
}

inline static void
tree_mover_set_waiting_flags (TreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		TreeMoverActSlot *slot = &(tree_mover->ACT [i]);
		slot->waiting_flag = TRUE;
	}
}

inline static void
tree_mover_verify_dependency_nodes_are_clear (TreeMover *tree_mover, int size) {
	int i;
	for (i = 0; i < size; i++) {
		TreeMoverActSlot *slot = &(tree_mover->ACT [i]);
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
tree_mover_print_act_slot (const char* message, TreeMover *tree_mover, TreeMoverActSlot *slot) {
	TreeMoverDependencyNode *node;
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

static TreeMoverTreeMove*
mono_cprop_copy_values (MonoCompile *cfg, TreeMover *tree_mover, MonoInst *tree, MonoInst **acp)
{
	MonoInst *cp;
	int arity;
	TreeMoverTreeMove *pending_move = NULL;

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
			MonoType *inst_i0_underlying_type = mono_type_get_underlying_type (tree->inst_i0->inst_vtype);
			MonoType *cp_underlying_type = mono_type_get_underlying_type (cp->inst_vtype);
			if ((inst_i0_underlying_type->type == cp_underlying_type->type) ||
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
			TreeMoverTreeMove *result = mono_cprop_copy_values (cfg, tree_mover, tree->inst_i0, acp);
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
		TreeMoverActSlot *used_slot = &(tree_mover->ACT [used_index]);
		
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
					TreeMoverDependencyNode *node = used_slot->used_locals;
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
mono_cprop_invalidate_values (MonoInst *tree, TreeMover *tree_mover, MonoInst **acp, int acp_size)
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
	case OP_CALL:
	case OP_CALL_REG:
	case OP_CALLVIRT:
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
	case OP_VOIDCALL:
	case OP_TRAMPCALL_VTABLE:
	case OP_CALL_RGCTX:
	case OP_FCALL_RGCTX:
	case OP_VOIDCALL_RGCTX:
	case OP_LCALL_RGCTX:
	case OP_VCALL_RGCTX:
	case OP_CALL_REG_RGCTX:
	case OP_FCALL_REG_RGCTX:
	case OP_VOIDCALL_REG_RGCTX:
	case OP_LCALL_REG_RGCTX:
	case OP_VCALL_REG_RGCTX:
	case OP_CALLVIRT_IMT:
	case OP_VOIDCALLVIRT_IMT:
	case OP_FCALLVIRT_IMT:
	case OP_LCALLVIRT_IMT:
	case OP_VCALLVIRT_IMT: {
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
#define TREEMOVE_SPECIFIC_OPS 1
#define OPDEF(a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) case a1:
#include "simple-cee-ops.h"
#undef OPDEF
#define MINI_OP(a1,a2) case a1:
#include "simple-mini-ops.h"
#undef MINI_OP
#undef TREEMOVE_SPECIFIC_OPS
		break;
	default:
		if (tree_mover != NULL) {
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Recording side effect because of inst ");
				mono_print_tree_nl (tree);
			}
			tree_mover->tree_has_side_effects = TRUE;
		}
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
mono_local_cprop_bb (MonoCompile *cfg, TreeMover *tree_mover, MonoBasicBlock *bb, MonoInst **acp, int acp_size)
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
	MONO_BB_FOR_EACH_INS (bb, tree) {
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
			TreeMoverActSlot *forwarding_source = NULL;
			gboolean tree_can_be_moved = TRUE;

			acp [tree->inst_i0->inst_c0] = NULL;
			if (MONO_DEBUG_TREE_MOVER) {
				printf ("Assignment clears ACP[%d] at tree ", (int)tree->inst_i0->inst_c0);
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
					printf ("  Consequently, ACP[%ld] becomes constant ", (long)tree->inst_i0->inst_c0);
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
					printf ("  Consequently, ACP[%d] becomes local ", (int)tree->inst_i0->inst_c0);
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
				TreeMoverActSlot *defined_slot = tree_mover_slot_from_index (tree_mover, defined_index);
				TreeMoverDependencyNode *affected_node;
				
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
						TreeMoverDependencyNode *affecting_node;
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
					TreeMoverDependencyFromDeadDefinition *dependency;
					/* forwarding previous definition: */
					if (MONO_DEBUG_TREE_MOVER) {
						printf ("Handling forwarding in slot %d for tree: ", defined_index);
						mono_print_tree_nl (tree);
					}
					/* Setup slot for forwarding */
					defined_slot->pending_move = forwarding_source->pending_move;
					defined_slot->pending_move_is_forwarded = TRUE;
					/* Setup forwarding dependency node */
					dependency = mono_mempool_alloc0 (tree_mover->pool, sizeof (TreeMoverDependencyFromDeadDefinition));
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
					TreeMoverDependencyNode *next_affected_node = affected_node->next_affected_local;
					TreeMoverActSlot *affected_slot = affected_node->affected_slot;
					
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

		  tree->opcode = OP_BR;
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
apply_tree_mover (TreeMover *tree_mover, TreeMoverTreeMove *move) {
	TreeMoverDependencyFromDeadDefinition *dependency;
	TreeMoverAffectedMove *affected_move;

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
	move->definition->opcode = OP_NOP;
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
			dependency->dead_definition->opcode = OP_NOP;
			dependency->dead_definition->ssa_op = MONO_SSA_NOP;
		}
	}
}

void
mono_local_cprop (MonoCompile *cfg) {
	MonoBasicBlock *bb;
	MonoInst **acp;
	TreeMover *tree_mover;

	acp = alloca (sizeof (MonoInst *) * cfg->num_varinfo);
	
	if (cfg->opt & MONO_OPT_TREEPROP) {
		MonoMemPool *pool = mono_mempool_new();
		tree_mover = mono_mempool_alloc0(pool, sizeof (TreeMover));
		
		tree_mover->cfg = cfg;
		tree_mover->pool = pool;
		tree_mover->ACT = mono_mempool_alloc0 (pool, sizeof (TreeMoverActSlot) * (cfg->num_varinfo));		
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
		TreeMoverTreeMove *move;
		/* Move the movable trees */
		if (MONO_DEBUG_TREE_MOVER) {
			mono_print_code (cfg, "BEFORE TREE MOVER");
			printf ("Applying tree mover...\n");
		}
		for (move = tree_mover->scheduled_moves; move != NULL; move = move->next) {
			apply_tree_mover (tree_mover, move);
		}
		if (MONO_DEBUG_TREE_MOVER) {
			mono_print_code (cfg, "AFTER TREE MOVER");
		}
		
		/* Global cleanup of tree mover memory */
		mono_mempool_destroy(tree_mover->pool);
	}
}

static inline MonoBitSet* 
mono_bitset_mp_new_noinit (MonoMemPool *mp,  guint32 max_size)
{
	int size = mono_bitset_alloc_size (max_size, 0);
	gpointer mem;

	mem = mono_mempool_alloc (mp, size);
	return mono_bitset_mem_new (mem, max_size, MONO_BITSET_DONT_FREE);
}

/*
 * mono_local_cprop2:
 *
 *  A combined local copy and constant propagation pass.
 */
void
mono_local_cprop2 (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	MonoInst **defs;
	gint32 *def_index;
	int max;

restart:

	max = cfg->next_vreg;
	defs = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * (cfg->next_vreg + 1));
	def_index = mono_mempool_alloc (cfg->mempool, sizeof (guint32) * (cfg->next_vreg + 1));

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		int ins_index;
		int last_call_index;

		/* Manually init the defs entries used by the bblock */
		MONO_BB_FOR_EACH_INS (bb, ins) {
			if ((ins->dreg != -1) && (ins->dreg < max)) {
				defs [ins->dreg] = NULL;
#if SIZEOF_VOID_P == 4
				defs [ins->dreg + 1] = NULL;
#endif
			}
			if ((ins->sreg1 != -1) && (ins->sreg1 < max)) {
				defs [ins->sreg1] = NULL;
#if SIZEOF_VOID_P == 4
				defs [ins->sreg1 + 1] = NULL;
#endif
			}
			if ((ins->sreg2 != -1) && (ins->sreg2 < max)) {
				defs [ins->sreg2] = NULL;
#if SIZEOF_VOID_P == 4
				defs [ins->sreg2 + 1] = NULL;
#endif
			}
		}

		ins_index = 0;
		last_call_index = -1;
		MONO_BB_FOR_EACH_INS (bb, ins) {
			const char *spec = INS_INFO (ins->opcode);
			int regtype, srcindex, sreg;

			if (ins->opcode == OP_NOP) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}

			g_assert (ins->opcode > MONO_CEE_LAST);

			/* FIXME: Optimize this */
			if (ins->opcode == OP_LDADDR) {
				MonoInst *var = ins->inst_p0;

				defs [var->dreg] = NULL;
				/*
				if (!MONO_TYPE_ISSTRUCT (var->inst_vtype))
					break;
				*/
			}

			if (MONO_IS_STORE_MEMBASE (ins)) {
				sreg = ins->dreg;
				regtype = 'i';

				if ((regtype == 'i') && (sreg != -1) && defs [sreg]) {
					MonoInst *def = defs [sreg];

					if ((def->opcode == OP_MOVE) && (!defs [def->sreg1] || (def_index [def->sreg1] < def_index [sreg])) && !vreg_is_volatile (cfg, def->sreg1)) {
						int vreg = def->sreg1;
						//printf ("CCOPY: R%d -> R%d\n", sreg, vreg);
						ins->dreg = vreg;
					}
				}
			}

			for (srcindex = 0; srcindex < 2; ++srcindex) {
				MonoInst *def;

				regtype = srcindex == 0 ? spec [MONO_INST_SRC1] : spec [MONO_INST_SRC2];
				sreg = srcindex == 0 ? ins->sreg1 : ins->sreg2;

				if ((regtype == ' ') || (sreg == -1) || (!defs [sreg]))
					continue;

				def = defs [sreg];

				/* Copy propagation */
				/* 
				 * The first check makes sure the source of the copy did not change since 
				 * the copy was made.
				 * The second check avoids volatile variables.
				 * The third check avoids copy propagating local vregs through a call, 
				 * since the lvreg will be spilled 
				 * The fourth check avoids copy propagating a vreg in cases where
				 * it would be eliminated anyway by reverse copy propagation later,
				 * because propagating it would create another use for it, thus making 
				 * it impossible to use reverse copy propagation.
				 */
				/* Enabling this for floats trips up the fp stack */
				/* 
				 * Enabling this for floats on amd64 seems to cause a failure in 
				 * basic-math.cs, most likely because it gets rid of some r8->r4 
				 * conversions.
				 */
				if (MONO_IS_MOVE (def) &&
					(!defs [def->sreg1] || (def_index [def->sreg1] < def_index [sreg])) &&
					!vreg_is_volatile (cfg, def->sreg1) &&
					/* This avoids propagating local vregs across calls */
					((get_vreg_to_inst (cfg, def->sreg1) || !defs [def->sreg1] || (def_index [def->sreg1] >= last_call_index) || (def->opcode == OP_VMOVE))) &&
					!(defs [def->sreg1] && defs [def->sreg1]->next == def) &&
					(!MONO_ARCH_USE_FPSTACK || (def->opcode != OP_FMOVE)) &&
					(def->opcode != OP_FMOVE)) {
					int vreg = def->sreg1;

					//printf ("CCOPY: R%d -> R%d\n", sreg, vreg);
					if (srcindex == 0)
						ins->sreg1 = vreg;
					else
						ins->sreg2 = vreg;

					/* Allow further iterations */
					srcindex = -1;
					continue;
				}

				/* Constant propagation */
				/* FIXME: Make is_inst_imm a macro */
				/* FIXME: Make is_inst_imm take an opcode argument */
				/* is_inst_imm is only needed for binops */
				if ((((def->opcode == OP_ICONST) || ((sizeof (gpointer) == 8) && (def->opcode == OP_I8CONST))) &&
					 (((srcindex == 0) && (ins->sreg2 == -1)) || mono_arch_is_inst_imm (def->inst_c0))) || 
					(!MONO_ARCH_USE_FPSTACK && (def->opcode == OP_R8CONST))) {
					guint32 opcode2;

					/* srcindex == 1 -> binop, ins->sreg2 == -1 -> unop */
					if ((srcindex == 1) && (ins->sreg1 != -1) && defs [ins->sreg1] && (defs [ins->sreg1]->opcode == OP_ICONST) && defs [ins->sreg2]) {
						/* Both arguments are constants, perform cfold */
						mono_constant_fold_ins2 (cfg, ins, defs [ins->sreg1], defs [ins->sreg2], TRUE);
					} else if ((srcindex == 0) && (ins->sreg2 != -1) && defs [ins->sreg2]) {
						/* Arg 1 is constant, swap arguments if possible */
						int opcode = ins->opcode;
						mono_constant_fold_ins2 (cfg, ins, defs [ins->sreg1], defs [ins->sreg2], TRUE);
						if (ins->opcode != opcode) {
							/* Allow further iterations */
							srcindex = -1;
							continue;
						}
					} else if ((srcindex == 0) && (ins->sreg2 == -1)) {
						/* Constant unop, perform cfold */
						mono_constant_fold_ins2 (cfg, ins, defs [ins->sreg1], NULL, TRUE);
					}

					opcode2 = mono_op_to_op_imm (ins->opcode);
					if ((opcode2 != -1) && mono_arch_is_inst_imm (def->inst_c0) && ((srcindex == 1) || (ins->sreg2 == -1))) {
						ins->opcode = opcode2;
						if ((def->opcode == OP_I8CONST) && (sizeof (gpointer) == 4)) {
							ins->inst_ls_word = def->inst_ls_word;
							ins->inst_ms_word = def->inst_ms_word;
						} else {
							ins->inst_imm = def->inst_c0;
						}
						if (srcindex == 0)
							ins->sreg1 = -1;
						else
							ins->sreg2 = -1;

						if ((opcode2 == OP_VOIDCALL) || (opcode2 == OP_CALL) || (opcode2 == OP_LCALL) || (opcode2 == OP_FCALL))
							((MonoCallInst*)ins)->fptr = (gpointer)ins->inst_imm;

						/* Allow further iterations */
						srcindex = -1;
						continue;
					}
					else {
						/* Special cases */
#if defined(__i386__) || defined(__x86_64__)
						if ((ins->opcode == OP_X86_LEA) && (srcindex == 1)) {
#if SIZEOF_VOID_P == 8
							/* FIXME: Use OP_PADD_IMM when the new JIT is done */
							ins->opcode = OP_LADD_IMM;
#else
							ins->opcode = OP_ADD_IMM;
#endif
							ins->inst_imm += def->inst_c0 << ins->backend.shift_amount;
							ins->sreg2 = -1;
						}
#endif
						opcode2 = mono_load_membase_to_load_mem (ins->opcode);
						if ((srcindex == 0) && (opcode2 != -1) && mono_arch_is_inst_imm (def->inst_c0)) {
							ins->opcode = opcode2;
							ins->inst_imm = def->inst_c0 + ins->inst_offset;
							ins->sreg1 = -1;
						}
					}
				}
				else if (((def->opcode == OP_ADD_IMM) || (def->opcode == OP_LADD_IMM)) && (MONO_IS_LOAD_MEMBASE (ins) || MONO_ARCH_IS_OP_MEMBASE (ins->opcode))) {
					/* ADD_IMM is created by spill_global_vars */
					/* 
					 * We have to guarantee that def->sreg1 haven't changed since def->dreg
					 * was defined. cfg->frame_reg is assumed to remain constant.
					 */
					if ((def->sreg1 == cfg->frame_reg) || ((def->next == ins) && (def->dreg != def->sreg1))) {
						ins->inst_basereg = def->sreg1;
						ins->inst_offset += def->inst_imm;
					}
				} else if ((ins->opcode == OP_ISUB_IMM) && (def->opcode == OP_IADD_IMM) && (def->next == ins)) {
					ins->sreg1 = def->sreg1;
					ins->inst_imm -= def->inst_imm;
				} else if ((ins->opcode == OP_IADD_IMM) && (def->opcode == OP_ISUB_IMM) && (def->next == ins)) {
					ins->sreg1 = def->sreg1;
					ins->inst_imm -= def->inst_imm;
				} else if (ins->opcode == OP_STOREI1_MEMBASE_REG &&
						   (def->opcode == OP_ICONV_TO_U1 || def->opcode == OP_ICONV_TO_I1 || def->opcode == OP_SEXT_I4 || (SIZEOF_VOID_P == 8 && def->opcode == OP_LCONV_TO_U1)) &&
						   (!defs [def->sreg1] || (def_index [def->sreg1] < def_index [sreg]))) {
					/* Avoid needless sign extension */
					ins->sreg1 = def->sreg1;
				} else if (ins->opcode == OP_STOREI2_MEMBASE_REG &&
						   (def->opcode == OP_ICONV_TO_U2 || def->opcode == OP_ICONV_TO_I2 || def->opcode == OP_SEXT_I4 || (SIZEOF_VOID_P == 8 && def->opcode == OP_LCONV_TO_I2)) &&
						   (!defs [def->sreg1] || (def_index [def->sreg1] < def_index [sreg]))) {
					/* Avoid needless sign extension */
					ins->sreg1 = def->sreg1;
				}
			}

			/* Do strength reduction here */
			/* FIXME: Add long/float */
			switch (ins->opcode) {
			case OP_MOVE:
			case OP_XMOVE:
				if (ins->dreg == ins->sreg1) {
					MONO_DELETE_INS (bb, ins);
					spec = INS_INFO (ins->opcode);
				}
				break;
			case OP_ADD_IMM:
			case OP_IADD_IMM:
			case OP_SUB_IMM:
			case OP_ISUB_IMM:
#if SIZEOF_VOID_P == 8
			case OP_LADD_IMM:
			case OP_LSUB_IMM:
#endif
				if (ins->inst_imm == 0) {
					ins->opcode = OP_MOVE;
					spec = INS_INFO (ins->opcode);
				}
				break;
			case OP_MUL_IMM:
			case OP_IMUL_IMM:
#if SIZEOF_VOID_P == 8
			case OP_LMUL_IMM:
#endif
				if (ins->inst_imm == 0) {
					ins->opcode = (ins->opcode == OP_LMUL_IMM) ? OP_I8CONST : OP_ICONST;
					ins->inst_c0 = 0;
					ins->sreg1 = -1;
				} else if (ins->inst_imm == 1) {
					ins->opcode = OP_MOVE;
				} else if ((ins->opcode == OP_IMUL_IMM) && (ins->inst_imm == -1)) {
					ins->opcode = OP_INEG;
				} else if ((ins->opcode == OP_LMUL_IMM) && (ins->inst_imm == -1)) {
					ins->opcode = OP_LNEG;
				} else {
					int power2 = mono_is_power_of_two (ins->inst_imm);
					if (power2 >= 0) {
						ins->opcode = (ins->opcode == OP_MUL_IMM) ? OP_SHL_IMM : ((ins->opcode == OP_LMUL_IMM) ? OP_LSHL_IMM : OP_ISHL_IMM);
						ins->inst_imm = power2;
					}
				}
				spec = INS_INFO (ins->opcode);
				break;
			case OP_IREM_UN_IMM:
			case OP_IDIV_UN_IMM: {
				int c = ins->inst_imm;
				int power2 = mono_is_power_of_two (c);

				if (power2 >= 0) {
					if (ins->opcode == OP_IREM_UN_IMM) {
						ins->opcode = OP_IAND_IMM;
						ins->sreg2 = -1;
						ins->inst_imm = (1 << power2) - 1;
					} else if (ins->opcode == OP_IDIV_UN_IMM) {
						ins->opcode = OP_ISHR_UN_IMM;
						ins->sreg2 = -1;
						ins->inst_imm = power2;
					}
				}
				spec = INS_INFO (ins->opcode);
				break;
			}
			case OP_IDIV_IMM: {
				int c = ins->inst_imm;
				int power2 = mono_is_power_of_two (c);
				MonoInst *tmp1, *tmp2, *tmp3, *tmp4;

				/* FIXME: Move this elsewhere cause its hard to implement it here */
				if (power2 == 1) {
					int r1 = mono_alloc_ireg (cfg);

					NEW_BIALU_IMM (cfg, tmp1, OP_ISHR_UN_IMM, r1, ins->sreg1, 31);
					mono_bblock_insert_after_ins (bb, ins, tmp1);
					NEW_BIALU (cfg, tmp2, OP_IADD, r1, r1, ins->sreg1);
					mono_bblock_insert_after_ins (bb, tmp1, tmp2);
					NEW_BIALU_IMM (cfg, tmp3, OP_ISHR_IMM, ins->dreg, r1, 1);
					mono_bblock_insert_after_ins (bb, tmp2, tmp3);

					NULLIFY_INS (ins);

					// We allocated a new vreg, so need to restart
					goto restart;
				} else if (power2 > 0) {
					int r1 = mono_alloc_ireg (cfg);

					NEW_BIALU_IMM (cfg, tmp1, OP_ISHR_IMM, r1, ins->sreg1, 31);
					mono_bblock_insert_after_ins (bb, ins, tmp1);
					NEW_BIALU_IMM (cfg, tmp2, OP_ISHR_UN_IMM, r1, r1, (32 - power2));
					mono_bblock_insert_after_ins (bb, tmp1, tmp2);
					NEW_BIALU (cfg, tmp3, OP_IADD, r1, r1, ins->sreg1);
					mono_bblock_insert_after_ins (bb, tmp2, tmp3);
					NEW_BIALU_IMM (cfg, tmp4, OP_ISHR_IMM, ins->dreg, r1, power2);
					mono_bblock_insert_after_ins (bb, tmp3, tmp4);

					NULLIFY_INS (ins);

					// We allocated a new vreg, so need to restart
					goto restart;
				}
				break;
			}
			}
			
			if (spec [MONO_INST_DEST] != ' ') {
				MonoInst *def = defs [ins->dreg];

				if (def && (def->opcode == OP_ADD_IMM) && (def->sreg1 == cfg->frame_reg) && (MONO_IS_STORE_MEMBASE (ins))) {
					/* ADD_IMM is created by spill_global_vars */
					/* cfg->frame_reg is assumed to remain constant */
					ins->inst_destbasereg = def->sreg1;
					ins->inst_offset += def->inst_imm;
				}
			}
			
			if ((spec [MONO_INST_DEST] != ' ') && !MONO_IS_STORE_MEMBASE (ins) && !vreg_is_volatile (cfg, ins->dreg)) {
				defs [ins->dreg] = ins;
				def_index [ins->dreg] = ins_index;
			}

			if (MONO_IS_CALL (ins))
				last_call_index = ins_index;

			ins_index ++;
		}
	}
}

static inline gboolean
reg_is_softreg_no_fpstack (int reg, const char spec)
{
	return (spec == 'i' && reg > MONO_MAX_IREGS)
		|| ((spec == 'f' && reg > MONO_MAX_FREGS) && !MONO_ARCH_USE_FPSTACK)
#ifdef MONO_ARCH_SIMD_INTRINSICS
		|| (spec == 'x' && reg > MONO_MAX_XREGS)
#endif
		|| (spec == 'v');
}
		
static inline gboolean
reg_is_softreg (int reg, const char spec)
{
	return (spec == 'i' && reg > MONO_MAX_IREGS)
		|| (spec == 'f' && reg > MONO_MAX_FREGS)
#ifdef MONO_ARCH_SIMD_INTRINSICS
		|| (spec == 'x' && reg > MONO_MAX_XREGS)
#endif
		|| (spec == 'v');
}

/**
 * mono_local_deadce:
 *
 *   Get rid of the dead assignments to local vregs like the ones created by the 
 * copyprop pass.
 */
void
mono_local_deadce (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	MonoInst *ins, *prev;
	MonoBitSet *used, *defined;

	//mono_print_code (cfg, "BEFORE LOCAL-DEADCE");

	/*
	 * Assignments to global vregs can't be eliminated so this pass must come
	 * after the handle_global_vregs () pass.
	 */

	used = mono_bitset_mp_new_noinit (cfg->mempool, cfg->next_vreg + 1);
	defined = mono_bitset_mp_new_noinit (cfg->mempool, cfg->next_vreg + 1);

	/* First pass: collect liveness info */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		/* Manually init the defs entries used by the bblock */
		MONO_BB_FOR_EACH_INS (bb, ins) {
			const char *spec = INS_INFO (ins->opcode);

			if (spec [MONO_INST_DEST] != ' ') {
				mono_bitset_clear_fast (used, ins->dreg);
				mono_bitset_clear_fast (defined, ins->dreg);
#if SIZEOF_VOID_P == 4
				/* Regpairs */
				mono_bitset_clear_fast (used, ins->dreg + 1);
				mono_bitset_clear_fast (defined, ins->dreg + 1);
#endif
			}
			if (spec [MONO_INST_SRC1] != ' ') {
				mono_bitset_clear_fast (used, ins->sreg1);
#if SIZEOF_VOID_P == 4
				mono_bitset_clear_fast (used, ins->sreg1 + 1);
#endif
			}
			if (spec [MONO_INST_SRC2] != ' ') {
				mono_bitset_clear_fast (used, ins->sreg2);
#if SIZEOF_VOID_P == 4
				mono_bitset_clear_fast (used, ins->sreg2 + 1);
#endif
			}
		}

		/*
		 * Make a reverse pass over the instruction list
		 */
		MONO_BB_FOR_EACH_INS_REVERSE_SAFE (bb, prev, ins) {
			const char *spec = INS_INFO (ins->opcode);

			if (ins->opcode == OP_NOP) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}

			g_assert (ins->opcode > MONO_CEE_LAST);

			if (MONO_IS_NON_FP_MOVE (ins) && ins->prev) {
				MonoInst *def;
				const char *spec2;

				def = ins->prev;
				while (def->prev && (def->opcode == OP_NOP))
					def = def->prev;
				spec2 = INS_INFO (def->opcode);

				/* 
				 * Perform a limited kind of reverse copy propagation, i.e.
				 * transform B <- FOO; A <- B into A <- FOO
				 * This isn't copyprop, not deadce, but it can only be performed
				 * after handle_global_vregs () has run.
				 */
				if (!get_vreg_to_inst (cfg, ins->sreg1) && (spec2 [MONO_INST_DEST] != ' ') && (def->dreg == ins->sreg1) && !mono_bitset_test_fast (used, ins->sreg1) && !MONO_IS_STORE_MEMBASE (def) && reg_is_softreg (ins->sreg1, spec [MONO_INST_DEST])) {
					if (cfg->verbose_level > 2) {
						printf ("\tReverse copyprop in BB%d on ", bb->block_num);
						mono_print_ins (ins);
					}

					def->dreg = ins->dreg;
					MONO_DELETE_INS (bb, ins);
					spec = INS_INFO (ins->opcode);
				}
			}

			/* Enabling this on x86 could screw up the fp stack */
			if (reg_is_softreg_no_fpstack (ins->dreg, spec [MONO_INST_DEST])) {
				/* 
				 * Assignments to global vregs can only be eliminated if there is another
				 * assignment to the same vreg later in the same bblock.
				 */
				if (!mono_bitset_test_fast (used, ins->dreg) && 
					(!get_vreg_to_inst (cfg, ins->dreg) || (!bb->extended && !vreg_is_volatile (cfg, ins->dreg) && mono_bitset_test_fast (defined, ins->dreg))) &&
					MONO_INS_HAS_NO_SIDE_EFFECT (ins)) {
					/* Happens with CMOV instructions */
					if (ins->prev && ins->prev->opcode == OP_ICOMPARE_IMM) {
						MonoInst *prev = ins->prev;
						/* 
						 * Can't use DELETE_INS since that would interfere with the
						 * FOR_EACH_INS loop.
						 */
						NULLIFY_INS (prev);
					}
					//printf ("DEADCE: "); mono_print_ins (ins);
					MONO_DELETE_INS (bb, ins);
					spec = INS_INFO (ins->opcode);
				}

				if (spec [MONO_INST_DEST] != ' ')
					mono_bitset_clear_fast (used, ins->dreg);
			}

			if (spec [MONO_INST_DEST] != ' ')
				mono_bitset_set_fast (defined, ins->dreg);
			if (spec [MONO_INST_SRC1] != ' ')
				mono_bitset_set_fast (used, ins->sreg1);
			if (spec [MONO_INST_SRC2] != ' ')
				mono_bitset_set_fast (used, ins->sreg2);
			if (MONO_IS_STORE_MEMBASE (ins))
				mono_bitset_set_fast (used, ins->dreg);

			if (MONO_IS_CALL (ins)) {
				MonoCallInst *call = (MonoCallInst*)ins;
				GSList *l;

				if (call->out_ireg_args) {
					for (l = call->out_ireg_args; l; l = l->next) {
						guint32 regpair, reg;

						regpair = (guint32)(gssize)(l->data);
						reg = regpair & 0xffffff;
					
						mono_bitset_set_fast (used, reg);
					}
				}

				if (call->out_freg_args) {
					for (l = call->out_freg_args; l; l = l->next) {
						guint32 regpair, reg;

						regpair = (guint32)(gssize)(l->data);
						reg = regpair & 0xffffff;
					
						mono_bitset_set_fast (used, reg);
					}
				}
			}
		}
	}

	//mono_print_code (cfg, "AFTER LOCAL-DEADCE");
}
