// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_LOADER_TYPES_H
#define _MONO_LOADER_TYPES_H

#include <mono/utils/mono-forward.h>
#include <mono/metadata/details/metadata-types.h>
#include <mono/metadata/details/image-types.h>
#include <mono/utils/details/mono-error-types.h>

MONO_BEGIN_DECLS

typedef mono_bool (*MonoStackWalk)     (MonoMethod *method, int32_t native_offset, int32_t il_offset, mono_bool managed, void* data);

typedef mono_bool (*MonoStackWalkAsyncSafe)     (MonoMethod *method, MonoDomain *domain, void *base_address, int offset, void* data);

MONO_END_DECLS

#endif /* _MONO_LOADER_TYPES_H */
