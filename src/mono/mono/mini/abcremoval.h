/**
 * \file
 * Array bounds check removal
 *
 * Author:
 *   Massimiliano Mantione (massi@ximian.com)
 *
 * (C) 2004 Ximian, Inc.  http://www.ximian.com
 */

#ifndef __MONO_ABCREMOVAL_H__
#define __MONO_ABCREMOVAL_H__

#include <limits.h>

#include "mini.h"

typedef enum {
	MONO_VALUE_MAYBE_NULL = 0,
	MONO_VALUE_NOT_NULL = 1,

	MONO_VALUE_NULLNESS_MASK = 1,

	/*
	 * If this bit is set, and the enclosing MonoSummarizedValue is a
	 * MONO_VARIABLE_SUMMARIZED_VALUE, then the "nullness" value is related
	 * to the variable referenced in MonoSummarizedVariableValue. Otherwise,
	 * the "nullness" value is constant.
	 */
	MONO_VALUE_IS_VARIABLE = 2,
} MonoValueNullness;

/**
 * All handled value types (expressions) in variable definitions and branch
 * contitions:
 * ANY: not handled
 * CONSTANT: an integer constant
 * VARIABLE: a reference to a variable, with an optional delta (can be zero)
 * PHI: a PHI definition of the SSA representation
 */
typedef enum {
	MONO_ANY_SUMMARIZED_VALUE,
	MONO_CONSTANT_SUMMARIZED_VALUE,
	MONO_VARIABLE_SUMMARIZED_VALUE,
	MONO_PHI_SUMMARIZED_VALUE
} MonoSummarizedValueType;

/**
 * A MONO_CONSTANT_SUMMARIZED_VALUE value.
 * value: the value
 */
typedef struct MonoSummarizedConstantValue {
	int value;
	MonoValueNullness nullness;
} MonoSummarizedConstantValue;

/**
 * A MONO_VARIABLE_SUMMARIZED_VALUE value
 * variable: the variable index in the cfg
 * delta: the delta (can be zero)
 */
typedef struct MonoSummarizedVariableValue {
	int variable;
	int delta;
	MonoValueNullness nullness;
} MonoSummarizedVariableValue;

/**
 * A MONO_PHI_SUMMARIZED_VALUE value.
 * number_of_alternatives: the number of alternatives in the PHI definition
 * phi_alternatives: an array of integers with the indexes of the variables
 *                   which are the alternatives in this PHI definition
 */
typedef struct MonoSummarizedPhiValue {
	int number_of_alternatives;
	int *phi_alternatives;
} MonoSummarizedPhiValue;

/**
 * A summarized value.
 * In practice it is a "tagged union".
 */
typedef struct MonoSummarizedValue {
	MonoSummarizedValueType type;
	union {
		MonoSummarizedConstantValue constant;
		MonoSummarizedVariableValue variable;
		MonoSummarizedPhiValue phi;
	} value;
} MonoSummarizedValue;

/**
 * A "relation" between two values.
 * The enumeration is used as a bit field, with three significant bits.
 * The degenerated cases are meaningful:
 * MONO_ANY_RELATION: we know nothing of this relation
 * MONO_NO_RELATION: no relation is possible (this code is unreachable)
 */
typedef enum {
	MONO_EQ_RELATION = 1,
	MONO_LT_RELATION = 2,
	MONO_GT_RELATION = 4,
	MONO_NE_RELATION = (MONO_LT_RELATION|MONO_GT_RELATION),
	MONO_LE_RELATION = (MONO_LT_RELATION|MONO_EQ_RELATION),
	MONO_GE_RELATION = (MONO_GT_RELATION|MONO_EQ_RELATION),
	MONO_ANY_RELATION = (MONO_EQ_RELATION|MONO_LT_RELATION|MONO_GT_RELATION),
	MONO_NO_RELATION = 0
} MonoValueRelation;

/**
 * A "kind" of integer value.
 * The enumeration is used as a bit field, with two fields.
 * The first, four bits wide, is the "sizeof" in bytes.
 * The second is a flag that is true if the value is unsigned.
 */
