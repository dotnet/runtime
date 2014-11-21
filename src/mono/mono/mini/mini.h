/*
 * Copyright 2002-2003 Ximian Inc
 * Copyright 2003-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 */
#ifndef __MONO_MINI_H__
#define __MONO_MINI_H__

#include "config.h"
#include <glib.h>
#include <signal.h>
#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif
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
#include <mono/utils/mono-machine.h>
#include <mono/utils/mono-stack-unwinding.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-conc-hashtable.h>
#include <mono/utils/mono-signal-handler.h>

#define MONO_BREAKPOINT_ARRAY_SIZE 64

#include "mini-arch.h"
#include "regalloc.h"
#include "declsec.h"
#include "mini-unwind.h"
#include "jit.h"

#ifdef __native_client_codegen__
#include <nacl/nacl_dyncode.h>
#endif


/*
 * The mini code should not have any compile time dependencies on the GC being used, so the same object file from mini/
 * can be linked into both mono and mono-sgen.
 */
#if defined(HAVE_BOEHM_GC) || defined(HAVE_SGEN_GC)
#error "The code in mini/ should not depend on these defines."
#endif

#ifndef __GNUC__
/*#define __alignof__(a) sizeof(a)*/
#define __alignof__(type) G_STRUCT_OFFSET(struct { char c; type x; }, x)
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

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
#define COMPILE_SOFT_FLOAT(cfg) (!COMPILE_LLVM ((cfg)) && mono_arch_is_soft_float ())
#else
#define COMPILE_SOFT_FLOAT(cfg) (0)
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

#ifndef DISABLE_AOT
#define MONO_USE_AOT_COMPILER
#endif

/* Version number of the AOT file format */
#define MONO_AOT_FILE_VERSION 104

//TODO: This is x86/amd64 specific.
#define mono_simd_shuffle_mask(a,b,c,d) ((a) | ((b) << 2) | ((c) << 4) | ((d) << 6))

/* Remap printf to g_print (we use a mix of these in the mini code) */
#ifdef PLATFORM_ANDROID
#define printf g_print
#endif

#define MONO_TYPE_IS_PRIMITIVE(t) ((!(t)->byref && ((((t)->type >= MONO_TYPE_BOOLEAN && (t)->type <= MONO_TYPE_R8) || ((t)->type >= MONO_TYPE_I && (t)->type <= MONO_TYPE_U)))))

/* Constants used to encode different types of methods in AOT */
enum {
	MONO_AOT_METHODREF_MIN = 240,
	/* Image index bigger than METHODREF_MIN */
	MONO_AOT_METHODREF_LARGE_IMAGE_INDEX = 249,
	/* Runtime provided methods on arrays */
	MONO_AOT_METHODREF_ARRAY = 250,
	MONO_AOT_METHODREF_NO_AOT_TRAMPOLINE = 251,
	/* Wrappers */
	MONO_AOT_METHODREF_WRAPPER = 252,
	/* Methods on generic instances */
	MONO_AOT_METHODREF_GINST = 253,
	/* Methods resolve using a METHODSPEC token */
	MONO_AOT_METHODREF_METHODSPEC = 254,
};

/* Constants used to encode different types of types in AOT */
enum {
	/* typedef index */
	MONO_AOT_TYPEREF_TYPEDEF_INDEX = 1,
	/* typedef index + image index */
	MONO_AOT_TYPEREF_TYPEDEF_INDEX_IMAGE = 2,
	/* typespec token */
	MONO_AOT_TYPEREF_TYPESPEC_TOKEN = 3,
	/* generic inst */
	MONO_AOT_TYPEREF_GINST = 4,
	/* type/method variable */
	MONO_AOT_TYPEREF_VAR = 5,
	/* array */
	MONO_AOT_TYPEREF_ARRAY = 6,
	/* blob index of the type encoding */
	MONO_AOT_TYPEREF_BLOB_INDEX = 7,
	/* ptr */
	MONO_AOT_TYPEREF_PTR = 8
};

/* Trampolines which we have a lot of */
typedef enum {
	MONO_AOT_TRAMP_SPECIFIC = 0,
	MONO_AOT_TRAMP_STATIC_RGCTX = 1,
	MONO_AOT_TRAMP_IMT_THUNK = 2,
	MONO_AOT_TRAMP_GSHAREDVT_ARG = 3,
	MONO_AOT_TRAMP_NUM = 4
} MonoAotTrampoline;

typedef enum {
	MONO_AOT_FILE_FLAG_WITH_LLVM = 1,
	MONO_AOT_FILE_FLAG_FULL_AOT = 2,
	MONO_AOT_FILE_FLAG_DEBUG = 4,
	MONO_AOT_FILE_FLAG_DIRECT_METHOD_ADDRESSES = 8
} MonoAotFileFlags;

/* This structure is stored in the AOT file */
typedef struct MonoAotFileInfo
{
	/* The version number of the AOT file format, should match MONO_AOT_FILE_VERSION */
	guint32 version;
	/* For alignment */
	guint32 dummy;

	/* All the pointers should be at the start to avoid alignment problems */

	/* Mono's Global Offset Table */
	gpointer got;
	/* Compiled code for methods */
	gpointer methods;
	/* Mono EH Frame created by llc when using LLVM */
	gpointer mono_eh_frame;
	/* Data blob */
	gpointer blob;
	gpointer class_name_table;
	gpointer class_info_offsets;
	gpointer method_info_offsets;
	gpointer ex_info_offsets;
	gpointer code_offsets;
	gpointer method_addresses;
	gpointer extra_method_info_offsets;
	gpointer extra_method_table;
	gpointer got_info_offsets;
	gpointer methods_end;
	gpointer unwind_info;
	gpointer mem_end;
	gpointer image_table;
	/* Start of Mono's Program Linkage Table */
	gpointer plt;
	/* End of Mono's Program Linkage Table */
	gpointer plt_end;
	/* The GUID of the assembly which the AOT image was generated from */
	gpointer assembly_guid;
	/*
	 * The runtime version string for AOT images generated using 'bind-to-runtime-version',
	 * NULL otherwise.
	 */
	gpointer runtime_version;
	/* Blocks of various kinds of trampolines */
	gpointer specific_trampolines;
	gpointer static_rgctx_trampolines;
	gpointer imt_thunks;
	gpointer gsharedvt_arg_trampolines;
	/*
	 * The end of LLVM generated thumb code, or NULL.
	 */
	gpointer thumb_end;
	/* In static mode, points to a table of global symbols for trampolines etc */
	gpointer globals;
	/* Points to a string containing the assembly name*/
	gpointer assembly_name;
	/* Points to a table mapping methods to their unbox trampolines */
	gpointer unbox_trampolines;
	/* Points to the end of the previous table */
	gpointer unbox_trampolines_end;

	/* The index of the first GOT slot used by the PLT */
	guint32 plt_got_offset_base;
	/* Number of entries in the GOT */
	guint32 got_size;
	/* Number of entries in the PLT */
	guint32 plt_size;
	/* Number of methods */
	guint32 nmethods;
	/* A union of MonoAotFileFlags */
	guint32 flags;
	/* Optimization flags used to compile the module */
	guint32 opts;
	/* SIMD flags used to compile the module */
	guint32 simd_opts;
	/* Index of the blob entry holding the GC used by this module */
	gint32 gc_name_index;

	/* Number of trampolines */
	guint32 num_trampolines [MONO_AOT_TRAMP_NUM];
	/* The indexes of the first GOT slots used by the trampolines */
	guint32 trampoline_got_offset_base [MONO_AOT_TRAMP_NUM];
	/* The size of one trampoline */
	guint32 trampoline_size [MONO_AOT_TRAMP_NUM];
	guint32 num_rgctx_fetch_trampolines;

	/* These are used for sanity checking object layout problems when cross-compiling */
	guint32 double_align, long_align, generic_tramp_num;
	/* The page size used by trampoline pages */
	guint32 tramp_page_size;
	/* The offset where the trampolines begin on a trampoline page */
	guint32 tramp_page_code_offsets [MONO_AOT_TRAMP_NUM];
} MonoAotFileInfo;

typedef struct
{
	MonoClass *klass;
	MonoMethod *method;
} MonoClassMethodPair;

typedef struct
{
	MonoClass *klass;
	MonoMethod *method;
	gboolean virtual;
} MonoDelegateClassMethodPair;

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
	/* Maps ClassMethodPair -> MonoDelegateTrampInfo */
	GHashTable *static_rgctx_trampoline_hash;
	GHashTable *llvm_vcall_trampoline_hash;
	/* maps MonoMethod -> MonoJitDynamicMethodInfo */
	GHashTable *dynamic_code_hash;
	GHashTable *method_code_hash;
	/* Maps methods to a RuntimeInvokeInfo structure */
	MonoConcurrentHashTable *runtime_invoke_hash;
	/* Maps MonoMethod to a GPtrArray containing sequence point locations */
	GHashTable *seq_points;
	/* Debugger agent data */
	gpointer agent_info;
	/* Maps MonoMethod to an arch-specific structure */
	GHashTable *arch_seq_points;
	/* Maps a GSharedVtTrampInfo structure to a trampoline address */
	GHashTable *gsharedvt_arg_tramp_hash;
	/* memcpy/bzero methods specialized for small constant sizes */
	gpointer *memcpy_addr [17];
	gpointer *bzero_addr [17];
	gpointer llvm_module;
} MonoJitDomainInfo;

typedef struct {
	MonoJitInfo *ji;
	MonoCodeManager *code_mp;
} MonoJitDynamicMethodInfo;

#define domain_jit_info(domain) ((MonoJitDomainInfo*)((domain)->runtime_info))

/* Contains a list of ips which needs to be patched when a method is compiled */
typedef struct {
	GSList *list;
} MonoJumpList;

/* Arch-specific */
typedef struct {
	int dummy;
} MonoDynCallInfo;

/*
 * Information about a stack frame.
 * FIXME This typedef exists only to avoid tons of code rewriting
 */
typedef MonoStackFrameInfo StackFrameInfo;

#define MONO_SEQ_POINT_FLAG_NONEMPTY_STACK 1

typedef struct {
	int il_offset, native_offset, flags;
	/* Indexes of successor sequence points */
	int *next;
	/* Number of entries in next */
	int next_len;
} SeqPoint;

typedef struct {
	int len;
	SeqPoint seq_points [MONO_ZERO_LEN_ARRAY];
} MonoSeqPointInfo;

#if 0
#define mono_bitset_foreach_bit(set,b,n) \
	for (b = 0; b < n; b++)\
		if (mono_bitset_test_fast(set,b))
#else
#define mono_bitset_foreach_bit(set,b,n) \
	for (b = mono_bitset_find_start (set); b < n && b >= 0; b = mono_bitset_find_first (set, b))
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
/* INEG sets the condition codes, and the OP_LNEG decomposition depends on this on x86 */
#define MONO_INS_HAS_NO_SIDE_EFFECT(ins) (MONO_IS_MOVE (ins) || (ins->opcode == OP_ICONST) || (ins->opcode == OP_I8CONST) || MONO_IS_ZERO (ins) || (ins->opcode == OP_ADD_IMM) || (ins->opcode == OP_R8CONST) || (ins->opcode == OP_LADD_IMM) || (ins->opcode == OP_ISUB_IMM) || (ins->opcode == OP_IADD_IMM) || (ins->opcode == OP_LNEG) || (ins->opcode == OP_ISUB) || (ins->opcode == OP_CMOV_IGE) || (ins->opcode == OP_ISHL_IMM) || (ins->opcode == OP_ISHR_IMM) || (ins->opcode == OP_ISHR_UN_IMM) || (ins->opcode == OP_IAND_IMM) || (ins->opcode == OP_ICONV_TO_U1) || (ins->opcode == OP_ICONV_TO_I1) || (ins->opcode == OP_SEXT_I4) || (ins->opcode == OP_LCONV_TO_U1) || (ins->opcode == OP_ICONV_TO_U2) || (ins->opcode == OP_ICONV_TO_I2) || (ins->opcode == OP_LCONV_TO_I2) || (ins->opcode == OP_LDADDR) || (ins->opcode == OP_PHI) || (ins->opcode == OP_NOP) || (ins->opcode == OP_ZEXT_I4) || (ins->opcode == OP_NOT_NULL))

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
typedef struct MonoMethodVar MonoMethodVar;
typedef struct MonoBasicBlock MonoBasicBlock;
typedef struct MonoLMF MonoLMF;
typedef struct MonoSpillInfo MonoSpillInfo;
typedef struct MonoTraceSpec MonoTraceSpec;

extern MonoNativeTlsKey mono_jit_tls_id;
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
extern gboolean mono_do_x86_stack_align;
extern const char *mono_build_date;
extern gboolean mono_do_signal_chaining;
extern gboolean mono_do_crash_chaining;
extern gboolean mono_use_llvm;
extern gboolean mono_do_single_method_regression;
extern guint32 mono_single_method_regression_opt;
extern MonoMethod *mono_current_single_method;
extern GSList *mono_single_method_list;
extern GHashTable *mono_single_method_hash;

#define INS_INFO(opcode) (&ins_info [((opcode) - OP_START - 1) * 4])

extern const char ins_info[];
extern const gint8 ins_sreg_counts [];

