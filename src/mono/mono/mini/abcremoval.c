
#include <string.h>
#include <stdio.h>

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/opcodes.h>

#include "mini.h"
#include "inssel.h"

#include "abcremoval.h"

extern guint8 mono_burg_arity [];

#define TRACE_ABC_REMOVAL (verbose_level > 2)
#define REPORT_ABC_REMOVAL (verbose_level > 0)

#define CHEAT_AND_REMOVE_ALL_CHECKS 0

static int verbose_level;

#if (!CHEAT_AND_REMOVE_ALL_CHECKS)


#define IS_SUMMARIZED_VALUE_CONSTANT(value) ((value).value_type==MONO_CONSTANT_SUMMARIZED_VALUE)
#define IS_SUMMARIZED_VALUE_VARIABLE(value) ((value).value_type==MONO_VARIABLE_SUMMARIZED_VALUE)

#define RELATION_BETWEEN_VALUES(value,related_value) (\
	((value) > (related_value))? MONO_GT_RELATION :\
	(((value) < (related_value))? MONO_LT_RELATION : MONO_EQ_RELATION))

#define MAKE_VALUE_ANY(v) do{\
		(v)->relation_with_zero = MONO_ANY_RELATION;\
		(v)->relation_with_one = MONO_ANY_RELATION;\
		(v)->relation_with_value = MONO_ANY_RELATION;\
		(v)->value_type = MONO_CONSTANT_SUMMARIZED_VALUE;\
		(v)->value.constant = 0;\
	} while (0)

#define MONO_NEGATED_RELATION(r) ((~(r))&MONO_ANY_RELATION)
#define MONO_SYMMETRIC_RELATION(r) (((r)&MONO_EQ_RELATION)|(((r)&MONO_LT_RELATION)<<1)|((r&MONO_GT_RELATION)>>1))

#define RELATION_ADDS_INFORMATION(r,r_maybe_adds_information) ((r)&(~(r_maybe_adds_information)))

unsigned char propagated_relations_table [] = {
	#include "propagated_relations_table.def"
	MONO_ANY_RELATION
};
#define PROPAGATED_RELATION(r,r_to_propagate) (propagated_relations_table [((r)<<3)|(r_to_propagate)])


static void print_relation(int relation){
	int print_or = 0;
	printf("(");
	if (relation & MONO_LT_RELATION){
		printf("LT");
		print_or = 1;
	}
	if (relation & MONO_EQ_RELATION){
		if (print_or){
			printf("|");
		}
		printf("EQ");
		print_or = 1;
	}
	if (relation & MONO_GT_RELATION){
		if (print_or){
			printf("|");
		}
		printf("GT");
		print_or = 1;
	}
	printf(")");
}

static void print_summarized_value(MonoSummarizedValue *value){
	printf("relation_with_zero: ");
	print_relation(value->relation_with_zero);
	printf("\n");
	printf("relation_with_one: ");
	print_relation(value->relation_with_one);
	printf("\n");
	printf("relation_with_value: ");
	print_relation(value->relation_with_value);
	printf("\n");
	switch (value->value_type){
		case MONO_CONSTANT_SUMMARIZED_VALUE:
			printf("Constant value: %d\n", value->value.constant);
			break;
		case MONO_VARIABLE_SUMMARIZED_VALUE:
			printf("Variable value: %d\n", value->value.variable);
			break;
		case MONO_PHI_SUMMARIZED_VALUE: {
			int i;
			printf("PHI value: (");
			for (i = 0; i < *(value->value.phi_variables); i++){
				if (i)printf(",");
				printf("%d", *(value->value.phi_variables + i + 1));
			}
			printf(")\n");
			break;
		}
		default:
			printf("Unknown value type: %d\n", value->value_type);
	}
}

static void print_branch_condition(MonoBranchCondition *condition, int n){
	printf("Branch condition %d, on variable %d\n", n, condition->variable);
	print_summarized_value(&(condition->value));
}

static void print_branch_data(MonoBranchData *branch, int n){
	int i;
	printf("Branch %d, destination BB %d [dfn %d], conditions %d\n",
		n, branch->destination_block->block_num, branch->destination_block->dfn, branch->number_of_conditions);
	for(i = 0; i < branch->number_of_conditions; i++){
		print_branch_condition(&(branch->conditions [i]), i);
	}
}

static void print_summarized_block(MonoSummarizedBasicBlock *block){
	int i;
	printf("Summarization of BB %d [dfn %d] (has array accesses: %d), branches: %d\n",
		block->block->block_num, block->block->dfn, block->has_array_access_instructions, block->number_of_branches);
	for(i = 0; i < block->number_of_branches; i++){
		print_branch_data(&(block->branches [i]), i);
	}
}

static void print_variable_relations(MonoVariableRelations *relations, gssize variable, int n){
	int i;
	int significantRelations = 0;
	for (i = 0; i < n; i++){
		if (relations->relations_with_variables [i] != MONO_ANY_RELATION){
			significantRelations++;
		}
	}
	if ((relations->relation_with_zero != MONO_ANY_RELATION) ||
			(relations->relation_with_one != MONO_ANY_RELATION) ||
			(significantRelations > 0)){
		printf("Relations for variable %d:\n", variable);
		if (relations->relation_with_zero != MONO_ANY_RELATION){
			printf("relation_with_zero: ");
			print_relation(relations->relation_with_zero);
			printf("\n");
		}
		if (relations->relation_with_one != MONO_ANY_RELATION){
			printf("relation_with_one: ");
			print_relation(relations->relation_with_one);
			printf("\n");
		}
		if (significantRelations > 0){
			printf("relations_with_variables (%d significant)\n", significantRelations);
			for (i = 0; i < n; i++){
				if (relations->relations_with_variables [i] != MONO_ANY_RELATION){
					printf("relation with variable %d: ", i);
					print_relation(relations->relations_with_variables [i]);
					printf("\n");
				}
			}
		}
	}
}

