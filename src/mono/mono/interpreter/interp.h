
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
	VAL_OBJ     = 3 + VAL_POINTER,
	VAL_VALUETA = 8
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
		struct {
			gpointer vt;
			MonoClass *klass;
		} vt;
	} data;
	unsigned int type;
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
	MonoException     *ex;
	MonoExceptionClause *ex_handler;
};

void mono_init_icall (void);

void inline stackval_from_data (MonoType *type, stackval *result, char *data, gboolean pinvoke);
void ves_exec_method (MonoInvocation *frame);

typedef void (*MonoFunc) (void);
typedef void (*MonoPIFunc) (MonoFunc callme, void *retval, void *obj_this, stackval *arguments);

/*
 * defined in an arch specific file.
 */
MonoPIFunc
mono_create_trampoline (MonoMethodSignature *sig, gboolean string_ctor);
void *mono_create_method_pointer (MonoMethod *method);
