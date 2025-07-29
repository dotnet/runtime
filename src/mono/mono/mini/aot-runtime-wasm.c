/**
 * \file
 * WASM AOT runtime
 */

#include "config.h"

#include <sys/types.h>

#include "mini.h"
#include <mono/jit/mono-private-unstable.h>
#include "interp/interp.h"
#include "aot-runtime.h"
#include <mono/metadata/mh_log.h>
#ifdef HOST_WASM

#if SIZEOF_VOID_P == 4
static const char ptrChar = 'I';
#else
static const char ptrChar = 'L';
#endif

static char
type_to_c (MonoType *t, gboolean *is_byref_return)
{
	g_assert (t);

	if (is_byref_return)
		*is_byref_return = 0;
	if (m_type_is_byref (t))
	{
		return ptrChar;
	}

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
		return 'I';
	case MONO_TYPE_I: 
	case MONO_TYPE_U:		 
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
		return ptrChar;
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
	case MONO_TYPE_VALUETYPE: {
		MH_LOG_INDENT();	
		MH_LOG("Handling valuetype %s", mono_type_full_name (t));
		if (m_class_is_enumtype (m_type_data_get_klass_unchecked (t))) {
			t = mono_class_enum_basetype_internal (m_type_data_get_klass_unchecked (t));
			goto handle_enum;
		}

		// https://github.com/WebAssembly/tool-conventions/blob/main/BasicCABI.md#function-signatures
		// Any struct or union that recursively (including through nested structs, unions, and arrays)
		//  contains just a single scalar value and is not specified to have greater than natural alignment.
		// FIXME: Handle the scenario where there are fields of struct types that contain no members
		MonoType *scalar_vtype;
		if (mini_wasm_is_scalar_vtype (t, &scalar_vtype))
			return type_to_c (scalar_vtype, NULL);

		if (is_byref_return)
			*is_byref_return = 1;
		return ptrChar;		
	}
	case MONO_TYPE_GENERICINST: {
		// This previously erroneously used m_type_data_get_klass which isn't legal for genericinst, we have to use class_from_mono_type_internal
		if (m_class_is_valuetype (mono_class_from_mono_type_internal (t))) {
			MonoType *scalar_vtype;
			if (mini_wasm_is_scalar_vtype (t, &scalar_vtype))
				return type_to_c (scalar_vtype, NULL);

			if (is_byref_return)
				*is_byref_return = 1;

			return 'S';
		}
		return ptrChar;		
	}
	default:
		g_warning ("CANT TRANSLATE %s", mono_type_full_name (t));
		return 'X';
	}
}

#define FIDX(x) (x)

typedef union {
	gint64 l;
	struct {
		gint32 lo;
		gint32 hi;
	} pair;
} interp_pair;

static gint64
get_long_arg (InterpMethodArguments *margs, int idx)
{
	#if SIZEOF_VOID_P == 4
	interp_pair p;
	MH_LOG_INDENT();
	MH_LOG("Getting long arg [%d]", idx);
	p.pair.lo = (gint32)(gssize)margs->iargs [idx];
	p.pair.hi = (gint32)(gssize)margs->iargs [idx + 1];
	MH_LOG("Got pair %d %d", p.pair.lo, p.pair.hi);
	MH_LOG_UNINDENT();
	return p.l;
	#else
	MH_LOG_INDENT();
	MH_LOG("Getting long arg [%d]: %lld", idx, (gint64)(gssize)margs->iargs [idx]);
	MH_LOG_UNINDENT();
	return (gint64)(gssize)margs->iargs [idx];
	#endif
}

static MonoWasmNativeToInterpCallback mono_wasm_interp_to_native_callback;

void
mono_wasm_install_interp_to_native_callback (MonoWasmNativeToInterpCallback cb)
{
	mono_wasm_interp_to_native_callback = cb;
}

int
mono_wasm_interp_method_args_get_iarg (InterpMethodArguments *margs, int i)
{
	MH_LOG_INDENT();
	MH_LOG("Looking for iarg[%d]", i);
	int retval = (int)(gssize)margs->iargs[i];
	MH_LOG("Got %d", retval);
	MH_LOG_UNINDENT();
	return retval;	
}

