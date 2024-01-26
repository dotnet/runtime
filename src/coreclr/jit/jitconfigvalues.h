// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !defined(CONFIG_INTEGER) || !defined(CONFIG_STRING) || !defined(CONFIG_METHODSET)
#error CONFIG_INTEGER, CONFIG_STRING, and CONFIG_METHODSET must be defined before including this file.
#endif // !defined(CONFIG_INTEGER) || !defined(CONFIG_STRING) || !defined(CONFIG_METHODSET)

#ifdef DEBUG
#define OPT_CONFIG // Enable optimization level configuration.
#endif

#if defined(DEBUG)

///
/// JIT
///
CONFIG_INTEGER(AltJitLimit, W("AltJitLimit"), 0)               // Max number of functions to use altjit for (decimal)
CONFIG_INTEGER(AltJitSkipOnAssert, W("AltJitSkipOnAssert"), 0) // If AltJit hits an assert, fall back to the fallback
                                                               // JIT. Useful in conjunction with
                                                               // DOTNET_ContinueOnAssert=1
CONFIG_INTEGER(BreakOnDumpToken, W("BreakOnDumpToken"), 0xffffffff) // Breaks when using internal logging on a
                                                                    // particular token value.
CONFIG_INTEGER(DebugBreakOnVerificationFailure, W("DebugBreakOnVerificationFailure"), 0) // Halts the jit on
                                                                                         // verification failure
CONFIG_INTEGER(JitDasmWithAddress, W("JitDasmWithAddress"), 0) // Print the process address next to each instruction of
                                                               // the disassembly
CONFIG_INTEGER(DisplayLoopHoistStats, W("JitLoopHoistStats"), 0) // Display JIT loop hoisting statistics
CONFIG_INTEGER(DisplayLsraStats, W("JitLsraStats"), 0)      // Display JIT Linear Scan Register Allocator statistics
                                                            // If set to "1", display the stats in textual format.
                                                            // If set to "2", display the stats in csv format.
                                                            // If set to "3", display the stats in summarize format.
                                                            // Recommended to use with JitStdOutFile flag.
CONFIG_STRING(JitLsraOrdering, W("JitLsraOrdering"))        // LSRA heuristics ordering
CONFIG_INTEGER(EnablePCRelAddr, W("JitEnablePCRelAddr"), 1) // Whether absolute addr be encoded as PC-rel offset by
                                                            // RyuJIT where possible
CONFIG_INTEGER(JitAssertOnMaxRAPasses, W("JitAssertOnMaxRAPasses"), 0)
CONFIG_INTEGER(JitBreakEmitOutputInstr, W("JitBreakEmitOutputInstr"), -1)
CONFIG_INTEGER(JitBreakMorphTree, W("JitBreakMorphTree"), 0xffffffff)
CONFIG_INTEGER(JitBreakOnBadCode, W("JitBreakOnBadCode"), 0)
CONFIG_INTEGER(JitBreakOnMinOpts, W("JITBreakOnMinOpts"), 0) // Halt if jit switches to MinOpts
CONFIG_INTEGER(JitCloneLoops, W("JitCloneLoops"), 1) // If 0, don't clone. Otherwise clone loops for optimizations.
CONFIG_INTEGER(JitCloneLoopsWithGdvTests, W("JitCloneLoopsWithGdvTests"), 1) // If 0, don't clone loops based on
                                                                             // invariant type/method address tests
CONFIG_INTEGER(JitDebugLogLoopCloning, W("JitDebugLogLoopCloning"), 0) // In debug builds log places where loop cloning
                                                                       // optimizations are performed on the fast path.
CONFIG_INTEGER(JitDefaultFill, W("JitDefaultFill"), 0xdd) // In debug builds, initialize the memory allocated by the nra
                                                          // with this byte.
CONFIG_INTEGER(JitAlignLoopMinBlockWeight,
               W("JitAlignLoopMinBlockWeight"),
               DEFAULT_ALIGN_LOOP_MIN_BLOCK_WEIGHT) // Minimum weight needed for the first block of a loop to make it a
                                                    // candidate for alignment.
CONFIG_INTEGER(JitAlignLoopMaxCodeSize,
               W("JitAlignLoopMaxCodeSize"),
               DEFAULT_MAX_LOOPSIZE_FOR_ALIGN) // For non-adaptive alignment, minimum loop size (in bytes) for which
                                               // alignment will be done.
                                               // Defaults to 3 blocks of 32 bytes chunks = 96 bytes.
CONFIG_INTEGER(JitAlignLoopBoundary,
               W("JitAlignLoopBoundary"),
               DEFAULT_ALIGN_LOOP_BOUNDARY) // For non-adaptive alignment, address boundary (power of 2) at which loop
                                            // alignment should be done. By default, 32B.
CONFIG_INTEGER(JitAlignLoopForJcc,
               W("JitAlignLoopForJcc"),
               0) // If set, for non-adaptive alignment, ensure loop jmps are not on or cross alignment boundary.

CONFIG_INTEGER(JitAlignLoopAdaptive,
               W("JitAlignLoopAdaptive"),
               1) // If set, perform adaptive loop alignment that limits number of padding based on loop size.

CONFIG_INTEGER(JitHideAlignBehindJmp,
               W("JitHideAlignBehindJmp"),
               1) // If set, try to hide align instruction (if any) behind an unconditional jump instruction (if any)
                  // that is present before the loop start.

CONFIG_INTEGER(JitOptimizeStructHiddenBuffer, W("JitOptimizeStructHiddenBuffer"), 1) // Track assignments to locals done
                                                                                     // through return buffers.

CONFIG_INTEGER(JitUnrollLoopMaxIterationCount,
               W("JitUnrollLoopMaxIterationCount"),
               DEFAULT_UNROLL_LOOP_MAX_ITERATION_COUNT)

CONFIG_INTEGER(JitDirectAlloc, W("JitDirectAlloc"), 0)
CONFIG_INTEGER(JitDoubleAlign, W("JitDoubleAlign"), 1)
CONFIG_INTEGER(JitDumpASCII, W("JitDumpASCII"), 1)               // Uses only ASCII characters in tree dumps
CONFIG_INTEGER(JitDumpTerseLsra, W("JitDumpTerseLsra"), 1)       // Produce terse dump output for LSRA
CONFIG_INTEGER(JitDumpToDebugger, W("JitDumpToDebugger"), 0)     // Output JitDump output to the debugger
CONFIG_INTEGER(JitDumpVerboseSsa, W("JitDumpVerboseSsa"), 0)     // Produce especially verbose dump output for SSA
CONFIG_INTEGER(JitDumpVerboseTrees, W("JitDumpVerboseTrees"), 0) // Enable more verbose tree dumps
CONFIG_INTEGER(JitDumpTreeIDs, W("JitDumpTreeIDs"), 1)           // Print tree IDs in dumps
CONFIG_INTEGER(JitEmitPrintRefRegs, W("JitEmitPrintRefRegs"), 0)
CONFIG_INTEGER(JitEnableDevirtualization, W("JitEnableDevirtualization"), 1) // Enable devirtualization in importer
CONFIG_INTEGER(JitEnableLateDevirtualization, W("JitEnableLateDevirtualization"), 1) // Enable devirtualization after
                                                                                     // inlining
