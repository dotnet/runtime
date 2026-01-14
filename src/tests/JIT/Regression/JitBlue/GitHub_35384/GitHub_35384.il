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

  .method public hidebysig instance string 
          InstanceMethod() cil managed noinlining
  {
    .maxstack  1
    ldstr      "Instance method"
    ret
  } // end of method Struct::InstanceMethod

} // end of class Struct

.class public auto beforefieldinit Program
       extends [System.Runtime]System.Object
{
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
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {
        string('https://github.com/dotnet/runtime/issues/114908')
        type([TestLibrary]TestLibrary.PlatformDetection)
        string[1] ('IsAppleMobile')
    }
    .entrypoint
    .maxstack 1
    call       string Program::ValueTypeExplicitThisInstanceMethodCalli()
    pop
    ldc.i4 100
    ret
  } // end of method Program::Main
} // end of class Program
