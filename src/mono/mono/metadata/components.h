// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_METADATA_COMPONENTS_H
#define _MONO_METADATA_COMPONENTS_H

#include <mono/component/component.h>
#include <mono/component/hot_reload.h>
#include <mono/component/event_pipe.h>
#include <mono/component/diagnostics_server.h>

void
mono_component_event_pipe_100ns_ticks_start (void);

gint64
mono_component_event_pipe_100ns_ticks_stop (void);

void
mono_components_init (void);

/* Declare each component's getter function here */
static inline
MonoComponentHotReload *
mono_component_hot_reload (void)
{
	extern MonoComponentHotReload *hot_reload;
	return hot_reload;
}

static inline
MonoComponentEventPipe *
mono_component_event_pipe (void)
{
	extern MonoComponentEventPipe *event_pipe;
	return event_pipe;
}

static inline
MonoComponentDiagnosticsServer *
mono_component_diagnostics_server (void)
{
	extern MonoComponentDiagnosticsServer *diagnostics_server;
	return diagnostics_server;
}

#endif/*_MONO_METADATA_COMPONENTS_H*/
