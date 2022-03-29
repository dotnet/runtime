// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_APPDOMAIN_TYPES_H
#define _MONO_APPDOMAIN_TYPES_H

#include <mono/utils/details/mono-publib-types.h>
#include <mono/utils/mono-forward.h>
#include <mono/metadata/details/object-types.h>
#include <mono/metadata/details/reflection-types.h>

MONO_BEGIN_DECLS

typedef void (*MonoThreadStartCB) (intptr_t tid, void* stack_start,
				   void* func);
typedef void (*MonoThreadAttachCB) (intptr_t tid, void* stack_start);

typedef struct _MonoAppDomain MonoAppDomain;

typedef void (*MonoDomainFunc) (MonoDomain *domain, void* user_data);

typedef mono_bool (*MonoCoreClrPlatformCB) (const char *image_name);

MONO_END_DECLS

#endif /* _MONO_APPDOMAIN_TYPES_H */
