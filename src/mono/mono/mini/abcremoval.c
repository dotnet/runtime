/*
 * abcremoval.c: Array bounds check removal
 *
 * Author:
 *   Massimiliano Mantione (massi@ximian.com)
 *
 * (C) 2004 Ximian, Inc.  http://www.ximian.com
 */
#include <string.h>
#include <stdio.h>

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/opcodes.h>

#include "inssel.h"

#include "abcremoval.h"

extern guint8 mono_burg_arity [];

#define TRACE_ABC_REMOVAL (verbose_level > 2)
#define REPORT_ABC_REMOVAL (verbose_level > 0)

/*
 * A little hack for the verbosity level.
 * The verbosity level is stored in the cfg, but not all functions that must
 * print something see the cfg, so we store the verbosity level here at the
 * beginning of the algorithm.
 * This is not thread safe (does not handle correctly different verbosity
 * levels in different threads), and is not exact in case of dynamic changes
 * of the verbosity level...
 * Anyway, this is not needed, all that can happen is that something more
 * (or less) is logged, the result is in any case correct.
 */
static int verbose_level;


#define RELATION_BETWEEN_VALUES(value,related_value) (\
	((value) > (related_value))? MONO_GT_RELATION :\
	(((value) < (related_value))? MONO_LT_RELATION : MONO_EQ_RELATION))

#define MAKE_VALUE_ANY(v) do{\
		(v).type = MONO_ANY_SUMMARIZED_VALUE;\
	} while (0)

#define MAKE_VALUE_RELATION_ANY(r) do{\
		(r)->relation = MONO_ANY_RELATION;\
		MAKE_VALUE_ANY((r)->related_value);\
	} while (0)

#define INITIALIZE_VALUE_RELATION(r) do{\
		MAKE_VALUE_RELATION_ANY((r));\
		(r)->next = NULL;\
	} while (0)

#define MONO_NEGATED_RELATION(r) ((~(r))&MONO_ANY_RELATION)
#define MONO_SYMMETRIC_RELATION(r) (((r)&MONO_EQ_RELATION)|(((r)&MONO_LT_RELATION)<<1)|((r&MONO_GT_RELATION)>>1))



static void
print_relation (int relation) {
	int print_or = 0;
	printf ("(");
	if (relation & MONO_LT_RELATION) {
		printf ("LT");
		print_or = 1;
	}
	if (relation & MONO_EQ_RELATION) {
		if (print_or) {
			printf ("|");
		}
		printf ("EQ");
		print_or = 1;
	}
	if (relation & MONO_GT_RELATION) {
		if (print_or) {
			printf ("|");
		}
		printf ("GT");
		print_or = 1;
	}
	printf (")");
}

static void
print_summarized_value (MonoSummarizedValue *value) {
	switch (value->type) {
	case MONO_ANY_SUMMARIZED_VALUE:
		printf ("ANY");
		break;
	case MONO_CONSTANT_SUMMARIZED_VALUE:
		printf ("CONSTANT %d", value->value.constant.value);
		break;
	case MONO_VARIABLE_SUMMARIZED_VALUE:
		printf ("VARIABLE %d, delta %d", value->value.variable.variable, value->value.variable.delta);
		break;
	case MONO_PHI_SUMMARIZED_VALUE: {
		int phi;
		printf ("PHI (");
		for (phi = 0; phi < value->value.phi.number_of_alternatives; phi++) {
			if (phi) printf (",");
			printf ("%d", value->value.phi.phi_alternatives [phi]);
		}
		printf (")");
		break;
	}
	default:
		g_assert_not_reached ();
	}
}

static void
print_summarized_value_relation (MonoSummarizedValueRelation *relation) {
	printf ("Relation ");
	print_relation (relation->relation);
	printf (" with value ");
	print_summarized_value (&(relation->related_value));
}

#if 0
static void
print_summarized_value_relation_chain (MonoSummarizedValueRelation *relation) {
	printf ("Relations:\n");
	while (relation) {
		print_summarized_value_relation (relation);
		printf ("\n");
		relation = relation->next;
	}
}
#endif

static void
print_evaluation_context_status (MonoRelationsEvaluationStatus status) {
	if (status == MONO_RELATIONS_EVALUATION_NOT_STARTED) {
		printf ("EVALUATION_NOT_STARTED");
	} else {
		gboolean print_or = FALSE;
		
		printf ("(");
		if (status & MONO_RELATIONS_EVALUATION_IN_PROGRESS) {
			if (print_or) printf ("|");
			printf ("EVALUATION_IN_PROGRESS");
			print_or = TRUE;
		}
		if (status & MONO_RELATIONS_EVALUATION_COMPLETED) {
			if (print_or) printf ("|");
			printf ("EVALUATION_COMPLETED");
			print_or = TRUE;
		}
		if (status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_ASCENDING) {
			if (print_or) printf ("|");
			printf ("RECURSIVELY_ASCENDING");
			print_or = TRUE;
		}
		if (status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_DESCENDING) {
			if (print_or) printf ("|");
			printf ("RECURSIVELY_DESCENDING");
			print_or = TRUE;
		}
		if (status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_INDEFINITE) {
			if (print_or) printf ("|");
			printf ("RECURSIVELY_INDEFINITE");
			print_or = TRUE;
		}
		printf (")");
	}
}


static void
print_evaluation_context_ranges (MonoRelationsEvaluationRanges *ranges) {
	printf ("(ranges: zero [%d,%d], variable [%d,%d])", ranges->zero.lower, ranges->zero.upper, ranges->variable.lower, ranges->variable.upper);
}

