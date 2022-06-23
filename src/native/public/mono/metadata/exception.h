/**
 * \file
 */

#ifndef _MONO_METADATA_EXCEPTION_H_
#define _MONO_METADATA_EXCEPTION_H_

#include <mono/metadata/details/exception-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/exception-functions.h>
#undef MONO_API_FUNCTION

void          mono_invoke_unhandled_exception_hook  (MonoObject *exc);

MONO_END_DECLS

#endif /* _MONO_METADATA_EXCEPTION_H_ */
