#ifndef __MONO_MINI_H__
#define __MONO_MINI_H__

#include "config.h"
#include <glib.h>
#include <signal.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/mempool.h>
#include <mono/utils/monobitset.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/domain-internals.h>
#include "mono/metadata/class-internals.h"
#include "mono/metadata/object-internals.h"
#include <mono/metadata/profiler-private.h>

#include "mini-arch.h"
#include "regalloc.h"
#include "declsec.h"

#define MONO_USE_AOT_COMPILER

/* for 32 bit systems */
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define MINI_LS_WORD_OFFSET 0
#define MINI_MS_WORD_OFFSET 4
#define inst_ls_word data.op[0].const_val
#define inst_ms_word data.op[1].const_val
#else
#define MINI_LS_WORD_OFFSET 4
#define MINI_MS_WORD_OFFSET 0
#define inst_ls_word data.op[1].const_val
#define inst_ms_word data.op[0].const_val
#endif

/* Version number of the AOT file format */
#define MONO_AOT_FILE_VERSION "11"

#if 1
#define mono_bitset_test_fast(set,n) (((guint32*)set)[2+(n)/32] & (1 << ((n) % 32)))
#else
#define mono_bitset_test_fast(set,n) mono_bitset_test(set,n)
#endif

#if 0
#define mono_bitset_foreach_bit(set,b,n) \
	for (b = 0; b < n; b++)\
		if (mono_bitset_test_fast(set,b))
#define mono_bitset_foreach_bit_rev(set,b,n) \
	for (b = n - 1; b >= 0; b--)\
		if (mono_bitset_test_fast(set,b))
#else
#define mono_bitset_foreach_bit(set,b,n) \
	for (b = mono_bitset_find_first (set, -1); b < n && b >= 0; b = mono_bitset_find_first (set, b))
#define mono_bitset_foreach_bit_rev(set,b,n) \
	for (b = mono_bitset_find_last (set, n - 1); b >= 0; b = b ? mono_bitset_find_last (set, b) : -1)
 
#endif

/*
 * Pull the list of opcodes
 */
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	CEE_LASTOP
};
#undef OPDEF

#define MONO_VARINFO(cfg,varnum) ((cfg)->vars [varnum])

#define MONO_INST_NEW(cfg,dest,op) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = (op);	\
	} while (0)

#define MONO_INST_NEW_CALL(cfg,dest,op) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoCallInst));	\
		(dest)->inst.opcode = (op);	\
	} while (0)

#define MONO_ADD_INS(b,inst) do {	\
		if ((b)->last_ins) {	\
			(b)->last_ins->next = (inst);	\
			(b)->last_ins = (inst);	\
		} else {	\
			(b)->code = (b)->last_ins = (inst);	\
		}	\
	} while (0)

typedef struct MonoInst MonoInst;
typedef struct MonoCallInst MonoCallInst;
typedef struct MonoEdge MonoEdge;
typedef struct MonoMethodVar MonoMethodVar;
typedef struct MonoBasicBlock MonoBasicBlock;
typedef struct MonoLMF MonoLMF;
typedef struct MonoSpillInfo MonoSpillInfo;
typedef struct MonoTraceSpec MonoTraceSpec;

extern guint32 mono_jit_tls_id;
extern MonoTraceSpec *mono_jit_trace_calls;
extern gboolean mono_break_on_exc;
extern int mono_exc_esp_offset;
#ifdef DISABLE_AOT
#define mono_compile_aot 0
#else
extern gboolean mono_compile_aot;
#endif

struct MonoEdge {
	MonoEdge *next;
	MonoBasicBlock *bb;
	/* add edge type? */
};

struct MonoSpillInfo {
	MonoSpillInfo *next;
	int offset;
};

/*
 * The IR-level basic block.  
 *
 * A basic block can have multiple exits just fine, as long as the point of
 * 'departure' is the last instruction in the basic block. Extended basic
 * blocks, on the other hand, may have instructions that leave the block
 * midstream. The important thing is that they cannot be _entered_
 * midstream, ie, execution of a basic block (or extened bb) always start
 * at the beginning of the block, never in the middle.
 */
struct MonoBasicBlock {
	MonoInst *last_ins;

	/* Points to the start of the CIL code that initiated this BB */
	unsigned char* cil_code;

	/* Length of the CIL block */
	gint32 cil_length;

	/* The address of the generated code, used for fixups */
	int native_offset;
	int max_offset;
	