typedef enum {
	MONO_INTEGER_VALUE_SIZE_1 = 1,
	MONO_INTEGER_VALUE_SIZE_2 = 2,
	MONO_INTEGER_VALUE_SIZE_4 = 4,
	MONO_INTEGER_VALUE_SIZE_8 = 8,
	MONO_INTEGER_VALUE_SIZE_BITMASK = 15,
	MONO_UNSIGNED_VALUE_FLAG = 16,
	MONO_UNSIGNED_INTEGER_VALUE_SIZE_1 = MONO_UNSIGNED_VALUE_FLAG|MONO_INTEGER_VALUE_SIZE_1,
	MONO_UNSIGNED_INTEGER_VALUE_SIZE_2 = MONO_UNSIGNED_VALUE_FLAG|MONO_INTEGER_VALUE_SIZE_2,
	MONO_UNSIGNED_INTEGER_VALUE_SIZE_4 = MONO_UNSIGNED_VALUE_FLAG|MONO_INTEGER_VALUE_SIZE_4,
	MONO_UNSIGNED_INTEGER_VALUE_SIZE_8 = MONO_UNSIGNED_VALUE_FLAG|MONO_INTEGER_VALUE_SIZE_8,
	MONO_UNKNOWN_INTEGER_VALUE = 0
} MonoIntegerValueKind;

/**
 * A relation between variables (or a variable and a constant).
 * The first variable (the one "on the left of the expression") is implicit.
 * relation: the relation between the variable and the value
 * related_value: the related value
 * relation_is_static_definition: TRUE if the relation comes from a veriable
 *                                definition, FALSE if it comes from a branch
 *                                condition
 * next: pointer to the next relation of this variable in the evaluation area
 *       (relations are always kept in the evaluation area, one list for each
 *       variable)
 */
typedef struct MonoSummarizedValueRelation {
	MonoValueRelation relation;
	MonoSummarizedValue related_value;
	gboolean relation_is_static_definition;
	struct MonoSummarizedValueRelation *next;
} MonoSummarizedValueRelation;

/**
 * The evaluation status for one variable.
 * The enumeration is used as a bit field, because the status has two
 * distinct sections.
 * The first is the "main" one (bits 0, 1 and 2), which is actually a proper
 * enumeration (the bits are mutually exclusive, and their meaning is obvious).
 * The other section (the bits in the MONO_RELATIONS_EVALUATION_IS_RECURSIVE
 * set) is used to mark an evaluation as recursive (while backtracking through
 * the evaluation contexts), to state if the graph loop gives a value that is
 * ascending, descending or indefinite.
 * The bits are handled separately because the same evaluation context could
 * belong to more than one loop, so that each loop would set its bits.
 * After the backtracking, the bits are examined and a decision is taken.
 * 
 */
typedef enum {
	MONO_RELATIONS_EVALUATION_NOT_STARTED = 0,
	MONO_RELATIONS_EVALUATION_IN_PROGRESS = 1,
	MONO_RELATIONS_EVALUATION_COMPLETED = 2,
	MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_ASCENDING = 4,
	MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_DESCENDING = 8,
	MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_INDEFINITE = 16,
	MONO_RELATIONS_EVALUATION_IS_RECURSIVE = (MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_ASCENDING|MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_DESCENDING|MONO_RELATIONS_EVALUATION_IS_RECURSIVELY_INDEFINITE)
} MonoRelationsEvaluationStatus;

/**
 * A range of values (ranges include their limits).
 * A range from MIN_INT to MAX_INT is "indefinite" (any value).
 * A range where upper < lower means unreachable code (some of the relations
 * that generated the range is incompatible, like x = 0 and x > 0).
 * lower: the lower limit
 * upper: the upper limit
 */
typedef struct MonoRelationsEvaluationRange {
	int lower;
	int upper;
	MonoValueNullness nullness;
} MonoRelationsEvaluationRange;

/**
 * The two ranges that contain the result of a variable evaluation.
 * zero: the range with respect to zero
 * variable: the range with respect to the target variable in this evaluation
 */
typedef struct MonoRelationsEvaluationRanges {
	MonoRelationsEvaluationRange zero;
	MonoRelationsEvaluationRange variable;
} MonoRelationsEvaluationRanges;

/**
 * The context of a variable evaluation.
 * current_relation: the relation that is currently evaluated.
 * ranges: the result of the evaluation.
 * father: the context of the evaluation that invoked this one (used to
 *         perform the backtracking when loops are detected.
 */
typedef struct MonoRelationsEvaluationContext {
	MonoSummarizedValueRelation *current_relation;
	MonoRelationsEvaluationRanges ranges;
	struct MonoRelationsEvaluationContext *father;
} MonoRelationsEvaluationContext;

/*
 * Basic macros to initialize and check ranges.
 */
#define MONO_MAKE_RELATIONS_EVALUATION_RANGE_WEAK(r) do{\
		(r).lower = INT_MIN;\
		(r).upper = INT_MAX;\
		(r).nullness = MONO_VALUE_MAYBE_NULL; \
	} while (0)
