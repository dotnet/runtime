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
                                                               // COMPlus_ContinueOnAssert=1
CONFIG_INTEGER(BreakOnDumpToken, W("BreakOnDumpToken"), 0xffffffff) // Breaks when using internal logging on a
                                                                    // particular token value.
CONFIG_INTEGER(DebugBreakOnVerificationFailure, W("DebugBreakOnVerificationFailure"), 0) // Halts the jit on
                                                                                         // verification failure
CONFIG_INTEGER(DiffableDasm, W("JitDiffableDasm"), 0)          // Make the disassembly diff-able
CONFIG_INTEGER(JitDasmWithAddress, W("JitDasmWithAddress"), 0) // Print the process address next to each instruction of
                                                               // the disassembly
CONFIG_INTEGER(DisplayLoopHoistStats, W("JitLoopHoistStats"), 0) // Display JIT loop hoisting statistics
CONFIG_INTEGER(DisplayLsraStats, W("JitLsraStats"), 0) // Display JIT Linear Scan Register Allocator statistics
                                                       // if set to 1. If set to "2", display the stats in csv format.
                                                       // Recommended to use with JitStdOutFile flag.
CONFIG_STRING(JitLsraOrdering, W("JitLsraOrdering"))   // LSRA heuristics ordering
CONFIG_INTEGER(DumpJittedMethods, W("DumpJittedMethods"), 0) // Prints all jitted methods to the console
CONFIG_INTEGER(EnablePCRelAddr, W("JitEnablePCRelAddr"), 1)  // Whether absolute addr be encoded as PC-rel offset by
                                                             // RyuJIT where possible
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

CONFIG_INTEGER(JitDirectAlloc, W("JitDirectAlloc"), 0)
CONFIG_INTEGER(JitDoubleAlign, W("JitDoubleAlign"), 1)
CONFIG_INTEGER(JitDumpASCII, W("JitDumpASCII"), 1)               // Uses only ASCII characters in tree dumps
CONFIG_INTEGER(JitDumpTerseLsra, W("JitDumpTerseLsra"), 1)       // Produce terse dump output for LSRA
CONFIG_INTEGER(JitDumpToDebugger, W("JitDumpToDebugger"), 0)     // Output JitDump output to the debugger
CONFIG_INTEGER(JitDumpVerboseSsa, W("JitDumpVerboseSsa"), 0)     // Produce especially verbose dump output for SSA
CONFIG_INTEGER(JitDumpVerboseTrees, W("JitDumpVerboseTrees"), 0) // Enable more verbose tree dumps
CONFIG_INTEGER(JitEmitPrintRefRegs, W("JitEmitPrintRefRegs"), 0)
CONFIG_INTEGER(JitEnableDevirtualization, W("JitEnableDevirtualization"), 1) // Enable devirtualization in importer
CONFIG_INTEGER(JitEnableLateDevirtualization, W("JitEnableLateDevirtualization"), 1) // Enable devirtualization after
                                                                                     // inlining
CONFIG_INTEGER(JitExpensiveDebugCheckLevel, W("JitExpensiveDebugCheckLevel"), 0) // Level indicates how much checking
                                                                                 // beyond the default to do in debug
                                                                                 // builds (currently 1-2)
CONFIG_INTEGER(JitForceFallback, W("JitForceFallback"), 0) // Set to non-zero to test NOWAY assert by forcing a retry
CONFIG_INTEGER(JitForceVer, W("JitForceVer"), 0)
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
CONFIG_INTEGER(JitQueryCurrentStaticFieldClass, W("JitQueryCurrentStaticFieldClass"), 1)
CONFIG_INTEGER(JitReportFastTailCallDecisions, W("JitReportFastTailCallDecisions"), 0)
CONFIG_INTEGER(JitPInvokeCheckEnabled, W("JITPInvokeCheckEnabled"), 0)
CONFIG_INTEGER(JitPInvokeEnabled, W("JITPInvokeEnabled"), 1)
CONFIG_METHODSET(JitPrintInlinedMethods, W("JitPrintInlinedMethods"))
CONFIG_METHODSET(JitPrintDevirtualizedMethods, W("JitPrintDevirtualizedMethods"))
CONFIG_INTEGER(JitProfileChecks, W("JitProfileChecks"), 0) // 1 enable in dumps, 2 assert if issues found
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
CONFIG_STRING(JitStdOutFile, W("JitStdOutFile")) // If set, sends JIT's stdout output to this file.
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
CONFIG_INTEGER(JitVNMapSelLimit, W("JitVNMapSelLimit"), 0) // If non-zero, assert if # of VNF_MapSelect applications
                                                           // considered reaches this
