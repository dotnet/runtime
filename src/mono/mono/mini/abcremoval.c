/**
 * \file
 * Array bounds check removal
 *
 * Author:
 *   Massimiliano Mantione (massi@ximian.com)
 *
 * (C) 2004 Ximian, Inc.  http://www.ximian.com
 */
#include <config.h>
#include <string.h>
#include <stdio.h>

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/utils/mono-compiler.h>

#include <config.h>

#ifndef DISABLE_JIT

#include "abcremoval.h"

#if TARGET_SIZEOF_VOID_P == 8
#define OP_PCONST OP_I8CONST
#else
#define OP_PCONST OP_ICONST
#endif


#define TRACE_ABC_REMOVAL (verbose_level > 2)
#define REPORT_ABC_REMOVAL (verbose_level > 1)

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

#define MONO_NEGATED_RELATION(r) ((MonoValueRelation)((~(r))&MONO_ANY_RELATION))
#define MONO_SYMMETRIC_RELATION(r) ((MonoValueRelation)(((r)&MONO_EQ_RELATION)|(((r)&MONO_LT_RELATION)<<1)|((r&MONO_GT_RELATION)>>1)))



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
		printf ("CONSTANT %d, not-null = %d", value->value.constant.value, value->value.constant.nullness);
		break;
	case MONO_VARIABLE_SUMMARIZED_VALUE:
		printf ("VARIABLE %d, delta %d, not-null = %d", value->value.variable.variable, value->value.variable.delta, value->value.variable.nullness);
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
	printf ("(ranges: zero [%d,%d] (not-null = %d), variable [%d,%d])",
		ranges->zero.lower, ranges->zero.upper, ranges->zero.nullness,
		ranges->variable.lower, ranges->variable.upper);
}