#define MONO_MAKE_RELATIONS_EVALUATION_RANGES_WEAK(rs) do{\
		MONO_MAKE_RELATIONS_EVALUATION_RANGE_WEAK ((rs).zero); \
		MONO_MAKE_RELATIONS_EVALUATION_RANGE_WEAK ((rs).variable); \
	} while (0)
#define MONO_MAKE_RELATIONS_EVALUATION_RANGE_IMPOSSIBLE(r) do{\
		(r).lower = INT_MAX;\
		(r).upper = INT_MIN;\
		(r).nullness = MONO_VALUE_MAYBE_NULL; \
	} while (0)
#define MONO_MAKE_RELATIONS_EVALUATION_RANGES_IMPOSSIBLE(rs) do{\
		MONO_MAKE_RELATIONS_EVALUATION_RANGE_IMPOSSIBLE ((rs).zero); \
		MONO_MAKE_RELATIONS_EVALUATION_RANGE_IMPOSSIBLE ((rs).variable); \
	} while (0)
#define MONO_RELATIONS_EVALUATION_RANGE_IS_WEAK(r) (((r).lower==INT_MIN)&&((r).upper==INT_MAX))
#define MONO_RELATIONS_EVALUATION_RANGES_ARE_WEAK(rs) \
	(MONO_RELATIONS_EVALUATION_RANGE_IS_WEAK((rs).zero) && \
	MONO_RELATIONS_EVALUATION_RANGE_IS_WEAK((rs).variable))
#define MONO_RELATIONS_EVALUATION_RANGE_IS_IMPOSSIBLE(r) (((r).lower)>((r).upper))
#define MONO_RELATIONS_EVALUATION_RANGES_ARE_IMPOSSIBLE(rs) \
	(MONO_RELATIONS_EVALUATION_RANGE_IS_IMPOSSIBLE((rs).zero) || \
	MONO_RELATIONS_EVALUATION_RANGE_IS_IMPOSSIBLE((rs).variable))

/*
 * The following macros are needed because ranges include theit limits, but
 * some relations explicitly exclude them (GT and LT).
 */
#define MONO_UPPER_EVALUATION_RANGE_NOT_EQUAL(ur) ((((ur)==INT_MIN)||((ur)==INT_MAX))?(ur):((ur)-1))
#define MONO_LOWER_EVALUATION_RANGE_NOT_EQUAL(lr) ((((lr)==INT_MIN)||((lr)==INT_MAX))?(lr):((lr)+1))
#define MONO_APPLY_INEQUALITY_TO_EVALUATION_RANGE(r) do{\
		(r).lower = MONO_LOWER_EVALUATION_RANGE_NOT_EQUAL ((r).lower);\
		(r).upper = MONO_UPPER_EVALUATION_RANGE_NOT_EQUAL ((r).upper);\
	} while (0)
#define MONO_APPLY_INEQUALITY_TO_EVALUATION_RANGES(rs) do{\
		MONO_APPLY_INEQUALITY_TO_EVALUATION_RANGE ((rs).zero); \
		MONO_APPLY_INEQUALITY_TO_EVALUATION_RANGE ((rs).variable); \
	} while (0)

/*
 * The following macros perform union and intersection operations on ranges.
 */
#define MONO_LOWER_EVALUATION_RANGE_UNION(lr,other_lr) ((lr)=MIN(lr,other_lr))
#define MONO_UPPER_EVALUATION_RANGE_UNION(ur,other_ur) ((ur)=MAX(ur,other_ur))
#define MONO_LOWER_EVALUATION_RANGE_INTERSECTION(lr,other_lr) ((lr)=MAX(lr,other_lr))
#define MONO_UPPER_EVALUATION_RANGE_INTERSECTION(ur,other_ur) ((ur)=MIN(ur,other_ur))
#define MONO_RELATIONS_EVALUATION_RANGE_UNION(r,other_r) do{\
		MONO_LOWER_EVALUATION_RANGE_UNION((r).lower,(other_r).lower);\
		MONO_UPPER_EVALUATION_RANGE_UNION((r).upper,(other_r).upper);\
	} while (0)
#define MONO_RELATIONS_EVALUATION_RANGE_INTERSECTION(r,other_r) do{\
		MONO_LOWER_EVALUATION_RANGE_INTERSECTION((r).lower,(other_r).lower);\
		MONO_UPPER_EVALUATION_RANGE_INTERSECTION((r).upper,(other_r).upper);\
	} while (0)
#define MONO_RELATIONS_EVALUATION_RANGES_UNION(rs,other_rs) do{\
		MONO_RELATIONS_EVALUATION_RANGE_UNION ((rs).zero,(other_rs).zero); \
		MONO_RELATIONS_EVALUATION_RANGE_UNION ((rs).variable,(other_rs).variable); \
	} while (0)
