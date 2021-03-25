// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include "mono/component/diagnostics_server.h"

static void
diagnostics_server_stub_cleanup (MonoComponent *self);

static void
diagnostics_server_stub_ds_server_init (void);

static void
diagnostics_server_stub_ds_server_shutdown (void);

static void
diagnostics_server_stub_ds_server_pause_for_diagnostics_monitor (void);

static void
diagnostics_server_stub_ds_server_disable (void);

static MonoComponentDiagnosticsServer fn_table = {
	{ &diagnostics_server_stub_cleanup },
	&diagnostics_server_stub_ds_server_init,
	&diagnostics_server_stub_ds_server_shutdown,
	&diagnostics_server_stub_ds_server_pause_for_diagnostics_monitor,
	&diagnostics_server_stub_ds_server_disable
};

#ifdef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_init (void)
{
	return mono_component_diagnostics_server_stub_init ();
}
#endif

MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_stub_init (void)
{
	return &fn_table;
}

static void
diagnostics_server_stub_cleanup (MonoComponent *self)
{
}

static void
diagnostics_server_stub_ds_server_init (void)
{
}

static void
diagnostics_server_stub_ds_server_shutdown (void)
{
}

static void
diagnostics_server_stub_ds_server_pause_for_diagnostics_monitor (void)
{
}

static void
diagnostics_server_stub_ds_server_disable (void)
{
}