CONFIG_INTEGER(JitExpensiveDebugCheckLevel, W("JitExpensiveDebugCheckLevel"), 0) // Level indicates how much checking
                                                                                 // beyond the default to do in debug
                                                                                 // builds (currently 1-2)
CONFIG_INTEGER(JitForceFallback, W("JitForceFallback"), 0) // Set to non-zero to test NOWAY assert by forcing a retry
CONFIG_INTEGER(JitFullyInt, W("JitFullyInt"), 0)           // Forces Fully interruptible code
CONFIG_INTEGER(JitFunctionTrace, W("JitFunctionTrace"), 0) // If non-zero, print JIT start/end logging
CONFIG_INTEGER(JitGCChecks, W("JitGCChecks"), 0)
CONFIG_INTEGER(JitGCInfoLogging, W("JitGCInfoLogging"), 0) // If true, prints GCInfo-related output to standard output.
CONFIG_INTEGER(JitHashBreak, W("JitHashBreak"), -1)        // Same as JitBreak, but for a method hash
CONFIG_INTEGER(JitHashDump, W("JitHashDump"), -1)          // Same as JitDump, but for a method hash
CONFIG_INTEGER(JitHashHalt, W("JitHashHalt"), -1)          // Same as JitHalt, but for a method hash
CONFIG_INTEGER(JitInlineAdditionalMultiplier, W("JitInlineAdditionalMultiplier"), 0)
CONFIG_INTEGER(JitInlinePrintStats, W("JitInlinePrintStats"), 0)
CONFIG_INTEGER(JitInlineSize, W("JITInlineSize"), DEFAULT_MAX_INLINE_SIZE)
CONFIG_INTEGER(JitInlineDepth, W("JITInlineDepth"), DEFAULT_MAX_INLINE_DEPTH)
CONFIG_INTEGER(JitForceInlineDepth, W("JITForceInlineDepth"), DEFAULT_MAX_FORCE_INLINE_DEPTH)
CONFIG_INTEGER(JitLongAddress, W("JitLongAddress"), 0) // Force using the large pseudo instruction form for long address
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
CONFIG_INTEGER(JitNoCSE, W("JitNoCSE"), 0)
CONFIG_INTEGER(JitNoCSE2, W("JitNoCSE2"), 0)
CONFIG_INTEGER(JitNoForceFallback, W("JitNoForceFallback"), 0) // Set to non-zero to prevent NOWAY assert testing.
                                                               // Overrides DOTNET_JitForceFallback and JIT stress
                                                               // flags.
CONFIG_INTEGER(JitNoForwardSub, W("JitNoForwardSub"), 0)       // Disables forward sub
CONFIG_INTEGER(JitNoHoist, W("JitNoHoist"), 0)
CONFIG_INTEGER(JitNoInline, W("JitNoInline"), 0)                 // Disables inlining of all methods
CONFIG_INTEGER(JitNoMemoryBarriers, W("JitNoMemoryBarriers"), 0) // If 1, don't generate memory barriers
CONFIG_INTEGER(JitNoRegLoc, W("JitNoRegLoc"), 0)
CONFIG_INTEGER(JitNoStructPromotion, W("JitNoStructPromotion"), 0) // Disables struct promotion 1 - for all, 2 - for
                                                                   // params.
CONFIG_INTEGER(JitNoUnroll, W("JitNoUnroll"), 0)
CONFIG_INTEGER(JitOrder, W("JitOrder"), 0)
CONFIG_INTEGER(JitQueryCurrentStaticFieldClass, W("JitQueryCurrentStaticFieldClass"), 1)
CONFIG_INTEGER(JitReportFastTailCallDecisions, W("JitReportFastTailCallDecisions"), 0)
CONFIG_INTEGER(JitPInvokeCheckEnabled, W("JITPInvokeCheckEnabled"), 0)
CONFIG_INTEGER(JitPInvokeEnabled, W("JITPInvokeEnabled"), 1)

// Controls verbosity for JitPrintInlinedMethods. Ignored for JitDump where
// it's always set.
CONFIG_INTEGER(JitPrintInlinedMethodsVerbose, W("JitPrintInlinedMethodsVerboseLevel"), 0)
// Prints a tree of inlinees for a specific method (use '*' for all methods)
CONFIG_METHODSET(JitPrintInlinedMethods, W("JitPrintInlinedMethods"))

CONFIG_METHODSET(JitPrintDevirtualizedMethods, W("JitPrintDevirtualizedMethods"))
CONFIG_INTEGER(JitProfileChecks, W("JitProfileChecks"), 0) // Bitflag: 0x1 check classic, 0x2 check likely, 0x4 enable
                                                           // asserts
CONFIG_INTEGER(JitRequired, W("JITRequired"), -1)
CONFIG_INTEGER(JitRoundFloat, W("JITRoundFloat"), DEFAULT_ROUND_LEVEL)
CONFIG_INTEGER(JitStackAllocToLocalSize, W("JitStackAllocToLocalSize"), DEFAULT_MAX_LOCALLOC_TO_LOCAL_SIZE)
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
CONFIG_INTEGER(JitStressModeNamesOnly, W("JitStressModeNamesOnly"), 0) // Internal Jit stress: if nonzero, only enable
                                                                       // stress modes listed in JitStressModeNames
CONFIG_INTEGER(JitStressProcedureSplitting, W("JitStressProcedureSplitting"), 0) // Always split after the first basic
                                                                                 // block.
CONFIG_INTEGER(JitStressRegs, W("JitStressRegs"), 0)
CONFIG_STRING(JitStressRegsRange, W("JitStressRegsRange")) // Only apply JitStressRegs to methods in this hash range

CONFIG_INTEGER(JitVNMapSelLimit, W("JitVNMapSelLimit"), 0) // If non-zero, assert if # of VNF_MapSelect applications
                                                           // considered reaches this
