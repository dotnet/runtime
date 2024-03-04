#ifndef __MONO_MINI_INTERPRETER_INTERNALS_H__
#define __MONO_MINI_INTERPRETER_INTERNALS_H__

#include <setjmp.h>
#include <glib.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-internals.h>
#include "interp.h"

#define MINT_TYPE_I1 0
#define MINT_TYPE_U1 1
#define MINT_TYPE_I2 2
#define MINT_TYPE_U2 3
#define MINT_TYPE_I4 4
#define MINT_TYPE_I8 5
#define MINT_TYPE_R4 6
#define MINT_TYPE_R8 7
#define MINT_TYPE_O  8
#define MINT_TYPE_VT 9
#define MINT_TYPE_VOID 10

#define INLINED_METHOD_FLAG 0xffff
#define TRACING_FLAG 0x1
#define PROFILING_FLAG 0x2

#define MINT_STACK_SLOT_SIZE (sizeof (stackval))
// This alignment provides us with straight forward support for Vector128
#define MINT_STACK_ALIGNMENT (2 * MINT_STACK_SLOT_SIZE)
#define MINT_SIMD_ALIGNMENT (MINT_STACK_ALIGNMENT)
#define SIZEOF_V128 16

#define INTERP_STACK_SIZE (1024*1024)
#define INTERP_REDZONE_SIZE (8*1024)

enum {
	VAL_I32     = 0,
	VAL_DOUBLE  = 1,
	VAL_I64     = 2,
	VAL_VALUET  = 3,
	VAL_POINTER = 4,
	VAL_NATI    = 0 + VAL_POINTER,
	VAL_MP      = 1 + VAL_POINTER,
	VAL_TP      = 2 + VAL_POINTER,
	VAL_OBJ     = 3 + VAL_POINTER
};

#if SIZEOF_VOID_P == 4
typedef guint32 mono_u;
typedef gint32  mono_i;
#define MINT_TYPE_I MINT_TYPE_I4
#elif SIZEOF_VOID_P == 8
typedef guint64 mono_u;
typedef gint64  mono_i;
#define MINT_TYPE_I MINT_TYPE_I8
#endif

#ifdef TARGET_WASM
#define INTERP_NO_STACK_SCAN 1
#endif

/*
 * Value types are represented on the eval stack as pointers to the
 * actual storage. A value type cannot be larger than 16 MB.
 */
typedef struct {
	union {
		gint32 i;
		gint64 l;
		struct {
			gint32 lo;
			gint32 hi;
		} pair;
		float f_r4;
		double f;
#ifdef INTERP_NO_STACK_SCAN
		/* Ensure objref is always flushed to interp stack */
		MonoObject * volatile o;
#else
		MonoObject *o;
#endif
		/* native size integer and pointer types */
		gpointer p;
		mono_u nati;
		gpointer vt;
	} data;
} stackval;

typedef struct InterpFrame InterpFrame;

typedef void (*MonoFuncV) (void);
typedef void (*MonoPIFunc) (void *callme, void *margs);


typedef enum {
	IMETHOD_CODE_INTERP,
	IMETHOD_CODE_COMPILED,
	IMETHOD_CODE_UNKNOWN
} InterpMethodCodeType;

#define PROFILE_INTERP 0

#if __GNUC__
#define INTERP_ENABLE_SIMD
#endif

#define INTERP_IMETHOD_TAG_1(im) ((gpointer)((mono_u)(im) | 1))
#define INTERP_IMETHOD_IS_TAGGED_1(im) ((mono_u)(im) & 1)
#define INTERP_IMETHOD_UNTAG_1(im) ((InterpMethod*)((mono_u)(im) & ~1))

#define INTERP_IMETHOD_TAG_UNBOX(im) INTERP_IMETHOD_TAG_1(im)
#define INTERP_IMETHOD_IS_TAGGED_UNBOX(im) INTERP_IMETHOD_IS_TAGGED_1(im)
#define INTERP_IMETHOD_UNTAG_UNBOX(im) INTERP_IMETHOD_UNTAG_1(im)