static void
print_evaluation_context (MonoRelationsEvaluationContext *context) {
	printf ("Context status: ");
	print_evaluation_context_status (context->status);
	if (context->status & (MONO_RELATIONS_EVALUATION_IN_PROGRESS|MONO_RELATIONS_EVALUATION_COMPLETED)) {
		print_evaluation_context_ranges (&(context->ranges));
	}
	printf ("\n");
}

#if 0
static void
print_evaluation_area (MonoVariableRelationsEvaluationArea *area) {
	int i;
	printf ("Dump of evaluation area (%d variables):\n", area->cfg->num_varinfo);
	for (i = 0; i < area->cfg->num_varinfo; i++) {
		printf ("Variable %d: ", i);
		print_evaluation_context (&(area->contexts [i]));
		print_summarized_value_relation_chain (&(area->relations [i]));
	}
}

static void
print_evaluation_area_contexts (MonoVariableRelationsEvaluationArea *area) {
	int i;
	printf ("Dump of evaluation area contexts (%d variables):\n", area->cfg->num_varinfo);
	for (i = 0; i < area->cfg->num_varinfo; i++) {
		printf ("Variable %d: ", i);
		print_evaluation_context (&(area->contexts [i]));
	}
}
#endif

/*
 * Given a MonoInst, if it is a store to a variable return the MonoInst that
 * represents the stored value.
 * If anything goes wrong, return NULL.
 * store: the MonoInst that should be a store
 * expected_variable_index: the variable where the value should be stored
 * return: either the stored value, or NULL
 */
static MonoInst *
get_variable_value_from_store_instruction (MonoInst *store, int expected_variable_index) {
	switch (store->opcode) {
	case CEE_STIND_REF:
	case CEE_STIND_I:
	case CEE_STIND_I4:
	case CEE_STIND_I1:
	case CEE_STIND_I2:
	case CEE_STIND_I8:
	case CEE_STIND_R4:
	case CEE_STIND_R8:
		if (TRACE_ABC_REMOVAL) {
			printf ("[store instruction found]");
		}
		if (store->inst_left->opcode == OP_LOCAL) {
			int variable_index = store->inst_left->inst_c0;
			if (TRACE_ABC_REMOVAL) {
				printf ("[value put in local %d (expected %d)]", variable_index, expected_variable_index);
			}
			if (variable_index == expected_variable_index) {
				return store->inst_right;
			} else {
				return NULL;
			}
		}
		else
		{
			return NULL;
		}
		break;
	default:
		return NULL;
	}
}

/*
 * Given a MonoInst representing a value, store it in "summarized" form.
 * result: the "summarized" value
 */
static void
summarize_value (MonoInst *value, MonoSummarizedValue *result) {
	switch (value->opcode) {
	case OP_ICONST:
		result->type = MONO_CONSTANT_SUMMARIZED_VALUE;
		result->value.constant.value = value->inst_c0;
		break;
	case OP_LOCAL:
	case OP_ARG:
		result->type = MONO_VARIABLE_SUMMARIZED_VALUE;
		result->value.variable.variable = value->inst_c0;
		result->value.variable.delta = 0;
		break;
	case CEE_LDIND_I1:
	case CEE_LDIND_U1:
	case CEE_LDIND_I2:
	case CEE_LDIND_U2:
	case CEE_LDIND_I4:
	case CEE_LDIND_U4:
	case CEE_LDIND_REF:
		if ((value->inst_left->opcode == OP_LOCAL) || (value->inst_left->opcode == OP_ARG)) {
			summarize_value (value->inst_left, result);
		} else {
			MAKE_VALUE_ANY (*result);
		}
		break;
	case CEE_ADD: {
		MonoSummarizedValue left_value;
		MonoSummarizedValue right_value;
		summarize_value (value->inst_left, &left_value);
		summarize_value (value->inst_right, &right_value);

		if (left_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) {
			if (right_value.type == MONO_CONSTANT_SUMMARIZED_VALUE) {
				result->type = MONO_VARIABLE_SUMMARIZED_VALUE;
				result->value.variable.variable = left_value.value.variable.variable;
				result->value.variable.delta = left_value.value.variable.delta + right_value.value.constant.value;
			} else {
				MAKE_VALUE_ANY (*result);
			}
		} else if (right_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) {
			if (left_value.type == MONO_CONSTANT_SUMMARIZED_VALUE) {
				result->type = MONO_VARIABLE_SUMMARIZED_VALUE;
				result->value.variable.variable = right_value.value.variable.variable;
				result->value.variable.delta = left_value.value.constant.value + right_value.value.variable.delta;
			} else {
				MAKE_VALUE_ANY (*result);
			}
		} else if ((right_value.type == MONO_CONSTANT_SUMMARIZED_VALUE) && (left_value.type == MONO_CONSTANT_SUMMARIZED_VALUE)) {
			/* This should not happen if constant folding has been done */
			result->type = MONO_CONSTANT_SUMMARIZED_VALUE;
			result->value.constant.value = left_value.value.constant.value + right_value.value.constant.value;
		} else {
			MAKE_VALUE_ANY (*result);
		}
		break;
	}
	case CEE_SUB: {
		MonoSummarizedValue left_value;
		MonoSummarizedValue right_value;
		summarize_value (value->inst_left, &left_value);
		summarize_value (value->inst_right, &right_value);

		if (left_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) {
			if (right_value.type == MONO_CONSTANT_SUMMARIZED_VALUE) {
				result->type = MONO_VARIABLE_SUMMARIZED_VALUE;
				result->value.variable.variable = left_value.value.variable.variable;
				result->value.variable.delta = left_value.value.variable.delta - right_value.value.constant.value;
			} else {
				MAKE_VALUE_ANY (*result);
			}
		} else if ((right_value.type == MONO_CONSTANT_SUMMARIZED_VALUE) && (left_value.type == MONO_CONSTANT_SUMMARIZED_VALUE)) {
			/* This should not happen if constant folding has been done */
			result->type = MONO_CONSTANT_SUMMARIZED_VALUE;
			result->value.constant.value = left_value.value.constant.value - right_value.value.constant.value;
		} else {
			MAKE_VALUE_ANY (*result);
		}
		break;
	}
	case CEE_NEWARR:
		summarize_value (value->inst_newa_len, result);
		break;
	case CEE_LDLEN:
		summarize_value (value->inst_left, result);
		break;
	case OP_PHI:
		result->type = MONO_PHI_SUMMARIZED_VALUE;
		result->value.phi.number_of_alternatives = *(value->inst_phi_args);
		result->value.phi.phi_alternatives = value->inst_phi_args + 1;
		break;
	default:
		MAKE_VALUE_ANY (*result);
	}
}

