There are two primary ways to configure runtime behavior: CoreCLR hosts can pass in key-value string pairs during runtime initialization, or users can set special variables in the environment or registry. Today, the set of configuration options that can be set via the former method is relatively small, but moving forward, we expect to add more options there. Each set of options is described below.

## Host Configuration Knobs
These can be passed in by a host during initialization. Note that the values are all passed in as strings, so if the type is boolean, the value would be the string "true" or "false", and if it's a numeric value, it would be in the form "123".


Name | Description | Type
-----|-------------|------
`System.GC.Concurrent` | Enable concurrent GC | boolean
`System.GC.Server` | Enable server GC | boolean
`System.GC.RetainVM` | Put segments that should be deleted on a standby list for future use instead of releasing them back to the OS | boolean
`System.Runtime.TieredCompilation` | Enable tiered compilation | boolean
`System.Threading.ThreadPool.MinThreads` | Override MinThreads for the ThreadPool worker pool | numeric
`System.Threading.ThreadPool.MaxThreads` | Override MaxThreads for the ThreadPool worker pool | numeric



## Environment/Registry Configuration Knobs

This table was machine-generated using `clr-configuration-knobs.csx` script from repository commit [0ae3d02](https://github.com/dotnet/coreclr/commit/0ae3d020f82c3f8650b7e5eeaf9f1030f7e7e785) on 4/25/2019. It might be out of date. To generate latest documentation run `dotnet-script clr-configuration-knobs.csx` from this file directory.

When using these configurations from environment variables, the variables need to have the `COMPlus_` prefix in their names. e.g. To set DumpJittedMethods to 1, add the environment variable `COMPlus_DumpJittedMethods=1`.

See also [Setting configuration variables](../building/viewing-jit-dumps.md#setting-configuration-variables) for more information.

#### Tables
1. [AppDomain Configuration Knobs](#appdomain-configuration-knobs)
2. [ARM Configuration Knobs](#arm-configuration-knobs)
3. [Assembly Loader Configuration Knobs](#assembly-loader-configuration-knobs)
4. [Conditional breakpoints Configuration Knobs](#conditional-breakpoints-configuration-knobs)
5. [Debugger Configuration Knobs](#debugger-configuration-knobs)
6. [Diagnostics (internal general-purpose) Configuration Knobs](#diagnostics-internal-general-purpose-configuration-knobs)
7. [Entry point slot backpatch Configuration Knobs](#entry-point-slot-backpatch-configuration-knobs)
8. [Exception Handling Configuration Knobs](#exception-handling-configuration-knobs)
9. [Garbage collector Configuration Knobs](#garbage-collector-configuration-knobs)
10. [GDBJIT Configuration Knobs](#gdbjit-configuration-knobs)
11. [IBC Configuration Knobs](#ibc-configuration-knobs)
12. [Interop Configuration Knobs](#interop-configuration-knobs)
13. [Interpreter Configuration Knobs](#interpreter-configuration-knobs)
14. [JIT Configuration Knobs](#jit-configuration-knobs)
15. [JIT Hardware Intrinsics Configuration Knobs](#jit-hardware-intrinsics-configuration-knobs)
16. [Jit Pitching Configuration Knobs](#jit-pitching-configuration-knobs)
17. [Loader Configuration Knobs](#loader-configuration-knobs)
18. [Loader heap Configuration Knobs](#loader-heap-configuration-knobs)
19. [Log Configuration Knobs](#log-configuration-knobs)
20. [MetaData Configuration Knobs](#metadata-configuration-knobs)
21. [Native Binder Configuration Knobs](#native-binder-configuration-knobs)
22. [NGEN Configuration Knobs](#ngen-configuration-knobs)
23. [Performance Configuration Knobs](#performance-configuration-knobs)
24. [Profiling API / ETW Configuration Knobs](#profiling-api--etw-configuration-knobs)
25. [Spinning heuristics Configuration Knobs](#spinning-heuristics-configuration-knobs)
26. [Stress Configuration Knobs](#stress-configuration-knobs)
27. [Thread (miscellaneous) Configuration Knobs](#thread-miscellaneous-configuration-knobs)
28. [Thread Suspend Configuration Knobs](#thread-suspend-configuration-knobs)
29. [Threadpool Configuration Knobs](#threadpool-configuration-knobs)
30. [Tiered Compilation Configuration Knobs](#tiered-compilation-configuration-knobs)
31. [TypeLoader Configuration Knobs](#typeloader-configuration-knobs)
32. [Uncategorized Configuration Knobs](#uncategorized-configuration-knobs)
33. [Virtual call stubs Configuration Knobs](#virtual-call-stubs-configuration-knobs)
34. [Watson Configuration Knobs](#watson-configuration-knobs)
35. [Zap Configuration Knobs](#zap-configuration-knobs)

#### AppDomain Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`AddRejitNops` | Control for the profiler rejit feature infrastructure | `DWORD` | `UNSUPPORTED` | |
`ADDumpSB` | Not used | `DWORD` | `INTERNAL` | `0` |
`ADForceSB` | Forces sync block creation for all objects | `DWORD` | `INTERNAL` | `0` |
`ADLogMemory` | Superseded by test hooks | `DWORD` | `INTERNAL` | `0` |
`ADTakeDHSnapShot` | Superseded by test hooks | `DWORD` | `INTERNAL` | `0` |
`ADTakeSnapShot` | Superseded by test hooks | `DWORD` | `INTERNAL` | `0` |
`EnableFullDebug` | Heavy-weight checking for AD boundary violations (AD leaks) | `DWORD` | `INTERNAL` | |

#### ARM Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`ARMEnabled` | AppDomain Resource Monitoring. Set to 1 to enable it | `DWORD` | `UNSUPPORTED` | `(DWORD)0` |

#### Assembly Loader Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`designerNamespaceResolution` | Set it to 1 to enable DesignerNamespaceResolve event for WinRT types | `DWORD` | `EXTERNAL` | `FALSE` | IgnoreEnv \| IgnoreHKLM \| IgnoreHKCU \| FavorConfigFile
`GetAssemblyIfLoadedIgnoreRidMap` | Used to force loader to ignore assemblies cached in the rid-map | `DWORD` | `INTERNAL` | `0` | REGUTIL_default

#### Conditional breakpoints Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`BreakOnBadExit` |  | `DWORD` | `UNSUPPORTED` | `0` | REGUTIL_default
`BreakOnClassBuild` | Very useful for debugging class layout code. | `STRING` | `INTERNAL` | |
`BreakOnClassLoad` | Very useful for debugging class loading code. | `STRING` | `INTERNAL` | |
`BreakOnComToClrNativeInfoInit` | Throws an assert when native information about a COM -> CLR call are about to be gathered. | `STRING` | `INTERNAL` | |
`BreakOnDebugBreak` | Allows an assert in debug builds when a user break is hit | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnDILoad` | Allows an assert when the DI is loaded | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnDumpToken` | Breaks when using internal logging on a particular token value. | `DWORD` | `INTERNAL` | `0xffffffff` | REGUTIL_default
`BreakOnEELoad` |  | `DWORD` | `UNSUPPORTED` | `0` | REGUTIL_default
`BreakOnEEShutdown` |  | `DWORD` | `INTERNAL` | `0` |
`BreakOnExceptionInGetThrowable` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnFindMethod` | Breaks in findMethodInternal when it searches for the specified token. | `DWORD` | `INTERNAL` | `0` |
`BreakOnFirstPass` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnHR` | Debug.cpp, IfFailxxx use this macro to stop if hr matches  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnInstantiation` | Very useful for debugging generic class instantiation. | `STRING` | `INTERNAL` | |
`BreakOnInteropStubSetup` | Throws an assert when marshaling stub for the given method is about to be built. | `STRING` | `INTERNAL` | |
`BreakOnInteropVTableBuild` | Specifies a type name for which an assert should be thrown when building interop v-table. | `STRING` | `INTERNAL` | | REGUTIL_default
`BreakOnMethodName` | Very useful for debugging method override placement code. | `STRING` | `INTERNAL` | |
`BreakOnNotify` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnRetailAssert` | Used for debugging \"retail\" asserts (fatal errors) | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnSecondPass` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnSO` |  | `DWORD` | `UNSUPPORTED` | `0` | REGUTIL_default
`BreakOnStructMarshalSetup` | Throws an assert when field marshalers for the given type with layout are about to be created. | `STRING` | `INTERNAL` | |
`BreakOnUEF` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`BreakOnUncaughtException` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default

#### Debugger Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`D::FCE` | Allows an assert when crawling the managed stack for an exception handler | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgAssertOnDebuggeeDebugBreak` | If non-zero causes the managed-only debugger to assert on unhandled breakpoints in the debuggee | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgBreakIfLocksUnavailable` | Allows an assert when the debugger can't take a lock  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgBreakOnErr` | Allows an assert when we get a failing hresult | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgBreakOnMapPatchToDJI` | Allows an assert when mapping a patch to an address | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgBreakOnRawInt3` | Allows an assert for test coverage for debug break or other int3 breaks | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgBreakOnSendBreakpoint` | Allows an assert when sending a breakpoint to the right side | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgBreakOnSetIP` | Allows an assert when setting the IP | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgCheckInt3` | Asserts if the debugger explicitly writes int3 instead of calling SetUnmanagedBreakpoint | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgDACAssertOnMismatch` | Allows an assert when the mscordacwks and mscorwks dll versions don't match | `DWORD` | `INTERNAL` | |
`DbgDACEnableAssert` | Enables extra validity checking in DAC - assumes target isn't corrupt | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgDACSkipVerifyDlls` | Allows disabling the check to ensure mscordacwks and mscorwks dll versions match | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgDelayHelper` | Varies the wait in the helper thread startup for testing race between threads | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgDisableDynamicSymsCompat` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgDisableTargetConsistencyAsserts` | Allows explicitly testing with corrupt targets | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgDontResumeThreadsOnUnhandledException` | If non-zero, then don't try to unsuspend threads after continuing a 2nd-chance native exception | `DWORD` | `UNSUPPORTED` | `0` |
`DbgEnableMixedModeDebuggingInternalOnly` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgExtraThreads` | Allows extra unmanaged threads to run and throw debug events for stress testing | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgExtraThreadsCantStop` | Allows extra unmanaged threads in can't stop region to run and throw debug events for stress testing | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgExtraThreadsIB` | Allows extra in-band unmanaged threads to run and throw debug events for stress testing | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgExtraThreadsOOB` | Allows extra out of band unmanaged threads to run and throw debug events for stress testing | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgFaultInHandleIPCEvent` | Allows testing the unhandled event filter | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgInjectFEE` | Allows injecting a fatal execution error for testing Watson | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgLeakCheck` | Allows checking for leaked Cordb objects | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgNativeCodeBpBindsAcrossVersions` | If non-zero causes native breakpoints at offset 0 to bind in all tiered compilation versions of the given method | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgNo2ndChance` | Allows breaking on (and catching bogus) 2nd chance exceptions | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgNoDebugger` | Allows breaking if we don't want to lazily initialize the debugger | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgNoForceContinue` | Used to force a continue on longhorn | `DWORD` | `UNSUPPORTED` | `1` | REGUTIL_default
`DbgNoOpenMDByFile` | Allows opening MD by memory for perf testing | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgOOBinFEEE` | Allows forcing oob breakpoints when a fatal error occurs | `DWORD` | `INTERNAL` | `0` |
`DbgPackShimPath` | CoreCLR path to dbgshim.dll - we are trying to figure out if we can remove this | `STRING` | `EXTERNAL` | |
`DbgPingInterop` | Allows checking for deadlocks in interop debugging | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgRace` | Allows pausing for native debug events to get hijicked | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgRedirect` | Allows for redirecting the event pipeline | `DWORD` | `UNSUPPORTED` | `0` | REGUTIL_default
`DbgRedirectApplication` | Specifies the auxiliary debugger application to launch. | `STRING` | `EXTERNAL` | |
`DbgRedirectAttachCmd` | Specifies command parameters for attaching the auxiliary debugger. | `STRING` | `EXTERNAL` | |
`DbgRedirectCommonCmd` | Specifies a command line format string for the auxiliary debugger. | `STRING` | `EXTERNAL` | |
`DbgRedirectCreateCmd` | Specifies command parameters when creating the auxiliary debugger. | `STRING` | `EXTERNAL` | |
`DbgShortcutCanary` | Allows a way to force canary to fail to be able to test failure paths | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgSkipMEOnStep` | Turns off MethodEnter checks | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgSkipStackCheck` | Skip the stack pointer check during stackwalking | `DWORD` | `UNSUPPORTED` | `0` | REGUTIL_default
`DbgSkipVerCheck` | Allows different RS and LS versions (for servicing work) | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgTC` | Allows checking boundary compression for offset mappings | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgTransportFaultInject` | Allows injecting a fault for testing the debug transport | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgTransportLog` | Turns on logging for the debug transport | `DWORD` | `INTERNAL` | |
`DbgTransportLogClass` | Mask to control what is logged in DbgTransportLog | `DWORD` | `INTERNAL` | |
`DbgTransportProxyAddress` | Allows specifying the transport proxy address | `STRING` | `UNSUPPORTED` | | REGUTIL_default
`DbgTrapOnSkip` | Allows breaking when we skip a breakpoint | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DbgWaitTimeout` | Specifies the timeout value for waits | `DWORD` | `INTERNAL` | `1` | REGUTIL_default
`DbgWFDETimeout` | Specifies the timeout value for wait when waiting for a debug event | `DWORD` | `UNSUPPORTED` | `25` | REGUTIL_default
`DebugBreakOnAssert` | If DACCESS_COMPILE is defined, break on asserts. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DebugBreakOnVerificationFailure` | Halts the jit on verification failure | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`DebuggerBreakPoint` | Allows counting various debug events | `STRING` | `INTERNAL` | | REGUTIL_default
`Debugging_RequiredVersion` | The lowest ICorDebug version we should attempt to emulate, or 0 for default policy.  Use 2 for CLRv2, 4 for CLRv4, etc. | `DWORD` | `UNSUPPORTED` | `0` |
`DebugVerify` | Control for tracing in peverify | `STRING` | `INTERNAL` | | REGUTIL_default
`EnableDiagnostics` | Allows the debugger and profiler diagnostics to be disabled | `DWORD` | `EXTERNAL` | `1` | REGUTIL_default
`EncApplyChanges` | Allows breaking when ApplyEditAndContinue is called | `DWORD` | `INTERNAL` | `0` |
`EnCBreakOnRemapComplete` | Allows breaking after N RemapCompletes | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`EnCBreakOnRemapOpportunity` | Allows breaking after N RemapOpportunities | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`EncDumpApplyChanges` | Allows dumping edits in delta metadata and il files | `DWORD` | `INTERNAL` | `0` |
`EncFixupFieldBreak` | Unlikely that this is used anymore. | `DWORD` | `INTERNAL` | `0` |
`EncJitUpdatedFunction` | Allows breaking when an updated function is jitted | `DWORD` | `INTERNAL` | `0` |
`EnCResolveField` | Allows breaking when computing the address of an EnC-added field | `DWORD` | `INTERNAL` | `0` |
`EncResumeInUpdatedFunction` | Allows breaking when execution resumes in a new EnC version of a function | `DWORD` | `INTERNAL` | `0` |
`IntentionallyCorruptDataFromTarget` | Intentionally fakes bad data retrieved from target to try and break dump generation. | `DWORD` | `INTERNAL` | `0` |
`MiniMdBufferCapacity` | The max size of the buffer to store mini metadata information for triage- and mini-dumps. | `DWORD` | `INTERNAL` | `64 * 1024` |
`RaiseExceptionOnAssert` | Raise a first chance (if set to 1) or second chance (if set to 2) exception on asserts. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default

#### Diagnostics (internal general-purpose) Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`ConditionalContracts` | If ENABLE_CONTRACTS_IMPL is defined, sets whether contracts are conditional. (?) | `DWORD` | `INTERNAL` | |
`ConsistencyCheck` |  | `DWORD` | `INTERNAL` | `0` |
`ContinueOnAssert` | If set, doesn't break on asserts. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`disableStackOverflowProbing` |  | `DWORD` | `UNSUPPORTED` | `0` | FavorConfigFile
`InjectFatalError` |  | `DWORD` | `INTERNAL` | |
`InjectFault` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`SuppressChecks` |  | `DWORD` | `INTERNAL` | |
`SuppressLockViolationsOnReentryFromOS` | 64 bit OOM tests re-enter the CLR via RtlVirtualUnwind.  This indicates whether to suppress resulting locking violations. | `DWORD` | `INTERNAL` | `0` |
`TestHooks` | Used by tests to get test an insight on various CLR workings | `STRING` | `INTERNAL` | |

#### Entry point slot backpatch Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`BackpatchEntryPointSlots` | Indicates whether to enable entry point slot backpatching, for instance to avoid making virtual calls through a precode and instead to patch virtual slots for a method when its entry point changes. | `DWORD` | `UNSUPPORTED` | `1` |

#### Exception Handling Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`AssertOnFailFast` |  | `DWORD` | `INTERNAL` | |
`Corhost_Swallow_Uncaught_Exceptions` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`FailFastOnCorruptedStateException` | Failfast if a CSE is encountered | `DWORD` | `UNSUPPORTED` | `0` | FavorConfigFile
`legacyCorruptedStateExceptionsPolicy` | Enabled Pre-V4 CSE behavior | `DWORD` | `UNSUPPORTED` | `0` | FavorConfigFile
`SuppressLostExceptionTypeAssert` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`UseEntryPointFilter` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default

#### Garbage collector Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`BGCSpin` | Specifies the bgc spin time | `DWORD` | `UNSUPPORTED` | `2` |
`BGCSpinCount` | Specifies the bgc spin count | `DWORD` | `UNSUPPORTED` | `140` |
`FastGCCheckStack` |  | `DWORD` | `INTERNAL` | `0` |
`FastGCStress` | Reduce the number of GCs done by enabling GCStress | `DWORD` | `INTERNAL` | |
`gcAllowVeryLargeObjects` | Allow allocation of 2GB+ objects on GC heap | `DWORD` | `EXTERNAL` | `1` |
`GCBreakOnOOM` | Does a DebugBreak at the soonest time we detect an OOM | `DWORD` | `UNSUPPORTED` | |
`GCCompactRatio` | Specifies the ratio compacting GCs vs sweeping  | `DWORD` | `UNSUPPORTED` | `0` |
`gcConcurrent` | Enables/Disables concurrent GC | `DWORD` | `UNSUPPORTED` | `(DWORD)-1` |
`GCConfigLogEnabled` | Specifies if you want to turn on config logging in GC | `DWORD` | `UNSUPPORTED` | `0` |
`GCConfigLogFile` | Specifies the name of the GC config log file | `STRING` | `UNSUPPORTED` | |
`gcConservative` | Enables/Disables conservative GC | `DWORD` | `UNSUPPORTED` | `0` |
`GcCoverage` | Specify a method or regular expression of method names to run with GCStress | `STRING` | `INTERNAL` | |
`GCCpuGroup` | Specifies if to enable GC to support CPU groups | `DWORD` | `EXTERNAL` | `0` |
`gcForceCompact` | When set to true, always do compacting GC | `DWORD` | `UNSUPPORTED` | |
`GCGen0MaxBudget` | Specifies the largest gen0 allocation budget | `DWORD` | `UNSUPPORTED` | |
`GCgen0size` | Specifies the smallest gen0 size | `DWORD` | `UNSUPPORTED` | |
`GCHeapAffinitizeMask` | Specifies processor mask for Server GC threads | `DWORD` | `EXTERNAL` | |
`GCHeapAffinitizeRanges` | Specifies list of processors for Server GC threads. The format is a comma separated list of processor numbers or ranges of processor numbers. Example: 1,3,5,7-9,12 | `STRING` | `EXTERNAL` | |
`GCHeapCount` |  | `DWORD` | `EXTERNAL` | `0` |
`GCHeapHardLimit` | Specifies the maximum commit size for the GC heap | `DWORD` | `EXTERNAL` | |
`GCHeapHardLimitPercent` | Specifies the GC heap usage as a percentage of the total memory | `DWORD` | `EXTERNAL` | |
`GCHighMemPercent` | Specifies the percent for GC to consider as high memory | `DWORD` | `EXTERNAL` | `0` |
`GCLargePages` | Specifies whether large pages should be used when a heap hard limit is set | `DWORD` | `EXTERNAL` | |
`GCLatencyLevel` | Specifies the GC latency level that you want to optimize for | `DWORD` | `EXTERNAL` | `1` |
`GCLatencyMode` | Specifies the GC latency mode - batch, interactive or low latency (note that the same thing can be specified via API which is the supported way) | `DWORD` | `INTERNAL` | |
`GCLogEnabled` | Specifies if you want to turn on logging in GC | `DWORD` | `UNSUPPORTED` | `0` |
`GCLogFile` | Specifies the name of the GC log file | `STRING` | `UNSUPPORTED` | |
`GCLogFileSize` | Specifies the GC log file size | `DWORD` | `UNSUPPORTED` | `0` |
`GCLOHCompact` | Specifies the LOH compaction mode | `DWORD` | `UNSUPPORTED` | |
`GCLOHThreshold` | Specifies the size that will make objects go on LOH | `DWORD` | `EXTERNAL` | `0` |
`GCMixLog` | Specifies the name of the log file for GC mix statistics | `STRING` | `UNSUPPORTED` | |
`GCName` |  | `STRING` | `EXTERNAL` | |
`GCNoAffinitize` |  | `DWORD` | `EXTERNAL` | `0` |
`GCNumaAware` | Specifies if to enable GC NUMA aware | `DWORD` | `UNSUPPORTED` | `1` |
`GCPollType` |  | `DWORD` | `EXTERNAL` | |
`GCProvModeStress` | Stress the provisional modes | `DWORD` | `UNSUPPORTED` | `0` |
`GCRetainVM` | When set we put the segments that should be deleted on a standby list (instead of releasing them back to the OS) which will be considered to satisfy new segment requests (note that the same thing can be specified via API which is the supported way) | `DWORD` | `UNSUPPORTED` | `0` |
`GCSegmentSize` | Specifies the managed heap segment size | `DWORD` | `UNSUPPORTED` | |
`gcServer` | Enables server GC | `DWORD` | `UNSUPPORTED` | `0` |
`GCStress` | Trigger GCs at regular intervals | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`GCStressMaxFGCsPerBGC` | Specifies how many FGCs will occur during one BGC in GCStressMix mode | `DWORD` | `INTERNAL` | `~0U` |
`GCStressMix` | Specifies whether the GC mix mode is enabled or not | `DWORD` | `INTERNAL` | `0` |
`GcStressOnDirectCalls` | Whether to trigger a GC on direct calls | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`GCStressStart` | Start GCStress after N stress GCs have been attempted | `DWORD` | `EXTERNAL` | `0` |
`GCStressStartAtJit` | Start GCStress after N items are jitted | `DWORD` | `INTERNAL` | `0` |
`GCStressStep` | Specifies how often StressHeap will actually do a GC in GCStressMix mode | `DWORD` | `INTERNAL` | `1` |
`gcTrimCommitOnLowMemory` | When set we trim the committed space more aggressively for the ephemeral seg. This is used for running many instances of server processes where they want to keep as little memory committed as possible | `DWORD` | `EXTERNAL` | |
`HeapVerify` | When set verifies the integrity of the managed heap on entry and exit of each GC | `DWORD` | `UNSUPPORTED` | |
`SetupGcCoverage` | This doesn't appear to be a config flag | `STRING` | `EXTERNAL` | | REGUTIL_default
`SkipGcCoverage` | Specify a list of assembly names to skip with GC Coverage | `STRING` | `INTERNAL` | |
`StatsUpdatePeriod` | Specifies the interval, in seconds, at which to update the statistics | `DWORD` | `UNSUPPORTED` | `60` |
`SuspendTimeLog` | Specifies the name of the log file for suspension statistics | `STRING` | `UNSUPPORTED` | |

#### GDBJIT Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`GDBJitElfDump` | Dump ELF for specified method | `STRING` | `INTERNAL` | |
`GDBJitEmitDebugFrame` | Enable .debug_frame generation | `DWORD` | `INTERNAL` | `TRUE` |

#### IBC Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`ConvertIbcData` | Converts between v1 and v2 IBC data | `DWORD` | `UNSUPPORTED` | `1` | REGUTIL_default
`DisableHotCold` | Master hot/cold splitting switch in Jit64 | `DWORD` | `UNSUPPORTED` | |
`DisableIBC` | Disables the use of IBC data | `DWORD` | `UNSUPPORTED` | `0` | REGUTIL_default
`UseIBCFile` |  | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default

#### Interop Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`AllowDComReflection` | Allows out of process DCOM clients to marshal blocked reflection types. | `DWORD` | `EXTERNAL` | `0` |
`EnableEventPipe` | Enable/disable event pipe.  Non-zero values enable tracing. | `DWORD` | `INTERNAL` | `0` |
`EnableRCWCleanupOnSTAShutdown` | Performs RCW cleanup when STA shutdown is detected using IInitializeSpy in classic processes. | `DWORD` | `INTERNAL` | `0` |
`EventPipeCircularMB` | The EventPipe circular buffer size in megabytes. | `DWORD` | `INTERNAL` | `1024` |
`EventPipeConfig` | Configuration for EventPipe. | `STRING` | `INTERNAL` | |
`EventPipeOutputPath` | The full path excluding file name for the trace file that will be written when COMPlus_EnableEventPipe=1 | `STRING` | `INTERNAL` | |
`EventPipeRundown` | Enable/disable eventpipe rundown. | `DWORD` | `INTERNAL` | `1` |
`ExposeExceptionsInCOM` |  | `DWORD` | `INTERNAL` | |
`InteropLogArguments` | Log all pinned arguments passed to an interop call | `DWORD` | `EXTERNAL` | `0` |
`InteropValidatePinnedObjects` | After returning from a managed-to-unmanaged interop call, validate GC heap around objects pinned by IL stubs. | `DWORD` | `UNSUPPORTED` | `0` |
`LocalWinMDPath` | Additional path to probe for WinMD files in if a WinRT type is not resolved using the standard paths. | `STRING` | `INTERNAL` | |
`LogCCWRefCountChange` | Outputs debug information and calls LogCCWRefCountChange_BREAKPOINT when AddRef or Release is called on a CCW. | `STRING` | `UNSUPPORTED` | |
`PInvokeInline` |  | `STRING` | `EXTERNAL` | | REGUTIL_default

#### Interpreter Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`DumpInterpreterStubs` | Prints all interpreter stubs that are created to the console | `DWORD` | `INTERNAL` | `0` |
`Interpret` | Selectively uses the interpreter to execute the specified methods | `STRING` | `INTERNAL` | | REGUTIL_default
`InterpreterDoLoopMethods` | If set, don't check for loops, start by interpreting *all* methods | `DWORD` | `INTERNAL` | `0` |
`InterpreterFallback` | Fallback to the interpreter when the JIT compiler fails | `DWORD` | `INTERNAL` | `0` |
`InterpreterJITThreshold` | The number of times a method should be interpreted before being JITted | `DWORD` | `INTERNAL` | `10` |
`InterpreterLogFile` | If non-null, append interpreter logging to this file, else use stdout | `STRING` | `INTERNAL` | | REGUTIL_default
`InterpreterLooseRules` | If non-zero, allow ECMA spec violations required by managed C++. | `DWORD` | `INTERNAL` | `1` | REGUTIL_default
`InterpreterMethHashMax` | If non-zero, only interpret methods selected by 'Interpret' whose hash is at most this value | `DWORD` | `INTERNAL` | `UINT32_MAX` |
`InterpreterMethHashMin` | Only interpret methods selected by 'Interpret' whose hash is at least this value. or after nth | `DWORD` | `INTERNAL` | `0` |
`InterpreterPrintPostMortem` | Prints summary information about the execution to the console | `DWORD` | `INTERNAL` | `0` |
`InterpreterStubMax` | If non-zero, only interpret methods selected by 'Interpret' whose stub number is at most this value. | `DWORD` | `INTERNAL` | `UINT32_MAX` |
`InterpreterStubMin` | Only interpret methods selected by 'Interpret' whose stub num is at least this value. | `DWORD` | `INTERNAL` | `0` |
`InterpreterUseCaching` | If non-zero, use the caching mechanism. | `DWORD` | `INTERNAL` | `1` | REGUTIL_default
`InterpretExclude` | Excludes the specified methods from the set selected by 'Interpret' | `STRING` | `INTERNAL` | | REGUTIL_default
`TraceInterpreterEntries` | Logs entries to interpreted methods to the console | `DWORD` | `INTERNAL` | `0` |
`TraceInterpreterIL` | Logs individual instructions of interpreted methods to the console | `DWORD` | `INTERNAL` | `0` |
`TraceInterpreterJITTransition` | Logs when the interpreter determines a method should be JITted | `DWORD` | `INTERNAL` | `0` |
`TraceInterpreterOstack` | Logs operand stack after each IL instruction of interpreted methods to the console | `DWORD` | `INTERNAL` | `0` |
`TraceInterpreterVerbose` | Logs interpreter progress with detailed messages to the console | `DWORD` | `INTERNAL` | `0` |

#### JIT Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`` |  | `STRING` | `JitMeasureNowayAssertFile` | |
`AltJit` | Enables AltJit and selectively limits it to the specified methods. | `STRING` | `EXTERNAL` | | REGUTIL_default
`AltJitAssertOnNYI` | Controls the AltJit behavior of NYI stuff | `DWORD` | `INTERNAL` | `0` |
`AltJitExcludeAssemblies` | Do not use AltJit on this semicolon-delimited list of assemblies. | `STRING` | `EXTERNAL` | | REGUTIL_default
`AltJitLimit` | Max number of functions to use altjit for (decimal) | `DWORD` | | `0` |
`AltJitName` | Alternative Jit to use, will fall back to primary jit. | `STRING` | `EXTERNAL` | | REGUTIL_default
`AltJitNgen` | Enables AltJit for NGEN and selectively limits it to the specified methods. | `STRING` | `INTERNAL` | | REGUTIL_default
`AltJitSkipOnAssert` | If AltJit hits an assert, fall back to the fallback JIT. Useful in conjunction with COMPlus_ContinueOnAssert=1 | `DWORD` | | `0` |
`BreakOnDumpToken` | Breaks when using internal logging on a particular token value. | `DWORD` | | `0xffffffff` |
`DebugBreakOnVerificationFailure` | Halts the jit on verification failure | `DWORD` | | `0` |
`DumpJittedMethods` | Prints all jitted methods to the console | `DWORD` | | `0` |
`EnableAVX` | Enable AVX instruction set for wide operations as default | `DWORD` | `EXTERNAL` | `EXTERNAL_JitEnableAVX_Default` | REGUTIL_default
`FeatureSIMD` | Enable SIMD intrinsics recognition in System.Numerics.dll and/or System.Numerics.Vectors.dll | `DWORD` | `EXTERNAL` | `EXTERNAL_FeatureSIMD_Default` | REGUTIL_default
`InjectFault` |  | `DWORD` | `Should` | `0` |
`JitAggressiveInlining` | Aggressive inlining of all methods | `DWORD` | | `0` |
`JitAlignLoops` | Aligns loop targets to 8 byte boundaries | `DWORD` | `UNSUPPORTED` | |
`JitAssertOnMaxRAPasses` |  | `DWORD` | | `0` |
`JitBreak` | Stops in the importer when compiling a specified method | `SSV` | | |
`JitBreakEmit` |  | `DWORD` | `INTERNAL` | `(DWORD)-1` | REGUTIL_default
`JitBreakEmitOutputInstr` |  | `DWORD` | | `-1` |
`JitBreakMorphTree` |  | `DWORD` | | `0xffffffff` |
`JitBreakOnBadCode` |  | `DWORD` | | `0` |
`JITBreakOnMinOpts` | Halt if jit switches to MinOpts | `DWORD` | `JitBreakOnMinOpts` | `0` |
`JitBreakOnUnsafeCode` |  | `DWORD` | | `0` |
`JitCanUseSSE2` |  | `DWORD` | | `-1` |
`JitCloneLoops` | If 0, don't clone. Otherwise clone loops for optimizations. | `DWORD` | | `1` |
`JitComponentUnitTests` | Run JIT component unit tests | `DWORD` | `RunComponentUnitTests` | `0` |
`JitDebugBreak` |  | `SSV` | | |
`JitDebugDump` |  | `SSV` | | |
`JitDebuggable` |  | `DWORD` | `INTERNAL` | |
`JitDebugLogLoopCloning` | In debug builds log places where loop cloning optimizations are performed on the fast path. | `DWORD` | | `0` |
`JitDefaultFill` | In debug builds, initialize the memory allocated by the nra with this byte. | `DWORD` | | `0xdd` |
`JitDiffableDasm` | Make the disassembly diff-able | `DWORD` | `DiffableDasm` | `0` |
`JitDirectAlloc` |  | `DWORD` | | `0` |
`JitDisasm` | Dumps disassembly for specified method | `SSV` | | |
`JitDisasmAssemblies` | Only show JitDisasm and related info for methods from this semicolon-delimited list of assemblies. | `STRING` | | |
`JitDoAssertionProp` | Perform assertion propagation optimization | `DWORD` | | `1` |
`JitDoCopyProp` | Perform copy propagation on variables that appear redundant | `DWORD` | | `1` |
`JitDoEarlyProp` | Perform Early Value Propagation | `DWORD` | | `1` |
`JitDoLoopHoisting` | Perform loop hoisting on loop invariant values | `DWORD` | | `1` |
`JitDoRangeAnalysis` | Perform range check analysis | `DWORD` | | `1` |
`JitDoSsa` | Perform Static Single Assignment (SSA) numbering on the variables | `DWORD` | | `1` |
`JitDoubleAlign` |  | `DWORD` | | `1` |
`JitDoValueNumber` | Perform value numbering on method expressions | `DWORD` | | `1` |
`JitDump` | Dumps trees for specified method | `SSV` | | |
`JitDumpASCII` | Uses only ASCII characters in tree dumps | `DWORD` | | `1` |
`JitDumpBeforeAfterMorph` | If 1, display each tree before/after morphing | `DWORD` | `TreesBeforeAfterMorph` | `0` |
`JitDumpFg` | Dumps Xml/Dot Flowgraph for specified method | `STRING` | | |
`JitDumpFgDir` | Directory for Xml/Dot flowgraph dump(s) | `STRING` | | |
`JitDumpFgDot` | Set to non-zero to emit Dot instead of Xml Flowgraph dump | `DWORD` | | `0` |
`JitDumpFgFile` | Filename for Xml/Dot flowgraph dump(s) | `STRING` | | |
`JitDumpFgPhase` | Phase-based Xml/Dot flowgraph support. Set to the short name of a phase to see the flowgraph after that phase. Leave unset to dump after COLD-BLK (determine first cold block) or set to * for all phases | `STRING` | | |
`JitDumpIR` | Dumps trees (in linear IR form) for specified method | `SSV` | | |
`JitDumpIRFormat` | Comma separated format control for JitDumpIR, values = {types \| locals \| ssa \| valnums \| kinds \| flags \| nodes \| nolists \| nostmts \| noleafs \| trees \| dataflow} | `STRING` | | |
`JitDumpIRPhase` | Phase control for JitDumpIR, values = {* \| phasename} | `STRING` | | |
`JitDumpTerseLsra` | Produce terse dump output for LSRA | `DWORD` | | `1` |
`JitDumpToDebugger` | Output JitDump output to the debugger | `DWORD` | | `0` |
`JitDumpVerboseSsa` | Produce especially verbose dump output for SSA | `DWORD` | | `0` |
`JitDumpVerboseTrees` | Enable more verbose tree dumps | `DWORD` | | `0` |
`JitEECallTimingInfo` |  | `DWORD` | | `0` |
`JitEHDump` | Dump the EH table for the method, as reported to the VM | `SSV` | | |
`JitELTHookEnabled` | On ARM, setting this will emit Enter/Leave/TailCall callbacks | `DWORD` | `INTERNAL` | `0` |
`JitEmitPrintRefRegs` |  | `DWORD` | | `0` |
`JitEnableDevirtualization` | Enable devirtualization in importer | `DWORD` | | `1` |
`JitEnableFinallyCloning` |  | `DWORD` | | `1` |
`JitEnableGuardedDevirtualization` |  | `DWORD` | | `0` |
`JitEnableLateDevirtualization` | Enable devirtualization after inlining | `DWORD` | | `1` |
`JitEnableNoWayAssert` |  | `DWORD` | `INTERNAL` | `INTERNAL_JitEnableNoWayAssert_Default` | REGUTIL_default
`JitEnablePCRelAddr` | Whether absolute addr be encoded as PC-rel offset by RyuJIT where possible | `DWORD` | `EnablePCRelAddr` | `1` |
`JitEnableRemoveEmptyTry` |  | `DWORD` | | `1` |
`JitExclude` |  | `SSV` | | |
`JitExpensiveDebugCheckLevel` | Level indicates how much checking beyond the default to do in debug builds (currently 1-2) | `DWORD` | | `0` |
`JitForceFallback` | Set to non-zero to test NOWAY assert by forcing a retry | `DWORD` | | `0` |
`JitForceProcedureSplitting` |  | `SSV` | | |
`JitForceVer` |  | `DWORD` | | `0` |
`JitFramed` | Forces EBP frames | `DWORD` | `UNSUPPORTED` | |
`JitFullyInt` | Forces Fully interruptible code | `DWORD` | | `0` |
`JitFuncInfoLogFile` | If set, gather JIT function info and write to this file. | `STRING` | `INTERNAL` | |
`JitFunctionFile` |  | `STRING` | | |
`JitFunctionTrace` | If non-zero, print JIT start/end logging | `DWORD` | | `0` |
`JitGCChecks` |  | `DWORD` | | `0` |
`JitGCDump` |  | `SSV` | | |
`JitGCInfoLogging` | If true, prints GCInfo-related output to standard output. | `DWORD` | | `0` |
`JitGCStress` | GC stress mode for jit | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`JitGuardedDevirtualizationGuessBestClass` |  | `DWORD` | | `1` |
`JitGuardedDevirtualizationGuessUniqueInterface` |  | `DWORD` | | `1` |
`JitHalt` | Emits break instruction into jitted code | `SSV` | | |
`JitHashBreak` | Same as JitBreak, but for a method hash | `DWORD` | | `-1` |
`JitHashDump` | Same as JitDump, but for a method hash | `DWORD` | | `-1` |
`JitHashDumpIR` | Same as JitDumpIR, but for a method hash | `DWORD` | | `-1` |
`JitHashHalt` | Same as JitHalt, but for a method hash | `DWORD` | | `-1` |
`JitHeartbeat` |  | `DWORD` | `INTERNAL` | `0` |
`JitHelperLogging` |  | `DWORD` | `INTERNAL` | `0` |
`JitImportBreak` |  | `SSV` | | |
`JitInclude` |  | `SSV` | | |
`JitInlineAdditionalMultiplier` |  | `DWORD` | | `0` |
`JITInlineDepth` |  | `DWORD` | `JitInlineDepth` | `DEFAULT_MAX_INLINE_DEPTH` |
`JitInlineDumpData` |  | `DWORD` | | `0` |
`JitInlineDumpXml` | 1 = full xml (+ failures in DEBUG) 2 = only methods with inlines (+ failures in DEBUG) 3 = only methods with inlines, no failures | `DWORD` | | `0` |
`JitInlineLimit` |  | `DWORD` | | `-1` |
`JitInlinePolicyDiscretionary` |  | `DWORD` | | `0` |
`JitInlinePolicyFull` |  | `DWORD` | | `0` |
`JitInlinePolicyModel` |  | `DWORD` | | `0` |
`JitInlinePolicyRandom` | nonzero enables; value is the external random seed | `DWORD` | | `0` |
`JitInlinePolicyReplay` |  | `DWORD` | | `0` |
`JitInlinePolicySize` |  | `DWORD` | | `0` |
`JitInlinePrintStats` |  | `DWORD` | | `0` |
`JitInlineReplayFile` |  | `STRING` | | |
`JitInlineSIMDMultiplier` |  | `DWORD` | | `3` |
`JITInlineSize` |  | `DWORD` | `JitInlineSize` | `DEFAULT_MAX_INLINE_SIZE` |
`JitLargeBranches` | Force using the largest conditional branch format | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`JitLateDisasm` |  | `SSV` | | |
`JITLateDisasmTo` |  | `STRING` | `JitLateDisasmTo` | |
`JitLockWrite` | Force all volatile writes to be 'locked' | `DWORD` | `INTERNAL` | `0` |
`JitLongAddress` | Force using the large pseudo instruction form for long address | `DWORD` | | `0` |
`JitLoopHoistStats` | Display JIT loop hoisting statistics | `DWORD` | `DisplayLoopHoistStats` | `0` |
`JitLsraStats` | Display JIT Linear Scan Register Allocator statistics | `DWORD` | `DisplayLsraStats` | `0` |
`JITMaxTempAssert` |  | `DWORD` | `JitMaxTempAssert` | `1` |
`JitMaxUncheckedOffset` |  | `DWORD` | | `8` |
`JitMeasureIR` | If set, measure the IR size after some phases and report it in the time log. | `DWORD` | | `0` |
`JitMeasureNowayAssert` | Set to 1 to measure noway_assert usage. Only valid if MEASURE_NOWAY is defined. | `DWORD` | | `0` |
`JitMemStats` | Display JIT memory usage statistics | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`JITMinOpts` | Forces MinOpts | `DWORD` | `UNSUPPORTED` | |
`JITMinOptsBbCount` | Internal jit control of MinOpts | `DWORD` | `JitMinOptsBbCount` | `DEFAULT_MIN_OPTS_BB_COUNT` |
`JITMinOptsCodeSize` | Internal jit control of MinOpts | `DWORD` | `JitMinOptsCodeSize` | `DEFAULT_MIN_OPTS_CODE_SIZE` |
`JITMinOptsInstrCount` | Internal jit control of MinOpts | `DWORD` | `JitMinOptsInstrCount` | `DEFAULT_MIN_OPTS_INSTR_COUNT` |
`JITMinOptsLvNumcount` | Internal jit control of MinOpts | `DWORD` | `JitMinOptsLvNumCount` | `DEFAULT_MIN_OPTS_LV_NUM_COUNT` |
`JITMinOptsLvRefcount` | Internal jit control of MinOpts | `DWORD` | `JitMinOptsLvRefCount` | `DEFAULT_MIN_OPTS_LV_REF_COUNT` |
`JITMinOptsName` | Forces MinOpts for a named function | `SSV` | `JitMinOptsName` | |
`JitMinOptsTrackGCrefs` | Track GC roots | `DWORD` | | `JitMinOptsTrackGCrefs_Default` |
`JitName` | Primary Jit to use | `STRING` | `EXTERNAL` | |
`JitNoCMOV` |  | `DWORD` | | `0` |
`JitNoCSE` |  | `DWORD` | | `0` |
`JitNoCSE2` |  | `DWORD` | | `0` |
`JitNoForceFallback` | Set to non-zero to prevent NOWAY assert testing. Overrides COMPlus_JitForceFallback and JIT stress flags. | `DWORD` | | `0` |
`JitNoHoist` |  | `DWORD` | | `0` |
`JitNoInline` | Disables inlining of all methods | `DWORD` | | `0` |
`JitNoInlineRange` |  | `STRING` | | |
`JitNoMemoryBarriers` | If 1, don't generate memory barriers | `DWORD` | | `0` |
`JitNoProcedureSplitting` | Disallow procedure splitting for specified methods | `SSV` | | |
`JitNoProcedureSplittingEH` | Disallow procedure splitting for specified methods if they contain exception handling | `SSV` | | |
`JitNoRegLoc` |  | `DWORD` | | `0` |
`JitNoRngChks` | If 1, don't generate range checks | `DWORD` | `JitNoRangeChks` | `0` |
`JitNoStructPromotion` | Disables struct promotion in Jit32 | `DWORD` | | `0` |
`JitNoUnroll` |  | `DWORD` | | `0` |
`JitObjectStackAllocation` |  | `DWORD` | | `0` |
`JitOptimizeType` |  | `DWORD` | `EXTERNAL` | |
`JitOptRepeat` | Runs optimizer multiple times on the method | `SSV` | | |
`JitOptRepeatCount` | Number of times to repeat opts when repeating | `DWORD` | | `2` |
`JitOrder` |  | `DWORD` | | `0` |
`JITPInvokeCheckEnabled` |  | `DWORD` | `JitPInvokeCheckEnabled` | `0` |
`JITPInvokeEnabled` |  | `DWORD` | `JitPInvokeEnabled` | `1` |
`JitPrintDevirtualizedMethods` |  | `DWORD` | | `0` |
`JitPrintInlinedMethods` |  | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`JitQueryCurrentStaticFieldClass` |  | `DWORD` | | `1` |
`JitRange` |  | `STRING` | | |
`JitRegisterFP` | Control FP enregistration | `DWORD` | `EXTERNAL` | `3` | REGUTIL_default
`JitReportFastTailCallDecisions` |  | `DWORD` | | `0` |
`JITRequired` |  | `DWORD` | `JitRequired` | `-1` |
`JITRoundFloat` |  | `DWORD` | `JitRoundFloat` | `DEFAULT_ROUND_LEVEL` |
`JitSaveFpLrWithCalleeSavedRegisters` |  | `DWORD` | | `0` |
`JitSkipArrayBoundCheck` |  | `DWORD` | | `0` |
`JitSlowDebugChecksEnabled` | Turn on slow debug checks | `DWORD` | | `1` |
`JitSplitFunctionSize` | On ARM, use this as the maximum function/funclet size for creating function fragments (and creating multiple RUNTIME_FUNCTION entries) | `DWORD` | | `0` |
`JitSsaStress` | Perturb order of processing of blocks in SSA; 0 = no stress; 1 = use method hash; * = supplied value as random hash | `DWORD` | | `0` |
`JitStackAllocToLocalSize` |  | `DWORD` | | `DEFAULT_MAX_LOCALLOC_TO_LOCAL_SIZE` |
`JitStackChecks` |  | `DWORD` | | `0` |
`JitStdOutFile` | If set, sends JIT's stdout output to this file. | `STRING` | | |
`JitStress` | Internal Jit stress mode: 0 = no stress, 2 = all stress, other = vary stress based on a hash of the method and this value | `DWORD` | | `0` |
`JitStressBBProf` | Internal Jit stress mode | `DWORD` | | `0` |
`JitStressBiasedCSE` | Internal Jit stress mode: decimal bias value between (0,100) to perform CSE on a candidate. 100% = All CSEs. 0% = 0 CSE. (> 100) means no stress. | `DWORD` | | `0x101` |
`JitStressFP` | Internal Jit stress mode | `DWORD` | | `0` |
`JitStressModeNames` | Internal Jit stress mode: stress using the given set of stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL | `STRING` | | |
`JitStressModeNamesNot` | Internal Jit stress mode: do NOT stress using the given set of stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL | `STRING` | | |
`JitStressModeNamesOnly` | Internal Jit stress: if nonzero, only enable stress modes listed in JitStressModeNames | `DWORD` | | `0` |
`JitStressOnly` | Internal Jit stress mode: stress only the specified method(s) | `SSV` | | |
`JitStressRange` | Internal Jit stress mode | `STRING` | | |
`JitStressRegs` |  | `DWORD` | | `0` |
`JitStrictCheckForNonVirtualCallToVirtualMethod` |  | `DWORD` | | `1` |
`JitTelemetry` | If non-zero, gather JIT telemetry data | `DWORD` | `EXTERNAL` | `1` |
`JitTimeLogCsv` | If set, gather JIT throughput data and write to a CSV file. This mode must be used in internal retail builds. | `STRING` | `INTERNAL` | |
`JitTimeLogFile` | If set, gather JIT throughput data and write to this file. | `STRING` | `INTERNAL` | |
`JitUnwindDump` | Dump the unwind codes for the method | `SSV` | | |
`JitVerificationDisable` |  | `DWORD` | `INTERNAL` | |
`JitVNMapSelBudget` | Max # of MapSelect's considered for a particular top-level invocation. | `DWORD` | `INTERNAL` | `100` |
`JitVNMapSelLimit` | If non-zero, assert if # of VNF_MapSelect applications considered reaches this | `DWORD` | | `0` |
`MultiCoreJitProfile` | If set, use the file to store/control multi-core JIT. | `STRING` | `INTERNAL` | |
`MultiCoreJitProfileWriteDelay` | Set the delay after which the multi-core JIT profile will be written to disk. | `DWORD` | `INTERNAL` | `12` |
`NetFx40_PInvokeStackResilience` | Makes P/Invoke resilient against mismatched signature and calling convention (significant perf penalty). | `DWORD` | `EXTERNAL` | `(DWORD)-1` |
`NgenHashDump` | same as JitHashDump, but for ngen | `DWORD` | | `-1` |
`NgenHashDumpIR` | same as JitHashDumpIR, but for ngen | `DWORD` | | `-1` |
`NgenOrder` |  | `DWORD` | | `0` |
`RunAltJitCode` | If non-zero, and the compilation succeeds for an AltJit, then use the code. If zero, then we always throw away the generated code and fall back to the default compiler. | `DWORD` | | `1` |
`SIMD16ByteOnly` | Limit maximum SIMD vector length to 16 bytes (used by x64_arm64_altjit) | `DWORD` | `INTERNAL` | `0` |
`StackSamplingAfter` | When to start sampling (for some sort of app steady state), i.e., initial delay for sampling start in milliseconds. | `DWORD` | `UNSUPPORTED` | `0` |
`StackSamplingEnabled` | Is stack sampling based tracking of evolving hot methods enabled. | `DWORD` | `UNSUPPORTED` | `0` |
`StackSamplingEvery` | How frequent should thread stacks be sampled in milliseconds. | `DWORD` | `UNSUPPORTED` | `100` |
`StackSamplingNumMethods` | Number of evolving methods to track as hot and JIT them in the background at a given point of execution. | `DWORD` | `UNSUPPORTED` | `32` |
`StressCOMCall` |  | `DWORD` | | `0` |
`TailCallLoopOpt` | Convert recursive tail calls to loops | `DWORD` | `EXTERNAL` | `1` |
`TailCallMax` |  | `STRING` | `INTERNAL` | | REGUTIL_default
`TailCallOpt` |  | `STRING` | `EXTERNAL` | | REGUTIL_default
`TailcallStress` |  | `DWORD` | | `0` |

#### JIT Hardware Intrinsics Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`EnableAES` | Enable AES | `DWORD` | | `1` |
`EnableArm64Aes` |  | `DWORD` | `      ` | `1` |
`EnableArm64Atomics` |  | `DWORD` | `  ` | `1` |
`EnableArm64Crc32` |  | `DWORD` | `    ` | `1` |
`EnableArm64Dcpop` |  | `DWORD` | `    ` | `1` |
`EnableArm64Dp` |  | `DWORD` | `       ` | `1` |
`EnableArm64Fcma` |  | `DWORD` | `     ` | `1` |
`EnableArm64Fp` |  | `DWORD` | `       ` | `1` |
`EnableArm64Fp16` |  | `DWORD` | `     ` | `1` |
`EnableArm64Jscvt` |  | `DWORD` | `    ` | `1` |
`EnableArm64Lrcpc` |  | `DWORD` | `    ` | `1` |
`EnableArm64Pmull` |  | `DWORD` | `    ` | `1` |
`EnableArm64Sha1` |  | `DWORD` | `     ` | `1` |
`EnableArm64Sha256` |  | `DWORD` | `   ` | `1` |
`EnableArm64Sha3` |  | `DWORD` | `     ` | `1` |
`EnableArm64Sha512` |  | `DWORD` | `   ` | `1` |
`EnableArm64Simd` |  | `DWORD` | `     ` | `1` |
`EnableArm64Simd_fp16` |  | `DWORD` | | `1` |
`EnableArm64Simd_v81` |  | `DWORD` | ` ` | `1` |
`EnableArm64Sm3` |  | `DWORD` | `      ` | `1` |
`EnableArm64Sm4` |  | `DWORD` | `      ` | `1` |
`EnableArm64Sve` |  | `DWORD` | `      ` | `1` |
`EnableAVX` | Enable AVX | `DWORD` | | `1` |
`EnableAVX2` | Enable AVX2 | `DWORD` | | `1` |
`EnableBMI1` | Enable BMI1 | `DWORD` | | `1` |
`EnableBMI2` | Enable BMI2 | `DWORD` | | `1` |
`EnableFMA` | Enable FMA | `DWORD` | | `1` |
`EnableHWIntrinsic` | Enable Base | `DWORD` | | `1` |
`EnableIncompleteISAClass` | Enable testing not-yet-implemented intrinsic classes | `DWORD` | | `0` |
`EnableLZCNT` | Enable AES | `DWORD` | | `1` |
`EnablePCLMULQDQ` | Enable PCLMULQDQ | `DWORD` | | `1` |
`EnablePOPCNT` | Enable POPCNT | `DWORD` | | `1` |
`EnableSSE` | Enable SSE | `DWORD` | | `1` |
`EnableSSE2` | Enable SSE2 | `DWORD` | | `1` |
`EnableSSE3` | Enable SSE3 | `DWORD` | | `1` |
`EnableSSE3_4` | Enable SSE3, SSSE3, SSE 4.1 and 4.2 instruction set as default | `DWORD` | | `1` |
`EnableSSE41` | Enable SSE41 | `DWORD` | | `1` |
`EnableSSE42` | Enable SSE42 | `DWORD` | | `1` |
`EnableSSSE3` | Enable SSSE3 | `DWORD` | | `1` |

#### Jit Pitching Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`JitPitchEnabled` | Set it to 1 to enable Jit Pitching | `DWORD` | `INTERNAL` | `(DWORD)0` |
`JitPitchMaxVal` | Do Jit Pitching the value of the inner counter less then this value (for debuggin purpose only) | `DWORD` | `INTERNAL` | `(DWORD)0xffffffff` |
`JitPitchMemThreshold` | Do Jit Pitching when code heap usage is larger than this (in bytes) | `DWORD` | `INTERNAL` | `(DWORD)0` |
`JitPitchMethodSizeThreshold` | Do Jit Pitching for methods whose native code size larger than this (in bytes) | `DWORD` | `INTERNAL` | `(DWORD)0` |
`JitPitchMinVal` | Do Jit Pitching if the value of the inner counter greater than this value (for debugging purpose only) | `DWORD` | `INTERNAL` | `(DWORD)0` |
`JitPitchPrintStat` | Print statistics about Jit Pitching | `DWORD` | `INTERNAL` | `(DWORD)0` |
`JitPitchTimeInterval` | Time interval between Jit Pitchings in ms | `DWORD` | `INTERNAL` | `(DWORD)0` |

#### Loader Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`APIThreadStress` | Used to test Loader for race conditions | `DWORD` | `INTERNAL` | |
`CoreClrBinderLog` | Debug flag that enabled detailed log for new binder (similar to stress logging). | `STRING` | `INTERNAL` | |
`ForceLog` | Fusion flag to enforce assembly binding log. Heavily used and documented in MSDN and BLOGS. | `DWORD` | `EXTERNAL` | |
`WinMDPath` | Path for Windows WinMD files | `STRING` | `INTERNAL` | |

#### Loader heap Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`BreakOnOutOfMemoryWithinRange` | Break before out of memory within range exception is thrown | `DWORD` | `INTERNAL` | `0` |
`CodeHeapReserveForJumpStubs` | Percentage of code heap to reserve for jump stubs | `DWORD` | `INTERNAL` | `1` |
`LoaderHeapCallTracing` | Loader heap troubleshooting | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`NGenReserveForJumpStubs` | Percentage of ngen image size to reserve for jump stubs | `DWORD` | `INTERNAL` | `0` |

#### Log Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`LogEnable` | Turns on the traditional CLR log. | `DWORD` | `INTERNAL` | |
`LogFacility` | Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog. | `DWORD` | `INTERNAL` | |
`LogFacility2` | Specifies a facility mask for CLR log. (See 'loglf.h'; VM interprets string value as hex number.) Also used by stresslog. | `DWORD` | `INTERNAL` | |
`logFatalError` | Specifies whether EventReporter logs fatal errors in the Windows event log. | `DWORD` | `EXTERNAL` | `1` |
`LogFile` | Specifies a file name for the CLR log. | `STRING` | `INTERNAL` | | REGUTIL_default
`LogFileAppend` | Specifies whether to append to or replace the CLR log file. | `DWORD` | `INTERNAL` | |
`LogFlushFile` | Specifies whether to flush the CLR log file on each write. | `DWORD` | `INTERNAL` | |
`LogLevel` | 4=10 msgs, 9=1000000, 10=everything | `DWORD` | `EXTERNAL` | |
`LogPath` | ?Fusion debug log path. | `STRING` | `INTERNAL` | |
`LogToConsole` | Writes the CLR log to console. | `DWORD` | `INTERNAL` | |
`LogToDebugger` | Writes the CLR log to debugger (OutputDebugStringA). | `DWORD` | `INTERNAL` | |
`LogToFile` | Writes the CLR log to a file. | `DWORD` | `INTERNAL` | |
`LogWithPid` | Appends pid to filename for the CLR log. | `DWORD` | `INTERNAL` | |

#### MetaData Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`AssertOnBadImageFormat` | ASSERT when invalid MD read | `DWORD` | `INTERNAL` | |
`MD_ApplyDeltaBreak` | ASSERT when applying EnC | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_DeltaCheck` | Some checks of GUID when applying EnC (?) | `DWORD` | `INTERNAL` | `1` | REGUTIL_default
`MD_EncDelta` | Forces EnC Delta format in MD (?) | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_ForceNoColDesSharing` | Don't know - the only usage I could find is #if 0 (?) | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_KeepKnownCA` | Something with known CAs (?) | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_MiniMDBreak` | ASSERT when creating CMiniMdRw class | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_PreSaveBreak` | ASSERT when calling CMiniMdRw::PreSave | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_PreserveDebuggerMetadataMemory` | Save all versions of metadata memory in the debugger when debuggee metadata is updated | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`MD_RegMetaBreak` | ASSERT when creating RegMeta class | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_RegMetaDump` | Dump MD in 4 functions (?) | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_WinMD_AssertOnIllegalUsage` | ASSERT if a WinMD import adapter detects a tool incompatibility | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`MD_WinMD_Disable` | Never activate the WinMD import adapter | `DWORD` | `INTERNAL` | `0` | REGUTIL_default

#### Native Binder Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`NgenBind_ZapForbid` | Assert if an assembly succeeds in binding to a native image | `DWORD` | `INTERNAL` | `0` |
`NgenBind_ZapForbidExcludeList` |  | `STRING` | `INTERNAL` | |
`NgenBind_ZapForbidList` |  | `STRING` | `INTERNAL` | |
`SymDiffDump` | Used to create the map file while binding the assembly. Used by SemanticDiffer | `DWORD` | `INTERNAL` | `0` | REGUTIL_default

#### NGEN Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`CrossGenAssumeInputSigned` | CrossGen should assume that its input assemblies will be signed before deployment | `DWORD` | `INTERNAL` | `1` |
`NGen_JitName` |  | `STRING` | `EXTERNAL` | | REGUTIL_default
`NgenDebugDump` |  | `SSV` | | |
`NgenDisasm` | Same as JitDisasm, but for ngen | `SSV` | | |
`NgenDump` | Same as JitDump, but for ngen | `SSV` | | |
`NgenDumpFg` | Ngen Xml Flowgraph support | `STRING` | | |
`NgenDumpFgDir` | Ngen Xml Flowgraph support | `STRING` | | |
`NgenDumpFgFile` | Ngen Xml Flowgraph support | `STRING` | | |
`NgenDumpIR` | Same as JitDumpIR, but for ngen | `SSV` | | |
`NgenDumpIRFormat` | Same as JitDumpIRFormat, but for ngen | `STRING` | | |
`NgenDumpIRPhase` | Same as JitDumpIRPhase, but for ngen | `STRING` | | |
`NgenEHDump` | Dump the EH table for the method, as reported to the VM | `SSV` | | |
`NGenEnableCreatePdb` | If set to >0 ngen.exe displays help on, recognizes createpdb in the command line | `DWORD` | `UNSUPPORTED` | `0` |
`NgenForceFailureCount` | If set to >0 and we have IBC data we will force a failure after we reference an IBC data item <value> times | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`NgenForceFailureKind` | If set to 1, We will throw a TypeLoad exception; If set to 2, We will cause an A/V | `DWORD` | `INTERNAL` | `1` | REGUTIL_default
`NgenForceFailureMask` | Bitmask used to control which locations will check and raise the failure (defaults to bits: -1) | `DWORD` | `INTERNAL` | `(DWORD)-1` | REGUTIL_default
`NGenFramed` | Same as JitFramed, but for ngen | `DWORD` | `UNSUPPORTED` | `(DWORD)-1` | REGUTIL_default
`NgenGCDump` |  | `SSV` | | |
`NGenOnlyOneMethod` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`NgenOrder` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`NGenSimulateDiskFull` | If set to 1, ngen will throw a Disk full exception in ZapWriter.cpp:Save() | `DWORD` | `INTERNAL` | `0` |
`NgenUnwindDump` | Dump the unwind codes for the method | `SSV` | | |
`NoASLRForNgen` | Turn off IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE bit in generated ngen images. Makes nidump output repeatable from run to run. | `DWORD` | `INTERNAL` | `0` |
`PartialNGen` | Generate partial NGen images | `DWORD` | `INTERNAL` | `(DWORD)-1` |
`partialNGenStress` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`ZapDoNothing` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default

#### Performance Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`performanceScenario` | Activates a set of workload-specific default values for performance settings | `STRING` | `EXTERNAL` | |

#### Profiling API / ETW Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`AttachThreadAlwaysOn` | Forces profapi attach thread to be created on startup, instead of on-demand. | `DWORD` | `EXTERNAL` | |
`COR_ENABLE_PROFILING` | Flag to indicate whether profiling should be enabled for the currently running process. | `DWORD` | `EXTERNAL` | `0` | DontPrependCOMPlus_ \| IgnoreConfigFiles
`COR_PROFILER` | Specifies GUID of profiler to load into currently running process | `STRING` | `EXTERNAL` | | DontPrependCOMPlus_
`COR_PROFILER_PATH` | Specifies the path to the DLL of profiler to load into currently running process | `STRING` | `EXTERNAL` | | DontPrependCOMPlus_
`COR_PROFILER_PATH_32` | Specifies the path to the DLL of profiler to load into currently running 32 bits process | `STRING` | `EXTERNAL` | | DontPrependCOMPlus_
`COR_PROFILER_PATH_64` | Specifies the path to the DLL of profiler to load into currently running 64 bits process | `STRING` | `EXTERNAL` | | DontPrependCOMPlus_
`CORECLR_ENABLE_PROFILING` | CoreCLR only: Flag to indicate whether profiling should be enabled for the currently running process. | `DWORD` | `EXTERNAL` | `0` | DontPrependCOMPlus_ \| IgnoreConfigFiles
`CORECLR_PROFILER` | CoreCLR only: Specifies GUID of profiler to load into currently running process | `STRING` | `EXTERNAL` | | DontPrependCOMPlus_
`CORECLR_PROFILER_PATH` | CoreCLR only: Specifies the path to the DLL of profiler to load into currently running process | `STRING` | `EXTERNAL` | | DontPrependCOMPlus_
`CORECLR_PROFILER_PATH_32` | CoreCLR only: Specifies the path to the DLL of profiler to load into currently running 32 process | `STRING` | `EXTERNAL` | | DontPrependCOMPlus_
`CORECLR_PROFILER_PATH_64` | CoreCLR only: Specifies the path to the DLL of profiler to load into currently running 64 process | `STRING` | `EXTERNAL` | | DontPrependCOMPlus_
`ETW_ObjectAllocationEventsPerTypePerSec` | Desired number of GCSampledObjectAllocation ETW events to be logged per type per second.  If 0, then the default built in to the implementation for the enabled event (e.g., High, Low), will be used. | `STRING` | `UNSUPPORTED` | | REGUTIL_default
`ETWEnabled` | This flag is used on OSes < Vista to enable/disable ETW. It is disabled by default | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`MsBetweenAttachCheck` |  | `DWORD` | `EXTERNAL` | `500` |
`PerfMapEnabled` | This flag is used on Linux to enable writing /tmp/perf-$pid.map. It is disabled by default | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`PerfMapIgnoreSignal` | When perf map is enabled, this option will configure the specified signal to be accepted and ignored as a marker in the perf logs.  It is disabled by default | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`ProfAPI_AttachProfilerMinTimeoutInMs` | Timeout in ms for the minimum time out value of AttachProfiler | `DWORD` | `EXTERNAL` | `10*1000` |
`ProfAPI_DetachMaxSleepMs` | The maximum time, in milliseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded. | `DWORD` | `EXTERNAL` | `0` |
`ProfAPI_DetachMinSleepMs` | The minimum time, in milliseconds, the CLR will wait before checking whether a profiler that is in the process of detaching is ready to be unloaded. | `DWORD` | `EXTERNAL` | `0` |
`ProfAPI_EnableRejitDiagnostics` | Enable extra dumping to stdout of rejit structures | `DWORD` | `INTERNAL` | `0` |
`ProfAPI_ProfilerCompatibilitySetting` | Specifies the profiler loading policy (the default is not to load a V2 profiler in V4) | `STRING` | `EXTERNAL` | | REGUTIL_default \| TrimWhiteSpaceFromStringValue
`ProfApi_RejitOnAttach` | Enables the ability for profilers to rejit methods on attach. | `DWORD` | `EXTERNAL` | `1` |
`ProfAPI_TestOnlyEnableICorProfilerInfo` | Test-only flag to allow attaching profiler tests to call ICorProfilerInfo interface, which would otherwise be disallowed for attaching profilers | `DWORD` | `INTERNAL` | `0` |
`ProfAPI_ValidateNGENInstrumentation` | This flag enables additional validations when using the IMetaDataEmit APIs for NGEN'ed images to ensure only supported edits are made. | `DWORD` | `UNSUPPORTED` | `0` |
`ProfAPIFault` | Test-only bitmask to inject various types of faults in the profapi code | `DWORD` | `INTERNAL` | `0` |
`ProfAPIMaxWaitForTriggerMs` | Timeout in ms for profilee to wait for each blocking operation performed by trigger app. | `DWORD` | `EXTERNAL` | `5*60*1000` |
`StartupDelayMS` |  | `STRING` | `EXTERNAL` | |
`TestOnlyAllowedEventMask` | Test-only bitmask to allow profiler tests to override CLR enforcement of COR_PRF_ALLOWABLE_AFTER_ATTACH and COR_PRF_MONITOR_IMMUTABLE | `DWORD` | `INTERNAL` | `0` |
`TestOnlyEnableObjectAllocatedHook` | Test-only flag that forces CLR to initialize on startup as if ObjectAllocated callback were requested, to enable post-attach ObjectAllocated functionality. | `DWORD` | `INTERNAL` | `0` |
`TestOnlyEnableSlowELTHooks` | Test-only flag that forces CLR to initialize on startup as if slow-ELT were requested, to enable post-attach ELT functionality. | `DWORD` | `INTERNAL` | `0` |

#### Spinning heuristics Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`Monitor_SpinCount` | Hex value specifying the maximum number of spin iterations Monitor may perform upon contention on acquiring the lock before waiting. | `DWORD` | `INTERNAL` | `0x1e` | EEConfig_default
`SpinBackoffFactor` | Hex value specifying the growth of each successive spin duration | `DWORD` | `EXTERNAL` | `0x3` | EEConfig_default
`SpinInitialDuration` | Hex value specifying the first spin duration | `DWORD` | `EXTERNAL` | `0x32` | EEConfig_default
`SpinLimitConstant` | Hex value specifying the constant to add when calculating the maximum spin duration | `DWORD` | `EXTERNAL` | `0x0` | EEConfig_default
`SpinLimitProcCap` | Hex value specifying the largest value of NumProcs to use when calculating the maximum spin duration | `DWORD` | `EXTERNAL` | `0xFFFFFFFF` | EEConfig_default
`SpinLimitProcFactor` | Hex value specifying the multiplier on NumProcs to use when calculating the maximum spin duration | `DWORD` | `EXTERNAL` | `0x4E20` | EEConfig_default
`SpinRetryCount` | Hex value specifying the number of times the entire spin process is repeated (when applicable) | `DWORD` | `EXTERNAL` | `0xA` | EEConfig_default

#### Stress Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`ForceEnc` | Forces Edit and Continue to be on for all eligible modules. | `DWORD` | `UNSUPPORTED` | |
`StressCOMCall` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`StressLog` | Turns on the stress log. | `DWORD` | `UNSUPPORTED` | |
`StressLogSize` | Stress log size in bytes per thread. | `DWORD` | `UNSUPPORTED` | |
`StressOn` | Enables the STRESS_ASSERT macro that stops runtime quickly (to prevent the clr state from changing significantly before breaking) | `DWORD` | `INTERNAL` | |
`stressSynchronized` | Unknown if or where this is used; unless a test is specifically depending on this, it can be removed. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`StressThreadCount` |  | `DWORD` | `EXTERNAL` | |

#### Thread (miscellaneous) Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`Thread_DeadThreadCountThresholdForGCTrigger` | In the heuristics to clean up dead threads, this threshold must be reached before triggering a GC will be considered. Set to 0 to disable triggering a GC based on dead threads. | `DWORD` | `INTERNAL` | `75` |
`Thread_DeadThreadGCTriggerPeriodMilliseconds` | In the heuristics to clean up dead threads, this much time must have elapsed since the previous max-generation GC before triggering another GC will be considered | `DWORD` | `INTERNAL` | `1000 * 60 * 30` |

#### Thread Suspend Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`DiagnosticSuspend` |  | `DWORD` | `INTERNAL` | `0` |
`SuspendDeadlockTimeout` |  | `DWORD` | `INTERNAL` | `40000` |
`SuspendThreadDeadlockTimeoutMs` |  | `DWORD` | `INTERNAL` | `2000` |
`ThreadSuspendInjection` | Specifies whether to inject activations for thread suspension on Unix | `DWORD` | `INTERNAL` | `1` |

#### Threadpool Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`HillClimbing_Bias` | The 'cost' of a thread.  0 means drive for increased throughput regardless of thread count; higher values bias more against higher thread counts. | `DWORD` | `INTERNAL` | `15` |
`HillClimbing_Disable` | Disables hill climbing for thread adjustments in the thread pool | `DWORD` | `INTERNAL` | `0` |
`HillClimbing_ErrorSmoothingFactor` |  | `DWORD` | `INTERNAL` | `1` |
`HillClimbing_GainExponent` | The exponent to apply to the gain, times 100.  100 means to use linear gain, higher values will enhance large moves and damp small ones. | `DWORD` | `INTERNAL` | `200` |
`HillClimbing_MaxChangePerSample` |  | `DWORD` | `INTERNAL` | `20` |
`HillClimbing_MaxChangePerSecond` |  | `DWORD` | `INTERNAL` | `4` |
`HillClimbing_MaxSampleErrorPercent` |  | `DWORD` | `INTERNAL` | `15` |
`HillClimbing_MaxWaveMagnitude` |  | `DWORD` | `INTERNAL` | `20` |
`HillClimbing_SampleIntervalHigh` |  | `DWORD` | `INTERNAL` | `200` |
`HillClimbing_SampleIntervalLow` |  | `DWORD` | `INTERNAL` | `10` |
`HillClimbing_TargetSignalToNoiseRatio` |  | `DWORD` | `INTERNAL` | `300` |
`HillClimbing_WaveHistorySize` |  | `DWORD` | `INTERNAL` | `8` |
`HillClimbing_WaveMagnitudeMultiplier` |  | `DWORD` | `INTERNAL` | `100` |
`HillClimbing_WavePeriod` |  | `DWORD` | `INTERNAL` | `4` |
`Thread_UseAllCpuGroups` | Specifies if to automatically distribute thread across CPU Groups | `DWORD` | `EXTERNAL` | `0` |
`ThreadPool_DebugBreakOnWorkerStarvation` | Breaks into the debugger if the ThreadPool detects work queue starvation | `DWORD` | `INTERNAL` | `0` |
`ThreadPool_DisableStarvationDetection` | Disables the ThreadPool feature that forces new threads to be added when workitems run for too long | `DWORD` | `INTERNAL` | `0` |
`ThreadPool_EnableWorkerTracking` | Enables extra expensive tracking of how many workers threads are working simultaneously | `DWORD` | `INTERNAL` | `0` |
`ThreadPool_ForceMaxWorkerThreads` | Overrides the MaxThreads setting for the ThreadPool worker pool | `DWORD` | `INTERNAL` | `0` |
`ThreadPool_ForceMinWorkerThreads` | Overrides the MinThreads setting for the ThreadPool worker pool | `DWORD` | `INTERNAL` | `0` |
`ThreadPool_UnfairSemaphoreSpinLimit` | Maximum number of spins per processor a thread pool worker thread performs before waiting for work | `DWORD` | `INTERNAL` | `0x32` |
`ThreadpoolTickCountAdjustment` |  | `DWORD` | `INTERNAL` | `0` |

#### Tiered Compilation Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`TC_CallCounting` | Enabled by default (only activates when TieredCompilation is also enabled). If disabled immediately backpatches prestub, and likely prevents any promotion to higher tiers | `DWORD` | `INTERNAL` | `1` |
`TC_CallCountingDelayMs` | A perpetual delay in milliseconds that is applied call counting in tier 0 and jitting at higher tiers, while there is startup-like activity. | `DWORD` | `INTERNAL` | `100` |
`TC_CallCountThreshold` | Number of times a method must be called in tier 0 after which it is promoted to the next tier. | `DWORD` | `INTERNAL` | `30` |
`TC_DelaySingleProcMultiplier` | Multiplier for TC_CallCountingDelayMs that is applied on a single-processor machine or when the process is affinitized to a single processor. | `DWORD` | `INTERNAL` | `10` |
`TC_QuickJit` | For methods that would be jitted, enable using quick JIT when appropriate. | `DWORD` | `EXTERNAL` | `0` |
`TC_QuickJitForLoops` | When quick JIT is enabled, quick JIT may also be used for methods that contain loops. | `DWORD` | `UNSUPPORTED` | `0` |
`TieredCompilation` | Enables tiered compilation | `DWORD` | `EXTERNAL` | `1` |

#### TypeLoader Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`TypeLoader_InjectInterfaceDuplicates` | Injects duplicates in interface map for all types. | `DWORD` | `INTERNAL` | `0` |

#### Uncategorized Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`ActivatePatchSkip` | Allows an assert when ActivatePatchSkip is called | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`AlwaysUseMetadataInterfaceMapLayout` | Used for debugging generic interface map layout. | `DWORD` | `INTERNAL` | |
`AssertOnUnneededThis` | While the ConfigDWORD is unnecessary, the contained ASSERT should be kept. This may result in some work tracking down violating MethodDescCallSites. | `DWORD` | `INTERNAL` | `0` |
`AssertStacktrace` |  | `DWORD` | `INTERNAL` | `1` | REGUTIL_default
`clearNativeImageStress` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`CPUFamily` |  | `DWORD` | `INTERNAL` | |
`CPUFeatures` |  | `DWORD` | `INTERNAL` | |
`DisableConfigCache` | Used to disable the \"probabilistic\" config cache, which walks through the appropriate config registry keys on init and probabilistically keeps track of which exist. | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`DisableStackwalkCache` |  | `DWORD` | `EXTERNAL` | |
`DoubleArrayToLargeObjectHeap` | Controls double[] placement | `DWORD` | `UNSUPPORTED` | |
`DumpConfiguration` | Dumps runtime properties of xml configuration files to the log. | `DWORD` | `INTERNAL` | `0` |
`DumpOnClassLoad` | Dumps information about loaded class to log. | `STRING` | `INTERNAL` | |
`ExpandAllOnLoad` |  | `DWORD` | `INTERNAL` | |
`ForcedRuntime` | Verify version of CLR loaded | `STRING` | `INTERNAL` | |
`ForceRelocs` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`GenerateLongJumpDispatchStubRatio` | Useful for testing VSD on AMD64 | `DWORD` | `INTERNAL` | |
`HashStack` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`HostManagerConfig` |  | `DWORD` | `INTERNAL` | `(DWORD)-1` |
`HostTestThreadAbort` |  | `DWORD` | `INTERNAL` | `0` |
`IgnoreDllMainReturn` | Don't check the return value of DllMain if this is set | `DWORD` | `UNSUPPORTED` | `0` | ConfigFile_ApplicationFirst
`InvokeHalt` | Throws an assert when the given method is invoked through reflection. | `STRING` | `INTERNAL` | |
`legacyNullReferenceExceptionPolicy` |  | `DWORD` | `UNSUPPORTED` | |
`legacyUnhandledExceptionPolicy` |  | `DWORD` | `UNSUPPORTED` | |
`MaxStackDepth` |  | `DWORD` | `INTERNAL` | |
`MaxStubUnwindInfoSegmentSize` |  | `DWORD` | `INTERNAL` | |
`MaxThreadRecord` |  | `DWORD` | `INTERNAL` | |
`MessageDebugOut` |  | `DWORD` | `INTERNAL` | `0` |
`MscorsnLogging` | Enables strong name logging | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`NativeImageRequire` |  | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`NestedEhOom` |  | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`NoGuiOnAssert` |  | `DWORD` | `INTERNAL` | `INTERNAL_NoGuiOnAssert_Default` | REGUTIL_default
`NoProcedureSplitting` |  | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`NoStringInterning` | Disallows string interning. I see no value in it anymore. | `DWORD` | `INTERNAL` | `1` | REGUTIL_default
`NotifyBadAppCfg` | Whether to show a message box for bad application config file. | `DWORD` | `EXTERNAL` | |
`PauseOnLoad` | Stops in SystemDomain::init. I think it can be removed. | `DWORD` | `INTERNAL` | |
`PerfAllocsSizeThreshold` | Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler. | `DWORD` | `INTERNAL` | `0x3FFFFFFF` |
`PerfNumAllocsThreshold` | Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler. | `DWORD` | `INTERNAL` | `0x3FFFFFFF` |
`PerfTypesToLog` | Log facility LF_GCALLOC logs object allocations. This flag controls which ones also log stacktraces. Predates ClrProfiler. | `STRING` | `INTERNAL` | |
`Prepopulate1` |  | `DWORD` | `EXTERNAL` | `1` |
`PrestubGC` |  | `STRING` | `INTERNAL` | |
`PrestubHalt` |  | `STRING` | `INTERNAL` | |
`RestrictedGCStressExe` |  | `STRING` | `EXTERNAL` | |
`ReturnSourceTypeForTesting` | Allows returning the (internal only) source type of an IL to Native mapping for debugging purposes | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`RSStressLog` | Allows turning on logging for RS startup | `DWORD` | `UNSUPPORTED` | `0` | REGUTIL_default
`SaveThreadInfo` |  | `DWORD` | `INTERNAL` | |
`SaveThreadInfoMask` |  | `DWORD` | `INTERNAL` | |
`SBDumpOnNewIndex` | Used for Syncblock debugging. It's been a while since any of those have been used. | `DWORD` | `INTERNAL` | `0` |
`SBDumpOnResize` | Used for Syncblock debugging. It's been a while since any of those have been used. | `DWORD` | `INTERNAL` | `0` |
`SBDumpStyle` | Used for Syncblock debugging. It's been a while since any of those have been used. | `DWORD` | `INTERNAL` | `0` |
`ShimDatabaseVersion` | Force using shim database version in registry | `STRING` | `UNSUPPORTED` | |
`SleepOnExit` | Used for lrak detection. I'd say deprecated by umdh. | `DWORD` | `UNSUPPORTED` | `0` |
`StubLinkerUnwindInfoVerificationOn` |  | `DWORD` | `INTERNAL` | |
`SuccessExit` |  | `DWORD` | `UNSUPPORTED` | `0` | REGUTIL_default
`SymbolReadingPolicy` | Specifies when PDBs may be read | `DWORD` | `EXTERNAL` | |
`TestDataConsistency` | Allows ensuring the left side is not holding locks (and may thus be in an inconsistent state) when inspection occurs | `DWORD` | `UNSUPPORTED` | `FALSE` |
`ThreadGuardPages` |  | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`Timeline` |  | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`TotalStressLogSize` | Total stress log size in bytes. | `DWORD` | `UNSUPPORTED` | |
`TraceIUnknown` |  | `DWORD` | `EXTERNAL` | |
`TraceWrap` |  | `DWORD` | `EXTERNAL` | |
`TURNOFFDEBUGINFO` |  | `DWORD` | `EXTERNAL` | |
`UseMethodDataCache` | Used during feature development; may now be removed. | `DWORD` | `EXTERNAL` | `FALSE` |
`UseParentMethodData` | Used during feature development; may now be removed. | `DWORD` | `EXTERNAL` | `TRUE` |
`VerifierOff` |  | `DWORD` | `INTERNAL` | |
`VerifyAllOnLoad` |  | `DWORD` | `EXTERNAL` | |

#### Virtual call stubs Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`VirtualCallStubCollideMonoPct` | Used only when STUB_LOGGING is defined, which by default is not. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`VirtualCallStubCollideWritePct` | Used only when STUB_LOGGING is defined, which by default is not. | `DWORD` | `INTERNAL` | `100` | REGUTIL_default
`VirtualCallStubDumpLogCounter` | Used only when STUB_LOGGING is defined, which by default is not. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`VirtualCallStubDumpLogIncr` | Used only when STUB_LOGGING is defined, which by default is not. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`VirtualCallStubLogging` | Worth keeping, but should be moved into \"#ifdef STUB_LOGGING\" blocks. This goes for most (or all) of the stub logging infrastructure. | `DWORD` | `EXTERNAL` | `0` | REGUTIL_default
`VirtualCallStubMissCount` | Used only when STUB_LOGGING is defined, which by default is not. | `DWORD` | `INTERNAL` | `100` | REGUTIL_default
`VirtualCallStubResetCacheCounter` | Used only when STUB_LOGGING is defined, which by default is not. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default
`VirtualCallStubResetCacheIncr` | Used only when STUB_LOGGING is defined, which by default is not. | `DWORD` | `INTERNAL` | `0` | REGUTIL_default

#### Watson Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`DisableWatsonForManagedExceptions` | Disable Watson and debugger launching for managed exceptions | `DWORD` | `INTERNAL` | `0` |

#### Zap Configuration Knobs

Name | Description | Type | Class | Default Value | Flags
-----|-------------|------|-------|---------------|-------
`DebugAssertOnMissedCOWPage` |  | `DWORD` | `INTERNAL` | `1` |
`EnableEventLog` | Enable/disable use of EnableEventLogging mechanism  | `DWORD` | `EXTERNAL` | `0` |
`EventNameFilter` |  | `STRING` | `INTERNAL` | |
`EventSourceFilter` |  | `STRING` | `INTERNAL` | |
`ReadyToRun` | Enable/disable use of ReadyToRun native code | `DWORD` | `EXTERNAL` | `1` |
`ReadyToRunExcludeList` | List of assemblies that cannot use Ready to Run images | `STRING` | `EXTERNAL` | |
`ReadyToRunLogFile` | Name of file to log success/failure of using Ready to Run images | `STRING` | `EXTERNAL` | |
`ZapBBInstr` |  | `STRING` | `INTERNAL` | | REGUTIL_default
`ZapBBInstrDir` |  | `STRING` | `EXTERNAL` | |
`ZapDisable` |  | `DWORD` | `EXTERNAL` | `0` |
`ZapExclude` |  | `STRING` | `INTERNAL` | | REGUTIL_default
`ZapLazyCOWPagesEnabled` |  | `DWORD` | `INTERNAL` | `1` |
`ZapOnly` |  | `STRING` | `INTERNAL` | | REGUTIL_default
`ZapRequire` |  | `DWORD` | `EXTERNAL` | |
`ZapRequireExcludeList` |  | `STRING` | `EXTERNAL` | |
`ZapRequireList` |  | `STRING` | `EXTERNAL` | |
`ZapSet` |  | `STRING` | `EXTERNAL` | | REGUTIL_default


## PAL Configuration Knobs
All the names below need to be prefixed by `COMPlus_`.

Name | Description | Type | Default Value
-----|-------------|------|---------------
`DefaultStackSize` | Overrides the default stack size for secondary threads | `STRING` | `0`
`DbgEnableMiniDump` | If set to 1, enables this core dump generation. The default is NOT to generate a dump | `DWORD` | `0`
`DbgMiniDumpName` | If set, use as the template to create the dump path and file name. The pid can be placed in the name with %d. | `STRING` | `_/tmp/coredump.%d_`
`DbgMiniDumpType` | If set to 1 generates _MiniDumpNormal_, 2 _MiniDumpWithPrivateReadWriteMemory_, 3 _MiniDumpFilterTriage_, 4 _MiniDumpWithFullMemory_ | `DWORD` | `1`
`CreateDumpDiagnostics` | If set to 1, enables the _createdump_ utilities diagnostic messages (TRACE macro) | `DWORD` | `0`

