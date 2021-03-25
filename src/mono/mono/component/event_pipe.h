// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_EVENT_PIPE_H
#define _MONO_COMPONENT_EVENT_PIPE_H

#include "mono/component/component.h"

typedef struct _MonoComponentEventPipe {
	MonoComponent component;
	void (*ep_init) (void);
	void (*ep_finish_init) (void);
	void (*ep_shutdown) (void);
} MonoComponentEventPipe;

#ifdef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentEventPipe *
mono_component_event_pipe_init (void);
#endif

#endif /*_MONO_COMPONENT_EVENT_PIPE_H*/
