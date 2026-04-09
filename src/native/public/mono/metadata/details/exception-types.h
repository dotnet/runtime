// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_EXCEPTION_TYPES_H
#define _MONO_EXCEPTION_TYPES_H

#include <mono/metadata/object-forward.h>
#include <mono/metadata/details/object-types.h>
#include <mono/metadata/details/image-types.h>


MONO_BEGIN_DECLS

typedef void  (*MonoUnhandledExceptionFunc)         (MonoObject *exc, void *user_data);

MONO_END_DECLS

#endif /* _MONO_EXCEPTION_TYPES_H */
