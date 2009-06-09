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
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-compiler.h>

#define MONO_BREAKPOINT_ARRAY_SIZE 64

/* C type matching the size of a machine register. Not always the same as 'int' */
/* Note that member 'p' of MonoInst must be the same type, as OP_PCONST is defined
 * as one of the OP_ICONST types, so inst_c0 must be the same as inst_p0
 */
#if SIZEOF_REGISTER == 4
typedef gint32 mgreg_t;
#elif SIZEOF_REGISTER == 8
typedef gint64 mgreg_t;
#endif

#include "mini-arch.h"
#include "regalloc.h"
#include "declsec.h"
#include "unwind.h"

#ifndef G_LIKELY
#define G_LIKELY(a) (a)
#define G_UNLIKELY(a) (a)
#endif

#ifndef G_MAXINT32
#define G_MAXINT32 2147483647
#endif

#ifndef G_MININT32
#define G_MININT32 (-G_MAXINT32 - 1)
#endif

#if DISABLE_LOGGING
#define MINI_DEBUG(level,limit,code)
#else
#define MINI_DEBUG(level,limit,code) do {if (G_UNLIKELY ((level) >= (limit))) code} while (0)
#endif

#if !defined(DISABLE_TASKLETS) && defined(MONO_ARCH_SUPPORT_TASKLETS) && defined(__GNUC__)
#define MONO_SUPPORT_TASKLETS 1
#endif

#if ENABLE_LLVM
#define COMPILE_LLVM(cfg) ((cfg)->compile_llvm)
#define LLVM_ENABLED TRUE
#else
#define COMPILE_LLVM(cfg) (0)
#define LLVM_ENABLED FALSE
#endif

#define NOT_IMPLEMENTED do { g_assert_not_reached (); } while (0)

/* for 32 bit systems */
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
#define MINI_LS_WORD_IDX 0
#define MINI_MS_WORD_IDX 1
#else
#define MINI_LS_WORD_IDX 1
#define MINI_MS_WORD_IDX 0
#endif
#define MINI_LS_WORD_OFFSET (MINI_LS_WORD_IDX * 4)
#define MINI_MS_WORD_OFFSET (MINI_MS_WORD_IDX * 4)
#define inst_ls_word data.op[MINI_LS_WORD_IDX].const_val
#define inst_ms_word data.op[MINI_MS_WORD_IDX].const_val

#define MONO_FAKE_IMT_METHOD ((MonoMethod*)GINT_TO_POINTER(-1))
#define MONO_FAKE_VTABLE_METHOD ((MonoMethod*)GINT_TO_POINTER(-2))

#ifndef DISABLE_AOT
#define MONO_USE_AOT_COMPILER
#endif

/* Version number of the AOT file format */
#define MONO_AOT_FILE_VERSION "55"

/* Constants used to encode different types of methods in AOT */
enum {
	MONO_AOT_METHODREF_MIN = 240,
	/* Method encoded using its name */
	MONO_AOT_METHODREF_WRAPPER_NAME = 250,
	/* Runtime provided methods on arrays */
	MONO_AOT_METHODREF_ARRAY = 251,
	MONO_AOT_METHODREF_NO_AOT_TRAMPOLINE = 252,
	/* Wrappers */
	MONO_AOT_METHODREF_WRAPPER = 253,
	/* Methods on generic instances */
	MONO_AOT_METHODREF_GINST = 254,
	/* Methods resolve using a METHODSPEC token */
	MONO_AOT_METHODREF_METHODSPEC = 255,
};

/* Trampolines which we have a lot of */
typedef enum {
	MONO_AOT_TRAMP_SPECIFIC = 0,
	MONO_AOT_TRAMP_STATIC_RGCTX = 1,
	MONO_AOT_TRAMP_IMT_THUNK = 2,
	MONO_AOT_TRAMP_NUM = 3
} MonoAotTrampoline;

/* This structure is stored in the AOT file */
typedef struct MonoAotFileInfo
{
	guint32 plt_got_offset_base;
	guint32 got_size;
	guint32 plt_size;

	guint32 num_trampolines [MONO_AOT_TRAMP_NUM];
	guint32 trampoline_got_offset_base [MONO_AOT_TRAMP_NUM];
	guint32 trampoline_size [MONO_AOT_TRAMP_NUM];
} MonoAotFileInfo;
 
/* Per-domain information maintained by the JIT */
typedef struct
{
	/* Maps MonoMethod's to a GSList of GOT slot addresses pointing to its code */
	GHashTable *jump_target_got_slot_hash;
	GHashTable *jump_target_hash;
	/* Maps methods/klasses to the address of the given type of trampoline */
	GHashTable *class_init_trampoline_hash;
	GHashTable *jump_trampoline_hash;
	GHashTable *jit_trampoline_hash;
	GHashTable *delegate_trampoline_hash;
	GHashTable *static_rgctx_trampoline_hash;
	GHashTable *llvm_vcall_trampoline_hash;
	/* maps MonoMethod -> MonoJitDynamicMethodInfo */
	GHashTable *dynamic_code_hash;
	GHashTable *method_code_hash;
	/* Compiled runtime invoke function for parameterless ctors */
	gpointer ctor_runtime_invoke;
} MonoJitDomainInfo;

typedef struct {
	MonoJitInfo *ji;
	MonoCodeManager *code_mp;
} MonoJitDynamicMethodInfo;

#define domain_jit_info(domain) ((MonoJitDomainInfo*)((domain)->runtime_info))

#if 0
#define mono_bitset_foreach_bit(set,b,n) \
	for (b = 0; b < n; b++)\
		if (mono_bitset_test_fast(set,b))
#define mono_bitset_foreach_bit_rev(set,b,n) \
	for (b = n - 1; b >= 0; b--)\
		if (mono_bitset_test_fast(set,b))
#else
#define mono_bitset_foreach_bit(set,b,n) \
	for (b = mono_bitset_find_start (set); b < n && b >= 0; b = mono_bitset_find_first (set, b))
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

#define MONO_VARINFO(cfg,varnum) (&(cfg)->vars [varnum])

#define MONO_INST_NULLIFY_SREGS(dest) do {				\
		(dest)->sreg1 = (dest)->sreg2 = (dest)->sreg3 = -1;	\
	} while (0)

#define MONO_INST_NEW(cfg,dest,op) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = (op);	\
		(dest)->dreg = -1;			    \
		MONO_INST_NULLIFY_SREGS ((dest));	    \
        (dest)->cil_code = (cfg)->ip;  \
	} while (0)

#define MONO_INST_NEW_CALL(cfg,dest,op) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoCallInst));	\
		(dest)->inst.opcode = (op);	\
		(dest)->inst.dreg = -1;					\
		MONO_INST_NULLIFY_SREGS (&(dest)->inst);		\
        (dest)->inst.cil_code = (cfg)->ip;  \
	} while (0)

#define MONO_INST_NEW_CALL_ARG(cfg,dest,op) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoCallArgParm));	\
		(dest)->ins.opcode = (op);	\
	} while (0)

#define MONO_ADD_INS(b,inst) do {	\
		if ((b)->last_ins) {	\
			(b)->last_ins->next = (inst);	\
            (inst)->prev = (b)->last_ins;   \
			(b)->last_ins = (inst);	\
		} else {	\
			(b)->code = (b)->last_ins = (inst);	\
		}	\
	} while (0)

#define NULLIFY_INS(ins) do { \
        (ins)->opcode = OP_NOP; \
        (ins)->dreg = -1;				\
	MONO_INST_NULLIFY_SREGS ((ins));		\
		(ins)->ssa_op = MONO_SSA_NOP; \
    } while (0)

/* Remove INS from BB */
#define MONO_REMOVE_INS(bb,ins) do { \
        if ((ins)->prev) \
            (ins)->prev->next = (ins)->next; \
        if ((ins)->next) \
            (ins)->next->prev = (ins)->prev; \
        if ((bb)->code == (ins)) \
            (bb)->code = (ins)->next; \
        if ((bb)->last_ins == (ins)) \
            (bb)->last_ins = (ins)->prev; \
    } while (0)

/* Remove INS from BB and nullify it */
#define MONO_DELETE_INS(bb,ins) do { \
        MONO_REMOVE_INS ((bb), (ins)); \
        NULLIFY_INS ((ins)); \
    } while (0)

/* 
 * this is used to determine when some branch optimizations are possible: we exclude FP compares
 * because they have weird semantics with NaNs.
 */
#define MONO_IS_COND_BRANCH_OP(ins) (((ins)->opcode >= OP_LBEQ && (ins)->opcode <= OP_LBLT_UN) || ((ins)->opcode >= OP_FBEQ && (ins)->opcode <= OP_FBLT_UN) || ((ins)->opcode >= OP_IBEQ && (ins)->opcode <= OP_IBLT_UN))
#define MONO_IS_COND_BRANCH_NOFP(ins) (MONO_IS_COND_BRANCH_OP(ins) && !(((ins)->opcode >= OP_FBEQ) && ((ins)->opcode <= OP_FBLT_UN)) && (!(ins)->inst_left || (ins)->inst_left->inst_left->type != STACK_R8))

#define MONO_IS_BRANCH_OP(ins) (MONO_IS_COND_BRANCH_OP(ins) || ((ins)->opcode == OP_BR) || ((ins)->opcode == OP_BR_REG) || ((ins)->opcode == OP_SWITCH))

#define MONO_IS_COND_EXC(ins) ((((ins)->opcode >= OP_COND_EXC_EQ) && ((ins)->opcode <= OP_COND_EXC_LT_UN)) || (((ins)->opcode >= OP_COND_EXC_IEQ) && ((ins)->opcode <= OP_COND_EXC_ILT_UN)))

#define MONO_IS_SETCC(ins) ((((ins)->opcode >= OP_CEQ) && ((ins)->opcode <= OP_CLT_UN)) || (((ins)->opcode >= OP_ICEQ) && ((ins)->opcode <= OP_ICLT_UN)) || (((ins)->opcode >= OP_LCEQ) && ((ins)->opcode <= OP_LCLT_UN)) || (((ins)->opcode >= OP_FCEQ) && ((ins)->opcode <= OP_FCLT_UN)))


#define MONO_IS_LOAD_MEMBASE(ins) (((ins)->opcode >= OP_LOAD_MEMBASE) && ((ins)->opcode <= OP_LOADV_MEMBASE))
#define MONO_IS_STORE_MEMBASE(ins) (((ins)->opcode >= OP_STORE_MEMBASE_REG) && ((ins)->opcode <= OP_STOREV_MEMBASE))
#define MONO_IS_STORE_MEMINDEX(ins) (((ins)->opcode >= OP_STORE_MEMINDEX) && ((ins)->opcode <= OP_STORER8_MEMINDEX))

#define MONO_IS_CALL(ins) (((ins->opcode >= OP_VOIDCALL) && (ins->opcode <= OP_VOIDCALL_MEMBASE)) || ((ins->opcode >= OP_FCALL) && (ins->opcode <= OP_FCALL_MEMBASE)) || ((ins->opcode >= OP_LCALL) && (ins->opcode <= OP_LCALL_MEMBASE)) || ((ins->opcode >= OP_VCALL) && (ins->opcode <= OP_VCALL_MEMBASE)) || ((ins->opcode >= OP_CALL) && (ins->opcode <= OP_CALL_MEMBASE)) || ((ins->opcode >= OP_VCALL2) && (ins->opcode <= OP_VCALL2_MEMBASE)) || (ins->opcode == OP_TAILCALL))

