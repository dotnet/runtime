/**
 * \file
 * AppDomain functions
 *
 * Author:
 *	Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#ifndef _MONO_METADATA_APPDOMAIN_H_
#define _MONO_METADATA_APPDOMAIN_H_

#include <mono/metadata/details/appdomain-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/appdomain-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* _MONO_METADATA_APPDOMAIN_H_ */

