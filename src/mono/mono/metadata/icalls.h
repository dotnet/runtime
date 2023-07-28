/**
 * \file
 */

#ifndef __MONO_METADATA_ICALLS_H__
#define __MONO_METADATA_ICALLS_H__

#include <mono/utils/mono-publib.h>

typedef enum {
	MONO_ICALL_FLAGS_NONE = 0,
	MONO_ICALL_FLAGS_FOREIGN = 1 << 1,
	MONO_ICALL_FLAGS_USES_HANDLES = 1 << 2,
	MONO_ICALL_FLAGS_COOPERATIVE = 1 << 3,
	MONO_ICALL_FLAGS_NO_WRAPPER = 1 << 4,
	MONO_ICALL_FLAGS_NO_EXCEPTION = 1 << 5,
} MonoInternalCallFlags;

#ifdef ENABLE_ICALL_EXPORT
#define ICALL_EXPORT MONO_API
#define ICALL_EXTERN_C G_EXTERN_C
#else
/* Can't be static as icall.c defines icalls referenced by icall-tables.c */
#define ICALL_EXPORT /* nothing */
#define ICALL_EXTERN_C /* nothing */
#endif

#endif // __MONO_METADATA_ICALLS_H__
