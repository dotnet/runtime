/**
 * \file
 * Threading API
 *
 * Author:
 *	Dick Porter (dick@ximian.com)
 *	Patrik Torstensson (patrik.torstensson@labs2.com)
 *
 * (C) 2001 Ximian, Inc
 */

#ifndef _MONO_METADATA_THREADS_H_
#define _MONO_METADATA_THREADS_H_

#include <mono/metadata/details/threads-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/threads-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* _MONO_METADATA_THREADS_H_ */
