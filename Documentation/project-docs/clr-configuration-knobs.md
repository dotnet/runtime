
#CLR Configuration Knobs

This Document is machine-generated from commit 65f7881 on 08/15/15. It might be out of date.

When using these configurations from environment variables, the variable need to have `ComPlus_` prefix in its name. e.g. To set DumpJittedMethods to 1, add `ComPlus_DumpJittedMethods=1` to envvars.

See also [Dumps and Other Tools](../botr/ryujit-overview.md#dumps-and-other-tools) for more information.

Name | Description | Type | Class | Default Value | Flags 
-----|-------------|------|-------|---------------|-------
`ADBreakOnCannotUnload` | Used to troubleshoot failures to unload appdomain (e.g. someone sitting in unmanged code). In some cases by the time we throw the appropriate exception the thread has moved from the offending call. This setting allows in an instrumented build to stop exactly at the right moment. | DWORD | INTERNAL | 0 | 
`AddRejitNops` | Control for the profiler rejit feature infrastructure | DWORD | UNSUPPORTED | | 
`ADDumpSB` | Not used | DWORD | INTERNAL | 0 | 
`ADForceSB` | Forces sync block creation for all objects | DWORD | INTERNAL | 0 | 
`ADLogMemory` | Superseded by test hooks | DWORD | INTERNAL | 0 | 
`ADTakeDHSnapShot` | Superseded by test hooks | DWORD | INTERNAL | 0 | 
`ADTakeSnapShot` | Superseded by test hooks | DWORD | INTERNAL | 0 | 
`EnableFullDebug` | Heavy-weight checking for AD boundary violations (AD leaks) | DWORD | INTERNAL | | 
`DisableMSIPeek` | Disable MSI check in Fusion | DWORD | INTERNAL | 0 | 
`MsiPeekForbid` | Assert on MSI calls | DWORD | INTERNAL | 0 | 
`ADULazyMemoryRelease` | On by default. Turned off in cases when people try to catch memory leaks, in which case AD unload should be immediately followed by GC) | DWORD | EXTERNAL | 1 | 
`ADURetryCount` | Controls timeout of AD unload. Used for workarounds when machine is too slow, there are network issues etc. | DWORD | EXTERNAL | | 
`APPDOMAIN_MANAGER_ASM` | Legacy method to specify the assembly containing the AppDomainManager to use for the default domain | STRING | EXTERNAL | | DontPrependCOMPLUS_ / IgnoreHKLM / IgnoreHKCU
`APPDOMAIN_MANAGER_TYPE` | LegacyMethod to specify the type containing the AppDomainManager to use for the default domain | STRING | EXTERNAL | | DontPrependCOMPLUS_  / IgnoreHKLM / IgnoreHKCU
`appDomainManagerAssembly` | Config file switch to specify the assembly for the default AppDomainManager. | STRING | EXTERNAL | | IgnoreEnv / IgnoreHKLM / IgnoreHKCU
`appDomainManagerType` | Config file switch to specify the type for the default AppDomainManager. | STRING | EXTERNAL | | IgnoreEnv / IgnoreHKLM / IgnoreHKCU
`AppDomainAgilityChecked` | Used to detect AD boundary violations (AD leaks) | DWORD | INTERNAL | | 
`AppDomainNoUnload` | Not used | DWORD | INTERNAL | 0 | 
`TargetFrameworkMoniker` | Allows the test team to specify what TargetFrameworkMoniker to use. | STRING | INTERNAL | | IgnoreHKLM / IgnoreHKCU / IgnoreConfigFiles / IgnoreWindowsQuirkDB
`AppContextSwitchOverrides` | Allows default switch values defined in AppContext to be overwritten by values in the Config | STRING | INTERNAL | | IgnoreEnv / IgnoreHKLM / IgnoreHKCU / IgnoreWindowsQuirkDB / ConfigFile_ApplicationFirst
`ARMEnabled` | Set it to 1 to enable ARM | DWORD | UNSUPPORTED | (DWORD)0 | 
`designerNamespaceResolution` | Set it to 1 to enable DesignerNamespaceResolve event for WinRT types | DWORD | EXTERNAL | FALSE | IgnoreEnv / IgnoreHKLM / IgnoreHKCU / FavorConfigFile
`GetAssemblyIfLoadedIgnoreRidMap` | Used to force loader to ignore assemblies cached in the rid-map | DWORD | INTERNAL | 0 | REGUTIL_default
`BCLCorrectnessWarnings` | Flag a few common correctness bugs in the library with additional runtime checks. | DWORD | INTERNAL | | 
`BCLPerfWarnings` | Flag some performance-related problems via asserts when people mis-use the library. | DWORD | INTERNAL | | 
`TimeSpan_LegacyFormatMode` | Flag to enable System.TimeSpan legacy (.NET Framework 3.5 and earlier) ToString behavior. | DWORD | EXTERNAL | 0 | 
`CompatSortNLSVersion` | Determines the version of desired sorting behavior for AppCompat. | DWORD | EXTERNAL | 0 | 
`NetFx45_CultureAwareComparerGetHashCode_LongStrings` | Opt in to use the new (as of v4.5) constant space hash algorithm for strings | DWORD | EXTERNAL | 0 | 
`DisableUserPreferredFallback` | Resource lookups should be dependent only on the CurrentUICulture, not a user-defined list of preferred languages nor the OS preferred fallback language.  Intended to avoid falling back to a right-to-left language, which is undisplayable in console apps. | DWORD | EXTERNAL | 0 | 
`relativeBindForResources` | Enables probing for satellite assemblies only next to the parent assembly | DWORD | EXTERNAL | 0 | 
`NetFx45_LegacyManagedDeflateStream` | Flag to enable legacy managed implementation of the deflater used by System.IO.Compression.DeflateStream. | DWORD | EXTERNAL | 0 | 
`DateTime_NetFX35ParseMode` | Flag to enable the .NET 3.5 System.DateTime Token Replacement Policy | DWORD | EXTERNAL | 0 | 
`ThrowUnobservedTaskExceptions` | Flag to propagate unobserved task exceptions on the finalizer thread. | DWORD | EXTERNAL | 0 | 
`EnableAmPmParseAdjustment` | Flag to enable the .NET 4.0 DateTimeParse to correctly parse AM/PM cases | DWORD | EXTERNAL | 0 | 
`UseRandomizedStringHashAlgorithm` | Flag to use a string hashing algorithm who's behavior differs between AppDomains | DWORD | EXTERNAL | 0 | 
`Windows8ProfileAPICheckFlag` | Windows 8 Profile API check behavior (non-W8P framework APIs cannot be accessed through Reflection and RefEmit). 0: normal (only check in non-dev-mode APPX). 1: always check. 2: never check. | DWORD | INTERNAL | 0 | 
`BreakOnBadExit` |  | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`BreakOnClassBuild` | Very useful for debugging class layout code. | STRING | INTERNAL | | 
`BreakOnClassLoad` | Very useful for debugging class loading code. | STRING | INTERNAL | | 
`BreakOnComToClrNativeInfoInit` | Throws an assert when native information about a COM -> CLR call are about to be gathered. | STRING | INTERNAL | | 
`BreakOnDebugBreak` | allows an assert in debug builds when a user break is hit | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnDILoad` | allows an assert when the DI is loaded | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnDumpToken` | Breaks when using internal logging on a particular token value. | DWORD | INTERNAL | 0xffffffff | REGUTIL_default
`BreakOnEELoad` |  | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`BreakOnEEShutdown` |  | DWORD | INTERNAL | 0 | 
`BreakOnExceptionInGetThrowable` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnFinalizeTimeOut` | Triggers a debug break on the finalizer thread when it has exceeded the maximum wait time | DWORD | UNSUPPORTED | 0 | 
`BreakOnFindMethod` | Breaks in findMethodInternal when it searches for the specified token. | DWORD | INTERNAL | 0 | 
`BreakOnFirstPass` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnHR` | Debug.cpp, IfFailxxx use this macro to stop if hr matches  | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnInstantiation` | Very useful for debugging generic class instantiation. | STRING | INTERNAL | | 
`BreakOnInteropStubSetup` | Throws an assert when marshaling stub for the given method is about to be built. | STRING | INTERNAL | | 
`BreakOnInteropVTableBuild` | Specifies a type name for which an assert should be thrown when building interop v-table. | STRING | INTERNAL | | REGUTIL_default
`BreakOnMethodName` | Very useful for debugging method override placement code. | STRING | INTERNAL | | 
`BreakOnNGenRegistryAccessCount` | Breaks on the Nth' root store write | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`BreakOnNotify` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnRetailAssert` | Used for debugging 'retail' asserts (fatal errors) | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnSecondPass` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnSO` |  | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`BreakOnStructMarshalSetup` | Throws an assert when field marshalers for the given type with layout are about to be created. | STRING | INTERNAL | | 
`BreakOnUEF` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`BreakOnUncaughtException` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`CseBinarySearch` | Sets internal jit constants for CSE | STRING | INTERNAL | | REGUTIL_default
`CseMax` | Sets internal jit constants for CSE | STRING | INTERNAL | | REGUTIL_default
`CseOn` | Internal Jit control of CSE | STRING | UNSUPPORTED | | REGUTIL_default
`CseStats` | Collects CSE statistics | STRING | INTERNAL | | REGUTIL_default
`D::FCE` | allows an assert when crawling the managed stack for an exception handler | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgBreakIfLocksUnavailable` | allows an assert when the debugger can't take a lock  | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgBreakOnErr` | allows an assert when we get a failing hresult | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgBreakOnMapPatchToDJI` | allows an assert when mapping a patch to an address | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgBreakOnRawInt3` | allows an assert for test coverage for debug break or other int3 breaks | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgBreakOnSendBreakpoint` | allows an assert when sending a breakpoint to the right side | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgBreakOnSetIP` | allows an assert when setting the IP | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgCheckInt3` | asserts if the debugger explicitly writes int3 instead of calling SetUnmanagedBreakpoint | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgDACAssertOnMismatch` | allows an assert when the mscordacwks and mscorwks dll versions don't match | DWORD | INTERNAL | | 
`DbgDACEnableAssert` | Enables extra validity checking in DAC - assumes target isn't corrupt | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgDACSkipVerifyDlls` | allows disabling the check to ensure mscordacwks and mscorwks dll versions match | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgDelayHelper` | varies the wait in the helper thread startup for testing race between threads | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgDisableDynamicSymsCompat` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgDisableTargetConsistencyAsserts` | allows explicitly testing with corrupt targets | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgEnableMixedModeDebuggingInternalOnly` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgExtraThreads` | allows extra unmanaged threads to run and throw debug events for stress testing | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgExtraThreadsCantStop` | allows extra unmanaged threads in can't stop region to run and throw debug events for stress testing | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgExtraThreadsIB` | allows extra in-band unmanaged threads to run and throw debug events for stress testing | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgExtraThreadsOOB` | allows extra out of band unmanaged threads to run and throw debug events for stress testing | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgFaultInHandleIPCEvent` | allows testing the unhandled event filter | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgInjectFEE` | allows injecting a fatal execution error for testing Watson | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgLeakCheck` | allows checking for leaked Cordb objects | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgNo2ndChance` | allows breaking on (and catching bogus) 2nd chance exceptions | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgNoDebugger` | allows breaking if we don't want to lazily initialize the debugger | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgNoForceContinue` | used to force a continue on longhorn | DWORD | UNSUPPORTED | 1 | REGUTIL_default
`DbgNoOpenMDByFile` | allows opening MD by memory for perf testing | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgOOBinFEEE` | allows forcing oob breakpoints when a fatal error occurs | DWORD | INTERNAL | 0 | 
`DbgPackShimPath` | CoreCLR path to dbgshim.dll - we are trying to figure out if we can remove this | STRING | EXTERNAL | | 
`DbgPingInterop` | allows checking for deadlocks in interop debugging | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgRace` | allows pausing for native debug events to get hijicked | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgRedirect` | allows for redirecting the event pipeline | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`DbgRedirectApplication` | Specifies the auxillary debugger application to launch. | STRING | EXTERNAL | | 
`DbgRedirectAttachCmd` | Specifies command parameters for attaching the auxillary debugger. | STRING | EXTERNAL | | 
`DbgRedirectCommonCmd` | Specifies a command line format string for the auxillary debugger. | STRING | EXTERNAL | | 
`DbgRedirectCreateCmd` | Specifies command parameters when creating the auxillary debugger. | STRING | EXTERNAL | | 
`DbgShortcutCanary` | allows a way to force canary to fail to be able to test failure paths | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgSkipMEOnStep` | turns off MethodEnter checks | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgSkipVerCheck` | allows different RS and LS versions (for servicing work) | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgTC` | allows checking boundary compression for offset mappings | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgTransportFaultInject` | allows injecting a fault for testing the debug transport | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgTransportLog` | turns on logging for the debug transport | DWORD | INTERNAL | | 
`DbgTransportLogClass` | mask to control what is logged in DbgTransportLog | DWORD | INTERNAL | | 
`DbgTransportProxyAddress` | allows specifying the transport proxy address | STRING | UNSUPPORTED | | REGUTIL_default
`DbgTrapOnSkip` | allows breaking when we skip a breakpoint | DWORD | INTERNAL | 0 | REGUTIL_default
`DbgWaitForDebuggerAttach` | Makes CoreCLR wait for a managed debugger to attach on process start (1) or regular process start (0) | DWORD | UNSUPPORTED | 0 | 
`DbgWaitTimeout` | specifies the timeout value for waits | DWORD | INTERNAL | 1 | REGUTIL_default
`DbgWFDETimeout` | specifies the timeout value for wait when waiting for a debug event | DWORD | UNSUPPORTED | 25 | REGUTIL_default
`RaiseExceptionOnAssert` | Raise a first chance (if set to 1) or second chance (if set to 2) exception on asserts. | DWORD | INTERNAL | 0 | REGUTIL_default
`DebugBreakOnAssert` | If DACCESS_COMPILE is defined, break on asserts. | DWORD | INTERNAL | 0 | REGUTIL_default
`DebugBreakOnVerificationFailure` | Halts the jit on verification failure | DWORD | INTERNAL | 0 | REGUTIL_default
`DebuggerBreakPoint` | allows counting various debug events | STRING | INTERNAL | | REGUTIL_default
`DebugVerify` | Control for tracing in peverify | STRING | INTERNAL | | REGUTIL_default
`EncApplyChanges` | allows breaking when ApplyEditAndContinue is called | DWORD | INTERNAL | 0 | 
`EnCBreakOnRemapComplete` | allows breaking after N RemapCompletes | DWORD | INTERNAL | 0 | REGUTIL_default
`EnCBreakOnRemapOpportunity` | allows breaking after N RemapOpportunities | DWORD | INTERNAL | 0 | REGUTIL_default
`EncDumpApplyChanges` | allows dumping edits in delta metadata and il files | DWORD | INTERNAL | 0 | 
`EncFixupFieldBreak` | Unlikely that this is used anymore. | DWORD | INTERNAL | 0 | 
`EncJitUpdatedFunction` | allows breaking when an updated function is jitted | DWORD | INTERNAL | 0 | 
`EnCResolveField` | allows breaking when computing the address of an EnC-added field | DWORD | INTERNAL | 0 | 
`EncResumeInUpdatedFunction` | allows breaking when execution resumes in a new EnC version of a function | DWORD | INTERNAL | 0 | 
`DbgAssertOnDebuggeeDebugBreak` | If non-zero causes the managed-only debugger to assert on unhandled breakpoints in the debuggee | DWORD | INTERNAL | 0 | REGUTIL_default
`UNSUPPORTED_DbgDontResumeThreadsOnUnhandledException` | If non-zero, then don't try to unsuspend threads after continuing a 2nd-chance native exception | DWORD | UNSUPPORTED | 0 | 
`DbgSkipStackCheck` | Skip the stack pointer check during stackwalking | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`IntentionallyCorruptDataFromTarget` | Intentionally fakes bad data retrieved from target to try and break dump generation. | DWORD | INTERNAL | 0 | 
`UNSUPPORTED_Debugging_RequiredVersion` | The lowest ICorDebug version we should attempt to emulate, or 0 for default policy.  Use 2 for CLRv2, 4 for CLRv4, etc. | DWORD | UNSUPPORTED | 0 | 
`MiniMdBufferCapacity` | The max size of the buffer to store mini metadata information for triage- and mini-dumps. | DWORD | INTERNAL | 64 * 1024 | 
`ConditionalContracts` | ?If ENABLE_CONTRACTS_IMPL is defined, sets whether contracts are conditional. | DWORD | INTERNAL | | 
`ConsistencyCheck` |  | DWORD | INTERNAL | 0 | 
`ContinueOnAssert` | If set, doesn't break on asserts. | DWORD | INTERNAL | 0 | REGUTIL_default
`disableStackOverflowProbing` |  | DWORD | UNSUPPORTED | 0 | FavorConfigFile
`EnforceEEThreadNotRequiredContracts` | Indicates whether to enforce EE_THREAD_NOT_REQUIRED contracts (not enforced by default for perf reasons).  Only applicable in dbg/chk builds--EE_THREAD_NOT_REQUIRED contracts never enforced in ret builds. | DWORD | INTERNAL | 0 | 
`InjectFatalError` |  | DWORD | INTERNAL | | 
`InjectFault` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`SuppressChecks` |  | DWORD | INTERNAL | | 
`SuppressLockViolationsOnReentryFromOS` | 64 bit OOM tests re-enter the CLR via RtlVirtualUnwind.  This indicates whether to suppress resulting locking violations. | DWORD | INTERNAL | 0 | 
`TestHooks` | Used by tests to get test an insight on various CLR workings | STRING | INTERNAL | | 
`AssertOnFailFast` |  | DWORD | INTERNAL | | 
`legacyCorruptedStateExceptionsPolicy` | Enabled Pre-V4 CSE behaviour | DWORD | UNSUPPORTED | 0 | FavorConfigFile
`SuppressLostExceptionTypeAssert` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`FastGCCheckStack` |  | DWORD | INTERNAL | 0 | 
`FastGCStress` | reduce the number of GCs done by enabling GCStress | DWORD | INTERNAL | | 
`GCBreakOnOOM` | Does a DebugBreak at the soonest time we detect an OOM | DWORD | UNSUPPORTED | | 
`gcConcurrent` | Enables/Disables concurrent GC | DWORD | UNSUPPORTED | (DWORD)-1 | 
`gcConservative` | Enables/Disables conservative GC | DWORD | UNSUPPORTED | 0 | 
`gcServer` | Enables server GC | DWORD | UNSUPPORTED | 0 | 
`GcCoverage` | specify a method or regular expression of method names to run with GCStress | STRING | INTERNAL | | 
`SkipGcCoverage` | specify a list of assembly names to skip with GC Coverage | STRING | INTERNAL | | 
`gcForceCompact` | When set to true, always do compacting GC | DWORD | UNSUPPORTED | | 
`GCgen0size` | Specifies the smallest gen0 size | DWORD | UNSUPPORTED | | 
`GCStressMix` | Specifies whether the GC mix mode is enabled or not | DWORD | INTERNAL | 0 | 
`GCStressStep` | Specifies how often StressHeap will actually do a GC in GCStressMix mode | DWORD | INTERNAL | 1 | 
`GCStressMaxFGCsPerBGC` | Specifies how many FGCs will occur during one BGC in GCStressMix mode | DWORD | INTERNAL | ~0U | 
`StatsUpdatePeriod` | Specifies the interval, in seconds, at which to update the statistics | DWORD | UNSUPPORTED | 60 | 
`SuspendTimeLog` | Specifies the name of the log file for suspension statistics | STRING | UNSUPPORTED | | 
`GCMixLog` | Specifies the name of the log file for GC mix statistics | STRING | UNSUPPORTED | | 
`GCLatencyMode` | Specifies the GC latency mode - batch, interactive or low latency (note that the same thing can be specified via API which is the supported way) | DWORD | INTERNAL | | 
`GCLogEnabled` | Specifies if you want to turn on logging in GC | DWORD | UNSUPPORTED | 0 | 
`GCLogFile` | Specifies the name of the GC log file | STRING | UNSUPPORTED | | 
`GCLogFileSize` | Specifies the maximum GC log file size | DWORD | UNSUPPORTED | 0 | 
`GCPollType` |  | DWORD | EXTERNAL | | 
`NewGCCalc` |  | STRING | EXTERNAL | | REGUTIL_default
`GCprnLvl` | Specifies the maximum level of GC logging | DWORD | UNSUPPORTED | | 
`GCRetainVM` | When set we put the segments that should be deleted on a standby list (instead of releasing them back to the OS) which will be considered to satisfy new segment requests (note that the same thing can be specified via API which is the supported way) | DWORD | UNSUPPORTED | | 
`GCSegmentSize` | Specifies the managed heap segment size | DWORD | UNSUPPORTED | | 
`GCLOHCompact` | Specifies the LOH compaction mode | DWORD | UNSUPPORTED | | 
`gcAllowVeryLargeObjects` | allow allocation of 2GB+ objects on GC heap | DWORD | EXTERNAL | 0 | 
`GCStress` | trigger GCs at regular intervals | DWORD | EXTERNAL | 0 | REGUTIL_default
`GcStressOnDirectCalls` | whether to trigger a GC on direct calls | DWORD | INTERNAL | 0 | REGUTIL_default
`GCStressStart` | start GCStress after N stress GCs have been attempted | DWORD | EXTERNAL | 0 | 
`GCStressStartAtJit` | start GCStress after N items are jitted | DWORD | INTERNAL | 0 | 
`GCtraceEnd` | Specifies the index of the GC when the logging should end | DWORD | UNSUPPORTED | | 
`GCtraceFacility` | Specifies where to log to (this allows you to log to console, the stress log or a normal CLR log (good when you need to correlate the GC activities with other CLR activities) | DWORD | INTERNAL | | 
`GCtraceStart` | Specifies the index of the GC when the logging should start | DWORD | UNSUPPORTED | | 
`gcTrimCommitOnLowMemory` | When set we trim the committed space more aggressively for the ephemeral seg. This is used for running many instances of server processes where they want to keep as little memory committed as possible | DWORD | EXTERNAL | | 
`BGCSpinCount` | Specifies the bgc spin count | DWORD | UNSUPPORTED | 140 | 
`BGCSpin` | Specifies the bgc spin time | DWORD | UNSUPPORTED | 2 | 
`HeapVerify` | When set verifies the integrity of the managed heap on entry and exit of each GC | DWORD | UNSUPPORTED | | 
`SetupGcCoverage` | This doesn't appear to be a config flag | STRING | EXTERNAL | | REGUTIL_default
`GCNumaAware` | Specifies if to enable GC NUMA aware | DWORD | UNSUPPORTED | 1 | 
`GCCpuGroup` | Specifies if to enable GC to support CPU groups | DWORD | EXTERNAL | 0 | 
`IBCPrint` |  | STRING | INTERNAL | | REGUTIL_default
`IBCPrint3` |  | STRING | INTERNAL | | REGUTIL_default
`ConvertIbcData` | Converts between v1 and v2 IBC data | DWORD | UNSUPPORTED | 1 | REGUTIL_default
`DisableHotCold` | Master hot/cold splitting switch in Jit64 | DWORD | UNSUPPORTED | | 
`DisableIBC` | Disables the use of IBC data | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`UseIBCFile` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`DumpJittedMethods` | Prints all jitted methods to the console | DWORD | INTERNAL | 0 | REGUTIL_default
`Jit64Range` |  | STRING | INTERNAL | | REGUTIL_default
`JitAlignLoops` | Aligns loop targets to 8 byte boundaries | DWORD | UNSUPPORTED | | 
`JitCloneLoops` | If 0, don't clone. Otherwise clone loops for optimizations. | DWORD | INTERNAL | 1 | REGUTIL_default
`JitAssertOnMaxRAPasses` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitBreak` | Stops in the importer when compiling a specified method | STRING | INTERNAL | | REGUTIL_default
`JitBreakEmit` |  | DWORD | INTERNAL | (DWORD)-1 | REGUTIL_default
`JitBreakEmitOutputInstr` |  | DWORD | INTERNAL | (DWORD)-1 | REGUTIL_default
`JitBreakMorphTree` |  | DWORD | INTERNAL | 0xFFFFFFFF | REGUTIL_default
`JitBreakOnBadCode` |  | DWORD | INTERNAL | | 
`JitBreakOnUnsafeCode` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitCanUseSSE2` |  | DWORD | INTERNAL | | 
`JitDebugBreak` |  | STRING | INTERNAL | | REGUTIL_default
`JitDebuggable` |  | DWORD | INTERNAL | | 
`JitDefaultFill` | In debug builds, initialize the memory allocated by the nra with this byte. | DWORD | INTERNAL | 0xDD | REGUTIL_default
`JitDirectAlloc` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitEnableNoWayAssert` |  | DWORD | INTERNAL | INTERNAL_JitEnableNoWayAssert_Default | REGUTIL_default
`JitDisasm` | Dumps disassembly for specified method | STRING | INTERNAL | | REGUTIL_default
`JitDoubleAlign` |  | DWORD | INTERNAL | | 
`JitDump` | Dumps trees for specified method | STRING | INTERNAL | | REGUTIL_default
`JitDumpVerboseTrees` | Enable more verbose tree dumps | DWORD | INTERNAL | 0 | REGUTIL_default
`JitDumpVerboseSsa` | Produce especially verbose dump output for SSA | DWORD | INTERNAL | 0 | REGUTIL_default
`JitDumpBeforeAfterMorph` | If 1, display each tree before/after morphing | DWORD | INTERNAL | 0 | REGUTIL_default
`JitDumpFg` | Xml Flowgraph support | STRING | INTERNAL | | REGUTIL_default
`JitDumpFgDir` | Xml Flowgraph support | STRING | INTERNAL | | REGUTIL_default
`JitDumpFgFile` | Xml Flowgraph support | STRING | INTERNAL | | REGUTIL_default
`JitDumpLevel` |  | DWORD | INTERNAL | 1 | REGUTIL_default
`JitDumpASCII` | Uses only ASCII characters in tree dumps | DWORD | INTERNAL | 1 | REGUTIL_default
`JitDumpTerseLsra` | Produce terse dump output for LSRA | DWORD | INTERNAL | 1 | REGUTIL_default
`JitDumpToDebugger` | Output JitDump output to the debugger | DWORD | INTERNAL | 0 | REGUTIL_default
`JitEmitPrintRefRegs` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitExclude` |  | STRING | INTERNAL | | REGUTIL_default
`JitForceFallback` | Set to non-zero to test NOWAY assert by forcing a retry | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoForceFallback` | Set to non-zero to prevent NOWAY assert testing. Overrides COMPLUS_JitForceFallback and JIT stress flags. | DWORD | INTERNAL | 0 | REGUTIL_default
`JitExpensiveDebugCheckLevel` | Level indicates how much checking beyond the default to do in debug builds (currently 1-2) | DWORD | INTERNAL | 0 | REGUTIL_default
`JitForceProcedureSplitting` |  | STRING | INTERNAL | | REGUTIL_default
`JitForceVer` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitFramed` | Forces EBP frames | DWORD | UNSUPPORTED | | 
`JitFullyInt` | Forces Fully interruptable code | DWORD | INTERNAL | 0 | REGUTIL_default
`JitGCChecks` |  | DWORD | INTERNAL | | 
`JitGCDump` |  | STRING | INTERNAL | | REGUTIL_default
`JitGCInfoLogging` | If true, prints GCInfo-related output to standard output. | DWORD | INTERNAL | 0 | 
`JitGCStress` | GC stress mode for jit | DWORD | INTERNAL | 0 | REGUTIL_default
`JitHalt` | Emits break instruction into jitted code | STRING | INTERNAL | | REGUTIL_default
`JitHashHalt` | Same as JitHalt, but for a method hash | DWORD | INTERNAL | (DWORD)-1 | REGUTIL_default
`JitHashBreak` | Same as JitBreak, but for a method hash | DWORD | INTERNAL | (DWORD)-1 | REGUTIL_default
`JitHashDump` | Same as JitDump, but for a method hash | DWORD | INTERNAL | (DWORD)-1 | REGUTIL_default
`JitHeartbeat` |  | DWORD | INTERNAL | 0 | 
`JitHelperLogging` |  | DWORD | INTERNAL | 0 | 
`JitImportBreak` |  | STRING | INTERNAL | | REGUTIL_default
`JitInclude` |  | STRING | INTERNAL | | REGUTIL_default
`JitInlineAdditionalMultiplier` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`JitInlineSIMDMultiplier` |  | DWORD | INTERNAL | 3 | REGUTIL_default
`JitInlinePrintStats` |  | DWORD | INTERNAL | (DWORD)0 | REGUTIL_default
`JITInlineSize` |  | DWORD | INTERNAL | | 
`JitLateDisasm` |  | STRING | INTERNAL | | REGUTIL_default
`JITLateDisasmTo` |  | STRING | INTERNAL | | REGUTIL_default
`JitLRSampling` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`JITMaxTempAssert` |  | DWORD | INTERNAL | 1 | REGUTIL_default
`JitMaxUncheckedOffset` |  | DWORD | INTERNAL | (DWORD)8 | REGUTIL_default
`JITMinOpts` | Forces MinOpts | DWORD | UNSUPPORTED | | 
`JITMinOptsBbCount` | Internal jit control of MinOpts | DWORD | INTERNAL | | 
`JITMinOptsCodeSize` | Internal jit control of MinOpts | DWORD | INTERNAL | | 
`JITMinOptsInstrCount` | Internal jit control of MinOpts | DWORD | INTERNAL | | 
`JITMinOptsLvNumcount` | Internal jit control of MinOpts | DWORD | INTERNAL | | 
`JITMinOptsLvRefcount` | Internal jit control of MinOpts | DWORD | INTERNAL | | 
`JITBreakOnMinOpts` | Halt if jit switches to MinOpts | DWORD | INTERNAL | | 
`JitName` | Primary Jit to use | STRING | EXTERNAL | | 
`AltJitName` | Alternative Jit to use, will fall back to primary jit. | STRING | EXTERNAL | | REGUTIL_default
`AltJit` | Enables AltJit and selectively limits it to the specified methods. | STRING | EXTERNAL | | REGUTIL_default
`AltJitExcludeAssemblies` | Do not use AltJit on this semicolon-delimited list of assemblies. | STRING | EXTERNAL | | REGUTIL_default
`AltJitLimit` | Max number of functions to use altjit for (decimal) | DWORD | INTERNAL | 0 | REGUTIL_default
`StackSamplingEnabled` | Is stack sampling based tracking of evolving hot methods enabled. | DWORD | UNSUPPORTED | 0 | 
`StackSamplingAfter` | When to start sampling (for some sort of app steady state), i.e., initial delay for sampling start in milliseconds. | DWORD | UNSUPPORTED | 0 | 
`StackSamplingEvery` | How frequent should thread stacks be sampled in milliseconds. | DWORD | UNSUPPORTED | 100 | 
`StackSamplingNumMethods` | Number of evolving methods to track as hot and JIT them in the background at a given point of execution. | DWORD | UNSUPPORTED | 32 | 
`AltJitNgen` | Enables AltJit for NGEN and selectively limits it to the specified methods. | STRING | INTERNAL | | REGUTIL_default
`JitNoCMOV` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`UseRyuJIT` | Set to 1 by .NET 4.6 installer to indicate RyuJIT should be used, not JIT64. | DWORD | INTERNAL | 0 | IgnoreEnv / IgnoreHKCU / IgnoreConfigFiles
`useLegacyJit` | Set to 1 to do all JITing with compatjit.dll. Only applicable to x64. | DWORD | EXTERNAL | 0 | 
`DisableNativeImageLoadList` | Refuse to load native images corresponding to one of the assemblies on this semicolon-delimited list of assembly names. | STRING | EXTERNAL | | REGUTIL_default
`JitValNumCSE` | Enables ValNum CSE for the specified methods | STRING | INTERNAL | | REGUTIL_default 
`JitLexicalCSE` | Enables Lexical CSE for the specified methods | STRING | INTERNAL | | REGUTIL_default 
`JitNoCSE` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoCSE2` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoHoist` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoInline` | Disables inlining | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoProcedureSplitting` | Disallow procedure splitting for specified methods | STRING | INTERNAL | | REGUTIL_default
`JitNoProcedureSplittingEH` | Disallow procedure splitting for specified methods if they contain exception handling | STRING | INTERNAL | | REGUTIL_default
`JitNoRegLoc` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoStructPromotion` | Disables struct promotion in Jit32 | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoUnroll` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoMemoryBarriers` | If 1, don't generate memory barriers | DWORD | INTERNAL | 0 | REGUTIL_default
`JitNoRngChks` | If 1, don't generate range checks | DWORD | PRIVATE | 0 | 
`JitOptimizeType` |  | DWORD | EXTERNAL | | 
`JitOrder` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitDiffableDasm` | Make the disassembly diff-able | DWORD | INTERNAL | 0 | REGUTIL_default
`JitSlowDebugChecksEnabled` | Turn on slow debug checks | DWORD | INTERNAL | 1 | REGUTIL_default
`JITPInvokeCheckEnabled` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JITPInvokeEnabled` |  | DWORD | INTERNAL | 1 | 
`JitPrintInlinedMethods` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`JitRange` |  | STRING | INTERNAL | | REGUTIL_default
`JITRequired` |  | DWORD | INTERNAL | (unsigned)-1 | REGUTIL_default
`JITRoundFloat` |  | DWORD | INTERNAL | | 
`JitSkipArrayBoundCheck` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitStackChecks` |  | DWORD | INTERNAL | | 
`JitStackProbes` |  | DWORD | INTERNAL | | 
`JitStress` | Internal Jit stress mode: 0 = no stress, 2 = all stress, other = vary stress based on a hash of the method and this value | DWORD | INTERNAL | 0 | REGUTIL_default
`JitStressBBProf` | Internal Jit stress mode | DWORD | INTERNAL | 0 | REGUTIL_default
`JitStressFP` | Internal Jit stress mode | DWORD | INTERNAL | 0 | REGUTIL_default
`JitStressModeNames` | Internal Jit stress mode: stress using the given set of stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL | STRING | INTERNAL | | REGUTIL_default
`JitStressModeNamesNot` | Internal Jit stress mode: do NOT stress using the given set of stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL | STRING | INTERNAL | | REGUTIL_default
`JitStressOnly` | Internal Jit stress mode: stress only the specified method(s) | STRING | INTERNAL | | REGUTIL_default
`JitStressRange` | Internal Jit stress mode | STRING | INTERNAL | | REGUTIL_default
`JitStressRegs` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`JitStrictCheckForNonVirtualCallToVirtualMethod` |  | DWORD | INTERNAL | 1 | REGUTIL_default
`JitTimeLogFile` | If set, gather JIT throughput data and write to this file. | STRING | INTERNAL | | 
`JitTimeLogCsv` | If set, gather JIT throughput data and write to a CSV file. This mode must be used in internal retail builds. | STRING | INTERNAL | | 
`JitFuncInfoLogFile` | If set, gather JIT function info and write to this file. | STRING | INTERNAL | | 
`JitUnwindDump` | Dump the unwind codes for the method | STRING | INTERNAL | | 
`JitEHDump` | Dump the EH table for the method, as reported to the VM | STRING | INTERNAL | | 
`JitVerificationDisable` |  | DWORD | INTERNAL | | 
`JitLockWrite` | Force all volatile writes to be 'locked' | DWORD | INTERNAL | 0 | 
`TailCallMax` |  | STRING | INTERNAL | | REGUTIL_default
`TailCallOpt` |  | STRING | EXTERNAL | | REGUTIL_default
`TailcallStress` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`NetFx40_PInvokeStackResilience` | Makes P/Invoke resilient against mismatched signature and calling convention (significant perf penalty). | DWORD | EXTERNAL | (DWORD)-1 | 
`JitDoSsa` | Perform Static Single Assignment (SSA) numbering on the variables | DWORD | INTERNAL | 1 | REGUTIL_default
`JitDoValueNumber` | Perform value numbering on method expressions | DWORD | INTERNAL | 1 | REGUTIL_default
`JitDoLoopHoisting` | Perform loop hoisting on loop invariant values | DWORD | INTERNAL | 1 | REGUTIL_default
`JitDoCopyProp` | Perform copy propagation on variables that appear redundant | DWORD | INTERNAL | 1 | REGUTIL_default
`JitDoAssertionProp` | Perform assertion propagation optimization | DWORD | INTERNAL | 1 | REGUTIL_default
`JitDoRangeAnalysis` | Perform range check analysis | DWORD | INTERNAL | 1 | REGUTIL_default
`JitSsaStress` | Perturb order of processing of blocks in SSA; 0 = no stress; 1 = use method hash; * = supplied value as random hash | DWORD | INTERNAL | 0 | REGUTIL_default
`AltJitAssertOnNYI` | Controls the AltJit behavior of NYI stuff | DWORD | INTERNAL | 0 | 
`AltJitAssertOnNYI` | Controls the AltJit behavior of NYI stuff | DWORD | INTERNAL | 1 | 
`AltJitSkipOnAssert` | If AltJit hits an assert, fall back to the fallback JIT. Useful in conjunction with COMPLUS_ContinueOnAssert=1 | DWORD | INTERNAL | 0 | REGUTIL_default
`JitLargeBranches` | Force using the largest conditional branch format | DWORD | INTERNAL | 0 | REGUTIL_default
`JitSplitFunctionSize` | On ARM, use this as the maximum function/funclet size for creating function fragments (and creating multiple RUNTIME_FUNCTION entries) | DWORD | INTERNAL | 0 | REGUTIL_default
`JitRegisterFP` | Control FP enregistration | DWORD | EXTERNAL | 3 | REGUTIL_default
`JitELTHookEnabled` | On ARM, setting this will emit Enter/Leave/TailCall callbacks | DWORD | INTERNAL | 0 | 
`JitComponentUnitTests` | Run JIT component unit tests | DWORD | INTERNAL | 0 | REGUTIL_default
`JitMemStats` | Display JIT memory usage statistics | DWORD | INTERNAL | 0 | REGUTIL_default
`JitLoopHoistStats` | Display JIT loop hoisting statistics | DWORD | INTERNAL | 0 | REGUTIL_default
`JitDebugLogLoopCloning` | In debug builds log places where loop cloning optimizations are performed on the fast path. | DWORD | INTERNAL | 0 | REGUTIL_default
`JitVNMapSelLimit` | If non-zero, assert if # of VNF_MapSelect applications considered reaches this | DWORD | patsubst(INTERNAL_JitVNMapSelLimit, _.*, ) | 0 | patsubst(patsubst(CLRConfig::REGUTIL_default, CLRConfig::, ), |, /)
`JitVNMapSelBudget` | Max # of MapSelect's considered for a particular top-level invocation. | DWORD | patsubst(INTERNAL_JitVNMapSelBudget, _.*, ) | 100 | 
`FeatureSIMD` | Enable SIMD support with companion SIMDVector.dll | DWORD | EXTERNAL | EXTERNAL_FeatureSIMD_Default | REGUTIL_default
`EnableAVX` | Enable AVX instruction set for wide operations as default | DWORD | EXTERNAL | EXTERNAL_JitEnableAVX_Default | REGUTIL_default
`MultiCoreJitProfile` | If set, use the file to store/control multi-core JIT. | STRING | INTERNAL | | 
`MultiCoreJitProfileWriteDelay` | Set the delay after which the multi-core JIT profile will be written to disk. | DWORD | INTERNAL | 12 | 
`JitFunctionTrace` | If non-zero, print JIT start/end logging | DWORD | INTERNAL | 0 | 
`HashTableSize` | Size of Hashtable | DWORD | INTERNAL | 500 | REGUTIL_default
`LargeSymCount` | Large Sym Count Size | DWORD | INTERNAL | 100000 | REGUTIL_default
`Interpret` | Selectively uses the interpreter to execute the specified methods | STRING | INTERNAL | | REGUTIL_default
`InterpretExclude` | Excludes the specified methods from the set selected by 'Interpret' | STRING | INTERNAL | | REGUTIL_default
`InterpreterMethHashMin` | Only interpret methods selected by 'Interpret' whose hash is at least this value. or after nth | DWORD | INTERNAL | 0 | 
`InterpreterMethHashMax` | If non-zero, only interpret methods selected by 'Interpret' whose hash is at most this value | DWORD | INTERNAL | UINT32_MAX | 
`InterpreterStubMin` | Only interpret methods selected by 'Interpret' whose stub num is at least this value. | DWORD | INTERNAL | 0 | 
`InterpreterStubMax` | If non-zero, only interpret methods selected by 'Interpret' whose stub number is at most this value. | DWORD | INTERNAL | UINT32_MAX | 
`InterpreterJITThreshold` | The number of times a method should be interpreted before being JITted | DWORD | INTERNAL | 10 | 
`InterpreterDoLoopMethods` | If set, don't check for loops, start by interpreting *all* methods | DWORD | INTERNAL | 0 | 
`InterpreterUseCaching` | If non-zero, use the caching mechanism. | DWORD | INTERNAL | 1 | REGUTIL_default
`InterpreterLooseRules` | If non-zero, allow ECMA spec violations required by managed C++. | DWORD | INTERNAL | 1 | REGUTIL_default
`InterpreterPrintPostMortem` | Prints summary information about the execution to the console | DWORD | INTERNAL | 0 | 
`InterpreterLogFile` | If non-null, append interpreter logging to this file, else use stdout | STRING | INTERNAL | | REGUTIL_default
`DumpInterpreterStubs` | Prints all interpreter stubs that are created to the console | DWORD | INTERNAL | 0 | 
`TraceInterpreterEntries` | Logs entries to interpreted methods to the console | DWORD | INTERNAL | 0 | 
`TraceInterpreterIL` | Logs individual instructions of interpreted methods to the console | DWORD | INTERNAL | 0 | 
`TraceInterpreterOstack` | Logs operand stack after each IL instruction of interpreted methods to the console | DWORD | INTERNAL | 0 | 
`TraceInterpreterVerbose` | Logs interpreter progress with detailed messages to the console | DWORD | INTERNAL | 0 | 
`TraceInterpreterJITTransition` | Logs when the interpreter determines a method should be JITted | DWORD | INTERNAL | 0 | 
`InterpreterFallback` | Fallback to the interpreter when the JIT compiler fails | DWORD | INTERNAL | 0 | 
`APIThreadStress` | Used to test Loader for race conditions | DWORD | INTERNAL | | 
`ForceLog` | Fusion flag to enforce assembly binding log. Heavily used and documented in MSDN and BLOGS. | DWORD | EXTERNAL | | 
`LoaderOptimization` | Controls code sharing behavior | DWORD | EXTERNAL | | 
`CoreClrBinderLog` | Debug flag that enabled detailed log for new binder (similar to stress logging). | STRING | INTERNAL | | 
`DisableIJWVersionCheck` | Don't perform the new version check that prevents unsupported IJW in-proc SxS. | DWORD | EXTERNAL | 0 | 
`EnableFastBindClosure` | If set to >0 the binder uses CFastAssemblyBindingClosure instances | DWORD | UNSUPPORTED | 0 | 
`DisableFXClosureWalk` | Disable full closure walks even in the presence of FX binding redirects | DWORD | INTERNAL | 0 | 
`TagAssemblyNames` | Enable CAssemblyName::_tag field for more convenient debugging. | DWORD | INTERNAL | 0 | 
`WinMDPath` | Path for Windows WinMD files | STRING | INTERNAL | | 
`LoaderHeapCallTracing` | Loader heap troubleshooting | DWORD | INTERNAL | 0 | REGUTIL_default
`CodeHeapReserveForJumpStubs` | Percentage of code heap to reserve for jump stubs | DWORD | INTERNAL | 2 | 
`LogEnable` | Turns on the traditional CLR log. | DWORD | INTERNAL | | 
`LogFacility` | Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog. | DWORD | INTERNAL | | 
`LogFacility2` | Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog. | DWORD | INTERNAL | | 
`logFatalError` | Specifies whether EventReporter logs fatal errors in the Windows event log. | DWORD | EXTERNAL | 1 | 
`LogFile` | Specifies a file name for the CLR log. | STRING | INTERNAL | | REGUTIL_default
`LogFileAppend` | Specifies whether to append to or replace the CLR log file. | DWORD | INTERNAL | | 
`LogFlushFile` | Specifies whether to flush the CLR log file file on each write. | DWORD | INTERNAL | | 
`LogLevel` | 4=10 msgs, 9=1000000, 10=everything | DWORD | EXTERNAL | | 
`LogPath` | ?Fusion debug log path. | STRING | INTERNAL | | 
`LogToConsole` | Writes the CLR log to console. | DWORD | INTERNAL | | 
`LogToDebugger` | Writes the CLR log to debugger (OutputDebugStringA). | DWORD | INTERNAL | | 
`LogToFile` | Writes the CLR log to a file. | DWORD | INTERNAL | | 
`LogWithPid` | Appends pid to filename for the CLR log. | DWORD | INTERNAL | | 
`FusionLogFileNamesIncludePid` | Fusion logging will append process id to log filenames. | DWORD | EXTERNAL | 0 | REGUTIL_default
`MD_ApplyDeltaBreak` | ASSERT when appplying EnC | DWORD | INTERNAL | 0 | REGUTIL_default
`AssertOnBadImageFormat` | ASSERT when invalid MD read | DWORD | INTERNAL | | 
`MD_DeltaCheck` | ? Some checks of GUID when applying EnC? | DWORD | INTERNAL | 1 | REGUTIL_default
`MD_EncDelta` | ? Forces EnC Delta format in MD | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_ForceNoColDesSharing` | ? ? Don't know - the only usage I could find is #if 0 | DWORD | patsubst(INTERNAL_MD_ForceNoColDesSharing, _.*, ) | 0 | patsubst(patsubst(CLRConfig::REGUTIL_default, CLRConfig::, ), |, /)
`MD_KeepKnownCA` | ? Something with known CAs? | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_MiniMDBreak` | ASSERT when creating CMiniMdRw class | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_PreSaveBreak` | ASSERT when calling CMiniMdRw::PreSave | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_RegMetaBreak` | ASSERT when creating RegMeta class | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_RegMetaDump` | ? Dump MD in 4 functions? | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_TlbImp_BreakOnErr` | ASSERT when importing TLB into MD | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_TlbImp_BreakOnTypeImport` | ASSERT when importing a type from TLB | STRING | INTERNAL | | (LookupOptions) (REGUTIL_default / DontPrependCOMPLUS_)
`MD_UseMinimalDeltas` | ? Some MD modifications when applying EnC? | DWORD | INTERNAL | 1 | REGUTIL_default
`MD_WinMD_Disable` | Never activate the WinMD import adapter | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_WinMD_AssertOnIllegalUsage` | ASSERT if a WinMD import adapter detects a tool incompatibility | DWORD | INTERNAL | 0 | REGUTIL_default
`MD_PreserveDebuggerMetadataMemory` | Save all versions of metadata memory in the debugger when debuggee metadata is updated | DWORD | EXTERNAL | 0 | REGUTIL_default
`MDA` | Config string to determine which MDAs to enable | STRING | EXTERNAL | | REGUTIL_default
`MDAValidateFramework` | If set, validate the XML schema for MDA | STRING | INTERNAL | | REGUTIL_default
`SpinInitialDuration` | Hex value specifying the first spin duration | DWORD | EXTERNAL | 0x32 | EEConfig_default
`SpinBackoffFactor` | Hex value specifying the growth of each successive spin duration | DWORD | EXTERNAL | 0x3 | EEConfig_default
`SpinLimitProcCap` | Hex value specifying the largest value of NumProcs to use when calculating the maximum spin duration | DWORD | EXTERNAL | 0xFFFFFFFF | EEConfig_default
`SpinLimitProcFactor` | Hex value specifying the multiplier on NumProcs to use when calculating the maximum spin duration | DWORD | EXTERNAL | 0x4E20 | EEConfig_default
`SpinLimitConstant` | Hex value specifying the constant to add when calculating the maximum spin duration | DWORD | EXTERNAL | 0x0 | EEConfig_default
`SpinRetryCount` | Hex value specifying the number of times the entire spin process is repeated (when applicable) | DWORD | EXTERNAL | 0xA | EEConfig_default
`NgenBind_UseTimestamp` | Use timestamp to validate a native image | DWORD | INTERNAL | 0 | 
`NgenBind_UseTimestampList` |  | STRING | INTERNAL | | 
`NgenBind_UseTimestampExcludeList` |  | STRING | INTERNAL | | 
`NgenBind_ZapForbid` | Assert if an assembly succeeds in binding to a native image | DWORD | INTERNAL | 0 | 
`NgenBind_ZapForbidExcludeList` |  | STRING | INTERNAL | | 
`NgenBind_ZapForbidList` |  | STRING | INTERNAL | | 
`NgenBind_OptimizeNonGac` | Skip loading IL image outside of GAC when NI can be loaded | DWORD | EXTERNAL | 0 | 
`JIT_MDIL_MIN_TOKEN` | TBD | DWORD | INTERNAL | 0 | REGUTIL_default
`JIT_MDIL_MAX_TOKEN` | TBD | DWORD | INTERNAL | 0xffffffff | REGUTIL_default
`JitDisassembleMDIL` | TBD | DWORD | INTERNAL | 0 | REGUTIL_default
`JitListMDILtoNative` | TBD | DWORD | INTERNAL | 0 | REGUTIL_default
`MDIL_BREAK_ON` | TBD | STRING | INTERNAL | | REGUTIL_default
`SymDiffDump` | Used to create the map file while binding the assembly. Used by SemanticDiffer | DWORD | INTERNAL | 0 | REGUTIL_default
`TritonStressSeed` | Seed used for random number used to drive mdil stress modes | DWORD | INTERNAL | 0 | 
`TritonStressLogFlags` | Triton stress logging | DWORD | INTERNAL | 3 | 
`TritonStressPartialMDIL` | This stress mode will cause some number of methods to abort MDIL compilation. This should trigger them to fall back to JIT at runtime. | DWORD | INTERNAL | 0 | 
`TritonStressPartialCTL` | This stress mode will cause some number of types from this module to fail to generate CTL. | DWORD | INTERNAL | 0 | 
`TritonStressTypeLoad` | Triton Stress of type loading in mdilbind, parameter is LoadStressFlag | DWORD | INTERNAL | 0 | 
`TritonStressMethodLoad` | Triton Stress of method loading in mdilbind, parameter is LoadStressFlag | DWORD | INTERNAL | 0 | 
`TritonStressFieldLoad` | Triton Stress of field loading in mdilbind, parameter is LoadStressFlag | DWORD | INTERNAL | 0 | 
`TritonStressAssemblyLoad` | Triton Stress of assembly loading in mdilbind, parameter is LoadStressFlag | DWORD | INTERNAL | 0 | 
`MdilNIGenDefaultFailureMode` | Override default failure mode of mdil ni generation | DWORD | INTERNAL | 0 | 
`NGen_JitName` |  | STRING | EXTERNAL | | REGUTIL_default
`NGEN_USE_PRIVATE_STORE` |  | DWORD | EXTERNAL | -1 | REGUTIL_default
`NGENBreakOnInjectPerAssemblyFailure` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`NGENBreakOnInjectTransientFailure` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`NGENBreakOnWorker` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`NGenClean` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`NGenCompileWorkerHang` | If set to 1, NGen compile worker process hangs forever | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`NGenDeferAllCompiles` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`NGenDependencyWorkerHang` | If set to 1, NGen dependency walk worker process hangs forever | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`NgenDisasm` | Same as JitDisasm, but for ngen | STRING | INTERNAL | | REGUTIL_default
`NgenDump` | Same as JitDump, but for ngen | STRING | INTERNAL | | REGUTIL_default
`NgenDumpFg` | Ngen Xml Flowgraph support | STRING | INTERNAL | | REGUTIL_default
`NgenDumpFgDir` | Ngen Xml Flowgraph support | STRING | INTERNAL | | REGUTIL_default
`NgenDumpFgFile` | Ngen Xml Flowgraph support | STRING | INTERNAL | | REGUTIL_default
`NGenFramed` | same as JitFramed, but for ngen | DWORD | UNSUPPORTED | -1 | REGUTIL_default
`NgenGCDump` |  | STRING | INTERNAL | | REGUTIL_default
`NgenHashDump` | same as JitHashDump, but for ngen | DWORD | INTERNAL | (DWORD)-1 | REGUTIL_default
`NGENInjectFailuresServiceOnly` |  | DWORD | INTERNAL | 1 | REGUTIL_default
`NGENInjectPerAssemblyFailure` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`NGENInjectTransientFailure` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`NGenLocalWorker` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`NGenMaxLogSize` | The maximum size ngen.log and ngen_service.log files can grow to. | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`NGenLogVerbosity` | Default ngen log verbosity level | DWORD | EXTERNAL | 2 | REGUTIL_default
`NGenOnlyOneMethod` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`NgenOrder` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`CheckNGenImageTimeStamp` | Used to skip ngen timestamp check when switching compilers around. | DWORD | EXTERNAL | 1 | REGUTIL_default
`NGenRegistryAccessCount` |  | DWORD | EXTERNAL | -1 | REGUTIL_default
`NGenStressDelete` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`NGenUninstallKeep` | Semicolon-delimited list of assemblies to keep during 'ngen uninstall *' | STRING | INTERNAL | | 
`NgenUnwindDump` | Dump the unwind codes for the method | STRING | INTERNAL | | 
`NgenEHDump` | Dump the EH table for the method, as reported to the VM | STRING | INTERNAL | | 
`NGENUseService` |  | DWORD | EXTERNAL | 1 | REGUTIL_default
`NGenWorkerCount` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`partialNGenStress` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`ZapDoNothing` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`HardPrejitEnabled` |  | DWORD | EXTERNAL | | 
`EnableHardbinding` | Enables the use of hardbinding | DWORD | INTERNAL | 0 | REGUTIL_default
`WorkerRetryNgenFailures` | If set to 1, The Ngen worker will retry once when ngen fails | DWORD | INTERNAL | 0 | REGUTIL_default
`NgenForceFailureMask` | Bitmask used to control which locations will check and raise the failure (defaults to bits: -1) | DWORD | INTERNAL | -1 | REGUTIL_default
`NgenForceFailureCount` | If set to >0 and we have IBC data we will force a failure after we reference an IBC data item <value> times | DWORD | INTERNAL | 0 | REGUTIL_default
`NgenForceFailureKind` | If set to 1, We will throw a TypeLoad exception; If set to 2, We will cause an A/V | DWORD | INTERNAL | 1 | REGUTIL_default
`NGenEnableCreatePdb` | If set to >0 ngen.exe displays help on, recognizes createpdb in the command line | DWORD | UNSUPPORTED | 0 | 
`NGenSimulateDiskFull` | If set to 1, ngen will throw a Disk full exception in ZapWriter.cpp:Save() | DWORD | INTERNAL | 0 | 
`NGenAssemblyUsageLog` | Directory to store ngen usage logs in. | STRING | INTERNAL | | 
`NGenAssemblyUsageLogRefreshInterval` | Interval to update usage log timestamp (seconds) | DWORD | INTERNAL | 24 * 60 * 60 | 
`AppLocalAutongenNGenDisabled` | Autongen disable flag. | DWORD | INTERNAL | 0 | 
`PartialNGen` | Generate partial NGen images | DWORD | INTERNAL | -1 | 
`NgenAllowMscorlibSoftbind` | Disable forced hard-binding to mscorlib | DWORD | INTERNAL | 0 | 
`RegistryRoot` | Redirects all registry access under HKLM\Software to a specified alternative | STRING | UNSUPPORTED | | REGUTIL_default
`AssemblyPath` | Redirects v2 GAC access to a specified alternative path | STRING | UNSUPPORTED | | REGUTIL_default
`AssemblyPath2` | Redirects v4 GAC access to a specified alternative path | STRING | UNSUPPORTED | | REGUTIL_default
`NicPath` | Redirects NIC access to a specified alternative | STRING | UNSUPPORTED | | REGUTIL_default
`NGenTaskDelayStart` | Use NGen Task delay start trigger, instead of critical idle task | DWORD | INTERNAL | 0 | 
`Ningen` | Enable no-impact ngen | DWORD | INTERNAL | 1 | 
`Ningen` | Enable no-impact ngen | DWORD | INTERNAL | 0 | 
`NoASLRForNgen` | Turn off IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE bit in generated ngen images. Makes nidump output repeatable from run to run. | DWORD | INTERNAL | 0 | 
`NgenAllowOutput` | If set to 1, the NGEN worker will bind to the parent console, thus allowing stdout output to work | DWORD | EXTERNAL | 0 | REGUTIL_default
`CrossGenAssumeInputSigned` | CrossGen should assume that its input assemblies will be signed before deployment | DWORD | INTERNAL | 1 | 
`NGENServiceAbortIdleWorkUnderDebugger` | Determines whether the Ngen service will abort idle-time tasks while under a debugger. Off by default. Allows for single-machine debugging of the idle-time logic. | DWORD | INTERNAL | 1 | REGUTIL_default
`NGENServiceAggressiveHardDiskIdleTimeout` | This flag was intended as a backstop for HDD idle time detection (i.e. even if the hard disk is not idle, proceed with the compilation of the high-priority assemblies after the specified timeout). The current implementation compiles high-priority assemblies regardless of the state of the machine. | DWORD | EXTERNAL | 1*60*60*1000 | REGUTIL_default
`NGENServiceAggressiveWorkWaitTimeout` | This flag was intended as a backstop for machine idle time detection (i.e. even if the machine is not idle, proceed with the compilation of the high-priority assemblies after the specified timeout). The current implementation compiles high-priority assemblies regardless of the state of the machine. | DWORD | EXTERNAL | 0 | REGUTIL_default
`NGENServiceBreakOnStart` | Determines whether the Ngen service will call DebugBreak in its start routing. Off by default. Marginally useful for debugging service startup (there are other techniques as well). | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`NGENServiceConservative` | Determines whether the Ngen service will avoid compiling low-priority assemblies if multiple sessions exist on the machine and it can't determine their state. Off by default. | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`NGenServiceDebugLog` | Configures the level of debug logging. | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`NGENServiceIdleBatteryThreshold` | When a battery-powered system is below the threshold, Ngen will not process low-priority assemblies. | DWORD | UNSUPPORTED | 50 | REGUTIL_default
`NGENServiceIdleDebugInfo` | Determines whether the Ngen service will print the idle-time detection criteria to the debug log. Off by default. Ignored if NGenServiceDebugLog is 0. | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`NGENServiceIdleDiskLogic` | Determines if the Ngen service will use hard disk idle time  for its machine idle time heuristics. | DWORD | UNSUPPORTED | 1 | REGUTIL_default
`NGENServiceIdleDiskThreshold` | The amount of time after which a disk is declared idle. | DWORD | UNSUPPORTED | 80 | REGUTIL_default
`NGENServiceIdleNoInputPeriod` | The amount of time after which the machine is declared idle if no input was received. | DWORD | UNSUPPORTED | 5*60*1000 | REGUTIL_default
`NGENServicePassiveExceptInputTimeout` | The amount of time after which only input state is considered for idle time detection (input backstop mode, which ignores everything except input). | DWORD | UNSUPPORTED | 15*60*60*1000 | REGUTIL_default
`NGENServicePassiveHardDiskIdleTimeout` | The amount of time after which the state of the hard disk is ignored for idle time detection. | DWORD | UNSUPPORTED | 36*60*60*1000 | REGUTIL_default
`NGENServicePassiveWorkWaitTimeout` | The amount of time after which the machine is declared idle and low priority assemblies are compiled no matter what the actual state is (absolute backstop mode: declaring the machine as idle disregarding the actual state). | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`NGENServicePolicy` | The policy that will be used for the machine (client or server). By default, it's determined from the OS SKU. | DWORD | UNSUPPORTED | | 
`NGENServiceRestrictWorkersPrivileges` | Determines if worker processes are launched with restricted tokens. | DWORD | UNSUPPORTED | 1 | 
`NGENServiceSynchronization` | Determines if multiple services coordinate themselves so that only one service is working at a time. | DWORD | UNSUPPORTED | 1 | REGUTIL_default
`NGENServiceTestHookDll` | The name of a module used for testing in-process | STRING | UNSUPPORTED | | 
`NGENServiceWaitAggressiveWork` | Specifies how often the service will check the machine state when trying to do high-priority work. | STRING | UNSUPPORTED | | 
`NGENServiceWaitPassiveWork` | Specifies how often the service will check the machine state when trying to do low-priority work. | DWORD | UNSUPPORTED | 1*60*1000 | REGUTIL_default
`NGENServiceWaitWorking` | While working, the Ngen service polls the state of the machine for changes (another service trying to do higher priority work, the Ngen command line tool trying to do work, machine coming out of idle state). This variable controls the frequency of the polling. | DWORD | UNSUPPORTED | 1000 | REGUTIL_default
`NGENServiceWorkerPriority` | The process priority class for workers. | DWORD | UNSUPPORTED | | 
`EnableMultiproc` | Turns on multiproc ngen | DWORD | EXTERNAL | 1 | REGUTIL_default
`SvcRetryNgenFailures` | If set to 1, The Ngen service will retry once when ngen fails | DWORD | EXTERNAL | 1 | REGUTIL_default
`NGenTaskDelayStartAmount` | Number of seconds to delay for ngen update /queue /delay | DWORD | INTERNAL | 5 * 60 | REGUTIL_default
`NGenProtectedProcess_FeatureEnabled` | Run ngen as PPL (protected process) if needed. Set to 0 to disable the feature for compat with older Win8 builds. | DWORD | INTERNAL | -1 | IgnoreConfigFiles
`NGenProtectedProcess_RequiredList` | Semicolon-separated list of assembly names that are required to be ngen'd in PPL process. Each name in the list is matched as prefix or suffix of assembly name/assembly file name. | STRING | INTERNAL | | IgnoreConfigFiles
`NGenProtectedProcess_ForbiddenList` | Semicolon-separated list of assembly names that are forbidden to be ngen'd in PPL process. Each name in the list is matched as prefix or suffix of assembly name/assembly file name. | STRING | INTERNAL | | IgnoreConfigFiles
`NGenCopyFromRepository_SetCachedSigningLevel` | Support for test tree ngen.exe flag /CopyFromRepository to also vouch for the output NIs. | DWORD | INTERNAL | 0 | IgnoreConfigFiles
`performanceScenario` | Activates a set of workload-specific default values for performance settings | STRING | EXTERNAL | | 
`ProcessNameFormat` | Used by corperfmonext.dll to determine whether to decorate an instance name with the corresponding PID and runtime ID | DWORD | EXTERNAL | (DWORD)-1 | IgnoreHKLM / IgnoreHKCU / IgnoreConfigFiles
`COR_ENABLE_PROFILING` | Flag to indicate whether profiling should be enabled for the currently running process. | DWORD | EXTERNAL | 0 | DontPrependCOMPLUS_ / IgnoreConfigFiles
`COR_PROFILER` | Specifies GUID of profiler to load into currently running process | STRING | EXTERNAL | | DontPrependCOMPLUS_
`COR_PROFILER_PATH` | Specifies the path to the DLL of profiler to load into currently running process | STRING | EXTERNAL | | DontPrependCOMPLUS_
`COR_PROFILER_PATH_32` | Specifies the path to the DLL of profiler to load into currently running 32 bits process | STRING | EXTERNAL | | DontPrependCOMPLUS_
`COR_PROFILER_PATH_64` | Specifies the path to the DLL of profiler to load into currently running 64 bits process | STRING | EXTERNAL | | DontPrependCOMPLUS_
`CORECLR_ENABLE_PROFILING` | CoreCLR only: Flag to indicate whether profiling should be enabled for the currently running process. | DWORD | EXTERNAL | 0 | DontPrependCOMPLUS_ / IgnoreConfigFiles
`CORECLR_PROFILER` | CoreCLR only: Specifies GUID of profiler to load into currently running process | STRING | EXTERNAL | | DontPrependCOMPLUS_
`CORECLR_PROFILER_PATH` | CoreCLR only: Specifies the path to the DLL of profiler to load into currently running process | STRING | EXTERNAL | | DontPrependCOMPLUS_
`CORECLR_PROFILER_PATH_32` | CoreCLR only: Specifies the path to the DLL of profiler to load into currently running 32 process | STRING | EXTERNAL | | DontPrependCOMPLUS_
`CORECLR_PROFILER_PATH_64` | CoreCLR only: Specifies the path to the DLL of profiler to load into currently running 64 process | STRING | EXTERNAL | | DontPrependCOMPLUS_
`ProfAPI_ProfilerCompatibilitySetting` | Specifies the profiler loading policy (the default is not to load a V2 profiler in V4) | STRING | EXTERNAL | | REGUTIL_default / TrimWhiteSpaceFromStringValue
`AttachThreadAlwaysOn` | Forces profapi attach thread to be created on startup, instead of on-demand. | DWORD | EXTERNAL | | 
`MsBetweenAttachCheck` |  | DWORD | EXTERNAL | 500 | 
`ProfAPIMaxWaitForTriggerMs` | Timeout in ms for profilee to wait for each blocking operation performed by trigger app. | DWORD | EXTERNAL | 5*60*1000 | 
`ProfAPI_DetachMinSleepMs` | The minimum time, in millseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded. | DWORD | EXTERNAL | 0 | 
`ProfAPI_DetachMaxSleepMs` | The maximum time, in millseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded. | DWORD | EXTERNAL | 0 | 
`ProfAPI_EnableRejitDiagnostics` | Enable extra dumping to stdout of rejit structures | DWORD | INTERNAL | 0 | 
`ProfAPI_AttachProfilerMinTimeoutInMs` | Timeout in ms for the minimum time out value of AttachProfiler | DWORD | EXTERNAL | 10*1000 | 
`ProfAPIFault` | Test-only bitmask to inject various types of faults in the profapi code | DWORD | INTERNAL | 0 | 
`TestOnlyAllowedEventMask` | Test-only bitmask to allow profiler tests to override CLR enforcement of COR_PRF_ALLOWABLE_AFTER_ATTACH and COR_PRF_MONITOR_IMMUTABLE | DWORD | INTERNAL | 0 | 
`ProfAPI_TestOnlyEnableICorProfilerInfo` | Test-only flag to allow attaching profiler tests to call ICorProfilerInfo interface, which would otherwise be disallowed for attaching profilers | DWORD | INTERNAL | 0 | 
`TestOnlyEnableObjectAllocatedHook` | Test-only flag that forces CLR to initialize on startup as if ObjectAllocated callback were requested, to enable post-attach ObjectAllocated functionality. | DWORD | INTERNAL | 0 | 
`TestOnlyEnableSlowELTHooks` | Test-only flag that forces CLR to initialize on startup as if slow-ELT were requested, to enable post-attach ELT functionality. | DWORD | INTERNAL | 0 | 
`ETWEnabled` | This flag is used on OSes < Vista to enable/disable ETW. It is disabled by default | DWORD | EXTERNAL | 0 | REGUTIL_default
`ETWEnabled` | This flag is used on OSes >= Vista to enable/disable ETW. It is enabled by default | DWORD | EXTERNAL | 1 | REGUTIL_default
`ETW_ObjectAllocationEventsPerTypePerSec` | Desired number of GCSampledObjectAllocation ETW events to be logged per type per second.  If 0, then the default built in to the implementation for the enabled event (e.g., High, Low), will be used. | STRING | UNSUPPORTED | | REGUTIL_default
`ProfAPI_ValidateNGENInstrumentation` | This flag enables additional validations when using the IMetaDataEmit APIs for NGEN'ed images to ensure only supported edits are made. | DWORD | UNSUPPORTED | 0 | 
`PerfMapEnabled` | This flag is used on Linux to enable writing /tmp/perf-$pid.map. It is disabled by default | DWORD | EXTERNAL | 0 | REGUTIL_default
`EnableIEHosting` | Allow activation of IE hosting | DWORD | UNSUPPORTED | | 
`NoGuiFromShim` | Turn off GUI in shim | DWORD | UNSUPPORTED | | 
`OnlyUseLatestCLR` | Big red switch for loading CLR | DWORD | UNSUPPORTED | | 
`FailOnInProcSxS` | Fails the process when a second runtime is loaded in-process | DWORD | UNSUPPORTED | | 
`UseLegacyV2RuntimeActivationPolicyDefaultValue` | Modifies the default value | DWORD | UNSUPPORTED | | 
`ErrorDialog` | Allow showing UI on error | DWORD | UNSUPPORTED | | 
`Fod` | Test the Feature On Demand installation | DWORD | UNSUPPORTED | | 
`FodPath` | Name of executable for Feature On Demand mockup | STRING | UNSUPPORTED | | 
`FodArgs` | Command line arguments to pass to the FOD process | STRING | UNSUPPORTED | | 
`FodLaunchAsync` | Whether to launch FOD asynchronously. | DWORD | UNSUPPORTED | | 
`FodConservativeMode` | Whether to be conservative wrt Fod launch. | DWORD | UNSUPPORTED | | 
`ApplicationMigrationRuntimeActivationConfigPath` | Provides a path in which to look for configuration files to be used for runtime activation, for application migration scenarios, before looking next to the EXE itself. | DWORD | EXTERNAL | | 
`TestOnlyEnsureImmersive` | Test-only flag used to indicate that it is expected that a process should be running as immersive. | DWORD | INTERNAL | | 
`EnableCoreClrHost` | Enables hosting coreclr from desktop mscoreei.dll to run windows store apps | DWORD | INTERNAL | | 
`AptcaAssemblyBreak` | Sets a breakpoint when checking if an assembly is APTCA or not | STRING | INTERNAL | | REGUTIL_default
`AptcaAssemblySharingBreak` | Sets a breakpoint when checking if we can code share an assembly | STRING | INTERNAL | | 
`AptcaAssemblySharingDomainBreak` | Sets a breakpoint only in the specified domain when checking if we can code share an assembly | DWORD | INTERNAL | 0 | 
`DefaultSecurityRuleSet` | Overrides the security rule set that assemblies which don't explicitly select their own rule set should use | DWORD | INTERNAL | 0 | 
`legacyCasPolicy` | Enable CAS policy for the process - for test use only, official access to this switch is through NetFx40_LegacySecurityPolicy. | DWORD | EXTERNAL | 0 | 
`loadFromRemoteSources` | Enable loading from zones that are not MyComputer when not in CAS mode. | DWORD | EXTERNAL | 0 | 
`LogTransparencyErrors` | Add an entry to the CLR log file for all transparency errors, rather than throwing an exception | DWORD | UNSUPPORTED | 0 | 
`NetFx40_LegacySecurityPolicy` | Enable CAS policy for the process. | DWORD | EXTERNAL | 0 | 
`NGenForPartialTrust` | Force NGEN to generate code for assemblies that could be used in partial trust. | DWORD | INTERNAL | 0 | 
`TransparencyFieldBreak` | Sets a breakpoint when figuring out the transparency of a specific field | STRING | INTERNAL | | REGUTIL_default
`TransparencyMethodBreak` | Sets a breakpoint when figuring out the transparency of a specific method | STRING | INTERNAL | | REGUTIL_default
`TransparencyTypeBreak` | Sets a breakpoint when figuring out the transparency of a specific type | STRING | INTERNAL | | REGUTIL_default
`AlwaysInsertCallout` | Always insert security access/transparency/APTCA callouts | DWORD | INTERNAL | 0 | 
`DisableAnonymouslyHostedDynamicMethodCreatorSecurityCheck` | Disables security checks for anonymously hosted dynamic methods based on their creator's security. | DWORD | UNSUPPORTED | 0 | 
`unsafeTypeForwarding` | Enable unsafe type forwarding between unrelated assemblies | DWORD | EXTERNAL | 0 | 
`SOBreakOnProbeDuringSO` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`SODumpViolationsDir` |  | STRING | INTERNAL | | REGUTIL_default
`SODumpViolationsStackTraceLength` |  | DWORD | INTERNAL | | 
`SOEnableBackoutStackValidation` |  | DWORD | INTERNAL | | 
`SOEnableDefaultRWValidation` |  | DWORD | INTERNAL | | 
`SOEnableStackProtectionInDebugger` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`SOEnableStackProtectionInDebuggerForProbeAtLine` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`SOEntryPointProbe` |  | DWORD | INTERNAL | | 
`SOInteriorProbe` |  | DWORD | INTERNAL | | 
`SOLogger` |  | DWORD | INTERNAL | | 
`SOProbeAssertOnOverrun` |  | DWORD | INTERNAL | | 
`SOUpdateProbeAtLine` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`SOUpdateProbeAtLineAmount` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`SOUpdateProbeAtLineInFile` |  | STRING | INTERNAL | | REGUTIL_default
`StackWalkStressUsingOldImpl` | to be removed | DWORD | INTERNAL | 0 | REGUTIL_default
`StackWalkStressUsingOS` | to be removed | DWORD | INTERNAL | 0 | REGUTIL_default
`StartupDelayMS` |  | STRING | EXTERNAL | | 
`StressCOMCall` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`StressLog` | Turns on the stress log. | DWORD | UNSUPPORTED | | 
`ForceEnc` | Forces Edit and Continue to be on for all eligable modules. | DWORD | UNSUPPORTED | | 
`StressLogSize` | Stress log size in bytes per thread. | DWORD | UNSUPPORTED | | 
`StressOn` | Enables the STRESS_ASSERT macro that stops runtime quickly (to prevent the clr state from changing significantly before breaking) | DWORD | INTERNAL | | 
`stressSynchronized` | Unknown if or where this is used; unless a test is specifically depending on this, it can be removed. | DWORD | INTERNAL | 0 | REGUTIL_default
`StressThreadCount` |  | DWORD | EXTERNAL | | 
`ThreadPool_ForceMinWorkerThreads` | Overrides the MinThreads setting for the ThreadPool worker pool | DWORD | INTERNAL | 0 | 
`ThreadPool_ForceMaxWorkerThreads` | Overrides the MaxThreads setting for the ThreadPool worker pool | DWORD | INTERNAL | 0 | 
`ThreadPool_DisableStarvationDetection` | Disables the ThreadPool feature that forces new threads to be added when workitems run for too long | DWORD | INTERNAL | 0 | 
`ThreadPool_DebugBreakOnWorkerStarvation` | Breaks into the debugger if the ThreadPool detects work queue starvation | DWORD | INTERNAL | 0 | 
`ThreadPool_EnableWorkerTracking` | Enables extra expensive tracking of how many workers threads are working simultaneously | DWORD | INTERNAL | 0 | 
`Thread_UseAllCpuGroups` | Specifies if to automatically distribute thread across CPU Groups | DWORD | EXTERNAL | 0 | 
`ThreadpoolTickCountAdjustment` |  | DWORD | INTERNAL | 0 | 
`HillClimbing_WavePeriod` |  | DWORD | INTERNAL | 4 | 
`HillClimbing_TargetSignalToNoiseRatio` |  | DWORD | INTERNAL | 300 | 
`HillClimbing_ErrorSmoothingFactor` |  | DWORD | INTERNAL | 1 | 
`HillClimbing_WaveMagnitudeMultiplier` |  | DWORD | INTERNAL | 100 | 
`HillClimbing_MaxWaveMagnitude` |  | DWORD | INTERNAL | 20 | 
`HillClimbing_WaveHistorySize` |  | DWORD | INTERNAL | 8 | 
`HillClimbing_Bias` | The 'cost' of a thread.  0 means drive for increased throughput regardless of thread count; higher values bias more against higher thread counts. | DWORD | INTERNAL | 15 | 
`HillClimbing_MaxChangePerSecond` |  | DWORD | INTERNAL | 4 | 
`HillClimbing_MaxChangePerSample` |  | DWORD | INTERNAL | 20 | 
`HillClimbing_MaxSampleErrorPercent` |  | DWORD | INTERNAL | 15 | 
`HillClimbing_SampleIntervalLow` |  | DWORD | INTERNAL | 10 | 
`HillClimbing_SampleIntervalHigh` |  | DWORD | INTERNAL | 200 | 
`HillClimbing_GainExponent` | The exponent to apply to the gain, times 100.  100 means to use linear gain, higher values will enhance large moves and damp small ones. | DWORD | INTERNAL | 200 | 
`INTERNAL_TypeLoader_InjectInterfaceDuplicates` | Injects duplicates in interface map for all types. | DWORD | INTERNAL | 0 | 
`VirtualCallStubCollideMonoPct` | Used only when STUB_LOGGING is defined, which by default is not. | DWORD | INTERNAL | 0 | REGUTIL_default
`VirtualCallStubCollideWritePct` | Used only when STUB_LOGGING is defined, which by default is not. | DWORD | INTERNAL | 100 | REGUTIL_default
`VirtualCallStubDumpLogCounter` | Used only when STUB_LOGGING is defined, which by default is not. | DWORD | INTERNAL | 0 | REGUTIL_default
`VirtualCallStubDumpLogIncr` | Used only when STUB_LOGGING is defined, which by default is not. | DWORD | INTERNAL | 0 | REGUTIL_default
`VirtualCallStubLogging` | Worth keeping, but should be moved into '#ifdef STUB_LOGGING' blocks. This goes for most (or all) of the stub logging infrastructure. | DWORD | patsubst(EXTERNAL_VirtualCallStubLogging, _.*, ) | 0 | patsubst(patsubst(CLRConfig::REGUTIL_default, CLRConfig::, ), |, /)
`VirtualCallStubMissCount` | Used only when STUB_LOGGING is defined, which by default is not. | DWORD | INTERNAL | 100 | REGUTIL_default
`VirtualCallStubResetCacheCounter` | Used only when STUB_LOGGING is defined, which by default is not. | DWORD | INTERNAL | 0 | REGUTIL_default
`VirtualCallStubResetCacheIncr` | Used only when STUB_LOGGING is defined, which by default is not. | DWORD | INTERNAL | 0 | REGUTIL_default
`DisableWatsonForManagedExceptions` | disable Watson and debugger launching for managed exceptions | DWORD | INTERNAL | 0 | 
`ZapBBInstr` |  | STRING | INTERNAL | | REGUTIL_default
`ZapBBInstrDir` |  | STRING | EXTERNAL | | 
`ZapDisable` |  | DWORD | EXTERNAL | 0 | 
`ZapExclude` |  | STRING | INTERNAL | | REGUTIL_default
`ZapOnly` |  | STRING | INTERNAL | | REGUTIL_default
`ZapRequire` |  | DWORD | EXTERNAL | | 
`ZapRequireExcludeList` |  | STRING | EXTERNAL | | 
`ZapRequireList` |  | STRING | EXTERNAL | | 
`ZapSet` |  | STRING | EXTERNAL | | REGUTIL_default
`ZapLazyCOWPagesEnabled` |  | DWORD | INTERNAL | 1 | 
`ZapLazyCOWPagesEnabled` |  | DWORD | INTERNAL | 0 | 
`DebugAssertOnMissedCOWPage` |  | DWORD | INTERNAL | 1 | 
`ReadyToRun` | Enable/disable use of ReadyToRun native code | DWORD | EXTERNAL | 1 |  // On by default for CoreCLR
`ReadyToRun` | Enable/disable use of ReadyToRun native code | DWORD | EXTERNAL | 0 |  // Off by default for desktop
`ExposeExceptionsInCOM` |  | DWORD | INTERNAL | | 
`PreferComInsteadOfManagedRemoting` | When communicating with a cross app domain CCW, use COM instead of managed remoting. | DWORD | EXTERNAL | 0 | 
`GenerateStubForHost` | Forces the host hook stub to be built for all unmanaged calls, even when not running hosted. | DWORD | INTERNAL | 0 | 
`legacyApartmentInitPolicy` |  | DWORD | EXTERNAL | | 
`legacyComHierarchyVisibility` |  | DWORD | EXTERNAL | | 
`legacyComVTableLayout` |  | DWORD | EXTERNAL | | 
`newComVTableLayout` |  | DWORD | EXTERNAL | | 
`PInvokeInline` |  | STRING | EXTERNAL | | REGUTIL_default
`InteropValidatePinnedObjects` | After returning from a managed-to-unmanged interop call, validate GC heap around objects pinned by IL stubs. | DWORD | UNSUPPORTED | 0 | 
`InteropLogArguments` | Log all pinned arguments passed to an interop call | DWORD | EXTERNAL | 0 | 
`LogCCWRefCountChange` | Outputs debug information and calls LogCCWRefCountChange_BREAKPOINT when AddRef or Release is called on a CCW. | STRING | UNSUPPORTED | | 
`EnableRCWCleanupOnSTAShutdown` | Performs RCW cleanup when STA shutdown is detected using IInitializeSpy in classic processes. | DWORD | INTERNAL | 0 | 
`LocalWinMDPath` | Additional path to probe for WinMD files in if a WinRT type is not resolved using the standard paths. | STRING | INTERNAL | | 
`AllowDComReflection` | Allows out of process DCOM clients to marshal blocked reflection types. | DWORD | EXTERNAL | 0 | 
`3gbEatMem` | Testhook: Size of memory (in 64K chunks) to be reserved before CLR starts | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`ActivatePatchSkip` | allows an assert when ActivatePatchSkip is called | DWORD | INTERNAL | 0 | REGUTIL_default
`AlwaysCallInstantiatingStub` | Forces the Jit to use the instantiating stub for generics | DWORD | INTERNAL | 0 | REGUTIL_default
`alwaysFlowImpersonationPolicy` | Windows identities should always flow across async points | DWORD | EXTERNAL | FALSE | 
`AlwaysUseMetadataInterfaceMapLayout` | Used for debugging generic interface map layout. | DWORD | INTERNAL | | 
`AssertOnUnneededThis` | While the ConfigDWORD is unnecessary, the contained ASSERT should be kept. This may result in some work tracking down violating MethodDescCallSites. | DWORD | INTERNAL | 0 | 
`AssertStacktrace` |  | DWORD | INTERNAL | 1 | REGUTIL_default
`BuildFlavor` | Choice of build flavor (wks or svr) of CLR | STRING | UNSUPPORTED | | 
`CerLogging` | In vm\\ConstrainedExecutionRegion.cpp.  Debug-only logging when we prepare methods, find reliability contract problems, restore stuff from ngen images, etc. | DWORD | INTERNAL | 0 | 
`clearNativeImageStress` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`CLRLoadLogDir` | Enable logging of CLR selection | STRING | INTERNAL | | 
`CONFIG` | Used to specify an XML config file for EEConfig | STRING | EXTERNAL | | REGUTIL_default
`CopyPropMax` | Sets internal jit constants for CopyProp | STRING | INTERNAL | | REGUTIL_default
`CPUFamily` |  | DWORD | INTERNAL | | 
`CPUFeatures` |  | DWORD | INTERNAL | | 
`DeadCodeMax` | Sets internal jit constants for Dead Code elmination | STRING | INTERNAL | | REGUTIL_default
`DefaultVersion` | Version of CLR to load. | STRING | INTERNAL | | 
`developerInstallation` | Flag to enable DEVPATH binding feature | STRING | EXTERNAL | |  // TODO: check special handling
`DiagnosticSuspend` |  | DWORD | INTERNAL | 0 | 
`shadowCopyVerifyByTimestamp` | Fusion flag to enable quick verification of files in the shadow copy directory by using timestamps. | DWORD | EXTERNAL | 0 | FavorConfigFile / MayHavePerformanceDefault
`disableFusionUpdatesFromADManager` | Fusion flag to prevent changes to the AppDomainSetup object made by implementations of AppDomainManager.InitializeNewDomain from propagating to Fusion | DWORD | EXTERNAL | 0 | FavorConfigFile
`disableCachingBindingFailures` | Fusion flag to re-enable Everett bind caching behavior (Whidbey caches failures for sharing) | DWORD | EXTERNAL | 0 | FavorConfigFile
`enableVerboseInstallLogging` | Fusion flag to enable detailed logging of GAC install operations | DWORD | INTERNAL | 0 | 
`disableCommitThreadStack` | This should only be internal but I believe ASP.Net uses this | DWORD | EXTERNAL | | 
`DisableConfigCache` | Used to disable the 'probabilistic' config cache, which walks through the appropriate config registry keys on init and probabilistically keeps track of which exist. | DWORD | EXTERNAL | 0 | REGUTIL_default
`DisableStackwalkCache` |  | DWORD | EXTERNAL | | 
`DoubleArrayToLargeObjectHeap` | Controls double[] placement | DWORD | UNSUPPORTED | | 
`DumpConfiguration` | Dumps runtime properties of xml configuration files to the log. | DWORD | INTERNAL | 0 | 
`DumpOnClassLoad` | Dumps information about loaded class to log. | STRING | INTERNAL | | 
`EnableInternetHREFexes` | Part of security work related to locking down Internet No-touch deployment. It's not clear what happens to NTD in v4, but if it's till there the setting is needed | DWORD | EXTERNAL | 0 | (LookupOptions) (REGUTIL_default / IgnoreEnv / IgnoreHKCU)
`enforceFIPSPolicy` | Causes crypto algorithms which have not been FIPS certified to throw an exception if they are used on a machine that requriess FIPS enforcement | DWORD | EXTERNAL | | 
`ExpandAllOnLoad` |  | DWORD | INTERNAL | | 
`FORCE_ASSEMREF_DUPCHECK` | ? Has comment: Allow Avalon to use the SecurityCriticalAttribute ? but WHY? | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`ForcedRuntime` | Verify version of CLR loaded | STRING | INTERNAL | | 
`ForceRelocs` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`GenerateLongJumpDispatchStubRatio` | Useful for testing VSD on AMD64 | DWORD | INTERNAL | | 
`generatePublisherEvidence` | If set, when the CLR loads an assembly that has an Authenticode signature we will verify that signature to generate Publisher evidence, at the expense of network hits and perf. | DWORD | EXTERNAL | | 
`HashStack` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`HostManagerConfig` |  | DWORD | INTERNAL | (DWORD)-1 | 
`HostTestADUnload` | Alows setting Rude unload as default | DWORD | INTERNAL | 0 | 
`HostTestThreadAbort` |  | DWORD | INTERNAL | 0 | 
`IgnoreDllMainReturn` | Don't check the return value of DllMain if this is set | DWORD | UNSUPPORTED | 0 | ConfigFile_ApplicationFirst
`IJWEntrypointCompatMode` | Makes us run managed EP from DllMain. Basically brings the buggy behavior back. | DWORD | EXTERNAL | 1 | REGUTIL_default
`InstallRoot` | Directory with installed CLRs | STRING | INTERNAL | | 
`InvokeHalt` | Throws an assert when the given method is invoked through reflection. | STRING | INTERNAL | | 
`legacyHMACMode` | v2.0 of the CLR shipped with a bug causing HMAC-SHA-384 and HMAC-SHA-512 to be calculated incorrectly.  Orcas fixes this bug, but the config flag is added so that code which must verify v2.0 RTM HMACs can still interop with them. | DWORD | EXTERNAL | | 
`legacyImpersonationPolicy` | Windows identities should never flow across async points | DWORD | EXTERNAL | FALSE | 
`legacyLoadMscorsnOnStartup` | Force mscorsn.dll to load when the VM starts | DWORD | UNSUPPORTED | | 
`legacyNullReferenceExceptionPolicy` |  | DWORD | UNSUPPORTED | | 
`legacyUnhandledExceptionPolicy` |  | DWORD | UNSUPPORTED | | 
`legacyVirtualMethodCallVerification` |  | DWORD | EXTERNAL | | 
`ManagedLogFacility` | ?Log facility for managed code using the log | DWORD | INTERNAL | | 
`MaxStackDepth` |  | DWORD | INTERNAL | | 
`MaxStubUnwindInfoSegmentSize` |  | DWORD | INTERNAL | | 
`MaxThreadRecord` |  | DWORD | INTERNAL | | 
`MergeCriticalAttributes` |  | DWORD | EXTERNAL | 1 | REGUTIL_default
`MessageDebugOut` |  | DWORD | INTERNAL | 0 | 
`MscorsnLogging` | Enables strong name logging | DWORD | INTERNAL | 0 | REGUTIL_default
`NativeImageRequire` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`NestedEhOom` |  | DWORD | INTERNAL | 0 | REGUTIL_default
`NO_SO_NOT_MAINLINE` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`NoGuiOnAssert` |  | DWORD | INTERNAL | INTERNAL_NoGuiOnAssert_Default | REGUTIL_default
`NoProcedureSplitting` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`NoStringInterning` | Disallows string interning. I see no value in it anymore. | DWORD | INTERNAL | 1 | REGUTIL_default
`NotifyBadAppCfg` | Whether to show a message box for bad application config file. | DWORD | EXTERNAL | | 
`PauseOnLoad` | Stops in SystemDomain::init. I think it can be removed. | DWORD | INTERNAL | | 
`PerfAllocsSizeThreshold` | Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler. | DWORD | INTERNAL | 0x3FFFFFFF | 
`PerfNumAllocsThreshold` | Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler. | DWORD | INTERNAL | 0x3FFFFFFF | 
`PerfTypesToLog` | Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler. | STRING | INTERNAL | | 
`PEVerify` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`Prepopulate1` |  | DWORD | EXTERNAL | 1 | 
`PrestubGC` |  | STRING | INTERNAL | | 
`PrestubHalt` |  | STRING | INTERNAL | | 
`RepositoryDir` |  | STRING | EXTERNAL | | REGUTIL_default
`RepositoryFlags` |  | DWORD | EXTERNAL | | 
`RestrictedGCStressExe` |  | STRING | EXTERNAL | | 
`ReturnSourceTypeForTesting` | allows returning the (internal only) source type of an IL to Native mapping for debugging purposes | DWORD | INTERNAL | 0 | REGUTIL_default
`RSStressLog` | allows turning on logging for RS startup | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`SafeHandleStackTraces` | Debug-only ability to get a stack trace attached to every SafeHandle instance at creation time, for tracking down handle corruption problems. | DWORD | INTERNAL | | 
`SaveThreadInfo` |  | DWORD | INTERNAL | | 
`SaveThreadInfoMask` |  | DWORD | INTERNAL | | 
`SBDumpOnNewIndex` | Used for Syncblock debugging. It's been a while since any of those have been used. | DWORD | INTERNAL | 0 | 
`SBDumpOnResize` | Used for Syncblock debugging. It's been a while since any of those have been used. | DWORD | INTERNAL | 0 | 
`SBDumpStyle` | Used for Syncblock debugging. It's been a while since any of those have been used. | DWORD | INTERNAL | 0 | 
`ShimDatabaseVersion` | Force using shim database version in registry | STRING | UNSUPPORTED | | 
`SleepOnExit` | Used for lrak detection. I'd say deprecated by umdh. | DWORD | UNSUPPORTED | 0 | 
`StubLinkerUnwindInfoVerificationOn` |  | DWORD | INTERNAL | | 
`SuccessExit` |  | DWORD | UNSUPPORTED | 0 | REGUTIL_default
`SupressAllowUntrustedCallerChecks` | Disable APTCA | DWORD | INTERNAL | 0 | 
`SuspendDeadlockTimeout` |  | DWORD | INTERNAL | 40000 | 
`SuspendThreadDeadlockTimeoutMs` |  | DWORD | INTERNAL | 2000 | 
`SymbolReadingPolicy` | Specifies when PDBs may be read | DWORD | EXTERNAL | | 
`TestDataConsistency` | allows ensuring the left side is not holding locks (and may thus be in an inconsistent state) when inspection occurs | DWORD | UNSUPPORTED | FALSE | 
`ThreadGuardPages` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`Timeline` |  | DWORD | EXTERNAL | 0 | REGUTIL_default
`TlbImpShouldBreakOnConvFunction` |  | STRING | INTERNAL | | REGUTIL_default
`TlbImpSkipLoading` |  | DWORD | INTERNAL | | 
`TotalStressLogSize` | Total stress log size in bytes. | DWORD | UNSUPPORTED | | 
`TraceIUnknown` |  | DWORD | EXTERNAL | | 
`TraceWrap` |  | DWORD | EXTERNAL | | 
`TURNOFFDEBUGINFO` |  | DWORD | EXTERNAL | | 
`UseGenericTlsGetters` |  | DWORD | EXTERNAL | 0 | 
`useLegacyIdentityFormat` | Fusion flag to switch between Whidbey and Everett textual identity parser (have semantic differences) | DWORD | EXTERNAL | 0 | FavorConfigFile
`UseMethodDataCache` | Used during feature development; may now be removed. | DWORD | EXTERNAL | FALSE | 
`UseNewCrossDomainRemoting` | Forces the managed remoting stack to be used even for cross-domain remoting if set to 0 (default is 1) | DWORD | EXTERNAL | | 
`UseParentMethodData` | Used during feature development; may now be removed. | DWORD | EXTERNAL | TRUE | 
`VerifierOff` |  | DWORD | INTERNAL | | 
`VerifyAllOnLoad` |  | DWORD | EXTERNAL | | 
`Version` | Version of CLR to load. | STRING | INTERNAL | | 
`ShimHookLibrary` | Path to a DLL that should be notified when shim loads the runtime DLL. | STRING | INTERNAL | | 
