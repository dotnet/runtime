// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************/
#ifndef CompMemKindMacro
#error  Define CompMemKindMacro before including this file.
#endif

// This list of macro invocations should be used to define the CompMemKind enumeration,
// and the corresponding array of string names for these enum members.

CompMemKindMacro(AssertionProp)
CompMemKindMacro(ASTNode)
CompMemKindMacro(InstDesc)
CompMemKindMacro(ImpStack)
CompMemKindMacro(BasicBlock)
CompMemKindMacro(fgArgInfo)
CompMemKindMacro(fgArgInfoPtrArr)
CompMemKindMacro(FlowList)
CompMemKindMacro(TreeStatementList)
CompMemKindMacro(SiScope)
CompMemKindMacro(FlatFPStateX87)
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
CompMemKindMacro(AsIAllocator)
CompMemKindMacro(IndirAssignMap)
CompMemKindMacro(FieldSeqStore)
CompMemKindMacro(ZeroOffsetFieldMap)
CompMemKindMacro(ArrayInfoMap)
CompMemKindMacro(HeapPhiArg)
CompMemKindMacro(CSE)
CompMemKindMacro(GC)
CompMemKindMacro(CorSig)
CompMemKindMacro(Inlining)
CompMemKindMacro(ArrayStack)
CompMemKindMacro(DebugInfo)
CompMemKindMacro(DebugOnly)
CompMemKindMacro(Codegen)
CompMemKindMacro(LoopOpt)
CompMemKindMacro(LoopHoist)
CompMemKindMacro(Unknown)

#undef CompMemKindMacro