CONFIG_INTEGER(RunAltJitCode, W("RunAltJitCode"), 1) // If non-zero, and the compilation succeeds for an AltJit, then
                                                     // use the code. If zero, then we always throw away the generated
                                                     // code and fall back to the default compiler.
CONFIG_INTEGER(RunComponentUnitTests, W("JitComponentUnitTests"), 0) // Run JIT component unit tests
CONFIG_INTEGER(ShouldInjectFault, W("InjectFault"), 0)
CONFIG_INTEGER(StressCOMCall, W("StressCOMCall"), 0)
CONFIG_INTEGER(TailcallStress, W("TailcallStress"), 0)
CONFIG_INTEGER(TreesBeforeAfterMorph, W("JitDumpBeforeAfterMorph"), 0) // If 1, display each tree before/after morphing

CONFIG_METHODSET(JitBreak, W("JitBreak")) // Stops in the importer when compiling a specified method
CONFIG_METHODSET(JitDebugBreak, W("JitDebugBreak"))
CONFIG_STRING(JitDisasmAssemblies, W("JitDisasmAssemblies")) // Only show JitDisasm and related info for methods
                                                             // from this semicolon-delimited list of assemblies.
CONFIG_INTEGER(JitDisasmWithGC, W("JitDisasmWithGC"), 0)     // Dump interleaved GC Info for any method disassembled.
CONFIG_INTEGER(JitDisasmWithDebugInfo, W("JitDisasmWithDebugInfo"), 0) // Dump interleaved debug info for any method
                                                                       // disassembled.
CONFIG_INTEGER(JitDisasmSpilled, W("JitDisasmSpilled"), 0)      // Display native code when any register spilling occurs
CONFIG_METHODSET(JitDump, W("JitDump"))                         // Dumps trees for specified method
CONFIG_INTEGER(JitDumpTier0, W("JitDumpTier0"), 1)              // Dump tier0 jit compilations
CONFIG_INTEGER(JitDumpOSR, W("JitDumpOSR"), 1)                  // Dump OSR jit compilations
CONFIG_INTEGER(JitDumpAtOSROffset, W("JitDumpAtOSROffset"), -1) // Dump only OSR jit compilations with this offset
CONFIG_INTEGER(JitDumpInlinePhases, W("JitDumpInlinePhases"), 1)     // Dump inline compiler phases
CONFIG_INTEGER(JitDumpTerseNextBlock, W("JitDumpTerseNextBlock"), 0) // When dumping blocks, display "*" instead of
                                                                     // block number for lexical "next" blocks, to
                                                                     // reduce clutter.
CONFIG_METHODSET(JitEHDump, W("JitEHDump")) // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(JitExclude, W("JitExclude"))
CONFIG_INTEGER(JitFakeProcedureSplitting, W("JitFakeProcedureSplitting"), 0) // Do code splitting independent of VM.
CONFIG_METHODSET(JitForceProcedureSplitting, W("JitForceProcedureSplitting"))
CONFIG_METHODSET(JitGCDump, W("JitGCDump"))
CONFIG_METHODSET(JitDebugDump, W("JitDebugDump"))
CONFIG_METHODSET(JitHalt, W("JitHalt")) // Emits break instruction into jitted code
CONFIG_METHODSET(JitInclude, W("JitInclude"))
CONFIG_METHODSET(JitLateDisasm, W("JitLateDisasm"))   // Generate late disassembly for the specified methods.
CONFIG_STRING(JitLateDisasmTo, W("JitLateDisasmTo"))  // If set, sends late disassembly output to this file instead of
                                                      // stdout/JitStdOutFile.
CONFIG_METHODSET(JitMinOptsName, W("JITMinOptsName")) // Forces MinOpts for a named function
CONFIG_METHODSET(JitNoProcedureSplitting, W("JitNoProcedureSplitting")) // Disallow procedure splitting for specified
                                                                        // methods
CONFIG_METHODSET(JitNoProcedureSplittingEH, W("JitNoProcedureSplittingEH")) // Disallow procedure splitting for
                                                                            // specified methods if they contain
                                                                            // exception handling
CONFIG_METHODSET(JitStressOnly, W("JitStressOnly")) // Internal Jit stress mode: stress only the specified method(s)
CONFIG_METHODSET(JitUnwindDump, W("JitUnwindDump")) // Dump the unwind codes for the method

CONFIG_METHODSET(JitDumpFg, W("JitDumpFg"))        // Dumps Xml/Dot Flowgraph for specified method
CONFIG_STRING(JitDumpFgDir, W("JitDumpFgDir"))     // Directory for Xml/Dot flowgraph dump(s)
CONFIG_STRING(JitDumpFgFile, W("JitDumpFgFile"))   // Filename for Xml/Dot flowgraph dump(s) (default: "default")
CONFIG_STRING(JitDumpFgPhase, W("JitDumpFgPhase")) // Phase-based Xml/Dot flowgraph support. Set to the short name of a
                                                   // phase to see the flowgraph after that phase. Leave unset to dump
                                                   // after COLD-BLK (determine first cold block) or set to * for all
                                                   // phases
CONFIG_STRING(JitDumpFgPrePhase,
              W("JitDumpFgPrePhase")) // Same as JitDumpFgPhase, but specifies to dump pre-phase, not post-phase.
CONFIG_INTEGER(JitDumpFgDot, W("JitDumpFgDot"), 1)     // 0 == dump XML format; non-zero == dump DOT format
CONFIG_INTEGER(JitDumpFgEH, W("JitDumpFgEH"), 0)       // 0 == no EH regions; non-zero == include EH regions
CONFIG_INTEGER(JitDumpFgLoops, W("JitDumpFgLoops"), 0) // 0 == no loop regions; non-zero == include loop regions

CONFIG_INTEGER(JitDumpFgConstrained, W("JitDumpFgConstrained"), 1) // 0 == don't constrain to mostly linear layout;
                                                                   // non-zero == force mostly lexical block
                                                                   // linear layout
CONFIG_INTEGER(JitDumpFgBlockID, W("JitDumpFgBlockID"), 0) // 0 == display block with bbNum; 1 == display with both
                                                           // bbNum and bbID
CONFIG_INTEGER(JitDumpFgBlockFlags, W("JitDumpFgBlockFlags"), 0) // 0 == don't display block flags; 1 == display flags
CONFIG_INTEGER(JitDumpFgLoopFlags, W("JitDumpFgLoopFlags"), 0)   // 0 == don't display loop flags; 1 == display flags
CONFIG_INTEGER(JitDumpFgBlockOrder, W("JitDumpFgBlockOrder"), 0) // 0 == bbNext order;  1 == bbNum order; 2 == bbID
                                                                 // order
