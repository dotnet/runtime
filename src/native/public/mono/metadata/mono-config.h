/**
 * \file
 *
 * Author: Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */
#ifndef __MONO_METADATA_CONFIG_H__
#define __MONO_METADATA_CONFIG_H__

#include <mono/metadata/details/mono-config-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/mono-config-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* __MONO_METADATA_CONFIG_H__ */