	gint32 dfn;

	/* unique block number identification */
	gint32 block_num;

	/* Visited and reachable flags */
	guint32 flags;

	/* Basic blocks: incoming and outgoing counts and pointers */
	gint16 out_count, in_count;
	MonoBasicBlock **in_bb;
	MonoBasicBlock **out_bb;

	/* the next basic block in the order it appears in IL */
	MonoBasicBlock *next_bb;

	/*
	 * Before instruction selection it is the first tree in the
	 * forest and the first item in the list of trees. After
	 * instruction selection it is the first instruction and the
	 * first item in the list of instructions.
	 */
	MonoInst *code;

	/*
	 * SSA and loop based flags
	 */
	MonoBitSet *dominators;
	MonoBitSet *dfrontier;
	MonoBasicBlock *idom;
	GList *dominated;
	/* fast dominator algorithm */
	MonoBasicBlock *df_parent, *ancestor, *child, *label;
	MonoEdge *bucket;
	int size, sdom, idomn;
	
	/* loop nesting and recognition */
	GList *loop_blocks;
	gint8  nesting;
	gint8  loop_body_start;

	/* use for liveness analysis */
	MonoBitSet *gen_set;
	MonoBitSet *kill_set;
	MonoBitSet *live_in_set;
	MonoBitSet *live_out_set;

	/* fields to deal with non-empty stack slots at bb boundary */
	guint16 out_scount, in_scount;
	MonoInst **out_stack;
	MonoInst **in_stack;

	/* we use that to prevent merging of bblock covered by different clauses*/
	guint real_offset;

	/*
	 * The region encodes whether the basic block is inside
	 * a finally, catch, filter or none of thoese.
	 *
	 * If the value is -1, then it is neither finally, catch nor filter
	 *
	 * Otherwise the format is:
	 *
	 *  Bits: |     0-3      |       4-7      |     8-31
	 * 	  |		 |                |
	 *        | clause-flags |   MONO_REGION  | clause-index 
	 *
	 */
        guint region;

	/* The current symbolic register number, used in local register allocation. */
	guint32 max_ireg, max_freg;
};

/* BBlock flags */
#define BB_VISITED 1
#define BB_REACHABLE 2

struct MonoInst {
	union {
		union {
			MonoInst *src;
			MonoMethodVar *var;
			gssize const_val;
			gpointer p;
			MonoMethod *method;
			MonoMethodSignature *signature;
			MonoBasicBlock **many_blocks;
			MonoBasicBlock *target_block;
			MonoInst **args;
			MonoType *vtype;
			MonoClass *klass;
			int *phi_args;
		} op [2];
		gint64 i8const;
		double r8const;
	} data;
	guint16 opcode;
	guint8  type; /* stack type */
	guint   ssa_op : 3;
	guint8  flags  : 5;
	
	/* used by the register allocator */
	gint32 dreg, sreg1, sreg2, unused;
	
	MonoInst *next;
	MonoClass *klass;
	const unsigned char* cil_code; /* for debugging and bblock splitting */
};
	
struct MonoCallInst {
	MonoInst inst;
	MonoMethodSignature *signature;
	MonoMethod *method;
	MonoInst **args;
	MonoInst *out_args;
	gconstpointer fptr;
	guint stack_usage;
	gboolean virtual;
	regmask_t used_iregs;
	regmask_t used_fregs;
#ifdef __x86_64__
	GSList *out_ireg_args;
	GSList *out_freg_args;
#endif
};

/* 
 * flags for MonoInst
 * Note: some of the values overlap, because they can't appear
 * in the same MonoInst.
 */
enum {
	MONO_INST_HAS_METHOD = 1,
	/* temp local created by a DUP: used only within a BB */
	MONO_INST_IS_TEMP    = 1,
	MONO_INST_INIT       = 1, /* in localloc */
	MONO_INST_IS_DEAD    = 2,
	MONO_INST_TAILCALL   = 4,
	MONO_INST_VOLATILE   = 4,
	MONO_INST_BRLABEL    = 4,
	MONO_INST_NOTYPECHECK    = 4,
	MONO_INST_UNALIGNED  = 8,
	/* the address of the variable has been taken */
	MONO_INST_INDIRECT   = 16,
	MONO_INST_NORANGECHECK   = 16
};