CONFIG_INTEGER(NgenHashDump, W("NgenHashDump"), -1)        // same as JitHashDump, but for ngen
CONFIG_INTEGER(NgenOrder, W("NgenOrder"), 0)
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
CONFIG_METHODSET(JitDisasm, W("JitDisasm"))                  // Dumps disassembly for specified method
CONFIG_STRING(JitDisasmAssemblies, W("JitDisasmAssemblies")) // Only show JitDisasm and related info for methods
                                                             // from this semicolon-delimited list of assemblies.
CONFIG_INTEGER(JitDisasmWithGC, W("JitDisasmWithGC"), 0)     // Dump interleaved GC Info for any method disassembled.
CONFIG_METHODSET(JitDump, W("JitDump"))                      // Dumps trees for specified method
CONFIG_METHODSET(JitEHDump, W("JitEHDump"))                  // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(JitExclude, W("JitExclude"))
CONFIG_METHODSET(JitForceProcedureSplitting, W("JitForceProcedureSplitting"))
CONFIG_METHODSET(JitGCDump, W("JitGCDump"))
CONFIG_METHODSET(JitDebugDump, W("JitDebugDump"))
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
///
/// NGEN
///
CONFIG_METHODSET(NgenDisasm, W("NgenDisasm")) // Same as JitDisasm, but for ngen
CONFIG_METHODSET(NgenDump, W("NgenDump"))     // Same as JitDump, but for ngen
CONFIG_METHODSET(NgenEHDump, W("NgenEHDump")) // Dump the EH table for the method, as reported to the VM
CONFIG_METHODSET(NgenGCDump, W("NgenGCDump"))
CONFIG_METHODSET(NgenDebugDump, W("NgenDebugDump"))
CONFIG_METHODSET(NgenUnwindDump, W("NgenUnwindDump")) // Dump the unwind codes for the method
///
/// JIT
///
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

CONFIG_STRING(JitLateDisasmTo, W("JITLateDisasmTo"))
CONFIG_STRING(JitRange, W("JitRange"))
CONFIG_STRING(JitStressModeNames, W("JitStressModeNames")) // Internal Jit stress mode: stress using the given set of
                                                           // stress mode names, e.g. STRESS_REGS, STRESS_TAILCALL
CONFIG_STRING(JitStressModeNamesNot, W("JitStressModeNamesNot")) // Internal Jit stress mode: do NOT stress using the
                                                                 // given set of stress mode names, e.g. STRESS_REGS,
                                                                 // STRESS_TAILCALL
CONFIG_STRING(JitStressRange, W("JitStressRange"))               // Internal Jit stress mode
///
/// NGEN
///
CONFIG_METHODSET(NgenDumpFg, W("NgenDumpFg"))      // Ngen Xml/Dot flowgraph dump support
CONFIG_STRING(NgenDumpFgDir, W("NgenDumpFgDir"))   // Ngen Xml/Dot flowgraph dump support
CONFIG_STRING(NgenDumpFgFile, W("NgenDumpFgFile")) // Ngen Xml/Dot flowgraph dump support
///
/// JIT Hardware Intrinsics
///
CONFIG_INTEGER(EnableIncompleteISAClass, W("EnableIncompleteISAClass"), 0) // Enable testing not-yet-implemented
                                                                           // intrinsic classes

