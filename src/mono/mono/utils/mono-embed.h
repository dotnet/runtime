/**
 * \file
 */

#ifndef __MONO_EMBED_H__
#define __MONO_EMBED_H__

#include <mono/utils/mono-publib.h>

/* 
 * This is a fallback for platform symbol loading functionality.
 */
typedef struct {
	const char *name;	
	void *addr;
} MonoDlMapping;

MONO_API void mono_dl_register_library (const char *name, MonoDlMapping *mappings);

#endif /* __MONO_EMBED_H__ */
