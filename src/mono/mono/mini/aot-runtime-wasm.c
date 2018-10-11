/**
 * \file
 * WASM AOT runtime
 */

#include "config.h"

#include <sys/types.h>

#include "mini.h"
#include "interp/interp.h"

#ifdef TARGET_WASM

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
		if (m_class_is_enumtype (t->data.klass)) {
			t = mono_class_enum_basetype_internal (t->data.klass);
			goto handle_enum;
		}

		return 'I';
	case MONO_TYPE_GENERICINST:
		if (m_class_is_valuetype (t->data.klass))
			return 'S';
		return 'I';
	default:
		g_warning ("CANT TRANSLATE %s", mono_type_full_name (t));
		return 'X';
	}
}

#if TARGET_SIZEOF_VOID_P == 4
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

void
mono_wasm_interp_to_native_trampoline (void *target_func, InterpMethodArguments *margs)
{
	char cookie [32];
	int c_count;

	MonoMethodSignature *sig = margs->sig;

	c_count = sig->param_count + sig->hasthis + 1;
	g_assert (c_count < sizeof (cookie)); //ensure we don't overflow the local

	cookie [0] = type_to_c (sig->ret);
	if (sig->hasthis)
		cookie [1] = 'I';
	for (int i = 0; i < sig->param_count; ++i) {
		cookie [1 + sig->hasthis + i] = type_to_c (sig->params [i]);
	}
	cookie [c_count] = 0;

	icall_trampoline_dispatch (cookie, target_func, margs);
}

#else /* TARGET_WASM */

MONO_EMPTY_SOURCE_FILE (aot_runtime_wasm);

#endif /* TARGET_WASM */
