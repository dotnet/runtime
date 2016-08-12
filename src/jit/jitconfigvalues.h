// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !defined(CONFIG_INTEGER) || !defined(CONFIG_STRING) || !defined(CONFIG_METHODSET)
#error CONFIG_INTEGER, CONFIG_STRING, and CONFIG_METHODSET must be defined before including this file.
#endif // !defined(CONFIG_INTEGER) || !defined(CONFIG_STRING) || !defined(CONFIG_METHODSET)

#if defined(DEBUG)
CONFIG_INTEGER(AltJitLimit, W("AltJitLimit"), 0)               // Max number of functions to use altjit for (decimal)
CONFIG_INTEGER(AltJitSkipOnAssert, W("AltJitSkipOnAssert"), 0) // If AltJit hits an assert, fall back to the fallback
                                                               // JIT. Useful in conjunction with
                                                               // COMPlus_ContinueOnAssert=1
CONFIG_INTEGER(BreakOnDumpToken, W("BreakOnDumpToken"), 0xffffffff) // Breaks when using internal logging on a
                                                                    // particular token value.
CONFIG_INTEGER(DebugBreakOnVerificationFailure, W("DebugBreakOnVerificationFailure"), 0) // Halts the jit on
                                                                                         // verification failure
CONFIG_INTEGER(DiffableDasm, W("JitDiffableDasm"), 0)            // Make the disassembly diff-able
CONFIG_INTEGER(DisplayLoopHoistStats, W("JitLoopHoistStats"), 0) // Display JIT loop hoisting statistics
CONFIG_INTEGER(DisplayMemStats, W("JitMemStats"), 0)             // Display JIT memory usage statistics
CONFIG_INTEGER(DumpJittedMethods, W("DumpJittedMethods"), 0)     // Prints all jitted methods to the console
CONFIG_INTEGER(EnablePCRelAddr, W("JitEnablePCRelAddr"), 1)      // Whether absolute addr be encoded as PC-rel offset by
                                                                 // RyuJIT where possible
CONFIG_INTEGER(InterpreterFallback, W("InterpreterFallback"), 0) // Fallback to the interpreter when the JIT compiler
                                                                 // fails
CONFIG_INTEGER(JitAssertOnMaxRAPasses, W("JitAssertOnMaxRAPasses"), 0)
CONFIG_INTEGER(JitBreakEmitOutputInstr, W("JitBreakEmitOutputInstr"), -1)
CONFIG_INTEGER(JitBreakMorphTree, W("JitBreakMorphTree"), 0xffffffff)
CONFIG_INTEGER(JitBreakOnBadCode, W("JitBreakOnBadCode"), 0)
CONFIG_INTEGER(JitBreakOnMinOpts, W("JITBreakOnMinOpts"), 0) // Halt if jit switches to MinOpts
CONFIG_INTEGER(JitBreakOnUnsafeCode, W("JitBreakOnUnsafeCode"), 0)
CONFIG_INTEGER(JitCanUseSSE2, W("JitCanUseSSE2"), -1)
CONFIG_INTEGER(JitCloneLoops, W("JitCloneLoops"), 1) // If 0, don't clone. Otherwise clone loops for optimizations.
CONFIG_INTEGER(JitDebugLogLoopCloning, W("JitDebugLogLoopCloning"), 0) // In debug builds log places where loop cloning
                                                                       // optimizations are performed on the fast path.
CONFIG_INTEGER(JitDefaultFill, W("JitDefaultFill"), 0xff) // In debug builds, initialize the memory allocated by the nra
                                                          // with this byte.
