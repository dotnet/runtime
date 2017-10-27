/**
 * \file
 * WASM AOT runtime
 */

#include "config.h"

#include <sys/types.h>

#include "mini.h"
#include "interp/interp.h"

#ifdef TARGET_WASM

static void
wasm_restore_context (void)
{
	g_error ("wasm_restore_context");
}

static void
wasm_call_filter (void)
{
	g_error ("wasm_call_filter");
}

static void
wasm_throw_exception (void)
{
	g_error ("wasm_throw_exception");
}

static void
wasm_rethrow_exception (void)
{
	g_error ("wasm_rethrow_exception");
}

static void
wasm_throw_corlib_exception (void)
{
	g_error ("wasm_throw_corlib_exception");
}

static char
type_to_c (MonoType *t)
{
	if (t->byref)
		return 'I';

handle_enum:
	switch (t->type) {
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
		return 'I';
	case MONO_TYPE_R4:
		return 'F';
	case MONO_TYPE_R8:
		return 'D';
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return 'L';
	case MONO_TYPE_VOID:
		return 'V';
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass->enumtype) {
			t = mono_class_enum_basetype (t->data.klass);
			goto handle_enum;
		}

		return 'I';
	case MONO_TYPE_GENERICINST:
		if (t->data.klass->valuetype)
			return 'S';
		return 'I';
	default:
		g_warning ("CANT TRANSLATE %s", mono_type_full_name (t));
		return 'X';
	}
}

#if SIZEOF_VOID_P == 4
#define FIDX(x) ((x) * 2)
#else
#define FIDX(x) (x)
#endif

static void
wasm_invoke_v (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(void) = target_func;
	func ();
}

static void
wasm_invoke_vi (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a) = target_func;
	func (margs->iargs [0]);
}

static void
wasm_invoke_vii (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, gpointer b) = target_func;
	func (margs->iargs [0], margs->iargs [1]);
}

static void
wasm_invoke_viii (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, gpointer b, gpointer c) = target_func;
	func (margs->iargs [0], margs->iargs [1], margs->iargs [2]);
}

static void
wasm_invoke_viiii (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, gpointer b, gpointer c, gpointer d) = target_func;
	func (margs->iargs [0], margs->iargs [1], margs->iargs [2], margs->iargs [3]);
}

static void
wasm_invoke_viiiii (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, gpointer b, gpointer c, gpointer d, gpointer e) = target_func;
	func (margs->iargs [0], margs->iargs [1], margs->iargs [2], margs->iargs [3], margs->iargs [4]);
}

static void
wasm_invoke_viiiiii (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, gpointer b, gpointer c, gpointer d, gpointer e, gpointer f) = target_func;
	func (margs->iargs [0], margs->iargs [1], margs->iargs [2], margs->iargs [3], margs->iargs [4], margs->iargs [5]);
}

static void
wasm_invoke_i (void *target_func, InterpMethodArguments *margs)
{
	int (*func)(void) = target_func;
	int res = func ();
	*(int*)margs->retval = res;
}

static void
wasm_invoke_ii (void *target_func, InterpMethodArguments *margs)
{
	int (*func)(gpointer a) = target_func;
	int res = func (margs->iargs [0]);
	*(int*)margs->retval = res;
}

static void
wasm_invoke_iii (void *target_func, InterpMethodArguments *margs)
{
	int (*func)(gpointer a, gpointer b) = target_func;
	int res = func (margs->iargs [0], margs->iargs [1]);
	*(int*)margs->retval = res;
}

static void
wasm_invoke_iiii (void *target_func, InterpMethodArguments *margs)
{
	int (*func)(gpointer a, gpointer b, gpointer c) = target_func;
	int res = func (margs->iargs [0], margs->iargs [1], margs->iargs [2]);
	*(int*)margs->retval = res;
}

static void
wasm_invoke_iiiii (void *target_func, InterpMethodArguments *margs)
{
	int (*func)(gpointer a, gpointer b, gpointer c, gpointer d) = target_func;
	int res = func (margs->iargs [0], margs->iargs [1], margs->iargs [2], margs->iargs [3]);
	*(int*)margs->retval = res;
}

static void
wasm_invoke_iiiiii (void *target_func, InterpMethodArguments *margs)
{
	int (*func)(gpointer a, gpointer b, gpointer c, gpointer d, gpointer e) = target_func;
	int res = func (margs->iargs [0], margs->iargs [1], margs->iargs [2], margs->iargs [3], margs->iargs [4]);
	*(int*)margs->retval = res;
}

static void
wasm_invoke_iiiiiii (void *target_func, InterpMethodArguments *margs)
{
	int (*func)(gpointer a, gpointer b, gpointer c, gpointer d, gpointer e, gpointer f) = target_func;
	int res = func (margs->iargs [0], margs->iargs [1], margs->iargs [2], margs->iargs [3], margs->iargs [4], margs->iargs [5]);
	*(int*)margs->retval = res;
}

static void
wasm_invoke_iiiiiiiii (void *target_func, InterpMethodArguments *margs)
{
	int (*func)(gpointer a, gpointer b, gpointer c, gpointer d, gpointer e, gpointer f, gpointer g, gpointer h) = target_func;
	int res = func (margs->iargs [0], margs->iargs [1], margs->iargs [2], margs->iargs [3], margs->iargs [4], margs->iargs [5], margs->iargs [6], margs->iargs [7]);
	*(int*)margs->retval = res;
}


typedef union {
	gint64 l;
	struct {
		gint32 lo;
		gint32 hi;
	} pair;
} interp_pair;

static void
wasm_invoke_l (void *target_func, InterpMethodArguments *margs)
{
	gint64 (*func)(void) = target_func;

	gint64 res = func ();
	*(gint64*)margs->retval = res;
}

static void
wasm_invoke_ll (void *target_func, InterpMethodArguments *margs)
{
	gint64 (*func)(gint64 a) = target_func;

	interp_pair p;
	p.pair.lo = (gint32)margs->iargs [0];
	p.pair.hi = (gint32)margs->iargs [1];

	gint64 res = func (p.l);
	*(gint64*)margs->retval = res;
}

static void
wasm_invoke_li (void *target_func, InterpMethodArguments *margs)
{
	gint64 (*func)(gpointer a) = target_func;
	gint64 res = func (margs->iargs [0]);
	*(gint64*)margs->retval = res;
}

static void
wasm_invoke_lil (void *target_func, InterpMethodArguments *margs)
{
	gint64 (*func)(gpointer a, gint64 b) = target_func;

	interp_pair p;
	p.pair.lo = (gint32)margs->iargs [1];
	p.pair.hi = (gint32)margs->iargs [2];

	gint64 res = func (margs->iargs [0], p.l);
	*(gint64*)margs->retval = res;
}

static void
wasm_invoke_dd (void *target_func, InterpMethodArguments *margs)
{
	double (*func)(double a) = target_func;

	double res = func (margs->fargs [FIDX (0)]);
	*(double*)margs->retval = res;
}

static void
wasm_invoke_ddd (void *target_func, InterpMethodArguments *margs)
{
	double (*func)(double a, double b) = target_func;

	double res = func (margs->fargs [FIDX (0)], margs->fargs [FIDX (1)]);
	*(double*)margs->retval = res;
}


	
static void
wasm_invoke_vif (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, float b) = target_func;

	func (margs->iargs [0], 
		*(float*)&margs->fargs [FIDX (0)]);
}

static void
wasm_invoke_viff (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, float b, float c) = target_func;

	func (margs->iargs [0],
		*(float*)&margs->fargs [FIDX (0)],
		*(float*)&margs->fargs [FIDX (1)]);
}

static void
wasm_invoke_viffff (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, float b, float c, float d, float e) = target_func;

	func (margs->iargs [0],
		*(float*)&margs->fargs [FIDX (0)],
		*(float*)&margs->fargs [FIDX (1)],
		*(float*)&margs->fargs [FIDX (2)],
		*(float*)&margs->fargs [FIDX (3)]);
}

static void
wasm_invoke_vifffffi (void *target_func, InterpMethodArguments *margs)
{
	void (*func)(gpointer a, float b, float c, float d, float e, float f, int g) = target_func;

	func (margs->iargs [0],
		*(float*)&margs->fargs [FIDX (0)],
		*(float*)&margs->fargs [FIDX (1)],
		*(float*)&margs->fargs [FIDX (2)],
		*(float*)&margs->fargs [FIDX (3)],
		*(float*)&margs->fargs [FIDX (4)],
		*(float*)&margs->iargs [1]);
}

static void
wasm_invoke_ff (void *target_func, InterpMethodArguments *margs)
{
	float (*func)(float a) = target_func;

	float res = func (*(float*)&margs->fargs [FIDX (0)]);
	*(float*)margs->retval = res;
}

