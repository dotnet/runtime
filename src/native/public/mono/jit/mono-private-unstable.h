/**
 * \file
 *
 * Private unstable APIs.
 *
 * WARNING: The declarations and behavior of functions in this header are NOT STABLE and can be modified or removed at
 * any time.
 *
 */


#ifndef __MONO_JIT_MONO_PRIVATE_UNSTABLE_H__
#define __MONO_JIT_MONO_PRIVATE_UNSTABLE_H__

#include <mono/jit/details/mono-private-unstable-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/jit/details/mono-private-unstable-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /*__MONO_JIT_MONO_PRIVATE_UNSTABLE_H__*/
