// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_METADATA_COMPONENTS_H
#define _MONO_METADATA_COMPONENTS_H

#include <mono/component/component.h>
#include <mono/component/hot_reload.h>

void
mono_components_init (void);

void
mono_components_cleanup (void);

/* Declare each component's getter function here */
MonoComponentHotReload *
mono_component_hot_reload (void);

/* Declare each copomnents stub init function here */
MonoComponentHotReload *
mono_component_hot_reload_stub_init (void);

#endif/*_MONO_METADATA_COMPONENTS_H*/