static MonoValueRelation
get_relation_from_branch_instruction (int opcode) {
	switch (opcode) {
	case CEE_BEQ:
		return MONO_EQ_RELATION;
	case CEE_BLT:
	case CEE_BLT_UN:
		return MONO_LT_RELATION;
	case CEE_BLE:
	case CEE_BLE_UN:
		return MONO_LE_RELATION;
	case CEE_BGT:
	case CEE_BGT_UN:
		return MONO_GT_RELATION;
	case CEE_BGE:
	case CEE_BGE_UN:
		return MONO_GE_RELATION;
	case CEE_BNE_UN:
		return MONO_NE_RELATION;
	default:
		return MONO_ANY_RELATION;
	}
}

/*
 * Given a BB, find its entry condition and put its relations in a
 * "MonoAdditionalVariableRelationsForBB" structure.
 * bb: the BB
 * relations: the resulting relations (entry condition of the given BB)
 */
static void
get_relations_from_previous_bb (MonoBasicBlock *bb, MonoAdditionalVariableRelationsForBB *relations) {
	MonoBasicBlock *in_bb;
	MonoInst *branch;
	MonoValueRelation branch_relation;
	MonoValueRelation symmetric_relation;
	
	INITIALIZE_VALUE_RELATION (&(relations->relation1.relation));
	relations->relation1.relation.relation_is_static_definition = FALSE;
	relations->relation1.insertion_point = NULL;
	relations->relation1.variable = -1;
	INITIALIZE_VALUE_RELATION (&(relations->relation2.relation));
	relations->relation2.relation.relation_is_static_definition = FALSE;
	relations->relation2.insertion_point = NULL;
	relations->relation2.variable = -1;
	
	
	if (bb->in_count == 1) { /* Should write the code to "sum" conditions... */
		in_bb = bb->in_bb [0];
		branch = in_bb->last_ins;
		if (branch == NULL) return;
		branch_relation = get_relation_from_branch_instruction (branch->opcode);
		if ((branch_relation != MONO_ANY_RELATION) && (branch->inst_left->opcode == OP_COMPARE)) {
			MonoSummarizedValue left_value;
			MonoSummarizedValue right_value;
			gboolean code_path;

			if (branch->inst_true_bb == bb) {
				code_path = TRUE;
			} else if (branch->inst_false_bb == bb) {
				code_path = FALSE;
			} else {
				code_path = TRUE;
				g_assert_not_reached ();
			}

			if (!code_path) {
				branch_relation = MONO_NEGATED_RELATION (branch_relation);
			}
			symmetric_relation = MONO_SYMMETRIC_RELATION (branch_relation);

			summarize_value (branch->inst_left->inst_left, &left_value);
			summarize_value (branch->inst_left->inst_right, &right_value);

			if ((left_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) && ((right_value.type == MONO_VARIABLE_SUMMARIZED_VALUE)||(right_value.type == MONO_CONSTANT_SUMMARIZED_VALUE))) {
				relations->relation1.variable = left_value.value.variable.variable;
				relations->relation1.relation.relation = branch_relation;
				relations->relation1.relation.related_value = right_value;
				if (right_value.type == MONO_CONSTANT_SUMMARIZED_VALUE) {
					relations->relation1.relation.related_value.value.constant.value -= left_value.value.variable.delta;
				} else {
					relations->relation1.relation.related_value.value.variable.delta -= left_value.value.variable.delta;
				}
			}
			if ((right_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) && ((left_value.type == MONO_VARIABLE_SUMMARIZED_VALUE)||(left_value.type == MONO_CONSTANT_SUMMARIZED_VALUE))) {
				relations->relation2.variable = right_value.value.variable.variable;
				relations->relation2.relation.relation = symmetric_relation;
				relations->relation2.relation.related_value = left_value;
				if (left_value.type == MONO_CONSTANT_SUMMARIZED_VALUE) {
					relations->relation2.relation.related_value.value.constant.value -= right_value.value.variable.delta;
				} else {
					relations->relation2.relation.related_value.value.variable.delta -= right_value.value.variable.delta;
				}
			}
		}
	}
}


/*
 * Add the given relations to the evaluation area.
 * area: the evaluation area
 * change: the relations that must be added
 */
