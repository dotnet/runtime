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

typedef struct _InterpInst InterpInst;
typedef struct _InterpBasicBlock InterpBasicBlock;

typedef struct
{
	MonoClass *klass;
	unsigned char type;
	unsigned char flags;
} StackInfo;

#define STACK_VALUE_NONE 0
#define STACK_VALUE_LOCAL 1
#define STACK_VALUE_I4 2
#define STACK_VALUE_I8 3

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

struct _InterpInst {
	guint16 opcode;
	InterpInst *next, *prev;
	// If this is -1, this instruction is not logically associated with an IL offset, it is
	// part of the IL instruction associated with the previous interp instruction.
	int il_offset;
	guint32 flags;
	// This union serves the same purpose as the data array. The difference is that
	// the data array maps exactly to the final representation of the instruction.
	// FIXME We should consider using a separate higher level IR, that is also easier
	// to use for various optimizations.
	union {
		InterpBasicBlock *target_bb;
		InterpBasicBlock **target_bb_table;
	} info;
	guint16 data [MONO_ZERO_LEN_ARRAY];
};

struct _InterpBasicBlock {
	guint8 *ip;
	GSList *seq_points;
	SeqPoint *last_seq_point;

	InterpInst *first_ins, *last_ins;
	/* Next bb in IL order */
	InterpBasicBlock *next_bb;

	gint16 in_count;
	InterpBasicBlock **in_bb;
	gint16 out_count;
	InterpBasicBlock **out_bb;

	int native_offset;

	/*
	 * The state of the stack when entering this basic block. By default, the stack height is
	 * -1, which means it inherits the stack state from the previous instruction, in IL order
	 */
	int stack_height;
	StackInfo *stack_state;
	int vt_stack_size;

	int index;

	// This will hold a list of last sequence points of incoming basic blocks
	SeqPoint **pred_seq_points;
	guint num_pred_seq_points;

	// This block has special semantics and it shouldn't be optimized away
	int eh_block : 1;
	int dead: 1;
};

typedef enum {
	RELOC_SHORT_BRANCH,
	RELOC_LONG_BRANCH,
	RELOC_SWITCH
} RelocType;

typedef struct {
	RelocType type;
	/* In the interpreter IR */
	int offset;
	InterpBasicBlock *target_bb;
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
	unsigned int il_locals_offset;
	unsigned int il_locals_size;
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
	InterpBasicBlock *entry_bb, *cbb;
	int bb_count;
	MonoMemPool     *mempool;
	MonoMemoryManager *mem_manager;
	GList *basic_blocks;
	GPtrArray *relocs;
	gboolean verbose_level;
	GArray *line_numbers;
	gboolean prof_coverage;
	MonoProfilerCoverageInfo *coverage_info;
	GList *dont_inline;
	int inline_depth;
	int has_localloc : 1;
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
