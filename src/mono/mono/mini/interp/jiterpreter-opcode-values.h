#define VALUE_ABORT -1
#define VALUE_NONE 0
#define VALUE_CALL 0 // fixme: 1?
#define VALUE_RET 0 // fixme: 1?
#define VALUE_BRANCH 1
#define VALUE_NORMAL 2
#define VALUE_HEAVY 4
#define VALUE_SIMD 8

#define OP(OP, VAL) opcode_value_table[OP] = VALUE_ ## VAL;
#define OPRANGE(OP_MIN, OP_MAX, VAL) \
	for (int i = OP_MIN; i <= OP_MAX; i++) \
		opcode_value_table[i] = VALUE_ ## VAL;

OP()
