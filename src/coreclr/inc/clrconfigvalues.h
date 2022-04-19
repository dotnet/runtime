// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//*****************************************************************************
// CLRConfigValues.h
//

//
// Unified method of accessing configuration values from environment variables,
// registry and config file.
//
// Given any config knob below that looks like this example:
//    RETAIL_CONFIG_DWORD_INFO(INTERNAL_LogEnable, W("LogEnable"), 0, "Turns on the traditional CLR log.")
//                                                                  ---------
//                                                                     |
//                                                 --------------------
//                                                 |
//                                                 V
// You can set an environment variable COMPlus_LogEnable=1 to enable it.
//
// See below for more details
//
//*****************************************************************************

// IMPORTANT: Before adding a new config value, please read up on naming conventions (see
// code:#NamingConventions)
//
// ==========
// CONTENTS
// ==========
// * How to define config values (see code:#Define)
// * How to access config values (see code:#Access)
// * Naming conventions (see code:#NamingConventions)
//
//
// =====================================
// #Define - Use one of the following macros to define config values. (See code:#DWORDs and code:#Strings)
// =====================================
//
// By default, all macros are DEBUG ONLY. Add the "RETAIL_" prefix to make the config value available in retail builds.
//
// #DWORDs:
// --------------------------------------------------------------------------
// CONFIG_DWORD_INFO(symbol, name, defaultValue, description)
// --------------------------------------------------------------------------
// Use this macro to define a basic DWORD value. CLRConfig will look in environment variables (adding
// COMPlus_ to the name) for this value. To customize
// where CLRConfig looks, use the extended version of the macro below. IMPORTANT: please follow the
// code:#NamingConventions for the symbol and the name!
//
// Example: CONFIG_DWORD_INFO(INTERNAL_AllowCrossModuleInlining, W("AllowCrossModuleInlining"), 0, "")
//
// --------------------------------------------------------------------------
// CONFIG_DWORD_INFO_EX(symbol, name, defaultValue, description, lookupOptions)
// --------------------------------------------------------------------------
// To customize where CLRConfig looks to get a DWORD, use the extended (_EX) version of the macro. For a list
// of options and their descriptions, see code:CLRConfig.LookupOptions
//
// Example: CONFIG_DWORD_INFO_EX(INTERNAL_EnableInternetHREFexes, W("EnableInternetHREFexes"), 0, "",
// (CLRConfig::LookupOptions) (CLRConfig::LookupOptions::DontPrependPrefix))
//
// #Strings:
// --------------------------------------------------------------------------
// CONFIG_STRING_INFO(symbol, name, description)
// --------------------------------------------------------------------------
// Defines a string value. Same rules apply as DWORDs.
//
// --------------------------------------------------------------------------
// CONFIG_STRING_INFO_EX(symbol, name, description, lookupOptions)
// --------------------------------------------------------------------------
// Extended version of the String macro. Again, similar to the DWORD extended macro.
//
//
// ===============================================================
// #Access - Use the following overloaded method to access config values.
// ===============================================================
// From anywhere, use CLRConfig::GetConfigValue(CLRConfig::<symbol>) to access any value defined in this
// file.
//
//
// ===============================================================
// #NamingConventions
// ===============================================================
// ----------------
// #Symbol - used to access values from the source. (using CLRConfig::<symbol>)
// ----------------
// The symbol for each config value is named as such:
// ### <Class>_<feature area>_<name> ###
//
// <Class> indicates which of the following buckets the value is in:
// * INTERNAL ? this value is for internal (CLR team) use only
// * UNSUPPORTED ? this value is available to partners/developers, but is not officially supported
// * EXTERNAL ? this value is available for anyone to use and is publicly documented
//
// Examples:
// * INTERNAL_Security_FullAccessChecks
// * UNSUPPORTED_Security_DisableTransparency
// * EXTERNAL_Security_LegacyHMACMode
//
// ----------------
// #Name - the name of the registry value or environment variable that CLRConfig looks up.
// ----------------
// The name of each value is the same as the symbol, with one exception. Names of external values do NOT
// contain the EXTERNAL prefix.
//
// For compatibility reasons, current names do not follow the convention.
//
// Examples:
// * W("INTERNAL_Security_FullAccessChecks")
// * W("UNSUPPORTED_Security_DisableTransparency")
// * W("Security_LegacyHMACMode") <---------------------- (No EXTERNAL prefix)

///
/// AppDomain
///
CONFIG_DWORD_INFO(INTERNAL_EnableFullDebug, W("EnableFullDebug"), 0, "Heavy-weight checking for AD boundary violations (AD leaks)")

///
/// Jit Pitching
///
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchEnabled, W("JitPitchEnabled"), (DWORD)0, "Set it to 1 to enable Jit Pitching")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchMemThreshold, W("JitPitchMemThreshold"), (DWORD)0, "Do Jit Pitching when code heap usage is larger than this (in bytes)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchMethodSizeThreshold, W("JitPitchMethodSizeThreshold"), (DWORD)0, "Do Jit Pitching for methods whose native code size larger than this (in bytes)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchTimeInterval, W("JitPitchTimeInterval"), (DWORD)0, "Time interval between Jit Pitchings in ms")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchPrintStat, W("JitPitchPrintStat"), (DWORD)0, "Print statistics about Jit Pitching")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchMinVal, W("JitPitchMinVal"), (DWORD)0, "Do Jit Pitching if the value of the inner counter greater than this value (for debugging purpose only)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchMaxVal, W("JitPitchMaxVal"), (DWORD)0xffffffff, "Do Jit Pitching the value of the inner counter less then this value (for debuggin purpose only)")

///
/// Assembly Loader
///
CONFIG_DWORD_INFO(INTERNAL_GetAssemblyIfLoadedIgnoreRidMap, W("GetAssemblyIfLoadedIgnoreRidMap"), 0, "Used to force loader to ignore assemblies cached in the rid-map")

