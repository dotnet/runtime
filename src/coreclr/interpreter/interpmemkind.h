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
InterpMemKindMacro(Generic)           // General/uncategorized allocations
InterpMemKindMacro(BasicBlock)        // InterpBasicBlock allocations
InterpMemKindMacro(Instruction)       // InterpInst allocations
InterpMemKindMacro(StackInfo)         // Stack state tracking
InterpMemKindMacro(CallInfo)          // Call metadata (InterpCallInfo)
InterpMemKindMacro(Var)               // Variable tracking (InterpVar)
InterpMemKindMacro(GC)                // GC info encoding
InterpMemKindMacro(Reloc)             // Relocations
InterpMemKindMacro(DataItem)          // Data items array
InterpMemKindMacro(ILCode)            // IL code buffers
InterpMemKindMacro(SwitchTable)       // Switch target tables
InterpMemKindMacro(EHClause)          // Exception handling clauses
InterpMemKindMacro(IntervalMap)       // Variable interval maps
InterpMemKindMacro(DebugOnly)         // Debug-only allocations
// clang-format on

#undef InterpMemKindMacro