#define MONO_RELATIONS_EVALUATION_RANGES_INTERSECTION(rs,other_rs) do{\
		MONO_RELATIONS_EVALUATION_RANGE_INTERSECTION ((rs).zero,(other_rs).zero); \
		MONO_RELATIONS_EVALUATION_RANGE_INTERSECTION ((rs).variable,(other_rs).variable); \
	} while (0)

/*
 * The following macros add or subtract "safely" (without over/under-flow) a
 * delta (constant) value to a range.
 */
#define MONO_ADD_DELTA_SAFELY(v,d) do{\
		if (((d) > 0) && ((v) != INT_MIN)) {\
			(v) = (((v)+(d))>(v))?((v)+(d)):INT_MAX;\
		} else if (((d) < 0) && ((v) != INT_MAX)) {\
			(v) = (((v)+(d))<(v))?((v)+(d)):INT_MIN;\
		}\
	} while (0)
#define MONO_SUB_DELTA_SAFELY(v,d) do{\
		if (((d) < 0) && ((v) != INT_MIN)) {\
			(v) = (((v)-(d))>(v))?((v)-(d)):INT_MAX;\
		} else if (((d) > 0) && ((v) != INT_MAX)) {\
			(v) = (((v)-(d))<(v))?((v)-(d)):INT_MIN;\
		}\
	} while (0)
#define MONO_ADD_DELTA_SAFELY_TO_RANGE(r,d) do{\
		MONO_ADD_DELTA_SAFELY((r).lower,(d));\
		MONO_ADD_DELTA_SAFELY((r).upper,(d));\
	} while (0)
#define MONO_SUB_DELTA_SAFELY_FROM_RANGE(r,d) do{\
		MONO_SUB_DELTA_SAFELY((r).lower,(d));\
		MONO_SUB_DELTA_SAFELY((r).upper,(d));\
	} while (0)
#define MONO_ADD_DELTA_SAFELY_TO_RANGES(rs,d) do{\
		MONO_ADD_DELTA_SAFELY_TO_RANGE((rs).zero,(d));\
		MONO_ADD_DELTA_SAFELY_TO_RANGE((rs).variable,(d));\
	} while (0)
#define MONO_SUB_DELTA_SAFELY_FROM_RANGES(rs,d) do{\
		MONO_SUB_DELTA_SAFELY_FROM_RANGE((rs).zero,(d));\
		MONO_SUB_DELTA_SAFELY_FROM_RANGE((rs).variable,(d));\
	} while (0)


/**
 * The main evaluation area.
 * cfg: the cfg of the method that is being examined.
 * relations: and array of relations, one for each method variable (each
 *            relation is the head of a list); this is the evaluation graph
 * contexts: an array of evaluation contexts (one for each method variable)
 * variable_value_kind: an array of MonoIntegerValueKind, one for each local
 *                      variable (or argument)
 * defs: maps vregs to the instruction which defines it.
 */
typedef struct MonoVariableRelationsEvaluationArea {
	MonoCompile *cfg;
	MonoSummarizedValueRelation *relations;

/**
 * statuses and contexts are parallel arrays. A given index into each refers to
 * the same context. This is a performance optimization. Clean_context was
 * coming to dominate the running time of abcremoval. By
 * storing the statuses together, we can memset the entire
 * region.
 */ 
	MonoRelationsEvaluationStatus *statuses;
	MonoRelationsEvaluationContext *contexts;

	MonoIntegerValueKind *variable_value_kind;
	MonoInst **defs;
} MonoVariableRelationsEvaluationArea;

/**
 * Convenience structure to define an "additional" relation for the main
 * evaluation graph.
 * variable: the variable to which the relation is applied
 * relation: the relation
 * insertion_point: the point in the graph where the relation is inserted
 *                  (useful for removing it from the list when backtracking
 *                  in the traversal of the dominator tree)
 */
typedef struct MonoAdditionalVariableRelation {
	int variable;
	MonoSummarizedValueRelation relation;
	MonoSummarizedValueRelation *insertion_point;
} MonoAdditionalVariableRelation;

/**
 * Convenience structure that stores two additional relations.
 * In the current code, each BB can add at most two relations to the main
 * evaluation graph, so one of these structures is enough to hold all the
 * modifications to the graph made examining one BB.
 */
typedef struct MonoAdditionalVariableRelationsForBB {
	MonoAdditionalVariableRelation relation1;
	MonoAdditionalVariableRelation relation2;
} MonoAdditionalVariableRelationsForBB;


#endif /* __MONO_ABCREMOVAL_H__ */