static void
apply_change_to_evaluation_area (MonoVariableRelationsEvaluationArea *area, MonoAdditionalVariableRelation *change) {
	MonoSummarizedValueRelation *base_relation;
	
	if (change->relation.relation != MONO_ANY_RELATION) {
		base_relation = &(area->relations [change->variable]);
		while ((base_relation->next != NULL) && (base_relation->next->relation_is_static_definition)) {
			base_relation = base_relation->next;
		}
		change->insertion_point = base_relation;
		change->relation.next = base_relation->next;
		base_relation->next = &(change->relation);
	}
}

/*
 * Remove the given relation from the evaluation area.
 * change: the relation that must be removed
 */
static void
remove_change_from_evaluation_area (MonoAdditionalVariableRelation *change) {
	if (change->insertion_point != NULL) {
		change->insertion_point->next = change->relation.next;
		change->relation.next = NULL;
	}
}


static void
clean_contexts (MonoRelationsEvaluationContext *contexts, int number) {
	int i;
	for (i = 0; i < number; i++) {
		contexts [i].status = MONO_RELATIONS_EVALUATION_NOT_STARTED;
	}
}


/*
 * Perform the intersection of a range and a constant value (taking into
 * account the relation that the value has with the range).
 * range: the range that will be intersected with the value
 * value: the value that will be intersected with the range
 * relation: the relation between the range and the value
 */
static void
intersect_value( MonoRelationsEvaluationRange *range, int value, MonoValueRelation relation ) {
	switch (relation) {
	case MONO_NO_RELATION:
		MONO_MAKE_RELATIONS_EVALUATION_RANGE_IMPOSSIBLE (*range);
		break;
	case MONO_ANY_RELATION:
		break;
	case MONO_EQ_RELATION:
		MONO_UPPER_EVALUATION_RANGE_INTERSECTION (range->upper, value);
		MONO_LOWER_EVALUATION_RANGE_INTERSECTION (range->lower, value);
		break;
	case MONO_NE_RELATION: {
		/* IMPROVEMENT Figure this out! (ignoring it is safe anyway) */
		break;
	}
	case MONO_LT_RELATION:
		MONO_UPPER_EVALUATION_RANGE_INTERSECTION (range->upper, MONO_UPPER_EVALUATION_RANGE_NOT_EQUAL (value));
		break;
	case MONO_LE_RELATION:
		MONO_UPPER_EVALUATION_RANGE_INTERSECTION (range->upper, value);
		break;
	case MONO_GT_RELATION:
		MONO_LOWER_EVALUATION_RANGE_INTERSECTION (range->lower, MONO_LOWER_EVALUATION_RANGE_NOT_EQUAL (value));
		break;
	case MONO_GE_RELATION:
		MONO_LOWER_EVALUATION_RANGE_INTERSECTION (range->lower, value);
		break;
	default:
		g_assert_not_reached();
	}
}


/*
 * Perform the intersection of two pairs of ranges (taking into account the
 * relation between the ranges and a given delta).
 * ranges: the ranges that will be intersected
 * other_ranges the other ranges that will be intersected
 * delta: the delta between the pairs of ranges
 * relation: the relation between the pairs of ranges
 */
static void
intersect_ranges( MonoRelationsEvaluationRanges *ranges, MonoRelationsEvaluationRanges *other_ranges, int delta, MonoValueRelation relation ) {
	if (delta == 0) {
		switch (relation) {
		case MONO_NO_RELATION:
			MONO_MAKE_RELATIONS_EVALUATION_RANGES_IMPOSSIBLE (*ranges);
			break;
		case MONO_ANY_RELATION:
			break;
		case MONO_EQ_RELATION:
			MONO_RELATIONS_EVALUATION_RANGES_INTERSECTION (*ranges, *other_ranges);
			break;
		case MONO_NE_RELATION: {
			/* FIXME Figure this out! (ignoring it is safe anyway) */
			break;
		}
		case MONO_LT_RELATION:
			MONO_UPPER_EVALUATION_RANGE_INTERSECTION (ranges->zero.upper, MONO_UPPER_EVALUATION_RANGE_NOT_EQUAL (other_ranges->zero.upper));
			MONO_UPPER_EVALUATION_RANGE_INTERSECTION (ranges->variable.upper, MONO_UPPER_EVALUATION_RANGE_NOT_EQUAL (other_ranges->variable.upper));
			break;
		case MONO_LE_RELATION:
			MONO_UPPER_EVALUATION_RANGE_INTERSECTION (ranges->zero.upper, other_ranges->zero.upper);
			MONO_UPPER_EVALUATION_RANGE_INTERSECTION (ranges->variable.upper, other_ranges->variable.upper);
			break;
		case MONO_GT_RELATION:
			MONO_LOWER_EVALUATION_RANGE_INTERSECTION (ranges->zero.lower, MONO_LOWER_EVALUATION_RANGE_NOT_EQUAL (other_ranges->zero.lower));
			MONO_LOWER_EVALUATION_RANGE_INTERSECTION (ranges->variable.lower, MONO_LOWER_EVALUATION_RANGE_NOT_EQUAL (other_ranges->variable.lower));
			break;
		case MONO_GE_RELATION:
			MONO_LOWER_EVALUATION_RANGE_INTERSECTION (ranges->zero.lower, other_ranges->zero.lower);
			MONO_LOWER_EVALUATION_RANGE_INTERSECTION (ranges->variable.lower, other_ranges->variable.lower);
			break;
		default:
			g_assert_not_reached();
		}
	} else {
		MonoRelationsEvaluationRanges translated_ranges = *other_ranges;
		MONO_ADD_DELTA_SAFELY_TO_RANGES (translated_ranges, delta);
		intersect_ranges( ranges, &translated_ranges, FALSE, relation );
	}
}