#endif // defined(DEBUG)

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
#if defined(TARGET_ARM64) || defined(TARGET_X86)
CONFIG_INTEGER(AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 0) // Controls the AltJit behavior of NYI stuff
#else                                                        // !defined(TARGET_ARM64) && !defined(TARGET_X86)
CONFIG_INTEGER(AltJitAssertOnNYI, W("AltJitAssertOnNYI"), 1) // Controls the AltJit behavior of NYI stuff
#endif                                                       // defined(TARGET_ARM64) || defined(TARGET_X86)
///
/// JIT Hardware Intrinsics
///
#if defined(TARGET_X86) || defined(TARGET_AMD64)
CONFIG_INTEGER(EnableSSE3_4, W("EnableSSE3_4"), 1) // Enable SSE3, SSSE3, SSE 4.1 and 4.2 instruction set as default
#endif

#if defined(TARGET_AMD64) || defined(TARGET_X86)
// Enable AVX instruction set for wide operations as default. When both AVX and SSE3_4 are set, we will use the most
// capable instruction set available which will prefer AVX over SSE3/4.
CONFIG_INTEGER(EnableHWIntrinsic, W("EnableHWIntrinsic"), 1) // Enable Base
CONFIG_INTEGER(EnableSSE, W("EnableSSE"), 1)                 // Enable SSE
CONFIG_INTEGER(EnableSSE2, W("EnableSSE2"), 1)               // Enable SSE2
CONFIG_INTEGER(EnableSSE3, W("EnableSSE3"), 1)               // Enable SSE3
CONFIG_INTEGER(EnableSSSE3, W("EnableSSSE3"), 1)             // Enable SSSE3
CONFIG_INTEGER(EnableSSE41, W("EnableSSE41"), 1)             // Enable SSE41
CONFIG_INTEGER(EnableSSE42, W("EnableSSE42"), 1)             // Enable SSE42
CONFIG_INTEGER(EnableAVX, W("EnableAVX"), 1)                 // Enable AVX
CONFIG_INTEGER(EnableAVX2, W("EnableAVX2"), 1)               // Enable AVX2
CONFIG_INTEGER(EnableFMA, W("EnableFMA"), 1)                 // Enable FMA
CONFIG_INTEGER(EnableAES, W("EnableAES"), 1)                 // Enable AES
CONFIG_INTEGER(EnableBMI1, W("EnableBMI1"), 1)               // Enable BMI1
CONFIG_INTEGER(EnableBMI2, W("EnableBMI2"), 1)               // Enable BMI2
CONFIG_INTEGER(EnableLZCNT, W("EnableLZCNT"), 1)             // Enable AES
CONFIG_INTEGER(EnablePCLMULQDQ, W("EnablePCLMULQDQ"), 1)     // Enable PCLMULQDQ
CONFIG_INTEGER(EnablePOPCNT, W("EnablePOPCNT"), 1)           // Enable POPCNT
#else                                                        // !defined(TARGET_AMD64) && !defined(TARGET_X86)
// Enable AVX instruction set for wide operations as default
CONFIG_INTEGER(EnableAVX, W("EnableAVX"), 0)
#endif                                                       // !defined(TARGET_AMD64) && !defined(TARGET_X86)

CONFIG_INTEGER(EnableEHWriteThru, W("EnableEHWriteThru"), 1) // Enable the register allocator to support EH-write thru:
                                                             // partial enregistration of vars exposed on EH boundaries
CONFIG_INTEGER(EnableMultiRegLocals, W("EnableMultiRegLocals"), 1) // Enable the enregistration of locals that are
                                                                   // defined or used in a multireg context.

// clang-format off

