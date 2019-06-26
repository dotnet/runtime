/**
 * \file
 */

#ifndef __MONO_METADATA_ICALLS_H__
#define __MONO_METADATA_ICALLS_H__

#include <mono/utils/mono-publib.h>

#ifdef ENABLE_ICALL_EXPORT
#define ICALL_DECL_EXPORT MONO_API
#define ICALL_EXPORT MONO_API
#else
#define ICALL_DECL_EXPORT /* nothing */
/* Can't be static as icall.c defines icalls referenced by icall-tables.c */
#define ICALL_EXPORT /* nothing */
#endif

#endif // __MONO_METADATA_ICALLS_H__