/*
 * Recursive method that traverses the relation graph to evaluate the
 * relation between two variables.
 * At the end of the execution, the resulting ranges are in the context of
 * the "starting" variable.
 * area: the current evaluation area (it contains the relation graph and
 *       memory for all the evaluation contexts is already allocated)
 * variable: starting variable (the value ranges in its context are the result
 *           of the execution of this procedure)
 * target_variable: the variable with respect to which the starting variable
 *                  is evaluated (tipically the starting variable is the index
 *                  and the target one is the array (which means its length))
 * father_context: the previous evaluation context in recursive invocations
 *                 (or NULL for the first invocation)
 */
static void
evaluate_relation_with_target_variable (MonoVariableRelationsEvaluationArea *area, int variable, int target_variable, MonoRelationsEvaluationContext *father_context) {
	MonoRelationsEvaluationContext *context = &(area->contexts [variable]);
	
	// First of all, we check the evaluation status
	// (what must be done is *very* different in each case)
	switch (context->status) {
	case MONO_RELATIONS_EVALUATION_NOT_STARTED: {
		MonoSummarizedValueRelation *relation = &(area->relations [variable]);
		
		if (TRACE_ABC_REMOVAL) {
			printf ("Evaluating varible %d (target variable %d)\n", variable, target_variable);
			print_summarized_value_relation (relation);
			printf ("\n");
		}
		
		// We properly inizialize the context
		context->status = MONO_RELATIONS_EVALUATION_IN_PROGRESS;
		context->father = father_context;
		MONO_MAKE_RELATIONS_EVALUATION_RANGES_WEAK (context->ranges);
		
		// If we have found the target variable, we can set the range
		// related to it in the context to "equal" (which is [0,0])
		if (variable == target_variable) {
			if (TRACE_ABC_REMOVAL) {
				printf ("Target variable reached (%d), continuing to evaluate relations with constants\n", variable);
			}
			context->ranges.variable.lower = 0;
			context->ranges.variable.upper = 0;
		}
		
		// Examine all relations for this variable (scan the list)
		// The contribute of each relation will be intersected (logical and)
		while (relation != NULL) {
			context->current_relation = relation;
			
			if (TRACE_ABC_REMOVAL) {
				printf ("Processing (%d): ", variable);
				print_summarized_value_relation (relation);
				printf ("\n");
			}
			
			// We decie what to do according the the type of the related value
			switch (relation->related_value.type) {
			case MONO_ANY_SUMMARIZED_VALUE:
				// No added information, skip it
				break;
			case MONO_CONSTANT_SUMMARIZED_VALUE:
				// Intersect range with constant (taking into account the relation)
				intersect_value (&(context->ranges.zero), relation->related_value.value.constant.value, relation->relation);
				break;
			case MONO_VARIABLE_SUMMARIZED_VALUE:
				// Generally, evaluate related variable and intersect ranges.
				// However, some check is necessary...
				
				// If the relation is "ANY", nothing to do (no added information)
				if (relation->relation != MONO_ANY_RELATION) {
					gssize related_variable = relation->related_value.value.variable.variable;
					MonoRelationsEvaluationContext *related_context = &(area->contexts [related_variable]);
					
					// The second condition in the "or" avoids messing with "back edges" in the graph traversal
					// (they are simply ignored instead of triggering the handling of recursion)
					if ( (related_context->status == MONO_RELATIONS_EVALUATION_NOT_STARTED) || !
							((related_context->current_relation->related_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) &&
							(related_context->current_relation->related_value.value.variable.variable == variable))) {
						// Evaluate the related variable
						evaluate_relation_with_target_variable (area, related_variable, target_variable, context);
						
						// Check if we are part of a recursive loop
						if (context->status & MONO_RELATIONS_EVALUATION_IS_RECURSIVE) {
							if (TRACE_ABC_REMOVAL) {
								printf ("Recursivity detected for varible %d (target variable %d), status ", variable, target_variable);
								print_evaluation_context_status (context->status);
							}
							
							// If we are, check if the evaluation of the related variable is complete
							if (related_context->status == MONO_RELATIONS_EVALUATION_COMPLETED) {
								// If it is complete, we are part of a recursive definition.
								// Since it is a *definition* (and definitions are evaluated *before*
								// conditions because they are first in the list), intersection is not
								// strictly necessary, we simply copy the ranges and apply the delta
								context->ranges = related_context->ranges;
								MONO_ADD_DELTA_SAFELY_TO_RANGES (context->ranges, relation->related_value.value.variable.delta);
								context->status = MONO_RELATIONS_EVALUATION_COMPLETED;
								if (TRACE_ABC_REMOVAL) {
									printf (", ranges already computed, result: \n");
									print_evaluation_context_ranges (&(context->ranges));
									printf (" (delta is %d)\n", relation->related_value.value.variable.delta);
								}
							} else {
								// If it is not complete, do nothing (we do not have enough information)
								if (TRACE_ABC_REMOVAL) {
									printf (", ranges not computed\n");
								}
							}
						} else {
							// If we are not (the common case) intersect the result
							intersect_ranges( &(context->ranges), &(related_context->ranges), relation->related_value.value.variable.delta, relation->relation );
						}
					} else {
						if (TRACE_ABC_REMOVAL) {
							printf ("Relation is a back-edge in this traversal, skipping\n");
						}
					}
				}
				break;
			case MONO_PHI_SUMMARIZED_VALUE: {
				// We must compute all PHI alternatives, combining the results (with a union, which is a logical "or"),
				// and intersect this result with the ranges in the context; we must also take into account recursions
				// (with loops that can be ascending, descending, or indefinite)
				MonoRelationsEvaluationRanges phi_ranges;
				int phi;
				gboolean is_ascending = FALSE;
				gboolean is_descending = FALSE;
				
				MONO_MAKE_RELATIONS_EVALUATION_RANGES_IMPOSSIBLE (phi_ranges);
				for (phi = 0; phi < relation->related_value.value.phi.number_of_alternatives; phi++) {
					int phi_alternative = relation->related_value.value.phi.phi_alternatives [phi];
					evaluate_relation_with_target_variable (area, phi_alternative, target_variable, context);
					
					// This means we are part of a recursive loop
					if (context->status & MONO_RELATIONS_EVALUATION_IS_RECURSIVE) {
						if (TRACE_ABC_REMOVAL) {
							printf ("Recursivity detected for varible %d (target variable %d), status ", variable, target_variable);
							print_evaluation_context_status (context->status);
							printf ("\n");
						}
						if (context->status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_ASCENDING) {
							is_ascending = TRUE;
						}
						if (context->status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_DESCENDING) {
							is_descending = TRUE;
						}
						if (context->status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_INDEFINITE) {
							is_ascending = TRUE;
							is_descending = TRUE;
						}
						
						// Clear "recursivity" bits in the status (recursion has been handled)
						context->status = MONO_RELATIONS_EVALUATION_IN_PROGRESS;
					} else {
						MONO_RELATIONS_EVALUATION_RANGES_UNION (phi_ranges, area->contexts [phi_alternative].ranges);
					}
				}
				
				// Apply the effects of all recursive loops
				if (is_ascending) {
					phi_ranges.zero.upper = INT_MAX;
					phi_ranges.variable.upper = INT_MAX;
				}
				if (is_descending) {
					phi_ranges.zero.lower = INT_MIN;
					phi_ranges.variable.lower = INT_MIN;
				}
				
				// Intersect final result
				MONO_RELATIONS_EVALUATION_RANGES_INTERSECTION (context->ranges, phi_ranges);
				break;
			}
			default:
				g_assert_not_reached();
			}
			
			// Pass to next relation
			relation = relation->next;
		}
		
		// Check if any recursivity bits are still in the status, and in any case clear them
		if (context->status & MONO_RELATIONS_EVALUATION_IS_RECURSIVE) {
			if (TRACE_ABC_REMOVAL) {
				printf ("Recursivity for varible %d (target variable %d) discards computation, status ", variable, target_variable);
				print_evaluation_context_status (context->status);
				printf ("\n");
			}
			// If yes, we did not have enough information (most likely we were evaluated inside a PHI, but we also
			// depended on the same PHI, which was still in evaluation...), so clear the status to "NOT_STARTED"
			// (if we will be evaluated again, the PHI will be already done, so our evaluation will succeed)
			context->status = MONO_RELATIONS_EVALUATION_NOT_STARTED;
		} else {
			if (TRACE_ABC_REMOVAL) {
				printf ("Ranges for varible %d (target variable %d) computated: ", variable, target_variable);
				print_evaluation_context_ranges (&(context->ranges));
				printf ("\n");
			}
			// If not (the common case) the evaluation is complete, and the result is in the context
			context->status = MONO_RELATIONS_EVALUATION_COMPLETED;
		}
		break;
	}
	case MONO_RELATIONS_EVALUATION_IN_PROGRESS: {
		// This means we are in a recursive loop
		MonoRelationsEvaluationContext *current_context = father_context;
		MonoRelationsEvaluationContext *last_context = context->father;
		gboolean evaluation_can_be_recursive = TRUE;
		gboolean evaluation_is_definition = TRUE;
		int path_value = 0;
		
		if (TRACE_ABC_REMOVAL) {
			printf ("Evaluation of varible %d (target variable %d) already in progress\n", variable, target_variable);
			print_evaluation_context (context);
			print_summarized_value_relation (context->current_relation);
			printf ("\n");
		}
		
		// We must check if the loop can be a recursive definition (we scan the whole loop)
		while (current_context != last_context) {
			if (current_context == NULL) {
				printf ("Broken recursive ring in ABC removal\n");
				g_assert_not_reached ();
			}
			
			if (current_context->current_relation->relation_is_static_definition) {
				if (current_context->current_relation->related_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) {
					path_value += current_context->current_relation->related_value.value.variable.delta;
				} else if (current_context->current_relation->related_value.type != MONO_PHI_SUMMARIZED_VALUE) {
					evaluation_can_be_recursive = FALSE;
				}
			} else {
				evaluation_is_definition = FALSE;
				evaluation_can_be_recursive = FALSE;
			}
			
			current_context = current_context->father;
		}
		
		// If this is a recursive definition, we properly flag the status in all the involved contexts
		if (evaluation_is_definition) {
			MonoRelationsEvaluationStatus recursive_status;
			if (evaluation_can_be_recursive) {
				if (path_value > 0) {
					recursive_status = MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_ASCENDING;
				} else if (path_value < 0) {
					recursive_status = MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_DESCENDING;
				} else {
					recursive_status = MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_INDEFINITE;
				}
			} else {
				recursive_status = MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_INDEFINITE;
			}
			
			if (TRACE_ABC_REMOVAL) {
				printf ("Recursivity accepted (");
				print_evaluation_context_status (recursive_status);
				printf (")\n");
			}
			
			current_context = father_context;
			while (current_context != last_context) {
				current_context->status |= recursive_status;
				current_context = current_context->father;
			}
		} else {
			if (TRACE_ABC_REMOVAL) {
				printf ("Recursivity rejected (some relation in the cycle is not a defintion)\n");
			}
		}
		break;
	}
	case MONO_RELATIONS_EVALUATION_COMPLETED: {
		return;
	}
	default:
		if (TRACE_ABC_REMOVAL) {
			printf ("Varible %d (target variable %d) already in a recursive ring, skipping\n", variable, target_variable);
			print_evaluation_context (context);
			print_summarized_value_relation (context->current_relation);
			printf ("\n");
		}
		break;
	}
	
}


