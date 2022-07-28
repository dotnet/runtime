// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_OBJECT_TYPES_H
#define _MONO_OBJECT_TYPES_H

#include <mono/utils/mono-forward.h>
#include <mono/metadata/object-forward.h>
#include <mono/metadata/details/class-types.h>
#include <mono/utils/details/mono-error-types.h>

MONO_BEGIN_DECLS

typedef struct _MonoString MONO_RT_MANAGED_ATTR MonoString;
typedef struct _MonoArray MONO_RT_MANAGED_ATTR MonoArray;
typedef struct _MonoReflectionMethod MONO_RT_MANAGED_ATTR MonoReflectionMethod;
typedef struct _MonoReflectionModule MONO_RT_MANAGED_ATTR MonoReflectionModule;
typedef struct _MonoReflectionField MONO_RT_MANAGED_ATTR MonoReflectionField;
typedef struct _MonoReflectionProperty MONO_RT_MANAGED_ATTR MonoReflectionProperty;
typedef struct _MonoReflectionEvent MONO_RT_MANAGED_ATTR MonoReflectionEvent;
typedef struct _MonoReflectionType MONO_RT_MANAGED_ATTR MonoReflectionType;
typedef struct _MonoDelegate MONO_RT_MANAGED_ATTR MonoDelegate;
typedef struct _MonoThreadsSync MonoThreadsSync;
typedef struct _MonoInternalThread MONO_RT_MANAGED_ATTR MonoThread;
typedef struct _MonoDynamicAssembly MonoDynamicAssembly;
typedef struct _MonoDynamicImage MonoDynamicImage;
typedef struct _MonoReflectionMethodBody MONO_RT_MANAGED_ATTR MonoReflectionMethodBody;
typedef struct _MonoAppContext MONO_RT_MANAGED_ATTR MonoAppContext;

struct _MonoObject {
	MonoVTable *vtable;
	MonoThreadsSync *synchronisation;
};

typedef MonoObject* (*MonoInvokeFunc)	     (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error);
typedef void*    (*MonoCompileFunc)	     (MonoMethod *method);
typedef void	    (*MonoMainThreadFunc)    (void* user_data);

typedef void (*mono_reference_queue_callback) (void *user_data);
typedef struct _MonoReferenceQueue MonoReferenceQueue;

MONO_END_DECLS

#endif /* _MONO_OBJECT_TYPES_H */
