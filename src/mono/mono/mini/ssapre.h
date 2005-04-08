/*
 * ssapre.h: SSA Partial Redundancy Elimination
 *
 * Author:
 *   Massimiliano Mantione (massi@ximian.com)
 *
 * (C) 2004 Novell, Inc.  http://www.novell.com
 */

#ifndef __MONO_SSAPRE_H__
#define __MONO_SSAPRE_H__


#include "mini.h"
#include <mono/metadata/mempool.h>

/*
 * Hack to apply SSAPRE only to a given method (invaluable in debugging)
 */
#define MONO_APPLY_SSAPRE_TO_SINGLE_METHOD 0

/*
 * Hack to apply SSAPRE only to a given expression (invaluable in debugging)
 */
#define MONO_APPLY_SSAPRE_TO_SINGLE_EXPRESSION 0

/*
 * All the different kind of arguments we can handle.
 * "ANY" means the argument is unknown or cannot be handled, and "NOT_PRESENT"
 * that the expression does not have this argument (has not "enough" arity).
 */
typedef enum {
	MONO_SSAPRE_EXPRESSION_ARGUMENT_ANY,
	MONO_SSAPRE_EXPRESSION_ARGUMENT_NOT_PRESENT,
	MONO_SSAPRE_EXPRESSION_ARGUMENT_ORIGINAL_VARIABLE,
	MONO_SSAPRE_EXPRESSION_ARGUMENT_SSA_VARIABLE,
	MONO_SSAPRE_EXPRESSION_ARGUMENT_INTEGER_CONSTANT,
	MONO_SSAPRE_EXPRESSION_ARGUMENT_LONG_COSTANT,
	MONO_SSAPRE_EXPRESSION_ARGUMENT_FLOAT_COSTANT,
	MONO_SSAPRE_EXPRESSION_ARGUMENT_DOUBLE_COSTANT
} MonoSsapreExpressionArgumentType;

/*
 * A struct representing an expression argument (the used branch in the
 * union depends on the value of the type field).
 */
typedef struct MonoSsapreExpressionArgument {
	MonoSsapreExpressionArgumentType type;
	union {
		int original_variable;
		int ssa_variable;
		int integer_constant;
		gint64* long_constant;
		float* float_constant;
		double* double_constant;
	} argument;
} MonoSsapreExpressionArgument;

/*
 * Macros used when comparing expression arguments, which return -1,0 or 1.
 */
#define MONO_COMPARE_SSAPRE_DIRECT_VALUES(v1,v2) (((v2)>(v1)?(1):((v2)<(v1)?(-1):(0))))
#define MONO_COMPARE_SSAPRE_POINTER_VALUES(p1,p2) (((*p2)>(*p1)?(1):((*p2)<(*p1)?(-1):(0))))

#define MONO_COMPARE_SSAPRE_EXPRESSION_ARGUMENT_VALUES(t,v1,v2) (\
		(t)==MONO_SSAPRE_EXPRESSION_ARGUMENT_ORIGINAL_VARIABLE?\
			MONO_COMPARE_SSAPRE_DIRECT_VALUES ((v1).original_variable,(v2).original_variable):(\
		(t)==MONO_SSAPRE_EXPRESSION_ARGUMENT_SSA_VARIABLE?\
			MONO_COMPARE_SSAPRE_DIRECT_VALUES ((v1).ssa_variable,(v2).ssa_variable):(\
		(t)==MONO_SSAPRE_EXPRESSION_ARGUMENT_INTEGER_CONSTANT?\
			MONO_COMPARE_SSAPRE_DIRECT_VALUES ((v1).integer_constant,(v2).integer_constant):(\
		(t)==MONO_SSAPRE_EXPRESSION_ARGUMENT_LONG_COSTANT?\
			MONO_COMPARE_SSAPRE_POINTER_VALUES ((v1).long_constant,(v2).long_constant):(\
		(t)==MONO_SSAPRE_EXPRESSION_ARGUMENT_FLOAT_COSTANT?\
			MONO_COMPARE_SSAPRE_POINTER_VALUES ((v1).float_constant,(v2).float_constant):(\
		(t)==MONO_SSAPRE_EXPRESSION_ARGUMENT_DOUBLE_COSTANT?\
			MONO_COMPARE_SSAPRE_POINTER_VALUES ((v1).double_constant,(v2).double_constant):(\
		0)))))))

#define MONO_COMPARE_SSAPRE_EXPRESSION_ARGUMENTS(a1,a2) (\
		MONO_COMPARE_SSAPRE_DIRECT_VALUES ((a1).type,(a2).type)!=0?\
			MONO_COMPARE_SSAPRE_DIRECT_VALUES ((a1).type,(a2).type):\
		MONO_COMPARE_SSAPRE_EXPRESSION_ARGUMENT_VALUES ((a1).type,(a1).argument,(a2).argument) )