#define inst_c0 data.op[0].const_val
#define inst_c1 data.op[1].const_val
#define inst_i0 data.op[0].src
#define inst_i1 data.op[1].src
#define inst_p0 data.op[0].p
#define inst_p1 data.op[1].p
#define inst_l  data.i8const
#define inst_r  data.r8const
#define inst_left  data.op[0].src
#define inst_right data.op[1].src

#define inst_newa_len   data.op[0].src
#define inst_newa_class data.op[1].klass

#define inst_var    data.op[0].var
#define inst_vtype  data.op[1].vtype
/* in branch instructions */
#define inst_many_bb   data.op[1].many_blocks
#define inst_target_bb data.op[0].target_block
#define inst_true_bb   data.op[1].many_blocks[0]
#define inst_false_bb  data.op[1].many_blocks[1]

#define inst_basereg sreg1
#define inst_indexreg sreg2
#define inst_destbasereg dreg
#define inst_offset data.op[0].const_val
#define inst_imm    data.op[1].const_val

#define inst_phi_args   data.op[1].phi_args

/* instruction description for use in regalloc/scheduling */
enum {
	MONO_INST_DEST,
	MONO_INST_SRC1,
	MONO_INST_SRC2,
	MONO_INST_FLAGS,
	MONO_INST_CLOB,
	MONO_INST_COST,
	MONO_INST_DELAY,
	MONO_INST_RES,
	MONO_INST_LEN,
	MONO_INST_MAX
};

typedef union {
	struct {
		guint16 tid; /* tree number */
		guint16 bid; /* block number */
	} pos ;
	guint32 abs_pos; 
} MonoPosition;

typedef struct {
	MonoPosition first_use, last_use;
} MonoLiveRange;

/*
 * Additional information about a variable
 */
struct MonoMethodVar {
	guint           idx; /* inside cfg->varinfo, cfg->vars */
	guint           last_name;
	MonoBitSet     *dfrontier;
	MonoLiveRange   range; /* generated by liveness analysis */
	int             reg; /* != -1 if allocated into a register */
	int             spill_costs;
	MonoBitSet     *def_in; /* used by SSA */
	MonoInst       *def;    /* used by SSA */
	MonoBasicBlock *def_bb; /* used by SSA */
	GList          *uses;   /* used by SSA */
	char            cpstate;  /* used by SSA conditional  constant propagation */
};

typedef struct {
	gpointer          end_of_stack;
	guint32           stack_size;
	MonoLMF          *lmf;
	MonoLMF          *first_lmf;
	gpointer         signal_stack;
	guint32          signal_stack_size;
	void            (*abort_func) (MonoObject *object);
} MonoJitTlsData;

typedef enum {
	MONO_PATCH_INFO_BB,
	MONO_PATCH_INFO_ABS,
	MONO_PATCH_INFO_LABEL,
	MONO_PATCH_INFO_METHOD,
	MONO_PATCH_INFO_METHOD_JUMP,
	MONO_PATCH_INFO_METHOD_REL,
	MONO_PATCH_INFO_METHODCONST,
	MONO_PATCH_INFO_INTERNAL_METHOD,
	MONO_PATCH_INFO_SWITCH,
	MONO_PATCH_INFO_EXC,
	MONO_PATCH_INFO_EXC_NAME,
	MONO_PATCH_INFO_CLASS,
	MONO_PATCH_INFO_IMAGE,
	MONO_PATCH_INFO_FIELD,
	MONO_PATCH_INFO_VTABLE,
	MONO_PATCH_INFO_CLASS_INIT,
	MONO_PATCH_INFO_SFLDA,
	MONO_PATCH_INFO_LDSTR,
	MONO_PATCH_INFO_LDTOKEN,
	MONO_PATCH_INFO_TYPE_FROM_HANDLE,
	MONO_PATCH_INFO_R4,
	MONO_PATCH_INFO_R8,
	MONO_PATCH_INFO_IP,
	MONO_PATCH_INFO_IID,
	MONO_PATCH_INFO_BB_OVF,
	MONO_PATCH_INFO_EXC_OVF,
	MONO_PATCH_INFO_WRAPPER,
	MONO_PATCH_INFO_GOT_OFFSET,
	MONO_PATCH_INFO_NONE
} MonoJumpInfoType;

/*
 * We need to store the image which the token refers to along with the token,
 * since the image might not be the same as the image of the method which
 * contains the relocation, because of inlining.
 */
typedef struct MonoJumpInfoToken {
	MonoImage *image;
	guint32 token;
} MonoJumpInfoToken;

