// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>
#include <mono/component/diagnostics_server.h>
#include <mono/utils/mono-publib.h>
#include <mono/utils/mono-compiler.h>
#include <eventpipe/ds-server.h>

#ifndef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_init (void);
#endif

static void
diagnostics_server_cleanup (MonoComponent *self);

static MonoComponentDiagnosticsServer fn_table = {
	{ &diagnostics_server_cleanup },
	&ds_server_init,
	&ds_server_shutdown,
	&ds_server_pause_for_diagnostics_monitor,
	&ds_server_disable
};

static void
diagnostics_server_cleanup (MonoComponent *self)
{
	return;
}

MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_init (void)
{
	return &fn_table;
}
