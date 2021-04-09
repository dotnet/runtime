// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_DIAGNOSTICS_SERVER_H
#define _MONO_COMPONENT_DIAGNOSTICS_SERVER_H

#include "mono/component/component.h"
#include "mono/utils/mono-compiler.h"

#ifndef ENABLE_PERFTRACING
#define ENABLE_PERFTRACING
#endif

#include <eventpipe/ep-ipc-pal-types-forward.h>
#include <eventpipe/ep-types-forward.h>

typedef struct _MonoComponentDiagnosticsServer {
	MonoComponent component;
	bool (*init) (void);
	bool (*shutdown) (void);
	void (*pause_for_diagnostics_monitor) (void);
	void (*disable) (void);
} MonoComponentDiagnosticsServer;

#ifdef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_init (void);
#endif

#endif /*_MONO_COMPONENT_DIAGNOSTICS_SERVER_H*/
