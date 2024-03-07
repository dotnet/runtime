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
#define INTERP_INST_FLAG_CALL 32
// Flag used internally by the var offset allocator
#define INTERP_INST_FLAG_ACTIVE_CALL 64
// This instruction is protected by a clause
#define INTERP_INST_FLAG_PROTECTED_NEWOBJ 128
// This instruction bumps the liveness index. Enables liveness checks as new instructions
// are added in the code, since new instructions won't have this flag set.
#define INTERP_INST_FLAG_LIVENESS_MARKER 256

typedef struct _InterpInst InterpInst;
typedef struct _InterpBasicBlock InterpBasicBlock;
typedef struct _InterpCallInfo InterpCallInfo;

typedef struct
{
	MonoClass *klass;
	unsigned char type;
	unsigned char flags;
	/*
	 * The var associated with the value of this stack entry. Every time we push on
	 * the stack a new var is created.
	 */
	int var;
	/* The offset from the execution stack start where this is stored. Used by the fast offset allocator */
	int offset;
	/* Saves how much stack this is using. It is a multiple of MINT_STACK_SLOT_SIZE*/
	int size;
} StackInfo;

#define VAR_VALUE_NONE 0
#define VAR_VALUE_OTHER_VAR 1
#define VAR_VALUE_I4 2
#define VAR_VALUE_I8 3
#define VAR_VALUE_R4 4
#define VAR_VALUE_NON_NULL 5
#define VAR_VALUE_COUNT 6

typedef struct {
	guint32 bb_dfs_index;
	guint32 ins_index;
} InterpLivenessPosition;

typedef struct {
	// Indicates the type of the stored information. It can be another var or a constant
	int type;
	// Holds the local index or the actual constant value
	union {
		int var;
		gint32 i;
		gint64 l;
		float f;
	};
	// The instruction that writes this local.
	InterpInst *def;
	// Liveness marker of the definition
	InterpLivenessPosition liveness;
	// The number of times this var is referenced. After optimizations
	// this can become 0, in which case we can clear the def instruction.
	int ref_count;
} InterpVarValue;

struct _InterpInst {
	guint16 opcode;
	InterpInst *next, *prev;
	// If this is -1, this instruction is not logically associated with an IL offset, it is
	// part of the IL instruction associated with the previous interp instruction.
	int il_offset;
	guint32 flags;
	gint32 dreg;
	gint32 sregs [3]; // Currently all instructions have at most 3 sregs
	// This union serves the same purpose as the data array. The difference is that
	// the data array maps exactly to the final representation of the instruction.
	// FIXME We should consider using a separate higher level IR, that is also easier
	// to use for various optimizations.
	union {
		InterpBasicBlock *target_bb;
		InterpBasicBlock **target_bb_table;
		InterpCallInfo *call_info;
		int *args; // for variable number of args, used only for phi
		MonoBitSet *dead_phi_vars; // only for MINT_DEAD_PHI
	} info;
	// Variable data immediately following the dreg/sreg information. This is represented exactly
	// in the final code stream as in this array.
	guint16 data [MONO_ZERO_LEN_ARRAY];
};

struct _InterpBasicBlock {
	int il_offset;
	GSList *seq_points;
	SeqPoint *last_seq_point;

	InterpInst *first_ins, *last_ins;
	/* Next bb in IL order */
	InterpBasicBlock *next_bb;

	gint16 in_count;
	InterpBasicBlock **in_bb;
	gint16 out_count;
	InterpBasicBlock **out_bb;

	/* Index into td->bblocks */
	int dfs_index;

	/* Dominance frontier for this bblock */
	MonoBitSet *dfrontier;

	/* List of bblocks that are immediately dominated by this bblock */
	GSList *dominated;

	/* Live variable analysis, for vars in locals_ext */
	MonoBitSet *gen_set;
	MonoBitSet *kill_set;
	MonoBitSet *live_in_set;
	MonoBitSet *live_out_set;

	/* The real native offset of this bblock, computed when emitting the instructions in the code stream */
	int native_offset;
	/*
	 * Estimated native offset computed before the final code stream is generated. These offsets are used
	 * to determine whether we will use a long or short branch when branching to this bblock. Native offset
	 * estimates must respect the following condition: |bb1->n_o_e - bb2->n_o_e| >= |bb1->n_o - bb2->n_o|.
	 * The real native offset between two instructions is always smaller or equal to the estimate, allowing
	 * us to safely insert short branches based on the estimated offset.
	 */
	int native_offset_estimate;

