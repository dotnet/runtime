// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}
// MVID: {CCB52291-4072-427C-8C14-2780C164EB2B}
.namespace JitTest_catchfinally_tail_il
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
        IL_0004:  ldsfld     bool JitTest_catchfinally_tail_il.Test1::globalFlag
        IL_0009:  ldc.i4.0
        IL_000a:  ceq
        IL_000c:  dup
        IL_000d:  stsfld     bool JitTest_catchfinally_tail_il.Test1::globalFlag
        IL_0012:  brfalse.s  IL_001f

        IL_0014:  ldarg.0
        IL_0015:  ldc.i4.2
        IL_0016:  sub
        IL_0017:  call       bool JitTest_catchfinally_tail_il.Test1::TestTryCatch(int32)
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
        IL_0037:  call       bool JitTest_catchfinally_tail_il.Test1::Test(int32)
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
      // Code size       86 (0x56)
      .maxstack  3
      .locals (bool V_0,
               bool V_1,
               int32[] V_2,
               bool V_3)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.1
      IL_0002:  ble.s      IL_004d

      IL_0004:  ldc.i4.0
      IL_0005:  stloc.0
      .try
      {
        IL_0006:  ldc.i4.0
        IL_0007:  stloc.1
        .try
        {
          IL_0008:  ldsfld     bool JitTest_catchfinally_tail_il.Test1::globalFlag
          IL_000d:  ldc.i4.0
          IL_000e:  ceq
          IL_0010:  dup
          IL_0011:  stsfld     bool JitTest_catchfinally_tail_il.Test1::globalFlag
          IL_0016:  brfalse.s  IL_0023

          IL_0018:  ldarg.0
          IL_0019:  ldc.i4.2
          IL_001a:  sub
          IL_001b:  call       bool JitTest_catchfinally_tail_il.Test1::Test(int32)
          IL_0020:  stloc.3
          IL_0021:  leave.s    IL_0054

          IL_0023:  ldc.i4.1
          IL_0024:  stloc.1
          IL_0025:  ldnull
          IL_0026:  stloc.2
          IL_0027:  ldloc.2
          IL_0028:  ldc.i4.0
          IL_0029:  ldc.i4.0
          IL_002a:  stelem.i4
          IL_002b:  ldstr      "Shouldn't have reached here."
          IL_0030:  call       void [System.Console]System.Console::WriteLine(class System.String)
          IL_0035:  newobj     instance void ['mscorlib']System.Exception::.ctor()
          IL_003a:  throw

        }  // end .try
        finally
        {
          IL_003b:  ldloc.1
          IL_003c:  brfalse.s  IL_0045

          IL_003e:  ldarg.0
          IL_003f:  call       bool JitTest_catchfinally_tail_il.Test1::TestTryCatch(int32)
          IL_0044:  stloc.0
          IL_0045:  endfinally
        }  // end handler
      }  // end .try
      catch ['mscorlib']System.NullReferenceException 
      {
        IL_0046:  pop
        IL_0047:  leave.s    IL_0049

      }  // end handler
      IL_0049:  ldloc.0
      IL_004a:  stloc.3
      IL_004b:  br.s       IL_0054

      IL_004d:  ldarg.0
      IL_004e:  ldc.i4.0
      IL_004f:  ceq
      IL_0051:  stloc.3
      IL_0052:  br.s       IL_0054

      IL_0054:  ldloc.3
      IL_0055:  ret
    } // end of method 'Test1::TestTryFinally'

    .method private hidebysig static bool 
            Test(int32 recurseLevel) il managed
    {
      // Code size       36 (0x24)
      .maxstack  2
      .locals (bool V_0)
      IL_0000:  ldsfld     bool JitTest_catchfinally_tail_il.Test1::globalFlag2
      IL_0005:  ldc.i4.0
      IL_0006:  ceq
      IL_0008:  dup
      IL_0009:  stsfld     bool JitTest_catchfinally_tail_il.Test1::globalFlag2
      IL_000e:  brfalse.s  IL_0019

      IL_0010:  ldarg.0
      			tail. call       bool JitTest_catchfinally_tail_il.Test1::TestTryCatch(int32)
      			ret

      IL_0019:  ldarg.0
      			tail. call       bool JitTest_catchfinally_tail_il.Test1::TestTryFinally(int32)
      			ret
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
        IL_0002:  call       bool JitTest_catchfinally_tail_il.Test1::TestTryCatch(int32)
        IL_0007:  brtrue.s   IL_0012

        IL_0009:  ldc.i4.s   18
        IL_000b:  call       bool JitTest_catchfinally_tail_il.Test1::TestTryCatch(int32)
        IL_0010:  brtrue.s   IL_0020

        IL_0012:  ldstr      "try...catch test failed."
        IL_0017:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_001c:  ldc.i4.1
        IL_001d:  stloc.0
        IL_001e:  leave.s    IL_005f

        IL_0020:  ldc.i4.s   19
        IL_0022:  call       bool JitTest_catchfinally_tail_il.Test1::TestTryFinally(int32)
        IL_0027:  brtrue.s   IL_0032

        IL_0029:  ldc.i4.s   12
        IL_002b:  call       bool JitTest_catchfinally_tail_il.Test1::TestTryFinally(int32)
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
      IL_0001:  stsfld     bool JitTest_catchfinally_tail_il.Test1::globalFlag
      IL_0006:  ldc.i4.1
      IL_0007:  stsfld     bool JitTest_catchfinally_tail_il.Test1::globalFlag2
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

} // end of namespace 'JitTest_catchfinally_tail_il'

//*********** DISASSEMBLY COMPLETE ***********************