///
/// Conditional breakpoints
///
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BreakOnBadExit, W("BreakOnBadExit"), 0, "")
CONFIG_STRING_INFO(INTERNAL_BreakOnClassBuild, W("BreakOnClassBuild"), "Very useful for debugging class layout code.")
CONFIG_STRING_INFO(INTERNAL_BreakOnClassLoad, W("BreakOnClassLoad"), "Very useful for debugging class loading code.")
CONFIG_STRING_INFO(INTERNAL_BreakOnComToClrNativeInfoInit, W("BreakOnComToClrNativeInfoInit"), "Throws an assert when native information about a COM -> CLR call are about to be gathered.")
CONFIG_DWORD_INFO(INTERNAL_BreakOnDebugBreak, W("BreakOnDebugBreak"), 0, "Allows an assert in debug builds when a user break is hit")
CONFIG_DWORD_INFO(INTERNAL_BreakOnDILoad, W("BreakOnDILoad"), 0, "Allows an assert when the DI is loaded")
CONFIG_DWORD_INFO(INTERNAL_BreakOnDumpToken, W("BreakOnDumpToken"), 0xffffffff, "Breaks when using internal logging on a particular token value.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BreakOnEELoad, W("BreakOnEELoad"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_BreakOnEEShutdown, W("BreakOnEEShutdown"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_BreakOnExceptionInGetThrowable, W("BreakOnExceptionInGetThrowable"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_BreakOnFindMethod, W("BreakOnFindMethod"), 0, "Breaks in findMethodInternal when it searches for the specified token.")
CONFIG_DWORD_INFO(INTERNAL_BreakOnFirstPass, W("BreakOnFirstPass"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_BreakOnHR, W("BreakOnHR"), 0, "Debug.cpp, IfFailxxx use this macro to stop if hr matches ")
CONFIG_STRING_INFO(INTERNAL_BreakOnInstantiation, W("BreakOnInstantiation"), "Very useful for debugging generic class instantiation.")
CONFIG_STRING_INFO(INTERNAL_BreakOnInteropStubSetup, W("BreakOnInteropStubSetup"), "Throws an assert when marshaling stub for the given method is about to be built.")
CONFIG_STRING_INFO(INTERNAL_BreakOnInteropVTableBuild, W("BreakOnInteropVTableBuild"), "Specifies a type name for which an assert should be thrown when building interop v-table.")
CONFIG_STRING_INFO(INTERNAL_BreakOnMethodName, W("BreakOnMethodName"), "Very useful for debugging method override placement code.")
CONFIG_DWORD_INFO(INTERNAL_BreakOnNotify, W("BreakOnNotify"), 0, "")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_BreakOnRetailAssert, W("BreakOnRetailAssert"), 0, "Used for debugging \"retail\" asserts (fatal errors)")
CONFIG_DWORD_INFO(INTERNAL_BreakOnSecondPass, W("BreakOnSecondPass"), 0, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BreakOnSO, W("BreakOnSO"), 0, "")
CONFIG_STRING_INFO(INTERNAL_BreakOnStructMarshalSetup, W("BreakOnStructMarshalSetup"), "Throws an assert when field marshalers for the given type with layout are about to be created.")
CONFIG_DWORD_INFO(INTERNAL_BreakOnUEF, W("BreakOnUEF"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_BreakOnUncaughtException, W("BreakOnUncaughtException"), 0, "")

///
/// Debugger
///
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableDiagnostics, W("EnableDiagnostics"), 1, "Allows the debugger, profiler, and EventPipe diagnostics to be disabled")
CONFIG_DWORD_INFO(INTERNAL_D__FCE, W("D::FCE"), 0, "Allows an assert when crawling the managed stack for an exception handler")
CONFIG_DWORD_INFO(INTERNAL_DbgBreakIfLocksUnavailable, W("DbgBreakIfLocksUnavailable"), 0, "Allows an assert when the debugger can't take a lock ")
CONFIG_DWORD_INFO(INTERNAL_DbgBreakOnErr, W("DbgBreakOnErr"), 0, "Allows an assert when we get a failing hresult")
CONFIG_DWORD_INFO(INTERNAL_DbgBreakOnMapPatchToDJI, W("DbgBreakOnMapPatchToDJI"), 0, "Allows an assert when mapping a patch to an address")
CONFIG_DWORD_INFO(INTERNAL_DbgBreakOnRawInt3, W("DbgBreakOnRawInt3"), 0, "Allows an assert for test coverage for debug break or other int3 breaks")
CONFIG_DWORD_INFO(INTERNAL_DbgBreakOnSendBreakpoint, W("DbgBreakOnSendBreakpoint"), 0, "Allows an assert when sending a breakpoint to the right side")
CONFIG_DWORD_INFO(INTERNAL_DbgBreakOnSetIP, W("DbgBreakOnSetIP"), 0, "Allows an assert when setting the IP")
CONFIG_DWORD_INFO(INTERNAL_DbgCheckInt3, W("DbgCheckInt3"), 0, "Asserts if the debugger explicitly writes int3 instead of calling SetUnmanagedBreakpoint")
CONFIG_DWORD_INFO(INTERNAL_DbgDACAssertOnMismatch, W("DbgDACAssertOnMismatch"), 0, "Allows an assert when the mscordacwks and mscorwks dll versions don't match")
CONFIG_DWORD_INFO(INTERNAL_DbgDACEnableAssert, W("DbgDACEnableAssert"), 0, "Enables extra validity checking in DAC - assumes target isn't corrupt")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DbgDACSkipVerifyDlls, W("DbgDACSkipVerifyDlls"), 0, "Allows disabling the check to ensure mscordacwks and mscorwks dll versions match")
CONFIG_DWORD_INFO(INTERNAL_DbgDelayHelper, W("DbgDelayHelper"), 0, "Varies the wait in the helper thread startup for testing race between threads")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DbgDisableDynamicSymsCompat, W("DbgDisableDynamicSymsCompat"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_DbgDisableTargetConsistencyAsserts, W("DbgDisableTargetConsistencyAsserts"), 0, "Allows explicitly testing with corrupt targets")
CONFIG_DWORD_INFO(INTERNAL_DbgExtraThreads, W("DbgExtraThreads"), 0, "Allows extra unmanaged threads to run and throw debug events for stress testing")
CONFIG_DWORD_INFO(INTERNAL_DbgExtraThreadsCantStop, W("DbgExtraThreadsCantStop"), 0, "Allows extra unmanaged threads in can't stop region to run and throw debug events for stress testing")
CONFIG_DWORD_INFO(INTERNAL_DbgExtraThreadsIB, W("DbgExtraThreadsIB"), 0, "Allows extra in-band unmanaged threads to run and throw debug events for stress testing")
CONFIG_DWORD_INFO(INTERNAL_DbgExtraThreadsOOB, W("DbgExtraThreadsOOB"), 0, "Allows extra out of band unmanaged threads to run and throw debug events for stress testing")
CONFIG_DWORD_INFO(INTERNAL_DbgFaultInHandleIPCEvent, W("DbgFaultInHandleIPCEvent"), 0, "Allows testing the unhandled event filter")
CONFIG_DWORD_INFO(INTERNAL_DbgInjectFEE, W("DbgInjectFEE"), 0, "Allows injecting a fatal execution error for testing Watson")
CONFIG_DWORD_INFO(INTERNAL_DbgLeakCheck, W("DbgLeakCheck"), 0, "Allows checking for leaked Cordb objects")
CONFIG_DWORD_INFO(INTERNAL_DbgNo2ndChance, W("DbgNo2ndChance"), 0, "Allows breaking on (and catching bogus) 2nd chance exceptions")
CONFIG_DWORD_INFO(INTERNAL_DbgNoDebugger, W("DbgNoDebugger"), 0, "Allows breaking if we don't want to lazily initialize the debugger")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_DbgNoForceContinue, W("DbgNoForceContinue"), 1, "Used to force a continue on longhorn")
CONFIG_DWORD_INFO(INTERNAL_DbgNoOpenMDByFile, W("DbgNoOpenMDByFile"), 0, "Allows opening MD by memory for perf testing")
CONFIG_DWORD_INFO(INTERNAL_DbgOOBinFEEE, W("DbgOOBinFEEE"), 0, "Allows forcing oob breakpoints when a fatal error occurs")
CONFIG_DWORD_INFO(INTERNAL_DbgPingInterop, W("DbgPingInterop"), 0, "Allows checking for deadlocks in interop debugging")
CONFIG_DWORD_INFO(INTERNAL_DbgRace, W("DbgRace"), 0, "Allows pausing for native debug events to get hijicked")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_DbgRedirect, W("DbgRedirect"), 0, "Allows for redirecting the event pipeline")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_DbgRedirectApplication, W("DbgRedirectApplication"), "Specifies the auxiliary debugger application to launch.")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_DbgRedirectAttachCmd, W("DbgRedirectAttachCmd"), "Specifies command parameters for attaching the auxiliary debugger.")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_DbgRedirectCommonCmd, W("DbgRedirectCommonCmd"), "Specifies a command line format string for the auxiliary debugger.")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_DbgRedirectCreateCmd, W("DbgRedirectCreateCmd"), "Specifies command parameters when creating the auxiliary debugger.")
CONFIG_DWORD_INFO(INTERNAL_DbgShortcutCanary, W("DbgShortcutCanary"), 0, "Allows a way to force canary to fail to be able to test failure paths")
CONFIG_DWORD_INFO(INTERNAL_DbgSkipMEOnStep, W("DbgSkipMEOnStep"), 0, "Turns off MethodEnter checks")
CONFIG_DWORD_INFO(INTERNAL_DbgSkipVerCheck, W("DbgSkipVerCheck"), 0, "Allows different RS and LS versions (for servicing work)")
CONFIG_DWORD_INFO(INTERNAL_DbgTC, W("DbgTC"), 0, "Allows checking boundary compression for offset mappings")
CONFIG_DWORD_INFO(INTERNAL_DbgTransportFaultInject, W("DbgTransportFaultInject"), 0, "Allows injecting a fault for testing the debug transport")
CONFIG_DWORD_INFO(INTERNAL_DbgTransportLog, W("DbgTransportLog"), 0 /* LE_None */, "Turns on logging for the debug transport")
CONFIG_DWORD_INFO(INTERNAL_DbgTransportLogClass, W("DbgTransportLogClass"), (DWORD)-1 /* LC_All */, "Mask to control what is logged in DbgTransportLog")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_DbgTransportProxyAddress, W("DbgTransportProxyAddress"), "Allows specifying the transport proxy address")
CONFIG_DWORD_INFO(INTERNAL_DbgTrapOnSkip, W("DbgTrapOnSkip"), 0, "Allows breaking when we skip a breakpoint")
CONFIG_DWORD_INFO(INTERNAL_DbgWaitTimeout, W("DbgWaitTimeout"), 1, "Specifies the timeout value for waits")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_DbgWFDETimeout, W("DbgWFDETimeout"), 25, "Specifies the timeout value for wait when waiting for a debug event")
CONFIG_DWORD_INFO(INTERNAL_RaiseExceptionOnAssert, W("RaiseExceptionOnAssert"), 0, "Raise a first chance (if set to 1) or second chance (if set to 2) exception on asserts.")
CONFIG_DWORD_INFO(INTERNAL_DebugBreakOnAssert, W("DebugBreakOnAssert"), 0, "If DACCESS_COMPILE is defined, break on asserts.")
CONFIG_DWORD_INFO(INTERNAL_DebugBreakOnVerificationFailure, W("DebugBreakOnVerificationFailure"), 0, "Halts the jit on verification failure")
CONFIG_STRING_INFO(INTERNAL_DebuggerBreakPoint, W("DebuggerBreakPoint"), "Allows counting various debug events")
CONFIG_STRING_INFO(INTERNAL_DebugVerify, W("DebugVerify"), "Control for tracing in peverify")
CONFIG_DWORD_INFO(INTERNAL_EncApplyChanges, W("EncApplyChanges"), 0, "Allows breaking when ApplyEditAndContinue is called")
CONFIG_DWORD_INFO(INTERNAL_EnCBreakOnRemapComplete, W("EnCBreakOnRemapComplete"), 0, "Allows breaking after N RemapCompletes")
CONFIG_DWORD_INFO(INTERNAL_EnCBreakOnRemapOpportunity, W("EnCBreakOnRemapOpportunity"), 0, "Allows breaking after N RemapOpportunities")
CONFIG_DWORD_INFO(INTERNAL_EncDumpApplyChanges, W("EncDumpApplyChanges"), 0, "Allows dumping edits in delta metadata and il files")
CONFIG_DWORD_INFO(INTERNAL_EncFixupFieldBreak, W("EncFixupFieldBreak"), 0, "Unlikely that this is used anymore.")
CONFIG_DWORD_INFO(INTERNAL_EncJitUpdatedFunction, W("EncJitUpdatedFunction"), 0, "Allows breaking when an updated function is jitted")
CONFIG_DWORD_INFO(INTERNAL_EnCResolveField, W("EnCResolveField"), 0, "Allows breaking when computing the address of an EnC-added field")
CONFIG_DWORD_INFO(INTERNAL_EncResumeInUpdatedFunction, W("EncResumeInUpdatedFunction"), 0, "Allows breaking when execution resumes in a new EnC version of a function")
CONFIG_DWORD_INFO(INTERNAL_DbgAssertOnDebuggeeDebugBreak, W("DbgAssertOnDebuggeeDebugBreak"), 0, "If non-zero causes the managed-only debugger to assert on unhandled breakpoints in the debuggee")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_DbgDontResumeThreadsOnUnhandledException, W("UNSUPPORTED_DbgDontResumeThreadsOnUnhandledException"), 0, "If non-zero, then don't try to unsuspend threads after continuing a 2nd-chance native exception")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_DbgSkipStackCheck, W("DbgSkipStackCheck"), 0, "Skip the stack pointer check during stackwalking")
#ifdef DACCESS_COMPILE
CONFIG_DWORD_INFO(INTERNAL_DumpGeneration_IntentionallyCorruptDataFromTarget, W("IntentionallyCorruptDataFromTarget"), 0, "Intentionally fakes bad data retrieved from target to try and break dump generation.")
#endif
// Note that Debugging_RequiredVersion is sometimes an 'INTERNAL' knob and sometimes an 'UNSUPPORTED' knob, but we don't change it's name.
CONFIG_DWORD_INFO(UNSUPPORTED_Debugging_RequiredVersion, W("UNSUPPORTED_Debugging_RequiredVersion"), 0, "The lowest ICorDebug version we should attempt to emulate, or 0 for default policy.  Use 2 for CLRv2, 4 for CLRv4, etc.")

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MiniMdBufferCapacity, W("MiniMdBufferCapacity"), 64 * 1024, "The max size of the buffer to store mini metadata information for triage- and mini-dumps.")
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

