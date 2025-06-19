#ifndef __MONO_MINI_INTERP_PINVOKE_H__
#define __MONO_MINI_INTERP_PINVOKE_H__

#include <glib.h>
#include <mono/metadata/object.h>
#include "mintops.h"
#include "interp-internals.h"
#include "interp-frame.h"
#include "interp-mono.h"
#include "interp-mono-abi.h"
#include "interp-mono-abi-pinvoke.h"

extern MONO_NO_OPTIMIZATION MONO_NEVER_INLINE gpointer
ves_pinvoke_method (
	InterpMethod *imethod,
	MonoMethodSignature *sig,
	MonoFuncV addr,
	ThreadContext *context,
	InterpFrame *parent_frame,
	stackval *ret_sp,
	stackval *sp,
	gboolean save_last_error,
	gpointer *cache,
	gboolean *gc_transitions);

#endif
