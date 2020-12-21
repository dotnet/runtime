// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DIAGNOSTIC_SERVER_ADAPTER_H__
#define __DIAGNOSTIC_SERVER_ADAPTER_H__

#if defined(FEATURE_PERFTRACING) && !(CROSSGEN_COMPILE)

#ifdef FEATURE_PERFTRACING_C_LIB
#include "ds-server.h"
#else
#include "palclr.h"
#include "diagnosticserver.h"
#endif

class DiagnosticServerAdapter final
{
public:
	static inline bool Initialize()
	{
#ifdef FEATURE_PERFTRACING_C_LIB
		return ds_server_init();
#else
		return DiagnosticServer::Initialize();
#endif
	}

	static inline bool Shutdown()
	{
#ifdef FEATURE_PERFTRACING_C_LIB
		return ds_server_shutdown();
#else
		return DiagnosticServer::Shutdown();
#endif
	}

	NOINLINE static void PauseForDiagnosticsMonitor()
	{
#ifdef FEATURE_PERFTRACING_C_LIB
		return ds_server_pause_for_diagnostics_monitor();
#else
		return DiagnosticServer::PauseForDiagnosticsMonitor();
#endif
	}

	static void ResumeRuntimeStartup()
	{
#ifdef FEATURE_PERFTRACING_C_LIB
		return ds_server_resume_runtime_startup();
#else
		return DiagnosticServer::ResumeRuntimeStartup();
#endif
	}
};

#endif // FEATURE_PERFTRACING && !CROSSGEN_COMPILE

#endif // __DIAGNOSTIC_SERVER_ADAPTER_H__