/*
 * Structure representing a method transformed for the interpreter
 */
typedef struct InterpMethod InterpMethod;
struct InterpMethod {
	/* NOTE: These first two elements (method and
	   next_jit_code_hash) must be in the same order and at the
	   same offset as in MonoJitInfo, because of the jit_code_hash
	   internal hash table in MonoDomain. */
	MonoMethod *method;
	InterpMethod *next_jit_code_hash;

	// Sort pointers ahead of integers to minimize padding for alignment.

	unsigned short *code;
	MonoPIFunc func;
	MonoExceptionClause *clauses; // num_clauses
	void **data_items;
	guint32 *local_offsets;
	guint32 *arg_offsets;
	guint32 *clause_data_offsets;
	gpointer jit_call_info;
	gpointer jit_entry;
	gpointer llvmonly_unbox_entry;
	MonoType *rtype;
	MonoType **param_types;
	MonoJitInfo *jinfo;
	MonoFtnDesc *ftndesc;
	MonoFtnDesc *ftndesc_unbox;
	MonoDelegateTrampInfo *del_info;

	guint32 locals_size;
	guint32 alloca_size;
	int num_clauses; // clauses
	int transformed; // boolean
	unsigned int param_count;
	unsigned int hasthis; // boolean
	MonoProfilerCallInstrumentationFlags prof_flags;
	InterpMethodCodeType code_type;
#ifdef ENABLE_EXPERIMENT_TIERED
	MiniTieredCounter tiered_counter;
#endif
	gint32 entry_count;
	InterpMethod *optimized_imethod;
	// This data is used to resolve native offsets from unoptimized method to native offsets
	// in the optimized method. We rely on keys identifying a certain logical execution point
	// to be equal between unoptimized and optimized method. In unoptimized method we map from
	// native_offset to a key and in optimized_method we map from key to a native offset.
	//
	// The logical execution points that are being tracked are some basic block starts (in this
	// case we don't need any tracking in the unoptimized method, just the mapping from bbindex
	// to its native offset) and call handler returns. Call handler returns store the return ip
	// on the stack so once we tier up the method we need to update these to IPs in the optimized
	// method. The key for a call handler is its index, in appearance order in the IL, multiplied
	// by -1. (So we don't collide with basic block indexes)
	//
	// Since we have both positive and negative keys in this array, we use G_MAXINTRE as terminator.
	int *patchpoint_data;
	unsigned int init_locals : 1;
	unsigned int vararg : 1;
	unsigned int optimized : 1;
	unsigned int needs_thread_attach : 1;
	// If set, this method is MulticastDelegate.Invoke
	unsigned int is_invoke : 1;
	unsigned int is_verbose : 1;
#if HOST_BROWSER
	unsigned int contains_traces : 1;
	MonoBitSet *address_taken_bits;
#endif
#if PROFILE_INTERP
	long calls;
	long opcounts;
#endif
};