typedef struct MonoJumpInfoBBTable {
	MonoBasicBlock **table;
	int table_size;
} MonoJumpInfoBBTable;

typedef struct MonoJumpInfo MonoJumpInfo;
struct MonoJumpInfo {
	MonoJumpInfo *next;
	union {
		int i;
		guint8 *p;
		MonoInst *label;
	} ip;

	MonoJumpInfoType type;
	union {
		gconstpointer   target;
#if SIZEOF_VOID_P == 8
		gint64          offset;
#else
		int             offset;
#endif
		MonoBasicBlock *bb;
		MonoInst       *inst;
		MonoMethod     *method;
		MonoClass      *klass;
		MonoClassField *field;
		MonoImage      *image;
		MonoVTable     *vtable;
		const char     *name;
		MonoJumpInfoToken  *token;
		MonoJumpInfoBBTable *table;
	} data;
};

/* optimization flags: keep up to date with the name array in driver.c */
enum {
	MONO_OPT_PEEPHOLE = 1 << 0,
	MONO_OPT_BRANCH   = 1 << 1,
	MONO_OPT_INLINE   = 1 << 2,
	MONO_OPT_CFOLD    = 1 << 3,
	MONO_OPT_CONSPROP = 1 << 4,
	MONO_OPT_COPYPROP = 1 << 5,
	MONO_OPT_DEADCE   = 1 << 6,
	MONO_OPT_LINEARS  = 1 << 7,
	MONO_OPT_CMOV     = 1 << 8,
	MONO_OPT_SHARED   = 1 << 9,
	MONO_OPT_SCHED    = 1 << 10,
	MONO_OPT_INTRINS  = 1 << 11,
	MONO_OPT_TAILC    = 1 << 12,
	MONO_OPT_LOOP     = 1 << 13,
	MONO_OPT_FCMOV    = 1 << 14,
	MONO_OPT_LEAF     = 1 << 15,
	MONO_OPT_AOT      = 1 << 16,
	MONO_OPT_PRECOMP  = 1 << 17,
	MONO_OPT_ABCREM   = 1 << 18,
	MONO_OPT_SSAPRE   = 1 << 19
};

/* Bit-fields in the MonoBasicBlock.region */
#define MONO_REGION_FINALLY  16
#define MONO_REGION_CATCH    32
#define MONO_REGION_FAULT    64         /* Currently unused */
#define MONO_REGION_FILTER  128

/*
 * Control Flow Graph and compilation unit information
 */
typedef struct {
	MonoMethod      *method;
	MonoMemPool     *mempool;
	MonoInst       **varinfo;
	MonoMethodVar  **vars;
	MonoInst        *ret;
	MonoBasicBlock  *bb_entry;
	MonoBasicBlock  *bb_exit;
	MonoBasicBlock  *bb_init;
	MonoBasicBlock **bblocks;
	GHashTable      *bb_hash;
	MonoMemPool     *state_pool; /* used by instruction selection */
	MonoBasicBlock  *cbb;        /* used by instruction selection */
	MonoInst        *prev_ins;   /* in decompose */
	MonoJumpInfo    *patch_info;
	MonoJitInfo     *jit_info;
	MonoJitDynamicMethodInfo *dynamic_info;
	guint            num_bblocks;
	guint            locals_start;
	guint            num_varinfo; /* used items in varinfo */
	guint            varinfo_count; /* total storage in varinfo */
	gint             stack_offset;
	gint             max_ireg;
	MonoRegState    *rs;
	MonoSpillInfo   *spill_info; /* machine register spills */
	MonoSpillInfo   *spill_info_float; /* fp register spills */
	gint             spill_count;
	/* unsigned char   *cil_code; */
	MonoMethod      *inlined_method; /* the method which is currently inlined */

	/* the exception object passed to catch/filter blocks */
	MonoInst        *exvar;
	
	MonoInst        *domainvar; /* a cache for the current domain */
	MonoInst        *got_var; /* Global Offset Table variable */

	/* A hashtable of region ID-> SP var mappings */
	/* An SP var is a place to store the stack pointer (used by handlers)*/
	GHashTable      *spvars;

	GList           *ldstr_list; /* used by AOT */
	
	MonoDomain      *domain;

	unsigned char   *native_code;
	guint            code_size;
	guint            code_len;
	guint            prolog_end;
	guint            epilog_begin;
	regmask_t        used_int_regs;
	guint32          opt;
	guint32          prof_options;
	guint32          flags;
	guint32          comp_done;
	guint32          verbose_level;
	guint32          stack_usage;
	guint32          param_area;
	guint32          frame_reg;
	gint32           sig_cookie;
	gboolean         disable_aot;
	gboolean         disable_ssa;
	gboolean         run_cctors;
	gboolean         need_lmf_area;
	gboolean         compile_aot;
	gpointer         debug_info;
	guint32          lmf_offset;
	guint16          *intvars;
	MonoProfileCoverageInfo *coverage_info;
	MonoCompileArch  arch;
#ifdef __ia64
	guint8           ins, locals, outs; /* reg stack region sizes */
#endif /* __ia64 */
} MonoCompile;

