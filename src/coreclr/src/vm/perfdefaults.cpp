// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*************************************************************************************************
// PerfDefaults.cpp
// 

// 
// Implementation of the "PerformanceScenario" config option, which defines a workload-specific 
// set of performance defaults.  The key point of the code below is that it be clear exactly what 
// gets enabled for each scenario -- see the switch statements
//
//     for host STARTUP_FLAGS, in GetModifiedStartupFlags
//     for environment, registry, or config CLRConfig values, in LookupConfigValue
//     for other global initialization, in InitializeForScenario
//
//*************************************************************************************************

#include "common.h"
#include "perfdefaults.h"

// Useful to the readability of lists of settings below
#define MATCHES(a,b) (SString::_wcsicmp((a),(b)) == 0)

// The scenario we have been asked to run under
PerformanceDefaults::PerformanceScenario PerformanceDefaults::s_Scenario = Uninitialized;

// See use in code:PerformanceDefaults:InitializeForScenario
extern int32_t g_bLowMemoryFromHost;


//
// Initialize our system to provide performance defaults for a given scenario.
// If the scenario name is not recognized, intentionally ignore it and operate as if not specified.
//
 void PerformanceDefaults::InitializeForScenario(__in_opt LPWSTR scenarioName)
{
    LIMITED_METHOD_CONTRACT;

    // First convert the scenario name to the corresponding enum value
    s_Scenario = None;
    if (scenarioName != NULL)
    {
        if (MATCHES(scenarioName, W("HighDensityWebHosting"))) s_Scenario = HighDensityWebHosting;
    }

    // Next do any scenario-specific initialization
    switch (s_Scenario)
    {
        case None:
            break;

        case HighDensityWebHosting:
            // Tell the hosting API that we want the GC to operate as if the machine is under memory pressure.
            // This is a workaround because we do not want to expose "force low memory mode" as either a CLR
            // config option or a host startup flag (the two types of knob that PerformanceDefaults can alter) and
            // ASP.Net has not yet become a memory host.  When they do, this can be removed and they can
            // make an initial call to ICLRMemoryNotificationCallback:OnMemoryNotification(eMemoryAvailableLow).
            //
            // Note that in order for ASP.Net to become a memory host the CLR will need to support profiler attach
            // in that condition.
            g_bLowMemoryFromHost = 1;
            break;

        case Uninitialized:
            // Unreachable, but make GCC happy
            break;
    }

    // Finally register our lookup function with the CLRConfig system so we get called when a 'MayHavePerformanceDefault' 
    // config option is about to resolve to its runtime default
    if (s_Scenario != None)
    {
        CLRConfig::RegisterGetPerformanceDefaultValueCallback(&LookupConfigValue);
    }
}


//
// Called at runtime startup to allow host-specified STARTUP_FLAGS to be overridden when running
// under a scenario.
//
// Note that we are comfortable overriding the values the host has asked us to use (see
// file:PerfDefaults.h), but we never want to override a value specified by a user to CLRConfig.
// This comes up here because there are settings that are configurable through both systems.
// So where an option has both STARTUP_FLAG A and CLRConfig value B, first check whether B has
// been specified before altering the value of A.  This way, we maintain complete compatibility
// with whatever the interaction of A and B has produced in the past.
//
STARTUP_FLAGS PerformanceDefaults::GetModifiedStartupFlags(STARTUP_FLAGS originalFlags)
{
    LIMITED_METHOD_CONTRACT;

    DWORD newFlags = (DWORD)originalFlags;

    switch (s_Scenario)
    {
        case None:
            break;

        case HighDensityWebHosting:
            if (!CLRConfig::IsConfigOptionSpecified(W("gcServer"))) newFlags &= ~STARTUP_SERVER_GC;
            if (!CLRConfig::IsConfigOptionSpecified(W("gcConcurrent"))) newFlags &= ~STARTUP_CONCURRENT_GC;
            if (!CLRConfig::IsConfigOptionSpecified(W("gcTrimCommitOnLowMemory"))) newFlags |= STARTUP_TRIM_GC_COMMIT;

            break;

        case Uninitialized:
            // Check that no request for startup flags happens before our code has been initialized 
            // and given a chance to modify them.
            _ASSERTE(!"PerformanceDefaults::InitializeForScenario should have already been called");
            break;
    }

    return (STARTUP_FLAGS)newFlags;
}


//
// Called by the CLRConfig system whenever a 'MayHavePerformanceDefault' config option is about 
// to resolve to its runtime default (i.e. none of the corresponding environment variable, registry, or
// config file values were specified).
//
BOOL PerformanceDefaults::LookupConfigValue(LPCWSTR name, DWORD *pValue)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(pValue != NULL);

    switch (s_Scenario)
    {
        case None:
            return FALSE;
    
        case HighDensityWebHosting:
            if (MATCHES(name, W("shadowCopyVerifyByTimestamp"))) { *pValue = 1; return TRUE; }
            return FALSE;

        case Uninitialized:
            // Check that no request for a MayHavePerformanceDefault CLRConfig option happens 
            // before our code has been initialized and given a chance to provide a default value.
            _ASSERTE(!"PerformanceDefaults::InitializeForScenario should have already been called");
            break;
    }

    return FALSE;
}

