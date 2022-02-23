/**
 * \file
 */

#ifndef __MONO_LOGGER_H__
#define __MONO_LOGGER_H__

#include <mono/utils/mono-publib.h>
MONO_BEGIN_DECLS

#include <mono/utils/details/mono-logger-types.h>

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/utils/details/mono-logger-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* __MONO_LOGGER_H__ */