#if defined(TARGET_ARM64)
CONFIG_INTEGER(EnableHWIntrinsic,       W("EnableHWIntrinsic"), 1)
CONFIG_INTEGER(EnableArm64Aes,          W("EnableArm64Aes"), 1)
CONFIG_INTEGER(EnableArm64Atomics,      W("EnableArm64Atomics"), 1)
CONFIG_INTEGER(EnableArm64Crc32,        W("EnableArm64Crc32"), 1)
CONFIG_INTEGER(EnableArm64Dcpop,        W("EnableArm64Dcpop"), 1)
CONFIG_INTEGER(EnableArm64Dp,           W("EnableArm64Dp"), 1)
CONFIG_INTEGER(EnableArm64Fcma,         W("EnableArm64Fcma"), 1)
CONFIG_INTEGER(EnableArm64Fp,           W("EnableArm64Fp"), 1)
CONFIG_INTEGER(EnableArm64Fp16,         W("EnableArm64Fp16"), 1)
CONFIG_INTEGER(EnableArm64Jscvt,        W("EnableArm64Jscvt"), 1)
CONFIG_INTEGER(EnableArm64Lrcpc,        W("EnableArm64Lrcpc"), 1)
CONFIG_INTEGER(EnableArm64Pmull,        W("EnableArm64Pmull"), 1)
CONFIG_INTEGER(EnableArm64Sha1,         W("EnableArm64Sha1"), 1)
CONFIG_INTEGER(EnableArm64Sha256,       W("EnableArm64Sha256"), 1)
CONFIG_INTEGER(EnableArm64Sha512,       W("EnableArm64Sha512"), 1)
CONFIG_INTEGER(EnableArm64Sha3,         W("EnableArm64Sha3"), 1)
CONFIG_INTEGER(EnableArm64AdvSimd,      W("EnableArm64AdvSimd"), 1)
CONFIG_INTEGER(EnableArm64AdvSimd_v81,  W("EnableArm64AdvSimd_v81"), 1)
CONFIG_INTEGER(EnableArm64AdvSimd_Fp16, W("EnableArm64AdvSimd_Fp16"), 1)
CONFIG_INTEGER(EnableArm64Sm3,          W("EnableArm64Sm3"), 1)
CONFIG_INTEGER(EnableArm64Sm4,          W("EnableArm64Sm4"), 1)
CONFIG_INTEGER(EnableArm64Sve,          W("EnableArm64Sve"), 1)
#endif // defined(TARGET_ARM64)

#if defined(CONFIGURABLE_ARM_ABI)
CONFIG_INTEGER(JitSoftFP, W("JitSoftFP"), 0)
#endif // defined(CONFIGURABLE_ARM_ABI)

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

#define CONST_CSE_ENABLE_ARM64 0
#define CONST_CSE_DISABLE_ALL 1
#define CONST_CSE_ENABLE_ARM64_NO_SHARING 2
#define CONST_CSE_ENABLE_ALL 3
#define CONST_CSE_ENABLE_ALL_NO_SHARING 4

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

CONFIG_INTEGER(JitAggressiveInlining, W("JitAggressiveInlining"), 0) // Aggressive inlining of all methods
CONFIG_INTEGER(JitELTHookEnabled, W("JitELTHookEnabled"), 0)         // If 1, emit Enter/Leave/TailCall callbacks
CONFIG_INTEGER(JitInlineSIMDMultiplier, W("JitInlineSIMDMultiplier"), 3)

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
CONFIG_INTEGER(JitDoRedundantBranchOpts, W("JitDoRedundantBranchOpts"), 1) // Perform redundant branch optimizations
CONFIG_INTEGER(JitDoSsa, W("JitDoSsa"), 1) // Perform Static Single Assignment (SSA) numbering on the variables
CONFIG_INTEGER(JitDoValueNumber, W("JitDoValueNumber"), 1) // Perform value numbering on method expressions

CONFIG_METHODSET(JitOptRepeat, W("JitOptRepeat"))            // Runs optimizer multiple times on the method
CONFIG_INTEGER(JitOptRepeatCount, W("JitOptRepeatCount"), 2) // Number of times to repeat opts when repeating
#endif                                                       // defined(OPT_CONFIG)

CONFIG_INTEGER(JitTelemetry, W("JitTelemetry"), 1) // If non-zero, gather JIT telemetry data

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

#if defined(DEBUG) || defined(INLINE_DATA)
CONFIG_INTEGER(JitInlineDumpData, W("JitInlineDumpData"), 0)
CONFIG_INTEGER(JitInlineDumpXml, W("JitInlineDumpXml"), 0) // 1 = full xml (+ failures in DEBUG)
                                                           // 2 = only methods with inlines (+ failures in DEBUG)
                                                           // 3 = only methods with inlines, no failures
