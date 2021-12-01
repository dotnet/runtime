// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_METADATA_COMPONENTS_H
#define _MONO_METADATA_COMPONENTS_H

#include <mono/component/component.h>
#include <mono/component/hot_reload.h>
#include <mono/component/event_pipe.h>
#include <mono/component/diagnostics_server.h>
#include <mono/component/debugger.h>

void
mono_component_event_pipe_100ns_ticks_start (void);

gint64
mono_component_event_pipe_100ns_ticks_stop (void);

void
mono_components_init (void);

extern MonoComponentHotReload *mono_component_hot_reload_private_ptr;
extern MonoComponentEventPipe *mono_component_event_pipe_private_ptr;
extern MonoComponentDiagnosticsServer *mono_component_diagnostics_server_private_ptr;
extern MonoComponentDebugger *mono_component_debugger_private_ptr;

/* Declare each component's getter function here */
static inline
MonoComponentHotReload *
mono_component_hot_reload (void)
{
	return mono_component_hot_reload_private_ptr;
}

static inline
MonoComponentEventPipe *
mono_component_event_pipe (void)
{
	return mono_component_event_pipe_private_ptr;
}

static inline
MonoComponentDiagnosticsServer *
mono_component_diagnostics_server (void)
{
	return mono_component_diagnostics_server_private_ptr;
}

static inline
MonoComponentDebugger *
mono_component_debugger (void)
{
	return mono_component_debugger_private_ptr;
}

#endif/*_MONO_METADATA_COMPONENTS_H*/