static void
print_evaluation_context (MonoRelationsEvaluationContext *context, MonoRelationsEvaluationStatus status) {
	print_evaluation_context_status (status);
	if (status & (MONO_RELATIONS_EVALUATION_IN_PROGRESS|MONO_RELATIONS_EVALUATION_COMPLETED)) {
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
 * Check if the delta of an integer variable value is safe with respect
 * to the variable size in bytes and its kind (signed or unsigned).
 * If the delta is not safe, make the value an "any".
 */
static G_GNUC_UNUSED void
check_delta_safety (MonoVariableRelationsEvaluationArea *area, MonoSummarizedValue *value) {
	if (value->type == MONO_VARIABLE_SUMMARIZED_VALUE) {
		int variable = value->value.variable.variable;
		int delta = value->value.variable.delta;
		if ((area->variable_value_kind [variable]) & MONO_UNSIGNED_VALUE_FLAG) {
			if (delta < 0) {
				MAKE_VALUE_ANY (*value);
			}
		} else {
			if (((area->variable_value_kind [variable]) & MONO_INTEGER_VALUE_SIZE_BITMASK) < 4) {
				MAKE_VALUE_ANY (*value);
			} else if (delta > 16) {
				MAKE_VALUE_ANY (*value);
			}
		}
	}
}

/*
 * get_relation_from_ins:
 *
 *   Obtain relations from a MonoInst.
 *
 * result_value_kind: the "expected" kind of result;
 * result: the "summarized" value
 * returns the "actual" kind of result, if guessable (otherwise MONO_UNKNOWN_INTEGER_VALUE)
 */
static MonoIntegerValueKind
get_relation_from_ins (MonoVariableRelationsEvaluationArea *area, MonoInst *ins, MonoSummarizedValueRelation *result, MonoIntegerValueKind result_value_kind)
{
	MonoIntegerValueKind value_kind;
	MonoSummarizedValue *value = &result->related_value;

	if (ins->type == STACK_I8) {
		value_kind = MONO_INTEGER_VALUE_SIZE_8;
	} else if (ins->type == STACK_I4) {
		value_kind = MONO_INTEGER_VALUE_SIZE_4;
	} else {
		value_kind = MONO_UNKNOWN_INTEGER_VALUE;
	}

	result->relation = MONO_EQ_RELATION;
	MAKE_VALUE_ANY (*value);

	switch (ins->opcode) {
	case OP_ICONST:
		value->type = MONO_CONSTANT_SUMMARIZED_VALUE;
		value->value.constant.value = ins->inst_c0;
		value->value.constant.nullness = MONO_VALUE_MAYBE_NULL;
		break;
	case OP_MOVE:
		value->type = MONO_VARIABLE_SUMMARIZED_VALUE;
		value->value.variable.variable = ins->sreg1;
		value->value.variable.delta = 0;
		value->value.variable.nullness = (MonoValueNullness) (MONO_VALUE_IS_VARIABLE | MONO_VALUE_MAYBE_NULL);
		break;
	case OP_SEXT_I4:
		value->type = MONO_VARIABLE_SUMMARIZED_VALUE;
		value->value.variable.variable = ins->sreg1;
		value->value.variable.delta = 0;
		value->value.variable.nullness = MONO_VALUE_MAYBE_NULL;
		value_kind = MONO_INTEGER_VALUE_SIZE_8;
		break;
	case OP_PHI:
		value->type = MONO_PHI_SUMMARIZED_VALUE;
		value->value.phi.number_of_alternatives = *(ins->inst_phi_args);
		value->value.phi.phi_alternatives = ins->inst_phi_args + 1;
		break;
	case OP_IADD_IMM:
		value->type = MONO_VARIABLE_SUMMARIZED_VALUE;
		value->value.variable.variable = ins->sreg1;
		value->value.variable.delta = ins->inst_imm;
		value->value.variable.nullness = MONO_VALUE_MAYBE_NULL;
		/* FIXME: */
		//check_delta_safety (area, result);
		break;
	case OP_ISUB_IMM:
		value->type = MONO_VARIABLE_SUMMARIZED_VALUE;
		value->value.variable.variable = ins->sreg1;
		value->value.variable.delta = -ins->inst_imm;
		value->value.variable.nullness = MONO_VALUE_MAYBE_NULL;
		/* FIXME: */
		//check_delta_safety (area, result);
		break;
	case OP_IREM_UN:
		/* The result of an unsigned remainder is 0 < x < the divisor */
		result->relation = MONO_LT_RELATION;
		value->type = MONO_VARIABLE_SUMMARIZED_VALUE;
		value->value.variable.variable = ins->sreg2;
		value->value.variable.delta = 0;
		value->value.variable.nullness = MONO_VALUE_MAYBE_NULL;
		value_kind = MONO_UNSIGNED_INTEGER_VALUE_SIZE_4;
		break;
	case OP_LDLEN:
		/*
		 * We represent arrays by their length, so r1<-ldlen r2 is stored
		 * as r1 == r2 in the evaluation graph.
		 */
		value->type = MONO_VARIABLE_SUMMARIZED_VALUE;
		value->value.variable.variable = ins->sreg1;
		value->value.variable.delta = 0;
		value->value.variable.nullness = MONO_VALUE_MAYBE_NULL;
		value_kind = MONO_UNSIGNED_INTEGER_VALUE_SIZE_4;
		break;
	case OP_NEWARR:
		value->type = MONO_VARIABLE_SUMMARIZED_VALUE;
		value->value.variable.variable = ins->sreg1;
		value->value.variable.delta = 0;
		value->value.variable.nullness = MONO_VALUE_NOT_NULL;
		area->defs [ins->dreg] = ins;
		break;
	case OP_LDADDR:
		/* The result is non-null */
		result->relation = MONO_GE_RELATION;
		value->type = MONO_CONSTANT_SUMMARIZED_VALUE;
		value->value.constant.value = INT_MIN;
		value->value.constant.nullness = MONO_VALUE_NOT_NULL;
		break;

		/* FIXME: Add more opcodes */
	default:
		/* These opcodes are not currently handled while running SciMark, first
		 * column is the number of times the warning was shown:
		 *
		 *       1 add_imm
		 *       1 float_conv_to_i8
		 *       1 int_mul_ovf_un
		 *       1 int_neg
		 *       1 int_or
		 *       1 int_shr_un
		 *       1 localloc
		 *       1 long_ceq
		 *       1 long_rem
		 *       1 long_sub
		 *       2 int_ceq
		 *       2 int_conv_to_i2
		 *       2 int_min
		 *       2 lcall
		 *       2 long_div
		 *       3 int_conv_to_u2
		 *       3 long_shr_imm
		 *       4 int_rem
		 *       4 int_rem_imm
		 *       4 loadi1_membase
		 *       4 loadu4_membase
		 *       5 int_div
		 *       5 shl_imm
		 *       6 int_div_imm
		 *       6 int_mul
		 *       9 int_mul_imm
		 *       9 zext_i4
		 *      10 int_shr_imm
		 *      12 int_shr_un_imm
		 *      12 long_add_imm
		 *      12 outarg_vtretaddr
		 *      12 strlen
		 *      13 int_or_imm
		 *      23 call_membase
		 *      23 int_conv_to_u1
		 *      23 long_add
		 *      24 int_and_imm
		 *      24 int_shl_imm
		 *      24 loadu2_membase
		 *      29 loadi8_membase
		 *      31 llvm_outarg_vt
		 *      34 int_sub
		 *      34 loadu1_membase
		 *      42 int_add
		 *      85 ldaddr
		 *     116 loadi4_membase
		 *     159 x86_lea
		 *     179 sext_i4
		 *     291 load_membase
		 *     462 i8const
		 *     472 call
		 */

		break;
	}
	return value_kind;
}

static MonoValueRelation
get_relation_from_branch_instruction (MonoInst *ins)
{
	if (MONO_IS_COND_BRANCH_OP (ins)) {
		CompRelation rel = mono_opcode_to_cond (ins->opcode);

		switch (rel) {
		case CMP_EQ:
			return MONO_EQ_RELATION;
		case CMP_NE:
			return MONO_NE_RELATION;
		case CMP_LE:
		case CMP_LE_UN:
			return MONO_LE_RELATION;
		case CMP_GE:
		case CMP_GE_UN:
			return MONO_GE_RELATION;
		case CMP_LT:
		case CMP_LT_UN:
			return MONO_LT_RELATION;
		case CMP_GT:
		case CMP_GT_UN:
			return MONO_GT_RELATION;
		default:
			g_assert_not_reached ();
			return MONO_ANY_RELATION;
		}
	} else {
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
get_relations_from_previous_bb (MonoVariableRelationsEvaluationArea *area, MonoBasicBlock *bb, MonoAdditionalVariableRelationsForBB *relations)
{
	MonoBasicBlock *in_bb;
	MonoInst *ins, *compare, *branch;
	MonoValueRelation branch_relation;
	MonoValueRelation symmetric_relation;
	gboolean code_path;

	memset (relations, 0, sizeof (MonoAdditionalVariableRelationsForBB));
	INITIALIZE_VALUE_RELATION (&(relations->relation1.relation));
	relations->relation1.relation.relation_is_static_definition = FALSE;
	relations->relation1.relation.next = NULL;
	relations->relation1.insertion_point = NULL;
	relations->relation1.variable = -1;
	INITIALIZE_VALUE_RELATION (&(relations->relation2.relation));
	relations->relation2.relation.relation_is_static_definition = FALSE;
	relations->relation2.relation.next = NULL;
	relations->relation2.insertion_point = NULL;
	relations->relation2.variable = -1;

	if (bb->in_count == 1) { /* Should write the code to "sum" conditions... */
		in_bb = bb->in_bb [0];

		if ((in_bb->last_ins == NULL) || (in_bb->code == in_bb->last_ins))
			return;

		for (ins = in_bb->code; ins->next != in_bb->last_ins; ins = ins->next)
			;

		compare = ins;
		branch = ins->next;
		branch_relation = get_relation_from_branch_instruction (branch);

		if (branch_relation != MONO_ANY_RELATION) {
			if (branch->inst_true_bb == bb) {
				code_path = TRUE;
			} else if (branch->inst_false_bb == bb) {
				code_path = FALSE;
			} else {
				code_path = TRUE;
				g_assert_not_reached ();
			}
			if (!code_path)
				branch_relation = MONO_NEGATED_RELATION (branch_relation);
			symmetric_relation = MONO_SYMMETRIC_RELATION (branch_relation);

			/* FIXME: Other compare opcodes */
			if (compare->opcode == OP_ICOMPARE) {
				relations->relation1.variable = compare->sreg1;
				relations->relation1.relation.relation = branch_relation;
				relations->relation1.relation.related_value.type = MONO_VARIABLE_SUMMARIZED_VALUE;
				relations->relation1.relation.related_value.value.variable.variable = compare->sreg2;
				relations->relation1.relation.related_value.value.variable.delta = 0;
				relations->relation1.relation.related_value.value.variable.nullness = MONO_VALUE_MAYBE_NULL;

				relations->relation2.variable = compare->sreg2;
				relations->relation2.relation.relation = symmetric_relation;
				relations->relation2.relation.related_value.type = MONO_VARIABLE_SUMMARIZED_VALUE;
				relations->relation2.relation.related_value.value.variable.variable = compare->sreg1;
				relations->relation2.relation.related_value.value.variable.delta = 0;
				relations->relation1.relation.related_value.value.variable.nullness = MONO_VALUE_MAYBE_NULL;
			} else if (compare->opcode == OP_ICOMPARE_IMM) {
				relations->relation1.variable = compare->sreg1;
				relations->relation1.relation.relation = branch_relation;
				relations->relation1.relation.related_value.type = MONO_CONSTANT_SUMMARIZED_VALUE;
				relations->relation1.relation.related_value.value.constant.value = compare->inst_imm;
				relations->relation1.relation.related_value.value.constant.nullness = MONO_VALUE_MAYBE_NULL;
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
apply_change_to_evaluation_area (MonoVariableRelationsEvaluationArea *area, MonoAdditionalVariableRelation *change)
{
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
remove_change_from_evaluation_area (MonoAdditionalVariableRelation *change)
{
	if (change->insertion_point != NULL) {
		change->insertion_point->next = change->relation.next;
		change->relation.next = NULL;
	}
}


static void
clean_contexts (MonoVariableRelationsEvaluationArea *area, int number)
{
	memset(area->statuses, MONO_RELATIONS_EVALUATION_NOT_STARTED, number * sizeof(MonoRelationsEvaluationStatus));
}

static void
union_nullness (MonoRelationsEvaluationRange *range, MonoValueNullness n)
{
	range->nullness = (MonoValueNullness) (range->nullness & (MONO_VALUE_NULLNESS_MASK & n));
}

static void
intersect_nullness (MonoRelationsEvaluationRange *range, MonoValueNullness n, MonoValueRelation relation)
{
	switch (relation) {
	case MONO_NO_RELATION:
	case MONO_ANY_RELATION:
	case MONO_NE_RELATION:
		range->nullness = MONO_VALUE_MAYBE_NULL;
		break;
	default:
		range->nullness = (MonoValueNullness) (range->nullness | (MONO_VALUE_NULLNESS_MASK & n));
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
intersect_value( MonoRelationsEvaluationRange *range, MonoSummarizedConstantValue value, MonoValueRelation relation )
{
	switch (relation) {
	case MONO_NO_RELATION:
		MONO_MAKE_RELATIONS_EVALUATION_RANGE_IMPOSSIBLE (*range);
		break;
	case MONO_ANY_RELATION:
		break;
	case MONO_EQ_RELATION:
		MONO_UPPER_EVALUATION_RANGE_INTERSECTION (range->upper, value.value);
		MONO_LOWER_EVALUATION_RANGE_INTERSECTION (range->lower, value.value);
		break;
	case MONO_NE_RELATION: {
		/* IMPROVEMENT Figure this out! (ignoring it is safe anyway) */
		break;
	}
	case MONO_LT_RELATION:
		MONO_UPPER_EVALUATION_RANGE_INTERSECTION (range->upper, MONO_UPPER_EVALUATION_RANGE_NOT_EQUAL (value.value));
		break;
	case MONO_LE_RELATION:
		MONO_UPPER_EVALUATION_RANGE_INTERSECTION (range->upper, value.value);
		break;
	case MONO_GT_RELATION:
		MONO_LOWER_EVALUATION_RANGE_INTERSECTION (range->lower, MONO_LOWER_EVALUATION_RANGE_NOT_EQUAL (value.value));
		break;
	case MONO_GE_RELATION:
		MONO_LOWER_EVALUATION_RANGE_INTERSECTION (range->lower, value.value);
		break;
	default:
		g_assert_not_reached();
	}
	intersect_nullness (range, value.nullness, relation);
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
intersect_ranges (MonoRelationsEvaluationRanges *ranges, MonoRelationsEvaluationRanges *other_ranges, MonoSummarizedVariableValue value, MonoValueRelation relation)
{
	if (value.delta == 0) {
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
		if (value.nullness & MONO_VALUE_IS_VARIABLE)
			intersect_nullness (&ranges->zero, other_ranges->zero.nullness, relation);
		intersect_nullness (&ranges->zero, value.nullness, relation);
	} else {
		MonoRelationsEvaluationRanges translated_ranges = *other_ranges;
		MONO_ADD_DELTA_SAFELY_TO_RANGES (translated_ranges, value.delta);
		MonoSummarizedVariableValue translated_value = value;
		translated_value.delta = 0;
		intersect_ranges (ranges, &translated_ranges, translated_value, relation);
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
evaluate_relation_with_target_variable (MonoVariableRelationsEvaluationArea *area, const int variable, const int target_variable, MonoRelationsEvaluationContext *father_context)
{
	MonoRelationsEvaluationContext * const context = &(area->contexts [variable]);
	MonoRelationsEvaluationStatus * const status = &(area->statuses [variable]);

	// First of all, we check the evaluation status
	// (what must be done is *very* different in each case)
	switch (*status) {
	case MONO_RELATIONS_EVALUATION_NOT_STARTED: {
		MonoSummarizedValueRelation *relation = &(area->relations [variable]);

		if (TRACE_ABC_REMOVAL) {
			printf ("Evaluating variable %d (target variable %d); ", variable, target_variable);
			print_summarized_value_relation (relation);
			printf ("\n");
		}

		// We properly inizialize the context
		*status = MONO_RELATIONS_EVALUATION_IN_PROGRESS;
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
				intersect_value (&(context->ranges.zero), relation->related_value.value.constant, relation->relation);
				break;
			case MONO_VARIABLE_SUMMARIZED_VALUE:
				// Generally, evaluate related variable and intersect ranges.
				// However, some check is necessary...

				// If the relation is "ANY", nothing to do (no added information)
				if (relation->relation != MONO_ANY_RELATION) {
					int related_variable = relation->related_value.value.variable.variable;
					MonoRelationsEvaluationContext *related_context = &(area->contexts [related_variable]);
					MonoRelationsEvaluationStatus related_status = area->statuses [related_variable];

					// The second condition in the "or" avoids messing with "back edges" in the graph traversal
					// (they are simply ignored instead of triggering the handling of recursion)
					if ( (related_status == MONO_RELATIONS_EVALUATION_NOT_STARTED) || !
							((related_context->current_relation->related_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) &&
							(related_context->current_relation->related_value.value.variable.variable == variable))) {
						// Evaluate the related variable
						evaluate_relation_with_target_variable (area, related_variable, target_variable, context);

						// Check if we are part of a recursive loop
						if (*status & MONO_RELATIONS_EVALUATION_IS_RECURSIVE) {
							if (TRACE_ABC_REMOVAL) {
								printf ("Recursivity detected for variable %d (target variable %d), status ", variable, target_variable);
								print_evaluation_context_status (*status);
							}

							// If we are, check if the evaluation of the related variable is complete
							if (related_status == MONO_RELATIONS_EVALUATION_COMPLETED) {
								// If it is complete, we are part of a recursive definition.
								// Since it is a *definition* (and definitions are evaluated *before*
								// conditions because they are first in the list), intersection is not
								// strictly necessary, we simply copy the ranges and apply the delta
								context->ranges = related_context->ranges;
								/* Delta has already been checked for over/under-flow when evaluating values */
								MONO_ADD_DELTA_SAFELY_TO_RANGES (context->ranges, relation->related_value.value.variable.delta);
								*status = MONO_RELATIONS_EVALUATION_COMPLETED;
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
							intersect_ranges (&(context->ranges), &(related_context->ranges), relation->related_value.value.variable, relation->relation);
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
				phi_ranges.zero.nullness = relation->related_value.value.phi.number_of_alternatives > 0 ? MONO_VALUE_NOT_NULL : MONO_VALUE_MAYBE_NULL;
				for (phi = 0; phi < relation->related_value.value.phi.number_of_alternatives; phi++) {
					int phi_alternative = relation->related_value.value.phi.phi_alternatives [phi];
					evaluate_relation_with_target_variable (area, phi_alternative, target_variable, context);

					// This means we are part of a recursive loop
					if (*status & MONO_RELATIONS_EVALUATION_IS_RECURSIVE) {
						if (TRACE_ABC_REMOVAL) {
							printf ("Recursivity detected for variable %d (target variable %d), status ", variable, target_variable);
							print_evaluation_context_status (*status);
							printf ("\n");
						}
						if (*status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_ASCENDING) {
							is_ascending = TRUE;
						}
						if (*status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_DESCENDING) {
							is_descending = TRUE;
						}
						if (*status & MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_INDEFINITE) {
							is_ascending = TRUE;
							is_descending = TRUE;
						}
						phi_ranges.zero.nullness = MONO_VALUE_MAYBE_NULL;

						// Clear "recursivity" bits in the status (recursion has been handled)
						*status = MONO_RELATIONS_EVALUATION_IN_PROGRESS;
					} else {
						MONO_RELATIONS_EVALUATION_RANGES_UNION (phi_ranges, area->contexts [phi_alternative].ranges);
						union_nullness (&phi_ranges.zero, area->contexts [phi_alternative].ranges.zero.nullness);
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
				intersect_nullness (&context->ranges.zero, phi_ranges.zero.nullness, MONO_EQ_RELATION);
				break;
			}
			default:
				g_assert_not_reached();
			}

			// Pass to next relation
			relation = relation->next;
		}

		// Check if any recursivity bits are still in the status, and in any case clear them
		if (*status & MONO_RELATIONS_EVALUATION_IS_RECURSIVE) {
			if (TRACE_ABC_REMOVAL) {
				printf ("Recursivity for variable %d (target variable %d) discards computation, status ", variable, target_variable);
				print_evaluation_context_status (*status);
				printf ("\n");
			}
			// If yes, we did not have enough information (most likely we were evaluated inside a PHI, but we also
			// depended on the same PHI, which was still in evaluation...), so clear the status to "NOT_STARTED"
			// (if we will be evaluated again, the PHI will be already done, so our evaluation will succeed)
			*status = MONO_RELATIONS_EVALUATION_NOT_STARTED;
		} else {
			if (TRACE_ABC_REMOVAL) {
				printf ("Ranges for variable %d (target variable %d) computed: ", variable, target_variable);
				print_evaluation_context_ranges (&(context->ranges));
				printf ("\n");
			}
			// If not (the common case) the evaluation is complete, and the result is in the context
			*status = MONO_RELATIONS_EVALUATION_COMPLETED;
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
			printf ("Evaluation of variable %d (target variable %d) already in progress\n", variable, target_variable);
			print_evaluation_context (context, *status);
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
					/* No need to check path_value for over/under-flow, since delta should be safe */
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
				int index = current_context - area->contexts;
				MonoRelationsEvaluationStatus *current_status = &(area->statuses [index]);
				*current_status = (MonoRelationsEvaluationStatus)(*current_status | recursive_status);
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
			printf ("Variable %d (target variable %d) already in a recursive ring, skipping\n", variable, target_variable);
			print_evaluation_context (context, *status);
			print_summarized_value_relation (context->current_relation);
			printf ("\n");
		}
		break;
	}

}

/*
 * Apply the given value kind to the given range
 */
static void
apply_value_kind_to_range (MonoRelationsEvaluationRange *range, MonoIntegerValueKind value_kind)
{
	if (value_kind != MONO_UNKNOWN_INTEGER_VALUE) {
		if (value_kind & MONO_UNSIGNED_VALUE_FLAG) {
			if (range->lower < 0) {
				range->lower = 0;
			}
			if ((value_kind & MONO_INTEGER_VALUE_SIZE_BITMASK) == 1) {
				if (range->upper > 0xff) {
					range->upper = 0xff;
				}
			} else if ((value_kind & MONO_INTEGER_VALUE_SIZE_BITMASK) == 2) {
				if (range->upper > 0xffff) {
					range->upper = 0xffff;
				}
			}
		} else {
			if ((value_kind & MONO_INTEGER_VALUE_SIZE_BITMASK) == 1) {
				if (range->lower < -0x80) {
					range->lower = -0x80;
				}
				if (range->upper > 0x7f) {
					range->upper = 0x7f;
				}
			} else if ((value_kind & MONO_INTEGER_VALUE_SIZE_BITMASK) == 2) {
				if (range->lower < -0x8000) {
					range->lower = -0x8000;
				}
				if (range->upper > 0x7fff) {
					range->upper = 0x7fff;
				}
			}
		}
	}
}

/*
 * Attempt the removal of bounds checks from a MonoInst.
 * inst: the MonoInst
 * area: the current evaluation area (it contains the relation graph and
 *       memory for all the evaluation contexts is already allocated)
 */
static void
remove_abc_from_inst (MonoInst *ins, MonoVariableRelationsEvaluationArea *area)
{
	/* FIXME: Add support for 'constant' arrays and constant indexes */

	int array_variable = ins->sreg1;
	int index_variable = ins->sreg2;
	MonoRelationsEvaluationContext *array_context = &(area->contexts [array_variable]);
	MonoRelationsEvaluationContext *index_context = &(area->contexts [index_variable]);

	clean_contexts (area, area->cfg->next_vreg);

	evaluate_relation_with_target_variable (area, index_variable, array_variable, NULL);
	evaluate_relation_with_target_variable (area, array_variable, array_variable, NULL);

	if ((index_context->ranges.zero.lower >=0) && ((index_context->ranges.variable.upper < 0)||(index_context->ranges.zero.upper < array_context->ranges.zero.lower))) {
		if (REPORT_ABC_REMOVAL) {
			printf ("ARRAY-ACCESS: removed bounds check on array %d with index %d\n",
					array_variable, index_variable);
		}
		NULLIFY_INS (ins);
	} else {
		if (TRACE_ABC_REMOVAL) {
			if (index_context->ranges.zero.lower >= 0) {
				printf ("ARRAY-ACCESS: Removed lower bound check on array %d with index %d\n", array_variable, index_variable);
			}
			if (index_context->ranges.variable.upper < 0) {
				printf ("ARRAY-ACCESS: Removed upper bound check (through variable) on array %d with index %d\n", array_variable, index_variable);
			}
			if (index_context->ranges.zero.upper < array_context->ranges.zero.lower) {
				printf ("ARRAY-ACCESS: Removed upper bound check (through constant) on array %d with index %d\n", array_variable, index_variable);
			}
		}
	}
}

static gboolean
eval_non_null (MonoVariableRelationsEvaluationArea *area, int reg)
{
	MonoRelationsEvaluationContext *context = &(area->contexts [reg]);

	clean_contexts (area, area->cfg->next_vreg);
	evaluate_relation_with_target_variable (area, reg, reg, NULL);

	return context->ranges.zero.nullness == MONO_VALUE_NOT_NULL;
}

static void
add_non_null (MonoVariableRelationsEvaluationArea *area, MonoCompile *cfg, int reg,
			  GSList **check_relations)
{
	MonoAdditionalVariableRelation *rel;

	rel = (MonoAdditionalVariableRelation *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoAdditionalVariableRelation));
	rel->variable = reg;
	rel->relation.relation = MONO_GE_RELATION;
	rel->relation.related_value.type = MONO_CONSTANT_SUMMARIZED_VALUE;
	rel->relation.related_value.value.constant.value = INT_MIN;
	rel->relation.related_value.value.constant.nullness = MONO_VALUE_NOT_NULL;

	apply_change_to_evaluation_area (area, rel);

	*check_relations = g_slist_append_mempool (cfg->mempool, *check_relations, rel);
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
process_block (MonoCompile *cfg, MonoBasicBlock *bb, MonoVariableRelationsEvaluationArea *area) {
	MonoInst *ins;
	MonoAdditionalVariableRelationsForBB additional_relations;
	GSList *dominated_bb, *l;
	GSList *check_relations = NULL;

	if (TRACE_ABC_REMOVAL) {
		printf ("\nABCREM BLOCK/2 %d [dfn %d]...\n", bb->block_num, bb->dfn);
	}

	if (bb->region != -1)
		return;

	get_relations_from_previous_bb (area, bb, &additional_relations);
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

	for (ins = bb->code; ins; ins = ins->next) {
		MonoAdditionalVariableRelation *rel;
		int array_var, index_var;

		if (TRACE_ABC_REMOVAL)
			mono_print_ins (ins);

		if (ins->opcode == OP_BOUNDS_CHECK) { /* Handle OP_LDELEMA2D, too */
			array_var = ins->sreg1;
			index_var = ins->sreg2;

			remove_abc_from_inst (ins, area);

			/* We can derive additional relations from the bounds check */
			if (ins->opcode != OP_NOP) {
				rel = (MonoAdditionalVariableRelation *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoAdditionalVariableRelation));
				rel->variable = index_var;
				rel->relation.relation = MONO_LT_RELATION;
				rel->relation.related_value.type = MONO_VARIABLE_SUMMARIZED_VALUE;
				rel->relation.related_value.value.variable.variable = array_var;
				rel->relation.related_value.value.variable.delta = 0;
				rel->relation.related_value.value.variable.nullness = MONO_VALUE_MAYBE_NULL;

				apply_change_to_evaluation_area (area, rel);

				check_relations = g_slist_append_mempool (cfg->mempool, check_relations, rel);

				rel = (MonoAdditionalVariableRelation *)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoAdditionalVariableRelation));
				rel->variable = index_var;
				rel->relation.relation = MONO_GE_RELATION;
				rel->relation.related_value.type = MONO_CONSTANT_SUMMARIZED_VALUE;
				rel->relation.related_value.value.constant.value = 0;
				rel->relation.related_value.value.constant.nullness = MONO_VALUE_MAYBE_NULL;

				apply_change_to_evaluation_area (area, rel);

				check_relations = g_slist_append_mempool (cfg->mempool, check_relations, rel);
			}
		}

		if (ins->opcode == OP_CHECK_THIS) {
			if (eval_non_null (area, ins->sreg1)) {
				if (REPORT_ABC_REMOVAL)
					printf ("ARRAY-ACCESS: removed check_this instruction for R%d.\n", ins->sreg1);
				NULLIFY_INS (ins);
			}
		}

		if (ins->opcode == OP_NOT_NULL)
			add_non_null (area, cfg, ins->sreg1, &check_relations);

		if (ins->opcode == OP_COMPARE_IMM && ins->inst_imm == 0 && ins->next && ins->next->opcode == OP_COND_EXC_EQ) {
			if (eval_non_null (area, ins->sreg1)) {
				if (REPORT_ABC_REMOVAL)
					printf ("ARRAY-ACCESS: Removed null check for R%d.\n", ins->sreg1);
				NULLIFY_INS (ins->next);
				NULLIFY_INS (ins);
			}
		}

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */

		/*
		 * Eliminate MONO_INST_FAULT flags if possible.
		 */
		if (COMPILE_LLVM (cfg) && (ins->opcode == OP_LDLEN ||
								   ins->opcode == OP_BOUNDS_CHECK ||
								   ins->opcode == OP_STRLEN ||
								   (MONO_IS_LOAD_MEMBASE (ins) && (ins->flags & MONO_INST_FAULT)) ||
								   (MONO_IS_STORE_MEMBASE (ins) && (ins->flags & MONO_INST_FAULT)))) {
			int reg;

			if (MONO_IS_STORE_MEMBASE (ins))
				reg = ins->inst_destbasereg;
			else if (MONO_IS_LOAD_MEMBASE (ins))
				reg = ins->inst_basereg;
			else
				reg = ins->sreg1;

			/*
			 * This doesn't work because LLVM can move the non-faulting loads before the faulting
			 * ones (test_0_llvm_moving_faulting_loads ()).
			 * So only do it if we know the load cannot be moved before the instruction which ensures it is not
			 * null (i.e. the def of its sreg).
			 */
			if (area->defs [reg] && area->defs [reg]->opcode == OP_NEWARR) {
				if (REPORT_ABC_REMOVAL)
					printf ("ARRAY-ACCESS: removed MONO_INST_FAULT flag.\n");
				ins->flags &= ~MONO_INST_FAULT;
			}
			/*
			if (eval_non_null (area, reg)) {
				if (REPORT_ABC_REMOVAL)
					printf ("ARRAY-ACCESS: removed MONO_INST_FAULT flag.\n");
				ins->flags &= ~MONO_INST_FAULT;
			} else {
				add_non_null (area, cfg, reg, &check_relations);
			}
			*/
		}

MONO_RESTORE_WARNING
	}

	for (dominated_bb = bb->dominated; dominated_bb != NULL; dominated_bb = dominated_bb->next) {
		process_block (cfg, (MonoBasicBlock*) (dominated_bb->data), area);
	}

	for (l = check_relations; l; l = l->next)
		remove_change_from_evaluation_area ((MonoAdditionalVariableRelation *)l->data);

	remove_change_from_evaluation_area (&(additional_relations.relation1));
	remove_change_from_evaluation_area (&(additional_relations.relation2));
}

static MonoIntegerValueKind
type_to_value_kind (MonoType *type)
{
	if (m_type_is_byref (type))
		return MONO_UNKNOWN_INTEGER_VALUE;
	switch (type->type) {
	case MONO_TYPE_I1:
		return MONO_INTEGER_VALUE_SIZE_1;
		break;
	case MONO_TYPE_U1:
		return MONO_UNSIGNED_INTEGER_VALUE_SIZE_1;
		break;
	case MONO_TYPE_I2:
		return MONO_INTEGER_VALUE_SIZE_2;
		break;
	case MONO_TYPE_U2:
		return MONO_UNSIGNED_INTEGER_VALUE_SIZE_2;
		break;
	case MONO_TYPE_I4:
		return MONO_INTEGER_VALUE_SIZE_4;
		break;
	case MONO_TYPE_U4:
		return MONO_UNSIGNED_INTEGER_VALUE_SIZE_4;
		break;
	case MONO_TYPE_I:
		return (MonoIntegerValueKind)TARGET_SIZEOF_VOID_P;
		break;
	case MONO_TYPE_U:
		return (MonoIntegerValueKind)(MONO_UNSIGNED_VALUE_FLAG | TARGET_SIZEOF_VOID_P);
		break;
	case MONO_TYPE_I8:
		return MONO_INTEGER_VALUE_SIZE_8;
		break;
	case MONO_TYPE_U8:
		return MONO_UNSIGNED_INTEGER_VALUE_SIZE_8;
	default:
		return MONO_UNKNOWN_INTEGER_VALUE;
	}
}

/**
 * mono_perform_abc_removal:
 * \param cfg Control Flow Graph
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
	MonoBasicBlock *bb;
	int i;

	verbose_level = cfg->verbose_level;

	area.cfg = cfg;
	area.relations = (MonoSummarizedValueRelation *)
		mono_mempool_alloc (cfg->mempool, sizeof (MonoSummarizedValueRelation) * (cfg->next_vreg) * 2);

	area.contexts = (MonoRelationsEvaluationContext *)
		mono_mempool_alloc0 (cfg->mempool, sizeof (MonoRelationsEvaluationContext) * (cfg->next_vreg));

	area.statuses = (MonoRelationsEvaluationStatus *)
		mono_mempool_alloc0 (cfg->mempool, sizeof (MonoRelationsEvaluationStatus) * (cfg->next_vreg));

	area.variable_value_kind = (MonoIntegerValueKind *)
		mono_mempool_alloc (cfg->mempool, sizeof (MonoIntegerValueKind) * (cfg->next_vreg));
	area.defs = (MonoInst **)mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * cfg->next_vreg);
	for (i = 0; i < cfg->next_vreg; i++) {
		area.variable_value_kind [i] = MONO_UNKNOWN_INTEGER_VALUE;
		area.relations [i].relation = MONO_EQ_RELATION;
		area.relations [i].relation_is_static_definition = TRUE;
		MAKE_VALUE_ANY (area.relations [i].related_value);
		area.relations [i].next = NULL;
		area.defs [i] = NULL;
	}

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;

		if (TRACE_ABC_REMOVAL)
			printf ("\nABCREM BLOCK %d:\n", bb->block_num);

		for (ins = bb->code; ins; ins = ins->next) {
			const char *spec = INS_INFO (ins->opcode);
			gint32 idx, *reg;

			if (spec [MONO_INST_DEST] == ' ' || MONO_IS_STORE_MEMBASE (ins))
				continue;

			MONO_INS_FOR_EACH_REG (ins, idx, reg) {
				MonoInst *var = get_vreg_to_inst (cfg, *reg);
				if (var && (var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)))
					break;
			}
			if (idx < MONO_INST_LEN) {
				if (TRACE_ABC_REMOVAL)
					printf ("Global register %d is not in the SSA form, skipping.\n", *reg);
				continue;
			}

			if (spec [MONO_INST_DEST] == 'i') {
				MonoIntegerValueKind effective_value_kind;
				MonoRelationsEvaluationRange range;
				MonoSummarizedValueRelation *type_relation;
				MonoInst *var;

				if (TRACE_ABC_REMOVAL)
					mono_print_ins (ins);

				var = get_vreg_to_inst (cfg, ins->dreg);
				if (var)
					area.variable_value_kind [ins->dreg] = type_to_value_kind (var->inst_vtype);

				effective_value_kind = get_relation_from_ins (&area, ins, &area.relations [ins->dreg], area.variable_value_kind [ins->dreg]);

				MONO_MAKE_RELATIONS_EVALUATION_RANGE_WEAK (range);
				apply_value_kind_to_range (&range, area.variable_value_kind [ins->dreg]);
				apply_value_kind_to_range (&range, effective_value_kind);

				if (range.upper < INT_MAX) {
					type_relation = (MonoSummarizedValueRelation *) mono_mempool_alloc (cfg->mempool, sizeof (MonoSummarizedValueRelation));
					type_relation->relation = MONO_LE_RELATION;
					type_relation->related_value.type = MONO_CONSTANT_SUMMARIZED_VALUE;
					type_relation->related_value.value.constant.value = range.upper;
					type_relation->relation_is_static_definition = TRUE;
					type_relation->next = area.relations [ins->dreg].next;
					area.relations [ins->dreg].next = type_relation;
					if (TRACE_ABC_REMOVAL) {
						printf ("[var%d <= %d]", ins->dreg, range.upper);
					}
				}
				if (range.lower > INT_MIN) {
					type_relation = (MonoSummarizedValueRelation *) mono_mempool_alloc (cfg->mempool, sizeof (MonoSummarizedValueRelation));
					type_relation->relation = MONO_GE_RELATION;
					type_relation->related_value.type = MONO_CONSTANT_SUMMARIZED_VALUE;
					type_relation->related_value.value.constant.value = range.lower;
					type_relation->relation_is_static_definition = TRUE;
					type_relation->next = area.relations [ins->dreg].next;
					area.relations [ins->dreg].next = type_relation;
					if (TRACE_ABC_REMOVAL) {
						printf ("[var%d >= %d]", ins->dreg, range.lower);
					}
				}
				if (TRACE_ABC_REMOVAL) {
					printf ("Summarized variable %d: ", ins->dreg);
					print_summarized_value (&(area.relations [ins->dreg].related_value));
					printf ("\n");
				}
			}
		}
	}

	/* Add symmetric relations */
	for (i = 0; i < cfg->next_vreg; i++) {
		if (area.relations [i].related_value.type == MONO_VARIABLE_SUMMARIZED_VALUE) {
			int related_index = cfg->next_vreg + i;
			int related_variable = area.relations [i].related_value.value.variable.variable;
			MonoValueNullness symmetric_nullness = MONO_VALUE_MAYBE_NULL;
			if (area.relations [i].related_value.value.variable.nullness & MONO_VALUE_IS_VARIABLE) {
				symmetric_nullness = area.relations [i].related_value.value.variable.nullness;
			}

			area.relations [related_index].relation = MONO_EQ_RELATION;
			area.relations [related_index].relation_is_static_definition = TRUE;
			area.relations [related_index].related_value.type = MONO_VARIABLE_SUMMARIZED_VALUE;
			area.relations [related_index].related_value.value.variable.variable = i;
			area.relations [related_index].related_value.value.variable.delta = - area.relations [i].related_value.value.variable.delta;
			area.relations [related_index].related_value.value.variable.nullness = symmetric_nullness;

			area.relations [related_index].next = area.relations [related_variable].next;
			area.relations [related_variable].next = &(area.relations [related_index]);

			if (TRACE_ABC_REMOVAL) {
				printf ("Added symmetric summarized value for variable variable %d (to %d): ", i, related_variable);
				print_summarized_value (&(area.relations [related_index].related_value));
				printf ("\n");
			}
		}
	}

	process_block (cfg, cfg->bblocks [0], &area);
}

#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (abcremoval);

#endif /* !DISABLE_JIT */