/*
 * A struct representing an expression, with its opcode and two arguments
 * (if the opcode has arity 1 right_argument is MONO_SSAPRE_EXPRESSION_ARGUMENT_NOT_PRESENT).
 */
typedef struct MonoSsapreExpressionDescription {
	guint16 opcode;
	MonoSsapreExpressionArgument left_argument;
	MonoSsapreExpressionArgument right_argument;
} MonoSsapreExpressionDescription;

/*
 * Macro that compares two expression descriptions (returns -1, 0 or 1).
 */
#define MONO_COMPARE_SSAPRE_EXPRESSION_DESCRIPTIONS(d1,d2) (\
		MONO_COMPARE_SSAPRE_DIRECT_VALUES ((d1).opcode,(d2).opcode)!=0?\
			MONO_COMPARE_SSAPRE_DIRECT_VALUES ((d1).opcode,(d2).opcode):(\
		MONO_COMPARE_SSAPRE_EXPRESSION_ARGUMENTS ((d1).left_argument,(d2).left_argument)!=0?\
			MONO_COMPARE_SSAPRE_EXPRESSION_ARGUMENTS ((d1).left_argument,(d2).left_argument):(\
		MONO_COMPARE_SSAPRE_EXPRESSION_ARGUMENTS ((d1).right_argument,(d2).right_argument)!=0?\
			MONO_COMPARE_SSAPRE_EXPRESSION_ARGUMENTS ((d1).right_argument,(d2).right_argument):(\
		0))))

/*
 * Struct that contains all the information related to a BB.
 * Some of them are taken from the corresponding MonoBasicBlock, some are
 * constant during the compilation of the whole method, others must be
 * recomputed for each expression.
 */
typedef struct MonoSsapreBBInfo {
	/* Information constant during the compilation of the whole method: */
	
	/* Depth First Number relative to a traversal of the dominator tree */
	gint32 dt_dfn;
	/* Depth First Number relative to a traversal of the CFG */
	gint32 cfg_dfn;
	/* Number of descendants in the dominator tree (is equal to the number of strictly dominated BBs) */
	int dt_descendants;
	/* In and out count (taken from the corresponding MonoBasicBlock) */
	gint16 in_count, out_count;
	/* Idominator (taken from the corresponding MonoBasicBlock, but pointing */
	/* to the MonoSsapreBBInfo for convenience) */
	struct MonoSsapreBBInfo *idominator;
	/* In and out BBs (taken from the corresponding MonoBasicBlock, but pointing */
	/* to the MonoSsapreBBInfo for convenience) */
	struct MonoSsapreBBInfo **in_bb;
	struct MonoSsapreBBInfo **out_bb;
	/* Dominance frontier (taken from the corresponding MonoBasicBlock) */
	MonoBitSet *dfrontier;
	
	/* MonoInst where new phi definitions must be added in the BB */
	/* (the last existing phi definition, or NULL if there is none) */
	MonoInst *phi_insertion_point;
	
	/* Information recomputed during the analysis of each expression: */
	
	/* True if the whole BB subtree in the dominator tree is "covered" with */
	/* BBs marked "interesting" (a BB where this is false cannot be down */
	/* safe, since there would be a path to exit with no occurrence at all). */
	/* A more formal way of stating this is that on the DT there is no path */
	/* from this BB to any leaf that does not meet an interesting BB */
	gboolean dt_covered_by_interesting_BBs;
	
	/* True if this BB has a PHI occurrence */
	gboolean has_phi;
	/* True if this PHI defines a real occurrence */
	gboolean phi_defines_a_real_occurrence;
	/* True if this PHI is down safe */
	gboolean phi_is_down_safe;
	/* True if this PHI can be available */
	gboolean phi_can_be_available;
	/* True if this PHI is "later" */
	gboolean phi_is_later;
	/* The PHI class number */
	int phi_redundancy_class;
	/* The index of this PHI in the cfg->vars array */
	int phi_variable_index;
	/* Array of the class numbers of the PHI arguments (has "in_count" elements) */
	int *phi_arguments_classes;
	
	/* True if this BB has a PHI argument */
	gboolean has_phi_argument;
	/* True if this PHI argument "has real use" */
	gboolean phi_argument_has_real_use;
	/* True if this PHI argument needs the insertion of a new occurrence */
	gboolean phi_argument_needs_insert;
	/* True if this PHI argument has been processed (see "set_save") */
	gboolean phi_argument_has_been_processed;
	/* The PHI argument class number */
	int phi_argument_class;
	/* The index of this PHI argument in the cfg->vars array */
	int phi_argument_variable_index;
	/* Points to the real occurrence defining this PHI argument (NULL otherwise) */
	struct MonoSsapreExpressionOccurrence *phi_argument_defined_by_real_occurrence;
	/* Points to the BB containing the PHI defining this PHI argument (NULL otherwise) */
	struct MonoSsapreBBInfo *phi_argument_defined_by_phi;
	/* Variable version of the left argument og the PHI argument "expected" at */
	/* the PHI (or BOTTOM_REDUNDANCY_CLASS otherwise), see "renaming_pass" */
	int phi_argument_left_argument_version;
	/* As above, but for the right argument */
	int phi_argument_right_argument_version;
	
	/* The first real occurrence in this BB (NULL if there is none) */
	struct MonoSsapreExpressionOccurrence *first_expression_in_bb;
	/* Next BB which has either a real occurrence, a PHI or a PHI argument */
	/* (NULL if there is none, BBs are in dominator tree depth first preorder) */
	struct MonoSsapreBBInfo *next_interesting_bb;
	
	/* Used in maintaining the renaming stack */
	struct MonoSsapreBBInfo *next_in_renaming_stack;
	struct MonoSsapreExpressionOccurrence *top_of_local_renaming_stack;
	
	/* MonoBasicBlock representing this BB in the CFG (this is obviously constant) */
	MonoBasicBlock *bb;
} MonoSsapreBBInfo;


