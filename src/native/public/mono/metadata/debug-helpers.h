/**
 * \file
 */

#ifndef __MONO_DEBUG_HELPERS_H__
#define __MONO_DEBUG_HELPERS_H__

#include <mono/metadata/details/debug-helpers-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/debug-helpers-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* __MONO_DEBUG_HELPERS_H__ */