/*
 * Attempt the removal of bounds checks from a MonoInst.
 * inst: the MonoInst
 * area: the current evaluation area (it contains the relation graph and
 *       memory for all the evaluation contexts is already allocated)
 */
static void
remove_abc_from_inst (MonoInst *inst, MonoVariableRelationsEvaluationArea *area) {
	if (inst->opcode == CEE_LDELEMA) {
		MonoInst *array_inst = inst->inst_left;
		MonoInst *index_inst = inst->inst_right;
		
		// The array must be a local variable and the index must be a properly summarized value
		if ((array_inst->opcode == CEE_LDIND_REF) &&
				((array_inst->inst_left->opcode == OP_LOCAL)||(array_inst->inst_left->opcode == OP_ARG))) {
			gssize array_variable = array_inst->inst_left->inst_c0;
			MonoRelationsEvaluationContext *array_context = &(area->contexts [array_variable]);
			MonoSummarizedValue index_value;
			
			summarize_value (index_inst, &index_value);
			if (index_value.type == MONO_CONSTANT_SUMMARIZED_VALUE) {
				// The easiest case: we just evaluate the array length, to see if it has some relation
				// with the index constant, and act accordingly
				
				clean_contexts (area->contexts, area->cfg->num_varinfo);
				evaluate_relation_with_target_variable (area, array_variable, array_variable, NULL);
				
				if ((index_value.value.constant.value >= 0) && (index_value.value.constant.value < array_context->ranges.zero.lower)) {
					if (REPORT_ABC_REMOVAL) {
						printf ("ARRAY-ACCESS: removed bounds check on array %d with constant index %d in method %s\n",
								array_variable, index_value.value.constant.value, mono_method_full_name (area->cfg->method, TRUE));
					}
					inst->flags |= (MONO_INST_NORANGECHECK);
				}
			} else if (index_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) {
				// The common case: we must evaluate both the index and the array length, and check for relevant
				// relations both through variable definitions and as constant definitions
				
				gssize index_variable = index_value.value.variable.variable;
				MonoRelationsEvaluationContext *index_context = &(area->contexts [index_variable]);
				
				clean_contexts (area->contexts, area->cfg->num_varinfo);
				
				evaluate_relation_with_target_variable (area, index_variable, array_variable, NULL);
				evaluate_relation_with_target_variable (area, array_variable, array_variable, NULL);
				
				MONO_SUB_DELTA_SAFELY_FROM_RANGES (index_context->ranges, index_value.value.variable.delta);
				
				if (index_context->ranges.zero.lower >= 0) {
					if (TRACE_ABC_REMOVAL) {
						printf ("ARRAY-ACCESS: Removed lower bound check on array %d with index %d\n", array_variable, index_variable);
					}
					if ((index_context->ranges.variable.upper < 0)||(index_context->ranges.zero.upper < array_context->ranges.zero.lower)) {
						if (REPORT_ABC_REMOVAL) {
							printf ("ARRAY-ACCESS: removed bounds check on array %d with index %d in method %s\n",
									array_variable, index_variable, mono_method_full_name (area->cfg->method, TRUE));
						}
						inst->flags |= (MONO_INST_NORANGECHECK);
					}
				}
				if (TRACE_ABC_REMOVAL) {
					if (index_context->ranges.variable.upper < 0) {
						printf ("ARRAY-ACCESS: Removed upper bound check (through variable) on array %d with index %d\n", array_variable, index_variable);
					}
					if (index_context->ranges.zero.upper < array_context->ranges.zero.lower) {
						printf ("ARRAY-ACCESS: Removed upper bound check (through constant) on array %d with index %d\n", array_variable, index_variable);
					}
				}
			}
			
		}
	}
}