/*
 * The father of an occurrence in the tree of MonoInst.
 * (needed just because a MonoInst cannot point to its father)
 */
typedef struct MonoSsapreFatherExpression {
	/* The father occurrence */
	MonoInst *father_occurrence;
	/* The MonoSsapreFatherExpression node of the "grand father" */
	struct MonoSsapreFatherExpression *grand_father;
} MonoSsapreFatherExpression;

/*
 * A "real" occurrence.
 */
typedef struct MonoSsapreExpressionOccurrence {
	/* The occurrence in the CFG */
	MonoInst *occurrence;
	/* The "father" of this occurrence in the inst tree (if the occurrence is */
	/* part of a compound expression, otherwise it is NULL) */
	MonoSsapreFatherExpression *father_in_tree;
	/* The tree just before the occurrence in the CFG (if the occurrence must */
	/* saved into a temporary, the definition will be placed just after that tree) */
	MonoInst *previous_tree;
	/* The BB where this occurrence is found */
	MonoSsapreBBInfo *bb_info;
	/* The description of the occurrence */
	MonoSsapreExpressionDescription description;
	/* Next occurrence of this expression */
	struct MonoSsapreExpressionOccurrence *next;
	/* Previous occurrence of this expression */
	struct MonoSsapreExpressionOccurrence *previous;
	/* True if this occurrence is the first in its BB */
	gboolean is_first_in_bb;
	/* True if this occurrence is the last in its BB */
	gboolean is_last_in_bb;
	/* "reload" flag (see "finalize") */
	gboolean reload;
	/* "save" flag (see "finalize") */
	gboolean save;
	
	/* Used in maintaining the renaming stack */
	struct MonoSsapreExpressionOccurrence *next_in_renaming_stack;
	
	/* Class number of this occurrence */
	int redundancy_class;
	/* The index of the temporary of this occurrence in the cfg->vars array */
	int variable_index;
	/* Points to the real occurrence defining this occurrence (NULL otherwise) */
	struct MonoSsapreExpressionOccurrence *defined_by_real_occurrence;
	/* Points to the BB containing the PHI defining this occurrence (NULL otherwise) */
	struct MonoSsapreBBInfo *defined_by_phi;
} MonoSsapreExpressionOccurrence;


/*
 * An expression to be processed (in the worklist).
 */
typedef struct MonoSsapreExpression {
	/* The description of the expression */
	MonoSsapreExpressionDescription description;
	/* The type to use when creating values of this expression */
	MonoType *type;
	/* The list of expression occurrences */
	MonoSsapreExpressionOccurrence *occurrences;
	/* The last expression occurrence in the list */
	MonoSsapreExpressionOccurrence *last_occurrence;
	
	/* Used in maintaining the worklist (an autobalancing binary tree) */
	struct MonoSsapreExpression *father;
	struct MonoSsapreExpression *previous;
	struct MonoSsapreExpression *next;
	int tree_size;
	
	/* Next expression to be processed in the worklist */
	struct MonoSsapreExpression *next_in_queue;	
} MonoSsapreExpression;

/*
 * Macros used to maintain the worklist
 */
#define MONO_SSAPRE_GOTO_FIRST_EXPRESSION(e) do{\
		while ((e)->previous != NULL) (e) = (e)->previous;\
	} while (0)
#define MONO_SSAPRE_REMOVE_FIRST_EXPRESSION(e) do{\
		if ((e)->father != NULL) {\
			(e)->father->previous = (e)->next;\
		}\
	} while (0)
#define MONO_SSAPRE_GOTO_LAST_EXPRESSION(e) do{\
		while ((e)->next != NULL) (e) = (e)->next;\
	} while (0)