CONFIG_DWORD_INFO(INTERNAL_DbgNativeCodeBpBindsAcrossVersions, W("DbgNativeCodeBpBindsAcrossVersions"), 0, "If non-zero causes native breakpoints at offset 0 to bind in all tiered compilation versions of the given method")

///
/// Diagnostics (internal general-purpose)
///
CONFIG_DWORD_INFO(INTERNAL_ConditionalContracts, W("ConditionalContracts"), 0, "If ENABLE_CONTRACTS_IMPL is defined, sets whether contracts are conditional. (?)")
CONFIG_DWORD_INFO(INTERNAL_ConsistencyCheck, W("ConsistencyCheck"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_ContinueOnAssert, W("ContinueOnAssert"), 0, "If set, doesn't break on asserts.")
CONFIG_DWORD_INFO(INTERNAL_InjectFatalError, W("InjectFatalError"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_InjectFault, W("InjectFault"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_SuppressChecks, W("SuppressChecks"),0,  "")
#ifdef FEATURE_EH_FUNCLETS
CONFIG_DWORD_INFO(INTERNAL_SuppressLockViolationsOnReentryFromOS, W("SuppressLockViolationsOnReentryFromOS"), 0, "64 bit OOM tests re-enter the CLR via RtlVirtualUnwind.  This indicates whether to suppress resulting locking violations.")
#endif // FEATURE_EH_FUNCLETS

///
/// Exception Handling
///
CONFIG_DWORD_INFO(INTERNAL_AssertOnFailFast, W("AssertOnFailFast"), 1, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_legacyCorruptedStateExceptionsPolicy, W("legacyCorruptedStateExceptionsPolicy"), 0, "Enabled Pre-V4 CSE behavior")
CONFIG_DWORD_INFO(INTERNAL_SuppressLostExceptionTypeAssert, W("SuppressLostExceptionTypeAssert"), 0, "")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_UseEntryPointFilter, W("UseEntryPointFilter"), 0, "")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_Corhost_Swallow_Uncaught_Exceptions, W("Corhost_Swallow_Uncaught_Exceptions"), 0, "")

///
/// Garbage collector
///
CONFIG_DWORD_INFO(INTERNAL_FastGCCheckStack, W("FastGCCheckStack"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_FastGCStress, W("FastGCStress"), 0, "Reduce the number of GCs done by enabling GCStress")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCBreakOnOOM, W("GCBreakOnOOM"), 0, "Does a DebugBreak at the soonest time we detect an OOM")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcConcurrent, W("gcConcurrent"), (DWORD)-1, "Enables/Disables concurrent GC")

#ifdef FEATURE_CONSERVATIVE_GC
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcConservative, W("gcConservative"), 0, "Enables/Disables conservative GC")
#endif
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcServer, W("gcServer"), 0, "Enables server GC")
CONFIG_STRING_INFO(INTERNAL_GcCoverage, W("GcCoverage"), "Specify a method or regular expression of method names to run with GCStress")
CONFIG_STRING_INFO(INTERNAL_SkipGCCoverage, W("SkipGcCoverage"), "Specify a list of assembly names to skip with GC Coverage")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StatsUpdatePeriod, W("StatsUpdatePeriod"), 60, "Specifies the interval, in seconds, at which to update the statistics")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCRetainVM, W("GCRetainVM"), 0, "When set we put the segments that should be deleted on a standby list (instead of releasing them back to the OS) which will be considered to satisfy new segment requests (note that the same thing can be specified via API which is the supported way)")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCLOHThreshold, W("GCLOHThreshold"), 0, "Specifies the size that will make objects go on LOH")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_gcAllowVeryLargeObjects, W("gcAllowVeryLargeObjects"), 1, "Allow allocation of 2GB+ objects on GC heap")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCStress, W("GCStress"), 0, "Trigger GCs at regular intervals")
CONFIG_DWORD_INFO(INTERNAL_GcStressOnDirectCalls, W("GcStressOnDirectCalls"), 0, "Whether to trigger a GC on direct calls")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_HeapVerify, W("HeapVerify"), 0, "When set verifies the integrity of the managed heap on entry and exit of each GC")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCNumaAware, W("GCNumaAware"), 1, "Specifies if to enable GC NUMA aware")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCCpuGroup, W("GCCpuGroup"), 0, "Specifies if to enable GC to support CPU groups")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_GCName, W("GCName"), "")
/**
 * This flag allows us to force the runtime to use global allocation context on Windows x86/amd64 instead of thread allocation context just for testing purpose.
 * The flag is unsafe for a subtle reason. Although the access to the g_global_alloc_context is protected under a lock. The implementation of
 * that lock in the JIT helpers are not multi-core safe (in particular, it used and inc instruction without using the LOCK prefix). This is
 * only useful for ad-hoc testing.
 */
CONFIG_DWORD_INFO(INTERNAL_GCUseGlobalAllocationContext, W("GCUseGlobalAllocationContext"), 0, "Force using the global allocation context for testing only")

///
/// JIT
///
CONFIG_DWORD_INFO(INTERNAL_JitBreakEmit, W("JitBreakEmit"), (DWORD)-1, "")
CONFIG_DWORD_INFO(INTERNAL_JitDebuggable, W("JitDebuggable"), 0, "")
#if !defined(DEBUG) && !defined(_DEBUG)
#define INTERNAL_JitEnableNoWayAssert_Default 0
#else
#define INTERNAL_JitEnableNoWayAssert_Default 1
#endif
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitEnableNoWayAssert, W("JitEnableNoWayAssert"), INTERNAL_JitEnableNoWayAssert_Default, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_JitFramed, W("JitFramed"), 0, "Forces EBP frames")
CONFIG_DWORD_INFO(INTERNAL_JitThrowOnAssertionFailure, W("JitThrowOnAssertionFailure"), 0, "Throw managed exception on assertion failures during JIT instead of crashing the process")
CONFIG_DWORD_INFO(INTERNAL_JitGCStress, W("JitGCStress"), 0, "GC stress mode for jit")
CONFIG_DWORD_INFO(INTERNAL_JitHeartbeat, W("JitHeartbeat"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_JitHelperLogging, W("JitHelperLogging"), 0, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_JITMinOpts, W("JITMinOpts"), 0, "Forces MinOpts")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_JitName, W("JitName"), "Primary Jit to use")
#if defined(ALLOW_SXS_JIT)
RETAIL_CONFIG_STRING_INFO(EXTERNAL_AltJitName, W("AltJitName"), "Alternative Jit to use, will fall back to primary jit.")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_AltJit, W("AltJit"), "Enables AltJit and selectively limits it to the specified methods.")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_AltJitOs, W("AltJitOS"), "Sets target OS for AltJit or uses native one by default. Only applicable for ARM/AMR64 at the moment.")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_AltJitExcludeAssemblies, W("AltJitExcludeAssemblies"), "Do not use AltJit on this semicolon-delimited list of assemblies.")
#endif // defined(ALLOW_SXS_JIT)

#if defined(FEATURE_STACK_SAMPLING)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StackSamplingEnabled, W("StackSamplingEnabled"), 0, "Is stack sampling based tracking of evolving hot methods enabled.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StackSamplingAfter, W("StackSamplingAfter"), 0, "When to start sampling (for some sort of app steady state), i.e., initial delay for sampling start in milliseconds.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StackSamplingEvery, W("StackSamplingEvery"), 100, "How frequent should thread stacks be sampled in milliseconds.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StackSamplingNumMethods, W("StackSamplingNumMethods"), 32, "Number of evolving methods to track as hot and JIT them in the background at a given point of execution.")
#endif // defined(FEATURE_JIT_SAMPLING)

#if defined(ALLOW_SXS_JIT_NGEN)
RETAIL_CONFIG_STRING_INFO(INTERNAL_AltJitNgen, W("AltJitNgen"), "Enables AltJit for NGEN and selectively limits it to the specified methods.")
#endif // defined(ALLOW_SXS_JIT_NGEN)

RETAIL_CONFIG_DWORD_INFO(EXTERNAL_JitHostMaxSlabCache, W("JitHostMaxSlabCache"), 0x1000000, "Sets jit host max slab cache size, 16MB default")

RETAIL_CONFIG_DWORD_INFO(EXTERNAL_JitOptimizeType, W("JitOptimizeType"), 0 /* OPT_DEFAULT */, "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_JitPrintInlinedMethods, W("JitPrintInlinedMethods"), 0, "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_JitTelemetry, W("JitTelemetry"), 1, "If non-zero, gather JIT telemetry data")
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitTimeLogFile, W("JitTimeLogFile"), "If set, gather JIT throughput data and write to this file.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitTimeLogCsv, W("JitTimeLogCsv"), "If set, gather JIT throughput data and write to a CSV file. This mode must be used in internal retail builds.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitFuncInfoLogFile, W("JitFuncInfoLogFile"), "If set, gather JIT function info and write to this file.")
CONFIG_DWORD_INFO(INTERNAL_JitVerificationDisable, W("JitVerificationDisable"), 0, "")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitLockWrite, W("JitLockWrite"), 0, "Force all volatile writes to be 'locked'")
CONFIG_STRING_INFO(INTERNAL_TailCallMax, W("TailCallMax"), "")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_TailCallOpt, W("TailCallOpt"), "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TailCallLoopOpt, W("TailCallLoopOpt"), 1, "Convert recursive tail calls to loops")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Jit_NetFx40PInvokeStackResilience, W("NetFx40_PInvokeStackResilience"), (DWORD)-1, "Makes P/Invoke resilient against mismatched signature and calling convention (significant perf penalty).")

// AltJitAssertOnNYI should be 0 on targets where JIT is under development or bring up stage, so as to facilitate fallback to main JIT on hitting a NYI.
#if defined(TARGET_X86)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 0, "Controls the AltJit behavior of NYI stuff")
#else
RETAIL_CONFIG_DWORD_INFO(INTERNAL_AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 1, "Controls the AltJit behavior of NYI stuff")
#endif
CONFIG_DWORD_INFO(INTERNAL_JitLargeBranches, W("JitLargeBranches"), 0, "Force using the largest conditional branch format")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_JitRegisterFP, W("JitRegisterFP"), 3, "Control FP enregistration")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitELTHookEnabled, W("JitELTHookEnabled"), 0, "On ARM, setting this will emit Enter/Leave/TailCall callbacks")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitMemStats, W("JitMemStats"), 0, "Display JIT memory usage statistics")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitVNMapSelBudget, W("JitVNMapSelBudget"), 100, "Max # of MapSelect's considered for a particular top-level invocation.")
#if defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_ARM64)
#define EXTERNAL_FeatureSIMD_Default 1
#else // !(defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_ARM64))
#define EXTERNAL_FeatureSIMD_Default 0
#endif // !(defined(TARGET_AMD64) || defined(TARGET_X86) || defined(TARGET_ARM64))
RETAIL_CONFIG_DWORD_INFO(INTERNAL_SIMD16ByteOnly, W("SIMD16ByteOnly"), 0, "Limit maximum SIMD vector length to 16 bytes (used by x64_arm64_altjit)")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TrackDynamicMethodDebugInfo, W("TrackDynamicMethodDebugInfo"), 0, "Specifies whether debug info should be generated and tracked for dynamic methods")

#ifdef FEATURE_MULTICOREJIT

RETAIL_CONFIG_STRING_INFO(INTERNAL_MultiCoreJitProfile, W("MultiCoreJitProfile"), "If set, use the file to store/control multi-core JIT.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MultiCoreJitProfileWriteDelay, W("MultiCoreJitProfileWriteDelay"), 12, "Set the delay after which the multi-core JIT profile will be written to disk.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MultiCoreJitMinNumCpus, W("MultiCoreJitMinNumCpus"), 2, "Minimum number of cpus that must be present to allow MultiCoreJit usage.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MultiCoreJitNoProfileGather, W("MultiCoreJitNoProfileGather"), 0, "Set to 1 to disable profile gathering (but leave possibly enabled profile usage).")

#endif

#ifdef FEATURE_INTERPRETER
///
/// Interpreter
///
RETAIL_CONFIG_STRING_INFO(INTERNAL_Interpret, W("Interpret"), "Selectively uses the interpreter to execute the specified methods")
RETAIL_CONFIG_STRING_INFO(INTERNAL_InterpretExclude, W("InterpretExclude"), "Excludes the specified methods from the set selected by 'Interpret'")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterMethHashMin, W("InterpreterMethHashMin"), 0, "Only interpret methods selected by 'Interpret' whose hash is at least this value. or after nth")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterMethHashMax, W("InterpreterMethHashMax"), UINT32_MAX, "If non-zero, only interpret methods selected by 'Interpret' whose hash is at most this value")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterStubMin, W("InterpreterStubMin"), 0, "Only interpret methods selected by 'Interpret' whose stub num is at least this value.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterStubMax, W("InterpreterStubMax"), UINT32_MAX, "If non-zero, only interpret methods selected by 'Interpret' whose stub number is at most this value.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterJITThreshold, W("InterpreterJITThreshold"), 10, "The number of times a method should be interpreted before being JITted")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterDoLoopMethods, W("InterpreterDoLoopMethods"), 0, "If set, don't check for loops, start by interpreting *all* methods")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterUseCaching, W("InterpreterUseCaching"), 1, "If non-zero, use the caching mechanism.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterLooseRules, W("InterpreterLooseRules"), 1, "If non-zero, allow ECMA spec violations required by managed C++.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterPrintPostMortem, W("InterpreterPrintPostMortem"), 0, "Prints summary information about the execution to the console")
RETAIL_CONFIG_STRING_INFO(INTERNAL_InterpreterLogFile, W("InterpreterLogFile"), "If non-null, append interpreter logging to this file, else use stdout")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DumpInterpreterStubs, W("DumpInterpreterStubs"), 0, "Prints all interpreter stubs that are created to the console")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterEntries, W("TraceInterpreterEntries"), 0, "Logs entries to interpreted methods to the console")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterIL, W("TraceInterpreterIL"), 0, "Logs individual instructions of interpreted methods to the console")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterOstack, W("TraceInterpreterOstack"), 0, "Logs operand stack after each IL instruction of interpreted methods to the console")
CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterVerbose, W("TraceInterpreterVerbose"), 0, "Logs interpreter progress with detailed messages to the console")
CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterJITTransition, W("TraceInterpreterJITTransition"), 0, "Logs when the interpreter determines a method should be JITted")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ForceInterpreter, W("ForceInterpreter"), 0, "If non-zero, force the interpreter to be used")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterHWIntrinsicsIsSupportedFalse, W("InterpreterHWIntrinsicsIsSupportedFalse"), 0, "If non-zero, force get_IsSupported to return false for hardware intrinsics") // for internal testing purposes
#endif
// The JIT queries this ConfigDWORD but it doesn't know if FEATURE_INTERPRETER is enabled
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterFallback, W("InterpreterFallback"), 0, "Fallback to the interpreter when the JIT compiler fails")

///
/// Loader heap
///
CONFIG_DWORD_INFO(INTERNAL_LoaderHeapCallTracing, W("LoaderHeapCallTracing"), 0, "Loader heap troubleshooting")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_CodeHeapReserveForJumpStubs, W("CodeHeapReserveForJumpStubs"), 1, "Percentage of code heap to reserve for jump stubs")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NGenReserveForJumpStubs, W("NGenReserveForJumpStubs"), 0, "Percentage of ngen image size to reserve for jump stubs")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_BreakOnOutOfMemoryWithinRange, W("BreakOnOutOfMemoryWithinRange"), 0, "Break before out of memory within range exception is thrown")

///
/// Log
///
RETAIL_CONFIG_DWORD_INFO(INTERNAL_LogEnable, W("LogEnable"), 0, "Turns on the traditional CLR log.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_LogFacility,  W("LogFacility"), 0, "Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_LogFacility2, W("LogFacility2"), 0, "Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_logFatalError, W("logFatalError"), 1, "Specifies whether EventReporter logs fatal errors in the Windows event log.")
CONFIG_STRING_INFO(INTERNAL_LogFile, W("LogFile"), "Specifies a file name for the CLR log.")
CONFIG_DWORD_INFO(INTERNAL_LogFileAppend, W("LogFileAppend"), 0 , "Specifies whether to append to or replace the CLR log file.")
CONFIG_DWORD_INFO(INTERNAL_LogFlushFile, W("LogFlushFile"), 0 , "Specifies whether to flush the CLR log file on each write.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_LogLevel, W("LogLevel"), 0 , "4=10 msgs, 9=1000000, 10=everything")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_LogToConsole, W("LogToConsole"), 0 , "Writes the CLR log to console.")
CONFIG_DWORD_INFO(INTERNAL_LogToDebugger, W("LogToDebugger"), 0 , "Writes the CLR log to debugger (OutputDebugStringA).")
CONFIG_DWORD_INFO(INTERNAL_LogToFile, W("LogToFile"), 0 , "Writes the CLR log to a file.")
CONFIG_DWORD_INFO(INTERNAL_LogWithPid, W("LogWithPid"), FALSE, "Appends pid to filename for the CLR log.")

///
/// MetaData
///
CONFIG_DWORD_INFO(INTERNAL_MD_ApplyDeltaBreak, W("MD_ApplyDeltaBreak"), 0, "ASSERT when applying EnC")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_AssertOnBadImageFormat, W("AssertOnBadImageFormat"), 0, "ASSERT when invalid MD read")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MD_DeltaCheck, W("MD_DeltaCheck"), 1, "Some checks of GUID when applying EnC (?)")
CONFIG_DWORD_INFO(INTERNAL_MD_EncDelta, W("MD_EncDelta"), 0, "Forces EnC Delta format in MD (?)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MD_ForceNoColDesSharing, W("MD_ForceNoColDesSharing"), 0, "Don't know - the only usage I could find is #if 0 (?)")
CONFIG_DWORD_INFO(INTERNAL_MD_KeepKnownCA, W("MD_KeepKnownCA"), 0, "Something with known CAs (?)")
CONFIG_DWORD_INFO(INTERNAL_MD_MiniMDBreak, W("MD_MiniMDBreak"), 0, "ASSERT when creating CMiniMdRw class")
CONFIG_DWORD_INFO(INTERNAL_MD_PreSaveBreak, W("MD_PreSaveBreak"), 0, "ASSERT when calling CMiniMdRw::PreSave")
CONFIG_DWORD_INFO(INTERNAL_MD_RegMetaBreak, W("MD_RegMetaBreak"), 0, "ASSERT when creating RegMeta class")
CONFIG_DWORD_INFO(INTERNAL_MD_RegMetaDump, W("MD_RegMetaDump"), 0, "Dump MD in 4 functions (?)")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_DOTNET_MODIFIABLE_ASSEMBLIES, W("MODIFIABLE_ASSEMBLIES"), "Enables hot reload on debug built assemblies with the 'debug' keyword", CLRConfig::LookupOptions::TrimWhiteSpaceFromStringValue);