	/*
	 * The state of the stack when entering this basic block. By default, the stack height is
	 * -1, which means it inherits the stack state from the previous instruction, in IL order
	 */
	int stack_height;
	StackInfo *stack_state;

	int index;
	int jump_targets;

	InterpBasicBlock *try_bblock;

	// This will hold a list of last sequence points of incoming basic blocks
	SeqPoint **pred_seq_points;
	guint num_pred_seq_points;

	guint reachable : 1;
	// This block has special semantics and it shouldn't be optimized away
	guint preserve : 1;
	guint dead: 1;
	// This bblock is detectead early as being dead, we don't inline into it
	guint no_inlining: 1;
	// If patchpoint is set we will store mapping information between native offset and bblock index within
	// InterpMethod. In the unoptimized method we will map from native offset to the bb_index while in the
	// optimized method we will map the bb_index to the corresponding native offset.
	guint patchpoint_data: 1;
	guint patchpoint_bb: 1;
	// used by jiterpreter
	guint backwards_branch_target: 1;
	guint contains_call_instruction: 1;
};

struct _InterpCallInfo {
	// For call instructions, this represents an array of all call arg vars
	// in the order they are pushed to the stack. This makes it easy to find
	// all source vars for these types of opcodes. This is terminated with -1.
	int *call_args;
	int call_offset;
	union {
		// Array of call dependencies that need to be resolved before
		GSList *call_deps;
		// Stack end offset of call arguments
		int call_end_offset;
	};
};

typedef enum {
	RELOC_SHORT_BRANCH,
	RELOC_LONG_BRANCH,
	RELOC_SWITCH
} RelocType;

typedef struct {
	RelocType type;
	/* For branch relocation, how many sreg slots to skip */
	int skip;
	/* In the interpreter IR */
	int offset;
	InterpBasicBlock *target_bb;
} Reloc;

typedef struct {
	MonoType *type;
	int mt;
	int indirects;
	int offset;
	int size;
	int ext_index;
	GSList *declare_bbs;
	union {
		// live_start and live_end are used by the offset allocator for optimized code
		int live_start;
		// used only by the fast offset allocator, which only works for unoptimized code
		int stack_offset;
	};
	int live_end;
	// index of first basic block where this var is used
	int bb_index;
	union {
		// If var is INTERP_LOCAL_FLAG_CALL_ARGS, this is the call instruction using it.
		// Only used during var offset allocator
		InterpInst *call;
	};

	guint execution_stack : 1;
	guint call_args : 1;
	guint global : 1;
	guint no_call_args : 1;
	guint simd : 1; // We use this flag to avoid addition of align field in InterpVar, for now
	guint eh_var : 1; // This var is used inside a clause handler. It will not be in ssa form.
	guint no_ssa : 1; // Var is not in ssa form, not subject to all optimizations
	guint has_indirects : 1; // Var had ldloca applied to it, not subject to optimizations
	guint il_global : 1; // Args and IL locals
	guint renamed_ssa_fixed : 1; // If true, ext_index points to InterpRenamedVar, otherwise to InterpRenamableVar
	guint def_arg : 1; // Var is a result of MINT_DEF_ARG. This var will have to be renamed back to the original arg var
} InterpVar;

typedef struct {
	int var_index;
	GSList *ssa_stack;
	// This liveness is bblock only. It is used during cprop to determine whether we
	// can move the definition of a renamed fixed var earlier (if there are no conflicts with
	// other renamed vars from the same var)
	InterpLivenessPosition last_use_liveness;

	// Var that is global and might take part in phi opcodes
	guint ssa_global : 1;
	// IL locals/args. Vars included in phi opcodes. All renamed vars are allocated
	// to the same offset. Optimizations need to ensure there is no overlapping liveness
	guint ssa_fixed : 1;
} InterpRenamableVar;

// In addition to InterpRenamableVar information, this stores liveness information that enables us
// to ensure that the liveness of the corresponding var is not overlapping with the other renamed vars,
// after optimization.
typedef struct {
	int var_index;
	int renamable_var_ext_index;
	// Bit set of bblocks where the renamed var is live at the bb end
	// This means that within these bblocks we can freely increase the var liveness
	MonoBitSet *live_out_bblocks;
	// This is a list of InterpLivenessPosition*, that indicates that in bblock with
	// index bb_index, the var can have its liveness extended to at most inst_index
	GSList *live_limit_bblocks;
} InterpRenamedFixedVar;