#define MONO_SSAPRE_REMOVE_LAST_EXPRESSION(e) do{\
		if ((e)->father != NULL) {\
			(e)->father->next = (e)->previous;\
		}\
	} while (0)

#define MONO_SSAPRE_MAX_TREE_DEPTH(size,depth) do{\
		unsigned __mask__ = ~1;\
		(depth) = 1;\
		while (((size)&__mask__)!=0) {\
			__mask__ <<= 1;\
			(depth)++;\
		}\
	} while (0)

#define MONO_SSAPRE_ADD_EXPRESSION_OCCURRANCE(e,o) do{\
		if ((e)->occurrences == NULL) {\
			(e)->occurrences = (o);\
		} else {\
			(e)->last_occurrence->next = (o);\
		}\
		(o)->next = NULL;\
		(o)->previous = (e)->last_occurrence;\
		(e)->last_occurrence = (o);\
	} while (0)
#define MONO_SSAPRE_REMOVE_EXPRESSION_OCCURRANCE(e,o) do{\
		if ((e)->occurrences == (o)) {\
			(e)->occurrences = (o)->next;\
		}\
		if ((e)->last_occurrence == (o)) {\
			(e)->last_occurrence = (o)->previous;\
		}\
		if ((o)->previous != NULL) {\
			(o)->previous->next = (o)->next;\
		}\
		if ((o)->next != NULL) {\
			(o)->next->previous = (o)->previous;\
		}\
		(o)->next = NULL;\
		(o)->previous = NULL;\
	} while (0)


/*
 * Availability table element (see "finalize"), one for each redundancy class
 */
typedef struct MonoSsapreAvailabilityTableElement {
	/* Points to the real occurrence defining this redundancy class (NULL otherwise) */
	struct MonoSsapreExpressionOccurrence *class_defined_by_real_occurrence;
	/* Points to the BB containing the PHI defining this redundancy class (NULL otherwise) */
	struct MonoSsapreBBInfo *class_defined_by_phi;
} MonoSsapreAvailabilityTableElement;

/*
 * The "main" work area for the algorithm.
 */
typedef struct MonoSsapreWorkArea {
	/* The CFG */
	MonoCompile *cfg;
	/* The SSAPRE specific mempool */
	MonoMemPool *mempool;
	
	/* Number of BBs in the CFG (from cfg) */
	int num_bblocks;
	/* BB information, in dominator tree depth first preorder */
	MonoSsapreBBInfo *bb_infos;
	/* Pointers to BB information, in CFG depth first preorder */
	MonoSsapreBBInfo **bb_infos_in_cfg_dfn_order;
	
	/* Number of variables in the CFG */
	int num_vars;
	/* Size of bitset for BBs */
	int sizeof_bb_bitset;
	/* Various bitsets used when working with iterated dfrontiers */
	MonoBitSet *expression_occurrences_buffer;
	MonoBitSet *bb_iteration_buffer;
	MonoBitSet *iterated_dfrontier_buffer;
	MonoBitSet *left_argument_bb_bitset;
	MonoBitSet *right_argument_bb_bitset;
	
	/* The depth of the dominator tree */
	int dt_depth;
	
	/* The expression worklist */
	MonoSsapreExpression *worklist;
	
	/* The expression queue head */
	MonoSsapreExpression *first_in_queue;
	/* The expression queue tail */
	MonoSsapreExpression *last_in_queue;
	
	/* The expression being processed */
	MonoSsapreExpression *current_expression;
	/* The expression being allocated */
	MonoSsapreExpressionOccurrence *current_occurrence;
	
	/* The BB on top of the renaming stack (if "top_of_renaming_stack" is NULL */
	/* but this is not, then the top of the stack is the PHI in this BB) */
	struct MonoSsapreBBInfo *bb_on_top_of_renaming_stack;
	/* The top of the renaming stack */
	struct MonoSsapreExpressionOccurrence *top_of_renaming_stack;
	
	/* The head of the list of "interesting" BBs */
	struct MonoSsapreBBInfo *first_interesting_bb;
	
	/* The number of generated class numbers */
	int number_of_classes;
	
	/* The number of occurrences scheduled for reloading/insertion */
	/* (used to decide if the redundancy is worth eliminating) */
	int occurrences_scheduled_for_reloading;
	int arguments_scheduled_for_insertion;
	int dominating_arguments_scheduled_for_insertion;
	
	/* Statistics fields (per expression)  */
	int saved_occurrences;
	int reloaded_occurrences;
	int inserted_occurrences;
	int unaltered_occurrences;
	int added_phis;
	
#if (MONO_APPLY_SSAPRE_TO_SINGLE_EXPRESSION)
	gboolean expression_is_handled_father;
#endif
} MonoSsapreWorkArea;


#endif /* __MONO_SSAPRE_H__ */
