
#include <glib.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/object.h>

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
	} data;
	unsigned int type : 8;
	unsigned int size : 24; /* used for value types */
} stackval;

typedef struct _MonoInvocation MonoInvocation;

struct _MonoInvocation {
	MonoInvocation *parent; /* parent */
	MonoInvocation *child;
	MonoMethod     *method; /* parent */
	stackval       *retval; /* parent */
	void           *obj;    /* this - parent */
	char           *locals;
	char           *args;
	stackval       *stack_args; /* parent */
	stackval       *stack;
	/* exception info */
	const unsigned char  *ip;
	MonoObject     *ex;
	MonoExceptionClause *ex_handler;
};

void mono_init_icall ();

