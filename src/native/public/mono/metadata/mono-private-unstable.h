/**
 * \file
 *
 * Private unstable APIs.
 *
 * WARNING: The declarations and behavior of functions in this header are NOT STABLE and can be modified or removed at
 * any time.
 *
 */


#ifndef __MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__
#define __MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__

#include <mono/utils/mono-forward.h>
#include <mono/utils/mono-publib.h>

#include <mono/metadata/details/mono-private-unstable-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/mono-private-unstable-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /*__MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__*/