typedef struct
{
	MonoMethod *method;
	MonoMethod *inlined_method;
	MonoMethodHeader *header;
	InterpMethod *rtm;
	const unsigned char *il_code;
	const unsigned char *ip;
	const unsigned char *in_start;
	InterpInst *last_ins;
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
	gint32 param_area_offset;
	gint32 total_locals_size;
	gint32 max_stack_size;
	int dummy_var;
	int *local_ref_count;
	unsigned int il_locals_offset;
	unsigned int il_locals_size;

	// All vars, used in instructions
	InterpVar *vars;
	unsigned int vars_size;
	unsigned int vars_capacity;

	// Additional information for vars that are renamable
	InterpRenamableVar *renamable_vars;
	unsigned int renamable_vars_size;
	unsigned int renamable_vars_capacity;

	// Newly created, renamed vars of fixed vars. We compute liveness on this subset
	// of vars so we ensure we don't have conflicting liveness.
	unsigned int renamed_fixed_vars_size;
	unsigned int renamed_fixed_vars_capacity;
	InterpRenamedFixedVar *renamed_fixed_vars;

	InterpVarValue *var_values;

	int n_data_items;
	int max_data_items;
	void **data_items;
	GHashTable *data_hash;
	GSList *imethod_items;
#ifdef ENABLE_EXPERIMENT_TIERED
	GHashTable *patchsite_hash;
#endif
	int *clause_indexes;
	int *clause_vars;
	gboolean gen_seq_points;
	gboolean gen_sdb_seq_points;
	GPtrArray *seq_points;
	InterpBasicBlock **offset_to_bb;
	InterpBasicBlock *entry_bb, *cbb;
	InterpBasicBlock **bblocks; // ordering of bblocks in reverse postorder dfs
	int bblocks_count_no_eh;
	int bblocks_count_eh;
	InterpBasicBlock **idoms; // immediate dominator for each bblock, index from reverse postorder dfs
	int bb_count;
	MonoMemPool     *mempool;
	MonoMemPool     *opt_mempool;
	MonoMemoryManager *mem_manager;
	GList *basic_blocks;
	GPtrArray *relocs;
	gboolean verbose_level;
	GArray *line_numbers;
	gboolean prof_coverage;
	MonoProfilerCoverageInfo *coverage_info;
	GList *dont_inline;
	int inline_depth;
	int patchpoint_data_n;
	int *patchpoint_data;
	guint has_localloc : 1;
	// If method compilation fails due to certain limits being exceeded, we disable inlining
	// and retry compilation.
	guint disable_inlining : 1;
	// If the current method (inlined_method) has the aggressive inlining attribute, we no longer
	// bail out of inlining when having to generate certain opcodes (like call, throw).
	guint aggressive_inlining : 1;
	guint optimized : 1;
	guint has_invalid_code : 1;
	guint has_inlined_one_call : 1;
	guint need_optimization_retry : 1;
	guint disable_ssa : 1;
	guint eh_vars_computed : 1;
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

#define interp_ins_set_dummy_dreg(ins,td) do { \
	if (td->dummy_var < 0) \
		interp_create_dummy_var (td); \
	ins->dreg = td->dummy_var; \
} while (0)

#define interp_ins_set_dreg(ins,dr) do { \
        ins->dreg = dr; \
} while (0)

#define interp_ins_set_sreg(ins,s1) do { \
        ins->sregs [0] = s1; \
} while (0)

#define interp_ins_set_sregs2(ins,s1,s2) do { \
        ins->sregs [0] = s1; \
        ins->sregs [1] = s2; \
} while (0)

#define interp_ins_set_sregs3(ins,s1,s2,s3) do { \
        ins->sregs [0] = s1; \
        ins->sregs [1] = s2; \
        ins->sregs [2] = s3; \
} while (0)

#if NO_UNALIGNED_ACCESS
#define WRITE32(ip, v) \
        do { \
                * (ip) = * (guint16 *)(v); \
                * ((ip) + 1) = * ((guint16 *)(v) + 1); \
                (ip) += 2; \
        } while (0)

#define WRITE32_INS(ins, index, v) \
        do { \
                (ins)->data [index] = * (guint16 *)(v); \
                (ins)->data [index + 1] = * ((guint16 *)(v) + 1); \
        } while (0)