typedef enum {
	MONO_CFG_HAS_ALLOCA = 1 << 0,
	MONO_CFG_HAS_CALLS  = 1 << 1,
	MONO_CFG_HAS_LDELEMA  = 1 << 2,
	MONO_CFG_HAS_VARARGS  = 1 << 3,
	MONO_CFG_HAS_TAIL     = 1 << 4
} MonoCompileFlags;

typedef struct {
	gulong methods_compiled;
	gulong methods_aot;
	gulong methods_lookups;
	gulong method_trampolines;
	gulong allocate_var;
	gulong analyze_stack_repeat;
	gulong cil_code_size;
	gulong native_code_size;
	gulong code_reallocs;
	gulong max_code_size_ratio;
	gulong biggest_method_size;
	gulong allocated_code_size;
	gulong inlineable_methods;
	gulong inlined_methods;
	gulong basic_blocks;
	gulong max_basic_blocks;
	MonoMethod *max_ratio_method;
	MonoMethod *biggest_method;
	gboolean enabled;
} MonoJitStats;

extern MonoJitStats mono_jit_stats;

/* values for MonoInst.ssa_op */
enum {
	MONO_SSA_NOP,
	MONO_SSA_LOAD,
	MONO_SSA_STORE,
	MONO_SSA_MAYBE_LOAD,
	MONO_SSA_MAYBE_STORE
};

#define OP_CEQ    (256+CEE_CEQ)
#define OP_CLT    (256+CEE_CLT)
#define OP_CLT_UN (256+CEE_CLT_UN)
#define OP_CGT    (256+CEE_CGT)
#define OP_CGT_UN (256+CEE_CGT_UN)
#define OP_LOCALLOC (256+CEE_LOCALLOC)

/* opcodes: value assigned after all the CIL opcodes */
#ifdef MINI_OP
#undef MINI_OP
#endif
#define MINI_OP(a,b) a,
enum {
	OP_START = MONO_CEE_LAST,
#include "mini-ops.h"
	OP_LAST
};
#undef MINI_OP

#if SIZEOF_VOID_P == 8
#define OP_PADD OP_LADD
#define OP_PNEG OP_LNEG
#define OP_PCONV_TO_U2 OP_LCONV_TO_U2
#define OP_PCONV_TO_OVF_I1_UN OP_LCONV_TO_OVF_I1_UN
#define OP_PCONV_TO_OVF_I1 OP_LCONV_TO_OVF_I1
#define OP_PCEQ CEE_CEQ
#define OP_STOREP_MEMBASE_REG OP_STOREI8_MEMBASE_REG
#define OP_STOREP_MEMBASE_IMM OP_STOREI8_MEMBASE_IMM
#else
#define OP_PADD CEE_ADD
#define OP_PNEG CEE_NEG
#define OP_PCONV_TO_U2 CEE_CONV_U2
#define OP_PCONV_TO_OVF_I1_UN CEE_CONV_OVF_I1_UN
#define OP_PCONV_TO_OVF_I1 CEE_CONV_OVF_I1
#define OP_PCEQ CEE_CEQ
#define OP_STOREP_MEMBASE_REG OP_STOREI4_MEMBASE_REG
#define OP_STOREP_MEMBASE_IMM OP_STOREI4_MEMBASE_IMM
#endif

typedef enum {
	STACK_INV,
	STACK_I4,
	STACK_I8,
	STACK_PTR,
	STACK_R8,
	STACK_MP,
	STACK_OBJ,
	STACK_VTYPE,
	STACK_MAX
} MonoStackType;

typedef struct {
	union {
		double   r8;
		gint32   i4;
		gint64   i8;
		gpointer p;
		MonoClass *klass;
	} data;
	int type;
} StackSlot;