CONFIG_INTEGER(JitDumpFgMemorySsa, W("JitDumpFgMemorySsa"), 0)   // non-zero: show memory phis + SSA/VNs

CONFIG_STRING(JitRange, W("JitRange"))
CONFIG_STRING(JitStressModeNames, W("JitStressModeNames")) // Internal Jit stress mode: stress using the given set of
                                                           // stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL
CONFIG_STRING(JitStressModeNamesNot, W("JitStressModeNamesNot")) // Internal Jit stress mode: do NOT stress using the
                                                                 // given set of stress mode names, e.g. STRESS_REGS,
                                                                 // STRESS_TAILCALL
CONFIG_STRING(JitStressRange, W("JitStressRange"))               // Internal Jit stress mode
CONFIG_METHODSET(JitEmitUnitTests, W("JitEmitUnitTests")) // Generate emitter unit tests in the specified functions
CONFIG_STRING(JitEmitUnitTestsSections, W("JitEmitUnitTestsSections")) // Generate this set of unit tests

///
/// JIT Hardware Intrinsics
///
CONFIG_INTEGER(EnableIncompleteISAClass, W("EnableIncompleteISAClass"), 0) // Enable testing not-yet-implemented
#endif                                                                     // defined(DEBUG)

CONFIG_METHODSET(JitDisasm, W("JitDisasm"))                // Print codegen for given methods
CONFIG_INTEGER(JitDisasmTesting, W("JitDisasmTesting"), 0) // Display BEGIN METHOD/END METHOD anchors for disasm testing
CONFIG_INTEGER(JitDisasmDiffable, W("JitDisasmDiffable"), 0)           // Make the disassembly diff-able
CONFIG_INTEGER(JitDisasmSummary, W("JitDisasmSummary"), 0)             // Prints all jitted methods to the console
CONFIG_INTEGER(JitDisasmOnlyOptimized, W("JitDisasmOnlyOptimized"), 0) // Hides disassembly for unoptimized codegen
CONFIG_INTEGER(JitDisasmWithAlignmentBoundaries, W("JitDisasmWithAlignmentBoundaries"), 0) // Print the alignment
                                                                                           // boundaries.
CONFIG_INTEGER(JitDisasmWithCodeBytes, W("JitDisasmWithCodeBytes"), 0) // Print the instruction code bytes
CONFIG_STRING(JitStdOutFile, W("JitStdOutFile")) // If set, sends JIT's stdout output to this file.

CONFIG_INTEGER(RichDebugInfo, W("RichDebugInfo"), 0) // If 1, keep rich debug info and report it back to the EE

#ifdef DEBUG
CONFIG_STRING(WriteRichDebugInfoFile, W("WriteRichDebugInfoFile")) // Write rich debug info in JSON format to this file
#endif

#if FEATURE_LOOP_ALIGN
CONFIG_INTEGER(JitAlignLoops, W("JitAlignLoops"), 1) // If set, align inner loops
#else
CONFIG_INTEGER(JitAlignLoops, W("JitAlignLoops"), 0)
#endif

///
/// JIT
///
#ifdef FEATURE_ENABLE_NO_RANGE_CHECKS
CONFIG_INTEGER(JitNoRangeChks, W("JitNoRngChks"), 0) // If 1, don't generate range checks
#endif

// AltJitAssertOnNYI should be 0 on targets where JIT is under development or bring up stage, so as to facilitate
// fallback to main JIT on hitting a NYI.
CONFIG_INTEGER(AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 1) // Controls the AltJit behavior of NYI stuff

CONFIG_INTEGER(EnableEHWriteThru, W("EnableEHWriteThru"), 1) // Enable the register allocator to support EH-write thru:
                                                             // partial enregistration of vars exposed on EH boundaries
CONFIG_INTEGER(EnableMultiRegLocals, W("EnableMultiRegLocals"), 1) // Enable the enregistration of locals that are
                                                                   // defined or used in a multireg context.
#if defined(DEBUG)
CONFIG_INTEGER(JitStressEvexEncoding, W("JitStressEvexEncoding"), 0) // Enable EVEX encoding for SIMD instructions when
                                                                     // AVX-512VL is available.
#endif

// clang-format off

CONFIG_INTEGER(PreferredVectorBitWidth,     W("PreferredVectorBitWidth"),   0) // The preferred decimal width, in bits, to use for any implicit vectorization emitted. A value less than 128 is treated as the system default.

//
// Hardware Intrinsic ISAs; keep in sync with clrconfigvalues.h
//
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
//TODO: should implement LoongArch64's features.
//TODO-RISCV64-CQ: should implement RISCV64's features.
CONFIG_INTEGER(EnableHWIntrinsic,           W("EnableHWIntrinsic"),         0) // Allows Base+ hardware intrinsics to be disabled
#else
CONFIG_INTEGER(EnableHWIntrinsic,           W("EnableHWIntrinsic"),         1) // Allows Base+ hardware intrinsics to be disabled
#endif // defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

