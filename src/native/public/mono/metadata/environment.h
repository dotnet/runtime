/**
 * \file
 * System.Environment support internal calls
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *
 * (C) 2002 Ximian, Inc
 */

#ifndef _MONO_METADATA_ENVIRONMENT_H_
#define _MONO_METADATA_ENVIRONMENT_H_

#include <mono/utils/mono-publib.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/environment-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* _MONO_METADATA_ENVIRONMENT_H_ */