enum {
	MONO_COMP_DOM = 1,
	MONO_COMP_IDOM = 2,
	MONO_COMP_DFRONTIER = 4,
	MONO_COMP_DOM_REV = 8,
	MONO_COMP_LIVENESS = 16,
	MONO_COMP_SSA = 32,
	MONO_COMP_SSA_DEF_USE = 64,
	MONO_COMP_REACHABILITY = 128,
	MONO_COMP_LOOPS = 256
};

typedef enum {
	MONO_GRAPH_CFG = 1,
	MONO_GRAPH_DTREE = 2,
	MONO_GRAPH_CFG_CODE = 4,
	MONO_GRAPH_CFG_SSA = 8,
	MONO_GRAPH_CFG_OPTCODE = 16
} MonoGraphOptions;

typedef struct {
	guint16 size;
	guint16 offset;
	guint8  pad;
} MonoJitArgumentInfo;

typedef void (*MonoInstFunc) (MonoInst *tree, gpointer data);

/* main function */
int         mono_main                      (int argc, char* argv[]);
void        mono_set_defaults              (int verbose_level, guint32 opts);
MonoDomain* mini_init                      (const char *filename);
void        mini_cleanup                   (MonoDomain *domain);

/* helper methods */
MonoJumpInfoToken * mono_jump_info_token_new (MonoMemPool *mp, MonoImage *image, guint32 token);
MonoInst* mono_find_spvar_for_region        (MonoCompile *cfg, int region);
void      mono_precompile_assemblies        (void);
int       mono_parse_default_optimizations  (const char* p);
void      mono_bblock_add_inst              (MonoBasicBlock *bb, MonoInst *inst);
void      mono_constant_fold                (MonoCompile *cfg);
void      mono_constant_fold_inst           (MonoInst *inst, gpointer data);
int       mono_is_power_of_two              (guint32 val);
void      mono_cprop_local                  (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **acp, int acp_size);
MonoInst* mono_compile_create_var           (MonoCompile *cfg, MonoType *type, int opcode);
void      mono_compile_make_var_load        (MonoCompile *cfg, MonoInst *dest, gssize var_index);
MonoInst* mono_compile_create_var_load      (MonoCompile *cfg, gssize var_index);
MonoInst* mono_compile_create_var_store     (MonoCompile *cfg, gssize var_index, MonoInst *value);
MonoType* mono_type_from_stack_type         (MonoInst *ins);
void      mono_blockset_print               (MonoCompile *cfg, MonoBitSet *set, const char *name, guint idom);
void      mono_print_tree                   (MonoInst *tree);
void      mono_print_tree_nl                (MonoInst *tree);
void      mono_print_code                   (MonoCompile *cfg);
void      mono_print_method_from_ip         (void *ip);
void      mono_select_instructions          (MonoCompile *cfg);
const char* mono_inst_name                  (int op);
void      mono_inst_foreach                 (MonoInst *tree, MonoInstFunc func, gpointer data);
void      mono_disassemble_code             (guint8 *code, int size, char *id);
guint     mono_type_to_ldind                (MonoType *t);
guint     mono_type_to_stind                (MonoType *t);
void      mono_add_patch_info               (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target);
void      mono_remove_patch_info            (MonoCompile *cfg, int ip);
gpointer  mono_resolve_patch_target         (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *patch_info, gboolean run_cctors);
MonoLMF** mono_get_lmf_addr                 (void);
GList    *mono_varlist_insert_sorted        (MonoCompile *cfg, GList *list, MonoMethodVar *mv, gboolean sort_end);
GList    *mono_varlist_sort                 (MonoCompile *cfg, GList *list, int sort_type);
void      mono_analyze_liveness             (MonoCompile *cfg);
void      mono_linear_scan                  (MonoCompile *cfg, GList *vars, GList *regs, regmask_t *used_mask);
void      mono_create_jump_table            (MonoCompile *cfg, MonoInst *label, MonoBasicBlock **bbs, int num_blocks);
int       mono_compile_assembly             (MonoAssembly *ass, guint32 opts, const char *aot_options);
MonoCompile *mini_method_compile            (MonoMethod *method, guint32 opts, MonoDomain *domain, gboolean run_cctors, gboolean compile_aot, int parts);
void      mono_destroy_compile              (MonoCompile *cfg);
void      mono_aot_init                     (void);
MonoJitInfo*  mono_aot_get_method           (MonoDomain *domain,
											 MonoMethod *method);
