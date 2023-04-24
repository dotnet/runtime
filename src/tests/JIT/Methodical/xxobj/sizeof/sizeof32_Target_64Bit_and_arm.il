// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly 'sizeof32_Target_64Bit_and_arm' { }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.namespace JitTest_sizeof32_Target_64Bit_and_arm_il
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
  }
  .class private sequential ansi sealed beforefieldinit RefComplexStruct
         extends [mscorlib]System.ValueType
  {
    .field public valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct ss1
    .field public valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct[0...] ss_array
    .field public valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct ss2
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
      .locals (int32 V_0,
               int32 V_1,
               valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct V_2,
               valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct V_3)
      IL_0000:  sizeof     JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0006:  conv.i1
      IL_0007:  conv.i4
      IL_0008:  stloc.0
      IL_0009:  ldloc.0
      IL_000a:  sizeof     JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0010:  ldloca.s   V_2
      IL_0012:  initobj    JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0018:  ldloc.2
      IL_0019:  stloc.2
      IL_001a:  ldloca.s   V_2
      IL_001c:  ldfld      valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct::ss1
      IL_0021:  stloc.3
      IL_0022:  ldloca.s   V_3
      IL_0024:  ldfld      int8 JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct::m_sbyte
      IL_0029:  conv.i4
      IL_002a:  add
      IL_002b:  add
      IL_002c:  stloc.0
      IL_002d:  ldloc.0
      IL_002e:  ldc.i4     144
      IL_0033:  ldloca.s   V_2
      IL_0035:  initobj    JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_003b:  ldloc.2
      IL_003c:  stloc.2
      IL_003d:  ldloca.s   V_2
      IL_003f:  ldfld      valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct::ss2
      IL_0044:  stloc.3
      IL_0045:  ldloca.s   V_3
      IL_0047:  ldfld      unsigned int16 JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct::m_ushort
      IL_004c:  conv.i4
      IL_004d:  sub
      IL_004e:  sizeof     JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0054:  sub
      IL_0055:  sub
      IL_0056:  stloc.0
      IL_0057:  ldloc.0
      IL_0058:  sizeof     JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_005e:  ldloca.s   V_2
      IL_0060:  initobj    JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0066:  ldloc.2
      IL_0067:  stloc.2
      IL_0068:  ldloca.s   V_2
      IL_006a:  ldfld      valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct::ss1
      IL_006f:  stloc.3
      IL_0070:  ldloca.s   V_3
      IL_0072:  ldfld      unsigned int32 JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct::m_uint
      IL_0077:  ldc.i4.1
      IL_0078:  add
      IL_0079:  mul
      IL_007a:  mul
      IL_007b:  stloc.0
      IL_007c:  ldloc.0
      IL_007d:  sizeof     JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_0083:  ldloca.s   V_2
      IL_0085:  initobj    JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_008b:  ldloc.2
      IL_008c:  stloc.2
      IL_008d:  ldloca.s   V_2
      IL_008f:  ldfld      valuetype JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct::ss2
      IL_0094:  stloc.3
      IL_0095:  ldloca.s   V_3
      IL_0097:  ldfld      unsigned int64 JitTest_sizeof32_Target_64Bit_and_arm_il.SimpleStruct::m_ulong
      IL_009c:  ldc.i4.1
      IL_009d:  conv.i8
      IL_009e:  add
      IL_009f:  conv.i4
      IL_00a0:  div
      IL_00a1:  div
      IL_00a2:  stloc.0
      IL_00a3:  sizeof     JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_00a9:  ldc.i4.s   72
      IL_00ab:  xor
      IL_00ac:  ldloc.0
      IL_00ad:  or
      IL_00ae:  stloc.0
      IL_00af:  sizeof     JitTest_sizeof32_Target_64Bit_and_arm_il.RefComplexStruct
      IL_00b5:  ldc.i4.s   -73
      IL_00b7:  xor
      IL_00b8:  ldloc.0
      IL_00b9:  and
      IL_00ba:  stloc.0
      IL_00bb:  ldloc.0
      IL_00bc:  ldc.i4.s   28
      IL_00be:  add
      IL_00bf:  stloc.1
      IL_00c0:  br.s       IL_00c2
      IL_00c2:  ldloc.1
      IL_00c3:  ret
    }
  }
}
