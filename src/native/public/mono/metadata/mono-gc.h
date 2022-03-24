/**
 * \file
 * GC related public interface
 *
 */
#ifndef __METADATA_MONO_GC_H__
#define __METADATA_MONO_GC_H__

#include <mono/metadata/details/mono-gc-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/mono-gc-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* __METADATA_MONO_GC_H__ */