gboolean  mono_method_blittable             (MonoMethod *method);
gboolean  mono_method_same_domain           (MonoJitInfo *caller, MonoJitInfo *callee);
void      mono_register_opcode_emulation    (int opcode, const char* name, MonoMethodSignature *sig, gpointer func, gboolean no_throw);
void      mono_arch_register_lowlevel_calls (void);
void      mono_draw_graph                   (MonoCompile *cfg, MonoGraphOptions draw_options);
void      mono_add_varcopy_to_end           (MonoCompile *cfg, MonoBasicBlock *bb, int src, int dest);
void      mono_add_ins_to_end               (MonoBasicBlock *bb, MonoInst *inst);

int               mono_find_method_opcode      (MonoMethod *method);
MonoJitICallInfo *mono_find_jit_icall_by_name  (const char *name);
MonoJitICallInfo *mono_find_jit_icall_by_addr  (gconstpointer addr);
MonoJitICallInfo *mono_register_jit_icall      (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save);
gconstpointer     mono_icall_get_wrapper       (MonoJitICallInfo* callinfo);

gpointer          mono_create_jump_trampoline (MonoDomain *domain, 
											   MonoMethod *method, 
											   gboolean add_sync_wrapper);
gpointer          mono_create_class_init_trampoline (MonoVTable *vtable);
gpointer          mono_create_jit_trampoline (MonoMethod *method);
MonoVTable*       mono_find_class_init_trampoline_by_addr (gconstpointer addr);
gboolean          mono_running_on_valgrind (void);

/* methods that must be provided by the arch-specific port */
void      mono_arch_cpu_init                    (void);
guint32   mono_arch_cpu_optimizazions           (guint32 *exclude_mask);
void      mono_arch_instrument_mem_needs        (MonoMethod *method, int *stack, int *code);
void     *mono_arch_instrument_prolog           (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments);
void     *mono_arch_instrument_epilog           (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments);
MonoCallInst *mono_arch_call_opcode             (MonoCompile *cfg, MonoBasicBlock* bb, MonoCallInst *call, int is_virtual);
MonoInst *mono_arch_get_inst_for_method       (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args);
void      mono_codegen                          (MonoCompile *cfg);
const char *mono_arch_regname                   (int reg);
gpointer  mono_arch_get_throw_exception         (void);
gpointer  mono_arch_get_rethrow_exception       (void);
gpointer  mono_arch_get_throw_exception_by_name (void);
gpointer  mono_arch_get_throw_corlib_exception  (void);
gpointer  mono_arch_create_jit_trampoline       (MonoMethod *method);
MonoJitInfo *mono_arch_create_jump_trampoline      (MonoMethod *method);
gpointer  mono_arch_create_class_init_trampoline(MonoVTable *vtable);
GList    *mono_arch_get_allocatable_int_vars    (MonoCompile *cfg);
GList    *mono_arch_get_global_int_regs         (MonoCompile *cfg);
guint32   mono_arch_regalloc_cost               (MonoCompile *cfg, MonoMethodVar *vmv);
void      mono_arch_patch_code                  (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors);
void      mono_arch_flush_icache                (guint8 *code, gint size);
int       mono_arch_max_epilog_size             (MonoCompile *cfg);
guint8   *mono_arch_emit_prolog                 (MonoCompile *cfg);
void      mono_arch_emit_epilog                 (MonoCompile *cfg);
void      mono_arch_local_regalloc              (MonoCompile *cfg, MonoBasicBlock *bb);
void      mono_arch_output_basic_block          (MonoCompile *cfg, MonoBasicBlock *bb);
gboolean  mono_arch_has_unwind_info             (gconstpointer addr);
void      mono_arch_setup_jit_tls_data          (MonoJitTlsData *tls);
void      mono_arch_free_jit_tls_data           (MonoJitTlsData *tls);
void      mono_arch_emit_this_vret_args         (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg);
void      mono_arch_allocate_vars               (MonoCompile *m);
int       mono_arch_get_argument_info           (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info);
gboolean  mono_arch_print_tree			(MonoInst *tree, int arity);
MonoJitInfo *mono_arch_find_jit_info            (MonoDomain *domain, 
						 MonoJitTlsData *jit_tls, 
						 MonoJitInfo *res, 
						 MonoJitInfo *prev_ji, 
						 MonoContext *ctx, 
						 MonoContext *new_ctx, 
						 char **trace, 
						 MonoLMF **lmf, 
						 int *native_offset,
						 gboolean *managed);