#if defined(TARGET_AMD64) || defined(TARGET_X86)
CONFIG_INTEGER(EnableAES,                   W("EnableAES"),                 1) // Allows AES+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX,                   W("EnableAVX"),                 1) // Allows AVX+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX2,                  W("EnableAVX2"),                1) // Allows AVX2+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512BW,              W("EnableAVX512BW"),            1) // Allows AVX512BW+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512BW_VL,           W("EnableAVX512BW_VL"),         1) // Allows AVX512BW+ AVX512VL+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512CD,              W("EnableAVX512CD"),            1) // Allows AVX512CD+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512CD_VL,           W("EnableAVX512CD_VL"),         1) // Allows AVX512CD+ AVX512VL+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512DQ,              W("EnableAVX512DQ"),            1) // Allows AVX512DQ+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512DQ_VL,           W("EnableAVX512DQ_VL"),         1) // Allows AVX512DQ+ AVX512VL+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512F,               W("EnableAVX512F"),             1) // Allows AVX512F+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512F_VL,            W("EnableAVX512F_VL"),          1) // Allows AVX512F+ AVX512VL+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512VBMI,            W("EnableAVX512VBMI"),          1) // Allows AVX512VBMI+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVX512VBMI_VL,         W("EnableAVX512VBMI_VL"),       1) // Allows AVX512VBMI_VL+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableAVXVNNI,               W("EnableAVXVNNI"),             1) // Allows AVXVNNI+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableBMI1,                  W("EnableBMI1"),                1) // Allows BMI1+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableBMI2,                  W("EnableBMI2"),                1) // Allows BMI2+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableFMA,                   W("EnableFMA"),                 1) // Allows FMA+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableLZCNT,                 W("EnableLZCNT"),               1) // Allows LZCNT+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnablePCLMULQDQ,             W("EnablePCLMULQDQ"),           1) // Allows PCLMULQDQ+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnablePOPCNT,                W("EnablePOPCNT"),              1) // Allows POPCNT+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableSSE,                   W("EnableSSE"),                 1) // Allows SSE+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableSSE2,                  W("EnableSSE2"),                1) // Allows SSE2+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableSSE3,                  W("EnableSSE3"),                1) // Allows SSE3+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableSSE3_4,                W("EnableSSE3_4"),              1) // Allows SSE3+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableSSE41,                 W("EnableSSE41"),               1) // Allows SSE4.1+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableSSE42,                 W("EnableSSE42"),               1) // Allows SSE4.2+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableSSSE3,                 W("EnableSSSE3"),               1) // Allows SSSE3+ hardware intrinsics to be disabled
#elif defined(TARGET_ARM64)
CONFIG_INTEGER(EnableArm64AdvSimd,          W("EnableArm64AdvSimd"),        1) // Allows Arm64 AdvSimd+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Aes,              W("EnableArm64Aes"),            1) // Allows Arm64 Aes+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Atomics,          W("EnableArm64Atomics"),        1) // Allows Arm64 Atomics+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Crc32,            W("EnableArm64Crc32"),          1) // Allows Arm64 Crc32+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Dczva,            W("EnableArm64Dczva"),          1) // Allows Arm64 Dczva+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Dp,               W("EnableArm64Dp"),             1) // Allows Arm64 Dp+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Rdm,              W("EnableArm64Rdm"),            1) // Allows Arm64 Rdm+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Sha1,             W("EnableArm64Sha1"),           1) // Allows Arm64 Sha1+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Sha256,           W("EnableArm64Sha256"),         1) // Allows Arm64 Sha256+ hardware intrinsics to be disabled
CONFIG_INTEGER(EnableArm64Sve,              W("EnableArm64Sve"),            1) // Allows Arm64 Sve+ hardware intrinsics to be disabled
#endif

// clang-format on

#ifdef FEATURE_SIMD
CONFIG_INTEGER(JitDisableSimdVN, W("JitDisableSimdVN"), 0) // Default 0, ValueNumbering of SIMD nodes and HW Intrinsic
                                                           // nodes enabled
                                                           // If 1, then disable ValueNumbering of SIMD nodes
                                                           // If 2, then disable ValueNumbering of HW Intrinsic nodes
                                                           // If 3, disable both SIMD and HW Intrinsic nodes
#endif                                                     // FEATURE_SIMD

// Default 0, enable the CSE of Constants, including nearby offsets. (only for ARM64)
// If 1, disable all the CSE of Constants
// If 2, enable the CSE of Constants but don't combine with nearby offsets. (only for ARM64)
// If 3, enable the CSE of Constants including nearby offsets. (all platforms)
// If 4, enable the CSE of Constants but don't combine with nearby offsets. (all platforms)
//
CONFIG_INTEGER(JitConstCSE, W("JitConstCSE"), 0)

#define CONST_CSE_ENABLE_ARM 0
#define CONST_CSE_DISABLE_ALL 1
#define CONST_CSE_ENABLE_ARM_NO_SHARING 2
#define CONST_CSE_ENABLE_ALL 3
#define CONST_CSE_ENABLE_ALL_NO_SHARING 4

#if defined(DEBUG)
// Allow fine-grained controls of CSEs done in a particular method
//
// Specify method that will respond to the CSEMask.
// 0 means feature disabled and all methods run CSE normally.
CONFIG_INTEGER(JitCSEHash, W("JitCSEHash"), 0)

// Bitmask of allowed CSEs in methods specified by JitCSEHash.
// These bits control the "cse attempts" made by normal jitting,
// for the first 32 CSEs attempted (Note this is not the same as
// the CSE candidate number, which reflects the order
// in which CSEs were discovered).
//
// 0: do no CSEs
// 1: do only the first CSE
// 2: do only the second CSE
// C: do only the third and fourth CSEs
// F: do only the first four CSEs
// ...etc...
// FFFFFFFF : do all the CSEs normally done
CONFIG_INTEGER(JitCSEMask, W("JitCSEMask"), 0)

// Enable metric output in jit disasm & elsewhere
CONFIG_INTEGER(JitMetrics, W("JitMetrics"), 0)

// When nonzero, choose CSE candidates randomly, with probability
// specified by the (decimal) value of the config
CONFIG_INTEGER(JitRandomCSE, W("JitRandomCSE"), 0)

#endif

///
/// JIT
///
#if !defined(DEBUG) && !defined(_DEBUG)
CONFIG_INTEGER(JitEnableNoWayAssert, W("JitEnableNoWayAssert"), 0)
#else  // defined(DEBUG) || defined(_DEBUG)
CONFIG_INTEGER(JitEnableNoWayAssert, W("JitEnableNoWayAssert"), 1)
#endif // !defined(DEBUG) && !defined(_DEBUG)

#if defined(TARGET_AMD64) || defined(TARGET_X86)
#define JitMinOptsTrackGCrefs_Default 0 // Not tracking GC refs in MinOpts is new behavior
#else
#define JitMinOptsTrackGCrefs_Default 1
#endif
CONFIG_INTEGER(JitMinOptsTrackGCrefs, W("JitMinOptsTrackGCrefs"), JitMinOptsTrackGCrefs_Default) // Track GC roots

// The following should be wrapped inside "#if MEASURE_MEM_ALLOC / #endif", but
// some files include this one without bringing in the definitions from "jit.h"
// so we don't always know what the "true" value of that flag should be. For now
// we take the easy way out and always include the flag, even in release builds
// (normally MEASURE_MEM_ALLOC is off for release builds but if it's toggled on
// for release in "jit.h" the flag would be missing for some includers).
// TODO-Cleanup: need to make 'MEASURE_MEM_ALLOC' well-defined here at all times.
CONFIG_INTEGER(DisplayMemStats, W("JitMemStats"), 0) // Display JIT memory usage statistics

#if defined(DEBUG)
CONFIG_INTEGER(JitEnregStats, W("JitEnregStats"), 0) // Display JIT enregistration statistics
#endif                                               // DEBUG