CONFIG_INTEGER(JitDirectAlloc, W("JitDirectAlloc"), 0)
CONFIG_INTEGER(JitDoAssertionProp, W("JitDoAssertionProp"), 1) // Perform assertion propagation optimization
CONFIG_INTEGER(JitDoCopyProp, W("JitDoCopyProp"), 1)   // Perform copy propagation on variables that appear redundant
CONFIG_INTEGER(JitDoEarlyProp, W("JitDoEarlyProp"), 1) // Perform Early Value Propagataion
CONFIG_INTEGER(JitDoLoopHoisting, W("JitDoLoopHoisting"), 1)   // Perform loop hoisting on loop invariant values
CONFIG_INTEGER(JitDoRangeAnalysis, W("JitDoRangeAnalysis"), 1) // Perform range check analysis
CONFIG_INTEGER(JitDoSsa, W("JitDoSsa"), 1) // Perform Static Single Assignment (SSA) numbering on the variables
CONFIG_INTEGER(JitDoValueNumber, W("JitDoValueNumber"), 1) // Perform value numbering on method expressions
CONFIG_INTEGER(JitDoubleAlign, W("JitDoubleAlign"), 1)
CONFIG_INTEGER(JitDumpASCII, W("JitDumpASCII"), 1)         // Uses only ASCII characters in tree dumps
CONFIG_INTEGER(JitDumpFgDot, W("JitDumpFgDot"), 0)         // Set to non-zero to emit Dot instead of Xml Flowgraph dump
CONFIG_INTEGER(JitDumpTerseLsra, W("JitDumpTerseLsra"), 1) // Produce terse dump output for LSRA
CONFIG_INTEGER(JitDumpToDebugger, W("JitDumpToDebugger"), 0)     // Output JitDump output to the debugger
CONFIG_INTEGER(JitDumpVerboseSsa, W("JitDumpVerboseSsa"), 0)     // Produce especially verbose dump output for SSA
CONFIG_INTEGER(JitDumpVerboseTrees, W("JitDumpVerboseTrees"), 0) // Enable more verbose tree dumps
CONFIG_INTEGER(JitEmitPrintRefRegs, W("JitEmitPrintRefRegs"), 0)
CONFIG_INTEGER(JitExpensiveDebugCheckLevel, W("JitExpensiveDebugCheckLevel"), 0) // Level indicates how much checking
                                                                                 // beyond the default to do in debug
                                                                                 // builds (currently 1-2)
CONFIG_INTEGER(JitForceFallback, W("JitForceFallback"), 0) // Set to non-zero to test NOWAY assert by forcing a retry
CONFIG_INTEGER(JitForceVer, W("JitForceVer"), 0)
CONFIG_INTEGER(JitFullyInt, W("JitFullyInt"), 0)           // Forces Fully interruptable code
CONFIG_INTEGER(JitFunctionTrace, W("JitFunctionTrace"), 0) // If non-zero, print JIT start/end logging
CONFIG_INTEGER(JitGCChecks, W("JitGCChecks"), 0)
CONFIG_INTEGER(JitGCInfoLogging, W("JitGCInfoLogging"), 0) // If true, prints GCInfo-related output to standard output.
CONFIG_INTEGER(JitHashBreak, W("JitHashBreak"), -1)        // Same as JitBreak, but for a method hash
CONFIG_INTEGER(JitHashDump, W("JitHashDump"), -1)          // Same as JitDump, but for a method hash
CONFIG_INTEGER(JitHashDumpIR, W("JitHashDumpIR"), -1)      // Same as JitDumpIR, but for a method hash
CONFIG_INTEGER(JitHashHalt, W("JitHashHalt"), -1)          // Same as JitHalt, but for a method hash
CONFIG_INTEGER(JitInlineAdditionalMultiplier, W("JitInlineAdditionalMultiplier"), 0)
CONFIG_INTEGER(JitInlinePrintStats, W("JitInlinePrintStats"), 0)
CONFIG_INTEGER(JitInlineSize, W("JITInlineSize"), DEFAULT_MAX_INLINE_SIZE)
CONFIG_INTEGER(JitInlineDepth, W("JITInlineDepth"), DEFAULT_MAX_INLINE_DEPTH)
CONFIG_INTEGER(JitLongAddress, W("JitLongAddress"), 0) // Force using the large pseudo instruction form for long address
CONFIG_INTEGER(JitMaxTempAssert, W("JITMaxTempAssert"), 1)
CONFIG_INTEGER(JitMaxUncheckedOffset, W("JitMaxUncheckedOffset"), 8)
CONFIG_INTEGER(JitMinOpts, W("JITMinOpts"), 0)                                       // Forces MinOpts
CONFIG_INTEGER(JitMinOptsBbCount, W("JITMinOptsBbCount"), DEFAULT_MIN_OPTS_BB_COUNT) // Internal jit control of MinOpts
CONFIG_INTEGER(JitMinOptsCodeSize, W("JITMinOptsCodeSize"), DEFAULT_MIN_OPTS_CODE_SIZE)       // Internal jit control of
                                                                                              // MinOpts
