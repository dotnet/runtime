// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// CLRConfigValues.h
//

//
// Unified method of accessing configuration values from environment variables,
// registry and config file.
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
// COMPlus_ to the name), the registry (HKLM and HKCU), and all the config files for this value. To customize
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
// (CLRConfig::LookupOptions) (CLRConfig::IgnoreEnv | CLRConfig::IgnoreHKCU))
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
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_AddRejitNops, W("AddRejitNops"), "Control for the profiler rejit feature infrastructure")
CONFIG_DWORD_INFO(INTERNAL_ADDumpSB, W("ADDumpSB"), 0, "Not used")
CONFIG_DWORD_INFO(INTERNAL_ADForceSB, W("ADForceSB"), 0, "Forces sync block creation for all objects")
CONFIG_DWORD_INFO(INTERNAL_ADLogMemory, W("ADLogMemory"), 0, "Superseded by test hooks")
CONFIG_DWORD_INFO(INTERNAL_ADTakeDHSnapShot, W("ADTakeDHSnapShot"), 0, "Superseded by test hooks")
CONFIG_DWORD_INFO(INTERNAL_ADTakeSnapShot, W("ADTakeSnapShot"), 0, "Superseded by test hooks")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_EnableFullDebug, W("EnableFullDebug"), "Heavy-weight checking for AD boundary violations (AD leaks)")

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
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_DesignerNamespaceResolutionEnabled, W("designerNamespaceResolution"), FALSE, "Set it to 1 to enable DesignerNamespaceResolve event for WinRT types", CLRConfig::IgnoreEnv | CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU | CLRConfig::FavorConfigFile)
CONFIG_DWORD_INFO_EX(INTERNAL_GetAssemblyIfLoadedIgnoreRidMap, W("GetAssemblyIfLoadedIgnoreRidMap"), 0, "Used to force loader to ignore assemblies cached in the rid-map", CLRConfig::REGUTIL_default)

///
/// Conditional breakpoints
///
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_BreakOnBadExit, W("BreakOnBadExit"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_BreakOnClassBuild, W("BreakOnClassBuild"), "Very useful for debugging class layout code.")
CONFIG_STRING_INFO(INTERNAL_BreakOnClassLoad, W("BreakOnClassLoad"), "Very useful for debugging class loading code.")
CONFIG_STRING_INFO(INTERNAL_BreakOnComToClrNativeInfoInit, W("BreakOnComToClrNativeInfoInit"), "Throws an assert when native information about a COM -> CLR call are about to be gathered.")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnDebugBreak, W("BreakOnDebugBreak"), 0, "Allows an assert in debug builds when a user break is hit", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnDILoad, W("BreakOnDILoad"), 0, "Allows an assert when the DI is loaded", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnDumpToken, W("BreakOnDumpToken"), 0xffffffff, "Breaks when using internal logging on a particular token value.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_BreakOnEELoad, W("BreakOnEELoad"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_BreakOnEEShutdown, W("BreakOnEEShutdown"), 0, "")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnExceptionInGetThrowable, W("BreakOnExceptionInGetThrowable"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_BreakOnFindMethod, W("BreakOnFindMethod"), 0, "Breaks in findMethodInternal when it searches for the specified token.")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnFirstPass, W("BreakOnFirstPass"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnHR, W("BreakOnHR"), 0, "Debug.cpp, IfFailxxx use this macro to stop if hr matches ", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_BreakOnInstantiation, W("BreakOnInstantiation"), "Very useful for debugging generic class instantiation.")
CONFIG_STRING_INFO(INTERNAL_BreakOnInteropStubSetup, W("BreakOnInteropStubSetup"), "Throws an assert when marshaling stub for the given method is about to be built.")
CONFIG_STRING_INFO_EX(INTERNAL_BreakOnInteropVTableBuild, W("BreakOnInteropVTableBuild"), "Specifies a type name for which an assert should be thrown when building interop v-table.", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_BreakOnMethodName, W("BreakOnMethodName"), "Very useful for debugging method override placement code.")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnNotify, W("BreakOnNotify"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnRetailAssert, W("BreakOnRetailAssert"), 0, "Used for debugging \"retail\" asserts (fatal errors)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnSecondPass, W("BreakOnSecondPass"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_BreakOnSO, W("BreakOnSO"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_BreakOnStructMarshalSetup, W("BreakOnStructMarshalSetup"), "Throws an assert when field marshalers for the given type with layout are about to be created.")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnUEF, W("BreakOnUEF"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnUncaughtException, W("BreakOnUncaughtException"), 0, "", CLRConfig::REGUTIL_default)

///

/// Debugger
///
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_EnableDiagnostics, W("EnableDiagnostics"), 1, "Allows the debugger and profiler diagnostics to be disabled", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_D__FCE, W("D::FCE"), 0, "Allows an assert when crawling the managed stack for an exception handler", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakIfLocksUnavailable, W("DbgBreakIfLocksUnavailable"), 0, "Allows an assert when the debugger can't take a lock ", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnErr, W("DbgBreakOnErr"), 0, "Allows an assert when we get a failing hresult", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnMapPatchToDJI, W("DbgBreakOnMapPatchToDJI"), 0, "Allows an assert when mapping a patch to an address", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnRawInt3, W("DbgBreakOnRawInt3"), 0, "Allows an assert for test coverage for debug break or other int3 breaks", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnSendBreakpoint, W("DbgBreakOnSendBreakpoint"), 0, "Allows an assert when sending a breakpoint to the right side", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnSetIP, W("DbgBreakOnSetIP"), 0, "Allows an assert when setting the IP", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgCheckInt3, W("DbgCheckInt3"), 0, "Asserts if the debugger explicitly writes int3 instead of calling SetUnmanagedBreakpoint", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_DbgDACAssertOnMismatch, W("DbgDACAssertOnMismatch"), "Allows an assert when the mscordacwks and mscorwks dll versions don't match")
CONFIG_DWORD_INFO_EX(INTERNAL_DbgDACEnableAssert, W("DbgDACEnableAssert"), 0, "Enables extra validity checking in DAC - assumes target isn't corrupt", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_DbgDACSkipVerifyDlls, W("DbgDACSkipVerifyDlls"), 0, "Allows disabling the check to ensure mscordacwks and mscorwks dll versions match", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgDelayHelper, W("DbgDelayHelper"), 0, "Varies the wait in the helper thread startup for testing race between threads", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_DbgDisableDynamicSymsCompat, W("DbgDisableDynamicSymsCompat"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgDisableTargetConsistencyAsserts, W("DbgDisableTargetConsistencyAsserts"), 0, "Allows explicitly testing with corrupt targets", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_DbgEnableMixedModeDebugging, W("DbgEnableMixedModeDebuggingInternalOnly"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgExtraThreads, W("DbgExtraThreads"), 0, "Allows extra unmanaged threads to run and throw debug events for stress testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgExtraThreadsCantStop, W("DbgExtraThreadsCantStop"), 0, "Allows extra unmanaged threads in can't stop region to run and throw debug events for stress testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgExtraThreadsIB, W("DbgExtraThreadsIB"), 0, "Allows extra in-band unmanaged threads to run and throw debug events for stress testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgExtraThreadsOOB, W("DbgExtraThreadsOOB"), 0, "Allows extra out of band unmanaged threads to run and throw debug events for stress testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgFaultInHandleIPCEvent, W("DbgFaultInHandleIPCEvent"), 0, "Allows testing the unhandled event filter", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgInjectFEE, W("DbgInjectFEE"), 0, "Allows injecting a fatal execution error for testing Watson", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgLeakCheck, W("DbgLeakCheck"), 0, "Allows checking for leaked Cordb objects", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgNo2ndChance, W("DbgNo2ndChance"), 0, "Allows breaking on (and catching bogus) 2nd chance exceptions", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgNoDebugger, W("DbgNoDebugger"), 0, "Allows breaking if we don't want to lazily initialize the debugger", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DbgNoForceContinue, W("DbgNoForceContinue"), 1, "Used to force a continue on longhorn", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgNoOpenMDByFile, W("DbgNoOpenMDByFile"), 0, "Allows opening MD by memory for perf testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_DbgOOBinFEEE, W("DbgOOBinFEEE"), 0, "Allows forcing oob breakpoints when a fatal error occurs")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgPackShimPath, W("DbgPackShimPath"), "CoreCLR path to dbgshim.dll - we are trying to figure out if we can remove this")
CONFIG_DWORD_INFO_EX(INTERNAL_DbgPingInterop, W("DbgPingInterop"), 0, "Allows checking for deadlocks in interop debugging", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgRace, W("DbgRace"), 0, "Allows pausing for native debug events to get hijicked", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DbgRedirect, W("DbgRedirect"), 0, "Allows for redirecting the event pipeline", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgRedirectApplication, W("DbgRedirectApplication"), "Specifies the auxiliary debugger application to launch.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgRedirectAttachCmd, W("DbgRedirectAttachCmd"), "Specifies command parameters for attaching the auxiliary debugger.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgRedirectCommonCmd, W("DbgRedirectCommonCmd"), "Specifies a command line format string for the auxiliary debugger.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgRedirectCreateCmd, W("DbgRedirectCreateCmd"), "Specifies command parameters when creating the auxiliary debugger.")
CONFIG_DWORD_INFO_EX(INTERNAL_DbgShortcutCanary, W("DbgShortcutCanary"), 0, "Allows a way to force canary to fail to be able to test failure paths", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgSkipMEOnStep, W("DbgSkipMEOnStep"), 0, "Turns off MethodEnter checks", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgSkipVerCheck, W("DbgSkipVerCheck"), 0, "Allows different RS and LS versions (for servicing work)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgTC, W("DbgTC"), 0, "Allows checking boundary compression for offset mappings", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgTransportFaultInject, W("DbgTransportFaultInject"), 0, "Allows injecting a fault for testing the debug transport", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_DbgTransportLog, W("DbgTransportLog"), "Turns on logging for the debug transport")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_DbgTransportLogClass, W("DbgTransportLogClass"), "Mask to control what is logged in DbgTransportLog")
RETAIL_CONFIG_STRING_INFO_EX(UNSUPPORTED_DbgTransportProxyAddress, W("DbgTransportProxyAddress"), "Allows specifying the transport proxy address", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgTrapOnSkip, W("DbgTrapOnSkip"), 0, "Allows breaking when we skip a breakpoint", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgWaitTimeout, W("DbgWaitTimeout"), 1, "Specifies the timeout value for waits", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DbgWFDETimeout, W("DbgWFDETimeout"), 25, "Specifies the timeout value for wait when waiting for a debug event", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_RaiseExceptionOnAssert, W("RaiseExceptionOnAssert"), 0, "Raise a first chance (if set to 1) or second chance (if set to 2) exception on asserts.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DebugBreakOnAssert, W("DebugBreakOnAssert"), 0, "If DACCESS_COMPILE is defined, break on asserts.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DebugBreakOnVerificationFailure, W("DebugBreakOnVerificationFailure"), 0, "Halts the jit on verification failure", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_DebuggerBreakPoint, W("DebuggerBreakPoint"), "Allows counting various debug events", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_DebugVerify, W("DebugVerify"), "Control for tracing in peverify", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_EncApplyChanges, W("EncApplyChanges"), 0, "Allows breaking when ApplyEditAndContinue is called")
CONFIG_DWORD_INFO_EX(INTERNAL_EnCBreakOnRemapComplete, W("EnCBreakOnRemapComplete"), 0, "Allows breaking after N RemapCompletes", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_EnCBreakOnRemapOpportunity, W("EnCBreakOnRemapOpportunity"), 0, "Allows breaking after N RemapOpportunities", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_EncDumpApplyChanges, W("EncDumpApplyChanges"), 0, "Allows dumping edits in delta metadata and il files")
CONFIG_DWORD_INFO(INTERNAL_EncFixupFieldBreak, W("EncFixupFieldBreak"), 0, "Unlikely that this is used anymore.")
CONFIG_DWORD_INFO(INTERNAL_EncJitUpdatedFunction, W("EncJitUpdatedFunction"), 0, "Allows breaking when an updated function is jitted")
CONFIG_DWORD_INFO(INTERNAL_EnCResolveField, W("EnCResolveField"), 0, "Allows breaking when computing the address of an EnC-added field")
CONFIG_DWORD_INFO(INTERNAL_EncResumeInUpdatedFunction, W("EncResumeInUpdatedFunction"), 0, "Allows breaking when execution resumes in a new EnC version of a function")
CONFIG_DWORD_INFO_EX(INTERNAL_DbgAssertOnDebuggeeDebugBreak, W("DbgAssertOnDebuggeeDebugBreak"), 0, "If non-zero causes the managed-only debugger to assert on unhandled breakpoints in the debuggee", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_DbgDontResumeThreadsOnUnhandledException, W("UNSUPPORTED_DbgDontResumeThreadsOnUnhandledException"), 0, "If non-zero, then don't try to unsuspend threads after continuing a 2nd-chance native exception")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DbgSkipStackCheck, W("DbgSkipStackCheck"), 0, "Skip the stack pointer check during stackwalking", CLRConfig::REGUTIL_default)
#ifdef DACCESS_COMPILE
CONFIG_DWORD_INFO(INTERNAL_DumpGeneration_IntentionallyCorruptDataFromTarget, W("IntentionallyCorruptDataFromTarget"), 0, "Intentionally fakes bad data retrieved from target to try and break dump generation.")
#endif
// Note that Debugging_RequiredVersion is sometimes an 'INTERNAL' knob and sometimes an 'UNSUPPORTED' knob, but we don't change it's name.
CONFIG_DWORD_INFO(UNSUPPORTED_Debugging_RequiredVersion, W("UNSUPPORTED_Debugging_RequiredVersion"), 0, "The lowest ICorDebug version we should attempt to emulate, or 0 for default policy.  Use 2 for CLRv2, 4 for CLRv4, etc.")

#ifdef FEATURE_MINIMETADATA_IN_TRIAGEDUMPS
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MiniMdBufferCapacity, W("MiniMdBufferCapacity"), 64 * 1024, "The max size of the buffer to store mini metadata information for triage- and mini-dumps.")
#endif // FEATURE_MINIMETADATA_IN_TRIAGEDUMPS

CONFIG_DWORD_INFO_EX(INTERNAL_DbgNativeCodeBpBindsAcrossVersions, W("DbgNativeCodeBpBindsAcrossVersions"), 0, "If non-zero causes native breakpoints at offset 0 to bind in all tiered compilation versions of the given method", CLRConfig::REGUTIL_default)

///
/// Diagnostics (internal general-purpose)
///
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_ConditionalContracts, W("ConditionalContracts"), "If ENABLE_CONTRACTS_IMPL is defined, sets whether contracts are conditional. (?)")
CONFIG_DWORD_INFO(INTERNAL_ConsistencyCheck, W("ConsistencyCheck"), 0, "")
CONFIG_DWORD_INFO_EX(INTERNAL_ContinueOnAssert, W("ContinueOnAssert"), 0, "If set, doesn't break on asserts.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_disableStackOverflowProbing, W("disableStackOverflowProbing"), 0, "", CLRConfig::FavorConfigFile)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_InjectFatalError, W("InjectFatalError"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_InjectFault, W("InjectFault"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SuppressChecks, W("SuppressChecks"), "")
#ifdef WIN64EXCEPTIONS
CONFIG_DWORD_INFO(INTERNAL_SuppressLockViolationsOnReentryFromOS, W("SuppressLockViolationsOnReentryFromOS"), 0, "64 bit OOM tests re-enter the CLR via RtlVirtualUnwind.  This indicates whether to suppress resulting locking violations.")
#endif // WIN64EXCEPTIONS

///
/// Diagnostics (unsuported general-purpose)
///
RETAIL_CONFIG_DWORD_INFO(UNSUPORTED_LTTng, W("LTTng"), 1, "If COMPlus_LTTng is set to 0, this will prevent the LTTng library from being loaded at runtime")

///
/// Exception Handling
///
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_AssertOnFailFast, W("AssertOnFailFast"), "")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_legacyCorruptedStateExceptionsPolicy, W("legacyCorruptedStateExceptionsPolicy"), 0, "Enabled Pre-V4 CSE behavior", CLRConfig::FavorConfigFile)
CONFIG_DWORD_INFO_EX(INTERNAL_SuppressLostExceptionTypeAssert, W("SuppressLostExceptionTypeAssert"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_FailFastOnCorruptedStateException, W("FailFastOnCorruptedStateException"), 0, "Failfast if a CSE is encountered", CLRConfig::FavorConfigFile)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_UseEntryPointFilter, W("UseEntryPointFilter"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_Corhost_Swallow_Uncaught_Exceptions, W("Corhost_Swallow_Uncaught_Exceptions"), 0, "", CLRConfig::REGUTIL_default)

///
/// Garbage collector
///
CONFIG_DWORD_INFO(INTERNAL_FastGCCheckStack, W("FastGCCheckStack"), 0, "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_FastGCStress, W("FastGCStress"), "Reduce the number of GCs done by enabling GCStress")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCBreakOnOOM, W("GCBreakOnOOM"), "Does a DebugBreak at the soonest time we detect an OOM")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcConcurrent, W("gcConcurrent"), (DWORD)-1, "Enables/Disables concurrent GC")

#ifdef FEATURE_CONSERVATIVE_GC
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcConservative, W("gcConservative"), 0, "Enables/Disables conservative GC")
#endif
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcServer, W("gcServer"), 0, "Enables server GC")
CONFIG_STRING_INFO(INTERNAL_GcCoverage, W("GcCoverage"), "Specify a method or regular expression of method names to run with GCStress")
CONFIG_STRING_INFO(INTERNAL_SkipGCCoverage, W("SkipGcCoverage"), "Specify a list of assembly names to skip with GC Coverage")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_gcForceCompact, W("gcForceCompact"), "When set to true, always do compacting GC")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCgen0size, W("GCgen0size"), "Specifies the smallest gen0 size")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCGen0MaxBudget, W("GCGen0MaxBudget"), "Specifies the largest gen0 allocation budget")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GCStressMix, W("GCStressMix"), 0, "Specifies whether the GC mix mode is enabled or not")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GCStressStep, W("GCStressStep"), 1, "Specifies how often StressHeap will actually do a GC in GCStressMix mode")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GCStressMaxFGCsPerBGC, W("GCStressMaxFGCsPerBGC"), ~0U, "Specifies how many FGCs will occur during one BGC in GCStressMix mode")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StatsUpdatePeriod, W("StatsUpdatePeriod"), 60, "Specifies the interval, in seconds, at which to update the statistics")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_SuspendTimeLog, W("SuspendTimeLog"), "Specifies the name of the log file for suspension statistics")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_GCMixLog, W("GCMixLog"), "Specifies the name of the log file for GC mix statistics")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_GCLatencyMode, W("GCLatencyMode"), "Specifies the GC latency mode - batch, interactive or low latency (note that the same thing can be specified via API which is the supported way)")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCLatencyLevel, W("GCLatencyLevel"), 1, "Specifies the GC latency level that you want to optimize for")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCConfigLogEnabled, W("GCConfigLogEnabled"), 0, "Specifies if you want to turn on config logging in GC")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCLogEnabled, W("GCLogEnabled"), 0, "Specifies if you want to turn on logging in GC")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_GCLogFile, W("GCLogFile"), "Specifies the name of the GC log file")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_GCConfigLogFile, W("GCConfigLogFile"), "Specifies the name of the GC config log file")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCLogFileSize, W("GCLogFileSize"), 0, "Specifies the GC log file size")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCCompactRatio, W("GCCompactRatio"), 0, "Specifies the ratio compacting GCs vs sweeping ")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_GCPollType, W("GCPollType"), "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCRetainVM, W("GCRetainVM"), 0, "When set we put the segments that should be deleted on a standby list (instead of releasing them back to the OS) which will be considered to satisfy new segment requests (note that the same thing can be specified via API which is the supported way)")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCSegmentSize, W("GCSegmentSize"), "Specifies the managed heap segment size")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCLOHThreshold, W("GCLOHThreshold"), 0, "Specifies the size that will make objects go on LOH")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCLOHCompact, W("GCLOHCompact"), "Specifies the LOH compaction mode")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_gcAllowVeryLargeObjects, W("gcAllowVeryLargeObjects"), 1, "Allow allocation of 2GB+ objects on GC heap")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_GCStress, W("GCStress"), 0, "Trigger GCs at regular intervals", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_GcStressOnDirectCalls, W("GcStressOnDirectCalls"), 0, "Whether to trigger a GC on direct calls", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCStressStart, W("GCStressStart"), 0, "Start GCStress after N stress GCs have been attempted")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GCStressStartAtJit, W("GCStressStartAtJit"), 0, "Start GCStress after N items are jitted")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_gcTrimCommitOnLowMemory, W("gcTrimCommitOnLowMemory"), "When set we trim the committed space more aggressively for the ephemeral seg. This is used for running many instances of server processes where they want to keep as little memory committed as possible")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BGCSpinCount, W("BGCSpinCount"), 140, "Specifies the bgc spin count")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BGCSpin, W("BGCSpin"), 2, "Specifies the bgc spin time")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_HeapVerify, W("HeapVerify"), "When set verifies the integrity of the managed heap on entry and exit of each GC")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_SetupGcCoverage, W("SetupGcCoverage"), "This doesn't appear to be a config flag", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCNumaAware, W("GCNumaAware"), 1, "Specifies if to enable GC NUMA aware")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCCpuGroup, W("GCCpuGroup"), 0, "Specifies if to enable GC to support CPU groups")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCHeapCount, W("GCHeapCount"), 0, "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCNoAffinitize, W("GCNoAffinitize"), 0, "")
// this config is only in effect if the process is not running in multiple CPU groups.
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_GCHeapAffinitizeMask, W("GCHeapAffinitizeMask"), "Specifies processor mask for Server GC threads")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCProvModeStress, W("GCProvModeStress"), 0, "Stress the provisional modes")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCHighMemPercent, W("GCHighMemPercent"), 0, "Specifies the percent for GC to consider as high memory")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_GCName, W("GCName"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_GCHeapHardLimit, W("GCHeapHardLimit"), "Specifies the maximum commit size for the GC heap")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_GCHeapHardLimitPercent, W("GCHeapHardLimitPercent"), "Specifies the GC heap usage as a percentage of the total memory")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_GCHeapAffinitizeRanges, W("GCHeapAffinitizeRanges"), "Specifies list of processors for Server GC threads. The format is a comma separated list of processor numbers or ranges of processor numbers. Example: 1,3,5,7-9,12")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_GCLargePages, W("GCLargePages"), "Specifies whether large pages should be used when a heap hard limit is set")

///
/// IBC
///
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_ConvertIbcData, W("ConvertIbcData"), 1, "Converts between v1 and v2 IBC data", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_DisableHotCold, W("DisableHotCold"), "Master hot/cold splitting switch in Jit64")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DisableIBC, W("DisableIBC"), 0, "Disables the use of IBC data", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_UseIBCFile, W("UseIBCFile"), 0, "", CLRConfig::REGUTIL_default)


///
/// JIT
///
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_JitAlignLoops, W("JitAlignLoops"), "Aligns loop targets to 8 byte boundaries")
CONFIG_DWORD_INFO_EX(INTERNAL_JitBreakEmit, W("JitBreakEmit"), (DWORD)-1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitDebuggable, W("JitDebuggable"), "")
#if !defined(DEBUG) && !defined(_DEBUG)
#define INTERNAL_JitEnableNoWayAssert_Default 0
#else
#define INTERNAL_JitEnableNoWayAssert_Default 1
#endif
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_JitEnableNoWayAssert, W("JitEnableNoWayAssert"), INTERNAL_JitEnableNoWayAssert_Default, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_JitFramed, W("JitFramed"), "Forces EBP frames")
CONFIG_DWORD_INFO_EX(INTERNAL_JitGCStress, W("JitGCStress"), 0, "GC stress mode for jit", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_JitHeartbeat, W("JitHeartbeat"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_JitHelperLogging, W("JitHelperLogging"), 0, "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_JITMinOpts, W("JITMinOpts"), "Forces MinOpts")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_JitName, W("JitName"), "Primary Jit to use")
#if defined(ALLOW_SXS_JIT)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_AltJitName, W("AltJitName"), "Alternative Jit to use, will fall back to primary jit.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_AltJit, W("AltJit"), "Enables AltJit and selectively limits it to the specified methods.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_AltJitExcludeAssemblies, W("AltJitExcludeAssemblies"), "Do not use AltJit on this semicolon-delimited list of assemblies.", CLRConfig::REGUTIL_default)
#endif // defined(ALLOW_SXS_JIT)

#if defined(FEATURE_STACK_SAMPLING)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StackSamplingEnabled, W("StackSamplingEnabled"), 0, "Is stack sampling based tracking of evolving hot methods enabled.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StackSamplingAfter, W("StackSamplingAfter"), 0, "When to start sampling (for some sort of app steady state), i.e., initial delay for sampling start in milliseconds.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StackSamplingEvery, W("StackSamplingEvery"), 100, "How frequent should thread stacks be sampled in milliseconds.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StackSamplingNumMethods, W("StackSamplingNumMethods"), 32, "Number of evolving methods to track as hot and JIT them in the background at a given point of execution.")
#endif // defined(FEATURE_JIT_SAMPLING)

#if defined(ALLOW_SXS_JIT_NGEN)
RETAIL_CONFIG_STRING_INFO_EX(INTERNAL_AltJitNgen, W("AltJitNgen"), "Enables AltJit for NGEN and selectively limits it to the specified methods.", CLRConfig::REGUTIL_default)
#endif // defined(ALLOW_SXS_JIT_NGEN)

RETAIL_CONFIG_DWORD_INFO(EXTERNAL_JitHostMaxSlabCache, W("JitHostMaxSlabCache"), 0x1000000, "Sets jit host max slab cache size, 16MB default")

RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_JitOptimizeType, W("JitOptimizeType"), "")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_JitPrintInlinedMethods, W("JitPrintInlinedMethods"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_JitTelemetry, W("JitTelemetry"), 1, "If non-zero, gather JIT telemetry data")
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitTimeLogFile, W("JitTimeLogFile"), "If set, gather JIT throughput data and write to this file.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitTimeLogCsv, W("JitTimeLogCsv"), "If set, gather JIT throughput data and write to a CSV file. This mode must be used in internal retail builds.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitFuncInfoLogFile, W("JitFuncInfoLogFile"), "If set, gather JIT function info and write to this file.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitVerificationDisable, W("JitVerificationDisable"), "")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitLockWrite, W("JitLockWrite"), 0, "Force all volatile writes to be 'locked'")
CONFIG_STRING_INFO_EX(INTERNAL_TailCallMax, W("TailCallMax"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_TailCallOpt, W("TailCallOpt"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TailCallLoopOpt, W("TailCallLoopOpt"), 1, "Convert recursive tail calls to loops")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Jit_NetFx40PInvokeStackResilience, W("NetFx40_PInvokeStackResilience"), (DWORD)-1, "Makes P/Invoke resilient against mismatched signature and calling convention (significant perf penalty).")

// AltJitAssertOnNYI should be 0 on targets where JIT is under development or bring up stage, so as to facilitate fallback to main JIT on hitting a NYI.
#if defined(_TARGET_X86_)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 0, "Controls the AltJit behavior of NYI stuff")
#else
RETAIL_CONFIG_DWORD_INFO(INTERNAL_AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 1, "Controls the AltJit behavior of NYI stuff")
#endif
CONFIG_DWORD_INFO_EX(INTERNAL_JitLargeBranches, W("JitLargeBranches"), 0, "Force using the largest conditional branch format", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_JitRegisterFP, W("JitRegisterFP"), 3, "Control FP enregistration", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitELTHookEnabled, W("JitELTHookEnabled"), 0, "On ARM, setting this will emit Enter/Leave/TailCall callbacks")
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_JitMemStats, W("JitMemStats"), 0, "Display JIT memory usage statistics", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitVNMapSelBudget, W("JitVNMapSelBudget"), 100, "Max # of MapSelect's considered for a particular top-level invocation.")
#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_) || defined(_TARGET_ARM64_)
#define EXTERNAL_FeatureSIMD_Default 1
#else // !(defined(_TARGET_AMD64_) || defined(_TARGET_X86_) || defined(_TARGET_ARM64_))
#define EXTERNAL_FeatureSIMD_Default 0
#endif // !(defined(_TARGET_AMD64_) || defined(_TARGET_X86_) || defined(_TARGET_ARM64_))
#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
#define EXTERNAL_JitEnableAVX_Default 1
#else // !(defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
#define EXTERNAL_JitEnableAVX_Default 0
#endif // !(defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_FeatureSIMD, W("FeatureSIMD"), EXTERNAL_FeatureSIMD_Default, "Enable SIMD intrinsics recognition in System.Numerics.dll and/or System.Numerics.Vectors.dll", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_SIMD16ByteOnly, W("SIMD16ByteOnly"), 0, "Limit maximum SIMD vector length to 16 bytes (used by x64_arm64_altjit)")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_EnableAVX, W("EnableAVX"), EXTERNAL_JitEnableAVX_Default, "Enable AVX instruction set for wide operations as default", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_TrackDynamicMethodDebugInfo, W("TrackDynamicMethodDebugInfo"), 0, "Specifies whether debug info should be generated and tracked for dynamic methods", CLRConfig::REGUTIL_default)

#ifdef FEATURE_MULTICOREJIT

RETAIL_CONFIG_STRING_INFO(INTERNAL_MultiCoreJitProfile, W("MultiCoreJitProfile"), "If set, use the file to store/control multi-core JIT.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MultiCoreJitProfileWriteDelay, W("MultiCoreJitProfileWriteDelay"), 12, "Set the delay after which the multi-core JIT profile will be written to disk.")

#endif

#ifdef FEATURE_INTERPRETER
///
/// Interpreter
///
RETAIL_CONFIG_STRING_INFO_EX(INTERNAL_Interpret, W("Interpret"), "Selectively uses the interpreter to execute the specified methods", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(INTERNAL_InterpretExclude, W("InterpretExclude"), "Excludes the specified methods from the set selected by 'Interpret'", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterMethHashMin, W("InterpreterMethHashMin"), 0, "Only interpret methods selected by 'Interpret' whose hash is at least this value. or after nth")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterMethHashMax, W("InterpreterMethHashMax"), UINT32_MAX, "If non-zero, only interpret methods selected by 'Interpret' whose hash is at most this value")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterStubMin, W("InterpreterStubMin"), 0, "Only interpret methods selected by 'Interpret' whose stub num is at least this value.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterStubMax, W("InterpreterStubMax"), UINT32_MAX, "If non-zero, only interpret methods selected by 'Interpret' whose stub number is at most this value.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterJITThreshold, W("InterpreterJITThreshold"), 10, "The number of times a method should be interpreted before being JITted")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterDoLoopMethods, W("InterpreterDoLoopMethods"), 0, "If set, don't check for loops, start by interpreting *all* methods")
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_InterpreterUseCaching, W("InterpreterUseCaching"), 1, "If non-zero, use the caching mechanism.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_InterpreterLooseRules, W("InterpreterLooseRules"), 1, "If non-zero, allow ECMA spec violations required by managed C++.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterPrintPostMortem, W("InterpreterPrintPostMortem"), 0, "Prints summary information about the execution to the console")
RETAIL_CONFIG_STRING_INFO_EX(INTERNAL_InterpreterLogFile, W("InterpreterLogFile"), "If non-null, append interpreter logging to this file, else use stdout", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DumpInterpreterStubs, W("DumpInterpreterStubs"), 0, "Prints all interpreter stubs that are created to the console")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterEntries, W("TraceInterpreterEntries"), 0, "Logs entries to interpreted methods to the console")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterIL, W("TraceInterpreterIL"), 0, "Logs individual instructions of interpreted methods to the console")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterOstack, W("TraceInterpreterOstack"), 0, "Logs operand stack after each IL instruction of interpreted methods to the console")
CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterVerbose, W("TraceInterpreterVerbose"), 0, "Logs interpreter progress with detailed messages to the console")
CONFIG_DWORD_INFO(INTERNAL_TraceInterpreterJITTransition, W("TraceInterpreterJITTransition"), 0, "Logs when the interpreter determines a method should be JITted")
#endif
// The JIT queries this ConfigDWORD but it doesn't know if FEATURE_INTERPRETER is enabled
RETAIL_CONFIG_DWORD_INFO(INTERNAL_InterpreterFallback, W("InterpreterFallback"), 0, "Fallback to the interpreter when the JIT compiler fails")

///
/// Loader
///
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_APIThreadStress, W("APIThreadStress"), "Used to test Loader for race conditions")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_ForceLog, W("ForceLog"), "Fusion flag to enforce assembly binding log. Heavily used and documented in MSDN and BLOGS.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_CoreClrBinderLog, W("CoreClrBinderLog"), "Debug flag that enabled detailed log for new binder (similar to stress logging).")
RETAIL_CONFIG_STRING_INFO(INTERNAL_WinMDPath, W("WinMDPath"), "Path for Windows WinMD files")

///
/// Loader heap
///
CONFIG_DWORD_INFO_EX(INTERNAL_LoaderHeapCallTracing, W("LoaderHeapCallTracing"), 0, "Loader heap troubleshooting", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_CodeHeapReserveForJumpStubs, W("CodeHeapReserveForJumpStubs"), 1, "Percentage of code heap to reserve for jump stubs")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NGenReserveForJumpStubs, W("NGenReserveForJumpStubs"), 0, "Percentage of ngen image size to reserve for jump stubs")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_BreakOnOutOfMemoryWithinRange, W("BreakOnOutOfMemoryWithinRange"), 0, "Break before out of memory within range exception is thrown")

///
/// Log
///
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogEnable, W("LogEnable"), "Turns on the traditional CLR log.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogFacility,  W("LogFacility"),  "Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogFacility2, W("LogFacility2"), "Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_logFatalError, W("logFatalError"), 1, "Specifies whether EventReporter logs fatal errors in the Windows event log.")
CONFIG_STRING_INFO_EX(INTERNAL_LogFile, W("LogFile"), "Specifies a file name for the CLR log.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogFileAppend, W("LogFileAppend"), "Specifies whether to append to or replace the CLR log file.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogFlushFile, W("LogFlushFile"), "Specifies whether to flush the CLR log file on each write.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_LogLevel, W("LogLevel"), "4=10 msgs, 9=1000000, 10=everything")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_LogPath, W("LogPath"), "?Fusion debug log path.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogToConsole, W("LogToConsole"), "Writes the CLR log to console.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogToDebugger, W("LogToDebugger"), "Writes the CLR log to debugger (OutputDebugStringA).")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogToFile, W("LogToFile"), "Writes the CLR log to a file.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogWithPid, W("LogWithPid"), "Appends pid to filename for the CLR log.")

///
/// MetaData
///
CONFIG_DWORD_INFO_EX(INTERNAL_MD_ApplyDeltaBreak, W("MD_ApplyDeltaBreak"), 0, "ASSERT when applying EnC", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_AssertOnBadImageFormat, W("AssertOnBadImageFormat"), "ASSERT when invalid MD read")
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_MD_DeltaCheck, W("MD_DeltaCheck"), 1, "Some checks of GUID when applying EnC (?)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_EncDelta, W("MD_EncDelta"), 0, "Forces EnC Delta format in MD (?)", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_MD_ForceNoColDesSharing, W("MD_ForceNoColDesSharing"), 0, "Don't know - the only usage I could find is #if 0 (?)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_KeepKnownCA, W("MD_KeepKnownCA"), 0, "Something with known CAs (?)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_MiniMDBreak, W("MD_MiniMDBreak"), 0, "ASSERT when creating CMiniMdRw class", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_PreSaveBreak, W("MD_PreSaveBreak"), 0, "ASSERT when calling CMiniMdRw::PreSave", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_RegMetaBreak, W("MD_RegMetaBreak"), 0, "ASSERT when creating RegMeta class", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_RegMetaDump, W("MD_RegMetaDump"), 0, "Dump MD in 4 functions (?)", CLRConfig::REGUTIL_default)
// MetaData - Desktop-only
CONFIG_DWORD_INFO_EX(INTERNAL_MD_WinMD_Disable, W("MD_WinMD_Disable"), 0, "Never activate the WinMD import adapter", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_WinMD_AssertOnIllegalUsage, W("MD_WinMD_AssertOnIllegalUsage"), 0, "ASSERT if a WinMD import adapter detects a tool incompatibility", CLRConfig::REGUTIL_default)

// Metadata - mscordbi only - this flag is only intended to mitigate potential issues in bug fix 458597.
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_MD_PreserveDebuggerMetadataMemory, W("MD_PreserveDebuggerMetadataMemory"), 0, "Save all versions of metadata memory in the debugger when debuggee metadata is updated", CLRConfig::REGUTIL_default)

///
/// Spinning heuristics
///
// Note that these only take effect once the runtime has been started; prior to that the values hardcoded in g_SpinConstants (vars.cpp) are used
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinInitialDuration, W("SpinInitialDuration"), 0x32, "Hex value specifying the first spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinBackoffFactor, W("SpinBackoffFactor"), 0x3, "Hex value specifying the growth of each successive spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinLimitProcCap, W("SpinLimitProcCap"), 0xFFFFFFFF, "Hex value specifying the largest value of NumProcs to use when calculating the maximum spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinLimitProcFactor, W("SpinLimitProcFactor"), 0x4E20, "Hex value specifying the multiplier on NumProcs to use when calculating the maximum spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinLimitConstant, W("SpinLimitConstant"), 0x0, "Hex value specifying the constant to add when calculating the maximum spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinRetryCount, W("SpinRetryCount"), 0xA, "Hex value specifying the number of times the entire spin process is repeated (when applicable)", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_Monitor_SpinCount, W("Monitor_SpinCount"), 0x1e, "Hex value specifying the maximum number of spin iterations Monitor may perform upon contention on acquiring the lock before waiting.", EEConfig_default)

///
/// Native Binder
///

CONFIG_DWORD_INFO(INTERNAL_NgenBind_ZapForbid,             W("NgenBind_ZapForbid"), 0, "Assert if an assembly succeeds in binding to a native image")
CONFIG_STRING_INFO(INTERNAL_NgenBind_ZapForbidExcludeList, W("NgenBind_ZapForbidExcludeList"), "")
CONFIG_STRING_INFO(INTERNAL_NgenBind_ZapForbidList,        W("NgenBind_ZapForbidList"), "")

CONFIG_DWORD_INFO_EX(INTERNAL_SymDiffDump, W("SymDiffDump"), 0, "Used to create the map file while binding the assembly. Used by SemanticDiffer", CLRConfig::REGUTIL_default)

///
/// NGEN
///
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_NGen_JitName, W("NGen_JitName"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGenFramed, W("NGenFramed"), (DWORD)-1, "Same as JitFramed, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NGenOnlyOneMethod, W("NGenOnlyOneMethod"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenOrder, W("NgenOrder"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_partialNGenStress, W("partialNGenStress"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_ZapDoNothing, W("ZapDoNothing"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenForceFailureMask, W("NgenForceFailureMask"), (DWORD)-1, "Bitmask used to control which locations will check and raise the failure (defaults to bits: -1)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenForceFailureCount, W("NgenForceFailureCount"), 0, "If set to >0 and we have IBC data we will force a failure after we reference an IBC data item <value> times", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenForceFailureKind, W("NgenForceFailureKind"), 1, "If set to 1, We will throw a TypeLoad exception; If set to 2, We will cause an A/V", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_NGenEnableCreatePdb, W("NGenEnableCreatePdb"), 0, "If set to >0 ngen.exe displays help on, recognizes createpdb in the command line")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NGenSimulateDiskFull, W("NGenSimulateDiskFull"), 0, "If set to 1, ngen will throw a Disk full exception in ZapWriter.cpp:Save()")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_PartialNGen, W("PartialNGen"), (DWORD)-1, "Generate partial NGen images")

CONFIG_DWORD_INFO(INTERNAL_NoASLRForNgen, W("NoASLRForNgen"), 0, "Turn off IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE bit in generated ngen images. Makes nidump output repeatable from run to run.")

#ifdef CROSSGEN_COMPILE
RETAIL_CONFIG_DWORD_INFO(INTERNAL_CrossGenAssumeInputSigned, W("CrossGenAssumeInputSigned"), 1, "CrossGen should assume that its input assemblies will be signed before deployment")
#endif

///
/// Performance
///
RETAIL_CONFIG_STRING_INFO(EXTERNAL_PerformanceScenario, W("performanceScenario"), "Activates a set of workload-specific default values for performance settings")

///
/// Profiling API / ETW
///
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_COR_ENABLE_PROFILING, W("COR_ENABLE_PROFILING"), 0, "Flag to indicate whether profiling should be enabled for the currently running process.", CLRConfig::DontPrependCOMPlus_ | CLRConfig::IgnoreConfigFiles)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_COR_PROFILER, W("COR_PROFILER"), "Specifies GUID of profiler to load into currently running process", CLRConfig::DontPrependCOMPlus_)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_COR_PROFILER_PATH, W("COR_PROFILER_PATH"), "Specifies the path to the DLL of profiler to load into currently running process", CLRConfig::DontPrependCOMPlus_)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_COR_PROFILER_PATH_32, W("COR_PROFILER_PATH_32"), "Specifies the path to the DLL of profiler to load into currently running 32 bits process", CLRConfig::DontPrependCOMPlus_)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_COR_PROFILER_PATH_64, W("COR_PROFILER_PATH_64"), "Specifies the path to the DLL of profiler to load into currently running 64 bits process", CLRConfig::DontPrependCOMPlus_)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_CORECLR_ENABLE_PROFILING, W("CORECLR_ENABLE_PROFILING"), 0, "CoreCLR only: Flag to indicate whether profiling should be enabled for the currently running process.", CLRConfig::DontPrependCOMPlus_ | CLRConfig::IgnoreConfigFiles)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER, W("CORECLR_PROFILER"), "CoreCLR only: Specifies GUID of profiler to load into currently running process", CLRConfig::DontPrependCOMPlus_)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER_PATH, W("CORECLR_PROFILER_PATH"), "CoreCLR only: Specifies the path to the DLL of profiler to load into currently running process", CLRConfig::DontPrependCOMPlus_)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER_PATH_32, W("CORECLR_PROFILER_PATH_32"), "CoreCLR only: Specifies the path to the DLL of profiler to load into currently running 32 process", CLRConfig::DontPrependCOMPlus_)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CORECLR_PROFILER_PATH_64, W("CORECLR_PROFILER_PATH_64"), "CoreCLR only: Specifies the path to the DLL of profiler to load into currently running 64 process", CLRConfig::DontPrependCOMPlus_)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_ProfAPI_ProfilerCompatibilitySetting, W("ProfAPI_ProfilerCompatibilitySetting"), "Specifies the profiler loading policy (the default is not to load a V2 profiler in V4)", CLRConfig::REGUTIL_default | CLRConfig::TrimWhiteSpaceFromStringValue)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_AttachThreadAlwaysOn, W("AttachThreadAlwaysOn"), "Forces profapi attach thread to be created on startup, instead of on-demand.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_MsBetweenAttachCheck, W("MsBetweenAttachCheck"), 500, "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPIMaxWaitForTriggerMs, W("ProfAPIMaxWaitForTriggerMs"), 5*60*1000, "Timeout in ms for profilee to wait for each blocking operation performed by trigger app.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_DetachMinSleepMs, W("ProfAPI_DetachMinSleepMs"), 0, "The minimum time, in milliseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_DetachMaxSleepMs, W("ProfAPI_DetachMaxSleepMs"), 0, "The maximum time, in milliseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_RejitOnAttach, W("ProfApi_RejitOnAttach"), 1, "Enables the ability for profilers to rejit methods on attach.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_InliningTracking, W("ProfApi_InliningTracking"), 1, "Enables the runtime's tracking of inlining for profiler ReJIT.")
CONFIG_DWORD_INFO(INTERNAL_ProfAPI_EnableRejitDiagnostics, W("ProfAPI_EnableRejitDiagnostics"), 0, "Enable extra dumping to stdout of rejit structures")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_AttachProfilerMinTimeoutInMs, W("ProfAPI_AttachProfilerMinTimeoutInMs"), 10*1000, "Timeout in ms for the minimum time out value of AttachProfiler")
CONFIG_DWORD_INFO(INTERNAL_ProfAPIFault, W("ProfAPIFault"), 0, "Test-only bitmask to inject various types of faults in the profapi code")
CONFIG_DWORD_INFO(INTERNAL_TestOnlyAllowedEventMask, W("TestOnlyAllowedEventMask"), 0, "Test-only bitmask to allow profiler tests to override CLR enforcement of COR_PRF_ALLOWABLE_AFTER_ATTACH and COR_PRF_MONITOR_IMMUTABLE")
CONFIG_DWORD_INFO(INTERNAL_TestOnlyEnableICorProfilerInfo, W("ProfAPI_TestOnlyEnableICorProfilerInfo"), 0, "Test-only flag to allow attaching profiler tests to call ICorProfilerInfo interface, which would otherwise be disallowed for attaching profilers")
CONFIG_DWORD_INFO(INTERNAL_TestOnlyEnableObjectAllocatedHook, W("TestOnlyEnableObjectAllocatedHook"), 0, "Test-only flag that forces CLR to initialize on startup as if ObjectAllocated callback were requested, to enable post-attach ObjectAllocated functionality.")
CONFIG_DWORD_INFO(INTERNAL_TestOnlyEnableSlowELTHooks, W("TestOnlyEnableSlowELTHooks"), 0, "Test-only flag that forces CLR to initialize on startup as if slow-ELT were requested, to enable post-attach ELT functionality.")

RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_PreVistaETWEnabled, W("ETWEnabled"), 0, "This flag is used on OSes < Vista to enable/disable ETW. It is disabled by default", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_VistaAndAboveETWEnabled, W("ETWEnabled"), 1, "This flag is used on OSes >= Vista to enable/disable ETW. It is enabled by default", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(UNSUPPORTED_ETW_ObjectAllocationEventsPerTypePerSec, W("ETW_ObjectAllocationEventsPerTypePerSec"), "Desired number of GCSampledObjectAllocation ETW events to be logged per type per second.  If 0, then the default built in to the implementation for the enabled event (e.g., High, Low), will be used.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_ProfAPI_ValidateNGENInstrumentation, W("ProfAPI_ValidateNGENInstrumentation"), 0, "This flag enables additional validations when using the IMetaDataEmit APIs for NGEN'ed images to ensure only supported edits are made.")

#ifdef FEATURE_PERFMAP
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_PerfMapEnabled, W("PerfMapEnabled"), 0, "This flag is used on Linux to enable writing /tmp/perf-$pid.map. It is disabled by default", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_PerfMapIgnoreSignal, W("PerfMapIgnoreSignal"), 0, "When perf map is enabled, this option will configure the specified signal to be accepted and ignored as a marker in the perf logs.  It is disabled by default", CLRConfig::REGUTIL_default)
#endif

RETAIL_CONFIG_STRING_INFO(EXTERNAL_StartupDelayMS, W("StartupDelayMS"), "")

///
/// Stress
///
CONFIG_DWORD_INFO_EX(INTERNAL_StressCOMCall, W("StressCOMCall"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_StressLog, W("StressLog"), "Turns on the stress log.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_ForceEnc, W("ForceEnc"), "Forces Edit and Continue to be on for all eligible modules.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_StressLogSize, W("StressLogSize"), "Stress log size in bytes per thread.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_StressOn, W("StressOn"), "Enables the STRESS_ASSERT macro that stops runtime quickly (to prevent the clr state from changing significantly before breaking)")
CONFIG_DWORD_INFO_EX(INTERNAL_stressSynchronized, W("stressSynchronized"), 0, "Unknown if or where this is used; unless a test is specifically depending on this, it can be removed.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_StressThreadCount, W("StressThreadCount"), "")

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

///
/// Threadpool
///
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_ForceMinWorkerThreads, W("ThreadPool_ForceMinWorkerThreads"), 0, "Overrides the MinThreads setting for the ThreadPool worker pool")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_ForceMaxWorkerThreads, W("ThreadPool_ForceMaxWorkerThreads"), 0, "Overrides the MaxThreads setting for the ThreadPool worker pool")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_DisableStarvationDetection, W("ThreadPool_DisableStarvationDetection"), 0, "Disables the ThreadPool feature that forces new threads to be added when workitems run for too long")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_DebugBreakOnWorkerStarvation, W("ThreadPool_DebugBreakOnWorkerStarvation"), 0, "Breaks into the debugger if the ThreadPool detects work queue starvation")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_EnableWorkerTracking, W("ThreadPool_EnableWorkerTracking"), 0, "Enables extra expensive tracking of how many workers threads are working simultaneously")
#ifdef _TARGET_ARM64_
// Spinning scheme is currently different on ARM64, see CLRLifoSemaphore::Wait(DWORD, UINT32, UINT32)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_UnfairSemaphoreSpinLimit, W("ThreadPool_UnfairSemaphoreSpinLimit"), 0x32, "Maximum number of spins per processor a thread pool worker thread performs before waiting for work")
#else // !_TARGET_ARM64_
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_UnfairSemaphoreSpinLimit, W("ThreadPool_UnfairSemaphoreSpinLimit"), 0x46, "Maximum number of spins a thread pool worker thread performs before waiting for work")
#endif // _TARGET_ARM64_
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Thread_UseAllCpuGroups, W("Thread_UseAllCpuGroups"), 0, "Specifies if to automatically distribute thread across CPU Groups")

CONFIG_DWORD_INFO(INTERNAL_ThreadpoolTickCountAdjustment, W("ThreadpoolTickCountAdjustment"), 0, "")

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
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TieredCompilation, W("TieredCompilation"), 1, "Enables tiered compilation")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TC_QuickJit, W("TC_QuickJit"), 1, "For methods that would be jitted, enable using quick JIT when appropriate.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TC_QuickJitForLoops, W("TC_QuickJitForLoops"), 0, "When quick JIT is enabled, quick JIT may also be used for methods that contain loops.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_CallCountThreshold, W("TC_CallCountThreshold"), 30, "Number of times a method must be called in tier 0 after which it is promoted to the next tier.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_CallCountingDelayMs, W("TC_CallCountingDelayMs"), 100, "A perpetual delay in milliseconds that is applied call counting in tier 0 and jitting at higher tiers, while there is startup-like activity.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_DelaySingleProcMultiplier, W("TC_DelaySingleProcMultiplier"), 10, "Multiplier for TC_CallCountingDelayMs that is applied on a single-processor machine or when the process is affinitized to a single processor.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_TC_CallCounting, W("TC_CallCounting"), 1, "Enabled by default (only activates when TieredCompilation is also enabled). If disabled immediately backpatches prestub, and likely prevents any promotion to higher tiers")
#endif

///
/// Entry point slot backpatch
///
#ifndef CROSSGEN_COMPILE
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BackpatchEntryPointSlots, W("BackpatchEntryPointSlots"), 1, "Indicates whether to enable entry point slot backpatching, for instance to avoid making virtual calls through a precode and instead to patch virtual slots for a method when its entry point changes.")
#endif

///
/// TypeLoader
///
CONFIG_DWORD_INFO(INTERNAL_TypeLoader_InjectInterfaceDuplicates, W("INTERNAL_TypeLoader_InjectInterfaceDuplicates"), 0, "Injects duplicates in interface map for all types.")

