// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Runtime.Extensions { }
.assembly extern System.Console { }
.assembly RVAOrderingTest { }


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit RVAOrderingTest
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       106 (0x6a)
    .maxstack  2
    .locals init ([0] int32 result,
             [1] int32* pBegin,
             [2] int32& pinned V_2,
             [3] int32* pEnd,
             [4] int32& pinned V_4,
             [5] bool V_5,
             [6] int32 V_6)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  ldsflda    int32 RVAOrderingTest::s_First
    IL_0008:  stloc.2
    IL_0009:  ldloc.2
    IL_000a:  conv.u
    IL_000b:  stloc.1
    IL_000c:  ldsflda    int32 RVAOrderingTest::s_Last
    IL_0011:  stloc.s    V_4
    IL_0013:  ldloc.s    V_4
    IL_0015:  conv.u
    IL_0016:  stloc.3
    IL_0017:  ldloc.1
    IL_0018:  ldloc.3
    IL_0019:  call       int32 RVAOrderingTest::AddFields(int32*,
                                                          int32*)
    ldsflda    int8 RVAOrderingTest::s_Another1
    ldind.i1
    add
    
    IL_001e:  stloc.0
    IL_001f:  ldc.i4.0
    IL_0020:  conv.u
    IL_0021:  stloc.s    V_4
    IL_0023:  ldc.i4.0
    IL_0024:  conv.u
    IL_0025:  stloc.2
    IL_0026:  ldstr      "{0:x}"
    IL_002b:  ldloc.0
    IL_002c:  box        [System.Runtime]System.Int32
    IL_0031:  call       void [System.Console]System.Console::WriteLine(string,
                                                                        object)
    IL_0036:  nop
    IL_0037:  ldloc.0
    IL_0038:  ldc.i4     0x500000f2
    IL_003d:  ceq
    IL_003f:  stloc.s    V_5
    IL_0041:  ldloc.s    V_5
    IL_0043:  brfalse.s  IL_0057

    IL_0045:  nop
    IL_0046:  ldstr      "PASS"
    IL_004b:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0050:  nop
    IL_0051:  ldc.i4.s   100
    IL_0053:  stloc.s    V_6
    IL_0055:  br.s       IL_0067

    IL_0057:  ldstr      "FAIL"
    IL_005c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0061:  nop
    IL_0062:  ldc.i4.m1
    IL_0063:  stloc.s    V_6
    IL_0065:  br.s       IL_0067

    IL_0067:  ldloc.s    V_6
    IL_0069:  ret
  } // end of method RVAOrderingTest::Main

  .method private hidebysig static int32 
          AddFields(int32* pBegin,
                    int32* pEnd) cil managed
  {
    // Code size       34 (0x22)
    .maxstack  2
    .locals init ([0] int32 result,
             [1] bool V_1,
             [2] int32 V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  br.s       IL_0011

    IL_0005:  nop
    IL_0006:  ldloc.0
    IL_0007:  ldarg.0
    IL_0008:  ldind.i4
    IL_0009:  add
    IL_000a:  stloc.0
    IL_000b:  ldarg.0
    IL_000c:  ldc.i4.4
    IL_000d:  add
    IL_000e:  starg.s    pBegin
    IL_0010:  nop
    IL_0011:  ldarg.0
    IL_0012:  ldarg.1
    IL_0013:  cgt.un
    IL_0015:  ldc.i4.0
    IL_0016:  ceq
    IL_0018:  stloc.1
    IL_0019:  ldloc.1
    IL_001a:  brtrue.s   IL_0005
    IL_001c:  ldloc.0
    IL_001d:  stloc.2
    IL_001e:  br.s       IL_0020
    IL_0020:  ldloc.2
    IL_0021:  ret
  } // end of method RVAOrderingTest::AddFields

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method RVAOrderingTest::.ctor

  .field public static int32 s_First at D_00014000
  .field public static int32 s_Last at D_00014030
  .field public static int32 s_1 at D_00014008
  .field public static int8 s_Another1 at D_00014008
  .field public static int32 s_2 at D_00014010
  .field public static int32 s_3 at D_00014018
  .field public static int32 s_4 at D_00014020
  .field public static int32 s_5 at D_00014028
  
} // end of class RVAOrderingTest


.data D_00014000 = bytearray (00 00 00 00 00 00 00 00) 

.data D_00014008 = bytearray (01 00 00 10 00 00 00 00) 
.data D_00014010 = bytearray (10 00 00 10 00 00 00 00) 
.data D_00014018 = bytearray (20 00 00 10 00 00 00 00) 
.data D_00014020 = bytearray (40 00 00 10 00 00 00 00) 
.data D_00014028 = bytearray (80 00 00 10 00 00 00 00) 

.data D_00014030 = bytearray (00 00 00 00 00 00 00 00) 
