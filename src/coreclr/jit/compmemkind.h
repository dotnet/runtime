// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef CompMemKindMacro
#error Define CompMemKindMacro before including this file.
#endif

// This list of macro invocations should be used to define the CompMemKind enumeration,
// and the corresponding array of string names for these enum members.

// clang-format off
CompMemKindMacro(AssertionProp)
CompMemKindMacro(ASTNode)
CompMemKindMacro(InstDesc)
CompMemKindMacro(ImpStack)
CompMemKindMacro(BasicBlock)
CompMemKindMacro(CallArgs)
CompMemKindMacro(FlowEdge)
CompMemKindMacro(DepthFirstSearch)
CompMemKindMacro(Loops)
CompMemKindMacro(TreeStatementList)
CompMemKindMacro(SiScope)
CompMemKindMacro(DominatorMemory)
CompMemKindMacro(LSRA)
CompMemKindMacro(LSRA_Interval)
CompMemKindMacro(LSRA_RefPosition)
CompMemKindMacro(Reachability)
CompMemKindMacro(SSA)
CompMemKindMacro(ValueNumber)
CompMemKindMacro(LvaTable)
CompMemKindMacro(UnwindInfo)
CompMemKindMacro(hashBv)
CompMemKindMacro(bitset)
CompMemKindMacro(FixedBitVect)
CompMemKindMacro(Generic)
CompMemKindMacro(LocalAddressVisitor)
CompMemKindMacro(FieldSeqStore)
CompMemKindMacro(MemorySsaMap)
CompMemKindMacro(MemoryPhiArg)
CompMemKindMacro(CSE)
CompMemKindMacro(GC)
CompMemKindMacro(CorTailCallInfo)
CompMemKindMacro(Inlining)
CompMemKindMacro(ArrayStack)
CompMemKindMacro(DebugInfo)
CompMemKindMacro(DebugOnly)
CompMemKindMacro(Codegen)
CompMemKindMacro(LoopOpt)
CompMemKindMacro(LoopClone)
CompMemKindMacro(LoopUnroll)
CompMemKindMacro(LoopHoist)
CompMemKindMacro(LoopIVOpts)
CompMemKindMacro(Unknown)
CompMemKindMacro(RangeCheck)
CompMemKindMacro(CopyProp)
CompMemKindMacro(Promotion)
CompMemKindMacro(SideEffects)
CompMemKindMacro(ObjectAllocator)
CompMemKindMacro(VariableLiveRanges)
CompMemKindMacro(ClassLayout)
CompMemKindMacro(TailMergeThrows)
CompMemKindMacro(EarlyProp)
CompMemKindMacro(ZeroInit)
CompMemKindMacro(Pgo)
//clang-format on

#undef CompMemKindMacro
