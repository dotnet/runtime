/**
 * \file
 */

#ifndef _MONO_METADATA_LOADER_H_
#define _MONO_METADATA_LOADER_H_ 1

#include <mono/metadata/details/loader-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/loader-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif

