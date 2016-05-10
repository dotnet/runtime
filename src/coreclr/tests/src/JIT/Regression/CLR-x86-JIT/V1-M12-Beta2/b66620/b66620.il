// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ctor_merge2.il
//
// NEGATIVE
// merge of instance and static method

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ctor_merge2 { }
.class public auto ansi beforefieldinit TestClass
       extends [mscorlib]System.Object
{
  .field private static bool _sel
  .field private bool dummy
  .method public hidebysig static void
          DelegateFunc1(int32) il managed
  {
    .maxstack  3
    IL_0000:  ldstr      "DelegateFunc1"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(class System.String)
    IL_001a:  ret
  }
  .method public hidebysig instance void DelegateFunc2(int32) il managed
  {
    .maxstack  8
    .locals (int32[] V_0)
    ldarg.0
    castclass [mscorlib]System.Object
    pop
    
    IL_0000:  ldstr      "DelegateFunc2"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(class System.String)
    IL_000f:  ret
  }
  .method private hidebysig static bool sel() il managed
  {
    .maxstack  2
    .locals (bool V_0)
    IL_0000:  ldsfld     bool TestClass::_sel
    IL_0005:  ldc.i4.0
    IL_0006:  ceq
    IL_0008:  stsfld     bool TestClass::_sel
    IL_000d:  ldsfld     bool TestClass::_sel
    IL_0012:  stloc.0
    IL_0013:  br.s       IL_0015
    IL_0015:  ldloc.0
    IL_0016:  ret
  }
  .method public hidebysig static int32 Main(class System.String[] args) il managed
  {
    .entrypoint
    .maxstack  8
    newobj     instance void TestClass::.ctor()
	ldc.i4.1
    dup
    brfalse.s   BRANCH
    ldftn      instance void TestClass::DelegateFunc2(int32)
    pop
	pop
    br.s       MERGE

BRANCH:
	pop
    ldftn      void TestClass::DelegateFunc1(int32)
    pop

MERGE:
	pop

	ldc.i4 100    
    ret
  }
  .method private hidebysig specialname rtspecialname static 
          void .cctor() il managed
  {
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     bool TestClass::_sel
    IL_0006:  ret
  }
  .method public hidebysig specialname rtspecialname 
          instance void .ctor() il managed
  {
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }
}