/* Used for localloc memory allocation */
typedef struct _FrameDataFragment FrameDataFragment;
struct _FrameDataFragment {
	guint8 *pos, *end;
	struct _FrameDataFragment *next;
#if SIZEOF_VOID_P == 4
	/* Align data field to MINT_VT_ALIGNMENT */
	gint32 pad;
#endif
	double data [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	InterpFrame *frame;
	/*
	 * frag and pos hold the current allocation position when the stored frame
	 * starts allocating memory. This is used for restoring the localloc stack
	 * when frame returns.
	 */
	FrameDataFragment *frag;
	guint8 *pos;
} FrameDataInfo;

typedef struct {
	FrameDataFragment *first, *current;
	FrameDataInfo *infos;
	int infos_len, infos_capacity;
	/* For GC sync */
	int inited;
} FrameDataAllocator;


/* Arguments that are passed when invoking only a finally/filter clause from the frame */
typedef struct FrameClauseArgs FrameClauseArgs;

/* State of the interpreter main loop */
typedef struct {
	const unsigned short  *ip;
} InterpState;

struct InterpFrame {
	InterpFrame *parent; /* parent */
	InterpMethod  *imethod; /* parent */
	stackval       *retval; /* parent */
	stackval       *stack;
	InterpFrame    *next_free;
	/* State saved before calls */
	/* This is valid if state.ip != NULL */
	InterpState state;
};

#define frame_locals(frame) ((guchar*)(frame)->stack)

typedef struct {
	/* Lets interpreter know it has to resume execution after EH */
	gboolean has_resume_state;
	/* Frame to resume execution at */
	/* Can be NULL if the exception is caught in an AOTed frame */
	InterpFrame *handler_frame;
	/* IP to resume execution at */
	const guint16 *handler_ip;
	/* Clause that we are resuming to */
	MonoJitExceptionInfo *handler_ei;
	/* Exception that is being thrown. Set with rest of resume state */
	MonoGCHandle exc_gchandle;
	/* This is a contiguous space allocated for interp execution stack */
	guchar *stack_start;
	/* End of the stack space excluding the redzone used to handle stack overflows */
	guchar *stack_end;
	guchar *stack_real_end;
	/*
	 * This stack pointer is the highest stack memory that can be used by the current frame. This does not
	 * change throughout the execution of a frame and it is essentially the upper limit of the execution
	 * stack pointer. It is needed when re-entering interp, to know from which address we can start using
	 * stack, and also needed for the GC to be able to scan the stack.
	 */
	guchar *stack_pointer;
	/* Used for allocation of localloc regions */
	FrameDataAllocator data_stack;
} ThreadContext;

typedef struct {
	gint64 transform_time;
	gint64 methods_transformed;
	gint64 optimize_time;
	gint64 ssa_compute_time;
	gint64 ssa_compute_dominance_time;
	gint64 ssa_compute_global_vars_time;
	gint64 ssa_compute_pruned_liveness_time;
	gint64 ssa_rename_vars_time;
	gint64 optimize_bblocks_time;
	gint64 cprop_time;
	gint64 super_instructions_time;
	gint32 emitted_instructions;
	gint32 inlined_methods;
	gint32 inline_failures;
} MonoInterpStats;

extern MonoInterpStats mono_interp_stats;

extern int mono_interp_traceopt;
extern int mono_interp_opt;
extern GSList *mono_interp_jit_classes;

void
mono_interp_transform_method (InterpMethod *imethod, ThreadContext *context, MonoError *error);

void
mono_interp_transform_init (void);

InterpMethod *
mono_interp_get_imethod (MonoMethod *method);

void
mono_interp_print_code (InterpMethod *imethod);

gboolean
mono_interp_jit_call_supported (MonoMethod *method, MonoMethodSignature *sig);

void
mono_interp_error_cleanup (MonoError *error);

int
mono_mint_type (MonoType *type);

int
mono_interp_type_size (MonoType *type, int mt, int *align_p);

#if HOST_BROWSER

gboolean
mono_jiterp_isinst (MonoObject* object, MonoClass* klass);

void
mono_jiterp_check_pending_unwind (ThreadContext *context);

void *
mono_jiterp_get_context (void);

int
mono_jiterp_overflow_check_i4 (gint32 lhs, gint32 rhs, int opcode);

int
mono_jiterp_overflow_check_u4 (guint32 lhs, guint32 rhs, int opcode);

void
mono_jiterp_ld_delegate_method_ptr (gpointer *destination, MonoDelegate **source);

void
mono_jiterp_stackval_to_data (MonoType *type, stackval *val, void *data);

void
mono_jiterp_stackval_from_data (MonoType *type, stackval *result, const void *data);

int
mono_jiterp_get_arg_offset (InterpMethod *imethod, MonoMethodSignature *sig, int index);

gpointer
mono_jiterp_frame_data_allocator_alloc (FrameDataAllocator *stack, InterpFrame *frame, int size);

gpointer
mono_jiterp_get_simd_intrinsic (int arity, int index);

int
mono_jiterp_get_simd_opcode (int arity, int index);

int
mono_jiterp_get_opcode_info (int opcode, int type);

#endif

#endif /* __MONO_MINI_INTERPRETER_INTERNALS_H__ */