CONFIG_INTEGER(JitAggressiveInlining, W("JitAggressiveInlining"), 0) // Aggressive inlining of all methods
CONFIG_INTEGER(JitELTHookEnabled, W("JitELTHookEnabled"), 0)         // If 1, emit Enter/Leave/TailCall callbacks
CONFIG_INTEGER(JitInlineSIMDMultiplier, W("JitInlineSIMDMultiplier"), 3)

// Ex lclMAX_TRACKED constant.
CONFIG_INTEGER(JitMaxLocalsToTrack, W("JitMaxLocalsToTrack"), 0x400)

#if defined(FEATURE_ENABLE_NO_RANGE_CHECKS)
CONFIG_INTEGER(JitNoRngChks, W("JitNoRngChks"), 0) // If 1, don't generate range checks
#endif                                             // defined(FEATURE_ENABLE_NO_RANGE_CHECKS)

#if defined(OPT_CONFIG)
CONFIG_INTEGER(JitDoAssertionProp, W("JitDoAssertionProp"), 1) // Perform assertion propagation optimization
CONFIG_INTEGER(JitDoCopyProp, W("JitDoCopyProp"), 1)   // Perform copy propagation on variables that appear redundant
CONFIG_INTEGER(JitDoEarlyProp, W("JitDoEarlyProp"), 1) // Perform Early Value Propagation
CONFIG_INTEGER(JitDoLoopHoisting, W("JitDoLoopHoisting"), 1)   // Perform loop hoisting on loop invariant values
CONFIG_INTEGER(JitDoLoopInversion, W("JitDoLoopInversion"), 1) // Perform loop inversion on "for/while" loops
CONFIG_INTEGER(JitDoRangeAnalysis, W("JitDoRangeAnalysis"), 1) // Perform range check analysis
CONFIG_INTEGER(JitDoVNBasedDeadStoreRemoval, W("JitDoVNBasedDeadStoreRemoval"), 1) // Perform VN-based dead store
                                                                                   // removal
CONFIG_INTEGER(JitDoRedundantBranchOpts, W("JitDoRedundantBranchOpts"), 1) // Perform redundant branch optimizations
CONFIG_STRING(JitEnableRboRange, W("JitEnableRboRange"))
CONFIG_STRING(JitEnableHeadTailMergeRange, W("JitEnableHeadTailMergeRange"))
CONFIG_STRING(JitEnableVNBasedDeadStoreRemovalRange, W("JitEnableVNBasedDeadStoreRemovalRange"))
CONFIG_STRING(JitEnableEarlyLivenessRange, W("JitEnableEarlyLivenessRange"))
CONFIG_STRING(JitOnlyOptimizeRange,
              W("JitOnlyOptimizeRange")) // If set, all methods that do _not_ match are forced into MinOpts
CONFIG_STRING(JitEnablePhysicalPromotionRange, W("JitEnablePhysicalPromotionRange"))
CONFIG_STRING(JitEnableCrossBlockLocalAssertionPropRange, W("JitEnableCrossBlockLocalAssertionPropRange"))

CONFIG_INTEGER(JitDoSsa, W("JitDoSsa"), 1) // Perform Static Single Assignment (SSA) numbering on the variables
CONFIG_INTEGER(JitDoValueNumber, W("JitDoValueNumber"), 1) // Perform value numbering on method expressions

CONFIG_METHODSET(JitOptRepeat, W("JitOptRepeat"))            // Runs optimizer multiple times on the method
CONFIG_INTEGER(JitOptRepeatCount, W("JitOptRepeatCount"), 2) // Number of times to repeat opts when repeating
CONFIG_INTEGER(JitDoIfConversion, W("JitDoIfConversion"), 1) // Perform If conversion
#endif                                                       // defined(OPT_CONFIG)

// Max # of MapSelect's considered for a particular top-level invocation.
CONFIG_INTEGER(JitVNMapSelBudget, W("JitVNMapSelBudget"), DEFAULT_MAP_SELECT_BUDGET)

CONFIG_INTEGER(TailCallLoopOpt, W("TailCallLoopOpt"), 1) // Convert recursive tail calls to loops
CONFIG_METHODSET(AltJit, W("AltJit"))         // Enables AltJit and selectively limits it to the specified methods.
CONFIG_METHODSET(AltJitNgen, W("AltJitNgen")) // Enables AltJit for NGEN and selectively limits it
                                              // to the specified methods.

CONFIG_STRING(AltJitExcludeAssemblies, W("AltJitExcludeAssemblies")) // Do not use AltJit on this
                                                                     // semicolon-delimited list of assemblies.

CONFIG_INTEGER(JitMeasureIR, W("JitMeasureIR"), 0) // If set, measure the IR size after some phases and report it in
                                                   // the time log.

CONFIG_STRING(JitFuncInfoFile, W("JitFuncInfoLogFile")) // If set, gather JIT function info and write to this file.
CONFIG_STRING(JitTimeLogCsv, W("JitTimeLogCsv")) // If set, gather JIT throughput data and write to a CSV file. This
                                                 // mode must be used in internal retail builds.
CONFIG_STRING(TailCallOpt, W("TailCallOpt"))
CONFIG_INTEGER(FastTailCalls, W("FastTailCalls"), 1) // If set, allow fast tail calls; otherwise allow only helper-based
                                                     // calls
                                                     // for explicit tail calls.

CONFIG_INTEGER(JitMeasureNowayAssert, W("JitMeasureNowayAssert"), 0) // Set to 1 to measure noway_assert usage. Only
                                                                     // valid if MEASURE_NOWAY is defined.
CONFIG_STRING(JitMeasureNowayAssertFile,
              W("JitMeasureNowayAssertFile")) // Set to file to write noway_assert usage to a file (if not
                                              // set: stdout). Only valid if MEASURE_NOWAY is defined.
#if defined(DEBUG)
CONFIG_INTEGER(EnableExtraSuperPmiQueries, W("EnableExtraSuperPmiQueries"), 0) // Make extra queries to somewhat
                                                                               // future-proof SuperPmi method contexts.
#endif                                                                         // DEBUG

#if defined(DEBUG)
CONFIG_INTEGER(JitInlineDumpData, W("JitInlineDumpData"), 0)
CONFIG_INTEGER(JitInlineDumpXml, W("JitInlineDumpXml"), 0) // 1 = full xml (+ failures in DEBUG)
                                                           // 2 = only methods with inlines (+ failures in DEBUG)
                                                           // 3 = only methods with inlines, no failures