// Metadata - mscordbi only - this flag is only intended to mitigate potential issues in bug fix 458597.
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_MD_PreserveDebuggerMetadataMemory, W("MD_PreserveDebuggerMetadataMemory"), 0, "Save all versions of metadata memory in the debugger when debuggee metadata is updated")

///
/// Spinning heuristics
///
// Note that these only take effect once the runtime has been started; prior to that the values hardcoded in g_SpinConstants (vars.cpp) are used
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_SpinInitialDuration, W("SpinInitialDuration"), 0x32, "Hex value specifying the first spin duration")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_SpinBackoffFactor, W("SpinBackoffFactor"), 0x3, "Hex value specifying the growth of each successive spin duration")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_SpinLimitProcCap, W("SpinLimitProcCap"), 0xFFFFFFFF, "Hex value specifying the largest value of NumProcs to use when calculating the maximum spin duration")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_SpinLimitProcFactor, W("SpinLimitProcFactor"), 0x4E20, "Hex value specifying the multiplier on NumProcs to use when calculating the maximum spin duration")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_SpinLimitConstant, W("SpinLimitConstant"), 0x0, "Hex value specifying the constant to add when calculating the maximum spin duration")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_SpinRetryCount, W("SpinRetryCount"), 0xA, "Hex value specifying the number of times the entire spin process is repeated (when applicable)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_Monitor_SpinCount, W("Monitor_SpinCount"), 0x1e, "Hex value specifying the maximum number of spin iterations Monitor may perform upon contention on acquiring the lock before waiting.")

///
/// Native Binder
///

CONFIG_DWORD_INFO(INTERNAL_NgenBind_ZapForbid,             W("NgenBind_ZapForbid"), 0, "Assert if an assembly succeeds in binding to a native image")
CONFIG_STRING_INFO(INTERNAL_NgenBind_ZapForbidExcludeList, W("NgenBind_ZapForbidExcludeList"), "")
CONFIG_STRING_INFO(INTERNAL_NgenBind_ZapForbidList,        W("NgenBind_ZapForbidList"), "")

CONFIG_DWORD_INFO(INTERNAL_SymDiffDump, W("SymDiffDump"), 0, "Used to create the map file while binding the assembly. Used by SemanticDiffer")

///
/// Profiling API / ETW
///
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_CORECLR_ENABLE_PROFILING, W("CORECLR_ENABLE_PROFILING"), 0, "CoreCLR only: Flag to indicate whether profiling should be enabled for the currently running process.", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER, W("CORECLR_PROFILER"), "CoreCLR only: Specifies GUID of profiler to load into currently running process", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER_PATH, W("CORECLR_PROFILER_PATH"), "CoreCLR only: Specifies the path to the DLL of profiler to load into currently running process", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER_PATH_32, W("CORECLR_PROFILER_PATH_32"), "CoreCLR only: Specifies the path to the DLL of profiler to load into currently running 32 process", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER_PATH_64, W("CORECLR_PROFILER_PATH_64"), "CoreCLR only: Specifies the path to the DLL of profiler to load into currently running 64 process", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER_PATH_ARM32, W("CORECLR_PROFILER_PATH_ARM32"), "CoreCLR only: Specifies the path to the DLL of profiler to load into currently running ARM32 process", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER_PATH_ARM64, W("CORECLR_PROFILER_PATH_ARM64"), "CoreCLR only: Specifies the path to the DLL of profiler to load into currently running ARM64 process", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_CORECLR_ENABLE_NOTIFICATION_PROFILERS, W("CORECLR_ENABLE_NOTIFICATION_PROFILERS"), 0, "Set to 0 to disable loading notification profilers.", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_NOTIFICATION_PROFILERS, W("CORECLR_NOTIFICATION_PROFILERS"), "A semi-colon separated list of notification profilers to load into currently running process in the form \"path={guid}\"", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_NOTIFICATION_PROFILERS_32, W("CORECLR_NOTIFICATION_PROFILERS_32"), "A semi-colon separated list of notification profilers to load into currently running 32 process in the form \"path={guid}\"", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_NOTIFICATION_PROFILERS_64, W("CORECLR_NOTIFICATION_PROFILERS_64"), "A semi-colon separated list of notification profilers to load into currently running 64 process in the form \"path={guid}\"", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_NOTIFICATION_PROFILERS_ARM32, W("CORECLR_NOTIFICATION_PROFILERS_ARM32"), "A semi-colon separated list of notification profilers to load into currently running ARM32 process in the form \"path={guid}\"", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_NOTIFICATION_PROFILERS_ARM64, W("CORECLR_NOTIFICATION_PROFILERS_ARM64"), "A semi-colon separated list of notification profilers to load into currently running ARM64 process in the form \"path={guid}\"", CLRConfig::LookupOptions::DontPrependPrefix)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_ProfAPI_ProfilerCompatibilitySetting, W("ProfAPI_ProfilerCompatibilitySetting"), "Specifies the profiler loading policy (the default is not to load a V2 profiler in V4)", CLRConfig::LookupOptions::TrimWhiteSpaceFromStringValue)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_DetachMinSleepMs, W("ProfAPI_DetachMinSleepMs"), 0, "The minimum time, in milliseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_DetachMaxSleepMs, W("ProfAPI_DetachMaxSleepMs"), 0, "The maximum time, in milliseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_RejitOnAttach, W("ProfApi_RejitOnAttach"), 1, "Enables the ability for profilers to rejit methods on attach.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_InliningTracking, W("ProfApi_InliningTracking"), 1, "Enables the runtime's tracking of inlining for profiler ReJIT.")
CONFIG_DWORD_INFO(INTERNAL_ProfAPI_EnableRejitDiagnostics, W("ProfAPI_EnableRejitDiagnostics"), 0, "Enable extra dumping to stdout of rejit structures")
CONFIG_DWORD_INFO(INTERNAL_ProfAPIFault, W("ProfAPIFault"), 0, "Test-only bitmask to inject various types of faults in the profapi code")
CONFIG_DWORD_INFO(INTERNAL_TestOnlyAllowedEventMask, W("TestOnlyAllowedEventMask"), 0, "Test-only bitmask to allow profiler tests to override CLR enforcement of COR_PRF_ALLOWABLE_AFTER_ATTACH and COR_PRF_MONITOR_IMMUTABLE")
CONFIG_DWORD_INFO(INTERNAL_TestOnlyEnableICorProfilerInfo, W("ProfAPI_TestOnlyEnableICorProfilerInfo"), 0, "Test-only flag to allow attaching profiler tests to call ICorProfilerInfo interface, which would otherwise be disallowed for attaching profilers")
CONFIG_DWORD_INFO(INTERNAL_TestOnlyEnableObjectAllocatedHook, W("TestOnlyEnableObjectAllocatedHook"), 0, "Test-only flag that forces CLR to initialize on startup as if ObjectAllocated callback were requested, to enable post-attach ObjectAllocated functionality.")
CONFIG_DWORD_INFO(INTERNAL_TestOnlyEnableSlowELTHooks, W("TestOnlyEnableSlowELTHooks"), 0, "Test-only flag that forces CLR to initialize on startup as if slow-ELT were requested, to enable post-attach ELT functionality.")

RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_ETW_ObjectAllocationEventsPerTypePerSec, W("ETW_ObjectAllocationEventsPerTypePerSec"), "Desired number of GCSampledObjectAllocation ETW events to be logged per type per second.  If 0, then the default built in to the implementation for the enabled event (e.g., High, Low), will be used.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_ProfAPI_ValidateNGENInstrumentation, W("ProfAPI_ValidateNGENInstrumentation"), 0, "This flag enables additional validations when using the IMetaDataEmit APIs for NGEN'ed images to ensure only supported edits are made.")

#ifdef FEATURE_PERFMAP
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_PerfMapEnabled, W("PerfMapEnabled"), 0, "This flag is used on Linux to enable writing /tmp/perf-$pid.map. It is disabled by default")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_PerfMapJitDumpPath, W("PerfMapJitDumpPath"), "Specifies a path to write the perf jitdump file. Defaults to GetTempPathA()")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_PerfMapIgnoreSignal, W("PerfMapIgnoreSignal"), 0, "When perf map is enabled, this option will configure the specified signal to be accepted and ignored as a marker in the perf logs.  It is disabled by default")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_PerfMapShowOptimizationTiers, W("PerfMapShowOptimizationTiers"), 1, "Shows optimization tiers in the perf map for methods, as part of the symbol name. Useful for seeing separate stack frames for different optimization tiers of each method.")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_NativeImagePerfMapFormat, W("NativeImagePerfMapFormat"), "Specifies the format of native image perfmap files generated by crossgen.  Valid options are RVA or OFFSET.")
#endif

RETAIL_CONFIG_STRING_INFO(EXTERNAL_StartupDelayMS, W("StartupDelayMS"), "")