/*
 * Recursively scan a tree of MonoInst looking for array accesses.
 * inst: the root of the MonoInst tree
 * area: the current evaluation area (it contains the relation graph and
 *       memory for all the evaluation contexts is already allocated)
 */
static void
process_inst (MonoInst *inst, MonoVariableRelationsEvaluationArea *area) {
	if (inst->opcode == CEE_LDELEMA) { /* Handle OP_LDELEMA2D, too */
		if (TRACE_ABC_REMOVAL) {
			printf ("Attempting check removal...\n");
		}
		
		remove_abc_from_inst (inst, area);
	}

	if (mono_burg_arity [inst->opcode]) {
		process_inst (inst->inst_left, area);
		if (mono_burg_arity [inst->opcode] > 1) {
			process_inst (inst->inst_right, area);
		}
	}
}




/*
 * Process a BB removing bounds checks from array accesses.
 * It does the following (in sequence):
 * - Get the BB entry condition
 * - Add its relations to the relation graph in the evaluation area
 * - Process all the MonoInst trees in the BB
 * - Recursively process all the children BBs in the dominator tree
 * - Remove the relations previously added to the relation graph
 *
 * bb: the BB that must be processed
 * area: the current evaluation area (it contains the relation graph and
 *       memory for all the evaluation contexts is already allocated)
 */
