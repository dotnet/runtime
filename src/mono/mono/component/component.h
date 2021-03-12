// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_COMPONENT_H
#define _MONO_COMPONENT_COMPONENT_H

typedef struct _MonoComponent MonoComponent;

typedef void (*MonoComponent_CleanupFn) (MonoComponent *self);

struct _MonoComponent {
	MonoComponent_CleanupFn cleanup;
};

#endif/*_MONO_COMPONENT_COMPONENT_H*/
