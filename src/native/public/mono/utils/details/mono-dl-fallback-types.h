// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_DL_FALLBACK_TYPES_H
#define _MONO_DL_FALLBACK_TYPES_H

#include <mono/utils/details/mono-publib-types.h>

MONO_BEGIN_DECLS

enum {
	MONO_DL_EAGER  = 0,
	MONO_DL_LAZY   = 1,
	// If MONO_DL_LOCAL is set, it will trump MONO_DL_GLOBAL.
	MONO_DL_LOCAL  = 2,
	// MONO_DL_MASK is unused internally and no longer a full mask on netcore, given the introduction of MONO_DL_GLOBAL. Avoid.
	MONO_DL_MASK   = 3,
	// Only applicable when building Mono in netcore mode.
	MONO_DL_GLOBAL = 4
};

/*
 * This is the dynamic loader fallback API
 */
typedef struct MonoDlFallbackHandler MonoDlFallbackHandler;

/*
 * The "err" variable contents must be allocated using g_malloc or g_strdup
 */
typedef void* (*MonoDlFallbackLoad) (const char *name, int flags, char **err, void *user_data);
typedef void* (*MonoDlFallbackSymbol) (void *handle, const char *name, char **err, void *user_data);
typedef void* (*MonoDlFallbackClose) (void *handle, void *user_data);

MONO_END_DECLS

#endif
