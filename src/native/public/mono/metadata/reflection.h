/**
 * \file
 */

#ifndef __METADATA_REFLECTION_H__
#define __METADATA_REFLECTION_H__

#include <mono/metadata/details/reflection-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/reflection-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* __METADATA_REFLECTION_H__ */