CONFIG_INTEGER(JitInlineLimit, W("JitInlineLimit"), -1)
CONFIG_INTEGER(JitInlinePolicyDiscretionary, W("JitInlinePolicyDiscretionary"), 0)
CONFIG_INTEGER(JitInlinePolicyFull, W("JitInlinePolicyFull"), 0)
CONFIG_INTEGER(JitInlinePolicySize, W("JitInlinePolicySize"), 0)
CONFIG_INTEGER(JitInlinePolicyRandom, W("JitInlinePolicyRandom"), 0) // nonzero enables; value is the external random
                                                                     // seed
CONFIG_INTEGER(JitInlinePolicyReplay, W("JitInlinePolicyReplay"), 0)
CONFIG_STRING(JitNoInlineRange, W("JitNoInlineRange"))
CONFIG_STRING(JitInlineReplayFile, W("JitInlineReplayFile"))
#endif // defined(DEBUG) || defined(INLINE_DATA)

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

// Various policies for GuardedDevirtualization
CONFIG_INTEGER(JitGuardedDevirtualizationChainLikelihood, W("JitGuardedDevirtualizationChainLikelihood"), 0x4B) // 75
CONFIG_INTEGER(JitGuardedDevirtualizationChainStatements, W("JitGuardedDevirtualizationChainStatements"), 4)
#if defined(DEBUG)
CONFIG_STRING(JitGuardedDevirtualizationRange, W("JitGuardedDevirtualizationRange"))
#endif // DEBUG

// Enable insertion of patchpoints into Tier0 methods with loops.
CONFIG_INTEGER(TC_OnStackReplacement, W("TC_OnStackReplacement"), 0)
// Initial patchpoint counter value used by jitted code
CONFIG_INTEGER(TC_OnStackReplacement_InitialCounter, W("TC_OnStackReplacement_InitialCounter"), 1000)

// Profile instrumentation options
CONFIG_INTEGER(JitMinimalJitProfiling, W("JitMinimalJitProfiling"), 1)
CONFIG_INTEGER(JitMinimalPrejitProfiling, W("JitMinimalPrejitProfiling"), 0)
CONFIG_INTEGER(JitClassProfiling, W("JitClassProfiling"), 1)
CONFIG_INTEGER(JitEdgeProfiling, W("JitEdgeProfiling"), 1)
CONFIG_INTEGER(JitCollect64BitCounts, W("JitCollect64BitCounts"), 0) // Collect counts as 64-bit values.

// Profile consumption options
CONFIG_INTEGER(JitDisablePgo, W("JitDisablePgo"), 0) // Ignore pgo data for all methods
#if defined(DEBUG)
CONFIG_STRING(JitEnablePgoRange, W("JitEnablePgoRange")) // Enable pgo data for only some methods
#endif                                                   // debug

// Control when Virtual Calls are expanded
CONFIG_INTEGER(JitExpandCallsEarly, W("JitExpandCallsEarly"), 1) // Expand Call targets early (in the global morph
                                                                 // phase)

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
#if defined(TARGET_ARM64)
// JitSaveFpLrWithCalleeSavedRegisters:
//    0: use default frame type decision
//    1: disable frames that save FP/LR registers with the callee-saved registers (at the top of the frame)
//    2: force all frames to use the frame types that save FP/LR registers with the callee-saved registers (at the top
//    of the frame)
CONFIG_INTEGER(JitSaveFpLrWithCalleeSavedRegisters, W("JitSaveFpLrWithCalleeSavedRegisters"), 0)
#endif // defined(TARGET_ARM64)
#endif // DEBUG

// Allow to enregister locals with struct type.
CONFIG_INTEGER(JitEnregStructLocals, W("JitEnregStructLocals"), 0)

#undef CONFIG_INTEGER
#undef CONFIG_STRING
#undef CONFIG_METHODSET