#define MONO_IS_JUMP_TABLE(ins) (((ins)->opcode == OP_JUMP_TABLE) ? TRUE : ((((ins)->opcode == OP_AOTCONST) && (ins->inst_i1 == (gpointer)MONO_PATCH_INFO_SWITCH)) ? TRUE : ((ins)->opcode == OP_SWITCH) ? TRUE : ((((ins)->opcode == OP_GOT_ENTRY) && ((ins)->inst_right->inst_i1 == (gpointer)MONO_PATCH_INFO_SWITCH)) ? TRUE : FALSE)))

#define MONO_JUMP_TABLE_FROM_INS(ins) (((ins)->opcode == OP_JUMP_TABLE) ? (ins)->inst_p0 : (((ins)->opcode == OP_AOTCONST) && (ins->inst_i1 == (gpointer)MONO_PATCH_INFO_SWITCH) ? (ins)->inst_p0 : (((ins)->opcode == OP_SWITCH) ? (ins)->inst_p0 : ((((ins)->opcode == OP_GOT_ENTRY) && ((ins)->inst_right->inst_i1 == (gpointer)MONO_PATCH_INFO_SWITCH)) ? (ins)->inst_right->inst_p0 : NULL))))

/* FIXME: Add more instructions */
#define MONO_INS_HAS_NO_SIDE_EFFECT(ins) (MONO_IS_MOVE (ins) || (ins->opcode == OP_ICONST) || (ins->opcode == OP_I8CONST) || MONO_IS_ZERO (ins) || (ins->opcode == OP_ADD_IMM) || (ins->opcode == OP_R8CONST) || (ins->opcode == OP_LADD_IMM) || (ins->opcode == OP_ISUB_IMM) || (ins->opcode == OP_IADD_IMM) || (ins->opcode == OP_INEG) || (ins->opcode == OP_LNEG) || (ins->opcode == OP_ISUB) || (ins->opcode == OP_CMOV_IGE) || (ins->opcode == OP_ISHL_IMM) || (ins->opcode == OP_ISHR_IMM) || (ins->opcode == OP_ISHR_UN_IMM) || (ins->opcode == OP_IAND_IMM) || (ins->opcode == OP_ICONV_TO_U1) || (ins->opcode == OP_ICONV_TO_I1) || (ins->opcode == OP_SEXT_I4) || (ins->opcode == OP_LCONV_TO_U1) || (ins->opcode == OP_ICONV_TO_U2) || (ins->opcode == OP_ICONV_TO_I2) || (ins->opcode == OP_LCONV_TO_I2))

#define MONO_METHOD_IS_FINAL(m) (((m)->flags & METHOD_ATTRIBUTE_FINAL) || ((m)->klass && ((m)->klass->flags & TYPE_ATTRIBUTE_SEALED)))


#ifdef MONO_ARCH_SIMD_INTRINSICS

#define MONO_IS_PHI(ins) (((ins)->opcode == OP_PHI) || ((ins)->opcode == OP_FPHI) || ((ins)->opcode == OP_VPHI)  || ((ins)->opcode == OP_XPHI))
#define MONO_IS_MOVE(ins) (((ins)->opcode == OP_MOVE) || ((ins)->opcode == OP_FMOVE) || ((ins)->opcode == OP_VMOVE) || ((ins)->opcode == OP_XMOVE))
#define MONO_IS_NON_FP_MOVE(ins) (((ins)->opcode == OP_MOVE) || ((ins)->opcode == OP_VMOVE) || ((ins)->opcode == OP_XMOVE))
#define MONO_IS_REAL_MOVE(ins) (((ins)->opcode == OP_MOVE) || ((ins)->opcode == OP_FMOVE) || ((ins)->opcode == OP_XMOVE))
#define MONO_IS_ZERO(ins) (((ins)->opcode == OP_VZERO) || ((ins)->opcode == OP_XZERO))

#define MONO_CLASS_IS_SIMD(cfg, klass) (((cfg)->opt & MONO_OPT_SIMD) && (klass)->simd_type)

#else

#define MONO_IS_PHI(ins) (((ins)->opcode == OP_PHI) || ((ins)->opcode == OP_FPHI) || ((ins)->opcode == OP_VPHI))
#define MONO_IS_MOVE(ins) (((ins)->opcode == OP_MOVE) || ((ins)->opcode == OP_FMOVE) || ((ins)->opcode == OP_VMOVE))
#define MONO_IS_NON_FP_MOVE(ins) (((ins)->opcode == OP_MOVE) || ((ins)->opcode == OP_VMOVE))
/*A real MOVE is one that isn't decomposed such as a VMOVE or LMOVE*/
#define MONO_IS_REAL_MOVE(ins) (((ins)->opcode == OP_MOVE) || ((ins)->opcode == OP_FMOVE))
#define MONO_IS_ZERO(ins) ((ins)->opcode == OP_VZERO)

#define MONO_CLASS_IS_SIMD(cfg, klass) (0)

#endif

typedef struct MonoInstList MonoInstList;
typedef struct MonoInst MonoInst;
typedef struct MonoCallInst MonoCallInst;
typedef struct MonoCallArgParm MonoCallArgParm;
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
extern gboolean mono_compile_aot;
extern gboolean mono_aot_only;
extern gboolean mono_use_imt;
extern MonoMethodDesc *mono_inject_async_exc_method;
extern int mono_inject_async_exc_pos;
extern MonoMethodDesc *mono_break_at_bb_method;
extern int mono_break_at_bb_bb_num;
extern gboolean check_for_pending_exc;
extern gboolean disable_vtypes_in_regs;
extern gboolean mono_verify_all;
extern gboolean mono_dont_free_global_codeman;
extern gboolean mono_do_x86_stack_align;
extern const char *mono_build_date;
extern gboolean mono_do_signal_chaining;

#define INS_INFO(opcode) (&ins_info [((opcode) - OP_START - 1) * 4])

extern const char ins_info[];
extern const gint8 ins_sreg_counts [];

#define mono_inst_get_num_src_registers(ins) (ins_sreg_counts [(ins)->opcode - OP_START - 1])
#define mono_inst_get_src_registers(ins, regs) (((regs) [0] = (ins)->sreg1), ((regs) [1] = (ins)->sreg2), ((regs) [2] = (ins)->sreg3), mono_inst_get_num_src_registers ((ins)))

#define MONO_BB_FOR_EACH_INS(bb, ins) for ((ins) = (bb)->code; (ins); (ins) = (ins)->next)

#define MONO_BB_FOR_EACH_INS_SAFE(bb, n, ins) for ((ins) = (bb)->code, n = (ins) ? (ins)->next : NULL; (ins); (ins) = (n), (n) = (ins) ? (ins)->next : NULL)

#define MONO_BB_FOR_EACH_INS_REVERSE_SAFE(bb, p, ins) for ((ins) = (bb)->last_ins, p = (ins) ? (ins)->prev : NULL; (ins); (ins) = (p), (p) = (ins) ? (ins)->prev : NULL)

#define mono_bb_first_ins(bb) (bb)->code

struct MonoEdge {
	MonoEdge *next;
	MonoBasicBlock *bb;
	/* add edge type? */
};

struct MonoSpillInfo {
	int offset;
};

/*
 * The IR-level extended basic block.  
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

	/* the next basic block in the order it appears in IL */
	MonoBasicBlock *next_bb;

	/*
	 * Before instruction selection it is the first tree in the
	 * forest and the first item in the list of trees. After
	 * instruction selection it is the first instruction and the
	 * first item in the list of instructions.
	 */
	MonoInst *code;

	/* unique block number identification */
	gint32 block_num;
	
	gint32 dfn;

	/* Basic blocks: incoming and outgoing counts and pointers */
	/* Each bb should only appear once in each array */
	gint16 out_count, in_count;
	MonoBasicBlock **in_bb;
	MonoBasicBlock **out_bb;

	/* Points to the start of the CIL code that initiated this BB */
	unsigned char* cil_code;

	/* Length of the CIL block */
	gint32 cil_length;

	/* The address of the generated code, used for fixups */
	int native_offset;
	int max_offset;
	int max_length;

	/* Visited and reachable flags */
	guint32 flags;

	/*
	 * SSA and loop based flags
	 */
	MonoBitSet *dominators;
	MonoBitSet *dfrontier;
	MonoBasicBlock *idom;
	GSList *dominated;
	/* fast dominator algorithm */
	MonoBasicBlock *df_parent, *ancestor, *child, *label;
	MonoEdge *bucket;
	int size, sdom, idomn;
	
	/* loop nesting and recognition */
	GList *loop_blocks;
	gint8  nesting;
	gint8  loop_body_start;

	/* 
	 * Whenever the bblock is rarely executed so it should be emitted after
	 * the function epilog.
	 */
	guint out_of_line : 1;
	/* Caches the result of uselessness calculation during optimize_branches */
	guint not_useless : 1;
	/* Whenever the decompose_array_access_opts () pass needs to process this bblock */
	guint has_array_access : 1;
	/* Whenever this bblock is extended, ie. it has branches inside it */
	guint extended : 1;
	
	/* use for liveness analysis */
	MonoBitSet *gen_set;
	MonoBitSet *kill_set;
	MonoBitSet *live_in_set;
	MonoBitSet *live_out_set;

	/* fields to deal with non-empty stack slots at bb boundary */
	guint16 out_scount, in_scount;
	MonoInst **out_stack;
	MonoInst **in_stack;

	/* we use that to prevent merging of bblocks covered by different clauses*/
	guint real_offset;

	/*
	 * The region encodes whether the basic block is inside
	 * a finally, catch, filter or none of these.
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
	guint32 max_vreg;
};

/* BBlock flags */
enum {
	BB_VISITED            = 1 << 0,
	BB_REACHABLE          = 1 << 1,
	BB_EXCEPTION_DEAD_OBJ = 1 << 2,
	BB_EXCEPTION_UNSAFE   = 1 << 3,
	BB_EXCEPTION_HANDLER  = 1 << 4
};

typedef struct MonoMemcpyArgs {
	int size, align;
} MonoMemcpyArgs;

typedef enum {
	LLVMArgNone,
	LLVMArgInIReg,
	LLVMArgInFPReg,
	LLVMArgVtypeInReg,
	LLVMArgVtypeByVal,
	LLVMArgVtypeRetAddr /* On on cinfo->ret */
} LLVMArgStorage;

typedef struct {
	LLVMArgStorage storage;

	/* Only if storage == ArgValuetypeInReg */
	LLVMArgStorage pair_storage [2];
} LLVMArgInfo;

typedef struct {
	LLVMArgInfo ret;
	/* args [0] is for the this argument if it exists */
	LLVMArgInfo args [1];
} LLVMCallInfo;

#define MONO_MAX_SRC_REGS	3

struct MonoInst {
 	guint16 opcode;
	guint8  type; /* stack type */
	guint   ssa_op : 3;
	guint8  flags  : 5;
	
	/* used by the register allocator */
	gint32 dreg, sreg1, sreg2, sreg3;

	MonoInst *next, *prev;

