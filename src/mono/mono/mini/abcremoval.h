
#include "mini.h"


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


typedef enum {
	MONO_CONSTANT_SUMMARIZED_VALUE = 0,
	MONO_VARIABLE_SUMMARIZED_VALUE,
	MONO_PHI_SUMMARIZED_VALUE
} MonoSummarizedValueType;


typedef struct MonoSummarizedValue {
	unsigned char relation_with_zero; /* MonoValueRelation */
	unsigned char relation_with_one; /* MonoValueRelation */
	unsigned char relation_with_value; /* MonoValueRelation */
	unsigned char value_type; /* MonoSummarizedValueType */
	union {
		int constant;
		gssize variable;
		int *phi_variables;
	} value;
} MonoSummarizedValue;


typedef struct MonoBranchCondition {
	gssize variable;
	MonoSummarizedValue value;
} MonoBranchCondition;

typedef struct MonoBranchData {
	MonoBasicBlock *destination_block;
	int number_of_conditions;
	MonoBranchCondition *conditions;
} MonoBranchData;

typedef struct MonoSummarizedBasicBlock {
	MonoBasicBlock *block;
	unsigned char has_array_access_instructions;
	int number_of_branches;
	MonoBranchData* branches;
} MonoSummarizedBasicBlock;

typedef enum {
	MONO_RELATIONS_EVALUATION_NOT_STARTED,
	MONO_RELATIONS_EVALUATION_IN_PROGRESS,
	MONO_RELATIONS_EVALUATION_COMPLETED
} MonoRelationsEvaluationStep;

typedef struct MonoVariableRelations {
	unsigned char relation_with_zero; /* MonoValueRelation */
	unsigned char relation_with_one; /* MonoValueRelation */
	unsigned char evaluation_step; /* MonoRelationsEvaluationStep */
	unsigned char definition_is_recursive;
	unsigned char *relations_with_variables; /* many MonoValueRelation */
} MonoVariableRelations;

typedef struct MonoVariableRelationsEvaluationArea {
	MonoCompile *cfg;
	MonoMemPool *pool;
	MonoVariableRelations *variable_relations;
	MonoSummarizedValue *variable_definitions;
	MonoSummarizedBasicBlock *blocks;
} MonoVariableRelationsEvaluationArea;

typedef struct MonoRelationsEvaluationContext {
	struct MonoRelationsEvaluationContext *father_context;
	gssize variable;
} MonoRelationsEvaluationContext;

extern void
mono_perform_abc_removal (MonoCompile *cfg);