static void print_all_variable_relations(MonoVariableRelationsEvaluationArea *evaluation_area){
	int i;
	printf("relations in evaluation area:\n");
	for (i = 0; i < evaluation_area->cfg->num_varinfo; i++){
		print_variable_relations(&(evaluation_area->variable_relations [i]), i, evaluation_area->cfg->num_varinfo);
	}
}


static MonoValueRelation relation_for_sum_of_variable_and_constant(
		MonoValueRelation variable_relation, MonoValueRelation constant_relation_with_zero){
	switch (variable_relation){
		case MONO_EQ_RELATION:
			return constant_relation_with_zero;
		case MONO_GE_RELATION:
			if (constant_relation_with_zero & MONO_LT_RELATION){
				return MONO_ANY_RELATION;
			} else {
				return constant_relation_with_zero;
			}
		case MONO_LE_RELATION:
			if (constant_relation_with_zero & MONO_GT_RELATION){
				return MONO_ANY_RELATION;
			} else {
				return constant_relation_with_zero;
			}
		case MONO_GT_RELATION:
			if (constant_relation_with_zero & MONO_LT_RELATION){
				return MONO_ANY_RELATION;
			} else {
				return MONO_GT_RELATION;
			}
		case MONO_LT_RELATION:
			if (constant_relation_with_zero & MONO_GT_RELATION){
				return MONO_ANY_RELATION;
			} else {
				return MONO_LE_RELATION;
			}
		default:
			g_assert_not_reached();
			return MONO_ANY_RELATION;
	}
}