///
/// Stress
///
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StressLog, W("StressLog"), 0, "Turns on the stress log.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_ForceEnc, W("ForceEnc"), 0, "Forces Edit and Continue to be on for all eligible modules.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StressLogSize, W("StressLogSize"), 0, "Stress log size in bytes per thread.")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_StressLogFilename, W("StressLogFilename"), "Stress log filename for memory mapped stress log.")
CONFIG_DWORD_INFO(INTERNAL_stressSynchronized, W("stressSynchronized"), 0, "Unknown if or where this is used; unless a test is specifically depending on this, it can be removed.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TotalStressLogSize, W("TotalStressLogSize"), 0, "Total stress log size in bytes.")

///
/// Thread Suspend
///
CONFIG_DWORD_INFO(INTERNAL_DiagnosticSuspend, W("DiagnosticSuspend"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_SuspendDeadlockTimeout, W("SuspendDeadlockTimeout"), 40000, "")
CONFIG_DWORD_INFO(INTERNAL_SuspendThreadDeadlockTimeoutMs, W("SuspendThreadDeadlockTimeoutMs"), 2000, "")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadSuspendInjection, W("INTERNAL_ThreadSuspendInjection"), 1, "Specifies whether to inject activations for thread suspension on Unix")

///
/// Thread (miscellaneous)
///
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DefaultStackSize, W("DefaultStackSize"), 0, "Stack size to use for new VM threads when thread is created with default stack size (dwStackSize == 0).")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_Thread_DeadThreadCountThresholdForGCTrigger, W("Thread_DeadThreadCountThresholdForGCTrigger"), 75, "In the heuristics to clean up dead threads, this threshold must be reached before triggering a GC will be considered. Set to 0 to disable triggering a GC based on dead threads.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_Thread_DeadThreadGCTriggerPeriodMilliseconds, W("Thread_DeadThreadGCTriggerPeriodMilliseconds"), 1000 * 60 * 30, "In the heuristics to clean up dead threads, this much time must have elapsed since the previous max-generation GC before triggering another GC will be considered")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Thread_UseAllCpuGroups, W("Thread_UseAllCpuGroups"), 0, "Specifies whether to query and use CPU group information for determining the processor count.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Thread_AssignCpuGroups, W("Thread_AssignCpuGroups"), 1, "Specifies whether to automatically distribute threads created by the CLR across CPU Groups. Effective only when Thread_UseAllCpuGroups and GCCpuGroup are enabled.")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_ProcessorCount, W("PROCESSOR_COUNT"), 0, "Specifies the number of processors available for the process, which is returned by Environment.ProcessorCount", CLRConfig::LookupOptions::ParseIntegerAsBase10)

///
/// Threadpool
///
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_UsePortableThreadPool, W("ThreadPool_UsePortableThreadPool"), 1, "Uses the managed portable thread pool implementation instead of the unmanaged one.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_UsePortableThreadPoolForIO, W("ThreadPool_UsePortableThreadPoolForIO"), 1, "Uses the managed portable thread pool implementation instead of the unmanaged one for async IO.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_ForceMinWorkerThreads, W("ThreadPool_ForceMinWorkerThreads"), 0, "Overrides the MinThreads setting for the ThreadPool worker pool")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_ForceMaxWorkerThreads, W("ThreadPool_ForceMaxWorkerThreads"), 0, "Overrides the MaxThreads setting for the ThreadPool worker pool")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_DisableStarvationDetection, W("ThreadPool_DisableStarvationDetection"), 0, "Disables the ThreadPool feature that forces new threads to be added when workitems run for too long")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_DebugBreakOnWorkerStarvation, W("ThreadPool_DebugBreakOnWorkerStarvation"), 0, "Breaks into the debugger if the ThreadPool detects work queue starvation")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_EnableWorkerTracking, W("ThreadPool_EnableWorkerTracking"), 0, "Enables extra expensive tracking of how many workers threads are working simultaneously")
#ifdef TARGET_ARM64
// Spinning scheme is currently different on ARM64, see CLRLifoSemaphore::Wait(DWORD, UINT32, UINT32)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_UnfairSemaphoreSpinLimit, W("ThreadPool_UnfairSemaphoreSpinLimit"), 0x32, "Maximum number of spins per processor a thread pool worker thread performs before waiting for work")
#else // !TARGET_ARM64
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_UnfairSemaphoreSpinLimit, W("ThreadPool_UnfairSemaphoreSpinLimit"), 0x46, "Maximum number of spins a thread pool worker thread performs before waiting for work")
#endif // TARGET_ARM64

RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_Disable,                             W("HillClimbing_Disable"),                            0, "Disables hill climbing for thread adjustments in the thread pool");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_WavePeriod,                          W("HillClimbing_WavePeriod"),                         4, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_TargetSignalToNoiseRatio,            W("HillClimbing_TargetSignalToNoiseRatio"),           300, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_ErrorSmoothingFactor,                W("HillClimbing_ErrorSmoothingFactor"),               1, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_WaveMagnitudeMultiplier,             W("HillClimbing_WaveMagnitudeMultiplier"),            100, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_MaxWaveMagnitude,                    W("HillClimbing_MaxWaveMagnitude"),                   20, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_WaveHistorySize,                     W("HillClimbing_WaveHistorySize"),                    8, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_Bias,                                W("HillClimbing_Bias"),                               15, "The 'cost' of a thread.  0 means drive for increased throughput regardless of thread count; higher values bias more against higher thread counts.");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_MaxChangePerSecond,                  W("HillClimbing_MaxChangePerSecond"),                 4, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_MaxChangePerSample,                  W("HillClimbing_MaxChangePerSample"),                 20, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_MaxSampleErrorPercent,               W("HillClimbing_MaxSampleErrorPercent"),              15, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_SampleIntervalLow,                   W("HillClimbing_SampleIntervalLow"),                  10, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_SampleIntervalHigh,                  W("HillClimbing_SampleIntervalHigh"),                 200, "");
RETAIL_CONFIG_DWORD_INFO(INTERNAL_HillClimbing_GainExponent,                        W("HillClimbing_GainExponent"),                       200, "The exponent to apply to the gain, times 100.  100 means to use linear gain, higher values will enhance large moves and damp small ones.");

///
/// Tiered Compilation
///
#ifdef FEATURE_TIERED_COMPILATION
#ifdef _DEBUG
// Use lower values to exercise more paths sooner
#define TC_BackgroundWorkerTimeoutMs (100)
#define TC_CallCountThreshold (2)
#define TC_CallCountingDelayMs (1)
#define TC_DelaySingleProcMultiplier (2)
#else // !_DEBUG
#define TC_BackgroundWorkerTimeoutMs (4000)
#define TC_CallCountThreshold (30)
#define TC_CallCountingDelayMs (100)
#define TC_DelaySingleProcMultiplier (10)
#endif // _DEBUG
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TieredCompilation, W("TieredCompilation"), 1, "Enables tiered compilation")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TC_QuickJit, W("TC_QuickJit"), 1, "For methods that would be jitted, enable using quick JIT when appropriate.")
#if defined(TARGET_AMD64) || defined(TARGET_ARM64)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TC_QuickJitForLoops, W("TC_QuickJitForLoops"), 1, "When quick JIT is enabled, quick JIT may also be used for methods that contain loops.")
#else // !(defined(TARGET_AMD64) || defined(TARGET_ARM64))
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TC_QuickJitForLoops, W("TC_QuickJitForLoops"), 0, "When quick JIT is enabled, quick JIT may also be used for methods that contain loops.")
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TC_AggressiveTiering, W("TC_AggressiveTiering"), 0, "Transition through tiers aggressively.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_BackgroundWorkerTimeoutMs, W("TC_BackgroundWorkerTimeoutMs"), TC_BackgroundWorkerTimeoutMs, "How long in milliseconds the background worker thread may remain idle before exiting.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_CallCountThreshold, W("TC_CallCountThreshold"), TC_CallCountThreshold, "Number of times a method must be called in tier 0 after which it is promoted to the next tier.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_CallCountingDelayMs, W("TC_CallCountingDelayMs"), TC_CallCountingDelayMs, "A perpetual delay in milliseconds that is applied call counting in tier 0 and jitting at higher tiers, while there is startup-like activity.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_DelaySingleProcMultiplier, W("TC_DelaySingleProcMultiplier"), TC_DelaySingleProcMultiplier, "Multiplier for TC_CallCountingDelayMs that is applied on a single-processor machine or when the process is affinitized to a single processor.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_CallCounting, W("TC_CallCounting"), 1, "Enabled by default (only activates when TieredCompilation is also enabled). If disabled immediately backpatches prestub, and likely prevents any promotion to higher tiers")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_UseCallCountingStubs, W("TC_UseCallCountingStubs"), 1, "Uses call counting stubs for faster call counting.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_DeleteCallCountingStubsAfter, W("TC_DeleteCallCountingStubsAfter"), 0, "Deletes call counting stubs after this many have completed. Zero to disable deleting.")
#undef TC_BackgroundWorkerTimeoutMs
#undef TC_CallCountThreshold
#undef TC_CallCountingDelayMs
#undef TC_DelaySingleProcMultiplier
#undef TC_DeleteCallCountingStubsAfter
#endif // FEATURE_TIERED_COMPILATION

///
/// On-Stack Replacement
///
#ifdef FEATURE_ON_STACK_REPLACEMENT
RETAIL_CONFIG_DWORD_INFO(INTERNAL_OSR_CounterBump, W("OSR_CounterBump"), 1000, "Counter reload value when a patchpoint is hit")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_OSR_HitLimit, W("OSR_HitLimit"), 10, "Number of times a patchpoint must call back to trigger an OSR transition")
CONFIG_DWORD_INFO(INTERNAL_OSR_LowId, W("OSR_LowId"), (DWORD)-1, "Low end of enabled patchpoint range (inclusive)");
CONFIG_DWORD_INFO(INTERNAL_OSR_HighId, W("OSR_HighId"), 10000000, "High end of enabled patchpoint range (inclusive)");
#endif