#ifndef DISABLE_JIT
#define mono_inst_get_num_src_registers(ins) (ins_sreg_counts [(ins)->opcode - OP_START - 1])
#else
#define mono_inst_get_num_src_registers(ins) 0
#endif

#define mono_inst_get_src_registers(ins, regs) (((regs) [0] = (ins)->sreg1), ((regs) [1] = (ins)->sreg2), ((regs) [2] = (ins)->sreg3), mono_inst_get_num_src_registers ((ins)))

#define MONO_BB_FOR_EACH_INS(bb, ins) for ((ins) = (bb)->code; (ins); (ins) = (ins)->next)

#define MONO_BB_FOR_EACH_INS_SAFE(bb, n, ins) for ((ins) = (bb)->code, n = (ins) ? (ins)->next : NULL; (ins); (ins) = (n), (n) = (ins) ? (ins)->next : NULL)

#define MONO_BB_FOR_EACH_INS_REVERSE_SAFE(bb, p, ins) for ((ins) = (bb)->last_ins, p = (ins) ? (ins)->prev : NULL; (ins); (ins) = (p), (p) = (ins) ? (ins)->prev : NULL)

#define mono_bb_first_ins(bb) (bb)->code

struct MonoSpillInfo {
	int offset;
};

/*
 * Information about a call site for the GC map creation code
 */
typedef struct {
	/* The next offset after the call instruction */
	int pc_offset;
	/* The basic block containing the call site */
	MonoBasicBlock *bb;
	/* 
	 * The set of variables live at the call site.
	 * Has length cfg->num_varinfo in bits.
	 */
	guint8 *liveness;
	/*
	 * List of OP_GC_PARAM_SLOT_LIVENESS_DEF instructions defining the param slots
	 * used by this call.
	 */
	GSList *param_slots;
} GCCallSite;

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

	/* The offset of the generated code, used for fixups */
	int native_offset;
	/* The length of the generated code, doesn't include alignment padding */
	int native_length;
	/* The real native offset, which includes alignment padding too */
	int real_native_offset;
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
	/* Whenever this bblock contains a OP_JUMP_TABLE instruction */
	guint has_jump_table : 1;
	/* Whenever this bblock contains an OP_CALL_HANDLER instruction */
	guint has_call_handler : 1;
	/* Whenever this bblock starts a try block */
	guint try_start : 1;
	/*
	 * If this is set, extend the try range started by this bblock by an arch specific
	 * number of bytes to encompass the end of the previous bblock (e.g. a Monitor.Enter
	 * call).
	 */
	guint extend_try_block : 1;
	
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

	GSList *seq_points;
	MonoInst *last_seq_point;

	GSList *spill_slot_defs;

	/* List of call sites in this bblock sorted by pc_offset */
	GSList *gc_callsites;

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
	BB_VISITED              = 1 << 0,
	BB_REACHABLE            = 1 << 1,
	BB_EXCEPTION_DEAD_OBJ   = 1 << 2,
	BB_EXCEPTION_UNSAFE     = 1 << 3,
	BB_EXCEPTION_HANDLER    = 1 << 4,
	/* for Native Client, mark the blocks that can be jumped to indirectly */
	BB_INDIRECT_JUMP_TARGET = 1 << 5 
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
	LLVMArgVtypeRetAddr, /* On on cinfo->ret */
	LLVMArgGSharedVt,
} LLVMArgStorage;

typedef struct {
	LLVMArgStorage storage;

	/* Only if storage == ArgValuetypeInReg */
	LLVMArgStorage pair_storage [2];
} LLVMArgInfo;

typedef struct {
	LLVMArgInfo ret;
	/* Whenever there is an rgctx argument */
	gboolean rgctx_arg;
	/* Whenever there is an IMT argument */
	gboolean imt_arg;
	/* 
	 * The position of the vret arg in the argument list.
	 * Only if ret->storage == ArgVtypeRetAddr.
	 * Should be 0 or 1.
	 */
	int vret_arg_index;
	/* args [0] is for the this argument if it exists */
	LLVMArgInfo args [1];
} LLVMCallInfo;

#define MONO_MAX_SRC_REGS	3

struct MonoInst {
 	guint16 opcode;
	guint8  type; /* stack type */
	guint8  flags;
	
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
			MonoExceptionClause *exception_clause;
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
		int pc_offset; /* OP_GC_LIVERANGE_START/END */
		int memory_barrier_kind; /* see mono-memory-model.h for valid values */
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
	guint stack_align_amount;
	guint virtual : 1;
	guint tail_call : 1;
	/* If this is TRUE, 'fptr' points to a MonoJumpInfo instead of an address. */
	guint fptr_is_patch : 1;
	/*
	 * If this is true, then the call returns a vtype in a register using the same 
	 * calling convention as OP_CALL.
	 */
	guint vret_in_reg : 1;
	/* Whenever vret_in_reg returns fp values */
	guint vret_in_reg_fp : 1;
	/* Whenever there is an IMT argument and it is dynamic */
	guint dynamic_imt_arg : 1;
	/* Whenever there is an RGCTX argument */
	guint32 rgctx_reg : 1;
	/* Whenever the call will need an unbox trampoline */
	guint need_unbox_trampoline : 1;
	regmask_t used_iregs;
	regmask_t used_fregs;
	GSList *out_ireg_args;
	GSList *out_freg_args;
	GSList *outarg_vts;
	gpointer call_info;
#ifdef ENABLE_LLVM
	LLVMCallInfo *cinfo;
	int rgctx_arg_reg, imt_arg_reg;
#endif
#ifdef TARGET_ARM
	/* See the comment in mini-arm.c!mono_arch_emit_call for RegTypeFP. */
	GSList *float_args;
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
	MONO_INST_INIT       = 1, /* in localloc */
	MONO_INST_SINGLE_STEP_LOC = 1, /* in SEQ_POINT */
	MONO_INST_IS_DEAD    = 2,
	MONO_INST_TAILCALL   = 4,
	MONO_INST_VOLATILE   = 4,
	MONO_INST_NOTYPECHECK    = 4,
	MONO_INST_NONEMPTY_STACK = 4, /* in SEQ_POINT */
	MONO_INST_UNALIGNED  = 8,
    MONO_INST_CFOLD_TAKEN = 8, /* On branches */
    MONO_INST_CFOLD_NOT_TAKEN = 16, /* On branches */
	MONO_INST_DEFINITION_HAS_SIDE_EFFECTS = 8,
	/* the address of the variable has been taken */
	MONO_INST_INDIRECT   = 16,
	MONO_INST_NORANGECHECK   = 16,
	/* On loads, the source address can be null */
	MONO_INST_FAULT = 32,
	/* 
	 * On variables, identifies LMF variables. These variables have a dummy type (int), but
	 * require stack space for a MonoLMF struct.
	 */
	MONO_INST_LMF = 32,
	/* On loads, the source address points to a constant value */
	MONO_INST_INVARIANT_LOAD = 64,
	/* On variables, the variable needs GC tracking */
	MONO_INST_GC_TRACK = 128,
	/*
	 * Set on instructions during code emission which make calls, i.e. OP_CALL, OP_THROW.
	 * backend.pc_offset will be set to the pc offset at the end of the native call instructions.
	 */
	MONO_INST_GC_CALLSITE = 128
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
#define inst_eh_block	 data.op[1].exception_clause

static inline void
mono_inst_set_src_registers (MonoInst *ins, int *regs)
{
	ins->sreg1 = regs [0];
	ins->sreg2 = regs [1];
	ins->sreg3 = regs [2];
}

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

/*
 * Stores state need to resume exception handling when using LLVM
 */
typedef struct {
	MonoJitInfo *ji;
	int clause_index;
	MonoContext ctx, new_ctx;
	/* FIXME: GC */
	gpointer        ex_obj;
	MonoLMF *lmf;
	int first_filter_idx, filter_idx;
} ResumeState;

typedef struct {
	gpointer          end_of_stack;
	guint32           stack_size;
	/* !defined(HAVE_KW_THREAD) || !defined(MONO_ARCH_ENABLE_MONO_LMF_VAR) */
	MonoLMF          *lmf;
	MonoLMF          *first_lmf;
	gpointer         restore_stack_prot;
	guint32          handling_stack_ovf;
	gpointer         signal_stack;
	guint32          signal_stack_size;
	gpointer         stack_ovf_guard_base;
	guint32          stack_ovf_guard_size;
	guint            stack_ovf_valloced : 1;
	void            (*abort_func) (MonoObject *object);
	/* Used to implement --debug=casts */
	MonoClass       *class_cast_from, *class_cast_to;

	/* Stores state needed by handler block with a guard */
	MonoContext     ex_ctx;
	ResumeState resume_state;

	/*Variabled use to implement handler blocks (finally/catch/etc) guards during interruption*/
	/* handler block return address */
	gpointer handler_block_return_address;

	/* handler block been guarded. It's safe to store this even for dynamic methods since there
	is an activation on stack making sure it will remain alive.*/
	MonoJitExceptionInfo *handler_block;

	/* context to be used by the guard trampoline when resuming interruption.*/
	MonoContext handler_block_context;
	/* 
	 * Stores the state at the exception throw site to be used by mono_stack_walk ()
	 * when it is called from profiler functions during exception handling.
	 */
	MonoContext orig_ex_ctx;
	gboolean orig_ex_ctx_set;

	/* 
	 * Stores if we need to run a chained exception in Windows.
	 */
	gboolean mono_win_chained_exception_needs_run;
} MonoJitTlsData;

/*
 * This structure is an extension of MonoLMF and contains extra information.
 */
typedef struct {
	struct MonoLMF lmf;
	gboolean debugger_invoke;
	MonoContext ctx; /* if debugger_invoke is TRUE */
} MonoLMFExt;

/* Generic sharing */
typedef enum {
	MONO_RGCTX_INFO_STATIC_DATA,
	MONO_RGCTX_INFO_KLASS,
	MONO_RGCTX_INFO_VTABLE,
	MONO_RGCTX_INFO_TYPE,
	MONO_RGCTX_INFO_REFLECTION_TYPE,
	MONO_RGCTX_INFO_METHOD,
	MONO_RGCTX_INFO_GENERIC_METHOD_CODE,
	MONO_RGCTX_INFO_CLASS_FIELD,
	MONO_RGCTX_INFO_METHOD_RGCTX,
	MONO_RGCTX_INFO_METHOD_CONTEXT,
	MONO_RGCTX_INFO_REMOTING_INVOKE_WITH_CHECK,
	MONO_RGCTX_INFO_METHOD_DELEGATE_CODE,
	MONO_RGCTX_INFO_CAST_CACHE,
	MONO_RGCTX_INFO_ARRAY_ELEMENT_SIZE,
	MONO_RGCTX_INFO_VALUE_SIZE,
	MONO_RGCTX_INFO_FIELD_OFFSET,
	/* Either the code for a gsharedvt method, or the address for a gsharedvt-out trampoline for the method */
	MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE,
	/* Same for virtual calls */
	MONO_RGCTX_INFO_METHOD_GSHAREDVT_OUT_TRAMPOLINE_VIRT,
	/* Same for calli, associated with a signature */
	MONO_RGCTX_INFO_SIG_GSHAREDVT_OUT_TRAMPOLINE_CALLI,
	/*
	 * 0 - vtype
	 * 1 - ref
	 * 2 - gsharedvt type
	 */
	MONO_RGCTX_INFO_CLASS_BOX_TYPE,
	/* Resolves to a MonoGSharedVtMethodRuntimeInfo */
	MONO_RGCTX_INFO_METHOD_GSHAREDVT_INFO,
	MONO_RGCTX_INFO_LOCAL_OFFSET,
	MONO_RGCTX_INFO_MEMCPY,
	MONO_RGCTX_INFO_BZERO,
	/* The address of Nullable<T>.Box () */
	MONO_RGCTX_INFO_NULLABLE_CLASS_BOX,
	MONO_RGCTX_INFO_NULLABLE_CLASS_UNBOX,
} MonoRgctxInfoType;

typedef struct _MonoRuntimeGenericContextInfoTemplate {
	MonoRgctxInfoType info_type;
	gpointer data;
	struct _MonoRuntimeGenericContextInfoTemplate *next;
} MonoRuntimeGenericContextInfoTemplate;

typedef struct {
	MonoClass *next_subclass;
	MonoRuntimeGenericContextInfoTemplate *infos;
	GSList *method_templates;
} MonoRuntimeGenericContextTemplate;

typedef struct {
	MonoVTable *class_vtable; /* must be the first element */
	MonoGenericInst *method_inst;
	gpointer infos [MONO_ZERO_LEN_ARRAY];
} MonoMethodRuntimeGenericContext;

#define MONO_SIZEOF_METHOD_RUNTIME_GENERIC_CONTEXT (sizeof (MonoMethodRuntimeGenericContext) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)

#define MONO_RGCTX_SLOT_MAKE_RGCTX(i)	(i)
#define MONO_RGCTX_SLOT_MAKE_MRGCTX(i)	((i) | 0x80000000)
#define MONO_RGCTX_SLOT_INDEX(s)	((s) & 0x7fffffff)
#define MONO_RGCTX_SLOT_IS_MRGCTX(s)	(((s) & 0x80000000) ? TRUE : FALSE)

#define MONO_GSHAREDVT_DEL_INVOKE_VT_OFFSET -2

typedef struct {
	MonoMethod *method;
	MonoRuntimeGenericContextInfoTemplate *entries;
	int num_entries, count_entries;
} MonoGSharedVtMethodInfo;

/* This is used by gsharedvt methods to allocate locals and compute local offsets */
typedef struct {
	int locals_size;
	/*
	 * The results of resolving the entries in MOonGSharedVtMethodInfo->entries.
	 * We use this instead of rgctx slots since these can be loaded using a load instead
	 * of a call to an rgctx fetch trampoline.
	 */
	gpointer entries [MONO_ZERO_LEN_ARRAY];
} MonoGSharedVtMethodRuntimeInfo;

typedef struct
{
	MonoMethod *invoke;
	MonoMethod *method;
	MonoMethodSignature *invoke_sig;
	MonoMethodSignature *sig;
	gpointer method_ptr;
	gpointer invoke_impl;
	gpointer impl_this;
	gpointer impl_nothis;
	gboolean need_rgctx_tramp;
} MonoDelegateTrampInfo;

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

/* Contains information describing an LLVM IMT trampoline */
typedef struct MonoJumpInfoImtTramp {
	MonoMethod *method;
	int vt_offset;
} MonoJumpInfoImtTramp;

typedef struct MonoJumpInfoGSharedVtCall MonoJumpInfoGSharedVtCall;

typedef struct MonoJumpInfo MonoJumpInfo;
struct MonoJumpInfo {
	MonoJumpInfo *next;
	/* Relocation type for patching */
	int relocation;
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
		int index;
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
		MonoJumpInfoImtTramp *imt_tramp;
		MonoJumpInfoGSharedVtCall *gsharedvt;
		MonoGSharedVtMethodInfo *gsharedvt_method;
		MonoMethodSignature *sig;
		MonoDelegateClassMethodPair *del_tramp;
	} data;
};
 