gint64
mono_wasm_interp_method_args_get_larg (InterpMethodArguments *margs, int i)
{
	return get_long_arg (margs, i);
}

float
mono_wasm_interp_method_args_get_farg (InterpMethodArguments *margs, int i)
{
	return *(float*)&margs->fargs [FIDX (i)];
}

double
mono_wasm_interp_method_args_get_darg (InterpMethodArguments *margs, int i)
{
	return margs->fargs [FIDX (i)];
}

gpointer*
mono_wasm_interp_method_args_get_retval (InterpMethodArguments *margs)
{
	return margs->retval;
}

static int
compare_icall_tramp (const void *key, const void *elem)
{
	return strcmp (key, *(void**)elem);
}
static void 
logCookie (int c_count, const char *cookie) {
	MH_LOG("WASM ICALL COOKIE: %s\n", cookie);
}
gpointer
mono_wasm_get_interp_to_native_trampoline (MonoMethodSignature *sig)
{
	char cookie [32];
	int c_count, offset = 1;
	gboolean is_byref_return = 0;

	memset (cookie, 0, 32);
	cookie [0] = type_to_c (sig->ret, &is_byref_return);
	MH_LOG_INDENT();
	MH_LOG("Parameter cookie[0] = %c (from type: %s (enum: %d))\n", cookie [0], mono_type_get_name_full(sig->ret, MONO_TYPE_NAME_FORMAT_FULL_NAME), (int)sig->ret->type);

	c_count = sig->param_count + sig->hasthis + is_byref_return + 1;
	g_assert (c_count < sizeof (cookie)); //ensure we don't overflow the local

	if (is_byref_return) {
		cookie[0] = 'V';
		// return value address goes in arg0
		cookie[1] = ptrChar;
		MH_LOG_INDENT();
		MH_LOG("Return value is byref: cookie[0][1] = %c, %c", cookie [0], cookie [1]);		
		MH_LOG_UNINDENT();
		offset += 1;
	}
	if (sig->hasthis) {
		// thisptr goes in arg0/arg1 depending on return type
		cookie [offset] = ptrChar;
		MH_LOG_INDENT();
		MH_LOG("Sig hasthis: value is byref: cookie[%d] = %c", offset, cookie [offset]);		
		MH_LOG_UNINDENT();
		offset += 1;
	}
	
	for (int i = 0; i < sig->param_count; ++i) {
		cookie [offset + i] = type_to_c (sig->params [i], NULL);
		MH_LOG("Parameter cookie[%d] = %c (from type: %s)\n", offset + i, cookie [offset + i], mono_type_get_name_full(sig->params[i], MONO_TYPE_NAME_FORMAT_FULL_NAME));
	}
	MH_LOG_UNINDENT();
	logCookie(c_count, cookie);

	void *p = mono_wasm_interp_to_native_callback (cookie);
	if (!p)
		g_error ("CANNOT HANDLE INTERP ICALL SIG %s\n", cookie);
	MH_LOG("Got interp to native trampoline %p for cookie %s", p, cookie);
	return p;
}

static MonoWasmGetNativeToInterpTramp get_native_to_interp_tramp_cb;

MONO_API void
mono_wasm_install_get_native_to_interp_tramp (MonoWasmGetNativeToInterpTramp cb)
{
	get_native_to_interp_tramp_cb = cb;
}

gpointer
mono_wasm_get_native_to_interp_trampoline (MonoMethod *method, gpointer extra_arg)
{
	if (get_native_to_interp_tramp_cb)
		return get_native_to_interp_tramp_cb (method, extra_arg);
	else
		return NULL;
}

#else /* TARGET_WASM */

void
mono_wasm_install_get_native_to_interp_tramp (MonoWasmGetNativeToInterpTramp cb)
{
	g_assert_not_reached ();
}

#endif /* TARGET_WASM */
