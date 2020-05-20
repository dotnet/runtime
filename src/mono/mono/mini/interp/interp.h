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

#define MAX_INTERP_ENTRY_ARGS 8

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

enum {
	INTERP_OPT_NONE = 0,
	INTERP_OPT_INLINE = 1,
	INTERP_OPT_CPROP = 2,
	INTERP_OPT_SUPER_INSTRUCTIONS = 4,
	INTERP_OPT_DEFAULT = INTERP_OPT_INLINE | INTERP_OPT_CPROP | INTERP_OPT_SUPER_INSTRUCTIONS
};

typedef struct _InterpMethodArguments InterpMethodArguments;

/* must be called either
 *  - by mini_init ()
 *  - xor, before mini_init () is called (embedding scenario).
 */
MONO_API void mono_ee_interp_init (const char *);

#endif /* __MONO_MINI_INTERPRETER_H__ */
