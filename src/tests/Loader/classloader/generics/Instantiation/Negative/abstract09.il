// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
// DESCRIPTION:
// 
// This tests that a non-generic struct that implements a generic abstract interface cannot 
// be loaded without implementing all methods declared on that interface.
// Here, Gen implements GenBase<T>, but does not implement GenBase.Dummy(T).



.assembly extern mscorlib {}

.assembly 'abstract09' {}


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi GenBase<([mscorlib]System.Object) T>
{
  .method public hidebysig newslot abstract virtual 
        instance void  Dummy(!0 t) cil managed
  {
  } // end of method GenBase::Dummy

} // end of class GenBase

.class public sequential ansi sealed beforefieldinit Gen
	extends [mscorlib]System.ValueType
       implements class GenBase<int32>
{
	.size 1
	.pack 1
} 


.class public auto ansi beforefieldinit GenTest
       extends [mscorlib]System.Object
{
  .method private hidebysig instance valuetype Gen
          InternalTest() cil managed
  {
    // Code size       10 (0xa)
    .maxstack  1
    .locals init (valuetype Gen V_0)
    IL_0000:  newobj     instance void valuetype Gen::.ctor()
    IL_0005:  stloc.0
    IL_0006:  br.s       IL_0008

    IL_0008:  ldloc.0
    IL_0009:  ret
  } // end of method GenTest::InternalTest

  .method private hidebysig instance void 
          IndirectTest() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance valuetype Gen class GenTest::InternalTest()
    IL_0006:  pop
    IL_0007:  ret
  } // end of method GenTest::IndirectTest

  .method public hidebysig instance bool 
          Test_abstract09() cil managed
  {
    // Code size       48 (0x30)
    .maxstack  2
    .locals init (class [mscorlib]System.Exception V_0,
             bool V_1)
    .try
    {
      IL_0000:  ldarg.0
      IL_0001:  call       instance void class GenTest::IndirectTest()
      IL_0006:  ldstr      "Test did not throw expected TypeLoadException"
      IL_000b:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0010:  ldc.i4.0
      IL_0011:  stloc.1
      IL_0012:  leave.s    IL_002e

    }  // end .try
    catch [mscorlib]System.TypeLoadException
    {
      IL_0014:  pop
      IL_0015:  ldc.i4.1
      IL_0016:  stloc.1
      IL_0017:  leave.s    IL_002e

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0019:  stloc.0
      IL_001a:  ldstr      "Test caught unexpected Exception "
      IL_001f:  ldloc.0
      IL_0020:  call       string [mscorlib]System.String::Concat(object,
                                                                  object)
      IL_0025:  call       void [System.Console]System.Console::WriteLine(string)
      IL_002a:  ldc.i4.0
      IL_002b:  stloc.1
      IL_002c:  leave.s    IL_002e

    }  // end handler
    IL_002e:  ldloc.1
    IL_002f:  ret
  } // end of method GenTest::Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GenTest::.ctor

} // end of class GenTest

.class public auto ansi beforefieldinit Test_abstract09
       extends [mscorlib]System.Object
{
  .field public static int32 counter
  .field public static bool result
  .method public hidebysig static void  Eval(bool exp) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    IL_0000:  ldsfld     int32 Test_abstract09::counter
    IL_0005:  ldc.i4.1
    IL_0006:  add
    IL_0007:  stsfld     int32 Test_abstract09::counter
    IL_000c:  ldarg.0
    IL_000d:  brtrue.s   IL_002e

    IL_000f:  ldarg.0
    IL_0010:  stsfld     bool Test_abstract09::result
    IL_0015:  ldstr      "Test Failed at location: "
    IL_001a:  ldsfld     int32 Test_abstract09::counter
    IL_001f:  box        [mscorlib]System.Int32
    IL_0024:  call       string [mscorlib]System.String::Concat(object,
                                                                object)
    IL_0029:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002e:  ret
  } // end of method Test::Eval

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       53 (0x35)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  newobj     instance void GenTest::.ctor()
    IL_0005:  call       instance bool GenTest::Test_abstract09()
    IL_000a:  call       void Test_abstract09::Eval(bool)
    IL_000f:  ldsfld     bool Test_abstract09::result
    IL_0014:  brfalse.s  IL_0025

    IL_0016:  ldstr      "Test Passed"
    IL_001b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0020:  ldc.i4.s   100
    IL_0022:  stloc.0
    IL_0023:  br.s       IL_0033

    IL_0025:  ldstr      "Test Failed"
    IL_002a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002f:  ldc.i4.1
    IL_0030:  stloc.0
    IL_0031:  br.s       IL_0033

    IL_0033:  ldloc.0
    IL_0034:  ret
  } // end of method Test::Main

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  1
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     int32 Test_abstract09::counter
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool Test_abstract09::result
    IL_000c:  ret
  } // end of method Test::.cctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  1
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================


//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file abstract02.res
