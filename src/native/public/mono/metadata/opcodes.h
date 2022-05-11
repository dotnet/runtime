/**
 * \file
 */

#ifndef __MONO_METADATA_OPCODES_H__
#define __MONO_METADATA_OPCODES_H__

/*
 * opcodes.h: CIL instruction information
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <mono/metadata/details/opcodes-types.h>

MONO_BEGIN_DECLS
MONO_API_DATA const MonoOpcode mono_opcodes [];

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/metadata/details/opcodes-functions.h>
#undef MONO_API_FUNCTION
MONO_END_DECLS

#endif /* __MONO_METADATA_OPCODES_H__ */

