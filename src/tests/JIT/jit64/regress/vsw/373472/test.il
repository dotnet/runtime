// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}




// Metadata version: v4.0.amd64ret
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly 'test_il'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {D7D039EB-2FFD-4888-8BC9-782CC0C93C1E}
.imagebase 0x10000000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000000310000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit StrideTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       40 (0x28)
    .maxstack  2
    .locals init (bool V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldc.i4.1
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  call       bool StrideTest::Test1()
    IL_0009:  and
    IL_000a:  stloc.0
    IL_000b:  ldloc.0
    IL_000c:  call       bool StrideTest::Test2()
    IL_0011:  and
    IL_0012:  stloc.0
    IL_0013:  ldloc.0
    IL_0014:  call       bool StrideTest::Test3()
    IL_0019:  and
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_001c:  brtrue.s   IL_0021

    IL_001e:  ldc.i4.1
    IL_001f:  br.s       IL_0023

    IL_0021:  ldc.i4.s   100
    IL_0023:  stloc.1
    IL_0024:  br.s       IL_0026

    IL_0026:  ldloc.1
    IL_0027:  ret
  } // end of method StrideTest::Main

  .method public hidebysig static bool  Test1() cil managed
  {
    // Code size       89 (0x59)
    .maxstack  3
    .locals init (uint32[] V_0,
             uint32 V_1,
             class [mscorlib]System.Exception V_2,
             bool V_3,
             bool V_4)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4     0x8ffffff
      IL_0007:  newarr     [mscorlib]System.UInt32
      IL_000c:  stloc.0
      IL_000d:  ldc.i4     0x8fffffe
      IL_0012:  stloc.1
      IL_0013:  br.s       IL_0025

      IL_0015:  nop
      IL_0016:  ldloc.0
      IL_0017:  ldloc.1
      IL_0018:  conv.u
      IL_0019:  ldc.i4.s   40
      IL_001b:  stelem.i4
      IL_001c:  nop
      IL_001d:  ldloc.1
      IL_001e:  ldc.i4     0xa0000001
      IL_0023:  sub
      IL_0024:  stloc.1
      IL_0025:  ldc.i4.1
      IL_0026:  stloc.s    V_4
      IL_0028:  br.s       IL_0015

    }  // end .try
    catch [mscorlib]System.IndexOutOfRangeException 
    {
      IL_002a:  pop
      IL_002b:  nop
      IL_002c:  ldc.i4.1
      IL_002d:  stloc.3
      IL_002e:  leave.s    IL_0056

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0030:  stloc.2
      IL_0031:  nop
      IL_0032:  ldstr      "test1 exception: {0}"
      IL_0037:  ldloc.2
      IL_0038:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_003d:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_0042:  nop
      IL_0043:  nop
      IL_0044:  leave.s    IL_0046

    }  // end handler
    IL_0046:  nop
    IL_0047:  ldstr      "Test1 failed"
    IL_004c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0051:  nop
    IL_0052:  ldc.i4.0
    IL_0053:  stloc.3
    IL_0054:  br.s       IL_0056

    IL_0056:  nop
    IL_0057:  ldloc.3
    IL_0058:  ret
  } // end of method StrideTest::Test1

  .method public hidebysig static bool  Test2() cil managed
  {
    // Code size       84 (0x54)
    .maxstack  3
    .locals init (uint32[] V_0,
             uint32 V_1,
             class [mscorlib]System.Exception V_2,
             bool V_3,
             bool V_4)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4     0x8ffffff
      IL_0007:  newarr     [mscorlib]System.UInt32
      IL_000c:  stloc.0
      IL_000d:  ldc.i4.0
      IL_000e:  stloc.1
      IL_000f:  br.s       IL_0020

      IL_0011:  nop
      IL_0012:  ldloc.0
      IL_0013:  ldloc.1
      IL_0014:  conv.u
      IL_0015:  ldloc.1
      IL_0016:  stelem.i4
      IL_0017:  nop
      IL_0018:  ldloc.1
      IL_0019:  ldc.i4     0xa0a0a0a0
      IL_001e:  sub
      IL_001f:  stloc.1
      IL_0020:  ldc.i4.1
      IL_0021:  stloc.s    V_4
      IL_0023:  br.s       IL_0011

    }  // end .try
    catch [mscorlib]System.IndexOutOfRangeException 
    {
      IL_0025:  pop
      IL_0026:  nop
      IL_0027:  ldc.i4.1
      IL_0028:  stloc.3
      IL_0029:  leave.s    IL_0051

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_002b:  stloc.2
      IL_002c:  nop
      IL_002d:  ldstr      "test2 exception: {0}"
      IL_0032:  ldloc.2
      IL_0033:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0038:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_003d:  nop
      IL_003e:  nop
      IL_003f:  leave.s    IL_0041

    }  // end handler
    IL_0041:  nop
    IL_0042:  ldstr      "Test2 failed"
    IL_0047:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004c:  nop
    IL_004d:  ldc.i4.0
    IL_004e:  stloc.3
    IL_004f:  br.s       IL_0051

    IL_0051:  nop
    IL_0052:  ldloc.3
    IL_0053:  ret
  } // end of method StrideTest::Test2

  .method public hidebysig static bool  Test3() cil managed
  {
    // Code size       113 (0x71)
    .maxstack  3
    .locals init (int32[] V_0,
             int64 V_1,
             class [mscorlib]System.Exception V_2,
             bool V_3,
             bool V_4)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4     0x8ffffff
      IL_0007:  newarr     [mscorlib]System.Int32
      IL_000c:  stloc.0
      IL_000d:  ldc.i4     0x8fffffe
      IL_0012:  conv.i8
      IL_0013:  stloc.1
      IL_0014:  br.s       IL_0027

      IL_0016:  nop
      IL_0017:  ldloc.0
      IL_0018:  ldloc.1
      IL_0019:  conv.ovf.i
      IL_001a:  ldloc.1
      IL_001b:  conv.i4
      IL_001c:  stelem.i4
      IL_001d:  nop
      IL_001e:  ldloc.1
      IL_001f:  ldc.i4     0xa0a0a0a0
      IL_0024:  conv.u8
      IL_0025:  sub
      IL_0026:  stloc.1
      IL_0027:  ldloc.1
      IL_0028:  ldc.i8     0xffffffff685f5b77
      IL_0031:  cgt
      IL_0033:  stloc.s    V_4
      IL_0035:  ldloc.s    V_4
      IL_0037:  brtrue.s   IL_0016

      IL_0039:  nop
      IL_003a:  leave.s    IL_005e

    }  // end .try
    catch [mscorlib]System.IndexOutOfRangeException 
    {
      IL_003c:  pop
      IL_003d:  nop
      IL_003e:  ldc.i4.1
      IL_003f:  stloc.3
      IL_0040:  leave.s    IL_006e

    }  // end handler
    catch [mscorlib]System.OverflowException 
    {
      IL_0042:  pop
      IL_0043:  nop
      IL_0044:  ldc.i4.1
      IL_0045:  stloc.3
      IL_0046:  leave.s    IL_006e

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0048:  stloc.2
      IL_0049:  nop
      IL_004a:  ldstr      "test3 exception: {0}"
      IL_004f:  ldloc.2
      IL_0050:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0055:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_005a:  nop
      IL_005b:  nop
      IL_005c:  leave.s    IL_005e

    }  // end handler
    IL_005e:  nop
    IL_005f:  ldstr      "Test3 failed"
    IL_0064:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0069:  nop
    IL_006a:  ldc.i4.0
    IL_006b:  stloc.3
    IL_006c:  br.s       IL_006e

    IL_006e:  nop
    IL_006f:  ldloc.3
    IL_0070:  ret
  } // end of method StrideTest::Test3

  .method public hidebysig static bool  Test4() cil managed
  {
    // Code size       78 (0x4e)
    .maxstack  3
    .locals init (uint64[] V_0,
             uint64 V_1,
             bool V_2,
             bool V_3)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4     0xfffffff
      IL_0007:  newarr     [mscorlib]System.UInt64
      IL_000c:  stloc.0
      IL_000d:  ldc.i8     0xa000000000000002
      IL_0016:  stloc.1
      IL_0017:  br.s       IL_002c

      IL_0019:  nop
      IL_001a:  ldloc.1
      IL_001b:  ldc.i8     0xa000000000000001
      IL_0024:  sub
      IL_0025:  stloc.1
      IL_0026:  ldloc.0
      IL_0027:  ldloc.1
      IL_0028:  conv.ovf.i.un
      IL_0029:  ldloc.1
      IL_002a:  stelem.i8
      IL_002b:  nop
      IL_002c:  ldc.i4.1
      IL_002d:  stloc.3
      IL_002e:  br.s       IL_0019

    }  // end .try
    catch [mscorlib]System.IndexOutOfRangeException 
    {
      IL_0030:  pop
      IL_0031:  nop
      IL_0032:  ldc.i4.1
      IL_0033:  stloc.2
      IL_0034:  leave.s    IL_004b

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0036:  pop
      IL_0037:  nop
      IL_0038:  nop
      IL_0039:  leave.s    IL_003b

    }  // end handler
    IL_003b:  nop
    IL_003c:  ldstr      "Test4 failed"
    IL_0041:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0046:  nop
    IL_0047:  ldc.i4.0
    IL_0048:  stloc.2
    IL_0049:  br.s       IL_004b

    IL_004b:  nop
    IL_004c:  ldloc.2
    IL_004d:  ret
  } // end of method StrideTest::Test4

  .method public hidebysig static bool  Test5() cil managed
  {
    // Code size       104 (0x68)
    .maxstack  3
    .locals init (uint64[] V_0,
             uint64 V_1,
             bool V_2,
             bool V_3)
    IL_0000:  nop
    .try
    {
      IL_0001:  nop
      IL_0002:  ldc.i4     0xfffffff
      IL_0007:  newarr     [mscorlib]System.UInt64
      IL_000c:  stloc.0
      IL_000d:  ldc.i8     0xa000000000000010
      IL_0016:  stloc.1
      IL_0017:  br.s       IL_0046

      IL_0019:  nop
      IL_001a:  ldloc.1
      IL_001b:  ldc.i8     0xa000000000000001
      IL_0024:  sub
      IL_0025:  stloc.1
      IL_0026:  br.s       IL_0034

      IL_0028:  nop
      IL_0029:  ldloc.0
      IL_002a:  ldloc.1
      IL_002b:  conv.ovf.i.un
      IL_002c:  ldloc.1
      IL_002d:  stelem.i8
      IL_002e:  ldloc.1
      IL_002f:  ldc.i4.1
      IL_0030:  conv.i8
      IL_0031:  sub
      IL_0032:  stloc.1
      IL_0033:  nop
      IL_0034:  ldloc.1
      IL_0035:  ldc.i4.0
      IL_0036:  conv.i8
      IL_0037:  clt.un
      IL_0039:  ldc.i4.0
      IL_003a:  ceq
      IL_003c:  stloc.3
      IL_003d:  ldloc.3
      IL_003e:  brtrue.s   IL_0028

      IL_0040:  ldloc.0
      IL_0041:  ldloc.1
      IL_0042:  conv.ovf.i.un
      IL_0043:  ldloc.1
      IL_0044:  stelem.i8
      IL_0045:  nop
      IL_0046:  ldc.i4.1
      IL_0047:  stloc.3
      IL_0048:  br.s       IL_0019

    }  // end .try
    catch [mscorlib]System.IndexOutOfRangeException 
    {
      IL_004a:  pop
      IL_004b:  nop
      IL_004c:  ldc.i4.1
      IL_004d:  stloc.2
      IL_004e:  leave.s    IL_0065

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_0050:  pop
      IL_0051:  nop
      IL_0052:  nop
      IL_0053:  leave.s    IL_0055

    }  // end handler
    IL_0055:  nop
    IL_0056:  ldstr      "Test5 failed"
    IL_005b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0060:  nop
    IL_0061:  ldc.i4.0
    IL_0062:  stloc.2
    IL_0063:  br.s       IL_0065

    IL_0065:  nop
    IL_0066:  ldloc.2
    IL_0067:  ret
  } // end of method StrideTest::Test5

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method StrideTest::.ctor

} // end of class StrideTest


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file t.res
