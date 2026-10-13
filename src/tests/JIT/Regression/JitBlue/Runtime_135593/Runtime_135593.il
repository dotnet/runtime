// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/135593.
//
// A method whose first instruction lies inside a handler or filter is not valid IL
// (ECMA-335 I.12.4.2.8.1). This test requires the JIT to reject all four scenarios with
// InvalidProgramException. The scenario methods are never called: each is compiled through
// RuntimeHelpers.PrepareMethod inside a try/catch.

.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly Runtime_135593 { }
.module Runtime_135593.dll

.class public auto ansi abstract sealed beforefieldinit Runtime_135593
       extends [System.Runtime]System.Object
{
  // Method entry inside a finally handler.
  .method public hidebysig static void EntryInFinally() cil managed noinlining
  {
    .maxstack 1
  HandlerStart:
    endfinally
  TryStart:
    leave MethodEnd
  MethodEnd:
    ret
    .try TryStart to MethodEnd finally handler HandlerStart to TryStart
  }

  // Method entry inside a fault handler.
  .method public hidebysig static void EntryInFault() cil managed noinlining
  {
    .maxstack 1
  HandlerStart:
    endfault
  TryStart:
    leave MethodEnd
  MethodEnd:
    ret
    .try TryStart to MethodEnd fault handler HandlerStart to TryStart
  }

  // Method entry inside a catch handler. The handler does not pop the exception object, so the entry point is
  // the only thing wrong with it.
  .method public hidebysig static void EntryInCatch() cil managed noinlining
  {
    .maxstack 1
  HandlerStart:
    leave MethodEnd
  TryStart:
    leave MethodEnd
  MethodEnd:
    ret
    .try TryStart to MethodEnd catch [System.Runtime]System.Exception handler HandlerStart to TryStart
  }

  // Method entry inside a filter.
  .method public hidebysig static void EntryInFilter() cil managed noinlining
  {
    .maxstack 1
  FilterStart:
    pop
    ldc.i4.1
    endfilter
  HandlerStart:
    pop
    leave MethodEnd
  TryStart:
    leave MethodEnd
  MethodEnd:
    ret
    .try TryStart to MethodEnd filter FilterStart handler HandlerStart to TryStart
  }

  // Compiles the method; returns 1 if the JIT rejected it, 0 if it compiled.
  .method private hidebysig static int32 Probe(valuetype [System.Runtime]System.RuntimeMethodHandle handle, string name) cil managed noinlining
  {
    .maxstack 2
    .locals init (int32 rejected)
    ldc.i4.0
    stloc.0
    .try
    {
      ldarg.0
      call void [System.Runtime]System.Runtime.CompilerServices.RuntimeHelpers::PrepareMethod(valuetype [System.Runtime]System.RuntimeMethodHandle)
      ldstr "FAIL: compiled "
      ldarg.1
      call string [System.Runtime]System.String::Concat(string, string)
      call void [System.Console]System.Console::WriteLine(string)
      leave.s Done
    }
    catch [System.Runtime]System.InvalidProgramException
    {
      pop
      ldc.i4.1
      stloc.0
      leave.s Done
    }
  Done:
    ldloc.0
    ret
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = {
        string('https://github.com/dotnet/runtime/issues/135639')
        int32(0x2) // Mono
    }
    .entrypoint
    .maxstack 3
    .locals init (int32 rejected)

    ldtoken method void Runtime_135593::EntryInFinally()
    ldstr "EntryInFinally"
    call int32 Runtime_135593::Probe(valuetype [System.Runtime]System.RuntimeMethodHandle, string)
    stloc.0

    ldtoken method void Runtime_135593::EntryInFault()
    ldstr "EntryInFault"
    call int32 Runtime_135593::Probe(valuetype [System.Runtime]System.RuntimeMethodHandle, string)
    ldloc.0
    add
    stloc.0

    ldtoken method void Runtime_135593::EntryInCatch()
    ldstr "EntryInCatch"
    call int32 Runtime_135593::Probe(valuetype [System.Runtime]System.RuntimeMethodHandle, string)
    ldloc.0
    add
    stloc.0

    ldtoken method void Runtime_135593::EntryInFilter()
    ldstr "EntryInFilter"
    call int32 Runtime_135593::Probe(valuetype [System.Runtime]System.RuntimeMethodHandle, string)
    ldloc.0
    add
    stloc.0

    ldloc.0
    ldc.i4.4
    bne.un.s Failed
    ldc.i4 100
    ret
  Failed:
    ldc.i4 101
    ret
  }
}
