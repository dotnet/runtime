/**
 * \file
 */

#ifndef _MONO_CLI_CLASS_H_
#define _MONO_CLI_CLASS_H_

#include <mono/metadata/metadata.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>
#include <mono/utils/mono-error.h>

#include <mono/metadata/details/class-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/class-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* _MONO_CLI_CLASS_H_ */