static MonoInst *get_variable_value_from_ssa_store(MonoInst *store, int expected_variable_index)
{
	switch (store->opcode){
		case CEE_STIND_REF:
		case CEE_STIND_I:
		case CEE_STIND_I4:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
			if (TRACE_ABC_REMOVAL) {
				printf("Store instruction found...\n");
			}
			if (store->inst_left->opcode == OP_LOCAL){
				int variable_index = store->inst_left->inst_c0;
				if (TRACE_ABC_REMOVAL) {
					printf("Value put in local %d (expected %d)\n", variable_index, expected_variable_index);
				}
				if (variable_index == expected_variable_index)
				{
					return store->inst_right;
				}
				else
				{
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

static void summarize_value(MonoInst *value, MonoSummarizedValue *result){
	switch (value->opcode){
		case OP_ICONST: {
			result->relation_with_zero = RELATION_BETWEEN_VALUES(value->inst_c0, 0);
			result->relation_with_one = RELATION_BETWEEN_VALUES(abs(value->inst_c0), 1);
			result->relation_with_value = MONO_EQ_RELATION;
			result->value_type = MONO_CONSTANT_SUMMARIZED_VALUE;
			result->value.constant = value->inst_c0;
			break;
		}
		case OP_LOCAL:
		case OP_ARG: {
			result->relation_with_zero = MONO_ANY_RELATION;
			result->relation_with_one = MONO_ANY_RELATION;
			result->relation_with_value = MONO_EQ_RELATION;
			result->value_type = MONO_VARIABLE_SUMMARIZED_VALUE;
			result->value.variable = value->inst_c0;
			break;
		}
		case CEE_LDIND_I4: {
			if ((value->inst_left->opcode == OP_LOCAL) || (value->inst_left->opcode == OP_ARG)){
				summarize_value(value->inst_left, result);
			} else {
				MAKE_VALUE_ANY(result);
			}
			break;
		}
		case CEE_LDIND_REF: {
			if ((value->inst_left->opcode == OP_LOCAL) || (value->inst_left->opcode == OP_ARG)){
				summarize_value(value->inst_left, result);
			} else {
				MAKE_VALUE_ANY(result);
			}
			break;
		}
		case CEE_ADD:
		case CEE_SUB: {
			MonoSummarizedValue left_value;
			MonoSummarizedValue right_value;
			summarize_value(value->inst_left, &left_value);
			summarize_value(value->inst_right, &right_value);
			
			if (IS_SUMMARIZED_VALUE_VARIABLE(left_value)) {
				if (IS_SUMMARIZED_VALUE_CONSTANT(right_value)&& (right_value.value.constant == 1)){
					MonoValueRelation constant_relation_with_zero = right_value.relation_with_zero;
					if (value->opcode == CEE_SUB) {
						constant_relation_with_zero = MONO_SYMMETRIC_RELATION(constant_relation_with_zero);
					}
					result->relation_with_value = relation_for_sum_of_variable_and_constant(
						left_value.relation_with_value, constant_relation_with_zero);
					if (result->relation_with_value != MONO_ANY_RELATION){
						result->relation_with_zero = MONO_ANY_RELATION;
						result->relation_with_one = MONO_ANY_RELATION;
						result->value_type = MONO_VARIABLE_SUMMARIZED_VALUE;
						result->value.variable = left_value.value.variable;
					} else {
						MAKE_VALUE_ANY(result);
					}
				} else {
					MAKE_VALUE_ANY(result);
				}
			} else if (IS_SUMMARIZED_VALUE_VARIABLE(right_value)) {
				if (IS_SUMMARIZED_VALUE_CONSTANT(left_value)&& (left_value.value.constant == 1)){
					MonoValueRelation constant_relation_with_zero = left_value.relation_with_zero;
					if (value->opcode == CEE_SUB) {
						constant_relation_with_zero = MONO_SYMMETRIC_RELATION(constant_relation_with_zero);
					}
					result->relation_with_value = relation_for_sum_of_variable_and_constant(
						right_value.relation_with_value, constant_relation_with_zero);
					if (result->relation_with_value != MONO_ANY_RELATION){
						result->relation_with_zero = MONO_ANY_RELATION;
						result->relation_with_one = MONO_ANY_RELATION;
						result->value_type = MONO_VARIABLE_SUMMARIZED_VALUE;
						result->value.variable = right_value.value.variable;
					} else {
						MAKE_VALUE_ANY(result);
					}
				} else {
					MAKE_VALUE_ANY(result);
				}
			} else if (IS_SUMMARIZED_VALUE_CONSTANT(right_value)&& IS_SUMMARIZED_VALUE_CONSTANT(left_value)) {
				/* This should not happen if constant folding has been done */
				if (right_value.relation_with_value == MONO_EQ_RELATION && left_value.relation_with_value == MONO_EQ_RELATION){
					int constant;
					if (value->opcode == CEE_ADD) {
						constant = right_value.value.constant + left_value.value.constant;
					}
					else {
						constant = right_value.value.constant - left_value.value.constant;
					}
					result->relation_with_zero = RELATION_BETWEEN_VALUES(constant, 0);
					result->relation_with_one = RELATION_BETWEEN_VALUES(abs(constant), 1);
					result->relation_with_value = MONO_EQ_RELATION;
					result->value_type = MONO_CONSTANT_SUMMARIZED_VALUE;
					result->value.constant = constant;
				} else {
					MAKE_VALUE_ANY(result);
				}
			} else {
				MAKE_VALUE_ANY(result);
			}
			break;
		}
		case CEE_NEWARR: {
			summarize_value(value->inst_newa_len, result);
			break;
		}
		case CEE_LDLEN: {
			summarize_value(value->inst_left, result);
			break;
		}
		case OP_PHI: {
			result->relation_with_zero = MONO_ANY_RELATION;
			result->relation_with_one = MONO_ANY_RELATION;
			result->relation_with_value = MONO_EQ_RELATION;
			result->value_type = MONO_PHI_SUMMARIZED_VALUE;
			result->value.phi_variables = value->inst_phi_args;
			break;
		}
		default: {
			MAKE_VALUE_ANY(result);
		}
	}
}

static MonoValueRelation get_relation_from_branch_instruction(int opcode){
	switch (opcode){
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
				g_assert_not_reached();
				return MONO_ANY_RELATION;
	}
}

static int contains_array_access(MonoInst *inst){
	if (inst->opcode == CEE_LDELEMA){ /* Handle OP_LDELEMA2D, too */
		return 1;
	}
	
	if (mono_burg_arity [inst->opcode]){
		if (contains_array_access(inst->inst_left)) {
			return 1;
		}
		if (mono_burg_arity [inst->opcode] > 1){
			return contains_array_access(inst->inst_right);
		}
	}
	return 0;
}

static void analyze_block(MonoBasicBlock *bb, MonoVariableRelationsEvaluationArea *evaluation_area){
	MonoSummarizedBasicBlock *b = &(evaluation_area->blocks [bb->dfn]);
	MonoInst *current_inst = bb->code;
	MonoInst *last_inst = NULL;
	int inst_index = 0;
	
	if (TRACE_ABC_REMOVAL) {
		printf("Analyzing block %d [dfn %d]\n", bb->block_num, bb->dfn);
	}
	
	g_assert(bb->dfn < evaluation_area->cfg->num_bblocks);
	b->has_array_access_instructions = 0;
	b->block = bb;
	
	while (current_inst != NULL){
		if (TRACE_ABC_REMOVAL) {
			printf("Analyzing instruction %d\n", inst_index);
			inst_index++;
		}
		
		if (contains_array_access(current_inst)) {
			b->has_array_access_instructions = 1;
		}
		
		if (current_inst->next == NULL){
			last_inst = current_inst;
		}
		current_inst = current_inst->next;
	}
	
	if (last_inst != NULL){
		switch (last_inst->opcode){
			case CEE_BEQ:
			case CEE_BLT:
			case CEE_BLE:
			case CEE_BGT:
			case CEE_BGE:
			case CEE_BNE_UN:
			case CEE_BLT_UN:
			case CEE_BLE_UN:
			case CEE_BGT_UN:
			case CEE_BGE_UN: {
				if (last_inst->inst_left->opcode == OP_COMPARE){
					int number_of_variables;
					int current_variable;

					MonoSummarizedValue left_value;
					MonoSummarizedValue right_value;
					MonoValueRelation relation = get_relation_from_branch_instruction(last_inst->opcode);
					MonoValueRelation symmetric_relation = MONO_SYMMETRIC_RELATION(relation);
					summarize_value(last_inst->inst_left->inst_left, &left_value);
					summarize_value(last_inst->inst_left->inst_right, &right_value);
					number_of_variables = 0;
					current_variable = 0;
					/* It is actually possible to handle some more case... */
					if ((left_value.relation_with_value == MONO_EQ_RELATION) &&
						(right_value.relation_with_value == MONO_EQ_RELATION)){
						if (left_value.value_type == MONO_VARIABLE_SUMMARIZED_VALUE)number_of_variables++;
						if (right_value.value_type == MONO_VARIABLE_SUMMARIZED_VALUE)number_of_variables++;
						if (number_of_variables > 0){
							MonoBranchData *branch_true;
							MonoBranchData *branch_false;

							b->number_of_branches = 2;
							b->branches = (MonoBranchData*) mono_mempool_alloc(
								evaluation_area->pool, sizeof(MonoBranchData) * 2);
							branch_true = &(b->branches [0]);
							branch_true->destination_block = last_inst->inst_true_bb;
							branch_true->number_of_conditions = number_of_variables;
							branch_true->conditions = (MonoBranchCondition*)
								mono_mempool_alloc(evaluation_area->pool, sizeof(MonoBranchCondition) * number_of_variables);
							branch_false = &(b->branches [1]);
							branch_false->destination_block = last_inst->inst_false_bb;
							branch_false->number_of_conditions = number_of_variables;
							branch_false->conditions = (MonoBranchCondition*)
								mono_mempool_alloc(evaluation_area->pool, sizeof(MonoBranchCondition) * number_of_variables);
							if (left_value.value_type == MONO_VARIABLE_SUMMARIZED_VALUE){
								MonoBranchCondition *condition_true = &(branch_true->conditions [current_variable]);
								MonoBranchCondition *condition_false;

								condition_true->variable = left_value.value.variable;
								condition_true->value = right_value;
								condition_true->value.relation_with_zero = MONO_ANY_RELATION;
								condition_true->value.relation_with_one = MONO_ANY_RELATION;
								condition_true->value.relation_with_value = relation;
								condition_false = &(branch_false->conditions [current_variable]);
								condition_false->variable = left_value.value.variable;
								condition_false->value = right_value;
								condition_false->value.relation_with_zero = MONO_ANY_RELATION;
								condition_false->value.relation_with_one = MONO_ANY_RELATION;
								condition_false->value.relation_with_value = MONO_NEGATED_RELATION(relation);
								current_variable++;
							}
							if (right_value.value_type == MONO_VARIABLE_SUMMARIZED_VALUE){
								MonoBranchCondition *condition_true = &(branch_true->conditions [current_variable]);
								MonoBranchCondition *condition_false;

								condition_true->variable = right_value.value.variable;
								condition_true->value = left_value;
								condition_true->value.relation_with_zero = MONO_ANY_RELATION;
								condition_true->value.relation_with_one = MONO_ANY_RELATION;
								condition_true->value.relation_with_value = symmetric_relation;
								condition_false = &(branch_false->conditions [current_variable]);
								condition_false->variable = right_value.value.variable;
								condition_false->value = left_value;
								condition_false->value.relation_with_zero = MONO_ANY_RELATION;
								condition_false->value.relation_with_one = MONO_ANY_RELATION;
								condition_false->value.relation_with_value = MONO_NEGATED_RELATION(symmetric_relation);
							}
							
						} else {
							b->number_of_branches = 0;
							b->branches = NULL;
						}
					} else {
						b->number_of_branches = 0;
						b->branches = NULL;
					}
				} else {
					b->number_of_branches = 0;
					b->branches = NULL;
				}
				break;
			}
			case CEE_SWITCH:
				/* Should handle switches... */
				/* switch_operand = last_inst->inst_right; */
				/* number_of_cases = GPOINTER_TO_INT (last_inst->klass); */
				/* cases = last_inst->inst_many_bb; */
				b->number_of_branches = 0;
				b->branches = NULL;
				break;
			default:
				b->number_of_branches = 0;
				b->branches = NULL;
		}
	} else {
		b->number_of_branches = 0;
		b->branches = NULL;
	}
	
	if (TRACE_ABC_REMOVAL) {
		print_summarized_block(b);
	}
}

#define SET_VARIABLE_RELATIONS(vr, relation, n)do {\
	(vr)->relation_with_zero = (relation);\
	(vr)->relation_with_one = (relation);\
	memset((vr)->relations_with_variables,(relation),(n));\
} while (0);

#define COMBINE_VARIABLE_RELATIONS(operator, vr, related_vr, n)do {\
	int i;\
	operator((vr)->relation_with_zero, (related_vr)->relation_with_zero);\
	operator((vr)->relation_with_one, (related_vr)->relation_with_one);\
	for (i = 0; i < (n); i++){\
		operator((vr)->relations_with_variables [i], (related_vr)->relations_with_variables [i]);\
	}\
} while (0);

#define RELATION_ASSIGNMENT(destination,source) (destination)=(source)
#define RELATION_UNION(destination,source) (destination)|=(source)
#define RELATION_INTERSECTION(destination,source) (destination)&=(source)



static void evaluate_variable_relations(gssize variable, MonoVariableRelationsEvaluationArea *evaluation_area, MonoRelationsEvaluationContext *father_context){
	MonoVariableRelations *relations;
	MonoRelationsEvaluationContext context;

	if (TRACE_ABC_REMOVAL) {
	printf("Applying definition of variable %d\n", variable);
	}
	relations = &(evaluation_area->variable_relations [variable]);
	context.father_context = father_context;
	context.variable = variable;
	switch (relations->evaluation_step) {
		case MONO_RELATIONS_EVALUATION_NOT_STARTED: {
			MonoSummarizedValue *value = &(evaluation_area->variable_definitions [variable]);
			relations->evaluation_step = MONO_RELATIONS_EVALUATION_IN_PROGRESS;
			if (TRACE_ABC_REMOVAL) {
				printf("Current step is MONO_RELATIONS_EVALUATION_NOT_STARTED, summarized value is:\n");
				print_summarized_value(value);
			}
			switch (value->value_type){
				case MONO_CONSTANT_SUMMARIZED_VALUE: {
					if (value->relation_with_value == MONO_EQ_RELATION){
						relations->relation_with_zero &= RELATION_BETWEEN_VALUES(value->value.constant, 0);
						relations->relation_with_one &= RELATION_BETWEEN_VALUES(abs(value->value.constant), 1);
					}
					/* Other cases should not happen... */
					break;
				}
				case MONO_VARIABLE_SUMMARIZED_VALUE: {
					gssize related_variable = value->value.variable;
					relations->relations_with_variables [related_variable] = value->relation_with_value;
					evaluate_variable_relations(related_variable, evaluation_area, &context);
					if (value->relation_with_value == MONO_EQ_RELATION){
						COMBINE_VARIABLE_RELATIONS(RELATION_INTERSECTION, relations,
							&(evaluation_area->variable_relations [related_variable]), evaluation_area->cfg->num_varinfo);
					}
					/* Other cases should be handled... */
					break;
				}
				case MONO_PHI_SUMMARIZED_VALUE: {
					if (value->relation_with_value == MONO_EQ_RELATION){
						int phi;
						MonoVariableRelations *phi_union = (MonoVariableRelations*) alloca(sizeof(MonoVariableRelations));
						phi_union->relations_with_variables = (unsigned char*) alloca(evaluation_area->cfg->num_varinfo);
						SET_VARIABLE_RELATIONS(phi_union, MONO_NO_RELATION, evaluation_area->cfg->num_varinfo);
						for (phi = 0; phi < *(value->value.phi_variables); phi++){
							gssize related_variable = value->value.phi_variables [phi+1];
							evaluate_variable_relations(related_variable, evaluation_area, &context);
							COMBINE_VARIABLE_RELATIONS(RELATION_UNION, phi_union,
								&(evaluation_area->variable_relations [related_variable]), evaluation_area->cfg->num_varinfo);
						}
						if (TRACE_ABC_REMOVAL) {
							printf("Resulting phi_union is:\n");
							print_variable_relations(phi_union, variable, evaluation_area->cfg->num_varinfo);
						}
						COMBINE_VARIABLE_RELATIONS(RELATION_INTERSECTION, relations,
							phi_union, evaluation_area->cfg->num_varinfo);
					}
					/* Other cases should not happen... */
					break;
				}
				default: {
					g_assert_not_reached();
				}
			}
			break;
		}
		case MONO_RELATIONS_EVALUATION_IN_PROGRESS: {
			MonoVariableRelations *recursive_value_relations = NULL;
			unsigned char recursive_relation = MONO_ANY_RELATION;
			MonoRelationsEvaluationContext *current_context = context.father_context;

			if (TRACE_ABC_REMOVAL) {
				printf("Current step is MONO_RELATIONS_EVALUATION_IN_PROGRESS\n");
			}
			relations->definition_is_recursive = 1;
			while (current_context != NULL && current_context->variable != variable){
				MonoVariableRelations *context_relations = &(evaluation_area->variable_relations [current_context->variable]);
				MonoSummarizedValue *context_value = &(evaluation_area->variable_definitions [current_context->variable]);
				if (TRACE_ABC_REMOVAL) {
					printf("Backtracing to context %d\n", current_context->variable);
				}
				if (recursive_value_relations == NULL){
					if (context_value->relation_with_value != MONO_EQ_RELATION){
						recursive_value_relations = context_relations;
						recursive_relation = context_value->relation_with_value;
						if (TRACE_ABC_REMOVAL) {
							printf("Accepted recursive definition, relation is ");
							print_relation(recursive_relation);
							printf("\n");
						}
					}
				} else {
					if (context_value->relation_with_value != MONO_EQ_RELATION){
						recursive_relation = MONO_NO_RELATION;
						if (TRACE_ABC_REMOVAL) {
							printf("Rejected recursive definition, bad relation is ");
							print_relation(context_value->relation_with_value);
							printf("\n");
						}
					}
				}
				current_context = current_context->father_context;
			}
			if (recursive_value_relations != NULL && recursive_relation != MONO_NO_RELATION){
				int i;
				/* This should handle "grows" and "decreases" cases */
				recursive_value_relations->relation_with_zero &= recursive_relation;
				for (i = 0; i < evaluation_area->cfg->num_varinfo; i++){
					recursive_value_relations->relations_with_variables [i] &= recursive_relation;
				}
			}
			return;
		}
		case MONO_RELATIONS_EVALUATION_COMPLETED: {
			if (TRACE_ABC_REMOVAL) {
				printf("Current step is MONO_RELATIONS_EVALUATION_COMPLETED\n");
			}
			return;
		}
		default: {
			g_assert_not_reached();
		}
	}
	
	relations->evaluation_step = MONO_RELATIONS_EVALUATION_COMPLETED;
}

static int propagate_relations(MonoVariableRelationsEvaluationArea *evaluation_area){
	int changes = 0;
	gssize variable;
	for (variable = 0; variable < evaluation_area->cfg->num_varinfo; variable++){
		MonoVariableRelations *relations = &(evaluation_area->variable_relations [variable]);
		gssize related_variable;
		for (related_variable = 0; related_variable < evaluation_area->cfg->num_varinfo; related_variable++){
			unsigned char relation_with_variable = relations->relations_with_variables [related_variable];
			if (relation_with_variable != MONO_ANY_RELATION){
				MonoVariableRelations *related_relations = &(evaluation_area->variable_relations [related_variable]);
				gssize variable_related_to_related_variable;
				unsigned char new_relation_with_zero = PROPAGATED_RELATION(relation_with_variable, related_relations->relation_with_zero);
				if (RELATION_ADDS_INFORMATION(relations->relation_with_zero, new_relation_with_zero)) {
					if (TRACE_ABC_REMOVAL) {
						printf("RELATION_ADDS_INFORMATION variable %d, related_variable %d, relation_with_zero ",
							variable, related_variable);
						print_relation(relations->relation_with_zero);
						printf(" - ");
						print_relation(new_relation_with_zero);
						printf(" => ");
					}
					relations->relation_with_zero &= new_relation_with_zero;
					if (TRACE_ABC_REMOVAL) {
						print_relation(relations->relation_with_zero);
						printf("\n");
					}
					changes++;
				}
				for (variable_related_to_related_variable = 0; variable_related_to_related_variable < evaluation_area->cfg->num_varinfo; variable_related_to_related_variable++){
					unsigned char relation_of_variable = related_relations->relations_with_variables [variable_related_to_related_variable];
					unsigned char relation_with_other_variable = relations->relations_with_variables [variable_related_to_related_variable];
					unsigned char new_relation_with_other_variable = PROPAGATED_RELATION(relation_with_variable, relation_of_variable);
					if (RELATION_ADDS_INFORMATION(relation_with_other_variable, new_relation_with_other_variable)) {
						if (TRACE_ABC_REMOVAL) {
							printf("RELATION_ADDS_INFORMATION variable %d, related_variable %d, variable_related_to_related_variable %d, ",
								variable, related_variable, variable_related_to_related_variable);
							print_relation(relation_with_variable);
							printf(" - ");
							print_relation(new_relation_with_other_variable);
							printf(" => ");
						}
						relations->relations_with_variables [variable_related_to_related_variable] &= new_relation_with_other_variable;
						if (TRACE_ABC_REMOVAL) {
							print_relation(relations->relations_with_variables [variable_related_to_related_variable]);
							printf("\n");
						}
						changes++;
					}
				}
			}
		}
	}
	return changes;
}

static void remove_abc_from_inst(MonoInst *inst, MonoVariableRelationsEvaluationArea *evaluation_area){
	if (inst->opcode == CEE_LDELEMA){
		MonoInst *array_inst = inst->inst_left;
		MonoInst *index_inst = inst->inst_right;
		/* Both the array and the index must be local variables */
		if ((array_inst->opcode == CEE_LDIND_REF) &&
				((array_inst->inst_left->opcode == OP_LOCAL)||(array_inst->inst_left->opcode == OP_ARG)) &&
				((index_inst->opcode == CEE_LDIND_I1) ||
				(index_inst->opcode == CEE_LDIND_U1) ||
				(index_inst->opcode == CEE_LDIND_I2) ||
				(index_inst->opcode == CEE_LDIND_U2) ||
				(index_inst->opcode == CEE_LDIND_I4) ||
				(index_inst->opcode == CEE_LDIND_U4))&&
				((index_inst->inst_left->opcode == OP_LOCAL)||(index_inst->inst_left->opcode == OP_ARG))){
			gssize array_variable = array_inst->inst_left->inst_c0;
			gssize index_variable = index_inst->inst_left->inst_c0;
			MonoVariableRelations *index_relations = &(evaluation_area->variable_relations [index_variable]);
			if ( (! (index_relations->relation_with_zero & ~MONO_GE_RELATION)) && 
					(! (index_relations->relations_with_variables [array_variable] & ~MONO_LT_RELATION)) ) {
				inst->flags |= (MONO_INST_NORANGECHECK);
			}
			if (TRACE_ABC_REMOVAL) {
				if (! (index_relations->relation_with_zero & ~MONO_GE_RELATION)){
					printf ("ARRAY-ACCESS: Removed lower bound check on array %d with index %d\n", array_variable, index_variable);
				}
				else {
					printf ("ARRAY-ACCESS: Left lower bound check on array %d with index %d\n", array_variable, index_variable);
				}
				if (! (index_relations->relations_with_variables [array_variable] & ~MONO_LT_RELATION)){
					printf ("ARRAY-ACCESS: Removed upper bound check on array %d with index %d\n", array_variable, index_variable);
				}
				else {
					printf ("ARRAY-ACCESS: Left upper bound check on array %d with index %d\n", array_variable, index_variable);
				}
			}
			if (REPORT_ABC_REMOVAL) {
				if (inst->flags & (MONO_INST_NORANGECHECK)){
					printf ("ARRAY-ACCESS: removed bounds check on array %d with index %d in method %s\n",
						array_variable, index_variable, mono_method_full_name(evaluation_area->cfg->method, TRUE));
				}
			}
		}
	}
	
	if (mono_burg_arity [inst->opcode]){
		remove_abc_from_inst(inst->inst_left, evaluation_area);
		if (mono_burg_arity [inst->opcode] > 1){
			remove_abc_from_inst(inst->inst_right, evaluation_area);
		}
	}
}

static void remove_abc_from_block(MonoSummarizedBasicBlock *b, MonoVariableRelationsEvaluationArea *evaluation_area){
	int i;
	int changes;
	MonoBasicBlock *bb;
	MonoInst *current_inst = b->block->code;
	
	if (TRACE_ABC_REMOVAL) {
		printf("Working on block %d [dfn %d], has_array_access_instructions is %d\n",
			b->block->block_num, b->block->dfn, b->has_array_access_instructions);
	}
	
	if (b->has_array_access_instructions){
		for (i = 0; i < evaluation_area->cfg->num_varinfo; i++){
			evaluation_area->variable_relations [i].evaluation_step = MONO_RELATIONS_EVALUATION_NOT_STARTED;
			evaluation_area->variable_relations [i].definition_is_recursive = 0;
			SET_VARIABLE_RELATIONS(&(evaluation_area->variable_relations [i]), MONO_ANY_RELATION, evaluation_area->cfg->num_varinfo);
		}
		
		bb = b->block;
		while (bb != NULL){
			/* Walk up dominators tree to put conditions in area */
			int in_index = 0;
			/* for (in_index = 0; in_index < bb->in_count; in_index++){ */
			if (bb->in_count == 1){ /* Should write the code to "sum" conditions... */
				int out_index;
				MonoBasicBlock *in_bb = bb->in_bb [in_index];
				MonoSummarizedBasicBlock *in_b = &(evaluation_area->blocks [in_bb->dfn]);
				for (out_index = 0; out_index < in_b->number_of_branches; out_index++){
					if (in_b->branches [out_index].destination_block == bb){
						MonoBranchData *branch;
						int condition_index;

						if (TRACE_ABC_REMOVAL) {
							printf("Applying conditions of branch %d -> %d\n", in_b->block->block_num, bb->block_num);
						}
						branch = &(in_b->branches [out_index]);
						for (condition_index = 0; condition_index < branch->number_of_conditions; condition_index++) {
							MonoBranchCondition *condition = &(branch->conditions [condition_index]);
							MonoVariableRelations *relations = &(evaluation_area->variable_relations [condition->variable]);
							switch (condition->value.value_type){
								case MONO_CONSTANT_SUMMARIZED_VALUE: {
									if (condition->value.relation_with_value == MONO_EQ_RELATION){
										relations->relation_with_zero &= RELATION_BETWEEN_VALUES(condition->value.value.constant, 0);
										relations->relation_with_one &= RELATION_BETWEEN_VALUES(abs(condition->value.value.constant), 1);
										if (TRACE_ABC_REMOVAL) {
											printf("Applied equality condition with constant to variable %d; relatrion with 0: ", condition->variable);
											print_relation(relations->relation_with_zero);
											printf("\n");
										}
									} else if (condition->value.value.constant == 0){
										relations->relation_with_zero &= condition->value.relation_with_value;
										if (TRACE_ABC_REMOVAL) {
											printf("Applied relation with 0 to variable %d: ", condition->variable);
											print_relation(relations->relation_with_zero);
											printf("\n");
										}
									}
									/* Other cases should be handled */
									break;
								}
								case MONO_VARIABLE_SUMMARIZED_VALUE: {
									relations->relations_with_variables [condition->value.value.variable] &= condition->value.relation_with_value;
									if (TRACE_ABC_REMOVAL) {
										printf("Applied relation between variables %d and %d: ", condition->variable, condition->value.value.variable);
										print_relation(relations->relations_with_variables [condition->value.value.variable]);
										printf("\n");
									}
									break;
								}
								default:
									g_assert_not_reached(); /* PHIs are not OK here */
							}
						}
					}
				}
			}
			bb = bb->idom;
		}
		
		if (TRACE_ABC_REMOVAL) {
			printf("Branch conditions applied... ");
			print_all_variable_relations(evaluation_area);
		}
		
		for (i = 0; i < evaluation_area->cfg->num_varinfo; i++){
			evaluate_variable_relations(i, evaluation_area, NULL);
		}
		
		if (TRACE_ABC_REMOVAL) {
			printf("Variable definitions applied... ");
			print_all_variable_relations(evaluation_area);
		}
		
		i = 0;
		do {
			changes = propagate_relations(evaluation_area);
			i++;
			if (TRACE_ABC_REMOVAL) {
				printf("Propagated %d changes\n", changes);
			}
		} while ((changes > 0) && (i < evaluation_area->cfg->num_varinfo));
		
		if (TRACE_ABC_REMOVAL) {
			printf("Relations fully propagated... ");
			print_all_variable_relations(evaluation_area);
		}
		
		/* Area is ready, look for array access instructions */
		if (TRACE_ABC_REMOVAL) {
			printf("Going after array accesses...\n");
		}
		while (current_inst != NULL){
			remove_abc_from_inst(current_inst, evaluation_area);
			current_inst = current_inst->next;
		}
	}
}

void
mono_perform_abc_removal (MonoCompile *cfg)
{
	MonoVariableRelationsEvaluationArea evaluation_area;
	int i;
	verbose_level = cfg->verbose_level;
	
	
	if (TRACE_ABC_REMOVAL) {
		printf("Removing array bound checks in %s\n", mono_method_full_name (cfg->method, TRUE));
	}
	
	if (cfg->num_varinfo > 250) {
		if (TRACE_ABC_REMOVAL) {
			printf("Too many variables (%d), giving up...\n", cfg->num_varinfo);
		}
		return;
	}
	
	evaluation_area.pool = mono_mempool_new();
	evaluation_area.cfg = cfg;
	evaluation_area.variable_relations = (MonoVariableRelations *)
		mono_mempool_alloc(evaluation_area.pool, sizeof(MonoVariableRelations) * (cfg->num_varinfo));
	//printf("Allocated %d bytes for %d variable relations at pointer %p\n",
	//	sizeof(MonoVariableRelations) * (cfg->num_varinfo), (cfg->num_varinfo), evaluation_area.variable_relations);
	for (i = 0; i < cfg->num_varinfo; i++){
		evaluation_area.variable_relations [i].relations_with_variables = (unsigned char *)
			mono_mempool_alloc(evaluation_area.pool, (cfg->num_varinfo));
		//printf("Allocated %d bytes [%d] at pointer %p\n",
		//	cfg->num_varinfo, i, evaluation_area.variable_relations [i].relations_with_variables);
	}
	evaluation_area.variable_definitions = (MonoSummarizedValue *)
		mono_mempool_alloc(evaluation_area.pool, sizeof(MonoSummarizedValue) * (cfg->num_varinfo));
	//printf("Allocated %d bytes for %d variable definitions at pointer %p\n",
	//	sizeof(MonoSummarizedValue) * (cfg->num_varinfo), (cfg->num_varinfo), evaluation_area.variable_definitions);
	if (TRACE_ABC_REMOVAL) {
		printf("Method contains %d variables\n", i);
	}
	for (i = 0; i < cfg->num_varinfo; i++){
		if (cfg->vars [i]->def != NULL)
		{
			MonoInst *value = get_variable_value_from_ssa_store(cfg->vars [i]->def, i);
			if (value != NULL)
			{
				summarize_value(value, evaluation_area.variable_definitions + i);
				if (TRACE_ABC_REMOVAL) {
					printf("Summarized variable %d\n", i);
					print_summarized_value(evaluation_area.variable_definitions + i);
				}
			}
			else
			{
				MAKE_VALUE_ANY(evaluation_area.variable_definitions + i);
				if (TRACE_ABC_REMOVAL) {
					printf("Definition of variable %d is not a proper store\n", i);
				}
			}
		}
		else
		{
			MAKE_VALUE_ANY(evaluation_area.variable_definitions + i);
			if (TRACE_ABC_REMOVAL) {
				printf("Variable %d has no definition, probably it is an argument\n", i);
				print_summarized_value(evaluation_area.variable_definitions + i);
			}
		}
	}
	
	evaluation_area.blocks = (MonoSummarizedBasicBlock *)
		mono_mempool_alloc(evaluation_area.pool, sizeof(MonoSummarizedBasicBlock) * (cfg->num_bblocks));
	//printf("Allocated %d bytes for %d blocks at pointer %p\n",
	//	sizeof(MonoSummarizedBasicBlock) * (cfg->num_bblocks), (cfg->num_bblocks), evaluation_area.blocks);
	
	for (i = 0; i < cfg->num_bblocks; i++){
		analyze_block(cfg->bblocks [i], &evaluation_area);
	}
	
	for (i = 0; i < cfg->num_bblocks; i++){
		remove_abc_from_block(&(evaluation_area.blocks [i]), &evaluation_area);
	}
	
	mono_mempool_destroy(evaluation_area.pool);
}

#else
static void remove_abc(MonoInst *inst){
	if (inst->opcode == CEE_LDELEMA){
		if (TRACE_ABC_REMOVAL||REPORT_ABC_REMOVAL) {
			printf("Performing removal...\n");
		}
		inst->flags |= (MONO_INST_NORANGECHECK);
	}
	
	if (mono_burg_arity [inst->opcode]){
		remove_abc(inst->inst_left);
		if (mono_burg_arity [inst->opcode] > 1){
			remove_abc(inst->inst_right);
		}
	}
}

void
mono_perform_abc_removal (MonoCompile *cfg) {
	verbose_level = cfg->verbose_level;
	
	int i;
	#if (TRACE_ABC_REMOVAL)
	printf("Removing array bound checks in %s\n", mono_method_full_name (cfg->method, TRUE));
	#endif
	
	for (i = 0; i < cfg->num_bblocks; i++){
		#if (TRACE_ABC_REMOVAL)
		printf("  Working on block %d [dfn %d]\n", cfg->bblocks [i]->block_num, i);
		#endif
		
		MonoBasicBlock *bb = cfg->bblocks [i];
		MonoInst *inst = bb->code;
		while (inst != NULL){
			remove_abc(inst);
			inst = inst->next;
		}
	}
}
#endif
