/**
 * \file
 */

#ifndef __MONO_MINI_INTERPRETER_H__
#define __MONO_MINI_INTERPRETER_H__
#include <mono/mini/mini-runtime.h>

#ifdef TARGET_WASM
#define INTERP_ICALL_TRAMP_IARGS 12
#define INTERP_ICALL_TRAMP_FARGS 12
#else
#define INTERP_ICALL_TRAMP_IARGS 12
#define INTERP_ICALL_TRAMP_FARGS 4
#endif

struct _InterpMethodArguments {
	size_t ilen;
	gpointer *iargs;
	size_t flen;
	double *fargs;
	gpointer *retval;
	size_t is_float_ret;
#ifdef TARGET_WASM
	MonoMethodSignature *sig;
#endif
};

typedef struct _InterpMethodArguments InterpMethodArguments;

/* must be called either
 *  - by mini_init ()
 *  - xor, before mini_init () is called (embedding scenario).
 */
MONO_API void mono_ee_interp_init (const char *);

#endif /* __MONO_MINI_INTERPRETER_H__ */
