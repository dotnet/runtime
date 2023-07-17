// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME// as "catchfault_tail"
{
}
.assembly extern xunit.core {}
// MVID: {7D0BA0DB-DCB6-4997-83C9-9F31894A25EF}
.namespace JitTest_catchfault_jmp_il
{
  .class public auto ansi Test1
         extends ['mscorlib']System.Object
  {
    .field private static bool globalFlag
    .field private static bool globalFlag2
    .method private hidebysig static bool 
            TestTryCatch(int32 recurseLevel) il managed
    {
      // Code size       72 (0x48)
      .maxstack  3
      .locals (int32[] V_0,
               bool V_1)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.1
      IL_0002:  ble.s      IL_003f

      .try
      {
        IL_0004:  ldsfld     bool JitTest_catchfault_jmp_il.Test1::globalFlag
        IL_0009:  ldc.i4.0
        IL_000a:  ceq
        IL_000c:  dup
        IL_000d:  stsfld     bool JitTest_catchfault_jmp_il.Test1::globalFlag
        IL_0012:  brfalse.s  IL_001f

        IL_0014:  ldarg.0
        IL_0015:  ldc.i4.2
        IL_0016:  sub
        IL_0017:  call       bool JitTest_catchfault_jmp_il.Test1::TestTryCatch(int32)
        IL_001c:  stloc.1
        IL_001d:  leave.s    IL_0046

        IL_001f:  ldnull
        IL_0020:  stloc.0
        IL_0021:  ldloc.0
        IL_0022:  ldc.i4.0
        IL_0023:  ldc.i4.0
        IL_0024:  stelem.i4
        IL_0025:  ldstr      "Shouldn't have reached here."
        IL_002a:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_002f:  newobj     instance void ['mscorlib']System.Exception::.ctor()
        IL_0034:  throw

      }  // end .try
      catch ['mscorlib']System.NullReferenceException 
      {
        IL_0035:  pop
        IL_0036:  ldarg.0
        IL_0037:  call       bool JitTest_catchfault_jmp_il.Test1::Test(int32)
        IL_003c:  stloc.1
        IL_003d:  leave.s    IL_0046

      }  // end handler
      IL_003f:  ldarg.0
      IL_0040:  ldc.i4.0
      IL_0041:  ceq
      IL_0043:  stloc.1
      IL_0044:  br.s       IL_0046

      IL_0046:  ldloc.1
      IL_0047:  ret
    } // end of method 'Test1::TestTryCatch'

    .method private hidebysig static bool 
            TestTryFinally(int32 recurseLevel) il managed
    {
      // Code size       79 (0x4f)
      .maxstack  3
      .locals (bool V_0,
               int32[] V_1,
               bool V_2)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.1
      IL_0002:  ble.s      IL_0046

      IL_0004:  ldc.i4.0
      IL_0005:  stloc.0
      .try
      {
        .try
        {
          IL_0006:  ldsfld     bool JitTest_catchfault_jmp_il.Test1::globalFlag
          IL_000b:  ldc.i4.0
          IL_000c:  ceq
          IL_000e:  dup
          IL_000f:  stsfld     bool JitTest_catchfault_jmp_il.Test1::globalFlag
          IL_0014:  brfalse.s  IL_0021

          IL_0016:  ldarg.0
          IL_0017:  ldc.i4.2
          IL_0018:  sub
          IL_0019:  call       bool JitTest_catchfault_jmp_il.Test1::TestTryCatch(int32)
          IL_001e:  stloc.2
          IL_001f:  leave.s    IL_004d

          IL_0021:  ldnull
          IL_0022:  stloc.1
          IL_0023:  ldloc.1
          IL_0024:  ldc.i4.0
          IL_0025:  ldc.i4.0
          IL_0026:  stelem.i4
          IL_0027:  ldstr      "Shouldn't have reached here."
          IL_002c:  call       void [System.Console]System.Console::WriteLine(class System.String)
          IL_0031:  newobj     instance void ['mscorlib']System.Exception::.ctor()
          IL_0036:  throw

        }  // end .try
        fault
        {
          IL_0037:  ldarg.0
          IL_0038:  call       bool JitTest_catchfault_jmp_il.Test1::Test(int32)
          IL_003d:  stloc.0
          IL_003e:  endfinally
        }  // end handler
      }  // end .try
      catch ['mscorlib']System.NullReferenceException 
      {
        IL_003f:  pop
        IL_0040:  leave.s    IL_0042

      }  // end handler
      IL_0042:  ldloc.0
      IL_0043:  stloc.2
      IL_0044:  br.s       IL_004d

      IL_0046:  ldarg.0
      IL_0047:  ldc.i4.0
      IL_0048:  ceq
      IL_004a:  stloc.2
      IL_004b:  br.s       IL_004d

      IL_004d:  ldloc.2
      IL_004e:  ret
    } // end of method 'Test1::TestTryFinally'

    .method private hidebysig static bool 
            Test(int32 recurseLevel) il managed
    {
      // Code size       36 (0x24)
      .maxstack  2
      .locals (bool V_0)
      IL_0000:  ldsfld     bool JitTest_catchfault_jmp_il.Test1::globalFlag2
      IL_0005:  ldc.i4.0
      IL_0006:  ceq
      IL_0008:  dup
      IL_0009:  stsfld     bool JitTest_catchfault_jmp_il.Test1::globalFlag2
      IL_000e:  brfalse.s  IL_0019

      			jmp    bool JitTest_catchfault_jmp_il.Test1::TestTryCatch(int32)

      IL_0019:  jmp    bool JitTest_catchfault_jmp_il.Test1::TestTryFinally(int32)
    } // end of method 'Test1::Test'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       97 (0x61)
      .maxstack  1
      .locals (int32 V_0)
      .try
      {
        IL_0000:  ldc.i4.s   15
        IL_0002:  call       bool JitTest_catchfault_jmp_il.Test1::TestTryCatch(int32)
        IL_0007:  brtrue.s   IL_0012

        IL_0009:  ldc.i4.s   18
        IL_000b:  call       bool JitTest_catchfault_jmp_il.Test1::TestTryCatch(int32)
        IL_0010:  brtrue.s   IL_0020

        IL_0012:  ldstr      "try...catch test failed."
        IL_0017:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_001c:  ldc.i4.1
        IL_001d:  stloc.0
        IL_001e:  leave.s    IL_005f

        IL_0020:  ldc.i4.s   19
        IL_0022:  call       bool JitTest_catchfault_jmp_il.Test1::TestTryFinally(int32)
        IL_0027:  brtrue.s   IL_0032

        IL_0029:  ldc.i4.s   12
        IL_002b:  call       bool JitTest_catchfault_jmp_il.Test1::TestTryFinally(int32)
        IL_0030:  brtrue.s   IL_0040

        IL_0032:  ldstr      "try...finally test failed."
        IL_0037:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_003c:  ldc.i4.2
        IL_003d:  stloc.0
        IL_003e:  leave.s    IL_005f

        IL_0040:  leave.s    IL_0051

      }  // end .try
      catch ['mscorlib']System.Exception 
      {
        IL_0042:  pop
        IL_0043:  ldstr      "Failed w/ exception"
        IL_0048:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_004d:  ldc.i4.m1
        IL_004e:  stloc.0
        IL_004f:  leave.s    IL_005f

      }  // end handler
      IL_0051:  ldstr      "Passed"
      IL_0056:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_005b:  ldc.i4    0x64
      IL_005c:  stloc.0
      IL_005d:  br.s       IL_005f

      IL_005f:  ldloc.0
      IL_0060:  ret
    } // end of method 'Test1::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  stsfld     bool JitTest_catchfault_jmp_il.Test1::globalFlag
      IL_0006:  ldc.i4.1
      IL_0007:  stsfld     bool JitTest_catchfault_jmp_il.Test1::globalFlag2
      IL_000c:  ret
    } // end of method 'Test1::.cctor'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test1::.ctor'

  } // end of class 'Test1'

} // end of namespace 'JitTest_catchfault_jmp_il'

//*********** DISASSEMBLY COMPLETE ***********************