	union {
		union {
			MonoInst *src;
			MonoMethodVar *var;
			mgreg_t const_val;
#if (SIZEOF_REGISTER > SIZEOF_VOID_P) && (G_BYTE_ORDER == G_BIG_ENDIAN)
			struct {
				gpointer p[SIZEOF_REGISTER/SIZEOF_VOID_P];
			} pdata;
#else
			gpointer p;
#endif
			MonoMethod *method;
			MonoMethodSignature *signature;
			MonoBasicBlock **many_blocks;
			MonoBasicBlock *target_block;
			MonoInst **args;
			MonoType *vtype;
			MonoClass *klass;
			int *phi_args;
			MonoCallInst *call_inst;
		} op [2];
		gint64 i8const;
		double r8const;
	} data;

	const unsigned char* cil_code; /* for debugging and bblock splitting */

	/* used mostly by the backend to store additional info it may need */
	union {
		gint32 reg3;
		gint32 arg_info;
		gint32 size;
		MonoMemcpyArgs *memcpy_args; /* in OP_MEMSET and OP_MEMCPY */
		gpointer data;
		gint shift_amount;
		gboolean is_pinvoke; /* for variables in the unmanaged marshal format */
		gboolean record_cast_details; /* For CEE_CASTCLASS */
		MonoInst *spill_var; /* for OP_ICONV_TO_R8_RAW and OP_FCONV_TO_R8_X */
		guint16 source_opcode; /*OP_XCONV_R8_TO_I4 needs to know which op was used to do proper widening*/
	} backend;
	
	MonoClass *klass;
};
	
struct MonoCallInst {
	MonoInst inst;
	MonoMethodSignature *signature;
	MonoMethod *method;
	MonoInst **args;
	MonoInst *out_args;
	MonoInst *vret_var;
	gconstpointer fptr;
	guint stack_usage;
	guint virtual : 1;
	guint tail_call : 1;
	/* If this is TRUE, 'fptr' points to a MonoJumpInfo instead of an address. */
	guint fptr_is_patch : 1;
	/*
	 * If this is true, then the call returns a vtype in a register using the same 
	 * calling convention as OP_CALL.
	 */
	guint vret_in_reg : 1;
	/* Whenever there is an IMT argument and it is dynamic */
	guint dynamic_imt_arg : 1;
	/* Whenever there is an RGCTX argument */
	guint32 rgctx_reg : 1;
	regmask_t used_iregs;
	regmask_t used_fregs;
	GSList *out_ireg_args;
	GSList *out_freg_args;
#ifdef ENABLE_LLVM
	LLVMCallInfo *cinfo;
#endif
};

struct MonoCallArgParm {
	MonoInst ins;
	gint32 size;
	gint32 offset;
	gint32 offPrm;
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
	MONO_INST_NOTYPECHECK    = 4,
	MONO_INST_UNALIGNED  = 8,
    MONO_INST_CFOLD_TAKEN = 8, /* On branches */
    MONO_INST_CFOLD_NOT_TAKEN = 16, /* On branches */
	MONO_INST_DEFINITION_HAS_SIDE_EFFECTS = 8,
	/* the address of the variable has been taken */
	MONO_INST_INDIRECT   = 16,
	MONO_INST_NORANGECHECK   = 16
};

#define inst_c0 data.op[0].const_val
#define inst_c1 data.op[1].const_val
#define inst_i0 data.op[0].src
#define inst_i1 data.op[1].src
#if (SIZEOF_REGISTER > SIZEOF_VOID_P) && (G_BYTE_ORDER == G_BIG_ENDIAN)
#define inst_p0 data.op[0].pdata.p[SIZEOF_REGISTER/SIZEOF_VOID_P - 1]
#define inst_p1 data.op[1].pdata.p[SIZEOF_REGISTER/SIZEOF_VOID_P - 1]
#else
#define inst_p0 data.op[0].p
#define inst_p1 data.op[1].p
#endif
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
#define inst_call   data.op[1].call_inst

#define inst_phi_args   data.op[1].phi_args

/* instruction description for use in regalloc/scheduling */
enum {
	MONO_INST_DEST,
	MONO_INST_SRC1,		/* we depend on the SRCs to be consecutive */
	MONO_INST_SRC2,
	MONO_INST_SRC3,
	MONO_INST_LEN,
	MONO_INST_CLOB,
	/* Unused, commented out to reduce the size of the mdesc tables
	MONO_INST_FLAGS,
	MONO_INST_COST,
	MONO_INST_DELAY,
	MONO_INST_RES,
	*/
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

typedef struct MonoLiveRange2 MonoLiveRange2;

struct MonoLiveRange2 {
	int from, to;
	MonoLiveRange2 *next;
};

typedef struct {
	/* List of live ranges sorted by 'from' */
	MonoLiveRange2 *range;
	MonoLiveRange2 *last_range;
} MonoLiveInterval;

/*
 * Additional information about a variable
 */
struct MonoMethodVar {
	guint           idx; /* inside cfg->varinfo, cfg->vars */
	MonoLiveRange   range; /* generated by liveness analysis */
	MonoLiveInterval *interval; /* generated by liveness analysis */
	int             reg; /* != -1 if allocated into a register */
	int             spill_costs;
	MonoBitSet     *def_in; /* used by SSA */
	MonoInst       *def;    /* used by SSA */
	MonoBasicBlock *def_bb; /* used by SSA */
	GList          *uses;   /* used by SSA */
	char            cpstate;  /* used by SSA conditional  constant propagation */
	/* The native offsets corresponding to the live range of the variable */
	gint32         live_range_start, live_range_end;
	/* 
	 * cfg->varinfo [idx]->dreg could be replaced for OP_REGVAR, this contains the 
	 * original vreg.
	 */
	gint32         vreg;
};

typedef struct {
	gpointer          end_of_stack;
	guint32           stack_size;
#if !defined(HAVE_KW_THREAD) || !defined(MONO_ARCH_ENABLE_MONO_LMF_VAR)
	MonoLMF          *lmf;
#endif
	MonoLMF          *first_lmf;
	gpointer         restore_stack_prot;
	guint32          handling_stack_ovf;
	gpointer         signal_stack;
	guint32          signal_stack_size;
	gpointer         stack_ovf_guard_base;
	guint32          stack_ovf_guard_size;
	void            (*abort_func) (MonoObject *object);
	/* Used to implement --debug=casts */
	MonoClass       *class_cast_from, *class_cast_to;
} MonoJitTlsData;

typedef enum {
#define PATCH_INFO(a,b) MONO_PATCH_INFO_ ## a,
#include "patch-info.h"
#undef PATCH_INFO
	MONO_PATCH_INFO_NUM
} MonoJumpInfoType;

/*
 * We need to store the image which the token refers to along with the token,
 * since the image might not be the same as the image of the method which
 * contains the relocation, because of inlining.
 */
typedef struct MonoJumpInfoToken {
	MonoImage *image;
	guint32 token;
	gboolean has_context;
	MonoGenericContext context;
} MonoJumpInfoToken;

typedef struct MonoJumpInfoBBTable {
	MonoBasicBlock **table;
	int table_size;
} MonoJumpInfoBBTable;

typedef struct MonoJumpInfoRgctxEntry MonoJumpInfoRgctxEntry;

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
		MonoJumpInfoRgctxEntry *rgctx_entry;
	} data;
};
 
/* Contains information describing an rgctx entry */
struct MonoJumpInfoRgctxEntry {
	MonoMethod *method;
	gboolean in_mrgctx;
	MonoJumpInfo *data; /* describes the data to be loaded */
	int info_type;
};

typedef enum {
	MONO_TRAMPOLINE_JIT,
	MONO_TRAMPOLINE_JUMP,
	MONO_TRAMPOLINE_CLASS_INIT,
	MONO_TRAMPOLINE_GENERIC_CLASS_INIT,
	MONO_TRAMPOLINE_RGCTX_LAZY_FETCH,
	MONO_TRAMPOLINE_AOT,
	MONO_TRAMPOLINE_AOT_PLT,
	MONO_TRAMPOLINE_DELEGATE,
	MONO_TRAMPOLINE_RESTORE_STACK_PROT,
	MONO_TRAMPOLINE_GENERIC_VIRTUAL_REMOTING,
	MONO_TRAMPOLINE_MONITOR_ENTER,
	MONO_TRAMPOLINE_MONITOR_EXIT,
#ifdef ENABLE_LLVM
	MONO_TRAMPOLINE_LLVM_VCALL,
#endif
	MONO_TRAMPOLINE_NUM
} MonoTrampolineType;

#define MONO_TRAMPOLINE_TYPE_MUST_RETURN(t)		\
	((t) == MONO_TRAMPOLINE_CLASS_INIT ||		\
	 (t) == MONO_TRAMPOLINE_GENERIC_CLASS_INIT ||	\
	 (t) == MONO_TRAMPOLINE_RESTORE_STACK_PROT ||	\
	 (t) == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH ||	\
	 (t) == MONO_TRAMPOLINE_MONITOR_ENTER ||	\
	 (t) == MONO_TRAMPOLINE_MONITOR_EXIT)

/* optimization flags */
#define OPTFLAG(id,shift,name,descr) MONO_OPT_ ## id = 1 << shift,
enum {
#include "optflags-def.h"
	MONO_OPT_LAST
};

/* Bit-fields in the MonoBasicBlock.region */
#define MONO_REGION_TRY       0
#define MONO_REGION_FINALLY  16
#define MONO_REGION_CATCH    32
#define MONO_REGION_FAULT    64         /* Currently unused */
#define MONO_REGION_FILTER  128

#define MONO_BBLOCK_IS_IN_REGION(bblock, regtype) (((bblock)->region & (0xf << 4)) == (regtype))

#define get_vreg_to_inst(cfg, vreg) ((vreg) < (cfg)->vreg_to_inst_len ? (cfg)->vreg_to_inst [(vreg)] : NULL)

#define vreg_is_volatile(cfg, vreg) (G_UNLIKELY (get_vreg_to_inst ((cfg), (vreg)) && (get_vreg_to_inst ((cfg), (vreg))->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))))

/*
 * Control Flow Graph and compilation unit information
 */
typedef struct {
	MonoMethod      *method;
	MonoMemPool     *mempool;
	MonoInst       **varinfo;
	MonoMethodVar   *vars;
	MonoInst        *ret;
	MonoBasicBlock  *bb_entry;
	MonoBasicBlock  *bb_exit;
	MonoBasicBlock  *bb_init;
	MonoBasicBlock **bblocks;
	MonoBasicBlock **cil_offset_to_bb;
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
	gint             cil_offset_to_bb_len;
	gint             locals_min_stack_offset, locals_max_stack_offset;
	MonoRegState    *rs;
	MonoSpillInfo   *spill_info [16]; /* machine register spills */
	gint             spill_count;
	gint             spill_info_len [16];
	/* unsigned char   *cil_code; */
	MonoMethod      *inlined_method; /* the method which is currently inlined */
	MonoInst        *domainvar; /* a cache for the current domain */
	MonoInst        *got_var; /* Global Offset Table variable */
	MonoInst        **locals;
	MonoInst	*rgctx_var; /* Runtime generic context variable (for static generic methods) */
	MonoInst        **args;
	MonoType        **arg_types;
	MonoMethod      *current_method; /* The method currently processed by method_to_ir () */
	MonoGenericContext *generic_context;

	/* 
	 * This variable represents the hidden argument holding the vtype
	 * return address. If the method returns something other than a vtype, or
	 * the vtype is returned in registers this is NULL.
	 */
	MonoInst        *vret_addr;

	/*
	 * This is used to initialize the cil_code field of MonoInst's.
	 */
	const unsigned char *ip;
	
	struct MonoAliasingInformation *aliasing_info;

	/* A hashtable of region ID-> SP var mappings */
	/* An SP var is a place to store the stack pointer (used by handlers)*/
	GHashTable      *spvars;

	/* A hashtable of region ID -> EX var mappings */
	/* An EX var stores the exception object passed to catch/filter blocks */
	GHashTable      *exvars;

	GList           *ldstr_list; /* used by AOT */
	
	MonoDomain      *domain;

	guint            real_offset;
	GHashTable      *cbb_hash;

	/* The current virtual register number */
	guint32 next_vreg;

	MonoGenericSharingContext *generic_sharing_context;

	unsigned char   *cil_start;
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
	guint            disable_aot : 1;
	guint            disable_ssa : 1;
	guint            disable_llvm : 1;
	guint            enable_extended_bblocks : 1;
	guint            run_cctors : 1;
	guint            need_lmf_area : 1;
	guint            compile_aot : 1;
	guint            compile_llvm : 1;
	guint            got_var_allocated : 1;
	guint            ret_var_is_local : 1;
	guint            ret_var_set : 1;
	guint            globalra : 1;
	guint            unverifiable : 1;
	guint            skip_visibility : 1;
	guint            disable_reuse_registers : 1;
	guint            disable_reuse_stack_slots : 1;
	guint            disable_initlocals_opt : 1;
	guint            disable_omit_fp : 1;
	guint            disable_vreg_to_lvreg : 1;
	guint            disable_deadce_vars : 1;
	guint            extend_live_ranges : 1;
	guint            has_got_slots : 1;
	guint            uses_rgctx_reg : 1;
	guint            uses_vtable_reg : 1;
	guint            uses_simd_intrinsics : 1;
	guint            keep_cil_nops : 1;
	gpointer         debug_info;
	guint32          lmf_offset;
    guint16          *intvars;
	MonoProfileCoverageInfo *coverage_info;
	GHashTable       *token_info_hash;
	MonoCompileArch  arch;
	guint32          exception_type;	/* MONO_EXCEPTION_* */
	guint32          exception_data;
	char*            exception_message;
	gpointer         exception_ptr;

	guint8 *         encoded_unwind_ops;
	guint32          encoded_unwind_ops_len;
	GSList*          unwind_ops;

	/* Fields used by the local reg allocator */
	void*            reginfo;
	int              reginfo_len;

	/* Maps vregs to their associated MonoInst's */
	/* vregs with an associated MonoInst are 'global' while others are 'local' */
	MonoInst **vreg_to_inst;

	/* Size of above array */
	guint32 vreg_to_inst_len;

	/* 
	 * The original method to compile, differs from 'method' when doing generic
	 * sharing.
	 */
	MonoMethod *orig_method;

	/* Patches which describe absolute addresses embedded into the native code */
	GHashTable *abs_patches;

	/* If the arch passes valuetypes by address, then for methods
	   which use JMP the arch code should use these local
	   variables to store the addresses of incoming valuetypes.
	   The addresses should be stored in mono_arch_emit_prolog()
	   and can be used when emitting code for OP_JMP.  See
	   mini-ppc.c. */
	MonoInst **tailcall_valuetype_addrs;

	/* Used to implement iconv_to_r8_raw on archs that can't do raw
	copy between an ireg and a freg. This is an int32 var.*/
	MonoInst *iconv_raw_var;

	/* Used to implement fconv_to_r8_x. This is a double (8 bytes) var.*/
	MonoInst *fconv_to_r8_x_var;

	/*Use to implement simd constructors. This is a vector (16 bytes) var.*/
	MonoInst *simd_ctor_var;

	/* Used by AOT */
	guint32 got_offset;
} MonoCompile;

typedef enum {
	MONO_CFG_HAS_ALLOCA = 1 << 0,
	MONO_CFG_HAS_CALLS  = 1 << 1,
	MONO_CFG_HAS_LDELEMA  = 1 << 2,
	MONO_CFG_HAS_VARARGS  = 1 << 3,
	MONO_CFG_HAS_TAIL     = 1 << 4,
	MONO_CFG_HAS_FPOUT    = 1 << 5, /* there are fp values passed in int registers */
	MONO_CFG_HAS_SPILLUP  = 1 << 6, /* spill var slots are allocated from bottom to top */
	MONO_CFG_HAS_CHECK_THIS  = 1 << 7,
	MONO_CFG_HAS_ARRAY_ACCESS = 1 << 8
} MonoCompileFlags;

typedef struct {
	gulong methods_compiled;
	gulong methods_aot;
	gulong methods_lookups;
	gulong method_trampolines;
	gulong allocate_var;
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
	gulong locals_stack_size;
	gulong regvars;
	gulong cas_declsec_check;
	gulong cas_linkdemand_icall;
	gulong cas_linkdemand_pinvoke;
	gulong cas_linkdemand_aptc;
	gulong cas_linkdemand;
	gulong cas_demand_generation;
	gulong generic_virtual_invocations;
	char *max_ratio_method;
	char *biggest_method;
	gboolean enabled;
} MonoJitStats;

extern MonoJitStats mono_jit_stats;

/* values for MonoInst.ssa_op */
enum {
	MONO_SSA_NOP = 0,
	MONO_SSA_ADDRESS_TAKEN = 1,
	MONO_SSA_LOAD = 2,
	MONO_SSA_STORE = 4,
	MONO_SSA_LOAD_STORE = MONO_SSA_LOAD|MONO_SSA_STORE,
	MONO_SSA_INDIRECT_LOAD = MONO_SSA_LOAD|MONO_SSA_ADDRESS_TAKEN,
	MONO_SSA_INDIRECT_STORE = MONO_SSA_STORE|MONO_SSA_ADDRESS_TAKEN,
	MONO_SSA_INDIRECT_LOAD_STORE =
	MONO_SSA_LOAD|MONO_SSA_STORE|MONO_SSA_ADDRESS_TAKEN
};

/* opcodes: value assigned after all the CIL opcodes */
#ifdef MINI_OP
#undef MINI_OP
#endif
#ifdef MINI_OP3
#undef MINI_OP3
#endif
#define MINI_OP(a,b,dest,src1,src2) a,
#define MINI_OP3(a,b,dest,src1,src2,src3) a,
enum {
	OP_START = MONO_CEE_LAST - 1,
#include "mini-ops.h"
	OP_LAST
};
#undef MINI_OP
#undef MINI_OP3

#if SIZEOF_VOID_P == 8
#define OP_PCONST OP_I8CONST
#define OP_PADD OP_LADD
#define OP_PADD_IMM OP_LADD_IMM
#define OP_PSUB OP_LSUB
#define OP_PMUL OP_LMUL
#define OP_PMUL_IMM OP_LMUL_IMM
#define OP_PNEG OP_LNEG
#define OP_PCONV_TO_I1 OP_LCONV_TO_I1
#define OP_PCONV_TO_U1 OP_LCONV_TO_U1
#define OP_PCONV_TO_I2 OP_LCONV_TO_I2
#define OP_PCONV_TO_U2 OP_LCONV_TO_U2
#define OP_PCONV_TO_OVF_I1_UN OP_LCONV_TO_OVF_I1_UN
#define OP_PCONV_TO_OVF_I1 OP_LCONV_TO_OVF_I1
#define OP_PBEQ OP_LBEQ
#define OP_PCEQ OP_LCEQ
#define OP_PBNE_UN OP_LBNE_UN
#define OP_PBGE_UN OP_LBGE_UN
#define OP_PBLT_UN OP_LBLT_UN
#define OP_PBGE OP_LBGE
#define OP_STOREP_MEMBASE_REG OP_STOREI8_MEMBASE_REG
#define OP_STOREP_MEMBASE_IMM OP_STOREI8_MEMBASE_IMM
#else
#define OP_PCONST OP_ICONST
#define OP_PADD OP_IADD
#define OP_PADD_IMM OP_IADD_IMM
#define OP_PSUB OP_ISUB
#define OP_PMUL OP_IMUL
#define OP_PMUL_IMM OP_IMUL_IMM
#define OP_PNEG OP_INEG
#define OP_PCONV_TO_I1 OP_ICONV_TO_I1
#define OP_PCONV_TO_U1 OP_ICONV_TO_U1
#define OP_PCONV_TO_I2 OP_ICONV_TO_I2
#define OP_PCONV_TO_U2 OP_ICONV_TO_U2
#define OP_PCONV_TO_OVF_I1_UN OP_ICONV_TO_OVF_I1_UN
#define OP_PCONV_TO_OVF_I1 OP_ICONV_TO_OVF_I1
#define OP_PBEQ OP_IBEQ
#define OP_PCEQ OP_ICEQ
#define OP_PBNE_UN OP_IBNE_UN
#define OP_PBGE_UN OP_IBGE_UN
#define OP_PBLT_UN OP_IBLT_UN
#define OP_PBGE OP_IBGE
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

#if HAVE_ARRAY_ELEM_INIT
extern const guint8 mono_burg_arity [];
#else
extern guint8 mono_burg_arity [];
#endif

extern const char MONO_ARCH_CPU_SPEC [] MONO_INTERNAL;
#define MONO_ARCH_CPU_SPEC_IDX_COMBINE(a) a ## _idx
#define MONO_ARCH_CPU_SPEC_IDX(a) MONO_ARCH_CPU_SPEC_IDX_COMBINE(a)
extern const guint16 MONO_ARCH_CPU_SPEC_IDX(MONO_ARCH_CPU_SPEC) [] MONO_INTERNAL;
#define ins_get_spec(op) ((const char*)&MONO_ARCH_CPU_SPEC + MONO_ARCH_CPU_SPEC_IDX(MONO_ARCH_CPU_SPEC)[(op) - OP_LOAD])

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

typedef struct {
	gboolean handle_sigint;
	gboolean keep_delegates;
	gboolean collect_pagefault_stats;
	gboolean break_on_unverified;
	gboolean better_cast_details;
	gboolean mdb_optimizations;
	gboolean no_gdb_backtrace;
	gboolean suspend_on_sigsegv;
} MonoDebugOptions;

enum {
	BRANCH_NOT_TAKEN,
	BRANCH_TAKEN,
	BRANCH_UNDEF
};

typedef enum {
	CMP_EQ,
	CMP_NE,
	CMP_LE,
	CMP_GE,
	CMP_LT,
	CMP_GT,
	CMP_LE_UN,
	CMP_GE_UN,
	CMP_LT_UN,
	CMP_GT_UN
} CompRelation;

typedef enum {
	CMP_TYPE_L,
	CMP_TYPE_I,
	CMP_TYPE_F
} CompType;

/* Implicit exceptions */
enum {
	MONO_EXC_INDEX_OUT_OF_RANGE,
	MONO_EXC_OVERFLOW,
	MONO_EXC_ARITHMETIC,
	MONO_EXC_DIVIDE_BY_ZERO,
	MONO_EXC_INVALID_CAST,
	MONO_EXC_NULL_REF,
	MONO_EXC_ARRAY_TYPE_MISMATCH,
	MONO_EXC_INTRINS_NUM
};

enum {
	MINI_TOKEN_SOURCE_CLASS,
	MINI_TOKEN_SOURCE_METHOD,
	MINI_TOKEN_SOURCE_FIELD
};

