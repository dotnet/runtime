// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include "mono/component/event_pipe.h"

static void
event_pipe_stub_cleanup (MonoComponent *self);

static void
event_pipe_stub_ep_init (void);

static void
event_pipe_stub_ep_finish_init (void);

static void
event_pipe_stub_ep_shutdown (void);

static void
event_pipe_stub_ds_server_init (void);

static void
event_pipe_stub_ds_server_shutdown (void);

static void
event_pipe_stub_ds_server_pause_for_diagnostics_monitor (void);

static void
event_pipe_stub_ds_server_disable (void);

static MonoComponentEventPipe fn_table = {
	{ &event_pipe_stub_cleanup },
	&event_pipe_stub_ep_init,
	&event_pipe_stub_ep_finish_init,
	&event_pipe_stub_ep_shutdown
};

#ifdef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentEventpipe *
mono_component_event_pipe_init (void)
{
	return mono_component_event_pipe_stub_init ();
}
#endif

MonoComponentEventPipe *
mono_component_event_pipe_stub_init (void)
{
	return &fn_table;
}

static void
event_pipe_stub_cleanup (MonoComponent *self)
{
}

static void
event_pipe_stub_ep_init (void)
{
}

static void
event_pipe_stub_ep_finish_init (void)
{
}

static void
event_pipe_stub_ep_shutdown (void)
{
}

static void
event_pipe_stub_ds_server_init (void)
{
}

static void
event_pipe_stub_ds_server_shutdown (void)
{
}

static void
event_pipe_stub_ds_server_pause_for_diagnostics_monitor (void)
{
}

static void
event_pipe_stub_ds_server_disable (void)
{
}