/* Contains information describing an rgctx entry */
struct MonoJumpInfoRgctxEntry {
	MonoMethod *method;
	gboolean in_mrgctx;
	MonoJumpInfo *data; /* describes the data to be loaded */
	MonoRgctxInfoType info_type;
};

/* Contains information about a gsharedvt call */
struct MonoJumpInfoGSharedVtCall {
	/* The original signature of the call */
	MonoMethodSignature *sig;
	/* The method which is called */
	MonoMethod *method;
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
	MONO_TRAMPOLINE_VCALL,
	MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD,
	MONO_TRAMPOLINE_NUM
} MonoTrampolineType;

/* These trampolines return normally to their caller */
#define MONO_TRAMPOLINE_TYPE_MUST_RETURN(t)		\
	((t) == MONO_TRAMPOLINE_CLASS_INIT ||		\
	 (t) == MONO_TRAMPOLINE_GENERIC_CLASS_INIT ||	\
	 (t) == MONO_TRAMPOLINE_RESTORE_STACK_PROT ||	\
	 (t) == MONO_TRAMPOLINE_RGCTX_LAZY_FETCH ||	\
	 (t) == MONO_TRAMPOLINE_MONITOR_ENTER ||	\
	 (t) == MONO_TRAMPOLINE_MONITOR_EXIT)

/* These trampolines receive an argument directly in a register */
#define MONO_TRAMPOLINE_TYPE_HAS_ARG(t)		\
	((t) == MONO_TRAMPOLINE_GENERIC_CLASS_INIT ||	\
	 (t) == MONO_TRAMPOLINE_MONITOR_ENTER ||		\
	 (t) == MONO_TRAMPOLINE_MONITOR_EXIT ||			\
	 (t) == MONO_TRAMPOLINE_HANDLER_BLOCK_GUARD)

/* optimization flags */
#define OPTFLAG(id,shift,name,descr) MONO_OPT_ ## id = 1 << shift,
enum {
#include "optflags-def.h"
	MONO_OPT_LAST
};

/* Flags for mini_method_compile () */
typedef enum {
	/* Whenever to run cctors during JITting */
	JIT_FLAG_RUN_CCTORS = (1 << 0),
	/* Whenever this is an AOT compilation */
	JIT_FLAG_AOT = (1 << 1),
	/* Whenever this is a full AOT compilation */
	JIT_FLAG_FULL_AOT = (1 << 2),
	/* Whenever to compile with LLVM */
	JIT_FLAG_LLVM = (1 << 3),
} JitFlags;

/* Bit-fields in the MonoBasicBlock.region */
#define MONO_REGION_TRY       0
#define MONO_REGION_FINALLY  16
#define MONO_REGION_CATCH    32
#define MONO_REGION_FAULT    64         /* Currently unused */
#define MONO_REGION_FILTER  128

#define MONO_BBLOCK_IS_IN_REGION(bblock, regtype) (((bblock)->region & (0xf << 4)) == (regtype))

#define MONO_REGION_FLAGS(region) ((region) & 0x7)
#define MONO_REGION_CLAUSE_INDEX(region) (((region) >> 8) - 1)

#define get_vreg_to_inst(cfg, vreg) ((vreg) < (cfg)->vreg_to_inst_len ? (cfg)->vreg_to_inst [(vreg)] : NULL)

#define vreg_is_volatile(cfg, vreg) (G_UNLIKELY (get_vreg_to_inst ((cfg), (vreg)) && (get_vreg_to_inst ((cfg), (vreg))->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))))

#define vreg_is_ref(cfg, vreg) ((vreg) < (cfg)->vreg_is_ref_len ? (cfg)->vreg_is_ref [(vreg)] : 0)
#define vreg_is_mp(cfg, vreg) ((vreg) < (cfg)->vreg_is_mp_len ? (cfg)->vreg_is_mp [(vreg)] : 0)

/*
 * Control Flow Graph and compilation unit information
 */
typedef struct {
	MonoMethod      *method;
	MonoMethodHeader *header;
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
	guint            num_bblocks, max_block_num;
	guint            locals_start;
	guint            num_varinfo; /* used items in varinfo */
	guint            varinfo_count; /* total storage in varinfo */
	gint             stack_offset;
	gint             max_ireg;
	gint             cil_offset_to_bb_len;
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
	MonoMethod      *method_to_register; /* The method to register in JIT info tables */
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

	MonoGenericSharingContext gsctx;
	MonoGenericContext *gsctx_context;

	MonoGSharedVtMethodInfo *gsharedvt_info;

	/* Points to the gsharedvt locals area at runtime */
	MonoInst *gsharedvt_locals_var;

	/* The localloc instruction used to initialize gsharedvt_locals_var */
	MonoInst *gsharedvt_locals_var_ins;

	/* Points to a MonoGSharedVtMethodRuntimeInfo at runtime */
	MonoInst *gsharedvt_info_var;

	/* For native-to-managed wrappers, the saved old domain */
	MonoInst *orig_domain_var;

	MonoInst *lmf_var;
	MonoInst *lmf_addr_var;

	MonoInst *stack_inbalance_var;

	unsigned char   *cil_start;
#ifdef __native_client_codegen__
	/* this alloc is not aligned, native_code */
	/* is the 32-byte aligned version of this */
	unsigned char   *native_code_alloc;
#endif
	unsigned char   *native_code;
	guint            code_size;
	guint            code_len;
	guint            prolog_end;
	guint            epilog_begin;
	guint            epilog_end;
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
	guint            full_aot : 1;
	guint            compile_llvm : 1;
	guint            got_var_allocated : 1;
	guint            ret_var_is_local : 1;
	guint            ret_var_set : 1;
	guint            globalra : 1;
	guint            unverifiable : 1;
	guint            skip_visibility : 1;
	guint            disable_reuse_registers : 1;
	guint            disable_reuse_stack_slots : 1;
	guint            disable_reuse_ref_stack_slots : 1;
	guint            disable_ref_noref_stack_slot_share : 1;
	guint            disable_initlocals_opt : 1;
	guint            disable_initlocals_opt_refs : 1;
	guint            disable_omit_fp : 1;
	guint            disable_vreg_to_lvreg : 1;
	guint            disable_deadce_vars : 1;
	guint            disable_out_of_line_bblocks : 1;
	guint            create_lmf_var : 1;
	/*
	 * When this is set, the code to push/pop the LMF from the LMF stack is generated as IR
	 * instead of being generated in emit_prolog ()/emit_epilog ().
	 */
	guint            lmf_ir : 1;
	/*
	 * Whenever to use the mono_lmf TLS variable instead of indirection through the
	 * mono_lmf_addr TLS variable.
	 */
	guint            lmf_ir_mono_lmf : 1;
	guint            gen_write_barriers : 1;
	guint            init_ref_vars : 1;
	guint            extend_live_ranges : 1;
	guint            compute_precise_live_ranges : 1;
	guint            has_got_slots : 1;
	guint            uses_rgctx_reg : 1;
	guint            uses_vtable_reg : 1;
	guint            uses_simd_intrinsics : 1;
	guint            keep_cil_nops : 1;
	guint            gen_seq_points : 1;
	guint            explicit_null_checks : 1;
	guint            compute_gc_maps : 1;
	guint            soft_breakpoints : 1;
	guint            arch_eh_jit_info : 1;
	guint            has_indirection : 1;
	guint            has_atomic_add_i4 : 1;
	guint            has_atomic_exchange_i4 : 1;
	guint            has_atomic_cas_i4 : 1;
	guint            check_pinvoke_callconv : 1;
	guint            has_unwind_info_for_epilog : 1;
	guint            disable_inline : 1;
	guint            gshared : 1;
	guint            gsharedvt : 1;
	gpointer         debug_info;
	guint32          lmf_offset;
    guint16          *intvars;
	MonoProfileCoverageInfo *coverage_info;
	GHashTable       *token_info_hash;
	MonoCompileArch  arch;
	guint32          inline_depth;
	guint32          exception_type;	/* MONO_EXCEPTION_* */
	guint32          exception_data;
	char*            exception_message;
	gpointer         exception_ptr;

	guint8 *         encoded_unwind_ops;
	guint32          encoded_unwind_ops_len;
	GSList*          unwind_ops;

	GList*           dont_inline;

	/* Fields used by the local reg allocator */
	void*            reginfo;
	int              reginfo_len;

	/* Maps vregs to their associated MonoInst's */
	/* vregs with an associated MonoInst are 'global' while others are 'local' */
	MonoInst **vreg_to_inst;

	/* Size of above array */
	guint32 vreg_to_inst_len;

	/* Marks vregs which hold a GC ref */
	/* FIXME: Use a bitmap */
	gboolean *vreg_is_ref;

	/* Size of above array */
	guint32 vreg_is_ref_len;

	/* Marks vregs which hold a managed pointer */
	/* FIXME: Use a bitmap */
	gboolean *vreg_is_mp;

	/* Size of above array */
	guint32 vreg_is_mp_len;

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

	/* Used to implement dyn_call */
	MonoInst *dyn_call_var;

	/*
	 * List of sequence points represented as IL offset+native offset pairs.
	 * Allocated using glib.
	 * IL offset can be -1 or 0xffffff to refer to the sequence points
	 * inside the prolog and epilog used to implement method entry/exit events.
	 */
	GPtrArray *seq_points;

	/* The encoded sequence point info */
	MonoSeqPointInfo *seq_point_info;

	/* Method headers which need to be freed after compilation */
	GSList *headers_to_free;

	/* Used by AOT */
	guint32 got_offset, ex_info_offset, method_info_offset, method_index;
	/* Symbol used to refer to this method in generated assembly */
	char *asm_symbol;
	char *llvm_method_name;
	int castclass_cache_index;

	MonoJitExceptionInfo *llvm_ex_info;
	guint32 llvm_ex_info_len;
	int llvm_this_reg, llvm_this_offset;

	GSList *try_block_holes;

	/* DWARF location list for 'this' */
	GSList *this_loclist;

	/* DWARF location list for 'rgctx_var' */
	GSList *rgctx_loclist;

	/* GC Maps */
   
	/* The offsets of the locals area relative to the frame pointer */
	gint locals_min_stack_offset, locals_max_stack_offset;

	/* The final CFA rule at the end of the prolog */
	int cfa_reg, cfa_offset;

	/* Points to a MonoCompileGC */
	gpointer gc_info;

	/*
	 * The encoded GC map along with its size. This contains binary data so it can be saved in an AOT
	 * image etc, but it requires a 4 byte alignment.
	 */
	guint8 *gc_map;
	guint32 gc_map_size;

	/* Error handling */
	MonoError error;

	/* Stats */
	int stat_allocate_var;
	int stat_locals_stack_size;
	int stat_basic_blocks;
	int stat_cil_code_size;
	int stat_n_regvars;
	int stat_inlineable_methods;
	int stat_inlined_methods;
	int stat_cas_demand_generation;
	int stat_code_reallocs;
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
	gint32 methods_compiled;
	gint32 methods_aot;
	gint32 methods_lookups;
	gint32 allocate_var;
	gint32 cil_code_size;
	gint32 native_code_size;
	gint32 code_reallocs;
	gint32 max_code_size_ratio;
	gint32 biggest_method_size;
	gint32 allocated_code_size;
	gint32 inlineable_methods;
	gint32 inlined_methods;
	gint32 basic_blocks;
	gint32 max_basic_blocks;
	gint32 locals_stack_size;
	gint32 regvars;
	gint32 cas_declsec_check;
	gint32 cas_linkdemand_icall;
	gint32 cas_linkdemand_pinvoke;
	gint32 cas_linkdemand_aptc;
	gint32 cas_linkdemand;
	gint32 cas_demand_generation;
	gint32 generic_virtual_invocations;
	gint32 alias_found;
	gint32 alias_removed;
	gint32 loads_eliminated;
	gint32 stores_eliminated;
	int methods_with_llvm;
	int methods_without_llvm;
	char *max_ratio_method;
	char *biggest_method;
	double jit_time;
	gboolean enabled;
} MonoJitStats;

