// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This tests a calli with explicit this in the signature

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly GitHub_35384 {}
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }

.class private sequential ansi sealed beforefieldinit Struct
       extends [System.Runtime]System.ValueType
{
  .field public int32 a
  .field public int32 b
  .field public int32 c
  .field public int32 d
  .field public int32 e
  .field public int32 f
  .field public int32 g
  .field public int32 h
  .field public int32 i
  .field public int32 j
  .field public int32 k
  .field public int32 l
  .field public int32 m

  .method public hidebysig instance string 
          InstanceMethod() cil managed noinlining
  {
    .maxstack  1
    ldstr      "Instance method"
    ret
  } // end of method Struct::InstanceMethod

  .field public static valuetype Struct s

  .method public hidebysig instance valuetype Struct
          InstanceMethodReturningLargeValuetype() cil managed noinlining
  {
    .maxstack  1
    ldsfld valuetype Struct Struct::s
    ret
  } // end of method Struct::InstanceMethodReturningLargeValuetype
} // end of class Struct

.class public auto beforefieldinit Program
       extends [System.Runtime]System.Object
{
  .method private hidebysig static int32 
          ValueTypeExplicitThisInstanceMethodCalli_ReturnLargeValuetype() cil managed noinlining
  {
    .maxstack  3
    .locals init (valuetype Struct V_0,
           valuetype Struct V_1)
    ldloca.s   V_0
    initobj    Struct

    ldsflda valuetype Struct Struct::s
    ldc.i4.1
    stfld      int32 Struct::a
    ldsflda valuetype Struct Struct::s
    ldc.i4.2
    stfld      int32 Struct::b
    ldsflda valuetype Struct Struct::s
    ldc.i4.3
    stfld      int32 Struct::c
    ldsflda valuetype Struct Struct::s
    ldc.i4.4
    stfld      int32 Struct::d
    ldsflda valuetype Struct Struct::s
    ldc.i4.5
    stfld      int32 Struct::e
    ldsflda valuetype Struct Struct::s
    ldc.i4.6
    stfld      int32 Struct::f
    ldsflda valuetype Struct Struct::s
    ldc.i4.7
    stfld      int32 Struct::g
    ldsflda valuetype Struct Struct::s
    ldc.i4.8
    stfld      int32 Struct::h
    ldsflda valuetype Struct Struct::s
    ldc.i4.s   9
    stfld      int32 Struct::i
    ldsflda valuetype Struct Struct::s
    ldc.i4.s   10
    stfld      int32 Struct::j
    ldsflda valuetype Struct Struct::s
    ldc.i4.s   11
    stfld      int32 Struct::k
    ldsflda valuetype Struct Struct::s
    ldc.i4.s   12
    stfld      int32 Struct::l
    ldsflda valuetype Struct Struct::s
    ldc.i4.s   22
    stfld      int32 Struct::m

    ldloca.s   V_0
    ldftn      instance valuetype Struct Struct::InstanceMethodReturningLargeValuetype()
    calli      explicit instance valuetype Struct(valuetype Struct&)

    stloc.1

    ldloca.s   V_0
    initobj    Struct

    ldloc.1
    ldfld      int32 Struct::a
    ldloc.1
    ldfld      int32 Struct::b
    add
    ldloc.1
    ldfld      int32 Struct::c
    add
    ldloc.1
    ldfld      int32 Struct::d
    add
    ldloc.1
    ldfld      int32 Struct::e
    add
    ldloc.1
    ldfld      int32 Struct::f
    add
    ldloc.1
    ldfld      int32 Struct::g
    add
    ldloc.1
    ldfld      int32 Struct::h
    add
    ldloc.1
    ldfld      int32 Struct::i
    add
    ldloc.1
    ldfld      int32 Struct::j
    add
    ldloc.1
    ldfld      int32 Struct::k
    add
    ldloc.1
    ldfld      int32 Struct::l
    add
    ldloc.1
    ldfld      int32 Struct::m
    add

    ret
  } // end of method Program::ValueTypeExplicitThisInstanceMethodCalli
    .method private hidebysig static string 
          ValueTypeExplicitThisInstanceMethodCalli() cil managed noinlining
  {
    .maxstack  2
    .locals init (valuetype Struct V_0)
    ldloca.s   V_0
    initobj    Struct
    ldloca.s   V_0
    ldftn      instance string Struct::InstanceMethod()
    calli      explicit instance string(valuetype Struct&)
    ret
  } // end of method Program::ValueTypeExplicitThisInstanceMethodCalli
  
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = {}
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [System.Runtime]System.Type, string[]) = {
        string('https://github.com/dotnet/runtime/issues/114908')
        type([TestLibrary]TestLibrary.PlatformDetection)
        string[1] ('IsMonoRuntime')
    }
    .entrypoint
    .maxstack 2
    call       string Program::ValueTypeExplicitThisInstanceMethodCalli()
    pop
    ldc.i4 0
    // This should return 100 if it works correctly
    call int32 Program::ValueTypeExplicitThisInstanceMethodCalli_ReturnLargeValuetype()
    add
    ret
  } // end of method Program::Main
} // end of class Program
