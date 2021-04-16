// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_COMPONENT_H
#define _MONO_COMPONENT_COMPONENT_H

#include <stdint.h>
#ifndef __cplusplus
#include <stdbool.h>
#endif  // __cplusplus

#define MONO_COMPONENT_ITF_VERSION 1

typedef struct _MonoComponent MonoComponent;

typedef bool (*MonoComponent_AvailableFn) (void);

struct _MonoComponent {
	intptr_t itf_version;
	MonoComponent_AvailableFn available;
};

#endif/*_MONO_COMPONENT_COMPONENT_H*/
