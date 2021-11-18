// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa20b8571
{
  
}
.assembly extern xunit.core {}
.module '1'
// MVID: {9A90DFD6-02B6-45EC-9CEF-25C2CAF3BF19}

.file alignment 512

.class private auto ansi ILGEN_0xa20b8571
       extends [mscorlib]System.Object
{
  .field private static int32 field_0x0
  .field private static float32[] field_0x1
  .method private static int32  Method_0x15903b4b(int8 Arg_0x0,
                                                  float64 Arg_0x1,
                                                  unsigned int64 Arg_0x2,
                                                  unsigned int8 Arg_0x3) cil managed
  {
    // Code size       284 (0x11c)
    .maxstack  9
    .locals (native int V_0,
             unsigned int32[] V_1,
             native int[] V_2)
    IL_0000:  ldc.i4     0xb3fc875
    IL_0005:  stloc      V_0
    IL_0009:  ldc.i4     0xff
    IL_000e:  newarr     [mscorlib]System.UInt32
    IL_0013:  stloc      V_1
    IL_0017:  ldc.i4     0xff
    IL_001c:  newarr     [mscorlib]System.IntPtr
    IL_0021:  stloc      V_2
    IL_0025:  ldc.i4     0x74de023c
    IL_002a:  stsfld     int32 ILGEN_0xa20b8571::field_0x0
    IL_002f:  ldc.i4     0xff
    IL_0034:  newarr     [mscorlib]System.Single
    IL_0039:  stsfld     float32[] ILGEN_0xa20b8571::field_0x1
    IL_003e:  ldarg      Arg_0x1
    IL_0042:  conv.ovf.i
    IL_0043:  ldsfld     float32[] ILGEN_0xa20b8571::field_0x1
    IL_0048:  ldarg      Arg_0x0
    IL_004c:  ldarg      Arg_0x1
    IL_0050:  stelem.r4
    IL_0051:  ldloc      V_0
    IL_0055:  ldc.i4.7
    IL_0056:  ble.un     IL_0064

    IL_005b:  ldloc      V_0
    IL_005f:  br         IL_0068

    IL_0064:  ldloc      V_0
    IL_0068:  ldloc      V_0
    IL_006c:  brfalse    IL_0077

    IL_0071:  ldc.i4.3
    IL_0072:  br         IL_0078

    IL_0077:  ldc.i4.1
    IL_0078:  add
    IL_0079:  blt.un     IL_008b

    IL_007e:  ldloc      V_1
    IL_0082:  stloc      V_1
    IL_0086:  br         IL_00c1

    IL_008b:  ldloc      V_0
    IL_008f:  ldc.i4.1
    IL_0090:  add.ovf.un
    IL_0091:  brtrue     IL_00ad

    IL_0096:  ldc.i4.5
    IL_0097:  ldsfld     int32 ILGEN_0xa20b8571::field_0x0
    IL_009c:  blt        IL_00a7

    IL_00a1:  nop
    IL_00a2:  br         IL_00a8

    IL_00a7:  nop
    IL_00a8:  br         IL_00c1

    IL_00ad:  ldarg      Arg_0x2
    IL_00b1:  ldarg      Arg_0x2
    IL_00b5:  bgt.un     IL_00c0

    IL_00ba:  nop
    IL_00bb:  br         IL_00c1

    IL_00c0:  nop
    IL_00c1:  ldarg      Arg_0x1
    IL_00c5:  ldarg      Arg_0x1
    IL_00c9:  ble        IL_00dc

    IL_00ce:  ldc.r8     -2.9898347196621448e+210
    IL_00d7:  br         IL_00e6

    IL_00dc:  ldc.r8     -2.4831387566328225e+123
    IL_00e5:  ckfinite
    IL_00e6:  ldarg      Arg_0x1
    IL_00ea:  ldc.r4     -4.3014368e+008
    IL_00ef:  bge.un     IL_0112

    IL_00f4:  ldloc      V_0
    IL_00f8:  ldc.i4.s   -24
    IL_00fa:  beq        IL_0108

    IL_00ff:  ldarg      Arg_0x1
    IL_0103:  br         IL_010d

    IL_0108:  ldc.r4     (28 F3 A0 00)
    IL_010d:  br         IL_0119

    IL_0112:  ldsfld     float32[] ILGEN_0xa20b8571::field_0x1
    IL_0117:  ldc.i4.8
    IL_0118:  ldelem.r4
    IL_0119:  div
    IL_011a:  conv.u2
    IL_011b:  ret
  } // end of method ILGEN_0xa20b8571::Method_0x15903b4b

  .method private static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       34 (0x22)
    .maxstack  20
    .try {
    IL_0000:  ldc.i4     0xdb00f868
    IL_0005:  ldc.r8     2.9917397036046681e+294
    IL_000e:  ldc.i8     0x25ac0fd7ac80f732
    IL_0017:  ldc.i4     0x99a739e1
    IL_001c:  call       int32 ILGEN_0xa20b8571::Method_0x15903b4b(int8,
                                                                   float64,
                                                                   unsigned int64,
                                                                   unsigned int8)
    leave EOM
    } catch [mscorlib]System.OverflowException {
    leave EOM
    }
    EOM:
    ldc.i4 100  
    ret
  } // end of method ILGEN_0xa20b8571::Main

} // end of class ILGEN_0xa20b8571

//*********** DISASSEMBLY COMPLETE ***********************