#define WRITE64(ins, v) \
        do { \
                *((ins) + 0) = * ((guint16 *)(v) + 0); \
                *((ins) + 1) = * ((guint16 *)(v) + 1); \
                *((ins) + 2) = * ((guint16 *)(v) + 2); \
                *((ins) + 3) = * ((guint16 *)(v) + 3); \
        } while (0)

#define WRITE64_INS(ins, index, v) \
        do { \
                (ins)->data [index] = * (guint16 *)(v); \
                (ins)->data [index + 1] = * ((guint16 *)(v) + 1); \
                (ins)->data [index + 2] = * ((guint16 *)(v) + 2); \
                (ins)->data [index + 3] = * ((guint16 *)(v) + 3); \
        } while (0)
#else
#define WRITE32(ip, v) \
        do { \
                * (guint32 *)(ip) = * (guint32 *)(v); \
                (ip) += 2; \
        } while (0)
#define WRITE32_INS(ins, index, v) \
        do { \
                * (guint32 *)(&(ins)->data [index]) = * (guint32 *)(v); \
        } while (0)

#define WRITE64(ip, v) \
        do { \
                * (guint64 *)(ip) = * (guint64 *)(v); \
                (ip) += 4; \
        } while (0)
#define WRITE64_INS(ins, index, v) \
        do { \
                * (guint64 *)(&(ins)->data [index]) = * (guint64 *)(v); \
        } while (0)

#endif

/* test exports for white box testing */
void
mono_test_interp_cprop (TransformData *td);
gboolean
mono_test_interp_generate_code (TransformData *td, MonoMethod *method, MonoMethodHeader *header, MonoGenericContext *generic_context, MonoError *error);
void
mono_test_interp_method_compute_offsets (TransformData *td, InterpMethod *imethod, MonoMethodSignature *signature, MonoMethodHeader *header);

#if HOST_BROWSER
InterpInst*
mono_jiterp_insert_ins (TransformData *td, InterpInst *prev_ins, int opcode);
#endif

/* debugging aid */
void
mono_interp_print_td_code (TransformData *td);

/* Compilation internal methods */

InterpInst*
interp_new_ins (TransformData *td, int opcode, int len);

InterpInst*
interp_insert_ins_bb (TransformData *td, InterpBasicBlock *bb, InterpInst *prev_ins, int opcode);

InterpInst*
interp_insert_ins (TransformData *td, InterpInst *prev_ins, int opcode);

InterpInst*
interp_first_ins (InterpBasicBlock *bb);

InterpInst*
interp_next_ins (InterpInst *ins);

InterpInst*
interp_prev_ins (InterpInst *ins);

void
interp_clear_ins (InterpInst *ins);

gboolean
interp_ins_is_nop (InterpInst *ins);

int
interp_get_ins_length (InterpInst *ins);

void
interp_dump_ins (InterpInst *ins, gpointer *data_items);

InterpInst*
interp_get_ldc_i4_from_const (TransformData *td, InterpInst *ins, gint32 ct, int dreg);

gint32 
interp_get_const_from_ldc_i4 (InterpInst *ins);

int
interp_get_mov_for_type (int mt, gboolean needs_sext);

gboolean
interp_is_short_offset (int src_offset, int dest_offset);

InterpBasicBlock*
interp_alloc_bb (TransformData *td);

void
interp_link_bblocks (TransformData *td, InterpBasicBlock *from, InterpBasicBlock *to);

int
interp_compute_native_offset_estimates (TransformData *td);

void
interp_optimize_code (TransformData *td);

void
interp_alloc_offsets (TransformData *td);

int
interp_alloc_global_var_offset (TransformData *td, int var);

int
interp_create_var (TransformData *td, MonoType *type);

int
interp_make_var_renamable (TransformData *td, int var);

int
interp_create_renamed_fixed_var (TransformData *td, int var_index, int renamable_var_index);

void
interp_foreach_ins_var (TransformData *td, InterpInst *ins, gpointer data, void (*callback)(TransformData*, int*, gpointer));

void
interp_foreach_ins_svar (TransformData *td, InterpInst *ins, gpointer data, void (*callback)(TransformData*, int*, gpointer));


/* Forward definitions for simd methods */
static gboolean
interp_emit_simd_intrinsics (TransformData *td, MonoMethod *cmethod, MonoMethodSignature *csignature, gboolean newobj);

#endif /* __MONO_MINI_INTERP_TRANSFORM_H__ */
