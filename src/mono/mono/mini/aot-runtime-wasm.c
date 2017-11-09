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



typedef union {
	gint64 l;
	struct {
		gint32 lo;
		gint32 hi;
	} pair;
} interp_pair;

static inline gint64
get_long_arg (InterpMethodArguments *margs, int idx)
{
	interp_pair p;
	p.pair.lo = (gint32)margs->iargs [idx];
	p.pair.hi = (gint32)margs->iargs [idx + 1];
	return p.l;
}

#include "wasm_m2n_invoke.g.h"

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

	icall_trampoline_dispatch (cookie, target_func, margs);
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
