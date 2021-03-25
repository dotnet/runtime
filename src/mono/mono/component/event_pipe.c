// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <mono/component/event_pipe.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-compiler.h>
#include <eventpipe/ep.h>

#ifndef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentEventPipe *
mono_component_event_pipe_init (void);
#endif

static void
event_pipe_cleanup (MonoComponent *self);

static MonoComponentEventPipe fn_table = {
	{ &event_pipe_cleanup },
	&ep_init,
	&ep_finish_init,
	&ep_shutdown
};

static void
event_pipe_cleanup (MonoComponent *self)
{
	return;
}

MonoComponentEventPipe *
mono_component_event_pipe_init (void)
{
	return &fn_table;
}
