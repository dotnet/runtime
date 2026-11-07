// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DIAGNOSTIC_SERVER_ADAPTER_H__
#define __DIAGNOSTIC_SERVER_ADAPTER_H__

#if defined(FEATURE_PERFTRACING)

#include <eventpipe/ds-server.h>

#if defined(TARGET_BROWSER)

class DiagnosticServerAdapter final
{
public:
    static bool Initialize();
    static bool Shutdown();
    static void PauseForDiagnosticsMonitor();
    static void ResumeRuntimeStartup();
    static bool IsPausedInRuntimeStartup();
};

#else // TARGET_BROWSER

class DiagnosticServerAdapter final
{
public:
	static inline bool Initialize()
	{
		return ds_server_init();
	}

	static inline bool Shutdown()
	{
		return ds_server_shutdown();
	}

	NOINLINE static void PauseForDiagnosticsMonitor()
	{
		return ds_server_pause_for_diagnostics_monitor();
	}

	static void ResumeRuntimeStartup()
	{
		return ds_server_resume_runtime_startup();
	}

	static bool IsPausedInRuntimeStartup()
	{
		return ds_server_is_paused_in_startup();
	}
};

#endif // TARGET_BROWSER

#endif // FEATURE_PERFTRACING

#endif // __DIAGNOSTIC_SERVER_ADAPTER_H__
