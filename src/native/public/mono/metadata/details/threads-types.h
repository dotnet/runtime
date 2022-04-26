// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_METADATA_DETAILS_THREADS_TYPES_H
#define _MONO_METADATA_DETAILS_THREADS_TYPES_H

#include <mono/utils/details/mono-publib-types.h>
#include <mono/metadata/details/object-types.h>
#include <mono/metadata/details/appdomain-types.h>

MONO_BEGIN_DECLS

/* This callback should return TRUE if the runtime must wait for the thread, FALSE otherwise */
typedef mono_bool (*MonoThreadManageCallback) (MonoThread* thread);

MONO_END_DECLS

#endif /* _MONO_METADATA_DETAILS_THREADS_TYPES_H */
