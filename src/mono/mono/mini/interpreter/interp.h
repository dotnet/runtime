#include <setjmp.h>
#include <glib.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/class-internals.h>
#include "config.h"

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
		double f;
		/* native size integer and pointer types */
		gpointer p;
		mono_u nati;
		gpointer vt;
	} data;
#if defined(__ppc__) || defined(__powerpc__)
	int pad;
#endif
} stackval;

typedef struct _MonoInvocation MonoInvocation;

typedef void (*MonoFunc) (void);
typedef void (*MonoPIFunc) (MonoFunc callme, void *retval, void *obj_this, stackval *arguments);

/* 
 * Structure representing a method transformed for the interpreter 
 * This is domain specific
 */
typedef struct _RuntimeMethod
{
	/* NOTE: These first two elements (method and
	   next_jit_code_hash) must be in the same order and at the
	   same offset as in MonoJitInfo, because of the jit_code_hash
	   internal hash table in MonoDomain. */
	MonoMethod *method;
	struct _RuntimeMethod *next_jit_code_hash;
	guint32 locals_size;
	guint32 args_size;
	guint32 stack_size;
	guint32 vt_stack_size;
	guint32 alloca_size;
	unsigned short *code;
	unsigned short *new_body_start; /* after all STINARG instrs */
	MonoPIFunc func;
	int num_clauses;
	MonoExceptionClause *clauses;
	void **data_items;
	int transformed;
	guint32 *arg_offsets;
	guint32 *local_offsets;
	unsigned int param_count;
	unsigned int hasthis;
	unsigned int valuetype;
} RuntimeMethod;

struct _MonoInvocation {
	MonoInvocation *parent; /* parent */
	RuntimeMethod  *runtime_method; /* parent */
	MonoMethod     *method; /* parent */
	stackval       *retval; /* parent */
	void           *obj;    /* this - parent */
	char           *args;
	stackval       *stack_args; /* parent */
	stackval       *stack;
	stackval       *sp; /* For GC stack marking */
	/* exception info */
	unsigned char  invoke_trap;
	const unsigned short  *ip;
	MonoException     *ex;
	MonoExceptionClause *ex_handler;
};

typedef struct {
	MonoDomain *domain;
	MonoInvocation *base_frame;
	MonoInvocation *current_frame;
	MonoInvocation *env_frame;
	jmp_buf *current_env;
	unsigned char search_for_handler;
	unsigned char managed_code;
} ThreadContext;

void mono_init_icall (void);

MonoException *
mono_interp_transform_method (RuntimeMethod *runtime_method, ThreadContext *context);

MonoDelegate*
mono_interp_ftnptr_to_delegate (MonoClass *klass, gpointer ftn);

void
mono_interp_transform_init (void);

void inline stackval_from_data (MonoType *type, stackval *result, char *data, gboolean pinvoke);
void inline stackval_to_data (MonoType *type, stackval *val, char *data, gboolean pinvoke);
void ves_exec_method (MonoInvocation *frame);

/*
 * defined in an arch specific file.
 */
MonoPIFunc
mono_arch_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor);

RuntimeMethod *
mono_interp_get_runtime_method (MonoMethod *method);

void *mono_arch_create_method_pointer (MonoMethod *method);

extern int mono_interp_traceopt;
