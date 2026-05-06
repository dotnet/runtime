// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_CLASS_TYPES_H
#define _MONO_CLASS_TYPES_H

#include <mono/metadata/details/metadata-types.h>
#include <mono/metadata/details/image-types.h>
#include <mono/metadata/details/loader-types.h>
#include <mono/utils/details/mono-error-types.h>

MONO_BEGIN_DECLS

typedef struct MonoVTable MonoVTable;

typedef struct _MonoClassField MonoClassField;
typedef struct _MonoProperty MonoProperty;
typedef struct _MonoEvent MonoEvent;

typedef enum {
	MONO_TYPE_NAME_FORMAT_IL,
	MONO_TYPE_NAME_FORMAT_REFLECTION,
	MONO_TYPE_NAME_FORMAT_FULL_NAME,
	MONO_TYPE_NAME_FORMAT_ASSEMBLY_QUALIFIED
} MonoTypeNameFormat;

MONO_END_DECLS

#endif /* _MONO_CLASS_TYPES_H */
