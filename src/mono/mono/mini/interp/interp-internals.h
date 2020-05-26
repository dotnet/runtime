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

#define INLINED_METHOD_FLAG 0xffff
#define TRACING_FLAG 0x1
#define PROFILING_FLAG 0x2

#define MINT_VT_ALIGNMENT 8

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


/*
 * GC SAFETY:
 *
 *  The interpreter executes in gc unsafe (non-preempt) mode. On wasm, the C stack is
 * scannable but the wasm stack is not, so to make the code GC safe, the following rules
 * should be followed:
 * - every objref handled by the code needs to either be stored volatile or stored
 *   into a volatile; volatile stores are stack packable, volatile values are not.
 *   Use either OBJREF or stackval->data.o.
 *   This will ensure the objects are pinned. A volatile local
 *   is on the stack and not in registers. Volatile stores ditto.
 * - minimize the number of MonoObject* locals/arguments (or make them volatile).
 *
 * Volatile on a type/local forces all reads and writes to go to memory/stack,
 *   and each such local to have a unique address.
 *
 * Volatile absence on a type/local allows multiple locals to share storage,
 *   if their lifetimes do not overlap. This is called "stack packing".
 *
 * Volatile absence on a type/local allows the variable to live in
 * both stack and register, for fast reads and "write through".
 */
#ifdef TARGET_WASM

#define WASM_VOLATILE volatile

static inline MonoObject * WASM_VOLATILE *
mono_interp_objref (MonoObject **o)
{
	return o;
}

#define OBJREF(x) (*mono_interp_objref (&x))

#else

#define WASM_VOLATILE /* nothing */

#define OBJREF(x) x

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
		MonoObject * WASM_VOLATILE o;
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

/* 
 * Structure representing a method transformed for the interpreter 
 * This is domain specific
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
	guint32 *exvar_offsets;
	gpointer jit_wrapper;
	gpointer jit_addr;
	MonoMethodSignature *jit_sig;
	gpointer jit_entry;
	gpointer llvmonly_unbox_entry;
	MonoType *rtype;
	MonoType **param_types;
	MonoJitInfo *jinfo;
	MonoDomain *domain;

	guint32 locals_size;
	guint32 total_locals_size;
	guint32 stack_size;
	guint32 vt_stack_size;
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
	unsigned int init_locals : 1;
	unsigned int vararg : 1;
	unsigned int needs_thread_attach : 1;
};

typedef struct _StackFragment StackFragment;
struct _StackFragment {
	guint8 *pos, *end;
	struct _StackFragment *next;
#if SIZEOF_VOID_P == 4
	/* Align data field to MINT_VT_ALIGNMENT */
	gint32 pad;
#endif
	double data [MONO_ZERO_LEN_ARRAY];
};

typedef struct {
	StackFragment *first, *current;
	/* For GC sync */
	int inited;
} FrameStack;


/* Arguments that are passed when invoking only a finally/filter clause from the frame */
typedef struct FrameClauseArgs FrameClauseArgs;

/* State of the interpreter main loop */
typedef struct {
	stackval *sp;
	unsigned char *vt_sp;
	const unsigned short  *ip;
	GSList *finally_ips;
	FrameClauseArgs *clause_args;
} InterpState;

struct InterpFrame {
	InterpFrame *parent; /* parent */
	InterpMethod  *imethod; /* parent */
	stackval       *stack_args; /* parent */
	stackval       *retval; /* parent */
	stackval       *stack;
	InterpFrame    *next_free;
	/* Stack fragments this frame was allocated from */
	StackFragment *data_frag;
	/* exception info */
	const unsigned short  *ip;
	/* State saved before calls */
	/* This is valid if state.ip != NULL */
	InterpState state;
};

#define frame_locals(frame) (((guchar*)((frame)->stack)) + (frame)->imethod->stack_size + (frame)->imethod->vt_stack_size)

typedef struct {
	/* Lets interpreter know it has to resume execution after EH */
	gboolean has_resume_state;
	/* Frame to resume execution at */
	InterpFrame *handler_frame;
	/* IP to resume execution at */
	const guint16 *handler_ip;
	/* Clause that we are resuming to */
	MonoJitExceptionInfo *handler_ei;
	/* Exception that is being thrown. Set with rest of resume state */
	MonoGCHandle exc_gchandle;
	/* Stack of frame data */
	FrameStack data_stack;
} ThreadContext;

typedef struct {
	gint64 transform_time;
	gint64 methods_transformed;
	gint64 cprop_time;
	gint64 super_instructions_time;
	gint32 stloc_nps;
	gint32 movlocs;
	gint32 copy_propagations;
	gint32 constant_folds;
	gint32 ldlocas_removed;
	gint32 killed_instructions;
	gint32 emitted_instructions;
	gint32 super_instructions;
	gint32 added_pop_count;
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
mono_interp_get_imethod (MonoDomain *domain, MonoMethod *method, MonoError *error);

void
mono_interp_print_code (InterpMethod *imethod);

gboolean
mono_interp_jit_call_supported (MonoMethod *method, MonoMethodSignature *sig);

static inline int
mint_type(MonoType *type_)
{
	MonoType *type = mini_native_type_replace_type (type_);
	if (type->byref)
		return MINT_TYPE_I;
enum_type:
	switch (type->type) {
	case MONO_TYPE_I1:
		return MINT_TYPE_I1;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return MINT_TYPE_U1;
	case MONO_TYPE_I2:
		return MINT_TYPE_I2;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return MINT_TYPE_U2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return MINT_TYPE_I4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
		return MINT_TYPE_I;
	case MONO_TYPE_R4:
		return MINT_TYPE_R4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MINT_TYPE_I8;
	case MONO_TYPE_R8:
		return MINT_TYPE_R8;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		return MINT_TYPE_O;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			type = mono_class_enum_basetype_internal (type->data.klass);
			goto enum_type;
		} else
			return MINT_TYPE_VT;
	case MONO_TYPE_TYPEDBYREF:
		return MINT_TYPE_VT;
	case MONO_TYPE_GENERICINST:
		type = m_class_get_byval_arg (type->data.generic_class->container_class);
		goto enum_type;
	default:
		g_warning ("got type 0x%02x", type->type);
		g_assert_not_reached ();
	}
	return -1;
}

#endif /* __MONO_MINI_INTERPRETER_INTERNALS_H__ */