gpointer mono_arch_get_call_filter              (void);
gpointer mono_arch_get_restore_context          (void);
gboolean mono_arch_handle_exception             (void *sigctx, gpointer obj, gboolean test_only);
gpointer mono_arch_ip_from_context              (void *sigctx);
void     mono_arch_flush_register_windows       (void);
gboolean mono_arch_is_inst_imm                  (gint64 imm);
MonoInst* mono_arch_get_domain_intrinsic        (MonoCompile* cfg);
MonoInst* mono_arch_get_thread_intrinsic        (MonoCompile* cfg);
gboolean mono_arch_is_int_overflow              (void *sigctx, void *info);
void     mono_arch_invalidate_method            (MonoJitInfo *ji, void *func, gpointer func_arg);
guint32  mono_arch_get_patch_offset             (guint8 *code);

/* Exception handling */
gboolean mono_handle_exception                  (MonoContext *ctx, gpointer obj,
						 gpointer original_ip, gboolean test_only);
void      mono_jit_walk_stack                   (MonoStackWalk func, gboolean do_il_offset, gpointer user_data);

/* the new function to do stack walks */
typedef gboolean (*MonoStackFrameWalk)          (MonoDomain *domain, MonoContext *ctx, MonoJitInfo *ji, gpointer data);
void      mono_walk_stack                       (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoContext *start_ctx, MonoStackFrameWalk func, gpointer user_data);

MonoArray *ves_icall_get_trace                  (MonoException *exc, gint32 skip, MonoBoolean need_file_info);
MonoBoolean ves_icall_get_frame_info            (gint32 skip, MonoBoolean need_file_info, 
						 MonoReflectionMethod **method, 
						 gint32 *iloffset, gint32 *native_offset,
						 MonoString **file, gint32 *line, gint32 *column);

/* Dominator/SSA methods */
void        mono_compile_dominator_info         (MonoCompile *cfg, int dom_flags);
void        mono_compute_natural_loops          (MonoCompile *cfg);
MonoBitSet* mono_compile_iterated_dfrontier     (MonoCompile *cfg, MonoBitSet *set);
void        mono_ssa_compute                    (MonoCompile *cfg);
void        mono_ssa_remove                     (MonoCompile *cfg);
void        mono_ssa_cprop                      (MonoCompile *cfg);
void        mono_ssa_deadce                     (MonoCompile *cfg);
void        mono_ssa_strength_reduction         (MonoCompile *cfg);
void        mono_free_loop_info                 (MonoCompile *cfg);

/* debugging support */
void      mono_debug_init_method                (MonoCompile *cfg, MonoBasicBlock *start_block,
						 guint32 breakpoint_id);
void      mono_debug_open_method                (MonoCompile *cfg);
void      mono_debug_close_method               (MonoCompile *cfg);
void      mono_debug_open_block                 (MonoCompile *cfg, MonoBasicBlock *bb, guint32 address);
void      mono_debug_record_line_number         (MonoCompile *cfg, MonoInst *ins, guint32 address);
void      mono_debug_serialize_debug_info       (MonoCompile *cfg, guint8 **out_buf, guint32 *buf_len);
void      mono_debug_add_aot_method             (MonoDomain *domain,
												MonoMethod *method, guint8 *code_start, 
												guint8 *debug_info, guint32 debug_info_len);
void      mono_debug_add_icall_wrapper          (MonoMethod *method, MonoJitICallInfo* info);


/* Tracing */
MonoTraceSpec *mono_trace_parse_options         (char *options);
void           mono_trace_set_assembly          (MonoAssembly *assembly);
gboolean       mono_trace_eval                  (MonoMethod *method);

extern void
mono_perform_abc_removal (MonoCompile *cfg);
extern void
mono_perform_ssapre (MonoCompile *cfg);

/* CAS - stack walk */
//MonoBoolean ves_icall_System_Security_SecurityFrame_GetSecurityFrameInformation (gint32 skip, MonoReflectionMethod **method, gint32 *flags);
MonoSecurityFrame* ves_icall_System_Security_SecurityFrame_GetSecurityFrame (gint32 skip);
MonoArray* ves_icall_System_Security_SecurityFrame_GetSecurityStack (gint32 skip);

#endif /* __MONO_MINI_H__ */