CONFIG_STRING(JitInlineDumpXmlFile, W("JitInlineDumpXmlFile"))
CONFIG_INTEGER(JitInlinePolicyDumpXml, W("JitInlinePolicyDumpXml"), 0)
CONFIG_INTEGER(JitInlineLimit, W("JitInlineLimit"), -1)
CONFIG_INTEGER(JitInlinePolicyDiscretionary, W("JitInlinePolicyDiscretionary"), 0)
CONFIG_INTEGER(JitInlinePolicyFull, W("JitInlinePolicyFull"), 0)
CONFIG_INTEGER(JitInlinePolicySize, W("JitInlinePolicySize"), 0)
CONFIG_INTEGER(JitInlinePolicyRandom, W("JitInlinePolicyRandom"), 0) // nonzero enables; value is the external random
                                                                     // seed
CONFIG_INTEGER(JitInlinePolicyReplay, W("JitInlinePolicyReplay"), 0)
CONFIG_STRING(JitNoInlineRange, W("JitNoInlineRange"))
CONFIG_STRING(JitInlineReplayFile, W("JitInlineReplayFile"))
#endif // defined(DEBUG)

// Extended version of DefaultPolicy that includes a more precise IL scan,
// relies on PGO if it exists and generally is more aggressive.
CONFIG_INTEGER(JitExtDefaultPolicy, W("JitExtDefaultPolicy"), 1)
CONFIG_INTEGER(JitExtDefaultPolicyMaxIL, W("JitExtDefaultPolicyMaxIL"), 0x80)
CONFIG_INTEGER(JitExtDefaultPolicyMaxILProf, W("JitExtDefaultPolicyMaxILProf"), 0x400)
CONFIG_INTEGER(JitExtDefaultPolicyMaxBB, W("JitExtDefaultPolicyMaxBB"), 7)

// Inliner uses the following formula for PGO-driven decisions:
//
//    BM = BM * ((1.0 - ProfTrust) + ProfWeight * ProfScale)
//
// Where BM is a benefit multiplier composed from various observations (e.g. "const arg makes a branch foldable").
// If a profile data can be trusted for 100% we can safely just give up on inlining anything inside cold blocks
// (except the cases where inlining in cold blocks improves type info/escape analysis for the whole caller).
// For now, it's only applied for dynamic PGO.
CONFIG_INTEGER(JitExtDefaultPolicyProfTrust, W("JitExtDefaultPolicyProfTrust"), 0x7)
CONFIG_INTEGER(JitExtDefaultPolicyProfScale, W("JitExtDefaultPolicyProfScale"), 0x2A)

CONFIG_INTEGER(JitInlinePolicyModel, W("JitInlinePolicyModel"), 0)
CONFIG_INTEGER(JitInlinePolicyProfile, W("JitInlinePolicyProfile"), 0)
CONFIG_INTEGER(JitInlinePolicyProfileThreshold, W("JitInlinePolicyProfileThreshold"), 40)
CONFIG_INTEGER(JitObjectStackAllocation, W("JitObjectStackAllocation"), 0)

CONFIG_INTEGER(JitEECallTimingInfo, W("JitEECallTimingInfo"), 0)

#if defined(DEBUG)
CONFIG_INTEGER(JitEnableFinallyCloning, W("JitEnableFinallyCloning"), 1)
CONFIG_INTEGER(JitEnableRemoveEmptyTry, W("JitEnableRemoveEmptyTry"), 1)
#endif // DEBUG

// Overall master enable for Guarded Devirtualization.
CONFIG_INTEGER(JitEnableGuardedDevirtualization, W("JitEnableGuardedDevirtualization"), 1)

#define MAX_GDV_TYPE_CHECKS 5
// Number of types to probe for polymorphic virtual call-sites to devirtualize them,
// Max number is MAX_GDV_TYPE_CHECKS defined above ^. -1 means it's up to JIT to decide
CONFIG_INTEGER(JitGuardedDevirtualizationMaxTypeChecks, W("JitGuardedDevirtualizationMaxTypeChecks"), -1)

// Various policies for GuardedDevirtualization
CONFIG_INTEGER(JitGuardedDevirtualizationChainLikelihood, W("JitGuardedDevirtualizationChainLikelihood"), 0x4B) // 75
CONFIG_INTEGER(JitGuardedDevirtualizationChainStatements, W("JitGuardedDevirtualizationChainStatements"), 1)
#if defined(DEBUG)
CONFIG_STRING(JitGuardedDevirtualizationRange, W("JitGuardedDevirtualizationRange"))
CONFIG_INTEGER(JitRandomGuardedDevirtualization, W("JitRandomGuardedDevirtualization"), 0)
#endif // DEBUG

// Enable insertion of patchpoints into Tier0 methods, switching to optimized where needed.
#if defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
CONFIG_INTEGER(TC_OnStackReplacement, W("TC_OnStackReplacement"), 1)
#else
CONFIG_INTEGER(TC_OnStackReplacement, W("TC_OnStackReplacement"), 0)
#endif // defined(TARGET_AMD64) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
// Initial patchpoint counter value used by jitted code
CONFIG_INTEGER(TC_OnStackReplacement_InitialCounter, W("TC_OnStackReplacement_InitialCounter"), 1000)
// Enable partial compilation for Tier0 methods
CONFIG_INTEGER(TC_PartialCompilation, W("TC_PartialCompilation"), 0)
// Patchpoint strategy:
// 0 - backedge sources
// 1 - backedge targets
// 2 - adaptive (default)
CONFIG_INTEGER(TC_PatchpointStrategy, W("TC_PatchpointStrategy"), 2)
#if defined(DEBUG)
// Randomly sprinkle patchpoints. Value is the likelihood any given stack-empty point becomes a patchpoint.
CONFIG_INTEGER(JitRandomOnStackReplacement, W("JitRandomOnStackReplacement"), 0)
// Place patchpoint at the specified IL offset, if possible. Overrides random placement.
CONFIG_INTEGER(JitOffsetOnStackReplacement, W("JitOffsetOnStackReplacement"), -1)
#endif // debug

#if defined(DEBUG)
// EnableOsrRange allows you to limit the set of methods that will rely on OSR to escape
// from Tier0 code. Methods outside the range that would normally be jitted at Tier0
// and have patchpoints will instead be switched to optimized.
CONFIG_STRING(JitEnableOsrRange, W("JitEnableOsrRange"))
// EnablePatchpointRange allows you to limit the set of Tier0 methods that
// will have patchpoints, and hence control which methods will create OSR methods.
// Unlike EnableOsrRange, it will not alter the optimization setting for methods
// outside the enabled range.
CONFIG_STRING(JitEnablePatchpointRange, W("JitEnablePatchpointRange"))
#endif

