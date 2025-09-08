#ifndef __MONO_MINI_INTERP_ICALLS_H__
#define __MONO_MINI_INTERP_ICALLS_H__

#include <glib.h>
#include <mono/metadata/object.h>
#include "mintops.h"
#include "interp-internals.h"
extern void stackval_from_data (MonoType *type, stackval *result, const void *data, gboolean pinvoke);
extern void do_icall(MonoMethodSignature * sig, MintICallSig op, stackval * ret_sp, stackval * sp, gpointer ptr, gboolean save_last_error);
extern gboolean interp_type_as_ptr4 (MonoType *tp);
extern gboolean interp_type_as_ptr8 (MonoType *tp);
#endif
