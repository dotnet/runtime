// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly 'sizeof64_Target_64Bit_and_arm' { }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.namespace JitTest_sizeof64_Target_64Bit_and_arm_il
{
  .class private sequential ansi sealed beforefieldinit SimpleStruct
         extends [mscorlib]System.ValueType
  {
    .field public int32 m_int
    .field public unsigned int32 m_uint
    .field public unsigned int8 m_byte
    .field public int8 m_sbyte
    .field public char m_char
    .field public int16 m_short
    .field public unsigned int16 m_ushort
    .field public int64 m_long
    .field public unsigned int64 m_ulong
    .field public valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct[0...] ss_array
  }
  .class private sequential ansi sealed beforefieldinit RefComplexStruct
         extends [mscorlib]System.ValueType
  {
    .field public valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct ss1
    .field public valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct[0...] ss_array
    .field public valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct ss2
  }
  .class public sequential ansi sealed beforefieldinit Test
         extends [mscorlib]System.ValueType
  {
    .pack 1
    .size 1
    .method public hidebysig static int32
            Main() cil managed
    {
      // [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsNotX86Process))]
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                    string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                                  6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                                  2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                                  65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                                  43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                                  20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                                  6E 75 6C 6C 01 00 00 00 0F 49 73 4E 6F 74 58 38   // null.....IsNotX8
                                                                                                                  36 50 72 6F 63 65 73 73 00 00 )                   // 6Process..
      .entrypoint
      .maxstack  4
      .locals (int64 V_0,
               int32 V_1,
               valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct V_2,
               valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct V_3)
      IL_0000:  sizeof     JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0006:  conv.i1
      IL_0007:  conv.i8
      IL_0008:  stloc.0
      IL_0009:  ldloc.0
      IL_000a:  sizeof     JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0010:  ldloca.s   V_2
      IL_0012:  initobj    JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0018:  ldloc.2
      IL_0019:  stloc.2
      IL_001a:  ldloca.s   V_2
      IL_001c:  ldfld      valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct::ss1
      IL_0021:  stloc.3
      IL_0022:  ldloca.s   V_3
      IL_0024:  ldfld      int8 JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct::m_sbyte
      IL_0029:  conv.i4
      IL_002a:  add
      IL_002b:  conv.i8
      IL_002c:  add
      IL_002d:  stloc.0
      IL_002e:  ldloc.0
      IL_002f:  ldc.i4     176
      IL_0034:  conv.i8
      IL_0035:  ldloca.s   V_2
      IL_0037:  initobj    JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_003d:  ldloc.2
      IL_003e:  stloc.2
      IL_003f:  ldloca.s   V_2
      IL_0041:  ldfld      valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct::ss2
      IL_0046:  stloc.3
      IL_0047:  ldloca.s   V_3
      IL_0049:  ldfld      unsigned int16 JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct::m_ushort
      IL_004e:  conv.u8
      IL_004f:  sub
      IL_0050:  sizeof     JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0056:  conv.i8
      IL_0057:  sub
      IL_0058:  sub
      IL_0059:  stloc.0
      IL_005a:  ldloc.0
      IL_005b:  sizeof     JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0061:  conv.i8
      IL_0062:  ldloca.s   V_2
      IL_0064:  initobj    JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_006a:  ldloc.2
      IL_006b:  stloc.2
      IL_006c:  ldloca.s   V_2
      IL_006e:  ldfld      valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct::ss1
      IL_0073:  stloc.3
      IL_0074:  ldloca.s   V_3
      IL_0076:  ldfld      unsigned int32 JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct::m_uint
      IL_007b:  ldc.i4.1
      IL_007c:  add
      IL_007d:  conv.u8
      IL_007e:  mul
      IL_007f:  mul
      IL_0080:  stloc.0
      IL_0081:  ldloc.0
      IL_0082:  sizeof     JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0088:  ldloca.s   V_2
      IL_008a:  initobj    JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0090:  ldloc.2
      IL_0091:  stloc.2
      IL_0092:  ldloca.s   V_2
      IL_0094:  ldfld      valuetype JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct::ss2
      IL_0099:  stloc.3
      IL_009a:  ldloca.s   V_3
      IL_009c:  ldfld      unsigned int64 JitTest_sizeof64_Target_64Bit_and_arm_il.SimpleStruct::m_ulong
      IL_00a1:  ldc.i4.1
      IL_00a2:  conv.i8
      IL_00a3:  add
      IL_00a4:  conv.i4
      IL_00a5:  div
      IL_00a6:  conv.i8
      IL_00a7:  div
      IL_00a8:  stloc.0
      IL_00a9:  sizeof     JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_00af:  ldc.i4.s   88
      IL_00b1:  xor
      IL_00b2:  conv.i8
      IL_00b3:  ldloc.0
      IL_00b4:  or
      IL_00b5:  stloc.0
      IL_00b6:  sizeof     JitTest_sizeof64_Target_64Bit_and_arm_il.RefComplexStruct
      IL_00bc:  ldc.i4.s   -89
      IL_00be:  xor
      IL_00bf:  conv.i8
      IL_00c0:  ldloc.0
      IL_00c1:  and
      IL_00c2:  stloc.0
      IL_00c3:  ldloc.0
      IL_00c4:  ldc.i4.s   12
      IL_00c6:  conv.i8
      IL_00c7:  add
      IL_00c8:  conv.i4
      IL_00c9:  stloc.1
      IL_00ca:  br.s       IL_00cc
      IL_00cc:  ldloc.1
      IL_00cd:  ret
    }
  }
}
