/**
 * \file
 */

#ifndef _MONONET_METADATA_ASSEMBLY_H_
#define _MONONET_METADATA_ASSEMBLY_H_

#include <mono/metadata/details/assembly-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/assembly-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif

