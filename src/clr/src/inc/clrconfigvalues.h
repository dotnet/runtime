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

// 
// AppDomain
// 
CONFIG_DWORD_INFO(INTERNAL_ADBreakOnCannotUnload, W("ADBreakOnCannotUnload"), 0, "Used to troubleshoot failures to unload appdomain (e.g. someone sitting in unmanged code). In some cases by the time we throw the appropriate exception the thread has moved from the offending call. This setting allows in an instrumented build to stop exactly at the right moment.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_AddRejitNops, W("AddRejitNops"), "Control for the profiler rejit feature infrastructure")
CONFIG_DWORD_INFO(INTERNAL_ADDumpSB, W("ADDumpSB"), 0, "Not used")
CONFIG_DWORD_INFO(INTERNAL_ADForceSB, W("ADForceSB"), 0, "Forces sync block creation for all objects")
CONFIG_DWORD_INFO(INTERNAL_ADLogMemory, W("ADLogMemory"), 0, "Superseded by test hooks")
CONFIG_DWORD_INFO(INTERNAL_ADTakeDHSnapShot, W("ADTakeDHSnapShot"), 0, "Superseded by test hooks")
CONFIG_DWORD_INFO(INTERNAL_ADTakeSnapShot, W("ADTakeSnapShot"), 0, "Superseded by test hooks")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_EnableFullDebug, W("EnableFullDebug"), "Heavy-weight checking for AD boundary violations (AD leaks)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DisableMSIPeek, W("DisableMSIPeek"), 0, "Disable MSI check in Fusion")
CONFIG_DWORD_INFO(INTERNAL_MsiPeekForbid, W("MsiPeekForbid"), 0, "Assert on MSI calls")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ADULazyMemoryRelease, W("ADULazyMemoryRelease"), 1, "On by default. Turned off in cases when people try to catch memory leaks, in which case AD unload should be immediately followed by GC)")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_ADURetryCount, W("ADURetryCount"), "Controls timeout of AD unload. Used for workarounds when machine is too slow, there are network issues etc.")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_LEGACY_APPDOMAIN_MANAGER_ASM, W("APPDOMAIN_MANAGER_ASM"), "Legacy method to specify the assembly containing the AppDomainManager to use for the default domain", CLRConfig::DontPrependCOMPlus_ | CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_LEGACY_APPDOMAIN_MANAGER_TYPE, W("APPDOMAIN_MANAGER_TYPE"), "LegacyMethod to specify the type containing the AppDomainManager to use for the default domain", CLRConfig::DontPrependCOMPlus_  | CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_AppDomainManagerAsm, W("appDomainManagerAssembly"), "Config file switch to specify the assembly for the default AppDomainManager.", CLRConfig::IgnoreEnv | CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_AppDomainManagerType, W("appDomainManagerType"), "Config file switch to specify the type for the default AppDomainManager.", CLRConfig::IgnoreEnv | CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_AppDomainAgilityChecked, W("AppDomainAgilityChecked"), "Used to detect AD boundary violations (AD leaks)")
CONFIG_DWORD_INFO(INTERNAL_AppDomainNoUnload, W("AppDomainNoUnload"), 0, "Not used")
RETAIL_CONFIG_STRING_INFO_EX(INTERNAL_TargetFrameworkMoniker, W("TargetFrameworkMoniker"), "Allows the test team to specify what TargetFrameworkMoniker to use.", CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU | CLRConfig::IgnoreConfigFiles | CLRConfig::IgnoreWindowsQuirkDB)
RETAIL_CONFIG_STRING_INFO_EX(INTERNAL_AppContextSwitchOverrides, W("AppContextSwitchOverrides"), "Allows default switch values defined in AppContext to be overwritten by values in the Config", CLRConfig::IgnoreEnv | CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU | CLRConfig::IgnoreWindowsQuirkDB | CLRConfig::ConfigFile_ApplicationFirst)

// For the proposal and discussion on why finalizers are not run on shutdown by default anymore in CoreCLR, see the API review:
// https://github.com/dotnet/corefx/issues/5205
#define DEFAULT_FinalizeOnShutdown (0)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_FinalizeOnShutdown, W("FinalizeOnShutdown"), DEFAULT_FinalizeOnShutdown, "When enabled, on shutdown, blocks all user threads and calls finalizers for all finalizable objects, including live objects")

//
// ARM
//
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_ARMEnabled, W("ARMEnabled"), (DWORD)0, "Set it to 1 to enable ARM")

//
// Jit Pitching
//
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchEnabled, W("JitPitchEnabled"), (DWORD)0, "Set it to 1 to enable Jit Pitching")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchMemThreshold, W("JitPitchMemThreshold"), (DWORD)0, "Do Jit Pitching when code heap usage is larger than this (in bytes)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchMethodSizeThreshold, W("JitPitchMethodSizeThreshold"), (DWORD)0, "Do Jit Pitching for methods whose native code size larger than this (in bytes)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchTimeInterval, W("JitPitchTimeInterval"), (DWORD)0, "Time interval between Jit Pitchings in ms")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchPrintStat, W("JitPitchPrintStat"), (DWORD)0, "Print statistics about Jit Pitching")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchMinVal, W("JitPitchMinVal"), (DWORD)0, "Do Jit Pitching if the value of the inner counter greater than this value (for debugging purpose only)")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitPitchMaxVal, W("JitPitchMaxVal"), (DWORD)0xffffffff, "Do Jit Pitching the value of the inner counter less then this value (for debuggin purpose only)")

//
// Assembly Loader
//
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_DesignerNamespaceResolutionEnabled, W("designerNamespaceResolution"), FALSE, "Set it to 1 to enable DesignerNamespaceResolve event for WinRT types", CLRConfig::IgnoreEnv | CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU | CLRConfig::FavorConfigFile)
CONFIG_DWORD_INFO_EX(INTERNAL_GetAssemblyIfLoadedIgnoreRidMap, W("GetAssemblyIfLoadedIgnoreRidMap"), 0, "Used to force loader to ignore assemblies cached in the rid-map", CLRConfig::REGUTIL_default)

// 
// BCL
// 
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_BCLCorrectnessWarnings, W("BCLCorrectnessWarnings"), "Flag a few common correctness bugs in the library with additional runtime checks.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_BCLPerfWarnings, W("BCLPerfWarnings"), "Flag some performance-related problems via asserts when people mis-use the library.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TimeSpan_LegacyFormatMode, W("TimeSpan_LegacyFormatMode"), 0, "Flag to enable System.TimeSpan legacy (.NET Framework 3.5 and earlier) ToString behavior.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_CompatSortNLSVersion, W("CompatSortNLSVersion"), 0, "Determines the version of desired sorting behavior for AppCompat.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_NetFx45_CultureAwareComparerGetHashCode_LongStrings, W("NetFx45_CultureAwareComparerGetHashCode_LongStrings"), 0, "Opt in to use the new (as of v4.5) constant space hash algorithm for strings")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Resources_DisableUserPreferredFallback, W("DisableUserPreferredFallback"), 0, "Resource lookups should be dependent only on the CurrentUICulture, not a user-defined list of preferred languages nor the OS preferred fallback language.  Intended to avoid falling back to a right-to-left language, which is undisplayable in console apps.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_RelativeBindForResources , W("relativeBindForResources"), 0, "Enables probing for satellite assemblies only next to the parent assembly")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_NetFx45_LegacyManagedDeflateStream, W("NetFx45_LegacyManagedDeflateStream"), 0, "Flag to enable legacy managed implementation of the deflater used by System.IO.Compression.DeflateStream.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_DateTime_NetFX35ParseMode, W("DateTime_NetFX35ParseMode"), 0, "Flag to enable the .NET 3.5 System.DateTime Token Replacement Policy")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ThrowUnobservedTaskExceptions, W("ThrowUnobservedTaskExceptions"), 0, "Flag to propagate unobserved task exceptions on the finalizer thread.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_DateTime_NetFX40AmPmParseAdjustment, W("EnableAmPmParseAdjustment"), 0, "Flag to enable the .NET 4.0 DateTimeParse to correctly parse AM/PM cases")
#ifdef FEATURE_RANDOMIZED_STRING_HASHING
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_UseRandomizedStringHashAlgorithm, W("UseRandomizedStringHashAlgorithm"), 0, "Flag to use a string hashing algorithm who's behavior differs between AppDomains")
#endif // FEATURE_RANDOMIZED_STRING_HASHING

// 
// Conditional breakpoints
// 
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_BreakOnBadExit, W("BreakOnBadExit"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_BreakOnClassBuild, W("BreakOnClassBuild"), "Very useful for debugging class layout code.")
CONFIG_STRING_INFO(INTERNAL_BreakOnClassLoad, W("BreakOnClassLoad"), "Very useful for debugging class loading code.")
CONFIG_STRING_INFO(INTERNAL_BreakOnComToClrNativeInfoInit, W("BreakOnComToClrNativeInfoInit"), "Throws an assert when native information about a COM -> CLR call are about to be gathered.")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnDebugBreak, W("BreakOnDebugBreak"), 0, "allows an assert in debug builds when a user break is hit", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnDILoad, W("BreakOnDILoad"), 0, "allows an assert when the DI is loaded", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnDumpToken, W("BreakOnDumpToken"), 0xffffffff, "Breaks when using internal logging on a particular token value.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_BreakOnEELoad, W("BreakOnEELoad"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_BreakOnEEShutdown, W("BreakOnEEShutdown"), 0, "")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnExceptionInGetThrowable, W("BreakOnExceptionInGetThrowable"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BreakOnFinalizeTimeOut, W("BreakOnFinalizeTimeOut"), 0, "Triggers a debug break on the finalizer thread when it has exceeded the maximum wait time")
CONFIG_DWORD_INFO(INTERNAL_BreakOnFindMethod, W("BreakOnFindMethod"), 0, "Breaks in findMethodInternal when it searches for the specified token.")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnFirstPass, W("BreakOnFirstPass"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnHR, W("BreakOnHR"), 0, "Debug.cpp, IfFailxxx use this macro to stop if hr matches ", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_BreakOnInstantiation, W("BreakOnInstantiation"), "Very useful for debugging generic class instantiation.")
CONFIG_STRING_INFO(INTERNAL_BreakOnInteropStubSetup, W("BreakOnInteropStubSetup"), "Throws an assert when marshaling stub for the given method is about to be built.")
CONFIG_STRING_INFO_EX(INTERNAL_BreakOnInteropVTableBuild, W("BreakOnInteropVTableBuild"), "Specifies a type name for which an assert should be thrown when building interop v-table.", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_BreakOnMethodName, W("BreakOnMethodName"), "Very useful for debugging method override placement code.")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_BreakOnNGenRegistryAccessCount, W("BreakOnNGenRegistryAccessCount"), 0, "Breaks on the Nth' root store write", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnNotify, W("BreakOnNotify"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnRetailAssert, W("BreakOnRetailAssert"), 0, "Used for debugging \"retail\" asserts (fatal errors)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnSecondPass, W("BreakOnSecondPass"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_BreakOnSO, W("BreakOnSO"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_BreakOnStructMarshalSetup, W("BreakOnStructMarshalSetup"), "Throws an assert when field marshalers for the given type with layout are about to be created.")
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnUEF, W("BreakOnUEF"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_BreakOnUncaughtException, W("BreakOnUncaughtException"), 0, "", CLRConfig::REGUTIL_default)

// 
// CSE
// 
CONFIG_STRING_INFO_EX(INTERNAL_CseBinarySearch, W("CseBinarySearch"), "Sets internal jit constants for CSE", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_CseMax, W("CseMax"), "Sets internal jit constants for CSE", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(UNSUPPORTED_CseOn, W("CseOn"), "Internal Jit control of CSE", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_CseStats, W("CseStats"), "Collects CSE statistics", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_D__FCE, W("D::FCE"), 0, "allows an assert when crawling the managed stack for an exception handler", CLRConfig::REGUTIL_default)

// 
// Debugger
// 
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakIfLocksUnavailable, W("DbgBreakIfLocksUnavailable"), 0, "allows an assert when the debugger can't take a lock ", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnErr, W("DbgBreakOnErr"), 0, "allows an assert when we get a failing hresult", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnMapPatchToDJI, W("DbgBreakOnMapPatchToDJI"), 0, "allows an assert when mapping a patch to an address", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnRawInt3, W("DbgBreakOnRawInt3"), 0, "allows an assert for test coverage for debug break or other int3 breaks", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnSendBreakpoint, W("DbgBreakOnSendBreakpoint"), 0, "allows an assert when sending a breakpoint to the right side", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgBreakOnSetIP, W("DbgBreakOnSetIP"), 0, "allows an assert when setting the IP", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgCheckInt3, W("DbgCheckInt3"), 0, "asserts if the debugger explicitly writes int3 instead of calling SetUnmanagedBreakpoint", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_DbgDACAssertOnMismatch, W("DbgDACAssertOnMismatch"), "allows an assert when the mscordacwks and mscorwks dll versions don't match")
CONFIG_DWORD_INFO_EX(INTERNAL_DbgDACEnableAssert, W("DbgDACEnableAssert"), 0, "Enables extra validity checking in DAC - assumes target isn't corrupt", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_DbgDACSkipVerifyDlls, W("DbgDACSkipVerifyDlls"), 0, "allows disabling the check to ensure mscordacwks and mscorwks dll versions match", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgDelayHelper, W("DbgDelayHelper"), 0, "varies the wait in the helper thread startup for testing race between threads", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_DbgDisableDynamicSymsCompat, W("DbgDisableDynamicSymsCompat"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgDisableTargetConsistencyAsserts, W("DbgDisableTargetConsistencyAsserts"), 0, "allows explicitly testing with corrupt targets", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_DbgEnableMixedModeDebugging, W("DbgEnableMixedModeDebuggingInternalOnly"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgExtraThreads, W("DbgExtraThreads"), 0, "allows extra unmanaged threads to run and throw debug events for stress testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgExtraThreadsCantStop, W("DbgExtraThreadsCantStop"), 0, "allows extra unmanaged threads in can't stop region to run and throw debug events for stress testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgExtraThreadsIB, W("DbgExtraThreadsIB"), 0, "allows extra in-band unmanaged threads to run and throw debug events for stress testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgExtraThreadsOOB, W("DbgExtraThreadsOOB"), 0, "allows extra out of band unmanaged threads to run and throw debug events for stress testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgFaultInHandleIPCEvent, W("DbgFaultInHandleIPCEvent"), 0, "allows testing the unhandled event filter", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgInjectFEE, W("DbgInjectFEE"), 0, "allows injecting a fatal execution error for testing Watson", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgLeakCheck, W("DbgLeakCheck"), 0, "allows checking for leaked Cordb objects", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgNo2ndChance, W("DbgNo2ndChance"), 0, "allows breaking on (and catching bogus) 2nd chance exceptions", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgNoDebugger, W("DbgNoDebugger"), 0, "allows breaking if we don't want to lazily initialize the debugger", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DbgNoForceContinue, W("DbgNoForceContinue"), 1, "used to force a continue on longhorn", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgNoOpenMDByFile, W("DbgNoOpenMDByFile"), 0, "allows opening MD by memory for perf testing", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_DbgOOBinFEEE, W("DbgOOBinFEEE"), 0, "allows forcing oob breakpoints when a fatal error occurs")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgPackShimPath, W("DbgPackShimPath"), "CoreCLR path to dbgshim.dll - we are trying to figure out if we can remove this")
CONFIG_DWORD_INFO_EX(INTERNAL_DbgPingInterop, W("DbgPingInterop"), 0, "allows checking for deadlocks in interop debugging", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgRace, W("DbgRace"), 0, "allows pausing for native debug events to get hijicked", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DbgRedirect, W("DbgRedirect"), 0, "allows for redirecting the event pipeline", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgRedirectApplication, W("DbgRedirectApplication"), "Specifies the auxillary debugger application to launch.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgRedirectAttachCmd, W("DbgRedirectAttachCmd"), "Specifies command parameters for attaching the auxillary debugger.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgRedirectCommonCmd, W("DbgRedirectCommonCmd"), "Specifies a command line format string for the auxillary debugger.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_DbgRedirectCreateCmd, W("DbgRedirectCreateCmd"), "Specifies command parameters when creating the auxillary debugger.")
CONFIG_DWORD_INFO_EX(INTERNAL_DbgShortcutCanary, W("DbgShortcutCanary"), 0, "allows a way to force canary to fail to be able to test failure paths", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgSkipMEOnStep, W("DbgSkipMEOnStep"), 0, "turns off MethodEnter checks", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgSkipVerCheck, W("DbgSkipVerCheck"), 0, "allows different RS and LS versions (for servicing work)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgTC, W("DbgTC"), 0, "allows checking boundary compression for offset mappings", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgTransportFaultInject, W("DbgTransportFaultInject"), 0, "allows injecting a fault for testing the debug transport", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_DbgTransportLog, W("DbgTransportLog"), "turns on logging for the debug transport")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_DbgTransportLogClass, W("DbgTransportLogClass"), "mask to control what is logged in DbgTransportLog")
RETAIL_CONFIG_STRING_INFO_EX(UNSUPPORTED_DbgTransportProxyAddress, W("DbgTransportProxyAddress"), "allows specifying the transport proxy address", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgTrapOnSkip, W("DbgTrapOnSkip"), 0, "allows breaking when we skip a breakpoint", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DbgWaitTimeout, W("DbgWaitTimeout"), 1, "specifies the timeout value for waits", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DbgWFDETimeout, W("DbgWFDETimeout"), 25, "specifies the timeout value for wait when waiting for a debug event", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_RaiseExceptionOnAssert, W("RaiseExceptionOnAssert"), 0, "Raise a first chance (if set to 1) or second chance (if set to 2) exception on asserts.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DebugBreakOnAssert, W("DebugBreakOnAssert"), 0, "If DACCESS_COMPILE is defined, break on asserts.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_DebugBreakOnVerificationFailure, W("DebugBreakOnVerificationFailure"), 0, "Halts the jit on verification failure", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_DebuggerBreakPoint, W("DebuggerBreakPoint"), "allows counting various debug events", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_DebugVerify, W("DebugVerify"), "Control for tracing in peverify", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_EncApplyChanges, W("EncApplyChanges"), 0, "allows breaking when ApplyEditAndContinue is called")
CONFIG_DWORD_INFO_EX(INTERNAL_EnCBreakOnRemapComplete, W("EnCBreakOnRemapComplete"), 0, "allows breaking after N RemapCompletes", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_EnCBreakOnRemapOpportunity, W("EnCBreakOnRemapOpportunity"), 0, "allows breaking after N RemapOpportunities", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_EncDumpApplyChanges, W("EncDumpApplyChanges"), 0, "allows dumping edits in delta metadata and il files")
CONFIG_DWORD_INFO(INTERNAL_EncFixupFieldBreak, W("EncFixupFieldBreak"), 0, "Unlikely that this is used anymore.")
CONFIG_DWORD_INFO(INTERNAL_EncJitUpdatedFunction, W("EncJitUpdatedFunction"), 0, "allows breaking when an updated function is jitted")
CONFIG_DWORD_INFO(INTERNAL_EnCResolveField, W("EnCResolveField"), 0, "allows breaking when computing the address of an EnC-added field")
CONFIG_DWORD_INFO(INTERNAL_EncResumeInUpdatedFunction, W("EncResumeInUpdatedFunction"), 0, "allows breaking when execution resumes in a new EnC version of a function")
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
//
// Diagnostics (internal general-purpose)
//
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_ConditionalContracts, W("ConditionalContracts"), "?If ENABLE_CONTRACTS_IMPL is defined, sets whether contracts are conditional.")
CONFIG_DWORD_INFO(INTERNAL_ConsistencyCheck, W("ConsistencyCheck"), 0, "")
CONFIG_DWORD_INFO_EX(INTERNAL_ContinueOnAssert, W("ContinueOnAssert"), 0, "If set, doesn't break on asserts.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_disableStackOverflowProbing, W("disableStackOverflowProbing"), 0, "", CLRConfig::FavorConfigFile)
CONFIG_DWORD_INFO(INTERNAL_EnforceEEThreadNotRequiredContracts, W("EnforceEEThreadNotRequiredContracts"), 0, "Indicates whether to enforce EE_THREAD_NOT_REQUIRED contracts (not enforced by default for perf reasons).  Only applicable in dbg/chk builds--EE_THREAD_NOT_REQUIRED contracts never enforced in ret builds.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_InjectFatalError, W("InjectFatalError"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_InjectFault, W("InjectFault"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SuppressChecks, W("SuppressChecks"), "")
#ifdef WIN64EXCEPTIONS
CONFIG_DWORD_INFO(INTERNAL_SuppressLockViolationsOnReentryFromOS, W("SuppressLockViolationsOnReentryFromOS"), 0, "64 bit OOM tests re-enter the CLR via RtlVirtualUnwind.  This indicates whether to suppress resulting locking violations.")
#endif // WIN64EXCEPTIONS
CONFIG_STRING_INFO(INTERNAL_TestHooks, W("TestHooks"), "Used by tests to get test an insight on various CLR workings")


// 
// Exception Handling
// 
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_AssertOnFailFast, W("AssertOnFailFast"), "")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_legacyCorruptedStateExceptionsPolicy, W("legacyCorruptedStateExceptionsPolicy"), 0, "Enabled Pre-V4 CSE behaviour", CLRConfig::FavorConfigFile)
CONFIG_DWORD_INFO_EX(INTERNAL_SuppressLostExceptionTypeAssert, W("SuppressLostExceptionTypeAssert"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_FailFastOnCorruptedStateException, W("FailFastOnCorruptedStateException"), 0, "Failfast if a CSE is encountered", CLRConfig::FavorConfigFile)

// 
// Garbage collector
// 
CONFIG_DWORD_INFO(INTERNAL_FastGCCheckStack, W("FastGCCheckStack"), 0, "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_FastGCStress, W("FastGCStress"), "reduce the number of GCs done by enabling GCStress")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCBreakOnOOM, W("GCBreakOnOOM"), "Does a DebugBreak at the soonest time we detect an OOM")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcConcurrent, W("gcConcurrent"), (DWORD)-1, "Enables/Disables concurrent GC")

#ifdef FEATURE_CONSERVATIVE_GC
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcConservative, W("gcConservative"), 0, "Enables/Disables conservative GC")
#endif
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_gcServer, W("gcServer"), 0, "Enables server GC")
CONFIG_STRING_INFO(INTERNAL_GcCoverage, W("GcCoverage"), "specify a method or regular expression of method names to run with GCStress")
CONFIG_STRING_INFO(INTERNAL_SkipGCCoverage, W("SkipGcCoverage"), "specify a list of assembly names to skip with GC Coverage")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_gcForceCompact, W("gcForceCompact"), "When set to true, always do compacting GC")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCgen0size, W("GCgen0size"), "Specifies the smallest gen0 size")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GCStressMix, W("GCStressMix"), 0, "Specifies whether the GC mix mode is enabled or not")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GCStressStep, W("GCStressStep"), 1, "Specifies how often StressHeap will actually do a GC in GCStressMix mode")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GCStressMaxFGCsPerBGC, W("GCStressMaxFGCsPerBGC"), ~0U, "Specifies how many FGCs will occur during one BGC in GCStressMix mode")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_StatsUpdatePeriod, W("StatsUpdatePeriod"), 60, "Specifies the interval, in seconds, at which to update the statistics")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_SuspendTimeLog, W("SuspendTimeLog"), "Specifies the name of the log file for suspension statistics")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_GCMixLog, W("GCMixLog"), "Specifies the name of the log file for GC mix statistics")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_GCLatencyMode, W("GCLatencyMode"), "Specifies the GC latency mode - batch, interactive or low latency (note that the same thing can be specified via API which is the supported way)")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCConfigLogEnabled, W("GCConfigLogEnabled"), 0, "Specifies if you want to turn on config logging in GC")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCLogEnabled, W("GCLogEnabled"), 0, "Specifies if you want to turn on logging in GC")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_GCLogFile, W("GCLogFile"), "Specifies the name of the GC log file")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_GCConfigLogFile, W("GCConfigLogFile"), "Specifies the name of the GC config log file")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCLogFileSize, W("GCLogFileSize"), 0, "Specifies the GC log file size")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCCompactRatio, W("GCCompactRatio"), 0, "Specifies the ratio compacting GCs vs sweeping ")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_GCPollType, W("GCPollType"), "")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_NewGCCalc, W("NewGCCalc"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCRetainVM, W("GCRetainVM"), 0, "When set we put the segments that should be deleted on a standby list (instead of releasing them back to the OS) which will be considered to satisfy new segment requests (note that the same thing can be specified via API which is the supported way)")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCSegmentSize, W("GCSegmentSize"), "Specifies the managed heap segment size")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_GCLOHCompact, W("GCLOHCompact"), "Specifies the LOH compaction mode")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_gcAllowVeryLargeObjects, W("gcAllowVeryLargeObjects"), 1, "allow allocation of 2GB+ objects on GC heap")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_GCStress, W("GCStress"), 0, "trigger GCs at regular intervals", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_GcStressOnDirectCalls, W("GcStressOnDirectCalls"), 0, "whether to trigger a GC on direct calls", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCStressStart, W("GCStressStart"), 0, "start GCStress after N stress GCs have been attempted")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_GCStressStartAtJit, W("GCStressStartAtJit"), 0, "start GCStress after N items are jitted")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_gcTrimCommitOnLowMemory, W("gcTrimCommitOnLowMemory"), "When set we trim the committed space more aggressively for the ephemeral seg. This is used for running many instances of server processes where they want to keep as little memory committed as possible")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BGCSpinCount, W("BGCSpinCount"), 140, "Specifies the bgc spin count")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_BGCSpin, W("BGCSpin"), 2, "Specifies the bgc spin time")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_HeapVerify, W("HeapVerify"), "When set verifies the integrity of the managed heap on entry and exit of each GC")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_SetupGcCoverage, W("SetupGcCoverage"), "This doesn't appear to be a config flag", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCNumaAware, W("GCNumaAware"), 1, "Specifies if to enable GC NUMA aware")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCCpuGroup, W("GCCpuGroup"), 0, "Specifies if to enable GC to support CPU groups")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCHeapCount, W("GCHeapCount"), 0, "")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_GCNoAffinitize, W("GCNoAffinitize"), 0, "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_GCUseStandalone, W("GCUseStandalone"), 0, "")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_GCStandaloneLocation, W("GCStandaloneLocation"), "")

//
// IBC
// 
CONFIG_STRING_INFO_EX(INTERNAL_IBCPrint, W("IBCPrint"), "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_IBCPrint3, W("IBCPrint3"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_ConvertIbcData, W("ConvertIbcData"), 1, "Converts between v1 and v2 IBC data", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_DisableHotCold, W("DisableHotCold"), "Master hot/cold splitting switch in Jit64")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_DisableIBC, W("DisableIBC"), 0, "Disables the use of IBC data", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_UseIBCFile, W("UseIBCFile"), 0, "", CLRConfig::REGUTIL_default)


// 
// JIT
// 
CONFIG_DWORD_INFO_EX(INTERNAL_DumpJittedMethods, W("DumpJittedMethods"), 0, "Prints all jitted methods to the console", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_Jit64Range, W("Jit64Range"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_JitAlignLoops, W("JitAlignLoops"), "Aligns loop targets to 8 byte boundaries")
CONFIG_DWORD_INFO_EX(INTERNAL_JitCloneLoops, W("JitCloneLoops"), 1, "If 0, don't clone. Otherwise clone loops for optimizations.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitAssertOnMaxRAPasses, W("JitAssertOnMaxRAPasses"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitBreak, W("JitBreak"), "Stops in the importer when compiling a specified method", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitBreakEmit, W("JitBreakEmit"), (DWORD)-1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitBreakEmitOutputInstr, W("JitBreakEmitOutputInstr"), (DWORD)-1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitBreakMorphTree, W("JitBreakMorphTree"), 0xFFFFFFFF, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitBreakOnBadCode, W("JitBreakOnBadCode"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_JitBreakOnUnsafeCode, W("JitBreakOnUnsafeCode"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitCanUseSSE2, W("JitCanUseSSE2"), "")
CONFIG_STRING_INFO_EX(INTERNAL_JitDebugBreak, W("JitDebugBreak"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitDebuggable, W("JitDebuggable"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_JitDefaultFill, W("JitDefaultFill"), 0xDD, "In debug builds, initialize the memory allocated by the nra with this byte.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDirectAlloc, W("JitDirectAlloc"), 0, "", CLRConfig::REGUTIL_default)
#if !defined(DEBUG) && !defined(_DEBUG)
#define INTERNAL_JitEnableNoWayAssert_Default 0
#else
#define INTERNAL_JitEnableNoWayAssert_Default 1
#endif
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_JitEnableNoWayAssert, W("JitEnableNoWayAssert"), INTERNAL_JitEnableNoWayAssert_Default, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitDisasm, W("JitDisasm"), "Dumps disassembly for specified method", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitDoubleAlign, W("JitDoubleAlign"), "")
CONFIG_STRING_INFO_EX(INTERNAL_JitDump, W("JitDump"), "Dumps trees for specified method", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitDumpIR, W("JitDumpIR"), "Dumps trees (in linear IR form) for specified method", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitDumpIRFormat, W("JitDumpIRFormat"), "Comma separated format control for JitDumpIR, values = {types | locals | ssa | valnums | kinds | flags | nodes | nolists | nostmts | noleafs | trees | dataflow}", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitDumpIRPhase, W("JitDumpIRPhase"), "Phase control for JitDumpIR, values = {* | phasename}", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDumpVerboseTrees, W("JitDumpVerboseTrees"), 0, "Enable more verbose tree dumps", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDumpVerboseSsa, W("JitDumpVerboseSsa"), 0, "Produce especially verbose dump output for SSA", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDumpBeforeAfterMorph, W("JitDumpBeforeAfterMorph"), 0, "If 1, display each tree before/after morphing", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitDumpFg, W("JitDumpFg"), "Dumps Xml/Dot Flowgraph for specified method", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitDumpFgDir, W("JitDumpFgDir"), "Directory for Xml/Dot flowgraph dump(s)", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitDumpFgFile, W("JitDumpFgFile"), "Filename for Xml/Dot flowgraph dump(s)", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitDumpFgPhase, W("JitDumpFgPhase"), "Phase-based Xml/Dot flowgraph support. Set to the short name of a phase to see the flowgraph after that phase. Leave unset to dump after COLD-BLK (determine first cold block) or set to * for all phases", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDumpFgDot, W("JitDumpFgDot"), 0, "Set to non-zero to emit Dot instead of Xml Flowgraph dump", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDumpLevel, W("JitDumpLevel"), 1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDumpASCII, W("JitDumpASCII"), 1, "Uses only ASCII characters in tree dumps", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDumpTerseLsra, W("JitDumpTerseLsra"), 1, "Produce terse dump output for LSRA", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDumpToDebugger, W("JitDumpToDebugger"), 0, "Output JitDump output to the debugger", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitEmitPrintRefRegs, W("JitEmitPrintRefRegs"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitExclude, W("JitExclude"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitForceFallback, W("JitForceFallback"), 0, "Set to non-zero to test NOWAY assert by forcing a retry", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoForceFallback, W("JitNoForceFallback"), 0, "Set to non-zero to prevent NOWAY assert testing. Overrides COMPlus_JitForceFallback and JIT stress flags.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitExpensiveDebugCheckLevel, W("JitExpensiveDebugCheckLevel"), 0, "Level indicates how much checking beyond the default to do in debug builds (currently 1-2)", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitForceProcedureSplitting, W("JitForceProcedureSplitting"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitForceVer, W("JitForceVer"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_JitFramed, W("JitFramed"), "Forces EBP frames")
CONFIG_DWORD_INFO_EX(INTERNAL_JitFullyInt, W("JitFullyInt"), 0, "Forces Fully interruptable code", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitGCChecks, W("JitGCChecks"), "")
CONFIG_STRING_INFO_EX(INTERNAL_JitGCDump, W("JitGCDump"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_JitGCInfoLogging, W("JitGCInfoLogging"), 0, "If true, prints GCInfo-related output to standard output.")
CONFIG_DWORD_INFO_EX(INTERNAL_JitGCStress, W("JitGCStress"), 0, "GC stress mode for jit", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitHalt, W("JitHalt"), "Emits break instruction into jitted code", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitHashHalt, W("JitHashHalt"), (DWORD)-1, "Same as JitHalt, but for a method hash", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitHashBreak, W("JitHashBreak"), (DWORD)-1, "Same as JitBreak, but for a method hash", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitHashDump, W("JitHashDump"), (DWORD)-1, "Same as JitDump, but for a method hash", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitHashDumpIR, W("JitHashDumpIR"), (DWORD)-1, "Same as JitDumpIR, but for a method hash", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_JitHeartbeat, W("JitHeartbeat"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_JitHelperLogging, W("JitHelperLogging"), 0, "")
CONFIG_STRING_INFO_EX(INTERNAL_JitImportBreak, W("JitImportBreak"), "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitInclude, W("JitInclude"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_JitInlineAdditionalMultiplier, W("JitInlineAdditionalMultiplier"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_JitInlineSIMDMultiplier, W("JitInlineSIMDMultiplier"), 3, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitInlinePrintStats, W("JitInlinePrintStats"), (DWORD)0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JITInlineSize, W("JITInlineSize"), "")
CONFIG_STRING_INFO_EX(INTERNAL_JitLateDisasm, W("JitLateDisasm"), "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JITLateDisasmTo, W("JITLateDisasmTo"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JITMaxTempAssert, W("JITMaxTempAssert"), 1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitMaxUncheckedOffset, W("JitMaxUncheckedOffset"), (DWORD)8, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_JITMinOpts, W("JITMinOpts"), "Forces MinOpts")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JITMinOptsBbCount, W("JITMinOptsBbCount"), "Internal jit control of MinOpts")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JITMinOptsCodeSize, W("JITMinOptsCodeSize"), "Internal jit control of MinOpts")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JITMinOptsInstrCount, W("JITMinOptsInstrCount"), "Internal jit control of MinOpts")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JITMinOptsLvNumcount, W("JITMinOptsLvNumcount"), "Internal jit control of MinOpts")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JITMinOptsLvRefcount, W("JITMinOptsLvRefcount"), "Internal jit control of MinOpts")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JITBreakOnMinOpts, W("JITBreakOnMinOpts"), "Halt if jit switches to MinOpts")
CONFIG_STRING_INFO_EX(INTERNAL_JITMinOptsName, W("JITMinOptsName"), "Forces MinOpts for a named function", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO(EXTERNAL_JitName, W("JitName"), "Primary Jit to use")
#if defined(ALLOW_SXS_JIT)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_AltJitName, W("AltJitName"), "Alternative Jit to use, will fall back to primary jit.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_AltJit, W("AltJit"), "Enables AltJit and selectively limits it to the specified methods.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_AltJitExcludeAssemblies, W("AltJitExcludeAssemblies"), "Do not use AltJit on this semicolon-delimited list of assemblies.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_AltJitLimit, W("AltJitLimit"), 0, "Max number of functions to use altjit for (decimal)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_RunAltJitCode, W("RunAltJitCode"), 1, "If non-zero, and the compilation succeeds for an AltJit, then use the code. If zero, then we always throw away the generated code and fall back to the default compiler.", CLRConfig::REGUTIL_default)
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
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoCMOV, W("JitNoCMOV"), 0, "", CLRConfig::REGUTIL_default)

CONFIG_STRING_INFO_EX(INTERNAL_JitValNumCSE,  W("JitValNumCSE"),  "Enables ValNum CSE for the specified methods", CLRConfig::REGUTIL_default) 
CONFIG_STRING_INFO_EX(INTERNAL_JitLexicalCSE, W("JitLexicalCSE"), "Enables Lexical CSE for the specified methods", CLRConfig::REGUTIL_default) 
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoCSE, W("JitNoCSE"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoCSE2, W("JitNoCSE2"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoHoist, W("JitNoHoist"), 0, "", CLRConfig::REGUTIL_default)

RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_JitNoInline, W("JitNoInline"), 0, "Disables inlining of all methods", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_JitAggressiveInlining, W("JitAggressiveInlining"), 0, "Aggressive inlining of all methods", CLRConfig::REGUTIL_default)

CONFIG_STRING_INFO_EX(INTERNAL_JitNoProcedureSplitting, W("JitNoProcedureSplitting"), "Disallow procedure splitting for specified methods", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitNoProcedureSplittingEH, W("JitNoProcedureSplittingEH"), "Disallow procedure splitting for specified methods if they contain exception handling", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoRegLoc, W("JitNoRegLoc"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoStructPromotion, W("JitNoStructPromotion"), 0, "Disables struct promotion in Jit32", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoUnroll, W("JitNoUnroll"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitNoMemoryBarriers, W("JitNoMemoryBarriers"), 0, "If 1, don't generate memory barriers", CLRConfig::REGUTIL_default)
#ifdef FEATURE_ENABLE_NO_RANGE_CHECKS
RETAIL_CONFIG_DWORD_INFO_EX(PRIVATE_JitNoRangeChks, W("JitNoRngChks"), 0, "If 1, don't generate range checks", CLRConfig::REGUTIL_default)
#endif
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_JitOptimizeType, W("JitOptimizeType"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_JitOrder, W("JitOrder"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDiffableDasm, W("JitDiffableDasm"), 0, "Make the disassembly diff-able", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitSlowDebugChecksEnabled, W("JitSlowDebugChecksEnabled"), 1, "Turn on slow debug checks", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JITPInvokeCheckEnabled, W("JITPInvokeCheckEnabled"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_JITPInvokeEnabled, W("JITPInvokeEnabled"), 1, "")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_JitPrintInlinedMethods, W("JitPrintInlinedMethods"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_JitTelemetry, W("JitTelemetry"), 1, "If non-zero, gather JIT telemetry data")
CONFIG_STRING_INFO_EX(INTERNAL_JitRange, W("JitRange"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JITRequired, W("JITRequired"), (unsigned)-1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JITRoundFloat, W("JITRoundFloat"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_JitSkipArrayBoundCheck, W("JitSkipArrayBoundCheck"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitStackChecks, W("JitStackChecks"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitStackProbes, W("JitStackProbes"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_JitStress, W("JitStress"), 0, "Internal Jit stress mode: 0 = no stress, 2 = all stress, other = vary stress based on a hash of the method and this value", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitStressBBProf, W("JitStressBBProf"), 0, "Internal Jit stress mode", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitStressFP, W("JitStressFP"), 0, "Internal Jit stress mode", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitStressModeNames, W("JitStressModeNames"), "Internal Jit stress mode: stress using the given set of stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitStressModeNamesNot, W("JitStressModeNamesNot"), "Internal Jit stress mode: do NOT stress using the given set of stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitStressOnly, W("JitStressOnly"), "Internal Jit stress mode: stress only the specified method(s)", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_JitStressRange, W("JitStressRange"), "Internal Jit stress mode", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitStressRegs, W("JitStressRegs"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitStressBiasedCSE, W("JitStressBiasedCSE"), 0x101, "Internal Jit stress mode: decimal bias value between (0,100) to perform CSE on a candidate. 100% = All CSEs. 0% = 0 CSE. (> 100) means no stress.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitStrictCheckForNonVirtualCallToVirtualMethod, W("JitStrictCheckForNonVirtualCallToVirtualMethod"), 1, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitTimeLogFile, W("JitTimeLogFile"), "If set, gather JIT throughput data and write to this file.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitTimeLogCsv, W("JitTimeLogCsv"), "If set, gather JIT throughput data and write to a CSV file. This mode must be used in internal retail builds.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_JitFuncInfoLogFile, W("JitFuncInfoLogFile"), "If set, gather JIT function info and write to this file.")
CONFIG_STRING_INFO(INTERNAL_JitUnwindDump, W("JitUnwindDump"), "Dump the unwind codes for the method")
CONFIG_STRING_INFO(INTERNAL_JitEHDump, W("JitEHDump"), "Dump the EH table for the method, as reported to the VM")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_JitVerificationDisable, W("JitVerificationDisable"), "")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitLockWrite, W("JitLockWrite"), 0, "Force all volatile writes to be 'locked'")
CONFIG_STRING_INFO_EX(INTERNAL_TailCallMax, W("TailCallMax"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_TailCallOpt, W("TailCallOpt"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_TailcallStress, W("TailcallStress"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_TailCallLoopOpt, W("TailCallLoopOpt"), 1, "Convert recursive tail calls to loops")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Jit_NetFx40PInvokeStackResilience, W("NetFx40_PInvokeStackResilience"), (DWORD)-1, "Makes P/Invoke resilient against mismatched signature and calling convention (significant perf penalty).")
CONFIG_DWORD_INFO_EX(INTERNAL_JitDoSsa, W("JitDoSsa"), 1, "Perform Static Single Assignment (SSA) numbering on the variables", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDoEarlyProp,   W("JitDoEarlyProp"), 1, "Perform Early Value Propagataion", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDoValueNumber, W("JitDoValueNumber"), 1, "Perform value numbering on method expressions", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDoLoopHoisting, W("JitDoLoopHoisting"), 1, "Perform loop hoisting on loop invariant values", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDoCopyProp, W("JitDoCopyProp"), 1, "Perform copy propagation on variables that appear redundant", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDoAssertionProp, W("JitDoAssertionProp"), 1, "Perform assertion propagation optimization", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDoRangeAnalysis, W("JitDoRangeAnalysis"), 1, "Perform range check analysis", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitSsaStress, W("JitSsaStress"), 0, "Perturb order of processing of blocks in SSA; 0 = no stress; 1 = use method hash; * = supplied value as random hash", CLRConfig::REGUTIL_default)
// AltJitAssertOnNYI should be 0 on targets where JIT is under developement or bring up stage, so as to facilitate fallback to main JIT on hitting a NYI.
#if defined(_TARGET_ARM64_) || defined(_TARGET_X86_)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 0, "Controls the AltJit behavior of NYI stuff")
#else
RETAIL_CONFIG_DWORD_INFO(INTERNAL_AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 1, "Controls the AltJit behavior of NYI stuff")
#endif
CONFIG_DWORD_INFO_EX(INTERNAL_AltJitSkipOnAssert, W("AltJitSkipOnAssert"), 0, "If AltJit hits an assert, fall back to the fallback JIT. Useful in conjunction with COMPlus_ContinueOnAssert=1", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitLargeBranches, W("JitLargeBranches"), 0, "Force using the largest conditional branch format", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitSplitFunctionSize, W("JitSplitFunctionSize"), 0, "On ARM, use this as the maximum function/funclet size for creating function fragments (and creating multiple RUNTIME_FUNCTION entries)", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_JitRegisterFP, W("JitRegisterFP"), 3, "Control FP enregistration", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitELTHookEnabled, W("JitELTHookEnabled"), 0, "On ARM, setting this will emit Enter/Leave/TailCall callbacks")
CONFIG_DWORD_INFO_EX(INTERNAL_JitComponentUnitTests, W("JitComponentUnitTests"), 0, "Run JIT component unit tests", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_JitMemStats, W("JitMemStats"), 0, "Display JIT memory usage statistics", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitLoopHoistStats, W("JitLoopHoistStats"), 0, "Display JIT loop hoisting statistics", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_JitDebugLogLoopCloning, W("JitDebugLogLoopCloning"), 0, "In debug builds log places where loop cloning optimizations are performed on the fast path.", CLRConfig::REGUTIL_default);
CONFIG_DWORD_INFO_EX(INTERNAL_JitVNMapSelLimit, W("JitVNMapSelLimit"), 0, "If non-zero, assert if # of VNF_MapSelect applications considered reaches this", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_JitVNMapSelBudget, W("JitVNMapSelBudget"), 100, "Max # of MapSelect's considered for a particular top-level invocation.")
#if defined(_TARGET_AMD64_) || defined(_TARGET_X86_)
#define EXTERNAL_FeatureSIMD_Default 1
#define EXTERNAL_JitEnableAVX_Default 1
#else // !defined(_TARGET_AMD64_) && !defined(_TARGET_X86_)
#define EXTERNAL_FeatureSIMD_Default 0
#define EXTERNAL_JitEnableAVX_Default 0
#endif // !defined(_TARGET_AMD64_) && !defined(_TARGET_X86_)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_FeatureSIMD, W("FeatureSIMD"), EXTERNAL_FeatureSIMD_Default, "Enable SIMD support with companion SIMDVector.dll", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_EnableAVX, W("EnableAVX"), EXTERNAL_JitEnableAVX_Default, "Enable AVX instruction set for wide operations as default", CLRConfig::REGUTIL_default)

#if defined(_TARGET_X86_) || defined(_TARGET_AMD64_)
CONFIG_DWORD_INFO_EX(INTERNAL_JitEnablePCRelAddr, W("JitEnablePCRelAddr"), 1, "Whether absolute addr be encoded as PC-rel offset by RyuJIT where possible", CLRConfig::REGUTIL_default)
#endif //_TARGET_X86_ || _TARGET_AMD64_

#ifdef FEATURE_MULTICOREJIT

RETAIL_CONFIG_STRING_INFO(INTERNAL_MultiCoreJitProfile, W("MultiCoreJitProfile"), "If set, use the file to store/control multi-core JIT.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_MultiCoreJitProfileWriteDelay, W("MultiCoreJitProfileWriteDelay"), 12, "Set the delay after which the multi-core JIT profile will be written to disk.")

#endif

CONFIG_DWORD_INFO(INTERNAL_JitFunctionTrace, W("JitFunctionTrace"), 0, "If non-zero, print JIT start/end logging")

//
// JIT64
//
CONFIG_DWORD_INFO_EX(INTERNAL_Jit64_HashtableSize, W("HashTableSize"),500, "Size of Hashtable",CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_Jit64_LargeSymCount, W("LargeSymCount"),100000, "Large Sym Count Size",CLRConfig::REGUTIL_default)

#ifdef FEATURE_INTERPRETER
//
// Interpreter
// 
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

// Loader
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_APIThreadStress, W("APIThreadStress"), "Used to test Loader for race conditions")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_ForceLog, W("ForceLog"), "Fusion flag to enforce assembly binding log. Heavily used and documented in MSDN and BLOGS.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_LoaderOptimization, W("LoaderOptimization"), "Controls code sharing behavior")
RETAIL_CONFIG_STRING_INFO(INTERNAL_CoreClrBinderLog, W("CoreClrBinderLog"), "Debug flag that enabled detailed log for new binder (similar to stress logging).")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_DisableIJWVersionCheck, W("DisableIJWVersionCheck"), 0, "Don't perform the new version check that prevents unsupported IJW in-proc SxS.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_EnableFastBindClosure, W("EnableFastBindClosure"), 0, "If set to >0 the binder uses CFastAssemblyBindingClosure instances")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DisableFXClosureWalk, W("DisableFXClosureWalk"), 0, "Disable full closure walks even in the presence of FX binding redirects")
CONFIG_DWORD_INFO(INTERNAL_TagAssemblyNames, W("TagAssemblyNames"), 0, "Enable CAssemblyName::_tag field for more convenient debugging.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_WinMDPath, W("WinMDPath"), "Path for Windows WinMD files")

// 
// Loader heap
// 
CONFIG_DWORD_INFO_EX(INTERNAL_LoaderHeapCallTracing, W("LoaderHeapCallTracing"), 0, "Loader heap troubleshooting", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_CodeHeapReserveForJumpStubs, W("CodeHeapReserveForJumpStubs"), 2, "Percentage of code heap to reserve for jump stubs")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NGenReserveForJumpStubs, W("NGenReserveForJumpStubs"), 0, "Percentage of ngen image size to reserve for jump stubs")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_BreakOnOutOfMemoryWithinRange, W("BreakOnOutOfMemoryWithinRange"), 0, "Break before out of memory within range exception is thrown")

// 
// Log
// 
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogEnable, W("LogEnable"), "Turns on the traditional CLR log.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogFacility,  W("LogFacility"),  "Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogFacility2, W("LogFacility2"), "Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_logFatalError, W("logFatalError"), 1, "Specifies whether EventReporter logs fatal errors in the Windows event log.")
CONFIG_STRING_INFO_EX(INTERNAL_LogFile, W("LogFile"), "Specifies a file name for the CLR log.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogFileAppend, W("LogFileAppend"), "Specifies whether to append to or replace the CLR log file.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogFlushFile, W("LogFlushFile"), "Specifies whether to flush the CLR log file file on each write.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_LogLevel, W("LogLevel"), "4=10 msgs, 9=1000000, 10=everything")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_LogPath, W("LogPath"), "?Fusion debug log path.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogToConsole, W("LogToConsole"), "Writes the CLR log to console.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogToDebugger, W("LogToDebugger"), "Writes the CLR log to debugger (OutputDebugStringA).")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogToFile, W("LogToFile"), "Writes the CLR log to a file.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_LogWithPid, W("LogWithPid"), "Appends pid to filename for the CLR log.")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_FusionLogFileNamesIncludePid, W("FusionLogFileNamesIncludePid"), 0, "Fusion logging will append process id to log filenames.", CLRConfig::REGUTIL_default)

// 
// MetaData
// 
CONFIG_DWORD_INFO_EX(INTERNAL_MD_ApplyDeltaBreak, W("MD_ApplyDeltaBreak"), 0, "ASSERT when appplying EnC", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_AssertOnBadImageFormat, W("AssertOnBadImageFormat"), "ASSERT when invalid MD read")
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_MD_DeltaCheck, W("MD_DeltaCheck"), 1, "? Some checks of GUID when applying EnC?", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_EncDelta, W("MD_EncDelta"), 0, "? Forces EnC Delta format in MD", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_MD_ForceNoColDesSharing, W("MD_ForceNoColDesSharing"), 0, "? ? Don't know - the only usage I could find is #if 0", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_KeepKnownCA, W("MD_KeepKnownCA"), 0, "? Something with known CAs?", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_MiniMDBreak, W("MD_MiniMDBreak"), 0, "ASSERT when creating CMiniMdRw class", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_PreSaveBreak, W("MD_PreSaveBreak"), 0, "ASSERT when calling CMiniMdRw::PreSave", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_RegMetaBreak, W("MD_RegMetaBreak"), 0, "ASSERT when creating RegMeta class", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_RegMetaDump, W("MD_RegMetaDump"), 0, "? Dump MD in 4 functions?", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_TlbImp_BreakOnErr, W("MD_TlbImp_BreakOnErr"), 0, "ASSERT when importing TLB into MD", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_MD_TlbImp_BreakOnTypeImport, W("MD_TlbImp_BreakOnTypeImport"), "ASSERT when importing a type from TLB", (CLRConfig::LookupOptions) (CLRConfig::REGUTIL_default | CLRConfig::DontPrependCOMPlus_))
// MetaData - Desktop-only
CONFIG_DWORD_INFO_EX(INTERNAL_MD_WinMD_Disable, W("MD_WinMD_Disable"), 0, "Never activate the WinMD import adapter", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_MD_WinMD_AssertOnIllegalUsage, W("MD_WinMD_AssertOnIllegalUsage"), 0, "ASSERT if a WinMD import adapter detects a tool incompatibility", CLRConfig::REGUTIL_default)

// Metadata - mscordbi only - this flag is only intended to mitigate potential issues in bug fix 458597. 
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_MD_PreserveDebuggerMetadataMemory, W("MD_PreserveDebuggerMetadataMemory"), 0, "Save all versions of metadata memory in the debugger when debuggee metadata is updated", CLRConfig::REGUTIL_default)

// 
// MDA ?
// 
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_MDA, W("MDA"), "Config string to determine which MDAs to enable", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_MDAValidateFramework, W("MDAValidateFramework"), "If set, validate the XML schema for MDA", CLRConfig::REGUTIL_default)


//
// Spinning heuristics
// Note that these only take effect once the runtime has been started; prior to that the values hardcoded in g_SpinConstants (vars.cpp) are used
//
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinInitialDuration, W("SpinInitialDuration"), 0x32, "Hex value specifying the first spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinBackoffFactor, W("SpinBackoffFactor"), 0x3, "Hex value specifying the growth of each successive spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinLimitProcCap, W("SpinLimitProcCap"), 0xFFFFFFFF, "Hex value specifying the largest value of NumProcs to use when calculating the maximum spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinLimitProcFactor, W("SpinLimitProcFactor"), 0x4E20, "Hex value specifying the multiplier on NumProcs to use when calculating the maximum spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinLimitConstant, W("SpinLimitConstant"), 0x0, "Hex value specifying the constant to add when calculating the maximum spin duration", EEConfig_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SpinRetryCount, W("SpinRetryCount"), 0xA, "Hex value specifying the number of times the entire spin process is repeated (when applicable)", EEConfig_default)

// 
// Native Binder
//

CONFIG_DWORD_INFO(INTERNAL_NgenBind_UseTimestamp, W("NgenBind_UseTimestamp"), 0, "Use timestamp to validate a native image")
CONFIG_STRING_INFO(INTERNAL_NgenBind_UseTimestampList, W("NgenBind_UseTimestampList"), "")
CONFIG_STRING_INFO(INTERNAL_NgenBind_UseTimestampExcludeList, W("NgenBind_UseTimestampExcludeList"), "")

CONFIG_DWORD_INFO(INTERNAL_NgenBind_ZapForbid,             W("NgenBind_ZapForbid"), 0, "Assert if an assembly succeeds in binding to a native image")
CONFIG_STRING_INFO(INTERNAL_NgenBind_ZapForbidExcludeList, W("NgenBind_ZapForbidExcludeList"), "")
CONFIG_STRING_INFO(INTERNAL_NgenBind_ZapForbidList,        W("NgenBind_ZapForbidList"), "")

RETAIL_CONFIG_DWORD_INFO(EXTERNAL_NgenBind_OptimizeNonGac, W("NgenBind_OptimizeNonGac"), 0, "Skip loading IL image outside of GAC when NI can be loaded")

CONFIG_DWORD_INFO_EX(INTERNAL_SymDiffDump, W("SymDiffDump"), 0, "Used to create the map file while binding the assembly. Used by SemanticDiffer", CLRConfig::REGUTIL_default)

// 
// NGEN
// 
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_NGen_JitName, W("NGen_JitName"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGEN_USE_PRIVATE_STORE, W("NGEN_USE_PRIVATE_STORE"), -1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NGENBreakOnInjectPerAssemblyFailure, W("NGENBreakOnInjectPerAssemblyFailure"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NGENBreakOnInjectTransientFailure, W("NGENBreakOnInjectTransientFailure"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGENBreakOnWorker, W("NGENBreakOnWorker"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGenClean, W("NGenClean"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGenCompileWorkerHang, W("NGenCompileWorkerHang"), 0, "If set to 1, NGen compile worker process hangs forever", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGenDeferAllCompiles, W("NGenDeferAllCompiles"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGenDependencyWorkerHang, W("NGenDependencyWorkerHang"), 0, "If set to 1, NGen dependency walk worker process hangs forever", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenDisasm, W("NgenDisasm"), "Same as JitDisasm, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenDump, W("NgenDump"), "Same as JitDump, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenDumpIR, W("NgenDumpIR"), "Same as JitDumpIR, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenDumpIRFormat, W("NgenDumpIRFormat"), "Same as JitDumpIRFormat, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenDumpIRPhase, W("NgenDumpIRPhase"), "Same as JitDumpIRPhase, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenDumpFg, W("NgenDumpFg"), "Ngen Xml Flowgraph support", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenDumpFgDir, W("NgenDumpFgDir"), "Ngen Xml Flowgraph support", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenDumpFgFile, W("NgenDumpFgFile"), "Ngen Xml Flowgraph support", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGenFramed, W("NGenFramed"), -1, "same as JitFramed, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_NgenGCDump, W("NgenGCDump"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenHashDump, W("NgenHashDump"), (DWORD)-1, "same as JitHashDump, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenHashDumpIR, W("NgenHashDumpIR"), (DWORD)-1, "same as JitHashDumpIR, but for ngen", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NGENInjectFailuresServiceOnly, W("NGENInjectFailuresServiceOnly"), 1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NGENInjectPerAssemblyFailure, W("NGENInjectPerAssemblyFailure"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NGENInjectTransientFailure, W("NGENInjectTransientFailure"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGenLocalWorker, W("NGenLocalWorker"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGenMaxLogSize, W("NGenMaxLogSize"), 0, "The maximum size ngen.log and ngen_service.log files can grow to.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGenLogVerbosity, W("NGenLogVerbosity"), 2, "Default ngen log verbosity level", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NGenOnlyOneMethod, W("NGenOnlyOneMethod"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenOrder, W("NgenOrder"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_CheckNGenImageTimeStamp, W("CheckNGenImageTimeStamp"), 1, "Used to skip ngen timestamp check when switching compilers around.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGenRegistryAccessCount, W("NGenRegistryAccessCount"), -1, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGenStressDelete, W("NGenStressDelete"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO(INTERNAL_NGenUninstallKeep, W("NGenUninstallKeep"), "Semicolon-delimited list of assemblies to keep during 'ngen uninstall *'")
CONFIG_STRING_INFO(INTERNAL_NgenUnwindDump, W("NgenUnwindDump"), "Dump the unwind codes for the method")
CONFIG_STRING_INFO(INTERNAL_NgenEHDump, W("NgenEHDump"), "Dump the EH table for the method, as reported to the VM")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGENUseService, W("NGENUseService"), 1, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGenWorkerCount, W("NGenWorkerCount"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_partialNGenStress, W("partialNGenStress"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_ZapDoNothing, W("ZapDoNothing"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_HardPrejitEnabled, W("HardPrejitEnabled"), "")
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_EnableHardbinding, W("EnableHardbinding"), 0, "Enables the use of hardbinding", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_WorkerRetryNgenFailures, W("WorkerRetryNgenFailures"), 0, "If set to 1, The Ngen worker will retry once when ngen fails", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenForceFailureMask, W("NgenForceFailureMask"), -1, "Bitmask used to control which locations will check and raise the failure (defaults to bits: -1)", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenForceFailureCount, W("NgenForceFailureCount"), 0, "If set to >0 and we have IBC data we will force a failure after we reference an IBC data item <value> times", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NgenForceFailureKind, W("NgenForceFailureKind"), 1, "If set to 1, We will throw a TypeLoad exception; If set to 2, We will cause an A/V", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_NGenEnableCreatePdb, W("NGenEnableCreatePdb"), 0, "If set to >0 ngen.exe displays help on, recognizes createpdb in the command line")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NGenSimulateDiskFull, W("NGenSimulateDiskFull"), 0, "If set to 1, ngen will throw a Disk full exception in ZapWriter.cpp:Save()")
RETAIL_CONFIG_STRING_INFO(INTERNAL_NGenAssemblyUsageLog, W("NGenAssemblyUsageLog"), "Directory to store ngen usage logs in.")
#ifdef FEATURE_APPX
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NGenAssemblyUsageLogRefreshInterval, W("NGenAssemblyUsageLogRefreshInterval"), 24 * 60 * 60, "Interval to update usage log timestamp (seconds)");
#endif
RETAIL_CONFIG_DWORD_INFO(INTERNAL_AppLocalAutongenNGenDisabled, W("AppLocalAutongenNGenDisabled"), 0, "Autongen disable flag.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_PartialNGen, W("PartialNGen"), -1, "Generate partial NGen images")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NgenAllowMscorlibSoftbind, W("NgenAllowMscorlibSoftbind"), 0, "Disable forced hard-binding to mscorlib")
RETAIL_CONFIG_STRING_INFO_EX(UNSUPPORTED_RegistryRoot, W("RegistryRoot"), "Redirects all registry access under HKLM\Software to a specified alternative", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(UNSUPPORTED_AssemblyPath, W("AssemblyPath"), "Redirects v2 GAC access to a specified alternative path", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(UNSUPPORTED_AssemblyPath2, W("AssemblyPath2"), "Redirects v4 GAC access to a specified alternative path", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_EX(UNSUPPORTED_NicPath, W("NicPath"), "Redirects NIC access to a specified alternative", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_NGenTaskDelayStart, W("NGenTaskDelayStart"), 0, "Use NGen Task delay start trigger, instead of critical idle task")

// Flag for cross-platform ngen: Removes all execution of managed or third-party code in the ngen compilation process.
RETAIL_CONFIG_DWORD_INFO(INTERNAL_Ningen, W("Ningen"), 1, "Enable no-impact ngen")

CONFIG_DWORD_INFO(INTERNAL_NoASLRForNgen, W("NoASLRForNgen"), 0, "Turn off IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE bit in generated ngen images. Makes nidump output repeatable from run to run.")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NgenAllowOutput, W("NgenAllowOutput"), 0, "If set to 1, the NGEN worker will bind to the parent console, thus allowing stdout output to work", CLRConfig::REGUTIL_default)

#ifdef CROSSGEN_COMPILE
RETAIL_CONFIG_DWORD_INFO(INTERNAL_CrossGenAssumeInputSigned, W("CrossGenAssumeInputSigned"), 1, "CrossGen should assume that its input assemblies will be signed before deployment")
#endif

// 
// NGEN service
// 
// UNSUPPORTED_NGenMaxLogSize from NGEN section also applies to NGEN service.
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_NGENServiceAbortIdleWorkUnderDebugger, W("NGENServiceAbortIdleWorkUnderDebugger"), 1, "Determines whether the Ngen service will abort idle-time tasks while under a debugger. Off by default. Allows for single-machine debugging of the idle-time logic.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGENServiceAggressiveHardDiskIdleTimeout, W("NGENServiceAggressiveHardDiskIdleTimeout"), 1*60*60*1000, "This flag was intended as a backstop for HDD idle time detection (i.e. even if the hard disk is not idle, proceed with the compilation of the high-priority assemblies after the specified timeout). The current implementation compiles high-priority assemblies regardless of the state of the machine.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NGENServiceAggressiveWorkWaitTimeout, W("NGENServiceAggressiveWorkWaitTimeout"), 0, "This flag was intended as a backstop for machine idle time detection (i.e. even if the machine is not idle, proceed with the compilation of the high-priority assemblies after the specified timeout). The current implementation compiles high-priority assemblies regardless of the state of the machine.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceBreakOnStart, W("NGENServiceBreakOnStart"), 0, "Determines whether the Ngen service will call DebugBreak in its start routing. Off by default. Marginally useful for debugging service startup (there are other techniques as well).", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceConservative, W("NGENServiceConservative"), 0, "Determines whether the Ngen service will avoid compiling low-priority assemblies if multiple sessions exist on the machine and it can't determine their state. Off by default.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGenServiceDebugLog, W("NGenServiceDebugLog"), 0, "Configures the level of debug logging.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceIdleBatteryThreshold, W("NGENServiceIdleBatteryThreshold"), 50, "When a battery-powered system is below the threshold, Ngen will not process low-priority assemblies.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceIdleDebugInfo, W("NGENServiceIdleDebugInfo"), 0, "Determines whether the Ngen service will print the idle-time detection criteria to the debug log. Off by default. Ignored if NGenServiceDebugLog is 0.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceIdleDiskLogic, W("NGENServiceIdleDiskLogic"), 1, "Determines if the Ngen service will use hard disk idle time  for its machine idle time heuristics.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceIdleDiskThreshold, W("NGENServiceIdleDiskThreshold"), 80, "The amount of time after which a disk is declared idle.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceIdleNoInputPeriod, W("NGENServiceIdleNoInputPeriod"), 5*60*1000, "The amount of time after which the machine is declared idle if no input was received.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServicePassiveExceptInputTimeout, W("NGENServicePassiveExceptInputTimeout"), 15*60*60*1000, "The amount of time after which only input state is considered for idle time detection (input backstop mode, which ignores everything except input).", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServicePassiveHardDiskIdleTimeout, W("NGENServicePassiveHardDiskIdleTimeout"), 36*60*60*1000, "The amount of time after which the state of the hard disk is ignored for idle time detection.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServicePassiveWorkWaitTimeout, W("NGENServicePassiveWorkWaitTimeout"), 0, "The amount of time after which the machine is declared idle and low priority assemblies are compiled no matter what the actual state is (absolute backstop mode: declaring the machine as idle disregarding the actual state).", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_NGENServicePolicy, W("NGENServicePolicy"), "The policy that will be used for the machine (client or server). By default, it's determined from the OS SKU.")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_NGENServiceRestrictWorkersPrivileges, W("NGENServiceRestrictWorkersPrivileges"), 1, "Determines if worker processes are launched with restricted tokens.")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceSynchronization, W("NGENServiceSynchronization"), 1, "Determines if multiple services coordinate themselves so that only one service is working at a time.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(UNSUPPORTED_NGENServiceTestHookDll, W("NGENServiceTestHookDll"), "The name of a module used for testing in-process")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(UNSUPPORTED_NGENServiceWaitAggressiveWork, W("NGENServiceWaitAggressiveWork"), "Specifies how often the service will check the machine state when trying to do high-priority work.")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceWaitPassiveWork, W("NGENServiceWaitPassiveWork"), 1*60*1000, "Specifies how often the service will check the machine state when trying to do low-priority work.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_NGENServiceWaitWorking, W("NGENServiceWaitWorking"), 1000, "While working, the Ngen service polls the state of the machine for changes (another service trying to do higher priority work, the Ngen command line tool trying to do work, machine coming out of idle state). This variable controls the frequency of the polling.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_NGENServiceWorkerPriority, W("NGENServiceWorkerPriority"), "The process priority class for workers.")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_EnableMultiproc, W("EnableMultiproc"), 1, "Turns on multiproc ngen", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_SvcRetryNgenFailures, W("SvcRetryNgenFailures"), 1, "If set to 1, The Ngen service will retry once when ngen fails", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_NGenTaskDelayStartAmount, W("NGenTaskDelayStartAmount"), 5 * 60, "Number of seconds to delay for ngen update /queue /delay", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_NGenProtectedProcess_FeatureEnabled, W("NGenProtectedProcess_FeatureEnabled"), -1, "Run ngen as PPL (protected process) if needed. Set to 0 to disable the feature for compat with older Win8 builds.", CLRConfig::IgnoreConfigFiles)
CONFIG_STRING_INFO_EX(INTERNAL_NGenProtectedProcess_RequiredList,  W("NGenProtectedProcess_RequiredList"),  "Semicolon-separated list of assembly names that are required to be ngen'd in PPL process. Each name in the list is matched as prefix or suffix of assembly name/assembly file name.", CLRConfig::IgnoreConfigFiles)
CONFIG_STRING_INFO_EX(INTERNAL_NGenProtectedProcess_ForbiddenList, W("NGenProtectedProcess_ForbiddenList"), "Semicolon-separated list of assembly names that are forbidden to be ngen'd in PPL process. Each name in the list is matched as prefix or suffix of assembly name/assembly file name.", CLRConfig::IgnoreConfigFiles)
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_NGenCopyFromRepository_SetCachedSigningLevel, W("NGenCopyFromRepository_SetCachedSigningLevel"), 0, "Support for test tree ngen.exe flag /CopyFromRepository to also vouch for the output NIs.", CLRConfig::IgnoreConfigFiles)

//
// Perf
//
RETAIL_CONFIG_STRING_INFO(EXTERNAL_PerformanceScenario, W("performanceScenario"), "Activates a set of workload-specific default values for performance settings")

//
// Perfcounters
//
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_ProcessNameFormat, W("ProcessNameFormat"), (DWORD)-1, "Used by corperfmonext.dll to determine whether to decorate an instance name with the corresponding PID and runtime ID", CLRConfig::IgnoreHKLM | CLRConfig::IgnoreHKCU | CLRConfig::IgnoreConfigFiles)

// 
// Profiling API / ETW
// 
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
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_DetachMinSleepMs, W("ProfAPI_DetachMinSleepMs"), 0, "The minimum time, in millseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ProfAPI_DetachMaxSleepMs, W("ProfAPI_DetachMaxSleepMs"), 0, "The maximum time, in millseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded.")
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
#endif

//
// Shim
//
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_EnableIEHosting, W("EnableIEHosting"), "Allow activation of IE hosting")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_NoGuiFromShim, W("NoGuiFromShim"), "Turn off GUI in shim")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_OnlyUseLatestCLR, W("OnlyUseLatestCLR"), "Big red switch for loading CLR")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_FailOnInProcSxS, W("FailOnInProcSxS"), "Fails the process when a second runtime is loaded in-process")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_UseLegacyV2RuntimeActivationPolicyDefaultValue, W("UseLegacyV2RuntimeActivationPolicyDefaultValue"), "Modifies the default value")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_ErrorDialog, W("ErrorDialog"), "Allow showing UI on error")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_Fod, W("Fod"), "Test the Feature On Demand installation")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(UNSUPPORTED_FodPath, W("FodPath"), "Name of executable for Feature On Demand mockup")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(UNSUPPORTED_FodArgs, W("FodArgs"), "Command line arguments to pass to the FOD process")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_FodLaunchAsync, W("FodLaunchAsync"), "Whether to launch FOD asynchronously.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_FodConservativeMode, W("FodConservativeMode"), "Whether to be conservative wrt Fod launch.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_ApplicationMigrationRuntimeActivationConfigPath, W("ApplicationMigrationRuntimeActivationConfigPath"), "Provides a path in which to look for configuration files to be used for runtime activation, for application migration scenarios, before looking next to the EXE itself.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_TestOnlyEnsureImmersive, W("TestOnlyEnsureImmersive"), "Test-only flag used to indicate that it is expected that a process should be running as immersive.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_EnableCoreClrHost, W("EnableCoreClrHost"), "Enables hosting coreclr from desktop mscoreei.dll to run windows store apps")


//
// Security
//
CONFIG_STRING_INFO_EX(INTERNAL_Security_AptcaAssemblyBreak, W("AptcaAssemblyBreak"), "Sets a breakpoint when checking if an assembly is APTCA or not", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO(INTERNAL_Security_AptcaAssemblySharingBreak, W("AptcaAssemblySharingBreak"), "Sets a breakpoint when checking if we can code share an assembly")
CONFIG_DWORD_INFO(INTERNAL_Security_AptcaAssemblySharingDomainBreak, W("AptcaAssemblySharingDomainBreak"), 0, "Sets a breakpoint only in the specified domain when checking if we can code share an assembly")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_Security_DefaultSecurityRuleSet, W("DefaultSecurityRuleSet"), 0, "Overrides the security rule set that assemblies which don't explicitly select their own rule set should use")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Security_LegacyCasPolicy, W("legacyCasPolicy"), 0, "Enable CAS policy for the process - for test use only, official access to this switch is through NetFx40_LegacySecurityPolicy.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Security_LoadFromRemoteSources, W("loadFromRemoteSources"), 0, "Enable loading from zones that are not MyComputer when not in CAS mode.")
CONFIG_DWORD_INFO(UNSUPPORTED_Security_LogTransparencyErrors, W("LogTransparencyErrors"), 0, "Add an entry to the CLR log file for all transparency errors, rather than throwing an exception")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Security_NetFx40LegacySecurityPolicy, W("NetFx40_LegacySecurityPolicy"), 0, "Enable CAS policy for the process.")
CONFIG_DWORD_INFO(INTERNAL_Security_NGenForPartialTrust, W("NGenForPartialTrust"), 0, "Force NGEN to generate code for assemblies that could be used in partial trust.")
CONFIG_STRING_INFO_EX(INTERNAL_Security_TransparencyFieldBreak, W("TransparencyFieldBreak"), "Sets a breakpoint when figuring out the transparency of a specific field", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_Security_TransparencyMethodBreak, W("TransparencyMethodBreak"), "Sets a breakpoint when figuring out the transparency of a specific method", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_Security_TransparencyTypeBreak, W("TransparencyTypeBreak"), "Sets a breakpoint when figuring out the transparency of a specific type", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_Security_AlwaysInsertCallout, W("AlwaysInsertCallout"), 0, "Always insert security access/transparency/APTCA callouts")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_Security_DisableAnonymouslyHostedDynamicMethodCreatorSecurityCheck, W("DisableAnonymouslyHostedDynamicMethodCreatorSecurityCheck"), 0, "Disables security checks for anonymously hosted dynamic methods based on their creator's security.")

//
// Serialization
//
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Serialization_UnsafeTypeForwarding, W("unsafeTypeForwarding"), 0, "Enable unsafe type forwarding between unrelated assemblies")

// 
// Stack overflow
// 
CONFIG_DWORD_INFO_EX(INTERNAL_SOBreakOnProbeDuringSO, W("SOBreakOnProbeDuringSO"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_SODumpViolationsDir, W("SODumpViolationsDir"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SODumpViolationsStackTraceLength, W("SODumpViolationsStackTraceLength"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SOEnableBackoutStackValidation, W("SOEnableBackoutStackValidation"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SOEnableDefaultRWValidation, W("SOEnableDefaultRWValidation"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_SOEnableStackProtectionInDebugger, W("SOEnableStackProtectionInDebugger"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_SOEnableStackProtectionInDebuggerForProbeAtLine, W("SOEnableStackProtectionInDebuggerForProbeAtLine"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SOEntryPointProbe, W("SOEntryPointProbe"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SOInteriorProbe, W("SOInteriorProbe"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SOLogger, W("SOLogger"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SOProbeAssertOnOverrun, W("SOProbeAssertOnOverrun"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_SOUpdateProbeAtLine, W("SOUpdateProbeAtLine"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_SOUpdateProbeAtLineAmount, W("SOUpdateProbeAtLineAmount"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_SOUpdateProbeAtLineInFile, W("SOUpdateProbeAtLineInFile"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_StackWalkStressUsingOldImpl, W("StackWalkStressUsingOldImpl"), 0, "to be removed", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_StackWalkStressUsingOS, W("StackWalkStressUsingOS"), 0, "to be removed", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO(EXTERNAL_StartupDelayMS, W("StartupDelayMS"), "")

// 
// Stress
// 
CONFIG_DWORD_INFO_EX(INTERNAL_StressCOMCall, W("StressCOMCall"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_StressLog, W("StressLog"), "Turns on the stress log.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_ForceEnc, W("ForceEnc"), "Forces Edit and Continue to be on for all eligable modules.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_StressLogSize, W("StressLogSize"), "Stress log size in bytes per thread.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_StressOn, W("StressOn"), "Enables the STRESS_ASSERT macro that stops runtime quickly (to prevent the clr state from changing significantly before breaking)")
CONFIG_DWORD_INFO_EX(INTERNAL_stressSynchronized, W("stressSynchronized"), 0, "Unknown if or where this is used; unless a test is specifically depending on this, it can be removed.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_StressThreadCount, W("StressThreadCount"), "")

//
// Thread Suspend
//
CONFIG_DWORD_INFO(INTERNAL_DiagnosticSuspend, W("DiagnosticSuspend"), 0, "")
CONFIG_DWORD_INFO(INTERNAL_SuspendDeadlockTimeout, W("SuspendDeadlockTimeout"), 40000, "")
CONFIG_DWORD_INFO(INTERNAL_SuspendThreadDeadlockTimeoutMs, W("SuspendThreadDeadlockTimeoutMs"), 2000, "")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadSuspendInjection, W("INTERNAL_ThreadSuspendInjection"), 1, "Specifies whether to inject activations for thread suspension on Unix")

//
// Thread (miscellaneous)
//
RETAIL_CONFIG_DWORD_INFO(INTERNAL_Thread_DeadThreadCountThresholdForGCTrigger, W("Thread_DeadThreadCountThresholdForGCTrigger"), 75, "In the heuristics to clean up dead threads, this threshold must be reached before triggering a GC will be considered. Set to 0 to disable triggering a GC based on dead threads.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_Thread_DeadThreadGCTriggerPeriodMilliseconds, W("Thread_DeadThreadGCTriggerPeriodMilliseconds"), 1000 * 60 * 30, "In the heuristics to clean up dead threads, this much time must have elapsed since the previous max-generation GC before triggering another GC will be considered")

// 
// Threadpool
// 
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_ForceMinWorkerThreads, W("ThreadPool_ForceMinWorkerThreads"), 0, "Overrides the MinThreads setting for the ThreadPool worker pool")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_ForceMaxWorkerThreads, W("ThreadPool_ForceMaxWorkerThreads"), 0, "Overrides the MaxThreads setting for the ThreadPool worker pool")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_DisableStarvationDetection, W("ThreadPool_DisableStarvationDetection"), 0, "Disables the ThreadPool feature that forces new threads to be added when workitems run for too long")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_DebugBreakOnWorkerStarvation, W("ThreadPool_DebugBreakOnWorkerStarvation"), 0, "Breaks into the debugger if the ThreadPool detects work queue starvation")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_EnableWorkerTracking, W("ThreadPool_EnableWorkerTracking"), 0, "Enables extra expensive tracking of how many workers threads are working simultaneously")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ThreadPool_UnfairSemaphoreSpinLimit, W("ThreadPool_UnfairSemaphoreSpinLimit"), 50, "Per processor limit used when calculating spin duration in UnfairSemaphore::Wait")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Thread_UseAllCpuGroups, W("Thread_UseAllCpuGroups"), 0, "Specifies if to automatically distribute thread across CPU Groups")

CONFIG_DWORD_INFO(INTERNAL_ThreadpoolTickCountAdjustment, W("ThreadpoolTickCountAdjustment"), 0, "")

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


//
// Tiered Compilation
//
#ifdef FEATURE_TIERED_COMPILATION
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TieredCompilation, W("EXPERIMENTAL_TieredCompilation"), 0, "Enables tiered compilation")
#endif


// 
// TypeLoader
// 
CONFIG_DWORD_INFO(INTERNAL_TypeLoader_InjectInterfaceDuplicates, W("INTERNAL_TypeLoader_InjectInterfaceDuplicates"), 0, "Injects duplicates in interface map for all types.")

// 
// Virtual call stubs
// 
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubCollideMonoPct, W("VirtualCallStubCollideMonoPct"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubCollideWritePct, W("VirtualCallStubCollideWritePct"), 100, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubDumpLogCounter, W("VirtualCallStubDumpLogCounter"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubDumpLogIncr, W("VirtualCallStubDumpLogIncr"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_VirtualCallStubLogging, W("VirtualCallStubLogging"), 0, "Worth keeping, but should be moved into \"#ifdef STUB_LOGGING\" blocks. This goes for most (or all) of the stub logging infrastructure.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubMissCount, W("VirtualCallStubMissCount"), 100, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubResetCacheCounter, W("VirtualCallStubResetCacheCounter"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_VirtualCallStubResetCacheIncr, W("VirtualCallStubResetCacheIncr"), 0, "Used only when STUB_LOGGING is defined, which by default is not.", CLRConfig::REGUTIL_default)

//
// Watson
//
RETAIL_CONFIG_DWORD_INFO(INTERNAL_DisableWatsonForManagedExceptions, W("DisableWatsonForManagedExceptions"), 0, "disable Watson and debugger launching for managed exceptions")

// 
// Zap
// 
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

#ifdef FEATURE_WINDOWSPHONE
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ZapLazyCOWPagesEnabled, W("ZapLazyCOWPagesEnabled"), 1, "");
#else //FEATURE_WINDOWSPHONE
RETAIL_CONFIG_DWORD_INFO(INTERNAL_ZapLazyCOWPagesEnabled, W("ZapLazyCOWPagesEnabled"), 0, "");
#endif //FEATURE_WINDOWSPHONE

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

//
// Interop
//
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_ExposeExceptionsInCOM, W("ExposeExceptionsInCOM"), "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_ComInsteadOfManagedRemoting, W("PreferComInsteadOfManagedRemoting"), 0, "When communicating with a cross app domain CCW, use COM instead of managed remoting.")
CONFIG_DWORD_INFO(INTERNAL_GenerateStubForHost, W("GenerateStubForHost"), 0, "Forces the host hook stub to be built for all unmanaged calls, even when not running hosted.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_legacyApartmentInitPolicy, W("legacyApartmentInitPolicy"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_legacyComHierarchyVisibility, W("legacyComHierarchyVisibility"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_legacyComVTableLayout, W("legacyComVTableLayout"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_newComVTableLayout, W("newComVTableLayout"), "")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_PInvokeInline, W("PInvokeInline"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_InteropValidatePinnedObjects, W("InteropValidatePinnedObjects"), 0, "After returning from a managed-to-unmanged interop call, validate GC heap around objects pinned by IL stubs.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_InteropLogArguments, W("InteropLogArguments"), 0, "Log all pinned arguments passed to an interop call")
RETAIL_CONFIG_STRING_INFO(UNSUPPORTED_LogCCWRefCountChange, W("LogCCWRefCountChange"), "Outputs debug information and calls LogCCWRefCountChange_BREAKPOINT when AddRef or Release is called on a CCW.")
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EnableRCWCleanupOnSTAShutdown, W("EnableRCWCleanupOnSTAShutdown"), 0, "Performs RCW cleanup when STA shutdown is detected using IInitializeSpy in classic processes.")
RETAIL_CONFIG_STRING_INFO(INTERNAL_LocalWinMDPath, W("LocalWinMDPath"), "Additional path to probe for WinMD files in if a WinRT type is not resolved using the standard paths.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_AllowDComReflection, W("AllowDComReflection"), 0, "Allows out of process DCOM clients to marshal blocked reflection types.")

//
// Performance Tracing
//
RETAIL_CONFIG_DWORD_INFO(INTERNAL_PerformanceTracing, W("PerformanceTracing"), 0, "Enable/disable performance tracing.  Non-zero values enable tracing.")

//
// Unknown
// 
//---------------------------------------------------------------------------------------
// **
// PLEASE MOVE ANY CONFIG SWITCH YOU OWN OUT OF THIS SECTION INTO A CATEGORY ABOVE
// 
// DO NOT ADD ANY MORE CONFIG SWITCHES TO THIS SECTION!
// **
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_3gbEatMem, W("3gbEatMem"), 0, "Testhook: Size of memory (in 64K chunks) to be reserved before CLR starts", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_ActivatePatchSkip, W("ActivatePatchSkip"), 0, "allows an assert when ActivatePatchSkip is called", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_AlwaysCallInstantiatingStub, W("AlwaysCallInstantiatingStub"), 0, "Forces the Jit to use the instantiating stub for generics", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_alwaysFlowImpersonationPolicy, W("alwaysFlowImpersonationPolicy"), FALSE, "Windows identities should always flow across async points")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_AlwaysUseMetadataInterfaceMapLayout, W("AlwaysUseMetadataInterfaceMapLayout"), "Used for debugging generic interface map layout.")
CONFIG_DWORD_INFO(INTERNAL_AssertOnUnneededThis, W("AssertOnUnneededThis"), 0, "While the ConfigDWORD is unnecessary, the contained ASSERT should be kept. This may result in some work tracking down violating MethodDescCallSites.")
CONFIG_DWORD_INFO_EX(INTERNAL_AssertStacktrace, W("AssertStacktrace"), 1, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(UNSUPPORTED_BuildFlavor, W("BuildFlavor"), "Choice of build flavor (wks or svr) of CLR")
CONFIG_DWORD_INFO_EX(INTERNAL_clearNativeImageStress, W("clearNativeImageStress"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_CLRLoadLogDir, W("CLRLoadLogDir"), "Enable logging of CLR selection")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_CONFIG, W("CONFIG"), "Used to specify an XML config file for EEConfig", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_CopyPropMax, W("CopyPropMax"), "Sets internal jit constants for CopyProp", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_CPUFamily, W("CPUFamily"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_CPUFeatures, W("CPUFeatures"), "")
CONFIG_STRING_INFO_EX(INTERNAL_DeadCodeMax, W("DeadCodeMax"), "Sets internal jit constants for Dead Code elmination", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_DefaultVersion, W("DefaultVersion"), "Version of CLR to load.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(EXTERNAL_developerInstallation, W("developerInstallation"), "Flag to enable DEVPATH binding feature") // TODO: check special handling
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_shadowCopyVerifyByTimestamp, W("shadowCopyVerifyByTimestamp"), 0, "Fusion flag to enable quick verification of files in the shadow copy directory by using timestamps.", CLRConfig::FavorConfigFile | CLRConfig::MayHavePerformanceDefault)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_disableFusionUpdatesFromADManager, W("disableFusionUpdatesFromADManager"), 0, "Fusion flag to prevent changes to the AppDomainSetup object made by implementations of AppDomainManager.InitializeNewDomain from propagating to Fusion", CLRConfig::FavorConfigFile)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_disableCachingBindingFailures, W("disableCachingBindingFailures"), 0, "Fusion flag to re-enable Everett bind caching behavior (Whidbey caches failures for sharing)", CLRConfig::FavorConfigFile)
RETAIL_CONFIG_DWORD_INFO(INTERNAL_EnableVerboseInstallLogging, W("enableVerboseInstallLogging"), 0, "Fusion flag to enable detailed logging of GAC install operations")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_disableCommitThreadStack, W("disableCommitThreadStack"), "This should only be internal but I believe ASP.Net uses this")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_DisableConfigCache, W("DisableConfigCache"), 0, "Used to disable the \"probabilistic\" config cache, which walks through the appropriate config registry keys on init and probabilistically keeps track of which exist.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_DisableStackwalkCache, W("DisableStackwalkCache"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_DoubleArrayToLargeObjectHeap, W("DoubleArrayToLargeObjectHeap"), "Controls double[] placement")
CONFIG_DWORD_INFO(INTERNAL_DumpConfiguration, W("DumpConfiguration"), 0, "Dumps runtime properties of xml configuration files to the log.")
CONFIG_STRING_INFO(INTERNAL_DumpOnClassLoad, W("DumpOnClassLoad"), "Dumps information about loaded class to log.")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_EnableInternetHREFexes, W("EnableInternetHREFexes"), 0, "Part of security work related to locking down Internet No-touch deployment. It's not clear what happens to NTD in v4, but if it's till there the setting is needed", (CLRConfig::LookupOptions) (CLRConfig::REGUTIL_default | CLRConfig::IgnoreEnv | CLRConfig::IgnoreHKCU))
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_enforceFIPSPolicy, W("enforceFIPSPolicy"), "Causes crypto algorithms which have not been FIPS certified to throw an exception if they are used on a machine that requriess FIPS enforcement")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_ExpandAllOnLoad, W("ExpandAllOnLoad"), "")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_FORCE_ASSEMREF_DUPCHECK, W("FORCE_ASSEMREF_DUPCHECK"), 0, "? Has comment: Allow Avalon to use the SecurityCriticalAttribute ? but WHY?", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_ForcedRuntime, W("ForcedRuntime"), "Verify version of CLR loaded")
CONFIG_DWORD_INFO_EX(INTERNAL_ForceRelocs, W("ForceRelocs"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_GenerateLongJumpDispatchStubRatio, W("GenerateLongJumpDispatchStubRatio"), "Useful for testing VSD on AMD64")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_generatePublisherEvidence, W("generatePublisherEvidence"), "If set, when the CLR loads an assembly that has an Authenticode signature we will verify that signature to generate Publisher evidence, at the expense of network hits and perf.")
CONFIG_DWORD_INFO_EX(INTERNAL_HashStack, W("HashStack"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_HostManagerConfig, W("HostManagerConfig"), (DWORD)-1, "")
CONFIG_DWORD_INFO(INTERNAL_HostTestADUnload, W("HostTestADUnload"), 0, "Alows setting Rude unload as default")
CONFIG_DWORD_INFO(INTERNAL_HostTestThreadAbort, W("HostTestThreadAbort"), 0, "")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_IgnoreDllMainReturn, W("IgnoreDllMainReturn"), 0, "Don't check the return value of DllMain if this is set", CLRConfig::ConfigFile_ApplicationFirst)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_IJWEntrypointCompatMode, W("IJWEntrypointCompatMode"), 1, "Makes us run managed EP from DllMain. Basically brings the buggy behavior back.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_InstallRoot, W("InstallRoot"), "Directory with installed CLRs")
CONFIG_STRING_INFO(INTERNAL_InvokeHalt, W("InvokeHalt"), "Throws an assert when the given method is invoked through reflection.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_legacyHMACMode, W("legacyHMACMode"), "v2.0 of the CLR shipped with a bug causing HMAC-SHA-384 and HMAC-SHA-512 to be calculated incorrectly.  Orcas fixes this bug, but the config flag is added so that code which must verify v2.0 RTM HMACs can still interop with them.")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_legacyImpersonationPolicy, W("legacyImpersonationPolicy"), FALSE, "Windows identities should never flow across async points")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_legacyLoadMscorsnOnStartup, W("legacyLoadMscorsnOnStartup"), "Force mscorsn.dll to load when the VM starts")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_legacyNullReferenceExceptionPolicy, W("legacyNullReferenceExceptionPolicy"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_legacyUnhandledExceptionPolicy, W("legacyUnhandledExceptionPolicy"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_legacyVirtualMethodCallVerification, W("legacyVirtualMethodCallVerification"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_ManagedLogFacility, W("ManagedLogFacility"), "?Log facility for managed code using the log")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_MaxStackDepth, W("MaxStackDepth"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_MaxStubUnwindInfoSegmentSize, W("MaxStubUnwindInfoSegmentSize"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_MaxThreadRecord, W("MaxThreadRecord"), "")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_MergeCriticalAttributes, W("MergeCriticalAttributes"), 1, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_MessageDebugOut, W("MessageDebugOut"), 0, "")
CONFIG_DWORD_INFO_EX(INTERNAL_MscorsnLogging, W("MscorsnLogging"), 0, "Enables strong name logging", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NativeImageRequire, W("NativeImageRequire"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NestedEhOom, W("NestedEhOom"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NO_SO_NOT_MAINLINE, W("NO_SO_NOT_MAINLINE"), 0, "", CLRConfig::REGUTIL_default)
#define INTERNAL_NoGuiOnAssert_Default 1
RETAIL_CONFIG_DWORD_INFO_EX(INTERNAL_NoGuiOnAssert, W("NoGuiOnAssert"), INTERNAL_NoGuiOnAssert_Default, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_NoProcedureSplitting, W("NoProcedureSplitting"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_EX(INTERNAL_NoStringInterning, W("NoStringInterning"), 1, "Disallows string interning. I see no value in it anymore.", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_NotifyBadAppCfg, W("NotifyBadAppCfg"), "Whether to show a message box for bad application config file.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_PauseOnLoad, W("PauseOnLoad"), "Stops in SystemDomain::init. I think it can be removed.")
CONFIG_DWORD_INFO(INTERNAL_PerfAllocsSizeThreshold, W("PerfAllocsSizeThreshold"), 0x3FFFFFFF, "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
CONFIG_DWORD_INFO(INTERNAL_PerfNumAllocsThreshold, W("PerfNumAllocsThreshold"), 0x3FFFFFFF, "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
CONFIG_STRING_INFO(INTERNAL_PerfTypesToLog, W("PerfTypesToLog"), "Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler.")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_PEVerify, W("PEVerify"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_Prepopulate1, W("Prepopulate1"), 1, "")
CONFIG_STRING_INFO(INTERNAL_PrestubGC, W("PrestubGC"), "")
CONFIG_STRING_INFO(INTERNAL_PrestubHalt, W("PrestubHalt"), "")
RETAIL_CONFIG_STRING_INFO_EX(EXTERNAL_RepositoryDir, W("RepositoryDir"), "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_RepositoryFlags, W("RepositoryFlags"), "")
RETAIL_CONFIG_STRING_INFO(EXTERNAL_RestrictedGCStressExe, W("RestrictedGCStressExe"), "")
CONFIG_DWORD_INFO_EX(INTERNAL_ReturnSourceTypeForTesting, W("ReturnSourceTypeForTesting"), 0, "allows returning the (internal only) source type of an IL to Native mapping for debugging purposes", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_RSStressLog, W("RSStressLog"), 0, "allows turning on logging for RS startup", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SafeHandleStackTraces, W("SafeHandleStackTraces"), "Debug-only ability to get a stack trace attached to every SafeHandle instance at creation time, for tracking down handle corruption problems.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SaveThreadInfo, W("SaveThreadInfo"), "")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_SaveThreadInfoMask, W("SaveThreadInfoMask"), "")
CONFIG_DWORD_INFO(INTERNAL_SBDumpOnNewIndex, W("SBDumpOnNewIndex"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
CONFIG_DWORD_INFO(INTERNAL_SBDumpOnResize, W("SBDumpOnResize"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
CONFIG_DWORD_INFO(INTERNAL_SBDumpStyle, W("SBDumpStyle"), 0, "Used for Syncblock debugging. It's been a while since any of those have been used.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(UNSUPPORTED_ShimDatabaseVersion, W("ShimDatabaseVersion"), "Force using shim database version in registry")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_SleepOnExit, W("SleepOnExit"), 0, "Used for lrak detection. I'd say deprecated by umdh.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_StubLinkerUnwindInfoVerificationOn, W("StubLinkerUnwindInfoVerificationOn"), "")
RETAIL_CONFIG_DWORD_INFO_EX(UNSUPPORTED_SuccessExit, W("SuccessExit"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO(INTERNAL_SupressAllowUntrustedCallerChecks, W("SupressAllowUntrustedCallerChecks"), 0, "Disable APTCA")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_SymbolReadingPolicy, W("SymbolReadingPolicy"), "Specifies when PDBs may be read")
RETAIL_CONFIG_DWORD_INFO(UNSUPPORTED_TestDataConsistency, W("TestDataConsistency"), FALSE, "allows ensuring the left side is not holding locks (and may thus be in an inconsistent state) when inspection occurs")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_ThreadGuardPages, W("ThreadGuardPages"), 0, "", CLRConfig::REGUTIL_default)
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_Timeline, W("Timeline"), 0, "", CLRConfig::REGUTIL_default)
CONFIG_STRING_INFO_EX(INTERNAL_TlbImpShouldBreakOnConvFunction, W("TlbImpShouldBreakOnConvFunction"), "", CLRConfig::REGUTIL_default)
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_TlbImpSkipLoading, W("TlbImpSkipLoading"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(UNSUPPORTED_TotalStressLogSize, W("TotalStressLogSize"), "Total stress log size in bytes.")

#ifdef _DEBUG
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_TraceIUnknown, W("TraceIUnknown"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_TraceWrap, W("TraceWrap"), "")
#endif

RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_TURNOFFDEBUGINFO, W("TURNOFFDEBUGINFO"), "")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_UseGenericTlsGetters, W("UseGenericTlsGetters"), 0, "")
RETAIL_CONFIG_DWORD_INFO_EX(EXTERNAL_useLegacyIdentityFormat, W("useLegacyIdentityFormat"), 0, "Fusion flag to switch between Whidbey and Everett textual identity parser (have semantic differences)", CLRConfig::FavorConfigFile)
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_UseMethodDataCache, W("UseMethodDataCache"), FALSE, "Used during feature development; may now be removed.")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_UseNewCrossDomainRemoting, W("UseNewCrossDomainRemoting"), "Forces the managed remoting stack to be used even for cross-domain remoting if set to 0 (default is 1)")
RETAIL_CONFIG_DWORD_INFO(EXTERNAL_UseParentMethodData, W("UseParentMethodData"), TRUE, "Used during feature development; may now be removed.")
CONFIG_DWORD_INFO_DIRECT_ACCESS(INTERNAL_VerifierOff, W("VerifierOff"), "")
RETAIL_CONFIG_DWORD_INFO_DIRECT_ACCESS(EXTERNAL_VerifyAllOnLoad, W("VerifyAllOnLoad"), "")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_Version, W("Version"), "Version of CLR to load.")
RETAIL_CONFIG_STRING_INFO_DIRECT_ACCESS(INTERNAL_ShimHookLibrary, W("ShimHookLibrary"), "Path to a DLL that should be notified when shim loads the runtime DLL.")
// **
// PLEASE MOVE ANY CONFIG SWITCH YOU OWN OUT OF THIS SECTION INTO A CATEGORY ABOVE
// 
// DO NOT ADD ANY MORE CONFIG SWITCHES TO THIS SECTION!
// **
//---------------------------------------------------------------------------------------