/* 
 * This structures contains information about a trampoline function required by
 * the AOT compiler in full-aot mode.
 */
typedef struct
{
	guint8 *code;
	guint32 code_size;
	char *name;
} MonoAotTrampInfo;

typedef void (*MonoInstFunc) (MonoInst *tree, gpointer data);

/* main function */
int         mono_main                      (int argc, char* argv[]);
void        mono_set_defaults              (int verbose_level, guint32 opts);
MonoDomain* mini_init                      (const char *filename, const char *runtime_version) MONO_INTERNAL;
void        mini_cleanup                   (MonoDomain *domain) MONO_INTERNAL;
MonoDebugOptions *mini_get_debug_options   (void) MONO_INTERNAL;
char*       mono_get_runtime_build_info    (void) MONO_INTERNAL;

/* helper methods */
MonoJumpInfoToken* mono_jump_info_token_new (MonoMemPool *mp, MonoImage *image, guint32 token) MONO_INTERNAL;
MonoJumpInfoToken* mono_jump_info_token_new2 (MonoMemPool *mp, MonoImage *image, guint32 token, MonoGenericContext *context) MONO_INTERNAL;
MonoInst* mono_find_spvar_for_region        (MonoCompile *cfg, int region) MONO_INTERNAL;
void      mono_precompile_assemblies        (void) MONO_INTERNAL;
int       mono_parse_default_optimizations  (const char* p);
void      mono_bblock_add_inst              (MonoBasicBlock *bb, MonoInst *inst) MONO_INTERNAL;
void      mono_bblock_insert_after_ins      (MonoBasicBlock *bb, MonoInst *ins, MonoInst *ins_to_insert) MONO_INTERNAL;
void      mono_bblock_insert_before_ins     (MonoBasicBlock *bb, MonoInst *ins, MonoInst *ins_to_insert) MONO_INTERNAL;
void      mono_verify_bblock                (MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_verify_cfg                   (MonoCompile *cfg) MONO_INTERNAL;
void      mono_constant_fold                (MonoCompile *cfg) MONO_INTERNAL;
MonoInst* mono_constant_fold_ins            (MonoCompile *cfg, MonoInst *ins, MonoInst *arg1, MonoInst *arg2, gboolean overwrite) MONO_INTERNAL;
int       mono_eval_cond_branch             (MonoInst *branch) MONO_INTERNAL;
int       mono_is_power_of_two              (guint32 val) MONO_INTERNAL;
void      mono_cprop_local                  (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **acp, int acp_size) MONO_INTERNAL;
MonoInst* mono_compile_create_var           (MonoCompile *cfg, MonoType *type, int opcode) MONO_INTERNAL;
MonoInst* mono_compile_create_var_for_vreg  (MonoCompile *cfg, MonoType *type, int opcode, int vreg) MONO_INTERNAL;
void      mono_compile_make_var_load        (MonoCompile *cfg, MonoInst *dest, gssize var_index) MONO_INTERNAL;
MonoInst* mono_compile_create_var_load      (MonoCompile *cfg, gssize var_index) MONO_INTERNAL;
MonoInst* mono_compile_create_var_store     (MonoCompile *cfg, gssize var_index, MonoInst *value) MONO_INTERNAL;
MonoType* mono_type_from_stack_type         (MonoInst *ins) MONO_INTERNAL;
guint32   mono_alloc_ireg                   (MonoCompile *cfg) MONO_INTERNAL;
guint32   mono_alloc_freg                   (MonoCompile *cfg) MONO_INTERNAL;
guint32   mono_alloc_preg                   (MonoCompile *cfg) MONO_INTERNAL;
guint32   mono_alloc_dreg                   (MonoCompile *cfg, MonoStackType stack_type) MONO_INTERNAL;

void      mono_link_bblock                  (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to) MONO_INTERNAL;
void      mono_unlink_bblock                (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to) MONO_INTERNAL;
gboolean  mono_bblocks_linked               (MonoBasicBlock *bb1, MonoBasicBlock *bb2) MONO_INTERNAL;
void      mono_remove_bblock                (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_nullify_basic_block          (MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_merge_basic_blocks           (MonoCompile *cfg, MonoBasicBlock *bb, MonoBasicBlock *bbn) MONO_INTERNAL;
void      mono_optimize_branches            (MonoCompile *cfg) MONO_INTERNAL;

void      mono_blockset_print               (MonoCompile *cfg, MonoBitSet *set, const char *name, guint idom) MONO_INTERNAL;
void      mono_print_ins_index              (int i, MonoInst *ins) MONO_INTERNAL;
void      mono_print_ins                    (MonoInst *ins) MONO_INTERNAL;
void      mono_print_bb                     (MonoBasicBlock *bb, const char *msg) MONO_INTERNAL;
void      mono_print_code                   (MonoCompile *cfg, const char *msg) MONO_INTERNAL;
void      mono_print_method_from_ip         (void *ip);
char     *mono_pmip                         (void *ip);
const char* mono_inst_name                  (int op);
void      mono_inst_set_src_registers       (MonoInst *ins, int *regs) MONO_INTERNAL;
int       mono_op_to_op_imm                 (int opcode) MONO_INTERNAL;
int       mono_op_imm_to_op                 (int opcode) MONO_INTERNAL;
int       mono_load_membase_to_load_mem     (int opcode) MONO_INTERNAL;
guint     mono_type_to_load_membase         (MonoCompile *cfg, MonoType *type) MONO_INTERNAL;
guint     mono_type_to_store_membase        (MonoCompile *cfg, MonoType *type) MONO_INTERNAL;
guint     mini_type_to_stind                (MonoCompile* cfg, MonoType *type) MONO_INTERNAL;
guint32   mono_reverse_branch_op            (guint32 opcode) MONO_INTERNAL;
void      mono_disassemble_code             (MonoCompile *cfg, guint8 *code, int size, char *id) MONO_INTERNAL;
void      mono_add_patch_info               (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target) MONO_INTERNAL;
void      mono_remove_patch_info            (MonoCompile *cfg, int ip) MONO_INTERNAL;
MonoJumpInfo* mono_patch_info_dup_mp        (MonoMemPool *mp, MonoJumpInfo *patch_info) MONO_INTERNAL;
guint     mono_patch_info_hash (gconstpointer data) MONO_INTERNAL;
gint      mono_patch_info_equal (gconstpointer ka, gconstpointer kb) MONO_INTERNAL;
MonoJumpInfo *mono_patch_info_list_prepend  (MonoJumpInfo *list, int ip, MonoJumpInfoType type, gconstpointer target) MONO_INTERNAL;
gpointer  mono_resolve_patch_target         (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *patch_info, gboolean run_cctors) MONO_INTERNAL;
gpointer  mono_jit_find_compiled_method_with_jit_info (MonoDomain *domain, MonoMethod *method, MonoJitInfo **ji) MONO_INTERNAL;
gpointer  mono_jit_find_compiled_method     (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;
gpointer  mono_jit_compile_method           (MonoMethod *method) MONO_INTERNAL;
MonoLMF * mono_get_lmf                      (void) MONO_INTERNAL;
MonoLMF** mono_get_lmf_addr                 (void) MONO_INTERNAL;
void      mono_jit_thread_attach            (MonoDomain *domain);
guint32   mono_get_jit_tls_key              (void) MONO_INTERNAL;
gint32    mono_get_jit_tls_offset           (void) MONO_INTERNAL;
gint32    mono_get_lmf_tls_offset           (void) MONO_INTERNAL;
gint32    mono_get_lmf_addr_tls_offset      (void) MONO_INTERNAL;
MonoInst* mono_get_jit_tls_intrinsic        (MonoCompile *cfg) MONO_INTERNAL;
MonoInst* mono_get_domain_intrinsic         (MonoCompile* cfg) MONO_INTERNAL;
MonoInst* mono_get_thread_intrinsic         (MonoCompile* cfg) MONO_INTERNAL;
GList    *mono_varlist_insert_sorted        (MonoCompile *cfg, GList *list, MonoMethodVar *mv, gboolean sort_end) MONO_INTERNAL;
GList    *mono_varlist_sort                 (MonoCompile *cfg, GList *list, int sort_type) MONO_INTERNAL;
void      mono_analyze_liveness             (MonoCompile *cfg) MONO_INTERNAL;
void      mono_linear_scan                  (MonoCompile *cfg, GList *vars, GList *regs, regmask_t *used_mask) MONO_INTERNAL;
void      mono_global_regalloc              (MonoCompile *cfg) MONO_INTERNAL;
void      mono_create_jump_table            (MonoCompile *cfg, MonoInst *label, MonoBasicBlock **bbs, int num_blocks) MONO_INTERNAL;
int       mono_compile_assembly             (MonoAssembly *ass, guint32 opts, const char *aot_options) MONO_INTERNAL;
MonoCompile *mini_method_compile            (MonoMethod *method, guint32 opts, MonoDomain *domain, gboolean run_cctors, gboolean compile_aot, int parts) MONO_INTERNAL;
void      mono_destroy_compile              (MonoCompile *cfg) MONO_INTERNAL;
MonoJitICallInfo *mono_find_jit_opcode_emulation (int opcode) MONO_INTERNAL;
void	  mono_print_ins_index (int i, MonoInst *ins) MONO_INTERNAL;
void	  mono_print_ins (MonoInst *ins) MONO_INTERNAL;
gboolean  mini_assembly_can_skip_verification (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;
gboolean  mini_method_verify (MonoCompile *cfg, MonoMethod *method) MONO_INTERNAL;

gboolean  mini_class_is_system_array (MonoClass *klass) MONO_INTERNAL;
MonoMethodSignature *mono_get_element_address_signature (int arity) MONO_INTERNAL;
MonoJitICallInfo    *mono_get_element_address_icall (int rank) MONO_INTERNAL;
MonoJitICallInfo    *mono_get_array_new_va_icall (int rank) MONO_INTERNAL;

void      mono_linterval_add_range          (MonoCompile *cfg, MonoLiveInterval *interval, int from, int to) MONO_INTERNAL;
void      mono_linterval_print              (MonoLiveInterval *interval) MONO_INTERNAL;
void      mono_linterval_print_nl (MonoLiveInterval *interval) MONO_INTERNAL;
gboolean  mono_linterval_covers             (MonoLiveInterval *interval, int pos) MONO_INTERNAL;
gint32    mono_linterval_get_intersect_pos  (MonoLiveInterval *i1, MonoLiveInterval *i2) MONO_INTERNAL;
void      mono_linterval_split              (MonoCompile *cfg, MonoLiveInterval *interval, MonoLiveInterval **i1, MonoLiveInterval **i2, int pos) MONO_INTERNAL;
void      mono_liveness_handle_exception_clauses (MonoCompile *cfg) MONO_INTERNAL;

/* AOT */
void      mono_aot_init                     (void) MONO_INTERNAL;
gpointer  mono_aot_get_method               (MonoDomain *domain,
											 MonoMethod *method) MONO_INTERNAL;
gpointer  mono_aot_get_method_from_token    (MonoDomain *domain, MonoImage *image, guint32 token) MONO_INTERNAL;
gboolean  mono_aot_is_got_entry             (guint8 *code, guint8 *addr) MONO_INTERNAL;
guint8*   mono_aot_get_plt_entry            (guint8 *code) MONO_INTERNAL;
guint32   mono_aot_get_plt_info_offset      (gssize *regs, guint8 *code) MONO_INTERNAL;
gboolean  mono_aot_get_cached_class_info    (MonoClass *klass, MonoCachedClassInfo *res) MONO_INTERNAL;
gboolean  mono_aot_get_class_from_name      (MonoImage *image, const char *name_space, const char *name, MonoClass **klass) MONO_INTERNAL;
MonoJitInfo* mono_aot_find_jit_info         (MonoDomain *domain, MonoImage *image, gpointer addr) MONO_INTERNAL;
gpointer mono_aot_plt_resolve               (gpointer aot_module, guint32 plt_info_offset, guint8 *code) MONO_INTERNAL;
gpointer mono_aot_get_method_from_vt_slot   (MonoDomain *domain, MonoVTable *vtable, int slot) MONO_INTERNAL;
gpointer mono_aot_create_specific_trampoline   (MonoImage *image, gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len) MONO_INTERNAL;
gpointer mono_aot_get_named_code            (const char *name) MONO_INTERNAL;
gpointer mono_aot_get_unbox_trampoline      (MonoMethod *method) MONO_INTERNAL;
gpointer mono_aot_get_lazy_fetch_trampoline (guint32 slot) MONO_INTERNAL;
gpointer mono_aot_get_static_rgctx_trampoline (gpointer ctx, gpointer addr) MONO_INTERNAL;
gpointer mono_aot_get_imt_thunk             (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp) MONO_INTERNAL;
guint8*  mono_aot_get_unwind_info           (MonoJitInfo *ji, guint32 *unwind_info_len) MONO_INTERNAL;
guint32  mono_aot_method_hash               (MonoMethod *method) MONO_INTERNAL;
char*    mono_aot_wrapper_name              (MonoMethod *method) MONO_INTERNAL;
MonoAotTrampInfo* mono_aot_tramp_info_create (const char *name, guint8 *code, guint32 code_len) MONO_INTERNAL;
guint    mono_aot_str_hash                  (gconstpointer v1) MONO_INTERNAL;

/* This is an exported function */
void     mono_aot_register_globals          (gpointer *globals);
/* This too */
void     mono_aot_register_module           (gpointer *aot_info);

void     mono_xdebug_init                   (void) MONO_INTERNAL;
void     mono_save_xdebug_info              (MonoCompile *cfg) MONO_INTERNAL;
void     mono_save_trampoline_xdebug_info   (const char *tramp_name, guint8 *code, guint32 code_size, GSList *unwind_info) MONO_INTERNAL;
/* This is an exported function */
void     mono_xdebug_emit                   (void) MONO_INTERNAL;

/* LLVM backend */
void     mono_llvm_init                     (void) MONO_INTERNAL;
void     mono_llvm_cleanup                  (void) MONO_INTERNAL;
void     mono_llvm_emit_method              (MonoCompile *cfg) MONO_INTERNAL;
void     mono_llvm_emit_call                (MonoCompile *cfg, MonoCallInst *call) MONO_INTERNAL;

gboolean  mono_method_blittable             (MonoMethod *method) MONO_INTERNAL;
gboolean  mono_method_same_domain           (MonoJitInfo *caller, MonoJitInfo *callee) MONO_INTERNAL;

void      mono_register_opcode_emulation    (int opcode, const char* name, const char *sigstr, gpointer func, gboolean no_throw) MONO_INTERNAL;
void      mono_draw_graph                   (MonoCompile *cfg, MonoGraphOptions draw_options) MONO_INTERNAL;
void      mono_add_ins_to_end               (MonoBasicBlock *bb, MonoInst *inst) MONO_INTERNAL;
gpointer  mono_create_ftnptr                (MonoDomain *domain, gpointer addr) MONO_INTERNAL;

void      mono_replace_ins                  (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, MonoInst **prev, MonoBasicBlock *first_bb, MonoBasicBlock *last_bb);

int               mono_find_method_opcode      (MonoMethod *method) MONO_INTERNAL;
MonoJitICallInfo *mono_find_jit_icall_by_name  (const char *name) MONO_INTERNAL;
MonoJitICallInfo *mono_find_jit_icall_by_addr  (gconstpointer addr) MONO_INTERNAL;
MonoJitICallInfo *mono_register_jit_icall      (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save) MONO_INTERNAL;
gconstpointer     mono_icall_get_wrapper       (MonoJitICallInfo* callinfo) MONO_INTERNAL;

void              mono_trampolines_init (void) MONO_INTERNAL;
void              mono_trampolines_cleanup (void) MONO_INTERNAL;
guint8 *          mono_get_trampoline_code (MonoTrampolineType tramp_type) MONO_INTERNAL;
gpointer          mono_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len) MONO_INTERNAL;
gpointer          mono_create_jump_trampoline (MonoDomain *domain, 
											   MonoMethod *method, 
											   gboolean add_sync_wrapper) MONO_INTERNAL;
gpointer          mono_create_class_init_trampoline (MonoVTable *vtable) MONO_INTERNAL;
gpointer          mono_create_generic_class_init_trampoline (void) MONO_INTERNAL;
gpointer          mono_create_jit_trampoline (MonoMethod *method) MONO_INTERNAL;
gpointer          mono_create_jit_trampoline_from_token (MonoImage *image, guint32 token) MONO_INTERNAL;
gpointer          mono_create_jit_trampoline_in_domain (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;
gpointer          mono_create_delegate_trampoline (MonoClass *klass) MONO_INTERNAL;
gpointer          mono_create_rgctx_lazy_fetch_trampoline (guint32 offset) MONO_INTERNAL;
gpointer          mono_create_monitor_enter_trampoline (void) MONO_INTERNAL;
gpointer          mono_create_monitor_exit_trampoline (void) MONO_INTERNAL;
gpointer          mono_create_static_rgctx_trampoline (MonoMethod *m, gpointer addr) MONO_INTERNAL;
gpointer          mono_create_llvm_vcall_trampoline (MonoMethod *method) MONO_INTERNAL;
MonoVTable*       mono_find_class_init_trampoline_by_addr (gconstpointer addr) MONO_INTERNAL;
guint32           mono_find_rgctx_lazy_fetch_trampoline_by_addr (gconstpointer addr) MONO_INTERNAL;
gpointer          mono_magic_trampoline (gssize *regs, guint8 *code, MonoMethod *m, guint8* tramp) MONO_INTERNAL;
gpointer          mono_generic_virtual_remoting_trampoline (gssize *regs, guint8 *code, MonoMethod *m, guint8 *tramp) MONO_INTERNAL;
gpointer          mono_delegate_trampoline (gssize *regs, guint8 *code, gpointer *tramp_data, guint8* tramp) MONO_INTERNAL;
gpointer          mono_aot_trampoline (gssize *regs, guint8 *code, guint8 *token_info, 
									   guint8* tramp) MONO_INTERNAL;
gpointer          mono_aot_plt_trampoline (gssize *regs, guint8 *code, guint8 *token_info, 
										   guint8* tramp) MONO_INTERNAL;
void              mono_class_init_trampoline (gssize *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp) MONO_INTERNAL;
void              mono_generic_class_init_trampoline (gssize *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp) MONO_INTERNAL;
void              mono_monitor_enter_trampoline (gssize *regs, guint8 *code, MonoObject *obj, guint8 *tramp) MONO_INTERNAL;
void              mono_monitor_exit_trampoline (gssize *regs, guint8 *code, MonoObject *obj, guint8 *tramp) MONO_INTERNAL;
gconstpointer     mono_get_trampoline_func (MonoTrampolineType tramp_type);
gpointer          mini_get_vtable_trampoline (void) MONO_INTERNAL;
gpointer*         mono_get_vcall_slot_addr (guint8* code, gpointer *regs) MONO_INTERNAL;

gboolean          mono_running_on_valgrind (void) MONO_INTERNAL;
void*             mono_global_codeman_reserve (int size) MONO_INTERNAL;
const char       *mono_regname_full (int reg, int bank) MONO_INTERNAL;
gint32*           mono_allocate_stack_slots_full (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align) MONO_INTERNAL;
gint32*           mono_allocate_stack_slots (MonoCompile *cfg, guint32 *stack_size, guint32 *stack_align) MONO_INTERNAL;
void              mono_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
MonoInst         *mono_branch_optimize_exception_target (MonoCompile *cfg, MonoBasicBlock *bb, const char * exname) MONO_INTERNAL;
void              mono_remove_critical_edges (MonoCompile *cfg) MONO_INTERNAL;
gboolean          mono_is_regsize_var (MonoType *t) MONO_INTERNAL;
void              mini_emit_memcpy (MonoCompile *cfg, int destreg, int doffset, int srcreg, int soffset, int size, int align) MONO_INTERNAL;
CompRelation      mono_opcode_to_cond (int opcode) MONO_INTERNAL;
CompType          mono_opcode_to_type (int opcode, int cmp_opcode) MONO_INTERNAL;
CompRelation      mono_negate_cond (CompRelation cond) MONO_INTERNAL;
int               mono_op_imm_to_op (int opcode) MONO_INTERNAL;
void              mono_decompose_op_imm (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins) MONO_INTERNAL;
void              mono_peephole_ins (MonoBasicBlock *bb, MonoInst *ins) MONO_INTERNAL;
MonoUnwindOp     *mono_create_unwind_op (int when, 
										 int tag, int reg, 
										 int val) MONO_INTERNAL;
void              mono_emit_unwind_op (MonoCompile *cfg, int when, 
									   int tag, int reg, 
									   int val) MONO_INTERNAL;

int               mono_method_to_ir (MonoCompile *cfg, MonoMethod *method, MonoBasicBlock *start_bblock, MonoBasicBlock *end_bblock, 
									 MonoInst *return_var, GList *dont_inline, MonoInst **inline_args, 
									 guint inline_offset, gboolean is_virtual_call) MONO_INTERNAL;

MonoInst         *mono_decompose_opcode (MonoCompile *cfg, MonoInst *ins) MONO_INTERNAL;
void              mono_decompose_long_opts (MonoCompile *cfg) MONO_INTERNAL;
void              mono_decompose_vtype_opts (MonoCompile *cfg) MONO_INTERNAL;
void              mono_decompose_array_access_opts (MonoCompile *cfg) MONO_INTERNAL;
void              mono_decompose_soft_float (MonoCompile *cfg) MONO_INTERNAL;
void              mono_handle_global_vregs (MonoCompile *cfg) MONO_INTERNAL;
void              mono_spill_global_vars (MonoCompile *cfg, gboolean *need_local_opts) MONO_INTERNAL;
void              mono_if_conversion (MonoCompile *cfg) MONO_INTERNAL;

/* methods that must be provided by the arch-specific port */
void      mono_arch_init                        (void) MONO_INTERNAL;
void      mono_arch_cleanup                     (void) MONO_INTERNAL;
void      mono_arch_cpu_init                    (void) MONO_INTERNAL;
guint32   mono_arch_cpu_optimizazions           (guint32 *exclude_mask) MONO_INTERNAL;
void      mono_arch_instrument_mem_needs        (MonoMethod *method, int *stack, int *code) MONO_INTERNAL;
void     *mono_arch_instrument_prolog           (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments) MONO_INTERNAL;
void     *mono_arch_instrument_epilog           (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments) MONO_INTERNAL;
void     *mono_arch_instrument_epilog_full     (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments, gboolean preserve_argument_registers) MONO_INTERNAL;
void      mono_codegen                          (MonoCompile *cfg) MONO_INTERNAL;
void      mono_call_inst_add_outarg_reg         (MonoCompile *cfg, MonoCallInst *call, int vreg, int hreg, gboolean fp) MONO_INTERNAL;
const char *mono_arch_regname                   (int reg) MONO_INTERNAL;
const char *mono_arch_fregname                  (int reg) MONO_INTERNAL;
gpointer  mono_arch_get_throw_exception         (void) MONO_INTERNAL;
gpointer  mono_arch_get_rethrow_exception       (void) MONO_INTERNAL;
gpointer  mono_arch_get_throw_exception_by_name (void) MONO_INTERNAL;
gpointer  mono_arch_get_throw_corlib_exception  (void) MONO_INTERNAL;
void      mono_arch_exceptions_init             (void) MONO_INTERNAL;
guchar*   mono_arch_create_trampoline_code      (MonoTrampolineType tramp_type) MONO_INTERNAL;
guchar*   mono_arch_create_trampoline_code_full (MonoTrampolineType tramp_type, guint32 *code_size, MonoJumpInfo **ji, GSList **out_unwind_ops, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot) MONO_INTERNAL;
gpointer  mono_arch_create_rgctx_lazy_fetch_trampoline_full (guint32 slot, guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_create_generic_class_init_trampoline (void) MONO_INTERNAL;
gpointer  mono_arch_get_nullified_class_init_trampoline (guint32 *code_len) MONO_INTERNAL;
gpointer  mono_arch_create_monitor_enter_trampoline (void) MONO_INTERNAL;
gpointer  mono_arch_create_monitor_exit_trampoline (void) MONO_INTERNAL;
gpointer  mono_arch_create_monitor_enter_trampoline_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_create_monitor_exit_trampoline_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_create_generic_class_init_trampoline_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
GList    *mono_arch_get_allocatable_int_vars    (MonoCompile *cfg) MONO_INTERNAL;
GList    *mono_arch_get_global_int_regs         (MonoCompile *cfg) MONO_INTERNAL;
GList    *mono_arch_get_global_fp_regs          (MonoCompile *cfg) MONO_INTERNAL;
GList    *mono_arch_get_iregs_clobbered_by_call (MonoCallInst *call) MONO_INTERNAL;
GList    *mono_arch_get_fregs_clobbered_by_call (MonoCallInst *call) MONO_INTERNAL;
guint32   mono_arch_regalloc_cost               (MonoCompile *cfg, MonoMethodVar *vmv) MONO_INTERNAL;
void      mono_arch_patch_code                  (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors) MONO_INTERNAL;
void      mono_arch_flush_icache                (guint8 *code, gint size) MONO_INTERNAL;
int       mono_arch_max_epilog_size             (MonoCompile *cfg) MONO_INTERNAL;
guint8   *mono_arch_emit_prolog                 (MonoCompile *cfg) MONO_INTERNAL;
void      mono_arch_emit_epilog                 (MonoCompile *cfg) MONO_INTERNAL;
void      mono_arch_emit_exceptions             (MonoCompile *cfg) MONO_INTERNAL;
void      mono_arch_lowering_pass               (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_arch_peephole_pass_1             (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_arch_peephole_pass_2             (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_arch_output_basic_block          (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
gboolean  mono_arch_has_unwind_info             (gconstpointer addr) MONO_INTERNAL;
void      mono_arch_setup_jit_tls_data          (MonoJitTlsData *tls) MONO_INTERNAL;
void      mono_arch_free_jit_tls_data           (MonoJitTlsData *tls) MONO_INTERNAL;
void      mono_arch_fill_argument_info          (MonoCompile *cfg) MONO_INTERNAL;
void      mono_arch_allocate_vars               (MonoCompile *m) MONO_INTERNAL;
int       mono_arch_get_argument_info           (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info) MONO_INTERNAL;
gboolean  mono_arch_print_tree			(MonoInst *tree, int arity) MONO_INTERNAL;
void      mono_arch_emit_call                   (MonoCompile *cfg, MonoCallInst *call) MONO_INTERNAL;
void      mono_arch_emit_outarg_vt              (MonoCompile *cfg, MonoInst *ins, MonoInst *src) MONO_INTERNAL;
void      mono_arch_emit_setret                 (MonoCompile *cfg, MonoMethod *method, MonoInst *val) MONO_INTERNAL;
MonoInst *mono_arch_emit_inst_for_method        (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args) MONO_INTERNAL;
void      mono_arch_decompose_opts              (MonoCompile *cfg, MonoInst *ins) MONO_INTERNAL;
void      mono_arch_decompose_long_opts         (MonoCompile *cfg, MonoInst *ins) MONO_INTERNAL;
GSList*   mono_arch_get_delegate_invoke_impls   (void) MONO_INTERNAL;
LLVMCallInfo* mono_arch_get_llvm_call_info      (MonoCompile *cfg, MonoMethodSignature *sig) MONO_INTERNAL;

MonoJitInfo *mono_arch_find_jit_info            (MonoDomain *domain, 
						 MonoJitTlsData *jit_tls, 
						 MonoJitInfo *res, 
						 MonoJitInfo *prev_ji, 
						 MonoContext *ctx, 
						 MonoContext *new_ctx, 
						 MonoLMF **lmf, 
						 gboolean *managed) MONO_INTERNAL;
gpointer mono_arch_get_call_filter              (void) MONO_INTERNAL;
gpointer mono_arch_get_restore_context          (void) MONO_INTERNAL;
gpointer mono_arch_get_call_filter_full         (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer mono_arch_get_restore_context_full     (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_throw_exception_full    (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_rethrow_exception_full  (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_throw_exception_by_name_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_throw_corlib_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_throw_pending_exception_full (guint32 *code_size, MonoJumpInfo **ji, gboolean aot) MONO_INTERNAL;
gboolean mono_arch_handle_exception             (void *sigctx, gpointer obj, gboolean test_only) MONO_INTERNAL;
void     mono_arch_handle_altstack_exception    (void *sigctx, gpointer fault_addr, gboolean stack_ovf) MONO_INTERNAL;
gboolean mono_handle_soft_stack_ovf             (MonoJitTlsData *jit_tls, MonoJitInfo *ji, void *ctx, guint8* fault_addr) MONO_INTERNAL;
gpointer mono_arch_ip_from_context              (void *sigctx) MONO_INTERNAL;
void     mono_arch_sigctx_to_monoctx            (void *sigctx, MonoContext *ctx) MONO_INTERNAL;
void     mono_arch_monoctx_to_sigctx            (MonoContext *mctx, void *ctx) MONO_INTERNAL;
gpointer mono_arch_context_get_int_reg		(MonoContext *ctx, int reg) MONO_INTERNAL;
void     mono_arch_flush_register_windows       (void) MONO_INTERNAL;
gboolean mono_arch_is_inst_imm                  (gint64 imm) MONO_INTERNAL;
MonoInst* mono_arch_get_domain_intrinsic        (MonoCompile* cfg) MONO_INTERNAL;
MonoInst* mono_arch_get_thread_intrinsic        (MonoCompile* cfg) MONO_INTERNAL;
gboolean mono_arch_is_int_overflow              (void *sigctx, void *info) MONO_INTERNAL;
void     mono_arch_invalidate_method            (MonoJitInfo *ji, void *func, gpointer func_arg) MONO_INTERNAL;
guint32  mono_arch_get_patch_offset             (guint8 *code) MONO_INTERNAL;
gpointer mono_arch_get_vcall_slot               (guint8 *code, gpointer *regs, int *displacement) MONO_INTERNAL;
gpointer*mono_arch_get_delegate_method_ptr_addr (guint8* code, gpointer *regs) MONO_INTERNAL;
void     mono_arch_create_vars                  (MonoCompile *cfg) MONO_INTERNAL;
void     mono_arch_save_unwind_info             (MonoCompile *cfg) MONO_INTERNAL;
void     mono_arch_register_lowlevel_calls      (void) MONO_INTERNAL;
gpointer mono_arch_get_unbox_trampoline         (MonoGenericSharingContext *gsctx, MonoMethod *m, gpointer addr) MONO_INTERNAL;
gpointer mono_arch_get_static_rgctx_trampoline  (MonoMethod *m, MonoMethodRuntimeGenericContext *mrgctx, gpointer addr) MONO_INTERNAL;
void     mono_arch_patch_callsite               (guint8 *method_start, guint8 *code, guint8 *addr) MONO_INTERNAL;
void     mono_arch_patch_plt_entry              (guint8 *code, guint8 *addr) MONO_INTERNAL;
void     mono_arch_nullify_class_init_trampoline(guint8 *code, gssize *regs) MONO_INTERNAL;
void     mono_arch_nullify_plt_entry            (guint8 *code) MONO_INTERNAL;
int      mono_arch_get_this_arg_reg             (MonoMethodSignature *sig, MonoGenericSharingContext *gsctx, guint8 *code) MONO_INTERNAL;
gpointer mono_arch_get_this_arg_from_call       (MonoGenericSharingContext *gsctx, MonoMethodSignature *sig, gssize *regs, guint8 *code) MONO_INTERNAL;
MonoObject* mono_arch_find_this_argument        (gpointer *regs, MonoMethod *method, MonoGenericSharingContext *gsctx) MONO_INTERNAL;
gpointer mono_arch_get_delegate_invoke_impl     (MonoMethodSignature *sig, gboolean has_target) MONO_INTERNAL;
gpointer mono_arch_create_specific_trampoline   (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len) MONO_INTERNAL;
void        mono_arch_emit_imt_argument         (MonoCompile *cfg, MonoCallInst *call, MonoInst *imt_arg) MONO_INTERNAL;
MonoMethod* mono_arch_find_imt_method           (gpointer *regs, guint8 *code) MONO_INTERNAL;
MonoVTable* mono_arch_find_static_call_vtable   (gpointer *regs, guint8 *code) MONO_INTERNAL;
gpointer    mono_arch_build_imt_thunk           (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp) MONO_INTERNAL;
void    mono_arch_notify_pending_exc            (void) MONO_INTERNAL;

/* Exception handling */
void     mono_exceptions_init                   (void) MONO_INTERNAL;
gboolean mono_handle_exception                  (MonoContext *ctx, gpointer obj,
						 gpointer original_ip, gboolean test_only) MONO_INTERNAL;
void     mono_handle_native_sigsegv             (int signal, void *sigctx) MONO_INTERNAL;
void     mono_print_thread_dump                 (void *sigctx);
void     mono_jit_walk_stack                    (MonoStackWalk func, gboolean do_il_offset, gpointer user_data) MONO_INTERNAL;
void     mono_jit_walk_stack_from_ctx           (MonoStackWalk func, MonoContext *ctx, gboolean do_il_offset, gpointer user_data) MONO_INTERNAL;
void     mono_setup_altstack                    (MonoJitTlsData *tls) MONO_INTERNAL;
void     mono_free_altstack                     (MonoJitTlsData *tls) MONO_INTERNAL;
gpointer mono_altstack_restore_prot             (gssize *regs, guint8 *code, gpointer *tramp_data, guint8* tramp) MONO_INTERNAL;

MonoJitInfo * mono_find_jit_info                (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset, gboolean *managed) MONO_INTERNAL;

gpointer mono_get_throw_exception               (void) MONO_INTERNAL;
gpointer mono_get_rethrow_exception             (void) MONO_INTERNAL;
gpointer mono_get_call_filter                   (void) MONO_INTERNAL;
gpointer mono_get_restore_context               (void) MONO_INTERNAL;
gpointer mono_get_throw_exception_by_name       (void) MONO_INTERNAL;
gpointer mono_get_throw_corlib_exception        (void) MONO_INTERNAL;

/* the new function to do stack walks */
typedef gboolean (*MonoStackFrameWalk)          (MonoDomain *domain, MonoContext *ctx, MonoJitInfo *ji, gpointer data);
void      mono_walk_stack                       (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoContext *start_ctx, MonoStackFrameWalk func, gpointer user_data);

MonoArray *ves_icall_get_trace                  (MonoException *exc, gint32 skip, MonoBoolean need_file_info) MONO_INTERNAL;
MonoBoolean ves_icall_get_frame_info            (gint32 skip, MonoBoolean need_file_info, 
						 MonoReflectionMethod **method, 
						 gint32 *iloffset, gint32 *native_offset,
						 MonoString **file, gint32 *line, gint32 *column) MONO_INTERNAL;
MonoString *ves_icall_System_Exception_get_trace (MonoException *exc) MONO_INTERNAL;

/* Dominator/SSA methods */
void        mono_compile_dominator_info         (MonoCompile *cfg, int dom_flags) MONO_INTERNAL;
void        mono_compute_natural_loops          (MonoCompile *cfg) MONO_INTERNAL;
MonoBitSet* mono_compile_iterated_dfrontier     (MonoCompile *cfg, MonoBitSet *set) MONO_INTERNAL;
void        mono_ssa_compute                    (MonoCompile *cfg) MONO_INTERNAL;
void        mono_ssa_remove                     (MonoCompile *cfg) MONO_INTERNAL;
void        mono_ssa_cprop                      (MonoCompile *cfg) MONO_INTERNAL;
void        mono_ssa_deadce                     (MonoCompile *cfg) MONO_INTERNAL;
void        mono_ssa_strength_reduction         (MonoCompile *cfg) MONO_INTERNAL;
void        mono_free_loop_info                 (MonoCompile *cfg) MONO_INTERNAL;

void        mono_ssa_compute2                   (MonoCompile *cfg);
void        mono_ssa_remove2                    (MonoCompile *cfg);
void        mono_ssa_cprop2                     (MonoCompile *cfg);
void        mono_ssa_deadce2                    (MonoCompile *cfg);

/* debugging support */
void      mono_debug_init_method                (MonoCompile *cfg, MonoBasicBlock *start_block,
						 guint32 breakpoint_id) MONO_INTERNAL;
void      mono_debug_open_method                (MonoCompile *cfg) MONO_INTERNAL;
void      mono_debug_close_method               (MonoCompile *cfg) MONO_INTERNAL;
void      mono_debug_open_block                 (MonoCompile *cfg, MonoBasicBlock *bb, guint32 address) MONO_INTERNAL;
void      mono_debug_record_line_number         (MonoCompile *cfg, MonoInst *ins, guint32 address) MONO_INTERNAL;
void      mono_debug_serialize_debug_info       (MonoCompile *cfg, guint8 **out_buf, guint32 *buf_len) MONO_INTERNAL;
void      mono_debug_add_aot_method             (MonoDomain *domain,
						 MonoMethod *method, guint8 *code_start, 
						 guint8 *debug_info, guint32 debug_info_len) MONO_INTERNAL;
void      mono_debug_add_icall_wrapper          (MonoMethod *method, MonoJitICallInfo* info) MONO_INTERNAL;
void      mono_debug_print_vars                 (gpointer ip, gboolean only_arguments);
void      mono_debugger_run_finally             (MonoContext *start_ctx);

extern gssize mono_breakpoint_info_index [MONO_BREAKPOINT_ARRAY_SIZE];

gboolean mono_breakpoint_clean_code (guint8 *method_start, guint8 *code, int offset, guint8 *buf, int size);

#ifdef MONO_DEBUGGER_SUPPORTED

/* Mono Debugger support */
void      mini_debugger_init                    (void);
int       mini_debugger_main                    (MonoDomain *domain, MonoAssembly *assembly, int argc, char **argv);
gboolean  mini_debug_running_inside_mdb         (void);

#endif

/* Tracing */
MonoTraceSpec *mono_trace_parse_options         (const char *options) MONO_INTERNAL;
void           mono_trace_set_assembly          (MonoAssembly *assembly) MONO_INTERNAL;
gboolean       mono_trace_eval                  (MonoMethod *method) MONO_INTERNAL;

extern void
mono_perform_abc_removal (MonoCompile *cfg) MONO_INTERNAL;
extern void
mono_perform_abc_removal (MonoCompile *cfg) MONO_INTERNAL;
extern void
mono_perform_ssapre (MonoCompile *cfg) MONO_INTERNAL;
extern void
mono_local_cprop (MonoCompile *cfg) MONO_INTERNAL;
extern void
mono_local_cprop (MonoCompile *cfg);
extern void
mono_local_deadce (MonoCompile *cfg);

/* CAS - stack walk */
MonoSecurityFrame* ves_icall_System_Security_SecurityFrame_GetSecurityFrame (gint32 skip) MONO_INTERNAL;
MonoArray* ves_icall_System_Security_SecurityFrame_GetSecurityStack (gint32 skip) MONO_INTERNAL;

/* Generic sharing */

MonoGenericSharingContext* mono_get_generic_context_from_code (guint8 *code) MONO_INTERNAL;

MonoGenericContext* mini_method_get_context (MonoMethod *method) MONO_INTERNAL;

int mono_method_check_context_used (MonoMethod *method) MONO_INTERNAL;

gboolean mono_generic_context_equal_deep (MonoGenericContext *context1, MonoGenericContext *context2) MONO_INTERNAL;

gpointer mono_helper_get_rgctx_other_ptr (MonoClass *caller_class, MonoVTable *vtable,
					  guint32 token, guint32 token_source, guint32 rgctx_type,
					  gint32 rgctx_index) MONO_INTERNAL;

void mono_generic_sharing_init (void) MONO_INTERNAL;

MonoClass* mini_class_get_container_class (MonoClass *class) MONO_INTERNAL;
MonoGenericContext* mini_class_get_context (MonoClass *class) MONO_INTERNAL;

MonoType* mini_get_basic_type_from_generic (MonoGenericSharingContext *gsctx, MonoType *type) MONO_INTERNAL;
MonoType* mini_type_get_underlying_type (MonoGenericSharingContext *gsctx, MonoType *type) MONO_INTERNAL;

int mini_type_stack_size (MonoGenericSharingContext *gsctx, MonoType *t, int *align) MONO_INTERNAL;
int mini_type_stack_size_full (MonoGenericSharingContext *gsctx, MonoType *t, guint32 *align, gboolean pinvoke) MONO_INTERNAL;
void type_to_eval_stack_type (MonoCompile *cfg, MonoType *type, MonoInst *inst) MONO_INTERNAL;
guint mono_type_to_regmove (MonoCompile *cfg, MonoType *type) MONO_INTERNAL;

/* wapihandles.c */
int mini_wapi_hps (int argc, char **argv) MONO_INTERNAL;

int mini_wapi_semdel (int argc, char **argv) MONO_INTERNAL;

int mini_wapi_seminfo (int argc, char **argv) MONO_INTERNAL;

/* SIMD support */

/*
This enum MUST be kept in sync with its managed mirror Mono.Simd.AccelMode.
The AccelMode values are masks while the ones here are the bit indexes.
 */
enum {
	SIMD_VERSION_SSE1	= 0,
	SIMD_VERSION_SSE2	= 1,
	SIMD_VERSION_SSE3	= 2,
	SIMD_VERSION_SSSE3	= 3,
	SIMD_VERSION_SSE41	= 4,
	SIMD_VERSION_SSE42	= 5,
	SIMD_VERSION_SSE4a	= 6,
};

enum {
	SIMD_COMP_EQ,
	SIMD_COMP_LT,
	SIMD_COMP_LE,
	SIMD_COMP_UNORD,
	SIMD_COMP_NEQ,
	SIMD_COMP_NLT,
	SIMD_COMP_NLE,
	SIMD_COMP_ORD
};

enum {
	SIMD_PREFETCH_MODE_NTA,
	SIMD_PREFETCH_MODE_0,
	SIMD_PREFETCH_MODE_1,
	SIMD_PREFETCH_MODE_2,
};

const char *mono_arch_xregname (int reg) MONO_INTERNAL;
void        mono_simd_simplify_indirection (MonoCompile *cfg) MONO_INTERNAL;
MonoInst*   mono_emit_simd_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args) MONO_INTERNAL;
guint32     mono_arch_cpu_enumerate_simd_versions (void) MONO_INTERNAL;
void        mono_simd_intrinsics_init (void) MONO_INTERNAL;

/*
 * Per-OS implementation functions.
 */
void mono_runtime_install_handlers (void) MONO_INTERNAL;
void mono_runtime_cleanup_handlers (void) MONO_INTERNAL;
void mono_runtime_setup_stat_profiler (void) MONO_INTERNAL;
void mono_runtime_shutdown_stat_profiler (void) MONO_INTERNAL;
void mono_runtime_posix_install_handlers (void) MONO_INTERNAL;

/*
 * Signal handling
 */
#ifdef MONO_GET_CONTEXT
#define GET_CONTEXT MONO_GET_CONTEXT
#endif

#ifndef GET_CONTEXT
#ifdef PLATFORM_WIN32
#define GET_CONTEXT \
	struct sigcontext *ctx = (struct sigcontext*)_dummy;
#else
#ifdef MONO_ARCH_USE_SIGACTION
#define GET_CONTEXT \
    void *ctx = context;
#elif defined(__sparc__)
#define GET_CONTEXT \
    void *ctx = sigctx;
#else
#define GET_CONTEXT \
	void **_p = (void **)&_dummy; \
	struct sigcontext *ctx = (struct sigcontext *)++_p;
#endif
#endif
#endif

#ifdef MONO_ARCH_USE_SIGACTION
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, siginfo_t *info, void *context)
#define SIG_HANDLER_PARAMS _dummy, info, context
#elif defined(__sparc__)
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, void *sigctx)
#define SIG_HANDLER_PARAMS _dummy, sigctx
#else
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy)
#define SIG_HANDLER_PARAMS _dummy
#endif

void SIG_HANDLER_SIGNATURE (mono_sigfpe_signal_handler)  MONO_INTERNAL;
void SIG_HANDLER_SIGNATURE (mono_sigill_signal_handler)  MONO_INTERNAL;
void SIG_HANDLER_SIGNATURE (mono_sigsegv_signal_handler) MONO_INTERNAL;
void SIG_HANDLER_SIGNATURE (mono_sigint_signal_handler)  MONO_INTERNAL;
gboolean SIG_HANDLER_SIGNATURE (mono_chain_signal) MONO_INTERNAL;

/* for MONO_WRAPPER_UNKNOWN subtypes */
enum {
	MONO_AOT_WRAPPER_MONO_ENTER,
	MONO_AOT_WRAPPER_MONO_EXIT,
	MONO_AOT_WRAPPER_LAST
};

#endif /* __MONO_MINI_H__ */