///
/// Profile Guided Opts
///
#ifdef FEATURE_PGO
RETAIL_CONFIG_STRING_INFO(INTERNAL_PGODataPath, W("PGODataPath"), "Read/Write PGO data from/to the indicated file.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ReadPGOData, W("ReadPGOData"), 0, "Read PGO data")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_WritePGOData, W("WritePGOData"), 0, "Write PGO data")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TieredPGO, W("TieredPGO"), 0, "Instrument Tier0 code and make counts available to Tier1")
#endif

///
/// Entry point slot backpatch
///
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BackpatchEntryPointSlots, W("BackpatchEntryPointSlots"), 1, "Indicates whether to enable entry point slot backpatching, for instance to avoid making virtual calls through a precode and instead to patch virtual slots for a method when its entry point changes.")

///
/// TypeLoader
///
CONFIG_DWORD_INFO(INTERNAL_TypeLoader_InjectInterfaceDuplicates, W("INTERNAL_TypeLoader_InjectInterfaceDuplicates"), 0, "Injects duplicates in interface map for all types.")

///
/// Virtual call stubs
///
CONFIG_DWORD_INFO(INTERNAL_VirtualCallStubCollideMonoPct, W("VirtualCallStubCollideMonoPct"), 0, "Used only when STUB_LOGGING is defined, which by default is not.")
CONFIG_DWORD_INFO(INTERNAL_VirtualCallStubCollideWritePct, W("VirtualCallStubCollideWritePct"), 100, "Used only when STUB_LOGGING is defined, which by default is not.")
CONFIG_DWORD_INFO(INTERNAL_VirtualCallStubDumpLogCounter, W("VirtualCallStubDumpLogCounter"), 0, "Used only when STUB_LOGGING is defined, which by default is not.")
CONFIG_DWORD_INFO(INTERNAL_VirtualCallStubDumpLogIncr, W("VirtualCallStubDumpLogIncr"), 0, "Used only when STUB_LOGGING is defined, which by default is not.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_VirtualCallStubLogging, W("VirtualCallStubLogging"), 0, "Worth keeping, but should be moved into \"#ifdef STUB_LOGGING\" blocks. This goes for most (or all) of the stub logging infrastructure.")
CONFIG_DWORD_INFO(INTERNAL_VirtualCallStubMissCount, W("VirtualCallStubMissCount"), 100, "Used only when STUB_LOGGING is defined, which by default is not.")
CONFIG_DWORD_INFO(INTERNAL_VirtualCallStubResetCacheCounter, W("VirtualCallStubResetCacheCounter"), 0, "Used only when STUB_LOGGING is defined, which by default is not.")
CONFIG_DWORD_INFO(INTERNAL_VirtualCallStubResetCacheIncr, W("VirtualCallStubResetCacheIncr"), 0, "Used only when STUB_LOGGING is defined, which by default is not.")

///
/// Watson
///
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DisableWatsonForManagedExceptions, W("DisableWatsonForManagedExceptions"), 0, "Disable Watson and debugger launching for managed exceptions")

///
/// Dump generation
///
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DbgEnableMiniDump, W("DbgEnableMiniDump"), 0, "Enable unhandled exception crash dump generation")
RETAIL_CONFIG_STRING_INFO(INTERNAL_DbgMiniDumpName, W("DbgMiniDumpName"), "Crash dump name")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DbgMiniDumpType, W("DbgMiniDumpType"), 0, "Crash dump type: 1 normal, 2 withheap, 3 triage, 4 full")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_CreateDumpDiagnostics, W("CreateDumpDiagnostics"), 0, "Enable crash dump generation diagnostic logging")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EnableDumpOnSigTerm, W("EnableDumpOnSigTerm"), 0, "Enable crash dump generation on SIGTERM")

///
/// Zap
///
RETAIL_CONFIG_STRING_INFO(INTERNAL_ZapBBInstr, W("ZapBBInstr"), "")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_ZapBBInstrDir, W("ZapBBInstrDir"), "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ZapDisable, W("ZapDisable"), 0, "")

RETAIL_CONFIG_STRING_INFO(INTERNAL_NativeImageSearchPaths, W("NativeImageSearchPaths"), "Extra search paths for native composite R2R images")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ReadyToRun, W("ReadyToRun"), 1, "Enable/disable use of ReadyToRun native code") // On by default for CoreCLR
RETAIL_CONFIG_STRING_INFO(EXTERNAL_ReadyToRunExcludeList, W("ReadyToRunExcludeList"), "List of assemblies that cannot use Ready to Run images")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_ReadyToRunLogFile, W("ReadyToRunLogFile"), "Name of file to log success/failure of using Ready to Run images")

#if defined(FEATURE_EVENT_TRACE) || defined(FEATURE_EVENTSOURCE_XPLAT)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableEventLog, W("EnableEventLog"), 0, "Enable/disable use of EnableEventLogging mechanism ") // Off by default
RETAIL_CONFIG_STRING_INFO(INTERNAL_EventSourceFilter, W("EventSourceFilter"), "")
RETAIL_CONFIG_STRING_INFO(INTERNAL_EventNameFilter, W("EventNameFilter"), "")
#endif //defined(FEATURE_EVENT_TRACE) || defined(FEATURE_EVENTSOURCE_XPLAT)

///
/// Interop
///
CONFIG_DWORD_INFO(INTERNAL_ExposeExceptionsInCOM, W("ExposeExceptionsInCOM"), 0, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_InteropValidatePinnedObjects, W("InteropValidatePinnedObjects"), 0, "After returning from a managed-to-unmanaged interop call, validate GC heap around objects pinned by IL stubs.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_InteropLogArguments, W("InteropLogArguments"), 0, "Log all pinned arguments passed to an interop call")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_LogCCWRefCountChange, W("LogCCWRefCountChange"), "Outputs debug information and calls LogCCWRefCountChange_BREAKPOINT when AddRef or Release is called on a CCW.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EnableRCWCleanupOnSTAShutdown, W("EnableRCWCleanupOnSTAShutdown"), 0, "Performs RCW cleanup when STA shutdown is detected using IInitializeSpy in classic processes.")

//
// EventPipe
//
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EnableEventPipe, W("EnableEventPipe"), 0, "Enable/disable event pipe.  Non-zero values enable tracing.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EventPipeNetTraceFormat, W("EventPipeNetTraceFormat"), 1, "Enable/disable using the newer nettrace file format.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_EventPipeOutputPath, W("EventPipeOutputPath"), "The full path excluding file name for the trace file that will be written when COMPlus_EnableEventPipe=1")
RETAIL_CONFIG_STRING_INFO(INTERNAL_EventPipeConfig, W("EventPipeConfig"), "Configuration for EventPipe.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EventPipeRundown, W("EventPipeRundown"), 1, "Enable/disable eventpipe rundown.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EventPipeCircularMB, W("EventPipeCircularMB"), 1024, "The EventPipe circular buffer size in megabytes.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EventPipeProcNumbers, W("EventPipeProcNumbers"), 0, "Enable/disable capturing processor numbers in EventPipe event headers")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EventPipeOutputStreaming, W("EventPipeOutputStreaming"), 0, "Enable/disable streaming for trace file set in COMPlus_EventPipeOutputPath.  Non-zero values enable streaming.")

#ifdef FEATURE_AUTO_TRACE
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_AutoTrace_N_Tracers, W("AutoTrace_N_Tracers"), 0, "", CLRConfig::LookupOptions::ParseIntegerAsBase10)
RETAIL_CONFIG_STRING_INFO(INTERNAL_AutoTrace_Command, W("AutoTrace_Command"), "")
#endif // FEATURE_AUTO_TRACE

//
// Generational Aware Analysis
//
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCGenAnalysisGen, W("GCGenAnalysisGen"), 0, "The generation to trigger generational aware analysis")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCGenAnalysisBytes, W("GCGenAnalysisBytes"), 0, "The number of bytes to trigger generational aware analysis")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCGenAnalysisTimeUSec, W("GCGenAnalysisTimeUSec"), 0, "The number of microseconds to trigger generational aware analysis")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCGenAnalysisTimeMSec, W("GCGenAnalysisTimeMSec"), 0, "The number of milliseconds to trigger generational aware analysis")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCGenAnalysisIndex, W("GCGenAnalysisIndex"), 0, "The gc index to trigger generational aware analysis")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_GCGenAnalysisCmd, W("GCGenAnalysisCmd"), "An optional filter to match with the command line used to spawn the process")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCGenAnalysisTrace, W("GCGenAnalysisTrace"), 1, "Enable/Disable capturing a trace")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCGenAnalysisDump, W("GCGenAnalysisDump"), 0, "Enable/Disable capturing a dump")

//
// Diagnostics Ports
//
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_DOTNET_DefaultDiagnosticPortSuspend, W("DefaultDiagnosticPortSuspend"), 0, "This sets the deafult diagnostic port to suspend causing the runtime to pause during startup before major subsystems are started.  Resume using the Diagnostics IPC ResumeStartup command on the default diagnostic port.");
RETAIL_CONFIG_STRING_INFO(EXTERNAL_DOTNET_DiagnosticPorts, W("DiagnosticPorts"), "A semicolon delimited list of additional Diagnostic Ports, where a Diagnostic Port is a NamedPipe path without '\\\\.\\pipe\\' on Windows or the full path of Unix Domain Socket on Linux/Unix followed by optional tags, e.g., '<path>,connect,nosuspend;<path>'");

//
// LTTng
//
RETAIL_CONFIG_STRING_INFO(INTERNAL_LTTngConfig, W("LTTngConfig"), "Configuration for LTTng.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_LTTng, W("LTTng"), 1, "If COMPlus_LTTng is set to 0, this will prevent the LTTng library from being loaded at runtime")

//
// Executable code
//
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableWriteXorExecute, W("EnableWriteXorExecute"), 0, "Enable W^X for executable memory.");