extern MonoJitStats mono_jit_stats;

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
#define OP_DUMMY_PCONST OP_DUMMY_I8CONST
#define OP_PADD OP_LADD
#define OP_PADD_IMM OP_LADD_IMM
#define OP_PAND_IMM OP_LAND_IMM
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
#define OP_DUMMY_PCONST OP_DUMMY_ICONST
#define OP_PADD OP_IADD
#define OP_PADD_IMM OP_IADD_IMM
#define OP_PAND_IMM OP_IAND_IMM
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

/* Opcodes to load/store regsize quantities */
#if defined (__mono_ilp32__)
#define OP_LOADR_MEMBASE OP_LOADI8_MEMBASE
#define OP_STORER_MEMBASE_REG OP_STOREI8_MEMBASE_REG
#else
#define OP_LOADR_MEMBASE OP_LOAD_MEMBASE
#define OP_STORER_MEMBASE_REG OP_STORE_MEMBASE_REG
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
	gboolean reverse_pinvoke_exceptions;
	gboolean collect_pagefault_stats;
	gboolean break_on_unverified;
	gboolean better_cast_details;
	gboolean mdb_optimizations;
	gboolean no_gdb_backtrace;
	gboolean suspend_on_sigsegv;
	gboolean suspend_on_exception;
	gboolean suspend_on_unhandled;
	gboolean dyn_runtime_invoke;
	gboolean gdb;
	gboolean gen_seq_points;
	gboolean explicit_null_checks;
	/*
	 * Fill stack frames with 0x2a in method prologs. This helps with the
	 * debugging of the stack marking code in the GC.
	 */
	gboolean init_stacks;

	/*
	 * Whenever to implement single stepping and breakpoints without signals in the
	 * soft debugger. This is useful on platforms without signals, like the ps3, or during
	 * runtime debugging, since it avoids SIGSEGVs when a single step location or breakpoint
	 * is hit.
	 */
	gboolean soft_breakpoints;
	/*
	 * Whenever to break in the debugger using G_BREAKPOINT on unhandled exceptions.
	 */
	gboolean break_on_exc;
	/*
	 * Load AOT JIT info eagerly.
	 */
	gboolean load_aot_jit_info_eagerly;
	/*
	 * Check for pinvoke calling convention mismatches.
	 */
	gboolean check_pinvoke_callconv;
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
	MONO_EXC_ARGUMENT,
	MONO_EXC_INTRINS_NUM
};

enum {
	MINI_TOKEN_SOURCE_CLASS,
	MINI_TOKEN_SOURCE_METHOD,
	MINI_TOKEN_SOURCE_FIELD
};

 /* 
  * Information about a trampoline function.
  */
 typedef struct
 {
	/* 
	 * The native code of the trampoline. Not owned by this structure.
	 */
 	guint8 *code;
 	guint32 code_size;
	/*
	 * The name of the trampoline which can be used in AOT/xdebug. Owned by this
	 * structure.
	 */
 	char *name;
	/* 
	 * Patches required by the trampoline when aot-ing. Owned by this structure.
	 */
	MonoJumpInfo *ji;
	/*
	 * Unwind information. Owned by this structure.
	 */
	GSList *unwind_ops;

	 /*
	  * Encoded unwind info loaded from AOT images
	  */
	 guint8 *uw_info;
	 guint32 uw_info_len;
} MonoTrampInfo;

typedef void (*MonoInstFunc) (MonoInst *tree, gpointer data);

/* main function */
MONO_API int         mono_main                      (int argc, char* argv[]);
MONO_API void        mono_set_defaults              (int verbose_level, guint32 opts);
MonoDomain* mini_init                      (const char *filename, const char *runtime_version) MONO_INTERNAL;
void        mini_cleanup                   (MonoDomain *domain) MONO_INTERNAL;
MonoDebugOptions *mini_get_debug_options   (void) MONO_INTERNAL;

/* helper methods */
void      mono_disable_optimizations       (guint32 opts) MONO_INTERNAL;
void      mono_set_optimizations           (guint32 opts) MONO_INTERNAL;
guint32   mono_get_optimizations_for_method (MonoMethod *method, guint32 default_opt) MONO_INTERNAL;
void      mono_set_verbose_level           (guint32 level) MONO_INTERNAL;
MonoJumpInfoToken* mono_jump_info_token_new (MonoMemPool *mp, MonoImage *image, guint32 token) MONO_INTERNAL;
MonoJumpInfoToken* mono_jump_info_token_new2 (MonoMemPool *mp, MonoImage *image, guint32 token, MonoGenericContext *context) MONO_INTERNAL;
MonoInst* mono_find_spvar_for_region        (MonoCompile *cfg, int region) MONO_INTERNAL;
MonoInst* mono_find_exvar_for_offset        (MonoCompile *cfg, int offset) MONO_INTERNAL;
int       mono_get_block_region_notry       (MonoCompile *cfg, int region) MONO_LLVM_INTERNAL;

