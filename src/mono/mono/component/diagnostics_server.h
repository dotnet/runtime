// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_DIAGNOSTICS_SERVER_H
#define _MONO_COMPONENT_DIAGNOSTICS_SERVER_H

#include "mono/component/component.h"

typedef struct _MonoComponentDiagnosticsServer {
	MonoComponent component;
	void (*ds_server_init) (void);
	void (*ds_server_shutdown) (void);
	void (*ds_server_pause_for_diagnostics_monitor) (void);
	void (*ds_server_disable) (void);
} MonoComponentDiagnosticsServer;

#ifdef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_init (void);
#endif

#endif /*_MONO_COMPONENT_DIAGNOSTICS_SERVER_H*/