#ifdef FEATURE_GDBJIT
///
/// GDBJIT
///
CONFIG_STRING_INFO(INTERNAL_GDBJitElfDump, W("GDBJitElfDump"), "Dump ELF for specified method")
#ifdef FEATURE_GDBJIT_FRAME
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GDBJitEmitDebugFrame, W("GDBJitEmitDebugFrame"), TRUE, "Enable .debug_frame generation")
#endif
#endif

//
// Hardware Intrinsic ISAs
//
#if defined(TARGET_LOONGARCH64)
//TODO: should implement LoongArch64's features.
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableHWIntrinsic,  W("EnableHWIntrinsic"),  0, "Allows Base+ hardware intrinsics to be disabled")
#else
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableHWIntrinsic,  W("EnableHWIntrinsic"),  1, "Allows Base+ hardware intrinsics to be disabled")
#endif // defined(TARGET_LOONGARCH64)

#if defined(TARGET_AMD64) || defined(TARGET_X86)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableAES,          W("EnableAES"),          1, "Allows AES+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableAVX,          W("EnableAVX"),          1, "Allows AVX+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableAVX2,         W("EnableAVX2"),         1, "Allows AVX2+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableAVXVNNI,      W("EnableAVXVNNI"),      1, "Allows AVX VNNI+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableBMI1,         W("EnableBMI1"),         1, "Allows BMI1+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableBMI2,         W("EnableBMI2"),         1, "Allows BMI2+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableFMA,          W("EnableFMA"),          1, "Allows FMA+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableLZCNT,        W("EnableLZCNT"),        1, "Allows LZCNT+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnablePCLMULQDQ,    W("EnablePCLMULQDQ"),    1, "Allows PCLMULQDQ+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnablePOPCNT,       W("EnablePOPCNT"),       1, "Allows POPCNT+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableSSE,          W("EnableSSE"),          1, "Allows SSE+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableSSE2,         W("EnableSSE2"),         1, "Allows SSE2+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableSSE3,         W("EnableSSE3"),         1, "Allows SSE3+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableSSE3_4,       W("EnableSSE3_4"),       1, "Allows SSE3+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableSSE41,        W("EnableSSE41"),        1, "Allows SSE4.1+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableSSE42,        W("EnableSSE42"),        1, "Allows SSE4.2+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableSSSE3,        W("EnableSSSE3"),        1, "Allows SSSE3+ hardware intrinsics to be disabled")
#elif defined(TARGET_ARM64)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64AdvSimd, W("EnableArm64AdvSimd"), 1, "Allows Arm64 AdvSimd+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Aes,     W("EnableArm64Aes"),     1, "Allows Arm64 Aes+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Atomics, W("EnableArm64Atomics"), 1, "Allows Arm64 Atomics+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Crc32,   W("EnableArm64Crc32"),   1, "Allows Arm64 Crc32+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Dczva,   W("EnableArm64Dczva"),   1, "Allows Arm64 Dczva+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Dp,      W("EnableArm64Dp"),      1, "Allows Arm64 Dp+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Rdm,     W("EnableArm64Rdm"),     1, "Allows Arm64 Rdm+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Sha1,    W("EnableArm64Sha1"),    1, "Allows Arm64 Sha1+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Sha256,  W("EnableArm64Sha256"),  1, "Allows Arm64 Sha256+ hardware intrinsics to be disabled")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_EnableArm64Rcpc,    W("EnableArm64Rcpc"),    1, "Allows Arm64 Rcpc+ hardware intrinsics to be disabled")
#endif

///
/// Uncategorized
///
//
// Unknown
//
//---------------------------------------------------------------------------------------
// **
// PLEASE MOVE ANY CONFIG SWITCH YOU OWN OUT OF THIS SECTION INTO A CATEGORY ABOVE
//
// DO NOT ADD ANY MORE CONFIG SWITCHES TO THIS SECTION!
// **
CONFIG_DWORD_INFO(INTERNAL_ActivatePatchSkip, W("ActivatePatchSkip"), 0, "Allows an assert when ActivatePatchSkip is called")
CONFIG_DWORD_INFO(INTERNAL_AlwaysUseMetadataInterfaceMapLayout, W("AlwaysUseMetadataInterfaceMapLayout"), 0, "Used for debugging generic interface map layout.")
CONFIG_DWORD_INFO(INTERNAL_AssertOnUnneededThis, W("AssertOnUnneededThis"), 0, "While the ConfigDWORD is unnecessary, the contained ASSERT should be kept. This may result in some work tracking down violating MethodDescCallSites.")
CONFIG_DWORD_INFO(INTERNAL_AssertStacktrace, W("AssertStacktrace"), 1, "")
CONFIG_DWORD_INFO(INTERNAL_clearNativeImageStress, W("clearNativeImageStress"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_CPUFamily, W("CPUFamily"), 0xFFFFFFFF, "")
CONFIG_DWORD_INFO(INTERNAL_CPUFeatures, W("CPUFeatures"), 0xFFFFFFFF, "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_DisableConfigCache, W("DisableConfigCache"), 0, "Used to disable the \"probabilistic\" config cache, which walks through the appropriate config registry keys on init and probabilistically keeps track of which exist.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_DisableStackwalkCache, W("DisableStackwalkCache"), 0, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_DoubleArrayToLargeObjectHeap, W("DoubleArrayToLargeObjectHeap"), 0, "Controls double[] placement")
CONFIG_STRING_INFO(INTERNAL_DumpOnClassLoad, W("DumpOnClassLoad"), "Dumps information about loaded class to log.")
CONFIG_DWORD_INFO(INTERNAL_ExpandAllOnLoad, W("ExpandAllOnLoad"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_ForceRelocs, W("ForceRelocs"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_GenerateLongJumpDispatchStubRatio, W("GenerateLongJumpDispatchStubRatio"), 0, "Useful for testing VSD on AMD64")
CONFIG_DWORD_INFO(INTERNAL_HashStack, W("HashStack"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_HostManagerConfig, W("HostManagerConfig"), (DWORD)-1, "")
CONFIG_DWORD_INFO(INTERNAL_HostTestThreadAbort, W("HostTestThreadAbort"), 0, "")
CONFIG_STRING_INFO(INTERNAL_InvokeHalt, W("InvokeHalt"), "Throws an assert when the given method is invoked through reflection.")
CONFIG_DWORD_INFO(INTERNAL_MaxStubUnwindInfoSegmentSize, W("MaxStubUnwindInfoSegmentSize"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_MessageDebugOut, W("MessageDebugOut"), 0, "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_NativeImageRequire, W("NativeImageRequire"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_NestedEhOom, W("NestedEhOom"), 0, "")
#define INTERNAL_NoGuiOnAssert_Default 1
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NoGuiOnAssert, W("NoGuiOnAssert"), INTERNAL_NoGuiOnAssert_Default, "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_NoProcedureSplitting, W("NoProcedureSplitting"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_PauseOnLoad, W("PauseOnLoad"), 0, "Stops in SystemDomain::init. I think it can be removed.")
CONFIG_DWORD_INFO(INTERNAL_PerfAllocsSizeThreshold, W("PerfAllocsSizeThreshold"), 0x3FFFFFFF, "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
CONFIG_DWORD_INFO(INTERNAL_PerfNumAllocsThreshold, W("PerfNumAllocsThreshold"), 0x3FFFFFFF, "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
CONFIG_STRING_INFO(INTERNAL_PerfTypesToLog, W("PerfTypesToLog"), "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Prepopulate1, W("Prepopulate1"), 1, "")
CONFIG_STRING_INFO(INTERNAL_PrestubGC, W("PrestubGC"), "")
CONFIG_STRING_INFO(INTERNAL_PrestubHalt, W("PrestubHalt"), "")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_RestrictedGCStressExe, W("RestrictedGCStressExe"), "")
CONFIG_DWORD_INFO(INTERNAL_ReturnSourceTypeForTesting, W("ReturnSourceTypeForTesting"), 0, "Allows returning the (internal only) source type of an IL to Native mapping for debugging purposes")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_RSStressLog, W("RSStressLog"), 0, "Allows turning on logging for RS startup")
CONFIG_DWORD_INFO(INTERNAL_SBDumpOnNewIndex, W("SBDumpOnNewIndex"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
CONFIG_DWORD_INFO(INTERNAL_SBDumpOnResize, W("SBDumpOnResize"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
CONFIG_DWORD_INFO(INTERNAL_SBDumpStyle, W("SBDumpStyle"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_SleepOnExit, W("SleepOnExit"), 0, "Used for lrak detection. I'd say deprecated by umdh.")
CONFIG_DWORD_INFO(INTERNAL_StubLinkerUnwindInfoVerificationOn, W("StubLinkerUnwindInfoVerificationOn"), 0, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_SuccessExit, W("SuccessExit"), 0, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TestDataConsistency, W("TestDataConsistency"), FALSE, "Allows ensuring the left side is not holding locks (and may thus be in an inconsistent state) when inspection occurs")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ThreadGuardPages, W("ThreadGuardPages"), 0, "")

#ifdef _DEBUG
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TraceWrap, W("TraceWrap"), 0, "")
#endif

RETAIL_CONFIG_DWORD_INFO(EXTERNAL_UseMethodDataCache, W("UseMethodDataCache"), FALSE, "Used during feature development; may now be removed.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_UseParentMethodData, W("UseParentMethodData"), TRUE, "Used during feature development; may now be removed.")
CONFIG_DWORD_INFO(INTERNAL_VerifierOff, W("VerifierOff"), 0, "")
// **
// PLEASE MOVE ANY CONFIG SWITCH YOU OWN OUT OF THIS SECTION INTO A CATEGORY ABOVE
//
// DO NOT ADD ANY MORE CONFIG SWITCHES TO THIS SECTION!
// **
//---------------------------------------------------------------------------------------
