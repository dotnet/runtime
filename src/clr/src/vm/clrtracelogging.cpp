//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

//*****************************************************************************
// clrttracelogging.cpp
// Telemetry Logging for clr.dll
//
//*****************************************************************************

#include "common.h"
#include "clrtracelogging.h"
#include "TraceLoggingProvider.h"
#include "MicrosoftTelemetry.h"

TRACELOGGING_DEFINE_PROVIDER(g_hClrProvider, CLR_PROVIDER_NAME, CLR_PROVIDER_ID, TraceLoggingOptionMicrosoftTelemetry());

// Used for initialization and deconstruction.
static CLRTraceLog::Provider g_clrTraceProvider(g_hClrProvider);

//--- CLRTraceLogProvider

// static
void CLRTraceLog::Logger::LogTargetFrameworkAttribute(LPCWSTR targetFrameworkAttribute, const char * assemblyName)
{
	STANDARD_VM_CONTRACT;
	
	EX_TRY
	{
		TraceLoggingWrite(g_hClrProvider,"CLR.AssemblyInfo",
		TraceLoggingWideString(targetFrameworkAttribute, "TARGET_FRAMEWORK_ATTRIBUTE"),
		TraceLoggingString(assemblyName, "ASSEMBLY_NAME"),
		TraceLoggingKeyword(MICROSOFT_KEYWORD_TELEMETRY));
	}
	EX_CATCH{}
	EX_END_CATCH(SwallowAllExceptions)
	
}
//--- CLRTraceLog 