static void
process_block (MonoBasicBlock *bb, MonoVariableRelationsEvaluationArea *area) {
	int inst_index;
	MonoInst *current_inst;
	MonoAdditionalVariableRelationsForBB additional_relations;
	GList *dominated_bb;
	
	if (TRACE_ABC_REMOVAL) {
		printf ("Processing block %d [dfn %d]...\n", bb->block_num, bb->dfn);
	}
	
	get_relations_from_previous_bb (bb, &additional_relations);
	if (TRACE_ABC_REMOVAL) {
		if (additional_relations.relation1.relation.relation != MONO_ANY_RELATION) {
			printf ("Adding relation 1 on variable %d: ", additional_relations.relation1.variable);
			print_summarized_value_relation (&(additional_relations.relation1.relation));
			printf ("\n");
		}
		if (additional_relations.relation2.relation.relation != MONO_ANY_RELATION) {
			printf ("Adding relation 2 on variable %d: ", additional_relations.relation2.variable);
			print_summarized_value_relation (&(additional_relations.relation2.relation));
			printf ("\n");
		}
	}
	apply_change_to_evaluation_area (area, &(additional_relations.relation1));
	apply_change_to_evaluation_area (area, &(additional_relations.relation2));
	
	inst_index = 0;
	current_inst = bb->code;
	while (current_inst != NULL) {
		if (TRACE_ABC_REMOVAL) {
			printf ("Processing instruction %d\n", inst_index);
			inst_index++;
		}
		
		process_inst (current_inst, area);
		
		current_inst = current_inst->next;
	}
	
	
	if (TRACE_ABC_REMOVAL) {
		printf ("Processing block %d [dfn %d] done.\n", bb->block_num, bb->dfn);
	}
	
	for (dominated_bb = g_list_first (bb->dominated); dominated_bb != NULL; dominated_bb = g_list_next (dominated_bb)) {
		process_block ((MonoBasicBlock*) (dominated_bb->data), area);
	}
	
	remove_change_from_evaluation_area (&(additional_relations.relation1));
	remove_change_from_evaluation_area (&(additional_relations.relation2));
}



/**
 * mono_perform_abc_removal:
 * @cfg: Control Flow Graph
 *
 * Performs the ABC removal from a cfg in SSA form.
 * It does the following:
 * - Prepare the evaluation area
 * - Allocate memory for the relation graph in the evaluation area
 *   (of course, only for variable definitions) and summarize there all
 *   variable definitions
 * - Allocate memory for the evaluation contexts in the evaluation area
 * - Recursively process all the BBs in the dominator tree (it is enough
 *   to invoke the processing on the entry BB)
 * 
 * cfg: the method code
 */
void
mono_perform_abc_removal (MonoCompile *cfg)
{
	MonoVariableRelationsEvaluationArea area;
	int i;
	
	verbose_level = cfg->verbose_level;
	
	if (TRACE_ABC_REMOVAL) {
		printf ("Removing array bound checks in %s\n", mono_method_full_name (cfg->method, TRUE));
	}
	
	area.cfg = cfg;
	area.relations = (MonoSummarizedValueRelation *)
		alloca (sizeof (MonoSummarizedValueRelation) * (cfg->num_varinfo) * 2);
	area.contexts = (MonoRelationsEvaluationContext *)
		alloca (sizeof (MonoRelationsEvaluationContext) * (cfg->num_varinfo));
	for (i = 0; i < cfg->num_varinfo; i++) {
		area.relations [i].relation = MONO_EQ_RELATION;
		area.relations [i].relation_is_static_definition = TRUE;
		area.relations [i].next = NULL;
		if (cfg->vars [i]->def != NULL) {
			MonoInst *value = get_variable_value_from_store_instruction (cfg->vars [i]->def, i);
			if (value != NULL) {
				summarize_value (value, &(area.relations [i].related_value));
				if (TRACE_ABC_REMOVAL) {
					printf ("Summarized variable %d: ", i);
					print_summarized_value (&(area.relations [i].related_value));
					printf ("\n");
				}
			} else {
				MAKE_VALUE_ANY (area.relations [i].related_value);
				if (TRACE_ABC_REMOVAL) {
					printf ("Definition of variable %d is not a proper store\n", i);
				}
			}
		} else {
			MAKE_VALUE_ANY (area.relations [i].related_value);
			if (TRACE_ABC_REMOVAL) {
				printf ("Variable %d has no definition, probably it is an argument\n", i);
			}
		}
	}
	for (i = 0; i < cfg->num_varinfo; i++) {
		if (area.relations [i].related_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) {
			int related_index = cfg->num_varinfo + i;
			int related_variable = area.relations [i].related_value.value.variable.variable;
			
			area.relations [related_index].relation = MONO_EQ_RELATION;
			area.relations [related_index].relation_is_static_definition = TRUE;
			area.relations [related_index].related_value.type = MONO_VARIABLE_SUMMARIZED_VALUE;
			area.relations [related_index].related_value.value.variable.variable = i;
			area.relations [related_index].related_value.value.variable.delta = - area.relations [i].related_value.value.variable.delta;
			
			area.relations [related_index].next = area.relations [related_variable].next;
			area.relations [related_variable].next = &(area.relations [related_index]);
			
			if (TRACE_ABC_REMOVAL) {
				printf ("Added symmetric summarized value for variable variable %d (to %d): ", i, related_variable);
				print_summarized_value (&(area.relations [related_index].related_value));
				printf ("\n");
			}
		}
	}
	
	process_block (cfg->bblocks [0], &area);
}

