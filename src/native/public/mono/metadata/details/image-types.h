// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_IMAGE_TYPES_H
#define _MONO_IMAGE_TYPES_H

#include <stdio.h>
#include <mono/utils/details/mono-publib-types.h>
#include <mono/utils/details/mono-error-types.h>
#include <mono/metadata/object-forward.h>

MONO_BEGIN_DECLS

typedef struct _MonoAssembly MonoAssembly;
typedef struct _MonoAssemblyName MonoAssemblyName;
typedef struct _MonoTableInfo MonoTableInfo;

typedef enum {
	MONO_IMAGE_OK,
	MONO_IMAGE_ERROR_ERRNO,
	MONO_IMAGE_MISSING_ASSEMBLYREF,
	MONO_IMAGE_IMAGE_INVALID,
	MONO_IMAGE_NOT_SUPPORTED, ///< \since net7
} MonoImageOpenStatus;


MONO_END_DECLS

#endif /* _MONO_IMAGE_TYPES_H */
