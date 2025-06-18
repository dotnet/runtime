#ifndef __MONO_MINI_INTERP_ICALLS_H__
#define __MONO_MINI_INTERP_ICALLS_H__

#include <glib.h>
#include <mono/metadata/object.h>
#include "mintSigs.h"
#include "interp-internals.h"
#include "interp-enum-codecs.h"

extern void do_icall(MonoMethodSignature * sig, MintICallSig op, stackval * ret_sp, stackval * sp, gpointer ptr, gboolean save_last_error);

#endif