static void
wasm_enter_icall_trampoline (void *target_func, InterpMethodArguments *margs)
{
	static char cookie [8];
	static int c_count;

	MonoMethodSignature *sig = margs->sig;

	c_count = sig->param_count + sig->hasthis + 1;
	cookie [0] = type_to_c (sig->ret);
	if (sig->hasthis)
		cookie [1] = 'I';
	for (int i = 0; i < sig->param_count; ++i)
		cookie [1 + sig->hasthis + i ] = type_to_c (sig->params [i]);
	cookie [c_count] = 0;

	if (!strcmp ("V", cookie))
		wasm_invoke_v (target_func, margs);
	else if (!strcmp ("VI", cookie))
		wasm_invoke_vi (target_func, margs);
	else if (!strcmp ("VII", cookie))
		wasm_invoke_vii (target_func, margs);
	else if (!strcmp ("VIII", cookie))
		wasm_invoke_viii (target_func, margs);
	else if (!strcmp ("VIIII", cookie))
		wasm_invoke_viiii (target_func, margs);
	else if (!strcmp ("VIIIII", cookie))
		wasm_invoke_viiiii (target_func, margs);
	else if (!strcmp ("VIIIIII", cookie))
		wasm_invoke_viiiiii (target_func, margs);
	else if (!strcmp ("I", cookie))
		wasm_invoke_i (target_func, margs);
	else if (!strcmp ("II", cookie))
		wasm_invoke_ii (target_func, margs);
	else if (!strcmp ("III", cookie))
		wasm_invoke_iii (target_func, margs);
	else if (!strcmp ("IIII", cookie))
		wasm_invoke_iiii (target_func, margs);
	else if (!strcmp ("IIIII", cookie))
		wasm_invoke_iiiii (target_func, margs);
	else if (!strcmp ("IIIIII", cookie))
		wasm_invoke_iiiiii (target_func, margs);
	else if (!strcmp ("IIIIIII", cookie))
		wasm_invoke_iiiiiii (target_func, margs);
	else if (!strcmp ("IIIIIIIII", cookie))
		wasm_invoke_iiiiiiiii (target_func, margs);
	else if (!strcmp ("L", cookie))
		wasm_invoke_l (target_func, margs);
	else if (!strcmp ("LL", cookie))
		wasm_invoke_ll (target_func, margs);
	else if (!strcmp ("LI", cookie))
		wasm_invoke_li (target_func, margs);
	else if (!strcmp ("LIL", cookie))
		wasm_invoke_lil (target_func, margs);
	else if (!strcmp ("DD", cookie))
		wasm_invoke_dd (target_func, margs);
	else if (!strcmp ("DDD", cookie))
		wasm_invoke_ddd (target_func, margs);
	else if (!strcmp ("VIF", cookie))
		wasm_invoke_vif (target_func, margs);
	else if (!strcmp ("VIFF", cookie))
		wasm_invoke_viff (target_func, margs);
	else if (!strcmp ("VIFFFF", cookie))
		wasm_invoke_viffff (target_func, margs);
	else if (!strcmp ("VIFFFFFI", cookie))
		wasm_invoke_vifffffi (target_func, margs);
	else if (!strcmp ("FF", cookie))
		wasm_invoke_ff (target_func, margs);
	else {
		printf ("CANNOT HANDLE COOKIE %s\n", cookie);
		g_assert (0);
	}
}

gpointer
mono_aot_get_trampoline_full (const char *name, MonoTrampInfo **out_tinfo)
{
	gpointer code = NULL;

	if (!strcmp (name, "restore_context"))
		code = wasm_restore_context;
	else if (!strcmp (name, "call_filter"))
		code = wasm_call_filter;
	else if (!strcmp (name, "throw_exception"))
		code = wasm_throw_exception;
	else if (!strcmp (name, "rethrow_exception"))
		code = wasm_rethrow_exception;
	else if (!strcmp (name, "throw_corlib_exception"))
		code = wasm_throw_corlib_exception;
	else if (!strcmp (name, "enter_icall_trampoline"))
		code = wasm_enter_icall_trampoline;

	g_assert (code);

	if (out_tinfo) {
		MonoTrampInfo *tinfo = g_new0 (MonoTrampInfo, 1);
		tinfo->code = code;
		tinfo->code_size = 1;
		tinfo->name = g_strdup (name);
		tinfo->ji = NULL;
		tinfo->unwind_ops = NULL;
		tinfo->uw_info = NULL;
		tinfo->uw_info_len = 0;
		tinfo->owns_uw_info = FALSE;

		*out_tinfo = tinfo;
	}

	return code;
}
#else /* TARGET_WASM */

MONO_EMPTY_SOURCE_FILE (aot_runtime_wasm);

#endif /* TARGET_WASM */
