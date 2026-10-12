// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/135592.
//
// A clause nested in the handler of an earlier-listed clause is legal IL: ECMA-335 I.12.4.2.5 orders nested
// try blocks only, and the JIT sorts the EH table (fgSortEHTable) precisely to accept handler-nested clauses
// listed after the clause that encloses them. The JIT used to stamp the blocks of a filter with the clause's
// pre-sort index and never updated it after the sort. When the sort moved the clause, the stale index pointed
// at another clause: at a non-filter clause the method was rejected with "Unexpected endfilter"
// (InvalidProgramException); at another filter clause the two filters were attributed to each other's clause.
//
// The four scenario methods throw inside their nested try and return 1 when the nested handler catches.
// Three scenarios use filters; CatchInEarlierFinally is a catch-only control.
//
// Disabled on Mono: Mono's JIT can leave the blocks of a handler or filter nested in an earlier-listed
// clause's handler in the enclosing handler's region. FilterInEarlierFinally crashes or hangs on some Mono
// configurations, and the Mono LLVM AOT compiler asserts on CatchInEarlierFinally.

.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly Runtime_135592 { }
.module Runtime_135592.dll

.class public auto ansi abstract sealed beforefieldinit Runtime_135592
       extends [System.Runtime]System.Object
{
  // Filter clause nested in a finally handler, finally clause listed first. Rejected before the fix.
  .method public hidebysig static int32 FilterInEarlierFinally() cil managed noinlining
  {
    .maxstack 2
    .locals init (int32 result)
    ldc.i4.0
    stloc.0
  OuterTry:
    leave Done
  FinallyStart:
    nop
  InnerTry:
    newobj instance void [System.Runtime]System.Exception::.ctor()
    throw
  FilterStart:
    isinst [System.Runtime]System.Exception
    ldnull
    cgt.un
    endfilter
  HandlerStart:
    pop
    ldc.i4.1
    stloc.0
    leave FinallyEnd
  FinallyEnd:
    endfinally
  Done:
    ldloc.0
    ret
    .try OuterTry to FinallyStart finally handler FinallyStart to Done
    .try InnerTry to FilterStart filter FilterStart handler HandlerStart to FinallyEnd
  }

  // Control: the same IL with the clauses in the other order (the order compilers emit). Always accepted.
  .method public hidebysig static int32 FilterInEarlierFinally_InnerFirst() cil managed noinlining
  {
    .maxstack 2
    .locals init (int32 result)
    ldc.i4.0
    stloc.0
  OuterTry:
    leave Done
  FinallyStart:
    nop
  InnerTry:
    newobj instance void [System.Runtime]System.Exception::.ctor()
    throw
  FilterStart:
    isinst [System.Runtime]System.Exception
    ldnull
    cgt.un
    endfilter
  HandlerStart:
    pop
    ldc.i4.1
    stloc.0
    leave FinallyEnd
  FinallyEnd:
    endfinally
  Done:
    ldloc.0
    ret
    .try InnerTry to FilterStart filter FilterStart handler HandlerStart to FinallyEnd
    .try OuterTry to FinallyStart finally handler FinallyStart to Done
  }

  // Control: the same order with a catch instead of a filter. Always accepted (the sort handles non-filter
  // clauses correctly).
  .method public hidebysig static int32 CatchInEarlierFinally() cil managed noinlining
  {
    .maxstack 1
    .locals init (int32 result)
    ldc.i4.0
    stloc.0
  OuterTry:
    leave Done
  FinallyStart:
    nop
  InnerTry:
    newobj instance void [System.Runtime]System.Exception::.ctor()
    throw
  CatchStart:
    pop
    ldc.i4.1
    stloc.0
    leave FinallyEnd
  FinallyEnd:
    endfinally
  Done:
    ldloc.0
    ret
    .try OuterTry to FinallyStart finally handler FinallyStart to Done
    .try InnerTry to CatchStart catch [System.Runtime]System.Exception handler CatchStart to FinallyEnd
  }

  // Filter clause nested in the handler of another filter clause, outer clause listed first. Before the fix
  // the sort swapped the clauses and each filter kept the other's index.
  .method public hidebysig static int32 FilterInEarlierFilterHandler() cil managed noinlining
  {
    .maxstack 2
    .locals init (int32 result)
    ldc.i4.0
    stloc.0
  OuterTry:
    newobj instance void [System.Runtime]System.Exception::.ctor()
    throw
  OuterFilter:
    isinst [System.Runtime]System.Exception
    ldnull
    cgt.un
    endfilter
  OuterHandler:
    pop
  InnerTry:
    newobj instance void [System.Runtime]System.Exception::.ctor()
    throw
  InnerFilter:
    isinst [System.Runtime]System.Exception
    ldnull
    cgt.un
    endfilter
  InnerHandler:
    pop
    ldc.i4.1
    stloc.0
    leave Done
  Done:
    ldloc.0
    ret
    .try OuterTry to OuterFilter filter OuterFilter handler OuterHandler to Done
    .try InnerTry to InnerFilter filter InnerFilter handler InnerHandler to Done
  }

  .method private hidebysig static int32 Check(int32 actual, string name) cil managed noinlining
  {
    .maxstack 2
    ldarg.0
    ldc.i4.1
    beq.s Ok
    ldstr "FAIL: "
    ldarg.1
    call string [System.Runtime]System.String::Concat(string, string)
    call void [System.Console]System.Console::WriteLine(string)
    ldc.i4.1
    ret
  Ok:
    ldc.i4.0
    ret
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = {
        string('https://github.com/dotnet/runtime/issues/135641')
        int32(0x2) // Mono
    }
    .entrypoint
    .maxstack 3
    .locals init (int32 failures)
    ldc.i4.0
    stloc.0

    call int32 Runtime_135592::FilterInEarlierFinally()
    ldstr "FilterInEarlierFinally"
    call int32 Runtime_135592::Check(int32, string)
    ldloc.0
    add
    stloc.0

    call int32 Runtime_135592::FilterInEarlierFinally_InnerFirst()
    ldstr "FilterInEarlierFinally_InnerFirst"
    call int32 Runtime_135592::Check(int32, string)
    ldloc.0
    add
    stloc.0

    call int32 Runtime_135592::CatchInEarlierFinally()
    ldstr "CatchInEarlierFinally"
    call int32 Runtime_135592::Check(int32, string)
    ldloc.0
    add
    stloc.0

    call int32 Runtime_135592::FilterInEarlierFilterHandler()
    ldstr "FilterInEarlierFilterHandler"
    call int32 Runtime_135592::Check(int32, string)
    ldloc.0
    add
    stloc.0

    ldloc.0
    brtrue.s Failed
    ldc.i4 100
    ret
  Failed:
    ldc.i4 101
    ret
  }
}
