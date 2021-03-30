// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_COMPONENT_DIAGNOSTICS_SERVER_H
#define _MONO_COMPONENT_DIAGNOSTICS_SERVER_H

#ifdef ENABLE_PERFTRACING
#include "mono/component/component.h"
#include "mono/utils/mono-compiler.h"

typedef struct _MonoComponentDiagnosticsServer {
	MonoComponent component;
	void (*init) (void);
	void (*shutdown) (void);
	void (*pause_for_diagnostics_monitor) (void);
	void (*disable) (void);
} MonoComponentDiagnosticsServer;

#ifdef STATIC_COMPONENTS
MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentDiagnosticsServer *
mono_component_diagnostics_server_init (void);
#endif

#endif /* ENABLE_PERFTRACING */
#endif /*_MONO_COMPONENT_DIAGNOSTICS_SERVER_H*/
