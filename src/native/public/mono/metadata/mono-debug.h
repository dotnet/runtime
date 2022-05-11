/**
 * \file
 * This header is only installed for use by the debugger:
 * the structures and the API declared here are not supported.
 */

#ifndef __MONO_DEBUG_H__
#define __MONO_DEBUG_H__

#include <mono/metadata/details/mono-debug-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/mono-debug-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* __MONO_DEBUG_H__ */