CONFIG_INTEGER(JitMinOptsInstrCount, W("JITMinOptsInstrCount"), DEFAULT_MIN_OPTS_INSTR_COUNT) // Internal jit control of
                                                                                              // MinOpts
CONFIG_INTEGER(JitMinOptsLvNumCount, W("JITMinOptsLvNumcount"), DEFAULT_MIN_OPTS_LV_NUM_COUNT) // Internal jit control
                                                                                               // of MinOpts
CONFIG_INTEGER(JitMinOptsLvRefCount, W("JITMinOptsLvRefcount"), DEFAULT_MIN_OPTS_LV_REF_COUNT) // Internal jit control
                                                                                               // of MinOpts
CONFIG_INTEGER(JitNoCMOV, W("JitNoCMOV"), 0)
CONFIG_INTEGER(JitNoCSE, W("JitNoCSE"), 0)
CONFIG_INTEGER(JitNoCSE2, W("JitNoCSE2"), 0)
CONFIG_INTEGER(JitNoForceFallback, W("JitNoForceFallback"), 0) // Set to non-zero to prevent NOWAY assert testing.
                                                               // Overrides COMPlus_JitForceFallback and JIT stress
                                                               // flags.
CONFIG_INTEGER(JitNoHoist, W("JitNoHoist"), 0)
CONFIG_INTEGER(JitNoInline, W("JitNoInline"), 0)                 // Disables inlining of all methods
CONFIG_INTEGER(JitNoMemoryBarriers, W("JitNoMemoryBarriers"), 0) // If 1, don't generate memory barriers
CONFIG_INTEGER(JitNoRegLoc, W("JitNoRegLoc"), 0)
CONFIG_INTEGER(JitNoStructPromotion, W("JitNoStructPromotion"), 0) // Disables struct promotion in Jit32
CONFIG_INTEGER(JitNoUnroll, W("JitNoUnroll"), 0)
CONFIG_INTEGER(JitOrder, W("JitOrder"), 0)
CONFIG_INTEGER(JitPInvokeCheckEnabled, W("JITPInvokeCheckEnabled"), 0)
CONFIG_INTEGER(JitPInvokeEnabled, W("JITPInvokeEnabled"), 1)
CONFIG_INTEGER(JitPrintInlinedMethods, W("JitPrintInlinedMethods"), 0)
CONFIG_INTEGER(JitRequired, W("JITRequired"), -1)
CONFIG_INTEGER(JitRoundFloat, W("JITRoundFloat"), DEFAULT_ROUND_LEVEL)
CONFIG_INTEGER(JitSkipArrayBoundCheck, W("JitSkipArrayBoundCheck"), 0)
CONFIG_INTEGER(JitSlowDebugChecksEnabled, W("JitSlowDebugChecksEnabled"), 1) // Turn on slow debug checks
CONFIG_INTEGER(JitSplitFunctionSize, W("JitSplitFunctionSize"), 0) // On ARM, use this as the maximum function/funclet
                                                                   // size for creating function fragments (and creating
                                                                   // multiple RUNTIME_FUNCTION entries)
CONFIG_INTEGER(JitSsaStress, W("JitSsaStress"), 0) // Perturb order of processing of blocks in SSA; 0 = no stress; 1 =
                                                   // use method hash; * = supplied value as random hash
CONFIG_INTEGER(JitStackChecks, W("JitStackChecks"), 0)
CONFIG_INTEGER(JitStress, W("JitStress"), 0) // Internal Jit stress mode: 0 = no stress, 2 = all stress, other = vary
                                             // stress based on a hash of the method and this value
CONFIG_INTEGER(JitStressBBProf, W("JitStressBBProf"), 0)               // Internal Jit stress mode
CONFIG_INTEGER(JitStressBiasedCSE, W("JitStressBiasedCSE"), 0x101)     // Internal Jit stress mode: decimal bias value
                                                                       // between (0,100) to perform CSE on a candidate.
                                                                       // 100% = All CSEs. 0% = 0 CSE. (> 100) means no
                                                                       // stress.
CONFIG_INTEGER(JitStressFP, W("JitStressFP"), 0)                       // Internal Jit stress mode
CONFIG_INTEGER(JitStressModeNamesOnly, W("JitStressModeNamesOnly"), 0) // Internal Jit stress: if nonzero, only enable
                                                                       // stress modes listed in JitStressModeNames
CONFIG_INTEGER(JitStressRegs, W("JitStressRegs"), 0)
CONFIG_INTEGER(JitStrictCheckForNonVirtualCallToVirtualMethod, W("JitStrictCheckForNonVirtualCallToVirtualMethod"), 1)
CONFIG_INTEGER(JitVNMapSelLimit, W("JitVNMapSelLimit"), 0) // If non-zero, assert if # of VNF_MapSelect applications
                                                           // considered reaches this
CONFIG_INTEGER(NgenHashDump, W("NgenHashDump"), -1)        // same as JitHashDump, but for ngen
CONFIG_INTEGER(NgenHashDumpIR, W("NgenHashDumpIR"), -1)    // same as JitHashDumpIR, but for ngen
CONFIG_INTEGER(NgenOrder, W("NgenOrder"), 0)
CONFIG_INTEGER(RunAltJitCode, W("RunAltJitCode"), 1) // If non-zero, and the compilation succeeds for an AltJit, then
                                                     // use the code. If zero, then we always throw away the generated
                                                     // code and fall back to the default compiler.
CONFIG_INTEGER(RunComponentUnitTests, W("JitComponentUnitTests"), 0) // Run JIT component unit tests
CONFIG_INTEGER(ShouldInjectFault, W("InjectFault"), 0)
CONFIG_INTEGER(StackProbesOverride, W("JitStackProbes"), 0)
CONFIG_INTEGER(StressCOMCall, W("StressCOMCall"), 0)
CONFIG_INTEGER(TailcallStress, W("TailcallStress"), 0)
CONFIG_INTEGER(TreesBeforeAfterMorph, W("JitDumpBeforeAfterMorph"), 0) // If 1, display each tree before/after morphing
CONFIG_METHODSET(JitBreak, W("JitBreak")) // Stops in the importer when compiling a specified method
CONFIG_METHODSET(JitDebugBreak, W("JitDebugBreak"))
CONFIG_METHODSET(JitDisasm, W("JitDisasm")) // Dumps disassembly for specified method
CONFIG_METHODSET(JitDump, W("JitDump"))     // Dumps trees for specified method
CONFIG_METHODSET(JitDumpIR, W("JitDumpIR")) // Dumps trees (in linear IR form) for specified method
CONFIG_METHODSET(JitEHDump, W("JitEHDump")) // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(JitExclude, W("JitExclude"))
CONFIG_METHODSET(JitForceProcedureSplitting, W("JitForceProcedureSplitting"))
CONFIG_METHODSET(JitGCDump, W("JitGCDump"))
CONFIG_METHODSET(JitHalt, W("JitHalt")) // Emits break instruction into jitted code
CONFIG_METHODSET(JitImportBreak, W("JitImportBreak"))
CONFIG_METHODSET(JitInclude, W("JitInclude"))
CONFIG_METHODSET(JitLateDisasm, W("JitLateDisasm"))
CONFIG_METHODSET(JitMinOptsName, W("JITMinOptsName"))                   // Forces MinOpts for a named function
CONFIG_METHODSET(JitNoProcedureSplitting, W("JitNoProcedureSplitting")) // Disallow procedure splitting for specified
                                                                        // methods
CONFIG_METHODSET(JitNoProcedureSplittingEH, W("JitNoProcedureSplittingEH")) // Disallow procedure splitting for
                                                                            // specified methods if they contain
                                                                            // exception handling
