// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// 
// --------------------------------------------------------------------------------------------------
// PerfDefaults.h
// 

// 
// Implementation of the "PerformanceScenario" config option, which defines a workload-specific 
// set of performance defaults. 
//
// The motivation is that every release we work closely with a partner to understand the performance
// of their scenario and provide CLR behavior to improve it.  Sometimes we are able to give them
// an entirely different build of the CLR (because it happens to be on a different architecture, or
// it needs to have a small size, etc), but not always.  Sometimes we are able to change the 
// runtime to have the new behavior by default, but not always.  When neither applies, we fall to
// the ugly option of a set of one-off config values for the scenario (or machine administrators)
// to opt in.  This then creates the problem of a huge number of generic knobs that any application
// can use in any combination.  It also means that as we add additional improvements for the same
// scenario, we need to ask developers or machine administrators to keep up with the growing list.
//
// Our solution is to opt-in to a set of performance settings all at once and based on the workload.
// When a recognized 'PerformanceScenario' is specified in a config file, we want the runtime to
// operate with a defined set of performance settings.  This set may evolve over time to
// automatically provide additional behavior to the same workload, just as we would for runtime
// defaults.  Note however that a key design point is that in any conflict between these performance
// settings and one that is explicitly set by the user, the explicit user setting wins.  Hence the
// name PerformanceDefaults -- it is as if we had created a separate build of the runtime for a
// particular scenario, changing only the default performance behavior, and continued to let people
// opt in or opt-out individually to those settings that are important and general-purpose enough
// to have an individual setting.
//
// To add a scenario,
//     Update the PerformanceScenario enum and InitializeForScenario so that it is recognized
//
// To add an overridden host STARTUP_FLAG,
//     Update the switch statement in GetModifiedStartupFlags; see note about IsConfigOptionSpecified
//
// To add an overridden CLRConfig default value,
//     Mark it with LookupOption CLRConfig::MayHavePerformanceDefault in clrconfigvalues.h
//     Update the switch statement in LookupConfigValue
//     If a new CLRConfig option, decide if it needs to be a general-purpose switch (hopefully no)
//         and if not create it as INTERNAL_
//
// --------------------------------------------------------------------------------------------------


#ifndef __PerfDefaults_h__
#define __PerfDefaults_h__

class PerformanceDefaults
{
public:

    static void InitializeForScenario(__in_opt LPWSTR scenarioName);

    // Called at runtime startup to allow host-specified STARTUP_FLAGS to be overridden when running
    // under a scenario.
    //
    // Note that this does not follow the model of "only override if not specified."  By the nature
    // of the startup flags being bits, they are all specified (as on or off).  We have no way of
    // knowing which the host actually care about and which were left as the runtime default
    // out of lack of concern.  However we found that so far the specified startup flags are hard
    // coded by the host (no way for the user to override at run time) and so we can build in
    // to the set of defaults which the host allows us to override.
    //
    // In the future, if a host needs us to be able to override startup flags conditionally, the
    // solution would be a new hosting API ICLRRuntimeInfo::SetOverridableStartupFlags.
    static STARTUP_FLAGS GetModifiedStartupFlags(STARTUP_FLAGS originalFlags);

    // Called by the CLRConfig system whenever a 'MayHavePerformanceDefault' config option is about 
    // to resolve to its runtime default (i.e. none of the corresponding environment variable, 
    // registry, or config file values were specified).
    static BOOL LookupConfigValue(LPCWSTR name, DWORD *pValue);

private:

    enum PerformanceScenario
    {
        Uninitialized,
        None,
        HighDensityWebHosting,
    };

    static PerformanceScenario s_Scenario;

};

#endif //__PerfDefaults_h__