///
/// Virtual call stubs
///
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubCollideMonoPct, W("VirtualCallStubCollideMonoPct"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubCollideWritePct, W("VirtualCallStubCollideWritePct"), 100, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubDumpLogCounter, W("VirtualCallStubDumpLogCounter"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubDumpLogIncr, W("VirtualCallStubDumpLogIncr"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_VirtualCallStubLogging, W("VirtualCallStubLogging"), 0, "Worth keeping, but should be moved into \"#ifdef STUB_LOGGING\" blocks. This goes for most (or all) of the stub logging infrastructure.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubMissCount, W("VirtualCallStubMissCount"), 100, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubResetCacheCounter, W("VirtualCallStubResetCacheCounter"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubResetCacheIncr, W("VirtualCallStubResetCacheIncr"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)

///
/// Watson
///
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DisableWatsonForManagedExceptions, W("DisableWatsonForManagedExceptions"), 0, "Disable Watson and debugger launching for managed exceptions")

///
/// Zap
///
RETAIL_CONFIG_STRING_INFO_EX(INTERNAL_ZapBBInstr, W("ZapBBInstr"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO(EXTERNAL_ZapBBInstrDir, W("ZapBBInstrDir"), "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ZapDisable, W("ZapDisable"), 0, "")
CONFIG_STRING_INFO_EX(INTERNAL_ZapExclude, W("ZapExclude"), "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_ZapOnly, W("ZapOnly"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_ZapRequire, W("ZapRequire"), "")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_ZapRequireExcludeList, W("ZapRequireExcludeList"), "")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_ZapRequireList, W("ZapRequireList"), "")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_ZapSet, W("ZapSet"), "", CLRConfig::REGUTIL_default)

#ifdef FEATURE_LAZY_COW_PAGES
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ZapLazyCOWPagesEnabled, W("ZapLazyCOWPagesEnabled"), 1, "");
CONFIG_DWORD_INFO(INTERNAL_DebugAssertOnMissedCOWPage, W("DebugAssertOnMissedCOWPage"), 1, "");
#endif //FEATURE_LAZY_COW_PAGES

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
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_ExposeExceptionsInCOM, W("ExposeExceptionsInCOM"), "")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_PInvokeInline, W("PInvokeInline"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_InteropValidatePinnedObjects, W("InteropValidatePinnedObjects"), 0, "After returning from a managed-to-unmanaged interop call, validate GC heap around objects pinned by IL stubs.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_InteropLogArguments, W("InteropLogArguments"), 0, "Log all pinned arguments passed to an interop call")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_LogCCWRefCountChange, W("LogCCWRefCountChange"), "Outputs debug information and calls LogCCWRefCountChange_BREAKPOINT when AddRef or Release is called on a CCW.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EnableRCWCleanupOnSTAShutdown, W("EnableRCWCleanupOnSTAShutdown"), 0, "Performs RCW cleanup when STA shutdown is detected using IInitializeSpy in classic processes.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_LocalWinMDPath, W("LocalWinMDPath"), "Additional path to probe for WinMD files in if a WinRT type is not resolved using the standard paths.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_AllowDComReflection, W("AllowDComReflection"), 0, "Allows out of process DCOM clients to marshal blocked reflection types.")

//
// EventPipe
//
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EnableEventPipe, W("EnableEventPipe"), 0, "Enable/disable event pipe.  Non-zero values enable tracing.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_EventPipeOutputPath, W("EventPipeOutputPath"), "The full path excluding file name for the trace file that will be written when COMPlus_EnableEventPipe=1")
RETAIL_CONFIG_STRING_INFO(INTERNAL_EventPipeConfig, W("EventPipeConfig"), "Configuration for EventPipe.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EventPipeRundown, W("EventPipeRundown"), 1, "Enable/disable eventpipe rundown.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EventPipeCircularMB, W("EventPipeCircularMB"), 1024, "The EventPipe circular buffer size in megabytes.")

#ifdef FEATURE_GDBJIT
///
/// GDBJIT
///
CONFIG_STRING_INFO(INTERNAL_GDBJitElfDump, W("GDBJitElfDump"), "Dump ELF for specified method")
#ifdef FEATURE_GDBJIT_FRAME
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GDBJitEmitDebugFrame, W("GDBJitEmitDebugFrame"), TRUE, "Enable .debug_frame generation")
#endif
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
CONFIG_DWORD_INFO_EX(INTERNAL_ActivatePatchSkip, W("ActivatePatchSkip"), 0, "Allows an assert when ActivatePatchSkip is called", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_AlwaysUseMetadataInterfaceMapLayout, W("AlwaysUseMetadataInterfaceMapLayout"), "Used for debugging generic interface map layout.")
CONFIG_DWORD_INFO(INTERNAL_AssertOnUnneededThis, W("AssertOnUnneededThis"), 0, "While the ConfigDWORD is unnecessary, the contained ASSERT should be kept. This may result in some work tracking down violating MethodDescCallSites.")
CONFIG_DWORD_INFO_EX(INTERNAL_AssertStacktrace, W("AssertStacktrace"), 1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_clearNativeImageStress, W("clearNativeImageStress"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_CPUFamily, W("CPUFamily"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_CPUFeatures, W("CPUFeatures"), "")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_DisableConfigCache, W("DisableConfigCache"), 0, "Used to disable the \"probabilistic\" config cache, which walks through the appropriate config registry keys on init and probabilistically keeps track of which exist.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_DisableStackwalkCache, W("DisableStackwalkCache"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_DoubleArrayToLargeObjectHeap, W("DoubleArrayToLargeObjectHeap"), "Controls double[] placement")
CONFIG_DWORD_INFO(INTERNAL_DumpConfiguration, W("DumpConfiguration"), 0, "Dumps runtime properties of xml configuration files to the log.")
CONFIG_STRING_INFO(INTERNAL_DumpOnClassLoad, W("DumpOnClassLoad"), "Dumps information about loaded class to log.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_ExpandAllOnLoad, W("ExpandAllOnLoad"), "")
CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_ForcedRuntime, W("ForcedRuntime"), "Verify version of CLR loaded")
CONFIG_DWORD_INFO_EX(INTERNAL_ForceRelocs, W("ForceRelocs"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_GenerateLongJumpDispatchStubRatio, W("GenerateLongJumpDispatchStubRatio"), "Useful for testing VSD on AMD64")
CONFIG_DWORD_INFO_EX(INTERNAL_HashStack, W("HashStack"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_HostManagerConfig, W("HostManagerConfig"), (DWORD)-1, "")
CONFIG_DWORD_INFO(INTERNAL_HostTestThreadAbort, W("HostTestThreadAbort"), 0, "")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_IgnoreDllMainReturn, W("IgnoreDllMainReturn"), 0, "Don't check the return value of DllMain if this is set", CLRConfig::ConfigFile_ApplicationFirst)
CONFIG_STRING_INFO(INTERNAL_InvokeHalt, W("InvokeHalt"), "Throws an assert when the given method is invoked through reflection.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_legacyNullReferenceExceptionPolicy, W("legacyNullReferenceExceptionPolicy"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_legacyUnhandledExceptionPolicy, W("legacyUnhandledExceptionPolicy"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_MaxStackDepth, W("MaxStackDepth"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_MaxStubUnwindInfoSegmentSize, W("MaxStubUnwindInfoSegmentSize"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_MaxThreadRecord, W("MaxThreadRecord"), "")
CONFIG_DWORD_INFO(INTERNAL_MessageDebugOut, W("MessageDebugOut"), 0, "")
CONFIG_DWORD_INFO_EX(INTERNAL_MscorsnLogging, W("MscorsnLogging"), 0, "Enables strong name logging", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NativeImageRequire, W("NativeImageRequire"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NestedEhOom, W("NestedEhOom"), 0, "", CLRConfig::REGUTIL_default)
#define INTERNAL_NoGuiOnAssert_Default 1
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_NoGuiOnAssert, W("NoGuiOnAssert"), INTERNAL_NoGuiOnAssert_Default, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NoProcedureSplitting, W("NoProcedureSplitting"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NoStringInterning, W("NoStringInterning"), 1, "Disallows string interning. I see no value in it anymore.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_NotifyBadAppCfg, W("NotifyBadAppCfg"), "Whether to show a message box for bad application config file.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_PauseOnLoad, W("PauseOnLoad"), "Stops in SystemDomain::init. I think it can be removed.")
CONFIG_DWORD_INFO(INTERNAL_PerfAllocsSizeThreshold, W("PerfAllocsSizeThreshold"), 0x3FFFFFFF, "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
CONFIG_DWORD_INFO(INTERNAL_PerfNumAllocsThreshold, W("PerfNumAllocsThreshold"), 0x3FFFFFFF, "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
CONFIG_STRING_INFO(INTERNAL_PerfTypesToLog, W("PerfTypesToLog"), "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Prepopulate1, W("Prepopulate1"), 1, "")
CONFIG_STRING_INFO(INTERNAL_PrestubGC, W("PrestubGC"), "")
CONFIG_STRING_INFO(INTERNAL_PrestubHalt, W("PrestubHalt"), "")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_RestrictedGCStressExe, W("RestrictedGCStressExe"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_ReturnSourceTypeForTesting, W("ReturnSourceTypeForTesting"), 0, "Allows returning the (internal only) source type of an IL to Native mapping for debugging purposes", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_RSStressLog, W("RSStressLog"), 0, "Allows turning on logging for RS startup", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SaveThreadInfo, W("SaveThreadInfo"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SaveThreadInfoMask, W("SaveThreadInfoMask"), "")
CONFIG_DWORD_INFO(INTERNAL_SBDumpOnNewIndex, W("SBDumpOnNewIndex"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
CONFIG_DWORD_INFO(INTERNAL_SBDumpOnResize, W("SBDumpOnResize"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
CONFIG_DWORD_INFO(INTERNAL_SBDumpStyle, W("SBDumpStyle"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(UNSUPPORTED_ShimDatabaseVersion, W("ShimDatabaseVersion"), "Force using shim database version in registry")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_SleepOnExit, W("SleepOnExit"), 0, "Used for lrak detection. I'd say deprecated by umdh.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_StubLinkerUnwindInfoVerificationOn, W("StubLinkerUnwindInfoVerificationOn"), "")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_SuccessExit, W("SuccessExit"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_SymbolReadingPolicy, W("SymbolReadingPolicy"), "Specifies when PDBs may be read")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TestDataConsistency, W("TestDataConsistency"), FALSE, "Allows ensuring the left side is not holding locks (and may thus be in an inconsistent state) when inspection occurs")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_ThreadGuardPages, W("ThreadGuardPages"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_Timeline, W("Timeline"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_TotalStressLogSize, W("TotalStressLogSize"), "Total stress log size in bytes.")

#ifdef _DEBUG
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_TraceIUnknown, W("TraceIUnknown"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_TraceWrap, W("TraceWrap"), "")
#endif

RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_TURNOFFDEBUGINFO, W("TURNOFFDEBUGINFO"), "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_UseMethodDataCache, W("UseMethodDataCache"), FALSE, "Used during feature development; may now be removed.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_UseParentMethodData, W("UseParentMethodData"), TRUE, "Used during feature development; may now be removed.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_VerifierOff, W("VerifierOff"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_VerifyAllOnLoad, W("VerifyAllOnLoad"), "")
// **
// PLEASE MOVE ANY CONFIG SWITCH YOU OWN OUT OF THIS SECTION INTO A CATEGORY ABOVE
//
// DO NOT ADD ANY MORE CONFIG SWITCHES TO THIS SECTION!
// **
//---------------------------------------------------------------------------------------