// Profile instrumentation options
CONFIG_INTEGER(JitInterlockedProfiling, W("JitInterlockedProfiling"), 0)
CONFIG_INTEGER(JitScalableProfiling, W("JitScalableProfiling"), 1)
CONFIG_INTEGER(JitCounterPadding, W("JitCounterPadding"), 0) // number of unused extra slots per counter
CONFIG_INTEGER(JitMinimalJitProfiling, W("JitMinimalJitProfiling"), 1)
CONFIG_INTEGER(JitMinimalPrejitProfiling, W("JitMinimalPrejitProfiling"), 0)

CONFIG_INTEGER(JitProfileValues, W("JitProfileValues"), 1) // Value profiling, e.g. Buffer.Memmove's size
CONFIG_INTEGER(JitProfileCasts, W("JitProfileCasts"), 1)   // Profile castclass/isinst
CONFIG_INTEGER(JitConsumeProfileForCasts, W("JitConsumeProfileForCasts"), 1) // Consume profile data (if any) for
                                                                             // castclass/isinst

CONFIG_INTEGER(JitClassProfiling, W("JitClassProfiling"), 1)         // Profile virtual and interface calls
CONFIG_INTEGER(JitDelegateProfiling, W("JitDelegateProfiling"), 1)   // Profile resolved delegate call targets
CONFIG_INTEGER(JitVTableProfiling, W("JitVTableProfiling"), 0)       // Profile resolved vtable call targets
CONFIG_INTEGER(JitEdgeProfiling, W("JitEdgeProfiling"), 1)           // Profile edges instead of blocks
CONFIG_INTEGER(JitCollect64BitCounts, W("JitCollect64BitCounts"), 0) // Collect counts as 64-bit values.

// Profile consumption options
CONFIG_INTEGER(JitDisablePGO, W("JitDisablePGO"), 0) // Ignore PGO data for all methods
#if defined(DEBUG)
CONFIG_STRING(JitEnablePGORange, W("JitEnablePGORange"))         // Enable PGO data for only some methods
CONFIG_INTEGER(JitRandomEdgeCounts, W("JitRandomEdgeCounts"), 0) // Substitute random values for edge counts
CONFIG_INTEGER(JitCrossCheckDevirtualizationAndPGO, W("JitCrossCheckDevirtualizationAndPGO"), 0)
CONFIG_INTEGER(JitNoteFailedExactDevirtualization, W("JitNoteFailedExactDevirtualization"), 0)
CONFIG_INTEGER(JitRandomlyCollect64BitCounts, W("JitRandomlyCollect64BitCounts"), 0) // Collect 64-bit counts randomly
                                                                                     // for some methods.
// 1: profile synthesis for root methods
// 2: profile synthesis for root methods w/o PGO data
// 3: profile synthesis for root methods, blend with existing PGO data
CONFIG_INTEGER(JitSynthesizeCounts, W("JitSynthesizeCounts"), 0)
// Check if synthesis left consistent counts
CONFIG_INTEGER(JitCheckSynthesizedCounts, W("JitCheckSynthesizedCounts"), 0)
// If instrumenting the method, run synthesis and save the synthesis results
// as edge or block profile data. Do not actually instrument.
CONFIG_INTEGER(JitPropagateSynthesizedCountsToProfileData, W("JitPropagateSynthesizedCountsToProfileData"), 0)
#endif

// Devirtualize virtual calls with getExactClasses (NativeAOT only for now)
CONFIG_INTEGER(JitEnableExactDevirtualization, W("JitEnableExactDevirtualization"), 1)

// Force the generation of CFG checks
CONFIG_INTEGER(JitForceControlFlowGuard, W("JitForceControlFlowGuard"), 0);
// JitCFGUseDispatcher values:
// 0: Never use dispatcher
// 1: Use dispatcher on all platforms that support it
// 2: Default behavior, depends on platform (yes on x64, no on arm64)
CONFIG_INTEGER(JitCFGUseDispatcher, W("JitCFGUseDispatcher"), 2)

// Enable head and tail merging
CONFIG_INTEGER(JitEnableHeadTailMerge, W("JitEnableHeadTailMerge"), 1)

// Enable physical promotion
CONFIG_INTEGER(JitEnablePhysicalPromotion, W("JitEnablePhysicalPromotion"), 1)

// Enable cross-block local assertion prop
CONFIG_INTEGER(JitEnableCrossBlockLocalAssertionProp, W("JitEnableCrossBlockLocalAssertionProp"), 1)

#if defined(DEBUG)
// JitFunctionFile: Name of a file that contains a list of functions. If the currently compiled function is in the
// file, certain other JIT config variables will be active. If the currently compiled function is not in the file,
// the specific JIT config variables will not be active.
//
// Functions are approximately in the format output by JitFunctionTrace, e.g.:
//
// System.CLRConfig:GetBoolValue(ref,byref):bool (MethodHash=3c54d35e)
//   -- use the MethodHash, not the function name
//
// System.CLRConfig:GetBoolValue(ref,byref):bool
//   -- use just the name
//
// Lines with leading ";" "#" or "//" are ignored.
//
// If this is unset, then the JIT config values have their normal behavior.
//
CONFIG_STRING(JitFunctionFile, W("JitFunctionFile"))
#endif // DEBUG

#if defined(DEBUG)
CONFIG_METHODSET(JitRawHexCode, W("JitRawHexCode"))
CONFIG_STRING(JitRawHexCodeFile, W("JitRawHexCodeFile"))
#endif // DEBUG

#if defined(DEBUG)
#if defined(TARGET_ARM64)
// JitSaveFpLrWithCalleeSavedRegisters:
//    0: use default frame type decision
//    1: disable frames that save FP/LR registers with the callee-saved registers (at the top of the frame)
//    2: force all frames to use the frame types that save FP/LR registers with the callee-saved registers (at the top
//    of the frame)
//    3: force all frames to use the frame types that save FP/LR registers with the callee-saved registers (at the top
//    of the frame) and also force using the large funclet frame variation (frame 5) if possible.
CONFIG_INTEGER(JitSaveFpLrWithCalleeSavedRegisters, W("JitSaveFpLrWithCalleeSavedRegisters"), 0)
#endif // defined(TARGET_ARM64)

#if defined(TARGET_LOONGARCH64)
// Disable emitDispIns by default
CONFIG_INTEGER(JitDispIns, W("JitDispIns"), 0)
#endif // defined(TARGET_LOONGARCH64)
#endif // DEBUG

CONFIG_INTEGER(JitEnregStructLocals, W("JitEnregStructLocals"), 1) // Allow to enregister locals with struct type.

#undef CONFIG_INTEGER
#undef CONFIG_STRING
#undef CONFIG_METHODSET
