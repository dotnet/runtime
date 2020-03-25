#ifndef __MONO_MINI_INTERP_TRANSFORM_H__
#define __MONO_MINI_INTERP_TRANSFORM_H__
#include <mono/mini/mini-runtime.h>
#include <mono/metadata/seq-points-data.h>
#include "interp-internals.h"

#define INTERP_INST_FLAG_SEQ_POINT_NONEMPTY_STACK 1
#define INTERP_INST_FLAG_SEQ_POINT_METHOD_ENTRY 2
#define INTERP_INST_FLAG_SEQ_POINT_METHOD_EXIT 4
#define INTERP_INST_FLAG_SEQ_POINT_NESTED_CALL 8
#define INTERP_INST_FLAG_RECORD_CALL_PATCH 16

#define INTERP_LOCAL_FLAG_DEAD 1

typedef struct InterpInst InterpInst;

typedef struct
{
	MonoClass *klass;
	unsigned char type;
	unsigned char flags;
} StackInfo;

#define STACK_VALUE_NONE 0
#define STACK_VALUE_LOCAL 1
#define STACK_VALUE_ARG 2
#define STACK_VALUE_I4 3
#define STACK_VALUE_I8 4

// StackValue contains data to construct an InterpInst that is equivalent with the contents
// of the stack slot / local / argument.
typedef struct {
	// Indicates the type of the stored information. It can be a local, argument or a constant
	int type;
	// Holds the local index or the actual constant value
	union {
		int local;
		int arg;
		gint32 i;
		gint64 l;
	};
} StackValue;

typedef struct
{
	// This indicates what is currently stored in this stack slot. This can be a constant
	// or the copy of a local / argument.
	StackValue val;
	// The instruction that pushed this stack slot. If ins is null, we can't remove the usage
	// of the stack slot, because we can't clear the instruction that set it.
	InterpInst *ins;
} StackContentInfo;

struct InterpInst {
	guint16 opcode;
	InterpInst *next, *prev;
	// If this is -1, this instruction is not logically associated with an IL offset, it is
	// part of the IL instruction associated with the previous interp instruction.
	int il_offset;
	guint32 flags;
	guint16 data [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	guint8 *ip;
	GSList *preds;
	GSList *seq_points;
	SeqPoint *last_seq_point;

	// This will hold a list of last sequence points of incoming basic blocks
	SeqPoint **pred_seq_points;
	guint num_pred_seq_points;
} InterpBasicBlock;

typedef enum {
	RELOC_SHORT_BRANCH,
	RELOC_LONG_BRANCH,
	RELOC_SWITCH
} RelocType;

typedef struct {
	RelocType type;
	/* In the interpreter IR */
	int offset;
	/* In the IL code */
	int target;
} Reloc;

typedef struct {
	MonoType *type;
	int mt;
	int flags;
	int indirects;
	int offset;
} InterpLocal;

typedef struct
{
	MonoMethod *method;
	MonoMethod *inlined_method;
	MonoMethodHeader *header;
	InterpMethod *rtm;
	const unsigned char *il_code;
	const unsigned char *ip;
	const unsigned char *in_start;
	InterpInst *last_ins, *first_ins;
	int code_size;
	int *in_offsets;
	int current_il_offset;
	StackInfo **stack_state;
	int *stack_height;
	int *vt_stack_size;
	unsigned char *is_bb_start;
	unsigned short *new_code;
	unsigned short *new_code_end;
	unsigned int max_code_size;
	StackInfo *stack;
	StackInfo *sp;
	unsigned int max_stack_height;
	unsigned int stack_capacity;
	unsigned int vt_sp;
	unsigned int max_vt_sp;
	unsigned int total_locals_size;
	InterpLocal *locals;
	unsigned int locals_size;
	unsigned int locals_capacity;
	int n_data_items;
	int max_data_items;
	void **data_items;
	GHashTable *data_hash;
#ifdef ENABLE_EXPERIMENT_TIERED
	GHashTable *patchsite_hash;
#endif
	int *clause_indexes;
	gboolean gen_sdb_seq_points;
	GPtrArray *seq_points;
	InterpBasicBlock **offset_to_bb;
	InterpBasicBlock *entry_bb;
	MonoMemPool     *mempool;
	GList *basic_blocks;
	GPtrArray *relocs;
	gboolean verbose_level;
	GArray *line_numbers;
	gboolean prof_coverage;
	MonoProfilerCoverageInfo *coverage_info;
} TransformData;

#define STACK_TYPE_I4 0
#define STACK_TYPE_I8 1
#define STACK_TYPE_R4 2
#define STACK_TYPE_R8 3
#define STACK_TYPE_O  4
#define STACK_TYPE_VT 5
#define STACK_TYPE_MP 6
#define STACK_TYPE_F  7

#if SIZEOF_VOID_P == 8
#define STACK_TYPE_I STACK_TYPE_I8
#else
#define STACK_TYPE_I STACK_TYPE_I4
#endif

/* test exports for white box testing */
void
mono_test_interp_cprop (TransformData *td);
gboolean
mono_test_interp_generate_code (TransformData *td, MonoMethod *method, MonoMethodHeader *header, MonoGenericContext *generic_context, MonoError *error);
void
mono_test_interp_method_compute_offsets (TransformData *td, InterpMethod *imethod, MonoMethodSignature *signature, MonoMethodHeader *header);

/* debugging aid */
void
mono_interp_print_td_code (TransformData *td);

#endif /* __MONO_MINI_INTERP_TRANSFORM_H__ */