CONFIG_METHODSET(JitStressOnly, W("JitStressOnly")) // Internal Jit stress mode: stress only the specified method(s)
CONFIG_METHODSET(JitUnwindDump, W("JitUnwindDump")) // Dump the unwind codes for the method
CONFIG_METHODSET(NgenDisasm, W("NgenDisasm"))       // Same as JitDisasm, but for ngen
CONFIG_METHODSET(NgenDump, W("NgenDump"))           // Same as JitDump, but for ngen
CONFIG_METHODSET(NgenDumpIR, W("NgenDumpIR"))       // Same as JitDumpIR, but for ngen
CONFIG_METHODSET(NgenEHDump, W("NgenEHDump"))       // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(NgenGCDump, W("NgenGCDump"))
CONFIG_METHODSET(NgenUnwindDump, W("NgenUnwindDump")) // Dump the unwind codes for the method
CONFIG_STRING(JitDumpFg, W("JitDumpFg"))              // Dumps Xml/Dot Flowgraph for specified method
CONFIG_STRING(JitDumpFgDir, W("JitDumpFgDir"))        // Directory for Xml/Dot flowgraph dump(s)
CONFIG_STRING(JitDumpFgFile, W("JitDumpFgFile"))      // Filename for Xml/Dot flowgraph dump(s)
CONFIG_STRING(JitDumpFgPhase, W("JitDumpFgPhase")) // Phase-based Xml/Dot flowgraph support. Set to the short name of a
                                                   // phase to see the flowgraph after that phase. Leave unset to dump
                                                   // after COLD-BLK (determine first cold block) or set to * for all
                                                   // phases
CONFIG_STRING(JitDumpIRFormat, W("JitDumpIRFormat")) // Comma separated format control for JitDumpIR, values = {types |
                                                     // locals | ssa | valnums | kinds | flags | nodes | nolists |
                                                     // nostmts | noleafs | trees | dataflow}
CONFIG_STRING(JitDumpIRPhase, W("JitDumpIRPhase"))   // Phase control for JitDumpIR, values = {* | phasename}
CONFIG_STRING(JitLateDisasmTo, W("JITLateDisasmTo"))
CONFIG_STRING(JitRange, W("JitRange"))
CONFIG_STRING(JitStressModeNames, W("JitStressModeNames")) // Internal Jit stress mode: stress using the given set of
                                                           // stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL
CONFIG_STRING(JitStressModeNamesNot, W("JitStressModeNamesNot")) // Internal Jit stress mode: do NOT stress using the
                                                                 // given set of stress mode names, e.g. STRESS_REGS,
                                                                 // STRESS_TAILCALL
CONFIG_STRING(JitStressRange, W("JitStressRange"))               // Internal Jit stress mode
CONFIG_STRING(NgenDumpFg, W("NgenDumpFg"))                       // Ngen Xml Flowgraph support
CONFIG_STRING(NgenDumpFgDir, W("NgenDumpFgDir"))                 // Ngen Xml Flowgraph support
CONFIG_STRING(NgenDumpFgFile, W("NgenDumpFgFile"))               // Ngen Xml Flowgraph support
CONFIG_STRING(NgenDumpIRFormat, W("NgenDumpIRFormat"))           // Same as JitDumpIRFormat, but for ngen
CONFIG_STRING(NgenDumpIRPhase, W("NgenDumpIRPhase"))             // Same as JitDumpIRPhase, but for ngen
#endif                                                           // defined(DEBUG)

// AltJitAssertOnNYI should be 0 on targets where JIT is under developement or bring up stage, so as to facilitate
// fallback to main JIT on hitting a NYI.
#if defined(_TARGET_ARM64_) || defined(_TARGET_X86_)
CONFIG_INTEGER(AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 0) // Controls the AltJit behavior of NYI stuff
#else                                                        // !defined(_TARGET_ARM64_) && !defined(_TARGET_X86_)
CONFIG_INTEGER(AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 1) // Controls the AltJit behavior of NYI stuff
#endif                                                       // defined(_TARGET_ARM64_) || defined(_TARGET_X86_)

#if defined(_TARGET_AMD64_)
CONFIG_INTEGER(EnableAVX, W("EnableAVX"), 1) // Enable AVX instruction set for wide operations as default
#else                                        // !defined(_TARGET_AMD64_)
CONFIG_INTEGER(EnableAVX, W("EnableAVX"), 0)                 // Enable AVX instruction set for wide operations as default
#endif                                       // defined(_TARGET_AMD64_)

