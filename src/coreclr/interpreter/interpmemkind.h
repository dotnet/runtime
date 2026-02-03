// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*****************************************************************************/
#ifndef InterpMemKindMacro
#error Define InterpMemKindMacro before including this file.
#endif

// This list of macro invocations defines the InterpMemKind enumeration,
// and the corresponding array of string names for these enum members.
// This follows the same X-macro pattern as the JIT's compmemkind.h.

// clang-format off
InterpMemKindMacro(AllocOffsets)      // Alloc offsets allocations
InterpMemKindMacro(AsyncSuspend)      // Async suspend allocations
InterpMemKindMacro(BasicBlock)        // InterpBasicBlock allocations
InterpMemKindMacro(CallDependencies)  // Call dependency tracking
InterpMemKindMacro(CallInfo)          // Call metadata (InterpCallInfo)
InterpMemKindMacro(ConservativeRange) // Conservative GC range tracking
InterpMemKindMacro(DataItem)          // Data items array
InterpMemKindMacro(DebugOnly)         // Debug-only allocations
InterpMemKindMacro(DelegateCtorPeep)  // Ldftn delegate constructor peep info
InterpMemKindMacro(EHClause)          // Exception handling clauses
InterpMemKindMacro(GC)                // GC info encoding
InterpMemKindMacro(Generic)           // General/uncategorized allocations
InterpMemKindMacro(ILCode)            // IL code buffers
InterpMemKindMacro(InterpCode)        // Interpreter bytecode
InterpMemKindMacro(Instruction)       // InterpInst allocations
InterpMemKindMacro(IntervalMap)       // Variable interval maps
InterpMemKindMacro(NativeToILMapping) // Native to IL offset mappings
InterpMemKindMacro(Reloc)             // Relocations
InterpMemKindMacro(RetryData)         // Data for retrying compilation
InterpMemKindMacro(StackInfo)         // Stack state tracking
InterpMemKindMacro(StackMap)          // Stack map information
InterpMemKindMacro(StackMapHash)      // Stack map hash information
InterpMemKindMacro(SwitchTable)       // Switch target tables
InterpMemKindMacro(Var)               // Variable tracking (InterpVar)
InterpMemKindMacro(VarSizedDataItem)  // Variable-sized data items
// clang-format on

#undef InterpMemKindMacro