void      mono_precompile_assemblies        (void) MONO_INTERNAL;
MONO_API int       mono_parse_default_optimizations  (const char* p);
void      mono_bblock_add_inst              (MonoBasicBlock *bb, MonoInst *inst) MONO_LLVM_INTERNAL;
void      mono_bblock_insert_after_ins      (MonoBasicBlock *bb, MonoInst *ins, MonoInst *ins_to_insert) MONO_INTERNAL;
void      mono_bblock_insert_before_ins     (MonoBasicBlock *bb, MonoInst *ins, MonoInst *ins_to_insert) MONO_INTERNAL;
void      mono_verify_bblock                (MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_verify_cfg                   (MonoCompile *cfg) MONO_INTERNAL;
void      mono_constant_fold                (MonoCompile *cfg) MONO_INTERNAL;
MonoInst* mono_constant_fold_ins            (MonoCompile *cfg, MonoInst *ins, MonoInst *arg1, MonoInst *arg2, gboolean overwrite) MONO_INTERNAL;
int       mono_eval_cond_branch             (MonoInst *branch) MONO_INTERNAL;
int       mono_is_power_of_two              (guint32 val) MONO_LLVM_INTERNAL;
void      mono_cprop_local                  (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **acp, int acp_size) MONO_INTERNAL;
MonoInst* mono_compile_create_var           (MonoCompile *cfg, MonoType *type, int opcode) MONO_INTERNAL;
MonoInst* mono_compile_create_var_for_vreg  (MonoCompile *cfg, MonoType *type, int opcode, int vreg) MONO_INTERNAL;
void      mono_compile_make_var_load        (MonoCompile *cfg, MonoInst *dest, gssize var_index) MONO_INTERNAL;
MonoInst* mono_compile_create_var_load      (MonoCompile *cfg, gssize var_index) MONO_INTERNAL;
MonoInst* mono_compile_create_var_store     (MonoCompile *cfg, gssize var_index, MonoInst *value) MONO_INTERNAL;
MonoType* mono_type_from_stack_type         (MonoInst *ins) MONO_INTERNAL;
guint32   mono_alloc_ireg                   (MonoCompile *cfg) MONO_LLVM_INTERNAL;
guint32   mono_alloc_lreg                   (MonoCompile *cfg) MONO_LLVM_INTERNAL;
guint32   mono_alloc_freg                   (MonoCompile *cfg) MONO_LLVM_INTERNAL;
guint32   mono_alloc_preg                   (MonoCompile *cfg) MONO_LLVM_INTERNAL;
guint32   mono_alloc_dreg                   (MonoCompile *cfg, MonoStackType stack_type) MONO_INTERNAL;
guint32   mono_alloc_ireg_ref               (MonoCompile *cfg) MONO_LLVM_INTERNAL;
guint32   mono_alloc_ireg_mp                (MonoCompile *cfg) MONO_LLVM_INTERNAL;
guint32   mono_alloc_ireg_copy              (MonoCompile *cfg, guint32 vreg) MONO_LLVM_INTERNAL;
void      mono_mark_vreg_as_ref             (MonoCompile *cfg, int vreg) MONO_INTERNAL;
void      mono_mark_vreg_as_mp              (MonoCompile *cfg, int vreg) MONO_INTERNAL;

void      mono_link_bblock                  (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to) MONO_INTERNAL;
void      mono_unlink_bblock                (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to) MONO_INTERNAL;
gboolean  mono_bblocks_linked               (MonoBasicBlock *bb1, MonoBasicBlock *bb2) MONO_INTERNAL;
void      mono_remove_bblock                (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_nullify_basic_block          (MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_merge_basic_blocks           (MonoCompile *cfg, MonoBasicBlock *bb, MonoBasicBlock *bbn) MONO_INTERNAL;
void      mono_optimize_branches            (MonoCompile *cfg) MONO_INTERNAL;

void      mono_blockset_print               (MonoCompile *cfg, MonoBitSet *set, const char *name, guint idom) MONO_INTERNAL;
void      mono_print_ji                     (const MonoJumpInfo *ji) MONO_INTERNAL;
void      mono_print_ins_index              (int i, MonoInst *ins) MONO_INTERNAL;
void      mono_print_ins                    (MonoInst *ins) MONO_INTERNAL;
void      mono_print_bb                     (MonoBasicBlock *bb, const char *msg) MONO_INTERNAL;
void      mono_print_code                   (MonoCompile *cfg, const char *msg) MONO_INTERNAL;
MONO_API void      mono_print_method_from_ip         (void *ip);
MONO_API char     *mono_pmip                         (void *ip);
gboolean  mono_debug_count                  (void) MONO_INTERNAL;
MONO_API const char* mono_inst_name                  (int op);
int       mono_op_to_op_imm                 (int opcode) MONO_INTERNAL;
int       mono_op_imm_to_op                 (int opcode) MONO_INTERNAL;
int       mono_load_membase_to_load_mem     (int opcode) MONO_INTERNAL;
guint     mono_type_to_load_membase         (MonoCompile *cfg, MonoType *type) MONO_INTERNAL;
guint     mono_type_to_store_membase        (MonoCompile *cfg, MonoType *type) MONO_INTERNAL;
guint     mini_type_to_stind                (MonoCompile* cfg, MonoType *type) MONO_INTERNAL;
guint32   mono_reverse_branch_op            (guint32 opcode) MONO_INTERNAL;
void      mono_disassemble_code             (MonoCompile *cfg, guint8 *code, int size, char *id) MONO_INTERNAL;
void      mono_add_patch_info               (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target) MONO_LLVM_INTERNAL;
void      mono_add_patch_info_rel           (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target, int relocation) MONO_LLVM_INTERNAL;
void      mono_remove_patch_info            (MonoCompile *cfg, int ip) MONO_INTERNAL;
MonoJumpInfo* mono_patch_info_dup_mp        (MonoMemPool *mp, MonoJumpInfo *patch_info) MONO_INTERNAL;
guint     mono_patch_info_hash (gconstpointer data) MONO_INTERNAL;
gint      mono_patch_info_equal (gconstpointer ka, gconstpointer kb) MONO_INTERNAL;
MonoJumpInfo *mono_patch_info_list_prepend  (MonoJumpInfo *list, int ip, MonoJumpInfoType type, gconstpointer target) MONO_INTERNAL;
gpointer  mono_resolve_patch_target         (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *patch_info, gboolean run_cctors) MONO_LLVM_INTERNAL;
gpointer  mono_jit_find_compiled_method_with_jit_info (MonoDomain *domain, MonoMethod *method, MonoJitInfo **ji) MONO_INTERNAL;
gpointer  mono_jit_find_compiled_method     (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;
gpointer  mono_jit_compile_method           (MonoMethod *method) MONO_INTERNAL;
MonoLMF * mono_get_lmf                      (void) MONO_INTERNAL;
MonoLMF** mono_get_lmf_addr                 (void) MONO_INTERNAL;
void      mono_set_lmf                      (MonoLMF *lmf) MONO_INTERNAL;
MonoJitTlsData* mono_get_jit_tls            (void) MONO_INTERNAL;
MONO_API MonoDomain *mono_jit_thread_attach          (MonoDomain *domain);
MONO_API void      mono_jit_set_domain               (MonoDomain *domain);
gint32    mono_get_jit_tls_offset           (void) MONO_INTERNAL;
gint32    mono_get_lmf_tls_offset           (void) MONO_INTERNAL;
gint32    mono_get_lmf_addr_tls_offset      (void) MONO_INTERNAL;
int       mini_get_tls_offset               (MonoTlsKey key) MONO_INTERNAL;
gboolean  mini_tls_get_supported            (MonoCompile *cfg, MonoTlsKey key) MONO_INTERNAL;
MonoInst* mono_create_tls_get               (MonoCompile *cfg, MonoTlsKey key) MONO_INTERNAL;
MonoInst* mono_get_jit_tls_intrinsic        (MonoCompile *cfg) MONO_INTERNAL;
MonoInst* mono_get_domain_intrinsic         (MonoCompile* cfg) MONO_INTERNAL;
MonoInst* mono_get_thread_intrinsic         (MonoCompile* cfg) MONO_INTERNAL;
MonoInst* mono_get_lmf_intrinsic            (MonoCompile* cfg) MONO_INTERNAL;
MonoInst* mono_get_lmf_addr_intrinsic       (MonoCompile* cfg) MONO_INTERNAL;
GList    *mono_varlist_insert_sorted        (MonoCompile *cfg, GList *list, MonoMethodVar *mv, int sort_type) MONO_INTERNAL;
GList    *mono_varlist_sort                 (MonoCompile *cfg, GList *list, int sort_type) MONO_INTERNAL;
void      mono_analyze_liveness             (MonoCompile *cfg) MONO_INTERNAL;
void      mono_analyze_liveness_gc          (MonoCompile *cfg) MONO_INTERNAL;
void      mono_linear_scan                  (MonoCompile *cfg, GList *vars, GList *regs, regmask_t *used_mask) MONO_INTERNAL;
void      mono_global_regalloc              (MonoCompile *cfg) MONO_INTERNAL;
void      mono_create_jump_table            (MonoCompile *cfg, MonoInst *label, MonoBasicBlock **bbs, int num_blocks) MONO_INTERNAL;
int       mono_compile_assembly             (MonoAssembly *ass, guint32 opts, const char *aot_options) MONO_INTERNAL;
MonoCompile *mini_method_compile            (MonoMethod *method, guint32 opts, MonoDomain *domain, JitFlags flags, int parts) MONO_INTERNAL;
void      mono_destroy_compile              (MonoCompile *cfg) MONO_INTERNAL;
MonoJitICallInfo *mono_find_jit_opcode_emulation (int opcode) MONO_INTERNAL;
void	  mono_print_ins_index (int i, MonoInst *ins) MONO_INTERNAL;
void	  mono_print_ins (MonoInst *ins) MONO_INTERNAL;
gboolean  mini_assembly_can_skip_verification (MonoDomain *domain, MonoMethod *method) MONO_INTERNAL;
gboolean mono_compile_is_broken (MonoCompile *cfg, MonoMethod *method, gboolean fail_compile) MONO_INTERNAL;
MonoInst *mono_get_got_var (MonoCompile *cfg) MONO_INTERNAL;
void      mono_add_seq_point (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, int native_offset) MONO_INTERNAL;
void      mono_add_var_location (MonoCompile *cfg, MonoInst *var, gboolean is_reg, int reg, int offset, int from, int to) MONO_INTERNAL;
MonoInst* mono_emit_jit_icall (MonoCompile *cfg, gconstpointer func, MonoInst **args) MONO_INTERNAL;
MonoInst* mono_emit_method_call (MonoCompile *cfg, MonoMethod *method, MonoInst **args, MonoInst *this) MONO_INTERNAL;
void      mono_create_helper_signatures (void) MONO_INTERNAL;

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

/* Native Client functions */
gpointer mono_realloc_native_code(MonoCompile *cfg);
#ifdef __native_client_codegen__
void mono_nacl_align_inst(guint8 **pcode, int instlen);
void mono_nacl_align_call(guint8 **start, guint8 **pcode);
guint8 *mono_nacl_pad_call(guint8 *code, guint8 ilength);
guint8 *mono_nacl_align(guint8 *code);
void mono_nacl_fix_patches(const guint8 *code, MonoJumpInfo *ji);
/* Defined for each arch */
guint8 *mono_arch_nacl_pad(guint8 *code, int pad);
guint8 *mono_arch_nacl_skip_nops(guint8 *code);

#if defined(TARGET_X86)
#define kNaClAlignment kNaClAlignmentX86
#define kNaClAlignmentMask kNaClAlignmentMaskX86
#elif defined(TARGET_AMD64)
#define kNaClAlignment kNaClAlignmentAMD64
#define kNaClAlignmentMask kNaClAlignmentMaskAMD64
#elif defined(TARGET_ARM)
#define kNaClAlignment kNaClAlignmentARM
#define kNaClAlignmentMask kNaClAlignmentMaskARM
#endif

#define NACL_BUNDLE_ALIGN_UP(p) ((((p)+kNaClAlignmentMask)) & ~kNaClAlignmentMask)
#endif

#if defined(__native_client__) || defined(__native_client_codegen__)
extern volatile int __nacl_thread_suspension_needed;
void __nacl_suspend_thread_if_needed(void);
void mono_nacl_gc(void);
#endif

#if defined(__native_client_codegen__) || defined(__native_client__)
#define NACL_SIZE(a, b) (b)
#else
#define NACL_SIZE(a, b) (a)
#endif

static inline MonoMethod*
jinfo_get_method (MonoJitInfo *ji)
{
	return mono_jit_info_get_method (ji);
}

/* AOT */
void      mono_aot_init                     (void) MONO_INTERNAL;
void      mono_aot_cleanup                  (void) MONO_INTERNAL;
gpointer  mono_aot_get_method               (MonoDomain *domain,
											 MonoMethod *method) MONO_INTERNAL;
gpointer  mono_aot_get_method_from_token    (MonoDomain *domain, MonoImage *image, guint32 token) MONO_INTERNAL;
gboolean  mono_aot_is_got_entry             (guint8 *code, guint8 *addr) MONO_INTERNAL;
guint8*   mono_aot_get_plt_entry            (guint8 *code) MONO_INTERNAL;
guint32   mono_aot_get_plt_info_offset      (mgreg_t *regs, guint8 *code) MONO_INTERNAL;
gboolean  mono_aot_get_cached_class_info    (MonoClass *klass, MonoCachedClassInfo *res) MONO_INTERNAL;
gboolean  mono_aot_get_class_from_name      (MonoImage *image, const char *name_space, const char *name, MonoClass **klass) MONO_INTERNAL;
MonoJitInfo* mono_aot_find_jit_info         (MonoDomain *domain, MonoImage *image, gpointer addr) MONO_INTERNAL;
gpointer mono_aot_plt_resolve               (gpointer aot_module, guint32 plt_info_offset, guint8 *code) MONO_INTERNAL;
void     mono_aot_patch_plt_entry           (guint8 *code, guint8 *plt_entry, gpointer *got, mgreg_t *regs, guint8 *addr) MONO_INTERNAL;
gpointer mono_aot_get_method_from_vt_slot   (MonoDomain *domain, MonoVTable *vtable, int slot) MONO_INTERNAL;
gpointer mono_aot_create_specific_trampoline   (MonoImage *image, gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len) MONO_INTERNAL;
gpointer mono_aot_get_trampoline            (const char *name) MONO_INTERNAL;
gpointer mono_aot_get_trampoline_full       (const char *name, MonoTrampInfo **out_tinfo) MONO_INTERNAL;
gpointer mono_aot_get_unbox_trampoline      (MonoMethod *method) MONO_INTERNAL;
gpointer mono_aot_get_lazy_fetch_trampoline (guint32 slot) MONO_INTERNAL;
gpointer mono_aot_get_static_rgctx_trampoline (gpointer ctx, gpointer addr) MONO_INTERNAL;
gpointer mono_aot_get_imt_thunk             (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp) MONO_INTERNAL;
gpointer mono_aot_get_gsharedvt_arg_trampoline(gpointer arg, gpointer addr) MONO_INTERNAL;
guint8*  mono_aot_get_unwind_info           (MonoJitInfo *ji, guint32 *unwind_info_len) MONO_INTERNAL;
guint32  mono_aot_method_hash               (MonoMethod *method) MONO_INTERNAL;
MonoMethod* mono_aot_get_array_helper_from_wrapper (MonoMethod *method) MONO_INTERNAL;
guint32  mono_aot_get_got_offset            (MonoJumpInfo *ji) MONO_LLVM_INTERNAL;
char*    mono_aot_get_method_name           (MonoCompile *cfg) MONO_LLVM_INTERNAL;
gboolean mono_aot_is_direct_callable        (MonoJumpInfo *patch_info) MONO_LLVM_INTERNAL;
void     mono_aot_mark_unused_llvm_plt_entry(MonoJumpInfo *patch_info) MONO_LLVM_INTERNAL;
char*    mono_aot_get_plt_symbol            (MonoJumpInfoType type, gconstpointer data) MONO_LLVM_INTERNAL;
int      mono_aot_get_method_index          (MonoMethod *method) MONO_LLVM_INTERNAL;
MonoJumpInfo* mono_aot_patch_info_dup       (MonoJumpInfo* ji) MONO_LLVM_INTERNAL;
void     mono_aot_set_make_unreadable       (gboolean unreadable) MONO_INTERNAL;
gboolean mono_aot_is_pagefault              (void *ptr) MONO_INTERNAL;
void     mono_aot_handle_pagefault          (void *ptr) MONO_INTERNAL;
void     mono_aot_register_jit_icall        (const char *name, gpointer addr) MONO_INTERNAL;
void*    mono_aot_readonly_field_override   (MonoClassField *field) MONO_INTERNAL;

/* This is an exported function */
MONO_API void     mono_aot_register_globals          (gpointer *globals);
/* This too */
MONO_API void     mono_aot_register_module           (gpointer *aot_info);

void     mono_xdebug_init                   (const char *xdebug_opts) MONO_INTERNAL;
void     mono_save_xdebug_info              (MonoCompile *cfg) MONO_INTERNAL;
void     mono_save_trampoline_xdebug_info   (MonoTrampInfo *info) MONO_INTERNAL;
/* This is an exported function */
void     mono_xdebug_flush                  (void);

/* LLVM backend */
void     mono_llvm_init                     (void) MONO_LLVM_INTERNAL;
void     mono_llvm_cleanup                  (void) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_method              (MonoCompile *cfg) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_call                (MonoCompile *cfg, MonoCallInst *call) MONO_LLVM_INTERNAL;
void     mono_llvm_create_aot_module        (const char *got_symbol) MONO_LLVM_INTERNAL;
void     mono_llvm_emit_aot_module          (const char *filename, const char *cu_name, int got_size) MONO_LLVM_INTERNAL;
void     mono_llvm_check_method_supported   (MonoCompile *cfg) MONO_LLVM_INTERNAL;
void     mono_llvm_free_domain_info         (MonoDomain *domain) MONO_LLVM_INTERNAL;

gboolean mini_llvm_init                     (void);

gboolean  mono_method_blittable             (MonoMethod *method) MONO_INTERNAL;
gboolean  mono_method_same_domain           (MonoJitInfo *caller, MonoJitInfo *callee) MONO_INTERNAL;

void      mono_register_opcode_emulation    (int opcode, const char* name, const char *sigstr, gpointer func, gboolean no_throw) MONO_INTERNAL;
void      mono_draw_graph                   (MonoCompile *cfg, MonoGraphOptions draw_options) MONO_INTERNAL;
void      mono_add_ins_to_end               (MonoBasicBlock *bb, MonoInst *inst) MONO_INTERNAL;
gpointer  mono_create_ftnptr                (MonoDomain *domain, gpointer addr) MONO_INTERNAL;

MONO_API void      mono_replace_ins                  (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, MonoInst **prev, MonoBasicBlock *first_bb, MonoBasicBlock *last_bb);

int               mono_find_method_opcode      (MonoMethod *method) MONO_INTERNAL;
MonoJitICallInfo *mono_register_jit_icall      (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save) MONO_INTERNAL;
gconstpointer     mono_icall_get_wrapper       (MonoJitICallInfo* callinfo) MONO_LLVM_INTERNAL;

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
gpointer          mono_create_jit_trampoline_in_domain (MonoDomain *domain, MonoMethod *method) MONO_LLVM_INTERNAL;
gpointer          mono_create_delegate_trampoline (MonoDomain *domain, MonoClass *klass) MONO_INTERNAL;
MonoDelegateTrampInfo* mono_create_delegate_trampoline_info (MonoDomain *domain, MonoClass *klass, MonoMethod *method) MONO_INTERNAL;
gpointer          mono_create_delegate_virtual_trampoline (MonoDomain *domain, MonoClass *klass, MonoMethod *method) MONO_INTERNAL;
gpointer          mono_create_rgctx_lazy_fetch_trampoline (guint32 offset) MONO_INTERNAL;
gpointer          mono_create_monitor_enter_trampoline (void) MONO_INTERNAL;
gpointer          mono_create_monitor_exit_trampoline (void) MONO_INTERNAL;
gpointer          mono_create_static_rgctx_trampoline (MonoMethod *m, gpointer addr) MONO_INTERNAL;
gpointer          mono_create_llvm_imt_trampoline (MonoDomain *domain, MonoMethod *m, int vt_offset) MONO_LLVM_INTERNAL;
MonoVTable*       mono_find_class_init_trampoline_by_addr (gconstpointer addr) MONO_INTERNAL;
guint32           mono_find_rgctx_lazy_fetch_trampoline_by_addr (gconstpointer addr) MONO_INTERNAL;
gpointer          mono_magic_trampoline (mgreg_t *regs, guint8 *code, gpointer arg, guint8* tramp) MONO_INTERNAL;
#ifndef DISABLE_REMOTING
gpointer          mono_generic_virtual_remoting_trampoline (mgreg_t *regs, guint8 *code, MonoMethod *m, guint8 *tramp) MONO_INTERNAL;
#endif
gpointer          mono_delegate_trampoline (mgreg_t *regs, guint8 *code, gpointer *tramp_data, guint8* tramp) MONO_INTERNAL;
gpointer          mono_aot_trampoline (mgreg_t *regs, guint8 *code, guint8 *token_info, 
									   guint8* tramp) MONO_INTERNAL;
gpointer          mono_aot_plt_trampoline (mgreg_t *regs, guint8 *code, guint8 *token_info, 
										   guint8* tramp) MONO_INTERNAL;
void              mono_class_init_trampoline (mgreg_t *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp) MONO_INTERNAL;
void              mono_generic_class_init_trampoline (mgreg_t *regs, guint8 *code, MonoVTable *vtable, guint8 *tramp) MONO_INTERNAL;
void              mono_monitor_enter_trampoline (mgreg_t *regs, guint8 *code, MonoObject *obj, guint8 *tramp) MONO_INTERNAL;
void              mono_monitor_exit_trampoline (mgreg_t *regs, guint8 *code, MonoObject *obj, guint8 *tramp) MONO_INTERNAL;
gconstpointer     mono_get_trampoline_func (MonoTrampolineType tramp_type);
gpointer          mini_get_vtable_trampoline (int slot_index) MONO_INTERNAL;
char*             mono_get_generic_trampoline_name (MonoTrampolineType tramp_type) MONO_INTERNAL;
char*             mono_get_rgctx_fetch_trampoline_name (int slot) MONO_INTERNAL;
gpointer          mini_get_nullified_class_init_trampoline (void) MONO_INTERNAL;
gpointer          mini_add_method_trampoline (MonoMethod *orig_method, MonoMethod *m, gpointer compiled_method, gboolean add_static_rgctx_tramp, gboolean add_unbox_tramp) MONO_INTERNAL;
gboolean          mini_jit_info_is_gsharedvt (MonoJitInfo *ji) MONO_INTERNAL;

gboolean          mono_running_on_valgrind (void) MONO_INTERNAL;
void*             mono_global_codeman_reserve (int size) MONO_INTERNAL;
void*             nacl_global_codeman_get_dest(void *data) MONO_INTERNAL;
void              mono_global_codeman_commit(void *data, int size, int newsize) MONO_INTERNAL;
void              nacl_global_codeman_validate(guint8 **buf_base, int buf_size, guint8 **code_end) MONO_INTERNAL;
const char       *mono_regname_full (int reg, int bank) MONO_INTERNAL;
gint32*           mono_allocate_stack_slots (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align) MONO_INTERNAL;
void              mono_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
MonoInst         *mono_branch_optimize_exception_target (MonoCompile *cfg, MonoBasicBlock *bb, const char * exname) MONO_INTERNAL;
void              mono_remove_critical_edges (MonoCompile *cfg) MONO_INTERNAL;
gboolean          mono_is_regsize_var (MonoType *t) MONO_INTERNAL;
void              mini_emit_memcpy (MonoCompile *cfg, int destreg, int doffset, int srcreg, int soffset, int size, int align) MONO_INTERNAL;
void              mini_emit_stobj (MonoCompile *cfg, MonoInst *dest, MonoInst *src, MonoClass *klass, gboolean native) MONO_INTERNAL;
void              mini_emit_initobj (MonoCompile *cfg, MonoInst *dest, const guchar *ip, MonoClass *klass) MONO_INTERNAL;
CompRelation      mono_opcode_to_cond (int opcode) MONO_LLVM_INTERNAL;
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
MonoTrampInfo*    mono_tramp_info_create (const char *name, guint8 *code, guint32 code_size, MonoJumpInfo *ji, GSList *unwind_ops) MONO_INTERNAL;
void              mono_tramp_info_free (MonoTrampInfo *info) MONO_INTERNAL;
void              mono_tramp_info_register (MonoTrampInfo *info) MONO_INTERNAL;
int               mini_exception_id_by_name (const char *name) MONO_INTERNAL;

int               mono_method_to_ir (MonoCompile *cfg, MonoMethod *method, MonoBasicBlock *start_bblock, MonoBasicBlock *end_bblock, 
									 MonoInst *return_var, MonoInst **inline_args,
									 guint inline_offset, gboolean is_virtual_call) MONO_INTERNAL;

MonoInst         *mono_decompose_opcode (MonoCompile *cfg, MonoInst *ins) MONO_INTERNAL;
void              mono_decompose_long_opts (MonoCompile *cfg) MONO_INTERNAL;
void              mono_decompose_vtype_opts (MonoCompile *cfg) MONO_INTERNAL;
void              mono_decompose_vtype_opts_llvm (MonoCompile *cfg) MONO_INTERNAL;
void              mono_decompose_array_access_opts (MonoCompile *cfg) MONO_INTERNAL;
void              mono_decompose_soft_float (MonoCompile *cfg) MONO_INTERNAL;
void              mono_handle_global_vregs (MonoCompile *cfg) MONO_INTERNAL;
void              mono_spill_global_vars (MonoCompile *cfg, gboolean *need_local_opts) MONO_INTERNAL;
void              mono_if_conversion (MonoCompile *cfg) MONO_INTERNAL;

/* virtual function delegate */
gpointer          mono_get_delegate_virtual_invoke_impl  (MonoMethodSignature *sig, MonoMethod *method) MONO_INTERNAL;

/* methods that must be provided by the arch-specific port */
void      mono_arch_init                        (void) MONO_INTERNAL;
void      mono_arch_finish_init                 (void) MONO_INTERNAL;
void      mono_arch_cleanup                     (void) MONO_INTERNAL;
void      mono_arch_cpu_init                    (void) MONO_INTERNAL;
guint32   mono_arch_cpu_optimizations           (guint32 *exclude_mask) MONO_INTERNAL;
void      mono_arch_instrument_mem_needs        (MonoMethod *method, int *stack, int *code) MONO_INTERNAL;
void     *mono_arch_instrument_prolog           (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments) MONO_INTERNAL;
void     *mono_arch_instrument_epilog           (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments) MONO_INTERNAL;
void     *mono_arch_instrument_epilog_full     (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments, gboolean preserve_argument_registers) MONO_INTERNAL;
void      mono_codegen                          (MonoCompile *cfg) MONO_INTERNAL;
void      mono_call_inst_add_outarg_reg         (MonoCompile *cfg, MonoCallInst *call, int vreg, int hreg, int bank) MONO_LLVM_INTERNAL;
void      mono_call_inst_add_outarg_vt          (MonoCompile *cfg, MonoCallInst *call, MonoInst *outarg_vt) MONO_INTERNAL;
const char *mono_arch_regname                   (int reg) MONO_INTERNAL;
const char *mono_arch_fregname                  (int reg) MONO_INTERNAL;
void      mono_arch_exceptions_init             (void) MONO_INTERNAL;
guchar*   mono_arch_create_generic_trampoline   (MonoTrampolineType tramp_type, MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_create_rgctx_lazy_fetch_trampoline (guint32 slot, MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_create_general_rgctx_lazy_fetch_trampoline (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_create_generic_class_init_trampoline (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_nullified_class_init_trampoline (MonoTrampInfo **info) MONO_INTERNAL;
gpointer  mono_arch_create_monitor_enter_trampoline (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_create_monitor_exit_trampoline (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
guint8   *mono_arch_create_llvm_native_thunk     (MonoDomain *domain, guint8* addr) MONO_LLVM_INTERNAL;
GList    *mono_arch_get_allocatable_int_vars    (MonoCompile *cfg) MONO_INTERNAL;
GList    *mono_arch_get_global_int_regs         (MonoCompile *cfg) MONO_INTERNAL;
GList    *mono_arch_get_global_fp_regs          (MonoCompile *cfg) MONO_INTERNAL;
GList    *mono_arch_get_iregs_clobbered_by_call (MonoCallInst *call) MONO_INTERNAL;
GList    *mono_arch_get_fregs_clobbered_by_call (MonoCallInst *call) MONO_INTERNAL;
guint32   mono_arch_regalloc_cost               (MonoCompile *cfg, MonoMethodVar *vmv) MONO_INTERNAL;
void      mono_arch_patch_code                  (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, MonoCodeManager *dyn_code_mp, gboolean run_cctors) MONO_INTERNAL;
void      mono_arch_flush_icache                (guint8 *code, gint size) MONO_INTERNAL;
int       mono_arch_max_epilog_size             (MonoCompile *cfg) MONO_INTERNAL;
guint8   *mono_arch_emit_prolog                 (MonoCompile *cfg) MONO_INTERNAL;
void      mono_arch_emit_epilog                 (MonoCompile *cfg) MONO_INTERNAL;
void      mono_arch_emit_exceptions             (MonoCompile *cfg) MONO_INTERNAL;
void      mono_arch_lowering_pass               (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_arch_peephole_pass_1             (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_arch_peephole_pass_2             (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_arch_output_basic_block          (MonoCompile *cfg, MonoBasicBlock *bb) MONO_INTERNAL;
void      mono_arch_free_jit_tls_data           (MonoJitTlsData *tls) MONO_INTERNAL;
void      mono_arch_fill_argument_info          (MonoCompile *cfg) MONO_INTERNAL;
void      mono_arch_allocate_vars               (MonoCompile *m) MONO_INTERNAL;
int       mono_arch_get_argument_info           (MonoGenericSharingContext *gsctx, MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info) MONO_INTERNAL;
gboolean  mono_arch_print_tree			(MonoInst *tree, int arity) MONO_INTERNAL;
void      mono_arch_emit_call                   (MonoCompile *cfg, MonoCallInst *call) MONO_INTERNAL;
void      mono_arch_emit_outarg_vt              (MonoCompile *cfg, MonoInst *ins, MonoInst *src) MONO_INTERNAL;
void      mono_arch_emit_setret                 (MonoCompile *cfg, MonoMethod *method, MonoInst *val) MONO_INTERNAL;
MonoDynCallInfo *mono_arch_dyn_call_prepare     (MonoMethodSignature *sig) MONO_INTERNAL;
void      mono_arch_dyn_call_free               (MonoDynCallInfo *info) MONO_INTERNAL;
void      mono_arch_start_dyn_call              (MonoDynCallInfo *info, gpointer **args, guint8 *ret, guint8 *buf, int buf_len) MONO_INTERNAL;
void      mono_arch_finish_dyn_call             (MonoDynCallInfo *info, guint8 *buf) MONO_INTERNAL;
MonoInst *mono_arch_emit_inst_for_method        (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args) MONO_INTERNAL;
void      mono_arch_decompose_opts              (MonoCompile *cfg, MonoInst *ins) MONO_INTERNAL;
void      mono_arch_decompose_long_opts         (MonoCompile *cfg, MonoInst *ins) MONO_INTERNAL;
GSList*   mono_arch_get_delegate_invoke_impls   (void) MONO_INTERNAL;
LLVMCallInfo* mono_arch_get_llvm_call_info      (MonoCompile *cfg, MonoMethodSignature *sig) MONO_LLVM_INTERNAL;
guint8*   mono_arch_emit_load_got_addr          (guint8 *start, guint8 *code, MonoCompile *cfg, MonoJumpInfo **ji) MONO_INTERNAL;
guint8*   mono_arch_emit_load_aotconst          (guint8 *start, guint8 *code, MonoJumpInfo **ji, int tramp_type, gconstpointer target) MONO_INTERNAL;
GSList*   mono_arch_get_cie_program             (void) MONO_INTERNAL;
void      mono_arch_set_target                  (char *mtriple) MONO_INTERNAL;
gboolean  mono_arch_gsharedvt_sig_supported     (MonoMethodSignature *sig) MONO_INTERNAL;
gpointer  mono_arch_get_gsharedvt_trampoline    (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_gsharedvt_call_info     (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, MonoGenericSharingContext *gsctx, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli) MONO_INTERNAL;
gboolean  mono_arch_opcode_needs_emulation      (MonoCompile *cfg, int opcode) MONO_INTERNAL;
gboolean  mono_arch_tail_call_supported         (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig) MONO_INTERNAL;
int       mono_arch_translate_tls_offset        (int offset) MONO_INTERNAL;
gboolean  mono_arch_opcode_supported            (int opcode) MONO_INTERNAL;

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
gboolean  mono_arch_is_soft_float               (void) MONO_INTERNAL;
#else
static inline MONO_ALWAYS_INLINE gboolean
mono_arch_is_soft_float (void)
{
	return FALSE;
}
#endif

/* Soft Debug support */
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
void      mono_arch_set_breakpoint              (MonoJitInfo *ji, guint8 *ip) MONO_INTERNAL;
void      mono_arch_clear_breakpoint            (MonoJitInfo *ji, guint8 *ip) MONO_INTERNAL;
void      mono_arch_start_single_stepping       (void) MONO_INTERNAL;
void      mono_arch_stop_single_stepping        (void) MONO_INTERNAL;
gboolean  mono_arch_is_single_step_event        (void *info, void *sigctx) MONO_INTERNAL;
gboolean  mono_arch_is_breakpoint_event         (void *info, void *sigctx) MONO_INTERNAL;
void     mono_arch_skip_breakpoint              (MonoContext *ctx, MonoJitInfo *ji) MONO_INTERNAL;
void     mono_arch_skip_single_step             (MonoContext *ctx) MONO_INTERNAL;
gpointer mono_arch_get_seq_point_info           (MonoDomain *domain, guint8 *code) MONO_INTERNAL;
void     mono_arch_setup_resume_sighandler_ctx  (MonoContext *ctx, gpointer func) MONO_INTERNAL;
void     mono_arch_init_lmf_ext                 (MonoLMFExt *ext, gpointer prev_lmf) MONO_INTERNAL;
#endif

#ifdef USE_JUMP_TABLES
void
mono_jumptable_init  (void) MONO_INTERNAL;
gpointer*
mono_jumptable_add_entry (void) MONO_INTERNAL;
gpointer*
mono_jumptable_add_entries (guint32 entries) MONO_INTERNAL;
void
mono_jumptable_cleanup  (void) MONO_INTERNAL;
gpointer*
mono_arch_jumptable_entry_from_code (guint8 *code);
gpointer*
mono_jumptable_get_entry (guint8 *code);
#endif

gboolean
mono_arch_find_jit_info (MonoDomain *domain, MonoJitTlsData *jit_tls, 
						 MonoJitInfo *ji, MonoContext *ctx, 
						 MonoContext *new_ctx, MonoLMF **lmf,
						 mgreg_t **save_locations,
						 StackFrameInfo *frame_info) MONO_INTERNAL;
gpointer  mono_arch_get_throw_exception_by_name (void) MONO_INTERNAL;
gpointer mono_arch_get_call_filter              (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer mono_arch_get_restore_context          (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_throw_exception         (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_rethrow_exception       (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_throw_corlib_exception  (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer  mono_arch_get_throw_pending_exception (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gboolean mono_arch_handle_exception             (void *sigctx, gpointer obj) MONO_INTERNAL;
void     mono_arch_handle_altstack_exception    (void *sigctx, gpointer fault_addr, gboolean stack_ovf) MONO_INTERNAL;
gboolean mono_handle_soft_stack_ovf             (MonoJitTlsData *jit_tls, MonoJitInfo *ji, void *ctx, guint8* fault_addr) MONO_INTERNAL;
void     mono_handle_hard_stack_ovf             (MonoJitTlsData *jit_tls, MonoJitInfo *ji, void *ctx, guint8* fault_addr) MONO_INTERNAL;
gpointer mono_arch_ip_from_context              (void *sigctx) MONO_INTERNAL;
mgreg_t mono_arch_context_get_int_reg		    (MonoContext *ctx, int reg) MONO_INTERNAL;
void     mono_arch_context_set_int_reg		    (MonoContext *ctx, int reg, mgreg_t val) MONO_INTERNAL;
void     mono_arch_flush_register_windows       (void) MONO_INTERNAL;
gboolean mono_arch_is_inst_imm                  (gint64 imm) MONO_INTERNAL;
gboolean mono_arch_is_int_overflow              (void *sigctx, void *info) MONO_INTERNAL;
void     mono_arch_invalidate_method            (MonoJitInfo *ji, void *func, gpointer func_arg) MONO_INTERNAL;
guint32  mono_arch_get_patch_offset             (guint8 *code) MONO_INTERNAL;
gpointer*mono_arch_get_delegate_method_ptr_addr (guint8* code, mgreg_t *regs) MONO_INTERNAL;
void     mono_arch_create_vars                  (MonoCompile *cfg) MONO_INTERNAL;
void     mono_arch_save_unwind_info             (MonoCompile *cfg) MONO_INTERNAL;
void     mono_arch_register_lowlevel_calls      (void) MONO_INTERNAL;
gpointer mono_arch_get_unbox_trampoline         (MonoMethod *m, gpointer addr) MONO_INTERNAL;
gpointer mono_arch_get_static_rgctx_trampoline  (MonoMethod *m, MonoMethodRuntimeGenericContext *mrgctx, gpointer addr) MONO_INTERNAL;
gpointer  mono_arch_get_llvm_imt_trampoline     (MonoDomain *domain, MonoMethod *method, int vt_offset) MONO_INTERNAL;
gpointer mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr) MONO_INTERNAL;
void     mono_arch_patch_callsite               (guint8 *method_start, guint8 *code, guint8 *addr) MONO_INTERNAL;
void     mono_arch_patch_plt_entry              (guint8 *code, gpointer *got, mgreg_t *regs, guint8 *addr) MONO_INTERNAL;
void     mono_arch_nullify_class_init_trampoline(guint8 *code, mgreg_t *regs) MONO_INTERNAL;
int      mono_arch_get_this_arg_reg             (guint8 *code) MONO_INTERNAL;
gpointer mono_arch_get_this_arg_from_call       (mgreg_t *regs, guint8 *code) MONO_INTERNAL;
gpointer mono_arch_get_delegate_invoke_impl     (MonoMethodSignature *sig, gboolean has_target) MONO_INTERNAL;
gpointer mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method, int offset, gboolean load_imt_reg) MONO_INTERNAL;
gpointer mono_arch_create_specific_trampoline   (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len) MONO_INTERNAL;
void        mono_arch_emit_imt_argument         (MonoCompile *cfg, MonoCallInst *call, MonoInst *imt_arg) MONO_INTERNAL;
MonoMethod* mono_arch_find_imt_method           (mgreg_t *regs, guint8 *code) MONO_INTERNAL;
MonoVTable* mono_arch_find_static_call_vtable   (mgreg_t *regs, guint8 *code) MONO_INTERNAL;
gpointer    mono_arch_build_imt_thunk           (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp) MONO_INTERNAL;
void    mono_arch_notify_pending_exc            (MonoThreadInfo *info) MONO_INTERNAL;
guint8* mono_arch_get_call_target               (guint8 *code) MONO_INTERNAL;
guint32 mono_arch_get_plt_info_offset           (guint8 *plt_entry, mgreg_t *regs, guint8 *code) MONO_INTERNAL;
GSList *mono_arch_get_trampolines               (gboolean aot) MONO_INTERNAL;

/* Handle block guard */
gpointer mono_arch_install_handler_block_guard (MonoJitInfo *ji, MonoJitExceptionInfo *clause, MonoContext *ctx, gpointer new_value) MONO_INTERNAL;
gpointer mono_arch_create_handler_block_trampoline (MonoTrampInfo **info, gboolean aot) MONO_INTERNAL;
gpointer mono_create_handler_block_trampoline (void) MONO_INTERNAL;
gboolean mono_install_handler_block_guard (MonoThreadUnwindState *ctx) MONO_INTERNAL;

/*New interruption machinery */
void
mono_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data) MONO_INTERNAL;

void
mono_arch_setup_async_callback (MonoContext *ctx, void (*async_cb)(void *fun), gpointer user_data) MONO_INTERNAL;

gboolean
mono_thread_state_init_from_handle (MonoThreadUnwindState *tctx, MonoThreadInfo *info) MONO_INTERNAL;


/* Exception handling */
typedef gboolean (*MonoJitStackWalk)            (StackFrameInfo *frame, MonoContext *ctx, gpointer data);

void     mono_exceptions_init                   (void) MONO_INTERNAL;
gboolean mono_handle_exception                  (MonoContext *ctx, gpointer obj) MONO_INTERNAL;
void     mono_handle_native_sigsegv             (int signal, void *sigctx) MONO_INTERNAL;
MONO_API void     mono_print_thread_dump                 (void *sigctx);
MONO_API void     mono_print_thread_dump_from_ctx        (MonoContext *ctx);
void     mono_walk_stack_with_ctx               (MonoJitStackWalk func, MonoContext *start_ctx, MonoUnwindOptions unwind_options, void *user_data) MONO_INTERNAL;
void     mono_walk_stack_with_state             (MonoJitStackWalk func, MonoThreadUnwindState *state, MonoUnwindOptions unwind_options, void *user_data) MONO_INTERNAL;
void     mono_walk_stack                        (MonoJitStackWalk func, MonoUnwindOptions options, void *user_data) MONO_INTERNAL;
gboolean mono_thread_state_init_from_sigctx     (MonoThreadUnwindState *ctx, void *sigctx) MONO_INTERNAL;
gboolean mono_thread_state_init_from_current    (MonoThreadUnwindState *ctx) MONO_INTERNAL;
gboolean mono_thread_state_init_from_monoctx    (MonoThreadUnwindState *ctx, MonoContext *mctx) MONO_INTERNAL;

void     mono_setup_altstack                    (MonoJitTlsData *tls) MONO_INTERNAL;
void     mono_free_altstack                     (MonoJitTlsData *tls) MONO_INTERNAL;
gpointer mono_altstack_restore_prot             (mgreg_t *regs, guint8 *code, gpointer *tramp_data, guint8* tramp) MONO_INTERNAL;
MonoJitInfo* mini_jit_info_table_find           (MonoDomain *domain, char *addr, MonoDomain **out_domain) MONO_INTERNAL;
void     mono_resume_unwind                     (MonoContext *ctx) MONO_LLVM_INTERNAL;

MonoJitInfo * mono_find_jit_info                (MonoDomain *domain, MonoJitTlsData *jit_tls, MonoJitInfo *res, MonoJitInfo *prev_ji, MonoContext *ctx, MonoContext *new_ctx, char **trace, MonoLMF **lmf, int *native_offset, gboolean *managed) MONO_INTERNAL;

typedef gboolean (*MonoExceptionFrameWalk)      (MonoMethod *method, gpointer ip, size_t native_offset, gboolean managed, gpointer user_data);
gboolean mono_exception_walk_trace              (MonoException *ex, MonoExceptionFrameWalk func, gpointer user_data);
void mono_restore_context                       (MonoContext *ctx) MONO_INTERNAL;
guint8* mono_jinfo_get_unwind_info              (MonoJitInfo *ji, guint32 *unwind_info_len) MONO_INTERNAL;
int  mono_jinfo_get_epilog_size                 (MonoJitInfo *ji) MONO_INTERNAL;

gboolean
mono_find_jit_info_ext (MonoDomain *domain, MonoJitTlsData *jit_tls, 
						MonoJitInfo *prev_ji, MonoContext *ctx,
						MonoContext *new_ctx, char **trace, MonoLMF **lmf,
						mgreg_t **save_locations,
						StackFrameInfo *frame) MONO_INTERNAL;

gpointer mono_get_throw_exception               (void) MONO_INTERNAL;
gpointer mono_get_rethrow_exception             (void) MONO_INTERNAL;
gpointer mono_get_call_filter                   (void) MONO_INTERNAL;
gpointer mono_get_restore_context               (void) MONO_INTERNAL;
gpointer mono_get_throw_exception_by_name       (void) MONO_INTERNAL;
gpointer mono_get_throw_corlib_exception        (void) MONO_INTERNAL;

MonoArray *ves_icall_get_trace                  (MonoException *exc, gint32 skip, MonoBoolean need_file_info) MONO_INTERNAL;
MonoBoolean ves_icall_get_frame_info            (gint32 skip, MonoBoolean need_file_info, 
						 MonoReflectionMethod **method, 
						 gint32 *iloffset, gint32 *native_offset,
						 MonoString **file, gint32 *line, gint32 *column) MONO_INTERNAL;
MonoString *ves_icall_System_Exception_get_trace (MonoException *exc) MONO_INTERNAL;
void mono_set_cast_details                      (MonoClass *from, MonoClass *to) MONO_INTERNAL;

/* Installs a function which is called when the runtime encounters an unhandled exception.
 * This hook isn't expected to return.
 * If no hook has been installed, the runtime will print a message before aborting.
 */
typedef void  (*MonoUnhandledExceptionFunc)         (MonoObject *exc, gpointer user_data);
void          mono_install_unhandled_exception_hook (MonoUnhandledExceptionFunc func, gpointer user_data);
void          mono_invoke_unhandled_exception_hook  (MonoObject *exc);

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
void        mono_ssa_loop_invariant_code_motion (MonoCompile *cfg) MONO_INTERNAL;

void        mono_ssa_compute2                   (MonoCompile *cfg);
void        mono_ssa_remove2                    (MonoCompile *cfg);
void        mono_ssa_cprop2                     (MonoCompile *cfg);
void        mono_ssa_deadce2                    (MonoCompile *cfg);

/* debugging support */
void      mono_debug_init_method                (MonoCompile *cfg, MonoBasicBlock *start_block,
						 guint32 breakpoint_id) MONO_INTERNAL;
void      mono_debug_open_method                (MonoCompile *cfg) MONO_INTERNAL;
void      mono_debug_close_method               (MonoCompile *cfg) MONO_INTERNAL;
void      mono_debug_free_method                (MonoCompile *cfg) MONO_INTERNAL;
void      mono_debug_open_block                 (MonoCompile *cfg, MonoBasicBlock *bb, guint32 address) MONO_INTERNAL;
void      mono_debug_record_line_number         (MonoCompile *cfg, MonoInst *ins, guint32 address) MONO_INTERNAL;
void      mono_debug_serialize_debug_info       (MonoCompile *cfg, guint8 **out_buf, guint32 *buf_len) MONO_INTERNAL;
void      mono_debug_add_aot_method             (MonoDomain *domain,
						 MonoMethod *method, guint8 *code_start, 
						 guint8 *debug_info, guint32 debug_info_len) MONO_INTERNAL;
MONO_API void      mono_debug_print_vars                 (gpointer ip, gboolean only_arguments);
MONO_API void      mono_debugger_run_finally             (MonoContext *start_ctx);

extern gssize mono_breakpoint_info_index [MONO_BREAKPOINT_ARRAY_SIZE];

MONO_API gboolean mono_breakpoint_clean_code (guint8 *method_start, guint8 *code, int offset, guint8 *buf, int size);

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
void
mono_local_alias_analysis (MonoCompile *cfg) MONO_INTERNAL;

/* CAS - stack walk */
MonoSecurityFrame* ves_icall_System_Security_SecurityFrame_GetSecurityFrame (gint32 skip) MONO_INTERNAL;
MonoArray* ves_icall_System_Security_SecurityFrame_GetSecurityStack (gint32 skip) MONO_INTERNAL;

/* Generic sharing */

void
mono_set_generic_sharing_supported (gboolean supported) MONO_INTERNAL;

void
mono_set_generic_sharing_vt_supported (gboolean supported) MONO_INTERNAL;

void
mono_set_partial_sharing_supported (gboolean supported) MONO_INTERNAL;

gboolean
mono_class_generic_sharing_enabled (MonoClass *class) MONO_INTERNAL;

gpointer
mono_class_fill_runtime_generic_context (MonoVTable *class_vtable, guint8 *caller, guint32 slot) MONO_INTERNAL;

gpointer
mono_method_fill_runtime_generic_context (MonoMethodRuntimeGenericContext *mrgctx, guint8 *caller, guint32 slot) MONO_INTERNAL;

MonoMethodRuntimeGenericContext*
mono_method_lookup_rgctx (MonoVTable *class_vtable, MonoGenericInst *method_inst) MONO_INTERNAL;

const char*
mono_rgctx_info_type_to_str (MonoRgctxInfoType type) MONO_INTERNAL;

MonoJumpInfoType
mini_rgctx_info_type_to_patch_info_type (MonoRgctxInfoType info_type) MONO_INTERNAL;

gboolean
mono_method_needs_static_rgctx_invoke (MonoMethod *method, gboolean allow_type_vars) MONO_INTERNAL;

int
mono_class_rgctx_get_array_size (int n, gboolean mrgctx) MONO_INTERNAL;

guint32
mono_method_lookup_or_register_info (MonoMethod *method, gboolean in_mrgctx, gpointer data,
	MonoRgctxInfoType info_type, MonoGenericContext *generic_context) MONO_INTERNAL;

MonoGenericContext
mono_method_construct_object_context (MonoMethod *method) MONO_INTERNAL;

MonoMethod*
mono_method_get_declaring_generic_method (MonoMethod *method) MONO_INTERNAL;

int
mono_generic_context_check_used (MonoGenericContext *context) MONO_INTERNAL;

int
mono_class_check_context_used (MonoClass *class) MONO_INTERNAL;

gboolean
mono_generic_context_is_sharable (MonoGenericContext *context, gboolean allow_type_vars) MONO_INTERNAL;

gboolean
mono_generic_context_is_sharable_full (MonoGenericContext *context, gboolean allow_type_vars, gboolean allow_partial) MONO_INTERNAL;

gboolean
mono_method_is_generic_impl (MonoMethod *method) MONO_INTERNAL;

gboolean
mono_method_is_generic_sharable (MonoMethod *method, gboolean allow_type_vars) MONO_INTERNAL;

gboolean
mono_method_is_generic_sharable_full (MonoMethod *method, gboolean allow_type_vars, gboolean allow_partial, gboolean allow_gsharedvt) MONO_INTERNAL;

gboolean
mini_class_is_generic_sharable (MonoClass *klass) MONO_INTERNAL;

gboolean
mono_is_partially_sharable_inst (MonoGenericInst *inst) MONO_INTERNAL;

MonoGenericSharingContext* mono_get_generic_context_from_code (guint8 *code) MONO_INTERNAL;

MonoGenericContext* mini_method_get_context (MonoMethod *method) MONO_INTERNAL;

int mono_method_check_context_used (MonoMethod *method) MONO_INTERNAL;

gboolean mono_generic_context_equal_deep (MonoGenericContext *context1, MonoGenericContext *context2) MONO_INTERNAL;

gpointer mono_helper_get_rgctx_other_ptr (MonoClass *caller_class, MonoVTable *vtable,
					  guint32 token, guint32 token_source, guint32 rgctx_type,
					  gint32 rgctx_index) MONO_INTERNAL;

void mono_generic_sharing_init (void) MONO_INTERNAL;
void mono_generic_sharing_cleanup (void) MONO_INTERNAL;

MonoClass* mini_class_get_container_class (MonoClass *class) MONO_INTERNAL;
MonoGenericContext* mini_class_get_context (MonoClass *class) MONO_INTERNAL;

MonoType* mini_replace_type (MonoType *type) MONO_LLVM_INTERNAL;
MonoType* mini_get_basic_type_from_generic (MonoGenericSharingContext *gsctx, MonoType *type) MONO_INTERNAL;
MonoType* mini_type_get_underlying_type (MonoGenericSharingContext *gsctx, MonoType *type) MONO_INTERNAL;
MonoMethod* mini_get_shared_method (MonoMethod *method) MONO_INTERNAL;
MonoMethod* mini_get_shared_method_to_register (MonoMethod *method) MONO_INTERNAL;
MonoMethod* mini_get_shared_method_full (MonoMethod *method, gboolean all_vt, gboolean is_gsharedvt) MONO_INTERNAL;

int mini_type_stack_size (MonoGenericSharingContext *gsctx, MonoType *t, int *align) MONO_INTERNAL;
int mini_type_stack_size_full (MonoGenericSharingContext *gsctx, MonoType *t, guint32 *align, gboolean pinvoke) MONO_INTERNAL;
void type_to_eval_stack_type (MonoCompile *cfg, MonoType *type, MonoInst *inst) MONO_INTERNAL;
guint mono_type_to_regmove (MonoCompile *cfg, MonoType *type) MONO_LLVM_INTERNAL;

void mono_cfg_add_try_hole (MonoCompile *cfg, MonoExceptionClause *clause, guint8 *start, MonoBasicBlock *bb) MONO_INTERNAL;

void mono_cfg_set_exception (MonoCompile *cfg, int type) MONO_INTERNAL;
gboolean mini_type_is_reference (MonoCompile *cfg, MonoType *type) MONO_INTERNAL;
gboolean mini_type_is_vtype (MonoCompile *cfg, MonoType *t); /* should be internal but it's used by llvm */
gboolean mini_type_var_is_vt (MonoCompile *cfg, MonoType *type); /* should be internal but it's used by llvm */
gboolean mini_is_gsharedvt_klass (MonoCompile *cfg, MonoClass *klass); /* should be internal but it's used by llvm */
gboolean mini_is_gsharedvt_type (MonoCompile *cfg, MonoType *t) MONO_INTERNAL;
gboolean mini_is_gsharedvt_signature (MonoCompile *cfg, MonoMethodSignature *sig) MONO_INTERNAL;
gboolean mini_is_gsharedvt_type_gsctx (MonoGenericSharingContext *gsctx, MonoType *t) MONO_INTERNAL;
gboolean mini_is_gsharedvt_variable_type (MonoCompile *cfg, MonoType *t) MONO_LLVM_INTERNAL;
gboolean mini_is_gsharedvt_variable_klass (MonoCompile *cfg, MonoClass *klass) MONO_LLVM_INTERNAL;
gboolean mini_is_gsharedvt_sharable_method (MonoMethod *method) MONO_INTERNAL;
gboolean mini_is_gsharedvt_variable_signature (MonoMethodSignature *sig) MONO_INTERNAL;
gboolean mini_is_gsharedvt_sharable_inst (MonoGenericInst *inst) MONO_INTERNAL;
gpointer mini_method_get_rgctx (MonoMethod *m) MONO_INTERNAL;
void mini_init_gsctx (MonoDomain *domain, MonoMemPool *mp, MonoGenericContext *context, MonoGenericSharingContext *gsctx) MONO_INTERNAL;

gpointer mini_get_gsharedvt_wrapper (gboolean gsharedvt_in, gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, MonoGenericSharingContext *gsctx,
									 gint32 vcall_offset, gboolean calli) MONO_INTERNAL;

/* wapihandles.c */
int mini_wapi_hps (int argc, char **argv) MONO_INTERNAL;

int mini_wapi_semdel (int argc, char **argv) MONO_INTERNAL;

int mini_wapi_seminfo (int argc, char **argv) MONO_INTERNAL;

/* SIMD support */

/*
This enum MUST be kept in sync with its managed mirror Mono.Simd.AccelMode.
 */
enum {
	SIMD_VERSION_SSE1	= 1 << 0,
	SIMD_VERSION_SSE2	= 1 << 1,
	SIMD_VERSION_SSE3	= 1 << 2,
	SIMD_VERSION_SSSE3	= 1 << 3,
	SIMD_VERSION_SSE41	= 1 << 4,
	SIMD_VERSION_SSE42	= 1 << 5,
	SIMD_VERSION_SSE4a	= 1 << 6,
	SIMD_VERSION_ALL	= SIMD_VERSION_SSE1 | SIMD_VERSION_SSE2 |
			  SIMD_VERSION_SSE3 | SIMD_VERSION_SSSE3 |
			  SIMD_VERSION_SSE41 | SIMD_VERSION_SSE42 |
			  SIMD_VERSION_SSE4a,

	/* this value marks the end of the bit indexes used in 
	 * this emum.
	 */
	SIMD_VERSION_INDEX_END = 6 
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

MonoInst*   mono_emit_native_types_intrinsics (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args) MONO_INTERNAL;
MonoType*   mini_native_type_replace_type (MonoType *type) MONO_INTERNAL;

#ifdef __linux__
/* maybe enable also for other systems? */
#define ENABLE_JIT_MAP 1
void mono_enable_jit_map (void) MONO_INTERNAL;
void mono_emit_jit_map   (MonoJitInfo *jinfo) MONO_INTERNAL;
void mono_emit_jit_tramp (void *start, int size, const char *desc) MONO_INTERNAL;
gboolean mono_jit_map_is_enabled (void) MONO_INTERNAL;
#else
#define mono_enable_jit_map()
#define mono_emit_jit_map(ji)
#define mono_emit_jit_tramp(s,z,d)
#define mono_jit_map_is_enabled() (0)
#endif

/*
 * Per-OS implementation functions.
 */
void mono_runtime_install_handlers (void) MONO_INTERNAL;
void mono_runtime_cleanup_handlers (void) MONO_INTERNAL;
void mono_runtime_setup_stat_profiler (void) MONO_INTERNAL;
void mono_runtime_shutdown_stat_profiler (void) MONO_INTERNAL;
void mono_runtime_posix_install_handlers (void) MONO_INTERNAL;
pid_t mono_runtime_syscall_fork (void) MONO_INTERNAL;
void mono_gdb_render_native_backtraces (pid_t crashed_pid) MONO_INTERNAL;

void mono_cross_helpers_run (void) MONO_INTERNAL;

/*
 * Signal handling
 */

void MONO_SIG_HANDLER_SIGNATURE (mono_sigfpe_signal_handler)  MONO_INTERNAL;
void MONO_SIG_HANDLER_SIGNATURE (mono_sigill_signal_handler)  MONO_INTERNAL;
void MONO_SIG_HANDLER_SIGNATURE (mono_sigsegv_signal_handler) MONO_INTERNAL;
void MONO_SIG_HANDLER_SIGNATURE (mono_sigint_signal_handler)  MONO_INTERNAL;
gboolean MONO_SIG_HANDLER_SIGNATURE (mono_chain_signal) MONO_INTERNAL;

#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
#define ARCH_HAVE_DELEGATE_TRAMPOLINES 1
#else
#define ARCH_HAVE_DELEGATE_TRAMPOLINES 0
#endif

#ifdef MONO_ARCH_HAVE_OP_TAIL_CALL
#define ARCH_HAVE_OP_TAIL_CALL 1
#else
#define ARCH_HAVE_OP_TAIL_CALL 0
#endif

#ifndef MONO_ARCH_HAVE_TLS_GET
#define MONO_ARCH_HAVE_TLS_GET 0
#endif

#ifdef MONO_ARCH_HAVE_TLS_GET_REG
#define ARCH_HAVE_TLS_GET_REG 1
#else
#define ARCH_HAVE_TLS_GET_REG 0
#endif

#ifdef MONO_ARCH_EMULATE_MUL_DIV
#define ARCH_EMULATE_MUL_DIV 1
#else
#define ARCH_EMULATE_MUL_DIV 0
#endif

#ifndef MONO_ARCH_MONITOR_ENTER_ADJUSTMENT
#define MONO_ARCH_MONITOR_ENTER_ADJUSTMENT 1
#endif

#ifndef MONO_ARCH_DYN_CALL_PARAM_AREA
#define MONO_ARCH_DYN_CALL_PARAM_AREA 0
#endif

#ifdef MONO_ARCH_VARARG_ICALLS
#define ARCH_VARARG_ICALLS 1
#else
#define ARCH_VARARG_ICALLS 0
#endif

#ifdef MONO_ARCH_HAVE_DUMMY_INIT
#define ARCH_HAVE_DUMMY_INIT 1
#else
#define ARCH_HAVE_DUMMY_INIT 0
#endif

#ifdef MONO_CROSS_COMPILE
#define MONO_IS_CROSS_COMPILE 1
#else
#define MONO_IS_CROSS_COMPILE 0
#endif

#if defined(__mono_ilp32__)
#define MONO_IS_ILP32 1
#else
#define MONO_IS_ILP32 0
#endif

#endif /* __MONO_MINI_H__ */