#if !defined(DEBUG) && !defined(_DEBUG)
CONFIG_INTEGER(JitEnableNoWayAssert, W("JitEnableNoWayAssert"), 0)
#else  // defined(DEBUG) || defined(_DEBUG)
CONFIG_INTEGER(JitEnableNoWayAssert, W("JitEnableNoWayAssert"), 1)
#endif // !defined(DEBUG) && !defined(_DEBUG)

CONFIG_INTEGER(JitAggressiveInlining, W("JitAggressiveInlining"), 0) // Aggressive inlining of all methods
CONFIG_INTEGER(JitELTHookEnabled, W("JitELTHookEnabled"), 0) // On ARM, setting this will emit Enter/Leave/TailCall
                                                             // callbacks
CONFIG_INTEGER(JitInlineSIMDMultiplier, W("JitInlineSIMDMultiplier"), 3)

#if defined(FEATURE_ENABLE_NO_RANGE_CHECKS)
CONFIG_INTEGER(JitNoRngChks, W("JitNoRngChks"), 0) // If 1, don't generate range checks
#endif                                             // defined(FEATURE_ENABLE_NO_RANGE_CHECKS)

CONFIG_INTEGER(JitRegisterFP, W("JitRegisterFP"), 3)           // Control FP enregistration
CONFIG_INTEGER(JitTelemetry, W("JitTelemetry"), 1)             // If non-zero, gather JIT telemetry data
CONFIG_INTEGER(JitVNMapSelBudget, W("JitVNMapSelBudget"), 100) // Max # of MapSelect's considered for a particular
                                                               // top-level invocation.
CONFIG_INTEGER(TailCallLoopOpt, W("TailCallLoopOpt"), 1)       // Convert recursive tail calls to loops
CONFIG_METHODSET(AltJit, W("AltJit")) // Enables AltJit and selectively limits it to the specified methods.
CONFIG_METHODSET(AltJitNgen,
                 W("AltJitNgen")) // Enables AltJit for NGEN and selectively limits it to the specified methods.

#if defined(ALT_JIT)
CONFIG_STRING(AltJitExcludeAssemblies,
              W("AltJitExcludeAssemblies")) // Do not use AltJit on this semicolon-delimited list of assemblies.
#endif                                      // defined(ALT_JIT)

CONFIG_STRING(JitFuncInfoFile, W("JitFuncInfoLogFile")) // If set, gather JIT function info and write to this file.
CONFIG_STRING(JitTimeLogCsv, W("JitTimeLogCsv")) // If set, gather JIT throughput data and write to a CSV file. This
                                                 // mode must be used in internal retail builds.
CONFIG_STRING(TailCallOpt, W("TailCallOpt"))

#if defined(DEBUG) || defined(INLINE_DATA)
CONFIG_INTEGER(JitInlineDumpData, W("JitInlineDumpData"), 0)
CONFIG_INTEGER(JitInlineDumpXml, W("JitInlineDumpXml"), 0) // 1 = full xml (all methods), 2 = minimal xml (only method
                                                           // with inlines)
CONFIG_INTEGER(JitInlineLimit, W("JitInlineLimit"), -1)
CONFIG_INTEGER(JitInlinePolicyDiscretionary, W("JitInlinePolicyDiscretionary"), 0)
CONFIG_INTEGER(JitInlinePolicyFull, W("JitInlinePolicyFull"), 0)
CONFIG_INTEGER(JitInlinePolicySize, W("JitInlinePolicySize"), 0)
CONFIG_INTEGER(JitInlinePolicyReplay, W("JitInlinePolicyReplay"), 0)
CONFIG_STRING(JitNoInlineRange, W("JitNoInlineRange"))
CONFIG_STRING(JitInlineReplayFile, W("JitInlineReplayFile"))
#endif // defined(DEBUG) || defined(INLINE_DATA)

CONFIG_INTEGER(JitInlinePolicyLegacy, W("JitInlinePolicyLegacy"), 0)
CONFIG_INTEGER(JitInlinePolicyModel, W("JitInlinePolicyModel"), 0)

#undef CONFIG_INTEGER
#undef CONFIG_STRING
#undef CONFIG_METHODSET
