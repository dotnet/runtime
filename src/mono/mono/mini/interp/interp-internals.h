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
#define MINT_TYPE_P  9
#define MINT_TYPE_VT 10

#define BOX_NOT_CLEAR_VT_SP 0x4000

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
#elif SIZEOF_VOID_P == 8
typedef guint64 mono_u;
typedef gint64  mono_i;
#endif

/*
 * Value types are represented on the eval stack as pointers to the
 * actual storage. The size field tells how much storage is allocated.
 * A value type can't be larger than 16 MB.
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
		/* native size integer and pointer types */
		MonoObject *o;
		gpointer p;
		mono_u nati;
		gpointer vt;
	} data;
#if defined(__ppc__) || defined(__powerpc__)
	int pad;
#endif
} stackval;

typedef struct _InterpFrame InterpFrame;

typedef void (*MonoFuncV) (void);
typedef void (*MonoPIFunc) (MonoFuncV callme, void *margs);

/* 
 * Structure representing a method transformed for the interpreter 
 * This is domain specific
 */
typedef struct _InterpMethod
{
	/* NOTE: These first two elements (method and
	   next_jit_code_hash) must be in the same order and at the
	   same offset as in MonoJitInfo, because of the jit_code_hash
	   internal hash table in MonoDomain. */
	MonoMethod *method;
	struct _InterpMethod *next_jit_code_hash;
	guint32 locals_size;
	guint32 total_locals_size;
	guint32 args_size;
	guint32 stack_size;
	guint32 vt_stack_size;
	guint32 alloca_size;
	unsigned int init_locals : 1;
	unsigned int vararg : 1;
	unsigned int needs_thread_attach : 1;
	unsigned short *code;
	unsigned short *new_body_start; /* after all STINARG instrs */
	MonoPIFunc func;
	int num_clauses;
	MonoExceptionClause *clauses;
	void **data_items;
	int transformed;
	guint32 *arg_offsets;
	guint32 *local_offsets;
	guint32 *exvar_offsets;
	unsigned int param_count;
	unsigned int hasthis;
	gpointer jit_wrapper;
	gpointer jit_addr;
	MonoMethodSignature *jit_sig;
	gpointer jit_entry;
	MonoType *rtype;
	MonoType **param_types;
	MonoJitInfo *jinfo;
	MonoDomain *domain;
	MonoProfilerCallInstrumentationFlags prof_flags;
} InterpMethod;

struct _InterpFrame {
	InterpFrame *parent; /* parent */
	InterpMethod  *imethod; /* parent */
	stackval       *retval; /* parent */
	char           *args;
	char           *varargs;
	stackval       *stack_args; /* parent */
	stackval       *stack;
	stackval       *sp; /* For GC stack marking */
	unsigned char  *locals;
	/* exception info */
	unsigned char  invoke_trap;
	const unsigned short  *ip;
	MonoException     *ex;
	MonoExceptionClause *ex_handler;
	MonoDomain *domain;
};

typedef struct {
	InterpFrame *current_frame;
	/* Resume state for resuming execution in mixed mode */
	gboolean       has_resume_state;
	/* Frame to resume execution at */
	InterpFrame *handler_frame;
	/* IP to resume execution at */
	gpointer handler_ip;
	/* Clause that we are resuming to */
	MonoJitExceptionInfo *handler_ei;
} ThreadContext;

extern int mono_interp_traceopt;
extern GSList *mono_interp_jit_classes;

void
mono_interp_transform_method (InterpMethod *imethod, ThreadContext *context, MonoError *error);

void
mono_interp_transform_init (void);

InterpMethod *
mono_interp_get_imethod (MonoDomain *domain, MonoMethod *method, MonoError *error);

static inline int
mint_type(MonoType *type_)
{
	MonoType *type = mini_native_type_replace_type (type_);
	if (type->byref)
		return MINT_TYPE_P;
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
#if SIZEOF_VOID_P == 4
		return MINT_TYPE_I4;
#else
		return MINT_TYPE_I8;
#endif
	case MONO_TYPE_PTR:
		return MINT_TYPE_P;
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
